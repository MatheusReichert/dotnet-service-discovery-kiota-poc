using System.Net;

namespace ServiceA.Tests;

/// <summary>
/// Integration tests for endpoints that call ServiceB.
///
/// How the mock works:
///   - IKubernetesServiceDiscovery → returns null (no K8s)
///   - IHttpClientFactory → returns HttpClient with FakeServiceBMessageHandler
///   - FakeServiceBMessageHandler → returns [Laptop, Mouse] for any request
///
/// This verifies that ServiceA correctly processes ServiceB's response,
/// without relying on real network or ServiceB being up.
/// </summary>
public class CrossServiceTests : IClassFixture<ServiceAWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CrossServiceTests(ServiceAWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---------- /api/users/with-products/{id} (HTTP manual) ----------

    [Fact]
    public async Task GetUserWithProducts_ReturnsOk_WithProductsFromServiceB()
    {
        var response = await _client.GetAsync("/api/users/with-products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Laptop", body);
    }

    [Fact]
    public async Task GetUserWithProducts_ResponseContains_DiscoveredUrl()
    {
        var response = await _client.GetAsync("/api/users/with-products/1");
        var body = await response.Content.ReadAsStringAsync();

        // Expected fallback URL when K8s returns null and config is not set
        Assert.Contains("http://serviceb", body);
    }

    // ---------- /api/users/with-products-typed/{id} (Kiota type-safe) ----------

    [Fact]
    public async Task GetUserWithProductsTyped_ReturnsOk_WithProductsFromServiceB()
    {
        var response = await _client.GetAsync("/api/users/with-products-typed/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Laptop", body);
    }

    [Fact]
    public async Task GetUserWithProductsTyped_ResponseContains_KiotaMetadata()
    {
        var response = await _client.GetAsync("/api/users/with-products-typed/1");
        var body = await response.Content.ReadAsStringAsync();

        // Message defined in the endpoint to identify the Kiota path
        Assert.Contains("Kiota", body);
    }

    // ---------- /api/services/catalog ----------

    [Fact]
    public async Task GetServiceCatalog_ReturnsOk_WithEmptyCatalog_WhenNotInKubernetes()
    {
        var response = await _client.GetAsync("/api/services/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("catalog", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"count\":0", body.Replace(" ", ""));
    }
}
