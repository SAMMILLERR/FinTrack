using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetUsersAsync();

    Task ToggleStatusAsync(int userId, bool currentStatus);

    Task ToggleRoleAsync(int userId, int currentRoleId);
}