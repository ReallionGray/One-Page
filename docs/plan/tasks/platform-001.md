---
id: platform-001
scope: platform foundation
status: done
depends-on: []
---

# Objective

Bootstrap the One Page .NET 10 modular-monolith solution and implement the first executable platform
contracts: immutable tenant context, entitlement keys/states, entitlement decisions, an in-memory
entitlement evaluator, a health endpoint, and contract tests.

This is a foundation slice, not a business-module implementation.

# Context

- `OnePage_Product_Blueprint.txt`
- `docs/INDEX.md`
- `docs/platform/README.md`
- `docs/plan/analysis/foundation.md`

# Required behavior

1. The solution compiles with the installed .NET SDK.
2. The API host exposes a basic health endpoint.
3. Tenant context is immutable and requires a non-empty user ID, tenant ID, and correlation ID.
4. Missing required context fails fast with an explicit typed error.
5. Entitlement keys support module, feature, and limit namespaces without relying on string literals
   spread throughout callers.
6. Entitlement states include `available`, `trial`, `active`, `suspended`, `grace_period`, `expired`,
   and `read_only`.
7. Active/trial/available entitlements allow an entitled operation; suspended/expired/read-only do not
   allow a new write operation unless the evaluator is explicitly asked for historical read access.
8. A missing entitlement returns a typed denial decision; it must not silently default to enabled.
9. Numeric limits expose current usage and limit and deny requests that exceed the limit.
10. API denial behavior is covered by an integration test using the real evaluator.

# Path

Expected paths (the developer may refine within this boundary):

- `OnePage.sln`
- `src/OnePage.Api/`
- `src/OnePage.Platform/`
- `tests/OnePage.Platform.Tests/`
- `docs/platform/README.md` if contract details need refinement
- `docs/INDEX.md` if new module docs are added

# Verification

- `dotnet build OnePage.sln`
- `dotnet test OnePage.sln`
- Contract tests for valid/invalid tenant context, missing entitlements, each entitlement state,
  limit boundaries, and API denial.
- No placeholder implementation, mock/fake at the platform integration boundary, or silent default
  may remain.
