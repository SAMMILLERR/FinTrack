using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IUserRepository _repository;

    public IndexModel(IUserRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<User> Users { get; set; }
        = new List<User>();

    public async Task OnGetAsync()
    {
        Users = await _repository.GetAllAsync();
    }
}