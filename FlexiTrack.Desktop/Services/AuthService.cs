using System.IdentityModel.Tokens.Jwt;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.Models;

namespace FlexiTrack.Desktop.Services;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly ITokenStorage _tokenStorage;

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUserEmail { get; private set; }
    public UserProfileDto? CurrentUser { get; private set; }

    public event EventHandler<bool>? AuthenticationChanged;

    public AuthService(IApiClient apiClient, ITokenStorage tokenStorage)
    {
        _apiClient = apiClient;
        _tokenStorage = tokenStorage;

        WeakReferenceMessenger.Default.Register<AuthenticationChangedMessage>(this, (r, m) =>
        {
            if (!m.IsAuthenticated)
            {
                IsAuthenticated = false;
                CurrentUserEmail = null;
                CurrentUser = null;
                AuthenticationChanged?.Invoke(this, false);
            }
        });
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        var response = await _apiClient.LoginAsync(email, password);

        if (!response.Success || string.IsNullOrEmpty(response.Token))
        {
            return (false, response.Error ?? "Login failed");
        }

        await _tokenStorage.SaveTokenAsync(response.Token);

        var profile = await _apiClient.GetProfileAsync();
        if (profile == null)
        {
            await _tokenStorage.ClearTokenAsync();
            return (false, "Failed to load user profile");
        }

        IsAuthenticated = true;
        CurrentUserEmail = profile.Email;
        CurrentUser = profile;
        AuthenticationChanged?.Invoke(this, true);

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        await _tokenStorage.ClearTokenAsync();
        IsAuthenticated = false;
        CurrentUserEmail = null;
        CurrentUser = null;
        AuthenticationChanged?.Invoke(this, false);
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        var token = await _tokenStorage.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return false;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            await _tokenStorage.ClearTokenAsync();
            return false;
        }

        try
        {
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await _tokenStorage.ClearTokenAsync();
                return false;
            }

            var profile = await _apiClient.GetProfileAsync();
            if (profile == null)
            {
                await _tokenStorage.ClearTokenAsync();
                return false;
            }

            IsAuthenticated = true;
            CurrentUserEmail = profile.Email;
            CurrentUser = profile;
            AuthenticationChanged?.Invoke(this, true);
            return true;
        }
        catch
        {
            await _tokenStorage.ClearTokenAsync();
            return false;
        }
    }
}
