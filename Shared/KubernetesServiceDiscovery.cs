using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure;

public interface IKubernetesServiceDiscovery
{
    Task<string?> DiscoverServiceUrlAsync(string apiType);
    Task<Dictionary<string, string>> DiscoverAllServicesAsync();
}

public class KubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    private readonly IKubernetes? _client;
    private readonly ILogger<KubernetesServiceDiscovery> _logger;
    private readonly bool _isRunningInKubernetes;

    public KubernetesServiceDiscovery(ILogger<KubernetesServiceDiscovery> logger)
    {
        _logger = logger;

        try
        {
            // Tenta configurar para in-cluster (quando rodando no K8s)
            var config = KubernetesClientConfiguration.InClusterConfig();
            _client = new Kubernetes(config);
            _isRunningInKubernetes = true;
            _logger.LogInformation("Kubernetes Service Discovery initialized (in-cluster mode)");
        }
        catch (Exception ex)
        {
            // Se falhar, estamos rodando localmente (Aspire)
            _logger.LogWarning(ex, "Not running in Kubernetes cluster. Service Discovery disabled.");
            _isRunningInKubernetes = false;
        }
    }

    public async Task<string?> DiscoverServiceUrlAsync(string apiType)
    {
        if (!_isRunningInKubernetes || _client == null)
        {
            _logger.LogDebug("Skipping K8s discovery for '{ApiType}' - not in cluster", apiType);
            return null; // Fallback para Aspire ou configuração manual
        }

        try
        {
            _logger.LogInformation("Discovering service with api-type={ApiType}", apiType);

            // Buscar Services com label específico em todos os namespaces
            var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
                labelSelector: $"api-type={apiType}"
            );

            var service = services.Items.FirstOrDefault();
            if (service == null)
            {
                _logger.LogWarning("No service found with api-type={ApiType}", apiType);
                return null;
            }

            var ns = service.Metadata.NamespaceProperty;
            var name = service.Metadata.Name;
            var url = $"http://{name}.{ns}.svc.cluster.local";

            _logger.LogInformation(
                "Discovered service: {ApiType} -> {Url} (namespace: {Namespace})",
                apiType, url, ns
            );

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering service with api-type={ApiType}", apiType);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> DiscoverAllServicesAsync()
    {
        var result = new Dictionary<string, string>();

        if (!_isRunningInKubernetes || _client == null)
        {
            return result;
        }

        try
        {
            // Buscar todos os Services com label de descoberta
            var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
                labelSelector: "api-type"
            );

            foreach (var service in services.Items)
            {
                var apiType = service.Metadata.Labels?.ContainsKey("api-type") == true
                    ? service.Metadata.Labels["api-type"]
                    : null;
                if (!string.IsNullOrEmpty(apiType))
                {
                    var ns = service.Metadata.NamespaceProperty;
                    var name = service.Metadata.Name;
                    var url = $"http://{name}.{ns}.svc.cluster.local";

                    result[apiType] = url;

                    _logger.LogInformation(
                        "Cataloged service: {ApiType} -> {Url}",
                        apiType, url
                    );
                }
            }

            _logger.LogInformation("Service catalog built with {Count} services", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building service catalog");
        }

        return result;
    }
}
