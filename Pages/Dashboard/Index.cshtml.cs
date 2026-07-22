using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;

    public IndexModel(
        ITransactionRepository transactionRepository,
        IBudgetRepository budgetRepository)
    {
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
    }

    public DashboardViewModel Dashboard { get; set; }
        = new();

    public async Task OnGetAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Dashboard.Summary =
            await _transactionRepository.GetSummaryAsync(userId);

        Dashboard.Budgets =
            await _budgetRepository.GetByUserAsync(userId);

        Dashboard.RecentTransactions =
            (await _transactionRepository.GetByUserAsync(userId))
            .Take(5);
    }
}