# Task Baseline Adjustment Report (2025-10)

## Scope & Intent
This report aligns the repository with the actual MVP target: restore login/health flow and stabilize desktop bootstrap. Historic task lists (e.g., **MVP "能看诊"** and **Quick Reference TODOs**) diverge from current priorities and are deferred/archived. Only the minimal fixes remain active.

## Summary Table

| Task/Scope | Current Status & Source | Decision | Notes / Next Steps |
|------------|------------------------|----------|--------------------|
| Auth / Health contract alignment | Login + health endpoints inconsistent between Shared Refit and WebAPI controller | **Keep (Immediate)** | Create issues , . Fix login/refresh/logout/health endpoints and DTOs. |
| Desktop bootstrap composition (IApplicationBootstrapper) | Start-up dependency resolution fails | **Keep (Immediate)** | Issue . Stabilize container registrations and add diagnostic logging. |
| Health check response contract | Client expects string, server returns JSON | **Keep (Short-term)** | Issue  + . Standardize payload & scripts. |
| CLAUDE.md/environment rules | Already updated | **Keep (Complete)** | Use Windows + PowerShell baseline and new MCP rules. |
| tech-design files (010–060) | Already describe current state | **Keep (Complete)** | Update when architecture changes. |
| docs/tasks/mvp-task-checklist-2025-10-16.md | Legacy 57-task backlog | **Defer/Archive** | Close Epic #1343, move doc to archive, open a slim MVP fix epic. |
| docs/tasks/quick-reference-improvement-todos.md & todo-progress-tracker.md | Documentation tasks, P2 pending | **Defer** | Mark P2 items as deferred; revisit post-MVP. |
| Offline mode design | Not yet formalized | **Split (Plan first)** | Add  for plan; implementation tasks later. |
| Spec Kit adoption | Not yet used | **Modify (Consider)** | Only use for generating specs; implementation still via current Issue workflow. |

## Immediate Actions (Keep)
1. **[SRV-1] / [CLI-1]** Align Auth/Health contracts (Shared ↔ WebAPI). Verify login/refresh/logout/health flows and update documentation.
2. **[CLI-2]** Stabilize desktop bootstrap registrations and diagnostics.
3. **[SRV-2] / [DOC-1]** Unify health check response payload and tooling.

## Deferred / Archived Items
- **MVP "能看诊" task set**: archive docs/tasks/mvp-task-checklist-2025-10-16.md, re-scope Epic #1343.
- **Quick Reference TODOs (P2)**: mark as deferred; schedule after MVP stabilization.
- **Documentation trackers**: move to archive alongside the deferred tasks.

## Planning Notes
- Draft  for future offline/local mode strategy.
- If Spec Kit is introduced, document usage (spec/plan/tasks) before implementation, ensuring compatibility with existing Issue → PR flow.

## Verification Checklist
- [ ] New issues created with module-specific checklist IDs.
- [ ] Legacy tasks archived or marked deferred.
- [ ] CLAUDE.md rules referenced in issues/PRs.
- [ ] Health/Auth fixes verified end-to-end (login + health).

