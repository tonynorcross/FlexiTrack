using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Users;

public static class GetUsers
{
    public record Query(bool IncludeRemoved = false) : IRequest<Response>;

    public record Response(IEnumerable<UserDto> Users);

    public record UserDto(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        DateTime Created,
        int LoginCount,
        DateTime? LastLogin,
        DateTime? Removed,
        int? CompanyId,
        string? CompanyName,
        bool IsCompanyAdmin,
        bool IsSystemAdmin);

    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var query = _db.Users.Include(u => u.Company).AsQueryable();

            if (!request.IncludeRemoved)
            {
                query = query.Where(u => u.Removed == null);
            }

            var users = await query
                .Select(u => new UserDto(
                    u.Id,
                    u.Email!,
                    u.FirstName,
                    u.LastName,
                    u.Created,
                    u.LoginCount,
                    u.LastLogin,
                    u.Removed,
                    u.CompanyId,
                    u.Company != null ? u.Company.Name : null,
                    u.IsCompanyAdmin,
                    u.IsSystemAdmin))
                .ToListAsync(cancellationToken);

            return new Response(users);
        }
    }
}
