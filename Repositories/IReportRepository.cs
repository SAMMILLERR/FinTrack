using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IReportRepository
{
    Task<IEnumerable<CategoryReport>> GetCategoryReportAsync(
    int userId,
    int month,
    int year);
}

