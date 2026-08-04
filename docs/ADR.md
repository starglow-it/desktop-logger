# Architecture decision records

## ADR-001 — Native .NET and WPF

Accepted. Use .NET 10 and WPF for the employee and manager applications, with ASP.NET Core/SignalR for local services. This aligns with Windows lifecycle, accessibility, service hosting and signing. Electron is not used.

## ADR-002 — Separate interactive and service processes

Accepted. Screenshot/UI work must remain in the signed-in user session. Reliability, policy sync and uploads belong in one normal Windows Service. Communication will use a restricted named pipe. No hidden mutual-resurrection processes are permitted.

## ADR-003 — SQLite WAL behind EF Core

Accepted for up to 50 devices. WAL, bounded transactions and indexed idempotency fields keep operations simple on one manager PC. Domain services do not depend on SQLite-specific APIs, leaving a PostgreSQL provider possible later.

## ADR-004 — Fail-closed security phases

Accepted. The server is loopback-only and screenshot/remote media endpoints are disabled until authentication, consent and retention gates pass. Shipping a disabled feature is safer and more truthful than exposing a partially secured path.

## ADR-005 — UTC event time, local schedule interpretation

Accepted. Events are stored as `DateTimeOffset` in UTC. Work schedules contain a Windows/IANA time-zone identifier and local wall-clock times. Schedule evaluation converts each real UTC instant to local time, preventing ambiguous fall-back hours from being collapsed.

## ADR-006 — Encrypted blobs separate from relational metadata

Accepted. SQLite stores indexed screenshot metadata; AES-GCM encrypted files are stored under server-generated date partitions. This supports retention deletion, storage forecasts and a future PostgreSQL move without inflating the database.

## ADR-007 — Portable preview before trusted installer

Accepted temporarily. CI publishes a self-contained ZIP for v0.1. Production MSI creation and Authenticode signing require the organization-controlled certificate and completed service/firewall lifecycle tests. A self-signed certificate will not be represented as a trusted production signature.
