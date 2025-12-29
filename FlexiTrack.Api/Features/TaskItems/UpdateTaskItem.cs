using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using TaskStatus = FlexiTrack.Api.Data.Entities.TaskStatus;

namespace FlexiTrack.Api.Features.TaskItems;

public static class UpdateTaskItem
{
    public record Command(
        int Id,
        string Title,
        string? Description,
        TaskStatus Status,
        TaskPriority Priority,
        DateOnly? DueDate,
        string? AssignedUserId,
        int? CompanyId,
        int? ParentTaskId) : IRequest<Response>;

    public record Response(bool Success, string? Error = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return new Response(false, Error: "Task title is required");
            }

            var taskItem = await _db.TaskItems.FindAsync([request.Id], cancellationToken);

            if (taskItem == null)
            {
                return new Response(false, Error: "Task not found");
            }

            if (taskItem.Removed != null)
            {
                return new Response(false, Error: "Cannot update a removed task");
            }

            if (request.ParentTaskId.HasValue)
            {
                if (request.ParentTaskId == request.Id)
                {
                    return new Response(false, Error: "Task cannot be its own parent");
                }

                var parentTask = await _db.TaskItems.FindAsync([request.ParentTaskId.Value], cancellationToken);
                if (parentTask == null)
                {
                    return new Response(false, Error: "Parent task not found");
                }
                if (parentTask.Removed != null)
                {
                    return new Response(false, Error: "Cannot set removed task as parent");
                }
            }

            if (request.AssignedUserId != null)
            {
                var user = await _db.Users.FindAsync([request.AssignedUserId], cancellationToken);
                if (user == null)
                {
                    return new Response(false, Error: "Assigned user not found");
                }
            }

            if (request.CompanyId.HasValue)
            {
                var company = await _db.Companies.FindAsync([request.CompanyId.Value], cancellationToken);
                if (company == null || company.Removed != null)
                {
                    return new Response(false, Error: "Company not found");
                }
            }

            var wasCompleted = taskItem.Status == TaskStatus.Completed;
            var isNowCompleted = request.Status == TaskStatus.Completed;

            taskItem.Title = request.Title.Trim();
            taskItem.Description = request.Description?.Trim();
            taskItem.Status = request.Status;
            taskItem.Priority = request.Priority;
            taskItem.DueDate = request.DueDate;
            taskItem.AssignedUserId = request.AssignedUserId;
            taskItem.CompanyId = request.CompanyId;
            taskItem.ParentTaskId = request.ParentTaskId;

            if (!wasCompleted && isNowCompleted)
            {
                taskItem.CompletedAt = DateTime.UtcNow;
            }
            else if (wasCompleted && !isNowCompleted)
            {
                taskItem.CompletedAt = null;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
