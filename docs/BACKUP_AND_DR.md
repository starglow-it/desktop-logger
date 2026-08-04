# Backup and disaster recovery

## Scope

Back up the SQLite database, WAL state through SQLite's online backup API (future operational command), encrypted file store, DPAPI-wrapped key metadata, server TLS identity, and configuration. Never copy only the live `.db` file while writes are active.

## Target procedure

1. Quiesce retention/export jobs.
2. Create a SQLite online backup to a timestamped staging directory.
3. Copy encrypted files and key metadata using an incremental, integrity-checking tool.
4. Encrypt the backup with an organization-controlled recovery key.
5. Write a manifest containing hashes, schema version, app version and capture time.
6. Store off-device under least-privilege access and the configured backup retention.
7. Restore quarterly into an isolated Windows VM, run `PRAGMA integrity_check`, verify audit chains and decrypt a designated test asset.

## Recovery objectives

Initial targets: RPO 24 hours, RTO 4 hours. Administrators must approve values based on legal and business requirements.

## v0.1 limitation

The database schema and backup design exist, but an online-backup command and automated restore test are not yet implemented. Do not treat a file copy as a tested backup.
