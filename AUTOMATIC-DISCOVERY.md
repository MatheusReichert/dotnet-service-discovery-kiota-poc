# 🔍 Descoberta Automática - Como Funciona

Esta POC implementa descoberta automática de serviços **sem URLs hardcoded** usando Labels + KubernetesClient.

## ❌ Problema: URLs Hardcoded (Abordagem Tradicional)

```yaml
# ❌ Deployment tradicional - URLs fixas
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"  # HARDCODED!
```

**Problemas:**
- Se ServiceB mudar de namespace, precisa atualizar deployment
- Se renomear o serviço, quebra
- Dificulta mover serviços entre ambientes
- Não funciona em multi-cluster

## ✅ Solução: Descoberta Automática via Labels

### Como Funciona

```
┌────────────────────────────────────────────────────────────────┐
│ 1. ServiceB anuncia sua API via LABEL                          │
│                                                                 │
│    apiVersion: v1                                               │
│    kind: Service                                                │
│    metadata:                                                    │
│      name: serviceb                                             │
│      namespace: products-ns                                     │
│      labels:                                                    │
│        api-type: products-api  ← IDENTIFICADOR DA API          │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 2. ServiceA recebe request                                      │
│                                                                 │
│    GET /api/users/with-products/1                              │
│    │                                                            │
│    └─→ Precisa chamar ServiceB                                 │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 3. KubernetesServiceDiscovery consulta K8s API                 │
│                                                                 │
│    var url = await k8sDiscovery                                 │
│        .DiscoverServiceUrlAsync("products-api");               │
│                                                                 │
│    Internamente:                                                │
│    - ListServiceForAllNamespacesAsync()                         │
│    - labelSelector: "api-type=products-api"                    │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 4. Kubernetes API retorna                                       │
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
│ 5. ServiceA constrói URL automaticamente                        │
│                                                                 │
│    url = "http://serviceb.products-ns.svc.cluster.local"       │
│                                                                 │
│    SEM HARDCODE! ✅                                            │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│ 6. HttpClient usa URL descoberta                               │
│                                                                 │
│    httpClient.BaseAddress = new Uri(url);                      │
│    var response = await httpClient.GetAsync("/api/products");  │
└────────────────────────────────────────────────────────────────┘
```

## 🎯 Vantagens

### 1. Zero Hardcode

```csharp
// ❌ ANTES - Hardcoded
httpClient.BaseAddress = new Uri("http://serviceb.products-ns.svc.cluster.local");

// ✅ AGORA - Descoberta automática
var url = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
httpClient.BaseAddress = new Uri(url);
```

### 2. Mudanças de Namespace Transparentes

```yaml
# ServiceB muda de namespace: products-ns → v2-products-ns
# ServiceA continua funcionando! Não precisa mudança de código/config
```

### 3. Renomeação de Serviços

```yaml
# Renomeia: serviceb → products-service
# ServiceA continua funcionando! Busca pela label "api-type: products-api"
```

### 4. Multi-Ambiente

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
    api-type: products-api  # ← Mesmo label, ServiceA descobre automaticamente
```

## 🔐 RBAC Necessário

Para que os pods possam consultar a API do Kubernetes:

```yaml
# ServiceAccount
apiVersion: v1
kind: ServiceAccount
metadata:
  name: servicea-sa
  namespace: users-ns
---
# ClusterRole - permissão para listar services
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: service-discovery-reader
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
---
# ClusterRoleBinding - conecta ServiceAccount ao ClusterRole
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

## 💻 Implementação C#

### Interface

```csharp
public interface IKubernetesServiceDiscovery
{
    Task<string?> DiscoverServiceUrlAsync(string apiType);
    Task<Dictionary<string, string>> GetAllServicesAsync();
}
```

### Implementação

```csharp
public class KubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    private readonly IKubernetes? _client;
    private readonly bool _isRunningInKubernetes;
    
    public KubernetesServiceDiscovery()
    {
        try
        {
            // Detecta se está rodando dentro do Kubernetes
            var config = KubernetesClientConfiguration.InClusterConfig();
            _client = new Kubernetes(config);
            _isRunningInKubernetes = true;
        }
        catch
        {
            // Não está no K8s (desenvolvimento local/Aspire)
            _isRunningInKubernetes = false;
        }
    }
    
    public async Task<string?> DiscoverServiceUrlAsync(string apiType)
    {
        // Se não está no K8s, retorna null (usa fallback)
        if (!_isRunningInKubernetes || _client == null)
            return null;
        
        // Busca services com label específico
        var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
            labelSelector: $"api-type={apiType}"
        );
        
        var service = services.Items.FirstOrDefault();
        if (service == null) 
            return null;
        
        // Constrói URL automaticamente
        var ns = service.Metadata.NamespaceProperty;
        var name = service.Metadata.Name;
        
        return $"http://{name}.{ns}.svc.cluster.local";
    }
}
```

### Uso nos Endpoints

```csharp
app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory,
    IKubernetesServiceDiscovery k8sDiscovery,
    IConfiguration configuration) =>
{
    // 1. PRIORIDADE: Descoberta automática (K8s)
    var serviceUrl = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
    
    // 2. FALLBACK: Configuração manual (dev/Aspire)
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

## 🧪 Testando

### 1. Deploy no Kubernetes

```bash
# RBAC
kubectl apply -f k8s/04-rbac.yaml

# Services (com labels)
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

### 2. Verificar Labels

```bash
# Listar services com labels
kubectl get svc -A --show-labels | grep api-type

# Deve mostrar:
# users-ns      servicea   ...   api-type=users-api
# products-ns   serviceb   ...   api-type=products-api
# orders-ns     servicec   ...   api-type=orders-api
```

### 3. Testar Descoberta

```bash
# Port-forward ServiceA
kubectl port-forward -n users-ns svc/servicea 8080:80

# Ver catálogo de serviços descobertos automaticamente
curl http://localhost:8080/api/services/catalog

# Resposta:
# {
#   "DiscoveryMethod": "Kubernetes API",
#   "Services": {
#     "users-api": "http://servicea.users-ns.svc.cluster.local",
#     "products-api": "http://serviceb.products-ns.svc.cluster.local",
#     "orders-api": "http://servicec.orders-ns.svc.cluster.local"
#   }
# }
```

### 4. Testar Cadeia de Chamadas

```bash
# ServiceA → ServiceB (descoberta automática) → ServiceC (descoberta automática)
curl http://localhost:8080/api/users/with-products/1
```

**Logs esperados:**
```
[ServiceA] Discovering services via Kubernetes API...
[ServiceA] Found products-api at: http://serviceb.products-ns.svc.cluster.local
[ServiceA] Calling ServiceB at discovered URL
[ServiceB] Discovering services via Kubernetes API...
[ServiceB] Found orders-api at: http://servicec.orders-ns.svc.cluster.local
[ServiceB] Calling ServiceC at discovered URL
```

## 📊 Comparação

| Aspecto | Hardcoded URLs | Descoberta Automática |
|---------|----------------|----------------------|
| **URLs no deployment** | ✅ Sim (env vars) | ❌ Não |
| **Mudança de namespace** | ❌ Precisa redeployar | ✅ Automático |
| **Renomear serviço** | ❌ Precisa redeployar | ✅ Automático |
| **Multi-ambiente** | ❌ Config por ambiente | ✅ Único label |
| **Multi-cluster** | ❌ Difícil | ✅ Possível (Federation) |
| **Acoplamento** | 🔴 Alto | 🟢 Baixo |
| **Manutenção** | 🔴 Manual | 🟢 Automática |

## 🎓 Conceitos-Chave

### 1. Labels como Contrato
- Labels definem o "tipo" de API (`api-type: products-api`)
- Serviços são descobertos pelo tipo, não pelo nome

### 2. Kubernetes API como Registry
- Pods consultam K8s API para descobrir outros serviços
- RBAC controla quem pode consultar

### 3. Fallback para Dev Local
- Em K8s: usa descoberta automática
- Localmente (Aspire): usa configuração manual

### 4. Zero Trust
- RBAC garante que apenas pods autorizados podem descobrir serviços
- Network Policies complementam (quem pode acessar)

## 📚 Arquivos da Implementação

```
dotnet-playground-test/
├── ServiceA/
│   └── Infrastructure/
│       └── KubernetesServiceDiscovery.cs  ← Implementação
├── ServiceB/
│   └── Infrastructure/
│       └── KubernetesServiceDiscovery.cs  ← Implementação
├── k8s/
│   ├── 01-servicea-deployment.yaml        ← serviceAccountName + labels
│   ├── 02-serviceb-deployment.yaml        ← serviceAccountName + labels
│   ├── 03-servicec-deployment.yaml        ← serviceAccountName + labels
│   ├── 04-rbac.yaml                       ← ServiceAccounts + RBAC
│   └── SERVICE-DISCOVERY.md               ← Documentação detalhada
└── AUTOMATIC-DISCOVERY.md                 ← Este arquivo
```

## 🚀 Próximos Passos

Para expandir:

- [ ] Cache de descoberta (evitar consultar K8s toda hora)
- [ ] Refresh automático (Watch API para updates)
- [ ] Load balancing entre múltiplas instâncias
- [ ] Descoberta cross-cluster (Kubernetes Federation)
- [ ] Métricas de descoberta (quantas consultas, latência)
- [ ] Fallback para versões específicas (`api-version` label)
