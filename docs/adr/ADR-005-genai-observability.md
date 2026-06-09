# ADR-005 — OTel GenAI semantic conventions

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Chamadas de LLM têm custo e latência que precisam ser observáveis como qualquer dependência.

## Decisão
Instrumentar com as **OpenTelemetry GenAI semantic conventions** (via a telemetria do
`Microsoft.Extensions.AI`): spans para embed/retrieve/chat, com modelo, tokens (in/out),
custo estimado e latência — exportados ao dashboard do Aspire (e a qualquer backend OTLP).

## Consequências
- ✅ O trace distribuído da request RAG mostra cada hop com tokens/custo — o killer detail.
- ✅ Liga ao lab de observabilidade (mesma stack OTel).
- ⚠️ Custo é estimado por tabela de preços (local não cobra); documentado.
