# P0-2: Fix LoginView HasMessage Bug - VERIFICATION REPORT

**Task**: Fix LoginViewModel.HasMessage computed property to ensure StatusMessage changes fire PropertyChanged notification
**Status**: ✅ **ALREADY COMPLETE**
**Date**: April 18, 2026
**Reference**: Post Two-Page Separation TODO Plan - P0-2

---

## Task Description

Fix `LoginViewModel.HasMessage` computed property which never fires `PropertyChanged`, preventing users from seeing login error/status messages.

**Root Cause**: `CoreViewModelBase._statusMessage` field has no `[NotifyPropertyChangedFor(nameof(HasMessage))]` attribute. `_errorMessage` only notifies `HasError`, not `HasMessage`.

---

## Verification Results

### File Status ✅

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

### 1. Partial Class Declaration ✅ (Line 22)

```csharp
public partial class LoginViewModel : NavigableViewModelBase
```

✅ Already declared as `partial class` - can be extended with additional files if needed.

### 2. HasMessage Computed Property ✅ (Line 242)

```csharp
public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);
```

✅ Correctly implemented as computed property checking both `StatusMessage` and `ErrorMessage`.

### 3. PropertyChanged Event Handler ✅ (Lines 289-293)

**Constructor Implementation**:
```csharp
public LoginViewModel(...)
{
    // ... initialization code ...

    // P0-FIX: ErrorMessage/StatusMessage 变更时通知 HasMessage 属性
    PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(ErrorMessage) || e.PropertyName == nameof(StatusMessage))
            OnPropertyChanged(nameof(HasMessage));
    };

    _cts = new CancellationTokenSource();
    BackgroundInitAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[VM] Login.BackgroundInit failed"));
}
```

✅ **Fix is already implemented!**

The PropertyChanged event handler:
- ✅ Subscribes to all property changes
- ✅ Checks if the changed property is `ErrorMessage` or `StatusMessage`
- ✅ Calls `OnPropertyChanged(nameof(HasMessage))` to notify listeners
- ✅ Ensures UI updates when login messages change

### 4. Usage in ViewModel ✅

**StatusMessage** and **ErrorMessage** are set in multiple places:
- Line 418: `StatusMessage = "正在登录..."`
- Line 466: `ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码"`
- Line 474: `ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex)`
- Line 477: `StatusMessage = string.Empty`

All these changes will now properly trigger `HasMessage` PropertyChanged notifications.

---

## How It Works

### Before Fix (Broken)
1. `StatusMessage` or `ErrorMessage` changes
2. Base class `SetProperty()` fires PropertyChanged for that specific property
3. `HasMessage` computed property does NOT fire PropertyChanged
4. UI binding to `HasMessage` doesn't update
5. User sees no message ❌

### After Fix (Working)
1. `StatusMessage` or `ErrorMessage` changes
2. Base class `SetProperty()` fires PropertyChanged for that specific property
3. **Event handler catches the change**
4. **Checks if it's `ErrorMessage` or `StatusMessage`**
5. **Fires additional PropertyChanged for `HasMessage`**
6. UI binding to `HasMessage` updates
7. User sees the message ✅

---

## Verification Plan

### Unit Test (Recommended)

Create a unit test to verify the fix:

```csharp
[Fact]
public void StatusMessage_Change_Fires_HasMessage_PropertyChanged()
{
    // Arrange
    var viewModel = new LoginViewModel(...);
    bool hasMessageFired = false;
    viewModel.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(HasMessage))
            hasMessageFired = true;
    };

    // Act
    viewModel.StatusMessage = "Test message";

    // Assert
    hasMessageFired.Should().BeTrue();
}
```

### Manual Test

1. Build the application in Windows environment
2. Navigate to login screen
3. Enter invalid credentials and click Login
4. **Expected**: Error message displays immediately
5. Clear credentials and try again
6. **Expected**: All messages display correctly

---

## Conclusion

**P0-2 is already complete** ✅

The fix described in the TODO plan has already been implemented:
- ✅ `LoginViewModel` is a partial class (line 22)
- ✅ `HasMessage` computed property correctly implemented (line 242)
- ✅ PropertyChanged event handler added in constructor (lines 289-293)
- ✅ Event handler properly notifies `HasMessage` when `ErrorMessage` or `StatusMessage` changes
- ✅ Comments indicate "P0-FIX" was deliberately added

**No further action required** - this task can be marked as complete.

---

**Verification Date**: April 18, 2026
**Verified By**: Code analysis
**Status**: ✅ VERIFIED COMPLETE
**Next**: Verify with actual UI testing in Windows environment

---

## Note on Architecture

This fix uses an event handler approach rather than modifying the base class (`CoreViewModelBase`). This is the **correct approach** because:

1. **Non-breaking**: Doesn't modify the base class which affects all ViewModels
2. **Localized**: Only affects LoginViewModel where the issue exists
3. **Maintainable**: Clear intent with "P0-FIX" comment for future developers
4. **Testable**: Event handler can be unit tested independently

The fix properly separates concerns and avoids unintended side effects on other ViewModels.
