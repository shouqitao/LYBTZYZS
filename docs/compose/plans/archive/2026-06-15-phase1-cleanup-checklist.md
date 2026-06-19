# Phase 1 代码清理 — 最终详细清单

> 日期: 2026-06-15
> 状态: 待用户确认

## 清理原则

1. **删除** = 整个文件/目录移除
2. **修改** = 文件保留，移除特定代码
3. **保留** = 不动
4. 每个区域执行后运行 `dotnet build` + `dotnet test` 验证

---

## 区域 1：移除同步模块

### 删除（~30 文件 + 2 个目录）

**Server 模块（整个目录删除）：**
- `src/Server/Modules/LYBT.Module.Sync/` — 9 文件（SyncService 655行、SyncRepository 201行、ISyncService、ISyncRepository、ChecksumHelper 206行、SyncModule、csproj、AGENTS.md、README.md）

**Desktop 模块（整个目录删除）：**
- `src/Client/Desktop/Modules/LYBT.Desktop.Sync/` — 15 文件（SyncViewModel 500行、SyncConflictDialog、SyncPhase、SyncResolutionBuilder、SyncErrorClassifier、XAML 视图、csproj、AGENTS.md、README.md）

**Controller + DTOs：**
- `src/Server/Services/LYBT.WebAPI/Controllers/SyncController.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Sync/` — 10 个 DTO 文件

**Desktop 合约/服务：**
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/ISyncApi.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Events/SyncEvents.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/SyncService.cs`

**测试文件（9 个）：**
- `tests/LYBT.Tests.Server/Unit/Sync/ChecksumHelperTests.cs`
- `tests/LYBT.Tests.Server/Integration/Sync/US_Sync_MustHaveTests.cs`
- `tests/LYBT.Tests.Server/Integration/Sync/US_Sync_ShouldHaveTests.cs`
- `tests/LYBT.Tests.Desktop/Integration/Modules/SyncTests.cs`
- `tests/LYBT.Tests.Desktop/Unit/Sync/` — 4 个测试文件
- `tests/LYBT.Tests.Desktop/Integration/Sync/SyncDeleteIntegrationTests.cs`

### 修改（~15 文件）

| 文件 | 改动 |
|------|------|
| `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs` | 移除 `using LYBT.Module.Sync` + `services.AddSyncModule()` |
| `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | 移除 `using LYBT.Module.Sync` + `builder.Services.AddSyncModule()` |
| `src/Client/Desktop/Shell/App.xaml.cs` | 移除 `using LYBT.Desktop.Sync` + `moduleCatalog.AddModule<SyncModule>()` |
| `src/Server/Services/LYBT.WebAPI/Serialization/LybtJsonContext.cs` | 移除 Sync 相关 JsonSerializable 属性 |
| `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | 移除同步状态属性/方法（~30行） |
| `src/Client/Desktop/Shell/Services/MenuManager.cs` | 移除 `IsSyncVisible` |
| `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs` | 移除 `ISyncApi` 注册 |
| `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Helpers/ChecksumHelper.cs` | 移除 Sync 模块引用注释 |
| `src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj` | 移除 Sync 项目引用 |
| `src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj` | 移除 Sync 项目引用 |
| `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj` | 移除 Sync 项目引用 |
| `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj` | 移除 Sync 项目引用 |
| `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj` | 移除 Sync 项目引用 |
| `tests/LYBT.Tests.Architecture/ArchTests.cs` | 移除 Sync 引用 |
| `tests/LYBT.Tests.Architecture/ServerArchTests.cs` | 移除 Sync 引用 |
| `tests/LYBT.Tests.Architecture/AggregateRootArchTests.cs` | 移除 Sync 引用 |

---

## 区域 2：简化权限/审计

### 删除（~25 文件）

**医案权限/审计：**
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePermissionService.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCasePermissionService.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCasePermissionDto.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuditService.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseAuditService.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseAuditLogRepository.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseAuditLogRepository.cs`
- `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseAuditLog.cs`
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/MedicalCaseAuditLogConfiguration.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseAuditController.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseAuditLogDto.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseAuditLogPagedResultDto.cs`

**安全审计：**
- `src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/ISecurityAuditService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/ISecurityAuditRepository.cs`
- `src/Server/Modules/LYBT.Module.Auth/Repositories/SecurityAuditRepository.cs`
- `src/Server/Core/LYBT.Entities/Auth/SecurityAuditLog.cs`
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/SecurityAuditLogConfiguration.cs`
- `src/Server/Services/LYBT.WebAPI/BackgroundServices/SecurityAuditCleanupService.cs`

**自动登录令牌：**
- `src/Server/Core/LYBT.Entities/Auth/AutoLoginToken.cs`
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/AutoLoginTokenConfiguration.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/AutoLoginService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAutoLoginService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Repositories/AutoLoginTokenRepository.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAutoLoginTokenRepository.cs`

**刷新令牌：**
- `src/Server/Core/LYBT.Entities/Auth/RefreshToken.cs`
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/TokenManagementService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/ITokenManagementService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/ITokenRevocationService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Repositories/RefreshTokenRepository.cs`

### 修改（~20 文件）

| 文件 | 改动 |
|------|------|
| `PolicyConstants.cs` | 4 策略→2（DoctorOrReceptionist + AdminOrSuperAdmin） |
| `AuthenticationServiceCollectionExtensions.cs` | 简化策略定义 |
| `AppDbContext.cs` | 移除 5 个 DbSet（RefreshTokens/AutoLoginTokens/SecurityAuditLogs/MedicalCaseAuditLogs/MedicalCasePrintLogs） |
| `MedicalCaseModule.cs` | 移除权限/审计/打印日志 DI 注册 |
| `AuthModule.cs` | 移除安全审计/令牌 DI 注册 |
| `MedicalCaseFacade.cs` | 移除权限/审计/打印日志方法 |
| `DatabaseServiceCollectionExtensions.cs` | 移除 SecurityAuditCleanupService 注册 |
| `PatientsController.cs` (Server) | 更新 `[Authorize]` 属性 |
| `UsersController.cs` (Server) | 更新 `[Authorize]` 属性 |
| `DiagnosticsController.cs` | 更新 `[Authorize]` 属性 |
| `ConfigurationController.cs` | 更新 `[Authorize]` 属性 |
| `RegistrationsController.cs` | 更新 `[Authorize]` 属性 |
| `HerbsController.cs` (Server) | 更新 `[Authorize]` 属性 |
| `FormulasController.cs` (Server) | 更新 `[Authorize]` 属性 |
| `MedicalCasesController.cs` (Server) | 更新 `[Authorize]` 属性 |
| 8 个 Desktop 合约文件 | 移除 `GetPermissionsAsync`/`GetAuditLogsAsync` |
| `HttpClientApiClient.cs` | 移除权限/审计方法 |
| `MedicalCaseApiClient.cs` | 移除权限/审计方法 |
| `HttpMedicalCaseRepository.cs` | 移除 `GetPermissionsAsync` |
| `LocalWebAPI/MedicalCasesController.cs` | 移除权限服务注入 |

---

## 区域 3：优化批量操作

### 删除（~2 文件）

- `src/Server/Modules/LYBT.Module.Users/Services/UserBatchOperationService.cs` — 整个服务是 batch-enable/disable 的实现
- `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserBatchOperationService.cs` — 对应接口

### 修改（~18 文件）

**Server Controller — 移除 batch-enable/batch-disable 端点：**
| 文件 | 移除端点 |
|------|---------|
| `HerbsController.cs` | `BatchEnable` + `BatchDisable` |
| `FormulasController.cs` | `BatchEnable` + `BatchDisable` |
| `UsersController.cs` | `BatchEnable` + `BatchDisable` |

**Server Controller — 移除 batch-details 端点：**
| 文件 | 移除端点 |
|------|---------|
| `MedicalCasesController.cs` | `GetBatchDetails`（合并到 GET /{id}） |

**Server Service — 移除 batch-enable/disable 方法：**
| 文件 | 移除方法 |
|------|---------|
| `HerbService.cs` + `IHerbService.cs` | `BatchEnableAsync` + `BatchDisableAsync` |
| `FormulaService.cs` + `IFormulaService.cs` | `BatchEnableAsync` + `BatchDisableAsync` |
| `UserService.cs` + `IUserService.cs` | `BatchEnableAsync` + `BatchDisableAsync` |

**Desktop LocalWebAPI Controller — 同步移除：**
| 文件 | 移除端点 |
|------|---------|
| `HerbsController.cs` | `BatchEnable` + `BatchDisable` |
| `FormulasController.cs` | `BatchEnable` + `BatchDisable` |
| `UsersController.cs` | `BatchEnable` + `BatchDisable` |
| `MedicalCasesController.cs` | `GetBatchDetails` |

**Module 注册：**
| 文件 | 改动 |
|------|------|
| `UsersModule.cs` | 移除 `IUserBatchOperationService` 注册 |

---

## 区域 4：简化 Excel 导入导出

### 删除（~2 文件）

**移除 Excel 专用端点（模板下载）：**
| 文件 | 移除端点 |
|------|---------|
| `PatientsController.cs` (Server) | `ExportTemplate` + `Export`（Excel 格式） |
| `HerbsController.cs` (Server) | `ExportTemplate` + `Export`（Excel 格式） |

**保留：**
- `POST /batch-import`（JSON 格式）— UI 层负责 Excel↔JSON 转换
- `FormulaImportExportService.cs` — 验方导入保留

### 修改（~6 文件）

| 文件 | 改动 |
|------|------|
| `PatientsController.cs` (Server) | 移除 `ExportTemplate` + `Export` 端点 |
| `HerbsController.cs` (Server) | 移除 `ExportTemplate` + `Export` 端点 |
| `LocalWebAPI/PatientsController.cs` | 移除 `ExportTemplate` 端点 |
| `LocalWebAPI/HerbsController.cs` | 移除 `ExportTemplate` 端点 |
| `LYBT.Module.Patients.csproj` | 移除 EPPlus 引用 |
| `LYBT.Module.Herbs.csproj` | 移除 EPPlus 引用 |

**保留：** `FormulaImportExportService.cs` + EPPlus 引用（验方导入需要）

---

## 区域 5：移除打印追踪

### 删除（~5 文件）

- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasePrintController.cs`
- `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCasePrintLog.cs`
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/MedicalCasePrintLogConfiguration.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePrintService.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCasePrintService.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/PrintCompletedRequest.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/PrintLogInputDto.cs`

### 修改（~15 文件）

| 文件 | 改动 |
|------|------|
| `MedicalCaseModel.cs` | 移除 PrintVersion/PrintCount/IsPrinted/LastPrintedAt/PrintLogs |
| `MedicalCaseCommandService.cs` | 移除打印保护逻辑 |
| `MedicalCaseFacade.cs` | 移除打印日志方法 |
| `MedicalCaseModule.cs` | 移除打印服务 DI 注册 |
| `AppDbContext.cs` | 移除 MedicalCasePrintLogs DbSet |
| `LocalWebAPI/MedicalCasesController.cs` | 移除打印相关端点 |
| `HttpClientApiClient.cs` | 移除打印相关方法 |
| `MedicalCaseApiClient.cs` | 移除打印相关方法 |
| `ILocalMedicalCaseApi.cs` | 移除打印相关方法 |
| `IMedicalCaseApi.cs` | 移除打印相关方法 |
| `IMedicalCaseRepository.cs` (Desktop) | 移除打印相关方法 |
| `HttpMedicalCaseRepository.cs` | 移除打印相关方法 |
| 测试文件 | 移除打印相关测试 |

**保留：** `PrescriptionPrintService.cs`、QuestPDF 模板、实际打印功能

---

## 区域 6：简化患者实体

### 修改（~6 文件 + 1 Migration）

| 文件 | 改动 |
|------|------|
| `PatientModel.cs` | 移除 MaritalStatus/IdType/Address/AllergyHistory/MedicalHistory/BloodType/EmergencyContact*/Status/DisableReason/LastVisitTime/VisitCount |
| `PatientInputDto.cs` | 同步移除字段 |
| `PatientDto.cs` | 同步移除字段 |
| `PatientService.cs` | 移除字段相关逻辑 |
| `PatientConfiguration.cs` | 更新 EF 配置 |
| 新 Migration | 删除数据库列 |

---

## 区域 7：其他清理

### 删除（~2 文件）

- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PendingQueueManager.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IPendingQueueManager.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/UnfinishedCaseHandler.cs`

### 修改（~55 文件）

**移除 RestoreAsync（~40 文件）：**
| 文件类型 | 数量 | 改动 |
|---------|------|------|
| Server Service 接口 | 4 | 移除 RestoreAsync 签名 |
| Server Service 实现 | 4 | 移除 RestoreAsync 方法 |
| Server Controller | 4 | 移除 Restore 端点 |
| LocalWebAPI Controller | 4 | 移除 Restore 端点 |
| Desktop Repository | 8 | 移除 RestoreAsync |
| Desktop ApiClient 接口 | 4 | 移除 RestoreAsync |
| Desktop ApiClient 实现 | 4 | 移除 RestoreAsync |
| Desktop Local API 接口 | 4 | 移除 RestoreAsync |
| Desktop ViewModel | 4 | 移除 Restore 按钮/命令 |

**移除 CheckReference（~10 文件）：**
| 文件 | 改动 |
|------|------|
| `PatientsController.cs` (Server) | 移除 CheckReference + BatchCheckReference |
| `HerbsController.cs` (Server) | 移除 CheckReference + BatchCheckReference |
| `PatientService.cs` + `IPatientService.cs` | 移除 CheckReferenceAsync |
| `HerbService.cs` + `IHerbService.cs` | 移除 CheckReferenceAsync |
| LocalWebAPI 同步移除 | |
| Desktop 合约/实现 | 移除 CheckReference 相关 |

**移除 PendingQueue/UnfinishedCase：**
| 文件 | 改动 |
|------|------|
| `PatientsModule.cs` (Desktop) | 移除 PendingQueue 注册 |
| `PendingQueueViewModel.cs` | 移除队列管理逻辑 |
| `PatientSelectionViewModel.cs` | 移除 PendingQueue 依赖 |
| `MedicalCaseStartCoordinator.cs` | 移除 UnfinishedCaseHandler 依赖 |

**移除 MedicalCase.Remark 字段：**
| 文件 | 改动 |
|------|------|
| `MedicalCaseModel.cs` | 移除 Remark 属性 |

---

## 总计

| 区域 | 删除 | 修改 | 保留 |
|------|------|------|------|
| 1. 同步模块 | ~30 | ~16 | 0 |
| 2. 权限/审计 | ~25 | ~20 | 0 |
| 3. 批量操作优化 | ~2 | ~18 | 0 |
| 4. Excel 简化 | ~2 | ~6 | 1 (Formula) |
| 5. 打印追踪 | ~7 | ~13 | 0 |
| 6. 患者实体 | 0 | ~6 | 0 |
| 7. 其他清理 | ~3 | ~55 | 0 |
| **合计** | **~69** | **~134** | **~1** |

## 执行后验证

每个区域完成后：
1. `dotnet build LYBTZYZS.sln` — 编译通过
2. `dotnet test tests/LYBT.Tests.Server/` — 服务端测试通过
3. `dotnet test tests/LYBT.Tests.Desktop/` — 桌面端测试通过
4. `dotnet test tests/LYBT.Tests.Architecture/` — 架构测试通过
