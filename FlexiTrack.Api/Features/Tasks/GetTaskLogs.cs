using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Tasks;

public static class GetTaskLogs
{
    public record Query(ClaimsPrincipal User) : IRequest<Response>;

    public record Response(IEnumerable<TaskLogDto> TaskLogs);

    public record TaskLogDto(
        int Id,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string Description,
        string? Client,
        DateTime Created);

    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new Response([]);
            }

            var taskLogs = await _db.TaskLogs
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.StartTime)
                .Select(t => new TaskLogDto(
                    t.Id,
                    t.Date,
                    t.StartTime,
                    t.EndTime,
                    t.Description,
                    t.Client,
                    t.Created))
                .ToListAsync(cancellationToken);

            return new Response(taskLogs);
        }
    }
}
