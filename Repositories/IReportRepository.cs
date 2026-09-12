using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IReportRepository
{
    Task<IEnumerable<CategoryReport>> GetCategoryReportAsync(
        int userId,
        int month,
        int year);

    Task<IEnumerable<FinancialReportCategory>>
        GetCategoryReportAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate,
            string transactionFilter);

    Task<(decimal TotalIncome, decimal TotalExpense, int TransactionCount)>
        GetReportSummaryAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate,
            string transactionFilter);
}