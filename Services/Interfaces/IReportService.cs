using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IReportService
{
    Task<IEnumerable<CategoryReport>> GetCurrentMonthReportAsync(
        int userId);

    Task<byte[]> ExportCurrentMonthReportAsync(
        int userId);
}