# dotnet-aspire-reference

.NET Aspire orchestrating a distributed app — Gateway + Catalog + Pricing + Postgres + Redis —
with service discovery, resilience, health and OpenTelemetry standardized via ServiceDefaults.

## AppHost
Declares resources and services in C#; Aspire provisions containers, wires connection strings
and service discovery, and feeds telemetry into the dashboard. No docker-compose.

## Distributed trace (the killer detail)
A single GET /storefront/{id} request crosses Gateway → Catalog (Redis + Postgres) → Pricing as
one distributed trace, visible in the Aspire dashboard. Context propagation (W3C traceparent)
is automatic because the HttpClient is instrumented by ServiceDefaults.
