# ⚡ Quick Start - 5 Minutes

Run the full POC in less than 5 minutes!

---

## 📋 Prerequisites

```bash
# Check versions
dotnet --version  # >= 10.0
kubectl version   # Kubernetes CLI
k3d version       # k3d for local cluster
```

**Not installed?**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [k3d](https://k3d.io/)

---

## 🚀 Option 1: Local Development (Aspire)

### 1. Clone the repository

```bash
git clone https://github.com/MatheusReichert/dotnet-service-discovery-kiota-poc.git
cd dotnet-service-discovery-kiota-poc
```

### 2. Run the Aspire AppHost

```bash
dotnet run apphost.cs
```

### 3. Access the Dashboard

Open in your browser: `https://localhost:17247/login?t=<token>`

The token appears in the console.

### 4. Test automatic discovery

In the dashboard, click **ServiceA** → Open the endpoint:

```
GET /api/users/with-products-typed/1
```

**Expected response:**
```json
{
  "message": "ServiceA → ServiceB usando Kiota + Descoberta Automática",
  "method": "Type-Safe Kiota Client",
  "products": [...]
}
```

✅ **Done!** Automatic discovery + Kiota working!

---

## ☸️ Option 2: Kubernetes (k3d)

### 1. Clone the repository

```bash
git clone https://github.com/MatheusReichert/dotnet-service-discovery-kiota-poc.git
cd dotnet-service-discovery-kiota-poc
```

### 2. Build images

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

### 3. Create k3d cluster and import images

```bash
# Create cluster
k3d cluster create aspire-poc

# Import images
podman save localhost:5555/servicea:latest -o /tmp/servicea.tar
k3d image import /tmp/servicea.tar -c aspire-poc

podman save localhost:5555/serviceb:latest -o /tmp/serviceb.tar
k3d image import /tmp/serviceb.tar -c aspire-poc

podman save localhost:5555/servicec:latest -o /tmp/servicec.tar
k3d image import /tmp/servicec.tar -c aspire-poc
```

### 4. Deploy to Kubernetes

```bash
kubectl apply -f k8s/00-namespaces.yaml
kubectl apply -f k8s/04-rbac.yaml
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

### 5. Verify pods

```bash
kubectl get pods -A | grep service
```

Wait for all pods to be `Running (1/1)`.

### 6. Test automatic discovery

```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products-typed/1
```

**Expected response:**
```json
{
  "message": "ServiceA → ServiceB usando Kiota + Descoberta Automática",
  "products": [...]
}
```

✅ **Done!** POC running on Kubernetes with automatic discovery!

---

## 📊 What did you just see?

### Automatic Discovery
- ServiceA discovered ServiceB via label `api-type=products-api`
- URL built automatically: `http://serviceb.products-ns.svc.cluster.local`
- **Zero hardcoded URLs!**

### Kiota Type-Safe
- Client generated automatically from OpenAPI
- Full IntelliSense
- Errors detected at compile-time

### Cross-Namespace
- ServiceA (users-ns) → ServiceB (products-ns) → ServiceC (orders-ns)
- Transparent communication between namespaces

---

## 🎯 Next Steps

Now that you've run the POC, explore:

1. **[KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md)** - Understand how Kiota works
2. **[AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md)** - How discovery works
3. **[INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md)** - How everything integrates
4. **[OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md)** - CI/CD Pipeline

---

## 🐛 Problems?

### Aspire won't start
```bash
# Check .NET SDK
dotnet --version

# Clean and rebuild
dotnet clean
dotnet build
```

### Pods won't come up in Kubernetes
```bash
# View logs
kubectl logs -n users-ns -l app=servicea

# Describe pod
kubectl describe pod -n users-ns <pod-name>
```

### Discovery not working
```bash
# Check RBAC
kubectl get clusterrole service-discovery-reader
kubectl get clusterrolebinding | grep discovery

# Check labels
kubectl get svc -A --show-labels | grep api-type
```

---

## 📚 Full Documentation

See: **[DOCUMENTATION-INDEX.md](DOCUMENTATION-INDEX.md)**

---

**Total time:** ~5 minutes ⚡

**Difficulty:** Easy 🟢

**Result:** Full POC working! 🎉
