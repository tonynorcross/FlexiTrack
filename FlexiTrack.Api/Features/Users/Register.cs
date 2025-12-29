using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Users;

public static class Register
{
    public record Command(
        string Email,
        string Password,
        string ConfirmPassword,
        string FirstName,
        string LastName) : IRequest<Response>;

    public record Response(bool Success, string? UserId = null, IEnumerable<string>? Errors = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            if (request.Password != request.ConfirmPassword)
            {
                return new Response(false, Errors: ["Passwords do not match"]);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return new Response(true, UserId: user.Id);
            }

            return new Response(false, Errors: result.Errors.Select(e => e.Description));
        }
    }
}
