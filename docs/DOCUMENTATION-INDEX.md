# 📚 Índice da Documentação

Esta POC demonstra a integração completa entre **Service Discovery**, **Kiota** e **OpenAPI** em microserviços .NET.

---

## 🗺️ Mapa da Documentação

### 🚀 Para Começar

| Documento | Descrição | Quando Ler |
|-----------|-----------|------------|
| **[README.md](README.md)** | **Visão geral da POC** - Introdução, tecnologias, arquitetura | **Comece aqui!** |
| **[QUICK-START.md](QUICK-START.md)** | Guia rápido para rodar a POC em 5 minutos | Quer testar rapidamente |

### 🔍 Conceitos Principais

| Documento | Descrição | Quando Ler |
|-----------|-----------|------------|
| **[KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md)** | O que é Kiota? Como funciona? (explicação simples) | Não conhece Kiota |
| **[AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md)** | Como funciona descoberta automática via K8s labels | Quer entender service discovery |
| **[INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md)** | Como Kiota + Descoberta trabalham juntos | Entender a integração completa |

### 🛠️ Implementação

| Documento | Descrição | Quando Ler |
|-----------|-----------|------------|
| **[OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md)** | Geração de OpenAPI e pipeline CI/CD | Implementar geração automática |
| **[TESTING.md](TESTING.md)** | Como testar a POC deployada no k3d | Testar localmente |
| **[k8s/README.md](k8s/README.md)** | Deploy no Kubernetes | Deployar no Kubernetes |
| **[k8s/SERVICE-DISCOVERY.md](k8s/SERVICE-DISCOVERY.md)** | Service Discovery detalhado no K8s | Entender descoberta no K8s |

### 📦 Arquitetura

| Documento | Descrição | Quando Ler |
|-----------|-----------|------------|
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | Visão técnica completa da arquitetura | Entender decisões técnicas |

---

## 🎯 Fluxos de Leitura Recomendados

### Para Iniciantes

```
1. README.md (visão geral)
   ↓
2. QUICK-START.md (rodar a POC)
   ↓
3. KIOTA-EXPLAINED.md (entender Kiota)
   ↓
4. TESTING.md (testar)
```

### Para Desenvolvedores

```
1. README.md (visão geral)
   ↓
2. INTEGRATION-GUIDE.md (entender integração)
   ↓
3. OPENAPI-WORKFLOW.md (workflow de desenvolvimento)
   ↓
4. AUTOMATIC-DISCOVERY.md (service discovery)
   ↓
5. ARCHITECTURE.md (decisões técnicas)
```

### Para DevOps

```
1. README.md (visão geral)
   ↓
2. k8s/README.md (deploy)
   ↓
3. k8s/SERVICE-DISCOVERY.md (descoberta no K8s)
   ↓
4. OPENAPI-WORKFLOW.md (pipeline CI/CD)
   ↓
5. TESTING.md (validação)
```

---

## 📖 Resumo de Cada Documento

### README.md
**Público:** Todos  
**Tamanho:** ~1500 linhas  
**Conteúdo:**
- Visão geral da POC
- Tecnologias utilizadas
- Arquitetura dos serviços
- Como executar (Aspire + Kubernetes)
- Endpoints disponíveis
- Service Discovery cross-namespace
- Prevenção de quebra de contratos

### QUICK-START.md
**Público:** Iniciantes  
**Tamanho:** Criado agora  
**Conteúdo:**
- Pré-requisitos mínimos
- 3 comandos para rodar
- Teste rápido
- Próximos passos

### KIOTA-EXPLAINED.md
**Público:** Todos (especialmente quem não conhece Kiota)  
**Tamanho:** ~600 linhas  
**Conteúdo:**
- O que é Kiota (analogias simples)
- Como funciona (OpenAPI → código gerado)
- Arquivos gerados
- Comparação antes/depois
- Prevenção de bugs em compile-time
- Exemplos práticos completos

### AUTOMATIC-DISCOVERY.md
**Público:** Desenvolvedores/Arquitetos  
**Tamanho:** ~400 linhas  
**Conteúdo:**
- Problema de URLs hardcoded
- Solução com Labels + KubernetesClient
- Fluxo completo da descoberta
- Vantagens vs URLs fixas
- Implementação passo a passo

### INTEGRATION-GUIDE.md
**Público:** Desenvolvedores  
**Tamanho:** ~800 linhas  
**Conteúdo:**
- Arquitetura da integração completa
- Projeto Shared (código reutilizável)
- Factories que combinam descoberta + Kiota
- Fluxo: Descoberta → Kiota → Type-safe
- Como adicionar novos serviços (10 linhas!)
- Comparação antes/depois

### OPENAPI-WORKFLOW.md
**Público:** Desenvolvedores/DevOps  
**Tamanho:** ~700 linhas  
**Conteúdo:**
- Geração automática de OpenAPI
- OpenAPI como artefato em CI/CD
- Scripts locais (bash/PowerShell)
- GitHub Actions workflow completo
- Validação de breaking changes
- Fluxo dev → prod

### TESTING.md
**Público:** QA/Desenvolvedores  
**Tamanho:** ~500 linhas  
**Conteúdo:**
- POC deployada no k3d
- Como testar descoberta automática
- Endpoints de teste
- Logs de descoberta
- Comandos kubectl úteis
- Troubleshooting

### k8s/README.md
**Público:** DevOps/SRE  
**Tamanho:** ~400 linhas  
**Conteúdo:**
- Manifests Kubernetes
- Deploy passo a passo
- Service Discovery cross-namespace
- Network policies
- Troubleshooting

### k8s/SERVICE-DISCOVERY.md
**Público:** DevOps/Arquitetos  
**Tamanho:** ~600 linhas  
**Conteúdo:**
- Descoberta automática detalhada
- RBAC permissions
- Labels e annotations
- Troubleshooting completo
- Endpoints de catálogo

### ARCHITECTURE.md
**Público:** Arquitetos/Tech Leads  
**Tamanho:** Criado agora  
**Conteúdo:**
- Decisões arquiteturais
- Trade-offs
- Alternativas consideradas
- Escalabilidade
- Segurança

---

## 🎓 Perguntas Frequentes → Documento

| Pergunta | Documento |
|----------|-----------|
| Como Kiota funciona? | [KIOTA-EXPLAINED.md](KIOTA-EXPLAINED.md) |
| Como descobrir serviços automaticamente? | [AUTOMATIC-DISCOVERY.md](AUTOMATIC-DISCOVERY.md) |
| Como gerar OpenAPI do código? | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| Como deployar no Kubernetes? | [k8s/README.md](k8s/README.md) |
| Como testar a POC? | [TESTING.md](TESTING.md) |
| Como adicionar novo serviço? | [INTEGRATION-GUIDE.md](INTEGRATION-GUIDE.md) |
| Quais são os benefícios? | [README.md](README.md) + [ARCHITECTURE.md](ARCHITECTURE.md) |

---

## 🔧 Scripts e Ferramentas

| Script | Descrição | Documento |
|--------|-----------|-----------|
| `generate-openapi.sh` | Gera OpenAPI localmente (Linux/Mac) | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| `generate-openapi.ps1` | Gera OpenAPI localmente (Windows) | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |
| `.github/workflows/openapi-sync.yml` | Pipeline CI/CD automático | [OPENAPI-WORKFLOW.md](OPENAPI-WORKFLOW.md) |

---

## 📊 Estatísticas da Documentação

| Métrica | Valor |
|---------|-------|
| **Total de documentos** | 10 |
| **Linhas totais** | ~6000 |
| **Exemplos de código** | 50+ |
| **Diagramas** | 15+ |
| **Scripts** | 3 |

---

## 🎯 Objetivos da Documentação

✅ **Didática** - Explicações simples com analogias  
✅ **Completa** - Cobre todos os aspectos da POC  
✅ **Prática** - Exemplos reais e executáveis  
✅ **Estruturada** - Navegação clara entre documentos  
✅ **Progressiva** - Do básico ao avançado  

---

## 🚀 Próximos Passos

Depois de ler a documentação:

1. ⭐ **Star o repositório** se foi útil
2. 🐛 **Reportar issues** se encontrar problemas
3. 🤝 **Contribuir** com melhorias
4. 📢 **Compartilhar** com a comunidade

---

## 📝 Contribuindo com a Documentação

Encontrou algo confuso? Quer adicionar mais exemplos?

1. Fork o repositório
2. Edite o documento relevante
3. Abra um Pull Request
4. Descreva o que melhorou

**Todas as contribuições são bem-vindas!** 🎉
