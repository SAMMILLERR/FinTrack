using System.Text;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(
        IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<CategoryReport>>
        GetCurrentMonthReportAsync(int userId)
    {
        return await _reportRepository.GetCategoryReportAsync(
            userId,
            DateTime.Today.Month,
            DateTime.Today.Year);
    }

    public async Task<byte[]> ExportCurrentMonthReportAsync(
        int userId)
    {
        var report =
            await GetCurrentMonthReportAsync(userId);

        var builder = new StringBuilder();

        builder.AppendLine("Category,TotalAmount");

        foreach (var item in report)
        {
            var categoryName =
                item.CategoryName?
                    .Replace("\"", "\"\"");

            builder.AppendLine(
                $"\"{categoryName}\",{item.TotalAmount}");
        }

        return Encoding.UTF8.GetBytes(
            builder.ToString());
    }
}