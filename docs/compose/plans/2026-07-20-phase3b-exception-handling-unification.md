# Phase 3b: Exception Handling Strategy Unification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task.

**Goal:** Unify exception handling strategy across all Client repositories - DeleteAsync and ToggleStatusAsync should throw instead of swallowing exceptions.

**Architecture:** Change DeleteAsync and ToggleStatusAsync from returning false/null on exception to rethrowing, consistent with "exceptions propagate to ViewModel" architecture standard.

**Tech Stack:** .NET 8, WPF/Prism

## Global Constraints

- All existing functionality must be preserved
- Build must succeed after each task
- All tests must pass after each task

---

## Task 1: Unify DeleteAsync to throw

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Registration/Repositories/RegistrationRepository.cs`

- [ ] **Step 1: Update HerbRepository.DeleteAsync to use ExecuteAsync**

Replace manual try/catch with ExecuteAsync helper.

- [ ] **Step 2: Update PatientRepository.DeleteAsync to use ExecuteAsync**

- [ ] **Step 3: Update UserRepository.DeleteAsync to use ExecuteAsync**

- [ ] **Step 4: Update FormulaRepository.DeleteAsync to use ExecuteAsync**

- [ ] **Step 5: Update MedicalCaseRepository.DeleteAsync to use ExecuteAsync**

- [ ] **Step 6: Update RegistrationRepository.CancelAsync to use ExecuteAsync**

- [ ] **Step 7: Update IRegistrationRepository.CancelAsync return type**

Change from `Task<bool>` to `Task` since we no longer return false.

- [ ] **Step 8: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): unify DeleteAsync to throw instead of returning false"
```

---

## Task 2: Unify ToggleStatusAsync to throw

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`

- [ ] **Step 1: Update HerbRepository.ToggleStatusAsync to use ExecuteAsync**

- [ ] **Step 2: Update UserRepository.ToggleStatusAsync to use ExecuteAsync**

- [ ] **Step 3: Update FormulaRepository.ToggleStatusAsync to use ExecuteAsync**

- [ ] **Step 4: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): unify ToggleStatusAsync to throw instead of returning null"
```

---

## Task 3: Update callers to handle new exception behavior

**Covers:** [S4]

**Files:**
- Check ViewModels and Services that call DeleteAsync/ToggleStatusAsync

- [ ] **Step 1: Find all callers of DeleteAsync**

Search for `DeleteAsync` in ViewModels and Services.

- [ ] **Step 2: Verify callers already handle exceptions**

Most callers should already use try/catch or ExecuteWithErrorHandlingAsync.

- [ ] **Step 3: Fix any callers that don't handle exceptions**

- [ ] **Step 4: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 5: Run tests**

```bash
$env:DOTNET_ROOT = "C:\Program Files\dotnet"; dotnet test tests/ --verbosity quiet
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): update callers to handle DeleteAsync/ToggleStatusAsync exceptions"
```

---

## Summary

**Total Tasks:** 3
**Estimated Time:** 1-2 days
