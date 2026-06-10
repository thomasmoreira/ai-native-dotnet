# ai-native-dotnet

Serviço de RAG em .NET: indexa documentos no pgvector e responde perguntas com citações usando um LLM. O retrieval é avaliado por evals automatizadas, cada chamada de IA gera um span com modelo, tokens e latência, e o provider de LLM é trocável (`Microsoft.Extensions.AI`), com testes que não dependem de um modelo real. Os modelos rodam localmente via Ollama por padrão, e tudo é orquestrado com .NET Aspire.

O corpus padrão são os docs de arquitetura dos meus outros labs, então dá para perguntar sobre eles. É o quarto projeto do conjunto, reaproveitando Postgres, Aspire e OpenTelemetry dos anteriores.

[![CI](https://github.com/thomasmoreira/ai-native-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/thomasmoreira/ai-native-dotnet/actions/workflows/ci.yml)

## Visão geral

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

Uma pergunta em `POST /ask` dispara o pipeline de RAG: gera o embedding da pergunta, busca os top-k trechos mais parecidos no pgvector, monta o contexto e chama o LLM (que pode usar tool-calling), retornando a resposta com as citações. A request inteira aparece como um único trace, com modelo, tokens e latência em cada span.

| Componente | Papel |
|---|---|
| **AppHost** | Orquestra pgvector, Ollama e o serviço (Aspire). |
| **ServiceDefaults** | OpenTelemetry, health checks, service discovery e resiliência. |
| **AiService** | Minimal API: `POST /ask` e `POST /ingest`, sobre `Microsoft.Extensions.AI`. |
| **pgvector** | Postgres com a extensão `vector`: embeddings e busca por similaridade. |
| **Ollama** | Modelos locais (`all-minilm` para embeddings, `llama3.2` para chat); trocável por um provider de nuvem. |

## Como rodar

Pré-requisitos: .NET 10 e Docker (o Aspire executa o pgvector e o Ollama como containers).

```bash
dotnet new install Aspire.ProjectTemplates   # apenas na primeira vez

# sobe pgvector + Ollama (baixa os modelos all-minilm e llama3.2) + o serviço + dashboard
dotnet run --project src/AppHost

# indexa o corpus e pergunta (a porta do serviço aparece no dashboard)
curl -X POST http://localhost:<porta>/ingest
curl -X POST http://localhost:<porta>/ask -H 'Content-Type: application/json' \
  -d '{"question":"What is the transactional outbox and why use it?"}'
```

### Endpoints

| Endpoint | O quê |
|---|---|
| `POST /ingest` | Indexa o corpus (chunk → embed → pgvector) |
| `GET /search?q=` | Busca por similaridade (a parte de retrieval do RAG) |
| `POST /ask` | RAG completo: retrieval → prompt com contexto → LLM (com tool-calling) → resposta com citações |

### O trace de uma request RAG

Um `POST /ask` aparece no dashboard do Aspire como um único trace, cruzando os hops com modelo, tokens e latência nos spans. Esta é uma captura real, de uma request que passou por embedding (all-minilm), busca no pgvector, chat (llama3.2) e uma chamada de tool:

![Trace de um POST /ask no dashboard do Aspire, passando por embedding, pgvector, chat e tool-calling](docs/images/rag-trace.png)

### Testes

```bash
dotnet test
```

- Plumbing (determinístico, com fake provider): `ingest`, `search` e `ask` com citações e tool-calling, sem depender de um LLM externo.
- Eval (com embeddings reais): recall@3 sobre um golden-set, com um gate de threshold (ADR-004). Na última execução, recall@3 = 8/8.

## Estrutura

```
src/
  AppHost/          orquestração (Aspire): pgvector + Ollama + serviço
  ServiceDefaults/  OpenTelemetry, health checks, service discovery e resiliência
  AiService/        Minimal API: /ingest, /search, /ask; Microsoft.Extensions.AI
data/               corpus indexado (docs de arquitetura dos outros labs)
tests/
  AppHost.Tests/    fakes determinísticos (plumbing) + eval com embeddings reais
docs/adr/           decisões de arquitetura
```

## Decisões de arquitetura

- [ADR-001 — Microsoft.Extensions.AI como abstração](docs/adr/ADR-001-extensions-ai-abstraction.md)
- [ADR-002 — pgvector como vector store](docs/adr/ADR-002-pgvector.md)
- [ADR-003 — Ollama local por padrão](docs/adr/ADR-003-ollama-default.md)
- [ADR-004 — Evals como gate de qualidade](docs/adr/ADR-004-evals-as-gate.md)
- [ADR-005 — OpenTelemetry GenAI semantic conventions](docs/adr/ADR-005-genai-observability.md)
- [ADR-006 — Testes com fake provider](docs/adr/ADR-006-fake-provider-tests.md)

Os modelos são pequenos e locais de propósito: o foco do projeto é a engenharia em volta (RAG avaliado, observável e testável), não o modelo em si. Apontar para um modelo maior é uma troca de configuração.
