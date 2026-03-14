# Automatic Service Discovery - Kubernetes

Esta POC implementa **descoberta automática de serviços** usando Labels + KubernetesClient.

## 🎯 Como Funciona

### 1. Serviços Anunciam suas APIs via Labels

Cada serviço possui um label `api-type` que identifica sua API:

```yaml
# k8s/02-serviceb-deployment.yaml
apiVersion: v1
kind: Service
metadata:
  name: serviceb
  namespace: products-ns
  labels:
    app: serviceb
    api-type: products-api      # ← Identificador da API
    api-version: v1
  annotations:
    api.company.com/description: "Products API"
    api.company.com/owner: "products-team"
```

**Labels disponíveis:**
- `api-type: users-api` - ServiceA (users-ns)
- `api-type: products-api` - ServiceB (products-ns)
- `api-type: orders-api` - ServiceC (orders-ns)

### 2. Consumidores Descobrem Automaticamente

Serviços usam `KubernetesServiceDiscovery` para consultar a API do Kubernetes:

```csharp
// ServiceA/Infrastructure/KubernetesServiceDiscovery.cs
public async Task<string?> DiscoverServiceUrlAsync(string apiType)
{
    // Busca serviços com label específico
    var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
        labelSelector: $"api-type={apiType}"
    );
    
    var service = services.Items.FirstOrDefault();
    if (service == null) return null;
    
    // Constrói URL automaticamente
    var ns = service.Metadata.NamespaceProperty;
    var name = service.Metadata.Name;
    return $"http://{name}.{ns}.svc.cluster.local";
}
```

### 3. Fallback Automático

O código funciona em **desenvolvimento (Aspire)** e **produção (Kubernetes)**:

```csharp
// ServiceA/Program.cs
app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    // 1. Tenta descoberta automática (K8s)
    var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
    
    // 2. Fallback para configuração manual (dev/Aspire)
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

Para que os pods possam consultar a API do Kubernetes, é necessário configurar RBAC:

### ServiceAccount

Cada serviço possui um ServiceAccount:

```yaml
# k8s/04-rbac.yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: servicea-sa
  namespace: users-ns
```

### ClusterRole

Permite listar serviços em todos os namespaces:

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

Liga o ServiceAccount ao ClusterRole:

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

Verifica:
```bash
kubectl get serviceaccount -A | grep service
kubectl get clusterrole service-discovery-reader
kubectl get clusterrolebinding | grep discovery
```

### 2. Deploy Serviços

```bash
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

Os deployments já estão configurados com `serviceAccountName`:

```yaml
spec:
  template:
    spec:
      serviceAccountName: servicea-sa  # ← Usa ServiceAccount com permissões
```

### 3. Verificar

```bash
# Verificar pods
kubectl get pods -A | grep service

# Verificar services com labels
kubectl get svc -A --show-labels | grep api-type

# Testar descoberta (de dentro do pod)
kubectl exec -it -n users-ns <servicea-pod> -- curl localhost:8080/api/users/with-products/1
```

## 🧪 Testando a Descoberta

### Endpoint de Catálogo

ServiceA e ServiceB expõem um endpoint que lista todos os serviços descobertos:

```bash
# Port-forward ServiceA
kubectl port-forward -n users-ns svc/servicea 8080:80

# Ver catálogo de serviços descobertos
curl http://localhost:8080/api/services/catalog
```

**Resposta esperada:**
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

### Testando Cadeia Completa

```bash
# ServiceA → ServiceB → ServiceC
curl http://localhost:8080/api/users/with-products/1
```

Nos logs, você verá:
```
[ServiceA] Discovered ServiceB at: http://serviceb.products-ns.svc.cluster.local
[ServiceB] Discovered ServiceC at: http://servicec.orders-ns.svc.cluster.local
```

## 🔍 Troubleshooting

### Erro: "Service with api-type=products-api not found"

**Causa:** RBAC não configurado ou ServiceB não tem label.

**Solução:**
```bash
# Verificar se ServiceB tem o label
kubectl get svc -n products-ns serviceb -o yaml | grep api-type

# Verificar RBAC
kubectl auth can-i list services --all-namespaces --as=system:serviceaccount:users-ns:servicea-sa
```

### Erro: "Forbidden: services is forbidden"

**Causa:** ServiceAccount sem permissões.

**Solução:**
```bash
# Reaplicar RBAC
kubectl apply -f k8s/04-rbac.yaml

# Verificar bindings
kubectl describe clusterrolebinding servicea-discovery-binding
```

### Descoberta retorna `null` no Kubernetes

**Causa:** Pod não está usando o ServiceAccount correto.

**Solução:**
```bash
# Verificar ServiceAccount do pod
kubectl get pod -n users-ns <pod-name> -o jsonpath='{.spec.serviceAccountName}'

# Deve retornar: servicea-sa
```

## 🎯 Vantagens desta Abordagem

| Aspecto | Sem Descoberta | Com Descoberta Automática |
|---------|----------------|---------------------------|
| **Hardcode** | URLs fixas no código | Labels dinâmicos |
| **Cross-Namespace** | Config manual por namespace | Automático |
| **Mudanças** | Redeployar com novas URLs | Transparente |
| **Desenvolvimento** | URLs diferentes dev/prod | Fallback automático |
| **Multi-Cluster** | Impossível | Funciona com Federation |

## 📊 Fluxo Completo

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. ServiceB deploy com label "api-type: products-api"          │
│    kubectl apply -f k8s/02-serviceb-deployment.yaml             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ServiceA pod inicia                                           │
│    - Injeta serviceAccountName: servicea-sa                      │
│    - ServiceAccount tem ClusterRole para listar services        │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Request chega: GET /api/users/with-products/1                │
│    - KubernetesServiceDiscovery.DiscoverServiceUrlAsync()       │
│    - Consulta: labelSelector="api-type=products-api"            │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Kubernetes API retorna:                                       │
│    - Service: serviceb                                           │
│    - Namespace: products-ns                                      │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. ServiceA constrói URL:                                        │
│    http://serviceb.products-ns.svc.cluster.local                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. HttpClient faz request para ServiceB                         │
│    Response: { "products": [...] }                              │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Próximos Passos

Para expandir esta POC:

- [ ] Cache de descoberta (evitar consultar K8s API toda hora)
- [ ] Health checks antes de retornar URL
- [ ] Fallback para múltiplas instâncias (load balancing)
- [ ] Descoberta de versões específicas (api-version label)
- [ ] Métricas de descoberta (Prometheus)
- [ ] Watch API para updates em tempo real

## 📚 Referências

- [KubernetesClient Documentation](https://github.com/kubernetes-client/csharp)
- [Kubernetes Service Discovery](https://kubernetes.io/docs/concepts/services-networking/service/)
- [RBAC Authorization](https://kubernetes.io/docs/reference/access-authn-authz/rbac/)
- [Label Selectors](https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/)
