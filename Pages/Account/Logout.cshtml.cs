using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Account;

public class LogoutModel : PageModel
{
public async Task<IActionResult> OnGet()
{
    Console.WriteLine("LOGOUT HIT");

    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    Console.WriteLine("COOKIE REMOVED");

    return RedirectToPage("/Account/Login");
}
}