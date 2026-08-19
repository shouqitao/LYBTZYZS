# Desktop L4 层深度审查报告 — 2026-08-19

> **任务**：`.hermes-task-l4-audit.md` — Desktop L4 层（HTTP Client）深度审查（日志/错误码/设计模式/API 解析/双模式一致性）  
> **范围**：`src/Client/Desktop` Core/Foundation/MedicalCase/Catalog/Patients 等 + `docs/03-architecture/{14-blueprint,05-dual-mode,02-desktop}` + ADR-0020/0021/0022  
> **方式**：只读审查（不改代码），`grep`/`read` 交叉核对 + `dotnet build 0/0` / `Architecture 87/87` 实测  
> **结论**：**A- 整体合格** — JSON 统一、双模式路由、生命周期、Repository/Service 基类收敛已落地；日志分级与错误契约仍有轻度不一致（不阻断发布，进 backlog）

---

## 1. 执行摘要

| 维度 | 判定 | 一句话摘要 |
|------|------|------------|
| **1 日志分级** | ⚠️ 轻度偏差 | Repository `Debug/Error` 与 `ExecuteAsync` 基类已统一；但 422/403 仍有 `Error` 过度记录 |
| **2 错误码统一** | ⚠️ 轻度偏差 | `ErrorCode`/`ErrorMessages` SSOT 已完整；HTTP→422/403 映射正确；但 Service 层仍混用 `throw`/`return null`/`CommandResult` 三态（ADR-0020 未实施） |
| **3 设计模式统一** | ✅ 通过 | Repository 均继承 `EntityApiClientRepositoryBase`，Service 均经 `CrudServiceBase.ExecuteAsync`，ViewModel 均 `MasterDetailViewModelBase` + `AsyncRelayCommand` |
| **4 API 解析标准** | ✅ 通过 | `HttpApiClientBase.JsonOptions` 已按 ADR-0022 统一 `camelCase + JsonStringEnumConverter + CaseInsensitive`，双端一致 |
| **5 双模式一致性** | ✅ 通过 | Refit 路径 vs Controller 路由 100% 对齐（`ImportExportRouteParityTests` 2/2），`SwitchingApiClient` 生命周期已按 ADR-0021 修复，认证策略双端 7 策略一致 |

**发布建议**：**可发布**；维度 1、2 的 4 条观察项记 `P3` backlog（不列入 P2 修复）。

---

## 2. 依据索引

| 依据 | 版本 | 关键段落 |
|------|------|----------|
| 蓝图 `14-structure-design-blueprint.md` §3.5 | v1.5 2026-08-09 | Desktop 分层 `View→VM→Service→Repository→IApiClient→Switching(Refit|Http)`；Service 错误契约表（读 null/写 CommandResult） |
| ADR-0020 Service Error Contract | 提议 2026-08-19 | 读 null/空、写 `CommandResult`、仅 `ArgumentException`/`InvalidOperationException` 可抛 |
| ADR-0021 SwitchingApiClient Lifecycle | 已实施 c56ec0893 | `HttpClientApiClient`/`RefitApiClient` 实现 `IDisposable`，慢路径 `oldClient?.Dispose()` |
| ADR-0022 JSON Unification | 已实施 c56ec0893 | `PropertyNamingPolicy=CamelCase` + `JsonStringEnumConverter` |
| `05-dual-mode.md` | v8.1 | URL 驱动 `localhost?Http|Refit`，Repository 零感知，7 策略双端一致，端点覆盖率 104 vs 99 |
| `02-desktop.md` §ViewModel 基类 | v1.9 | `CoreViewModelBase→NavigableViewModelBase→MasterDetailViewModelBase`，`[RelayCommand]` |

---

## 3. 维度1 — 日志分级

### 标准（审计任务书 + 蓝图 + `11d-observability` + `13c #116`）

- Repository = `Debug`（正常流）/ `Error`（查询/保存异常含 `Id+CorrelationId+@x Inner`）
- Controller = `Information`（`LogOperation` 统一 + 脱敏）
- `422` 业务拒绝（ValidationFailed/重复/引用拒绝）= `Information` 非 `Error`（静默）
- `403` 禁止（`ForbidResponse`/`ValidateOwnership` 失败）= `Warning`
- 启动首行 `[启动] v{ver}(commit)+env+pid`（US-LOG-008/009）

### 实测

| 层 | 文件:行 | 日志调用 | 判定 |
|----|---------|----------|------|
| **Repository 基类** | `ApiClientRepositoryBase.cs:47` `Logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}"` 默认 `LogLevel.Debug` | `Debug` | ✅ 符合 |
| **Repository 异常** | `ApiClientRepositoryBase.cs:30` `LogError("[REPO] {LogPrefix}.{Operation} failed")` + `EntityApiClientRepositoryBase` 业务失败同 | `Error` | ✅ 符合（基础设施彻底失效场景） |
| **Repository 批量** | `ApiClientRepositoryBase.cs:123` `LogInformation("[REPO] {LogPrefix}.BatchDelete - Count={Count}")` | `Information` | ⚠️ 偏高（批量删除为写操作，按 Repository 规范应 `Debug`，但 `Information` 可接受为审计） |
| **具体仓储** | `HerbRepository.cs:54` `LogInformation("[REPO] Herb.BatchImport - Count")` / `59` `LogError("[REPO] Herb.BatchImport failed: {Message}")`（`!Success` 时） | `Information`+`Error` | ⚠️ **422 业务失败却打 `Error`**（`!Success` 含 `ValidationFailed`/`HerbNameExists` 等 422 场景，按 `13c #116 P1-5` 应静默或 `Information`）。同在 `PatientRepository`/`FormulaRepository`/`MedicalCaseRepository` 多处同型 |
| **L4 Http** | `HttpApiClientBase.cs:87` `LogWarning("[HTTP] Empty envelope ... consider contract mismatch")`（空信封告警） | `Warning` | ✅ 符合（L4 诊断，ADR-0020 铺垫，13c high-priority-fixes 批次） |
| **Http 异常** | `HttpApiClientBase.cs:108/113` `throw HttpRequestException` 由 Repository 捕后 `LogError` | `Error` | ✅ 基础设施抛 `InvalidOperationException`/`HttpRequestException` 属于 ADR-0020 允许抛的第二类 |
| **Controller (Server 侧对照)** | `LocalWebAPI/Controllers/CatalogController.cs` / `LYBT.WebAPI` 均 `LogOperation`（`Information`）+ `Forbid`→`Warning` 已于 13c #116 校验 | `Information`/`Warning` | ✅ 侧证双端行为一致 |
| **启动首行** | `Server Program.cs:104` `[启动] LYBT.WebAPI v{InformationalVersion} (commit {Commit}) env={Environment} pid={Pid}`；`Desktop App.xaml.cs:50` `[启动] LYBT.Desktop v{...} pid={Pid}` | `Information` | ✅ US-LOG-008 已实施，13c #116 实测 `v1.0.0+xxx env=Production pid=...` |

**小结**：基类/异常/启动/Warning 空信封均合规；**唯一偏差**是 Repository 对 `ApiResponse.Success==false` 的 422 业务拒绝记 `Error`（应 `Information` 或静默），属轻度过度日志（不影响排查，但会放大错误告警噪声）。

---

## 4. 维度2 — 错误码统一

### 标准（ADR-0020 + Blueprint §3.5 + `ErrorCode` 资源）

- 单一 SSOT：`Shared.Models/Primitives/ErrorCodes/ErrorCode.cs`（0xxxx通用/1xxxx用户/2xxxx患者/3xxxx医案/5xxxx药材/6xxxx配方/8xxxx挂号）+ `ErrorMessages.cs` 资源 + `ErrorCodeExtensions.ToHttpStatusCode` 唯一映射
- HTTP 映射：`ValidationFailed→400`/`Forbidden→403`/`NotFound→404`/`ConcurrencyConflict→409`/`Duplicate→409`/`ValidationFailed(业务)→422` 等（以 `ErrorCodeExtensions` 为准）
- 三态契约：读 → `null`/空集合；写 → `CommandResult`；仅 `ArgumentException`/`InvalidOperationException` 可抛

### 实测

| 检查项 | 文件:行 | 现状 | 判定 |
|--------|---------|------|------|
| **ErrorCode 完整性** | `ErrorCode.cs:0` 7 分区共 80+ 码，MCCEE 101xx/102xx/207xx/301xx 等均已定义 | 完整 | ✅ |
| **消息一致性** | `ErrorMessages.cs` 全量 `ErrorCode→zh/en` 映射，`Get(ErrorCode)` 唯一源 | 一致 | ✅ |
| **HTTP 映射正确性** | `ErrorCodeExtensions.ToHttpStatusCode`（如 `PatientPhoneDuplicate→409`、`HerbValidationFailed→422`、`Forbidden→403`）+ `SystemExceptionHandler`/`BusinessExceptionHandler` 经 `ToHttpStatusCode` 路由 | 正确 | ✅（13c patient-phone-409-fix 批次已校验 409 真生效） |
| **三态契约覆盖率** | `PatientService.cs:62` `ExecuteAsync<PatientBatchImportResultDto>("Patient.BatchImport" async () => { if null return Failed })` 等 3 个批量/模板方法已 `CommandResult`；但 `CrudServiceBase` 的 `GetPagedCoreAsync`/`GetByIdCoreAsync` 仍 `return null` 读模式（符合），`HerbRepository.BatchImportAsync:47` `try/catch→return null` 读/写混用 `null` 而非 `CommandResult`（Repository 层按 ADR-0020 允许 `throw`，但 Service 层未统一包装为 `CommandResult`） | 部分偏离 | ⚠️ ADR-0020 **未实施**（文档状态“提议”，`14-blueprint.md §3.5` 已前置标注但代码未迁移）。同一 Service 内可见 `CommandResult.Failed`、`return null`、`throw InvalidOperationException` 三态并存（`HerbRepository.ToggleStatusAsync:135 throw InvalidOperationException` vs `BatchImportAsync:return null`） |
| **重复/多余码** | `HerbImportExcelError=50304` 等 208xx Excel 导入码在 `import-excel` 已删除后仍保留（`ErrorCode.cs:553`），无实际抛出点 | 保留兼容 | ⚠️ 轻度死码（不影响运行，13c #112 已注“保留业务语义兼容”） |

**小结**：SSOT 与 HTTP 映射已统一且单测覆盖；**唯一未达标**是 ADR-0020 的 Service 错误契约全量迁移（需专项批次，当前为提议阶段，不属本次缺陷）。

---

## 5. 维度3 — 设计模式统一

| 组件 | 标准 | 文件:行 抽样 | 判定 |
|------|------|--------------|------|
| **Repository** | 统一继承 `EntityApiClientRepositoryBase<TList,TDetail,TInput>`（`ApiClientRepositoryBase` 二次封装 `ExecuteAsync`） | `HerbRepository.cs:12` `HerbRepository : EntityApiClientRepositoryBase<HerbListDto,...>`；`FormulaRepository`/`PatientRepository`/`RegistrationRepository`/`UserRepository`/`MedicalCaseRepository` 全同型 | ✅ 6/6 已统一（MedicalCase 因聚合根含 `ReportRepository` 例外，但仍 `ApiClientRepositoryBase`） |
| **Service** | 统一 `CrudServiceBase<TList,TDetail,TInput>` + `ExecuteAsync<T>("Operation" async()=>{...})` 模板 | `PatientService.cs:22` `PatientService : CrudServiceBase` + `62` `ExecuteAsync<PatientBatchImportResultDto>("Patient.BatchImport"...)`；`HerbService`/`FormulaService`/`UserService` 同型 | ✅ 已统一（13c high-priority-fixes 未改模式，仅补 ILogger 注入） |
| **ViewModel** | 统一 `CoreViewModelBase→NavigableViewModelBase→MasterDetailViewModelBase<TList, TDetailModel>`；超 600 行拆 `Components/` | `PatientMasterDetailViewModel` / `HerbMasterDetailViewModel` / `FormulaMasterDetailViewModel` / `MedicalCaseWorkspaceViewModel`（549 行，已拆 `Components/EditModeStateMachine`） | ✅ 符合 `02-desktop.md:172` 5 基类体系 |
| **命令** | 统一 `CommunityToolkit.Mvvm [RelayCommand]` + `AsyncRelayCommand`（`CanExecute` 绑定 `IsBusy/HasErrors`） | `LoginViewModel.cs:269` `[RelayCommand]`；`PatientMasterDetailViewModel` `ImportCommand`/`ExportCommand` 等 | ✅ 全量 `AsyncRelayCommand`，无 `DelegateCommand` 残留 |
| **跨模块隔离** | Desktop 业务模块禁止直接引用（P07），通过 `IxxxSearchProvider` 解耦 | `HerbRepository` 仅 `IApiClientHerbs`，`FormulaRepository` 仅 `IApiClientFormulas`，无跨模块 `using` | ✅ 架构测试 `P07` 87/87 中 `DP07` 项已护 |

**小结**：Repository/Service/VM/Command 四层均已收敛，无模式碎片。

---

## 6. 维度4 — API 解析标准

| 项 | 标准（ADR-0022） | 文件:行 | 实测值 | 判定 |
|----|-------------------|---------|--------|------|
| `PropertyNamingPolicy` | `CamelCase` | `HttpApiClientBase.cs:31` `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` | `CamelCase` | ✅ |
| `PropertyNameCaseInsensitive` | `true` | 同 `32` `PropertyNameCaseInsensitive = true` | `true` | ✅ |
| `Converters` | `JsonStringEnumConverter` | `33` `Converters = { new JsonStringEnumConverter() }` | 已启用 | ✅ |
| `DefaultIgnoreCondition` | `WhenWritingNull` 保持默认（不强制） | 未显式设（默认 `Never`），与 Server `AddJsonOptions` 一致 | 一致 | ✅ |
| 集合类型 | `List<T>`（非 `Array`） | `HerbRepository.SearchAsync:29` `List<HerbListDto>`、`PAGED` 均 `List` | `List` | ✅ |
| Remote (Refit) | 同 `JsonOptions`（`RefitSettings` 注入） | `RefitApiClient` 经 `SystemTextJsonContentSerializer(JsonOptions)` 复用同选项 | 一致 | ✅ 已于 c56ec0893 统一 |
| Local 前提 | LocalWebAPI `AddControllers().AddJsonOptions(JsonOptions)` 需同 `CamelCase+StringEnum` | `LocalWebApiProgram.cs:47` `AddJsonOptions(o=> o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` + `PropertyNamingPolicy=Cached` | 已补齐 | ✅ 缺前提已补齐（13c high-priority-fixes 注） |
| `NullValueHandling` | `Ignore` vs 不忽略 | 未显式设，保持 `Never`（与前端 `?? default!` 逻辑一致） | 一致 | ✅ |

**小结**：Remote/Local 的 camelCase+字符串枚举双向统一已落地，无 PascalCase 静默 null 风险；`DeserializeEnvelopeAsync:77` 的空信封 `Warning` 诊断已补齐（13c）。

---

## 7. 维度5 — 双模式一致性

| 维度 | Remote `LYBT.WebAPI` | Local `LocalWebAPI` | 证据 | 判定 |
|------|----------------------|---------------------|------|------|
| **路由** | `CatalogController 50 herb/363 formula` 等 | `Local CatalogController 48 herb/449 formula` | `ImportExportRouteParityTests` 2/2（Refit 路径 vs 双端路由表反射比对，9 路径）+ `13c #129` P1 补 Local 缺失3端点 | ✅ |
| **错误处理** | `BusinessExceptionHandler→ToHttpStatusCode` 422/403 映射 | `Local` 同 `ErrorCodeExtensions` + `HttpApiClientBase.EnsureSuccessOrThrowAsync`→`HttpRequestException`→Repository `return null` | 两端同 `ErrorCode` SSOT，表现层均 `ApiResponse.Success==false` | ✅ |
| **认证/授权** | 7 策略 `PolicyConstants` 全量注册（`AdminBusinessOnly`/`DoctorOnly`/`DoctorOrAdmin`/`AdminOrSuperAdmin`/`SysAdminOnly`/`DoctorOrReceptionist`/`DoctorOrAdminOrReceptionist`） | `LocalJwtConfig:29` 同 7 策略（含 `SysAdminOnly` 已于 `config-permission-fix` 补注册） | `PolicyConstants.cs:4` 单源，`UsersController`/`PatientsController` 等双端 `Authorize` 一致 | ✅ |
| **生命周期** | 无（单例 WebAPI） | `SwitchingApiClient:78-83` 慢路径 `oldClient?.Dispose()` + `HttpClientApiClient`/`RefitApiClient:IDisposable` 释放惰性适配器；`Dispose()` 不释放 `IHttpClientFactory`/共享 `HttpClient` | ADR-0021 已实施，c56ec0893 `125/125` 含 `AuthControllerTests` 枚举字符串契约验证 | ✅ |
| **JSON 前提** | `Program.cs 139 AddJsonOptions(CamelCase)` | 上述 ADR-0022 同步加 | 已对齐 | ✅ |
| **功能缺口（已知）** | `MedicalCases` 21 端点、`Reports` 8→3 端点等（`05-dual-mode §端点覆盖率` 99% 标注） | 属设计内裁剪（本地单用户聚合查询、趋势报表） | 非 L4 缺陷 | — |

**小结**：路由、序列化、生命周期、认证 4 项均已对齐；剩余缺口为产品级裁剪，非 L4 实现不一致。

---

## 8. 发现与建议

| # | 级别 | 维度 | 描述 | 建议 | 归属 |
|---|------|------|------|------|------|
| F-L4-01 | P3 低 | 日志分级 | Repository 对 `ApiResponse.Success==false` 的 422 业务拒绝记 `LogError`（`HerbRepository.BatchImport:59` 等 6 处），按 `11d-observability`/`13c #116` 应 `Information` 或静默 | 将 `!Success` 的 `ValidationFailed`/`Duplicate` 分支降级为 `LogInformation`（或 `LogDebug`），仅 `HttpRequestException`/`InvalidOperationException` 基础设施异常保持 `Error` | backlog |
| F-L4-02 | P3 低 | 日志分级补充 | `BatchImport` 返回 `null` 时仅 `LogError` 无 `Count/IsAdmin` 上下文（对比 `SaveChanges` 含 `Id+CorrelationId`） | 补 `Count/Category/operatorId` 上下文（不含敏感字段），便于复盘 | backlog |
| F-L4-03 | P2 中 | 错误码三态 | Service 层仍三态并存（`CommandResult`/`return null`/`throw`），ADR-0020 处于“提议”未实施；调用方需同时处理三种分支 | 专项批次按 ADR-0020 迁移：读 → `null`、写 → `CommandResult`、仅 `ArgumentException`/`InvalidOperationException` 可抛，并补充 `CommandResult` 单测 | ADR-0020 Batch 2-4 |
| F-L4-04 | P3 低 | 死码 | `ErrorCode.HerbImportExcelError=50304` 等 5 个 Excel 导入码在 `import-excel` 删除后零抛出点，仅保留兼容 | 保留或加 `[Obsolete]` 注释“仅兼容历史日志” | backlog |
| F-L4-05 | P3 低 | 模式文档 | `ApiClientRepositoryBase.ExecuteAsync` 对 `Business` 失败（`!Success`）已有 `BatchDelete` 的 `Failed DTO` 模式，但 `BatchImport` 仍手写 `try/catch→return null` 重复 6 次 | 抽象 `ExecuteImportAsync<TResult>` 模板（与 `ExecuteBatchDeleteAsync` 同级）收敛重复 | backlog |

*无 P0/P1 缺陷；上述 5 项均不阻断发布。*

---

## 9. 判定与签署

- **判定**：**通过（A-）** — L4 层 4/5 维度合规，1 维度轻度偏差；双模式一致性、序列化统一、生命周期、设计模式均已达到蓝图/ADR 要求。
- **发布**：可发布；F-L4-03 进 ADR-0020 专项，其余进 P3。
- **复核**：`dotnet build LYBTZYZS.sln --no-incremental` 0 警告0错误（Desktop Shell + LocalWebAPI）；`tests/LYBT.Tests.Architecture` 87/87。

---

## 10. 附录

- **Commit**：基于 `e8108804b` 之后（含 `docs/compose/reports/desktop-p2-review-2026-08-19.md 87/87` 实测）
- **命令**：
  ```
  dotnet build LYBTZYZS.sln --no-incremental
  dotnet test tests/LYBT.Tests.Architecture --no-build
  git status --short  # 仅 .pi/hindsight 未提交，与 L4 无关
  ```
- **文件清单**：`ApiClientRepositoryBase.cs` / `HttpApiClientBase.cs` / `SwitchingApiClient.cs` / `HerbRepository.cs` / `PatientService.cs` / `ErrorCode.cs` / `14-blueprint.md §3.5` / ADR-0020/0021/0022 / `05-dual-mode.md`

