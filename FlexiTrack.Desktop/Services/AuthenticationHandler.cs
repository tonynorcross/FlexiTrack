using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;

namespace FlexiTrack.Desktop.Services;

public class AuthenticationHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;

    public AuthenticationHandler(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenStorage.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenStorage.ClearTokenAsync();
            WeakReferenceMessenger.Default.Send(new AuthenticationChangedMessage(false));
        }

        return response;
    }
}
