using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<Budget>> GetBudgetsAsync(int userId);

    Task<IEnumerable<Category>> GetExpenseCategoriesAsync();

    Task<Budget?> GetBudgetAsync(
        int budgetId,
        int userId);

    Task AddBudgetAsync(Budget budget);

    Task UpdateBudgetAsync(Budget budget);

    Task DeleteBudgetAsync(
        int budgetId,
        int userId);
}