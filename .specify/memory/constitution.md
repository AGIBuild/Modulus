<!--
Sync Impact Report
- Version change: N/A → 1.0.0
- Modified principles: initialized project-specific principles
  - UI-Agnostic Core
  - Dual-Engine Host Architecture
  - Vertical Slice Modularity
  - Pyramid Layering
  - AI-Friendly Contracts & Plugin SDK
  - Modern .NET & Technology Discipline
- Added sections:
  - Architecture & Additional Constraints
  - Development Workflow & AI Collaboration
- Removed sections: none
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md
  - ✅ .specify/templates/spec-template.md
  - ✅ .specify/templates/tasks-template.md
- Follow-up TODOs: none
-->

# Modulus Constitution

## Core Principles

### UI-Agnostic Core

- Core libraries in the Domain and Application layers MUST NOT reference any concrete UI
  framework (for example `Microsoft.AspNetCore.Components`, `Avalonia`, or HTML/XAML types).
- All user interaction flows MUST be expressed in terms of `Modulus.UI.Abstractions` contracts
  (such as `IUIFactory`, `IViewHost`, and related interfaces).
- Cross-cutting concerns (logging, configuration, localization) in core layers MUST remain
  independent of any UI host and be injectable from the outside.
- Rationale: This keeps the business model portable across Blazor, Avalonia, CLI tools, and
  future hosts.

### Dual-Engine Host Architecture

- The system MUST support at least two first-class hosts: `Modulus.Host.Blazor` (web ecosystem /
  hybrid) and `Modulus.Host.Avalonia` (native rendering).
- Each module MAY provide multiple UI assemblies (for example `Module.UI.Blazor.dll`,
  `Module.UI.Avalonia.dll`) that implement the same abstraction contracts for different hosts.
- Hosts are responsible for application shell concerns (windowing, routing, environment
  integration) while modules are responsible for business logic and UI contracts only.
- Rationale: This allows the same module to be reused across lightweight web-style UIs and
  high-performance native experiences.

### Vertical Slice Modularity

- The primary delivery unit is a module; each feature MUST be delivered as a module that
  represents a vertical slice through the architecture.
- A module MAY implement one or more layers (for example Domain + Application only, or a full
  stack including Presentation), but MUST declare its boundaries and registrations via
  dependency injection.
- Runtime discovery MUST treat built-in features and external plugins uniformly, based on module
  metadata and assembly scanning, without hidden "special core" modules.
- Rationale: Vertical slices keep features independently testable, deployable, and removable
  without impacting unrelated areas.

### Pyramid Layering

- Dependencies MUST flow only in this direction:
  Presentation → UI Abstraction → Application → Domain → Infrastructure.
- Cross-layer shortcuts (for example UI calling Infrastructure directly, or Application
  depending on concrete UI frameworks) are forbidden.
- Communication between modules MUST occur via MediatR or well-defined interfaces, never
  through direct coupling between feature implementations.
- Rationale: A strict dependency pyramid keeps the runtime composable, testable, and
  host-agnostic.

### AI-Friendly Contracts & Plugin SDK

- Public plugin and module contracts MUST be strongly typed, self-describing, and use explicit
  DTOs for inputs, outputs, and errors.
- The SDK MUST provide opinionated base classes and helpers (for example `BlazorToolPluginBase`,
  `AvaloniaToolPluginBase`, and module base types) that encode recommended patterns for AI and
  human authors.
- Any breaking change to public contracts MUST be versioned, documented in specifications, and
  accompanied by migration guidance.
- Rationale: Clear contracts enable AI agents to generate high-quality plugins that compile and
  behave correctly on first attempt.

### Modern .NET & Technology Discipline

- The project MUST target a current LTS or Current .NET runtime; introducing legacy frameworks
  or outdated runtimes requires explicit justification and governance review.
- Core libraries MUST NOT depend on web-only constructs such as `HttpContext` or
  environment-specific APIs that would prevent reuse across hosts.
- MediatR MUST be the default choice for in-process cross-module communication to avoid ad hoc
  event or static coupling.
- ViewModel implementations MUST use `CommunityToolkit.Mvvm` (Source Generators, `ObservableObject`, `RelayCommand`) to standardize MVVM patterns and avoid boilerplate.
- Rationale: A disciplined, modern stack reduces maintenance cost and keeps the framework
  portable.

## Architecture & Additional Constraints

### Module structure

- Each feature MUST be represented as a module with a clear root namespace and assembly set,
  typically following patterns such as `Modulus.Modules.<Name>.Domain`, `...Application`,
  `...Infrastructure`, and optional `...UI.Blazor` / `...UI.Avalonia`.
- Modules MAY implement only the layers they need (for example a pure infrastructure module or a
  domain-only module), but MUST respect the dependency pyramid and expose clear integration
  points.
- Module registration MUST be driven by DI and metadata (for example module attributes or
  manifests), not by hard-coded lists in host applications.

### Hosts and UI assemblies

- Modules MAY ship separate UI assemblies for different hosts (for example `Module.UI.Blazor.dll`
  and `Module.UI.Avalonia.dll`) implementing the same UI abstraction contracts.
- Hosts are responsible for resolving and loading the appropriate UI assemblies for the active
  environment, leaving core assemblies reusable across all hosts.
- Presentation-layer projects MAY depend on host-specific frameworks (Blazor, Avalonia,
  MAUI/Photino), but MUST only communicate with core logic through the UI abstraction layer,
  Application services, and MediatR.

### Plugin packaging and discovery

- Plugin packages SHOULD use a structured container format (for example `.modpkg`) containing a
  manifest plus assemblies for core and UI layers, as defined in the architecture docs.
- Plugin entry points MUST declare themselves via the Modulus SDK (for example module base
  types or explicit plugin descriptors) so that discovery and unloading rely on clear contracts
  rather than reflection heuristics.
- Runtime discovery MUST apply the same rules to built-in modules and external plugin packages
  to ensure consistent behavior and isolation.
- Rationale: A consistent packaging and discovery model simplifies deployment, enables
  hot-reload and unloading, and allows AI to generate deployable plugins.

## Development Workflow & AI Collaboration

### Planning and Constitution Check

- Every implementation plan generated from `/speckit.plan` MUST include a "Constitution Check"
  section that evaluates the feature against each core principle:
  UI-agnostic core, dual-engine host architecture, vertical slice modularity, pyramid layering,
  AI-friendly contracts, and modern .NET discipline.
- A plan MUST NOT proceed past Phase 0 research unless all identified constitutional risks have
  either a mitigation plan or an explicit governance decision.

### Specifications

- Feature specifications (`/speckit.specify`) MUST state:
  - Which module(s) own the feature as a vertical slice.
  - Which host(s) (Blazor, Avalonia, or both) the feature targets.
  - Any new or changed public contracts, DTOs, or SDK base types that affect plugins or AI
    integration.
- Requirements MUST remain technology-agnostic at the Domain and Application layers, expressing
  behavior without binding to a specific UI framework.

### Tasks and implementation

- Task breakdowns (`/speckit.tasks`) MUST group work by user story and also make module and host
  boundaries explicit in task descriptions (for example which module and which UI assembly a
  task touches).
- Foundational tasks MUST cover:
  - Enforcing the dependency pyramid in project references and namespaces.
  - Configuring MediatR for module-level and cross-module communication.
  - Ensuring no Domain/Application projects reference concrete UI frameworks.
- Cross-cutting tasks MAY include constitution compliance checks, architecture reviews, and
  updates to AI manifests used by `nuke StartAI`.

### AI-assisted development

- AI tools (for example GitHub Copilot) MUST consume up-to-date project context, including this
  constitution, before generating significant architecture or plugin code.
- When generating plugins or modules, AI prompts and manifests SHOULD reference the official
  SDK base types and contracts defined by Modulus, instead of ad hoc patterns.
- Changes produced with AI assistance MUST still pass all constitutional gates in plans, specs,
  and reviews.

## Governance

- This constitution supersedes conflicting practices in older documentation or legacy code for
  this repository.
- Amendments to the constitution MUST be proposed through design stories and specifications that
  explain the motivation, affected modules, and migration considerations.
- The constitution uses semantic versioning in the form `MAJOR.MINOR.PATCH`:
  - MAJOR: Backward-incompatible changes to principles or governance (for example removing or
    redefining a principle).
  - MINOR: New principles or sections added, or materially expanded guidance.
  - PATCH: Clarifications, wording adjustments, and non-semantic refinements.
- All implementation plans MUST pass the Constitution Check before work begins, and pull
  request reviews MUST verify that new code and modules adhere to UI agnosticism, dual host
  support, vertical slice modularity, pyramid layering, AI-friendly contracts, and technology
  discipline.
- Governance decisions and exceptions (if any) MUST be documented alongside the affected
  features and referenced from the relevant plan and spec files.

**Version**: 1.1.0 | **Ratified**: 2025-11-27 | **Last Amended**: 2025-12-01

