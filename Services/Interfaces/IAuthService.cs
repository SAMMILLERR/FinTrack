using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? ErrorMessage)> RegisterAsync(
        RegisterViewModel input);

    Task<(bool Success, string? ErrorMessage)> LoginAsync(
        LoginModel input);
}