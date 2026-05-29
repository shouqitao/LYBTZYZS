<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Receptionist

## Purpose

Receptionist role workspace module for the front desk. Provides the receptionist home view that serves as the entry point for patient registration and appointment management. Depends on Patients, Registration, and CardReader modules, composing their views into a unified front-desk workspace.

## Key Files

| File | Description |
|------|-------------|
| `ReceptionistModule.cs` | Prism IModule registration; declares dependencies on PatientsModule, RegistrationModule, CardReaderModule |
| `ViewModels/ReceptionistHomeViewModel.cs` | Main ViewModel for receptionist home view (13.5KB) |
| `Views/ReceptionistHomeView.xaml` | XAML layout for receptionist workspace (15KB) |
| `Views/ReceptionistHomeView.xaml.cs` | Code-behind for receptionist home view |
| `LYBT.Desktop.Receptionist.csproj` | Project file; net8.0-windows, WPF, Prism.DryIoc |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | ViewModels for receptionist-specific views |
| `Views/` | XAML views for the receptionist workspace |

## For AI Agents

### Working In This Directory

- This is a Prism role workspace module, not a business module. It composes views from other modules via Prism region injection.
- Module dependencies are declared via `[ModuleDependency]` attributes: PatientsModule, RegistrationModule, CardReaderModule.
- All business logic lives in the referenced modules (Patients, Registration). This module is layout and navigation only.
- The project references Desktop.Contracts, Desktop.Foundation, Desktop.Infrastructure, Desktop.Models, Desktop.CardReader, and the Registration and Patients modules.

### Testing Requirements

- No dedicated test project exists for this module. Test via LYBT.Tests.Desktop integration tests or manual front-desk workflow testing.
- Requires `net8.0-windows` target framework for WPF.

### Common Patterns

- **Role-based loading** -- Only loaded when user has Receptionist role (configured in Shell).
- **Module composition** -- Embeds controls from Patients and Registration modules into a single workspace.
- **Prism Region injection** -- Views from dependent modules are injected into named regions in ReceptionistHomeView.
- **CommunityToolkit.Mvvm** -- ViewModel uses CommunityToolkit MVVM source generators.

## Dependencies

### Internal

- `LYBT.Desktop.Contracts` -- Interface definitions
- `LYBT.Desktop.Foundation` -- Infrastructure (HTTP, security, config)
- `LYBT.Desktop.Infrastructure` -- WPF services (dialog, navigation)
- `LYBT.Desktop.Models` -- Client UI models
- `LYBT.Desktop.CardReader` -- ID card reader integration
- `LYBT.Desktop.Registration` -- Registration module (views/controls)
- `LYBT.Desktop.Patients` -- Patients module (views/controls)
- `LYBT.Shared.Models` -- DTOs and contracts

### External

- `Prism.Core`, `Prism.DryIoc`, `Prism.Wpf` -- Prism MVVM framework
- `CommunityToolkit.Mvvm` -- MVVM source generators
- `Microsoft.Extensions.Logging.Abstractions` -- Logging

<!-- MANUAL: -->
