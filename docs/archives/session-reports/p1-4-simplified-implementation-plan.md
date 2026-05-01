# P1-4: Add Validation Error Display - SIMPLIFIED IMPLEMENTATION PLAN

**Status**: ✅ REVISED PLAN - SIMPLER APPROACH
**Date**: April 18, 2026

## Architecture Discovery

**Current Base Classes**:
- `ConsultationItem` → `Prism.Mvvm.BindableBase` (Prism library)
- `PrescriptionItem` → `Prism.Mvvm.BindableBase` (Prism library)
- `CoreViewModelBase` → `CommunityToolkit.Mvvm.ObservableObject`

**Key Finding**: Cannot modify Prism.BindableBase, so we need to add `INotifyDataErrorInfo` directly to Item classes.

---

## Simplified Solution

### Approach: Add INotifyDataErrorInfo to Item Classes

Add `INotifyDataErrorInfo` implementation to `ConsultationItem` and `PrescriptionItem` directly. This bridges `IValidatable.ValidationMessage` to WPF validation system.

### Implementation

#### 1. ConsultationItem - Add INotifyDataErrorInfo

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/ConsultationItem.cs`

**Update class declaration**:
```csharp
public class ConsultationItem : BindableBase, IDataProvider, IValidatable, INotifyDataErrorInfo
```

**Add after IValidatable implementation** (after line 270):
```csharp
#region INotifyDataErrorInfo Implementation (P1-4 FIX)

/// <summary>P1-4: Fires when validation errors change</summary>
public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

/// <summary>P1-4: Returns true if validation message is present</summary>
bool INotifyDataErrorInfo.HasErrors => !string.IsNullOrWhiteSpace(ValidationMessage);

/// <summary>P1-4: Returns validation errors for WPF</summary>
IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
{
    // Return errors for all properties if no specific property requested
    if (string.IsNullOrEmpty(propertyName))
    {
        return !string.IsNullOrWhiteSpace(ValidationMessage) 
            ? new[] { ValidationMessage } 
            : Enumerable.Empty<string>();
    }
    
    // Return errors for specific property
    // For TcmDiagnosis, return ValidationMessage if it exists
    if (propertyName == nameof(TcmDiagnosis) && !string.IsNullOrWhiteSpace(ValidationMessage))
    {
        return new[] { ValidationMessage };
    }
    
    return Enumerable.Empty<string>();
}

/// <summary>P1-4: Override ValidationMessage setter to fire ErrorsChanged</summary>
private string _validationMessage = string.Empty;
public new string ValidationMessage
{
    get => _validationMessage;
    set
    {
        if (_validationMessage != value)
        {
            _validationMessage = value;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(TcmDiagnosis)));
        }
    }
}

#endregion
```

#### 2. PrescriptionItem - Add INotifyDataErrorInfo

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/PrescriptionItem.cs`

**Update class declaration**:
```csharp
public class PrescriptionItem : BindableBase, IDataProvider, IValidatable, INotifyDataErrorInfo
```

**Add after IValidatable implementation** (similar to ConsultationItem)

#### 3. Update XAML Bindings

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`

**PresentIllness TextBox** (line 183):
```xaml
<!-- BEFORE -->
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
         Style="{DynamicResource EditableTextBoxStyle}" ... />

<!-- AFTER -->
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}" ... />
```

**TcmDiagnosis TextBox** (line 216) - Already correct, just verify:
```xaml
<TextBox Text="{Binding Consultation.TcmDiagnosis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}" ... />
```

---

## Why This Approach Works

1. **Minimal Changes**: Only modifies 2 Item classes
2. **No Base Class Changes**: Prism.BindableBase stays untouched
3. **Direct Bridge**: ValidationMessage setter fires ErrorsChanged
4. **WPF Integration**: ValidatesOnNotifyDataErrors=True enables validation
5. **Existing Styles**: ValidatingTextBoxStyle works correctly

---

## Data Flow

```
User Action: Tab away from empty field
    ↓
WPF: ValidatesOnNotifyDataErrors triggers
    ↓
ConsultationItem.Validate() called
    ↓
ValidationMessage = "请填写中医诊断"
    ↓
ValidationMessage setter fires ErrorsChanged event
    ↓
WPF: Validation.Errors collection populated
    ↓
ValidatingTextBoxStyle shows red border + ToolTip
```

---

## Files Modified

1. **ConsultationItem.cs**
   - Add INotifyDataErrorInfo interface
   - Override ValidationMessage property
   - Add ErrorsChanged event firing
   - ~30 lines added

2. **PrescriptionItem.cs**
   - Same changes as ConsultationItem
   - ~30 lines added

3. **MedicalCaseEditControl.xaml**
   - Update PresentIllness binding
   - Verify TcmDiagnosis binding
   - ~3 lines modified

**Total**: ~63 lines across 3 files

---

## This is much simpler than the original plan because:
- ✅ No need to modify base classes
- ✅ No need to create bridge methods
- ✅ Direct implementation in Item classes
- ✅ Leverages existing IValidatable infrastructure
- ✅ Minimal XAML changes

