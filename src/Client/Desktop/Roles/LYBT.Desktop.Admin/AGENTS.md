# LYBT.Desktop.Admin - Desktop Admin Workspace

**Purpose**: Admin role workspace module embedding management views for all modules.

## Structure

```
LYBT.Desktop.Admin/
├── Views/               # AdminDashboardView, management views
├── ViewModels/          # Admin-specific ViewModels
├── Services/            # Admin-specific services
└── AdminModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `AdminModule.cs` | Loads management views |
| Dashboard | `Views/AdminDashboardView.xaml` | Admin home page |
| Embedded controls | `Views/` | HerbMasterDetailControl, FormulaMasterDetailControl, etc. |

## CONVENTIONS

- **Control reuse** — Embeds MasterDetailControl from Formula/Herbs/Patients/Users modules
- **Role-based loading** — Only loaded when user has Admin role
- **No direct module references** — Uses Prism region injection for module controls

## ANTI-PATTERNS

- **Direct module coupling** — Admin must NOT reference business modules directly
- **Business logic in workspace** — All logic stays in business modules; workspace is layout only
