using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(int userId);
}