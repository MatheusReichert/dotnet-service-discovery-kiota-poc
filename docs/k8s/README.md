# Kubernetes Deployment Guide

Este guia demonstra como fazer deploy da POC em Kubernetes com **Service Discovery entre namespaces**.

## 📋 Arquitetura

```
┌─────────────────────────┐
│   Namespace: users-ns   │
│  ┌─────────────────┐    │
│  │   ServiceA      │────┼──── http://serviceb.products-ns ───┐
│  │  (2 replicas)   │    │                                     │
│  └─────────────────┘    │                                     │
└─────────────────────────┘                                     │
                                                                │
┌─────────────────────────┐                                     │
│ Namespace: products-ns  │◄────────────────────────────────────┘
│  ┌─────────────────┐    │
│  │   ServiceB      │────┼──── http://servicec.orders-ns ─────┐
│  │  (2 replicas)   │    │                                     │
│  └─────────────────┘    │                                     │
└─────────────────────────┘                                     │
                                                                │
┌─────────────────────────┐                                     │
│  Namespace: orders-ns   │◄────────────────────────────────────┘
│  ┌─────────────────┐    │
│  │   ServiceC      │    │
│  │  (2 replicas)   │    │
│  └─────────────────┘    │
└─────────────────────────┘
```

## 🚀 Quick Start

### 1. Build Docker Images

```bash
# ServiceA
docker build -t your-registry/servicea:latest -f ServiceA/Dockerfile .
docker push your-registry/servicea:latest

# ServiceB  
docker build -t your-registry/serviceb:latest -f ServiceB/Dockerfile .
docker push your-registry/serviceb:latest

# ServiceC
docker build -t your-registry/servicec:latest -f ServiceC/Dockerfile .
docker push your-registry/servicec:latest
```

### 2. Atualizar Image Registry

Edite os arquivos de deployment e substitua `your-registry` pelo seu registry:

```bash
sed -i 's/your-registry/myregistry.azurecr.io/g' k8s/*.yaml
```

### 3. Deploy

```bash
# Criar namespaces
kubectl apply -f k8s/00-namespaces.yaml

# Deploy dos serviços
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml

# Network Policies (opcional mas recomendado)
kubectl apply -f k8s/04-network-policies.yaml

# Ingress (opcional - para expor externamente)
kubectl apply -f k8s/05-ingress.yaml
```

### 4. Verificar Deployment

```bash
# Ver pods
kubectl get pods -n users-ns
kubectl get pods -n products-ns
kubectl get pods -n orders-ns

# Ver services
kubectl get svc -n users-ns
kubectl get svc -n products-ns
kubectl get svc -n orders-ns

# Ver logs
kubectl logs -f -n users-ns deployment/servicea
```

## 🧪 Testar Service Discovery Cross-Namespace

### 1. Port-Forward para ServiceA

```bash
kubectl port-forward -n users-ns svc/servicea 8080:80
```

### 2. Testar Endpoint

```bash
# ServiceA chama ServiceB (outro namespace)
curl http://localhost:8080/api/users/with-products/1

# Resposta esperada:
# {
#   "Message": "ServiceA called ServiceB via service discovery",
#   "Products": "[...]"
# }
```

### 3. Verificar DNS Resolution

```bash
# Exec dentro do pod ServiceA
kubectl exec -it -n users-ns deployment/servicea -- /bin/bash

# Testar resolução DNS
nslookup serviceb.products-ns.svc.cluster.local

# Output esperado:
# Name:   serviceb.products-ns.svc.cluster.local
# Address: 10.x.x.x
```

## 📊 Monitoramento

### Logs Agregados

```bash
# Todos os pods de ServiceA
kubectl logs -n users-ns -l app=servicea --tail=100 -f

# Grep por erros
kubectl logs -n users-ns -l app=servicea | grep ERROR
```

### Métricas

```bash
# Top pods por CPU/Memory
kubectl top pods -n users-ns
kubectl top pods -n products-ns
kubectl top pods -n orders-ns
```

### Events

```bash
# Ver eventos recentes
kubectl get events -n users-ns --sort-by='.lastTimestamp'
```

## 🔒 Network Policies

As Network Policies implementam o princípio de **zero trust**:

- ✅ ServiceA pode acessar ServiceB (cross-namespace)
- ✅ ServiceB pode acessar ServiceC (cross-namespace)
- ❌ ServiceA NÃO pode acessar ServiceC diretamente
- ❌ Pods externos não podem acessar nenhum serviço

### Testar Network Policies

```bash
# Tentar acessar ServiceC de ServiceA (deve falhar)
kubectl exec -it -n users-ns deployment/servicea -- \
  curl http://servicec.orders-ns/api/orders

# Timeout esperado (bloqueado por network policy)
```

## 🌐 DNS do Kubernetes

### Formatos de DNS

| Formato | Escopo | Exemplo |
|---------|--------|---------|
| `serviceb` | Mesmo namespace | `http://serviceb` |
| `serviceb.products-ns` | Namespace específico | `http://serviceb.products-ns` |
| `serviceb.products-ns.svc` | Service | `http://serviceb.products-ns.svc` |
| `serviceb.products-ns.svc.cluster.local` | FQDN | `http://serviceb.products-ns.svc.cluster.local` |

### Configuração nos Serviços

Os serviços estão configurados para usar DNS cross-namespace:

**ATENÇÃO:** Esta POC usa **descoberta automática via Labels + KubernetesClient**.

Não é necessário configurar URLs manualmente! Os serviços descobrem uns aos outros automaticamente através de labels:

```yaml
# ServiceB anuncia sua API
metadata:
  labels:
    api-type: products-api  # ← ServiceA descobre automaticamente
```

Ver [SERVICE-DISCOVERY.md](SERVICE-DISCOVERY.md) para detalhes completos da implementação.

## 🔧 Troubleshooting

### Pods não iniciam

```bash
# Verificar status
kubectl describe pod -n users-ns <pod-name>

# Verificar eventos
kubectl get events -n users-ns

# Verificar imagem
kubectl get pod -n users-ns <pod-name> -o jsonpath='{.spec.containers[0].image}'
```

### Service Discovery não funciona

```bash
# Verificar DNS
kubectl exec -n users-ns deployment/servicea -- nslookup serviceb.products-ns

# Verificar endpoints
kubectl get endpoints -n products-ns serviceb

# Verificar network policies
kubectl get networkpolicies -n products-ns
```

### Conectividade entre namespaces

```bash
# Testar conexão TCP
kubectl exec -n users-ns deployment/servicea -- \
  nc -zv serviceb.products-ns 80

# Verificar logs de ambos os serviços
kubectl logs -f -n users-ns deployment/servicea &
kubectl logs -f -n products-ns deployment/serviceb &
```

## 🧹 Cleanup

```bash
# Deletar tudo
kubectl delete -f k8s/05-ingress.yaml
kubectl delete -f k8s/04-network-policies.yaml
kubectl delete -f k8s/03-servicec-deployment.yaml
kubectl delete -f k8s/02-serviceb-deployment.yaml
kubectl delete -f k8s/01-servicea-deployment.yaml
kubectl delete -f k8s/00-namespaces.yaml

# Ou deletar namespaces (cascading delete)
kubectl delete namespace users-ns
kubectl delete namespace products-ns
kubectl delete namespace orders-ns
```

## 📚 Próximos Passos

- [ ] Adicionar Horizontal Pod Autoscaler (HPA)
- [ ] Configurar Prometheus + Grafana
- [ ] Implementar Service Mesh (Istio/Linkerd)
- [ ] Adicionar distributed tracing (Jaeger)
- [ ] Configurar CI/CD pipeline
- [ ] Implementar canary deployments
