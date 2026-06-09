# ADR-006 — Testes com fake provider

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Testes não podem depender de um LLM externo (lento, não-determinístico, pago) nem rodar um
modelo pesado no CI.

## Decisão
Os testes injetam um **fake `IChatClient`/`IEmbeddingGenerator`** (respostas/vetores
determinísticos). O `Aspire.Hosting.Testing` sobe pgvector + o serviço; a verificação do
caminho real (Ollama) é feita num run local.

## Consequências
- ✅ CI determinístico, rápido e sem segredos.
- ✅ O pipeline (chunking, retrieval, montagem de prompt, citações) é testado de ponta a ponta.
- ⚠️ A qualidade do modelo real não é exercida no CI — é o papel das evals locais (ADR-004).
