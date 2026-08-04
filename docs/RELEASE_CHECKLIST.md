# Release checklist

## Every release

- [ ] Update version, implementation status and release notes.
- [ ] Restore, build and test the entire solution on Windows 11 tooling.
- [ ] Review skipped tests; no skipped test may cover an enabled capability.
- [ ] Run dependency vulnerability audit.
- [ ] Verify no secrets, PFX files, tokens, employee data or generated databases are committed.
- [ ] Verify server bind interfaces and firewall scope.
- [ ] Render/test both WPF apps at 100%, 150% and 200% DPI with keyboard navigation.
- [ ] Verify accessibility names, offline/loading/error/permission states and privacy labels.
- [ ] Publish self-contained `win-x64` binaries from the tagged commit.
- [ ] Authenticode-sign production executables and installer with the protected organization certificate.
- [ ] Verify signatures, hashes and package integrity on a clean Windows 11 VM.
- [ ] Test install, upgrade, repair, rollback and uninstall.
- [ ] Perform backup and restore drill.
- [ ] Attach SHA-256 checksum and provenance to the GitHub release.

## v0.1 preview exception

The first release is an unsigned developer preview ZIP because no organization signing certificate is available in this environment. It must not be represented or deployed as a production employee-monitoring release.
