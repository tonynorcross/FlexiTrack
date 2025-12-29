using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Api.Services;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Features.Users;

public static class Login
{
    public record Command(string Email, string Password) : IRequest<Response>;

    public record Response(bool Success, string? Token = null, string? Error = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public Handler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null || user.Removed != null)
            {
                return new Response(false, Error: "Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return new Response(false, Error: "Invalid email or password");
            }

            user.LoginCount++;
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = _jwtService.GenerateToken(user);
            return new Response(true, Token: token);
        }
    }
}
