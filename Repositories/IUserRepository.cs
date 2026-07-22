using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task AddAsync(User user);

    Task<IEnumerable<User>> GetAllAsync();

    Task UpdateRoleAsync(int userId, int roleId);

    Task UpdateStatusAsync(int userId, bool isActive);
    Task<User?> GetByIdAsync(int userId);
}