using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
namespace FinTrack.Pages.Categories;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ICategoryRepository _repository;

    public IEnumerable<Category> Categories { get; set; }
        = new List<Category>();

    public IndexModel(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task OnGetAsync()
    {
        Categories = await _repository.GetAllAsync();
    }
}