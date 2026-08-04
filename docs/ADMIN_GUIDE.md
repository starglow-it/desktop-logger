# Administrator guide

## v0.1 operations

The manager server stores data under `%ProgramData%\TeamActivity` unless `DataRoot` is changed. It binds to localhost and initializes the SQLite schema and default retention records on first start.

Create an employee record, create a 1–30 minute enrollment code, have the authorized employee/IT user review the policy and enroll, then approve the pending device and assign the server-side employee record. The agent must protect the returned token; do not place it in source control or ordinary logs.

## Health interpretation

- Online: heartbeat newer than two 30-second intervals.
- Degraded: two intervals missed.
- Offline: three intervals missed.
- Pending: enrolled but not yet producing an accepted heartbeat.
- Revoked: administrator-invalidated device.

A missed heartbeat is an inferred connectivity/health event, not proof of tampering. Confirmed uninstall/disable events require independent reliable evidence.

## Operational prohibitions

Do not enable or claim screenshot/remote-support functionality in v0.1. Do not use activity counts as quality scores or disciplinary evidence. Do not expose the HTTP loopback endpoint to a LAN.
