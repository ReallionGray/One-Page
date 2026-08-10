# One Page Documentation Index

One Page is a modular, multi-tenant corporate operations suite. The product blueprint at
`OnePage_Product_Blueprint.txt` is the strategic source of truth. These documents translate it into
implementation contracts and executable delivery tasks.

## Scope map

- [Platform scope](platform/README.md): tenant context, identity/authorization boundaries,
  entitlements, workflows, audit, billing, files, notifications, and integration rules.
- [Delivery plan](plan/README.md): task status, dependencies, analysis, reviews, and backlog.

## Working rules

- Design docs define contracts; code follows the docs.
- Cross-module ownership belongs in scope READMEs; local API and error contracts belong in module docs.
- A task that changes a contract updates its design doc in the same delivery.
- Payroll and country-specific compliance remain gated until the launch country is selected and reviewed
  by qualified specialists.
