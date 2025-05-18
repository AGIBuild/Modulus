<!-- Priority: P0 -->
<!-- Status: Completed -->
# S-0008-AI-Context-Manifest-and-Nuke-StartAI

**User Story**
As a developer using GitHub Copilot (or any AI agent), I want a single source of project context and a unified Nuke command to bootstrap AI context, so that Copilot can instantly understand the project's architecture, conventions, and goals, without manual explanation, and all team members have a consistent onboarding and daily AI experience.

**Acceptance Criteria**
- An `ai-manifest.yaml` (or `ai-manifest.json`) file exists in the project root, describing project overview, architecture, directory/naming rules, roadmap, and glossary.
- A Nuke target `StartAI` is available. Running `nuke StartAI` outputs the latest project manifest and key context files in a format suitable for Copilot/AI agent ingestion.
- The command supports parameters (e.g., `--role Frontend`) to filter context for different roles.
- Pre-commit and CI checks ensure the manifest is updated with any structural or convention changes.
- The onboarding/contribution guide instructs new members to use `nuke StartAI` for AI context injection.
- Team members can use `/sync`, `/roadmap`, `/why <file>` commands in Copilot Chat to quickly access project context (by referencing manifest sections).
- Every new Story must be provided in both English and Chinese versions, as part of the AI context documentation standard.

**Technical Tasks**
- [x] Draft and maintain `ai-manifest.yaml` in the root directory, including:
    - Project Overview (vision, features, tech stack)
    - Architecture (modules, DI, plugin system, data flow)
    - Directory & Naming Rules
    - Roadmap / Milestones
    - Glossary & FAQ
- [x] Add `StartAI` target to Nuke build script.
- [x] Implement logic to aggregate manifest, README, and progress reports for output.
- [x] Support context filtering by role (e.g., `nuke StartAI --role Backend`).
- [x] Add pre-commit and CI checks for manifest consistency.
- [x] Update onboarding/contribution docs with Copilot/AI context instructions.
- [x] (Optional) Implement ManifestSync CLI for auto-updating manifest from codebase.
- [x] Enforce the rule: every new Story must have both English and Chinese versions.

**Notes**
- This ensures all Copilot/AI agent users have consistent, up-to-date project context, maximizing AI-assisted development efficiency.
- The solution is vendor-agnostic and can be extended to other AI agents.
- The manifest and Nuke integration provide a single source of truth and a unified developer experience.
- Bilingual Story documentation is now a required part of the AI context standard.
