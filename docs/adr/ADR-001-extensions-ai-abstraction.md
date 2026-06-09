# ADR-001 — Microsoft.Extensions.AI como abstração

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
O serviço precisa de chat e embeddings, mas não deve acoplar-se a um provider concreto
(OpenAI, Azure, Ollama). Acoplar mata a testabilidade e a portabilidade.

## Decisão
Programar contra `Microsoft.Extensions.AI` (`IChatClient`, `IEmbeddingGenerator`). O domínio
nunca conhece o provider — ele é injetado.

## Consequências
- ✅ Troca local↔cloud trocando o registro no DI, sem refatorar o domínio.
- ✅ Testes injetam um fake — determinísticos, sem LLM externo (ADR-006).
- ✅ Middleware da MEAI (telemetria, function-invocation) compõe de forma uniforme.
- ⚠️ Recursos muito específicos de um provider ficam atrás de abstração; aceitável aqui.
