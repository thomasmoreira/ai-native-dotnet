# ADR-005 — OTel GenAI semantic conventions

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Chamadas de LLM têm latência e consumo de tokens que precisam ser observáveis como qualquer
dependência — caso contrário, custo e performance ficam invisíveis.

## Decisão
Instrumentar com as **OpenTelemetry GenAI semantic conventions** (via a telemetria do
`Microsoft.Extensions.AI`): spans para embed/chat com **modelo, tokens (in/out) e latência** —
exportados ao dashboard do Aspire (e a qualquer backend OTLP).

## Consequências
- ✅ O trace distribuído da request RAG mostra cada hop com modelo, tokens e latência — o killer detail.
- ✅ Liga ao lab de observabilidade (mesma stack OTel).
- ➡️ **Custo não é estimado** — os modelos são locais (Ollama), grátis. Para um provider de nuvem,
  o custo é derivável de `tokens × tabela de preços` (não implementado de propósito, seria número artificial aqui).
