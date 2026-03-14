# Script PowerShell para gerar clientes Kiota a partir de OpenAPI gerado automaticamente

Write-Host "🚀 Gerando clientes Kiota a partir de OpenAPI gerado automaticamente..." -ForegroundColor Cyan
Write-Host ""

function Wait-ForService {
    param([int]$Port)

    $maxAttempts = 30
    $attempt = 0

    while ($attempt -lt $maxAttempts) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$Port/openapi/v1.json" -UseBasicParsing -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ Serviço pronto na porta $Port" -ForegroundColor Green
                return $true
            }
        }
        catch {
            $attempt++
            Start-Sleep -Seconds 1
        }
    }

    Write-Host "❌ Timeout esperando serviço na porta $Port" -ForegroundColor Red
    return $false
}

# Limpar processos anteriores
Write-Host "🧹 Limpando processos anteriores..." -ForegroundColor Yellow
Get-Process | Where-Object { $_.ProcessName -like "*ServiceB*" -or $_.ProcessName -like "*ServiceC*" } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Build dos projetos
Write-Host "📦 Building projetos..." -ForegroundColor Blue
dotnet build --nologo -v quiet

# ServiceC → ServiceB
Write-Host ""
Write-Host "📦 Gerando cliente ServiceC para ServiceB..." -ForegroundColor Blue
Write-Host "   1. Iniciando ServiceC..."

$serviceCProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project ServiceC --no-build --urls http://localhost:5003" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2

Write-Host "   2. Aguardando ServiceC ficar pronto..."
if (-not (Wait-ForService -Port 5003)) {
    $serviceCProcess | Stop-Process -Force
    exit 1
}

Write-Host "   3. Baixando OpenAPI de ServiceC..."
Invoke-WebRequest -Uri "http://localhost:5003/openapi/v1.json" -OutFile "ServiceC/openapi.json"
Write-Host "   ✅ OpenAPI salvo em ServiceC/openapi.json" -ForegroundColor Green

Write-Host "   4. Gerando cliente Kiota..."
Push-Location ServiceB
kiota generate -l CSharp -d ../ServiceC/openapi.json -o ./Generated/ServiceCClient -n ServiceB.Generated.ServiceCClient --clean-output | Out-Null
Pop-Location
Write-Host "   ✅ Cliente ServiceC gerado para ServiceB" -ForegroundColor Green

Write-Host "   5. Parando ServiceC..."
$serviceCProcess | Stop-Process -Force
Start-Sleep -Seconds 1

# ServiceB → ServiceA
Write-Host ""
Write-Host "📦 Gerando cliente ServiceB para ServiceA..." -ForegroundColor Blue
Write-Host "   1. Iniciando ServiceB..."

$serviceBProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project ServiceB --no-build --urls http://localhost:5002" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2

Write-Host "   2. Aguardando ServiceB ficar pronto..."
if (-not (Wait-ForService -Port 5002)) {
    $serviceBProcess | Stop-Process -Force
    exit 1
}

Write-Host "   3. Baixando OpenAPI de ServiceB..."
Invoke-WebRequest -Uri "http://localhost:5002/openapi/v1.json" -OutFile "ServiceB/openapi.json"
Write-Host "   ✅ OpenAPI salvo em ServiceB/openapi.json" -ForegroundColor Green

Write-Host "   4. Gerando cliente Kiota..."
Push-Location ServiceA
kiota generate -l CSharp -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient -n ServiceA.Generated.ServiceBClient --clean-output | Out-Null
Pop-Location
Write-Host "   ✅ Cliente ServiceB gerado para ServiceA" -ForegroundColor Green

Write-Host "   5. Parando ServiceB..."
$serviceBProcess | Stop-Process -Force

# Cleanup
Write-Host ""
Write-Host "🧹 Limpando processos..." -ForegroundColor Yellow
Get-Process | Where-Object { $_.ProcessName -like "*ServiceB*" -or $_.ProcessName -like "*ServiceC*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Rebuild para validar
Write-Host ""
Write-Host "🔨 Validando clientes gerados..." -ForegroundColor Blue
dotnet build --nologo -v quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Clientes Kiota gerados e validados com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📁 Arquivos gerados:" -ForegroundColor Cyan
    Write-Host "   - ServiceB/openapi.json"
    Write-Host "   - ServiceB/Generated/ServiceCClient/"
    Write-Host "   - ServiceC/openapi.json"
    Write-Host "   - ServiceA/Generated/ServiceBClient/"
    Write-Host ""
    Write-Host "💡 Os clientes foram gerados a partir do código real!" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ Erro ao validar clientes gerados" -ForegroundColor Red
    exit 1
}
