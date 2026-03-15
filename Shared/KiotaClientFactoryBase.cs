using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Shared.Infrastructure;

/// <summary>
/// Base class for factories that combine Automatic Discovery + Kiota Client
/// </summary>
public abstract class KiotaClientFactoryBase<TClient> where TClient : class
{
    protected readonly IKubernetesServiceDiscovery K8sDiscovery;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IConfiguration Configuration;

    private string? _cachedBaseUrl;

    protected KiotaClientFactoryBase(
        IKubernetesServiceDiscovery k8sDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        K8sDiscovery = k8sDiscovery;
        HttpClientFactory = httpClientFactory;
        Configuration = configuration;
    }

    /// <summary>
    /// API type label used for discovery (e.g. "products-api")
    /// </summary>
    protected abstract string ApiType { get; }

    /// <summary>
    /// Configuration key used as fallback (e.g. "Services:ServiceB:Url")
    /// </summary>
    protected abstract string ConfigurationKey { get; }

    /// <summary>
    /// Default fallback URL (e.g. "http://serviceb")
    /// </summary>
    protected abstract string DefaultUrl { get; }

    /// <summary>
    /// Creates the Kiota client instance with the configured adapter
    /// </summary>
    protected abstract TClient CreateClient(HttpClientRequestAdapter adapter);

    /// <summary>
    /// Creates a Kiota client with the automatically discovered URL.
    /// The URL is resolved only on the first call and cached for subsequent ones
    /// (the factory must be registered as a Singleton in DI).
    /// </summary>
    public async Task<TClient> CreateClientAsync()
    {
        // 1. Resolve URL only once — no K8s query per request
        _cachedBaseUrl ??= await ResolveBaseUrlAsync();

        // 2. Create a fresh HttpClient per call — avoids DNS/connection pool issues
        var httpClient = HttpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_cachedBaseUrl);

        // 3. Configurar Kiota adapter
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

        // 4. Return type-safe Kiota client
        return CreateClient(adapter);
    }

    private async Task<string> ResolveBaseUrlAsync()
    {
        // Hierarchical fallback: K8s → Config → Default
        var discoveredUrl = await K8sDiscovery.DiscoverServiceUrlAsync(ApiType);
        return discoveredUrl
            ?? Configuration[ConfigurationKey]
            ?? DefaultUrl;
    }
}
