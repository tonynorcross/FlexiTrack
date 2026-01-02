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
        string? DefaultStartTime,
        string? WorkingDays,
        decimal? HoursPerDay,
        string? BankHolidayRegion) : IRequest<Response>;

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

            // Validate HoursPerDay
            if (request.HoursPerDay.HasValue)
            {
                if (request.HoursPerDay.Value < 0)
                {
                    return new Response(false, Error: "Hours per day cannot be negative");
                }
                if (request.HoursPerDay.Value > 24)
                {
                    return new Response(false, Error: "Hours per day cannot exceed 24 hours");
                }
            }

            // Validate BankHolidayRegion
            if (request.BankHolidayRegion != null && request.BankHolidayRegion != "UK" && request.BankHolidayRegion != "US")
            {
                return new Response(false, Error: "Invalid bank holiday region. Must be 'UK', 'US', or null");
            }

            // Validate WorkingDays format
            if (!string.IsNullOrEmpty(request.WorkingDays))
            {
                var validDays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                var days = request.WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var day in days)
                {
                    if (!validDays.Contains(day.Trim()))
                    {
                        return new Response(false, Error: "Invalid working days format. Use comma-separated values: Mon,Tue,Wed,Thu,Fri,Sat,Sun");
                    }
                }
            }

            user.WeeklyHoursTarget = request.WeeklyHoursTarget;
            user.DefaultStartTime = defaultStartTime;

            if (request.WorkingDays != null)
            {
                user.WorkingDays = request.WorkingDays;
            }

            if (request.HoursPerDay.HasValue)
            {
                user.HoursPerDay = request.HoursPerDay.Value;
            }

            user.BankHolidayRegion = request.BankHolidayRegion;

            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
