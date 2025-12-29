namespace FlexiTrack.Mediator;

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}

public interface IRequestHandler<TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest<Unit>
{
}
