# ADR-004 — Evals como gate de qualidade

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
"Funciona no meu teste manual" não é qualidade de IA. Sem medir, regressões de prompt/retrieval
passam despercebidas.

## Decisão
Uma suíte de **evals** sobre um **golden-set** (perguntas + respostas/fontes esperadas) que mede
**groundedness, relevância e precisão do retrieval**, rodando como teste — um gate, como o SLO
do lab de observabilidade é para disponibilidade.

## Consequências
- ✅ Qualidade medida, versionada e comparável entre mudanças.
- ✅ Regressão de prompt/chunking/retrieval vira teste vermelho.
- ⚠️ Evals de geração podem usar um juiz-LLM (não-determinístico); o retrieval é medido de forma determinística.
