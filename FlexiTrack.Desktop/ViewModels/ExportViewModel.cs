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

public record MonthOption(int Year, int Month, string Display);

public partial class ExportViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private IEnumerable<TaskLogDto> _allTasks = [];

    [ObservableProperty]
    private ObservableCollection<string> _availableClients = [];

    [ObservableProperty]
    private string _selectedClient = "all";

    [ObservableProperty]
    private MonthOption? _selectedMonth;

    [ObservableProperty]
    private bool _consolidate;

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<MonthOption> AvailableMonths { get; } = [];

    public ExportViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;

        // Populate last 12 months in reverse order (current month first)
        var today = DateTime.Today;
        for (int i = 0; i < 12; i++)
        {
            var date = today.AddMonths(-i);
            var display = date.ToString("MMM, yyyy");
            AvailableMonths.Add(new MonthOption(date.Year, date.Month, display));
        }
        SelectedMonth = AvailableMonths.FirstOrDefault();

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
            if (SelectedMonth == null)
            {
                StatusMessage = "Please select a month to export.";
                return;
            }

            var firstOfMonth = new DateOnly(SelectedMonth.Year, SelectedMonth.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // If exporting current month, only include up to today
            var endDate = lastOfMonth > today ? today : lastOfMonth;

            var tasks = _allTasks.Where(t => t.Date >= firstOfMonth && t.Date <= endDate);

            // Apply client filter
            if (SelectedClient != "all")
            {
                tasks = tasks.Where(t => t.Client == SelectedClient);
            }

            var taskList = tasks.OrderBy(t => t.Date).ThenBy(t => t.StartTime).ToList();
            Log($"ExportPeriod: {taskList.Count} tasks found");

            if (taskList.Count == 0)
            {
                StatusMessage = "No tasks to export for the selected period.";
                WeakReferenceMessenger.Default.Send(new ExportStatusMessage(StatusMessage, false));
                return;
            }

            // Generate filename: ID-YYYYMMDD.csv or ID-YYYYMMDD-detail.csv
            var clientId = SelectedClient != "all" ? SelectedClient : "All";
            var dateStr = $"{endDate:yyyyMMdd}";
            var detailSuffix = Consolidate ? "" : "-detail";
            var fileName = $"{clientId}-{dateStr}{detailSuffix}.csv";

            WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(true));
            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = fileName,
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                Log("Showing SaveFileDialog for CSV...");
                if (dialog.ShowDialog() == true)
                {
                    Log($"Saving to {dialog.FileName}");
                    var csv = Consolidate ? GenerateConsolidatedCsv(taskList) : GenerateDetailCsv(taskList);
                    File.WriteAllText(dialog.FileName, csv);
                    var exportType = Consolidate ? "consolidated" : "detailed";
                    StatusMessage = $"Exported {taskList.Count} tasks ({exportType}) to {Path.GetFileName(dialog.FileName)}";
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
                        var csv = GenerateDetailCsv(monthTasks);
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

    private string GenerateDetailCsv(IEnumerable<TaskLogDto> tasks)
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

    private string GenerateConsolidatedCsv(IEnumerable<TaskLogDto> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Hours,Tasks");

        // Group tasks by date
        var groupedByDate = tasks
            .GroupBy(t => t.Date)
            .OrderBy(g => g.Key);

        foreach (var group in groupedByDate)
        {
            // Calculate total hours for the date
            var totalMinutes = group.Sum(t => (t.EndTime.ToTimeSpan() - t.StartTime.ToTimeSpan()).TotalMinutes);
            var hours = totalMinutes / 60.0;

            // Combine all task descriptions
            var taskDescriptions = string.Join(", ", group.Select(t => t.Description));

            // Format date as "01 Jan 26"
            var dateStr = group.Key.ToString("dd MMM yy");

            sb.AppendLine($"{dateStr},{hours:F2},{EscapeCsvField(taskDescriptions)}");
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
