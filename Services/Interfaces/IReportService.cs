using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IReportService
{
    Task<IEnumerable<CategoryReport>> GetCurrentMonthReportAsync(
        int userId);

    Task<string> ExportCurrentMonthReportAsync(
        int userId);

    Task<FinancialReport> GenerateReportAsync(
        int userId,
        DateTime fromDate,
        DateTime toDate,
        string transactionFilter);
}