# Foundation Delivery Analysis

## Objective

Create the smallest executable foundation that future One Page modules can depend on without deciding
country-specific payroll rules or prematurely implementing business modules.

## Module decomposition

### Solution/bootstrap

Inputs: build invocation and application configuration.
Outputs: compilable .NET solution, API host, test projects, shared domain contracts.
Dependencies: none.

### Tenant context

Inputs: validated authenticated membership or trusted service context.
Outputs: immutable tenant context containing user, tenant, optional legal entity/scope, and correlation ID.
Dependencies: no persistence in this task; persistence belongs to `platform-002`.

### Entitlements

Inputs: tenant subscription state, entitlement key, requested usage where relevant.
Outputs: decision containing state, source, effective date, limit, usage, and denial reason.
Dependencies: in-memory provider for this first slice; persistent subscriptions belong to a later task.

### API boundary

Inputs: HTTP requests and platform commands.
Outputs: health endpoint and explicit typed errors for missing/invalid platform context.
Dependencies: tenant and entitlement contracts.

## Integration enumeration

1. API host creates/validates tenant context before invoking application services.
2. Application services ask the entitlement evaluator before module work is executed.
3. Entitlement denial crosses the API boundary as a stable problem response.
4. Tests exercise these paths using real implementations, not mocks for the core contracts.

## Deliberate non-goals

- No database persistence yet.
- No authentication provider integration yet.
- No HR, payroll, procurement, asset, billing-provider, or workflow implementation yet.
- No country-specific tax or statutory behavior.
