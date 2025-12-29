namespace FlexiTrack.Api.Data.Entities;

public class TaskLog
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public required string Description { get; set; }
    public string? Client { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;

    // Optional reference to a task
    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}
