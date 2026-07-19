# Phase 3: Design Pattern Unification Design

## [S1] Problem

After Phase 2 consolidation (40 → 34 projects), the codebase has inconsistent design patterns across modules:

1. **Client Repository no base class**: 6 repositories independently implement, with duplicate try/catch + logging + exception handling boilerplate.
2. **CancellationToken inconsistency**: Only PatientRepository has CancellationToken, other 5 repositories missing.
3. **DeleteAsync exception handling inconsistency**: Most swallow exceptions and return false, MedicalCase throws.
4. **BatchDeleteAsync return value inconsistency**: User returns null, others return error object.
5. **Error message language mixed**: PatientRepository uses English, others use Chinese.
6. **Duplicate using statements**: 10+ files have duplicate `using LYBT.Shared.Models.Contracts.Common;`.
7. **ValidatableModelBase uses Prism BindableBase**: Should use CommunityToolkit.Mvvm ObservableObject.
8. **Server module internal structure inconsistency**: Repositories/ vs Infrastructure/ mixed.
9. **Comment style mixed**: Chinese/English region headers mixed.
10. **IHerbRepositoryLegacy dual repository**: Old version not cleaned up.

## [S2] Solution Overview

Progressive unification in 4 phases, each independently verifiable:

| Phase | Scope | Priority |
|-------|-------|----------|
| 3a | Client Repository base class + CancellationToken | High |
| 3b | Exception handling strategy unification | High |
| 3c | Error message language + duplicate using cleanup | Medium |
| 3d | Server module structure + comment style unification | Low |

## [S3] Phase 3a: Client Repository Base Class

### Problem
6 Client repositories independently implement, with duplicate try/catch + logging + exception handling boilerplate.

### Solution
Extract `ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>` base class:

```csharp
public abstract class ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>
{
    protected readonly ILogger Logger;
    
    // Standard CRUD methods (with unified exception handling)
    public virtual Task<List<TListDto>> GetAllAsync(CancellationToken ct = default);
    public virtual Task<TDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    public virtual Task<TDetailDto> CreateAsync(TCreateDto dto, CancellationToken ct = default);
    public virtual Task UpdateAsync(int id, TUpdateDto dto, CancellationToken ct = default);
    public virtual Task DeleteAsync(int id, CancellationToken ct = default);  // Unified throw
    
    // Template methods (subclasses can override)
    protected virtual string GetLogPrefix() => GetType().Name;
}
```

### Benefits
- Eliminate duplicate code across 6 repositories
- Unified CancellationToken support
- Unified DeleteAsync exception handling strategy (all throw)

### Repositories to Migrate
1. `LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
2. `LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
3. `LYBT.Desktop.Users/Repositories/UserRepository.cs`
4. `LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`
5. `LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
6. `LYBT.Desktop.Registration/Repositories/RegistrationRepository.cs`

## [S4] Phase 3b: Exception Handling Strategy Unification

### Problem
- DeleteAsync: Most swallow exceptions and return false, MedicalCase throws
- BatchDeleteAsync: User returns null, others return error object
- ToggleStatusAsync: Swallow exceptions and return null

### Solution
- **DeleteAsync**: Unified to throw (consistent with MedicalCase, follows "exceptions propagate to ViewModel" architecture standard)
- **BatchDeleteAsync**: Unified to return `BatchOperationResultDto` with error information
- **ToggleStatusAsync**: Unified to throw (don't swallow exceptions)

### Affected Methods
| Repository | Method | Current Behavior | New Behavior |
|------------|--------|-----------------|--------------|
| HerbRepository | DeleteAsync | return false | throw |
| PatientRepository | DeleteAsync | return false | throw |
| UserRepository | DeleteAsync | return false | throw |
| FormulaRepository | DeleteAsync | return false | throw |
| MedicalCaseRepository | DeleteAsync | throw | throw (no change) |
| RegistrationRepository | DeleteAsync | return false | throw |
| UserRepository | BatchDeleteAsync | return null | return BatchOperationResultDto |
| HerbRepository | ToggleStatusAsync | return null | throw |
| FormulaRepository | ToggleStatusAsync | return null | throw |
| UserRepository | ToggleStatusAsync | return null | throw |

## [S5] Phase 3c: Error Message Language + Duplicate Using Cleanup

### Problem
- PatientRepository uses English error messages, others use Chinese
- 10+ files have duplicate `using LYBT.Shared.Models.Contracts.Common;`

### Solution
- **Error messages**: Unified to Chinese (system面向中文用户), update PatientRepository's English messages
- **Duplicate using**: Batch cleanup all duplicate using statements

### Files to Update (Error Messages)
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`

### Files to Update (Duplicate Using)
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Desktop/DesktopExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Desktop/IDesktopExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/BusinessExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/SystemExceptionHandler.cs`
- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- `src/Server/Core/LYBT.Infrastructure/Web/ControllerBaseExtensions.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IUserRepository.cs`
- And more (batch cleanup)

## [S6] Phase 3d: Server Module Structure + Comment Style Unification

### Problem
- Server module internal structure inconsistent (Repositories/ vs Infrastructure/ mixed)
- Comment style mixed (Chinese/English region headers mixed)
- IHerbRepositoryLegacy dual repository

### Solution
- **Module structure**: Unified to AGENTS.md standard structure `Domain/ + Application/ + Infrastructure/`
- **Comment style**: Unified to Chinese (follows AGENTS.md Chinese comment convention)
- **Legacy repository**: Remove `IHerbRepositoryLegacy` after migration complete

### Modules to Standardize
1. `LYBT.Module.Herbs` - Has dual repository (HerbRepository + HerbRepositoryLegacy)
2. `LYBT.Module.Patients` - Uses Infrastructure/ for repositories
3. Other modules - Check consistency

## [S7] Verification

1. **Build**: `dotnet build LYBTZYZS.sln` — zero errors
2. **Tests**: `dotnet test tests/` — all pass
3. **Architecture tests**: `dotnet test tests/LYBT.Tests.Architecture/` — all pass
4. **Code review**: Verify no behavioral changes

## [S8] Timeline

- **Phase 3a**: 2-3 days (Client Repository base class)
- **Phase 3b**: 1-2 days (Exception handling unification)
- **Phase 3c**: 1 day (Error message + using cleanup)
- **Phase 3d**: 2-3 days (Server module structure)

**Total**: 6-9 days
