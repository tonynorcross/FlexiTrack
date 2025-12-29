using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Companies;

public static class AssignUserToCompany
{
    public record Command(string UserId, int? CompanyId) : IRequest<Response>;

    public record Response(bool Success, string? Error = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;

        public Handler(UserManager<ApplicationUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null || user.Removed != null)
            {
                return new Response(false, Error: "User not found");
            }

            if (request.CompanyId.HasValue)
            {
                var company = await _db.Companies.FindAsync([request.CompanyId.Value], cancellationToken);

                if (company == null || company.Removed != null)
                {
                    return new Response(false, Error: "Company not found");
                }
            }

            user.CompanyId = request.CompanyId;

            if (request.CompanyId == null)
            {
                user.IsCompanyAdmin = false;
            }

            await _userManager.UpdateAsync(user);

            return new Response(true);
        }
    }
}
