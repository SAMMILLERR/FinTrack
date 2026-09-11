using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BudgetService(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<Budget>> GetBudgetsAsync(
        int userId)
    {
        return await _budgetRepository.GetByUserAsync(userId);
    }

    public async Task<IEnumerable<Category>> GetExpenseCategoriesAsync()
    {
        return await _categoryRepository.GetByTypeAsync("Expense");
    }

    public async Task<Budget?> GetBudgetAsync(
        int budgetId,
        int userId)
    {
        return await _budgetRepository.GetByIdAsync(
            budgetId,
            userId);
    }

    public async Task AddBudgetAsync(Budget budget)
    {
        await _budgetRepository.AddAsync(budget);
    }

    public async Task UpdateBudgetAsync(Budget budget)
    {
        await _budgetRepository.UpdateAsync(budget);
    }

    public async Task DeleteBudgetAsync(
        int budgetId,
        int userId)
    {
        await _budgetRepository.DeleteAsync(
            budgetId,
            userId);
    }
}