# 🧪 Testing Guide - POC Automatic Service Discovery

## ✅ POC Successfully Deployed!

This POC was successfully deployed and tested on k3d using **podman** and **dotnet publish**.

---

## 📦 What was deployed

### k3d Cluster
- **Name:** aspire-poc
- **Imported images:** ServiceA, ServiceB, ServiceC (via podman)

### Running Pods

```bash
kubectl get pods -A | grep service
```

**Result:**
```
NAMESPACE     NAME                        READY   STATUS    RESTARTS   AGE
orders-ns     servicec-8698cf4d67-7lpfp   1/1     Running   0          2m
orders-ns     servicec-8698cf4d67-pls2k   1/1     Running   0          2m
products-ns   serviceb-655d6bb6d6-5zspk   1/1     Running   0          2m
products-ns   serviceb-655d6bb6d6-c55n4   1/1     Running   0          2m
users-ns      servicea-5f8468f55-br9l6    1/1     Running   0          2m
users-ns      servicea-5f8468f55-k5cbh    1/1     Running   0          2m
```

### Services with Discovery Labels

```bash
kubectl get svc -A --show-labels | grep api-type
```

**Result:**
```
NAMESPACE     NAME       TYPE        CLUSTER-IP      PORT(S)   LABELS
users-ns      servicea   ClusterIP   10.43.172.236   80/TCP    api-type=users-api,api-version=v1
products-ns   serviceb   ClusterIP   10.43.18.49     80/TCP    api-type=products-api,api-version=v1
orders-ns     servicec   ClusterIP   10.43.29.101    80/TCP    api-type=orders-api,api-version=v1
```

---

## 🔍 Testing Automatic Discovery

### Test 1: ServiceA → ServiceB (Cross-Namespace)

**Command:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products/1
```

**Expected Result:**
```json
{
  "message": "ServiceA called ServiceB via service discovery",
  "discoveredUrl": "http://serviceb.products-ns.svc.cluster.local",
  "products": "[{\"id\":1,\"name\":\"Laptop\",\"price\":999.99},{\"id\":2,\"name\":\"Mouse\",\"price\":29.99}]"
}
```

**✅ Confirmation:**
- ServiceA discovered ServiceB automatically using label `api-type=products-api`
- URL built: `http://serviceb.products-ns.svc.cluster.local`
- Cross-namespace communication working

---

### Test 2: ServiceB → ServiceC (Cross-Namespace)

**Command:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://serviceb.products-ns/api/products/with-orders/1
```

**Expected Result:**
```json
{
  "message": "ServiceB called ServiceC via service discovery",
  "discoveredUrl": "http://servicec.orders-ns.svc.cluster.local",
  "orders": "[{\"id\":1,\"customerId\":1,\"total\":1299.98},{\"id\":2,\"customerId\":2,\"total\":59.98}]"
}
```

**✅ Confirmation:**
- ServiceB discovered ServiceC automatically using label `api-type=orders-api`
- URL built: `http://servicec.orders-ns.svc.cluster.local`
- Cross-namespace communication working

---

### Test 3: Check Discovery Logs

**ServiceA:**
```bash
kubectl logs -n users-ns -l app=servicea --tail=30 | grep -i discover
```

**Result:**
```
info: ServiceA.Infrastructure.KubernetesServiceDiscovery[0]
      Discovering service with api-type=products-api
info: ServiceA.Infrastructure.KubernetesServiceDiscovery[0]
      Discovered service: products-api -> http://serviceb.products-ns.svc.cluster.local (namespace: products-ns)
```

**ServiceB:**
```bash
kubectl logs -n products-ns -l app=serviceb --tail=30 | grep -i discover
```

**Result:**
```
info: ServiceB.Infrastructure.KubernetesServiceDiscovery[0]
      Discovering service with api-type=orders-api
info: ServiceB.Infrastructure.KubernetesServiceDiscovery[0]
      Discovered service: orders-api -> http://servicec.orders-ns.svc.cluster.local (namespace: orders-ns)
```

---

### Test 4: Discovered Services Catalog

**Command:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/services/catalog
```

**Expected Result:**
```json
{
  "discoveryMethod": "Kubernetes API",
  "services": {
    "users-api": "http://servicea.users-ns.svc.cluster.local",
    "products-api": "http://serviceb.products-ns.svc.cluster.local",
    "orders-api": "http://servicec.orders-ns.svc.cluster.local"
  }
}
```

---

## 🎯 Feature Confirmation

### ✅ Automatic Discovery
- [x] ServiceA discovers ServiceB via label `api-type=products-api`
- [x] ServiceB discovers ServiceC via label `api-type=orders-api`
- [x] URLs built automatically (no hardcode!)

### ✅ Cross-Namespace Communication
- [x] users-ns → products-ns: **Working**
- [x] products-ns → orders-ns: **Working**
- [x] Kubernetes DNS resolving correctly

### ✅ RBAC Permissions
- [x] ServiceAccounts created (servicea-sa, serviceb-sa, servicec-sa)
- [x] ClusterRole `service-discovery-reader` with permissions
- [x] ClusterRoleBindings connecting everything

### ✅ Labels and Metadata
- [x] Services with label `api-type`
- [x] Services with label `api-version`
- [x] Annotations documenting APIs

---

## 📊 Implemented Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│ 1. ServiceA (users-ns)                                           │
│    - Label: api-type=users-api                                   │
│    - ServiceAccount: servicea-sa                                 │
│    - Endpoint: /api/users/with-products/{id}                     │
│    │                                                              │
│    └─→ Discovers ServiceB via KubernetesClient                   │
│        Query: labelSelector="api-type=products-api"              │
│        Returns: http://serviceb.products-ns.svc.cluster.local    │
└──────────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│ 2. ServiceB (products-ns)                                        │
│    - Label: api-type=products-api                                │
│    - ServiceAccount: serviceb-sa                                 │
│    - Endpoint: /api/products/with-orders/{id}                    │
│    │                                                              │
│    └─→ Discovers ServiceC via KubernetesClient                   │
│        Query: labelSelector="api-type=orders-api"                │
│        Returns: http://servicec.orders-ns.svc.cluster.local      │
└──────────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│ 3. ServiceC (orders-ns)                                          │
│    - Label: api-type=orders-api                                  │
│    - ServiceAccount: servicec-sa                                 │
│    - Endpoint: /api/orders                                       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ How it was Built and Deployed

### 1. Build Images (dotnet publish)

```bash
# ServiceA
cd ServiceA
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/servicea -p ContainerImageTag=latest

# ServiceB
cd ServiceB
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/serviceb -p ContainerImageTag=latest

# ServiceC
cd ServiceC
dotnet publish -t:PublishContainer -p ContainerRepository=localhost:5555/servicec -p ContainerImageTag=latest
```

### 2. Create k3d Cluster

```bash
k3d cluster create aspire-poc --no-lb
```

### 3. Import Images

```bash
podman save localhost:5555/servicea:latest -o /tmp/servicea.tar
k3d image import /tmp/servicea.tar -c aspire-poc

podman save localhost:5555/serviceb:latest -o /tmp/serviceb.tar
k3d image import /tmp/serviceb.tar -c aspire-poc

podman save localhost:5555/servicec:latest -o /tmp/servicec.tar
k3d image import /tmp/servicec.tar -c aspire-poc
```

### 4. Deploy to Kubernetes

```bash
# Namespaces
kubectl apply -f k8s/00-namespaces.yaml

# RBAC (ServiceAccounts + ClusterRole + Bindings)
kubectl apply -f k8s/04-rbac.yaml

# Services
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml
```

---

## 🎓 Key Implementation Points

### 1. No Hardcoded URLs
The deployments **do not have** environment variables with URLs:
```yaml
# ❌ BEFORE (hardcoded)
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"

# ✅ NOW (automatic discovery)
env:
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"
# No URLs! Everything discovered via labels
```

### 2. KubernetesClient queries the K8s API
```csharp
var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
    labelSelector: $"api-type={apiType}"
);

var service = services.Items.FirstOrDefault();
var url = $"http://{service.Metadata.Name}.{service.Metadata.NamespaceProperty}.svc.cluster.local";
```

### 3. RBAC allows querying the API
```yaml
# ClusterRole
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
```

---

## 🧹 Cleanup

To clean up the environment:

```bash
# Delete k3d cluster
k3d cluster delete aspire-poc

# Remove local images
podman rmi localhost:5555/servicea:latest
podman rmi localhost:5555/serviceb:latest
podman rmi localhost:5555/servicec:latest
```

---

## 🚀 Conclusion

✅ **100% functional POC** demonstrating:

1. **Automatic discovery** via Labels + KubernetesClient
2. **Zero hardcoded** URLs
3. **Cross-namespace communication** working
4. **RBAC** correctly configured
5. **Simplified build** with `dotnet publish`
6. **Deploy on k3d** with podman

**Next steps:**
- Discovery cache for performance
- Health checks before returning URLs
- Discovery metrics (Prometheus)
- Watch API for real-time updates
