using System.Windows;
using CommunityToolkit.Mvvm.Input;
using FlexiTrack.Desktop.Services;
using FlexiTrack.Desktop.Views;
using Hardcodet.Wpf.TaskbarNotification;
using static FlexiTrack.Desktop.Services.LogService;

namespace FlexiTrack.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private WebViewPopupWindow? _popupWindow;

    public TaskbarIcon? TrayIcon { get; set; }

    public MainViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private string ApiUrl => _settingsService.ApiBaseUrl;

    private void OnSettingsChanged()
    {
        Log($"Settings changed, new API URL: {ApiUrl}");

        if (_popupWindow != null)
        {
            _popupWindow.Close();
            _popupWindow = null;
        }
    }

    [RelayCommand]
    private void ShowPopup()
    {
        try
        {
            Log("ShowPopup called");

            if (_popupWindow != null && _popupWindow.IsVisible)
            {
                _popupWindow.Activate();
                return;
            }

            Log("Creating WebViewPopupWindow...");
            _popupWindow = new WebViewPopupWindow(ApiUrl);
            _popupWindow.Show();
            Log("WebViewPopupWindow shown");
            PositionPopupNearTray();
        }
        catch (Exception ex)
        {
            LogError("ShowPopup", ex);
        }
    }

    public void ShowPopupDirectly()
    {
        try
        {
            if (_popupWindow != null && _popupWindow.IsVisible)
            {
                _popupWindow.Activate();
                return;
            }

            _popupWindow = new WebViewPopupWindow(ApiUrl);
            _popupWindow.Show();
            PositionPopupNearTray();
        }
        catch (Exception ex)
        {
            LogError("ShowPopupDirectly", ex);
        }
    }

    private void PositionPopupNearTray()
    {
        if (_popupWindow == null) return;

        var workArea = SystemParameters.WorkArea;
        _popupWindow.Left = workArea.Right - _popupWindow.Width - 10;
        _popupWindow.Top = workArea.Bottom - _popupWindow.Height - 10;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (_popupWindow != null)
        {
            await _popupWindow.ClearBrowsingDataAsync();
            _popupWindow.NavigateToApp();
        }
    }

    [RelayCommand]
    private void Exit()
    {
        _popupWindow?.Close();
        Application.Current.Shutdown();
    }
}
