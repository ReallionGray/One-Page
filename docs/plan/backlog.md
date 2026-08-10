# One Page Backlog

Non-blocking or intentionally deferred work is recorded here rather than silently expanding the
current task.

- Select initial launch country and obtain payroll/data-protection specialist review before payroll work.
- Select identity provider and implement OIDC/MFA/enterprise SSO in a later platform task.
- Replace in-memory entitlement provider with versioned persistent plans/subscriptions.
- Add PostgreSQL tenant persistence and database-level isolation tests.
- Define workflow engine contract and approval-specific UX.
- Define full audit retention, export, and tamper-detection requirements.
- Add billing provider adapters after pricing and payment jurisdiction are selected.
- Harden `EntitlementKey` construction so unsupported namespaces cannot bypass the typed catalog.
- Make runtime entitlement updates concurrency-safe when persistence/subscription updates are introduced.
- Add database-level tenant isolation defense-in-depth (for example PostgreSQL RLS) beyond repository checks.
- Add a tenant-scoped membership update command and cross-tenant update tests.
- Remediate or formally risk-accept the high-severity `SQLitePCLRaw.lib.e_sqlite3` test dependency warning.
