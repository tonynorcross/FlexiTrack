using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.Models;
using FlexiTrack.Desktop.Services;

namespace FlexiTrack.Desktop.ViewModels;

public partial class TaskLogViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private string _startTime = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private string _endTime = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string? _client;

    [ObservableProperty]
    private IEnumerable<string> _clients = [];

    public string Duration
    {
        get
        {
            if (TimeOnly.TryParse(StartTime, out var start) && TimeOnly.TryParse(EndTime, out var end))
            {
                var duration = end.ToTimeSpan() - start.ToTimeSpan();
                if (duration.TotalMinutes > 0)
                {
                    return $"{duration.Hours}h {duration.Minutes}m";
                }
            }
            return "";
        }
    }

    public TaskLogViewModel(IApiClient apiClient, IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        Clients = await _apiClient.GetClientsAsync();

        // Get last task's end time or use user's default for start time
        var tasks = await _apiClient.GetTaskLogsAsync();
        var todayTasks = tasks
            .Where(t => t.Date == DateOnly.FromDateTime(DateTime.Today))
            .OrderByDescending(t => t.EndTime)
            .FirstOrDefault();

        if (todayTasks != null)
        {
            StartTime = todayTasks.EndTime.ToString("HH:mm");
        }
        else if (_authService.CurrentUser?.DefaultStartTime != null)
        {
            StartTime = _authService.CurrentUser.DefaultStartTime;
        }
        else
        {
            StartTime = "09:00";
        }

        // Do not prepopulate end time
        EndTime = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateInput())
            return;

        IsBusy = true;
        ClearError();

        try
        {
            var request = new CreateTaskRequest(
                Date: DateOnly.FromDateTime(Date).ToString("yyyy-MM-dd"),
                StartTime: StartTime,
                EndTime: EndTime,
                Description: Description.Trim(),
                Client: string.IsNullOrWhiteSpace(Client) ? null : Client.Trim());

            var result = await _apiClient.CreateTaskAsync(request);

            if (result.Success)
            {
                var savedDuration = Duration;
                var savedDescription = Description.Trim();
                var savedEndTime = EndTime;

                // Reset form - start time becomes previous end time, end time is cleared
                Description = "";
                StartTime = savedEndTime;
                EndTime = "";

                WeakReferenceMessenger.Default.Send(new RefreshTasksMessage());
                WeakReferenceMessenger.Default.Send(new TaskSavedMessage(savedDescription, savedDuration));
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to save task";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateInput()
    {
        ClearError();

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Please enter a description";
            return false;
        }

        if (!TimeOnly.TryParse(StartTime, out var start))
        {
            ErrorMessage = "Invalid start time format (use HH:mm)";
            return false;
        }

        if (!TimeOnly.TryParse(EndTime, out var end))
        {
            ErrorMessage = "Invalid end time format (use HH:mm)";
            return false;
        }

        if (end <= start)
        {
            ErrorMessage = "End time must be after start time";
            return false;
        }

        if (Date > DateTime.Today)
        {
            ErrorMessage = "Cannot log future dates";
            return false;
        }

        return true;
    }
}
