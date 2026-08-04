# Threat model and privacy-impact assessment

## Assets

Employee identity, schedule and attendance; aggregate activity records; future screenshots; device credentials; encryption keys; audit history; quality reviews; support consent and session records.

## Threats and required controls

| Threat | Impact | Control / gate |
|---|---|---|
| Stolen enrollment code | Rogue device enrollment | 80-bit random code, short expiry, single use, server approval, audit; rate limit. |
| Client-supplied identity spoofing | Misattribution | Client provides no authoritative employee name; manager assigns an existing server record. |
| Token/certificate theft | Impersonation | v0.1 tokens are hashed server-side; phase 2 requires DPAPI-protected client key, mTLS, rotation and revocation. |
| LAN interception/replay | Data disclosure or duplicate events | Server remains loopback-only in v0.1; phase 2 requires TLS/mTLS, timestamp/nonce windows and idempotency keys. |
| Raw input capture | Severe privacy harm | No raw-key/button field exists; unmapped JSON is rejected; future sampler code review forbids content APIs. |
| Sensitive screenshot | Password/banking disclosure | Screenshot feature fail-closed until exclusions, secure-desktop detection, working-hours/lock/consent gates and employee removal process pass. |
| Screenshot path traversal | Arbitrary file access | Server-generated GUID paths and canonical root check. |
| Database/file theft | Confidentiality loss | AES-GCM file primitive exists; DPAPI-wrapped keys and database protection are production gates. |
| Manager overreach | Unauthorized view/export/delete | Future Windows auth, MFA/Hello, RBAC, reason/confirmation and audit on every sensitive access. |
| Covert remote access | Safety/privacy harm | No transport in v0.1; consent state requires explicit capabilities and immediate revoke. No unattended mode. |
| Audit modification | Loss of accountability | Chained hashes detect edits; production needs key-backed checkpoints and protected exports. |
| Storage exhaustion | Service failure | Request limits now; queue/storage caps, forecasting and scheduled retention are phase 2. |
| Process resurrection loop | Device instability | One normal service and one visible tray app; SCM bounded recovery only; no mutual resurrection processes. |

## Data minimization

Collected now: installation hash, app version, policy acceptance, heartbeat health, pending-upload count, aggregate activity counts/timestamps/state, and manager-created administrative records.

Not collected: typed keys/text, mouse-button identity, clipboard, microphone, secure desktop, personal messages, browser content, or automatic quality/discipline recommendations. Screenshot and remote media are disabled.

## Purpose and proportionality

Attendance, availability, device health and user-approved support are legitimate workplace operations when disclosed and governed. Input volume is not a reliable work-quality measure; quality is recorded only through task-specific human review. The 30-day screenshot default is a maximum starting point and should be reduced where business needs allow.

## Employee rights workflow

The target product provides personal history, policy and retention visibility, time correction, response to quality review, and screenshot report/removal request. v0.1 displays policy and work/break controls; history/correction/removal workflows must be completed before screenshot enablement.

## Residual risk

v0.1 is a localhost development foundation. It is not approved for employee data collection or LAN deployment. Enabling deferred capabilities without their phase gates would create unacceptable privacy and security risk.
