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
    }
}

public record CreateTaskRequest(
    string Date,
    string StartTime,
    string EndTime,
    string Description,
    string? Client = null);
