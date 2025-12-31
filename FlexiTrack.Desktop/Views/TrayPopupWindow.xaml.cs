using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.Services;
using FlexiTrack.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using static FlexiTrack.Desktop.Services.LogService;

namespace FlexiTrack.Desktop.Views;

public partial class TrayPopupWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private TodaySummaryViewModel? _summaryVm;
    private bool _isShowingDialog;
    private bool _isClosing;

    public TrayPopupWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        InitializeViewModels();

        WeakReferenceMessenger.Default.Register<EditTaskMessage>(this, OnEditTaskMessage);
        WeakReferenceMessenger.Default.Register<DeleteTaskMessage>(this, OnDeleteTaskMessage);
        WeakReferenceMessenger.Default.Register<TaskSavedMessage>(this, OnTaskSavedMessage);
        WeakReferenceMessenger.Default.Register<ExportStatusMessage>(this, OnExportStatusMessage);
        WeakReferenceMessenger.Default.Register<ShowingDialogMessage>(this, OnShowingDialogMessage);
    }

    private void OnShowingDialogMessage(object recipient, ShowingDialogMessage message)
    {
        _isShowingDialog = message.IsShowing;
    }

    private async void OnEditTaskMessage(object recipient, EditTaskMessage message)
    {
        _isShowingDialog = true;
        try
        {
            var apiClient = _serviceProvider.GetRequiredService<IApiClient>();
            var clients = await apiClient.GetClientsAsync();

            var editWindow = new EditTaskWindow(apiClient, message.Task, clients)
            {
                Owner = this
            };

            var result = editWindow.ShowDialog();
            if (result == true && editWindow.WasSaved)
            {
                WeakReferenceMessenger.Default.Send(new RefreshTasksMessage());
            }
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private async void OnDeleteTaskMessage(object recipient, DeleteTaskMessage message)
    {
        _isShowingDialog = true;
        try
        {
            var result = MessageBox.Show(
                this,
                $"Delete task '{message.Task.Description}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes && _summaryVm != null)
            {
                await _summaryVm.PerformDeleteAsync(message.Task);
            }
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private void OnTaskSavedMessage(object recipient, TaskSavedMessage message)
    {
        _isShowingDialog = true;
        try
        {
            MessageBox.Show(
                this,
                $"Task logged: {message.Description}\nDuration: {message.Duration}",
                "Task Saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private void OnExportStatusMessage(object recipient, ExportStatusMessage message)
    {
        _isShowingDialog = true;
        try
        {
            MessageBox.Show(
                this,
                message.Message,
                message.Success ? "Export Complete" : "Export",
                MessageBoxButton.OK,
                message.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private async void InitializeViewModels()
    {
        try
        {
            Log("InitializeViewModels starting...");

            // Task Log ViewModel
            Log("Creating TaskLogViewModel...");
            var taskLogVm = _serviceProvider.GetRequiredService<TaskLogViewModel>();
            await taskLogVm.InitializeAsync();
            LogContent.DataContext = taskLogVm;
            Log("TaskLogViewModel initialized");

            // Summary ViewModel
            Log("Creating TodaySummaryViewModel...");
            _summaryVm = _serviceProvider.GetRequiredService<TodaySummaryViewModel>();
            await _summaryVm.InitializeAsync();
            SummaryContent.DataContext = _summaryVm;
            Log("TodaySummaryViewModel initialized");

            // Export ViewModel
            Log("Creating ExportViewModel...");
            var exportVm = _serviceProvider.GetRequiredService<ExportViewModel>();
            await exportVm.InitializeAsync();
            ExportContent.DataContext = exportVm;
            Log("ExportViewModel initialized");

            // Set user name
            var authService = _serviceProvider.GetRequiredService<IAuthService>();
            if (authService.CurrentUser != null)
            {
                UserNameText.Text = $"{authService.CurrentUser.FirstName} {authService.CurrentUser.LastName}";
            }
            Log("InitializeViewModels completed");
        }
        catch (Exception ex)
        {
            LogError("InitializeViewModels", ex);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<EditTaskMessage>(this);
        WeakReferenceMessenger.Default.Unregister<DeleteTaskMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TaskSavedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ExportStatusMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ShowingDialogMessage>(this);
        base.OnClosed(e);
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (LogContent == null || SummaryContent == null || ExportContent == null)
            return;

        LogContent.Visibility = LogTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SummaryContent.Visibility = SummaryTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ExportContent.Visibility = ExportTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Don't close if we're showing a dialog or already closing
        if (_isShowingDialog || _isClosing)
            return;

        // Close popup when it loses focus
        _isClosing = true;
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _isClosing = true;
        base.OnClosing(e);
    }
}
