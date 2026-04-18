# P1-4: Add Validation Error Display - ANALYSIS & IMPLEMENTATION PLAN

**Task**: Add validation error display to MedicalCaseWorkspace
**Status**: 🔍 ANALYSIS COMPLETE - READY FOR IMPLEMENTATION
**Date**: April 18, 2026
**Reference**: Post Two-Page Separation TODO Plan - P1-4

---

## Problem Statement

Validation errors from ViewModel/Model validation (`IValidatable`) are **not displayed** to the user in MedicalCaseWorkspace. When validation fails (e.g., empty required field), the user sees no visual feedback.

---

## Root Cause Analysis

### Current State

**Validation Infrastructure EXISTS** ✅:
1. **ValidatingTextBoxStyle** (InputStyles.xaml, lines 109-130):
   - Has `Validation.ErrorTemplate` (red border on error)
   - Has ToolTip trigger showing `Validation.Errors[0].ErrorContent`
   - Uses WPF's built-in validation system

2. **IValidatable Interface** (implemented):
   - `ConsultationItem.Validate()` - checks `TcmDiagnosis` is complete
   - `ConsultationItem.ValidationMessage` - stores error text
   - `PrescriptionItem.Validate()` - validates prescription data
   - `PrescriptionItem.ValidationMessage` - stores error text

3. **XAML Usage**:
   - **TcmDiagnosis** (line 216): Uses `ValidatingTextBoxStyle` ✅
   - **PresentIllness** (line 183): Uses `EditableTextBoxStyle` (no validation)

### The Problem ❌

**Missing Bridge Between IValidatable and WPF Validation**:

```
IValidatable (Custom)                WPF Validation System
┌─────────────────────┐              ┌──────────────────────┐
│ Validate()          │              │ Validation.Errors     │
│ ValidationMessage    │  ❌ NO LINK  │ INotifyDataErrorInfo  │
└─────────────────────┘              └──────────────────────┘
```

**What Happens When Validation Fails**:
1. `ConsultationItem.Validate()` is called
2. Sets `ValidationMessage = "请填写中医诊断"`
3. `PropertyChanged` fires for `ValidationMessage` property
4. **WPF doesn't see the error** because `INotifyDataErrorInfo` is NOT implemented
5. **No red border, no tooltip, no error message** ❌

**Why ValidatingTextBoxStyle Doesn't Work**:
- ValidatingTextBoxStyle relies on `Validation.Errors` collection
- `Validation.Errors` is populated by `INotifyDataErrorInfo.ErrorsChanged` event
- `BindableBase` (base class) does NOT implement `INotifyDataErrorInfo`
- Result: Style works, but validation errors are invisible

---

## Solution Design

### Approach: Implement INotifyDataErrorInfo on BindableBase

Add `INotifyDataErrorInfo` implementation to `BindableBase` to bridge `IValidatable` and WPF validation.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ UI Layer                                                    │
│  TextBox with ValidatingTextBoxStyle                      │
│    ↓ ValidatesOnDataErrors=True                           │
│    ↓ Watches Validation.HasError                          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ WPF Validation System                                     │
│  Listens to INotifyDataErrorInfo.ErrorsChanged              │
│  Populates Validation.Errors collection                     │
│  Shows red border via Validation.ErrorTemplate              │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ BindableBase (Enhanced)                                   │
│  Implements INotifyDataErrorInfo                           │
│  Bridges IValidatable → WPF validation                      │
│  - Listens to IValidatable objects                         │
│  - Fires ErrorsChanged when ValidationMessage changes      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Item Models (IValidatable)                                 │
│  ConsultationItem                                         │
│  PrescriptionItem                                          │
│  - Validate() method                                       │
│  - ValidationMessage property                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Extend BindableBase with INotifyDataErrorInfo

**File**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/CoreViewModelBase.cs`

OR

**File**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/BindableBase.cs`

**Add**:
```csharp
public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
private readonly Dictionary<string, List<string>> _errors = new();

bool INotifyDataErrorInfo.HasErrors => _errors.Count > 0;

IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
{
    if (string.IsNullOrEmpty(propertyName))
        return _errors.SelectMany(kvp => kvp.Value);
    
    return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
}

/// <summary>
/// Clear validation errors for a property
/// </summary>
protected void ClearErrors(string? propertyName = null)
{
    if (string.IsNullOrEmpty(propertyName))
    {
        _errors.Clear();
    }
    else
    {
        _errors.Remove(propertyName);
    }
    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}

/// <summary>
/// Set validation error for a property
/// </summary>
protected void SetError(string propertyName, string error)
{
    if (!_errors.ContainsKey(propertyName))
        _errors[propertyName] = new List<string>();
    
    if (!_errors[propertyName].Contains(error))
    {
        _errors[propertyName].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
```

### Phase 2: Implement Validation Property Bridge

**Add** to `BindableBase`:

```csharp
private IValidatable? _validatableChild;
private string? _validatablePropertyName;

/// <summary>
/// Bridge IValidatable child to WPF validation system
/// Call this in constructor to enable validation for child object
/// </summary>
protected void RegisterValidatableChild(IValidatable validatable, string propertyName)
{
    _validatableChild = validatable;
    _validatablePropertyName = propertyName;
    
    // Subscribe to ValidationMessage changes
    if (validatable is BindableBase bindable)
    {
        bindable.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IValidatable.ValidationMessage))
            {
                OnValidationMessageChanged(validatable.ValidationMessage);
            }
        };
    }
}

private void OnValidationMessageChanged(string? validationMessage)
{
    if (string.IsNullOrEmpty(validationMessage))
    {
        ClearErrors(_validatablePropertyName);
    }
    else
    {
        SetError(_validatablePropertyName, validationMessage);
    }
}
```

### Phase 3: Update Child ViewModels

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs`

**Update constructor**:
```csharp
public ConsultationEditorViewModel(...)
{
    _consultation = new ConsultationItem();
    
    // P1-4 FIX: Enable WPF validation for ConsultationItem
    RegisterValidatableChild(_consultation, nameof(Consultation.TcmDiagnosis));
}
```

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/PrescriptionEditorViewModel.cs`

**Update constructor**:
```csharp
public PrescriptionEditorViewModel(...)
{
    _prescription = new PrescriptionItem();
    
    // P1-4 FIX: Enable WPF validation for PrescriptionItem
    RegisterValidatableChild(_prescription, nameof(Prescription.ItemCount));
}
```

### Phase 4: Update XAML Bindings

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`

**Update PresentIllness TextBox** (line 183):
```xaml
<!-- BEFORE -->
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
         Style="{DynamicResource EditableTextBoxStyle}"
         TextWrapping="Wrap" MinHeight="60" AcceptsReturn="True"/>

<!-- AFTER -->
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}"
         TextWrapping="Wrap" MinHeight="60" AcceptsReturn="True"/>
```

**Verify TcmDiagnosis TextBox** (line 216):
```xaml
<!-- Should already be correct -->
<TextBox Text="{Binding Consultation.TcmDiagnosis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}"
         MinHeight="36"/>
```

---

## Data Flow After Fix

### When User Leaves Empty Required Field

```
1. User tabs away from empty TcmDiagnosis field
   ↓
2. WPF validation triggers (ValidatesOnNotifyDataErrors=True)
   ↓
3. ConsultationItem.Validate() called
   ↓
4. Validate() returns false, sets ValidationMessage = "请填写中医诊断"
   ↓
5. BindableBase detects ValidationMessage change
   ↓
6. Calls SetError("TcmDiagnosis", "请填写中医诊断")
   ↓
7. Fires ErrorsChanged event
   ↓
8. WPF Validation.Errors collection populated
   ↓
9. Validation.HasError = True
   ↓
10. ValidatingTextBoxStyle triggers:
    - Red border appears (Validation.ErrorTemplate)
    - ToolTip shows error message
    - Focus can't move (if configured)
```

---

## Testing Strategy

### Unit Test

```csharp
[Fact]
public void ConsultationItem_TcmDiagnosisEmpty_ValidationErrorDisplayed()
{
    // Arrange
    var viewModel = CreateViewModel();
    var errorsChangedFired = false;
    ((INotifyDataErrorInfo)viewModel).ErrorsChanged += (s, e) => errorsChangedFired = true;
    
    // Act
    viewModel.Consultation.TcmDiagnosis = "";
    viewModel.Consultation.Validate();
    
    // Assert
    Assert.True(errorsChangedFired);
    var errors = ((INotifyDataErrorInfo)viewModel).GetErrors(nameof(Consultation.TcmDiagnosis));
    Assert.NotEmpty(errors);
}
```

### Manual Test

1. Open MedicalCaseWorkspace in edit mode
2. Leave TcmDiagnosis field empty
3. Tab away from the field
4. **Expected**: Red border appears, tooltip shows "请填写中医诊断"
5. Fill in TcmDiagnosis
6. **Expected**: Red border disappears, validation passes

---

## Files to Modify

1. **BindableBase.cs** (or CoreViewModelBase.cs)
   - Add INotifyDataErrorInfo implementation
   - Add validation bridge methods
   - ~80 lines added

2. **ConsultationEditorViewModel.cs**
   - Register ConsultationItem for validation
   - ~5 lines modified

3. **PrescriptionEditorViewModel.cs**
   - Register PrescriptionItem for validation
   - ~5 lines modified

4. **MedicalCaseEditControl.xaml**
   - Update PresentIllness TextBox style and binding
   - ~3 lines modified

**Total**: ~93 lines across 4 files

---

## Compliance

✅ **Non-Breaking**: Additive interface implementation
✅ **WPF Best Practices**: Uses built-in validation system
✅ **Consistent**: Follows existing ValidatingTextBoxStyle pattern
✅ **Testable**: ErrorsChanged event can be unit tested
✅ **Documented**: All changes marked with P1-4 FIX comments

---

## Alternatives Considered

### Alternative 1: Add ValidationMessage TextBlocks
❌ **Rejected**: Requires XAML changes for every field
❌ **Rejected**: Manual binding for each validation message

### Alternative 2: Use DataAnnotations
❌ **Rejected**: Requires server-side validation attributes
❌ **Rejected**: More complex infrastructure

### Alternative 3: Custom ValidationAdorner
❌ **Rejected**: More complex than needed
❌ **Rejected**: WPF built-in system is sufficient

**Chosen Approach**: INotifyDataErrorInfo on BindableBase
- ✅ Uses WPF built-in validation system
- ✅ Minimal XAML changes
- ✅ Reusable across all Item classes
- ✅ Follows MVVM best practices

---

## Verification Checklist

- [ ] BindableBase implements INotifyDataErrorInfo
- [ ] RegisterValidatableChild method added
- [ ] ConsultationEditorViewModel registers child
- [ ] PrescriptionEditorViewModel registers child
- [ ] PresentIllness TextBox uses ValidatingTextBoxStyle
- [ ] TcmDiagnosis TextBox has ValidatesOnNotifyDataErrors
- [ ] Unit test created
- [ ] Manual test: Empty field shows red border
- [ ] Manual test: Filled field clears red border
- [ ] Existing tests still pass

---

**Status**: 🔍 ANALYSIS COMPLETE - READY FOR IMPLEMENTATION
**Estimated Effort**: 2-3 hours
**Risk**: Low - additive enhancement, no breaking changes

