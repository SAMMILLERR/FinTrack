using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Budgets;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly IBudgetRepository _repository;

    public DeleteModel(IBudgetRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public Budget Budget { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Budget = await _repository.GetByIdAsync(id, userId);

        if (Budget == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _repository.DeleteAsync(
            Budget.BudgetId,
            userId);

        return RedirectToPage("Index");
    }
}