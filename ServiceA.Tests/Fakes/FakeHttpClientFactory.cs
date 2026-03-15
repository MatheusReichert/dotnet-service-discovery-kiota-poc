namespace ServiceA.Tests.Fakes;

/// <summary>
/// Replaces IHttpClientFactory in the test DI container.
/// Returns HttpClients backed by FakeServiceBMessageHandler,
/// preventing real network calls to ServiceB.
/// </summary>
public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler);
}
