using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<Models.User>> GetUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task ToggleStatusAsync(int userId, bool currentStatus)
    {
        await _userRepository.UpdateStatusAsync(
            userId,
            !currentStatus);
    }

    public async Task ToggleRoleAsync(int userId, int currentRoleId)
    {
        int newRole = currentRoleId == 1 ? 2 : 1;

        await _userRepository.UpdateRoleAsync(
            userId,
            newRole);
    }
}