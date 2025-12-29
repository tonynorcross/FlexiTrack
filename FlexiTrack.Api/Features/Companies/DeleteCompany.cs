using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.Companies;

public static class DeleteCompany
{
    public record Command(int Id) : IRequest<Response>;

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
            var company = await _db.Companies
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (company == null)
            {
                return new Response(false, Error: "Company not found");
            }

            if (company.Removed != null)
            {
                return new Response(false, Error: "Company already removed");
            }

            company.Removed = DateTime.UtcNow;

            foreach (var user in company.Users)
            {
                user.CompanyId = null;
                user.IsCompanyAdmin = false;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
