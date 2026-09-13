using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly IConfiguration _configuration;

    public BudgetRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString(
                "DefaultConnection"));
    }

    // =========================================================
    // GET ALL BUDGETS FOR USER
    // =========================================================

    public async Task<IEnumerable<Budget>>
        GetByUserAsync(int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
SELECT
    b.BudgetId,
    b.UserId,
    b.CategoryId,
    c.CategoryName,
    b.BudgetMonth,
    b.BudgetYear,
    b.LimitAmount,

    ISNULL(
        SUM(
            CASE
                WHEN t.TransactionType = 'Expense'
                THEN t.Amount
                ELSE 0
            END
        ),
        0
    ) AS SpentAmount,

    b.CreatedOn

FROM Budgets b

INNER JOIN Categories c
    ON b.CategoryId = c.CategoryId

LEFT JOIN Transactions t
    ON b.UserId = t.UserId
    AND b.CategoryId = t.CategoryId
    AND t.TransactionDate >=
        DATEFROMPARTS(
            b.BudgetYear,
            b.BudgetMonth,
            1
        )
    AND t.TransactionDate <
        DATEADD(
            MONTH,
            1,
            DATEFROMPARTS(
                b.BudgetYear,
                b.BudgetMonth,
                1
            )
        )

WHERE b.UserId = @userId

GROUP BY
    b.BudgetId,
    b.UserId,
    b.CategoryId,
    c.CategoryName,
    b.BudgetMonth,
    b.BudgetYear,
    b.LimitAmount,
    b.CreatedOn

ORDER BY
    b.BudgetYear DESC,
    b.BudgetMonth DESC,
    c.CategoryName;";

        return await connection.QueryAsync<Budget>(
            sql,
            new
            {
                userId
            });
    }

    // =========================================================
    // GET ONE BUDGET
    // =========================================================

    public async Task<Budget?>
        GetByIdAsync(
            int budgetId,
            int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
SELECT
    b.BudgetId,
    b.UserId,
    b.CategoryId,
    c.CategoryName,
    b.BudgetMonth,
    b.BudgetYear,
    b.LimitAmount,

    ISNULL(
        (
            SELECT SUM(t.Amount)
            FROM Transactions t
            WHERE t.UserId = b.UserId
              AND t.CategoryId = b.CategoryId
              AND t.TransactionType = 'Expense'
              AND t.TransactionDate >=
                  DATEFROMPARTS(
                      b.BudgetYear,
                      b.BudgetMonth,
                      1
                  )
              AND t.TransactionDate <
                  DATEADD(
                      MONTH,
                      1,
                      DATEFROMPARTS(
                          b.BudgetYear,
                          b.BudgetMonth,
                          1
                      )
                  )
        ),
        0
    ) AS SpentAmount,

    b.CreatedOn

FROM Budgets b

INNER JOIN Categories c
    ON b.CategoryId = c.CategoryId

WHERE b.BudgetId = @budgetId
  AND b.UserId = @userId;";

        return await connection
            .QueryFirstOrDefaultAsync<Budget>(
                sql,
                new
                {
                    budgetId,
                    userId
                });
    }

    // =========================================================
    // ADD BUDGET
    // =========================================================

    public async Task AddAsync(
        Budget budget)
    {
        using var connection = GetConnection();

        const string sql = @"
INSERT INTO Budgets
(
    UserId,
    CategoryId,
    BudgetMonth,
    BudgetYear,
    LimitAmount
)
VALUES
(
    @UserId,
    @CategoryId,
    @BudgetMonth,
    @BudgetYear,
    @LimitAmount
);";

        await connection.ExecuteAsync(
            sql,
            budget);
    }

    // =========================================================
    // UPDATE BUDGET
    // =========================================================

    public async Task UpdateAsync(
        Budget budget)
    {
        using var connection = GetConnection();

        const string sql = @"
UPDATE Budgets

SET
    CategoryId = @CategoryId,
    BudgetMonth = @BudgetMonth,
    BudgetYear = @BudgetYear,
    LimitAmount = @LimitAmount

WHERE BudgetId = @BudgetId
  AND UserId = @UserId;";

        await connection.ExecuteAsync(
            sql,
            budget);
    }

    // =========================================================
    // DELETE BUDGET
    // =========================================================

    public async Task DeleteAsync(
        int budgetId,
        int userId)
    {
        using var connection = GetConnection();

        const string sql = @"
DELETE FROM Budgets

WHERE BudgetId = @budgetId
  AND UserId = @userId;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                budgetId,
                userId
            });
    }

    // =========================================================
    // RECENT MONTHLY SPENDING
    // USED BY BUDGET RECOMMENDATION SERVICE
    // =========================================================

    public async Task<IReadOnlyList<MonthlyCategorySpending>>
        GetRecentMonthlySpendingAsync(
            int userId,
            int categoryId,
            DateTime startDate,
            DateTime endDate)
    {
        using var connection = GetConnection();

        const string sql = @"
SELECT
    DATEFROMPARTS(
        YEAR(t.TransactionDate),
        MONTH(t.TransactionDate),
        1
    ) AS MonthStart,

    ISNULL(
        SUM(t.Amount),
        0
    ) AS Amount

FROM Transactions t

WHERE t.UserId = @UserId
  AND t.CategoryId = @CategoryId
  AND t.TransactionType = 'Expense'

  AND t.TransactionDate >= @StartDate
  AND t.TransactionDate < @EndDate

GROUP BY
    YEAR(t.TransactionDate),
    MONTH(t.TransactionDate)

ORDER BY
    MonthStart;";

        var result =
            await connection.QueryAsync<MonthlyCategorySpending>(
                sql,
                new
                {
                    UserId = userId,
                    CategoryId = categoryId,
                    StartDate = startDate.Date,
                    EndDate = endDate.Date
                });

        return result.ToList();
    }
}