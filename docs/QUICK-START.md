# ⚡ Quick Start - 5 Minutos

Rode a POC completa em menos de 5 minutos!

---

## 📋 Pré-requisitos

```bash
# Verificar versões
dotnet --version  # >= 10.0
kubectl version   # Kubernetes CLI
k3d version       # k3d para cluster local
```

**Não tem instalado?**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [k3d](https://k3d.io/)

---

## 🚀 Opção 1: Desenvolvimento Local (Aspire)

### 1. Clone o repositório

```bash
git clone https://github.com/MatheusReichert/dotnet-service-discovery-kiota-poc.git
cd dotnet-service-discovery-kiota-poc
```

### 2. Rode o Aspire AppHost

```bash
dotnet run apphost.cs
```

### 3. Acesse o Dashboard

Abra no navegador: `https://localhost:17247/login?t=<token>`

O token aparece no console.

### 4. Teste a descoberta automática

No dashboard, clique em **ServiceA** → Abra o endpoint:

```
GET /api/users/with-products-typed/1
```

**Resposta esperada:**
```json
{
  "message": "ServiceA → ServiceB usando Kiota + Descoberta Automática",
  "method": "Type-Safe Kiota Client",
  "products": [...]
}
```

✅ **Pronto!** Descoberta automática + Kiota funcionando!

---

## ☸️ Opção 2: Kubernetes (k3d)

### 1. Clone o repositório

```bash
git clone https://github.com/MatheusReichert/dotnet-service-discovery-kiota-poc.git
cd dotnet-service-discovery-kiota-poc
```

### 2. Build das imagens

```bash
# ServiceA
cd ServiceA
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/servicea

# ServiceB
cd ../ServiceB
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/serviceb

# ServiceC
cd ../ServiceC
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/servicec
cd ..
```

### 3. Criar cluster k3d e importar imagens

```bash
# Criar cluster
k3d cluster create aspire-poc

# Importar imagens
podman save localhost:5555/servicea:latest -o /tmp/servicea.tar
k3d image import /tmp/servicea.tar -c aspire-poc

podman save localhost:5555/serviceb:latest -o /tmp/serviceb.tar
k3d image import /tmp/serviceb.tar -c aspire-poc

podman save localhost:5555/servicec:latest -o /tmp/servicec.tar
k3d image import /tmp/servicec.tar -c aspire-poc
```

### 4. Deploy no Kubernetes

```bash
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/04-rbac.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

### 5. Verificar pods

```bash
kubectl get pods -A | grep service
```

Aguarde todos os pods ficarem `Running (1/1)`.

### 6. Testar descoberta automática

```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products-typed/1
```

**Resposta esperada:**
```json
{
  "message": "ServiceA → ServiceB usando Kiota + Descoberta Automática",
  "products": [...]
}
```

✅ **Pronto!** POC rodando no Kubernetes com descoberta automática!

---

## 📊 O que você acabou de ver?

### Descoberta Automática
- ServiceA descobriu ServiceB via label `api-type=products-api`
- URL construída automaticamente: `http://serviceb.products-ns.svc.cluster.local`
- **Zero URLs hardcoded!**

### Kiota Type-Safe
- Cliente gerado automaticamente do OpenAPI
- IntelliSense completo
- Erros detectados em compile-time

### Cross-Namespace
- ServiceA (users-ns) → ServiceB (products-ns) → ServiceC (orders-ns)
- Comunicação transparente entre namespaces

---

## 🎯 Próximos Passos

Agora que você rodou a POC, explore:

1. **[KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md)** - Entender como Kiota funciona
2. **[AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md)** - Como funciona a descoberta
3. **[INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md)** - Como tudo se integra
4. **[OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md)** - Pipeline CI/CD

---

## 🐛 Problemas?

### Aspire não inicia
```bash
# Verificar .NET SDK
dotnet --version

# Limpar e recompilar
dotnet clean
dotnet build
```

### Pods não sobem no Kubernetes
```bash
# Ver logs
kubectl logs -n users-ns -l app=servicea

# Descrever pod
kubectl describe pod -n users-ns <pod-name>
```

### Descoberta não funciona
```bash
# Verificar RBAC
kubectl get clusterrole service-discovery-reader
kubectl get clusterrolebinding | grep discovery

# Verificar labels
kubectl get svc -A --show-labels | grep api-type
```

---

## 📚 Documentação Completa

Ver: **[DOCUMENTATION-INDEX.md](DOCUMENTATION-INDEX.md)**

---

**Tempo total:** ~5 minutos ⚡

**Dificuldade:** Fácil 🟢

**Resultado:** POC completa funcionando! 🎉
