# ai-native-dotnet

> Um serviço **.NET AI-native** feito **como arquiteto, não como demo de RAG**: RAG sobre
> **pgvector**, **tool-calling**, **evals automatizadas**, **observabilidade de LLM** (OTel
> GenAI) e **provider pluggable** (Ollama local por padrão) — tudo orquestrado pelo **.NET Aspire**.

[![CI](https://github.com/thomasmoreira/ai-native-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/thomasmoreira/ai-native-dotnet/actions/workflows/ci.yml)

---

## A tese

A maioria dos repos de IA é "chamei a OpenAI e funcionou". Este prova as **4 coisas que
separam um engenheiro de IA arquiteto**:

1. **RAG sério** — retrieval avaliado, resposta **com citações** (anti-alucinação).
2. **Evals automatizadas** — a qualidade é **medida** (groundedness/precisão), não no chute.
3. **Observabilidade de LLM** — cada chamada é um span com **tokens, custo e latência**.
4. **Provider-agnóstico e testável** — troca o modelo numa linha; **testes determinísticos** sem chamar LLM de verdade.

Fecha o arco do portfólio: **infra distribuída** ([consistency](https://github.com/thomasmoreira/distributed-consistency-lab) · [observability](https://github.com/thomasmoreira/observability-from-scratch) · [aspire](https://github.com/thomasmoreira/dotnet-aspire-reference)) **→ IA distribuída**, reusando Postgres, Aspire e OTel. O corpus default são **os próprios docs de arquitetura do portfólio** — um serviço de IA que **explica os seus labs**.

## Arquitetura

```mermaid
flowchart LR
  Client([Client]) -->|POST /ask| AI[AI Service]
  AI -->|1. embed| EMB[(IEmbeddingGenerator)]
  AI -->|2. similarity search| PG[(Postgres + pgvector)]
  AI -->|3. chat + tools| LLM[(IChatClient)]
  EMB -.-> OLL[Ollama]
  LLM -.-> OLL
  AI -. OTLP · GenAI .-> DASH[[Aspire Dashboard]]
```

Uma pergunta em `POST /ask` dispara o pipeline RAG: **embed da pergunta → retrieve no pgvector
(top-k) → contexto → LLM (com tool-calling) → resposta com citações**. Tudo vira **um único
trace distribuído** com tokens/custo nos spans.

## Componentes

| Peça | Papel |
|---|---|
| **AppHost** | Orquestra pgvector + Ollama + o serviço (Aspire). |
| **ServiceDefaults** | OTel + health + service discovery + resiliência. |
| **AiService** | Minimal API: `POST /ask` (RAG) + `POST /ingest`; `Microsoft.Extensions.AI`. |
| **pgvector** | Postgres com extensão `vector` — embeddings + busca por similaridade. |
| **Ollama** | Modelos locais (`all-minilm` embeddings, `llama3.2` chat); pluggable p/ cloud. |

## Sinais de arquiteto

- **Evals como gate** — qualidade medida e versionada (análogo ao SLO do lab de observabilidade).
- **Determinismo nos testes** — fake `IChatClient`/`IEmbeddingGenerator` → CI verde sem LLM externo.
- **Custo/latência observáveis** — a conversa que todo arquiteto de IA precisa ter.
- **Troca de provider sem refatorar** — `Microsoft.Extensions.AI` como abstração.

## Como rodar

**Pré-requisitos:** .NET 10 SDK e Docker (o Aspire roda pgvector e Ollama como containers).

```bash
dotnet new install Aspire.ProjectTemplates   # uma vez

# sobe pgvector + Ollama (baixa all-minilm + llama3.2) + o serviço + dashboard
dotnet run --project src/AppHost
#   → o console imprime a URL do dashboard (com token de login)

# indexa o corpus (docs do portfólio) e pergunta — a porta do serviço aparece no dashboard
curl -X POST http://localhost:<porta>/ingest
curl -X POST http://localhost:<porta>/ask -H 'Content-Type: application/json' \
  -d '{"question":"What is the transactional outbox and why use it?"}'
```

### Endpoints

| Endpoint | O quê |
|---|---|
| `POST /ingest` | Indexa o corpus (chunk → embed → pgvector) |
| `GET /search?q=` | Busca por similaridade (a metade de retrieval do RAG) |
| `POST /ask` | RAG completo: retrieve → prompt grounded → LLM (com tool-calling) → resposta **com citações** |

### Ver o trace RAG (o killer detail)

Dispare um `POST /ask` e abra o **dashboard → Traces**. A request aparece como **um único trace**
cruzando os hops, com **modelo, tokens e latência** nos spans (OTel GenAI conventions):

```
POST /ask (AiService)
├─ embed (gen_ai) ............ all-minilm
├─ db query (pgvector) ....... similarity search
└─ chat (gen_ai) ............. llama3.2 · tokens in/out · latência
```

Captura real do dashboard — uma request `POST /ask` (38s, 3 recursos, 14 spans) com o fluxo
completo: **embed (all-minilm) → pgvector → orchestrate_tools → chat (llama3.2) →
execute_tool (ListSources) → chat** — RAG, observabilidade GenAI e tool-calling num trace só:

![Trace RAG no dashboard do .NET Aspire: POST /ask cruzando embeddings (all-minilm) → pgvector → chat (llama3.2) com tool-calling, em spans GenAI](docs/images/rag-trace.png)

## Verificação ao vivo

```bash
dotnet test
```

- **Plumbing** (determinístico, fake provider): ingest + `/search` + `/ask` com citações + tool-calling — rápido, sem LLM externo.
- **Eval** (embeddings reais): recall@3 sobre um golden-set, com **gate** num threshold (ADR-004). Última execução: **recall@3 = 8/8 (100%)**.

## Estrutura

```
src/
  AppHost/          — orquestração (Aspire): pgvector + Ollama (all-minilm, llama3.2) + serviço
  ServiceDefaults/  — OTel + health + service discovery + resiliência
  AiService/        — Minimal API: /ingest, /search, /ask; Microsoft.Extensions.AI
data/               — corpus bundlado (docs de arquitetura dos 4 labs)
tests/
  AppHost.Tests/    — fakes determinísticos (plumbing) + eval com embeddings reais
docs/adr/           — decisões de arquitetura
```

## Decisões de arquitetura

- [ADR-001 — Microsoft.Extensions.AI como abstração](docs/adr/ADR-001-extensions-ai-abstraction.md)
- [ADR-002 — pgvector como vector store](docs/adr/ADR-002-pgvector.md)
- [ADR-003 — Ollama local por padrão](docs/adr/ADR-003-ollama-default.md)
- [ADR-004 — Evals como gate de qualidade](docs/adr/ADR-004-evals-as-gate.md)
- [ADR-005 — OTel GenAI semantic conventions](docs/adr/ADR-005-genai-observability.md)
- [ADR-006 — Testes com fake provider](docs/adr/ADR-006-fake-provider-tests.md)

> Modelos pequenos e locais **de propósito** — o ponto é a **engenharia** (RAG observável,
> avaliado e testável), não o modelo. A mesma arquitetura aponta para um modelo de fronteira
> trocando uma linha de configuração.

---

_Lab de portfólio. Foco: RAG, Microsoft.Extensions.AI, pgvector, evals, observabilidade de LLM e .NET Aspire._
