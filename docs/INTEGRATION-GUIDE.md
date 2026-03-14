# 🎯 Guia de Integração: Kiota + Descoberta Automática

Esta POC demonstra a integração de **duas estratégias complementares**:

1. **Descoberta Automática via Kubernetes Labels** - Para encontrar serviços dinamicamente
2. **Kiota Type-Safe Clients** - Para fazer chamadas com validação em compile-time

---

## 🏗️ Arquitetura da Solução

```
┌─────────────────────────────────────────────────────────────────┐
│                         Projeto Shared                          │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  KubernetesServiceDiscovery                               │  │
│  │  - Consulta K8s API                                       │  │
│  │  - Busca services por label (api-type)                    │  │
│  │  - Retorna URLs automaticamente                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  KiotaClientFactoryBase<TClient>                          │  │
│  │  - Classe base genérica                                   │  │
│  │  - Combina descoberta + Kiota                             │  │
│  │  - Reutilizável para todos os serviços                    │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↓ (referenciado por)
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   ServiceA    │    │   ServiceB    │    │   ServiceC    │
│               │    │               │    │               │
│ ServiceBClient│    │ ServiceCClient│    │               │
│ Factory       │    │ Factory       │    │               │
│ (herda base)  │    │ (herda base)  │    │               │
└───────────────┘    └───────────────┘    └───────────────┘
```

---

## 🔧 Componentes

### 1. Shared Project (Código Reutilizável)

**Localização:** `/Shared/`

**Classes:**

#### `IKubernetesServiceDiscovery` / `KubernetesServiceDiscovery`
- Consulta Kubernetes API para descobrir services
- Busca por label `api-type`
- Retorna URLs no formato: `http://{name}.{namespace}.svc.cluster.local`

**Exemplo:**
```csharp
var url = await k8sDiscovery.DiscoverServiceUrlAsync("products-api");
// Retorna: http://serviceb.products-ns.svc.cluster.local
```

#### `KiotaClientFactoryBase<TClient>`
- Classe base abstrata para criar factories
- Implementa o pattern: **Descoberta → Fallback → Kiota Client**
- Genérica: funciona com qualquer cliente Kiota

**Propriedades abstratas:**
```csharp
protected abstract string ApiType { get; }           // ex: "products-api"
protected abstract string ConfigurationKey { get; }  // ex: "Services:ServiceB:Url"
protected abstract string DefaultUrl { get; }        // ex: "http://serviceb"
protected abstract TClient CreateClient(HttpClientRequestAdapter adapter);
```

---

### 2. ServiceA (Consumidor do ServiceB)

**Factory Específica:**

```csharp
// ServiceA/Infrastructure/ServiceBClientFactory.cs
public class ServiceBClientFactory : KiotaClientFactoryBase<ApiClient>
{
    protected override string ApiType => "products-api";
    protected override string ConfigurationKey => "Services:ServiceB:Url";
    protected override string DefaultUrl => "http://serviceb";

    protected override ApiClient CreateClient(HttpClientRequestAdapter adapter)
    {
        return new ApiClient(adapter); // Cliente Kiota gerado
    }
}
```

**Registro no DI:**

```csharp
// ServiceA/Program.cs
builder.Services.AddSingleton<IKubernetesServiceDiscovery, KubernetesServiceDiscovery>();
builder.Services.AddScoped<ServiceBClientFactory>();
```

**Uso no Endpoint:**

```csharp
app.MapGet("/api/users/with-products-typed/{id}", async (
    int id, 
    ServiceBClientFactory clientFactory) =>
{
    // 1. Cria cliente com descoberta automática
    var client = await clientFactory.CreateClientAsync();
    
    // 2. Chamadas type-safe
    var products = await client.Api.Products.GetAsync();
    
    return Results.Ok(new { UserId = id, Products = products });
});
```

---

### 3. ServiceB (Consumidor do ServiceC)

**Factory Específica:**

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

## 🎯 Fluxo Completo de Descoberta + Kiota

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Request chega no ServiceA                                    │
│    GET /api/users/with-products-typed/1                         │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ServiceBClientFactory.CreateClientAsync()                    │
│    ├─→ KubernetesServiceDiscovery.DiscoverServiceUrlAsync()    │
│    │   └─→ Query: labelSelector="api-type=products-api"        │
│    │   └─→ Retorna: http://serviceb.products-ns.svc...         │
│    ├─→ Se não encontrar: usa Configuration ou Default          │
│    ├─→ Cria HttpClient com BaseAddress = URL descoberta        │
│    └─→ Retorna ApiClient (Kiota) configurado                   │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Chamada Type-Safe com Kiota                                  │
│    var products = await client.Api.Products.GetAsync();         │
│    ✅ IntelliSense completo                                     │
│    ✅ Compile-time validation                                   │
│    ✅ Models gerados automaticamente                            │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. HttpClient faz request                                       │
│    GET http://serviceb.products-ns.svc.cluster.local/api/products│
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. ServiceB responde                                             │
│    [{"id":1,"name":"Laptop","price":999.99}]                    │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. Kiota deserializa para objetos tipados                       │
│    List<Product> products                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎓 Vantagens da Integração

### 1. Zero Hardcode de URLs
```csharp
// ❌ ANTES
var url = "http://serviceb.products-ns.svc.cluster.local"; // hardcoded!

// ✅ AGORA
var client = await clientFactory.CreateClientAsync(); // descoberto automaticamente
```

### 2. Type-Safety em Compile-Time
```csharp
// ❌ HttpClient tradicional (runtime errors)
var response = await httpClient.GetStringAsync("/api/prodcts"); // typo! 💥
var json = JsonSerializer.Deserialize<Product>(response); // pode quebrar

// ✅ Kiota (compile-time errors)
var products = await client.Api.Products.GetAsync(); // IntelliSense ✅
// Se API mudar, código não compila!
```

### 3. Mudanças de Namespace Transparentes
```yaml
# ServiceB move de products-ns → v2-products-ns
# Código continua funcionando! Descoberta automática encontra pelo label
```

### 4. Refactoring Seguro
```csharp
// Se ServiceB renomear endpoint /products → /items
// Kiota regenera cliente
// Código em ServiceA não compila até atualizar
// ✅ Detecta problema ANTES do deploy
```

### 5. Reutilização de Código
```csharp
// Shared project usado por todos os serviços
// KiotaClientFactoryBase evita duplicação
// Apenas override de 3 propriedades por service
```

---

## 📦 Estrutura de Arquivos

```
dotnet-playground-test/
├── Shared/                                    # ← NOVO!
│   ├── KubernetesServiceDiscovery.cs         # Descoberta automática
│   ├── KiotaClientFactoryBase.cs             # Base para factories
│   └── Shared.csproj
│
├── ServiceA/
│   ├── Generated/
│   │   └── ServiceBClient/                   # Cliente Kiota gerado
│   ├── Infrastructure/
│   │   └── ServiceBClientFactory.cs          # Herda base, 10 linhas!
│   └── Program.cs                            # Usa factory
│
├── ServiceB/
│   ├── Generated/
│   │   └── ServiceCClient/                   # Cliente Kiota gerado
│   ├── Infrastructure/
│   │   └── ServiceCClientFactory.cs          # Herda base, 10 linhas!
│   ├── openapi.json                          # Contrato da API
│   └── Program.cs
│
└── ServiceC/
    ├── openapi.json                          # Contrato da API
    └── Program.cs
```

---

## 🚀 Como Adicionar Novo Serviço

### Passo 1: Criar OpenAPI spec
```json
// ServiceD/openapi.json
{
  "openapi": "3.0.1",
  "info": { "title": "ServiceD API" },
  "paths": { "/api/inventory": { ... } }
}
```

### Passo 2: Gerar Cliente Kiota
```bash
cd ServiceC
kiota generate -l CSharp \
  -d ../ServiceD/openapi.json \
  -o ./Generated/ServiceDClient \
  -n ServiceC.Generated.ServiceDClient
```

### Passo 3: Criar Factory (10 linhas!)
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

### Passo 4: Registrar no DI
```csharp
// ServiceC/Program.cs
builder.Services.AddScoped<ServiceDClientFactory>();
```

### Passo 5: Usar!
```csharp
app.MapGet("/inventory", async (ServiceDClientFactory factory) =>
{
    var client = await factory.CreateClientAsync();
    return await client.Api.Inventory.GetAsync();
});
```

---

## 🧪 Testando a Integração

### Endpoint Tradicional (HttpClient)
```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products/1
```

**Resposta:**
```json
{
  "message": "ServiceA called ServiceB via service discovery",
  "discoveredUrl": "http://serviceb.products-ns.svc.cluster.local",
  "products": "[...]"
}
```

### Endpoint Type-Safe (Kiota + Descoberta)
```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products-typed/1
```

**Resposta:**
```json
{
  "message": "ServiceA → ServiceB usando Kiota + Descoberta Automática",
  "method": "Type-Safe Kiota Client",
  "userId": 1,
  "products": [
    {"id": 1, "name": "Laptop", "price": 999.99},
    {"id": 2, "name": "Mouse", "price": 29.99}
  ],
  "benefits": [
    "✅ URL descoberta automaticamente via K8s labels",
    "✅ Cliente type-safe gerado por Kiota",
    "✅ IntelliSense completo",
    "✅ Compile-time validation"
  ]
}
```

---

## 📊 Comparação: Antes vs Agora

| Aspecto | Antes (HttpClient manual) | Agora (Kiota + Descoberta) |
|---------|---------------------------|----------------------------|
| **URLs** | Hardcoded em cada serviço | Descoberta automática |
| **Type-Safety** | ❌ Runtime errors | ✅ Compile-time errors |
| **Mudanças de API** | Quebra em produção | Não compila |
| **IntelliSense** | Nenhum | Completo |
| **Boilerplate** | ~50 linhas por consumidor | ~10 linhas (herda base) |
| **Refactoring** | Arriscado | Seguro |
| **Cross-Namespace** | Config manual | Automático |

---

## 🎯 Conclusão

Esta POC demonstra a **combinação perfeita** de duas tecnologias:

1. **Descoberta Automática** - Elimina hardcode, cross-namespace transparente
2. **Kiota** - Type-safety, IntelliSense, compile-time validation

**Resultado:**
- ✅ Código limpo e reutilizável (projeto Shared)
- ✅ Zero URLs hardcoded
- ✅ Erros detectados em compile-time
- ✅ Fácil adicionar novos serviços (10 linhas)
- ✅ Funciona em dev (Aspire) e prod (Kubernetes)

**Perfect match! 🚀**
