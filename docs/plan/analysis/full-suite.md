# Full Suite Delivery Analysis

## Scope

The complete One Page suite is delivered as one product with a shared control plane and explicit
business modules. The modules are all planned, but not all can start at once because they depend on
tenant persistence, authorization, workflows, audit, files, and reporting contracts.

## Dependency flow

```text
Platform bootstrap
  -> Tenant persistence + authorization
  -> Workflows + audit + billing + documents/integrations
  -> HR -> Payroll
  -> Procurement + Expenses + Assets
  -> Finance integration
  -> Inventory -> POS
  -> CRM + Work management
  -> Reporting + vertical templates
```

## Module ownership

| Module | Owns | Consumes |
|---|---|---|
| HR | employees, employment, leave, attendance | platform org, workflows, files |
| Payroll | pay groups, periods, calculations, payslips | HR snapshots, country rules, audit |
| Procurement | vendors, requisitions, POs, receipts | org, workflows, budgets, files |
| Expenses | claims, policies, reimbursements | users, workflows, finance export |
| Assets | register, custody, maintenance, disposal | procurement receipts, org, files |
| Finance | journals, mappings, reconciliation | payroll, procurement, expenses, POS |
| Inventory | items, stock, warehouses, movements | procurement, POS, assets |
| POS | stores, tills, sales, refunds, sessions | inventory, permissions, payments |
| CRM | customers, contacts, opportunities, interactions | party/org, files, work |
| Work | tasks, projects, service requests, field visits | users, workflows, files, CRM |
| Reporting | read models, dashboards, exports | versioned events from all modules |

## Cross-module integration tasks

Each connection must use real implementations and integration tests:

1. HR employee lifecycle -> payroll employee snapshot and access changes.
2. Procurement receipt -> asset creation or inventory movement.
3. Payroll/procurement/expenses/POS -> finance journal or export contract.
4. Inventory -> POS stock reservations, sale movements, and returns.
5. CRM/customer -> work request and reporting identity.
6. All modules -> audit, reporting events, documents, notifications, and entitlement checks.

## Full-suite completion criteria

- Every module has an owner, contract, persistence boundary, permission catalog, entitlement catalog,
  audit requirements, migration path, and contract tests.
- Every cross-module path has an integration test using real implementations.
- Every user-observable MVP journey has an end-to-end test.
- Subscription downgrade preserves historical read/export behavior.
- No module ships with a fake integration boundary.
