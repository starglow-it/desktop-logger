# Implementation status

Status: v0.1 security foundation, August 2026.

| Capability | Status | Evidence / limitation |
|---|---|---|
| Repository and .NET 10 solution | Implemented | Eight source projects and three test projects in `TeamActivity.slnx`. |
| Enrollment code | Implemented | Random 80-bit code, SHA-256 at rest, one use, 1–30 minute expiration. |
| Device authentication | Development implementation | Server-generated bearer token is hashed at rest. Replace with client certificates and mTLS before LAN use. |
| Manager authentication | Localhost boundary only | Manager mutations require a loopback caller. Windows Hello/MFA and full RBAC remain required. |
| Heartbeat | Implemented | Agent worker posts every 30 seconds when configured. Two missed intervals degrade; three mark offline. |
| Aggregate activity schema | Implemented | Timestamps, counts, movement distance, and state only. Unknown JSON members are rejected. Native Windows input sampler remains pending. |
| Screenshot scheduling rules | Implemented | Cryptographic random 5–10 minute default and deterministic tests. |
| Screenshot capture/upload | Disabled | API returns 501 until interactive-session consent, exclusions, mTLS, queue, and retention deletion are complete. |
| AES-GCM file storage primitive | Implemented | Random nonce, authentication tag, safe server-generated paths, round-trip tests. DPAPI key wrapping remains pending. |
| Manager dashboard | Implemented foundation | Device list, heartbeat state, version, activity state placeholder, alert totals. |
| Employee tray UI | Implemented foundation | Persistent visible icon, tracking/break controls, policy and privacy copy. State persistence and native telemetry remain pending. |
| Quality-review database model | Implemented schema | Workflow UI, task CSV import, rubric and reports remain pending. |
| Time corrections database model | Implemented schema | Submission/approval UI and audit workflow remain pending. |
| Remote-support consent rules | Implemented | No media/view/control transport exists; remote support remains disabled. |
| Audit chain | Implemented | Chained SHA-256 tamper evidence. Add key-backed checkpoints before production. |
| Retention configuration | Implemented schema | Scheduled deletion, legal hold, forecasting and secure deletion jobs remain pending. |
| Backup | Documented | Operational scripts and automated restore verification remain pending. |
| Portable release | Implemented | Self-contained Windows x64 ZIP produced by GitHub Actions. |
| Signed MSI | Blocked on certificate and installer phase | Signing script contract is documented; no certificate is stored in the repository. |

## Phase gates

1. Do not change the server bind address from loopback until TLS, mTLS, certificate rotation/revocation, manager authentication, firewall tests, and replay protection pass.
2. Do not enable screenshot capture until the visible consent flow, protected-app exclusions, secure-desktop detection, multi-monitor capture, encrypted offline queue, deletion workflow, and employee history/reporting pass.
3. Do not implement remote transport until the always-on-top banner, verified manager identity, separate permissions, immediate revocation, session keys, lock/sign-out termination, and audit tests pass.
4. Do not ship an installer as production-signed until an organization-controlled Authenticode certificate and protected CI signing process are configured.
