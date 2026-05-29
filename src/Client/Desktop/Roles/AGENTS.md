<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Roles (Desktop)

## Purpose
Role-based workspace modules for the WPF desktop client. Each role defines a distinct user experience with role-specific navigation, views, and permissions. Workspaces are loaded based on the authenticated user's role and provide the top-level layout and navigation structure.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Admin/ | Administrator workspace — user management, system configuration, full access |
| LYBT.Desktop.Clinical/ | Clinical staff workspace — consultations, medical cases, prescriptions |
| LYBT.Desktop.Receptionist/ | Receptionist workspace — patient registration, scheduling, basic lookups |

## For AI Agents

### Working In This Directory
- Roles depend on Modules (for views and ViewModels) but Modules MUST NOT depend on Roles.
- Each role workspace defines its own Prism region layout and navigation menu.
- Role selection happens at login based on the authenticated user's assigned role.
- When adding a new role, create a new project following the existing pattern and register it in the Shell.

### Common Patterns
- **Workspace structure**: Role module registers region layouts and navigation menu items
- **Navigation menu**: Defined per-role, navigates to module views via Prism region navigation

## Dependencies

### Internal
- [Modules/](../Modules/AGENTS.md) — Business module views and ViewModels
- [Core/](../Core/AGENTS.md) — Contracts, Foundation, Infrastructure

<!-- MANUAL: -->
