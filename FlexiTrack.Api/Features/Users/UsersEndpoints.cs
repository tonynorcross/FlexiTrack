using System.Security.Claims;
using FlexiTrack.Api.Authorization;
using FlexiTrack.Api.Services;
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

        // Update user settings
        group.MapPut("/settings", async (UpdateUserSettingsRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateUserSettings.Command(
                user,
                request.WeeklyHoursTarget,
                request.DefaultStartTime,
                request.WorkingDays,
                request.HoursPerDay,
                request.BankHolidayRegion), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization()
        .WithName("UpdateUserSettings");

        // Check if a date is a bank holiday
        group.MapGet("/bank-holiday-check", async (string? date, ClaimsPrincipal user, IBankHolidayService holidayService, IMediator mediator, CancellationToken ct) =>
        {
            var profile = await mediator.Send(new GetProfile.Query(user), ct);
            if (profile == null)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrEmpty(profile.BankHolidayRegion))
            {
                return Results.Ok(new BankHolidayCheckResponse(false, null));
            }

            var checkDate = string.IsNullOrEmpty(date)
                ? DateOnly.FromDateTime(DateTime.Today)
                : DateOnly.Parse(date);

            var isHoliday = holidayService.IsBankHoliday(checkDate, profile.BankHolidayRegion);
            var holidayName = isHoliday ? holidayService.GetHolidayName(checkDate, profile.BankHolidayRegion) : null;

            return Results.Ok(new BankHolidayCheckResponse(isHoliday, holidayName));
        })
        .RequireAuthorization()
        .WithName("CheckBankHoliday");
    }
}

public record SetSystemAdminRequest(bool IsAdmin);
public record UpdateUserSettingsRequest(
    decimal? WeeklyHoursTarget,
    string? DefaultStartTime,
    string? WorkingDays,
    decimal? HoursPerDay,
    string? BankHolidayRegion);
public record BankHolidayCheckResponse(bool IsHoliday, string? HolidayName);
