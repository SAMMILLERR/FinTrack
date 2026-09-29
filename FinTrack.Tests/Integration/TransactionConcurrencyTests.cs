using Dapper;
using FinTrack.Models;
using Xunit;

namespace FinTrack.Tests.Integration;

public class TransactionConcurrencyTests : IntegrationTestBase
{
    [Fact]
    public async Task ConcurrentExpenses_ShouldNotAllowNegativeBalance()
    {
        var testData = await CreateTestUserAsync();

        try
        {
            // Start with exactly ₹1,000 available.
            await InsertIncomeAsync(
                testData.UserId,
                testData.IncomeCategoryId,
                1000m);

            var startingBalance = await GetBalanceAsync(testData.UserId);

            Assert.Equal(1000m, startingBalance);

            var expense1 = new Transaction
            {
                UserId = testData.UserId,
                CategoryId = testData.ExpenseCategoryId,
                Amount = 700m,
                TransactionDate = DateTime.Today,
                Description = "Concurrent expense A",
                TransactionType = "Expense"
            };

            var expense2 = new Transaction
            {
                UserId = testData.UserId,
                CategoryId = testData.ExpenseCategoryId,
                Amount = 700m,
                TransactionDate = DateTime.Today,
                Description = "Concurrent expense B",
                TransactionType = "Expense"
            };

            // Use separate repository instances so each call has its
            // own SQL connection and database transaction.
            var repository1 = CreateTransactionRepository();
            var repository2 = CreateTransactionRepository();

            var task1 = ExecuteExpenseAsync(repository1, expense1);
            var task2 = ExecuteExpenseAsync(repository2, expense2);

            var results = await Task.WhenAll(task1, task2);

            // Exactly one ₹700 expense must succeed.
            Assert.Equal(1, results.Count(r => r.Succeeded));

            // Exactly one must fail because the remaining balance
            // after the first successful expense is only ₹300.
            Assert.Equal(1, results.Count(r => !r.Succeeded));

            var finalBalance = await GetBalanceAsync(testData.UserId);

            // The account must never become negative.
            Assert.Equal(300m, finalBalance);

            var successfulExpenses = await GetExpenseCountAsync(testData.UserId);

            Assert.Equal(1, successfulExpenses);
        }
        finally
        {
            await CleanupTestDataAsync(
                testData.UserId,
                testData.IncomeCategoryId,
                testData.ExpenseCategoryId);
        }
    }

    private static async Task<ExpenseResult> ExecuteExpenseAsync(
        FinTrack.Repositories.TransactionRepository repository,
        Transaction transaction)
    {
        try
        {
            await repository.AddAsync(transaction);
            return new ExpenseResult(true, null);
        }
        catch (Exception ex)
        {
            return new ExpenseResult(false, ex);
        }
    }

    private async Task<int> GetExpenseCountAsync(int userId)
    {
        await using var connection = await OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM Transactions t
            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId
            INNER JOIN CategoryTypes ct
                ON ct.CategoryTypeId = c.CategoryTypeId
            WHERE
                t.UserId = @UserId
                AND ct.TypeName = 'Expense'
                AND t.Description IN
                (
                    'Concurrent expense A',
                    'Concurrent expense B'
                );
            """,
            new { UserId = userId });
    }

    private async Task CleanupTestDataAsync(
        int userId,
        int incomeCategoryId,
        int expenseCategoryId)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction =
            (Microsoft.Data.SqlClient.SqlTransaction)
            await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(
                """
                DELETE FROM Transactions
                WHERE UserId = @UserId;
                """,
                new { UserId = userId },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Categories
                WHERE CategoryId IN
                (
                    @IncomeCategoryId,
                    @ExpenseCategoryId
                );
                """,
                new
                {
                    IncomeCategoryId = incomeCategoryId,
                    ExpenseCategoryId = expenseCategoryId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                DELETE FROM Users
                WHERE UserId = @UserId;
                """,
                new { UserId = userId },
                transaction);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private sealed record ExpenseResult(
        bool Succeeded,
        Exception? Exception);
}
