# Human Resources Scope

## Purpose and boundary

HR owns employee lifecycle records and people operations. It owns employee identity inside a tenant,
employment history, organization placement, leave, onboarding/offboarding, employee documents, and
basic attendance inputs.

HR does not own payroll calculations, financial journals, authentication credentials, or country tax
rules. Payroll consumes effective-dated HR snapshots and owns payroll-period results.

## Lifecycle

```text
Candidate/Preboarding -> Active -> Leave/Changes -> Offboarding -> Terminated
```

Every transition is authorized, effective-dated, attributable, and auditable. Termination must expose
an integration point for access review without directly deleting identity records.

## Core concepts

- Employee: tenant-owned person record with stable ID and sensitive-field controls.
- Employment: effective-dated relationship to legal entity, department, position, manager, and location.
- Leave policy: rules for entitlement, accrual, carryover, and approval.
- Leave request: employee-submitted request with decision history and balance impact.
- Employee document: tenant-scoped metadata and file reference with access and expiry controls.

## Ownership and integrations

- HR creates employee/employment records and emits lifecycle events.
- Payroll consumes immutable effective-dated employment/compensation snapshots.
- Workflows provide approval execution when available; until then, HR must not invent a second approval engine.
- Platform authorization and entitlements are mandatory on every API command.

## Required constraints

- Every record is tenant-scoped.
- Employee identifiers and required names fail fast with typed validation errors.
- Sensitive fields are not returned to unauthorized roles.
- Historical employment and leave decisions are retained; destructive deletion is not used for history.
- Leave balances and changes are explainable from transactions and policy versions.
