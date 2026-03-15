using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ServiceA.Tests;

/// <summary>
/// Integration tests for basic user endpoints.
/// No external dependencies — ServiceB and K8s are fully mocked.
/// </summary>
public class UsersEndpointTests : IClassFixture<ServiceAWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointTests(ServiceAWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithAliceAndBob()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice", body);
        Assert.Contains("Bob", body);
    }

    [Fact]
    public async Task GetUserById_ReturnsOk_WithCorrectId()
    {
        var response = await _client.GetAsync("/api/users/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("42", body);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WithLocationHeader()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/users", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}
