# ADR-003 — Ollama local por padrão

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
O lab precisa ser reprodutível por quem clonar, sem exigir chave paga nem vazar segredos.

## Decisão
**Ollama local** (via Aspire) como provider padrão — `all-minilm` (embeddings) e `llama3.2`
(chat). Cloud (OpenAI/Azure) é documentado como alternativa, com a chave em user-secrets.

## Consequências
- ✅ Reprodutível e grátis; roda offline.
- ✅ Nunca há chave commitada; quem quer qualidade aponta para um provider de fronteira.
- ⚠️ Modelos pequenos → respostas mais fracas; o ponto é a engenharia, não o modelo.
- ⚠️ Inferência em CPU; modelos escolhidos para serem leves.
