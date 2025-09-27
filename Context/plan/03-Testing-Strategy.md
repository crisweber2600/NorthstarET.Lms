# 03 — Testing Strategy

- **Unit**: domain invariants (enrollment rules, deactivation).
- **Integration**: EF + Outbox; repository + API.
- **Contract**: OpenAPI diff guard; Pact provider tests with consumer CDCs.
- **E2E**: Vertical slice (create school → event → consumer receipt).
- **Performance**: k6 scenarios (reads, writes, bulk).
- **Chaos**: broker outage; DB failover; backpressure.
