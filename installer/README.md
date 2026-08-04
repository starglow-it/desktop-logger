# Installer and signing

The production target is a per-machine WiX v6 MSI with separate manager and agent features, service registration, bounded SCM recovery, Private/Domain-only firewall rules, repair/upgrade/uninstall support, and Authenticode signatures.

v0.1 publishes a self-contained ZIP while those lifecycle tests and the organization-controlled signing certificate are unavailable. `scripts/sign-release.ps1` documents the CI signing contract without generating or committing a self-signed identity.

Required protected CI inputs:

- Base64-encoded organization PFX secret.
- PFX password secret.
- RFC 3161 timestamp URL approved by the organization.
- Optional hardware-backed or managed signing service configuration.

Never store a PFX, password, client certificate, enrollment token or AES key in this repository.
