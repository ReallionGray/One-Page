---
id: platform-003
scope: platform authorization
status: ready
depends-on: [platform-002]
---

# Objective

Implement the One Page permission catalog and scoped authorization layer. Authorization must combine
tenant membership, role permissions, organization scope, amount limits, active state, and explicit
action checks. The layer will be reused by HR, Payroll, Procurement, Expenses, Assets, Finance,
Inventory, POS, CRM, Work, and Reporting.

# Context

- `OnePage_Product_Blueprint.txt`
- `docs/INDEX.md`
- `docs/platform/README.md`
- `docs/plan/analysis/full-suite.md`
- `docs/plan/tasks/platform-002.md`

# Required behavior

1. Permissions are action-oriented identifiers such as `employee.view`, `payroll.run`,
   `purchase_order.approve`, and `report.export`.
2. A role maps to one or more permissions and may be assigned only within a tenant.
3. Authorization requires an active tenant membership; inactive memberships are denied.
4. A permission can be scoped to legal entity, branch, department, location, or manager chain.
5. Amount limits are enforced for approval actions.
6. Missing permission, inactive membership, missing scope, and exceeded amount limit return distinct
   typed denial reasons.
7. Authorization decisions require a validated tenant context and cannot be satisfied by request
   headers alone.
8. API endpoints demonstrate server-side authorization; UI visibility is not treated as security.
9. The permission catalog is extensible without changing existing module contracts.
10. Contract and integration tests cover allow, deny, cross-tenant, inactive membership, scope, and
    amount-limit behavior.

# Path

- `src/OnePage.Platform/`
- `src/OnePage.Api/`
- `tests/OnePage.Platform.Tests/`
- `docs/platform/README.md` if authorization contracts need refinement
- `docs/plan/README.md` and this task file if status/dependencies change

# Verification

- `dotnet build OnePage.sln`
- `dotnet test OnePage.sln`
- Real API integration tests for authorized and denied requests.
- No module may bypass the authorization evaluator for the demonstrated endpoints.
