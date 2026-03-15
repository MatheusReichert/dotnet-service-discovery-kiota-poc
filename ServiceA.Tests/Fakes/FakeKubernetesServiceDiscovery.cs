using Shared.Infrastructure;

namespace ServiceA.Tests.Fakes;

/// <summary>
/// Simulates an environment outside Kubernetes — DiscoverServiceUrlAsync returns null,
/// causing the code to fall back to configuration.
/// </summary>
public class FakeKubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    public Task<string?> DiscoverServiceUrlAsync(string apiType) =>
        Task.FromResult<string?>(null);

    public Task<Dictionary<string, string>> DiscoverAllServicesAsync() =>
        Task.FromResult(new Dictionary<string, string>());
}
