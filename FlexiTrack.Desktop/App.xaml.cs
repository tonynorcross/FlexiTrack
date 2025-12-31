using System.IO;
using System.Windows;
using System.Windows.Controls;
using FlexiTrack.Desktop.Services;
using FlexiTrack.Desktop.ViewModels;
using FlexiTrack.Desktop.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlexiTrack.Desktop;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _trayIcon;

    public IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        LogService.Log("Application starting...");

        // Global exception handling
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogService.LogError("AppDomain.UnhandledException", ex!);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LogService.LogError("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LogService.LogError("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        const string appName = "FlexiTrack.Desktop";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("FlexiTrack is already running in the system tray.", "FlexiTrack", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Services = ConfigureServices();

        // Create and show tray icon
        _trayIcon = CreateTrayIcon();

        // Try to restore session
        var authService = Services.GetRequiredService<IAuthService>();
        var restored = await authService.TryRestoreSessionAsync();

        if (!restored)
        {
            ShowLoginWindow();
        }
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Token storage
        services.AddSingleton<ITokenStorage, SecureTokenStorage>();

        // HTTP Client with auth handler
        services.AddTransient<AuthenticationHandler>();
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5265";
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<AuthenticationHandler>();

        // Services
        services.AddSingleton<IAuthService, AuthService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<TaskLogViewModel>();
        services.AddTransient<TodaySummaryViewModel>();
        services.AddTransient<ExportViewModel>();

        return services.BuildServiceProvider();
    }

    private TaskbarIcon CreateTrayIcon()
    {
        var mainViewModel = Services.GetRequiredService<MainViewModel>();

        var trayIcon = new TaskbarIcon
        {
            ToolTipText = "FlexiTrack",
            ContextMenu = CreateContextMenu(mainViewModel),
            LeftClickCommand = mainViewModel.ShowPopupCommand
        };

        // Use a built-in icon or embedded resource
        trayIcon.Icon = System.Drawing.SystemIcons.Application;

        mainViewModel.TrayIcon = trayIcon;

        return trayIcon;
    }

    private ContextMenu CreateContextMenu(MainViewModel viewModel)
    {
        var menu = new ContextMenu();

        var logoutItem = new MenuItem { Header = "Logout" };
        logoutItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding(nameof(MainViewModel.LogoutCommand)) { Source = viewModel });
        menu.Items.Add(logoutItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding(nameof(MainViewModel.ExitCommand)) { Source = viewModel });
        menu.Items.Add(exitItem);

        return menu;
    }

    public void ShowLoginWindow(bool showPopupAfterLogin = false)
    {
        LogService.Log($"ShowLoginWindow called, showPopupAfterLogin={showPopupAfterLogin}");
        var loginViewModel = Services.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow
        {
            DataContext = loginViewModel
        };

        loginViewModel.LoginSucceeded += (s, e) =>
        {
            LogService.Log("LoginSucceeded event fired");
            loginWindow.DialogResult = true;
            loginWindow.Close();
        };

        loginViewModel.LoginCancelled += (s, e) =>
        {
            LogService.Log("LoginCancelled event fired");
            // Only shutdown if this was the initial login (not triggered by tray click)
            if (!showPopupAfterLogin)
            {
                Shutdown();
            }
            else
            {
                loginWindow.DialogResult = false;
                loginWindow.Close();
            }
        };

        var result = loginWindow.ShowDialog();
        LogService.Log($"LoginWindow closed, result={result}, showPopupAfterLogin={showPopupAfterLogin}");
        if (result == true)
        {
            LogService.Log("Showing popup after login...");
            // Show popup after successful login
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            mainViewModel.ShowPopupDirectly();
        }
        else if (result != true && !showPopupAfterLogin)
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
