namespace FlexiTrack.Desktop.Models;

public record CreateTaskRequest(
    string Date,
    string StartTime,
    string EndTime,
    string Description,
    string? Client = null);

public record UpdateTaskRequest(
    string Date,
    string StartTime,
    string EndTime,
    string Description,
    string? Client = null);

public record ApiResponse(bool Success, string? Error = null);
