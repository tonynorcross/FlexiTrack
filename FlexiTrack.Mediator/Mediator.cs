namespace FlexiTrack.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var method = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found on handler for {requestType.Name}");

        var result = method.Invoke(handler, [request, cancellationToken]);

        if (result is Task<TResponse> task)
        {
            return await task;
        }

        throw new InvalidOperationException($"Handler for {requestType.Name} did not return expected type");
    }
}
