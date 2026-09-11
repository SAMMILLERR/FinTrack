using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;

    public DashboardService(
        ITransactionRepository transactionRepository,
        IBudgetRepository budgetRepository)
    {
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int userId)
    {
        var dashboard = new DashboardViewModel();

        dashboard.Summary =
            await _transactionRepository.GetSummaryAsync(userId);

        dashboard.Budgets =
            await _budgetRepository.GetByUserAsync(userId);

        dashboard.RecentTransactions =
            (await _transactionRepository.GetByUserAsync(userId))
            .Take(5)
            .ToList();

        return dashboard;
    }
}