# Validation & Error Feedback Fix Plan

> **Status**: READY FOR EXECUTION  
> **Approach**: TDD — write failing tests first, then implement fixes  
> **Commit Strategy**: One atomic commit per TODO group (marked with commit labels)  
> **Parallelization**: See Section 4 for which tasks can run simultaneously

---

## Section 1: P0 — LoginView HasMessage Bug (URGENT)

**Root Cause**: `HasMessage` is a computed property in `LoginViewModel` that depends on `StatusMessage` and `ErrorMessage` (defined in `CoreViewModelBase` via `[ObservableProperty]`). Neither property notifies `HasMessage` when it changes. Additionally, `LoginViewModel` is not a `partial class`, so it cannot implement the source-generated `partial void On*Changed` hooks.

### COMMIT: `fix(login): notify HasMessage when StatusMessage/ErrorMessage change`

- [ ] **TODO 1.1 — Test: HasMessage notifies on ErrorMessage change**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Auth/LoginViewModelTests.cs` (new file or append)
  - **Modification**: Add test that sets `ErrorMessage` on a `LoginViewModel` instance, subscribes to `PropertyChanged`, and asserts that `HasMessage` notification fires AND `HasMessage` returns `true`.
  - **Verification**: `dotnet test --filter "LoginViewModel" tests/LYBT.Tests.Desktop/` — test MUST FAIL before implementation (red phase).

- [ ] **TODO 1.2 — Test: HasMessage notifies on StatusMessage change**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Auth/LoginViewModelTests.cs`
  - **Modification**: Add test that sets `StatusMessage`, asserts `HasMessage` PropertyChanged fires and `HasMessage == true`.
  - **Verification**: `dotnet test --filter "LoginViewModel"` — test MUST FAIL (red phase).

- [ ] **TODO 1.3 — Test: HasMessage returns false when both messages cleared**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Auth/LoginViewModelTests.cs`
  - **Modification**: Set `ErrorMessage = "err"`, then set `ErrorMessage = ""`, assert `HasMessage == false` and PropertyChanged fired both times.
  - **Verification**: `dotnet test --filter "LoginViewModel"` — test MUST FAIL (red phase).

- [ ] **TODO 1.4 — Make LoginViewModel partial**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
  - **Modification**: Change line 22 from `public class LoginViewModel : NavigableViewModelBase` to `public partial class LoginViewModel : NavigableViewModelBase`
  - **Verification**: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Auth/` — must compile.

- [ ] **TODO 1.5 — Implement On*Changed hooks to notify HasMessage**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
  - **Modification**: Add two methods anywhere in the class body:
    ```csharp
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasMessage));
    }

    partial void OnStatusMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasMessage));
    }
    ```
  - **Verification**: All 3 tests from TODOs 1.1–1.3 must now PASS (green phase). Run `dotnet test --filter "LoginViewModel" tests/LYBT.Tests.Desktop/`.

---

## Section 2: P1 — System-wide Computed Property Notification Audit

**Audit Result**: System-wide grep found 6 `Has*` computed properties across 4 files. **Only `LoginViewModel.HasMessage` is broken** (fixed in Section 1). The remaining properties use different patterns that are NOT affected:

| Property | File | Pattern | Status |
|----------|------|---------|--------|
| `LoginViewModel.HasMessage` | `LoginViewModel.cs:242` | Depends on `[ObservableProperty]` without notification | **BROKEN — Fixed in P0** |
| `MasterDetailViewModelBase.HasSelection` | `MasterDetailViewModelBase.cs:164` | Delegates to `SelectionService` via event forwarding | OK |
| `MasterDetailViewModelBase.HasUnsavedChanges` | `MasterDetailViewModelBase.cs:189` | Delegates to dirty-tracking service | OK |
| `MasterDetailViewModelBase.HasError` | `MasterDetailViewModelBase.cs:208` | Delegates to `ErrorHandler` service | OK |
| `SelectionService.HasSelection` | `SelectionService.cs:25` | Raises own PropertyChanged in setter | OK |
| `ErrorHandler.HasErrors` | `ErrorHandler.cs:28` | Raises own PropertyChanged in setter | OK |
| `SyncViewModel.HasDataToSync` | `SyncViewModel.cs:105` | Needs verification — check notification chain | **VERIFY** |

### COMMIT: `refactor(sync): verify HasDataToSync notification chain`

- [ ] **TODO 2.1 — Verify SyncViewModel.HasDataToSync notification**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs`
  - **Modification**: Read line 105 and surrounding code. If `HasDataToSync` depends on other properties without `[NotifyPropertyChangedFor]`, apply the same fix pattern as P0 (add `partial void On*Changed` hooks). If it uses its own setter with notification, mark as OK and skip.
  - **Verification**: If fix needed, write a test in `tests/LYBT.Tests.Desktop/PureLogic/Sync/SyncViewModelTests.cs` first (TDD). Otherwise, document as verified-OK in commit message.

---

## Section 3: P2 — EditControl Validation Completion

Each module below is **independent** and can be worked on in parallel. For each, the approach is:
1. Write validation unit tests for the DetailModel (TDD red phase)
2. Add missing DataAnnotations and switch `SetProperty` → `SetPropertyAndValidate`
3. Run tests (TDD green phase)

---

### Module A: FormulaDetailModel (CRITICAL — only 1 of ~6 fields validated)

#### COMMIT: `feat(formula): add missing validation to FormulaDetailModel`

- [ ] **TODO 3A.1 — Test: Effect exceeding max length triggers validation error**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Formula/FormulaDetailModelValidationTests.cs` (new)
  - **Modification**: Create test that sets `Effect` to a string exceeding `LongRemarkMaxLength`, calls `ValidateAll()`, asserts `HasErrors == true` and `Errors["Effect"]` is non-empty.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3A.2 — Test: Usage exceeding max length triggers validation error**
  - **File**: Same test file
  - **Modification**: Same pattern for `Usage` with `UsageMaxLength`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3A.3 — Test: Remark exceeding max length triggers validation error**
  - **File**: Same test file
  - **Modification**: Same pattern for `Remark` with `RemarkMaxLength`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3A.4 — Test: Empty herbs list triggers validation error** (if implementable via custom validation)
  - **File**: Same test file
  - **Modification**: Create test that leaves `Herbs` collection empty, calls `ValidateAll()`, asserts error. Note: DataAnnotations may not support collection-not-empty natively — may need custom `[MinLength(1)]` or `IValidatableObject`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3A.5 — Add DataAnnotations to FormulaDetailModel**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/FormulaDetailModel.cs`
  - **Modification**:
    - `Effect`: Add `[StringLength(LongRemarkMaxLength)]`, change `SetProperty` → `SetPropertyAndValidate`
    - `Usage`: Add `[StringLength(UsageMaxLength)]`, change `SetProperty` → `SetPropertyAndValidate`
    - `Remark`: Add `[StringLength(RemarkMaxLength)]`, change `SetProperty` → `SetPropertyAndValidate`
  - **Verification**: All tests from TODOs 3A.1–3A.4 must PASS (green phase).

---

### Module B: MedicalCaseDetailModel (CRITICAL — 0 fields validated)

#### COMMIT: `feat(medical-case): add validation to MedicalCaseDetailModel`

- [ ] **TODO 3B.1 — Test: Empty PatientId triggers validation error**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseDetailModelValidationTests.cs` (new)
  - **Modification**: Create test that leaves `PatientId` as `Guid.Empty`, calls `ValidateAll()`, asserts `HasErrors == true`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3B.2 — Test: Empty UserId triggers validation error**
  - **File**: Same test file
  - **Modification**: Same pattern for `UserId`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3B.3 — Test: Remark exceeding max length triggers validation error**
  - **File**: Same test file
  - **Modification**: Set `Remark` beyond `RemarkMaxLength`, assert error.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3B.4 — Test: Valid MedicalCase passes validation**
  - **File**: Same test file
  - **Modification**: Set all required fields to valid values, call `ValidateAll()`, assert `HasErrors == false`.
  - **Verification**: Test MUST FAIL (red phase — currently no validation exists, so it may pass vacuously; adjust test to also assert that validation actually ran).

- [ ] **TODO 3B.5 — Add DataAnnotations and switch to SetPropertyAndValidate**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs`
  - **Modification**:
    - `PatientId`: Add `[Required]` (or custom `[NotEmptyGuid]` attribute), switch to `SetPropertyAndValidate`
    - `UserId`: Add `[Required]` (or `[NotEmptyGuid]`), switch to `SetPropertyAndValidate`
    - `Remark`: Add `[StringLength(RemarkMaxLength)]`, switch to `SetPropertyAndValidate`
    - All other string properties with max lengths from server: add appropriate `[StringLength]` and switch to `SetPropertyAndValidate`
  - **Verification**: All tests from TODOs 3B.1–3B.4 must PASS (green phase).

---

### Module C: PatientDetailModel (MODERATE — 6 validated, missing key constraints)

#### COMMIT: `feat(patient): strengthen PatientDetailModel validation`

- [ ] **TODO 3C.1 — Test: Empty IdNumber triggers Required validation error**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Patients/PatientDetailModelValidationTests.cs` (new)
  - **Modification**: Set `IdNumber` to empty/null, assert validation error.
  - **Verification**: Test MUST FAIL (red phase — currently no `[Required]` on IdNumber).

- [ ] **TODO 3C.2 — Test: Invalid IdNumber format triggers RegularExpression error**
  - **File**: Same test file
  - **Modification**: Set `IdNumber` to "INVALID", assert regex validation error.
  - **Verification**: Test MUST FAIL (red phase — no `[RegularExpression]` exists).

- [ ] **TODO 3C.3 — Test: Empty Address triggers Required validation error**
  - **File**: Same test file
  - **Modification**: Set `Address` to empty/null, assert validation error.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3C.4 — Test: BirthDate in future triggers validation error**
  - **File**: Same test file
  - **Modification**: Set `BirthDate` to `DateTime.Today.AddDays(1)`, assert error. Note: May need custom validation attribute or `IValidatableObject`.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3C.5 — Add missing validation attributes to PatientDetailModel**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/PatientDetailModel.cs`
  - **Modification**:
    - `IdNumber`: Add `[Required]`, add `[RegularExpression(@"^\d{15}$|^\d{17}[\dXx]$")]` (Chinese ID card format, matching server-side validator)
    - `Address`: Add `[Required]`
    - `BirthDate`: Add custom validation (consider `IValidatableObject.Validate()` or custom attribute `[NotFutureDate]`)
    - `Gender`: Add `[EnumDataType(typeof(Gender))]` if applicable
  - **Verification**: All tests from TODOs 3C.1–3C.4 must PASS (green phase).

---

### Module D: UserDetailModel (MODERATE — 5 validated, missing key constraints)

#### COMMIT: `feat(user): strengthen UserDetailModel validation`

- [ ] **TODO 3D.1 — Test: Invalid UserName format triggers RegularExpression error**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Users/UserDetailModelValidationTests.cs` (new)
  - **Modification**: Set `UserName` to "user name with spaces", assert regex validation error.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3D.2 — Test: Invalid Role enum triggers validation error**
  - **File**: Same test file
  - **Modification**: Set `Role` to an invalid enum value, assert error.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3D.3 — Add missing validation attributes to UserDetailModel**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Models/UserDetailModel.cs`
  - **Modification**:
    - `UserName`: Add `[RegularExpression(@"^[a-zA-Z0-9_]+$")]` (matching server pattern)
    - `Role`: Add `[EnumDataType(typeof(UserRole))]` or `[Required]`
  - **Verification**: All tests from TODOs 3D.1–3D.2 must PASS (green phase).

---

### Module E: HerbDetailModel (LOW — 7 validated, missing StringLength on minor fields)

#### COMMIT: `feat(herb): add missing StringLength to HerbDetailModel`

- [ ] **TODO 3E.1 — Test: Category exceeding max length triggers validation error**
  - **File**: `tests/LYBT.Tests.Desktop/PureLogic/Herbs/HerbDetailModelValidationTests.cs` (new)
  - **Modification**: Set `Category` to a string exceeding max length, assert error.
  - **Verification**: Test MUST FAIL (red phase).

- [ ] **TODO 3E.2 — Test: Origin, Spec, PinYinCode exceeding max length triggers errors**
  - **File**: Same test file
  - **Modification**: Set each of `Origin`, `Spec`, `PinYinCode` beyond max length, assert errors.
  - **Verification**: Tests MUST FAIL (red phase).

- [ ] **TODO 3E.3 — Add missing StringLength attributes to HerbDetailModel**
  - **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Models/HerbDetailModel.cs`
  - **Modification**:
    - `Category`: Add `[StringLength(NameMaxLength)]`, ensure `SetPropertyAndValidate`
    - `Origin`: Add `[StringLength(NameMaxLength)]`, ensure `SetPropertyAndValidate`
    - `Spec`: Add `[StringLength(NameMaxLength)]`, ensure `SetPropertyAndValidate`
    - `PinYinCode`: Add `[StringLength(NameMaxLength)]`, ensure `SetPropertyAndValidate`
  - **Verification**: All tests from TODOs 3E.1–3E.2 must PASS (green phase).

---

## Section 4: Parallelization Map

```
Phase 1 (can all run in parallel):
  ├── P0 Login Fix (TODOs 1.1–1.5)          ← ~30 min, URGENT
  ├── P1 SyncViewModel verify (TODO 2.1)     ← ~15 min
  ├── P2-A FormulaDetailModel (TODOs 3A.*)   ← ~45 min
  ├── P2-B MedicalCaseDetailModel (TODOs 3B.*)← ~60 min (most work)
  ├── P2-C PatientDetailModel (TODOs 3C.*)   ← ~45 min
  ├── P2-D UserDetailModel (TODOs 3D.*)      ← ~30 min
  └── P2-E HerbDetailModel (TODOs 3E.*)      ← ~20 min

Phase 2 (after all Phase 1 complete):
  └── Integration verification: run full Desktop test suite
      dotnet test tests/LYBT.Tests.Desktop/
```

**Dependencies**: None between Phase 1 tasks — all modules touch different files. Each task is a self-contained commit.

**Recommended execution order** (if sequential, by priority × effort):
1. P0 Login Fix (urgent, small)
2. P2-B MedicalCaseDetailModel (critical, 0 validation)
3. P2-A FormulaDetailModel (critical, 1 validation)
4. P2-C PatientDetailModel (moderate, missing key fields)
5. P2-D UserDetailModel (moderate)
6. P1 SyncViewModel (verify only)
7. P2-E HerbDetailModel (low, minor gaps)

---

## Section 5: Verification Methods

### Per-Task Verification (TDD Cycle)

| Phase | Command | Expected Result |
|-------|---------|-----------------|
| Red (tests written, no impl) | `dotnet test --filter "TestClassName"` | FAIL — tests detect missing validation |
| Green (impl done) | `dotnet test --filter "TestClassName"` | PASS — all new tests pass |
| Refactor | `dotnet test tests/LYBT.Tests.Desktop/` | PASS — no regressions |

### Full Suite Verification (after all commits)

```bash
# Desktop tests (760 tests — includes new validation tests)
dotnet test tests/LYBT.Tests.Desktop/

# Architecture tests (76 tests — ensure no layer violations)
dotnet test tests/LYBT.Tests.Architecture/

# Build verification
dotnet build LYBTZYZS.sln
```

### Manual Smoke Test Checklist (post-implementation)

- [ ] Launch app → Login with wrong credentials → error message VISIBLE in red border
- [ ] Login with empty fields → error message appears
- [ ] Patient Edit → clear Name → validation error shown inline
- [ ] Patient Edit → enter invalid ID number → validation error shown
- [ ] Formula Edit → enter overly long Effect → validation error shown
- [ ] MedicalCase Edit → attempt save without patient → validation error shown
- [ ] Herb Edit → enter price = 0 → validation error shown
- [ ] User Edit → enter username with spaces → validation error shown

---

## Atomic Commit Summary

| # | Commit Message | Files Touched | TODOs |
|---|---------------|---------------|-------|
| 1 | `fix(login): notify HasMessage when StatusMessage/ErrorMessage change` | LoginViewModel.cs, LoginViewModelTests.cs | 1.1–1.5 |
| 2 | `refactor(sync): verify HasDataToSync notification chain` | SyncViewModel.cs, SyncViewModelTests.cs (if needed) | 2.1 |
| 3 | `feat(formula): add missing validation to FormulaDetailModel` | FormulaDetailModel.cs, FormulaDetailModelValidationTests.cs | 3A.1–3A.5 |
| 4 | `feat(medical-case): add validation to MedicalCaseDetailModel` | MedicalCaseDetailModel.cs, MedicalCaseDetailModelValidationTests.cs | 3B.1–3B.5 |
| 5 | `feat(patient): strengthen PatientDetailModel validation` | PatientDetailModel.cs, PatientDetailModelValidationTests.cs | 3C.1–3C.5 |
| 6 | `feat(user): strengthen UserDetailModel validation` | UserDetailModel.cs, UserDetailModelValidationTests.cs | 3D.1–3D.3 |
| 7 | `feat(herb): add missing StringLength to HerbDetailModel` | HerbDetailModel.cs, HerbDetailModelValidationTests.cs | 3E.1–3E.3 |

---

## Notes

- **P7 AutomationProperties**: Excluded per user confirmation (N/A).
- **Password validation** (UserDetailModel): Deferred — requires UI-specific PasswordBox handling that DataAnnotations alone cannot address. Track separately.
- **Custom validation attributes**: `[NotEmptyGuid]` and `[NotFutureDate]` may need to be created in `LYBT.Desktop.Models` if they don't already exist. Check `ValidatableModelBase` for existing custom attributes first.
- **Herbs list NotEmpty** (FormulaDetailModel): May require `IValidatableObject` implementation rather than DataAnnotations. Assess feasibility during implementation.
- **Server max length constants**: Verify exact values match between server `FluentValidation` rules and client `DataAnnotation` constants before implementation. The constants are likely shared via `LYBT.Shared.Models`.
