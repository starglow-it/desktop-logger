# Product requirements

## Objective

Provide a local-network Windows 11 application for a team of up to 50 company-managed devices that lets employees transparently record attendance and aggregate activity states while managers review device health, timesheets, screenshots (future phase), corrections, and separately assessed work quality.

## Principles

1. Collection is disclosed, policy-versioned, visible, and limited to working hours.
2. Raw input content is never collected.
3. Activity telemetry is not productivity or quality.
4. Work quality is a separate human review tied to tasks, outcomes, evidence, and employee response.
5. Remote support is attended, separately permissioned, revocable, and visibly indicated.
6. Unknown or missing telemetry is classified as unknown, not inferred as active, idle, or misconduct.
7. Confirmed events and inferred health conditions are labeled separately.

## Primary users

- Employee: controls work/break state, sees policy, activity and personal screenshot history, requests corrections/deletion, and grants support consent.
- Manager: views assigned employees, timesheets, health, alerts, screenshots, and manual quality reviews.
- Reviewer: performs task-quality reviews without administrative access.
- Auditor: reads immutable audit and retention evidence.
- Administrator: enrolls devices, configures policy, retention, certificates, backup, RBAC, and legal hold.

## Functional requirements

The target product requirements are the detailed specification supplied with the project. The implementation is phased. A capability is not accepted until its code, automated tests, security controls, user documentation, and failure states are complete. Current acceptance is tracked in `IMPLEMENTATION_STATUS.md` and `TEST_MATRIX.md`.

## Non-functional requirements

- Windows 11 x64; .NET 10; WPF; Windows Services; ASP.NET Core; SignalR; REST; EF Core; SQLite WAL.
- Up to 50 enrolled devices with 30-second heartbeats.
- No cloud dependency for runtime operation.
- Keyboard navigation, high-DPI support, explicit loading/offline/error/permission states.
- Bounded disk usage, queues, retries and service recovery.
- TLS 1.3 where supported, mTLS, encryption at rest, DPAPI-protected keys, signed updates and artifacts before production LAN deployment.
- Deterministic clock-driven tests with no real-time waits.

## Acceptance policy

“Implemented” means compiled on Windows, tests pass, and a working path exists in the released binary. Schema-only or UI-only work is labeled foundation, not complete functionality. Deferred security gates are fail-closed.
