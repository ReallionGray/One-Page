# One Page Delivery Plan

## Current delivery strategy

Start with a .NET 10 modular monolith. The first slice establishes the solution boundary and the
smallest reusable platform contracts: tenant context and subscription entitlements. Business modules
will be added only after these contracts have tests and stable ownership rules.

## Task flow

```text
pending -> ready -> in-progress -> verify -> done
                         |            |
                       blocked <------+
```

Each task references its design context, code/doc paths, dependencies, and verification. Development
and verification are separate activities.

## Task index

| ID | Scope | Status | Depends on |
|---|---|---|---|
| platform-001 | Solution bootstrap, tenant context, entitlement contract | ready | — |
| platform-002 | Persistent tenant and organization model | pending | platform-001 |
| platform-003 | Permission catalog and scoped authorization | pending | platform-001 |
| platform-004 | Versioned workflows and approval execution | pending | platform-002, platform-003 |
| platform-005 | Append-only audit service | pending | platform-001, platform-002 |
| hr-001 | HR employee and organization module | pending | platform-002, platform-003 |
| payroll-001 | Launch-country payroll specification and engine | blocked pending country decision | hr-001, platform-004, platform-005 |

## Delivery gates

- No business module bypasses tenant context, permission, or entitlement contracts.
- Every task has contract tests and typed error behavior where applicable.
- Verification checks code against the referenced docs and rejects stubs at integration boundaries.
- Payroll remains blocked until the launch country and compliance matrix are approved.
