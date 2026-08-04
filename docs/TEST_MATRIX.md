# Test and requirements matrix

| Requirement | Automated evidence | Status |
|---|---|---|
| Random screenshot interval boundaries | `ScreenshotIntervalTests` | Implemented |
| Multi-monitor grouping and removal | `ScreenshotGroupingTests` | Rule implemented; native capture pending |
| Work-schedule boundary | `ScheduleTests.End_boundary_is_exclusive` | Implemented |
| DST fall-back transition | `ScheduleTests.Both_repeated_fall_back_hours...` | Implemented |
| Idle threshold and boundary | `ActivityStateTests` | Implemented |
| Lock/sign-in/sign-out/break priority | `ActivityStateTests.Explicit_session_states...` | Implemented |
| Missed heartbeats and recovery | `HeartbeatTests` | Implemented |
| Raw keystroke prohibition | `PrivacyContractTests`, integration unknown-field test | Implemented |
| AES-GCM round trip and path safety | `EncryptedFileStoreTests` | Implemented |
| Remote consent/denial/revocation/timeout | `RemoteSupportConsentTests` | Consent rules implemented; transport disabled |
| Audit tamper detection | `AuditTests` | Hash rule implemented |
| Server health | `ServerTests.Health_endpoint_is_available` | Implemented |
| Offline upload queue/backoff/dedup | None | Pending; screenshot feature disabled |
| Certificate expiry/revocation and mTLS | None | Pending; server loopback-only |
| Role authorization | None | Pending; local manager boundary only |
| Screenshot retention deletion | None | Pending; screenshot feature disabled |
| Time correction workflow | None | Schema only |
| Secure desktop exclusion | Skipped E2E test | Pending; capture disabled |
| Microphone prohibition | Architectural review only | No media implementation exists |
| Database online backup/restore | None | Pending |
| Installer upgrade/repair/uninstall | Skipped E2E test | Pending signing/installer phase |

Skipped tests document unavailable external prerequisites or intentionally disabled capabilities. They are not counted as evidence that a feature works.
