using Dapper;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Users;

[Authorize(Roles = "Admin")]
public class StatusModel : PageModel
{
    private readonly IUserRepository _repository;

    public StatusModel(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> OnGetAsync(int id, bool isActive)
    {
        await _repository.UpdateStatusAsync(
            id,
            !isActive);

        return RedirectToPage("Index");
    }
}