# 📄 OpenAPI: Geração Automática como Artefato

## 🎯 Objetivo

Gerar arquivos `openapi.json` **automaticamente** a partir do código e usá-los como **artefatos** em pipelines CI/CD para regenerar clientes Kiota.

---

## 🚀 Solução Recomendada: Pipeline CI/CD

### Arquitetura do Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Desenvolvedor altera código da API (ServiceB)                │
│    - Adiciona/remove endpoints                                  │
│    - Muda models, parâmetros, etc.                              │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. Commit & Push                                                 │
│    git commit -m "feat: add new endpoint"                       │
│    git push                                                      │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Pipeline CI/CD (GitHub Actions)                              │
│    ├─ Build ServiceB                                            │
│    ├─ Rodar ServiceB temporariamente                            │
│    ├─ Extrair OpenAPI de /openapi/v1.json                       │
│    ├─ Salvar como artefato: serviceb-openapi.json              │
│    └─ Regenerar clientes Kiota nos consumidores                 │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Validação                                                     │
│    ├─ Build dos consumidores (ServiceA)                         │
│    ├─ Se API quebrou contrato → Build falha ✅                  │
│    └─ Se OK → Commit clientes atualizados                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 Implementação: GitHub Actions

### Workflow Completo

**`.github/workflows/openapi-sync.yml`**

```yaml
name: OpenAPI Sync & Kiota Regeneration

on:
  push:
    branches: [main, master]
    paths:
      - 'ServiceB/Program.cs'
      - 'ServiceB/**/*.cs'
      - 'ServiceC/Program.cs'
      - 'ServiceC/**/*.cs'
  pull_request:
    branches: [main, master]

jobs:
  generate-openapi:
    name: Generate OpenAPI Documents
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      # ServiceB: Build & Generate OpenAPI
      - name: Build ServiceB
        run: dotnet build ServiceB/ServiceB.csproj --configuration Release
      
      - name: Start ServiceB
        run: |
          dotnet run --project ServiceB --no-build --urls "http://localhost:5002" &
          echo $! > serviceb.pid
          sleep 10
      
      - name: Extract OpenAPI from ServiceB
        run: |
          curl -f http://localhost:5002/openapi/v1.json -o ServiceB/openapi.json
          cat ServiceB/openapi.json | jq '.' # Validate JSON
      
      - name: Stop ServiceB
        run: kill $(cat serviceb.pid) || true
      
      # ServiceC: Build & Generate OpenAPI
      - name: Build ServiceC
        run: dotnet build ServiceC/ServiceC.csproj --configuration Release
      
      - name: Start ServiceC
        run: |
          dotnet run --project ServiceC --no-build --urls "http://localhost:5003" &
          echo $! > servicec.pid
          sleep 10
      
      - name: Extract OpenAPI from ServiceC
        run: |
          curl -f http://localhost:5003/openapi/v1.json -o ServiceC/openapi.json
          cat ServiceC/openapi.json | jq '.'
      
      - name: Stop ServiceC
        run: kill $(cat servicec.pid) || true
      
      # Upload OpenAPI as artifacts
      - name: Upload OpenAPI Artifacts
        uses: actions/upload-artifact@v4
        with:
          name: openapi-specs
          path: |
            ServiceB/openapi.json
            ServiceC/openapi.json
          retention-days: 90

  regenerate-clients:
    name: Regenerate Kiota Clients
    needs: generate-openapi
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Download OpenAPI Artifacts
        uses: actions/download-artifact@v4
        with:
          name: openapi-specs
      
      - name: Install Kiota
        run: dotnet tool install --global Microsoft.OpenApi.Kiota
      
      # Regenerate ServiceA client (consumes ServiceB)
      - name: Generate ServiceB Client
        run: |
          cd ServiceA
          kiota generate \
            -l CSharp \
            -d ../ServiceB/openapi.json \
            -o ./Generated/ServiceBClient \
            -n ServiceA.Generated.ServiceBClient \
            --clean-output
      
      # Regenerate ServiceB client (consumes ServiceC)
      - name: Generate ServiceC Client
        run: |
          cd ServiceB
          kiota generate \
            -l CSharp \
            -d ../ServiceC/openapi.json \
            -o ./Generated/ServiceCClient \
            -n ServiceB.Generated.ServiceCClient \
            --clean-output
      
      # Validate by building
      - name: Build All Projects
        run: dotnet build --configuration Release
      
      # Commit changes if on main branch
      - name: Commit regenerated clients
        if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/master'
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add ServiceA/Generated ServiceB/Generated ServiceB/openapi.json ServiceC/openapi.json
          git diff --staged --quiet || git commit -m "chore: regenerate Kiota clients from OpenAPI [skip ci]"
          git push

  validate-contracts:
    name: Validate API Contracts
    needs: generate-openapi
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Download OpenAPI Artifacts
        uses: actions/download-artifact@v4
        with:
          name: openapi-specs
      
      - name: Check for breaking changes
        run: |
          # Install oasdiff for breaking change detection
          npm install -g @oasdiff/oasdiff
          
          # Compare with previous version
          git fetch origin main:main || true
          
          if [ -f ServiceB/openapi.json ]; then
            echo "Checking ServiceB for breaking changes..."
            git show main:ServiceB/openapi.json > /tmp/old-serviceb.json || echo "{}" > /tmp/old-serviceb.json
            oasdiff breaking /tmp/old-serviceb.json ServiceB/openapi.json || echo "⚠️ Breaking changes detected in ServiceB"
          fi
```

---

## 🔧 Configuração Local (Desenvolvimento)

### Opção 1: Comando Manual

```bash
# ServiceB
cd ServiceB
dotnet run --urls "http://localhost:5002" &
curl http://localhost:5002/openapi/v1.json -o openapi.json
pkill -f ServiceB

# ServiceC
cd ServiceC
dotnet run --urls "http://localhost:5003" &
curl http://localhost:5003/openapi/v1.json -o openapi.json
pkill -f ServiceC
```

### Opção 2: Script Simplificado

**`generate-openapi.sh`**

```bash
#!/bin/bash
set -e

echo "🚀 Gerando OpenAPI a partir do código..."

# ServiceB
echo "📦 ServiceB..."
dotnet build ServiceB --configuration Release -v quiet
dotnet run --project ServiceB --no-build --urls "http://localhost:5002" > /dev/null 2>&1 &
PID_B=$!
sleep 8
curl -s http://localhost:5002/openapi/v1.json -o ServiceB/openapi.json
kill $PID_B 2>/dev/null || true
echo "✅ ServiceB/openapi.json"

# ServiceC
echo "📦 ServiceC..."
dotnet build ServiceC --configuration Release -v quiet
dotnet run --project ServiceC --no-build --urls "http://localhost:5003" > /dev/null 2>&1 &
PID_C=$!
sleep 8
curl -s http://localhost:5003/openapi/v1.json -o ServiceC/openapi.json
kill $PID_C 2>/dev/null || true
echo "✅ ServiceC/openapi.json"

echo ""
echo "✅ OpenAPI gerados com sucesso!"
```

**`generate-openapi.ps1`** (Windows)

```powershell
Write-Host "🚀 Gerando OpenAPI a partir do código..." -ForegroundColor Cyan

# ServiceB
Write-Host "📦 ServiceB..." -ForegroundColor Blue
dotnet build ServiceB --configuration Release -v quiet
$procB = Start-Process -FilePath "dotnet" -ArgumentList "run --project ServiceB --no-build --urls http://localhost:5002" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 8
Invoke-WebRequest -Uri "http://localhost:5002/openapi/v1.json" -OutFile "ServiceB/openapi.json"
Stop-Process -Id $procB.Id -Force
Write-Host "✅ ServiceB/openapi.json" -ForegroundColor Green

# ServiceC
Write-Host "📦 ServiceC..." -ForegroundColor Blue
dotnet build ServiceC --configuration Release -v quiet
$procC = Start-Process -FilePath "dotnet" -ArgumentList "run --project ServiceC --no-build --urls http://localhost:5003" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 8
Invoke-WebRequest -Uri "http://localhost:5003/openapi/v1.json" -OutFile "ServiceC/openapi.json"
Stop-Process -Id $procC.Id -Force
Write-Host "✅ ServiceC/openapi.json" -ForegroundColor Green

Write-Host ""
Write-Host "✅ OpenAPI gerados com sucesso!" -ForegroundColor Green
```

---

## 📦 OpenAPI como Artefato

### Por que artefatos?

1. **Versionamento** - Histórico de mudanças na API
2. **Auditoria** - Rastreabilidade de alterações
3. **Distribuição** - Consumidores baixam spec atualizada
4. **CI/CD** - Jobs seguintes usam o artefato

### Como usar artefatos no GitHub Actions

```yaml
# Job 1: Gera OpenAPI
- name: Upload OpenAPI
  uses: actions/upload-artifact@v4
  with:
    name: openapi-specs-${{ github.sha }}
    path: |
      ServiceB/openapi.json
      ServiceC/openapi.json

# Job 2: Baixa e usa
- name: Download OpenAPI
  uses: actions/download-artifact@v4
  with:
    name: openapi-specs-${{ github.sha }}
```

### Publicar como Release Asset

```yaml
- name: Create Release
  uses: softprops/action-gh-release@v1
  if: startsWith(github.ref, 'refs/tags/')
  with:
    files: |
      ServiceB/openapi.json
      ServiceC/openapi.json
```

---

## 🎯 Fluxo Completo: Dev → Prod

```
┌─────────────────────────────────────────────────────────────────┐
│ DEV: Altera endpoint em ServiceB                                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Roda generate-openapi.sh                                 │
│        → ServiceB/openapi.json atualizado                       │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Regenera cliente Kiota                                   │
│        kiota generate -d ServiceB/openapi.json                  │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Build                                                     │
│        Se quebrou contrato → Erro de compilação ✅              │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ GIT: Commit + Push                                               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ CI/CD: Pipeline roda                                             │
│        1. Gera OpenAPI novamente (valida)                       │
│        2. Salva como artefato                                    │
│        3. Regenera clientes                                      │
│        4. Valida breaking changes                                │
│        5. Build de todos os projetos                             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ DEPLOY: Se tudo OK                                               │
│         Artefato publicado em Release                            │
│         Consumidores externos podem baixar                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Checklist de Implementação

### Configuração Inicial

- [ ] Adicionar `.github/workflows/openapi-sync.yml`
- [ ] Criar `generate-openapi.sh` e `generate-openapi.ps1`
- [ ] Configurar `ServiceB/Program.cs` com OpenAPI metadata
- [ ] Configurar `ServiceC/Program.cs` com OpenAPI metadata

### Desenvolvimento

- [ ] Rodar `generate-openapi.sh` após mudar APIs
- [ ] Regenerar clientes Kiota
- [ ] Build local para validar
- [ ] Commit OpenAPI + clientes gerados

### CI/CD

- [ ] Pipeline gera OpenAPI automaticamente
- [ ] OpenAPI salvo como artefato
- [ ] Clientes regenerados automaticamente
- [ ] Breaking changes detectados
- [ ] Build valida contratos

---

## 🎓 Recomendações

### ✅ Faça

- Gere OpenAPI como artefato em CI/CD
- Version OpenAPI files no git (rastreabilidade)
- Use breaking change detection (oasdiff)
- Regenere clientes automaticamente em pipeline
- Valide contratos em build-time

### ❌ Não Faça

- Editar `openapi.json` manualmente
- Commitar clientes sem regenerar
- Deploy sem validar contratos
- Ignorar breaking changes

---

## 📊 Comparação: Manual vs Pipeline

| Aspecto | Manual | Pipeline (Artefato) |
|---------|--------|---------------------|
| **Geração** | Dev roda script | Automático em CI |
| **Validação** | Dev precisa lembrar | Sempre executado |
| **Histórico** | Git commits | Artefatos versionados |
| **Distribuição** | Email, Slack | GitHub Releases |
| **Confiança** | Depende do dev | Garantido |
| **Breaking Changes** | Manual | Automático (oasdiff) |

---

## 🚀 Próximos Passos

1. Implementar workflow `.github/workflows/openapi-sync.yml`
2. Criar scripts `generate-openapi.sh` e `.ps1`
3. Testar localmente
4. Fazer commit e ver pipeline rodar
5. Verificar artefatos no GitHub Actions

**Resultado:** OpenAPI sempre sincronizado, contratos validados, zero trabalho manual! 🎉
