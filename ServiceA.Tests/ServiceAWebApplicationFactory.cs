using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceA.Tests.Fakes;
using Shared.Infrastructure;

namespace ServiceA.Tests;

/// <summary>
/// Shared factory for ServiceA integration tests.
///
/// What this factory does:
///   1. Replaces IKubernetesServiceDiscovery with a fake that returns null
///      (simulates "not running in a K8s cluster").
///   2. Replaces IHttpClientFactory with a fake that returns clients with
///      FakeServiceBMessageHandler, intercepting all HTTP calls to ServiceB.
///
/// This allows tests to run without Kubernetes, a real ServiceB, or any network.
/// </summary>
public class ServiceAWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace K8s discovery — returns null (no cluster)
            services.RemoveAll<IKubernetesServiceDiscovery>();
            services.AddSingleton<IKubernetesServiceDiscovery, FakeKubernetesServiceDiscovery>();

            // Replace IHttpClientFactory — intercepts calls to ServiceB
            var fakeHandler = new FakeServiceBMessageHandler();
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(fakeHandler));
        });
    }
}
