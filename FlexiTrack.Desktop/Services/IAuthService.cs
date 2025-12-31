using FlexiTrack.Desktop.Models;

namespace FlexiTrack.Desktop.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? CurrentUserEmail { get; }
    UserProfileDto? CurrentUser { get; }

    Task<(bool Success, string? Error)> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<bool> TryRestoreSessionAsync();

    event EventHandler<bool>? AuthenticationChanged;
}
