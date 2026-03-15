using System.Net;
using System.Text;

namespace ServiceA.Tests.Fakes;

/// <summary>
/// Intercepts any HTTP call to ServiceB and returns stub product data.
/// URL-agnostic — works as long as the handler is in the pipeline.
/// </summary>
public class FakeServiceBMessageHandler : HttpMessageHandler
{
    private const string ProductsJson = """
        [
            {"id": 1, "name": "Laptop", "price": 999.99},
            {"id": 2, "name": "Mouse",  "price": 29.99}
        ]
        """;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ProductsJson, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
