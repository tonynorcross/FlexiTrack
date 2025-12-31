using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.Models;
using FlexiTrack.Desktop.Services;

namespace FlexiTrack.Desktop.ViewModels;

public partial class TodaySummaryViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;
    private IEnumerable<TaskLogDto> _allTasks = [];

    [ObservableProperty]
    private ObservableCollection<TaskLogDto> _filteredTasks = [];

    [ObservableProperty]
    private string _totalHoursDisplay = "0h 0m";

    [ObservableProperty]
    private decimal? _weeklyTarget;

    [ObservableProperty]
    private string _weekTotalDisplay = "0h 0m";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _userDisplayName = "";

    // Date filter
    [ObservableProperty]
    private string _selectedDateFilter = "today";

    public string[] DateFilters { get; } = ["today", "week", "lastweek", "month", "lastmonth"];
    public string[] DateFilterLabels { get; } = ["Today", "This Week", "Last Week", "This Month", "Last Month"];

    // Client filter
    [ObservableProperty]
    private ObservableCollection<string> _availableClients = [];

    [ObservableProperty]
    private string _selectedClientFilter = "all";

    public TodaySummaryViewModel(IApiClient apiClient, IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;

        WeakReferenceMessenger.Default.Register<RefreshTasksMessage>(this, async (r, m) =>
        {
            await RefreshAsync();
        });

        if (_authService.CurrentUser != null)
        {
            UserDisplayName = $"{_authService.CurrentUser.FirstName} {_authService.CurrentUser.LastName}";
            WeeklyTarget = _authService.CurrentUser.WeeklyHoursTarget;
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    partial void OnSelectedDateFilterChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedClientFilterChanged(string value)
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void SetDateFilter(string filter)
    {
        SelectedDateFilter = filter;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            _allTasks = await _apiClient.GetTaskLogsAsync();

            // Update available clients
            var clients = _allTasks
                .Where(t => !string.IsNullOrEmpty(t.Client))
                .Select(t => t.Client!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            AvailableClients = new ObservableCollection<string>(clients);

            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Apply date filter
        var dateFiltered = SelectedDateFilter switch
        {
            "today" => _allTasks.Where(t => t.Date == today),
            "week" => GetThisWeekTasks(today),
            "lastweek" => GetLastWeekTasks(today),
            "month" => GetThisMonthTasks(today),
            "lastmonth" => GetLastMonthTasks(today),
            _ => _allTasks
        };

        // Apply client filter
        var clientFiltered = SelectedClientFilter switch
        {
            "all" => dateFiltered,
            "none" => dateFiltered.Where(t => string.IsNullOrEmpty(t.Client)),
            _ => dateFiltered.Where(t => t.Client == SelectedClientFilter)
        };

        var filteredList = clientFiltered
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.StartTime)
            .ToList();

        FilteredTasks = new ObservableCollection<TaskLogDto>(filteredList);

        // Calculate filtered total
        var filteredTotal = CalculateTotalDuration(filteredList);
        TotalHoursDisplay = FormatDuration(filteredTotal);

        // Calculate week total (always for progress bar)
        var weekTasks = GetThisWeekTasks(today).ToList();
        var weekTotal = CalculateTotalDuration(weekTasks);
        WeekTotalDisplay = FormatDuration(weekTotal);

        // Calculate progress
        if (WeeklyTarget.HasValue && WeeklyTarget > 0)
        {
            ProgressPercentage = Math.Min(100, (double)(weekTotal.TotalHours / (double)WeeklyTarget.Value) * 100);
        }
    }

    private IEnumerable<TaskLogDto> GetThisWeekTasks(DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        return _allTasks.Where(t => t.Date >= monday && t.Date <= today);
    }

    private IEnumerable<TaskLogDto> GetLastWeekTasks(DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var thisMonday = today.AddDays(-daysSinceMonday);
        var lastMonday = thisMonday.AddDays(-7);
        var lastSunday = thisMonday.AddDays(-1);
        return _allTasks.Where(t => t.Date >= lastMonday && t.Date <= lastSunday);
    }

    private IEnumerable<TaskLogDto> GetThisMonthTasks(DateOnly today)
    {
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        return _allTasks.Where(t => t.Date >= firstOfMonth && t.Date <= today);
    }

    private IEnumerable<TaskLogDto> GetLastMonthTasks(DateOnly today)
    {
        var firstOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var lastOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        return _allTasks.Where(t => t.Date >= firstOfLastMonth && t.Date <= lastOfLastMonth);
    }

    private TimeSpan CalculateTotalDuration(IEnumerable<TaskLogDto> tasks)
    {
        var totalMinutes = tasks.Sum(t =>
        {
            var duration = t.EndTime.ToTimeSpan() - t.StartTime.ToTimeSpan();
            return duration.TotalMinutes;
        });

        return TimeSpan.FromMinutes(totalMinutes);
    }

    private string FormatDuration(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    [RelayCommand]
    private void DeleteTask(TaskLogDto task)
    {
        WeakReferenceMessenger.Default.Send(new DeleteTaskMessage(task));
    }

    public async Task PerformDeleteAsync(TaskLogDto task)
    {
        IsBusy = true;
        try
        {
            var response = await _apiClient.DeleteTaskAsync(task.Id);
            if (response.Success)
            {
                await RefreshAsync();
            }
            else
            {
                MessageBox.Show(response.Error ?? "Failed to delete task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void EditTask(TaskLogDto task)
    {
        WeakReferenceMessenger.Default.Send(new EditTaskMessage(task));
    }
}
