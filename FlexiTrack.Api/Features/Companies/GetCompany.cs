using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Companies;

public static class GetCompany
{
    public record Query(int Id) : IRequest<Response?>;

    public record Response(int Id, string Name, DateTime Created, DateTime? Removed, IEnumerable<UserDto> Users);

    public record UserDto(string Id, string Email, string FirstName, string LastName, bool IsCompanyAdmin);

    public class Handler : IRequestHandler<Query, Response?>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies
                .Include(c => c.Users.Where(u => u.Removed == null))
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (company == null) return null;

            return new Response(
                company.Id,
                company.Name,
                company.Created,
                company.Removed,
                company.Users.Select(u => new UserDto(u.Id, u.Email!, u.FirstName, u.LastName, u.IsCompanyAdmin)));
        }
    }
}
