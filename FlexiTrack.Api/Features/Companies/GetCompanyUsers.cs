using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Companies;

public static class GetCompanyUsers
{
    public record Query(int CompanyId) : IRequest<Response?>;

    public record Response(IEnumerable<UserDto> Users);

    public record UserDto(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        bool IsCompanyAdmin,
        DateTime Created,
        int LoginCount,
        DateTime? LastLogin);

    public class Handler : IRequestHandler<Query, Response?>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies.FindAsync([request.CompanyId], cancellationToken);

            if (company == null || company.Removed != null)
            {
                return null;
            }

            var users = await _db.Users
                .Where(u => u.CompanyId == request.CompanyId && u.Removed == null)
                .Select(u => new UserDto(
                    u.Id,
                    u.Email!,
                    u.FirstName,
                    u.LastName,
                    u.IsCompanyAdmin,
                    u.Created,
                    u.LoginCount,
                    u.LastLogin))
                .ToListAsync(cancellationToken);

            return new Response(users);
        }
    }
}
