using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Tasks;

public static class DeleteTaskLog
{
    public record Command(ClaimsPrincipal User, int Id) : IRequest<Response>;

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

            _db.TaskLogs.Remove(taskLog);
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
