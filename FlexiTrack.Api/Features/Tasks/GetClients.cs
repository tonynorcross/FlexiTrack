using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Tasks;

public static class GetClients
{
    public record Query(ClaimsPrincipal User) : IRequest<Response>;

    public record Response(IEnumerable<string> Clients);

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

            var clients = await _db.TaskLogs
                .Where(t => t.UserId == userId && t.Client != null && t.Client != "")
                .Select(t => t.Client!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            return new Response(clients);
        }
    }
}
