# 跨模块编译期解耦设计

> 日期: 2026-02-23 | 状态: 已确认 | 对齐设计: D5-1, D5-2, D5-3

## 目标

消除所有业务模块之间的直接 ProjectReference，统一通过接口抽象实现跨模块通信。

## 背景

当前 7 个跨模块 ProjectReference 造成编译期耦合:

| 消费者 | 引用 | 实际用途 |
|--------|------|----------|
| LYBT.Module.Sync | Herbs, Patients, Formula | CheckReferenceAsync (2处)，Formula 无实际调用 |
| LYBT.Module.MedicalCase | Patients, Users | IPatientRepository + IUserRepository (验证+权限) |
| LYBT.Desktop.MedicalCase | Herbs, Formula | IHerbRepository.SearchAsync + IFormulaRepository (2方法) |

## 设计

### Server 端: ICrossModuleService ISP 拆分

将 1 个接口 (7 方法) 拆分为 4 个域专用接口，定义在 `LYBT.Infrastructure/Services/CrossModule/`:

#### IPatientCrossModuleService

```csharp
public interface IPatientCrossModuleService
{
    Task<PatientBasicInfo?> GetPatientBasicInfoAsync(Guid patientId);
    Task<IReadOnlyList<PatientBasicInfo>> GetPatientsBasicInfoAsync(IEnumerable<Guid> ids);
    Task<bool> PatientExistsAsync(Guid patientId);                    // NEW
    Task<ReferenceCheckResult> CheckReferenceAsync(Guid patientId);   // NEW: 从 PatientService 迁移
}
```

#### IHerbCrossModuleService

```csharp
public interface IHerbCrossModuleService
{
    Task<HerbBasicInfo?> GetHerbBasicInfoAsync(Guid herbId);
    Task<HerbBasicInfo?> GetHerbByNameOrPinyinAsync(string keyword);
    Task<ReferenceCheckResult> CheckReferenceAsync(Guid herbId);      // NEW: 从 HerbService 迁移
}
```

#### IUserCrossModuleService

```csharp
public interface IUserCrossModuleService
{
    Task<UserBasicInfo?> GetUserBasicInfoAsync(Guid userId);
    Task<UserBasicInfo?> GetUserByUsernameAsync(string username);
    Task UpdateUserPasswordHashAsync(Guid userId, string newHash);
    Task<bool> UserExistsAsync(Guid userId);                          // NEW
}
```

#### ICrossModuleAuthService

```csharp
public interface ICrossModuleAuthService
{
    Task RevokeUserTokensAsync(Guid userId, string reason);
}
```

### 实现

CrossModuleService 一个类实现全部 4 接口，直接使用 AppDbContext 查询。

DI 注册:

```csharp
services.AddScoped<CrossModuleService>();
services.AddScoped<IPatientCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IHerbCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IUserCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<ICrossModuleAuthService>(sp => sp.GetRequiredService<CrossModuleService>());
```

### 消费者迁移 (Server)

| 消费者 | 当前注入 | 迁移后注入 |
|--------|----------|-----------|
| SyncService | IHerbService + IPatientService | IHerbCrossModuleService + IPatientCrossModuleService |
| MedicalCaseCommandService | IPatientRepository + IUserRepository | IPatientCrossModuleService + IUserCrossModuleService |
| MedicalCaseStateService | IUserRepository | IUserCrossModuleService |
| MedicalCaseServiceHelper | IPatientRepository + IUserRepository | IPatientCrossModuleService + IUserCrossModuleService |

### 移除的 ProjectReference (Server)

```diff
# LYBT.Module.Sync.csproj
- <ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
- <ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
- <ProjectReference Include="..\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />

# LYBT.Module.MedicalCase.csproj
- <ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
- <ProjectReference Include="..\LYBT.Module.Users\LYBT.Module.Users.csproj" />
```

旧 ICrossModuleService 标记 `[Obsolete]`，渐进迁移。

### Desktop 端: Provider 接口

定义在 `LYBT.Desktop.Contracts/Services/CrossModule/`:

#### IHerbSearchProvider

```csharp
public interface IHerbSearchProvider
{
    Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword);
}
```

#### IFormulaSearchProvider

```csharp
public interface IFormulaSearchProvider
{
    Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize);
    Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id);
}
```

### 实现与注册

- `HerbSearchProvider` 在 LYBT.Desktop.Herbs 中实现，委托给 IHerbRepository
- `FormulaSearchProvider` 在 LYBT.Desktop.Formula 中实现，委托给 IFormulaRepository
- 在各模块 Module.cs 中注册到 Prism DI 容器

### 消费者迁移 (Desktop)

| 消费者 | 当前注入 | 迁移后注入 |
|--------|----------|-----------|
| MedicalCaseMasterDetailViewModel | IHerbRepository (18方法) | IHerbSearchProvider (1方法) |
| FormulaImportDialogViewModel | IFormulaRepository (12方法) | IFormulaSearchProvider (2方法) |

### 移除的 ProjectReference (Desktop)

```diff
# LYBT.Desktop.MedicalCase.csproj
- <ProjectReference Include="..\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
- <ProjectReference Include="..\LYBT.Desktop.Formula\LYBT.Desktop.Formula.csproj" />
```

## 清理项

1. **空壳目录**: 删除 `src/Server/Modules/LYBT.Module.Consultation/` 和 `LYBT.Module.Prescriptions/`
2. **架构测试合并**: `LYBT.ArchTests` 测试迁移到 `LYBT.Tests.Architecture`，删除旧项目
3. **[Obsolete] 旧接口**: ICrossModuleService 标记废弃

## 执行顺序

```
Phase 1: 基础设施 (无依赖)
  [1] 创建 4 个 ISP 接口 (Infrastructure)
  [2] CrossModuleService 实现新接口 + 新方法
  [3] DI 注册 4 接口
  [7] 创建 2 个 Provider 接口 (Contracts)
  [11] 旧接口标记 [Obsolete]

Phase 2: Server 迁移 (依赖 Phase 1)
  [4] Sync 模块迁移
  [5] MedicalCase Server 迁移
  [6] 移除 5 个 Server ProjectReference

Phase 3: Desktop 迁移 (依赖 Phase 1)
  [8] Provider 实现 (Herbs + Formula)
  [9] MedicalCase Desktop 迁移
  [10] 移除 2 个 Desktop ProjectReference

Phase 4: 清理 (依赖 Phase 2+3)
  [12] 架构测试合并
  [13] 空壳目录删除

验证: dotnet build LYBT.All.sln + dotnet test
```

## 验收目标

- 7 个跨模块 ProjectReference 全部移除
- Server 模块仅依赖 Infrastructure + Entities + Shared
- Desktop 模块仅依赖 Core 项目 + Shared
- 编译通过 + 全量测试通过
- 架构测试统一为 1 个项目

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-23 | v1.0 | 初始设计，brainstorm 确认 |
