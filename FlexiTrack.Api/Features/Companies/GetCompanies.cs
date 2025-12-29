using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Companies;

public static class GetCompanies
{
    public record Query(bool IncludeRemoved = false) : IRequest<Response>;

    public record Response(IEnumerable<CompanyDto> Companies);

    public record CompanyDto(int Id, string Name, DateTime Created, DateTime? Removed, int UserCount);

    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken = default)
        {
            var query = _db.Companies.AsQueryable();

            if (!request.IncludeRemoved)
            {
                query = query.Where(c => c.Removed == null);
            }

            var companies = await query
                .Select(c => new CompanyDto(
                    c.Id,
                    c.Name,
                    c.Created,
                    c.Removed,
                    c.Users.Count(u => u.Removed == null)))
                .ToListAsync(cancellationToken);

            return new Response(companies);
        }
    }
}
