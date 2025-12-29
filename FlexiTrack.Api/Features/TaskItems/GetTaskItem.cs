using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FlexiTrack.Api.Data.Entities.TaskStatus;

namespace FlexiTrack.Api.Features.TaskItems;

public static class GetTaskItem
{
    public record Query(int Id) : IRequest<Response?>;

    public record Response(
        int Id,
        string Title,
        string? Description,
        TaskStatus Status,
        Data.Entities.TaskPriority Priority,
        DateOnly? DueDate,
        DateTime? CompletedAt,
        DateTime Created,
        DateTime? Removed,
        string? AssignedUserId,
        string? AssignedUserName,
        int? CompanyId,
        string? CompanyName,
        int? ParentTaskId,
        IEnumerable<SubTaskDto> SubTasks,
        IEnumerable<TaskLogDto> TaskLogs);

    public record SubTaskDto(
        int Id,
        string Title,
        TaskStatus Status,
        Data.Entities.TaskPriority Priority,
        DateOnly? DueDate);

    public record TaskLogDto(
        int Id,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string Description,
        string? UserName);

    public class Handler : IRequestHandler<Query, Response?>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var task = await _db.TaskItems
                .Include(t => t.AssignedUser)
                .Include(t => t.Company)
                .Include(t => t.SubTasks.Where(st => st.Removed == null))
                .Include(t => t.TaskLogs)
                    .ThenInclude(tl => tl.User)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (task == null) return null;

            return new Response(
                task.Id,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.CompletedAt,
                task.Created,
                task.Removed,
                task.AssignedUserId,
                task.AssignedUser != null ? $"{task.AssignedUser.FirstName} {task.AssignedUser.LastName}" : null,
                task.CompanyId,
                task.Company?.Name,
                task.ParentTaskId,
                task.SubTasks.Select(st => new SubTaskDto(
                    st.Id,
                    st.Title,
                    st.Status,
                    st.Priority,
                    st.DueDate)),
                task.TaskLogs.Select(tl => new TaskLogDto(
                    tl.Id,
                    tl.Date,
                    tl.StartTime,
                    tl.EndTime,
                    tl.Description,
                    tl.User != null ? $"{tl.User.FirstName} {tl.User.LastName}" : null)));
        }
    }
}
