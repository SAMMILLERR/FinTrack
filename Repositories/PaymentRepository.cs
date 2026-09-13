using Dapper;
using FinTrack.Models.Payments;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

internal sealed class UserPaymentInfo
{
    public int UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string GetDisplayName()
    {
        var name = string.Join(
            " ",
            new[] { FirstName, LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

        return string.IsNullOrWhiteSpace(name)
            ? $"User #{UserId}"
            : name;
    }
}


public class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;


    public PaymentRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }


    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _connectionString);
    }


    // =========================================================
    // PROCESS TRANSFER
    // =========================================================

    public async Task<PaymentResult> ProcessTransferAsync(
        PaymentRequest request)
    {
        using var connection =
            GetConnection();

        await connection.OpenAsync();


        using var transaction =
            await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);


        try
        {
            // =================================================
            // VALIDATION
            // =================================================

            if (request.FromUserId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid sender account.");
            }


            if (request.ToUserId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid receiver account.");
            }


            if (request.FromUserId ==
                request.ToUserId)
            {
                throw new InvalidOperationException(
                    "Sender and receiver cannot be the same account.");
            }


            if (request.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "Transfer amount must be greater than zero.");
            }


            if (request.PaymentDate == default)
            {
                throw new InvalidOperationException(
                    "Please provide a valid payment date.");
            }


            if (request.ExpenseCategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "Please select an expense category.");
            }


            // =================================================
            // CHECK USERS
            // =================================================

            const string usersSql = @"
                SELECT
                    UserId,
                    FirstName,
                    LastName
                FROM Users WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId IN
                (
                    @FromUserId,
                    @ToUserId
                );";


            var users =
                await connection.QueryAsync<UserPaymentInfo>(
                    usersSql,
                    new
                    {
                        request.FromUserId,
                        request.ToUserId
                    },
                    transaction);


            var userList = users.ToList();

            var userIds =
                userList.Select(u => u.UserId).ToHashSet();

            var senderUser =
                userList.FirstOrDefault(u =>
                    u.UserId == request.FromUserId);

            var receiverUser =
                userList.FirstOrDefault(u =>
                    u.UserId == request.ToUserId);


            if (!userIds.Contains(
                    request.FromUserId))
            {
                throw new InvalidOperationException(
                    "Sender account was not found.");
            }


            if (!userIds.Contains(
                    request.ToUserId))
            {
                throw new InvalidOperationException(
                    "Receiver account was not found.");
            }

            var senderName =
                senderUser?.GetDisplayName()
                ?? $"User #{request.FromUserId}";

            var receiverName =
                receiverUser?.GetDisplayName()
                ?? $"User #{request.ToUserId}";


            // =================================================
            // VALIDATE SENDER EXPENSE CATEGORY
            // =================================================

            const string expenseCategorySql = @"
                SELECT
                    c.CategoryId
                FROM Categories c
                INNER JOIN CategoryTypes ct
                    ON ct.CategoryTypeId =
                       c.CategoryTypeId
                WHERE
                    c.CategoryId =
                        @ExpenseCategoryId
                    AND ct.TypeName = 'Expense'
                    AND c.IsActive = 1;";


            var expenseCategoryId =
                await connection.QueryFirstOrDefaultAsync<int>(
                    expenseCategorySql,
                    new
                    {
                        request.ExpenseCategoryId
                    },
                    transaction);


            if (expenseCategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "The selected category is not a valid active expense category.");
            }


            // =================================================
            // FIND INCOME CATEGORY FOR RECEIVER
            // =================================================

            const string incomeCategorySql = @"
                SELECT TOP 1
                    c.CategoryId
                FROM Categories c
                INNER JOIN CategoryTypes ct
                    ON ct.CategoryTypeId =
                       c.CategoryTypeId
                WHERE
                    ct.TypeName = 'Income'
                    AND c.IsActive = 1
                ORDER BY
                    c.CategoryId;";


            var incomeCategoryId =
                await connection.QueryFirstOrDefaultAsync<int>(
                    incomeCategorySql,
                    transaction: transaction);


            if (incomeCategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "No active income category is configured.");
            }


            // =================================================
            // CALCULATE SENDER BALANCE
            // =================================================

            const string senderBalanceSql = @"
                SELECT
                    COALESCE(
                        SUM(
                            CASE
                                WHEN ct.TypeName = 'Income'
                                    THEN t.Amount

                                WHEN ct.TypeName = 'Expense'
                                    THEN -t.Amount

                                ELSE 0
                            END
                        ),
                        0
                    )
                FROM Transactions t

                INNER JOIN Categories c
                    ON c.CategoryId =
                       t.CategoryId

                INNER JOIN CategoryTypes ct
                    ON ct.CategoryTypeId =
                       c.CategoryTypeId

                WHERE
                    t.UserId = @UserId;";


            var senderBalance =
                await connection.ExecuteScalarAsync<decimal>(
                    senderBalanceSql,
                    new
                    {
                        UserId =
                            request.FromUserId
                    },
                    transaction);


            // =================================================
            // HARD BALANCE CONSTRAINT
            // =================================================

            if (request.Amount >
                senderBalance)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance. " +
                    $"Available balance is ₹{senderBalance:N2}, " +
                    $"but this transfer requires ₹{request.Amount:N2}.");
            }


            // =================================================
            // CHECK MONTHLY CATEGORY BUDGET
            // =================================================

            const string budgetSql = @"
                SELECT LimitAmount
                FROM Budgets WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId = @UserId
                  AND CategoryId = @CategoryId
                  AND BudgetMonth = @BudgetMonth
                  AND BudgetYear = @BudgetYear;";

            var budgetLimit =
                await connection.QueryFirstOrDefaultAsync<decimal?>(
                    budgetSql,
                    new
                    {
                        UserId = request.FromUserId,
                        CategoryId = expenseCategoryId,
                        BudgetMonth = request.PaymentDate.Month,
                        BudgetYear = request.PaymentDate.Year
                    },
                    transaction);

            if (budgetLimit.HasValue)
            {
                const string spentSql = @"
                    SELECT COALESCE(SUM(Amount), 0)
                    FROM Transactions
                    WHERE UserId = @UserId
                      AND CategoryId = @CategoryId
                      AND TransactionType = 'Expense'
                      AND TransactionDate >= DATEFROMPARTS(@BudgetYear, @BudgetMonth, 1)
                      AND TransactionDate < DATEADD(MONTH, 1, DATEFROMPARTS(@BudgetYear, @BudgetMonth, 1));";

                var spentAmount =
                    await connection.ExecuteScalarAsync<decimal>(
                        spentSql,
                        new
                        {
                            UserId = request.FromUserId,
                            CategoryId = expenseCategoryId,
                            BudgetMonth = request.PaymentDate.Month,
                            BudgetYear = request.PaymentDate.Year
                        },
                        transaction);

                var projectedSpending = spentAmount + request.Amount;

                if (projectedSpending > budgetLimit.Value)
                {
                    throw new InvalidOperationException(
                        $"Payment rejected. This payment would exceed your monthly budget for the selected category. " +
                        $"Budget limit is ₹{budgetLimit.Value:N2}, current spending is ₹{spentAmount:N2}, " +
                        $"and this payment would bring spending to ₹{projectedSpending:N2}.");
                }
            }


            // =================================================
            // CREATE PAYMENT RECORD
            // =================================================

            const string paymentSql = @"
                INSERT INTO Payments
                (
                    FromUserId,
                    ToUserId,
                    Amount,
                    PaymentDate,
                    PaymentType,
                    Status,
                    Description
                )
                VALUES
                (
                    @FromUserId,
                    @ToUserId,
                    @Amount,
                    @PaymentDate,
                    @PaymentType,
                    @Status,
                    @Description
                );

                SELECT CAST(
                    SCOPE_IDENTITY()
                    AS INT
                );";


            var paymentId =
                await connection.ExecuteScalarAsync<int>(
                    paymentSql,
                    new
                    {
                        request.FromUserId,
                        request.ToUserId,
                        request.Amount,
                        request.PaymentDate,

                        PaymentType =
                            request.PaymentType.ToString(),

                        Status =
                            PaymentStatus.Processing.ToString(),

                        request.Description
                    },
                    transaction);


            // =================================================
            // DEBIT SENDER
            // =================================================

            const string senderTransactionSql = @"
                INSERT INTO Transactions
                (
                    UserId,
                    CategoryId,
                    Amount,
                    TransactionDate,
                    Description,
                    TransactionType,
                    RecurringPaymentId
                )
                VALUES
                (
                    @UserId,
                    @CategoryId,
                    @Amount,
                    @PaymentDate,
                    @Description,
                    'Expense',
                    @RecurringPaymentId
                );

                SELECT CAST(
                    SCOPE_IDENTITY()
                    AS INT
                );";


            var senderTransactionId =
                await connection.ExecuteScalarAsync<int>(
                    senderTransactionSql,
                    new
                    {
                        UserId =
                            request.FromUserId,

                        CategoryId =
                            expenseCategoryId,

                        request.Amount,

                        request.PaymentDate,

                        Description =
                            $"Payment to {receiverName}" +
                            (
                                string.IsNullOrWhiteSpace(
                                    request.Description)

                                ? string.Empty

                                : $" - {request.Description}"
                            ),

                        RecurringPaymentId =
                            request.RecurringPaymentId
                    },
                    transaction);


            // =================================================
            // CREDIT RECEIVER
            // =================================================

            const string receiverTransactionSql = @"
                INSERT INTO Transactions
                (
                    UserId,
                    CategoryId,
                    Amount,
                    TransactionDate,
                    Description,
                    TransactionType,
                    RecurringPaymentId
                )
                VALUES
                (
                    @UserId,
                    @CategoryId,
                    @Amount,
                    @PaymentDate,
                    @Description,
                    'Income',
                    @RecurringPaymentId
                );

                SELECT CAST(
                    SCOPE_IDENTITY()
                    AS INT
                );";


            var receiverTransactionId =
                await connection.ExecuteScalarAsync<int>(
                    receiverTransactionSql,
                    new
                    {
                        UserId =
                            request.ToUserId,

                        CategoryId =
                            incomeCategoryId,

                        request.Amount,

                        request.PaymentDate,

                        Description =
                            $"Payment from {senderName}" +
                            (
                                string.IsNullOrWhiteSpace(
                                    request.Description)

                                ? string.Empty

                                : $" - {request.Description}"
                            ),

                        RecurringPaymentId =
                            request.RecurringPaymentId
                    },
                    transaction);


            // =================================================
            // UPDATE PAYMENT STATUS
            // =================================================

            const string completedSql = @"
                UPDATE Payments

                SET
                    Status = @Status,

                    SenderTransactionId =
                        @SenderTransactionId,

                    ReceiverTransactionId =
                        @ReceiverTransactionId,

                    CompletedOn =
                        SYSUTCDATETIME()

                WHERE
                    PaymentId = @PaymentId;";


            await connection.ExecuteAsync(
                completedSql,
                new
                {
                    PaymentId =
                        paymentId,

                    Status =
                        PaymentStatus.Completed.ToString(),

                    SenderTransactionId =
                        senderTransactionId,

                    ReceiverTransactionId =
                        receiverTransactionId
                },
                transaction);


            // =================================================
            // CALCULATE NEW BALANCES
            // =================================================

            var newSenderBalance =
                senderBalance -
                request.Amount;


            const string receiverBalanceSql = @"
                SELECT
                    COALESCE(
                        SUM(
                            CASE
                                WHEN ct.TypeName = 'Income'
                                    THEN t.Amount

                                WHEN ct.TypeName = 'Expense'
                                    THEN -t.Amount

                                ELSE 0
                            END
                        ),
                        0
                    )
                FROM Transactions t

                INNER JOIN Categories c
                    ON c.CategoryId =
                       t.CategoryId

                INNER JOIN CategoryTypes ct
                    ON ct.CategoryTypeId =
                       c.CategoryTypeId

                WHERE
                    t.UserId = @UserId;";


            var receiverBalance =
                await connection.ExecuteScalarAsync<decimal>(
                    receiverBalanceSql,
                    new
                    {
                        UserId =
                            request.ToUserId
                    },
                    transaction);


            // =================================================
            // COMMIT EVERYTHING
            // =================================================

            await transaction.CommitAsync();


            return new PaymentResult
            {
                Success = true,

                Status =
                    PaymentStatus.Completed,

                Message =
                    "Payment completed successfully.",

                PaymentId =
                    paymentId,

                SenderTransactionId =
                    senderTransactionId,

                ReceiverTransactionId =
                    receiverTransactionId,

                SenderBalance =
                    newSenderBalance,

                ReceiverBalance =
                    receiverBalance
            };
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Preserve original exception.
            }


            return new PaymentResult
            {
                Success = false,

                Status =
                    PaymentStatus.Failed,

                Message =
                    ex is InvalidOperationException
                        ? ex.Message
                        : "The payment could not be processed."
            };
        }
    }
}