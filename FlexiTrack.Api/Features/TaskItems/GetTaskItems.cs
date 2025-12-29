using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FlexiTrack.Api.Data.Entities.TaskStatus;

namespace FlexiTrack.Api.Features.TaskItems;

public static class GetTaskItems
{
    public record Query(
        int? CompanyId = null,
        string? AssignedUserId = null,
        TaskStatus? Status = null,
        bool IncludeRemoved = false) : IRequest<Response>;

    public record Response(IEnumerable<TaskItemDto> Tasks);

    public record TaskItemDto(
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
        int? ParentTaskId,
        int SubTaskCount);

    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var query = _db.TaskItems.AsQueryable();

            if (!request.IncludeRemoved)
            {
                query = query.Where(t => t.Removed == null);
            }

            if (request.CompanyId.HasValue)
            {
                query = query.Where(t => t.CompanyId == request.CompanyId);
            }

            if (!string.IsNullOrEmpty(request.AssignedUserId))
            {
                query = query.Where(t => t.AssignedUserId == request.AssignedUserId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status);
            }

            var tasks = await query
                .Select(t => new TaskItemDto(
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.DueDate,
                    t.CompletedAt,
                    t.Created,
                    t.Removed,
                    t.AssignedUserId,
                    t.AssignedUser != null ? t.AssignedUser.FirstName + " " + t.AssignedUser.LastName : null,
                    t.CompanyId,
                    t.ParentTaskId,
                    t.SubTasks.Count(st => st.Removed == null)))
                .ToListAsync(cancellationToken);

            return new Response(tasks);
        }
    }
}
