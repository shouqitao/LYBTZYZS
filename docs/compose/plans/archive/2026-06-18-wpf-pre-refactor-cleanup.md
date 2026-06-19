# WPF Pre-Refactor Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove 899 OpenSpec comment markers and fix 220 compilation warnings across the WPF Desktop + Server codebase before refactoring.

**Architecture:** Pure mechanical cleanup — no functional changes. Three parallel workstreams: (1) OpenSpec comment removal, (2) dead/stale comment cleanup, (3) compilation warning fixes. Each is independent and can be parallelized via subagents.

**Tech Stack:** C# / .NET 8 / WPF / Prism / xUnit

---

### Task 1: Remove OpenSpec comments from C# files

**Covers:** [S1]

**Files:** ~250 C# files across `src/Client/Desktop/` and `src/Server/`

- [ ] **Step 1: Find all files with OpenSpec markers**

Run: `rg -l "OpenSpec:" src/Client/Desktop src/Server -t cs`

- [ ] **Step 2: Remove standalone OpenSpec comment lines**

For each file, remove lines that are ONLY an OpenSpec marker:
- `/// OpenSpec: xxx - description` → delete entire line
- `// OpenSpec: xxx - description` → delete entire line  
- `// [已移除] xxx (OpenSpec: xxx)` → delete entire line

Keep lines where OpenSpec is PART of a meaningful comment (e.g., a region tag that also contains OpenSpec). Only remove the OpenSpec portion, not the entire line.

- [ ] **Step 3: Remove inline OpenSpec annotations**

For lines where OpenSpec appears as a suffix to existing comments:
- `/// <summary>description (OpenSpec: xxx)</summary>` → `/// <summary>description</summary>`
- `// existing code // OpenSpec: xxx` → `// existing code`

- [ ] **Step 4: Clean up empty regions left behind**

After removing OpenSpec-only lines, check for empty `#region` / `#endregion` blocks. Remove any that became empty.

- [ ] **Step 5: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors, same warning count or fewer

- [ ] **Step 6: Commit**

```bash
git add -A src/Client/Desktop src/Server
git commit -m "chore: remove OpenSpec comment markers from C# files"
```

### Task 2: Remove OpenSpec references from XAML files

**Covers:** [S1]

**Files:** ~20 XAML files across `src/Client/Desktop/`

- [ ] **Step 1: Find all XAML files with OpenSpec markers**

Run: `rg -l "OpenSpec:" src/Client/Desktop -t xml`

- [ ] **Step 2: Remove OpenSpec comments from XAML**

XAML comments use `<!-- -->` syntax. Remove entire comment blocks that are only OpenSpec markers:
- `<!-- OpenSpec: xxx -->` → delete
- `<!-- existing comment (OpenSpec: xxx) -->` → `<!-- existing comment -->`

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add -A src/Client/Desktop
git commit -m "chore: remove OpenSpec comment markers from XAML files"
```

### Task 3: Fix CS1998 async warnings (async method lacks await)

**Covers:** [S3]

**Files:**
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/MedicalCaseStartCoordinator.cs` (2 occurrences)
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs` (1 occurrence)
- Any other files with CS1998

- [ ] **Step 1: Find all CS1998 warnings**

Run: `dotnet build LYBTZYZS.sln --no-restore 2>&1 | Select-String "CS1998"`

- [ ] **Step 2: For each async method missing await, add `await Task.CompletedTask` at the end**

Before:
```csharp
private async Task SomeMethod()
{
    // body with no await
}
```

After:
```csharp
private async Task SomeMethod()
{
    // body with no await
    await Task.CompletedTask;
}
```

**Exception:** If the method is intentionally synchronous and just implements an interface requiring `Task`, consider removing `async` and returning `Task.CompletedTask` directly.

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: CS1998 count reduced to 0

- [ ] **Step 4: Commit**

```bash
git commit -m "fix: resolve CS1998 async warnings by adding await Task.CompletedTask"
```

### Task 4: Fix CS8601 nullable reference warnings

**Covers:** [S3]

**Files:**
- `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs` (2 occurrences at lines 103, 105)
- `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` (2 occurrences at lines 79, 589)
- Any other files with CS8601

- [ ] **Step 1: Find all CS8601 warnings**

Run: `dotnet build LYBTZYZS.sln --no-restore 2>&1 | Select-String "CS8601"`

- [ ] **Step 2: Fix each nullable warning**

For assignment warnings, use null-forgiving operator or null coalescing:

Before:
```csharp
SomeProperty = someVariable;  // CS8601: possible null reference
```

After (preferred — null coalescing):
```csharp
SomeProperty = someVariable ?? string.Empty;
```

Or (if type is non-nullable reference and value is known non-null at this point):
```csharp
SomeProperty = someVariable!;  // null-forgiving
```

**Rule:** Prefer `?? defaultValue` over `!` null-forgiving. Only use `!` when the code logically guarantees non-null but the compiler can't prove it.

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: CS8601 count reduced to 0

- [ ] **Step 4: Commit**

```bash
git commit -m "fix: resolve CS8601 nullable reference warnings"
```

### Task 5: Fix xUnit1013 warnings (public method missing [Fact])

**Covers:** [S3]

**Files:**
- `tests/LYBT.Tests.Desktop/Integration/Modules/PatientTests.cs` (2 methods)
- `tests/LYBT.Tests.Desktop/Integration/Modules/FormulaTests.cs` (1 method)
- `tests/LYBT.Tests.Desktop/Integration/Modules/HerbTests.cs` (2 methods)
- `tests/LYBT.Tests.Desktop/Integration/Modules/UserTests.cs` (2 methods)

- [ ] **Step 1: Find all xUnit1013 warnings**

Run: `dotnet build LYBTZYZS.sln --no-restore 2>&1 | Select-String "xUnit1013"`

- [ ] **Step 2: For each public test method, decide: add [Fact] or make private**

If the method is a lifecycle test (Create/Update/Delete/Restore) that should be run by xUnit:
```csharp
[Fact]
public async Task DeleteAndRestore_Patient_CompletesSuccessfully()
```

If the method is a helper used by other tests, make it private:
```csharp
private async Task DeleteAndRestore_Patient_CompletesSuccessfully()
```

**Check:** Read each method to determine if it has `[Fact]`, `[Theory]`, or neither. If it's a standalone test that should run → add `[Fact]`. If it's a helper → make private.

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: xUnit1013 count reduced to 0

- [ ] **Step 4: Commit**

```bash
git commit -m "fix: resolve xUnit1013 warnings by adding [Fact] or reducing visibility"
```

### Task 6: Full build + test verification

**Covers:** [S2], [S3]

- [ ] **Step 1: Clean rebuild**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, warnings reduced from 220

- [ ] **Step 2: Run architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: same pass count as before (78 pass, 3 pre-existing fail)

- [ ] **Step 3: Run server tests**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: all pass (requires SQL Server)

- [ ] **Step 4: Run desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: all pass

- [ ] **Step 5: Final commit if any incremental fixes needed**

```bash
git add -A
git commit -m "chore: WPF pre-refactor cleanup complete"
```
