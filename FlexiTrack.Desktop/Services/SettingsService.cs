using System.IO;
using System.Text.Json;
using FlexiTrack.Desktop.Models;
using Microsoft.Extensions.Configuration;

namespace FlexiTrack.Desktop.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlexiTrack");

    private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "settings.json");

    private readonly string _defaultApiUrl;
    private UserSettings _settings;

    public event Action? SettingsChanged;

    public SettingsService(IConfiguration configuration)
    {
        _defaultApiUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5265";
        _settings = LoadSettings();
    }

    public string ApiBaseUrl => _settings.ApiBaseUrl;

    public UserSettings GetSettings() => new()
    {
        ApiBaseUrl = _settings.ApiBaseUrl
    };

    public void SaveSettings(UserSettings settings)
    {
        _settings = settings;

        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFile, json);
            LogService.Log($"Settings saved to {SettingsFile}");

            SettingsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            LogService.LogError("SaveSettings", ex);
            throw;
        }
    }

    private UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);

                if (settings != null)
                {
                    LogService.Log($"Settings loaded from {SettingsFile}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.LogError("LoadSettings", ex);
        }

        LogService.Log($"Using default settings (API URL: {_defaultApiUrl})");
        return new UserSettings
        {
            ApiBaseUrl = _defaultApiUrl
        };
    }

    public static string GetSettingsFilePath() => SettingsFile;
}
