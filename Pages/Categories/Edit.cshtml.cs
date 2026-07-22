using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace FinTrack.Pages.Categories;

[Authorize(Roles = "Admin")]

public class EditModel : PageModel
{
    private readonly ICategoryRepository _repository;

    public EditModel(ICategoryRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public List<SelectListItem> CategoryTypes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        Category = category;

        await LoadCategoryTypes();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoryTypes();
            return Page();
        }

        await _repository.UpdateAsync(Category);

        return RedirectToPage("Index");
    }

    private async Task LoadCategoryTypes()
    {
        var types = await _repository.GetCategoryTypesAsync();

        CategoryTypes = types.Select(x => new SelectListItem
        {
            Value = x.CategoryTypeId.ToString(),
            Text = x.TypeName
        }).ToList();
    }
}