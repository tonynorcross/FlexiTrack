using System.Windows;
using System.Windows.Controls;
using FlexiTrack.Desktop.Models;
using FlexiTrack.Desktop.Services;

namespace FlexiTrack.Desktop.Views;

public partial class EditTaskWindow : Window
{
    private readonly IApiClient _apiClient;
    private readonly TaskLogDto _task;
    private readonly IEnumerable<string> _clients;

    public bool WasSaved { get; private set; }

    public EditTaskWindow(IApiClient apiClient, TaskLogDto task, IEnumerable<string> clients)
    {
        _apiClient = apiClient;
        _task = task;
        _clients = clients;

        InitializeComponent();
        LoadTask();
    }

    private void LoadTask()
    {
        DatePicker.SelectedDate = _task.Date.ToDateTime(TimeOnly.MinValue);
        StartTimeBox.Text = _task.StartTime.ToString("HH:mm");
        EndTimeBox.Text = _task.EndTime.ToString("HH:mm");
        DescriptionBox.Text = _task.Description;
        ClientCombo.Text = _task.Client ?? "";

        foreach (var client in _clients)
        {
            ClientCombo.Items.Add(new ComboBoxItem { Content = client });
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        // Validate
        if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            ShowError("Please enter a description");
            return;
        }

        if (!TimeOnly.TryParse(StartTimeBox.Text, out var startTime))
        {
            ShowError("Invalid start time format (use HH:mm)");
            return;
        }

        if (!TimeOnly.TryParse(EndTimeBox.Text, out var endTime))
        {
            ShowError("Invalid end time format (use HH:mm)");
            return;
        }

        if (endTime <= startTime)
        {
            ShowError("End time must be after start time");
            return;
        }

        if (DatePicker.SelectedDate == null)
        {
            ShowError("Please select a date");
            return;
        }

        var date = DateOnly.FromDateTime(DatePicker.SelectedDate.Value);
        if (date > DateOnly.FromDateTime(DateTime.Today))
        {
            ShowError("Cannot log future dates");
            return;
        }

        var request = new UpdateTaskRequest(
            Date: date.ToString("yyyy-MM-dd"),
            StartTime: StartTimeBox.Text,
            EndTime: EndTimeBox.Text,
            Description: DescriptionBox.Text.Trim(),
            Client: string.IsNullOrWhiteSpace(ClientCombo.Text) ? null : ClientCombo.Text.Trim());

        var result = await _apiClient.UpdateTaskAsync(_task.Id, request);

        if (result.Success)
        {
            WasSaved = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ShowError(result.Error ?? "Failed to update task");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
