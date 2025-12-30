using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Users;

public static class UpdateUserSettings
{
    public record Command(
        ClaimsPrincipal User,
        decimal? WeeklyHoursTarget,
        string? DefaultStartTime) : IRequest<Response>;

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

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                return new Response(false, Error: "User not found");
            }

            if (request.WeeklyHoursTarget.HasValue && request.WeeklyHoursTarget.Value < 0)
            {
                return new Response(false, Error: "Weekly hours target cannot be negative");
            }

            if (request.WeeklyHoursTarget.HasValue && request.WeeklyHoursTarget.Value > 168)
            {
                return new Response(false, Error: "Weekly hours target cannot exceed 168 hours");
            }

            TimeOnly? defaultStartTime = null;
            if (!string.IsNullOrEmpty(request.DefaultStartTime))
            {
                if (!TimeOnly.TryParse(request.DefaultStartTime, out var parsedTime))
                {
                    return new Response(false, Error: "Invalid start time format");
                }
                defaultStartTime = parsedTime;
            }

            user.WeeklyHoursTarget = request.WeeklyHoursTarget;
            user.DefaultStartTime = defaultStartTime;
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
