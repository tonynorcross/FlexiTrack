using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetHealth.Query(), ct);
            return Results.Ok(result);
        })
        .WithName("GetHealth");
    }
}
