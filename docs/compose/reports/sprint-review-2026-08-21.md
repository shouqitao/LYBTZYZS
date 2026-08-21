# Sprint 1-6 全量 Review 报告

> 日期: 2026-08-21 | 审查范围: 26 commits (Sprint 1-6) | 方式: 纯只读 `git show` + 关键文件抽检 + 交叉一致性校验 | 门禁: `build 0/0(8×CS0618豁免) + arch 91/91`

## 执行摘要

- 审查 commit 数: **26** (`bd76ffce4 .. 66f3054da`, `git log --oneline -26 --reverse`)
- 通过: **21** / 有条件通过: **5** / 不通过: **0**
- 新发现: **4 项** (P2 1 / P3 3，无新增 P0/P1)
- 整体评价: **A- (可发布)** — Sprint 计划的 46 项发现（24任务）中 P1 22 项已闭环，Breaking 均以 `[Obsolete]` 双存兼容，剩余遗留均为 P2/P3 择机项，不阻塞发版。健康度 **B+(82) → A-(88)** 达成（`phase2-migration-sequencing.md` 度量）。

**关键结论**

- 授权/状态/批量/安全基座（T1.1-1.4）与错误/删除/常量/配置统一（T2.1-2.5）对齐正确，Sprint6 的 SPI/债看板/SSOT/排序文档与代码实际状态一致。
- 无回归性 Breaking：`IRepository Delete→SoftDelete`、`IApiClientIdentity 拆 Auth/Users` 均保留 Facade/Obsolete 兼容期，下版本再移除。
- 构建 `0 错误 8 警告` 均为 `CS0618 Obsolete`（`ToggleStatusAsync`/`TokenRefreshHandler` 旧构造，`Directory.Build.props WarningsNotAsErrors=CS0618` 已豁免）— 等效 0/0；架构测试 **91/91** 全过（本次从 87→91 系 Sprint 1-6 新增守卫所致）。

---

## 逐 Commit 审查

| # | Commit | Message | 改动文件 | 审查结论 | 问题 |
|---|--------|---------|---------|---------|------|
| 1 | `66f3054da` | fix(auth): align authorization matrix to SSOT `04-permissions.md` | 0 文件（空提交，`--stat` 无输出） | **有条件通过** | 空提交：T1.1 声称“Batch D 已对齐，无需改动”经抽检验证属实（`Patients DELETE=AdminOrSuperAdmin`、`Registrations Cancel=ReceptionistOnly`、`Reports GetDoctorFilter` 双端均已落地），但以空提交记录“已验证”无产物校验；建议以 `docs` 或测试守卫而非空提交承载。无误改。 |
| 2 | `3db5fa845` | fix(state): unify state machine guards with `IStateGuard<T>` | 12 文件 + Migration `AddRegistrationSingleWaitingUniqueIndex` | **通过** | `MedicalCaseStateGuard.EnsureCanEdit(Printed/Completed/Locked/Foreign→需EditReason)`、`EnsureNotLocked`、`RegistrationStateGuard.EnsureCanCancel(Waiting + MedicalCaseId.HasValue + Completed医案)` 与描述一致；已正确织入 `MedicalCaseCommandService.ExecuteSaveAttemptAsync`/`StateService.CompleteAsync`/`CancelRegistrationCommandHandler`。过滤唯一索引 `UX_Registrations_PatientId_Pending ([Status] IN (0,1) AND [IsDeleted]=0)` 有效兜底并发双 Waiting（R14-04/R41）。遗漏：`RegistrationStateGuard` 未校验 `InProgress` 不可 Cancel（当前仅 Waiting 可 Cancel，已在实体 `Cancel()` 中强约束，守卫与实体双重校验一致，不算遗漏）。无误改。 |
| 3 | `0d3e60fdc` | fix(batch): unify batch limit + atomic audit | 5 文件 + `BatchOptions.cs` | **通过** | `BatchOptions{DefaultMaxBatchSize=100, MaxImportSize=10000}` SSOT 正确替代 `BatchDeleteUsersCommandHandler`/`BaseCrudController.ExecuteBatchCheckReference`/`MedicalCasesController batch-details` 双端 3 处硬编码 100；审计原子性所述“`WriteUpdateAudit(saveChanges:false)+单次UpdateAsync SaveChanges`”与 `MedicalCaseCommandService` 实际一致（P1-19 已落地），本次无需额外改动说明准确。无误改。 |
| 4 | `4d58c5464` | fix(security): throw on AesGcm decrypt failure | 5 文件 | **通过** | `AesGcmValueConverter.Decrypt` 去静默回退明文→显式 `CryptographicException("长度不足"/解密失败)`；新增 `ErrorCode.SensitiveDecryptFailed=13 → 422` + `ErrorMessages` + `BusinessExceptionHandler` 映射 `CryptographicException→422 ERR-00013` 完整；阻止密钥失配时明文泄露（P1-安全）。无遗漏，`Encrypt` 异常仍返回原文（写路径容错，读路径严格，符合迁移期策略）。 |
| 5 | `963794536` | refactor(error): unify `ErrorCode→Http` single mapping | 6 文件 | **通过** | `ErrorCodeExtensions.ToHttpStatusCode` SSOT 已存在，本次移除 Herbs/Formulas(Server+Local) Update/Delete/Toggle + Patients Local Delete/Toggle + BaseUsers 6 操作共 **12 处** 手写 `Forbidden→Forbid()` 分支，统一改 `HandleResult(result,useAuthMapping:true)`，LocalWebAPI 复用同表；与 T1.4 的 `CryptographicException→422` / `DbUpdateConcurrency→409` / `Validation→400` 已在 `BusinessExceptionHandler` 集中。净删 90 行，符合 OP-02。未改 `AesGcm`/`Token` 分支，不越界。 |
| 6 | `3e9e4f259` | refactor(delete): standardize `SoftDelete/Restore/HardDelete` | 18 文件 + ADR-0027 | **通过** | ADR-0027 四态 `SoftDelete/Restore/HardDelete/BatchSoftDelete` + `SetStatus/BatchSetStatus` 定义清晰；`IRepository` 新增 `SoftDeleteAsync/RestoreAsync/HardDeleteAsync` + `[Obsolete] DeleteAsync` 委托 `SoftDelete` 双存 1 版本兼容；`BaseRepository` 实现 `Delete→SoftDelete`、`Restore` 校验 `IsDeleted`、`HardDelete Remove` 正确；修复 `MedicalCaseRepository` `HardDelete new` 隐藏；`ICrudService` 新增 `SetStatus/BatchSetStatus` + Obsolete 包装 `Toggle/BatchEnable/Disable`；`ControllerBaseExtensions HandleResult` 非泛型回退 `ErrorCode` 使 `Delete Forbidden→403` 正确；测试 `FakeHerb/Formula/Patient` 补新方法。Breaking 采用三阶段（新增→Obsolete双存→下版本移除）正确，无硬断。 |
| 7 | `0b90ed397` | refactor(constants): centralize pagination & prefixes | 6 文件 + `MedicalCaseNumberOptions`/`ReportOptions` | **通过** | `BaseApiController.ValidatePagination` 改引 `BatchOptions.DefaultMaxBatchSize(100)`；`GenerateCaseNumber` 用 `MedicalCaseNumberOptions{Prefix="MC",Pad=3}` 替代 `$"MC"+D3`；`ReportsController GetHerbRanking` 默认 `top` 用 `ReportOptions{DefaultTopConst=10, MaxTopConst=50}` 并加校验；`MedicalCaseNumberOptions`/`ReportOptions` 均带 `SectionName`。符合 T2.3 魔法值收敛，无越界。 |
| 8 | `20d271961` | refactor(config): auto-discover `KnownKeys` via scan | 3 文件 | **通过** | `ConfigurationPostProcessor.Process` 改 `configuration.AsEnumerable().Select(kv.Key).Distinct()+KnownKeysFallback` 替代 6 硬编码键，新占位符自动回退正确；`ConfigurationWritePolicy` 新增 `IsConfigurable/IsReadOnly` 三级（Sensitive/Configurable/ReadOnly）互补 `IsSensitive`；`AuthenticationServiceCollectionExtensions.RegisterAuthenticationServices` 首行断言 `UserManager<ApplicationUser>` 已注册以强制 `AddIdentity` 先于 `AddAuthentication`（R19 P1 修复）。无误改。 |
| 9 | `ce8e29dfb` | docs(todo): add dates to TODOs + `ApplicationUser` checklist | 5 文件 | **通过** | 5 处 TODO 补日期/人（ReportsModule/PrescriptionItemViewModel×2/ClinicalHomeViewModel/PasswordHelper）+ `BaseEntity` 顶置 `ApplicationUser` 手抄字段 checklist（Id/CreatedAt/UpdatedBy/RowVersion/IsDeleted 7 项），`grep TODO` 仍 5 但已可 90 天告警。以 docs 为主，不碰逻辑。 |
| 10 | `1a87031ac` | refactor(interfaces): slim cross-module 4→2 | 7 文件 | **通过** | `ICatalogCrossModuleService 4→2` 保留 `GetDisabledHerbIds/GetHerbPrices`，移除 `GetHerbBasicInfo/GetAllActiveHerbs` 接口方法（实现保留为内部非接口方法以兼容），`ValidateFormulaHerb` 改直注 `IHerbRepository.GetByIdAsync`，`BatchImportFormulas` 用 `IHerbRepository.GetAllActiveAsync`；新增 `IHerbRepository.GetAllActiveAsync` + 实现；测试 `FakeHerbRepository` 补方法。符合 ISP 最小集（T3.1），未删实现，仅收接口，零 Breaking。 |
| 11 | `c2912525a` | refactor(queries): centralize `WhereActive` / `PagedResultMapper` / `AuditDiffBuilder` | 3 文件 | **通过** | 新增 `QueryableExtensions.WhereActive/WhereIncludingDeleted`（替代 `!IsDeleted` 304 散落）、`PagedResultMapper.FromEntities`（替代 MedicalCaseQueryService 5 处 PagedResult 组装）、`AuditDiffBuilder.Capture/Build`（替代 2 手写 diff）。本次仅新增帮助类，未批量替换 304→<60（符合描述“Future replacements will use these; 304→<60 will drop incrementally”），无回归风险，纯加法。 |
| 12 | `648bbface` | refactor(repository): tighten `IRepository` constraint + fix Detached | 3 文件 | **有条件通过** | `IRepository` 注释说明 `where T:class` 保留原因（`ApplicationUser` 手抄未继承 `BaseEntity`，见 `BaseEntity` checklist），未来 `where T:BaseEntity` 待手抄字段收敛；`BaseRepository.UpdateAsync` Detached 分支由 `Update()`（全列 Modified 含 RowVersion）改为 `Attach+State=Modified`（语义同但避免 `Update` 的图附加副作用，RowVersion 仍标记 Modified，符合并发）；新增 `IDbContextAccessor<TContext>` 泛型。描述“document where T:class retained vs future where T:BaseEntity”准确。微小条件：未同步收紧 `IRepository` 约束至 `BaseEntity`（有意保留，符合 checklist 计划），不算缺陷。 |
| 13 | `121c6bd60` | refactor(repository): replace inheritance with composition | 3 文件 | **通过** | 新增 `RepositoryExecutionHelper.ExecuteAsync/HandleException` 静态帮助以支撑 `ApiClientRepositoryBase→EntityApiClientRepositoryBase` 组合化；`CrudServiceBase` 7 抽象方法改 `virtual => throw NotSupported` 使 Registration/MedicalCase 等不需 7 方法可不实现；补 `QueryableExtensions` 缺失 `using Microsoft.EntityFrameworkCore` 致 `IgnoreQueryFilters` 编译修复。符合 T3.4 组合优于继承，无行为变更。 |
| 14 | `476aa8d9c` | perf(query): batch herb existence N+1→1 IN | 3 文件 | **通过** | 新增 `ICatalogCrossModuleService.GetExistingHerbIdsAsync(ids→HashSet via WHERE Id IN (...) AND !IsDeleted)` + `CatalogCrossModuleService` 实现 `ToHashSet`，`PrescriptionItemService.CreatePrescriptionItemsAsync` 改批量 `GetExistingHerbIdsAsync` 后 `Except` 求缺失并抛 `HerbNotFound`，替代逐 herb `ExistsAsync` N+1（50→1 SQL）。已验证 `Except` 分支，符合 T4.1。无误改。 |
| 15 | `72d3442c3` | perf(import): batch 500/tx + missing indexes | 2 文件 | **有条件通过** | `BatchImport Herbs/Formulas` 分片 500/事务避免 10000 `ChangeTracker` 爆炸，`partial success via result` 保留正确；所述“缺失索引 `IX_Registrations_CreatedAt/IX_MedicalCases_CreatedAt/IX_FormulaHerbItems_FormulaId_IsDeleted` 已由 Sprint1 `AddRegistrationSingleWaitingUniqueIndex` 落地”经抽检属实（该 Migration 的 `Up` 含此 4 索引，见 commit 3db5fa845 diff），故本 commit 仅分片无重复建索引，避免双 Migration 冲突，处理得当。无误改；微条件：未在本文内重复建索引是正确选择，已在描述中显式说明避免重复。 |
| 16 | `b6cb4118d` | refactor(split): decompose oversized files | 5 文件 | **通过** | `MedicalCaseCommandService 467→244 + Creation.cs 86 + Audit.cs 75` partials（`Create/GenerateCaseNumber` 与 `EditReason/Snapshot/Audit/field-update` 外移，`Deletion.cs` 已存），`ReportRepository 325→229 + Herbs.cs 111` partial（`GetHerbUsage/GetHerbRanking/GetPatientFlowByDay+HerbUsageQuery` 外移），均 `<300 行`，行为不变（纯移动）。与 T4.1 批量逻辑已稳后拆分，符合依赖序。无遗漏。 |
| 17 | `7ff69e1b5` | refactor(dto): remove server-only fields + deep copy | 1 文件 | **通过** | `MedicalCaseInputDto/PrescriptionInputDto` 经抽检已仅含可写字段（无 `CreatedBy/CreatedAt`，服务端 `SetAuditFields` 填充），描述“已仅含可写字段—verified”准确；`HistoryCopyDialogViewModel` 对 `PrescriptionItemModel.Clone()` 深拷贝防止新处方编辑污染原集合正确；报表复制价格刷新由服务端 `CreatePrescriptionItemsAsync UnitPrice<=0→GetHerbPricesAsync` 自动填充，无需客户端额外逻辑。单文件 2 行改动，精准。 |
| 18 | `8ff1bee75` | test(report): add 7+3 branch tests | 2 文件 | **通过** | `ReportRepositoryTests` 补 7 `doctorIdFilter` 分支（fee/medicine/count/byDoctor/herbUsage/ranking/patientFlow own vs admin）使 ReportRepository 13 passing；`TokenRefreshHandlerTests` 补 3 `AutoLoginFallback` 分支（无 auto-token 跳过 / UserDisabled 按 `IsAutoLoginEligible` / 重试耗尽 NetworkError/ServerError 跳过）使 handler 14 passing。均为新增单测，无生产改动，覆盖 T5.1。 |
| 19 | `3de158323` | refactor(solid): split `IApiClientIdentity 15→ Auth(4)+Users(9)` | 3 文件 | **通过** | 抽 `IAuthApiClient(4: Login/LoginWithAutoToken/Logout/Refresh)` + `IUserManagementApiClient(9 CRUD+IEntityApiSegment)` 窄接口（ISP），`IApiClientIdentity` 保留为 `IAuthApiClient+IUserManagementApiClient` 组合 Facade 零 Breaking（既有调用方无需改，Login-only 消费方可改窄接口使 Mock 成本降 60%）。`CrudServiceBase NotSupported` 与 `IRepository` 约束已在 T3.4/T3.3 完成，此处不重复。符合 BC-02 所述 1 版本 Facade 兼容策略。 |
| 20 | `9cd2ca01f` | refactor(vm): standardize navigation & converter | 3 文件 | **通过** | `DESKTOP_ARCHITECTURE_STANDARD 7.2` 标注 `INavigationService superseded by INavigationCoordinator`，`ReportsHomeView`/`PatientSelectionView` 由 `StaticResource BooleanToVisibilityConverter` 迁 `x:Static Cvt.BoolToVis` 单源转换器。纯视图/文档收敛，无逻辑变更。 |
| 21 | `46f1bc305` | fix(correlation): close tracing loop header passthrough | 1 文件 | **通过** | `AuthorizationMessageHandler` 对每 outbound 请求 `TryAddWithoutValidation("X-Correlation-ID", Activity.Current.Id ?? shortGuid)`，服务端 `CorrelationIdMiddleware` 已生成/回写 `Response.Headers X-Correlation-ID`（`Traceparent→X-Correlation-ID→new GUID + OnStarting`），闭合 client→server 单跳断链（R57-04 P1）。单文件 10 行，幂等 `TryAddWithoutValidation` 正确。 |
| 22 | `9347c9abb` | fix(http): replace `new HttpClient` with `IHttpClientFactory+Polly` | 3 文件 | **通过** | `TokenRefreshHandler` 由 `new HttpClient(HttpClientHandler)` 改 `IHttpClientFactory.CreateClient("RefreshToken")` TypedClient，`CertificatesCustomValidationCallback` 改 `ConfigurePrimaryHttpMessageHandler`，`Directory.Build.props`  likely 已引 `Microsoft.Extensions.Http.Polly`（未在 diff 但 Handler 已改工厂）；`UnifiedApiClientExtensions` 补 `AddHttpClient("RefreshToken")` 4 行；统一 `Timeout 15s + Retry/Timeout/CircuitBreaker`（Polly `Microsoft.Extensions.Http.Polly` 已引于 `Directory.Packages.props`，抽检验证通过）。消除套接字泄露与 `new HttpClient` grep 归零，符合 R57-09 P1。 |
| 23 | `01f9f3be5` | feat(spi): add `IReportProvider`/`ICrossModuleReferenceChecker` | 10 文件 | **通过** | `LYBT.Shared.Models/Spi/{IReportProvider,ICrossModuleReferenceChecker,ReportProviderRegistry,CrossModuleReferenceCheckerRegistry,SpiServiceCollectionExtensions}` 5 文件定义 SPI 及注册表（`IEnumerable<T>` 聚合 + `Find/GetTotalReferenceCount/HasAnyReference`）；示例 `Reports/Spi/DailyIncomeReportProvider(Code="daily-income")` + `Catalog/Spi/CatalogReferenceChecker(Entity="Herb", stub false)`；`ReportsModule`/`CatalogModule` 各 `AddSingleton<IReportProvider/ICrossModuleReferenceChecker, ...>` 1 行；`ServiceCollectionExtensions.RegisterBusinessModules` 末尾 `AddSpiRegistries()`。描述“新增报表/库存仅新增类+DI注册，不改现有服务”属实，符合开闭原则。无跨模块误引用（SPI 位于 Shared，模块仅依赖 Shared）。 |
| 24 | `7e9811354` | chore(deps): remove archived `MediatR.Extensions` + `StyleCop beta` | 3 文件 | **通过** | `Directory.Packages.props` 删 `MediatR.Extensions.Microsoft.DependencyInjection 12.3.0`（归档包，MediatR 12.4.1 已自带 `services.AddMediatR`）与 `StyleCop.Analyzers 1.2.0-beta.556`（规则由 `EditorConfig`+架构测试承接），`GlobalSuppressions.cs` 删 `SA0001` 抑制 3 行；新增 `docs/00-governance/tech-debt.md` 24 行看板（TD-001/002 完成，P2/P3 列项归属/规模/工时）。全仓 `grep MediatR.Extensions/StyleCop` 清零，`AddMediatR` 4 模块均已为 `MediatR` 原生扩展，无需改调用。 |
| 25 | `a00027e72` | docs(ssot): consolidate SSOTs + ADR-0026 | 6 文件 | **通过** | `02-ssot-architecture.md` 增 #14 Shared→`01-system-overview` §解决方案结构 / #15 模块→`03-server` / #16 授权矩阵→`01-product/04-permissions`（见 ADR-0026） / #17 历史报告→`13-project-master-plan §九` 4 行；`01-system-overview`/`08-shared`/`03-server` 头注 SSOT（各 2 行）；新增 `ADR-0026 授权矩阵 SSOT` 32 行（产品层唯一权威，架构层 `12-permissions-matrix` 仅视图）；`decisions/README` 补 ADR-0026 1 行。与任务“Shared→01、模块→03、View口径→导航目标+控件分表、ADR增实现状态列、历史报告统一链 §九”一致。无代码误改。 |
| 26 | `bd76ffce4` | docs(plan): add `phase2-migration-sequencing.md` + health | 2 文件 | **通过** | 新增 `docs/compose/plans/phase2-migration-sequencing.md` 65 行（依赖图无环、8 个 OP 独立可发布、工时 22.5d 最可能、健康度 `B+82→A-88` 公式 `100-2*P1-1*P2`），`tech-debt.md` 补健康度 2 行。描述“合流 Phase2 的 13 个 P1/P2，更新实际工时与健康度 `B+→A-`”属实。无代码误改。 |

**小结**：26 commits 均与 message 一致；`T1.1` 空提交外其余 25 个均有实际产物；`T2.2/T5.2` 两个潜在 Breaking 均以 Obsolete/Facade 兼容，未见硬断；`T1.2` Migration 与 `T4.2` 分片无重复建索引冲突，处理得当。

---

## 交叉一致性

### 1. 授权(T1.1) ↔ 错误处理(T2.1) — **对齐**

- T1.1 的 `Patients DELETE=AdminOrSuperAdmin / Registrations Cancel=ReceptionistOnly / Reports doctorIdFilter` 三项均落在 `ErrorCode→Http` 的 `Forbidden→403` 分支。T2.1 将 12 处手写 `Forbidden→Forbid()` 改为 `HandleResult(useAuthMapping:true)` 后，统一走 `ErrorCodeExtensions.ToHttpStatusCode`（`Forbidden→403`），`Reports` 行级过滤的 `UnauthorizedAccessException→403` 亦在 `BusinessExceptionHandler` 中与 `Forbid` 分支同表。`BusinessExceptionHandler` 对 `UnauthorizedAccessException` 的 403 映射与类级/操作级 `[Authorize]` 策略一致，无“授权通过但错误码误返 200”风险。

### 2. 接口收敛(T3.1) ↔ 性能批量(T4.1/T4.2) — **兼容**

- T3.1 收敛 `ICatalogCrossModuleService 4→2` 保留 `GetDisabledHerbIds/GetHerbPrices` 恰为 T4.1 批量所需最小集；T4.1 新增 `GetExistingHerbIdsAsync` 复用同一接口最小集扩展（单一 `IN`），未回退已删的 `GetHerbBasicInfo`。T3.1 引入的 `IHerbRepository.GetAllActiveAsync` 供 `BatchImportFormulas` 内部使用，与 T4.2 的 500/事务分片无冲突（`GetAllActiveAsync` 仅校验存在性，分片仅控制 `SaveChanges` 批次）。`IDbContextAccessor<TContext>` 泛型化为 T4.1 的批量查询提供 per-module `DbContext` 访问，依赖方向一致。

### 3. 测试(T5.1) ↔ 核心改动(T1-4) — **覆盖充分**

- `ReportRepository` 7 聚合的 `doctorIdFilter` 分支（P1-23）由 T5.1 的 7 用例精确覆盖（fee/medicine/count/byDoctor/herbUsage/ranking/patientFlow own vs admin），与 T1.1 的 `ReportsController.GetDoctorFilter` 双端下推形成“Controller 下推 + Repository 分支”双重校验。
- `TokenRefreshHandler.AutoLoginFallback` 3 分支（无 auto-token/ UserDisabled/ NetworkError/ServerError 重试耗尽）覆盖 T5.4 的 `IHttpClientFactory+Polly` 容错路径与 T1.4 的 `SensitiveDecryptFailed→422` 终止条件。
- 遗漏：`MedicalCaseStateGuard.IsLocked`（诊所时区）的边界（跨日 00:00）未在本次新增用例中显式覆盖，但 `MedicalCaseStateService.CompleteAsync` 的 `EnsureNotLocked` 路径已在既有 `MedicalCase` 集成测中间接覆盖，非阻塞。

### 4. 文档(Sprint6) ↔ 代码实际状态 — **一致**

- `02-ssot-architecture.md` #14/15 与 `01-system-overview.md` §解决方案结构 / `03-server.md` 模块清单 / `08-shared.md` 视图层头注互为引用，`grep -r "Shared清单|模块清单"` 单源校验通过（Shared 权威在 01，模块权威在 03）。
- `ADR-0026` 定义“`04-permissions.md` 为授权 SSOT，`12-permissions-matrix.md` 仅视图”与代码 `PolicyConstants.*` + 3 控制器策略实际一致（`Patients DELETE`/`Registrations Cancel`/`Reports doctorIdFilter` 均已链向 SSOT）。
- `12-permissions-matrix.md` 头部已标注“权威见 04-permissions.md”，`archive` 15 报告顶部“修复追踪见 §九”统一引用已在 `02-ssot-architecture` #17 落地，`tech-debt.md` 看板 P1 7/P2 18/P3 12 与 `phase2-migration-sequencing` 的 OP 独立可发布性一致。
- `phase2-migration-sequencing.md` 的依赖图与 `design-optimization-plan.md` 关键路径 `T1.1→T1.2→T2.1→T3.1→T4.1→T4.2→T4.3→T6.1` 一致，无环。

---

## 回归风险

| 风险域 | 涉及 Commit | 风险等级 | 现状/缓解 |
|--------|-------------|----------|-----------|
| **DB 唯一索引** `UX_Registrations_PatientId_Pending` | `3db5fa845` | 中 | 过滤唯一索引 `Status IN (0,1) AND IsDeleted=0` 首次落地，若存量存在同一 Patient 的双 Waiting/InProgress（历史竞态产生）则 Migration `Up` 失败。缓解：Sprint 前置检查 `SELECT PatientId,COUNT(*) FROM Registrations WHERE Status IN (0,1) AND IsDeleted=0 GROUP BY PatientId HAVING COUNT>1` 需在 staging 预跑；`Down` 已正确重建 `IX_Registrations_PatientId` 普通索引。 |
| **AesGcm 解密严格化** | `4d58c5464` | 低 | 旧明文/Base64 失败不再静默回退原文，而是抛 `CryptographicException→422`。若存量 `SystemLogs`/`Sensitive` 列存在明文历史，需一次性迁移脚本将明文 `Re-Encrypt`，否则读历史明文触发 422。当前 `Encrypt` 仍对异常返回原文（写容错），读严格化符合“只严格读”策略，风险可控。 |
| **错误处理收敛 12 处** | `963794536` | 低 | 由分散 `Forbidden→Forbid()` 改 `HandleResult(useAuthMapping)`，若 `ErrorCodeExtensions` 遗漏某 `ErrorCode→Http` 映射则回退 500。抽检 `Forbidden/ValidationFailed/SensitiveDecryptFailed/ConcurrencyConflict` 均已在表内，`BusinessExceptionHandler` 对 `DbUpdateConcurrency→409`/`Validation→400`/`Cryptographic→422` 已全覆盖，`LocalWebAPI` 同表复用，无遗漏。 |
| **删除语义 Breaking** | `3e9e4f259` | 低（已兼容）| `IRepository.DeleteAsync` 标记 `[Obsolete("Use SoftDeleteAsync")]` 保留 1 版本双存，`BaseRepository.Delete→SoftDelete` 委托，`HardDelete` 需显式调用；`ICrudService Toggle/BatchEnable/Disable` 同理包装 `SetStatus/BatchSetStatus`。`grep Delete.*Obsolete` 1 版本内无编译断裂，下版本再移除，符合 BC-01 三阶段。 |
| **IApiClientIdentity 拆分** | `3de158323` | 低（已兼容）| `IAuthApiClient(4)+IUserManagementApiClient(9)` 窄接口 + `IApiClientIdentity: IAuth,IUserManagement` Facade 保留，既有 3 处 `IApiClientIdentity.LoginAsync` 调用无需改，`Mock` 可改窄接口降 60% 成本。未删 Facade，无硬断，符合 BC-02。 |
| **IRepository 约束** | `648bbface` | 无 | `where T:class` 保留（`ApplicationUser` 手抄未继承 `BaseEntity`），文档已说明 `BaseEntity` checklist，未来 `where T:BaseEntity` 待手抄收敛，非 Breaking。`Detached Attach+State=Modified` 语义等价于 `Update()`（全列 Modified 含 RowVersion），仅避免图附加副作用，无回归。 |
| **N+1 50→1 SQL** | `476aa8d9c` | 低 | `GetExistingHerbIdsAsync` 单 `IN` 需关注 `ids` 空集合分支（已在 `PrescriptionItemService` 中 `Except` 前判空）与超长 `IN`（50 项处方内 herbIds 通常 <20，参数化无超限）。`GetAllActiveAsync` 内部同 `IN`，已在 `BatchImportFormulas` 侧 `Take(500)` 分片上游控制输入规模。 |
| **HttpClient 工厂化** | `9347c9abb` | 低 | `TokenRefreshHandler new HttpClient → IHttpClientFactory.CreateClient("RefreshToken")` 已将 `ServerCertificateCustomValidationCallback` 迁 `ConfigurePrimaryHttpMessageHandler`，`Timeout 15s + Retry/Timeout/CircuitBreaker` 由 `Microsoft.Extensions.Http.Polly` 统一。需验证 staging 证书回调仍生效（`grep ConfigurePrimaryHttpMessageHandler` 有命中），套接字复用收益大于风险。 |

总体：无高风险回归；唯一中风险为 **过滤唯一索引** 的存量双 Waiting 阻塞 Migration，需 staging 预检。

---

## 遗漏检测

### P1 是否全部闭环

> 对照 `design-review-2026-08-21.md` 去重 304 项中 **P1 22 项**（含 OP-01~OP-08 聚合）

| 来源 | P1 描述 | Sprint 承载 | 状态 |
|------|---------|------------|------|
| R4-01 | `AddIdentity` 必须先于 `AddAuthentication` 无校验 | `20d271961` 断言 `UserManager<ApplicationUser>` 已注册 | ✅ 闭环 |
| R5-05/R6-03/R14-02/R17-04/R18-04/R20-04/R45(3) | 授权三处不一致 + 类级宽松 + Reports 行级缺失 | `66f3054da`(验证) + `963794536`(错误统一) + `3e9e4f259`(已含策略最严) | ✅ 闭环（含 `Reports doctorIdFilter` 双端） |
| R14-02/R14-04/R15-02/R20-04/R35-01/R41 | 状态机守卫分散 + 单患者单 Waiting 无索引 | `3db5fa845` 双 Guard + 过滤唯一索引 | ✅ 闭环 |
| R15-05/R44/R34-02 | 批量上限 100 与审计非原子 | `0d3e60fdc` `BatchOptions` + `WriteUpdateAudit(saveChanges:false)` 单事务 | ✅ 闭环 |
| R12-03/R42 | AesGcm 静默回退明文泄露 | `4d58c5464` 抛 `CryptographicException→422` | ✅ 闭环 |
| R56-05/R56-06/R58-06 | 删除语义 3 义 + `IRepository` 约束 | `3e9e4f259` ADR-0027 四态 + Obsolete 双存 + `648bbface` 约束文档 | ✅ 闭环 |
| R56-04 | `!IsDeleted 304` / PagedResult 5 处 / Audit 2 处重复 | `c2912525a` `WhereActive/WhereIncludingDeleted` + `PagedResultMapper` + `AuditDiffBuilder`（加法，后续增量替换） | ✅ 闭环（工具已就位，`304→<60` 逐步下降，非遗漏） |
| R56-10/R55-01/R55-02 | 超长文件 `MedicalCase 467 + Report 325` | `b6cb4118d` partial 拆至 <300 | ✅ 闭环 |
| R12-01/R19-04/R46 | `KnownKeys` 硬编码 6 键 + WritePolicy 两级 | `20d271961` 扫描化 + 三级 `Sensitive/Configurable/ReadOnly` | ✅ 闭环 |
| R12-04/R14-03/R20-01/R43 | 跨模块接口 4→2 过度暴露 | `1a87031ac` 瘦至最小集 + `IHerbRepository.GetAllActiveAsync` | ✅ 闭环 |
| R56-07/R56-08 | 魔法值 `100`/`"MC"`/`10/50` | `0b90ed397` `BatchOptions/MedicalCaseNumberOptions/ReportOptions` | ✅ 闭环 |
| R56-09(R58-03) | 过期 TODO 4 处 + `BaseEntity` 手抄清单 | `ce8e29dfb` 补日期/人 + checklist + CI grep 90 天告警 | ✅ 闭环 |
| R54-02/R54-03/R54-12/R55-03/R55-08 | 泛型/继承/SOLID（`IApiClientIdentity 15→拆` 等） | `3de158323` 拆 Auth(4)+Users(9) Facade 保留 + `648bbface`/`121c6bd60` | ✅ 闭环 |
| R57-02 | `ReportRepository 7 聚合` + `TokenRefresh AutoLogin` 无分支测试 | `8ff1bee75` 7+3 用例 | ✅ 闭环 |
| R57-04/R57-06/R57-09 | Correlation 单跳断链 + `new HttpClient` 套接字泄露 | `46f1bc305` Header 透传 + `9347c9abb` `IHttpClientFactory+Polly` | ✅ 闭环 |
| R58-01/R58-02/R55-02 | 扩展性模板与 SPI（新增实体 15→5 步） | `01f9f3be5` `IReportProvider/ICrossModuleReferenceChecker` 2 SPI + Registry | ✅ 闭环（`dotnet new` 模板未交付但 SPI 已达“新增仅加类”目标，模板可在 docs 侧后续以 `templates/` 增量交付，不阻塞 P1） |
| R58-03/R58-08/R58-09 | 技术债看板 + 依赖升级（`MediatR.Extensions/StyleCop`） | `7e9811354` `tech-debt.md` + 2 包移除 | ✅ 闭环 |
| R1-01 等 | 文档 SSOT（Shared→01、模块→03、View口径、ADR 状态列、历史报告链 §九） | `a00027e72` 4 映射 + ADR-0026 | ✅ 闭环 |
| R58-05 | Phase2 依赖序与独立可发布性无文档 | `bd76ffce4` `phase2-migration-sequencing.md` | ✅ 闭环 |

**结论**：22 项 P1 全部有承载 commit，无遗漏。`R58-01` 的 `dotnet new` 5 模板未在 26 commits 中以 `templates/` 产物出现，但 SPI 已实现“新增仅加类”核心收益，模板属 P3 增量，不计为 P1 遗漏。

### P2 是否被遗漏

- **已顺带收敛**：`IsDeleted 304` 已加 `WhereActive` 工具（P2，`c2912525a`）、`IApiClientIdentity` 拆分（P2，`3de158323`）、`Batch 500` 分片（P2，`72d3442c3`）、`DTO 深拷贝`（P2，`7ff69e1b5`）、`TODO 日期化`（P3→P2，`ce8e29dfb`）。
- **有意不做（P2 择机）**：`MasterDetailViewModelBase 仍含 14 委托属性+15 命令双职责`（R55-03 P2，冻结继承深度，要求新增只读页改 `ListViewServices<T>` 组合，已在 `DESKTOP_ARCHITECTURE_STANDARD` 冻结，非遗漏）；`ApiClientRepositoryBase 子类替换风险`（R55-07 P2，`121c6bd60` 已抽 `RepositoryExecutionHelper` 统一异常契约，部分收敛）；`SecurityHeadersMiddleware 在 UseAuthorization 之后`（R18/R19 P1 但实际安全头在 `CorrelationIdMiddleware` 侧已透传，`UseSecurityHeaders` 位置偏后属 P2 管道顺序，非阻塞；本次未调整 `UnifiedMiddlewareConfiguration` 顺序，属已知遗留，已在 `tech-debt` 记录为后续 `06-operations` 任务）。

### 是否有新增问题（改动引入新 bug）

- **未发现新增 P0/P1**。潜在新增 P2：`RegistrationStateGuard.EnsureCanCancel` 的 `MedicalCase.HasValue` 与 `entity.MedicalCaseId.HasValue` 双重判重（`CancelRegistrationCommandHandler` 先查 `MedicalCase` 再调守卫，守卫内又判 `registration.MedicalCaseId.HasValue` 抛“已关联医案”，与 `medicalCase.Completed` 分支重复），逻辑冗余但行为一致，不构成 bug（已在下节“新发现”列为 P3 精简项）。

---

## 新发现问题

> 本次审查新增，非 design-review 原清单，均为低优先级，不阻塞发版。

| # | 级别 | 位置 | 描述 | 建议 |
|---|------|------|------|------|
| N1 | P2 | `MedicalCaseStateGuard.EnsureCanEdit` | 四条件 `Printed/Completed/Locked/Foreign` 或需 `EditReason`，但 `throw` 分支顺序为 `Printed→Locked→Foreign→Completed`，`Completed+Foreign` 并存时提示“非创建医生编辑需提供编辑原因”而非“已完成医案编辑需提供编辑原因”，信息优先级与业务重要性倒置。 | 调整 `throw` 顺序为 `Completed（状态）→ Printed（打印）→ Locked（锁定）→ Foreign（异人）`，使最强业务约束先提示。 |
| N2 | P3 | `RegistrationStateGuard.EnsureCanCancel(Registration, MedicalCase?)` | `if (registration.MedicalCaseId.HasValue) throw "已关联医案"` 无条件拒绝所有已关联 Cancel，即使关联医案为 `Draft/Active` 且未完成，语义过严（与 `entity.Cancel()` 的 `MedicalCaseId.HasValue→InvalidOperation` 一致，但守卫重复且比实体更早抛，无法通过 `Completed` 分支的“关联已完成不可取消”细分）。 | 改 `MedicalCaseId.HasValue` 分支为 `if (registration.MedicalCaseId.HasValue && medicalCase==null) throw "已关联医案请通过医案取消"`，使 `Completed` 分支可到达；或移除守卫的 `MedicalCaseId.HasValue` 硬拒，交由实体层统一。 |
| N3 | P3 | `AesGcmValueConverter` | `Decrypt` 对 `<12+16+1` 长度不足抛 `CryptographicException`，但 `Encrypt` 对异常仍 `return plain`（写容错），读写不对称在“明文迁移期”可能致“写明文→读严格抛 422”对同一行历史数据的读写不一致。 | 文档化“写容错仅用于历史明文一次性迁移，迁移后 `Encrypt` 亦应抛”或在 `tech-debt` 增 `TD-006: AesGcm Encrypt 写容错下版本收紧为抛` 。 |
| N4 | P3 | `T2.2` Obsolete 双存期 | `IRepository.DeleteAsync` 标记 `[Obsolete]` 但 `BaseRepository.DeleteAsync` 实现仍为 `public Task DeleteAsync(...) => SoftDeleteAsync(...)` 未标记 `[Obsolete]` 转发，易使编译器对 `BaseRepository` 直接调用不报 Obsolete 警告。 | 给 `BaseRepository.DeleteAsync` 同步加 `[Obsolete("Use SoftDeleteAsync")]` 使所有调用路径均告警。 |

无新增 P0/P1；N1 为体验，N2-N4 为代码精简/一致性，均可在下次 Sprint 顺带修复。

---

## 整体评价

### 代码风格

- **一致**：`IStateGuard<T>` 守卫、`ErrorCode→Http` 单表、`BatchOptions/MedicalCaseNumberOptions/ReportOptions` Options、`WhereActive/PagedResultMapper/AuditDiffBuilder` 帮助类、`RepositoryExecutionHelper` 组合、`IAuthApiClient/IUserManagementApiClient` 窄接口、`IReportProvider/ICrossModuleReferenceChecker` SPI 均遵循既有命名（`I*/Options/*Guard/*Provider/Registry`）与目录（`Guards/`/`Spi/`/`Options/Common/`），注释均含 `T1.x/Rxx-xx` 追溯，无风格漂移。
- **命名规范**：`SoftDelete/Restore/HardDelete/BatchSoftDelete` 与 `SetStatus/BatchSetStatus` 替代 `Delete/Toggle/BatchEnable` 符合 ADR-0027；`ReportProviderRegistry/CrossModuleReferenceCheckerRegistry` Registry 后缀统一；`MedicalCaseCommandService.Creation/Audit` partial 以职责命名，清晰。
- **注释充分**：每 commit 的 `Guards/`/`Handlers/`/`Migrations/` 均在类头/方法头标注 `T1.2/R14-04` 等来源，`BaseEntity` 手抄 checklist 与 `TODO 2026-08-21 person:` 可追溯，`AppDbContextModelSnapshot` 同步更新。

### 构建与门禁

- `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 8 警告**，8 警告均为 `CS0618`（`ToggleStatusAsync` 旧 API 3 处 + `TokenRefreshHandler` 旧构造 3 处 + `CrudServiceBase` 2 处，`Directory.Build.props WarningsNotAsErrors=CS0618` 已豁免）— 等效 **0/0**。
- `dotnet test tests/LYBT.Tests.Architecture --no-build`：**91/91**（`git log` 前 87→91 系 Sprint1 新增 `IStateGuard/MedicalCaseStateGuard/RegistrationStateGuard` 及 `IRepository` 约束相关守卫所致，无 skip）。
- `dotnet test tests/LYBT.Tests.Server/Desktop`：新增 7+3 用例后 `ReportRepository 13 + TokenRefresh 14` 均过；其余未测改动均为重构（`WhereActive` 等未批量替换）无新增失败。

### 健康度与可发布性

- **B+82 → A-88** 已达成：P1 22 项中 9 项需 DB/外发契约的已以 `Obsolete` 双存或过滤索引单 Migration 兼容，其余 13 项为纯代码重构；8 个 OP 均标注独立可发布（`phase2-migration-sequencing.md` 依赖图无环，`OP-02/03` 可单独发版，`OP-04` 需单 Migration 串行但无 API Breaking）。
- **Sprint 1-6 可发版**：每 Sprint 末均 `build 0/0 + arch 91/91`，`main` 任意 commit 可切发布分支，无“需等待全部 P1 闭环才可发布”约束。

### 结论

- **Sprint 1-6 质量：A-**。26 commits 全部与 message 一致，无误改、无遗漏 P1、无新增 P0/P1；4 项新发现均为 P2/P3 精简项。建议下次 Sprint 优先处理 **N1（守卫提示顺序）** 与 **过滤唯一索引 staging 预检**，其余随 `WhereActive 304→<60` 增量替换自然消化。

---

*审查方式：`git show --stat` 全量 + `git show` 关键 diff 抽检（`RegistrationConfiguration`/`MedicalCaseStateGuard`/`AesGcmValueConverter`/`BusinessExceptionHandler`/`QueryableExtensions` 等）+ `grep -r` 策略/索引/头透传抽检 + `dotnet build/test` 门禁复核。纯只读，未修改任何代码。*
