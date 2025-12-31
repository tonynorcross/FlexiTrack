using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.Models;
using FlexiTrack.Desktop.Services;
using Microsoft.Win32;
using static FlexiTrack.Desktop.Services.LogService;

namespace FlexiTrack.Desktop.ViewModels;

public partial class ExportViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private IEnumerable<TaskLogDto> _allTasks = [];

    [ObservableProperty]
    private ObservableCollection<string> _availableClients = [];

    [ObservableProperty]
    private string _selectedClient = "all";

    [ObservableProperty]
    private string _selectedPeriod = "month";

    [ObservableProperty]
    private string _statusMessage = "";

    public string[] Periods { get; } = ["month", "lastmonth"];
    public string[] PeriodLabels { get; } = ["This Month", "Last Month"];

    public ExportViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;

        WeakReferenceMessenger.Default.Register<RefreshTasksMessage>(this, async (r, m) =>
        {
            await RefreshAsync();
        });
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            _allTasks = await _apiClient.GetTaskLogsAsync();

            var clients = _allTasks
                .Where(t => !string.IsNullOrEmpty(t.Client))
                .Select(t => t.Client!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            AvailableClients = new ObservableCollection<string>(clients);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ExportPeriod()
    {
        Log("ExportPeriod called");
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            IEnumerable<TaskLogDto> tasks;
            string periodName;

            if (SelectedPeriod == "month")
            {
                var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
                tasks = _allTasks.Where(t => t.Date >= firstOfMonth && t.Date <= today);
                periodName = today.ToString("yyyy-MM");
            }
            else
            {
                var firstOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
                var lastOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
                tasks = _allTasks.Where(t => t.Date >= firstOfLastMonth && t.Date <= lastOfLastMonth);
                periodName = firstOfLastMonth.ToString("yyyy-MM");
            }

            // Apply client filter
            if (SelectedClient != "all")
            {
                tasks = tasks.Where(t => t.Client == SelectedClient);
                periodName += $"_{SelectedClient}";
            }

            var taskList = tasks.OrderBy(t => t.Date).ThenBy(t => t.StartTime).ToList();
            Log($"ExportPeriod: {taskList.Count} tasks found");

            if (taskList.Count == 0)
            {
                StatusMessage = "No tasks to export for the selected period.";
                WeakReferenceMessenger.Default.Send(new ExportStatusMessage(StatusMessage, false));
                return;
            }

            WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(true));
            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"FlexiTrack_{periodName}.csv",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                Log("Showing SaveFileDialog for CSV...");
                if (dialog.ShowDialog() == true)
                {
                    Log($"Saving to {dialog.FileName}");
                    var csv = GenerateCsv(taskList);
                    File.WriteAllText(dialog.FileName, csv);
                    StatusMessage = $"Exported {taskList.Count} tasks to {Path.GetFileName(dialog.FileName)}";
                    WeakReferenceMessenger.Default.Send(new ExportStatusMessage(StatusMessage, true));
                    Log("CSV export completed");
                }
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(false));
            }
        }
        catch (Exception ex)
        {
            LogError("ExportPeriod", ex);
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportAll()
    {
        Log("ExportAll called");
        try
        {
            var tasks = _allTasks;

            // Apply client filter
            if (SelectedClient != "all")
            {
                tasks = tasks.Where(t => t.Client == SelectedClient);
            }

            var taskList = tasks.OrderBy(t => t.Date).ThenBy(t => t.StartTime).ToList();
            Log($"ExportAll: {taskList.Count} tasks found");

            if (taskList.Count == 0)
            {
                StatusMessage = "No tasks to export.";
                WeakReferenceMessenger.Default.Send(new ExportStatusMessage(StatusMessage, false));
                return;
            }

            var clientSuffix = SelectedClient != "all" ? $"_{SelectedClient}" : "";

            WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(true));
            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = $"FlexiTrack_All{clientSuffix}.zip",
                    DefaultExt = ".zip",
                    Filter = "ZIP files (*.zip)|*.zip"
                };

                Log("Showing SaveFileDialog for ZIP...");
                if (dialog.ShowDialog() == true)
                {
                    Log($"Saving to {dialog.FileName}");
                    // Group tasks by year-month
                    var groupedTasks = taskList
                        .GroupBy(t => new { t.Date.Year, t.Date.Month })
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month)
                        .ToList();

                    using var zipStream = new FileStream(dialog.FileName, FileMode.Create);
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

                    var totalTasks = 0;
                    foreach (var group in groupedTasks)
                    {
                        var monthTasks = group.OrderBy(t => t.Date).ThenBy(t => t.StartTime).ToList();
                        var csv = GenerateCsv(monthTasks);
                        var fileName = $"{group.Key.Year}-{group.Key.Month:D2}{clientSuffix}.csv";

                        var entry = archive.CreateEntry(fileName);
                        using var entryStream = entry.Open();
                        using var writer = new StreamWriter(entryStream);
                        writer.Write(csv);

                        totalTasks += monthTasks.Count;
                    }

                    StatusMessage = $"Exported {totalTasks} tasks across {groupedTasks.Count} months to {Path.GetFileName(dialog.FileName)}";
                    WeakReferenceMessenger.Default.Send(new ExportStatusMessage(StatusMessage, true));
                    Log("ZIP export completed");
                }
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(false));
            }
        }
        catch (Exception ex)
        {
            LogError("ExportAll", ex);
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private string GenerateCsv(IEnumerable<TaskLogDto> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Start Time,End Time,Duration,Description,Client");

        foreach (var task in tasks)
        {
            var duration = task.EndTime.ToTimeSpan() - task.StartTime.ToTimeSpan();
            var durationStr = $"{(int)duration.TotalHours}:{duration.Minutes:D2}";

            var description = EscapeCsvField(task.Description);
            var client = EscapeCsvField(task.Client ?? "");

            sb.AppendLine($"{task.Date:yyyy-MM-dd},{task.StartTime:HH:mm},{task.EndTime:HH:mm},{durationStr},{description},{client}");
        }

        return sb.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
