#!/bin/bash
set -e

echo "🚀 Generating OpenAPI from code..."

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
echo "✅ OpenAPI files generated successfully!"
echo ""
echo "💡 Next step: Regenerate Kiota clients"
echo "   cd ServiceA && kiota generate -d ../ServiceB/openapi.json -o ./Generated/ServiceBClient"
echo "   cd ServiceB && kiota generate -d ../ServiceC/openapi.json -o ./Generated/ServiceCClient"
