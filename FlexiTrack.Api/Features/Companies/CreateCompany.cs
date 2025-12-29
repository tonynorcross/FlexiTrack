using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Companies;

public static class CreateCompany
{
    public record Command(string Name) : IRequest<Response>;

    public record Response(bool Success, int? CompanyId = null, string? Error = null);

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

            var company = new Company { Name = request.Name.Trim() };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true, CompanyId: company.Id);
        }
    }
}
