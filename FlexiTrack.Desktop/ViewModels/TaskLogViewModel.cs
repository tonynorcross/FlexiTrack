using System.Collections.ObjectModel;
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
    private IEnumerable<TaskLogDto> _allTasks = [];

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

    [ObservableProperty]
    private string _listClientFilter = "all";

    [ObservableProperty]
    private ObservableCollection<TaskLogDto> _tasksForDate = [];

    [ObservableProperty]
    private string _totalDuration = "";

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
        _allTasks = await _apiClient.GetTaskLogsAsync();

        // Set start time based on selected date
        UpdateStartTimeForDate(DateOnly.FromDateTime(Date));

        // Update task list for current date
        UpdateTasksForDate();

        // Do not prepopulate end time
        EndTime = "";
    }

    partial void OnDateChanged(DateTime value)
    {
        UpdateStartTimeForDate(DateOnly.FromDateTime(value));
        UpdateTasksForDate();
    }

    partial void OnListClientFilterChanged(string value)
    {
        UpdateTasksForDate();
    }

    private void UpdateTasksForDate()
    {
        var date = DateOnly.FromDateTime(Date);
        var tasks = _allTasks
            .Where(t => t.Date == date)
            .OrderByDescending(t => t.EndTime)
            .ThenByDescending(t => t.StartTime);

        // Apply client filter
        var filtered = ListClientFilter == "all"
            ? tasks
            : tasks.Where(t => t.Client == ListClientFilter);

        TasksForDate = new ObservableCollection<TaskLogDto>(filtered);

        // Calculate total duration
        var totalMinutes = TasksForDate.Sum(t =>
        {
            var duration = t.EndTime.ToTimeSpan() - t.StartTime.ToTimeSpan();
            return duration.TotalMinutes;
        });

        var hours = (int)(totalMinutes / 60);
        var minutes = (int)(totalMinutes % 60);
        TotalDuration = totalMinutes > 0 ? $"{hours}h {minutes}m" : "";
    }

    private void UpdateStartTimeForDate(DateOnly date)
    {
        // Get last task's end time for the selected date
        var lastTask = _allTasks
            .Where(t => t.Date == date)
            .OrderByDescending(t => t.EndTime)
            .FirstOrDefault();

        if (lastTask != null)
        {
            StartTime = lastTask.EndTime.ToString("HH:mm");
        }
        else if (_authService.CurrentUser?.DefaultStartTime != null)
        {
            StartTime = _authService.CurrentUser.DefaultStartTime;
        }
        else
        {
            StartTime = "09:00";
        }
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

                // Refresh tasks and update list
                _allTasks = await _apiClient.GetTaskLogsAsync();
                Clients = await _apiClient.GetClientsAsync();
                UpdateTasksForDate();

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
