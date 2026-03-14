# 🚀 POC: Service Discovery + Kiota

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-326CE5?logo=kubernetes&logoColor=white)](https://kubernetes.io/)
[![Aspire](https://img.shields.io/badge/Aspire-13.1.2-512BD4)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![Kiota](https://img.shields.io/badge/Kiota-1.22.0-00BCF2)](https://learn.microsoft.com/en-us/openapi/kiota/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Esta é uma prova de conceito demonstrando a **integração perfeita** entre:
- 🔍 **Descoberta Automática** via Kubernetes API com labels
- 🤖 **Kiota** para clientes HTTP type-safe
- 📦 **Projeto Shared** com código reutilizável

> **Destaque:** Elimina URLs hardcoded + Type-safety em compile-time = Zero bugs de contrato em produção! 🎯

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Como Executar](#como-executar)
  - [Desenvolvimento Local (Aspire)](#desenvolvimento-local-aspire)
  - [Kubernetes (Produção)](#kubernetes-produção)
- [Testando a Cadeia de Serviços](#testando-a-cadeia-de-serviços)
- [Service Discovery - Como Funciona](#service-discovery---como-funciona)
- [Projetos Inter-Soluções](#projetos-inter-soluções)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Kiota - Geração de Clientes](#kiota---geração-de-clientes)

---

## 🎯 Visão Geral

Esta POC demonstra:

- ✅ **Service Discovery** automático entre microsserviços
- ✅ Orquestração de serviços com **Aspire AppHost**
- ✅ Geração de clientes HTTP tipados com **Kiota**
- ✅ Documentação interativa com **Scalar** (alternativa moderna ao Swagger)
- ✅ Cadeia de chamadas: **ServiceA → ServiceB → ServiceC**

---

## 🏗️ Arquitetura

```
┌─────────────┐      Service Discovery      ┌─────────────┐      Service Discovery      ┌─────────────┐
│  ServiceA   │ ──────────────────────────> │  ServiceB   │ ──────────────────────────> │  ServiceC   │
│  (Users)    │  http://serviceb            │  (Products) │  http://servicec            │  (Orders)   │
└─────────────┘                             └─────────────┘                             └─────────────┘
      │                                            │                                            │
      │                                            │                                            │
      └────────────────────────────────────────────┴────────────────────────────────────────────┘
                                                   │
                                          ┌────────▼────────┐
                                          │  Aspire AppHost │
                                          │   (Orquestrador)│
                                          └─────────────────┘
```

### Cadeia de Serviços

1. **ServiceA** expõe endpoint `/api/users/with-products/{id}`
   - Chama **ServiceB** via Service Discovery

2. **ServiceB** expõe endpoint `/api/products/with-orders/{id}`
   - Chama **ServiceC** via Service Discovery

3. **ServiceC** expõe endpoints de pedidos
   - Endpoint final da cadeia

---

## 🛠️ Tecnologias Utilizadas

| Tecnologia | Versão | Propósito |
|------------|--------|-----------|
| **.NET** | 10.0 | Runtime e framework |
| **Aspire** | 13.1.2 | Orquestração e Service Discovery |
| **Kiota** | 1.30.0 | Geração de clientes HTTP tipados |
| **Scalar** | 2.13.8 | Documentação de API interativa |
| **Microsoft.Extensions.ServiceDiscovery** | 10.4.0 | Service Discovery |

### Pacotes NuGet por Serviço

**ServiceA e ServiceB:**
- `Microsoft.Extensions.ServiceDiscovery` (10.4.0)
- `Scalar.AspNetCore` (2.13.8)
- `Microsoft.Kiota.Http.HttpClientLibrary` (1.22.0)
- `Microsoft.Kiota.Serialization.Json` (1.22.0)
- `Microsoft.Kiota.Serialization.Form` (1.22.0)
- `Microsoft.Kiota.Serialization.Text` (1.22.0)
- `Microsoft.Kiota.Serialization.Multipart` (1.22.0)

**ServiceC:**
- `Microsoft.Extensions.ServiceDiscovery` (10.4.0)
- `Scalar.AspNetCore` (2.13.8)

---

## 📁 Estrutura do Projeto

```
dotnet-playground-test/
├── apphost.cs                    # Aspire AppHost (orquestrador)
├── apphost.run.json              # Configurações de inicialização
├── DotNetPlayground.sln          # Solution file
├── k8s/                          # Kubernetes manifests
│   ├── 00-namespaces.yaml        # Namespaces (users-ns, products-ns, orders-ns)
│   ├── 01-servicea-deployment.yaml   # ServiceA deployment
│   ├── 02-serviceb-deployment.yaml   # ServiceB deployment
│   ├── 03-servicec-deployment.yaml   # ServiceC deployment
│   ├── 04-network-policies.yaml      # Network policies
│   ├── 05-ingress.yaml               # Ingress controller
│   └── README.md                     # Kubernetes deployment guide
├── ServiceA/
│   ├── ServiceA.csproj
│   ├── Program.cs
│   ├── Dockerfile                # Docker image
│   ├── Generated/
│   │   └── ServiceBClient/       # Cliente Kiota gerado
│   └── openapi.json
├── ServiceB/
│   ├── ServiceB.csproj
│   ├── Program.cs
│   ├── Dockerfile                # Docker image
│   ├── Generated/
│   │   └── ServiceCClient/       # Cliente Kiota gerado
│   └── openapi.json
└── ServiceC/
    ├── ServiceC.csproj
    ├── Program.cs
    ├── Dockerfile                # Docker image
    └── openapi.json
```

---

## 🚀 Como Executar

### Pré-requisitos

**Desenvolvimento Local:**
- .NET SDK 10.0 ou superior
- Kiota CLI instalado globalmente

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota
```

**Kubernetes (Produção):**
- Cluster Kubernetes (minikube, k3s, AKS, EKS, GKE)
- `kubectl` configurado
- Docker para build de imagens

---

### Desenvolvimento Local (Aspire)

```bash
# Na raiz do projeto
dotnet run apphost.cs
```

O Aspire Dashboard será iniciado em:
```
https://localhost:17247
```

**Acessar o Dashboard:**

1. Abra o navegador em `https://localhost:17247/login?t=<token>`
2. O token é exibido no console ao iniciar
3. No dashboard você verá todos os serviços rodando

---

### Kubernetes (Produção)

Ver seção [Kubernetes Deployment](#kubernetes-deployment) para instruções completas.

**Quick Start:**

```bash
# 1. Build e push das imagens
docker build -t your-registry/servicea:latest ./ServiceA
docker build -t your-registry/serviceb:latest ./ServiceB
docker build -t your-registry/servicec:latest ./ServiceC

docker push your-registry/servicea:latest
docker push your-registry/serviceb:latest
docker push your-registry/servicec:latest

# 2. Deploy no Kubernetes
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml

# 3. Verificar deployments
kubectl get pods -n users-ns
kubectl get pods -n products-ns
kubectl get pods -n orders-ns
```

---

## 🧪 Testando a Cadeia de Serviços

### Via Scalar (Recomendado)

1. Acesse o **Aspire Dashboard**
2. Clique em **ServiceA**
3. Clique no link do endpoint ou acesse `/scalar`
4. Execute: `GET /api/users/with-products/1`

**Resultado esperado:**
```json
{
  "Message": "ServiceA called ServiceB via service discovery",
  "Products": "[{\"Id\":1,\"Name\":\"Laptop\",\"Price\":999.99},{\"Id\":2,\"Name\":\"Mouse\",\"Price\":29.99}]"
}
```

### Via cURL

```bash
# Substitua {PORT} pela porta do ServiceA no dashboard
curl http://localhost:{PORT}/api/users/with-products/1
```

### Endpoints Disponíveis

**ServiceA (Users):**
- `GET /api/users` - Lista todos os usuários
- `GET /api/users/{id}` - Busca usuário por ID
- `POST /api/users` - Cria novo usuário
- `GET /api/users/with-products/{id}` - **Chama ServiceB via Service Discovery**

**ServiceB (Products):**
- `GET /api/products` - Lista todos os produtos
- `GET /api/products/{id}` - Busca produto por ID
- `PUT /api/products/{id}` - Atualiza produto
- `GET /api/products/with-orders/{id}` - **Chama ServiceC via Service Discovery**

**ServiceC (Orders):**
- `GET /api/orders` - Lista todos os pedidos
- `GET /api/orders/{id}` - Busca pedido por ID
- `DELETE /api/orders/{id}` - Deleta pedido

---

## 🔍 Service Discovery - Como Funciona

### Configuração nos Serviços

Cada serviço configura Service Discovery no `Program.cs`:

```csharp
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
});
```

### Resolução de Nomes

Quando um serviço faz uma chamada HTTP usando um nome lógico:

```csharp
var httpClient = httpClientFactory.CreateClient();
httpClient.BaseAddress = new Uri("http://serviceb");  // Nome lógico
```

O **Service Discovery** resolve automaticamente:
- ✅ Encontra o endereço real do serviço
- ✅ Gerencia balanceamento de carga (se múltiplas instâncias)
- ✅ Atualiza dinamicamente se o endereço mudar
- ✅ Funciona em desenvolvimento e produção

### AppHost (Orquestrador)

O `apphost.cs` registra os serviços:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
var serviceB = builder.AddProject<Projects.ServiceB>("serviceb");
var serviceC = builder.AddProject<Projects.ServiceC>("servicec");

builder.Build().Run();
```

O Aspire cria automaticamente:
- Registro de serviços no discovery
- Configuração de rede
- Monitoramento e logs centralizados

---

## 🌐 Projetos Inter-Soluções

### ⚠️ Aspire: Limitado à Mesma Solução

O **Service Discovery do Aspire** funciona **apenas para projetos dentro da mesma solução** ou orquestrados pelo mesmo AppHost.

### Por Que?

O Aspire usa um **orquestrador local (DCP - Distributed Application Coordinator)** que:
- Gerencia apenas os recursos definidos no AppHost
- Cria uma rede isolada para os serviços
- Não tem visibilidade de serviços fora do seu escopo

### ❌ Aspire: NÃO Funciona Entre Soluções

```
Ambiente: Desenvolvimento Local (Aspire)

Solution A (AppHost A)          Solution B (AppHost B)
├── ServiceA                    ├── ServiceX
└── ServiceB                    └── ServiceY
     │
     └──> ❌ NÃO pode chamar ServiceX via Service Discovery
```

### ✅ Kubernetes: FUNCIONA Entre Soluções!

```
Ambiente: Kubernetes (Produção)

Cluster Kubernetes
│
├── Namespace: team-a (Solution A)
│   ├── ServiceA (desenvolvido por Team A)
│   └── ServiceB (desenvolvido por Team A)
│        │
│        └──> ✅ PODE chamar ServiceX via DNS
│
└── Namespace: team-b (Solution B)
    ├── ServiceX (desenvolvido por Team B)
    └── ServiceY (desenvolvido por Team B)
```

**No Kubernetes, cada equipe/solução pode ter:**
- ✅ Seu próprio repositório Git
- ✅ Seu próprio pipeline CI/CD
- ✅ Seu próprio namespace
- ✅ Comunicação cross-namespace via DNS

**Exemplo Real:**

```csharp
// Team A (Solution A) - ServiceB
// Chama serviço de outra equipe/solução
var httpClient = httpClientFactory.CreateClient();
httpClient.BaseAddress = new Uri("http://servicex.team-b.svc.cluster.local");
var response = await httpClient.GetAsync("/api/data");
```

---

### 🔧 Como Service Discovery Funciona em Cada Ambiente

#### Aspire (Desenvolvimento Local)

**✅ Automático - Não precisa configurar nada!**

```csharp
// apphost.cs
var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
var serviceB = builder.AddProject<Projects.ServiceB>("serviceb");

// ServiceA - NENHUMA configuração necessária!
var httpClient = httpClientFactory.CreateClient();
httpClient.BaseAddress = new Uri("http://serviceb"); // ✅ Aspire resolve automaticamente
```

**Como funciona:**
1. Aspire injeta variáveis de ambiente automaticamente
2. `Microsoft.Extensions.ServiceDiscovery` lê essas variáveis
3. Resolve `http://serviceb` → `http://localhost:5234` (porta real)

---

#### Kubernetes (Produção)

**❌ NÃO é automático - Precisa configurar via:**

**Opção 1: Variáveis de Ambiente (Recomendado)**

```yaml
# deployment.yaml
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"
```

```csharp
// Program.cs
builder.Services.AddHttpClient("ServiceB", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var url = config["Services:ServiceB:Url"]; // Lê do env
    client.BaseAddress = new Uri(url);
});
```

**Opção 2: ConfigMap**

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: service-urls
  namespace: users-ns
data:
  serviceb-url: "http://serviceb.products-ns.svc.cluster.local"
  servicec-url: "http://servicec.orders-ns.svc.cluster.local"
---
# deployment.yaml
envFrom:
- configMapRef:
    name: service-urls
```

**Opção 3: Hardcoded (Não recomendado)**

```csharp
// Program.cs - NÃO FAÇA ISSO!
httpClient.BaseAddress = new Uri("http://serviceb.products-ns.svc.cluster.local");
```

---

### 🎯 Por Que Não é Automático no Kubernetes?

**Aspire vs Kubernetes:**

| Aspecto | Aspire | Kubernetes |
|---------|--------|------------|
| **Orquestrador** | Único AppHost conhece todos os serviços | Cluster pode ter centenas de namespaces/serviços |
| **Descoberta** | Automática (DCP injeta config) | DNS nativo, mas você escolhe quais serviços usar |
| **Configuração** | Zero-config | Explícita via env vars |
| **Escopo** | Mesma solução | Todo o cluster |
| **Segurança** | Isolado localmente | Network policies controlam acesso |

**Kubernetes tem DNS, mas você precisa dizer qual serviço usar porque:**
- ✅ Cluster pode ter 1000+ serviços
- ✅ Você controla explicitamente as dependências
- ✅ Permite múltiplas versões do mesmo serviço
- ✅ Melhor segurança (princípio do menor privilégio)

---

### 💡 Solução: Service Discovery Híbrido

**Desenvolvimento (Aspire):**
```csharp
// Automático via Aspire
httpClient.BaseAddress = new Uri("http://serviceb");
```

**Produção (Kubernetes):**
```csharp
// Configurado via appsettings ou env vars
var serviceUrl = configuration["Services:ServiceB:Url"] 
    ?? "http://serviceb"; // fallback para dev

httpClient.BaseAddress = new Uri(serviceUrl);
```

**appsettings.json:**
```json
{
  "Services": {
    "ServiceB": {
      "Url": "http://serviceb"  // Dev/Aspire
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "Services": {
    "ServiceB": {
      "Url": "http://serviceb.products-ns.svc.cluster.local"  // K8s
    }
  }
}
```

Ou via **variáveis de ambiente no Kubernetes** (sobrescreve appsettings):
```yaml
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"
```

---

### 🚀 Descoberta Automática no Kubernetes (Avançado)

**SIM! Existem formas de tornar automático:**

#### Opção 1: Service Discovery com Labels/Annotations ✅ **IMPLEMENTADO NESTA POC**

**Conceito:** Serviços anunciam suas capacidades via labels, consumidores descobrem automaticamente.

Esta POC implementa descoberta automática usando **KubernetesClient** que consulta a API do Kubernetes em busca de serviços com labels específicos.

**Como Funciona:**

1. **Serviços anunciam suas APIs via labels**

```yaml
# k8s/02-serviceb-deployment.yaml
apiVersion: v1
kind: Service
metadata:
  name: serviceb
  namespace: products-ns
  labels:
    app: serviceb
    api-type: products-api       # ← Label para descoberta automática
    api-version: v1
  annotations:
    api.company.com/description: "Products API"
    api.company.com/owner: "products-team"
spec:
  selector:
    app: serviceb
  ports:
  - port: 80
```

2. **Consumidores descobrem automaticamente via KubernetesClient**

```csharp
// ServiceA/Infrastructure/KubernetesServiceDiscovery.cs
using k8s;

public class KubernetesServiceDiscovery : IKubernetesServiceDiscovery
{
    private readonly IKubernetes? _client;
    private readonly bool _isRunningInKubernetes;
    
    public KubernetesServiceDiscovery()
    {
        try
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            _client = new Kubernetes(config);
            _isRunningInKubernetes = true;
        }
        catch
        {
            // Não está rodando no Kubernetes (dev local)
            _isRunningInKubernetes = false;
        }
    }
    
    public async Task<string?> DiscoverServiceUrlAsync(string apiType)
    {
        if (!_isRunningInKubernetes || _client == null)
            return null;
        
        // Buscar serviços com label api-type específico
        var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
            labelSelector: $"api-type={apiType}"
        );
        
        var service = services.Items.FirstOrDefault();
        if (service == null) 
            return null;
        
        var ns = service.Metadata.NamespaceProperty;
        var name = service.Metadata.Name;
        
        return $"http://{name}.{ns}.svc.cluster.local";
    }
}
```

3. **Uso com fallback automático**

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
    
    return Results.Ok(new { Message = "Auto-discovered!", Products = response });
});
```

4. **RBAC Permissions necessárias**

```yaml
# k8s/04-rbac.yaml
---
# ServiceAccount
apiVersion: v1
kind: ServiceAccount
metadata:
  name: servicea-sa
  namespace: users-ns
---
# ClusterRole - permite listar services em todos os namespaces
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: service-discovery-reader
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
---
# ClusterRoleBinding
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

5. **Deploy no Kubernetes**

```bash
# Deploy com RBAC
kubectl apply -f k8s/04-rbac.yaml

# Deploy dos serviços (já configurados com serviceAccountName)
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

**Packages necessários:**
```bash
dotnet add package KubernetesClient --version 19.0.2
```

**Vantagens:**
- ✅ Descoberta automática no Kubernetes
- ✅ Funciona cross-namespace
- ✅ Fallback para configuração manual (dev/Aspire)
- ✅ Não precisa hardcode de URLs
- ✅ Mudanças de namespace são transparentes

**Fluxo Completo:**

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. ServiceB é deployado com label "api-type: products-api"     │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ServiceA precisa chamar ServiceB                             │
│    - KubernetesServiceDiscovery consulta K8s API                │
│    - Busca: labelSelector="api-type=products-api"               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. K8s API retorna:                                              │
│    - Service name: serviceb                                      │
│    - Namespace: products-ns                                      │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. ServiceA constrói URL automaticamente:                        │
│    http://serviceb.products-ns.svc.cluster.local                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. HttpClient faz chamada usando URL descoberta                 │
└─────────────────────────────────────────────────────────────────┘
```

---

#### Opção 2: Service Mesh (Istio/Linkerd) - Totalmente Automático ✅✅

**Istio** faz descoberta **100% automática** sem configuração!

```yaml
# ServiceA - NENHUMA configuração necessária!
apiVersion: apps/v1
kind: Deployment
metadata:
  name: servicea
  namespace: users-ns
spec:
  template:
    metadata:
      labels:
        app: servicea
        version: v1
    spec:
      containers:
      - name: servicea
        image: servicea:latest
        # SEM env vars de URLs! ✅
```

```csharp
// ServiceA - ZERO configuração!
var httpClient = httpClientFactory.CreateClient();

// Istio resolve automaticamente via service mesh
httpClient.BaseAddress = new Uri("http://serviceb"); // ✅ Funciona!

// Istio procura por Service "serviceb" em QUALQUER namespace
// e roteia automaticamente
```

**Como Istio descobre:**
1. Istio sincroniza com Kubernetes API
2. Conhece todos os Services em todos os namespaces
3. Injeta sidecar (Envoy proxy) em cada pod
4. Proxy resolve DNS automaticamente

**Benefícios extras:**
- ✅ mTLS automático
- ✅ Circuit breaker
- ✅ Retry automático
- ✅ Load balancing inteligente
- ✅ Distributed tracing

---

#### Opção 3: Consul Service Mesh - Descoberta Dinâmica ✅✅

```csharp
using Consul;

public class ConsulServiceDiscovery
{
    private readonly IConsulClient _consul;
    
    public ConsulServiceDiscovery()
    {
        _consul = new ConsulClient(config =>
        {
            config.Address = new Uri("http://consul.service.consul:8500");
        });
    }
    
    public async Task<string> GetServiceUrl(string serviceName)
    {
        var services = await _consul.Health.Service(serviceName, tag: "", passingOnly: true);
        var service = services.Response.FirstOrDefault();
        
        if (service == null)
            throw new Exception($"Service {serviceName} not found");
        
        return $"http://{service.Service.Address}:{service.Service.Port}";
    }
}

// Uso
var discovery = new ConsulServiceDiscovery();
var url = await discovery.GetServiceUrl("products-api");
```

---

#### Opção 4: External Secrets Operator com Service Catalog ✅

**Ideia:** Serviços registram suas URLs em um Secret Manager central.

```yaml
# ServiceB publica sua URL no AWS Secrets Manager/Azure Key Vault
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: service-registry
  namespace: users-ns
spec:
  refreshInterval: 1m
  secretStoreRef:
    name: aws-secrets-manager
    kind: SecretStore
  target:
    name: service-urls
  data:
  - secretKey: serviceb-url
    remoteRef:
      key: /services/products/url
```

**Auto-injetado como env var:**
```yaml
envFrom:
- secretRef:
    name: service-urls
```

---

#### Opção 5: Annotations + Init Container (Custom) ✅

**Conceito:** Init container lê annotations e cria ConfigMap automaticamente.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: servicea
  namespace: users-ns
  annotations:
    service-dependencies: "products-api@products-ns,orders-api@orders-ns"
spec:
  template:
    spec:
      initContainers:
      - name: service-discovery
        image: your-registry/service-discovery-init:latest
        command: ["/discover-services.sh"]
        # Script lê annotations e cria env vars
      containers:
      - name: servicea
        # Env vars injetadas automaticamente
```

**Script `/discover-services.sh`:**
```bash
#!/bin/bash
# Lê annotations e gera env vars
IFS=',' read -ra DEPS <<< "$SERVICE_DEPENDENCIES"
for dep in "${DEPS[@]}"; do
  IFS='@' read -ra PARTS <<< "$dep"
  name="${PARTS[0]}"
  ns="${PARTS[1]}"
  echo "export ${name^^}_URL=http://${name}.${ns}.svc.cluster.local" >> /etc/env
done
```

---

#### Opção 6: Kubernetes Gateway API + Service Catalog ✅

```yaml
# ServiceB registra-se no Service Catalog
apiVersion: v1
kind: Service
metadata:
  name: serviceb
  namespace: products-ns
  labels:
    api.company.com/discover: "true"
    api.company.com/type: "products"
    api.company.com/version: "v1"
spec:
  selector:
    app: serviceb
```

**Operator customizado monitora labels e atualiza ConfigMap global:**

```yaml
# Auto-gerado por operator
apiVersion: v1
kind: ConfigMap
metadata:
  name: service-catalog
  namespace: kube-system
data:
  services.json: |
    {
      "products": {
        "url": "http://serviceb.products-ns.svc.cluster.local",
        "version": "v1",
        "namespace": "products-ns"
      }
    }
```

---

### 📊 Comparação de Soluções

| Solução | Automação | Complexidade | Requer Service Mesh | Custo |
|---------|-----------|--------------|---------------------|-------|
| **Env Vars** | ❌ Manual | Baixa | Não | Grátis |
| **ConfigMap** | ❌ Manual | Baixa | Não | Grátis |
| **K8s API + Labels** | ✅ Automático | Média | Não | Grátis |
| **Istio/Linkerd** | ✅✅ Total | Alta | Sim | Médio |
| **Consul** | ✅✅ Total | Alta | Opcional | Médio |
| **External Secrets** | ✅ Automático | Média | Não | Cloud $ |
| **Custom Operator** | ✅ Automático | Alta | Não | Dev effort |

---

### 💡 Recomendação

**Para sua POC:**

1. **Desenvolvimento (Aspire):** ✅ Já é automático!
2. **Produção Simples:** Use **Env Vars** (já está nos manifests)
3. **Produção Avançada:** Use **Istio** (descoberta automática completa)

**Código híbrido (funciona em ambos):**

```csharp
// Program.cs
builder.Services.AddHttpClient("ServiceB", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    
    // Tenta configuração explícita primeiro
    var url = config["Services:ServiceB:Url"];
    
    // Fallback para descoberta automática (dev/Aspire)
    if (string.IsNullOrEmpty(url))
    {
        url = "http://serviceb"; // Aspire/Istio resolvem
    }
    
    client.BaseAddress = new Uri(url);
});
```

### Alternativas para Comunicação Inter-Soluções

Se você precisa comunicação entre soluções diferentes, use:

#### 1. **Kubernetes Service Discovery (Recomendado)** ✅

**SIM, funciona entre namespaces!**

No Kubernetes, Service Discovery funciona nativamente **entre namespaces** usando DNS:

```yaml
# Namespace: team-a
apiVersion: v1
kind: Service
metadata:
  name: serviceb
  namespace: team-a
spec:
  selector:
    app: serviceb
  ports:
  - port: 80
```

**Chamadas Cross-Namespace:**

```csharp
// ServiceA em namespace "team-b" chamando ServiceB em namespace "team-a"

// Mesmo namespace
httpClient.BaseAddress = new Uri("http://serviceb");

// Outro namespace
httpClient.BaseAddress = new Uri("http://serviceb.team-a");

// FQDN completo
httpClient.BaseAddress = new Uri("http://serviceb.team-a.svc.cluster.local");
```

**Formatos DNS:**

| Formato | Escopo | Exemplo |
|---------|--------|---------|
| `serviceb` | Mesmo namespace | `http://serviceb` |
| `serviceb.team-a` | Namespace específico | `http://serviceb.team-a` |
| `serviceb.team-a.svc.cluster.local` | FQDN | `http://serviceb.team-a.svc.cluster.local` |

**Network Policies (Segurança):**

```yaml
# Permitir apenas namespace "team-b" acessar serviceb
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-from-team-b
  namespace: team-a
spec:
  podSelector:
    matchLabels:
      app: serviceb
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          team: team-b
```

#### 2. **Service Mesh (Observabilidade + Segurança)**

```yaml
# Istio - mTLS automático entre namespaces
apiVersion: security.istio.io/v1beta1
kind: PeerAuthentication
metadata:
  name: default
  namespace: team-a
spec:
  mtls:
    mode: STRICT
```

**Benefícios:**
- ✅ mTLS automático
- ✅ Circuit breaker
- ✅ Retry policies
- ✅ Observabilidade completa
- ✅ Traffic splitting (canary)

**Popular Service Meshes:**
- **Istio** - Mais completo
- **Linkerd** - Leve e simples
- **Consul Connect** - HashiCorp stack

#### 3. **API Gateway**
```
Solution A ──> API Gateway ──> Solution B
              (Kong, YARP, Ocelot)
```

#### 4. **URLs Diretas (Desenvolvimento)**
```csharp
// Configuração via appsettings.json
var serviceUrl = configuration["Services:ServiceB:Url"];
httpClient.BaseAddress = new Uri(serviceUrl);
```

### Recomendação por Ambiente

| Ambiente | Solução | Por Quê |
|----------|---------|---------|
| **Desenvolvimento Local** | Aspire AppHost | Orquestração simples, logs integrados |
| **Kubernetes** | Service Discovery nativo | Funciona entre namespaces, DNS automático |
| **Produção Complexa** | Kubernetes + Istio | mTLS, observabilidade, resiliência |
| **Multi-Cloud** | Consul / Service Registry | Funciona além de Kubernetes |

---

## 🔒 Prevenção de Quebra de Contratos entre APIs

### O Problema

Quando você tem múltiplas soluções/equipes consumindo APIs:

```
❌ Cenário Problemático:
┌──────────────────────────────────────────────────────────────┐
│ Team A atualiza API:                                         │
│ GET /api/products/{id}                                       │
│                                                              │
│ ANTES: { "id": 1, "name": "Laptop" }                        │
│ DEPOIS: { "productId": 1, "productName": "Laptop" } ← BREAKING! │
└──────────────────────────────────────────────────────────────┘
         ↓
┌──────────────────────────────────────────────────────────────┐
│ Team B consome a API:                                        │
│ var product = response.name; ← RUNTIME ERROR! 💥            │
└──────────────────────────────────────────────────────────────┘
```

**Problemas:**
- ❌ Erros só aparecem em **runtime**
- ❌ Testes podem não cobrir todos os cenários
- ❌ Deploy quebra em produção
- ❌ Rollback emergencial
- ❌ Comunicação manual entre equipes

---

### ✅ A Solução: Kiota + OpenAPI

Com Kiota, os contratos são **validados em tempo de compilação**:

```
✅ Cenário Seguro:
┌──────────────────────────────────────────────────────────────┐
│ Team A atualiza openapi.json:                                │
│ {                                                            │
│   "properties": {                                            │
│     "productId": { "type": "integer" },  ← Mudou!           │
│     "productName": { "type": "string" }  ← Mudou!           │
│   }                                                          │
│ }                                                            │
└──────────────────────────────────────────────────────────────┘
         ↓
┌──────────────────────────────────────────────────────────────┐
│ Team B regenera cliente Kiota:                               │
│ $ kiota generate -d openapi.json                             │
│                                                              │
│ COMPILE ERROR! ← Detectado ANTES do deploy! ✅              │
│ error CS1061: 'Product' does not contain 'Name'             │
└──────────────────────────────────────────────────────────────┘
```

---

### 🎯 Fluxo Recomendado

#### 1. **Contrato como Fonte da Verdade**

```
ServiceB/
├── openapi.json          ← Fonte da verdade (versionada no Git)
└── Program.cs
```

#### 2. **Pipeline CI/CD Automatizado**

```yaml
# .github/workflows/contract-validation.yml
name: Contract Validation

on:
  pull_request:
    paths:
      - 'ServiceB/openapi.json'

jobs:
  validate-breaking-changes:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      # Detectar breaking changes
      - name: OpenAPI Breaking Change Detection
        uses: oasdiff/oasdiff-action@v0.0.15
        with:
          base: main
          revision: HEAD
          fail-on-diff: breaking
      
      # Regenerar clientes consumidores
      - name: Regenerate ServiceA Client
        run: |
          cd ServiceA
          kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient
      
      # Build para validar contratos
      - name: Build & Test
        run: |
          dotnet build
          dotnet test
      
      # Se passar, commit automático do cliente atualizado
      - name: Commit Generated Clients
        if: success()
        run: |
          git config user.name "Bot"
          git add ServiceA/Generated/
          git commit -m "chore: update ServiceB client"
```

#### 3. **Semantic Versioning no OpenAPI**

```json
{
  "openapi": "3.0.0",
  "info": {
    "title": "ServiceB API",
    "version": "2.0.0",  ← Versão semântica
    "x-breaking-changes": [
      {
        "version": "2.0.0",
        "date": "2026-03-14",
        "description": "Renamed 'name' to 'productName'"
      }
    ]
  }
}
```

#### 4. **Versionamento de API**

**Opção A - URL Versioning:**
```csharp
// ServiceB suporta múltiplas versões
app.MapGet("/v1/api/products/{id}", () => { /* old contract */ });
app.MapGet("/v2/api/products/{id}", () => { /* new contract */ });
```

**Opção B - Header Versioning:**
```csharp
app.MapGet("/api/products/{id}", (HttpContext ctx) =>
{
    var version = ctx.Request.Headers["X-API-Version"];
    return version == "2.0" ? newContract : oldContract;
});
```

---

### 🔄 Workflow Completo

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Dev atualiza ServiceB                                        │
│    - Modifica código                                            │
│    - Atualiza openapi.json                                      │
│    - git commit & push                                          │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. CI Pipeline detecta mudança em openapi.json                 │
│    ✅ Valida breaking changes (oasdiff)                         │
│    ✅ Regenera clientes de TODOS os consumidores                │
│    ✅ Roda testes integrados                                    │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3a. SE tem breaking change:                                     │
│     ❌ Build falha                                              │
│     📧 Notifica dev e equipes consumidoras                      │
│     📝 Exige atualização de versão (1.x → 2.0)                 │
│     🔄 Exige suporte a versão antiga (deprecated)              │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3b. SE NÃO tem breaking change:                                 │
│     ✅ Build passa                                              │
│     ✅ Clientes regenerados automaticamente                     │
│     ✅ PR aprovado                                              │
│     ✅ Deploy automático                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

### 📦 Distribuição de Contratos

**Opção 1: Repositório Centralizado de Contratos**

```
contracts-repo/
├── serviceb/
│   ├── v1.0.0/
│   │   └── openapi.json
│   └── v2.0.0/
│       └── openapi.json
└── servicec/
    └── v1.0.0/
        └── openapi.json
```

**Opção 2: NuGet Package com Cliente Gerado**

```bash
# ServiceB publica cliente como NuGet package
dotnet pack ServiceB.Client.csproj

# ServiceA consome o package
dotnet add package ServiceB.Client --version 2.0.0
```

**Opção 3: Git Submodules**

```bash
# ServiceA adiciona ServiceB como submodule
git submodule add https://github.com/company/serviceb contracts/serviceb

# Atualiza quando contrato muda
git submodule update --remote
kiota generate -d contracts/serviceb/openapi.json
```

---

### 🛡️ Ferramentas para Validação de Contratos

| Ferramenta | Propósito |
|------------|-----------|
| **oasdiff** | Detecta breaking changes em OpenAPI |
| **Spectral** | Linting de OpenAPI (qualidade do contrato) |
| **Pact** | Contract testing consumidor-driven |
| **Prism** | Mock server baseado em OpenAPI |
| **Kiota** | Geração de clientes tipados |

**Exemplo com oasdiff:**

```bash
# Detectar breaking changes entre versões
oasdiff breaking \
  contracts/serviceb/v1.0.0/openapi.json \
  contracts/serviceb/v2.0.0/openapi.json

# Output:
# Breaking changes detected:
# - DELETE /api/products/{id}/name (endpoint removed)
# - CHANGE /api/products/{id} response property 'name' renamed to 'productName'
```

---

### 🎓 Benefícios da Abordagem

| Benefício | Tradicional (HTTP manual) | Com Kiota + OpenAPI |
|-----------|---------------------------|---------------------|
| **Detecção de quebras** | Runtime 💥 | Compile time ✅ |
| **Refactoring seguro** | Impossível | Automático |
| **Documentação** | Desatualizada | Sempre sincronizada |
| **Testes** | Quebram em runtime | Não compilam |
| **Rollback** | Manual, arriscado | Versionamento Git |
| **Comunicação entre times** | Manual, email | CI/CD automático |

---

## 🤖 Kiota - Geração de Clientes

### O que é Kiota?

Kiota é uma ferramenta da Microsoft para gerar **clientes HTTP tipados** a partir de especificações OpenAPI.

### Vantagens

- ✅ **Type-safe**: erros em **tempo de compilação** (não runtime!)
- ✅ **IntelliSense completo**: autocomplete de endpoints e modelos
- ✅ **Reduz boilerplate**: não precisa escrever HttpClient manual
- ✅ **Sincronização automática**: regenere quando API mudar
- ✅ **Previne quebra de contratos**: compile-time validation

### Como Gerar Clientes

```bash
# Instalar Kiota globalmente
dotnet tool install --global Microsoft.OpenApi.Kiota

# Gerar cliente para ServiceB
cd ServiceA
kiota generate -l CSharp \
  -d ../ServiceB/openapi.json \
  -o ./Generated/ServiceBClient \
  -n ServiceA.Clients.ServiceB
```

### Estrutura Gerada

```
ServiceA/Generated/ServiceBClient/
├── ApiClient.cs              # Cliente principal
├── Api/
│   ├── ApiRequestBuilder.cs
│   └── Products/
│       ├── ProductsRequestBuilder.cs
│       └── Item/
│           └── ProductsItemRequestBuilder.cs
└── Models/                   # (se houver)
```

### Uso do Cliente Gerado

```csharp
var httpClient = httpClientFactory.CreateClient();
httpClient.BaseAddress = new Uri("http://serviceb");

var jsonSerializerFactory = new JsonSerializationWriterFactory();
var jsonParseFactory = new JsonParseNodeFactory();
var authProvider = new AnonymousAuthenticationProvider();

var adapter = new HttpClientRequestAdapter(
    authProvider,
    jsonParseFactory,
    jsonSerializerFactory,
    httpClient
);

var client = new ApiClient(adapter);
var products = await client.Api.Products.GetAsync();
```

### Atualizar Cliente

Se a API mudar, regenere:

```bash
kiota generate -l CSharp -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient -n ServiceA.Clients.ServiceB
```

---

## 📚 Documentação Completa

Toda documentação detalhada está organizada na pasta `/docs`:

- **[docs/DOCUMENTATION-INDEX.md](docs/DOCUMENTATION-INDEX.md)** - 📑 Índice completo de toda documentação
- **[docs/QUICK-START.md](docs/QUICK-START.md)** - 🚀 Guia rápido de 5 minutos
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - 🏗️ Decisões arquiteturais e trade-offs
- **[docs/INTEGRATION-GUIDE.md](docs/INTEGRATION-GUIDE.md)** - 🔗 Guia de integração Kiota + Descoberta
- **[docs/KIOTA-EXPLAINED.md](docs/KIOTA-EXPLAINED.md)** - 🤖 Kiota explicado de forma simples
- **[docs/AUTOMATIC-DISCOVERY.md](docs/AUTOMATIC-DISCOVERY.md)** - 🔍 Descoberta automática detalhada
- **[docs/OPENAPI-WORKFLOW.md](docs/OPENAPI-WORKFLOW.md)** - 📋 Workflow OpenAPI e CI/CD
- **[docs/TESTING.md](docs/TESTING.md)** - 🧪 Guia de testes
- **[docs/k8s/README.md](docs/k8s/README.md)** - ☸️ Deployment Kubernetes
- **[docs/k8s/SERVICE-DISCOVERY.md](docs/k8s/SERVICE-DISCOVERY.md)** - 🔎 Service Discovery no K8s

### Recursos Oficiais

- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)
- [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview)
- [Scalar](https://scalar.com/)

### Comandos Úteis

```bash
# Limpar e rebuild
dotnet clean
dotnet build

# Rodar apenas um serviço
cd ServiceA
dotnet run

# Ver logs do Aspire
# Os logs aparecem no dashboard ou no console do apphost

# Parar o Aspire
# Ctrl+C no terminal onde rodou o apphost
```

---

---

## ☸️ Kubernetes Deployment

Esta POC inclui manifests completos para deployment em Kubernetes com **Service Discovery entre namespaces**.

### Arquitetura

```
┌──────────────────────┐
│  Namespace: users-ns │
│  ServiceA (2 pods)   │──── http://serviceb.products-ns ────┐
└──────────────────────┘                                      │
                                                              ▼
┌──────────────────────┐                                ┌──────────────────────┐
│Namespace: products-ns│                                │ Namespace: orders-ns │
│  ServiceB (2 pods)   │──── http://servicec.orders-ns ────▶│  ServiceC (2 pods)   │
└──────────────────────┘                                └──────────────────────┘
```

### Manifests Incluídos

| Arquivo | Descrição |
|---------|-----------|
| `00-namespaces.yaml` | Cria 3 namespaces isolados |
| `01-servicea-deployment.yaml` | Deployment + Service para ServiceA |
| `02-serviceb-deployment.yaml` | Deployment + Service para ServiceB |
| `03-servicec-deployment.yaml` | Deployment + Service para ServiceC |
| `04-network-policies.yaml` | Network policies (zero trust) |
| `05-ingress.yaml` | Ingress para expor ServiceA |

### Deploy Rápido

```bash
# 1. Criar namespaces
kubectl apply -f k8s/00-namespaces.yaml

# 2. Deploy dos serviços (com Service Discovery configurado)
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml

# 3. Verificar
kubectl get pods -A | grep service
```

### Service Discovery Cross-Namespace

Os serviços usam DNS do Kubernetes para descoberta:

```csharp
// ServiceA chama ServiceB em outro namespace
httpClient.BaseAddress = new Uri("http://serviceb.products-ns.svc.cluster.local");

// ServiceB chama ServiceC em outro namespace
httpClient.BaseAddress = new Uri("http://servicec.orders-ns.svc.cluster.local");
```

**Formatos DNS suportados:**
- `serviceb` - mesmo namespace
- `serviceb.products-ns` - cross-namespace
- `serviceb.products-ns.svc.cluster.local` - FQDN completo

### Documentação Completa

Ver [docs/k8s/README.md](docs/k8s/README.md) para:
- Instruções detalhadas de deployment
- Troubleshooting
- Network policies explicadas
- Comandos de monitoramento

---

## 🎓 Conceitos-Chave Demonstrados

1. **Service Discovery**: Resolução automática de endereços de serviços (Aspire local + Kubernetes prod)
2. **Cross-Namespace Communication**: Comunicação entre serviços em diferentes namespaces (K8s)
3. **Contract Validation**: Prevenção de quebra de contratos com Kiota + OpenAPI
2. **Aspire Orchestration**: Gerenciamento de múltiplos serviços
3. **Type-Safe HTTP Clients**: Clientes gerados com Kiota
4. **API Documentation**: Scalar como alternativa moderna ao Swagger
5. **Distributed Application**: Arquitetura de microsserviços

---

## ⚡ Próximos Passos

Para expandir esta POC:

- [ ] Adicionar autenticação JWT
- [ ] Implementar Circuit Breaker (Polly)
- [ ] Adicionar OpenTelemetry para observabilidade
- [ ] Configurar retry policies
- [ ] Adicionar testes de integração
- [ ] Deploy em Kubernetes
- [ ] Configurar CI/CD

---

## 📖 Documentação Adicional

- **[INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md)** - Guia completo de integração Kiota + Descoberta
- **[KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md)** - Kiota explicado de forma simples
- **[AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md)** - Como funciona a descoberta automática
- **[TESTING.md](TESTING.md)** - Guia de testes da POC deployada
- **[k8s/SERVICE-DISCOVERY.md](k8s/SERVICE-DISCOVERY.md)** - Service Discovery no Kubernetes

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para:

1. Fork o projeto
2. Criar uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abrir um Pull Request

---

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👨‍💻 Autor

**Matheus Reichert**

POC criada para demonstrar Service Discovery com .NET Aspire e Kiota.

- GitHub: [@MatheusReichert](https://github.com/MatheusReichert)
- LinkedIn: [Matheus Reichert](https://www.linkedin.com/in/matheus-reichert/)

---

## ⭐ Star History

Se este projeto foi útil para você, considere dar uma ⭐!

---

**Data**: Março 2026
