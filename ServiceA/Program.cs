using Scalar.AspNetCore;
using ServiceA.Infrastructure;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
});

// Kubernetes Service Discovery (automático via labels)
builder.Services.AddSingleton<IKubernetesServiceDiscovery, KubernetesServiceDiscovery>();

// ServiceB Client Factory (Kiota + Descoberta Automática)
builder.Services.AddScoped<ServiceBClientFactory>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/users", () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, Name = "Alice" },
        new { Id = 2, Name = "Bob" }
    });
})
.WithName("GetUsers")
.Produces(200);

app.MapGet("/api/users/{id}", (int id) =>
{
    return Results.Ok(new { Id = id, Name = $"User {id}" });
})
.WithName("GetUserById")
.Produces(200);

app.MapPost("/api/users", (object user) =>
{
    return Results.Created($"/api/users/3", new { Id = 3, Name = "New User" });
})
.WithName("CreateUser")
.Produces(201);

app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    try
    {
        // 1. Tenta descoberta automática via K8s API (se rodando no cluster)
        var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");

        // 2. Fallback para configuração manual (appsettings ou env vars)
        if (string.IsNullOrEmpty(serviceUrl))
        {
            serviceUrl = configuration["Services:ServiceB:Url"] ?? "http://serviceb";
        }

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(serviceUrl);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ServiceA");

        var response = await httpClient.GetAsync("/api/products");
        var content = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            Message = "ServiceA called ServiceB via service discovery",
            DiscoveredUrl = serviceUrl,
            Products = content
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
    }
})
.WithName("GetUserWithProducts")
.Produces(200)
.Produces(400);

// ✅ NOVO: Endpoint usando Kiota + Descoberta Automática
app.MapGet("/api/users/with-products-typed/{id}", async (int id, ServiceBClientFactory clientFactory) =>
{
    try
    {
        // Cria cliente Kiota com URL descoberta automaticamente
        var client = await clientFactory.CreateClientAsync();

        // Chamadas type-safe usando Kiota
        var products = await client.Api.Products.GetAsync();

        return Results.Ok(new
        {
            Message = "ServiceA → ServiceB usando Kiota + Descoberta Automática",
            Method = "Type-Safe Kiota Client",
            UserId = id,
            Products = products,
            Benefits = new[]
            {
                "✅ URL descoberta automaticamente via K8s labels",
                "✅ Cliente type-safe gerado por Kiota",
                "✅ IntelliSense completo",
                "✅ Compile-time validation"
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetUserWithProductsTyped")
.Produces(200)
.Produces(400);

// Endpoint de diagnóstico - lista todos os serviços descobertos
app.MapGet("/api/services/catalog", async (IKubernetesServiceDiscovery k8sDiscovery) =>
{
    var catalog = await k8sDiscovery.DiscoverAllServicesAsync();
    return Results.Ok(new
    {
        Message = "Service catalog from Kubernetes API",
        Services = catalog,
        Count = catalog.Count
    });
})
.WithName("GetServiceCatalog")
.Produces(200);

app.Run();
