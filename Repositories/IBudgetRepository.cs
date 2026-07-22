using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IBudgetRepository
{
    Task<IEnumerable<Budget>> GetByUserAsync(int userId);

    Task<Budget?> GetByIdAsync(int budgetId, int userId);

    Task AddAsync(Budget budget);

    Task UpdateAsync(Budget budget);

    Task DeleteAsync(int budgetId, int userId);
}