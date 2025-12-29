using FlexiTrack.Mediator;

namespace FlexiTrack.Api.Features.Health;

public static class GetHealth
{
    public record Query : IRequest<Response>;

    public record Response(string Status, DateTime Timestamp);

    public class Handler : IRequestHandler<Query, Response>
    {
        public Task<Response> Handle(Query request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Response("Healthy", DateTime.UtcNow));
        }
    }
}
