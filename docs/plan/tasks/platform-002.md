---
id: platform-002
scope: platform persistence and organization model
status: done
depends-on: [platform-001]
---

# Objective

Implement persistent tenant and organization foundations for One Page using PostgreSQL and EF Core.
Add tenant, legal entity, branch, department, location, cost center, user membership, and organization
context persistence with tenant-safe repositories and integration tests.

# Context

- `OnePage_Product_Blueprint.txt`
- `docs/INDEX.md`
- `docs/platform/README.md`
- `docs/plan/analysis/foundation.md`
- `docs/plan/analysis/full-suite.md`

# Required behavior

1. A tenant can be created and retrieved by its stable identifier.
2. Tenant-owned records cannot be read or mutated through another tenant context.
3. Legal entities, branches, departments, locations, and cost centers are tenant-scoped.
4. User memberships are tenant-scoped and support active/inactive state.
5. Required identifiers and names fail fast with explicit validation errors.
6. Persistence uses real EF Core/PostgreSQL-compatible implementations; tests may use a disposable
   test database strategy but must exercise the repository contract, not a dictionary fake.
7. Tenant context flows from API request to repository query.
8. Database migrations or schema creation are reproducible from a clean checkout.
9. The existing entitlement evaluator remains compatible with the persisted tenant identifier.

# Path

- `OnePage.sln`
- `src/OnePage.Platform/`
- `src/OnePage.Api/`
- `tests/OnePage.Platform.Tests/`
- `docs/platform/README.md` if persistence contracts require clarification
- `docs/plan/tasks/platform-002.md`

# Verification

- `dotnet build OnePage.sln`
- `dotnet test OnePage.sln`
- Repository contract tests for create/read/update, missing required data, tenant isolation,
  membership state, and API context propagation.
- Clean-schema migration or initialization test.
