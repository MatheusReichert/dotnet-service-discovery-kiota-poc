using Microsoft.Kiota.Http.HttpClientLibrary;
using ServiceB.Generated.ServiceCClient;
using Shared.Infrastructure;

namespace ServiceB.Infrastructure;

/// <summary>
/// Factory for creating a Kiota client for ServiceC with automatic discovery
/// </summary>
public class ServiceCClientFactory : KiotaClientFactoryBase<ApiClient>
{
    public ServiceCClientFactory(
        IKubernetesServiceDiscovery k8sDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        : base(k8sDiscovery, httpClientFactory, configuration)
    {
    }

    protected override string ApiType => "orders-api";
    protected override string ConfigurationKey => "Services:ServiceC:Url";
    protected override string DefaultUrl => "http://servicec";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
    {
        return new ApiClient(adapter);
    }
}
