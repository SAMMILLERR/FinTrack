using BCrypt.Net;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FinTrack.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUserRepository _userRepository;

    public LoginModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [BindProperty]
    public FinTrack.Models.LoginModel Input { get; set; } = new();

    public string? RateLimitError { get; set; }

    public void OnGet()
    {
        RateLimitError = TempData["RateLimitError"] as string;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userRepository.GetByEmailAsync(Input.Email);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
        {
            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(
                "",
                "Your account has been deactivated.");

            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "User")
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        if (user.RoleId == 1)
        {
            return RedirectToPage("/Transactions/Index");
        }

        return RedirectToPage("/Dashboard/Index");
    }
}