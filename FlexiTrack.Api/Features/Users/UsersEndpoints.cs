using System.Security.Claims;
using FlexiTrack.Api.Authorization;
using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Users;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapPost("/register", async (Register.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("Register");

        group.MapPost("/login", async (Login.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success ? Results.Ok(result) : Results.Unauthorized();
        })
        .WithName("Login");

        group.MapPost("/forgot-password", async (ForgotPassword.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ForgotPassword");

        group.MapGet("/profile", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProfile.Query(user), ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("GetProfile");

        // Get all users - System Admin only
        group.MapGet("/", async (bool? includeRemoved, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUsers.Query(includeRemoved ?? false), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("GetUsers");

        // Set system admin status - System Admin only
        group.MapPut("/{userId}/system-admin", async (string userId, SetSystemAdminRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetSystemAdmin.Command(userId, request.IsAdmin), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("SetSystemAdmin");
    }
}

public record SetSystemAdminRequest(bool IsAdmin);
