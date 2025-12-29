using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Users;

public static class ForgotPassword
{
    public record Command(string Email) : IRequest<Response>;

    public record Response(bool Success, string Message);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<Handler> _logger;

        public Handler(UserManager<ApplicationUser> userManager, ILogger<Handler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user != null && user.Removed == null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation("Password reset token for {Email}: {Token}", request.Email, token);
            }

            return new Response(true, "If an account with that email exists, a password reset link has been sent.");
        }
    }
}
