# 📄 OpenAPI: Automatic Generation as Artifact

## 🎯 Objective

Generate `openapi.json` files **automatically** from code and use them as **artifacts** in CI/CD pipelines to regenerate Kiota clients.

---

## 🚀 Recommended Solution: CI/CD Pipeline

### Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Developer changes API code (ServiceB)                        │
│    - Adds/removes endpoints                                     │
│    - Changes models, parameters, etc.                           │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. Commit & Push                                                 │
│    git commit -m "feat: add new endpoint"                       │
│    git push                                                      │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. CI/CD Pipeline (GitHub Actions)                              │
│    ├─ Build ServiceB                                            │
│    ├─ Run ServiceB temporarily                                  │
│    ├─ Extract OpenAPI from /openapi/v1.json                     │
│    ├─ Save as artifact: serviceb-openapi.json                   │
│    └─ Regenerate Kiota clients in consumers                     │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Validation                                                    │
│    ├─ Build consumers (ServiceA)                                │
│    ├─ If API broke contract → Build fails ✅                    │
│    └─ If OK → Commit updated clients                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 Implementation: GitHub Actions

### Full Workflow

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

## 🔧 Local Configuration (Development)

### Option 1: Manual Command

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

### Option 2: Simplified Script

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

## 📦 OpenAPI as Artifact

### Why artifacts?

1. **Versioning** - History of API changes
2. **Auditing** - Traceability of modifications
3. **Distribution** - Consumers download the updated spec
4. **CI/CD** - Subsequent jobs use the artifact

### How to use artifacts in GitHub Actions

```yaml
# Job 1: Generate OpenAPI
- name: Upload OpenAPI
  uses: actions/upload-artifact@v4
  with:
    name: openapi-specs-${{ github.sha }}
    path: |
      ServiceB/openapi.json
      ServiceC/openapi.json

# Job 2: Download and use
- name: Download OpenAPI
  uses: actions/download-artifact@v4
  with:
    name: openapi-specs-${{ github.sha }}
```

### Publish as Release Asset

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

## 🎯 Full Flow: Dev → Prod

```
┌─────────────────────────────────────────────────────────────────┐
│ DEV: Changes endpoint in ServiceB                               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Runs generate-openapi.sh                                 │
│        → ServiceB/openapi.json updated                          │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Regenerates Kiota client                                 │
│        kiota generate -d ServiceB/openapi.json                  │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LOCAL: Build                                                     │
│        If contract broke → Compile error ✅                     │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ GIT: Commit + Push                                               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ CI/CD: Pipeline runs                                             │
│        1. Generates OpenAPI again (validates)                   │
│        2. Saves as artifact                                      │
│        3. Regenerates clients                                    │
│        4. Validates breaking changes                             │
│        5. Builds all projects                                    │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ DEPLOY: If everything OK                                         │
│         Artifact published in Release                            │
│         External consumers can download                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Implementation Checklist

### Initial Setup

- [ ] Add `.github/workflows/openapi-sync.yml`
- [ ] Create `generate-openapi.sh` and `generate-openapi.ps1`
- [ ] Configure `ServiceB/Program.cs` with OpenAPI metadata
- [ ] Configure `ServiceC/Program.cs` with OpenAPI metadata

### Development

- [ ] Run `generate-openapi.sh` after changing APIs
- [ ] Regenerate Kiota clients
- [ ] Local build to validate
- [ ] Commit OpenAPI + generated clients

### CI/CD

- [ ] Pipeline generates OpenAPI automatically
- [ ] OpenAPI saved as artifact
- [ ] Clients regenerated automatically
- [ ] Breaking changes detected
- [ ] Build validates contracts

---

## 🎓 Recommendations

### ✅ Do

- Generate OpenAPI as artifact in CI/CD
- Version OpenAPI files in git (traceability)
- Use breaking change detection (oasdiff)
- Regenerate clients automatically in pipeline
- Validate contracts at build-time

### ❌ Don't

- Edit `openapi.json` manually
- Commit clients without regenerating
- Deploy without validating contracts
- Ignore breaking changes

---

## 📊 Comparison: Manual vs Pipeline

| Aspect | Manual | Pipeline (Artifact) |
|--------|--------|---------------------|
| **Generation** | Dev runs script | Automatic in CI |
| **Validation** | Dev must remember | Always executed |
| **History** | Git commits | Versioned artifacts |
| **Distribution** | Email, Slack | GitHub Releases |
| **Reliability** | Depends on dev | Guaranteed |
| **Breaking Changes** | Manual | Automatic (oasdiff) |

---

## 🚀 Next Steps

1. Implement workflow `.github/workflows/openapi-sync.yml`
2. Create scripts `generate-openapi.sh` and `.ps1`
3. Test locally
4. Commit and watch the pipeline run
5. Check artifacts in GitHub Actions

**Result:** OpenAPI always in sync, contracts validated, zero manual work! 🎉
