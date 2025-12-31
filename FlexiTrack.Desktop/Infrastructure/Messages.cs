using FlexiTrack.Desktop.Models;

namespace FlexiTrack.Desktop.Infrastructure;

public record AuthenticationChangedMessage(bool IsAuthenticated);

public record TaskCreatedMessage(TaskLogDto Task);

public record RefreshTasksMessage;

public record EditTaskMessage(TaskLogDto Task);

public record DeleteTaskMessage(TaskLogDto Task);

public record TaskSavedMessage(string Description, string Duration);

public record ExportStatusMessage(string Message, bool Success);

public record ShowingDialogMessage(bool IsShowing);
