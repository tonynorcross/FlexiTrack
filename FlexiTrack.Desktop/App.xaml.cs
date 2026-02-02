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

    protected override void OnStartup(StartupEventArgs e)
    {
        LogService.Log("Application starting...");

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
        _trayIcon = CreateTrayIcon();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<SettingsService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

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

        trayIcon.Icon = System.Drawing.SystemIcons.Application;
        mainViewModel.TrayIcon = trayIcon;

        return trayIcon;
    }

    private ContextMenu CreateContextMenu(MainViewModel viewModel)
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open" };
        openItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding(nameof(MainViewModel.ShowPopupCommand)) { Source = viewModel });
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var settingsItem = new MenuItem { Header = "Settings" };
        settingsItem.Click += (s, e) => ShowSettingsWindow();
        menu.Items.Add(settingsItem);

        var logoutItem = new MenuItem { Header = "Logout" };
        logoutItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding(nameof(MainViewModel.LogoutCommand)) { Source = viewModel });
        menu.Items.Add(logoutItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding(nameof(MainViewModel.ExitCommand)) { Source = viewModel });
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowSettingsWindow()
    {
        var settingsViewModel = Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new SettingsWindow(settingsViewModel);
        settingsWindow.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
