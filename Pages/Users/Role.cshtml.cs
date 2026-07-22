using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Users;

[Authorize(Roles = "Admin")]
public class RoleModel : PageModel
{
    private readonly IUserRepository _repository;

    public RoleModel(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> OnGetAsync(int id, int roleId)
    {
        int newRole = roleId == 1 ? 2 : 1;

        await _repository.UpdateRoleAsync(id, newRole);

        return RedirectToPage("Index");
    }
}