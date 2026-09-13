using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IBudgetRecommendationService
{
    Task<BudgetRecommendation> GetRecommendationAsync(
        int userId,
        Budget budget);

    Task<bool> ApplyRecommendationAsync(
        int userId,
        int budgetId);

    Task<Budget?> SynchronizeCategoryBudgetAsync(
        int userId,
        Category category);
}