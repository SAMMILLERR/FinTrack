using BCrypt.Net;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Step 1: Validate form
        if (!ModelState.IsValid)
        {
            return Page();
        }
        // check if user is active or not
        
        // Step 2: Find user by email
        var user = await _userRepository.GetByEmailAsync(Input.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }
        // Step 3: Check if account is active
if (!user.IsActive)
{
    ModelState.AddModelError(
        string.Empty,
        "Your account has been deactivated. Please contact the administrator.");

    return Page();
}

        // Step 3: Verify password
        bool validPassword = BCrypt.Net.BCrypt.Verify(
            Input.Password,
            user.PasswordHash);

        if (!validPassword)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }

        // Step 4
        // Authentication cookie comes in the NEXT step

       // Create claims
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

    new Claim(ClaimTypes.Name,
        $"{user.FirstName} {user.LastName}"),

    new Claim(ClaimTypes.Email,
        user.Email),

    new Claim(ClaimTypes.Role,
        user.RoleId == 1 ? "Admin" : "User")
};

// Create identity
var identity = new ClaimsIdentity(
    claims,
    CookieAuthenticationDefaults.AuthenticationScheme);

// Create principal
var principal = new ClaimsPrincipal(identity);

// Sign in
await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    principal);
if (user.RoleId == 1)   // Admin
{
    return RedirectToPage("/Transactions/Index");
}
else                    // User
{
    return RedirectToPage("/Dashboard/Index");
}
    }
}