using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Users;

public static class SetSystemAdmin
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

            user.IsSystemAdmin = request.IsAdmin;
            await _userManager.UpdateAsync(user);

            return new Response(true);
        }
    }
}
