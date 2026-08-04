# TeamActivity

TeamActivity is a transparent, consent-based Windows 11 team activity foundation for company-managed computers on a local network. It separates aggregate activity telemetry from human work-quality review and intentionally does not implement covert monitoring.

## v0.1 scope

This repository was initialized from an empty repository. Version 0.1 is the first buildable security foundation, not the finished surveillance-sensitive product.

Implemented and testable:

- .NET 10 solution with employee WPF app, manager WPF app, agent Windows Service, and manager ASP.NET Core Windows Service.
- One-time, expiring enrollment codes; server-generated device IDs and tokens; manager approval and employee assignment.
- Thirty-second heartbeat transport and deterministic online/degraded/offline classification.
- Sixty-second aggregate activity contract with timestamps and event counts only—never key values, mouse-button values, clipboard contents, or typed text.
- Explicit active, idle, approved-break, locked, signed-out, offline, and unknown states with deterministic boundary rules.
- SQLite model, WAL mode, indexes, foreign keys, optimistic concurrency markers, soft deletion, and default retention records.
- AES-256-GCM encrypted file-store primitive with safe server-generated paths.
- Tamper-evident chained audit-event hashes.
- Remote-support consent state machine: separate view/control/system-audio grants, denial, immediate revocation, lock/sign-out termination, and timeout.
- Visible employee tray application and a manager team-health dashboard.
- Unit and integration tests plus a Windows CI/release workflow.

Intentionally disabled in v0.1:

- Screenshot capture and upload.
- Live remote viewing, control, and system audio.
- LAN exposure, TLS/mTLS certificate issuance, MFA/Windows Hello, production RBAC, signed MSI installation, PDF reporting, and updater.

The server binds to `127.0.0.1` by default. Do not expose it to a LAN until the certificate and authentication phase is complete. See [implementation status](docs/IMPLEMENTATION_STATUS.md).

## Prerequisites

- Windows 11 x64
- .NET 10 SDK (Visual Studio 2022 with .NET desktop development is also supported)
- PowerShell 7 recommended for packaging scripts

## Build and test

```powershell
dotnet restore TeamActivity.slnx
dotnet build TeamActivity.slnx -c Release --no-restore
dotnet test TeamActivity.slnx -c Release --no-build
```

## Run locally

Start the local manager service as a console process:

```powershell
dotnet run --project src/TeamActivity.Manager.Server
```

Then start the manager and employee apps in separate terminals:

```powershell
dotnet run --project src/TeamActivity.Manager.Desktop
dotnet run --project src/TeamActivity.Agent.Desktop
```

The agent service does not transmit until it is enrolled and its `DeviceId` and `DeviceToken` are configured. Never commit a real device token.

## Produce the Windows release bundle

```powershell
pwsh ./scripts/package-release.ps1 -Version 0.1.0
```

The command creates a self-contained `win-x64` ZIP and SHA-256 checksum under `artifacts/release`. GitHub Actions performs the same process on a Windows runner and publishes the first `v0.1.0` release.

## Documentation

- [Product requirements](docs/PRODUCT_REQUIREMENTS.md)
- [Architecture and data flow](docs/ARCHITECTURE.md)
- [Threat model and privacy-impact assessment](docs/THREAT_MODEL_AND_PIA.md)
- [Architecture decisions](docs/ADR.md)
- [Deployment guide](docs/DEPLOYMENT.md)
- [Administrator guide](docs/ADMIN_GUIDE.md)
- [Employee privacy guide](docs/EMPLOYEE_PRIVACY.md)
- [Backup and disaster recovery](docs/BACKUP_AND_DR.md)
- [Security review checklist](docs/SECURITY_REVIEW_CHECKLIST.md)
- [Release checklist](docs/RELEASE_CHECKLIST.md)
- [Test and requirements matrix](docs/TEST_MATRIX.md)

## Safety guarantees

TeamActivity must remain visible, policy-driven, and consent-based. Contributions that add raw key logging, covert capture, unattended remote access, microphone capture, secure-desktop access, restart-resurrection loops, or automated disciplinary scoring are out of scope and will not be accepted.

## License

Copyright © 2026 Starglow IT. No license has been granted yet; add an approved company license before external distribution.
