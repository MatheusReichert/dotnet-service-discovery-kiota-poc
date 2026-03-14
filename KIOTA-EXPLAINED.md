# 🤖 Kiota Explicado de Forma Simples

## 🎯 O que é Kiota?

Kiota é uma ferramenta da Microsoft que **gera código C# automaticamente** a partir de um arquivo OpenAPI (Swagger).

**Analogia:** É como ter um assistente que lê a documentação de uma API e escreve todo o código C# necessário para chamar essa API.

---

## 📝 Passo a Passo: Como Funciona

### 1️⃣ Você tem uma API (ServiceB)

```csharp
// ServiceB/Program.cs
app.MapGet("/api/products", () => 
{
    return new[] 
    { 
        new { Id = 1, Name = "Laptop", Price = 999.99 },
        new { Id = 2, Name = "Mouse", Price = 29.99 }
    };
});

app.MapGet("/api/products/{id}", (int id) => 
{
    return new { Id = id, Name = "Laptop", Price = 999.99 };
});
```

---

### 2️⃣ Você cria um arquivo OpenAPI descrevendo a API

```json
// ServiceB/openapi.json
{
  "openapi": "3.0.1",
  "info": {
    "title": "ServiceB - Products API",
    "version": "1.0.0"
  },
  "paths": {
    "/api/products": {
      "get": {
        "operationId": "getProducts",
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": { "$ref": "#/components/schemas/Product" }
                }
              }
            }
          }
        }
      }
    },
    "/api/products/{id}": {
      "get": {
        "operationId": "getProductById",
        "parameters": [
          { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } }
        ],
        "responses": {
          "200": {
            "content": {
              "application/json": {
                "schema": { "$ref": "#/components/schemas/Product" }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "Product": {
        "type": "object",
        "properties": {
          "id": { "type": "integer" },
          "name": { "type": "string" },
          "price": { "type": "number" }
        }
      }
    }
  }
}
```

**Este arquivo é como um "contrato" que descreve:**
- Quais endpoints existem? `/api/products`, `/api/products/{id}`
- Que parâmetros aceitam? `id` (inteiro)
- O que retornam? Array de `Product` ou um único `Product`
- Estrutura do `Product`? `id`, `name`, `price`

---

### 3️⃣ Você roda o comando Kiota

```bash
cd ServiceA
kiota generate \
  -l CSharp \
  -d ../ServiceB/openapi.json \
  -o ./Generated/ServiceBClient \
  -n ServiceA.Generated.ServiceBClient
```

**O que esse comando faz:**
- `-l CSharp` → Gera código em C#
- `-d ../ServiceB/openapi.json` → Lê o contrato da API
- `-o ./Generated/ServiceBClient` → Salva os arquivos gerados aqui
- `-n ServiceA.Generated.ServiceBClient` → Namespace do código gerado

---

### 4️⃣ Kiota gera VÁRIOS arquivos C# automaticamente

```
ServiceA/Generated/ServiceBClient/
├── ApiClient.cs                           # Cliente principal
├── Api/
│   ├── ApiRequestBuilder.cs              # Builder para /api
│   └── Products/
│       ├── ProductsRequestBuilder.cs     # Builder para /api/products
│       ├── Item/
│       │   └── WithIdItemRequestBuilder.cs  # Builder para /api/products/{id}
│       └── ProductsResponse.cs           # Modelo de resposta
└── Models/
    └── Product.cs                        # Classe Product gerada
```

**Vamos ver o conteúdo de alguns arquivos gerados:**

---

## 📦 Arquivos Gerados pelo Kiota

### Arquivo: `Models/Product.cs`

```csharp
namespace ServiceA.Generated.ServiceBClient.Models;

/// <summary>
/// Classe gerada automaticamente representando um Product
/// </summary>
public class Product
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public double? Price { get; set; }
}
```

**O Kiota criou uma classe C# com propriedades tipadas!**

---

### Arquivo: `Api/Products/ProductsRequestBuilder.cs` (simplificado)

```csharp
namespace ServiceA.Generated.ServiceBClient.Api.Products;

public class ProductsRequestBuilder
{
    private readonly IRequestAdapter _requestAdapter;
    private readonly string _urlTemplate = "{+baseurl}/api/products";

    public ProductsRequestBuilder(IRequestAdapter requestAdapter)
    {
        _requestAdapter = requestAdapter;
    }

    /// <summary>
    /// GET /api/products
    /// </summary>
    public async Task<List<Product>?> GetAsync()
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = _urlTemplate
        };

        return await _requestAdapter.SendAsync<List<Product>>(requestInfo);
    }
}
```

**O Kiota criou um método `GetAsync()` que:**
- Sabe que a URL é `/api/products`
- Sabe que retorna `List<Product>`
- Faz toda a serialização/deserialização automaticamente

---

### Arquivo: `ApiClient.cs` (simplificado)

```csharp
namespace ServiceA.Generated.ServiceBClient;

public class ApiClient
{
    private readonly IRequestAdapter _requestAdapter;

    public ApiClient(IRequestAdapter requestAdapter)
    {
        _requestAdapter = requestAdapter;
        Api = new ApiRequestBuilder(_requestAdapter);
    }

    /// <summary>
    /// Builder para /api
    /// </summary>
    public ApiRequestBuilder Api { get; }
}
```

**Este é o cliente principal que você usa no código!**

---

## 🚀 Como Usar o Código Gerado

### ❌ ANTES (HttpClient manual - sem Kiota)

```csharp
app.MapGet("/api/users/with-products/{id}", async (
    IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri("http://serviceb");
    
    // 1. Fazer request manual
    var response = await httpClient.GetAsync("/api/products");
    
    // 2. Ler conteúdo como string
    var json = await response.Content.ReadAsStringAsync();
    
    // 3. Deserializar manualmente
    var products = JsonSerializer.Deserialize<List<Product>>(json);
    
    // ⚠️ Problemas:
    // - URL pode ter typo: "/api/prodcts" 💥
    // - Deserialização pode falhar em runtime
    // - Sem IntelliSense
    // - Se API mudar, só descobre em produção
    
    return Results.Ok(products);
});
```

---

### ✅ AGORA (Kiota - type-safe)

```csharp
app.MapGet("/api/users/with-products-typed/{id}", async (
    ServiceBClientFactory clientFactory) =>
{
    // 1. Criar cliente (com descoberta automática de URL)
    var client = await clientFactory.CreateClientAsync();
    
    // 2. Chamar API de forma type-safe
    var products = await client.Api.Products.GetAsync();
    //                          ↑        ↑       ↑
    //                       /api   /products  método HTTP GET
    
    // ✅ Vantagens:
    // - IntelliSense completo
    // - Se digitar errado, não compila
    // - Se API mudar, código quebra em compile-time
    // - Models gerados automaticamente
    
    return Results.Ok(products);
});
```

**Veja a diferença:**

```csharp
// ❌ HttpClient: tudo é string
var response = await httpClient.GetAsync("/api/products"); // pode ter typo
var json = await response.Content.ReadAsStringAsync();     // manual
var products = JsonSerializer.Deserialize<...>(json);      // pode falhar

// ✅ Kiota: tudo é tipado
var products = await client.Api.Products.GetAsync();       // type-safe!
//                          ^^^^^^^^^^^
//                          IntelliSense mostra opções
```

---

## 🎨 IntelliSense em Ação

Quando você digita `client.Api.`, o Visual Studio mostra:

```
client.Api.
    ├── Products        ← Kiota gerou isso do OpenAPI!
    │   ├── GetAsync()  ← GET /api/products
    │   └── [id]
    │       └── GetAsync() ← GET /api/products/{id}
```

Tudo vem da especificação OpenAPI! 🤯

---

## 🔄 Fluxo Completo: OpenAPI → Kiota → Código

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Dev do ServiceB escreve API                                  │
│    app.MapGet("/api/products", () => [...])                     │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. Dev do ServiceB cria/atualiza openapi.json                  │
│    {                                                             │
│      "paths": { "/api/products": { "get": {...} } }            │
│    }                                                             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Dev do ServiceA roda Kiota                                   │
│    $ kiota generate -d ../ServiceB/openapi.json                 │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Kiota gera código C# automaticamente                         │
│    - Models/Product.cs                                           │
│    - Api/Products/ProductsRequestBuilder.cs                      │
│    - ApiClient.cs                                                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. Dev do ServiceA usa cliente gerado                           │
│    var products = await client.Api.Products.GetAsync();         │
│                                                                  │
│    ✅ IntelliSense completo                                     │
│    ✅ Type-safe                                                  │
│    ✅ Se ServiceB mudar API, ServiceA não compila               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛡️ Prevenção de Quebra de Contrato

### Cenário: ServiceB muda a API

```diff
// ServiceB muda campo "name" → "productName"
{
  "id": 1,
- "name": "Laptop",
+ "productName": "Laptop",
  "price": 999.99
}
```

**OpenAPI atualizado:**
```diff
"Product": {
  "properties": {
    "id": { "type": "integer" },
-   "name": { "type": "string" },
+   "productName": { "type": "string" },
    "price": { "type": "number" }
  }
}
```

**ServiceA roda Kiota novamente:**
```bash
$ kiota generate -d ../ServiceB/openapi.json
```

**Código gerado muda:**
```diff
public class Product
{
    public int? Id { get; set; }
-   public string? Name { get; set; }
+   public string? ProductName { get; set; }
    public double? Price { get; set; }
}
```

**Código do ServiceA não compila mais:**
```csharp
var products = await client.Api.Products.GetAsync();

foreach (var product in products)
{
    Console.WriteLine(product.Name); // ❌ ERRO DE COMPILAÇÃO!
    //                        ^^^^
    // 'Product' does not contain a definition for 'Name'
}
```

**✅ Problema detectado ANTES do deploy!**

O dev precisa atualizar:
```csharp
Console.WriteLine(product.ProductName); // ✅ Corrigido
```

---

## 📋 Resumo Visual

### Sem Kiota (Tradicional)

```
ServiceB (API)
    ↓ (chamadas HTTP manuais)
ServiceA (Cliente)
    
❌ Problemas:
- URLs hardcoded ou em strings
- Deserialização manual
- Erros só em runtime
- Sem IntelliSense
- API muda → quebra em produção
```

### Com Kiota

```
ServiceB (API)
    ↓ openapi.json (contrato)
Kiota (gerador)
    ↓ gera código C#
ServiceA (Cliente com código gerado)
    
✅ Vantagens:
- Type-safe
- IntelliSense completo
- Erros em compile-time
- API muda → não compila
- Refactoring seguro
```

---

## 🎯 Exemplo Prático Completo

### 1. ServiceB expõe API

```csharp
// ServiceB/Program.cs
app.MapGet("/api/products", () => new[] 
{ 
    new Product { Id = 1, Name = "Laptop", Price = 999.99 }
});
```

### 2. ServiceB documenta em OpenAPI

```json
// ServiceB/openapi.json
{
  "paths": {
    "/api/products": {
      "get": {
        "responses": {
          "200": {
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": { "$ref": "#/components/schemas/Product" }
                }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "Product": {
        "properties": {
          "id": { "type": "integer" },
          "name": { "type": "string" },
          "price": { "type": "number" }
        }
      }
    }
  }
}
```

### 3. ServiceA gera cliente

```bash
cd ServiceA
kiota generate -l CSharp -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient
```

### 4. Kiota gera automaticamente

```csharp
// ServiceA/Generated/ServiceBClient/Models/Product.cs
public class Product
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public double? Price { get; set; }
}

// ServiceA/Generated/ServiceBClient/Api/Products/ProductsRequestBuilder.cs
public class ProductsRequestBuilder
{
    public async Task<List<Product>?> GetAsync() { ... }
}

// ServiceA/Generated/ServiceBClient/ApiClient.cs
public class ApiClient
{
    public ApiRequestBuilder Api { get; }
}
```

### 5. ServiceA usa o cliente gerado

```csharp
// ServiceA/Program.cs
app.MapGet("/products", async (ServiceBClientFactory factory) =>
{
    var client = await factory.CreateClientAsync();
    var products = await client.Api.Products.GetAsync();
    //                          ↑ IntelliSense aqui!
    return products;
});
```

---

## 💡 Analogia Final

**Kiota é como:**

Você tem um amigo (ServiceB) que tem uma loja. Ele te dá um catálogo (openapi.json) descrevendo:
- Quais produtos vende
- Quanto custam
- Como fazer pedidos

Kiota lê esse catálogo e cria um **assistente robô** (código gerado) que:
- Conhece todos os produtos
- Sabe os preços
- Faz os pedidos automaticamente para você
- Te avisa se algo mudar no catálogo

**Resultado:** Você não precisa decorar o catálogo nem fazer pedidos manualmente. Apenas diz ao robô: "me traga os produtos!" e ele faz tudo. 🤖

---

## 🎓 Conclusão

**Kiota:**
1. Lê um arquivo OpenAPI (contrato da API)
2. Gera código C# automaticamente
3. Cria classes, métodos, tudo tipado
4. Você usa esse código com IntelliSense
5. Se API mudar, código não compila (segurança!)

**Benefícios:**
- ✅ Zero boilerplate manual
- ✅ Type-safe (compile-time errors)
- ✅ IntelliSense completo
- ✅ Refactoring seguro
- ✅ Contratos validados em build-time

**Na POC:**
- ServiceA gera cliente para ServiceB
- ServiceB gera cliente para ServiceC
- Ambos usam descoberta automática para URLs
- Resultado: Type-safe + Zero hardcode! 🚀
