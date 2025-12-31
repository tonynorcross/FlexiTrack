namespace FlexiTrack.Desktop.Models;

public record TaskLogDto(
    int Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Description,
    string? Client,
    DateTime Created);

public record TaskLogsResponse(IEnumerable<TaskLogDto> TaskLogs);
