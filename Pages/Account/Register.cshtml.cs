using BCrypt.Net;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IUserRepository _userRepository;

    public RegisterModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Step 1: Validate the form
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Step 2: Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(Input.Email);

        if (existingUser != null)
        {
            ModelState.AddModelError("Input.Email", "Email already exists.");

            return Page();
        }

        // Step 3: Create User object
        var user = new User
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Email = Input.Email,

            // Hash password before storing
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),

            RoleId = 2,          // Normal User
            IsActive = true
        };

        // Step 4: Save user
        await _userRepository.AddAsync(user);

        // Step 5: Redirect to Login page
        return RedirectToPage("Login");
    }
}