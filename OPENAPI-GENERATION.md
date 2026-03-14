# 📄 Geração Automática de OpenAPI

## 🎯 Situação Atual da POC

**Status:** Os arquivos `openapi.json` foram criados **manualmente**.

```
ServiceB/openapi.json  ← ❌ Criado manualmente
ServiceC/openapi.json  ← ❌ Criado manualmente
```

**Problema:** Se a API mudar, precisa atualizar o JSON manualmente (trabalhoso e propenso a erros).

---

## ✅ Solução: Geração Automática

.NET tem suporte nativo para gerar OpenAPI automaticamente a partir do código!

### Opção 1: Microsoft.AspNetCore.OpenApi (Nativo .NET 9+)

Esta POC já tem o package instalado:

```xml
<!-- ServiceB/ServiceB.csproj -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
```

#### Passo 1: Configurar geração de OpenAPI

```csharp
// ServiceB/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Mapear endpoint do OpenAPI
app.MapOpenApi(); // ← Expõe em /openapi/v1.json

app.MapScalarApiReference(); // ← UI interativa

app.Run();
```

#### Passo 2: Acessar OpenAPI gerado

```bash
# Rodar a aplicação
dotnet run --project ServiceB

# OpenAPI está disponível em:
curl http://localhost:5000/openapi/v1.json > ServiceB/openapi.json
```

**Resultado:** OpenAPI gerado automaticamente do código! 🎉

---

### Opção 2: Gerar OpenAPI em Build-Time

Para gerar o arquivo **durante o build** (sem rodar a aplicação):

#### Instalar ferramenta

```bash
dotnet tool install --global Microsoft.dotnet-openapi
```

#### Adicionar ao projeto

```xml
<!-- ServiceB/ServiceB.csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>
```

```csharp
// ServiceB/Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
```

#### Gerar arquivo durante build

Adicionar ao `.csproj`:

```xml
<Target Name="GenerateOpenApiSpec" AfterTargets="Build">
  <Exec Command="dotnet tool restore" />
  <Exec Command="dotnet swagger tofile --output $(ProjectDir)openapi.json $(OutputPath)$(AssemblyName).dll v1" />
</Target>
```

---

### Opção 3: NSwag (Mais Completo)

NSwag pode gerar OpenAPI em build-time sem rodar a aplicação.

#### Instalar package

```bash
dotnet add package NSwag.AspNetCore
dotnet tool install --global NSwag.ConsoleCore
```

#### Configurar

```csharp
// ServiceB/Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "ServiceB API";
    config.Version = "v1";
});

var app = builder.Build();

app.UseOpenApi(); // Serve o OpenAPI
app.UseSwaggerUi(); // UI
```

#### Gerar arquivo

```bash
# Durante desenvolvimento
dotnet build
nswag run

# Ou adicionar ao .csproj
```

**nswag.json:**
```json
{
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "aspNetCoreToOpenApi": {
      "project": "ServiceB.csproj",
      "output": "openapi.json"
    }
  }
}
```

---

## 🚀 Implementação Recomendada para Esta POC

Vou atualizar **ServiceB** e **ServiceC** para gerar OpenAPI automaticamente:

### ServiceB/Program.cs

```csharp
using Scalar.AspNetCore;
using ServiceB.Infrastructure;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
});

builder.Services.AddSingleton<IKubernetesServiceDiscovery, KubernetesServiceDiscovery>();
builder.Services.AddScoped<ServiceCClientFactory>();

// ✅ Configurar OpenAPI com metadados
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
});

var app = builder.Build();

// ✅ Mapear OpenAPI
app.MapOpenApi(); // Disponível em /openapi/v1.json

app.MapScalarApiReference();

// Endpoints...
app.MapGet("/api/products", () => 
{
    return new[] 
    { 
        new { Id = 1, Name = "Laptop", Price = 999.99 },
        new { Id = 2, Name = "Mouse", Price = 29.99 }
    };
})
.WithName("GetProducts")
.WithSummary("List all products")
.WithDescription("Returns a list of all available products")
.Produces<object[]>(200);

app.Run();
```

### Como Usar

#### 1. Durante Desenvolvimento

```bash
# Rodar ServiceB
dotnet run --project ServiceB

# Em outro terminal, baixar OpenAPI gerado
curl http://localhost:5000/openapi/v1.json -o ServiceB/openapi.json

# Gerar cliente Kiota
cd ServiceA
kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient
```

#### 2. Automatizar com Script

**generate-clients.sh:**
```bash
#!/bin/bash

echo "🚀 Gerando clientes Kiota..."

# ServiceB → ServiceA
echo "📦 ServiceB client para ServiceA..."
dotnet run --project ServiceB --no-build & 
sleep 5
curl -s http://localhost:5000/openapi/v1.json -o ServiceB/openapi.json
pkill -f ServiceB

cd ServiceA
kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient --clean-output
cd ..

# ServiceC → ServiceB
echo "📦 ServiceC client para ServiceB..."
dotnet run --project ServiceC --no-build &
sleep 5
curl -s http://localhost:5000/openapi/v1.json -o ServiceC/openapi.json
pkill -f ServiceC

cd ServiceB
kiota generate -d ../ServiceC/openapi.json -o ./Generated/ServiceCClient --clean-output
cd ..

echo "✅ Clientes gerados com sucesso!"
```

#### 3. Automatizar em CI/CD

**GitHub Actions:**
```yaml
name: Update Kiota Clients

on:
  push:
    paths:
      - 'ServiceB/Program.cs'
      - 'ServiceC/Program.cs'

jobs:
  regenerate-clients:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Install Kiota
        run: dotnet tool install --global Microsoft.OpenApi.Kiota
      
      - name: Run ServiceB and capture OpenAPI
        run: |
          dotnet run --project ServiceB &
          sleep 10
          curl http://localhost:5000/openapi/v1.json -o ServiceB/openapi.json
          pkill -f ServiceB
      
      - name: Generate ServiceA client
        run: |
          cd ServiceA
          kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient
      
      - name: Commit changes
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add .
          git commit -m "chore: regenerate Kiota clients" || echo "No changes"
          git push
```

---

## 🎯 Workflow Completo: Código → OpenAPI → Kiota

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Dev atualiza endpoint em ServiceB                            │
│    app.MapGet("/api/products/{id}", ...)                        │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. Build/Run ServiceB                                           │
│    dotnet run --project ServiceB                                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. OpenAPI gerado automaticamente                               │
│    GET http://localhost:5000/openapi/v1.json                    │
│    {                                                             │
│      "paths": {                                                  │
│        "/api/products/{id}": { ... }                            │
│      }                                                           │
│    }                                                             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Salvar OpenAPI em arquivo                                    │
│    curl ... > ServiceB/openapi.json                             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. Kiota regenera cliente                                       │
│    kiota generate -d ServiceB/openapi.json                      │
│    - Atualiza Models/                                            │
│    - Atualiza RequestBuilders/                                   │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. ServiceA build                                                │
│    Se API mudou incompativelmente → ERRO DE COMPILAÇÃO ✅       │
│    Dev precisa atualizar código antes de deployar               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Comparação: Manual vs Automático

| Aspecto | Manual (atual POC) | Automático |
|---------|-------------------|------------|
| **Criação** | Dev escreve JSON | Gerado do código |
| **Atualização** | Manual após mudanças | Automático |
| **Sincronização** | Pode desatualizar | Sempre sincronizado |
| **Erros** | Typos no JSON | Impossível |
| **Manutenção** | Alta | Baixa |
| **CI/CD** | Difícil validar | Fácil automatizar |

---

## 🎓 Recomendação

Para **projetos reais**, sempre use geração automática:

**Desenvolvimento:**
```bash
# Terminal 1: Rodar API
dotnet run --project ServiceB

# Terminal 2: Regenerar quando mudar
dotnet watch run --project ServiceB
# Quando mudar, rodar:
curl http://localhost:5000/openapi/v1.json -o ServiceB/openapi.json
kiota generate -d ../ServiceB/openapi.json
```

**Produção/CI:**
- GitHub Actions regenera clientes automaticamente
- Commit verifica se clientes estão atualizados
- Build quebra se contrato mudar sem atualizar cliente

---

## ✅ Próximos Passos para Esta POC

1. Atualizar ServiceB/Program.cs com geração OpenAPI
2. Atualizar ServiceC/Program.cs com geração OpenAPI
3. Criar script `generate-clients.sh`
4. Adicionar GitHub Actions workflow
5. Deletar openapi.json manuais (gerados automaticamente)

**Resultado:** API muda → OpenAPI atualiza → Kiota regenera → Build valida! 🚀
