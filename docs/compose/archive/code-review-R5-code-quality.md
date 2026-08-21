# R5 代码质量与最佳实践审查报告

**审查日期**：2026-08-21  
**审查范围**：src/Server/、src/Client/Desktop/、src/Shared/  
**审查角度**：命名规范、魔法数字/硬编码、重复代码(DRY)、方法长度、参数数量、未使用代码、注释质量、测试覆盖、Public API 设计、代码复杂度

---

## 审查概览

| 类别 | 发现数 | 严重度分布 |
|------|--------|-----------|
| 命名规范 | 4 | 🟡 中 ×3 / 🟢 低 ×1 |
| 魔法数字/硬编码 | 6 | 🟡 中 ×3 / 🟢 低 ×3 |
| 重复代码(DRY) | 4 | 🔴 高 ×1 / 🟡 中 ×3 |
| 过长方法(>50行) | 20+ | 🔴 高 ×3 / 🟡 中 ×10+ / 🟢 低 ×7 |
| 过多参数(>5) | 2 | 🟡 中 |
| 注释质量 | 5 | 🟡 中 ×2 / 🟢 低 ×3 |
| 测试覆盖 | 35+ handlers 无直接单元测试 | 🔴 高 ×1 |
| Public API 设计 | 3 | 🟡 中 |
| 代码复杂度 | 3 | 🟡 中 ×2 / 🟢 低 ×1 |

---

## 🔴 高严重度

### F1. 大量 CommandHandler 缺少直接单元测试

**文件**：Server 全部 35 个 CommandHandler 和 6 个 QueryHandler  
**问题**：虽然存在集成测试和架构测试，但 Server 侧几乎所有 CommandHandler 和 QueryHandler 均**没有直接对应的单元测试文件**。

**缺失测试的 Handler 清单（35+个）**：

| 模块 | 缺失的 Handler |
|------|---------------|
| Catalog | BatchDelete/Enable/DisableHerbs×3, BatchDelete/Enable/DisableFormulas×3, BatchImportHerbs, BatchImportFormulas, ValidateFormulaHerb, CheckHerbReference, GetPendingValidation |
| Identity | BatchDelete/Enable/DisableUsers×3, AutoLogin, ChangePassword, ChangeProfile, Logout, RefreshToken, ResetPassword, RestoreUser, ToggleUserStatus, ValidateToken |
| Patients | BatchDeletePatients, DeletePatient, RestorePatient, TogglePatient, BatchCheckPatientReference, CheckPatientReference |
| Registration | CancelRegistration, CreateRegistration, StartVisit, GetRegistration, GetRegistrations, GetWaitingQueue |

**影响**：CommandHandler 是业务逻辑核心，缺少直接单元测试意味着：
1. 业务规则变更无法快速验证
2. 重构时缺乏安全网
3. 依赖集成测试（需要数据库）使 CI 缓慢

**建议**：优先为以下 Handler 补充单元测试：
- `CreateUserCommandHandler`（含层级校验）
- `LoginCommandHandler`（安全关键）
- `MedicalCaseCommandService`（核心业务流程）
- `BatchImport*CommandHandler`（数据量大、边界条件多）

---

### F2. CatalogController 963行 — 违反单一职责

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/CatalogController.cs`（963 行，34 个方法）  
**问题**：CatalogController 同时处理 Herb 和 Formula 两套 CRUD 操作（各约 15 个端点），导致：
1. 文件体量巨大（963 行），可读性差
2. Herb 方法和 Formula 方法模式高度重复（Create/Update/Delete/ToggleStatus/Restore/BatchDelete/Import/Export 每套 8 个方法）
3. 路由前缀混用（`/herbs/` 和 `/formulas/`）

```csharp
// Herb 和 Formula 方法几乎完全对称
public async Task<IActionResult> Create([FromBody] HerbInputDto input, ...) { ... }
public async Task<IActionResult> CreateFormula([FromBody] FormulaInputDto input, ...) { ... }
public async Task<IActionResult> Update(..., [FromBody] HerbInputDto input, ...) { ... }
public async Task<IActionResult> UpdateFormula(..., [FromBody] FormulaInputDto input, ...) { ... }
```

**建议**：拆分为 `HerbsController` 和 `FormulasController`，或至少将共用逻辑提取到基类/Service。

---

### F3. 过长方法 Top-3

| 文件 | 方法(估算) | 行数 |
|------|-----------|------|
| `CatalogController.cs:535` | `FormulaImportTemplate()` | ~55 行（含大量内联模板定义） |
| `TokenRefreshHandler.cs:447` | 令牌刷新逻辑 | ~74 行（含多层 try-catch） |
| `MedicalCaseCommandsViewModel.cs:421` | 命令组装 | ~51 行 |

**影响**：方法过长增加认知负担，降低可测试性。

---

## 🟡 中严重度

### F4. MasterDetailViewModel 基类设计优秀，但子类仍有重复

**文件**：5 个 MasterDetailViewModel（Formula 481行、Patient 466行、Herb 378行、User 369行、MedicalCase 305行）  
**评估**：`MasterDetailViewModelBase<TListItem, TDetail>`（305行）通过组合模式提取了列表/详情/分页/搜索/选中等功能，子类只需覆写 5 个抽象方法（`LoadListAsync`/`LoadDetailAsync`/`CreateNewDetail`/`SaveDetailAsync`/`DeleteItemAsync`）。**基类设计良好**。

**残余重复**：5 个子类仍有相似的模式：
- `InvalidateCachesAsync()` 调用 `_cacheManager.InvalidateAllAsync()`
- `RestoreItemAsync()` 的恢复逻辑
- `ToggleStatusAsync()` / `CanToggleStatus()` 几乎一致

```csharp
// 3 个子类中的相同模式
private async Task ToggleStatusAsync() {
    if (SelectedItem is not IEntityWithStatus statusItem) return;
    await _statusHandler.ToggleStatusAsync(statusItem);
    await InvalidateCachesAsync();
    await LoadListAsync();
}
private bool CanToggleStatus() => HasSelection && !IsBusy;
```

**建议**：将 ToggleStatus/CanToggleStatus 提取到 `MasterDetailViewModelBase` 的可选虚拟方法。

---

### F5. 命名规范：私有字段命名不一致

**文件**：`src/Server/Modules/LYBT.Module.Identity/Services/JwtService.cs:24`  
**问题**：该文件使用 `CurrentOptionsMonitor`（PascalCase）作为私有字段名，违反项目规范 `_camelCase`：

```csharp
// 违反规范：private readonly IOptionsMonitor<JwtOptions> CurrentOptionsMonitor;
// 应为：
private readonly IOptionsMonitor<JwtOptions> _currentOptionsMonitor;
```

**范围**：仅发现此 1 处明显违规，整体命名规范遵守良好。

---

### F6. 魔法数字 — 硬编码 URL 和端口

**文件**：多处散布  
**问题**：以下端口号和 URL 在代码中硬编码，而非通过配置/常量统一管理：

| 位置 | 硬编码值 | 建议 |
|------|---------|------|
| `ApplicationStateService.cs:65` | `"http://localhost:5000"` | 提取为 `ApiDefaults.RemoteBaseUrl` |
| `ConnectionSettingsService.cs:23` | `LocalUrlConstant = "http://localhost:5300"` | 已是常量 ✅（但分散在多处引用字符串字面量） |
| `StatusBarManager.cs:27` | `"http://127.0.0.1:5300"` | 统一使用 `ConnectionSettingsService.LocalUrlConstant` |
| `ConfigurationCenterViewModel.cs:134` | `"http://localhost:5300"` | 同上 |
| `Program.cs:296` | `"http://0.0.0.0:5000"` / `"https://0.0.0.0:5001"` | 仅模板默认值，可接受 |
| `LocalWebApiProgram.cs` | `"http://localhost:5300"` | 同上 |

**建议**：在 `SystemConstants` 中统一定义默认 URL 常量，各处引用常量。

---

### F7. 魔法数字 — 批量操作阈值

**文件**：`src/Server/Modules/*/Application/Commands/Batch*Handler.cs`  
**问题**：多个 Batch Handler 中硬编码了相同的阈值：

```csharp
const int MAX_IMPORT_SIZE = 10000;  // BatchImportHerbsCommandHandler:25
const int MAX_IMPORT_SIZE = 10000;  // BatchImportFormulasCommandHandler:25
const int MAX_IMPORT_SIZE = 10000;  // BatchImportPatientsCommandHandler:27
const int MaxBatchSize = 100;       // BatchDeleteUsersCommandHandler:29
```

**建议**：提取到 `SystemConstants`：

```csharp
public const int MaxImportSize = 10000;
public const int MaxBatchDeleteSize = 100;
```

---

### F8. 魔法数字 — UI 阈值和超时

**文件**：多处  
**问题**：多个文件中使用了未命名的魔法数字：

| 文件 | 魔法数字 | 应提取为 |
|------|---------|---------|
| `IToastService.cs:16` | `3000`（默认持续时间） | `DefaultToastDurationMs` |
| `LoadingOverlay.xaml.cs:19` | `200`（显示延迟） | `LoadingDisplayDelayMs` |
| `CardReaderService.cs:212-215` | `500` / `100`（读卡间隔） | `DefaultCardReadIntervalMs` / `MinCardReadIntervalMs` |
| `ApiHealthCheckService.cs:26` | `5000`（超时） | `DefaultHealthCheckTimeoutMs` |
| `LocalJwtConfig.cs:24` | `365`（Token 有效期天数） | `DefaultTokenExpirationDays` |
| `DiagnosticsController.cs:80` | `500`（count 限制） | `MaxDiagnosticLogCount` |

---

### F9. DRY 违反 — Batch Handler 高度相似

**文件**：`BatchEnableHerbsCommandHandler` / `BatchEnableFormulasCommandHandler` / `BatchDisableHerbsCommandHandler` / `BatchDisableFormulasCommandHandler`（各 21 行）  
**问题**：4 个 Handler 的实现完全对称，仅实体类型不同：

```csharp
// BatchEnableHerbsCommandHandler
public async Task<Result<BatchOperationResultDto>> Handle(BatchEnableHerbsCommand request, ...) {
    var ids = request.Ids;
    var items = await _context.Herbs.Where(h => ids.Contains(h.Id)).ToListAsync();
    foreach (var item in items) item.Status = CommonStatus.Active;
    await _context.SaveChangesAsync();
    return Result<BatchOperationResultDto>.Success(new BatchOperationResultDto { ... });
}
// BatchEnableFormulasCommandHandler — 几乎一模一样
```

**建议**：提取 `BatchStatusToggleHandler<TEntity>` 泛型基类，或使用 `CatalogEntityCommandHandlerBase`（已存在）统一。

---

### F10. DRY 违反 — Batch Delete 模式重复

**文件**：`BatchDeleteHerbsCommandHandler`（61行）/ `BatchDeleteFormulasCommandHandler`（39行）/ `BatchDeletePatientsCommandHandler`（61行）  
**问题**：三个 Handler 的软删除逻辑高度相似（查实体 → 设 IsDeleted=true → SaveChanges）。`BatchDeleteFormulasCommandHandler` 较短（39行 vs 61行），说明已有部分提取但不完整。

**建议**：统一到 `CatalogEntityCommandHandlerBase` 的通用批量操作方法。

---

### F11. 方法参数过多 — API 接口

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`  
**问题**：`QueryMedicalCasesAsync` 有 **8 个参数**：

```csharp
Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
    MedicalCaseQueryType queryType, Guid? patientId, Guid? doctorId, 
    string? keyword, int pageIndex, int pageSize, bool includeAllDoctors, int? limit);
```

**建议**：将查询参数封装为 `MedicalCaseQueryRequest` DTO，减少参数数量。

---

### F12. 注释质量 — 残留 TODO 未清理

**文件**：多处  
**问题**：以下 TODO 注释已存在较长时间，且部分引用了旧的需求编号：

| 位置 | 内容 | 建议 |
|------|------|------|
| `ReportsModule.cs:14` | `// TODO: 后续迭代完善报表功能` | 转为 Backlog Issue |
| `PrescriptionItemViewModel.cs:24` | `// TODO(P1-3 保留): 本类暂不迁移...` | 确认是否仍需保留 |
| `PrescriptionItemViewModel.cs:33` | `// TODO: 静态mapper与DI风格不一致` | 已有明确改造方案，转 Issue |
| `ClinicalHomeViewModel.cs:215` | `// TODO: US-SHELL-005 - 从服务获取今日统计数据` | 对应需求是否已实现？ |
| `PasswordHelper.cs:11` | `// TODO: 超大类型，建议拆分` | 已标记引用，确认是否已拆分 |

---

### F13. Public API 设计 — 路由前缀混用

**文件**：Server WebAPI Controllers  
**问题**：路由前缀使用了两种风格：

| Controller | 路由前缀 |
|-----------|---------|
| 多数 Controller | `api/v1/[controller]`（模板化） |
| CatalogController (Server) | `api/v1/herbs` + `api/v1/formulas`（硬编码两个前缀） |
| LocalWebAPI CatalogController | `api/v1/herbs`（硬编码） |

**评估**：CatalogController 硬编码路由是因为一个 Controller 对应两个资源，这本身是 F2（SRP 违反）的表现。拆分 Controller 后自然解决。

---

### F14. 过深嵌套 — CatalogController 模板定义

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/CatalogController.cs:78-87`  
**问题**：`HerbImportTemplate()` 方法内联了完整的模板 JSON 结构定义，嵌套深度达到 **8 层**：

```csharp
var template = new {  // depth 5
    Description = "...",
    Fields = new[] {  // depth 7
        new {  // depth 8
            Field = "Name",
            Required = true,
```

**建议**：将模板定义提取为静态 JSON 常量或嵌入资源文件。

---

## 🟢 低严重度

### F15. 命名规范 — 整体评估优秀

**评估**：
- ✅ 所有接口使用 `I` 前缀（0 违规）
- ✅ 公共方法使用 PascalCase
- ✅ 私有字段使用 `_camelCase`（仅 JwtService 1 处违规）
- ✅ 常量使用 PascalCase（如 `MAX_IMPORT_SIZE` → 应统一为 `MaxImportSize`）
- ✅ 局部常量使用 `const` + PascalCase（如 `const int MAX_IMPORT_SIZE` → 建议统一）

**小瑕疵**：部分 Batch Handler 中的 `MAX_IMPORT_SIZE` 使用全大写风格，与项目其他 `const` 的 PascalCase 不一致（如 `MaxPageSize`、`ApiTimeoutMilliseconds`）。

---

### F16. 魔法数字 — 部分已规范化

**正面示例**：
- `PerformanceMetric.cs`：阈值已定义为命名常量 `ExcellentThreshold = 500`、`GoodThreshold = 1500`、`AcceptableThreshold = 3000` ✅
- `SystemConstants.cs`：`MaxPageSize = 100`、`ApiTimeoutMilliseconds = 30000` ✅
- `ResponsiveLayoutHelper.cs`：`SmallScreenWidth = 1024`、`MediumScreenWidth = 1366`、`LargeScreenWidth = 1920` ✅

---

### F17. 字节格式化代码重复

**文件**：`PerformanceMetric.cs:70-71` 和 `BackupManagementViewModel.cs:154-156`  
**问题**：两处都硬编码了相同的字节→MB/GB/KB 格式化逻辑：

```csharp
>= 1024 * 1024 => $"{delta / (1024.0 * 1024.0):F2}MB",
>= 1024 => $"{delta / 1024.0:F2}KB",
```

**建议**：提取为共享的 `FileSizeFormatter.Format(long bytes)` 工具方法。

---

### F18. 异常处理模式 — Server 端规范

**评估**：Server 侧的异常处理整体规范：
- ✅ Controller 使用 `ControllerBaseExtensions` 的 `BusinessFail()` / `HandleResult()`
- ✅ Repository 层有异常日志记录（`FormulaRepository.cs:53-54`）
- ✅ 未发现空 catch 块（`catch { }`）
- ✅ 未发现吞掉异常的模式

---

## 测试覆盖评估

### 当前测试状况

| 项目 | 测试文件数 | [Fact] 数量 | [Theory] 数量 |
|------|-----------|------------|--------------|
| Architecture | 9 | — | — |
| Server | 90 | — | — |
| Desktop | 107 | — | — |
| **合计** | **206** | **1267** | **104** |

### 覆盖矩阵

| 层级 | 有测试 | 无直接测试 |
|------|--------|-----------|
| Architecture 守卫 | ✅ 9 文件 | — |
| Server Controller (路由/权限) | ✅ 多个 `*RoutesTests.cs` | — |
| Server CommandHandler | ⚠️ 仅 Login/CreateUser/DeleteUser | 🔴 35+ Handler 无直接测试 |
| Server QueryHandler | 🔴 全部 6 个无直接测试 | — |
| Server Service 层 | ⚠️ JwtService/SecurityAudit | 🔴 MedicalCaseCommandService/PrescriptionItemService 等 |
| Desktop ViewModel | ✅ 覆盖良好 | — |
| Desktop Foundation | ✅ 覆盖良好 | — |
| Shared 验证器 | ✅ 全部有测试 | — |

### 关键缺口

1. **Server CQRS Handler**：35+ CommandHandler 和 6 个 QueryHandler 无直接单元测试
2. **Server Service 层**：`MedicalCaseCommandService`（449行）、`MedicalCaseQueryService`（441行）、`PrescriptionItemService` 无直接测试
3. **Desktop 覆盖良好**：ViewModel、Foundation、Repositories 都有较好的单元测试覆盖

---

## 汇总

### 按优先级排序

| 优先级 | 编号 | 问题 | 影响 |
|--------|------|------|------|
| P1 | F1 | 35+ Server Handler 无单元测试 | 业务逻辑缺乏安全网 |
| P1 | F2 | CatalogController 963行 SRP 违反 | 可维护性差 |
| P2 | F9/F10 | Batch Handler DRY 违反 | 维护成本高、一致性风险 |
| P2 | F4 | MasterDetailViewModel 残余重复 | 低 |
| P2 | F6 | 硬编码 URL/端口分散 | 配置不一致风险 |
| P2 | F11 | API 参数过多 | API 使用不便 |
| P3 | F7/F8 | 魔法数字（批量阈值/UI 常量） | 可读性 |
| P3 | F5 | 命名规范 1 处违规 | 低 |
| P3 | F12 | TODO 注释未清理 | 信息过时 |
| P3 | F17 | 字节格式化重复 | DRY |
| P3 | F14 | 嵌套深度过深 | 可读性 |

### 正面评价

1. **命名规范整体优秀**：接口 I 前缀、PascalCase/camelCase 一致
2. **异常处理规范**：无空 catch 块、异常均有日志
3. **MasterDetailViewModelBase 基类设计良好**：组合模式提取了通用逻辑
4. **部分常量已规范化**：PerformanceMetric 阈值、SystemConstants、ResponsiveLayoutHelper
5. **Desktop 测试覆盖良好**：ViewModel、Foundation、Repositories 有全面的单元测试
6. **零 TODO/HACK/FIXME 泛滥**：仅 5 处 TODO，数量可控
