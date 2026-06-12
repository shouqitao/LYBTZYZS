# EntityAudit 技术债务清理 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 全量清除通用实体审计 (EntityAudit) 代码、DTO、测试和 DI 注册，保留 MedicalCaseAudit 和 SecurityAudit 不受影响。

**Architecture:** 自底向上清理 -- 先删除实体/接口/服务，再删 Controller/DTO，再清 Desktop 端，最后清测试和文档。每步后编译验证。注意 AuditOperationType 枚举是共享的 (MedicalCaseAudit 也用)，不可删除。已有 migration 不修改，创建新 migration 删表。

**Tech Stack:** .NET 8, EF Core 8, WPF/Prism, xUnit

**Design Doc:** `docs/plans/2026-02-10-doc-code-alignment-design.md`

---

## 关键约束

- **保留 MedicalCaseAudit**: MedicalCaseAuditLog, MedicalCaseAuditService, IMedicalCaseAuditService, MedicalCaseAuditLogDto -- 全部不动
- **保留 SecurityAudit**: SecurityAuditLog, SecurityAuditService, SecurityAuditCleanupService -- 全部不动
- **保留 AuditOperationType 枚举**: 位于 MedicalCaseEnums.cs，被 MedicalCaseAudit 使用，不可删除
- **不修改历史 migration**: 已有 `AddEntityAuditLogsTable` migration 保留，创建新 migration 删表

---

## Task 1: Server 端实体层清理

**Files:**
- Delete: `src/Server/Core/LYBT.Entities/Common/EntityAuditLog.cs`
- Delete: `src/Server/Core/LYBT.Infrastructure/Interfaces/IAuditService.cs`
- Delete: `src/Server/Core/LYBT.Infrastructure/Services/EntityAuditService.cs`
- Delete: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/EntityAuditLogConfiguration.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`

**Step 1: 删除 4 个源文件**

```
rm src/Server/Core/LYBT.Entities/Common/EntityAuditLog.cs
rm src/Server/Core/LYBT.Infrastructure/Interfaces/IAuditService.cs
rm src/Server/Core/LYBT.Infrastructure/Services/EntityAuditService.cs
rm src/Server/Core/LYBT.Infrastructure/Data/Configurations/EntityAuditLogConfiguration.cs
```

**Step 2: 修改 AppDbContext.cs -- 移除 EntityAuditLogs DbSet**

在 `AppDbContext.cs` 中找到并删除:
```csharp
/// <summary>通用实体审计日志</summary>
public DbSet<EntityAuditLog> EntityAuditLogs { get; set; } = null!;
```

如果文件中还有对 `LYBT.Entities.Common` 的其他引用 (如 SystemLog)，保留 using 语句。否则也删除 `using LYBT.Entities.Common;`。

**Step 3: 编译验证 Infrastructure 项目**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 会报错 -- 因为 Module 中仍注册了 IAuditService。暂时忽略，下一步处理。

---

## Task 2: Server 端模块 DI 注册清理

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs`

**Step 1: 修改 4 个 Module 注册文件**

在每个文件中，删除以下模式的行 (行号可能略有偏差，按内容匹配):

```csharp
// EntityAudit 审计服务
services.AddScoped<IAuditService<Xxx>, EntityAuditService<Xxx>>();
```

同时删除对应的 using 语句 (如果文件中无其他对该命名空间的引用):
```csharp
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services;
```

> **注意**: 检查每个文件是否还有其他对 `LYBT.Infrastructure.Interfaces` 或 `LYBT.Infrastructure.Services` 的引用。如果有 (如 Repository 注册)，保留 using。

**Step 2: 编译验证**

Run: `dotnet build src/Server/Modules/LYBT.Module.Herbs && dotnet build src/Server/Modules/LYBT.Module.Users && dotnet build src/Server/Modules/LYBT.Module.Patients && dotnet build src/Server/Modules/LYBT.Module.Formula`
Expected: 全部编译通过

---

## Task 3: Controller + DTO + 配置清理

**Files:**
- Delete: `src/Server/Services/LYBT.WebAPI/Controllers/EntityAuditController.cs`
- Delete: `src/Shared/LYBT.Shared.Models/Contracts/Common/EntityAuditLogDto.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/appsettings.json`
- Modify: `src/Shared/LYBT.Shared.Configuration/Options/Server/UserManagementOptions.cs`

**Step 1: 删除 Controller 和 DTO**

```
rm src/Server/Services/LYBT.WebAPI/Controllers/EntityAuditController.cs
rm src/Shared/LYBT.Shared.Models/Contracts/Common/EntityAuditLogDto.cs
```

**Step 2: 修改 appsettings.json -- 移除审计配置项**

在 `UserManagement` 配置节中删除:
```json
"EnableDetailedAuditLogging": false
```

**Step 3: 修改 UserManagementOptions.cs -- 移除属性**

删除:
```csharp
public bool EnableDetailedAuditLogging { get; set; } = false;
```

**Step 4: 编译验证 Server 端**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: 编译通过

---

## Task 4: Desktop 端清理

**Files:**
- Delete: `src/Client/Desktop/Shell/Dialogs/ViewModels/EntityAuditLogDialogViewModel.cs`
- Delete: `src/Client/Desktop/Shell/Dialogs/Views/EntityAuditLogDialog.xaml`
- Delete: `src/Client/Desktop/Shell/Dialogs/Views/EntityAuditLogDialog.xaml.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Handlers/UserAuditHandler.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Handlers/IUserAuditHandler.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`

**Step 1: 删除 5 个 Desktop 文件**

```
rm src/Client/Desktop/Shell/Dialogs/ViewModels/EntityAuditLogDialogViewModel.cs
rm src/Client/Desktop/Shell/Dialogs/Views/EntityAuditLogDialog.xaml
rm src/Client/Desktop/Shell/Dialogs/Views/EntityAuditLogDialog.xaml.cs
rm src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Handlers/UserAuditHandler.cs
rm src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Handlers/IUserAuditHandler.cs
```

**Step 2: 修改 App.xaml.cs -- 移除对话框注册**

删除:
```csharp
containerRegistry.RegisterDialog<Dialogs.Views.EntityAuditLogDialog, Dialogs.ViewModels.EntityAuditLogDialogViewModel>();
```

**Step 3: 修改 4 个 MasterDetailViewModel -- 移除 ShowAuditLog 命令**

在每个 ViewModel 中找到并删除以下模式的代码:

**HerbMasterDetailViewModel.cs**:
- 删除 `[RelayCommand(CanExecute = nameof(CanShowAuditLog))]` 特性
- 删除 `private async Task ShowAuditLog()` 方法
- 删除 `private bool CanShowAuditLog()` 方法

**PatientMasterDetailViewModel.cs**:
- 同上模式

**FormulaMasterDetailViewModel.cs**:
- 同上模式

**UserMasterDetailViewModel.cs**:
- 删除 `ShowAuditLog` 方法
- 删除 `CanShowAuditLog` 方法
- 删除 `_auditHandler` 字段声明
- 删除构造函数中 `IUserAuditHandler auditHandler` 参数和赋值
- 删除相关 using 语句

**Step 4: 编译验证 Desktop 端**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译通过

---

## Task 5: 测试文件清理

**Files:**
- Delete: `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/EntityAuditServiceTests.cs`
- Delete: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/EntityAuditControllerIntegrationTests.cs`
- Delete: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Shell.Tests/Dialogs/EntityAuditLogDialogViewModelTests.cs`
- Delete: `tests/LYBT.Tests.Desktop.Unit/Shell/Dialogs/EntityAuditLogDialogViewModelTests.cs`
- Modify: `tests/Architecture/ArchTests.cs`
- Modify: `tests/LYBT.Tests.Architecture/ArchTests.cs`
- Modify: `tests/Architecture/Server/AggregateRootArchTests.cs`
- Modify: `tests/LYBT.Tests.Architecture/AggregateRootArchTests.cs`

**Step 1: 删除 4 个测试文件**

```
rm tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/EntityAuditServiceTests.cs
rm tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/EntityAuditControllerIntegrationTests.cs
rm tests/UnitTests/Client/Desktop/LYBT.Desktop.Shell.Tests/Dialogs/EntityAuditLogDialogViewModelTests.cs
rm tests/LYBT.Tests.Desktop.Unit/Shell/Dialogs/EntityAuditLogDialogViewModelTests.cs
```

**Step 2: 修改架构测试 -- 移除 EntityAudit 排除项**

在 `ArchTests.cs` (两个版本) 中，从 `excludedControllers` 数组移除 `"EntityAuditController"`。

在 `AggregateRootArchTests.cs` (两个版本) 中，从排除列表移除 `"LYBT.Entities.Common.EntityAuditLog"`。

**Step 3: 编译验证测试项目**

Run: `dotnet build LYBTZYZS.sln`
Expected: 全量编译通过，0 errors

---

## Task 6: 创建数据库 Migration

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Migrations/[timestamp]_RemoveEntityAuditLogsTable.cs` (EF 自动生成)

**Step 1: 生成 Migration**

Run: `dotnet ef migrations add RemoveEntityAuditLogsTable -p src/Server/Core/LYBT.Infrastructure -s src/Server/Services/LYBT.WebAPI`

Expected: 生成新 migration 文件，内容应包含:
```csharp
migrationBuilder.DropTable(name: "EntityAuditLogs");
```

**Step 2: 验证 Migration 内容**

检查生成的 migration:
- Up() 方法: 应只有 `DropTable("EntityAuditLogs")` 和相关索引删除
- Down() 方法: 应只有 `CreateTable("EntityAuditLogs")` 和相关索引创建
- 不应触及 MedicalCaseAuditLogs 或 SecurityAuditLogs 表

> **关键**: 如果 migration 中包含了非预期的变更 (比如修改其他表)，需要删除重新生成或手动编辑。

**Step 3: 编译验证**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 编译通过

---

## Task 7: 文档更新

**Files:**
- Modify: `docs/03-architecture/03-server.md`
- Modify: `docs/04-api-reference/README.md`
- Modify: `docs/02-requirements/07-medical-cases.md`
- Modify: `src/Client/Desktop/Shell/README.md`

**Step 1: 修改 server.md**

移除对 `EntityAuditService.cs` 的文件树引用。

**Step 2: 修改 04-api-reference/README.md**

从"系统模块 (非业务)"表格中移除 EntityAudit 端点行:
```
| GET | `/entityaudit/{entityType}/{entityId}` | 已认证 | 通用实体审计日志 |
```

以及相关的 EntityAudit 快捷端点。

**Step 3: 修改 medical-cases.md**

FR-MC-012 审计日志章节中，如果有对 EntityAuditController 的引用，改为仅引用 MedicalCaseController 的 `GetAuditLogs` 端点。

**Step 4: 修改 Shell/README.md**

移除 EntityAuditLogDialog 相关的文件列表项和注册代码示例。

---

## Task 8: 全量验证

**Step 1: 全量编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings (或仅预期的 Mapperly 警告)

**Step 2: 全量测试**

Run: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
Expected: 全部通过

**Step 3: 搜索残留引用**

Run: `grep -r "EntityAudit" src/ --include="*.cs" --include="*.xaml"`
Expected: 0 匹配

Run: `grep -r "IAuditService" src/ --include="*.cs"`
Expected: 0 匹配 (注意不要误删 IMedicalCaseAuditService)

Run: `grep -r "EntityAuditLog" src/ docs/ --include="*.cs" --include="*.md"`
Expected: 仅在历史 migration 文件中存在

**Step 4: 更新 planning-with-files**

更新 task_plan.md: 标记所有 Phase complete
更新 progress.md: 记录执行日志

---

## Task 依赖关系

```
Task 1 (实体层清理) ────────────────────┐
    ▼                                   │
Task 2 (Module DI清理) ────────────────┤
    ▼                                   │
Task 3 (Controller+DTO+配置) ──────────┤
    ▼                                   │── 严格顺序
Task 4 (Desktop端清理) ────────────────┤
    ▼                                   │
Task 5 (测试清理) ─────────────────────┤
    ▼                                   │
Task 6 (Migration) ────────────────────┤
    ▼                                   │
Task 7 (文档更新) ─────────────────────┤
    ▼                                   │
Task 8 (全量验证) ─────────────────────┘
```

**无并行机会**: 自底向上清理，每步依赖前步编译通过。

---

## 风险与注意事项

| 风险 | 缓解措施 |
|------|----------|
| 误删 MedicalCaseAudit 代码 | 搜索时区分 EntityAudit vs MedicalCaseAudit 前缀 |
| 误删 AuditOperationType 枚举 | 该枚举在 MedicalCaseEnums.cs 中，被 MedicalCaseAudit 使用，绝对不删 |
| Migration 包含非预期变更 | 检查生成的 migration 内容，仅保留 DropTable("EntityAuditLogs") |
| Desktop 编译因 XAML 缓存失败 | 清理 bin/obj 后重新编译 |
| 架构测试排除列表过时 | Task 5 中同步更新排除列表 |

---

## 汇总统计

| 操作 | 文件数 |
|------|--------|
| 完全删除 (源代码) | 11 |
| 完全删除 (测试) | 4 |
| 部分修改 (源代码) | 10 |
| 部分修改 (测试) | 4 |
| 部分修改 (文档) | 4 |
| 新建 (Migration) | 1 |
| **总影响文件** | **~34** |

---

**Created**: 2026-02-10
**Total Tasks**: 8 (严格顺序执行)
**Estimated Batches**: 8 (无并行)
