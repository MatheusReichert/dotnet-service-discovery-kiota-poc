# Automatic Service Discovery - Kubernetes

This POC implements **automatic service discovery** using Labels + KubernetesClient.

## 🎯 How It Works

### 1. Services Announce Their APIs via Labels

Each service has an `api-type` label that identifies its API:

```yaml
# k8s/02-serviceb-deployment.yaml
apiVersion: v1
kind: Service
metadata:
  name: serviceb
  namespace: products-ns
  labels:
    app: serviceb
    api-type: products-api      # ← API identifier
    api-version: v1
  annotations:
    api.company.com/description: "Products API"
    api.company.com/owner: "products-team"
```

**Available labels:**
- `api-type: users-api` - ServiceA (users-ns)
- `api-type: products-api` - ServiceB (products-ns)
- `api-type: orders-api` - ServiceC (orders-ns)

### 2. Consumers Discover Automatically

Services use `KubernetesServiceDiscovery` to query the Kubernetes API:

```csharp
// ServiceA/Infrastructure/KubernetesServiceDiscovery.cs
public async Task<string?> DiscoverServiceUrlAsync(string apiType)
{
    // Searches for services with a specific label
    var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
        labelSelector: $"api-type={apiType}"
    );

    var service = services.Items.FirstOrDefault();
    if (service == null) return null;

    // Builds URL automatically
    var ns = service.Metadata.NamespaceProperty;
    var name = service.Metadata.Name;
    return $"http://{name}.{ns}.svc.cluster.local";
}
```

### 3. Automatic Fallback

The code works in both **development (Aspire)** and **production (Kubernetes)**:

```csharp
// ServiceA/Program.cs
app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    // 1. Attempt automatic discovery (K8s)
    var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");

    // 2. Fallback to manual configuration (dev/Aspire)
    if (string.IsNullOrEmpty(serviceUrl))
    {
        serviceUrl = configuration["Services:ServiceB:Url"] ?? "http://serviceb";
    }

    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(serviceUrl);
    var response = await httpClient.GetStringAsync("/api/products");

    return Results.Ok(new { ServiceUrl = serviceUrl, Products = response });
});
```

## 🔐 RBAC Permissions

For pods to query the Kubernetes API, RBAC must be configured:

### ServiceAccount

Each service has a ServiceAccount:

```yaml
# k8s/04-rbac.yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: servicea-sa
  namespace: users-ns
```

### ClusterRole

Allows listing services across all namespaces:

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: service-discovery-reader
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
```

### ClusterRoleBinding

Binds the ServiceAccount to the ClusterRole:

```yaml
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

## 📦 Deploy

### 1. Deploy RBAC

```bash
kubectl apply -f k8s/04-rbac.yaml
```

Verify:
```bash
kubectl get serviceaccount -A | grep service
kubectl get clusterrole service-discovery-reader
kubectl get clusterrolebinding | grep discovery
```

### 2. Deploy Services

```bash
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

The deployments are already configured with `serviceAccountName`:

```yaml
spec:
  template:
    spec:
      serviceAccountName: servicea-sa  # ← Uses ServiceAccount with permissions
```

### 3. Verify

```bash
# Check pods
kubectl get pods -A | grep service

# Check services with labels
kubectl get svc -A --show-labels | grep api-type

# Test discovery (from inside the pod)
kubectl exec -it -n users-ns <servicea-pod> -- curl localhost:8080/api/users/with-products/1
```

## 🧪 Testing Discovery

### Catalog Endpoint

ServiceA and ServiceB expose an endpoint that lists all discovered services:

```bash
# Port-forward ServiceA
kubectl port-forward -n users-ns svc/servicea 8080:80

# View catalog of discovered services
curl http://localhost:8080/api/services/catalog
```

**Expected response:**
```json
{
  "DiscoveryMethod": "Kubernetes API",
  "Services": {
    "users-api": "http://servicea.users-ns.svc.cluster.local",
    "products-api": "http://serviceb.products-ns.svc.cluster.local",
    "orders-api": "http://servicec.orders-ns.svc.cluster.local"
  }
}
```

### Testing the Full Chain

```bash
# ServiceA → ServiceB → ServiceC
curl http://localhost:8080/api/users/with-products/1
```

In the logs, you will see:
```
[ServiceA] Discovered ServiceB at: http://serviceb.products-ns.svc.cluster.local
[ServiceB] Discovered ServiceC at: http://servicec.orders-ns.svc.cluster.local
```

## 🔍 Troubleshooting

### Error: "Service with api-type=products-api not found"

**Cause:** RBAC not configured or ServiceB is missing the label.

**Solution:**
```bash
# Check if ServiceB has the label
kubectl get svc -n products-ns serviceb -o yaml | grep api-type

# Check RBAC
kubectl auth can-i list services --all-namespaces --as=system:serviceaccount:users-ns:servicea-sa
```

### Error: "Forbidden: services is forbidden"

**Cause:** ServiceAccount has no permissions.

**Solution:**
```bash
# Re-apply RBAC
kubectl apply -f k8s/04-rbac.yaml

# Check bindings
kubectl describe clusterrolebinding servicea-discovery-binding
```

### Discovery returns `null` in Kubernetes

**Cause:** Pod is not using the correct ServiceAccount.

**Solution:**
```bash
# Check pod's ServiceAccount
kubectl get pod -n users-ns <pod-name> -o jsonpath='{.spec.serviceAccountName}'

# Should return: servicea-sa
```

## 🎯 Advantages of This Approach

| Aspect | Without Discovery | With Automatic Discovery |
|--------|-------------------|--------------------------|
| **Hardcode** | Fixed URLs in code | Dynamic labels |
| **Cross-Namespace** | Manual config per namespace | Automatic |
| **Changes** | Redeploy with new URLs | Transparent |
| **Development** | Different dev/prod URLs | Automatic fallback |
| **Multi-Cluster** | Impossible | Works with Federation |

## 📊 Full Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. ServiceB deployed with label "api-type: products-api"       │
│    kubectl apply -f k8s/02-serviceb-deployment.yaml             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ServiceA pod starts                                           │
│    - Injects serviceAccountName: servicea-sa                    │
│    - ServiceAccount has ClusterRole to list services           │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Request arrives: GET /api/users/with-products/1              │
│    - KubernetesServiceDiscovery.DiscoverServiceUrlAsync()       │
│    - Query: labelSelector="api-type=products-api"               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Kubernetes API returns:                                       │
│    - Service: serviceb                                           │
│    - Namespace: products-ns                                      │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. ServiceA builds URL:                                          │
│    http://serviceb.products-ns.svc.cluster.local                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. HttpClient makes request to ServiceB                         │
│    Response: { "products": [...] }                              │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Next Steps

To expand this POC:

- [ ] Discovery cache (avoid querying K8s API every time)
- [ ] Health checks before returning URL
- [ ] Fallback for multiple instances (load balancing)
- [ ] Discovery of specific versions (api-version label)
- [ ] Discovery metrics (Prometheus)
- [ ] Watch API for real-time updates

## 📚 References

- [KubernetesClient Documentation](https://github.com/kubernetes-client/csharp)
- [Kubernetes Service Discovery](https://kubernetes.io/docs/concepts/services-networking/service/)
- [RBAC Authorization](https://kubernetes.io/docs/reference/access-authn-authz/rbac/)
- [Label Selectors](https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/)
