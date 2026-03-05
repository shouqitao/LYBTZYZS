# MedicalCase Composite ViewModel Refactoring - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor MedicalCaseWorkspaceViewModel (1099 lines) into a thin composition shell (~200 lines) using Child VM Composition pattern, with immutable WorkspaceState record and Service SRP split.

**Architecture:** Strangler Pattern - add new components alongside existing ones, then swap the parent VM wiring. Each task is independently compilable. Child VMs communicate with parent via IWorkspaceHost (operations) and IMedicalCaseWorkspaceContext (state reading) interfaces.

**Tech Stack:** .NET 8 + WPF/Prism.DryIoc 8.1.97 + CommunityToolkit.Mvvm 8.4.0 + Riok.Mapperly + xUnit/NSubstitute

**Design Document:** `docs/plans/2026-03-05-medicalcase-and-module-refactoring-design.md`

---

## Baseline

- Desktop tests: ~760 tests, 0 failures
- Server tests: ~1185 tests, 0 failures
- Architecture tests: 76 tests, 0 failures

## Key File Locations

| Role | File Path | Lines |
|------|-----------|-------|
| Parent VM (rewrite target) | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` | 1099 |
| Service (SRP split target) | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs` | 740 |
| PendingQueueHandler (upgrade) | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/PendingQueueHandler.cs` | 379 |
| CardReaderHandler (upgrade) | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/CardReaderWorkspaceHandler.cs` | 540 |
| PrescriptionImportHandler (merge) | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/PrescriptionImportHandler.cs` | 337 |
| PrescriptionPrintHandler (merge) | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs` | ~283 |
| DI Registration (MedicalCase) | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` | 140 |
| DI Registration (Clinical) | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs` | - |
| XAML View | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` | 249 |
| ConsultationItem | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/ConsultationItem.cs` | - |
| PrescriptionItem | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/PrescriptionItem.cs` | - |
| ConsultationMapper | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/ConsultationMapper.cs` | - |
| PrescriptionMapper | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/PrescriptionMapper.cs` | - |
| CloneMapper | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseCloneMapper.cs` | - |
| Test Fixture | `tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixture.cs` | - |
| Existing E2E Tests | `tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/` | 3 files |

---

## Task 1: WorkspaceState Record

**Rationale:** Replace the deleted `WorkspaceState.cs` (ObservableObject, 217 lines) and inline state machine (~90 lines in VM, lines 180-287) with an immutable C# record. Eliminates `RaiseEditStateProperties()` with its 10 `OnPropertyChanged` + 4 `RaiseCanExecuteChanged` calls. Single `OnPropertyChanged(nameof(State))` replaces all of them.

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/WorkspaceState.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/WorkspaceStateTests.cs`

### Step 1: Write failing tests

```csharp
// tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/WorkspaceStateTests.cs
using LYBT.Desktop.MedicalCase.Models;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class WorkspaceStateTests
{
    [Fact]
    public void Default_state_is_editing_create_mode()
    {
        var state = new WorkspaceState();
        Assert.Equal(EditState.Editing, state.EditState);
        Assert.Equal(EditType.Create, state.EditType);
        Assert.Equal(WorkspaceMode.Clinical, state.Mode);
        Assert.True(state.IsEditing);
        Assert.False(state.IsReadOnly);
    }

    [Fact]
    public void EnterReadOnlyMode_returns_new_instance_with_readonly()
    {
        var state = new WorkspaceState();
        var readOnly = state.EnterReadOnlyMode();

        Assert.True(readOnly.IsReadOnly);
        Assert.False(readOnly.IsEditing);
        Assert.True(state.IsEditing); // Original unchanged (immutable)
    }

    [Fact]
    public void EnterEditMode_when_CanEdit_returns_editing_state()
    {
        var state = new WorkspaceState(CanEdit: true, EditState: EditState.ReadOnly);
        var editing = state.EnterEditMode();
        Assert.True(editing.IsEditing);
    }

    [Fact]
    public void EnterEditMode_when_CannotEdit_returns_same_state()
    {
        var state = new WorkspaceState(CanEdit: false, EditState: EditState.ReadOnly);
        var result = state.EnterEditMode();
        Assert.True(result.IsReadOnly);
    }

    [Fact]
    public void DetermineFromContext_completed_case_owner_clinical()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Clinical, isCompleted: true,
            isOwner: true, isAdmin: false, preferEditing: true);

        Assert.False(result.CanEdit); // Completed + owner (not admin) => cannot edit
        Assert.True(result.IsReadOnly);
        Assert.Equal(EditType.EditCompleted, result.EditType);
    }

    [Fact]
    public void DetermineFromContext_suspended_case_owner_clinical()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Clinical, isCompleted: false,
            isOwner: true, isAdmin: false, preferEditing: true);

        Assert.True(result.CanEdit);
        Assert.True(result.IsEditing);
        Assert.Equal(EditType.EditSuspended, result.EditType);
    }

    [Fact]
    public void DetermineFromContext_admin_can_always_edit()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Management, isCompleted: true,
            isOwner: false, isAdmin: true, preferEditing: false);

        Assert.True(result.CanEdit);
        Assert.True(result.IsReadOnly); // preferEditing=false
        Assert.Equal(WorkspaceMode.Management, result.Mode);
    }

    [Theory]
    [InlineData(WorkspaceMode.Clinical, true, "看诊中")]
    [InlineData(WorkspaceMode.Clinical, false, "查看医案")]
    [InlineData(WorkspaceMode.Management, true, "编辑医案")]
    [InlineData(WorkspaceMode.Management, false, "查看医案")]
    public void HeaderTitle_matches_mode_and_editing(WorkspaceMode mode, bool isEditing, string expected)
    {
        var editState = isEditing ? EditState.Editing : EditState.ReadOnly;
        var state = new WorkspaceState(Mode: mode, EditState: editState);
        Assert.Equal(expected, state.HeaderTitle);
    }

    [Fact]
    public void ShowSuspendButton_only_when_editing_clinical()
    {
        var editing = new WorkspaceState(EditState: EditState.Editing, Mode: WorkspaceMode.Clinical);
        var readOnly = new WorkspaceState(EditState: EditState.ReadOnly, Mode: WorkspaceMode.Clinical);
        var mgmt = new WorkspaceState(EditState: EditState.Editing, Mode: WorkspaceMode.Management);

        Assert.True(editing.ShowSuspendButton);
        Assert.False(readOnly.ShowSuspendButton);
        Assert.False(mgmt.ShowSuspendButton);
    }

    [Fact]
    public void With_expression_creates_new_instance()
    {
        var state = new WorkspaceState(Remark: "old");
        var updated = state with { Remark = "new" };

        Assert.Equal("old", state.Remark);
        Assert.Equal("new", updated.Remark);
    }
}
```

### Step 2: Run test to verify it fails

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~WorkspaceStateTests" -v m
```
Expected: Compilation error - `WorkspaceState` record does not exist.

### Step 3: Write WorkspaceState record

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/WorkspaceState.cs
namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// Immutable workspace state record. Replaces 30+ inline properties + RaiseEditStateProperties().
/// Use 'with' expressions for state transitions; single OnPropertyChanged(nameof(State)) in parent VM.
/// </summary>
public record WorkspaceState(
    EditState EditState = EditState.Editing,
    EditType EditType = EditType.Create,
    WorkspaceMode Mode = WorkspaceMode.Clinical,
    bool CanEdit = false,
    bool IsPrescriptionEnabled = false,
    bool NeedsPrescription = true,
    bool CanComplete = false,
    bool CanPrint = false,
    string EditReason = "",
    string Remark = "")
{
    // Edit state computed properties
    public bool IsEditing => EditState == EditState.Editing;
    public bool IsReadOnly => EditState == EditState.ReadOnly;
    public bool IsHistoricalEditMode => EditType == EditType.EditCompleted;

    // Button visibility computed properties
    public bool ShowEditButton => IsReadOnly && CanEdit && Mode == WorkspaceMode.Clinical;
    public bool ShowEditButtonTopRight => IsReadOnly && CanEdit && Mode == WorkspaceMode.Management;
    public bool ShowSaveButton => IsEditing && Mode == WorkspaceMode.Management;
    public bool ShowSuspendButton => IsEditing && Mode == WorkspaceMode.Clinical;
    public bool ShowCompleteButton => IsEditing && Mode == WorkspaceMode.Clinical;

    // Display text computed properties
    public string HeaderTitle => Mode switch
    {
        WorkspaceMode.Clinical => IsEditing ? "看诊中" : "查看医案",
        WorkspaceMode.Management => IsEditing ? "编辑医案" : "查看医案",
        _ => "看诊中"
    };

    public string BackButtonText => Mode switch
    {
        WorkspaceMode.Clinical => "返回患者选择",
        WorkspaceMode.Management => "返回医案列表",
        _ => "返回"
    };

    // State transition methods (return new instances - immutable)
    public WorkspaceState EnterEditMode()
        => CanEdit ? this with { EditState = EditState.Editing } : this;

    public WorkspaceState EnterReadOnlyMode()
        => this with { EditState = EditState.ReadOnly };

    public WorkspaceState DetermineFromContext(
        WorkspaceMode workspaceMode, bool isCompleted, bool isOwner,
        bool isAdmin, bool preferEditing)
    {
        var canEdit = isAdmin || (isOwner && !isCompleted);
        var editType = isCompleted ? EditType.EditCompleted : EditType.EditSuspended;
        var editState = preferEditing && canEdit ? EditState.Editing : EditState.ReadOnly;
        return this with
        {
            Mode = workspaceMode,
            CanEdit = canEdit,
            EditType = editType,
            EditState = editState
        };
    }
}
```

### Step 4: Run tests to verify they pass

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~WorkspaceStateTests" -v m
```
Expected: All 10 tests PASS.

### Step 5: Commit

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/WorkspaceState.cs tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/WorkspaceStateTests.cs
git commit -m "feat: add immutable WorkspaceState record with state transition methods"
```

---

## Task 2: IWorkspaceHost Interface + ChildViewModelBase

**Rationale:** Create the communication contracts that replace Handler callback Action/Func properties. IWorkspaceHost is the generic "child-to-parent operations" interface. ChildViewModelBase provides the shared infrastructure for all child VMs.

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IWorkspaceHost.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Composition/ChildViewModelBase.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Infrastructure/ChildViewModelBaseTests.cs`

### Step 1: Write failing tests

```csharp
// tests/LYBT.Tests.Desktop/PureLogic/Infrastructure/ChildViewModelBaseTests.cs
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

public class ChildViewModelBaseTests
{
    private class TestChildViewModel : ChildViewModelBase
    {
        public bool InitializeCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public TestChildViewModel(IWorkspaceHost host, ILoggerFactory loggerFactory)
            : base(host, loggerFactory) { }

        public override Task InitializeAsync()
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            DisposeCalled = true;
            base.Dispose();
        }

        public IWorkspaceHost TestHost => Host;
        public ILogger TestLogger => Logger;
    }

    [Fact]
    public void Constructor_stores_host_and_creates_logger()
    {
        var host = Substitute.For<IWorkspaceHost>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var vm = new TestChildViewModel(host, loggerFactory);

        Assert.Same(host, vm.TestHost);
        Assert.NotNull(vm.TestLogger);
    }

    [Fact]
    public void Constructor_throws_when_host_is_null()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        Assert.Throws<ArgumentNullException>(() => new TestChildViewModel(null!, loggerFactory));
    }

    [Fact]
    public async Task InitializeAsync_is_virtual_and_callable()
    {
        var host = Substitute.For<IWorkspaceHost>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var vm = new TestChildViewModel(host, loggerFactory);
        await vm.InitializeAsync();

        Assert.True(vm.InitializeCalled);
    }

    [Fact]
    public void Dispose_is_virtual_and_callable()
    {
        var host = Substitute.For<IWorkspaceHost>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var vm = new TestChildViewModel(host, loggerFactory);
        vm.Dispose();

        Assert.True(vm.DisposeCalled);
    }
}
```

### Step 2: Run test to verify it fails

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~ChildViewModelBaseTests" -v m
```
Expected: Compilation error - types don't exist.

### Step 3: Write IWorkspaceHost + ChildViewModelBase

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IWorkspaceHost.cs
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Generic child-to-parent operations contract.
/// Child VMs call these methods to request UI operations from the parent.
/// Replaces Handler callback Action/Func properties (SetBusy, ShowErrorMessage, etc.)
/// </summary>
public interface IWorkspaceHost
{
    void SetBusy(bool isBusy, string? message = null);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<bool> ShowConfirmAsync(string message, string title = "确认");
    ICommonDialogService? CommonDialogService { get; }

    /// <summary>
    /// Child VM notifies parent that state needs recalculation.
    /// Parent should recompute WorkspaceState (CanComplete, CanPrint, etc.)
    /// </summary>
    void NotifyStateChanged();
}
```

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Composition/ChildViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Composition;

/// <summary>
/// Base class for child ViewModels in the Composite VM pattern.
/// Provides access to parent host operations and logging.
/// </summary>
public abstract class ChildViewModelBase : ObservableObject, IDisposable
{
    protected IWorkspaceHost Host { get; }
    protected ILogger Logger { get; }

    protected ChildViewModelBase(IWorkspaceHost host, ILoggerFactory loggerFactory)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        Logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Initialize the child VM (data loading, subscriptions, etc.).
    /// Called by parent VM after navigation lifecycle.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose() { }
}
```

### Step 4: Run tests to verify they pass

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~ChildViewModelBaseTests" -v m
```
Expected: All 4 tests PASS.

### Step 5: Verify full solution compiles

```bash
dotnet build LYBT.All.sln -v m
```
Expected: BUILD SUCCEEDED. No breaking changes.

### Step 6: Commit

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IWorkspaceHost.cs src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Composition/ChildViewModelBase.cs tests/LYBT.Tests.Desktop/PureLogic/Infrastructure/ChildViewModelBaseTests.cs
git commit -m "feat: add IWorkspaceHost interface and ChildViewModelBase for composite VM pattern"
```

---

## Task 3: IMedicalCaseWorkspaceContext Interface

**Rationale:** Module-specific read-only context for child VMs to access workspace state without depending on the parent VM concrete type. Enables independent testing with mock context.

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseWorkspaceContext.cs`

### Step 1: Create the interface

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseWorkspaceContext.cs
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// Read-only context for MedicalCase child VMs.
/// Implemented by MedicalCaseWorkspaceViewModel.
/// Child VMs use this to read current workspace state without coupling to parent.
/// </summary>
public interface IMedicalCaseWorkspaceContext
{
    WorkspaceState State { get; }
    Guid MedicalCaseId { get; }
    PatientDetailDto? CurrentPatient { get; }
    ISessionManager? SessionManager { get; }
}
```

### Step 2: Verify compilation

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ -v m
```
Expected: BUILD SUCCEEDED.

### Step 3: Commit

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseWorkspaceContext.cs
git commit -m "feat: add IMedicalCaseWorkspaceContext interface for child VM state access"
```

---

## Task 4: MedicalCaseChangeTracker

**Rationale:** Extract change detection logic from MedicalCaseService (~lines 600-740) into a focused, independently testable class. Uses existing MedicalCaseCloneMapper for deep copy.

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseChangeTracker.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseChangeTrackerTests.cs`

### Step 1: Read existing change detection logic

Before implementing, read the exact change detection methods in `MedicalCaseService.cs`:
```bash
grep -n "HasChanges\|IsMedicalCaseChanged\|IsConsultationChanged\|IsPrescriptionChanged" src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs
```
Copy the exact field comparison logic.

### Step 2: Write failing tests

```csharp
// tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseChangeTrackerTests.cs
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCases;
using LYBT.Shared.Models.Contracts.Consultations;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class MedicalCaseChangeTrackerTests
{
    private static MedicalCaseDetailDto CreateTestDto(string remark = "test", string? illness = "headache")
    {
        return new MedicalCaseDetailDto
        {
            Id = Guid.NewGuid(),
            Remark = remark,
            Consultation = new ConsultationDetailDto { PresentIllness = illness },
            Prescription = new PrescriptionDetailDto { DosageCount = 7 }
        };
    }

    [Fact]
    public void HasChanges_returns_false_when_no_baseline()
    {
        var tracker = new MedicalCaseChangeTracker();
        Assert.False(tracker.HasChanges(CreateTestDto()));
    }

    [Fact]
    public void HasChanges_returns_false_when_unchanged()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        Assert.False(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_remark_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(remark: "original");
        tracker.SetBaseline(dto);
        dto.Remark = "modified";
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_consultation_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(illness: "headache");
        tracker.SetBaseline(dto);
        dto.Consultation!.PresentIllness = "fever";
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void ClearBaseline_resets_tracking()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        dto.Remark = "changed";
        tracker.ClearBaseline();
        Assert.False(tracker.HasChanges(dto));
    }

    [Fact]
    public void SetBaseline_deep_copies_so_original_mutations_detected()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(remark: "original");
        tracker.SetBaseline(dto);
        dto.Remark = "mutated"; // Mutate after baseline
        Assert.True(tracker.HasChanges(dto)); // Detected because baseline was deep-copied
    }
}
```

### Step 3: Implement MedicalCaseChangeTracker

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseChangeTracker.cs
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.Models.Contracts.Consultations;
using LYBT.Shared.Models.Contracts.MedicalCases;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// Tracks changes to a MedicalCase aggregate by comparing current state against a deep-copied baseline.
/// Extracted from MedicalCaseService for SRP.
/// </summary>
public class MedicalCaseChangeTracker
{
    private readonly MedicalCaseCloneMapper _cloneMapper = new();
    private MedicalCaseDetailDto? _baseline;

    public void SetBaseline(MedicalCaseDetailDto snapshot)
        => _baseline = _cloneMapper.Clone(snapshot);

    public bool HasChanges(MedicalCaseDetailDto? current)
    {
        if (_baseline == null || current == null) return false;
        return IsMedicalCaseChanged(_baseline, current)
            || IsConsultationChanged(_baseline.Consultation, current.Consultation)
            || IsPrescriptionChanged(_baseline.Prescription, current.Prescription);
    }

    public void ClearBaseline() => _baseline = null;

    // IMPORTANT: Migrate exact field comparison logic from MedicalCaseService.cs
    // Read the actual HasChanges property and related methods to ensure parity
    private static bool IsMedicalCaseChanged(MedicalCaseDetailDto baseline, MedicalCaseDetailDto current)
        => baseline.Remark != current.Remark
        || baseline.NeedsPrescription != current.NeedsPrescription;

    private static bool IsConsultationChanged(ConsultationDetailDto? baseline, ConsultationDetailDto? current)
    {
        if (baseline == null && current == null) return false;
        if (baseline == null || current == null) return true;
        return baseline.PresentIllness != current.PresentIllness
            || baseline.TongueDiagnosis != current.TongueDiagnosis
            || baseline.PulseDiagnosis != current.PulseDiagnosis
            || baseline.TcmDiagnosis != current.TcmDiagnosis;
    }

    private static bool IsPrescriptionChanged(PrescriptionDetailDto? baseline, PrescriptionDetailDto? current)
    {
        if (baseline == null && current == null) return false;
        if (baseline == null || current == null) return true;
        if (baseline.DosageCount != current.DosageCount) return true;
        if (baseline.Usage != current.Usage) return true;
        if (baseline.Remark != current.Remark) return true;
        if (baseline.Discount != current.Discount) return true;

        var baseItems = baseline.Items ?? [];
        var currentItems = current.Items ?? [];
        if (baseItems.Count != currentItems.Count) return true;

        for (var i = 0; i < baseItems.Count; i++)
        {
            if (baseItems[i].HerbName != currentItems[i].HerbName
                || baseItems[i].Dosage != currentItems[i].Dosage)
                return true;
        }
        return false;
    }
}
```

### Step 4: Run tests, commit

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~MedicalCaseChangeTrackerTests" -v m
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseChangeTracker.cs tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseChangeTrackerTests.cs
git commit -m "feat: extract MedicalCaseChangeTracker from MedicalCaseService for SRP"
```

---

## Task 5: ConsultationEditorViewModel

**Rationale:** First child VM. Wraps existing ConsultationItem, handles initialization from DTO via ConsultationMapper (replacing manual field-by-field copy in `InitializeChildViewModels()` lines 937-1009).

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/ConsultationEditorViewModelTests.cs`

### Step 1: Write failing tests

```csharp
// tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/ConsultationEditorViewModelTests.cs
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Shared.Models.Contracts.Consultations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase.Workspace;

public class ConsultationEditorViewModelTests
{
    private readonly IMedicalCaseWorkspaceContext _context = Substitute.For<IMedicalCaseWorkspaceContext>();
    private readonly IWorkspaceHost _host = Substitute.For<IWorkspaceHost>();
    private readonly ILoggerFactory _loggerFactory;

    public ConsultationEditorViewModelTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _context.State.Returns(new WorkspaceState());
    }

    private ConsultationEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);

    [Fact]
    public void Constructor_creates_empty_consultation_item()
    {
        var sut = CreateSut();
        Assert.NotNull(sut.Consultation);
    }

    [Fact]
    public void InitializeFromDto_maps_diagnosis_fields()
    {
        var dto = new ConsultationDetailDto
        {
            Id = Guid.NewGuid(), PresentIllness = "headache",
            TongueDiagnosis = "red", PulseDiagnosis = "rapid", TcmDiagnosis = "wind-heat"
        };
        var sut = CreateSut();
        sut.InitializeFromDto(dto);

        Assert.Equal("headache", sut.Consultation.PresentIllness);
        Assert.Equal("wind-heat", sut.Consultation.TcmDiagnosis);
    }

    [Fact]
    public void InitializeForNewCase_sets_patient_context()
    {
        var caseId = Guid.NewGuid();
        _context.MedicalCaseId.Returns(caseId);

        var sut = CreateSut();
        sut.InitializeForNewCase("张三", Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("张三", sut.Consultation.PatientName);
        Assert.Equal(caseId, sut.Consultation.MedicalCaseId);
    }

    [Fact]
    public void GetConsultationData_returns_input_dto()
    {
        var sut = CreateSut();
        sut.Consultation.TcmDiagnosis = "test";
        var data = sut.GetConsultationData();
        Assert.NotNull(data);
    }

    [Fact]
    public void Reset_clears_diagnosis_fields()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "test";
        sut.Reset();
        Assert.Null(sut.Consultation.PresentIllness);
    }

    [Fact]
    public void Validate_fails_when_no_tcm_diagnosis()
    {
        var sut = CreateSut();
        Assert.False(sut.Validate());
    }
}
```

### Step 2: Implement ConsultationEditorViewModel

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultations;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Workspace;

/// <summary>
/// Child VM for consultation (diagnosis) data editing.
/// Wraps ConsultationItem for XAML binding, handles DTO initialization via ConsultationMapper.
/// </summary>
public class ConsultationEditorViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly ConsultationMapper _mapper = new();

    public ConsultationItem Consultation { get; } = new();

    public ConsultationEditorViewModel(
        IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Initialize from existing consultation DTO (resume/view case).
    /// Replaces manual field-by-field copy in InitializeChildViewModels().
    /// </summary>
    public void InitializeFromDto(ConsultationDetailDto dto)
    {
        var item = _mapper.ToItem(dto);
        CopyToConsultation(item);
    }

    /// <summary>
    /// Initialize for new case creation.
    /// </summary>
    public void InitializeForNewCase(string patientName, Guid patientId, Guid userId)
    {
        Consultation.Reset();
        Consultation.PatientName = patientName;
        Consultation.PatientId = patientId;
        Consultation.UserId = userId;
        Consultation.MedicalCaseId = _context.MedicalCaseId;
    }

    public ConsultationInputDto? GetConsultationData() => Consultation.GetConsultationData();
    public bool Validate() => Consultation.Validate();
    public string ValidationMessage => Consultation.ValidationMessage;
    public void Reset() => Consultation.Reset();

    // Copy mapped item properties to our owned Consultation instance.
    // We maintain a single Consultation instance for stable XAML binding reference.
    private void CopyToConsultation(ConsultationItem source)
    {
        Consultation.Id = source.Id;
        Consultation.MedicalCaseId = source.MedicalCaseId;
        Consultation.PatientId = source.PatientId;
        Consultation.UserId = source.UserId;
        Consultation.PatientName = source.PatientName;
        Consultation.DoctorName = source.DoctorName;
        Consultation.PresentIllness = source.PresentIllness;
        Consultation.TongueDiagnosis = source.TongueDiagnosis;
        Consultation.PulseDiagnosis = source.PulseDiagnosis;
        Consultation.TcmDiagnosis = source.TcmDiagnosis;
        Consultation.CreatedAt = source.CreatedAt;
        Consultation.UpdatedAt = source.UpdatedAt;
    }
}
```

### Step 3: Run tests, commit

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~ConsultationEditorViewModelTests" -v m
git commit -m "feat: add ConsultationEditorViewModel child VM"
```

---

## Task 6: PrescriptionEditorViewModel

**Rationale:** Second child VM. Wraps PrescriptionItem with ObservableCollection<PrescriptionItemDto>, handles initialization, notifies parent of collection changes for state recalculation.

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/PrescriptionEditorViewModel.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/PrescriptionEditorViewModelTests.cs`

### Implementation Notes

Same pattern as ConsultationEditorViewModel, additionally:
- Subscribe to `Prescription.Items.CollectionChanged` to call `Host.NotifyStateChanged()`
- Expose `HasItems` for parent state computation
- `Dispose()` must unsubscribe collection changed handler
- `CopyToPrescription` must handle Items collection (clear + add, matching existing `InitializeChildViewModels` pattern)

### Key Test Scenarios

- InitializeFromDto maps fields AND Items collection
- Collection change triggers Host.NotifyStateChanged
- InitializeForNewCase sets MedicalCaseId from context
- Dispose unsubscribes event handlers
- GetPrescriptionData delegates to item

### Commit

```bash
git commit -m "feat: add PrescriptionEditorViewModel child VM with collection change notification"
```

---

## Task 7: MedicalCaseCommandsViewModel

**Rationale:** Aggregate root operations child VM. Consolidates save/suspend/complete/print/import/clear commands from the parent VM and handlers. All operations go through the MedicalCase aggregate root.

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/MedicalCaseCommandsViewModelTests.cs`

### Implementation Notes

Before implementing, read these source files to extract exact logic:
1. `MedicalCaseWorkspaceViewModel.cs` lines 500-900 (ExecuteSave, ExecuteSuspend, ExecuteComplete, EnterEditMode)
2. `PrescriptionPrintHandler.cs` (PrintPreviewAsync, BuildPrescriptionDetailDto)
3. `PrescriptionImportHandler.cs` (OpenFormulaImportDialog, OpenHistoryCopyDialog, ClearHerbItemsAsync)

**Constructor dependencies:**
- `IMedicalCaseWorkspaceContext context` - read state
- `IWorkspaceHost host` - UI operations
- `IMedicalCaseService medicalCaseService` - save/suspend/complete
- `PrescriptionPrintHandler printHandler` - print delegation
- `IDialogService? dialogService` - formula import/history copy dialogs
- `ILoggerFactory loggerFactory`

**Data provider delegates** (set by parent after child VM creation):
- `Func<ConsultationInputDto?> GetConsultationData`
- `Func<PrescriptionInputDto?> GetPrescriptionData`
- `Func<bool> ValidateConsultation`
- `Func<bool> ValidatePrescription`
- `Func<PrescriptionItem> GetPrescription` (for import operations)
- `Func<IEnumerable<HerbListDto>?> GetAllHerbs` (for import operations)

**Commands (all DelegateCommand):**
- SaveCommand, SuspendCommand, CompleteCommand
- PrintCommand, ImportFormulaCommand, CopyHistoryCommand, ClearHerbsCommand
- EnterEditModeCommand

**CanExecute bindings (observe State):**
- SaveCommand: State.IsEditing
- SuspendCommand: State.ShowSuspendButton
- CompleteCommand: State.ShowCompleteButton && State.CanComplete
- PrintCommand: State.CanPrint
- EnterEditModeCommand: State.ShowEditButton

**IMPORTANT: DelegateCommand.ObservesProperty does NOT work across child VM boundaries** because PropertyChanged fires on the parent. The parent VM must call `Commands.RefreshCanExecute()` when State changes.

### Key Test Scenarios

- SaveCommand invokes AggregateSaveAsync with correct parameters
- SuspendCommand calls SaveAndSuspendAsync
- CompleteCommand validates then calls SaveAndCompleteAsync
- Complete with invalid consultation shows error
- PrintCommand delegates to PrescriptionPrintHandler
- EnterEditMode transitions state via Host.NotifyStateChanged

### Commit

```bash
git commit -m "feat: add MedicalCaseCommandsViewModel with aggregate root operations"
```

---

## Task 8: PendingQueueViewModel (from Handler)

**Rationale:** Upgrade PendingQueueHandler (379 lines, 9 callback properties) to a proper child VM.

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs`

### Callback Replacement Map

| Handler Callback | Child VM Replacement |
|---|---|
| `GetCommonDialogService` | `Host.CommonDialogService` |
| `SetBusy` | `Host.SetBusy()` |
| `ShowErrorMessage` | `Host.ShowErrorAsync()` |
| `GetCurrentMedicalCaseId` | `_context.MedicalCaseId` |
| `GetCurrentPatient` | `_context.CurrentPatient` |
| `GetIsReadOnly` | `_context.State.IsReadOnly` |
| `SuspendOnly` | Callback delegate from parent (or via IWorkspaceHost extension) |
| `OnPropertyChanged` | Not needed (child has own INPC) |

### Key Properties (all [ObservableProperty])

- `Queue` (ObservableCollection<PendingMedicalCaseDto>)
- `SelectedCase` (PendingMedicalCaseDto?)
- `IsRefreshing` (bool)
- `HasNoPendingCases` (bool, computed)

### Key Commands

- `RefreshCommand` (DelegateCommand)
- `SelectCommand` (DelegateCommand<PendingMedicalCaseDto>)

### Commit

```bash
git commit -m "feat: upgrade PendingQueueHandler to PendingQueueViewModel child VM"
```

---

## Task 9: CardReaderViewModel (from Handler)

**Rationale:** Upgrade CardReaderWorkspaceHandler (540 lines, 6 callback properties + PropertyChanged forwarding) to a proper child VM.

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/CardReaderViewModel.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Clinical/CardReaderViewModelTests.cs`

### Callback Replacement Map

| Handler Callback | Child VM Replacement |
|---|---|
| `SetBusy` | `Host.SetBusy()` |
| `ShowErrorMessage` | `Host.ShowErrorAsync()` |
| `ShowSuccessMessage` | `Host.ShowSuccessAsync()` |
| `GetCommonDialogService` | `Host.CommonDialogService` |
| `OnPropertyChanged` | Not needed (child has own INPC) |
| `GetCurrentUserId` | `_context.SessionManager?.CurrentUser?.Id` |

### Key Properties

- `IsConnected` (bool, delegate to ICardReaderService)
- `IsAutoReadEnabled` (bool, delegate to ICardReaderService)
- `IsReading` (bool)
- `StatusMessage` (string)

### Key Commands

- `ReadCardCommand` (DelegateCommand)
- `ToggleAutoReadCommand` (DelegateCommand)

### Commit

```bash
git commit -m "feat: upgrade CardReaderWorkspaceHandler to CardReaderViewModel child VM"
```

---

## Task 10: Parent VM Rewrite + DI Registration

**Rationale:** Rewrite MedicalCaseWorkspaceViewModel as thin composition shell (~200 lines). It implements both IMedicalCaseWorkspaceContext and IWorkspaceHost, creates child VMs, and delegates all operations.

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` (1099 -> ~200 lines)
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`

### New Class Structure

```csharp
public partial class MedicalCaseWorkspaceViewModel : NavigableViewModelBase,
    IMedicalCaseWorkspaceContext, IWorkspaceHost
{
    // === Child VMs (created in constructor, NOT container-resolved) ===
    public ConsultationEditorViewModel ConsultationEditor { get; }
    public PrescriptionEditorViewModel PrescriptionEditor { get; }
    public MedicalCaseCommandsViewModel Commands { get; }
    public PendingQueueViewModel PendingQueue { get; }
    public CardReaderViewModel CardReader { get; }

    // === IMedicalCaseWorkspaceContext ===
    public WorkspaceState State { get; private set; } = new();
    [ObservableProperty] private Guid _medicalCaseId;
    [ObservableProperty] private PatientDetailDto? _currentPatient;
    public ISessionManager? SessionManager => Services.SessionManager;

    // === IWorkspaceHost ===
    public void SetBusy(bool isBusy, string? message = null) => base.SetBusy(isBusy, message);
    public Task ShowErrorAsync(string msg) => ShowErrorMessageAsync(msg);
    public Task ShowSuccessAsync(string msg) => ShowSuccessMessageAsync(msg);
    public Task<bool> ShowConfirmAsync(string msg, string title) => ShowConfirmMessageAsync(msg, title);
    public ICommonDialogService? CommonDialogService => Services.CommonDialogService;
    public void NotifyStateChanged() => UpdateState();

    // === State Management ===
    private void UpdateState()
    {
        State = State with {
            CanComplete = CalculateCanComplete(),
            CanPrint = PrescriptionEditor.HasItems
        };
        OnPropertyChanged(nameof(State));
        Commands.RefreshCanExecute();
    }

    // === Navigation Lifecycle ===
    public override async void OnNavigatedTo(NavigationContext context)
    {
        // 1. Parse navigation parameters
        // 2. Set State = new WorkspaceState().DetermineFromContext(...)
        // 3. Load data via IMedicalCaseService
        // 4. Initialize child VMs (ConsultationEditor, PrescriptionEditor, etc.)
        // 5. Wire data providers between child VMs
    }
}
```

### Constructor (target: ~30 lines)

```csharp
public MedicalCaseWorkspaceViewModel(
    IViewModelServices services,
    IMedicalCaseService medicalCaseService,
    INavigationCoordinator navigationCoordinator,
    IActiveConsultationService activeConsultationService,
    IPendingQueueManager pendingQueueManager,
    ICardReaderService cardReaderService,
    IPatientCardReaderIntegration patientCardReaderIntegration,
    IDialogService? dialogService = null)
    : base(services)
{
    _medicalCaseService = medicalCaseService;
    _navigationCoordinator = navigationCoordinator;
    _activeConsultationService = activeConsultationService;

    // Create child VMs (passing `this` as context + host)
    ConsultationEditor = new ConsultationEditorViewModel(this, this, services.LoggerFactory);
    PrescriptionEditor = new PrescriptionEditorViewModel(this, this, services.LoggerFactory);
    Commands = new MedicalCaseCommandsViewModel(this, this, services.LoggerFactory,
        medicalCaseService, new PrescriptionPrintHandler(...), dialogService);
    PendingQueue = new PendingQueueViewModel(this, this, services.LoggerFactory,
        medicalCaseService, pendingQueueManager, navigationCoordinator);
    CardReader = new CardReaderViewModel(this, this, services.LoggerFactory,
        cardReaderService, patientCardReaderIntegration);

    // Wire data providers
    Commands.GetConsultationData = () => ConsultationEditor.GetConsultationData();
    Commands.GetPrescriptionData = () => PrescriptionEditor.GetPrescriptionData();
    Commands.ValidateConsultation = () => ConsultationEditor.Validate();
    Commands.ValidatePrescription = () => PrescriptionEditor.Validate();
    Commands.GetPrescription = () => PrescriptionEditor.Prescription;
}
```

### DI Registration Update (MedicalCaseModule.cs)

No changes needed for child VMs (they're not container-resolved). Remove any stale registrations:
- Remove `MedicalCaseWorkspaceCoordinator` (already merged)
- Remove `MedicalCaseEditModeStateMachine` (already removed)
- Add `MedicalCaseChangeTracker` (Task 4)

### Verification

```bash
dotnet build LYBT.All.sln -v m
```

### Commit

```bash
git commit -m "refactor: rewrite MedicalCaseWorkspaceViewModel as thin composition shell (~200 lines)"
```

---

## Task 11: XAML Binding Path Updates

**Rationale:** Update XAML bindings to route through child VM properties.

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`

### Binding Path Changes

| Category | Current | New |
|---|---|---|
| **Data** | `{Binding Consultation}` | `{Binding ConsultationEditor.Consultation}` |
| **Data** | `{Binding Prescription}` | `{Binding PrescriptionEditor.Prescription}` |
| **Data** | `{Binding AllHerbs}` | `{Binding PrescriptionEditor.AllHerbs}` or keep flat |
| **State** | `{Binding HeaderTitle}` | `{Binding State.HeaderTitle}` |
| **State** | `{Binding IsEditing}` | `{Binding State.IsEditing}` |
| **State** | `{Binding ShowSuspendButton}` | `{Binding State.ShowSuspendButton}` |
| **State** | `{Binding ShowCompleteButton}` | `{Binding State.ShowCompleteButton}` |
| **State** | `{Binding ShowSaveButton}` | `{Binding State.ShowSaveButton}` |
| **State** | `{Binding ShowEditButton}` (footer) | `{Binding State.ShowEditButton}` |
| **State** | `{Binding CanPrintPrescription}` | `{Binding State.CanPrint}` |
| **State** | `{Binding CanComplete}` | `{Binding State.CanComplete}` |
| **State** | `{Binding IsReadOnly}` | `{Binding State.IsReadOnly}` |
| **State** | `{Binding IsHistoricalEditMode}` | `{Binding State.IsHistoricalEditMode}` |
| **Commands** | `{Binding SuspendCommand}` | `{Binding Commands.SuspendCommand}` |
| **Commands** | `{Binding SaveCommand}` | `{Binding Commands.SaveCommand}` |
| **Commands** | `{Binding CompleteMedicalCaseCommand}` | `{Binding Commands.CompleteCommand}` |
| **Commands** | `{Binding PrintPrescriptionCommand}` | `{Binding Commands.PrintCommand}` |
| **Commands** | `{Binding SaveChangesCommand}` | `{Binding Commands.SaveCommand}` |
| **Commands** | `{Binding EnterEditModeCommand}` | `{Binding Commands.EnterEditModeCommand}` |
| **Commands** | `{Binding OpenFormulaImportDialogCommand}` | `{Binding Commands.ImportFormulaCommand}` |
| **Commands** | `{Binding OpenHistoryCopyDialogCommand}` | `{Binding Commands.CopyHistoryCommand}` |
| **Commands** | `{Binding ClearHerbItemsCommand}` | `{Binding Commands.ClearHerbsCommand}` |
| **Queue** | `{Binding PendingQueue}` | `{Binding PendingQueue.Queue}` |
| **Queue** | `{Binding SelectedPendingCase}` | `{Binding PendingQueue.SelectedCase}` |
| **Queue** | `{Binding SelectPendingCaseCommand}` | `{Binding PendingQueue.SelectCommand}` |
| **Queue** | `{Binding RefreshQueueCommand}` | `{Binding PendingQueue.RefreshCommand}` |
| **Queue** | `{Binding IsRefreshingPendingQueue}` | `{Binding PendingQueue.IsRefreshing}` |
| **Queue** | `{Binding HasNoPendingCases}` | `{Binding PendingQueue.HasNoPendingCases}` |
| **CardReader** | `{Binding IsCardReaderConnected}` | `{Binding CardReader.IsConnected}` |
| **CardReader** | `{Binding IsAutoReadEnabled}` | `{Binding CardReader.IsAutoReadEnabled}` |
| **CardReader** | `{Binding IsReading}` | `{Binding CardReader.IsReading}` |
| **CardReader** | `{Binding CardReaderStatusMessage}` | `{Binding CardReader.StatusMessage}` |
| **CardReader** | `{Binding ReadCardCommand}` | `{Binding CardReader.ReadCardCommand}` |
| **CardReader** | `{Binding ToggleAutoReadCommand}` | `{Binding CardReader.ToggleAutoReadCommand}` |

### Remark/EditReason Handling

`Remark` and `EditReason` are TwoWay user-editable fields. WorkspaceState is immutable (record). Keep these as direct properties on parent VM, not in State.

### Commit

```bash
git commit -m "refactor: update XAML binding paths for child VM composition"
```

---

## Task 12: Delete Old Handlers + Cleanup

**Files to Delete:**
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/PendingQueueHandler.cs` (379 lines)
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/PrescriptionImportHandler.cs` (337 lines)
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/CardReaderWorkspaceHandler.cs` (540 lines)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs` (283 lines)

### Pre-deletion Verification

```bash
# Verify no remaining references
rg "PendingQueueHandler" src/ --type cs
rg "PrescriptionImportHandler" src/ --type cs
rg "CardReaderWorkspaceHandler" src/ --type cs
rg "PrescriptionPrintHandler" src/ --type cs
```

### Post-deletion Verification

```bash
dotnet build LYBT.All.sln -v m
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests" -v m
```

### Commit

```bash
git commit -m "refactor: remove old handler files replaced by child ViewModels

Deleted: PendingQueueHandler.cs (379 lines)
Deleted: PrescriptionImportHandler.cs (337 lines)
Deleted: CardReaderWorkspaceHandler.cs (540 lines)
Deleted: PrescriptionPrintHandler.cs (283 lines)
Total: 1539 lines removed"
```

---

## Task 13: Full Verification + Documentation

### Step 1: Full build and test

```bash
dotnet build LYBT.All.sln -v m
dotnet test tests/LYBT.Tests.Desktop/ -v m
dotnet test tests/LYBT.Tests.Server/ -v m
dotnet test tests/LYBT.Tests.Architecture/ -v m
```

### Step 2: Update documentation

- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/CLAUDE.md` - add child VM section, update code structure
- `CLAUDE.md` root - add Composite VM pattern reference
- Serena memory - record architectural decision

### Step 3: Commit

```bash
git commit -m "docs: update documentation for composite ViewModel refactoring"
```

---

## Dependency Graph

```
Task 1 (WorkspaceState)  ─────┐
Task 2 (IWorkspaceHost + Base) ┼──→ Task 5 (ConsultationEditorVM)  ─┐
Task 3 (Context Interface) ────┤    Task 6 (PrescriptionEditorVM) ──┤
Task 4 (ChangeTracker)  ───────┘    Task 8 (PendingQueueVM)  ───────┤
                                    Task 9 (CardReaderVM)  ──────────┤
                                         │                          │
                                    Task 7 (CommandsVM) ←───5,6─────┤
                                         │                          │
                                    Task 10 (Parent VM Rewrite) ←───┘
                                         │
                                    Task 11 (XAML Updates)
                                         │
                                    Task 12 (Cleanup)
                                         │
                                    Task 13 (Verification)
```

**Parallelizable:**
- Tasks 1, 2, 4 (no dependencies)
- Tasks 5, 6 (after 1-3)
- Tasks 7, 8, 9 (after 2-3; 7 needs 5,6 for data provider types)
- Tasks 10-13 (sequential)

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| XAML binding breaks silently | Test every binding path with runtime UI; WPF trace-level binding errors in Output window |
| ChangeTracker field mismatch | Copy exact comparison logic from existing MedicalCaseService.cs |
| Command CanExecute not updating | Parent calls `Commands.RefreshCanExecute()` when State changes |
| DryIoc resolution failure | Child VMs manually created (not container-resolved); only services are DI'd |
| Mapperly source gen conflicts | Follow existing pattern: [MapperIgnoreTarget] + manual mapping wrapper |
| Thread safety in CardReader | Preserve existing Dispatcher.Invoke patterns from handler |
| PendingQueue SuspendOnly callback | Use delegate Func from parent, or extend IWorkspaceHost |

---

## Metrics Target

| Metric | Before | After |
|--------|--------|-------|
| MedicalCaseWorkspaceViewModel | 1099 lines | ~200 lines |
| Handler callback properties | 32 total (8+8+9+7) | 0 |
| RaiseEditStateProperties() | 10 OnPropertyChanged + 4 RaiseCanExecuteChanged | 0 (record immutability) |
| InitializeChildViewModels() | 72 lines manual copy | 0 (Mapper.ToItem via child VMs) |
| Independently testable components | 1 (monolith) | 7 (parent + 5 child VMs + tracker) |
| Handler files | 4 (1539 lines) | 0 (replaced by child VMs) |
