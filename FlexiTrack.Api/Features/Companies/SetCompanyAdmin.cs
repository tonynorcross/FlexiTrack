using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Companies;

public static class SetCompanyAdmin
{
    public record Command(string UserId, bool IsAdmin) : IRequest<Response>;

    public record Response(bool Success, string? Error = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null || user.Removed != null)
            {
                return new Response(false, Error: "User not found");
            }

            if (user.CompanyId == null)
            {
                return new Response(false, Error: "User is not assigned to a company");
            }

            user.IsCompanyAdmin = request.IsAdmin;
            await _userManager.UpdateAsync(user);

            return new Response(true);
        }
    }
}
