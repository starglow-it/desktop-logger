# Local deployment guide

## v0.1 development deployment

1. Use an isolated Windows 11 test PC or VM.
2. Download the release ZIP and its `.sha256` file.
3. Verify the checksum with `Get-FileHash TeamActivity-0.1.0-win-x64.zip -Algorithm SHA256`.
4. Extract to a protected test folder.
5. Run `ManagerServer/TeamActivity.Manager.Server.exe`; it binds only to `127.0.0.1:7443`.
6. Run `ManagerDesktop/TeamActivity.Manager.Desktop.exe`.
7. Run `AgentDesktop/TeamActivity.Agent.Desktop.exe` only to inspect the transparent employee UI.

Do not copy the agent to employee PCs or change the server bind address in v0.1. The current enrollment token is a development bridge and not a replacement for mTLS.

## Production LAN gate

Before LAN deployment, complete and verify:

- Organization certificate authority or approved internal PKI; server TLS certificate; device client certificates; rotation and revocation.
- Windows-authenticated manager roles plus MFA/Windows Hello where feasible.
- Restricted named pipe between tray and service.
- DPAPI protection for device credentials and AES data keys.
- Explicit network-interface selection, Private/Domain-profile firewall rules, and negative tests from unapproved networks.
- Signed MSI upgrade/repair/uninstall and SCM recovery configuration.
- Backup/restore test, retention jobs, storage caps, alerting and operational runbook.

The required ports must be configurable. Never create an Any/Any/Public firewall rule.
