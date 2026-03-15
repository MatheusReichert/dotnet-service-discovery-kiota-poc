# 🔍 Automatic Discovery - How It Works

This POC implements automatic service discovery **without hardcoded URLs** using Labels + KubernetesClient.

## ❌ Problem: Hardcoded URLs (Traditional Approach)

```yaml
# ❌ Traditional deployment - fixed URLs
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"  # HARDCODED!
```

**Problems:**
- If ServiceB changes namespace, the deployment must be updated
- If the service is renamed, it breaks
- Makes it harder to move services between environments
- Does not work in multi-cluster

## ✅ Solution: Automatic Discovery via Labels

### How It Works

```
┌────────────────────────────────────────────────────────────────┐
│ 1. ServiceB announces its API via LABEL                        │
│                                                                 │
│    apiVersion: v1                                               │
│    kind: Service                                                │
│    metadata:                                                    │
│      name: serviceb                                             │
│      namespace: products-ns                                     │
│      labels:                                                    │
│        api-type: products-api  ← API IDENTIFIER                │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 2. ServiceA receives a request                                  │
│                                                                 │
│    GET /api/users/with-products/1                              │
│    │                                                            │
│    └─→ Needs to call ServiceB                                  │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 3. KubernetesServiceDiscovery queries the K8s API              │
│                                                                 │
│    var url = await k8sDiscovery                                 │
│        .DiscoverServiceUrlAsync("products-api");               │
│                                                                 │
│    Internally:                                                  │
│    - ListServiceForAllNamespacesAsync()                         │
│    - labelSelector: "api-type=products-api"                    │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 4. Kubernetes API returns                                       │
│                                                                 │
│    {                                                            │
│      "metadata": {                                              │
│        "name": "serviceb",                                      │
│        "namespace": "products-ns"                               │
│      }                                                          │
│    }                                                            │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 5. ServiceA builds the URL automatically                        │
│                                                                 │
│    url = "http://serviceb.products-ns.svc.cluster.local"       │
│                                                                 │
│    NO HARDCODE! ✅                                             │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 6. HttpClient uses the discovered URL                          │
│                                                                 │
│    httpClient.BaseAddress = new Uri(url);                      │
│    var response = await httpClient.GetAsync("/api/products");  │
└────────────────────────────────────────────────────────────────┘
```

## 🎯 Advantages

### 1. Zero Hardcode

```csharp
// ❌ BEFORE - Hardcoded
httpClient.BaseAddress = new Uri("http://serviceb.products-ns.svc.cluster.local");

// ✅ NOW - Automatic discovery
var url = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
httpClient.BaseAddress = new Uri(url);
```

### 2. Transparent Namespace Changes

```yaml
# ServiceB changes namespace: products-ns → v2-products-ns
# ServiceA keeps working! No code/config change needed
```

### 3. Service Renaming

```yaml
# Rename: serviceb → products-service
# ServiceA keeps working! Searches by label "api-type: products-api"
```

### 4. Multi-Environment

```yaml
# DEV
metadata:
  name: serviceb-dev
  namespace: products-dev
  labels:
    api-type: products-api

# PROD
metadata:
  name: serviceb
  namespace: products-prod
  labels:
    api-type: products-api  # ← Same label, ServiceA discovers automatically
```

## 🔐 Required RBAC

For pods to be able to query the Kubernetes API:

```yaml
# ServiceAccount
apiVersion: v1
kind: ServiceAccount
metadata:
  name: servicea-sa
  namespace: users-ns
---
# ClusterRole - permission to list services
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: service-discovery-reader
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
---
# ClusterRoleBinding - connects ServiceAccount to ClusterRole
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: servicea-discovery-binding
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: service-discovery-reader
subjects:
- kind: ServiceAccount
  name: servicea-sa
  namespace: users-ns
```

**Deploy:**
```bash
kubectl apply -f k8s/04-rbac.yaml
```

## 💻 C# Implementation

### Interface

```csharp
public interface IKubernetesServiceDiscovery
{
    Task<string?> DiscoverServiceUrlAsync(string apiType);
    Task<Dictionary<string, string>> GetAllServicesAsync();
}
```

### Implementation

```csharp
public class KubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    private readonly IKubernetes? _client;
    private readonly bool _isRunningInKubernetes;

    public KubernetesServiceDiscovery()
    {
        try
        {
            // Detects if running inside Kubernetes
            var config = KubernetesClientConfiguration.InClusterConfig();
            _client = new Kubernetes(config);
            _isRunningInKubernetes = true;
        }
        catch
        {
            // Not in K8s (local development/Aspire)
            _isRunningInKubernetes = false;
        }
    }

    public async Task<string?> DiscoverServiceUrlAsync(string apiType)
    {
        // If not in K8s, return null (use fallback)
        if (!_isRunningInKubernetes || _client == null)
            return null;

        // Search for services with specific label
        var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
            labelSelector: $"api-type={apiType}"
        );

        var service = services.Items.FirstOrDefault();
        if (service == null)
            return null;

        // Build URL automatically
        var ns = service.Metadata.NamespaceProperty;
        var name = service.Metadata.Name;

        return $"http://{name}.{ns}.svc.cluster.local";
    }
}
```

### Usage in Endpoints

```csharp
app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    // 1. PRIORITY: Automatic discovery (K8s)
    var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");

    // 2. FALLBACK: Manual configuration (dev/Aspire)
    if (string.IsNullOrEmpty(serviceUrl))
    {
        serviceUrl = configuration["Services:ServiceB:Url"] ?? "http://serviceb";
    }

    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(serviceUrl);
    var response = await httpClient.GetStringAsync("/api/products");

    return Results.Ok(new { DiscoveredUrl = serviceUrl, Products = response });
});
```

## 🧪 Testing

### 1. Deploy to Kubernetes

```bash
# RBAC
kubectl apply -f k8s/04-rbac.yaml

# Services (with labels)
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

### 2. Verify Labels

```bash
# List services with labels
kubectl get svc -A --show-labels | grep api-type

# Should show:
# users-ns      servicea   ...   api-type=users-api
# products-ns   serviceb   ...   api-type=products-api
# orders-ns     servicec   ...   api-type=orders-api
```

### 3. Test Discovery

```bash
# Port-forward ServiceA
kubectl port-forward -n users-ns svc/servicea 8080:80

# View automatically discovered service catalog
curl http://localhost:8080/api/services/catalog

# Response:
# {
#   "DiscoveryMethod": "Kubernetes API",
#   "Services": {
#     "users-api": "http://servicea.users-ns.svc.cluster.local",
#     "products-api": "http://serviceb.products-ns.svc.cluster.local",
#     "orders-api": "http://servicec.orders-ns.svc.cluster.local"
#   }
# }
```

### 4. Test the Call Chain

```bash
# ServiceA → ServiceB (automatic discovery) → ServiceC (automatic discovery)
curl http://localhost:8080/api/users/with-products/1
```

**Expected logs:**
```
[ServiceA] Discovering services via Kubernetes API...
[ServiceA] Found products-api at: http://serviceb.products-ns.svc.cluster.local
[ServiceA] Calling ServiceB at discovered URL
[ServiceB] Discovering services via Kubernetes API...
[ServiceB] Found orders-api at: http://servicec.orders-ns.svc.cluster.local
[ServiceB] Calling ServiceC at discovered URL
```

## 📊 Comparison

| Aspect | Hardcoded URLs | Automatic Discovery |
|--------|----------------|---------------------|
| **URLs in deployment** | ✅ Yes (env vars) | ❌ No |
| **Namespace change** | ❌ Needs redeploy | ✅ Automatic |
| **Rename service** | ❌ Needs redeploy | ✅ Automatic |
| **Multi-environment** | ❌ Config per environment | ✅ Single label |
| **Multi-cluster** | ❌ Difficult | ✅ Possible (Federation) |
| **Coupling** | 🔴 High | 🟢 Low |
| **Maintenance** | 🔴 Manual | 🟢 Automatic |

## 🎓 Key Concepts

### 1. Labels as Contract
- Labels define the "type" of API (`api-type: products-api`)
- Services are discovered by type, not by name

### 2. Kubernetes API as Registry
- Pods query the K8s API to discover other services
- RBAC controls who can query

### 3. Fallback for Local Dev
- In K8s: uses automatic discovery
- Locally (Aspire): uses manual configuration

### 4. Zero Trust
- RBAC ensures only authorized pods can discover services
- Network Policies complement this (who can access)

## 📚 Implementation Files

```
dotnet-playground-test/
├── ServiceA/
│   └── Infrastructure/
│       └── KubernetesServiceDiscovery.cs  ← Implementation
├── ServiceB/
│   └── Infrastructure/
│       └── KubernetesServiceDiscovery.cs  ← Implementation
├── k8s/
│   ├── 01-servicea-deployment.yaml        ← serviceAccountName + labels
│   ├── 02-serviceb-deployment.yaml        ← serviceAccountName + labels
│   ├── 03-servicec-deployment.yaml        ← serviceAccountName + labels
│   ├── 04-rbac.yaml                       ← ServiceAccounts + RBAC
│   └── SERVICE-DISCOVERY.md               ← Detailed documentation
└── AUTOMATIC-DISCOVERY.md                 ← This file
```

## 🚀 Next Steps

To expand:

- [ ] Discovery cache (avoid querying K8s every time)
- [ ] Automatic refresh (Watch API for updates)
- [ ] Load balancing across multiple instances
- [ ] Cross-cluster discovery (Kubernetes Federation)
- [ ] Discovery metrics (query count, latency)
- [ ] Fallback for specific versions (`api-version` label)
