# Architecture Cleanup Phase 2 — Structural Convergence

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate 5 module DbContexts into AppDbContext, align Herbs/Formulas auth policies, merge 6 pairs of duplicate Controllers, and fix MedicalCase dual-write path.

**Architecture:** Remove module DbContexts (Auth/Herbs/Formula/Users/Reports) — all Repositories switch to IDbContextAccessor → AppDbContext. Align WebAPI Herbs/Formulas authorization to DoctorOrReceptionist. Create shared Controller base class for 6 duplicate pairs. Fix CreateMedicalCaseCommandHandler to delegate to Service layer.

**Tech Stack:** .NET 8, ASP.NET Core Identity, EF Core, MediatR, Prism

## Global Constraints

- Commit messages: English, format `refactor/module: description`
- Code style: Chinese comments/docs, English identifiers
- `dotnet build LYBTZYZS.sln` must pass after each task
- `dotnet test tests/LYBT.Tests.Architecture/` must pass after each task
- Do NOT modify files outside the listed scope per task
- IDbContextAccessor already exists at `src/Server/Core/LYBT.Infrastructure/Interfaces/IDbContextAccessor.cs`

---

## File Structure

### Task 1: Remove Module DbContexts
- Delete: `Module.Auth/Infrastructure/AuthDbContext.cs` + `Migrations/` directory
- Delete: `Module.Herbs/Infrastructure/HerbsDbContext.cs`
- Delete: `Module.Formula/Infrastructure/FormulaDbContext.cs`
- Delete: `Module.Users/Infrastructure/UsersDbContext.cs`
- Delete: `Module.Reports/Infrastructure/ReportsDbContext.cs`
- Modify: 6 Repository files (inject IDbContextAccessor instead of module DbContext)
- Modify: 5 Module.cs files (remove AddDbContext registrations)

### Task 2: Align Herbs/Formulas Auth Policy
- Modify: `WebAPI/Controllers/HerbsController.cs` (line 24: AdminOrSuperAdmin → DoctorOrReceptionist)
- Modify: `WebAPI/Controllers/FormulasController.cs` (line 23: AdminOrSuperAdmin → DoctorOrReceptionist)

### Task 3: Controller Shared Base Class
- Create or modify: shared base class for 6 duplicate Controller pairs
- Modify: WebAPI and LocalWebAPI Controllers for Patients, Registrations, Reports, Configuration, Health, MedicalCases(query)

### Task 4: Fix CreateMedicalCaseCommandHandler
- Modify: `Module.MedicalCase/Application/Commands/CreateMedicalCaseCommandHandler.cs`

### Task 5: Full Verification
- No file changes

---

### Task 1: Remove Module DbContexts (H1)

**Covers:** [S2.1]

**Files:**
- Delete: `src/Server/Modules/LYBT.Module.Auth/Infrastructure/AuthDbContext.cs`
- Delete: `src/Server/Modules/LYBT.Module.Auth/Migrations/` (entire directory)
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Infrastructure/HerbsDbContext.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Infrastructure/FormulaDbContext.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Infrastructure/UsersDbContext.cs`
- Delete: `src/Server/Modules/LYBT.Module.Reports/Infrastructure/ReportsDbContext.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Infrastructure/AuthSessionRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Infrastructure/RefreshTokenRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Infrastructure/SecurityAuditRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Herbs/Infrastructure/HerbRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/Infrastructure/FormulaRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Infrastructure/UserRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/AuthModule.cs` (line 25)
- Modify: `src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs` (line 28)
- Modify: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs` (line 39)
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Reports/ReportsModule.cs` (line 24)

**Interfaces:**
- Consumes: `IDbContextAccessor` (existing in LYBT.Infrastructure)
- Produces: All Repositories now use AppDbContext via IDbContextAccessor

- [ ] **Step 1: Read all 6 Repository files to confirm current DbContext injection**

Read each Repository to understand the current injection pattern and all `_context` usage sites:
- `AuthSessionRepository.cs` — injects `AuthDbContext`
- `RefreshTokenRepository.cs` — injects `AuthDbContext`
- `SecurityAuditRepository.cs` — injects `AuthDbContext`
- `HerbRepository.cs` — injects `HerbsDbContext`
- `FormulaRepository.cs` — injects `FormulaDbContext`
- `UserRepository.cs` — injects `UsersDbContext`

- [ ] **Step 2: Modify AuthSessionRepository — AuthDbContext → IDbContextAccessor**

In `AuthSessionRepository.cs`:
- Replace `using LYBT.Module.Auth.Infrastructure;` with `using LYBT.Infrastructure.Interfaces;` and `using LYBT.Infrastructure.Data;`
- Change field: `private readonly AuthDbContext _context;` → `private readonly AppDbContext _context;`
- Change constructor: `AuthSessionRepository(AuthDbContext context)` → `AuthSessionRepository(IDbContextAccessor accessor)` with `_context = accessor.Context;`

- [ ] **Step 3: Modify RefreshTokenRepository — same pattern**

Same change as Step 2 for RefreshTokenRepository.

- [ ] **Step 4: Modify SecurityAuditRepository — same pattern**

Same change as Step 2 for SecurityAuditRepository.

- [ ] **Step 5: Modify HerbRepository — HerbsDbContext → IDbContextAccessor**

In `HerbRepository.cs`:
- Replace `using LYBT.Module.Herbs.Infrastructure;` with `using LYBT.Infrastructure.Interfaces;` and `using LYBT.Infrastructure.Data;`
- Change field: `private readonly HerbsDbContext _context;` → `private readonly AppDbContext _context;`
- Change constructor: `HerbRepository(HerbsDbContext context)` → `HerbRepository(IDbContextAccessor accessor)` with `_context = accessor.Context;`

- [ ] **Step 6: Modify FormulaRepository — FormulaDbContext → IDbContextAccessor**

Same pattern as Step 5 for FormulaRepository.

- [ ] **Step 7: Modify UserRepository — UsersDbContext → IDbContextAccessor**

Same pattern as Step 5 for UserRepository. Note: UserRepository uses `_context.Users` — verify that AppDbContext also has `DbSet<ApplicationUser> Users` (it does, via IdentityDbContext).

- [ ] **Step 8: Modify AuthModule.cs — remove AddDbContext<AuthDbContext>**

In `AuthModule.cs`, remove the `services.AddDbContext<AuthDbContext>(...)` block. Ensure IDbContextAccessor is registered (check if it's already registered in Infrastructure DI — if not, add it).

- [ ] **Step 9: Modify HerbsModule.cs — remove AddDbContext<HerbsDbContext>**

Same pattern as Step 8.

- [ ] **Step 10: Modify FormulaModule.cs — remove AddDbContext<FormulaDbContext>**

Same pattern as Step 8.

- [ ] **Step 11: Modify UsersModule.cs — remove AddDbContext<UsersDbContext>**

Same pattern as Step 8.

- [ ] **Step 12: Modify ReportsModule.cs — remove AddDbContext<ReportsDbContext>**

Same pattern as Step 8. ReportsDbContext is an empty shell — just remove the registration.

- [ ] **Step 13: Delete 5 module DbContext files**

```bash
rm src/Server/Modules/LYBT.Module.Auth/Infrastructure/AuthDbContext.cs
rm -rf src/Server/Modules/LYBT.Module.Auth/Migrations/
rm src/Server/Modules/LYBT.Module.Herbs/Infrastructure/HerbsDbContext.cs
rm src/Server/Modules/LYBT.Module.Formula/Infrastructure/FormulaDbContext.cs
rm src/Server/Modules/LYBT.Module.Users/Infrastructure/UsersDbContext.cs
rm src/Server/Modules/LYBT.Module.Reports/Infrastructure/ReportsDbContext.cs
```

- [ ] **Step 14: Verify IDbContextAccessor registration**

Check `src/Server/Core/LYBT.Infrastructure/DependencyInjection/RepositoryServiceCollectionExtensions.cs` or similar — confirm `services.AddScoped<IDbContextAccessor, DbContextAccessor>()` is registered. If not, add it.

- [ ] **Step 15: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 16: Run tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "Users|Herbs|Formula|Auth"`
Expected: All tests pass

- [ ] **Step 17: Commit**

```bash
git add -A
git commit -m "refactor(infra): remove 5 module DbContexts, unify to AppDbContext via IDbContextAccessor"
```

---

### Task 2: Align Herbs/Formulas Auth Policy (H2 Step 1)

**Covers:** [S2.2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs` (line 24)
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs` (line 23)

**Interfaces:**
- Consumes: N/A
- Produces: N/A (attribute change only)

- [ ] **Step 1: Read both Controller files to confirm current auth attributes**

Read HerbsController.cs and FormulasController.cs to find the `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]` attributes.

- [ ] **Step 2: Change HerbsController auth policy**

In `HerbsController.cs`, change:
```csharp
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
```
to:
```csharp
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
```

Apply to ALL methods that have this attribute (not just the class-level one — check each method).

- [ ] **Step 3: Change FormulasController auth policy**

Same change as Step 2 for FormulasController.

- [ ] **Step 4: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs
git commit -m "fix(auth): align Herbs/Formulas WebAPI auth policy to DoctorOrReceptionist matching LocalWebAPI"
```

---

### Task 3: Controller Shared Base Class (H2 Step 2)

**Covers:** [S2.2]

**Files:**
- Create/Modify: shared Controller base class
- Modify: 6 WebAPI Controller pairs
- Modify: 6 LocalWebAPI Controller pairs

**Interfaces:**
- Consumes: BaseApiController (existing)
- Produces: SharedController<TService> or similar base class

**Note:** This task requires reading the actual Controller code to design the base class. The plan provides the approach — the implementer reads the code and designs the optimal base class structure.

- [ ] **Step 1: Read 3 pairs of duplicate Controllers to understand shared logic**

Read these pairs to identify the common code pattern:
- `WebAPI/Controllers/PatientsController.cs` vs `LocalWebAPI/Controllers/PatientsController.cs`
- `WebAPI/Controllers/RegistrationsController.cs` vs `LocalWebAPI/Controllers/RegistrationsController.cs`
- `WebAPI/Controllers/ReportsController.cs` vs `LocalWebAPI/Controllers/ReportsController.cs`

Identify: common method signatures, shared service injection, identical business logic, differences (auth attributes, rate limiting, extra endpoints).

- [ ] **Step 2: Design shared base class**

Based on Step 1 findings, design a base class that:
- Contains all shared CRUD logic (injected via generic service type parameter)
- Inherits from `BaseApiController`
- Provides common helper methods (GetOperator, ValidatePagination, Success, etc.)
- Leaves auth attributes, rate limiting, and extra endpoints to subclasses

- [ ] **Step 3: Implement shared base class**

Create the base class in `src/Server/Core/LYBT.Infrastructure/Web/` or similar shared location.

- [ ] **Step 4: Refactor 6 WebAPI Controllers to inherit shared base**

For each of the 6 pairs, refactor the WebAPI Controller to inherit the shared base class, keeping only auth attributes and route decorations.

- [ ] **Step 5: Refactor 6 LocalWebAPI Controllers to inherit shared base**

Same as Step 4 for LocalWebAPI Controllers.

- [ ] **Step 6: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 7: Run tests**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(webapi): extract shared Controller base class for 6 duplicate Controller pairs"
```

---

### Task 4: Fix CreateMedicalCaseCommandHandler (H3)

**Covers:** [S2.3]

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Commands/CreateMedicalCaseCommandHandler.cs`

**Interfaces:**
- Consumes: `IMedicalCaseCommandService` (existing)
- Produces: Delegated creation via Service layer

- [ ] **Step 1: Read CreateMedicalCaseCommandHandler.cs**

Read the full file (118 lines) to understand the current direct-entity-creation logic.

- [ ] **Step 2: Read MedicalCaseCommandService to find the delegation target**

Read `MedicalCaseCommandService.cs` to find `SaveAsync` or `CreateFromInputDtoAsync` method that handles creation with BR-001 validation.

- [ ] **Step 3: Refactor Handler to delegate to Service**

Replace the direct entity creation (lines 32-105) with a call to the Service layer. The Handler should:
1. Still resolve doctor/patient basic info for validation
2. Delegate the actual entity creation + persistence to `IMedicalCaseCommandService`
3. Return the mapped DTO

- [ ] **Step 4: Build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Run MedicalCase tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "MedicalCase"`
Expected: All tests pass (BR-001 validation should now be enforced)

- [ ] **Step 6: Commit**

```bash
git add src/Server/Modules/LYBT.Module.MedicalCase/Application/Commands/CreateMedicalCaseCommandHandler.cs
git commit -m "fix(medicalcase): delegate CreateMedicalCaseCommandHandler to Service layer, restore BR-001 validation"
```

---

### Task 5: Full Verification

**Covers:** [S4] T5

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 3: Server tests (full)**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 4: Verify no module DbContext references remain**

Run: `grep -rn "AuthDbContext\|HerbsDbContext\|FormulaDbContext\|UsersDbContext\|ReportsDbContext" --include="*.cs" src/Server/`
Expected: No output (all module DbContext references removed)

- [ ] **Step 5: Final status**

All 4 fixes implemented and verified. Solution builds clean, all tests pass.
