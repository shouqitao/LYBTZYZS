# Remove Trivial MediatR Handlers — 4 CRUD Modules

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove trivial MediatR Command/Query/Handler files from Herbs, Formula, Patients, Users modules. Controllers call services directly for simple CRUD; complex operations (domain events, cross-module, Identity) keep MediatR.

**Architecture:** Each module gets a new `I<Module>Service` + `<Module>Service` that encapsulates repository calls + entity mapping. Controllers inject both `ISender` (for complex ops) and `I<Module>Service` (for trivial ops) — the MedicalCase hybrid pattern. BaseCrudController is unchanged.

**Tech Stack:** .NET 8, ASP.NET Core, EF Core, MediatR, FluentValidation

## Global Constraints

- DO NOT modify `BaseCrudController` — it stays as-is with `ISender` requirement
- DO NOT modify DTOs in `Shared/LYBT.Shared.Models/Contracts/`
- DO NOT change API routes (same HTTP methods, same URL patterns)
- Controllers MUST NOT import entity types (no `HerbDtoMapper`, `Patient.Create()`, etc.)
- `dotnet build LYBTZYZS.sln` must pass 0 errors after each task
- `dotnet test tests/LYBT.Tests.Architecture/` must pass after all tasks
- Commit after each task: `refactor(crud): <module> — remove trivial MediatR handlers`

---

## Handler Classification Summary

### KEEP (Complex — MediatR retained)

| Module | Handlers | Reason |
|--------|----------|--------|
| Herbs | CreateHerb, DeleteHerb, BatchDeleteHerbs, BatchImportHerbs, CheckHerbReference | Domain events, cache invalidation, cross-module |
| Formula | CreateFormula, DeleteFormula, BatchDeleteFormulas, BatchImportFormulas, ValidateFormulaHerb, GetPendingValidation | Domain events, cross-module |
| Patients | CreatePatient, DeletePatient, TogglePatientStatus, BatchDeletePatients, CheckPatientReference, BatchCheckPatientReference | Domain events, cross-module (MedicalCase) |
| Users | CreateUser, DeleteUser, ToggleUserStatus, RestoreUser, ResetPassword, ChangePassword, BatchDeleteUsers, BatchEnableUsers, BatchDisableUsers | Identity (UserManager), sysadmin protection, domain events |

### REMOVE (Trivial — direct service call)

| Module | Handlers Removed | Count |
|--------|-----------------|-------|
| Herbs | UpdateHerb, ToggleHerbStatus, RestoreHerb, BatchEnableHerbs, BatchDisableHerbs, GetHerbs, GetHerb | 7 |
| Formula | UpdateFormula, ToggleFormulaStatus, RestoreFormula, BatchEnableFormulas, BatchDisableFormulas, GetFormulas, GetFormula | 7 |
| Patients | UpdatePatient, RestorePatient, GetPatients, GetPatient, SearchPatientByIdNumber | 5 |
| Users | UpdateUser, GetUsers, GetUser, GetCurrentUser, ChangeProfile | 5 |
| **Total** | | **24** |

---

### Task 1: Herbs Module — Create IHerbService + HerbService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbService.cs`
- Create: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbRepository.cs` (add `GetByIdIncludingDeletedAsync` if missing)

- [ ] **Step 1: Create `IHerbService` interface**

```csharp
// src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbService.cs
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Herbs.Interfaces;

public interface IHerbService
{
    Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, Guid operatorId, CancellationToken ct);
    Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, Guid operatorId, CancellationToken ct);
    Task<Result<HerbDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchDisableAsync(List<Guid> ids, CancellationToken ct);
}
```

- [ ] **Step 2: Create `HerbService` implementation**

The service injects `IHerbRepository`, calls repository methods, maps via `HerbDtoMapper`, returns `Result<T>`.

```csharp
// src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs
using LYBT.Module.Herbs.Application.Mappers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Herbs.Services;

internal class HerbService : IHerbService
{
    private readonly IHerbRepository _herbRepository;

    public HerbService(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var paged = await _herbRepository.GetPagedAsync(page, pageSize, keyword, ct);
        var dtos = paged.Items.Select(HerbDtoMapper.ToListDto).ToList();
        return Result<PagedResult<HerbListDto>>.Success(
            new PagedResult<HerbListDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure("药材不存在");
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure("药材不存在");

        if (herb.Name != dto.Name && await _herbRepository.ExistsByNameAsync(dto.Name, id, ct))
            return Result<HerbDetailDto>.Failure("药材名称已存在");

        herb.UpdateProfile(dto.Name, dto.PinYinCode, dto.Category, dto.Properties, dto.Origin,
            dto.Spec, dto.Unit, dto.Price, dto.CostPrice, dto.Effect, dto.Usage, dto.Remark, operatorId);
        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure("药材不存在");

        var newStatus = herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
        herb.ChangeStatus(newStatus, operatorId);
        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdIncludingDeletedAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure("药材不存在");
        if (!herb.IsDeleted)
            return Result<HerbDetailDto>.Failure("药材未被删除，无需恢复");

        if (await _herbRepository.ExistsByNameAsync(herb.Name, id, ct))
            return Result<HerbDetailDto>.Failure("药材名称已存在，无法恢复");

        herb.Restore(operatorId);
        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new BatchOperationResultDto();
        foreach (var id in ids)
        {
            try
            {
                var herb = await _herbRepository.GetByIdAsync(id, ct);
                if (herb == null) { result.FailureCount++; result.Failures.Add(new() { Id = id, Reason = "药材不存在" }); continue; }
                herb.ChangeStatus(CommonStatus.Enabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, ct);
                result.SuccessCount++;
            }
            catch (Exception ex) { result.FailureCount++; result.Failures.Add(new() { Id = id, Reason = ex.Message }); }
        }
        return Result<BatchOperationResultDto>.Success(result);
    }

    public async Task<Result<BatchOperationResultDto>> BatchDisableAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new BatchOperationResultDto();
        foreach (var id in ids)
        {
            try
            {
                var herb = await _herbRepository.GetByIdAsync(id, ct);
                if (herb == null) { result.FailureCount++; result.Failures.Add(new() { Id = id, Reason = "药材不存在" }); continue; }
                herb.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, ct);
                result.SuccessCount++;
            }
            catch (Exception ex) { result.FailureCount++; result.Failures.Add(new() { Id = id, Reason = ex.Message }); }
        }
        return Result<BatchOperationResultDto>.Success(result);
    }
}
```

- [ ] **Step 3: Ensure `IHerbRepository` has `GetByIdIncludingDeletedAsync`**

Check if the method exists. If not, add it to the interface and implementation.

- [ ] **Step 4: Register `IHerbService` in `HerbsModule.cs`**

Add `services.AddScoped<IHerbService, HerbService>()` in `AddHerbsModule()`.

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Server/Modules/LYBT.Module.Herbs/`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbService.cs src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbRepository.cs
git commit -m "refactor(crud): add IHerbService for direct service injection in Herbs module"
```

---

### Task 2: Herbs Module — Refactor Controllers + Delete Handlers

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/UpdateHerbCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/UpdateHerbCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/ToggleHerbStatusCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/ToggleHerbStatusCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchEnableDisableHerbsCommands.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchEnableDisableHerbsCommandHandlers.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbsQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbsQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Herbs/Application/Validators/UpdateHerbValidator.cs`

- [ ] **Step 1: Refactor `HerbsController` (Server)**

Change constructor to inject `IHerbService` alongside `ISender`. Convert trivial methods to call `_herbService` directly:

```csharp
// Constructor
private readonly IHerbService _herbService;

public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService)
    : base(sender, logger)
{
    _herbService = herbService;
}

// GetList — replace Sender.Send(new GetHerbsQuery(...)) with:
var result = await _herbService.GetPagedAsync(page, pageSize, keyword, ct);

// GetById — replace Sender.Send(new GetHerbQuery(id)) with:
var result = await _herbService.GetByIdAsync(id, ct);

// Update — replace Sender.Send(new UpdateHerbCommand(...)) with:
var result = await _herbService.UpdateAsync(id, inputDto, operatorId, ct);

// ToggleStatus — replace Sender.Send(new ToggleHerbStatusCommand(...)) with:
var result = await _herbService.ToggleStatusAsync(id, operatorId, ct);

// Restore — replace Sender.Send(new RestoreHerbCommand(...)) with:
var result = await _herbService.RestoreAsync(id, operatorId, ct);

// BatchEnable — replace ExecuteBatchStatusAsync with direct call:
var result = await _herbService.BatchEnableAsync(dto.Ids, ct);

// BatchDisable — same pattern
var result = await _herbService.BatchDisableAsync(dto.Ids, ct);
```

Keep `Sender.Send()` for: `Create`, `Delete`, `BatchDelete`, `BatchImport`, `CheckReference`, `BatchCheckReference`.

- [ ] **Step 2: Refactor `HerbsController` (LocalWebAPI)**

Same pattern — inject `IHerbService`, convert `GetById`, `BatchEnable`, `BatchDisable`.

- [ ] **Step 3: Delete trivial handler files**

```bash
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/UpdateHerbCommand.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/UpdateHerbCommandHandler.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/ToggleHerbStatusCommand.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/ToggleHerbStatusCommandHandler.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommand.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/RestoreHerbCommandHandler.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchEnableDisableHerbsCommands.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchEnableDisableHerbsCommandHandlers.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbsQuery.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbsQueryHandler.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbQuery.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Queries/GetHerbQueryHandler.cs
git rm src/Server/Modules/LYBT.Module.Herbs/Application/Validators/UpdateHerbValidator.cs
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors (fix any missing references)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(crud): Herbs — remove trivial MediatR handlers, inject IHerbService directly"
```

---

### Task 3: Formula Module — Create IFormulaService + FormulaService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`
- Create: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaRepository.cs` (add `GetByIdIncludingDeletedAsync` if missing)

- [ ] **Step 1: Create `IFormulaService` interface**

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Formula.Interfaces;

public interface IFormulaService
{
    Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto, Guid operatorId, CancellationToken ct);
    Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id, Guid operatorId, CancellationToken ct);
    Task<Result<FormulaDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchDisableAsync(List<Guid> ids, CancellationToken ct);
}
```

- [ ] **Step 2: Create `FormulaService` implementation**

Same pattern as HerbService: inject `IFormulaRepository`, use `FormulaDtoMapper`, return `Result<T>`.

- [ ] **Step 3: Ensure `IFormulaRepository` has `GetByIdIncludingDeletedAsync`**

- [ ] **Step 4: Register in `FormulaModule.cs`**

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Server/Modules/LYBT.Module.Formula/`

- [ ] **Step 6: Commit**

---

### Task 4: Formula Module — Refactor Controllers + Delete Handlers

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/UpdateFormulaCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/UpdateFormulaCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/ToggleFormulaStatusCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/ToggleFormulaStatusCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/RestoreFormulaCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/RestoreFormulaCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/BatchEnableDisableFormulasCommands.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Commands/BatchEnableDisableFormulasCommandHandlers.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Queries/GetFormulasQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Queries/GetFormulasQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Queries/GetFormulaQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Queries/GetFormulaQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Formula/Application/Validators/UpdateFormulaValidator.cs`

- [ ] **Step 1: Refactor `FormulasController` (Server)**

Inject `IFormulaService`. Convert: `GetList`, `GetById`, `Update`, `ToggleStatus`, `Restore`, `BatchEnable`, `BatchDisable`.

Keep MediatR for: `Create`, `Delete`, `BatchDelete`, `Import`, `GetPendingValidation`, `ValidateHerb`.

- [ ] **Step 2: Refactor `FormulasController` (LocalWebAPI)**

Convert `GetById`, `BatchEnable`, `BatchDisable`.

- [ ] **Step 3: Delete trivial handler files**

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`

- [ ] **Step 5: Commit**

---

### Task 5: Patients Module — Create IPatientService + PatientService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Patients/Interfaces/IPatientService.cs`
- Create: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs`

- [ ] **Step 1: Create `IPatientService` interface**

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Patients.Interfaces;

public interface IPatientService
{
    Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken ct);
    Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PatientDetailDto>> GetByIdNumberAsync(string idNumber, CancellationToken ct);
    Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto, Guid operatorId, CancellationToken ct);
    Task<Result<PatientDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct);
}
```

- [ ] **Step 2: Create `PatientService` implementation**

- [ ] **Step 3: Register in `PatientsModule.cs`**

- [ ] **Step 4: Build and verify**

- [ ] **Step 5: Commit**

---

### Task 6: Patients Module — Refactor Controllers + Delete Handlers

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/UpdatePatientCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/UpdatePatientCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/RestorePatientCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/RestorePatientCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/GetPatientsQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/GetPatientsQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/GetPatientQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/GetPatientQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/SearchPatientByIdNumberQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Queries/SearchPatientByIdNumberQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Patients/Application/Validators/UpdatePatientValidator.cs`

- [ ] **Step 1: Refactor `PatientsController` (Server)**

Inject `IPatientService`. Convert: `GetList`, `GetById`, `Update`, `Restore`, `GetByIdNumber`.

Also convert `CheckOwnershipAsync` private helper to use `_patientService.GetByIdAsync()` instead of `Sender.Send(new GetPatientQuery(...))`.

Keep MediatR for: `Create`, `Delete`, `ToggleStatus`, `BatchDelete`, `CheckReference`, `BatchCheckReference`.

- [ ] **Step 2: Refactor `PatientsController` (LocalWebAPI)**

Convert `GetById`, `GetByIdNumber`.

- [ ] **Step 3: Delete trivial handler files**

- [ ] **Step 4: Build and verify**

- [ ] **Step 5: Commit**

---

### Task 7: Users Module — Create IUserService + UserService

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`

- [ ] **Step 1: Create `IUserService` interface**

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Users.Interfaces;

public interface IUserService
{
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto, Guid operatorId, bool isAdmin, CancellationToken ct);
    Task<Result<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto dto, Guid currentUserId, CancellationToken ct);
}
```

- [ ] **Step 2: Create `UserService` implementation**

Inject `IUserRepository`. Use `UserMapper.ToListDto/ToDetailDto`.

- [ ] **Step 3: Register in `UsersModule.cs`**

- [ ] **Step 4: Build and verify**

- [ ] **Step 5: Commit**

---

### Task 8: Users Module — Refactor Controllers + Delete Handlers

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangeProfileCommand.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Commands/ChangeProfileCommandHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetCurrentUserQuery.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetCurrentUserQueryHandler.cs`
- Delete: `src/Server/Modules/LYBT.Module.Users/Application/Validators/UpdateUserValidator.cs`

- [ ] **Step 1: Refactor `BaseUsersController`**

Inject `IUserService` in constructor (alongside `ISender`). Convert: `GetList`, `GetById`, `Update`, `GetCurrentUser`, `ChangeProfile`.

Keep MediatR for: `Create`, `Delete`, `ToggleStatus`, `Restore`, `BatchDelete`, `ResetPassword`, `ChangePassword`, `BatchEnable`, `BatchDisable`.

- [ ] **Step 2: Refactor concrete `UsersController` (both Server and LocalWebAPI)**

Pass `IUserService` through to base.

- [ ] **Step 3: Delete trivial handler files**

- [ ] **Step 4: Build and verify**

- [ ] **Step 5: Commit**

---

### Task 9: Final Verification

- [ ] **Step 1: Full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings related to missing types

- [ ] **Step 2: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass

- [ ] **Step 3: Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All tests pass (LocalWebAPI controllers now use services)

- [ ] **Step 4: Server tests**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "refactor(crud): remove trivial MediatR handlers, inject services directly in 4 CRUD modules"
```

---

## Files Deleted (Total: ~48 files)

| Module | Command/Query files | Handler files | Validator files | Total |
|--------|-------------------|--------------|----------------|-------|
| Herbs | 5 | 5 | 1 | 11 |
| Formula | 5 | 5 | 1 | 11 |
| Patients | 5 | 5 | 1 | 11 |
| Users | 5 | 5 | 1 | 11 |
| **Total** | **20** | **20** | **4** | **44** |

## Files Created (Total: 8 files)

| Module | Interface | Implementation |
|--------|-----------|---------------|
| Herbs | `IHerbService.cs` | `HerbService.cs` |
| Formula | `IFormulaService.cs` | `FormulaService.cs` |
| Patients | `IPatientService.cs` | `PatientService.cs` |
| Users | `IUserService.cs` | `UserService.cs` |

## Files Modified (Total: ~10 files)

| File | Change |
|------|--------|
| `HerbsController.cs` (Server) | Inject `IHerbService`, convert 7 methods |
| `HerbsController.cs` (LocalWebAPI) | Inject `IHerbService`, convert 3 methods |
| `FormulasController.cs` (Server) | Inject `IFormulaService`, convert 7 methods |
| `FormulasController.cs` (LocalWebAPI) | Inject `IFormulaService`, convert 3 methods |
| `PatientsController.cs` (Server) | Inject `IPatientService`, convert 5 methods |
| `PatientsController.cs` (LocalWebAPI) | Inject `IPatientService`, convert 2 methods |
| `BaseUsersController.cs` | Inject `IUserService`, convert 5 methods |
| `UsersController.cs` (Server) | Pass `IUserService` to base |
| `UsersController.cs` (LocalWebAPI) | Pass `IUserService` to base |
| 4x `*Module.cs` | Register new services |
