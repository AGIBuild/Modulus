# Change: Standardize module lifecycle and cleanup

## Why
- Module lifecycle lacks a clear Loaded/Active/Error state machine; initialization can run without a bound host, causing inconsistent state.
- Detail pages are eagerly loaded; this increases startup cost and risks failures before the user navigates.
- Unload/disable paths are inconsistent, leaving menus/navigation/messages and scoped resources registered, causing leaks and unstable hot-reload.

## What Changes
- Define a runtime module state machine with explicit transitions: Loaded (metadata/menus ready, host not bound), Active (initialized with host), Error (failed init or detail load) plus retry rules.
- Default to menu-only load; lazily load and render module detail on first navigation with async flow and cancellation/timeout handling.
- Gate initialization on host binding; when a host binds, batch-initialize eligible modules and persist state transitions.
- Standardize unload/disable cleanup: invoke module deregistration hooks (menu/navigation/message), dispose scoped providers and caches, and unload the module AssemblyLoadContext.

## Impact
- Affected specs: runtime
- Affected code: module runtime loader/state manager, navigation/detail loader, host bind/unbind flow, module cleanup pipeline
