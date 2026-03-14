# 🧪 Testing Guide - POC Automatic Service Discovery

## ✅ POC Deployada com Sucesso!

Esta POC foi deployada e testada com sucesso no k3d usando **podman** e **dotnet publish**.

---

## 📦 O que foi deployado

### Cluster k3d
- **Nome:** aspire-poc
- **Imagens importadas:** ServiceA, ServiceB, ServiceC (via podman)

### Pods Rodando

```bash
kubectl get pods -A | grep service
```

**Resultado:**
```
NAMESPACE     NAME                        READY   STATUS    RESTARTS   AGE
orders-ns     servicec-8698cf4d67-7lpfp   1/1     Running   0          2m
orders-ns     servicec-8698cf4d67-pls2k   1/1     Running   0          2m
products-ns   serviceb-655d6bb6d6-5zspk   1/1     Running   0          2m
products-ns   serviceb-655d6bb6d6-c55n4   1/1     Running   0          2m
users-ns      servicea-5f8468f55-br9l6    1/1     Running   0          2m
users-ns      servicea-5f8468f55-k5cbh    1/1     Running   0          2m
```

### Services com Labels para Descoberta

```bash
kubectl get svc -A --show-labels | grep api-type
```

**Resultado:**
```
NAMESPACE     NAME       TYPE        CLUSTER-IP      PORT(S)   LABELS
users-ns      servicea   ClusterIP   10.43.172.236   80/TCP    api-type=users-api,api-version=v1
products-ns   serviceb   ClusterIP   10.43.18.49     80/TCP    api-type=products-api,api-version=v1
orders-ns     servicec   ClusterIP   10.43.29.101    80/TCP    api-type=orders-api,api-version=v1
```

---

## 🔍 Testando a Descoberta Automática

### Teste 1: ServiceA → ServiceB (Cross-Namespace)

**Comando:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/users/with-products/1
```

**Resultado Esperado:**
```json
{
  "message": "ServiceA called ServiceB via service discovery",
  "discoveredUrl": "http://serviceb.products-ns.svc.cluster.local",
  "products": "[{\"id\":1,\"name\":\"Laptop\",\"price\":999.99},{\"id\":2,\"name\":\"Mouse\",\"price\":29.99}]"
}
```

**✅ Confirmação:**
- ServiceA descobriu ServiceB automaticamente usando label `api-type=products-api`
- URL construída: `http://serviceb.products-ns.svc.cluster.local`
- Comunicação cross-namespace funcionando

---

### Teste 2: ServiceB → ServiceC (Cross-Namespace)

**Comando:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://serviceb.products-ns/api/products/with-orders/1
```

**Resultado Esperado:**
```json
{
  "message": "ServiceB called ServiceC via service discovery",
  "discoveredUrl": "http://servicec.orders-ns.svc.cluster.local",
  "orders": "[{\"id\":1,\"customerId\":1,\"total\":1299.98},{\"id\":2,\"customerId\":2,\"total\":59.98}]"
}
```

**✅ Confirmação:**
- ServiceB descobriu ServiceC automaticamente usando label `api-type=orders-api`
- URL construída: `http://servicec.orders-ns.svc.cluster.local`
- Comunicação cross-namespace funcionando

---

### Teste 3: Verificar Logs da Descoberta

**ServiceA:**
```bash
kubectl logs -n users-ns -l app=servicea --tail=30 | grep -i discover
```

**Resultado:**
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

**Resultado:**
```
info: ServiceB.Infrastructure.KubernetesServiceDiscovery[0]
      Discovering service with api-type=orders-api
info: ServiceB.Infrastructure.KubernetesServiceDiscovery[0]
      Discovered service: orders-api -> http://servicec.orders-ns.svc.cluster.local (namespace: orders-ns)
```

---

### Teste 4: Catálogo de Serviços Descobertos

**Comando:**
```bash
kubectl run curl-test --image=curlimages/curl:latest --rm -i --restart=Never -- \
  curl -s http://servicea.users-ns/api/services/catalog
```

**Resultado Esperado:**
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

## 🎯 Confirmação de Funcionalidades

### ✅ Descoberta Automática
- [x] ServiceA descobre ServiceB via label `api-type=products-api`
- [x] ServiceB descobre ServiceC via label `api-type=orders-api`
- [x] URLs construídas automaticamente (sem hardcode!)

### ✅ Cross-Namespace Communication
- [x] users-ns → products-ns: **Funcionando**
- [x] products-ns → orders-ns: **Funcionando**
- [x] DNS do Kubernetes resolvendo corretamente

### ✅ RBAC Permissions
- [x] ServiceAccounts criados (servicea-sa, serviceb-sa, servicec-sa)
- [x] ClusterRole `service-discovery-reader` com permissões
- [x] ClusterRoleBindings conectando tudo

### ✅ Labels e Metadata
- [x] Services com label `api-type`
- [x] Services com label `api-version`
- [x] Annotations documentando APIs

---

## 📊 Arquitetura Implementada

```
┌──────────────────────────────────────────────────────────────────┐
│ 1. ServiceA (users-ns)                                           │
│    - Label: api-type=users-api                                   │
│    - ServiceAccount: servicea-sa                                 │
│    - Endpoint: /api/users/with-products/{id}                     │
│    │                                                              │
│    └─→ Descobre ServiceB via KubernetesClient                    │
│        Query: labelSelector="api-type=products-api"              │
│        Retorna: http://serviceb.products-ns.svc.cluster.local    │
└──────────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│ 2. ServiceB (products-ns)                                        │
│    - Label: api-type=products-api                                │
│    - ServiceAccount: serviceb-sa                                 │
│    - Endpoint: /api/products/with-orders/{id}                    │
│    │                                                              │
│    └─→ Descobre ServiceC via KubernetesClient                    │
│        Query: labelSelector="api-type=orders-api"                │
│        Retorna: http://servicec.orders-ns.svc.cluster.local      │
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

## 🛠️ Como foi Build e Deploy

### 1. Build das Imagens (dotnet publish)

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

### 2. Criar Cluster k3d

```bash
k3d cluster create aspire-poc --no-lb
```

### 3. Importar Imagens

```bash
podman save localhost:5555/servicea:latest -o /tmp/servicea.tar
k3d image import /tmp/servicea.tar -c aspire-poc

podman save localhost:5555/serviceb:latest -o /tmp/serviceb.tar
k3d image import /tmp/serviceb.tar -c aspire-poc

podman save localhost:5555/servicec:latest -o /tmp/servicec.tar
k3d image import /tmp/servicec.tar -c aspire-poc
```

### 4. Deploy no Kubernetes

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

## 🎓 Pontos-Chave da Implementação

### 1. Sem URLs Hardcoded
Os deployments **não têm** variáveis de ambiente com URLs:
```yaml
# ❌ ANTES (hardcoded)
env:
- name: Services__ServiceB__Url
  value: "http://serviceb.products-ns.svc.cluster.local"

# ✅ AGORA (descoberta automática)
env:
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"
# Sem URLs! Tudo descoberto via labels
```

### 2. KubernetesClient consulta K8s API
```csharp
var services = await _client.CoreV1.ListServiceForAllNamespacesAsync(
    labelSelector: $"api-type={apiType}"
);

var service = services.Items.FirstOrDefault();
var url = $"http://{service.Metadata.Name}.{service.Metadata.NamespaceProperty}.svc.cluster.local";
```

### 3. RBAC permite consulta à API
```yaml
# ClusterRole
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "list", "watch"]
```

---

## 🧹 Cleanup

Para limpar o ambiente:

```bash
# Deletar cluster k3d
k3d cluster delete aspire-poc

# Remover imagens locais
podman rmi localhost:5555/servicea:latest
podman rmi localhost:5555/serviceb:latest
podman rmi localhost:5555/servicec:latest
```

---

## 🚀 Conclusão

✅ **POC 100% funcional** demonstrando:

1. **Descoberta automática** via Labels + KubernetesClient
2. **Zero hardcode** de URLs
3. **Cross-namespace communication** funcionando
4. **RBAC** configurado corretamente
5. **Build simplificado** com `dotnet publish`
6. **Deploy em k3d** com podman

**Próximos passos:**
- Cache de descoberta para performance
- Health checks antes de retornar URLs
- Métricas de descoberta (Prometheus)
- Watch API para updates em tempo real
