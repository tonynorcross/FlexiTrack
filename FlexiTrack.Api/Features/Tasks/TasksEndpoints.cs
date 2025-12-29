using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Tasks;

public static class TasksEndpoints
{
    public static void MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks")
            .RequireAuthorization();

        group.MapGet("/logs", async (IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTaskLogs.Query(context.User), ct);
            return Results.Ok(result);
        })
        .WithName("GetTaskLogs");

        group.MapGet("/clients", async (IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetClients.Query(context.User), ct);
            return Results.Ok(result);
        })
        .WithName("GetClients");

        group.MapPost("/", async (CreateTaskRequest request, IMediator mediator, HttpContext context) =>
        {
            var cmd = new CreateTask.Command(
                context.User,
                request.Date,
                request.StartTime,
                request.EndTime,
                request.Description,
                request.Client);
            var result = await mediator.Send(cmd);

            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new { success = true, taskId = result.TaskId });
        })
        .WithName("CreateTaskLog");

        group.MapPut("/{id:int}", async (int id, UpdateTaskRequest request, IMediator mediator, HttpContext context) =>
        {
            var cmd = new UpdateTaskLog.Command(
                context.User,
                id,
                request.Date,
                request.StartTime,
                request.EndTime,
                request.Description,
                request.Client);
            var result = await mediator.Send(cmd);

            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new { success = true });
        })
        .WithName("UpdateTaskLog");

        group.MapDelete("/{id:int}", async (int id, IMediator mediator, HttpContext context) =>
        {
            var cmd = new DeleteTaskLog.Command(context.User, id);
            var result = await mediator.Send(cmd);

            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new { success = true });
        })
        .WithName("DeleteTaskLog");
    }
}

public record CreateTaskRequest(
    string Date,
    string StartTime,
    string EndTime,
    string Description,
    string? Client = null);

public record UpdateTaskRequest(
    string Date,
    string StartTime,
    string EndTime,
    string Description,
    string? Client = null);
