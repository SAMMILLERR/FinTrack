using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Transactions;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly ITransactionRepository _repository;

    public DeleteModel(ITransactionRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public Transaction Transaction { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Transaction = await _repository.GetByIdAsync(id, userId);

        if (Transaction == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _repository.DeleteAsync(
            Transaction.TransactionId,
            userId);

        return RedirectToPage("Index");
    }
}