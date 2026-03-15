using Microsoft.Kiota.Http.HttpClientLibrary;
using ServiceA.Generated.ServiceBClient;
using Shared.Infrastructure;

namespace ServiceA.Infrastructure;

/// <summary>
/// Factory for creating a Kiota client for ServiceB with automatic discovery
/// </summary>
public class ServiceBClientFactory : KiotaClientFactoryBase<ApiClient>
{
    public ServiceBClientFactory(
        IKubernetesServiceDiscovery k8sDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        : base(k8sDiscovery, httpClientFactory, configuration)
    {
    }

    protected override string ApiType => "products-api";
    protected override string ConfigurationKey => "Services:ServiceB:Url";
    protected override string DefaultUrl => "http://serviceb";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
    {
        return new ApiClient(adapter);
    }
}
