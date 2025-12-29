using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Tasks;

public static class UpdateTaskLog
{
    public record Command(
        ClaimsPrincipal User,
        int Id,
        string Date,
        string StartTime,
        string EndTime,
        string Description,
        string? Client) : IRequest<Response>;

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
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new Response(false, Error: "User not found");
            }

            var taskLog = await _db.TaskLogs
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken);

            if (taskLog == null)
            {
                return new Response(false, Error: "Task log not found");
            }

            if (!DateOnly.TryParse(request.Date, out var date))
            {
                return new Response(false, Error: "Invalid date format");
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

            taskLog.Date = date;
            taskLog.StartTime = startTime;
            taskLog.EndTime = endTime;
            taskLog.Description = request.Description;
            taskLog.Client = request.Client?.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
