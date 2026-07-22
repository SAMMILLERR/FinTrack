using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Pages.Transactions;

[Authorize]
public class EditModel : PageModel
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EditModel(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    [BindProperty]
    public Transaction Transaction { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = "";

    public List<SelectListItem> Categories { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Transaction = await _transactionRepository.GetByIdAsync(id, userId);

        if (Transaction == null)
            return NotFound();

        Type = Transaction.TransactionType;

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

        Transaction.UserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _transactionRepository.UpdateAsync(Transaction);

        return RedirectToPage("Index");
    }

    private async Task LoadCategories()
    {
        var categories =
            await _categoryRepository.GetByTypeAsync(Type);

        Categories = categories.Select(c =>
            new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList();
    }
}