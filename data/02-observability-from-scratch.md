# observability-from-scratch

The three pillars of observability — traces, metrics, logs — built by hand with OpenTelemetry,
correlated in Grafana (LGTM: Tempo, Loki, Prometheus).

## Collector in the middle
The app exports one protocol (OTLP) to an OpenTelemetry Collector, which fans out to backends.
Swapping a backend is Collector config, not an app redeploy.

## RED metrics + SLO
Rate, Errors, Duration per route. An availability SLO (99.5% non-5xx) defines an error budget;
a multi-window multi-burn-rate alert (Google SRE) pages only when the budget burns fast enough
to matter. A runbook makes the alert operable.

## Trace-log correlation
Logs carry trace_id/span_id; clicking a trace_id in a log opens the trace in Tempo.
