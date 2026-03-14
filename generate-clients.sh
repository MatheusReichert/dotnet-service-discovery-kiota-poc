#!/bin/bash
set -e

echo "🚀 Gerando clientes Kiota a partir de OpenAPI gerado automaticamente..."
echo ""

# Cores
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para esperar serviço estar pronto
wait_for_service() {
    local port=$1
    local max_attempts=30
    local attempt=0

    while ! curl -s http://localhost:$port/openapi/v1.json > /dev/null; do
        attempt=$((attempt + 1))
        if [ $attempt -eq $max_attempts ]; then
            echo "❌ Timeout esperando serviço na porta $port"
            return 1
        fi
        sleep 1
    done
    echo "✅ Serviço pronto na porta $port"
}

# Limpar processos anteriores
echo "🧹 Limpando processos anteriores..."
pkill -f "ServiceB.dll" 2>/dev/null || true
pkill -f "ServiceC.dll" 2>/dev/null || true
sleep 2

# Build dos projetos
echo -e "${BLUE}📦 Building projetos...${NC}"
dotnet build --nologo -v quiet

# ServiceC → ServiceB
echo ""
echo -e "${BLUE}📦 Gerando cliente ServiceC para ServiceB...${NC}"
echo "   1. Iniciando ServiceC..."
dotnet run --project ServiceC --no-build --urls "http://localhost:5003" > /dev/null 2>&1 &
SERVICEC_PID=$!

echo "   2. Aguardando ServiceC ficar pronto..."
wait_for_service 5003

echo "   3. Baixando OpenAPI de ServiceC..."
curl -s http://localhost:5003/openapi/v1.json -o ServiceC/openapi.json
echo -e "${GREEN}   ✅ OpenAPI salvo em ServiceC/openapi.json${NC}"

echo "   4. Gerando cliente Kiota..."
cd ServiceB
kiota generate -l CSharp \
  -d ../ServiceC/openapi.json \
  -o ./Generated/ServiceCClient \
  -n ServiceB.Generated.ServiceCClient \
  --clean-output > /dev/null 2>&1
cd ..
echo -e "${GREEN}   ✅ Cliente ServiceC gerado para ServiceB${NC}"

echo "   5. Parando ServiceC..."
kill $SERVICEC_PID 2>/dev/null || true
sleep 1

# ServiceB → ServiceA
echo ""
echo -e "${BLUE}📦 Gerando cliente ServiceB para ServiceA...${NC}"
echo "   1. Iniciando ServiceB..."
dotnet run --project ServiceB --no-build --urls "http://localhost:5002" > /dev/null 2>&1 &
SERVICEB_PID=$!

echo "   2. Aguardando ServiceB ficar pronto..."
wait_for_service 5002

echo "   3. Baixando OpenAPI de ServiceB..."
curl -s http://localhost:5002/openapi/v1.json -o ServiceB/openapi.json
echo -e "${GREEN}   ✅ OpenAPI salvo em ServiceB/openapi.json${NC}"

echo "   4. Gerando cliente Kiota..."
cd ServiceA
kiota generate -l CSharp \
  -d ../ServiceB/openapi.json \
  -o ./Generated/ServiceBClient \
  -n ServiceA.Generated.ServiceBClient \
  --clean-output > /dev/null 2>&1
cd ..
echo -e "${GREEN}   ✅ Cliente ServiceB gerado para ServiceA${NC}"

echo "   5. Parando ServiceB..."
kill $SERVICEB_PID 2>/dev/null || true

# Cleanup
echo ""
echo "🧹 Limpando processos..."
pkill -f "ServiceB.dll" 2>/dev/null || true
pkill -f "ServiceC.dll" 2>/dev/null || true

# Rebuild para validar
echo ""
echo -e "${BLUE}🔨 Validando clientes gerados...${NC}"
dotnet build --nologo -v quiet

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ Clientes Kiota gerados e validados com sucesso!${NC}"
    echo ""
    echo "📁 Arquivos gerados:"
    echo "   - ServiceB/openapi.json"
    echo "   - ServiceB/Generated/ServiceCClient/"
    echo "   - ServiceC/openapi.json"
    echo "   - ServiceA/Generated/ServiceBClient/"
    echo ""
    echo "💡 Os clientes foram gerados a partir do código real!"
else
    echo ""
    echo "❌ Erro ao validar clientes gerados"
    exit 1
fi
