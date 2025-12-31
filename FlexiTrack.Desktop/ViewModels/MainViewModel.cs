using System.Windows;
using CommunityToolkit.Mvvm.Input;
using FlexiTrack.Desktop.Services;
using FlexiTrack.Desktop.Views;
using Hardcodet.Wpf.TaskbarNotification;
using static FlexiTrack.Desktop.Services.LogService;

namespace FlexiTrack.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private TrayPopupWindow? _popupWindow;

    public TaskbarIcon? TrayIcon { get; set; }

    public MainViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;

        _authService.AuthenticationChanged += OnAuthenticationChanged;
    }

    private void OnAuthenticationChanged(object? sender, bool isAuthenticated)
    {
        Log($"OnAuthenticationChanged: {isAuthenticated}");
        if (!isAuthenticated)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _popupWindow?.Close();
                _popupWindow = null;
                ((App)Application.Current).ShowLoginWindow(showPopupAfterLogin: true);
            });
        }
    }

    [RelayCommand]
    private void ShowPopup()
    {
        try
        {
            Log("ShowPopup called");

            // Check if user is authenticated
            if (!_authService.IsAuthenticated)
            {
                Log("User not authenticated, showing login...");
                ((App)Application.Current).ShowLoginWindow(showPopupAfterLogin: true);
                return;
            }

            if (_popupWindow != null && _popupWindow.IsVisible)
            {
                _popupWindow.Activate();
                return;
            }

            Log("Creating TrayPopupWindow...");
            _popupWindow = new TrayPopupWindow(_serviceProvider);
            Log("TrayPopupWindow created, calling Show...");
            _popupWindow.Show();
            Log("TrayPopupWindow shown");
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

            _popupWindow = new TrayPopupWindow(_serviceProvider);
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
        _popupWindow?.Close();
        _popupWindow = null;
        await _authService.LogoutAsync();
    }

    [RelayCommand]
    private void Exit()
    {
        _popupWindow?.Close();
        Application.Current.Shutdown();
    }
}
