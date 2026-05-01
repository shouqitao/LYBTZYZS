# LYBT.Desktop.Clinical - Desktop Clinical Workspace

**Purpose**: Clinical role workspace module for doctors with patient workflow and consultation views.

## Structure

```
LYBT.Desktop.Clinical/
├── Views/               # ClinicalDashboardView, consultation workflow views
├── ViewModels/          # Clinical-specific ViewModels
└── ClinicalModule.cs    # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `ClinicalModule.cs` | Loads clinical workflow views |
| Dashboard | `Views/ClinicalDashboardView.xaml` | Doctor home page |
| Patient workflow | `Views/` | PatientSelectionControl, MedicalCase views |

## CONVENTIONS

- **Control reuse** — Embeds PatientSelectionControl from Patients module, MasterDetail controls
- **Role-based loading** — Only loaded when user has Doctor role
- **Workflow-driven** — Layout follows patient→consultation→prescription workflow

## ANTI-PATTERNS

- **Direct module coupling** — Clinical must NOT reference business modules directly
- **Business logic in workspace** — All logic stays in business modules; workspace is layout only
