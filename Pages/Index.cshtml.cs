using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Pages;

[Authorize]
public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Dashboard/Index");
    }
}