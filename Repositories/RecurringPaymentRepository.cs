using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class RecurringPaymentRepository : IRecurringPaymentRepository
{
    private readonly IConfiguration _configuration;

    public RecurringPaymentRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    // ============================================================
    // GET USER RECURRING PAYMENTS
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>> GetByUserIdAsync(
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
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
            WHERE rp.UserId = @UserId
            ORDER BY
                rp.IsActive DESC,
                rp.NextPaymentDate,
                rp.PaymentName;";

        return await connection.QueryAsync<RecurringPayment>(
            sql,
            new { UserId = userId });
    }


    // ============================================================
    // GET SINGLE RECURRING PAYMENT
    // ============================================================

    public async Task<RecurringPayment?> GetByIdAsync(
        int recurringPaymentId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
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

        return await connection.QueryFirstOrDefaultAsync<RecurringPayment>(
            sql,
            new
            {
                RecurringPaymentId = recurringPaymentId,
                UserId = userId
            });
    }


    // ============================================================
    // GET DUE PAYMENTS
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>> GetDuePaymentsAsync(
        DateTime today)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                rp.RecurringPaymentId,
                rp.UserId,
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
                AND (
                    rp.EndDate IS NULL
                    OR rp.NextPaymentDate <= rp.EndDate
                )
                AND c.IsActive = 1
            ORDER BY
                rp.NextPaymentDate;";

        return await connection.QueryAsync<RecurringPayment>(
            sql,
            new { Today = today.Date });
    }


    // ============================================================
    // ADD RECURRING PAYMENT
    // ============================================================

    public async Task AddAsync(
        RecurringPayment recurringPayment)
    {
        using var connection = GetConnection();

        const string sql = @"
            INSERT INTO RecurringPayments
            (
                UserId,
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
                @CategoryId,
                @PaymentName,
                @Amount,
                @Frequency,
                @StartDate,
                @NextPaymentDate,
                @EndDate,
                @IsActive
            );";

        await connection.ExecuteAsync(
            sql,
            recurringPayment);
    }


    // ============================================================
    // UPDATE RECURRING PAYMENT
    // ============================================================

    public async Task UpdateAsync(
        RecurringPayment recurringPayment)
    {
        using var connection = GetConnection();

        const string sql = @"
            UPDATE RecurringPayments
            SET
                CategoryId = @CategoryId,
                PaymentName = @PaymentName,
                Amount = @Amount,
                Frequency = @Frequency,
                StartDate = @StartDate,
                NextPaymentDate = @NextPaymentDate,
                EndDate = @EndDate,
                IsActive = @IsActive
            WHERE
                RecurringPaymentId = @RecurringPaymentId
                AND UserId = @UserId;";

        await connection.ExecuteAsync(
            sql,
            recurringPayment);
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
                        WHEN IsActive = 1 THEN 0
                        ELSE 1
                    END
            WHERE
                RecurringPaymentId = @RecurringPaymentId
                AND UserId = @UserId;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                RecurringPaymentId = recurringPaymentId,
                UserId = userId
            });
    }


    // ============================================================
    // PROCESS ONE OCCURRENCE
    // ============================================================

    public async Task ProcessOccurrenceAsync(
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
            const string insertTransactionSql = @"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Transactions
                    WHERE
                        RecurringPaymentId = @RecurringPaymentId
                        AND TransactionDate = @TransactionDate
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


            const string updateScheduleSql = @"
                UPDATE RecurringPayments
                SET
                    NextPaymentDate = @NextPaymentDate,
                    IsActive = @IsActive
                WHERE
                    RecurringPaymentId = @RecurringPaymentId;";

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


            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
}