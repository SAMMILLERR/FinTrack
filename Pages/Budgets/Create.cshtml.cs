using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Pages.Budgets;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateModel(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
    }

    [BindProperty]
    public Budget Budget { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Budget.BudgetMonth = DateTime.Today.Month;
        Budget.BudgetYear = DateTime.Today.Year;

        await LoadCategories();
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

        await _budgetRepository.AddAsync(Budget);

        return RedirectToPage("Index");
    }

    private async Task LoadCategories()
    {
        var categories =
            await _categoryRepository.GetByTypeAsync("Expense");

        Categories = categories.Select(c =>
            new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList();
    }
}