using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using FlexiTrack.Desktop.Models;
using FlexiTrack.Desktop.Services;

namespace FlexiTrack.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private string _originalApiUrl;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.GetSettings();
        _apiBaseUrl = settings.ApiBaseUrl;
        _originalApiUrl = settings.ApiBaseUrl;
    }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string? _connectionStatus;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isConnectionSuccess;

    public bool HasChanges => ApiBaseUrl != _originalApiUrl;

    partial void OnApiBaseUrlChanged(string value)
    {
        OnPropertyChanged(nameof(HasChanges));
        ConnectionStatus = null;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            ConnectionStatus = "Please enter a URL";
            IsConnectionSuccess = false;
            return;
        }

        IsBusy = true;
        ConnectionStatus = "Testing connection...";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var healthUrl = ApiBaseUrl.TrimEnd('/') + "/api/health";

            var response = await client.GetAsync(healthUrl);

            if (response.IsSuccessStatusCode)
            {
                ConnectionStatus = "Connection successful";
                IsConnectionSuccess = true;
            }
            else
            {
                ConnectionStatus = $"Server returned: {(int)response.StatusCode} {response.ReasonPhrase}";
                IsConnectionSuccess = false;
            }
        }
        catch (HttpRequestException ex)
        {
            ConnectionStatus = $"Connection failed: {ex.Message}";
            IsConnectionSuccess = false;
        }
        catch (TaskCanceledException)
        {
            ConnectionStatus = "Connection timed out";
            IsConnectionSuccess = false;
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Error: {ex.Message}";
            IsConnectionSuccess = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Save(Window window)
    {
        try
        {
            var settings = new UserSettings
            {
                ApiBaseUrl = ApiBaseUrl.TrimEnd('/')
            };

            _settingsService.SaveSettings(settings);
            _originalApiUrl = settings.ApiBaseUrl;

            MessageBox.Show(
                "Settings saved successfully.\n\nThe application will use the new API URL.",
                "Settings Saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save settings: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
