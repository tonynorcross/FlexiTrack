using System.Security.Claims;
using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Users;

public static class GetProfile
{
    public record Query(ClaimsPrincipal User) : IRequest<Response?>;

    public record Response(
        string UserId,
        string Email,
        string FirstName,
        string LastName,
        DateTime Created,
        int LoginCount,
        DateTime? LastLogin,
        int? CompanyId,
        string? CompanyName,
        bool IsCompanyAdmin,
        bool IsSystemAdmin);

    public class Handler : IRequestHandler<Query, Response?>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;

        public Handler(UserManager<ApplicationUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;

            var user = await _db.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null || user.Removed != null) return null;

            return new Response(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Created,
                user.LoginCount,
                user.LastLogin,
                user.CompanyId,
                user.Company?.Name,
                user.IsCompanyAdmin,
                user.IsSystemAdmin);
        }
    }
}
