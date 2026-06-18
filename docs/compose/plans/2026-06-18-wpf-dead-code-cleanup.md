# WPF Dead Code & Duplicate Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove ~2,500+ lines of dead code, unused controls, deprecated views, dead enums, unused converters, stale navigation constants, and duplicate infrastructure from the WPF Desktop codebase.

**Architecture:** Pure deletion + dead-code removal. No functional changes. Each task removes a self-contained category of dead code. Tasks are independent and safe to execute in any order.

**Tech Stack:** C# / .NET 8 / WPF / Prism

---

### Task 1: Remove deprecated LoginWindow

**Files:**
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginWindow.xaml`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginWindow.xaml.cs`
- Delete: `tests/LYBT.Tests.Desktop/Unit/Auth/LoginWindowAppearanceTests.cs`

- [ ] **Step 1: Delete files**

```bash
git rm src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginWindow.xaml
git rm src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginWindow.xaml.cs
git rm tests/LYBT.Tests.Desktop/Unit/Auth/LoginWindowAppearanceTests.cs
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git commit -m "chore: remove deprecated LoginWindow (dead code, single-window mode active)"
```

### Task 2: Remove dead MasterDetail navigation views (4 of 5)

**Files:**
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientMasterDetailView.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbMasterDetailView.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaMasterDetailView.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserMasterDetailView.xaml` + `.xaml.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs` — remove `RegisterForNavigation<PatientMasterDetailView>()`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs` — remove `RegisterForNavigation<HerbMasterDetailView>()`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/FormulaModule.cs` — remove `RegisterForNavigation<FormulaMasterDetailView>()`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/UsersModule.cs` — remove `RegisterForNavigation<UserMasterDetailView>()`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — remove `PatientMasterDetail` constant

**Note:** Keep `MedicalCaseMasterDetailView` — it IS navigated to from `MedicalCaseWorkspaceViewModel.cs:568,572`.

- [ ] **Step 1: Delete the 4 View XAML + code-behind files (8 files)**

- [ ] **Step 2: Remove RegisterForNavigation calls from each Module.cs**

- [ ] **Step 3: Remove `PatientMasterDetail` constant from ViewNames.cs**

- [ ] **Step 4: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: remove 4 dead MasterDetail navigation views (Patient/Herb/Formula/User)"
```

### Task 3: Remove unused PendingQueueControl, BaseMasterDataListView, PatientSearchControl, old BreadcrumbControl

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml` + `.xaml.cs` (~600 lines)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml` + `.xaml.cs` (~200 lines)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/BreadcrumbControl.xaml` + `.xaml.cs` (old DP-based version; keep `Navigation/Controls/BreadcrumbControl.xaml`)

- [ ] **Step 1: Verify no XAML references**

Run: `rg "PendingQueueControl|BaseMasterDataListView|PatientSearchControl" src/Client/Desktop -t xml`
Expected: only the files being deleted

- [ ] **Step 2: Delete 8 files**

```bash
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml.cs
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml.cs
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml.cs
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/BreadcrumbControl.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/BreadcrumbControl.xaml.cs
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove unused PendingQueueControl, BaseMasterDataListView, PatientSearchControl, old BreadcrumbControl"
```

### Task 4: Remove dead CommandHandler infrastructure + unused FormulaValidator

**Files:**
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/CommandHandlers/IFormulaCommandHandler.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/CommandHandlers/FormulaCommandHandler.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/CommandHandlers/IUserCommandHandler.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/CommandHandlers/UserCommandHandler.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/ICommandHandlerBase.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/CommandResult.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/CommandHandlers/QueryParams.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaValidator.cs` (registered but never injected)

- [ ] **Step 1: Delete 8 files**

- [ ] **Step 2: Remove FormulaValidator registration from FormulaModule.cs**

Remove: `containerRegistry.Register<Services.FormulaValidator>();`

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove dead CommandHandler infrastructure and unused FormulaValidator (~324 lines)"
```

### Task 5: Remove dead interfaces + implementations

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ILocalDbBackupService.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/LocalDbBackupService.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Interfaces/IMainWindowServicesFacade.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/MainWindowServicesFacade.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` — remove registration of `IMainWindowServicesFacade`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs` — remove duplicate `IApplicationInitializationService` registration (keep only in ServiceCollectionExtensions.cs)

- [ ] **Step 1: Delete 4 files**

- [ ] **Step 2: Remove DI registrations**

Remove from `ServiceCollectionExtensions.cs`:
```csharp
containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();
```

Remove from `App.xaml.cs`:
```csharp
containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
    LYBT.Desktop.Shell.Services.ApplicationInitializationService>();
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove dead interfaces (ILocalDbBackupService, IMainWindowServicesFacade) and duplicate DI registration"
```

### Task 6: Remove dead enums + stale ViewNames + dead ViewModel commands

**Files:**
- Delete: `src/Shared/LYBT.Shared.Models/Enums/PrintType.cs`
- Delete: `src/Shared/LYBT.Shared.Models/Enums/CaseStatus.cs`
- Modify: `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs` — remove `AuthSessionStatus` and `LoginType` enums
- Modify: `src/Shared/LYBT.Shared.Models/Enums/MedicalCaseEnums.cs` — remove `AuditOperationType` enum
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — remove `ControlExamples` and `MedicalCaseList`
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs` — remove `ShowControlExamplesCommand` + handler
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/AdminHomeViewModel.cs` — remove `EditProfileCommand`, `ChangePasswordCommand` + methods

- [ ] **Step 1: Delete 2 enum files, modify 2 enum files**

- [ ] **Step 2: Remove stale ViewNames + MenuManager dead command**

- [ ] **Step 3: Remove dead AdminHomeViewModel commands**

- [ ] **Step 4: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: remove dead enums, stale ViewNames, dead AdminHomeViewModel commands"
```

### Task 7: Remove unused converters

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/BoolToOpacityConverter.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/BoolToStringConverter.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/StatusToColorConverter.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/PatientCardDisplayModeToVisibilityConverter.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/ApiHealthStatusToTextConverter.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/FirstCharacterConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/ConverterInstances.cs` — remove static instances for deleted converters
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/Converters.xaml` — remove registrations for deleted converters
- Keep: `EnumDescriptionConverter.cs` (used via `Cvt.EnumDesc` in MedicalCaseViewControl + FormulaViewControl)

- [ ] **Step 1: Delete 6 converter files**

- [ ] **Step 2: Remove from ConverterInstances.cs**

Remove: `Cvt.BoolToOpacity`, `Cvt.BoolToString`, `Cvt.StatusToColor`, `Cvt.PatientCardModeToVis`, `Cvt.ApiStatusToText`, `Cvt.FirstChar`

- [ ] **Step 3: Remove from Converters.xaml**

- [ ] **Step 4: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: remove 6 unused value converters"
```

### Task 8: Remove PrintLogRequested event + PrintLogEntry model

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Models/PrintLogEntry.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` — remove `PrintLogRequested` event declaration + all `PrintLogRequested?.Invoke()` calls

- [ ] **Step 1: Delete PrintLogEntry.cs**

- [ ] **Step 2: Remove event from PrescriptionPrintService.cs**

Remove: `public event Action<PrintLogEntry>? PrintLogRequested;`
Remove all 4 `PrintLogRequested?.Invoke(...)` calls (lines 76, 89, 523, 541)

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove unused PrintLogRequested event and PrintLogEntry model"
```

### Task 9: Remove dead controls + unused resources + stale string entries

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/FormField/FormFieldControl.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/Diagnosis/TonguePulseDiagnosisControl.xaml` + `.xaml.cs`
- Delete: `src/Client/Desktop/Resources/Dictionaries/IconResources.xaml` (not in App.xaml MergedDictionaries)
- Modify: `src/Client/Desktop/Shell/Resources/Strings/StringResources.resx` — remove "Syncing" entry
- Modify: `src/Client/Desktop/Resources/Strings/StringResources.resx` — remove "Syncing" entry
- Keep: `ToastControl` (used by ToastService), `CardReaderStatusControl` (used in Patient card UI)

- [ ] **Step 1: Verify FormFieldControl and TonguePulseDiagnosisControl are unused**

Run: `rg "FormFieldControl|TonguePulseDiagnosisControl" src/Client/Desktop -t xml`
Expected: only self-references

- [ ] **Step 2: Delete 4 files + IconResources.xaml**

- [ ] **Step 3: Remove "Syncing" entries from both .resx files**

- [ ] **Step 4: Regenerate Designer.cs files**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: remove dead controls (FormField, TonguePulseDiagnosis), IconResources, stale Syncing string"
```

### Task 10: Remove unused style aliases from Controls.xaml + unify ReceptionistHomeView duplicate styles

**Files:**
- Modify: `src/Client/Desktop/Shell/Styles/Controls.xaml` — remove 6 unused style aliases: `StandardTextBoxStyle`, `SearchTextBoxStyle`, `StandardComboBoxStyle`, `StandardDataGridStyle`, `CardStyle`, `DividerStyle`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/Views/ReceptionistHomeView.xaml` — remove 6 inline style definitions (lines ~6-73), use `HomePageStyles.xaml` keys instead

- [ ] **Step 1: Remove unused aliases from Controls.xaml**

- [ ] **Step 2: Remove inline styles from ReceptionistHomeView.xaml**

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: remove unused style aliases and duplicate inline styles"
```

### Task 11: Full build + test verification

- [ ] **Step 1: Clean rebuild**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: Run architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`

- [ ] **Step 3: Run desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`

- [ ] **Step 4: Final status check**

Run: `git status`
Expected: clean working tree
