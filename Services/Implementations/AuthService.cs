using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(
        RegisterViewModel input)
    {
        var existingUser =
            await _userRepository.GetByEmailAsync(input.Email);

        if (existingUser != null)
        {
            return (false, "Email already exists.");
        }

        var user = new User
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = input.Email,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(input.Password),

            RoleId = 2,
            IsActive = true
        };

        await _userRepository.AddAsync(user);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(
        LoginModel input)
    {
        var user =
            await _userRepository.GetByEmailAsync(input.Email);

        if (user == null)
        {
            return (false, "Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(
                input.Password,
                user.PasswordHash))
        {
            return (false, "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return (false, "Your account has been deactivated.");
        }

        return (true, null);
    }
}