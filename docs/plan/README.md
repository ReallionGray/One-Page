# One Page Delivery Plan

## Current delivery strategy

Build the complete One Page suite as a .NET 10 modular monolith with explicit bounded contexts,
shared platform contracts, and an eventual extraction path for high-scale modules. Every requested
module remains in scope. Delivery is dependency-ordered: platform contracts first, then core business
modules, then transaction-heavy and vertical modules.

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
| platform-001 | Solution bootstrap, tenant context, entitlement contract | done | — |
| platform-002 | Persistent tenant and organization model | done | platform-001 |
| platform-003 | Permission catalog and scoped authorization | done | platform-002 |
| platform-004 | Versioned workflows and approval execution | pending | platform-002, platform-003 |
| platform-005 | Append-only audit service | pending | platform-001, platform-002 |
| platform-006 | Billing, plan versions, subscriptions, and usage | pending | platform-002, platform-001 |
| platform-007 | Documents, notifications, search, and import/export | pending | platform-002, platform-005 |
| platform-008 | Reporting read models and integration hub | pending | platform-004, platform-005 |
| hr-001 | HR employee and organization module | pending | platform-002, platform-003 |
| payroll-001 | Launch-country payroll specification and engine | blocked pending country decision | hr-001, platform-004, platform-005 |
| procurement-001 | Procurement and purchase-to-receipt module | pending | platform-004, platform-005, platform-007 |
| expense-001 | Expense policy, claims, and reimbursement module | pending | platform-004, platform-005, platform-007 |
| assets-001 | Asset register, custody, maintenance, and disposal | pending | platform-004, platform-005, platform-007 |
| finance-001 | Finance integration, journal export, and reconciliation | pending | platform-005, platform-008, payroll-001, procurement-001 |
| inventory-001 | Inventory, warehouse, stock movement, and valuation | pending | platform-004, platform-005, procurement-001 |
| pos-001 | POS stores, tills, sales, refunds, and offline sync | pending | inventory-001, platform-003, platform-005, platform-008 |
| crm-001 | Customer, contact, and vendor relationship management | pending | platform-002, platform-003, platform-007 |
| work-001 | Tasks, projects, service requests, and field work | pending | platform-004, platform-007 |
| reporting-001 | Cross-module dashboards, exports, and analytics | pending | platform-008, hr-001, payroll-001, procurement-001, assets-001, finance-001 |
| vertical-001 | Retail and distribution templates | pending | inventory-001, pos-001, procurement-001 |
| vertical-002 | Education, healthcare, NGO, and services templates | pending | hr-001, finance-001, work-001, crm-001 |

## Delivery gates

- No business module bypasses tenant context, permission, or entitlement contracts.
- Every task has contract tests and typed error behavior where applicable.
- Verification checks code against the referenced docs and rejects stubs at integration boundaries.
- Payroll remains blocked until the launch country and compliance matrix are approved.
- A task is not complete while its integration path is a stub, fake, or unverified mock.
- POS remains a full-scope module but requires offline, device, payments, reconciliation, and security
  acceptance tests before production release.
