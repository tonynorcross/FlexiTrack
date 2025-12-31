namespace FlexiTrack.Desktop.Services;

public interface ITokenStorage
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
    Task ClearTokenAsync();
}
