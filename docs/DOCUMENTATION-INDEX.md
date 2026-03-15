# 📚 Documentation Index

This POC demonstrates the complete integration between **Service Discovery**, **Kiota**, and **OpenAPI** in .NET microservices.

---

## 🗺️ Documentation Map

### 🚀 Getting Started

| Document | Description | When to Read |
|----------|-------------|--------------|
| **[README.md](README.md)** | **POC overview** - Introduction, technologies, architecture | **Start here!** |
| **[QUICK-START.md](QUICK-START.md)** | Quick guide to run the POC in 5 minutes | Want to test quickly |

### 🔍 Core Concepts

| Document | Description | When to Read |
|----------|-------------|--------------|
| **[KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md)** | What is Kiota? How does it work? (simple explanation) | Not familiar with Kiota |
| **[AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md)** | How automatic discovery via K8s labels works | Want to understand service discovery |
| **[INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md)** | How Kiota + Discovery work together | Understand the full integration |

### 🛠️ Implementation

| Document | Description | When to Read |
|----------|-------------|--------------|
| **[OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md)** | OpenAPI generation and CI/CD pipeline | Implement automatic generation |
| **[TESTING.md](TESTING.md)** | How to test the POC deployed on k3d | Test locally |
| **[k8s/README.md](k8s/README.md)** | Deploy on Kubernetes | Deploy on Kubernetes |
| **[k8s/SERVICE-DISCOVERY.md](k8s/SERVICE-DISCOVERY.md)** | Detailed Service Discovery on K8s | Understand discovery on K8s |

### 📦 Architecture

| Document | Description | When to Read |
|----------|-------------|--------------|
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | Complete technical architecture overview | Understand technical decisions |

---

## 🎯 Recommended Reading Paths

### For Beginners

```
1. README.md (overview)
   ↓
2. QUICK-START.md (run the POC)
   ↓
3. KIOTA-EXPLAINED.md (understand Kiota)
   ↓
4. TESTING.md (test)
```

### For Developers

```
1. README.md (overview)
   ↓
2. INTEGRATION-GUIDE.md (understand integration)
   ↓
3. OPENAPI-WORKFLOW.md (development workflow)
   ↓
4. AUTOMATIC-DISCOVERY.md (service discovery)
   ↓
5. ARCHITECTURE.md (technical decisions)
```

### For DevOps

```
1. README.md (overview)
   ↓
2. k8s/README.md (deploy)
   ↓
3. k8s/SERVICE-DISCOVERY.md (discovery on K8s)
   ↓
4. OPENAPI-WORKFLOW.md (CI/CD pipeline)
   ↓
5. TESTING.md (validation)
```

---

## 📖 Summary of Each Document

### README.md
**Audience:** Everyone
**Size:** ~1500 lines
**Contents:**
- POC overview
- Technologies used
- Service architecture
- How to run (Aspire + Kubernetes)
- Available endpoints
- Cross-namespace Service Discovery
- Contract break prevention

### QUICK-START.md
**Audience:** Beginners
**Size:** Created recently
**Contents:**
- Minimum prerequisites
- 3 commands to run
- Quick test
- Next steps

### KIOTA-EXPLAINED.md
**Audience:** Everyone (especially those unfamiliar with Kiota)
**Size:** ~600 lines
**Contents:**
- What is Kiota (simple analogies)
- How it works (OpenAPI → generated code)
- Generated files
- Before/after comparison
- Compile-time bug prevention
- Complete practical examples

### AUTOMATIC-DISCOVERY.md
**Audience:** Developers/Architects
**Size:** ~400 lines
**Contents:**
- Hardcoded URLs problem
- Solution with Labels + KubernetesClient
- Full discovery flow
- Advantages vs fixed URLs
- Step-by-step implementation

### INTEGRATION-GUIDE.md
**Audience:** Developers
**Size:** ~800 lines
**Contents:**
- Full integration architecture
- Shared project (reusable code)
- Factories combining discovery + Kiota
- Flow: Discovery → Kiota → Type-safe
- How to add new services (10 lines!)
- Before/after comparison

### OPENAPI-WORKFLOW.md
**Audience:** Developers/DevOps
**Size:** ~700 lines
**Contents:**
- Automatic OpenAPI generation
- OpenAPI as a CI/CD artifact
- Local scripts (bash/PowerShell)
- Complete GitHub Actions workflow
- Breaking change validation
- Dev → prod flow

### TESTING.md
**Audience:** QA/Developers
**Size:** ~500 lines
**Contents:**
- POC deployed on k3d
- How to test automatic discovery
- Test endpoints
- Discovery logs
- Useful kubectl commands
- Troubleshooting

### k8s/README.md
**Audience:** DevOps/SRE
**Size:** ~400 lines
**Contents:**
- Kubernetes manifests
- Step-by-step deploy
- Cross-namespace Service Discovery
- Network policies
- Troubleshooting

### k8s/SERVICE-DISCOVERY.md
**Audience:** DevOps/Architects
**Size:** ~600 lines
**Contents:**
- Detailed automatic discovery
- RBAC permissions
- Labels and annotations
- Complete troubleshooting
- Catalog endpoints

### ARCHITECTURE.md
**Audience:** Architects/Tech Leads
**Size:** Created recently
**Contents:**
- Architectural decisions
- Trade-offs
- Considered alternatives
- Scalability
- Security

---

## 🎓 Frequently Asked Questions → Document

| Question | Document |
|----------|----------|
| How does Kiota work? | [KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md) |
| How to discover services automatically? | [AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md) |
| How to generate OpenAPI from code? | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| How to deploy on Kubernetes? | [k8s/README.md](k8s/README.md) |
| How to test the POC? | [TESTING.md](TESTING.md) |
| How to add a new service? | [INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md) |
| What are the benefits? | [README.md](README.md) + [ARCHITECTURE.md](ARCHITECTURE.md) |

---

## 🔧 Scripts and Tools

| Script | Description | Document |
|--------|-------------|----------|
| `generate-openapi.sh` | Generates OpenAPI locally (Linux/Mac) | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| `generate-openapi.ps1` | Generates OpenAPI locally (Windows) | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| `.github/workflows/openapi-sync.yml` | Automatic CI/CD pipeline | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |

---

## 📊 Documentation Statistics

| Metric | Value |
|--------|-------|
| **Total documents** | 10 |
| **Total lines** | ~6000 |
| **Code examples** | 50+ |
| **Diagrams** | 15+ |
| **Scripts** | 3 |

---

## 🎯 Documentation Goals

✅ **Educational** - Simple explanations with analogies
✅ **Complete** - Covers all aspects of the POC
✅ **Practical** - Real, runnable examples
✅ **Structured** - Clear navigation between documents
✅ **Progressive** - From basics to advanced

---

## 🚀 Next Steps

After reading the documentation:

1. ⭐ **Star the repository** if it was helpful
2. 🐛 **Report issues** if you find problems
3. 🤝 **Contribute** with improvements
4. 📢 **Share** with the community

---

## 📝 Contributing to Documentation

Found something confusing? Want to add more examples?

1. Fork the repository
2. Edit the relevant document
3. Open a Pull Request
4. Describe what you improved

**All contributions are welcome!** 🎉
