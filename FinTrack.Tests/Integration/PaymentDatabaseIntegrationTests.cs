using Dapper;
using FinTrack.Models.Payments;
using FinTrack.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinTrack.Tests.Integration;

public class PaymentDatabaseIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task ReplayingSamePayment_ShouldNotCreateDuplicatePayment()
    {
        var sender = await CreateTestUserAsync();
        var receiver = await CreateTestUserAsync();

        try
        {
            await InsertIncomeAsync(
                sender.UserId,
                sender.IncomeCategoryId,
                1000m);

            var repository = CreatePaymentRepository();

            var idempotencyKey =
                $"integration-replay-{Guid.NewGuid():N}";

            var request = new PaymentRequest
            {
                FromUserId = sender.UserId,
                ToUserId = receiver.UserId,
                Amount = 250m,
                PaymentDate = DateTime.UtcNow,
                PaymentType = PaymentType.Manual,
                Description = "Idempotency integration test",
                ExpenseCategoryId = sender.ExpenseCategoryId,
                IdempotencyKey = idempotencyKey
            };

            var firstResult =
                await repository.ProcessTransferAsync(request);

            Assert.True(firstResult.Success);
            Assert.True(firstResult.PaymentId.HasValue);

            var secondResult =
                await repository.ProcessTransferAsync(request);

            Assert.True(secondResult.Success);
            Assert.Equal(
                firstResult.PaymentId,
                secondResult.PaymentId);

            await using var connection =
                await OpenConnectionAsync();

            var paymentCount =
                await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM Payments
                    WHERE PaymentId = @PaymentId;
                    """,
                    new
                    {
                        PaymentId = firstResult.PaymentId!.Value
                    });

            Assert.Equal(1, paymentCount);

            var senderBalance =
                await GetBalanceAsync(sender.UserId);

            var receiverBalance =
                await GetBalanceAsync(receiver.UserId);

            Assert.Equal(750m, senderBalance);
            Assert.Equal(250m, receiverBalance);
        }
        finally
        {
            await CleanupUsersAsync(
                sender.UserId,
                receiver.UserId,
                sender.IncomeCategoryId,
                sender.ExpenseCategoryId,
                receiver.IncomeCategoryId,
                receiver.ExpenseCategoryId);
        }
    }

    [Fact]
    public async Task FailedPayment_ShouldRollbackAllDatabaseChanges()
    {
        var sender = await CreateTestUserAsync();
        var receiver = await CreateTestUserAsync();

        try
        {
            await InsertIncomeAsync(
                sender.UserId,
                sender.IncomeCategoryId,
                1000m);

            var repository = CreatePaymentRepository();

            var idempotencyKey =
                $"integration-rollback-{Guid.NewGuid():N}";

            var request = new PaymentRequest
            {
                FromUserId = sender.UserId,
                ToUserId = receiver.UserId,
                Amount = 250m,
                PaymentDate = DateTime.UtcNow,
                PaymentType = PaymentType.Manual,
                Description = new string('X', 501),
                ExpenseCategoryId = sender.ExpenseCategoryId,
                IdempotencyKey = idempotencyKey
            };

            var result =
                await repository.ProcessTransferAsync(request);

            Assert.False(result.Success);

            await using var connection =
                await OpenConnectionAsync();

            var paymentCount =
                await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM Payments
                    WHERE FromUserId = @FromUserId
                      AND ToUserId = @ToUserId
                      AND IdempotencyKey = @IdempotencyKey;
                    """,
                    new
                    {
                        FromUserId = sender.UserId,
                        ToUserId = receiver.UserId,
                        IdempotencyKey = idempotencyKey
                    });

            var idempotencyCount =
                await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM PaymentIdempotency
                    WHERE InitiatedByUserId = @UserId
                      AND IdempotencyKey = @IdempotencyKey;
                    """,
                    new
                    {
                        UserId = sender.UserId,
                        IdempotencyKey = idempotencyKey
                    });

            var paymentTransactionCount =
                await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM Transactions
                    WHERE PaymentId IN
                    (
                        SELECT PaymentId
                        FROM Payments
                        WHERE FromUserId = @FromUserId
                          AND ToUserId = @ToUserId
                          AND IdempotencyKey = @IdempotencyKey
                    );
                    """,
                    new
                    {
                        FromUserId = sender.UserId,
                        ToUserId = receiver.UserId,
                        IdempotencyKey = idempotencyKey
                    });

            Assert.Equal(0, paymentCount);
            Assert.Equal(0, idempotencyCount);
            Assert.Equal(0, paymentTransactionCount);

            var senderBalance =
                await GetBalanceAsync(sender.UserId);

            var receiverBalance =
                await GetBalanceAsync(receiver.UserId);

            Assert.Equal(1000m, senderBalance);
            Assert.Equal(0m, receiverBalance);
        }
        finally
        {
            await CleanupUsersAsync(
                sender.UserId,
                receiver.UserId,
                sender.IncomeCategoryId,
                sender.ExpenseCategoryId,
                receiver.IncomeCategoryId,
                receiver.ExpenseCategoryId);
        }
    }

    private PaymentRepository CreatePaymentRepository()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            GetTestConnectionString()
                    })
                .Build();

        return new PaymentRepository(configuration);
    }

    private string GetTestConnectionString()
    {
        // Re-read the same configuration used by IntegrationTestBase
        // so PaymentRepository connects to FinTrack_Test.
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .Build();

        return configuration.GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "TestConnection is not configured.");
    }


    private async Task CleanupUsersAsync(
        int firstUserId,
        int secondUserId,
        int firstIncomeCategoryId,
        int firstExpenseCategoryId,
        int secondIncomeCategoryId,
        int secondExpenseCategoryId)
    {
        await using var connection = await OpenConnectionAsync();

        await using var transaction =
            (SqlTransaction)
            await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(
                """
                UPDATE Payments
                SET SenderTransactionId = NULL,
                    ReceiverTransactionId = NULL
                WHERE FromUserId IN (@FirstUserId, @SecondUserId)
                   OR ToUserId IN (@FirstUserId, @SecondUserId);
                """,
                new
                {
                    FirstUserId = firstUserId,
                    SecondUserId = secondUserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Transactions
                WHERE UserId IN (@FirstUserId, @SecondUserId);
                """,
                new
                {
                    FirstUserId = firstUserId,
                    SecondUserId = secondUserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM PaymentIdempotency
                WHERE InitiatedByUserId IN
                (
                    @FirstUserId,
                    @SecondUserId
                );
                """,
                new
                {
                    FirstUserId = firstUserId,
                    SecondUserId = secondUserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Payments
                WHERE FromUserId IN (@FirstUserId, @SecondUserId)
                   OR ToUserId IN (@FirstUserId, @SecondUserId);
                """,
                new
                {
                    FirstUserId = firstUserId,
                    SecondUserId = secondUserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Categories
                WHERE CategoryId IN
                (
                    @FirstIncomeCategoryId,
                    @FirstExpenseCategoryId,
                    @SecondIncomeCategoryId,
                    @SecondExpenseCategoryId
                );
                """,
                new
                {
                    FirstIncomeCategoryId = firstIncomeCategoryId,
                    FirstExpenseCategoryId = firstExpenseCategoryId,
                    SecondIncomeCategoryId = secondIncomeCategoryId,
                    SecondExpenseCategoryId = secondExpenseCategoryId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Users
                WHERE UserId IN (@FirstUserId, @SecondUserId);
                """,
                new
                {
                    FirstUserId = firstUserId,
                    SecondUserId = secondUserId
                },
                transaction);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

}
