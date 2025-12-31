using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FlexiTrack.Desktop.Services;

public class SecureTokenStorage : ITokenStorage
{
    private readonly string _filePath;
    private const string FileName = "flexitrack_auth.dat";

    public SecureTokenStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "FlexiTrack");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, FileName);
    }

    public async Task SaveTokenAsync(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(
            tokenBytes,
            null,
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(_filePath, encryptedBytes);
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var encryptedBytes = await File.ReadAllBytesAsync(_filePath);
            var tokenBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            await ClearTokenAsync();
            return null;
        }
    }

    public Task ClearTokenAsync()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
        return Task.CompletedTask;
    }
}
