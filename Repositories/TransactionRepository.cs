using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly string _connectionString;

    public TransactionRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    // =========================================================
    // CONNECTION
    // =========================================================

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    // =========================================================
    // GET USER TRANSACTIONS
    // =========================================================

    public async Task<IEnumerable<Transaction>> GetByUserAsync(
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                t.TransactionId,
                t.UserId,
                t.CategoryId,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.TransactionType,
                t.RecurringPaymentId,
                c.CategoryName
            FROM Transactions t
            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId
            WHERE t.UserId = @UserId
            ORDER BY
                t.TransactionDate DESC,
                t.TransactionId DESC;";

        return await connection.QueryAsync<Transaction>(
            sql,
            new
            {
                UserId = userId
            });
    }

    // =========================================================
    // GET TRANSACTION BY ID
    // =========================================================

    public async Task<Transaction?> GetByIdAsync(
        int transactionId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                t.TransactionId,
                t.UserId,
                t.CategoryId,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.TransactionType,
                t.RecurringPaymentId,
                c.CategoryName
            FROM Transactions t
            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId
            WHERE
                t.TransactionId = @TransactionId
                AND t.UserId = @UserId;";

        return await connection.QueryFirstOrDefaultAsync<Transaction>(
            sql,
            new
            {
                TransactionId = transactionId,
                UserId = userId
            });
    }

    // =========================================================
    // ADD TRANSACTION
    // =========================================================

    public async Task AddAsync(
        Transaction transaction)
    {
        using var connection = GetConnection();

        await connection.OpenAsync();

        using var dbTransaction =
            await connection.BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // VALIDATION
            // -------------------------------------------------

            if (transaction.UserId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid user.");
            }

            if (transaction.CategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "Please select a valid category.");
            }

            if (transaction.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "Transaction amount must be greater than zero.");
            }

            if (transaction.TransactionDate == default)
            {
                throw new InvalidOperationException(
                    "Please provide a valid transaction date.");
            }

            // -------------------------------------------------
            // EXPENSE BALANCE CHECK
            // -------------------------------------------------

            if (string.Equals(
                    transaction.TransactionType,
                    "Expense",
                    StringComparison.OrdinalIgnoreCase))
            {
                const string balanceSql = @"
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
                        ON c.CategoryId = t.CategoryId

                    INNER JOIN CategoryTypes ct
                        ON ct.CategoryTypeId = c.CategoryTypeId

                    WHERE t.UserId = @UserId;";

                var availableBalance =
                    await connection.ExecuteScalarAsync<decimal>(
                        balanceSql,
                        new
                        {
                            UserId = transaction.UserId
                        },
                        dbTransaction);

                // -------------------------------------------------
                // HARD FINANCIAL CONSTRAINT
                // -------------------------------------------------

                if (transaction.Amount > availableBalance)
                {
                    throw new InvalidOperationException(
                        $"Insufficient balance. " +
                        $"Available balance is ₹{availableBalance:N2}, " +
                        $"but this expense requires ₹{transaction.Amount:N2}.");
                }
            }

            // -------------------------------------------------
            // INSERT
            // -------------------------------------------------

            const string insertSql = @"
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

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            transaction.TransactionId =
                await connection.ExecuteScalarAsync<int>(
                    insertSql,
                    transaction,
                    dbTransaction);

            // -------------------------------------------------
            // COMMIT
            // -------------------------------------------------

            await dbTransaction.CommitAsync();
        }
        catch
        {
            // -------------------------------------------------
            // ROLLBACK
            // -------------------------------------------------

            await dbTransaction.RollbackAsync();

            throw;
        }
    }

    // =========================================================
    // UPDATE TRANSACTION
    // =========================================================

    public async Task UpdateAsync(
        Transaction transaction)
    {
        using var connection = GetConnection();

        await connection.OpenAsync();

        using var dbTransaction =
            await connection.BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // VALIDATION
            // -------------------------------------------------

            if (transaction.UserId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid user.");
            }

            if (transaction.TransactionId <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid transaction.");
            }

            if (transaction.CategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "Please select a valid category.");
            }

            if (transaction.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "Transaction amount must be greater than zero.");
            }

            // -------------------------------------------------
            // GET ORIGINAL TRANSACTION
            // -------------------------------------------------

            const string originalSql = @"
                SELECT
                    TransactionId,
                    UserId,
                    CategoryId,
                    Amount,
                    TransactionDate,
                    Description,
                    TransactionType,
                    RecurringPaymentId
                FROM Transactions
                WHERE
                    TransactionId = @TransactionId
                    AND UserId = @UserId;";

            var original =
                await connection.QueryFirstOrDefaultAsync<Transaction>(
                    originalSql,
                    new
                    {
                        TransactionId =
                            transaction.TransactionId,

                        UserId =
                            transaction.UserId
                    },
                    dbTransaction);

            if (original == null)
            {
                throw new InvalidOperationException(
                    "Transaction not found.");
            }

            // -------------------------------------------------
            // EXPENSE BALANCE CHECK
            // -------------------------------------------------

            if (string.Equals(
                    transaction.TransactionType,
                    "Expense",
                    StringComparison.OrdinalIgnoreCase))
            {
                const string balanceSql = @"
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
                        ON c.CategoryId = t.CategoryId

                    INNER JOIN CategoryTypes ct
                        ON ct.CategoryTypeId = c.CategoryTypeId

                    WHERE
                        t.UserId = @UserId
                        AND t.TransactionId <> @TransactionId;";

                var balanceWithoutOriginal =
                    await connection.ExecuteScalarAsync<decimal>(
                        balanceSql,
                        new
                        {
                            UserId =
                                transaction.UserId,

                            TransactionId =
                                transaction.TransactionId
                        },
                        dbTransaction);

                if (transaction.Amount >
                    balanceWithoutOriginal)
                {
                    throw new InvalidOperationException(
                        $"Insufficient balance. " +
                        $"Available balance is ₹{balanceWithoutOriginal:N2}, " +
                        $"but this expense requires ₹{transaction.Amount:N2}.");
                }
            }

            // -------------------------------------------------
            // UPDATE
            // -------------------------------------------------

            const string updateSql = @"
                UPDATE Transactions
                SET
                    CategoryId = @CategoryId,
                    Amount = @Amount,
                    TransactionDate = @TransactionDate,
                    Description = @Description
                WHERE
                    TransactionId = @TransactionId
                    AND UserId = @UserId;";

            await connection.ExecuteAsync(
                updateSql,
                transaction,
                dbTransaction);

            // -------------------------------------------------
            // COMMIT
            // -------------------------------------------------

            await dbTransaction.CommitAsync();
        }
        catch
        {
            // -------------------------------------------------
            // ROLLBACK
            // -------------------------------------------------

            await dbTransaction.RollbackAsync();

            throw;
        }
    }

    // =========================================================
    // DELETE TRANSACTION
    // =========================================================

    public async Task DeleteAsync(
        int transactionId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            DELETE FROM Transactions
            WHERE
                TransactionId = @TransactionId
                AND UserId = @UserId;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                TransactionId = transactionId,
                UserId = userId
            });
    }

    // =========================================================
    // GET ALL TRANSACTIONS
    // =========================================================

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                t.TransactionId,
                t.UserId,
                u.FirstName + ' ' + u.LastName AS UserName,
                t.CategoryId,
                c.CategoryName,
                ct.TypeName AS TransactionType,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.RecurringPaymentId,
                t.CreatedOn
            FROM Transactions t
            INNER JOIN Users u
                ON t.UserId = u.UserId
            INNER JOIN Categories c
                ON t.CategoryId = c.CategoryId
            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId
            ORDER BY
                t.TransactionDate DESC,
                t.TransactionId DESC;";

        return await connection.QueryAsync<Transaction>(
            sql);
    }

    // =========================================================
    // SEARCH TRANSACTIONS
    // =========================================================

    public async Task<IEnumerable<Transaction>> SearchAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                t.TransactionId,
                t.UserId,
                u.FirstName + ' ' + u.LastName AS UserName,
                t.CategoryId,
                c.CategoryName,
                ct.TypeName AS TransactionType,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.RecurringPaymentId,
                t.CreatedOn
            FROM Transactions t
            INNER JOIN Users u
                ON t.UserId = u.UserId
            INNER JOIN Categories c
                ON t.CategoryId = c.CategoryId
            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId
            WHERE
                (@UserId IS NULL OR
                    t.UserId = @UserId)
                AND
                (@FromDate IS NULL OR
                    t.TransactionDate >= @FromDate)
                AND
                (@ToDate IS NULL OR
                    t.TransactionDate < DATEADD(DAY, 1, @ToDate))
                AND
                (@CategoryId IS NULL OR
                    t.CategoryId = @CategoryId)
                AND
                (@Type IS NULL OR
                    t.TransactionType = @Type)
            ORDER BY
                t.TransactionDate DESC,
                t.TransactionId DESC;";

        return await connection.QueryAsync<Transaction>(
            sql,
            new
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                CategoryId = categoryId,
                Type = type
            });
    }

    // =========================================================
    // SEARCH PAGED
    // =========================================================

    public async Task<IEnumerable<Transaction>> SearchPagedAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type,
        int currentPage,
        int pageSize)
    {
        using var connection = GetConnection();

        var offset =
            (currentPage - 1) * pageSize;

        const string sql = @"
            SELECT
                t.TransactionId,
                t.UserId,
                t.CategoryId,
                t.Amount,
                t.TransactionDate,
                t.Description,
                t.TransactionType,
                t.RecurringPaymentId,
                c.CategoryName,
                u.FirstName + ' ' + u.LastName AS UserName
            FROM Transactions t

            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId

            INNER JOIN Users u
                ON u.UserId = t.UserId

            WHERE
                (@UserId IS NULL OR
                    t.UserId = @UserId)
                AND
                (@FromDate IS NULL OR
                    t.TransactionDate >= @FromDate)
                AND
                (@ToDate IS NULL OR
                    t.TransactionDate < DATEADD(DAY, 1, @ToDate))
                AND
                (@CategoryId IS NULL OR
                    t.CategoryId = @CategoryId)
                AND
                (@Type IS NULL OR
                    t.TransactionType = @Type)

            ORDER BY
                t.TransactionDate DESC,
                t.TransactionId DESC

            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        return await connection.QueryAsync<Transaction>(
            sql,
            new
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                CategoryId = categoryId,
                Type = type,
                Offset = offset,
                PageSize = pageSize
            });
    }

    // =========================================================
    // SEARCH COUNT
    // =========================================================

    public async Task<int> GetSearchCountAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM Transactions t
            WHERE
                (@UserId IS NULL OR
                    t.UserId = @UserId)
                AND
                (@FromDate IS NULL OR
                    t.TransactionDate >= @FromDate)
                AND
                (@ToDate IS NULL OR
                    t.TransactionDate < DATEADD(DAY, 1, @ToDate))
                AND
                (@CategoryId IS NULL OR
                    t.CategoryId = @CategoryId)
                AND
                (@Type IS NULL OR
                    t.TransactionType = @Type);";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                CategoryId = categoryId,
                Type = type
            });
    }

    // =========================================================
    // SUMMARY
    // =========================================================

    public async Task<TransactionSummary> GetSummaryAsync(
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
            SELECT
                COALESCE(
                    SUM(
                        CASE
                            WHEN ct.TypeName = 'Income'
                                THEN t.Amount
                            ELSE 0
                        END
                    ),
                    0
                ) AS TotalIncome,

                COALESCE(
                    SUM(
                        CASE
                            WHEN ct.TypeName = 'Expense'
                                THEN t.Amount
                            ELSE 0
                        END
                    ),
                    0
                ) AS TotalExpense

            FROM Transactions t

            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId

            INNER JOIN CategoryTypes ct
                ON ct.CategoryTypeId = c.CategoryTypeId

            WHERE t.UserId = @UserId;";

        var result =
            await connection.QueryFirstOrDefaultAsync<TransactionSummary>(
                sql,
                new
                {
                    UserId = userId
                });

        return result ?? new TransactionSummary();
    }

    // =========================================================
    // AVAILABLE BALANCE
    // =========================================================

    public async Task<decimal> GetAvailableBalanceAsync(
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
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
                ON c.CategoryId = t.CategoryId

            INNER JOIN CategoryTypes ct
                ON ct.CategoryTypeId = c.CategoryTypeId

            WHERE t.UserId = @UserId;";

        return await connection.ExecuteScalarAsync<decimal>(
            sql,
            new
            {
                UserId = userId
            });
    }
}