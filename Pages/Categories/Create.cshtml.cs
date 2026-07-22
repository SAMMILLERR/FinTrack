using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace FinTrack.Pages.Categories;

[Authorize(Roles = "Admin")]

public class CreateModel : PageModel
{
    private readonly ICategoryRepository _repository;

    public CreateModel(ICategoryRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public Category Category { get; set; }
        = new();

    public List<SelectListItem> CategoryTypes
        = new();

    public async Task OnGetAsync()
    {
        await LoadCategoryTypes();
    }

public async Task<IActionResult> OnPostAsync()
{
    Console.WriteLine("POST HIT");

    if (!ModelState.IsValid)
    {
        Console.WriteLine("Model Invalid");

        foreach (var state in ModelState)
        {
            foreach (var error in state.Value.Errors)
            {
                Console.WriteLine($"{state.Key} -> {error.ErrorMessage}");
            }
        }

        await LoadCategoryTypes();
        return Page();
    }

    Console.WriteLine("Model Valid");

    await _repository.AddAsync(Category);

    Console.WriteLine("Saved");

    return RedirectToPage("Index");
}

    private async Task LoadCategoryTypes()
    {
        var types = await _repository.GetCategoryTypesAsync();

        CategoryTypes = types.Select(x =>

            new SelectListItem
            {
                Value = x.CategoryTypeId.ToString(),

                Text = x.TypeName
            })

            .ToList();
    }
}