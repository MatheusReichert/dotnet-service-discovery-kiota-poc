# 🎯 Integration Guide: Kiota + Automatic Discovery

This POC demonstrates the integration of **two complementary strategies**:

1. **Automatic Discovery via Kubernetes Labels** - To find services dynamically
2. **Kiota Type-Safe Clients** - To make calls with compile-time validation

---

## 🏗️ Solution Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Shared Project                           │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  KubernetesServiceDiscovery                               │  │
│  │  - Queries K8s API                                        │  │
│  │  - Finds services by label (api-type)                     │  │
│  │  - Returns URLs automatically                             │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  KiotaClientFactoryBase<TClient>                          │  │
│  │  - Generic base class                                     │  │
│  │  - Combines discovery + Kiota                             │  │
│  │  - Reusable for all services                              │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↓ (referenced by)
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   ServiceA    │    │   ServiceB    │    │   ServiceC    │
│               │    │               │    │               │
│ ServiceBClient│    │ ServiceCClient│    │               │
│ Factory       │    │ Factory       │    │               │
│ (inherits base│    │ (inherits base│    │               │
└───────────────┘    └───────────────┘    └───────────────┘
```

---

## 🔧 Components

### 1. Shared Project (Reusable Code)

**Location:** `/Shared/`

**Classes:**

#### `IKubernetesServiceDiscovery` / `KubernetesServiceDiscovery`
- Queries Kubernetes API to discover services
- Searches by label `api-type`
- Returns URLs in the format: `http://{name}.{namespace}.svc.cluster.local`

**Example:**
```csharp
var url = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
// Returns: http://serviceb.products-ns.svc.cluster.local
```

#### `KiotaClientFactoryBase<TClient>`
- Abstract base class for creating factories
- Implements the pattern: **Discovery → Fallback → Kiota Client**
- Generic: works with any Kiota client

**Abstract properties:**
```csharp
protected abstract string ApiType { get; }           // e.g.: "products-api"
protected abstract string ConfigurationKey { get; }  // e.g.: "Services:ServiceB:Url"
protected abstract string DefaultUrl { get; }        // e.g.: "http://serviceb"
protected abstract TClient CreateClient(HttpClientRequestAdapter adapter);
```

---

### 2. ServiceA (Consumer of ServiceB)

**Specific Factory:**

```csharp
// ServiceA/Infrastructure/ServiceBClientFactory.cs
public class ServiceBClientFactory : KiotaClientFactoryBase<ApiClient>
{
    protected override string ApiType => "products-api";
    protected override string ConfigurationKey => "Services:ServiceB:Url";
    protected override string DefaultUrl => "http://serviceb";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
    {
        return new ApiClient(adapter); // Generated Kiota client
    }
}
```

**DI Registration:**

```csharp
// ServiceA/Program.cs
builder.Services.AddSingleton<IKubernetesServiceDiscovery, KubernetesServiceDiscovery>();
builder.Services.AddScoped<ServiceBClientFactory>();
```

**Usage in Endpoint:**

```csharp
app.MapGet("/api/users/with-products-typed/{id}", async (
    int id,
    ServiceBClientFactory clientFactory) =>
{
    // 1. Create client with automatic discovery
    var client = await clientFactory.CreateClientAsync();

    // 2. Type-safe calls
    var products = await client.Api.Products.GetAsync();

    return Results.Ok(new { UserId = id, Products = products });
});
```

---

### 3. ServiceB (Consumer of ServiceC)

**Specific Factory:**

```csharp
// ServiceB/Infrastructure/ServiceCClientFactory.cs
public class ServiceCClientFactory : KiotaClientFactoryBase<ApiClient>
{
    protected override string ApiType => "orders-api";
    protected override string ConfigurationKey => "Services:ServiceC:Url";
    protected override string DefaultUrl => "http://servicec";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
    {
        return new ApiClient(adapter);
    }
}
```

---

## 🎯 Complete Discovery + Kiota Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Request arrives at ServiceA                                  │
│    GET /api/users/with-products-typed/1                         │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ServiceBClientFactory.CreateClientAsync()                    │
│    ├─→ KubernetesServiceDiscovery.DiscoverServiceUrlAsync()    │
│    │   └─→ Query: labelSelector="api-type=products-api"        │
│    │   └─→ Returns: http://serviceb.products-ns.svc...         │
│    ├─→ If not found: uses Configuration or Default             │
│    ├─→ Creates HttpClient with BaseAddress = discovered URL    │
│    └─→ Returns configured ApiClient (Kiota)                    │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Type-Safe Call with Kiota                                    │
│    var products = await client.Api.Products.GetAsync();         │
│    ✅ Full IntelliSense                                         │
│    ✅ Compile-time validation                                   │
│    ✅ Automatically generated models                            │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. HttpClient makes request                                     │
│    GET http://serviceb.products-ns.svc.cluster.local/api/products│
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. ServiceB responds                                             │
│    [{"id":1,"name":"Laptop","price":999.99}]                    │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. Kiota deserializes into typed objects                        │
│    List<Product> products                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎓 Integration Benefits

### 1. Zero URL Hardcoding
```csharp
// ❌ BEFORE
var url = "http://serviceb.products-ns.svc.cluster.local"; // hardcoded!

// ✅ NOW
var client = await clientFactory.CreateClientAsync(); // discovered automatically
```

### 2. Compile-Time Type-Safety
```csharp
// ❌ Traditional HttpClient (runtime errors)
var response = await httpClient.GetStringAsync("/api/prodcts"); // typo! 💥
var json = JsonSerializer.Deserialize<Product>(response); // can break

// ✅ Kiota (compile-time errors)
var products = await client.Api.Products.GetAsync(); // IntelliSense ✅
// If API changes, code won't compile!
```

### 3. Transparent Namespace Changes
```yaml
# ServiceB moves from products-ns → v2-products-ns
# Code keeps working! Automatic discovery finds it by label
```

### 4. Safe Refactoring
```csharp
// If ServiceB renames endpoint /products → /items
// Kiota regenerates client
// Code in ServiceA won't compile until updated
// ✅ Detects problem BEFORE deploy
```

### 5. Code Reuse
```csharp
// Shared project used by all services
// KiotaClientFactoryBase avoids duplication
// Only 3 property overrides per service
```

---

## 📦 File Structure

```
dotnet-playground-test/
├── Shared/                                    # ← NEW!
│   ├── KubernetesServiceDiscovery.cs         # Automatic discovery
│   ├── KiotaClientFactoryBase.cs             # Base for factories
│   └── Shared.csproj
│
├── ServiceA/
│   ├── Generated/
│   │   └── ServiceBClient/                   # Generated Kiota client
│   ├── Infrastructure/
│   │   └── ServiceBClientFactory.cs          # Inherits base, 10 lines!
│   └── Program.cs                            # Uses factory
│
├── ServiceB/
│   ├── Generated/
│   │   └── ServiceCClient/                   # Generated Kiota client
│   ├── Infrastructure/
│   │   └── ServiceCClientFactory.cs          # Inherits base, 10 lines!
│   ├── openapi.json                          # API contract
│   └── Program.cs
│
└── ServiceC/
    ├── openapi.json                          # API contract
    └── Program.cs
```

---

## 🚀 How to Add a New Service

### Step 1: Create OpenAPI spec
```json
// ServiceD/openapi.json
{
  "openapi": "3.0.1",
  "info": { "title": "ServiceD API" },
  "paths": { "/api/inventory": { ... } }
}
```

### Step 2: Generate Kiota Client
```bash
cd ServiceC
kiota generate -l CSharp \
  -d ../ServiceD/openapi.json \
  -o ./Generated/ServiceDClient \
  -n ServiceC.Generated.ServiceDClient
```

### Step 3: Create Factory (10 lines!)
```csharp
// ServiceC/Infrastructure/ServiceDClientFactory.cs
public class ServiceDClientFactory : KiotaClientFactoryBase<ApiClient>
{
    public ServiceDClientFactory(
        IKubernetesServiceDiscovery k8sDiscovery,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        : base(k8sDiscovery, httpClientFactory, configuration) { }

    protected override string ApiType => "inventory-api";
    protected override string ConfigurationKey => "Services:ServiceD:Url";
    protected override string DefaultUrl => "http://serviced";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
        => new ApiClient(adapter);
}
```

### Step 4: Register in DI
```csharp
// ServiceC/Program.cs
builder.Services.AddScoped<ServiceDClientFactory>();
```

### Step 5: Use it!
```csharp
app.MapGet("/inventory", async (ServiceDClientFactory factory) =>
{
    var client = await factory.CreateClientAsync();
    return await client.Api.Inventory.GetAsync();
});
```

---

## 🧪 Testing the Integration

### Traditional Endpoint (HttpClient)
```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products/1
```

**Response:**
```json
{
  "message": "ServiceA called ServiceB via service discovery",
  "discoveredUrl": "http://serviceb.products-ns.svc.cluster.local",
  "products": "[...]"
}
```

### Type-Safe Endpoint (Kiota + Discovery)
```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products-typed/1
```

**Response:**
```json
{
  "message": "ServiceA → ServiceB using Kiota + Automatic Discovery",
  "method": "Type-Safe Kiota Client",
  "userId": 1,
  "products": [
    {"id": 1, "name": "Laptop", "price": 999.99},
    {"id": 2, "name": "Mouse", "price": 29.99}
  ],
  "benefits": [
    "✅ URL discovered automatically via K8s labels",
    "✅ Type-safe client generated by Kiota",
    "✅ Full IntelliSense",
    "✅ Compile-time validation"
  ]
}
```

---

## 📊 Comparison: Before vs Now

| Aspect | Before (manual HttpClient) | Now (Kiota + Discovery) |
|--------|----------------------------|-------------------------|
| **URLs** | Hardcoded in each service | Automatic discovery |
| **Type-Safety** | ❌ Runtime errors | ✅ Compile-time errors |
| **API Changes** | Breaks in production | Won't compile |
| **IntelliSense** | None | Full |
| **Boilerplate** | ~50 lines per consumer | ~10 lines (inherits base) |
| **Refactoring** | Risky | Safe |
| **Cross-Namespace** | Manual config | Automatic |

---

## 🎯 Conclusion

This POC demonstrates the **perfect combination** of two technologies:

1. **Automatic Discovery** - Eliminates hardcoding, transparent cross-namespace
2. **Kiota** - Type-safety, IntelliSense, compile-time validation

**Result:**
- ✅ Clean and reusable code (Shared project)
- ✅ Zero hardcoded URLs
- ✅ Errors detected at compile-time
- ✅ Easy to add new services (10 lines)
- ✅ Works in dev (Aspire) and prod (Kubernetes)

**Perfect match! 🚀**
