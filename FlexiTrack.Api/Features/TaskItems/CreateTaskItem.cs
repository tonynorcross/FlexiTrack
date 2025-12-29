using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using TaskStatus = FlexiTrack.Api.Data.Entities.TaskStatus;

namespace FlexiTrack.Api.Features.TaskItems;

public static class CreateTaskItem
{
    public record Command(
        string Title,
        string? Description,
        TaskPriority Priority = TaskPriority.Medium,
        DateOnly? DueDate = null,
        string? AssignedUserId = null,
        int? CompanyId = null,
        int? ParentTaskId = null) : IRequest<Response>;

    public record Response(bool Success, int? TaskId = null, string? Error = null);

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

            if (request.ParentTaskId.HasValue)
            {
                var parentTask = await _db.TaskItems.FindAsync([request.ParentTaskId.Value], cancellationToken);
                if (parentTask == null)
                {
                    return new Response(false, Error: "Parent task not found");
                }
                if (parentTask.Removed != null)
                {
                    return new Response(false, Error: "Cannot add subtask to a removed task");
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

            var taskItem = new TaskItem
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Priority = request.Priority,
                Status = TaskStatus.Pending,
                DueDate = request.DueDate,
                AssignedUserId = request.AssignedUserId,
                CompanyId = request.CompanyId,
                ParentTaskId = request.ParentTaskId
            };

            _db.TaskItems.Add(taskItem);
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true, TaskId: taskItem.Id);
        }
    }
}
