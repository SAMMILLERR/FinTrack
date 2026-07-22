using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Pages.Budgets;

[Authorize]
public class EditModel : PageModel
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EditModel(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
    }

    [BindProperty]
    public Budget Budget { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Budget = await _budgetRepository.GetByIdAsync(id, userId);

        if (Budget == null)
            return NotFound();

        await LoadCategories();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return Page();
        }

        Budget.UserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _budgetRepository.UpdateAsync(Budget);

        return RedirectToPage("Index");
    }

    private async Task LoadCategories()
    {
        var categories = await _categoryRepository.GetByTypeAsync("Expense");

        Categories = categories.Select(c => new SelectListItem
        {
            Value = c.CategoryId.ToString(),
            Text = c.CategoryName
        }).ToList();
    }
}