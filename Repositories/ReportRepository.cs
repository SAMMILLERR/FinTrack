using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinTrack.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IConfiguration _configuration;

    public ReportRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }


    // ============================================================
    // EXISTING CURRENT-MONTH REPORT
    // ============================================================

    public async Task<IEnumerable<CategoryReport>>
        GetCategoryReportAsync(
            int userId,
            int month,
            int year)
    {
        using var connection = GetConnection();

        return await connection.QueryAsync<CategoryReport>(
            "sp_GetCategoryWiseReport",
            new
            {
                UserId = userId,
                Month = month,
                Year = year
            },
            commandType: CommandType.StoredProcedure);
    }


    // ============================================================
    // FILTERED REPORT
    // ============================================================

    public async Task<IEnumerable<FinancialReportCategory>>
        GetCategoryReportAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate,
            string transactionFilter)
    {
        using var connection = GetConnection();

        const string sql = """
            SELECT
                c.CategoryId,
                c.CategoryName,
                ct.TypeName AS TransactionType,
                SUM(t.Amount) AS Amount
            FROM Transactions t

            INNER JOIN Categories c
                ON t.CategoryId = c.CategoryId

            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId

            WHERE
                t.UserId = @UserId

                AND t.TransactionDate >= @FromDate

                AND t.TransactionDate < @ToDateExclusive

                AND
                (
                    @TransactionFilter = 'Both'
                    OR ct.TypeName = @TransactionFilter
                )

            GROUP BY
                c.CategoryId,
                c.CategoryName,
                ct.TypeName

            ORDER BY
                SUM(t.Amount) DESC;
            """;

        return await connection.QueryAsync<FinancialReportCategory>(
            sql,
            new
            {
                UserId = userId,

                FromDate = fromDate.Date,

                ToDateExclusive =
                    toDate.Date.AddDays(1),

                TransactionFilter =
                    transactionFilter
            });
    }


    // ============================================================
    // REPORT SUMMARY
    // ============================================================

    public async Task<
        (decimal TotalIncome,
         decimal TotalExpense,
         int TransactionCount)>
        GetReportSummaryAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate,
            string transactionFilter)
    {
        using var connection = GetConnection();

        const string sql = """
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
                ) AS TotalExpense,

                COUNT(*) AS TransactionCount

            FROM Transactions t

            INNER JOIN Categories c
                ON t.CategoryId = c.CategoryId

            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId

            WHERE
                t.UserId = @UserId

                AND t.TransactionDate >= @FromDate

                AND t.TransactionDate < @ToDateExclusive

                AND
                (
                    @TransactionFilter = 'Both'
                    OR ct.TypeName = @TransactionFilter
                );
            """;

        var result =
            await connection.QueryFirstOrDefaultAsync<ReportSummaryResult>(
                sql,
                new
                {
                    UserId = userId,

                    FromDate = fromDate.Date,

                    ToDateExclusive =
                        toDate.Date.AddDays(1),

                    TransactionFilter =
                        transactionFilter
                });

        return
        (
            result?.TotalIncome ?? 0,

            result?.TotalExpense ?? 0,

            result?.TransactionCount ?? 0
        );
    }


    private class ReportSummaryResult
    {
        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public int TransactionCount { get; set; }
    }
}