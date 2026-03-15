Write-Host "🚀 Generating OpenAPI from code..." -ForegroundColor Cyan

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
Write-Host "✅ OpenAPI files generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Next step: Regenerate Kiota clients" -ForegroundColor Yellow
Write-Host "   cd ServiceA && kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient"
Write-Host "   cd ServiceB && kiota generate -d ../ServiceC/openapi.json -o ./Generated/ServiceCClient"
