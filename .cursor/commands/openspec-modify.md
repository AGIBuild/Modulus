---
name: /openspec-modify
id: openspec-modify
category: OpenSpec
description: Modify an existing OpenSpec change proposal.
---
<!-- OPENSPEC:START -->
**Guardrails**
- Favor straightforward, minimal modifications and add complexity only when it is requested or clearly required.
- Keep changes tightly scoped to the requested outcome.
- Refer to `openspec/AGENTS.md` (located inside the `openspec/` directory—run `ls openspec` or `openspec update` if you don't see it) if you need additional OpenSpec conventions or clarifications.
- Identify any vague or ambiguous details and ask the necessary follow-up questions before editing files.
- Do not write any code during the proposal modification stage. Only update design documents (proposal.md, tasks.md, design.md, and spec deltas).

**Steps**
1. Determine the change ID to modify:
   - If this prompt already includes a specific change ID (for example inside a `<ChangeId>` block populated by slash-command arguments), use that value after trimming whitespace.
   - If the conversation references a change loosely (for example by title or summary), run `openspec list` to surface likely IDs, share the relevant candidates, and confirm which one the user intends.
   - Otherwise, review the conversation, run `openspec list`, and ask the user which change to modify; wait for a confirmed change ID before proceeding.
   - If you still cannot identify a single change ID, stop and tell the user you cannot modify anything yet.
2. Validate the change exists by running `openspec show <id>` and stop if the change is missing or already archived.
3. Read the existing proposal files (`proposal.md`, `tasks.md`, `design.md` if present, and spec deltas under `specs/`) to understand current state.
4. Review the user's requested modifications and identify which files need to be updated.
5. Apply the requested changes to the appropriate files:
   - Update `proposal.md` for scope, rationale, or impact changes
   - Update `tasks.md` for implementation plan changes
   - Update `design.md` for architectural decision changes
   - Update spec deltas under `specs/<capability>/spec.md` for requirement changes
6. Validate with `openspec validate <id> --strict` and resolve every issue before confirming completion.

**Reference**
- Use `openspec show <id> --json --deltas-only` to inspect the current delta structure.
- Use `openspec show <spec> --type spec` to review the base spec when modifying delta requirements.
- Search existing requirements with `rg -n "Requirement:|Scenario:" openspec/specs` to avoid duplicates or conflicts.
<!-- OPENSPEC:END -->

