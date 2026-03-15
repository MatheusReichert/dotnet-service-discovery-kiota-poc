# Kubernetes Deployment Guide

This guide demonstrates how to deploy the POC on Kubernetes with **cross-namespace Service Discovery**.

## 📋 Architecture

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

### 2. Update Image Registry

Edit the deployment files and replace `your-registry` with your registry:

```bash
sed -i 's/your-registry/myregistry.azurecr.io/g' k8s/*.yaml
```

### 3. Deploy

```bash
# Create namespaces
kubectl apply -f k8s/00-namespaces.yaml

# Deploy services
kubectl apply -f k8s/01-servicea-deployment.yaml
kubectl apply -f k8s/02-serviceb-deployment.yaml
kubectl apply -f k8s/03-servicec-deployment.yaml

# Network Policies (optional but recommended)
kubectl apply -f k8s/04-network-policies.yaml

# Ingress (optional - to expose externally)
kubectl apply -f k8s/05-ingress.yaml
```

### 4. Verify Deployment

```bash
# View pods
kubectl get pods -n users-ns
kubectl get pods -n products-ns
kubectl get pods -n orders-ns

# View services
kubectl get svc -n users-ns
kubectl get svc -n products-ns
kubectl get svc -n orders-ns

# View logs
kubectl logs -f -n users-ns deployment/servicea
```

## 🧪 Test Cross-Namespace Service Discovery

### 1. Port-Forward to ServiceA

```bash
kubectl port-forward -n users-ns svc/servicea 8080:80
```

### 2. Test Endpoint

```bash
# ServiceA calls ServiceB (different namespace)
curl http://localhost:8080/api/users/with-products/1

# Expected response:
# {
#   "Message": "ServiceA called ServiceB via service discovery",
#   "Products": "[...]"
# }
```

### 3. Verify DNS Resolution

```bash
# Exec into ServiceA pod
kubectl exec -it -n users-ns deployment/servicea -- /bin/bash

# Test DNS resolution
nslookup serviceb.products-ns.svc.cluster.local

# Expected output:
# Name:   serviceb.products-ns.svc.cluster.local
# Address: 10.x.x.x
```

## 📊 Monitoring

### Aggregated Logs

```bash
# All ServiceA pods
kubectl logs -n users-ns -l app=servicea --tail=100 -f

# Grep for errors
kubectl logs -n users-ns -l app=servicea | grep ERROR
```

### Metrics

```bash
# Top pods by CPU/Memory
kubectl top pods -n users-ns
kubectl top pods -n products-ns
kubectl top pods -n orders-ns
```

### Events

```bash
# View recent events
kubectl get events -n users-ns --sort-by='.lastTimestamp'
```

## 🔒 Network Policies

The Network Policies implement the **zero trust** principle:

- ✅ ServiceA can access ServiceB (cross-namespace)
- ✅ ServiceB can access ServiceC (cross-namespace)
- ❌ ServiceA CANNOT access ServiceC directly
- ❌ External pods cannot access any service

### Test Network Policies

```bash
# Try to access ServiceC from ServiceA (should fail)
kubectl exec -it -n users-ns deployment/servicea -- \
  curl http://servicec.orders-ns/api/orders

# Expected timeout (blocked by network policy)
```

## 🌐 Kubernetes DNS

### DNS Formats

| Format | Scope | Example |
|--------|-------|---------|
| `serviceb` | Same namespace | `http://serviceb` |
| `serviceb.products-ns` | Specific namespace | `http://serviceb.products-ns` |
| `serviceb.products-ns.svc` | Service | `http://serviceb.products-ns.svc` |
| `serviceb.products-ns.svc.cluster.local` | FQDN | `http://serviceb.products-ns.svc.cluster.local` |

### Service Configuration

Services are configured to use cross-namespace DNS:

**NOTE:** This POC uses **automatic discovery via Labels + KubernetesClient**.

Manual URL configuration is not required! Services discover each other automatically through labels:

```yaml
# ServiceB announces its API
metadata:
  labels:
    api-type: products-api  # ← ServiceA discovers automatically
```

See [SERVICE-DISCOVERY.md](SERVICE-DISCOVERY.md) for full implementation details.

## 🔧 Troubleshooting

### Pods not starting

```bash
# Check status
kubectl describe pod -n users-ns <pod-name>

# Check events
kubectl get events -n users-ns

# Check image
kubectl get pod -n users-ns <pod-name> -o jsonpath='{.spec.containers[0].image}'
```

### Service Discovery not working

```bash
# Check DNS
kubectl exec -n users-ns deployment/servicea -- nslookup serviceb.products-ns

# Check endpoints
kubectl get endpoints -n products-ns serviceb

# Check network policies
kubectl get networkpolicies -n products-ns
```

### Cross-namespace connectivity

```bash
# Test TCP connection
kubectl exec -n users-ns deployment/servicea -- \
  nc -zv serviceb.products-ns 80

# Check logs from both services
kubectl logs -f -n users-ns deployment/servicea &
kubectl logs -f -n products-ns deployment/serviceb &
```

## 🧹 Cleanup

```bash
# Delete everything
kubectl delete -f k8s/05-ingress.yaml
kubectl delete -f k8s/04-network-policies.yaml
kubectl delete -f k8s/03-servicec-deployment.yaml
kubectl delete -f k8s/02-serviceb-deployment.yaml
kubectl delete -f k8s/01-servicea-deployment.yaml
kubectl delete -f k8s/00-namespaces.yaml

# Or delete namespaces (cascading delete)
kubectl delete namespace users-ns
kubectl delete namespace products-ns
kubectl delete namespace orders-ns
```

## 📚 Next Steps

- [ ] Add Horizontal Pod Autoscaler (HPA)
- [ ] Configure Prometheus + Grafana
- [ ] Implement Service Mesh (Istio/Linkerd)
- [ ] Add distributed tracing (Jaeger)
- [ ] Configure CI/CD pipeline
- [ ] Implement canary deployments
