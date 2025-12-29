namespace FlexiTrack.Api.Data.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Removed { get; set; }

    // Assigned user
    public string? AssignedUserId { get; set; }
    public ApplicationUser? AssignedUser { get; set; }

    // Company the task belongs to
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    // Parent task for subtasks
    public int? ParentTaskId { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = [];

    // Time logs associated with this task
    public ICollection<TaskLog> TaskLogs { get; set; } = [];
}
