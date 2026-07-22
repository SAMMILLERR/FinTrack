using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace FinTrack.Pages.Categories;

[Authorize(Roles = "Admin")]

public class DeleteModel : PageModel
{
    private readonly ICategoryRepository _repository;

    public DeleteModel(ICategoryRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        Category = category;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _repository.DeleteAsync(Category.CategoryId);

        return RedirectToPage("Index");
    }
}