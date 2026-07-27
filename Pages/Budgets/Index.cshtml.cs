using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Budgets;

[Authorize(Roles ="User")]
public class IndexModel : PageModel
{
    private readonly IBudgetRepository _repository;

    public IndexModel(IBudgetRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Budget> Budgets { get; set; }
        = new List<Budget>();

    public async Task OnGetAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Budgets = await _repository.GetByUserAsync(userId);
    }
}