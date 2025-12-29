using System.Security.Claims;
using FlexiTrack.Api.Authorization;
using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Companies;

public static class CompaniesEndpoints
{
    public static void MapCompaniesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .RequireAuthorization();

        // Get all companies - System Admin only
        group.MapGet("/", async (bool? includeRemoved, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCompanies.Query(includeRemoved ?? false), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("GetCompanies");

        // Get single company - System Admin or Company Admin of that company
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!CanAccessCompany(user, id))
            {
                return Results.Forbid();
            }

            var result = await mediator.Send(new GetCompany.Query(id), ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCompany");

        // Create company - System Admin only
        group.MapPost("/", async (CreateCompany.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success ? Results.Created($"/api/companies/{result.CompanyId}", result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("CreateCompany");

        // Update company - System Admin only
        group.MapPut("/{id:int}", async (int id, UpdateCompanyRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateCompany.Command(id, request.Name), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("UpdateCompany");

        // Delete company - System Admin only
        group.MapDelete("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCompany.Command(id), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("DeleteCompany");

        // Get company users - System Admin or Company Admin of that company
        group.MapGet("/{id:int}/users", async (int id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!CanAccessCompany(user, id))
            {
                return Results.Forbid();
            }

            var result = await mediator.Send(new GetCompanyUsers.Query(id), ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCompanyUsers");

        // Assign user to company - System Admin only
        group.MapPost("/{id:int}/users", async (int id, AssignUserRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AssignUserToCompany.Command(request.UserId, id), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("AssignUserToCompany");

        // Remove user from company - System Admin or Company Admin of that company
        group.MapDelete("/{id:int}/users/{userId}", async (int id, string userId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            if (!CanAccessCompany(user, id))
            {
                return Results.Forbid();
            }

            var result = await mediator.Send(new RemoveUserFromCompany.Command(userId, id), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("RemoveUserFromCompany");

        // Set company admin status - System Admin only
        group.MapPut("/{id:int}/users/{userId}/admin", async (int id, string userId, SetAdminRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetCompanyAdmin.Command(userId, request.IsAdmin), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
        .WithName("SetCompanyAdmin");
    }

    private static bool CanAccessCompany(ClaimsPrincipal user, int companyId)
    {
        var isSystemAdmin = user.FindFirstValue("isSystemAdmin") == "true";
        if (isSystemAdmin) return true;

        var isCompanyAdmin = user.FindFirstValue("isCompanyAdmin") == "true";
        var userCompanyId = user.FindFirstValue("companyId");

        return isCompanyAdmin && userCompanyId == companyId.ToString();
    }
}

public record UpdateCompanyRequest(string Name);
public record AssignUserRequest(string UserId);
public record SetAdminRequest(bool IsAdmin);
