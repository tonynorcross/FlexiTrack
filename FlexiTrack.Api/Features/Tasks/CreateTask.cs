using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Tasks;

public static class CreateTask
{
    public record Command(
        ClaimsPrincipal User,
        string Date,
        string StartTime,
        string EndTime,
        string Description,
        string? Client = null) : IRequest<Response>;

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
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new Response(false, Error: "User not found");
            }

            if (!DateOnly.TryParse(request.Date, out var date))
            {
                return new Response(false, Error: "Invalid date format");
            }

            if (date > DateOnly.FromDateTime(DateTime.Today))
            {
                return new Response(false, Error: "Date cannot be in the future");
            }

            if (!TimeOnly.TryParse(request.StartTime, out var startTime))
            {
                return new Response(false, Error: "Invalid start time format");
            }

            if (!TimeOnly.TryParse(request.EndTime, out var endTime))
            {
                return new Response(false, Error: "Invalid end time format");
            }

            if (endTime <= startTime)
            {
                return new Response(false, Error: "End time must be after start time");
            }

            // Check for overlapping task logs on the same date
            var overlappingTask = await _db.TaskLogs
                .Where(t => t.UserId == userId && t.Date == date)
                .Where(t => startTime < t.EndTime && endTime > t.StartTime)
                .Select(t => new { t.StartTime, t.EndTime })
                .FirstOrDefaultAsync(cancellationToken);

            if (overlappingTask != null)
            {
                return new Response(false, Error: $"Time overlaps with existing task: {overlappingTask.StartTime:HH:mm} - {overlappingTask.EndTime:HH:mm}");
            }

            var taskLog = new TaskLog
            {
                UserId = userId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                Description = request.Description,
                Client = request.Client?.Trim()
            };

            _db.TaskLogs.Add(taskLog);
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true, TaskId: taskLog.Id);
        }
    }
}
