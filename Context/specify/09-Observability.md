# 09 — Observability

- **OpenTelemetry** for traces/metrics/logs; propagate correlation/causation IDs across HTTP + broker.
- Key metrics: `http_server_duration`, `events_published_latency_ms`, `outbox_backlog`, `bulk_job_throughput`.
- Health endpoints: `/health/live`, `/health/ready`, `/health/degraded`.
