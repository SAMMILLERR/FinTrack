using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class RecurringPaymentRepository
    : IRecurringPaymentRepository
{
    private readonly IConfiguration _configuration;

    public RecurringPaymentRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }


    // ============================================================
    // CONNECTION
    // ============================================================

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString(
                "DefaultConnection"));
    }


    // ============================================================
    // GET USER RECURRING PAYMENTS
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>>
        GetByUserIdAsync(int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
                rp.ToUserId,
                rp.CategoryId,
                rp.PaymentName,
                rp.Amount,
                rp.Frequency,
                rp.StartDate,
                rp.NextPaymentDate,
                rp.EndDate,
                rp.IsActive,
                rp.CreatedAt,
                c.CategoryName,
                ct.TypeName AS TransactionType
            FROM RecurringPayments rp

            INNER JOIN Categories c
                ON rp.CategoryId = c.CategoryId

            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId

            WHERE
                rp.UserId = @UserId

            ORDER BY
                rp.IsActive DESC,
                rp.NextPaymentDate,
                rp.PaymentName;";


        return await connection.QueryAsync<RecurringPayment>(
            sql,
            new
            {
                UserId = userId
            });
    }


    // ============================================================
    // GET SINGLE RECURRING PAYMENT
    // ============================================================

    public async Task<RecurringPayment?>
        GetByIdAsync(
            int recurringPaymentId,
            int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
                rp.ToUserId,
                rp.CategoryId,
                rp.PaymentName,
                rp.Amount,
                rp.Frequency,
                rp.StartDate,
                rp.NextPaymentDate,
                rp.EndDate,
                rp.IsActive,
                rp.CreatedAt,
                c.CategoryName,
                ct.TypeName AS TransactionType
            FROM RecurringPayments rp

            INNER JOIN Categories c
                ON rp.CategoryId = c.CategoryId

            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId

            WHERE
                rp.RecurringPaymentId = @RecurringPaymentId
                AND rp.UserId = @UserId;";


        return await connection
            .QueryFirstOrDefaultAsync<RecurringPayment>(
                sql,
                new
                {
                    RecurringPaymentId =
                        recurringPaymentId,

                    UserId =
                        userId
                });
    }


    // ============================================================
    // GET DUE PAYMENTS
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>>
        GetDuePaymentsAsync(DateTime today)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
                rp.ToUserId,
                rp.CategoryId,
                rp.PaymentName,
                rp.Amount,
                rp.Frequency,
                rp.StartDate,
                rp.NextPaymentDate,
                rp.EndDate,
                rp.IsActive,
                rp.CreatedAt,
                c.CategoryName,
                ct.TypeName AS TransactionType
            FROM RecurringPayments rp

            INNER JOIN Categories c
                ON rp.CategoryId = c.CategoryId

            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId

            WHERE
                rp.IsActive = 1

                AND rp.NextPaymentDate <= @Today

                AND
                (
                    rp.EndDate IS NULL
                    OR rp.NextPaymentDate <= rp.EndDate
                )

                AND c.IsActive = 1

            ORDER BY
                rp.NextPaymentDate;";


        return await connection.QueryAsync<RecurringPayment>(
            sql,
            new
            {
                Today = today.Date
            });
    }


    // ============================================================
    // ADD
    // ============================================================

    public async Task AddAsync(
        RecurringPayment recurringPayment)
    {
        using var connection = GetConnection();

        const string sql = @"
            INSERT INTO RecurringPayments
            (
                UserId,
                ToUserId,
                CategoryId,
                PaymentName,
                Amount,
                Frequency,
                StartDate,
                NextPaymentDate,
                EndDate,
                IsActive
            )
            VALUES
            (
                @UserId,
                @ToUserId,
                @CategoryId,
                @PaymentName,
                @Amount,
                @Frequency,
                @StartDate,
                @NextPaymentDate,
                @EndDate,
                @IsActive
            );

            SELECT CAST(
                SCOPE_IDENTITY()
                AS INT
            );";


        recurringPayment.RecurringPaymentId =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    recurringPayment.UserId,

                    recurringPayment.ToUserId,

                    recurringPayment.CategoryId,

                    recurringPayment.PaymentName,

                    recurringPayment.Amount,

                    recurringPayment.Frequency,

                    recurringPayment.StartDate,

                    recurringPayment.NextPaymentDate,

                    recurringPayment.EndDate,

                    recurringPayment.IsActive
                });
    }


    // ============================================================
    // UPDATE
    // ============================================================

    public async Task UpdateAsync(
        RecurringPayment recurringPayment)
    {
        using var connection = GetConnection();

        const string sql = @"
            UPDATE RecurringPayments

            SET
                ToUserId = @ToUserId,
                CategoryId = @CategoryId,
                PaymentName = @PaymentName,
                Amount = @Amount,
                Frequency = @Frequency,
                StartDate = @StartDate,
                NextPaymentDate = @NextPaymentDate,
                EndDate = @EndDate,
                IsActive = @IsActive

            WHERE
                RecurringPaymentId =
                    @RecurringPaymentId

                AND UserId =
                    @UserId;";


        await connection.ExecuteAsync(
            sql,
            new
            {
                recurringPayment.ToUserId,

                recurringPayment.CategoryId,

                recurringPayment.PaymentName,

                recurringPayment.Amount,

                recurringPayment.Frequency,

                recurringPayment.StartDate,

                recurringPayment.NextPaymentDate,

                recurringPayment.EndDate,

                recurringPayment.IsActive,

                recurringPayment.RecurringPaymentId,

                recurringPayment.UserId
            });
    }


    // ============================================================
    // DELETE
    // ============================================================

    public async Task DeleteAsync(
        int recurringPaymentId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            DELETE FROM RecurringPayments

            WHERE
                RecurringPaymentId =
                    @RecurringPaymentId

                AND UserId =
                    @UserId;";


        await connection.ExecuteAsync(
            sql,
            new
            {
                RecurringPaymentId =
                    recurringPaymentId,

                UserId =
                    userId
            });
    }


    // ============================================================
    // PAUSE / RESUME
    // ============================================================

    public async Task ToggleStatusAsync(
        int recurringPaymentId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            UPDATE RecurringPayments

            SET
                IsActive =
                    CASE
                        WHEN IsActive = 1
                            THEN 0

                        ELSE 1
                    END

            WHERE
                RecurringPaymentId =
                    @RecurringPaymentId

                AND UserId =
                    @UserId;";


        await connection.ExecuteAsync(
            sql,
            new
            {
                RecurringPaymentId =
                    recurringPaymentId,

                UserId =
                    userId
            });
    }


    // ============================================================
    // PROCESS ONE OCCURRENCE
    // ============================================================
    //
    // IMPORTANT:
    //
    // The new recurring-payment flow processes payments through:
    //
    // RecurringPaymentService
    //        ↓
    // PaymentService
    //        ↓
    // PaymentRepository
    //
    // Therefore this legacy method is retained only because
    // IRecurringPaymentRepository currently requires it.
    //
    // It is NOT used by the new RecurringPaymentService.
    //
    // ============================================================

    public async Task<bool> ProcessOccurrenceAsync(
        RecurringPayment recurringPayment,
        DateTime transactionDate,
        DateTime nextPaymentDate,
        bool isActive)
    {
        using var connection = GetConnection();

        await connection.OpenAsync();

        using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            var budgetMonth =
                transactionDate.Month;

            var budgetYear =
                transactionDate.Year;


            // ========================================================
            // FIND AND LOCK BUDGET
            // ========================================================

            const string budgetSql = @"
                SELECT TOP 1
                    b.LimitAmount

                FROM Budgets b WITH (UPDLOCK, HOLDLOCK)

                WHERE
                    b.UserId = @UserId

                    AND b.CategoryId = @CategoryId

                    AND b.BudgetMonth = @BudgetMonth

                    AND b.BudgetYear = @BudgetYear;";


            var budgetLimit =
                await connection
                    .QueryFirstOrDefaultAsync<decimal?>(
                        budgetSql,
                        new
                        {
                            UserId =
                                recurringPayment.UserId,

                            CategoryId =
                                recurringPayment.CategoryId,

                            BudgetMonth =
                                budgetMonth,

                            BudgetYear =
                                budgetYear
                        },
                        transaction);


            // ========================================================
            // NO BUDGET
            // ========================================================

            if (!budgetLimit.HasValue)
            {
                await transaction.RollbackAsync();

                return false;
            }


            // ========================================================
            // CURRENT CATEGORY SPENDING
            // ========================================================

            const string spendingSql = @"
                SELECT
                    ISNULL(
                        SUM(Amount),
                        0
                    )

                FROM Transactions WITH (UPDLOCK, HOLDLOCK)

                WHERE
                    UserId = @UserId

                    AND CategoryId = @CategoryId

                    AND TransactionType = 'Expense'

                    AND TransactionDate >=
                        DATEFROMPARTS(
                            @BudgetYear,
                            @BudgetMonth,
                            1
                        )

                    AND TransactionDate <
                        DATEADD(
                            MONTH,
                            1,
                            DATEFROMPARTS(
                                @BudgetYear,
                                @BudgetMonth,
                                1
                            )
                        );";


            var currentSpending =
                await connection
                    .ExecuteScalarAsync<decimal>(
                        spendingSql,
                        new
                        {
                            UserId =
                                recurringPayment.UserId,

                            CategoryId =
                                recurringPayment.CategoryId,

                            BudgetMonth =
                                budgetMonth,

                            BudgetYear =
                                budgetYear
                        },
                        transaction);


            // ========================================================
            // BUDGET CHECK
            // ========================================================

            var projectedSpending =
                currentSpending +
                recurringPayment.Amount;


            if (projectedSpending >
                budgetLimit.Value)
            {
                await transaction.RollbackAsync();

                return false;
            }


            // ========================================================
            // INSERT LEGACY TRANSACTION
            // ========================================================

            const string insertTransactionSql = @"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Transactions

                    WHERE
                        RecurringPaymentId =
                            @RecurringPaymentId

                        AND TransactionDate =
                            @TransactionDate

                        AND UserId =
                            @UserId
                )

                BEGIN

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
                        @TransactionDate,
                        @Description,
                        @TransactionType,
                        @RecurringPaymentId
                    );

                END;";


            await connection.ExecuteAsync(
                insertTransactionSql,
                new
                {
                    UserId =
                        recurringPayment.UserId,

                    CategoryId =
                        recurringPayment.CategoryId,

                    Amount =
                        recurringPayment.Amount,

                    TransactionDate =
                        transactionDate.Date,

                    Description =
                        recurringPayment.PaymentName,

                    TransactionType =
                        recurringPayment.TransactionType,

                    RecurringPaymentId =
                        recurringPayment.RecurringPaymentId
                },
                transaction);


            // ========================================================
            // ADVANCE SCHEDULE
            // ========================================================

            const string updateScheduleSql = @"
                UPDATE RecurringPayments

                SET
                    NextPaymentDate =
                        @NextPaymentDate,

                    IsActive =
                        @IsActive

                WHERE
                    RecurringPaymentId =
                        @RecurringPaymentId;";


            await connection.ExecuteAsync(
                updateScheduleSql,
                new
                {
                    NextPaymentDate =
                        nextPaymentDate.Date,

                    IsActive =
                        isActive,

                    RecurringPaymentId =
                        recurringPayment.RecurringPaymentId
                },
                transaction);


            // ========================================================
            // COMMIT
            // ========================================================

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Preserve original exception.
            }

            throw;
        }
    }
}