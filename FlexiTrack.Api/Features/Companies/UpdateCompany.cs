using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Companies;

public static class UpdateCompany
{
    public record Command(int Id, string Name) : IRequest<Response>;

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
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new Response(false, Error: "Company name is required");
            }

            var company = await _db.Companies.FindAsync([request.Id], cancellationToken);

            if (company == null)
            {
                return new Response(false, Error: "Company not found");
            }

            if (company.Removed != null)
            {
                return new Response(false, Error: "Cannot update a removed company");
            }

            company.Name = request.Name.Trim();
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
