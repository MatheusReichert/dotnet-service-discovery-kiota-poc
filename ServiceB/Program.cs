using Scalar.AspNetCore;
using ServiceB.Infrastructure;
using ServiceB.Models;
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

// ServiceC Client Factory (Kiota + Descoberta Automática)
// Singleton: URL resolvida uma vez no startup, sem consulta ao K8s por request
builder.Services.AddSingleton<ServiceCClientFactory>();

// Configurar OpenAPI com metadados
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "ServiceB - Products API",
            Version = "1.0.0",
            Description = "API for managing products"
        };
        return Task.CompletedTask;
    });

    // .NET 10 gera "type": ["integer","string"] e "type": ["number","string"]
    // para int/double (OpenAPI 3.1 union). Kiota não consegue mapear union types
    // para tipos C# concretos e usa UntypedNode. Normaliza para tipo único.
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (schema.Format is "int32" or "int64")
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.Integer;
            schema.Pattern = null;
        }
        else if (schema.Format is "float" or "double")
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.Number;
            schema.Pattern = null;
        }
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/products", () =>
{
    return Results.Ok(new Product[]
    {
        new(1, "Laptop", 999.99),
        new(2, "Mouse", 29.99)
    });
})
.WithName("GetProducts")
.Produces<Product[]>(200);

app.MapGet("/api/products/{id}", (int id) =>
{
    return Results.Ok(new Product(id, $"Product {id}", 99.99));
})
.WithName("GetProductById")
.Produces<Product>(200);

app.MapPut("/api/products/{id}", (int id, object product) =>
{
    return Results.Ok(new Product(id, "Updated Product", 149.99));
})
.WithName("UpdateProduct")
.Produces<Product>(200);

app.MapGet("/api/products/with-orders/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    try
    {
        // 1. Tenta descoberta automática via K8s API (se rodando no cluster)
        var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("orders-api");

        // 2. Fallback para configuração manual (appsettings ou env vars)
        if (string.IsNullOrEmpty(serviceUrl))
        {
            serviceUrl = configuration["Services:ServiceC:Url"] ?? "http://servicec";
        }

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(serviceUrl);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ServiceB");

        var response = await httpClient.GetAsync("/api/orders");
        var content = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            Message = "ServiceB called ServiceC via service discovery",
            DiscoveredUrl = serviceUrl,
            Orders = content
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
    }
})
.WithName("GetProductWithOrders")
.Produces(200)
.Produces(400);

// ✅ NOVO: Endpoint usando Kiota + Descoberta Automática
app.MapGet("/api/products/with-orders-typed/{id}", async (int id, ServiceCClientFactory clientFactory) =>
{
    try
    {
        // Cria cliente Kiota com URL descoberta automaticamente
        var client = await clientFactory.CreateClientAsync();

        // Chamadas type-safe usando Kiota
        var orders = await client.Api.Orders.GetAsync();

        return Results.Ok(new
        {
            Message = "ServiceB → ServiceC usando Kiota + Descoberta Automática",
            Method = "Type-Safe Kiota Client",
            ProductId = id,
            Orders = orders,
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
.WithName("GetProductWithOrdersTyped")
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
