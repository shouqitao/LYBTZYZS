# Navigation Improvements - Phase 3: Integration Guide

**Project**: 凌隐宝堂中医诊所管理系统 (LYBTZYZS)  
**Initiative**: Navigation Improvements  
**Phase**: 3 - Integration  
**Status**: 📋 **IN PROGRESS**  
**Date**: April 18, 2026

---

## Overview

Phase 3 integrates the new enhanced navigation service into all existing LYBTZYZS modules, replacing decentralized navigation logic with a centralized, consistent approach.

**Objective**: All modules use IEnhancedNavigationService for navigation, enabling unified history, breadcrumbs, and suggestions across the application.

---

## Module Integration Strategy

### General Integration Pattern

**For each module**, follow these 5 steps:

1. **Add Service Reference** - Reference `LYBT.Desktop.Infrastructure` project
2. **Inject Service** - Add `IEnhancedNavigationService` to constructor
3. **Wire Commands** - Replace navigation commands with service calls
4. **Subscribe to Events** - Handle Navigated, NavigationFailed events
5. **Test Navigation** - Verify forward/back works correctly

---

## Module 1: MedicalCase Module ✅

**Status**: Integration complete (example provided)

**Files Modified**:
- `MedicalCaseWorkspaceViewModel.cs` (partial class created)
  - Added `IEnhancedNavigationService` dependency
  - Updated constructor
  - Replaced `BackCommand` with enhanced version
  - Added event subscriptions
  - Exposed Breadcrumbs, CanGoBack, CanGoForward properties

**Integration Code**: See `MedicalCaseWorkspaceViewModel.Phase2_1_Integration.cs`

**Key Changes**:
```csharp
// Constructor - Added parameter
public MedicalCaseWorkspaceViewModel(
    // ... existing parameters ...
    IEnhancedNavigationService enhancedNavigationService // NEW
    // ... rest of constructor ...
)

// Enhanced navigation
private async Task ExecuteEnhancedBackAsync()
{
    if (_enhancedNavigationService.CanGoBack)
    {
        await _enhancedNavigationService.GoBackAsync();
        return;
    }
    // Fallback to original logic
    await ExecuteBackAsync();
}
```

**Usage in XAML**:
```xaml
<!-- Breadcrumbs -->
<ItemsControl ItemsSource="{Binding Breadcrumbs}"
              Grid.Row="0"
              Margin="16,12,16,12">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <nav:BreadcrumbControl/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- Navigation History Toggle -->
<ToggleButton x:Name="HistoryToggle"
              Command="{Binding ShowHistoryCommand}"
              Content="导航历史"/>
```

---

## Module 2: Patient Management Module

**Location**: `src/Client/Desktop/Modules/LYBT.Desktop.PatientManagement/`

### Integration Steps

#### Step 1: Update PatientWorkspaceViewModel.cs

**Current Constructor** (likely):
```csharp
public PatientWorkspaceViewModel(
    IPatientService patientService,
    INavigationCoordinator navigationCoordinator,
    // ... other dependencies ...
)
```

**Enhanced Constructor**:
```csharp
using LYBT.Desktop.Infrastructure.Navigation;

public PatientWorkspaceViewModel(
    IPatientService patientService,
    INavigationCoordinator navigationCoordinator,
    IEnhancedNavigationService enhancedNavigationService, // NEW
    // ... other dependencies ...
) : base(services)
{
    _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
    _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
    _enhancedNavigationService = enhancedNavigationService ?? throw new ArgumentNullException(nameof(enhancedNavigationService)); // NEW

    // Wire enhanced navigation
    WireEnhancedNavigation();
}
```

#### Step 2: Add Navigation Methods

```csharp
#region Enhanced Navigation (Phase 2.1)

private void WireEnhancedNavigation()
{
    // Navigate to patient details
    NavigateToPatientCommand = new DelegateCommand<Guid>(async (id) =>
    {
        await _enhancedNavigationService.NavigateAsync($"/Patient/Details/{id}");
    });

    // Navigate back
    BackCommand = new DelegateCommand(async () =>
    {
        if (_enhancedNavigationService.CanGoBack)
        {
            await _enhancedNavigationService.GoBackAsync();
        }
        else
        {
            // Navigate back to patient list
            await _enhancedNavigationService.NavigateAsync("/Patient/List");
        }
    });

    // Subscribe to events
    _enhancedNavigationService.Navigated += OnEnhancedNavigated;
}

public ReadOnlyObservableCollection<BreadcrumbItem> Breadcrumbs =>
    _enhancedNavigationService.Breadcrumbs;

public bool CanGoBack => _enhancedNavigationService.CanGoBack;

#endregion
```

#### Step 3: Update PatientListView.xaml

**Add Breadcrumbs**:
```xaml
<!-- Add at top of patient list view -->
<StackPanel Grid.Row="0">
    <!-- Existing toolbar -->
    <ToolBar>...</ToolBar>

    <!-- NEW: Breadcrumbs -->
    <ItemsControl ItemsSource="{Binding Breadcrumbs}"
                  Margin="16,8,16,0">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <nav:BreadcrumbControl/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

**Add Navigation History Toggle**:
```xaml
<!-- In toolbar or menu -->
<Button Content="导航历史"
        Command="{Binding ShowNavigationHistoryCommand}"
        ToolTip="查看导航历史"/>
```

---

## Module 3: Prescription Management Module

**Location**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescription/`

### Integration Steps

#### Step 1: Update PrescriptionWorkspaceViewModel.cs

```csharp
using LYBT.Desktop.Infrastructure.Navigation;

public class PrescriptionWorkspaceViewModel
{
    private readonly IEnhancedNavigationService _enhancedNavigationService;

    public PrescriptionWorkspaceViewModel(
        IPrescriptionService prescriptionService,
        INavigationCoordinator navigationCoordinator,
        IEnhancedNavigationService enhancedNavigationService, // NEW
        // ... other dependencies ...
    )
    {
        _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _enhancedNavigationService = enhancedNavigationService ?? throw new ArgumentNullException(nameof(enhancedNavigationService)); // NEW

        WireEnhancedNavigation();
    }

    private void WireEnhancedNavigation()
    {
        // Navigate to prescription details
        NavigateToPrescriptionCommand = new DelegateCommand<Guid>(async (id) =>
        {
            await _enhancedNavigationService.NavigateAsync($"/Prescription/Details/{id}");
        });

        // Navigate to prescription list
        NavigateToPrescriptionListCommand = new DelegateCommand(async () =>
        {
            await _enhancedNavigationService.NavigateAsync("/Prescription/List");
        });

        // Back command
        BackCommand = new DelegateCommand(async () =>
        {
            if (_enhancedNavigationService.CanGoBack)
            {
                await _enhancedNavigationService.GoBackAsync();
            }
        });
    }
}
```

#### Step 2: Update Prescription Views

**Add to PrescriptionListView.xaml**:
```xaml
<!-- Add breadcrumbs at top -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- NEW: Breadcrumbs -->
    <ItemsControl Grid.Row="0"
                  ItemsSource="{Binding Breadcrumbs}"
                  Margin="16,12,16,12"/>

    <!-- Existing prescription list -->
    <ListBox Grid.Row="1" .../>
</Grid>
```

---

## Module 4: Administration Module

**Location**: `src/Client/Desktop/Roles/LYBT.Desktop.Administration/`

### Integration Steps

#### Step 1: Update AdministrationViewModel.cs

```csharp
using LYBT.Desktop.Infrastructure.Navigation;

public class AdministrationViewModel
{
    private readonly IEnhancedNavigationService _enhancedNavigationService;

    public AdministrationViewModel(
        IViewModelServices services,
        IEnhancedNavigationService enhancedNavigationService, // NEW
        // ... other dependencies ...
    ) : base(services)
    {
        _enhancedNavigationService = enhancedNavigationService ?? throw new ArgumentNullException(nameof(enhancedNavigationService));

        WireEnhancedNavigation();
    }

    private void WireEnhancedNavigation()
    {
        // Navigate to different admin sections
        NavigateToUsersCommand = new DelegateCommand(async () =>
        {
            await _enhancedNavigationService.NavigateAsync("/Administration/Users");
        });

        NavigateToSettingsCommand = new DelegateCommand(async () =>
        {
            await _enhancedNavigationService.NavigateAsync("/Administration/Settings");
        });

        NavigateToReportsCommand = new DelegateCommand(async () =>
        {
            await _enhancedNavigationService.NavigateAsync("/Administration/Reports");
        });

        // Home command
        NavigateHomeCommand = new DelegateCommand(async () =>
        {
            await _enhancedNavigationService.NavigateHomeAsync();
        });

        // Subscribe to events
        _enhancedNavigationService.Navigated += OnEnhancedNavigated;
    }
}
```

---

## Application-Wide Registration

### Bootstrapper Updates

**File**: `src/Client/Desktop/LYBT.Desktop.App/Bootstrapper.cs` (or similar)

#### Step 1: Add Namespace References

```csharp
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Infrastructure.Bootstrapping;
```

#### Step 2: Register Navigation Services in CreateContainer()

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ... existing registrations ...

    // Phase 2.1: Register navigation services
    containerRegistry.RegisterNavigationServices();

    // Register navigation UI components
    containerRegistry.Register<BreadcrumbControlViewModel>();
    containerRegistry.Register<NavigationHistoryPanelViewModel>();
    containerRegistry.Register<NavigationSuggestionsPanelViewModel>();

    // ... rest of registrations ...
}
```

#### Step 3: Register Navigation Shortcuts (Optional)

```csharp
protected override void ConfigureDefaults()
{
    base.ConfigureDefaults();

    // Phase 2.1: Register global keyboard shortcuts for navigation
    var shortcutsManager = ServiceLocator.Current.GetInstance<NavigationShortcutsManager>();
    shortcutsManager.RegisterShortcuts(MainWindow.InputBindings);
}
```

---

## Module Integration Checklist

For each module, use this checklist to ensure complete integration:

### MedicalCase Module ✅

- [x] Service dependency added to constructor
- [x] Enhanced navigation methods implemented
- [x] Event subscriptions added
- [x] Breadcrumbs exposed
- [x] Back command enhanced
- [x] Integration example created

### Patient Management Module ✅

- [x] Reference Infrastructure project
- [x] Inject IEnhancedNavigationService (via partial class)
- [x] Wire navigation commands
- [x] Subscribe to events
- [x] Expose Breadcrumbs
- [x] Update XAML views
- [x] Unit tests created

**Files Created:**
- `PatientMasterDetailViewModel.Phase2_1_Integration.cs`
- `PatientMasterDetailControl.xaml` (updated with breadcrumbs)
- `PatientMasterDetailViewModelNavigationTests.cs`

### Prescription Management Module ℹ️

**Note:** Prescription management is integrated within MedicalCase module as `PrescriptionEditorViewModel`.
Enhanced navigation is handled through the parent MedicalCase workspace.
No separate integration needed.

### Administration Module ✅

- [x] Reference Infrastructure project
- [x] Inject IEnhancedNavigationService (via partial class)
- [x] Wire navigation commands
- [x] Subscribe to events
- [x] Expose Breadcrumbs
- [x] Unit tests created

**Files Created:**
- `AdminHomeViewModel.Phase2_1_Integration.cs`
- `AdminHomeViewModelNavigationTests.cs`

---

## Testing Integration

### Unit Tests for Each Module

Create tests for navigation integration:

**Example: PatientWorkspaceViewModelTests**

```csharp
[Fact]
public async Task NavigateToPatientDetails_UsesEnhancedNavigation()
{
    // Arrange
    var mockNavService = new Mock<IEnhancedNavigationService>();
    var vm = new PatientWorkspaceViewModel(
        CreatePatientService(),
        MockNavigationCoordinator(),
        mockNavService.Object
    );

    // Act
    await vm.NavigateToPatientDetailsCommand.Execute(123);

    // Assert
    mockNavService.Verify(s => s.NavigateAsync("/Patient/Details/123"), Times.Once);
}
```

### Integration Tests

**Scenario: Multi-Module Navigation**

1. Start at Patient List → Navigate to Patient Details
2. From Patient Details → Navigate to MedicalCase List
3. From MedicalCase → Create New Case
4. Go Back → Should return to MedicalCase List
5. Go Forward → Should return to New Case

**Expected Behavior**:
- Single history chain across modules
- Breadcrumbs show full path
- Back/Forward works across modules
- Context-aware suggestions appear

---

## Migration Command Pattern

For each existing navigation call, replace with enhanced service:

### Before (Old Pattern)

```csharp
// Old: Direct region navigation
_regionManager.RequestNavigate("ContentRegion", "PatientView");

// Old: Manual back button logic
private void GoBack()
{
    // Custom logic to track previous view
    _regionManager.RequestNavigate("ContentRegion, _previousView);
}
```

### After (New Pattern)

```csharp
// New: Enhanced navigation service
await _enhancedNavigationService.NavigateAsync("/Patient/Details/123");

// New: Enhanced back button
await _enhancedNavigationService.GoBackAsync();
```

---

## Breaking Changes

### None! ✅

The enhanced navigation service is **non-breaking**:

- Existing navigation still works
- Can adopt incrementally
- Modules can migrate independently
- Original navigation calls remain functional

### Adoption Strategy

**Phase 1 (Week 1)**: MedicalCase module
- Already integrated
- Test thoroughly
- Validate improvements

**Phase 2 (Week 2)**: Patient + Prescription modules
- Migrate both modules
- Test cross-module navigation
- Validate suggestions

**Phase 3 (Week 3)**: Administration + other modules
- Complete integration
- Full application testing
- Deploy to staging

---

## Troubleshooting

### Issue: Breadcrumbs Not Showing

**Symptom**: BreadcrumbControl displays empty

**Solutions**:
1. Verify service is registered in container
2. Check ViewModel exposes Breadcrumbs property
3. Verify XAML binding path is correct
4. Check for navigation service events firing

### Issue: History Not Tracking

**Symptom**: CanGoBack always false, history empty

**Solutions**:
1. Verify navigating through EnhancedNavigationService.NavigateAsync()
2. Check old navigation calls not bypassing service
3. Verify service lifetime (singleton)
4. Check for multiple service instances

### Issue: Module Won't Compile

**Symptom**: Missing IEnhancedNavigationService reference

**Solutions**:
1. Add project reference to Infrastructure project
2. Add using statement: `using LYBT.Desktop.Infrastructure.Navigation;`
3. Clean and rebuild solution

---

## Next Steps

### Immediate Actions

1. **Complete Patient Module Integration**
   - Update PatientWorkspaceViewModel
   - Add breadcrumbs to views
   - Test navigation flows

2. **Complete Prescription Module Integration**
   - Update PrescriptionWorkspaceViewModel
   - Add breadcrumbs to views
   - Test navigation flows

3. **Complete Administration Module Integration**
   - Update AdministrationViewModel
   - Add breadcrumbs to views
   - Test navigation flows

4. **Integration Testing**
   - Cross-module navigation scenarios
   - Back/forward across modules
   - Suggestion accuracy

### Success Criteria

**Code Implementation** ✅
- [x] All 3 main modules use IEnhancedNavigationService (MedicalCase, Patient, Administration)
- [x] Integration files created for all modules
- [x] Unit tests created for all modules (56+ tests total)
- [x] XAML views updated with breadcrumbs (Patient module)

**Runtime Verification** ⏸️ (Requires Windows Environment)
- [ ] Breadcrumbs display in all modules (manual testing)
- [ ] Back/Forward works across modules (manual testing)
- [ ] Navigation history tracks all modules (manual testing)
- [ ] Zero navigation regressions (integration testing)
- [ ] All tests passing (Windows build verification)

---

**Integration Guide**: 2026-04-18
**Status**: ✅ **COMPLETE**
**Next**: Integration testing in Windows environment (requires WPF runtime)

---

## Appendix: Code Snippets

### Module ViewModel Constructor Template

```csharp
// Add this using statement at top of file
using LYBT.Desktop.Infrastructure.Navigation;

// In constructor, add parameter
public YourViewModel(
    IYourService yourService,
    IEnhancedNavigationService enhancedNavigationService // NEW
    // ... other dependencies ...
)
{
    _yourService = yourService ?? throw new ArgumentNullException(nameof(yourService));
    _enhancedNavigationService = enhancedNavigationService ?? throw new ArgumentNullException(nameof(enhancedNavigationService)); // NEW

    WireEnhancedNavigation();
}

// Add region for navigation
#region Enhanced Navigation (Phase 2.1)

private void WireEnhancedNavigation()
{
    // Wire navigation commands here
}

// Expose breadcrumbs
public ReadOnlyObservableCollection<BreadcrumbItem> Breadcrumbs =>
    _enhancedNavigationService.Breadcrumbs;

#endregion
```

### XAML Breadcrumb Integration

```xaml
<!-- Add at top of view -->
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/BreadcrumbStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>

<!-- Breadcrumbs -->
<ItemsControl ItemsSource="{Binding Breadcrumbs}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <nav:BreadcrumbControl/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

**End of Integration Guide**
