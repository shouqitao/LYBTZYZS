# Architecture Cleanup Phase 1 — Critical Correctness Fixes

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 5 CRITICAL correctness issues (password hash dual-track, Users table name drift, Local EnsureCreated, PrintingModule not registered, Desktop→Entities cross-layer reference) and 1 HIGH fail-open environment detection.

**Architecture:** Surgical fixes across Server (password handlers, DB init), Desktop (PrintingModule registration, Entities reference), and Shared (Entities project relocation). Each fix is independently verifiable. Entities project moves from `src/Server/Core/` to `src/Shared/` to eliminate cross-layer violation.

**Tech Stack:** .NET 8, ASP.NET Core Identity (UserManager), EF Core (MigrateAsync), Prism (ModuleCatalog), NetArchTest

## Global Constraints

- Commit messages: English, format `fix/module: description`
- Code style: Chinese comments/docs, English identifiers
- No new abstractions — minimal surgical fixes only
- `dotnet build LYBTZYZS.sln` must pass after each task
- `dotnet test tests/LYBT.Tests.Architecture/` must pass after each task
- Do NOT modify files outside the listed scope per task

---

## File Structure

### Task 1: Password Hash Unification
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangePasswordCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ResetPasswordCommandHandler.cs`

### Task 2: Users Table Name Fix
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/UserConfiguration.cs` (line 16)
- OR create new migration (depends on DB state verification)

### Task 3: Local EnsureCreated → MigrateAsync
- Modify: `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` (line 127)

### Task 4: PrintingModule Registration
- Modify: `src/Client/Desktop/Shell/App.xaml.cs` (line ~155, after ReportsModule)
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` (line 155, delete comment)

### Task 5: Entities Project Relocation + Architecture Test Fix
- Move: `src/Server/Core/LYBT.Entities/` → `src/Shared/LYBT.Entities/`
- Modify: `LYBTZYZS.sln` (project path)
- Modify: 11 `.csproj` files (ProjectReference paths)
- Modify: `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs` (line 35: `NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.Entities")` → `NotHaveDependencyOn("LYBT.Infrastructure")`)

### Task 6: Environment Fail-Open Fix
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs` (line 209-211)

### Task 7: Full Verification
- No file changes — verification only

---

### Task 1: Password Hash Unification (C1)

**Covers:** [S2.1]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangePasswordCommandHandler.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ResetPasswordCommandHandler.cs`

**Interfaces:**
- Consumes: `IUserRepository` (existing), `UserManager<ApplicationUser>` (new injection)
- Produces: Same command handler interface, no API contract change

- [ ] **Step 1: Read both handler files to confirm current code**

Read `ChangePasswordCommandHandler.cs` and `ResetPasswordCommandHandler.cs` to confirm they match the spec (direct BCrypt HashPassword calls at lines 32 and 25 respectively).

- [ ] **Step 2: Modify ChangePasswordCommandHandler**

Replace the entire file content:

```csharp
using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.Id != request.CurrentUserId)
            return Result.Failure(ErrorCode.Forbidden, "只能修改自己的密码");

        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result.Failure(ErrorCode.UserNotFound, "用户不存在");

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            var message = error?.Description ?? "密码修改失败";
            return Result.Failure(ErrorCode.InvalidPassword, message);
        }

        return Result.Success();
    }
}
```

- [ ] **Step 3: Modify ResetPasswordCommandHandler**

Replace the entire file content:

```csharp
using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Utilities.Security;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResult>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result<ResetPasswordResult>.Failure(ErrorCode.UserNotFound, "用户不存在");

        var newPassword = PasswordHelper.GenerateSecurePassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            var message = error?.Description ?? "密码重置失败";
            return Result<ResetPasswordResult>.Failure(ErrorCode.InvalidPassword, message);
        }

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(newPassword));
    }
}
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Verify no remaining direct BCrypt calls in handlers**

Run: `grep -n "PasswordHelper\.\(HashPassword\|VerifyPassword\)" src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangePasswordCommandHandler.cs src/Server/Modules/LYBT.Module.Users/Application/Commands/ResetPasswordCommandHandler.cs`
Expected: No output (zero matches)

- [ ] **Step 6: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangePasswordCommandHandler.cs src/Server/Modules/LYBT.Module.Users/Application/Commands/ResetPasswordCommandHandler.cs
git commit -m "fix(users): unify password hashing through UserManager to prevent lockout after password change"
```

---

### Task 2: Users Table Name Fix (C2)

**Covers:** [S2.2]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/UserConfiguration.cs` (line 16)
- OR create new migration (depends on verification result)

**Interfaces:**
- Consumes: N/A
- Produces: N/A (database schema alignment only)

- [ ] **Step 1: Verify current database table name**

Run: `dotnet ef migrations list --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`

Then check if the snapshot has "Users" or "AspNetUsers" as the current model state. Read the latest migration Designer.cs to see what table name the model currently expects.

- [ ] **Step 2: Read UserConfiguration.cs to confirm current mapping**

Read `src/Server/Core/LYBT.Infrastructure/Data/Configurations/UserConfiguration.cs` — confirm line 16 has `builder.ToTable("Users")`.

- [ ] **Step 3: Decide and implement fix**

**If model maps "Users" but DB has "AspNetUsers":**
- Change `UserConfiguration.cs:16` from `ToTable("Users")` to `ToTable("AspNetUsers")` — this aligns model to existing data without data migration.

**If both model and DB use "Users":**
- No change needed — verify with `dotnet ef migrations has-pending-model-changes` that no diff exists.

Run the appropriate fix based on Step 1 findings.

- [ ] **Step 4: Verify no pending model changes**

Run: `dotnet ef migrations has-pending-model-changes --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`
Expected: "No changes were detected"

- [ ] **Step 5: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Data/Configurations/UserConfiguration.cs
git commit -m "fix(infra): align UserConfiguration table name to match migration chain"
```

(If no change was needed, skip commit.)

---

### Task 3: Local EnsureCreated → MigrateAsync (C3)

**Covers:** [S2.3]

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` (line 127)

**Interfaces:**
- Consumes: N/A
- Produces: N/A

- [ ] **Step 1: Read LocalWebApiProgram.cs to confirm current code**

Read `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` — confirm line 127 has `await dbContext.Database.EnsureCreatedAsync();`.

- [ ] **Step 2: Change EnsureCreatedAsync to MigrateAsync**

In `LocalWebApiProgram.cs`, line 127, replace:
```csharp
await dbContext.Database.EnsureCreatedAsync();
```
with:
```csharp
await dbContext.Database.MigrateAsync();
```

- [ ] **Step 3: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs
git commit -m "fix(localwebapi): switch from EnsureCreatedAsync to MigrateAsync for proper migration support"
```

---

### Task 4: PrintingModule Registration (C4)

**Covers:** [S2.4]

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml.cs` (after line 156)
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` (line 155, delete comment)

**Interfaces:**
- Consumes: `LYBT.Desktop.Printing.PrintingModule` (existing Prism module)
- Produces: `IPrintService<PrescriptionPrintModel>` registered in DI via PrintingModule

- [ ] **Step 1: Read App.xaml.cs to find insertion point**

Read `src/Client/Desktop/Shell/App.xaml.cs` lines 130-160 — confirm PrintingModule is NOT in the catalog. The insertion point is after line 156 (`moduleCatalog.AddModule<ReportsModule>(InitializationMode.OnDemand);`).

- [ ] **Step 2: Add PrintingModule to ConfigureModuleCatalog**

In `App.xaml.cs`, after the `ReportsModule` line (line 156), add:

```csharp
        // 打印模块
        moduleCatalog.AddModule<PrintingModule>(InitializationMode.OnDemand);
```

Also ensure the using directive `using LYBT.Desktop.Printing;` is present at the top of the file. Read the top of the file to check.

- [ ] **Step 3: Delete misleading comment in ServiceCollectionExtensions**

In `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`, delete line 155:
```csharp
            // IPrintService<T> 由 PrintingModule 注册，此处不重复
```

- [ ] **Step 4: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/App.xaml.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "fix(printing): register PrintingModule in ModuleCatalog to enable prescription printing"
```

---

### Task 5: Entities Project Relocation + Architecture Test Fix (C5)

**Covers:** [S2.5]

**Files:**
- Move: `src/Server/Core/LYBT.Entities/` → `src/Shared/LYBT.Entities/`
- Modify: `LYBTZYZS.sln` (project path update)
- Modify: 11 `.csproj` files (ProjectReference path updates)
- Modify: `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs` (line 35)

**Interfaces:**
- Consumes: N/A
- Produces: Entities accessible from Shared tier (Server/Desktop/Shared all can reference)

- [ ] **Step 1: Move the Entities project directory**

```bash
# Windows: use robocopy or xcopy to move
robocopy "src\Server\Core\LYBT.Entities" "src\Shared\LYBT.Entities" /E /MOVE
```

Verify the move: `dir src\Shared\LYBT.Entities\LYBT.Entities.csproj` should exist.
Verify old location removed: `dir src\Server\Core\LYBT.Entities` should fail.

- [ ] **Step 2: Update LYBTZYZS.sln project path**

Open `LYBTZYZS.sln` and find the line referencing `src\Server\Core\LYBT.Entities\LYBT.Entities.csproj`. Replace with `src\Shared\LYBT.Entities\LYBT.Entities.csproj`.

Run: `dotnet sln LYBTZYZS.sln list | Select-String "Entities"` to verify the path updated.

- [ ] **Step 3: Update Entities.csproj internal reference**

In `src/Shared/LYBT.Entities/LYBT.Entities.csproj`, the `ProjectReference` to `LYBT.Shared.Models` needs path update. Current path is `..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj` — after move, it should be `..\LYBT.Shared.Models\LYBT.Shared.Models.csproj`.

Also update the `CopyDocumentationFiles` target path if needed.

- [ ] **Step 4: Update all 11 csproj ProjectReference paths**

For each project that references `LYBT.Entities`, update the `ProjectReference Include` path. The old path pattern is `..\..\Server\Core\LYBT.Entities\LYBT.Entities.csproj` (relative from each project). The new path is `..\..\Shared\LYBT.Entities\LYBT.Entities.csproj` (for Server projects) or adjust relative paths accordingly.

**Server Modules** (8 projects at `src/Server/Modules/LYBT.Module.*/`):
Old: `..\..\Server\Core\LYBT.Entities\LYBT.Entities.csproj`
New: `..\..\Shared\LYBT.Entities\LYBT.Entities.csproj`

**Server Infrastructure** (`src/Server/Core/LYBT.Infrastructure/`):
Old: `..\LYBT.Entities\LYBT.Entities.csproj`
New: `..\..\Shared\LYBT.Entities\LYBT.Entities.csproj`

**Desktop Infrastructure** (`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/`):
Old: `..\..\Server\Core\LYBT.Entities\LYBT.Entities.csproj`
New: `..\..\..\Shared\LYBT.Entities\LYBT.Entities.csproj`

**Desktop LocalWebAPI** (`src/Client/Desktop/LocalWebAPI/`):
Old: `..\..\Server\Core\LYBT.Entities\LYBT.Entities.csproj`
New: `..\..\..\Shared\LYBT.Entities\LYBT.Entities.csproj`

Run `grep -rn "LYBT.Entities" --include="*.csproj" src/` to find all references and verify none remain pointing to old path.

- [ ] **Step 5: Fix architecture test**

In `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs`, line 35, the current test is:
```csharp
.NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.Entities")
```

This uses `NotHaveDependencyOnAll` — it only fails if Desktop depends on BOTH `LYBT.Infrastructure` AND `LYBT.Entities` simultaneously. With Entities now in Shared, referencing it is合规. The test should only guard against `LYBT.Infrastructure` (Server tier).

Change line 35 to:
```csharp
.NotHaveDependencyOn("LYBT.Infrastructure")
```

Also update the XML comment (line 28) from "Desktop层不得依赖Server层" to be more specific about what the test guards.

- [ ] **Step 6: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 7: Run architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass (including updated DesktopLayerArchTests)

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(shared): relocate LYBT.Entities to Shared tier to eliminate cross-layer violation"
```

---

### Task 6: Environment Fail-Open Fix (H1)

**Covers:** [S2.6]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs` (lines 207-212)

**Interfaces:**
- Consumes: N/A
- Produces: N/A

- [ ] **Step 1: Read IsDevelopment method**

Read `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs` lines 202-213 — confirm the fail-open logic.

- [ ] **Step 2: Change default from Development to Production**

In `DatabaseInitializationService.cs`, replace the `IsDevelopment()` method (lines 207-213):

```csharp
private static bool IsDevelopment()
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    if (string.IsNullOrEmpty(env))
        return true; // 未设置环境变量时视为开发环境
    return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
}
```

with:

```csharp
private static bool IsDevelopment()
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
}
```

Also update the XML doc comment (lines 202-206) to reflect the new behavior: missing env var = Production (conservative default).

- [ ] **Step 3: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs
git commit -m "fix(infra): default to Production when ASPNETCORE_ENVIRONMENT is missing (fail-closed)"
```

---

### Task 7: Full Verification

**Covers:** [S4] T7

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 3: Server tests (quick smoke)**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "ChangePassword|ResetPassword"`
Expected: All tests pass

- [ ] **Step 4: Verify no Desktop→Server direct references remain**

Run: `grep -rn "Server/Core" --include="*.csproj" src/Client/`
Expected: No output (no Desktop project references Server directly)

- [ ] **Step 5: Final status**

All 6 fixes implemented and verified. Solution builds clean, architecture tests pass, no cross-layer violations remain.
