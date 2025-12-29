using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Mediator;
using TaskStatus = FlexiTrack.Api.Data.Entities.TaskStatus;

namespace FlexiTrack.Api.Features.TaskItems;

public static class TaskItemsEndpoints
{
    public static void MapTaskItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/taskitems")
            .RequireAuthorization();

        // Get all tasks with optional filters
        group.MapGet("/", async (
            int? companyId,
            string? assignedUserId,
            TaskStatus? status,
            bool? includeRemoved,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTaskItems.Query(
                companyId,
                assignedUserId,
                status,
                includeRemoved ?? false), ct);
            return Results.Ok(result);
        })
        .WithName("GetTaskItems");

        // Get single task
        group.MapGet("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTaskItem.Query(id), ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetTaskItem");

        // Create task
        group.MapPost("/", async (CreateTaskItemRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateTaskItem.Command(
                request.Title,
                request.Description,
                request.Priority ?? TaskPriority.Medium,
                request.DueDate,
                request.AssignedUserId,
                request.CompanyId,
                request.ParentTaskId), ct);

            return result.Success
                ? Results.Created($"/api/taskitems/{result.TaskId}", result)
                : Results.BadRequest(result);
        })
        .WithName("CreateTaskItem");

        // Update task
        group.MapPut("/{id:int}", async (int id, UpdateTaskItemRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateTaskItem.Command(
                id,
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.DueDate,
                request.AssignedUserId,
                request.CompanyId,
                request.ParentTaskId), ct);

            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("UpdateTaskItem");

        // Delete task (soft delete)
        group.MapDelete("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteTaskItem.Command(id), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("DeleteTaskItem");
    }
}

public record CreateTaskItemRequest(
    string Title,
    string? Description = null,
    TaskPriority? Priority = null,
    DateOnly? DueDate = null,
    string? AssignedUserId = null,
    int? CompanyId = null,
    int? ParentTaskId = null);

public record UpdateTaskItemRequest(
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    string? AssignedUserId,
    int? CompanyId,
    int? ParentTaskId);
