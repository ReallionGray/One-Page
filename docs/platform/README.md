# Platform Scope

## Purpose and boundary

The platform provides reusable controls required by every One Page module. It owns tenant context,
organization boundaries, identity memberships, permission/entitlement evaluation, workflow
infrastructure, audit, billing state, documents, notifications, integrations, and reporting access.

Business modules own their domain data and rules. The platform must not become a generic domain store
or allow modules to write each other's state directly.

## Relationship to business modules

```text
Tenant + User Context
        |
Permissions + Entitlements
        |
Workflow / Audit / Files / Notifications
        |
HR | Payroll | Procurement | Expenses | Assets | Inventory | POS | Finance
```

The initial implementation is a modular monolith. Cross-module calls use explicit application
contracts; asynchronous propagation uses versioned, tenant-scoped events through a transactional outbox.

## Ownership

- Platform creates and owns tenants, legal entities, memberships, roles, permissions, plans,
  subscriptions, entitlements, workflow instances, audit events, and platform configuration.
- HR owns employee records; Payroll consumes HR data and owns immutable payroll-period snapshots.
- Procurement owns vendors, requisitions, purchase orders, and receipts.
- Assets owns the asset register, assignment, maintenance, and disposal history.
- A module may reference another module's identifier but may not mutate another module's tables.

## Tenant context contract

Every authenticated command/query executes with a validated context containing:

```text
user_id, tenant_id, optional legal_entity_id, optional scope, correlation_id
```

Tenant identity is derived from authenticated membership or a verified service/partner credential,
never from an untrusted request body. All persistence, cache, file, event, and report access is tenant-scoped.

## Entitlement contract

An entitlement answers whether a tenant may use a module or feature and whether a numeric limit is
available. It is separate from user permission. Both must pass before a command executes.

Required states: available, trial, active, suspended, grace_period, expired, and read_only.
Plan versions are immutable and subscription changes are effective-dated.

## Module index

- `platform-001` (implementation task): bootstrap and platform contract foundation.
- Future docs: identity, entitlements, workflows, audit, billing, files, and notifications.

## Scope-wide constraints

- Required state fails fast with explicit errors.
- Optional lookup absence returns an absent value, not a fabricated default.
- Sensitive actions are auditable.
- Historical records remain readable/exportable after normal subscription downgrade.
- Payroll rules and compliance behavior require country-specific design approval.
