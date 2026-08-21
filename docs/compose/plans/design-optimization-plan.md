# LYBTZYZS 设计优化执行计划

> **来源**: `docs/compose/reports/design-review-2026-08-21.md` 1383 行（R1-R58 全部 304 项去重）+ `design-review-summary-2026-08-21.md` + `src/AGENTS.md` + `docs/03-architecture/13-project-master-plan.md` v2.1  
> **生成时间**: 2026-08-21 Phase2 后  
> **原则**: 先统一后优化 · 先安全后功能 · 每 Sprint 可发布 · 测试先行 · ADR 先行  
> **门禁**: 每 Sprint 结束 `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告 + `dotnet test tests/LYBT.Tests.Architecture --no-build` 91/91 + `tests/LYBT.Tests.Server` 已知 8 失败不阻塞

## 总览

- **总发现（去重）**: 304 项（P0 0 / P1 22 / P2 120 / P3 162，含 R41-R50 的 9 项 OP 聚合 + R51-R53 3 项保留）
- **本计划覆盖**: 全部 P1 22 项 + P2 中 38 项高ROI + P3 中 6 项（依赖升级/文档），计 **46 项发现** 收敛为 **24 个可执行任务**
- **分组依据**: 同文件/同模式可一起修复（如 `IsDeleted 304 行` 同为软删谓词、`BatchDelete 30 处` 同为批量模板、`ErrorCode→Http` 5 控制器同为错误映射）
- **总工时**: 乐观 18.5d / 悲观 28d / **最可能 22.5d**
- **Sprint 数**: 6（每 Sprint 5 天，日均 1 人）
- **预计完成**: 2026-08-22 → **2026-09-26**（6×5d，含 3 天缓冲）
- **可发布性**: 每 Sprint 均以 `build 0/0 + arch 91/91` 可发布，P1 在 Sprint1-2 闭环后健康度从 B+(82) → A-(88)
- **ADR 需求**: 需 3 个 ADR（ADR-0025 授权SSOT、ADR-0026 文档SSOT、ADR-0027 删除语义与批量归一）

---

## Sprint 1: 安全与授权基座（第 1-5 天）— *先安全后功能*

### 目标
闭环全部与越权/状态绕过相关的 P1（4 项），建立授权 SSOT 与状态守卫基座，为后续所有横切依赖提供不变量。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时(O/P/M) | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|-------------|------|------|-----|---------|
| T1.1 | **授权矩阵 SSOT 收敛（OP-03 核心）** — 将类级改最严：`PatientsController Delete→AdminOrSuperAdmin`、`Registrations Cancel→ReceptionistOnly`，`Reports 行级过滤 Where(DoctorId==currentUserId)`，`04-permissions` 单点引用，其余文档/代码均链至该表 | R5-05,R6-03,R14-02,R17-04,R18-04,R20-04,R45(3处不一致) | `04-permissions.md`, `PatientsController.cs`, `ReportsController.cs`, `RegistrationsController.cs`, `RegistrationsController( Local)` + `12-permissions-matrix.md` 同步 | 0.8/1.5/**1.2d** | 低 | 无 | 是 ADR-0025 | `grep -r DoctorOrAdmin PatientsController` 0 命中 + `RegistrationsController` `Cancel` 单测 + arch 91/91 |
| T1.2 | **状态机守卫统一入口（OP-01 核心）** — 新增 `IStateGuard<T>` + `MedicalCaseStateGuard(IsLocked via IMedicalCaseTimeService + CaseStatus + SingleWaiting)` + `RegistrationStateGuard(Waiting/Completed关联校验 + 单患者单Waiting索引)`，在 `MedicalCaseCommandService.Update/Complete` 与 `Registration Cancel` 首行强校验 | R14-02,R14-04,R15-02,R20-04,R35-01,R41,R55-01 中 IsLocked 部分 | `MedicalCaseCommandService.cs`, `RegistrationCommandService.cs`, `MedicalCaseTime.cs`, `RegistrationConfiguration.cs` + Migration `IX_Registrations_PatientId_Status_Filtered` | 1.0/1.8/**1.3d** | 中（索引需单迁移） | T1.1（行级权限为守卫前置） | 否 | 新增 `MedicalCaseStateGuardTests` 4用例 + 并发双Waiting集成测 + build 0/0 |
| T1.3 | **批量上限与审计原子性补强** — `BatchDelete` 统一走 `BatchOperationHandlerBase` 100上限（抽 `BatchOptions.MaxBatchSize`），`MedicalCase AuditLog` 与业务 `BeginTransaction` 单事务 | R15-05,R44,R34-02 | `BatchOperationHandlerBase.cs`, `BatchOptions.cs`, `MedicalCaseCommandService.cs` (`WriteUpdateAudit(saveChanges:false)`) | 0.5/1.0/**0.7d** | 低 | T1.2（事务边界） | 否 | 审计与业务同回滚集成测 + 批量>100 400测 |
| T1.4 | **AesGcm 静默回退修复** — Decrypt 失败抛 `CryptographicException` 转 422，记 Warning，不再回退明文 | R12-03,R42 | `AesGcmValueConverter.cs`, `BusinessExceptionHandler.cs` (+ `ErrorCode` 增 `SensitiveDecryptFailed`) | 0.3/0.6/**0.5d** | 低 | 无 | 否 | 解密篡改 422测 + 日志 grep |

**Sprint1 小计**: 3.7d（最可能）| P1 6项闭环 | 可发布：所有授权/状态绕过已封堵

---

## Sprint 2: 错误与命名统一（第 6-10 天）— *先统一后优化*

### 目标
单点化错误处理与命名，为后续所有 Service/Controller 重构提供稳定契约；配置与测试先行。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时 | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|------|------|-----|-----|---------|
| T2.1 | **错误处理单点化（OP-02 核心）** — `ErrorCodeExtensions.ToHttpStatusCode` 唯一映射，`BusinessExceptionHandler` 统转 `DbUpdateConcurrencyException→409`/`ValidationException→400` 类型名匹配（无EF直接引用），删除 Controller 内 `BusinessFail(200)` 5处分支，AesGcm/Token 异常同入该表 | R12-03,R13-02,R18-03,R20-05,R42,R55-07,R56-03 | `ErrorCodeExtensions.cs`, `BusinessExceptionHandler.cs`, `Patients/Registrations/Catalog/*Controller.cs` 5文件 | 1.0/1.8/**1.3d** | 中 | T1.1（状态码与授权一致） | 否 | 409/400 单测 + Controller 0 catch grep + arch 91/91 |
| T2.2 | **删除语义命名规范（Breaking 预备，ADR-0027）** — 定 `SoftDelete/Restore/HardDelete/BatchSoftDelete` 四名，`IRepository.Delete→SoftDelete` 标记 `[Obsolete]` 保留 1 版本，新增三态方法，`RemoveAsync` 等混用清理，全库 `grep Delete` 按表改名 | R56-05,R56-06,R58-06,R54-02 中约束关联 | `IRepository.cs`, `BaseRepository.cs`, `MedicalCaseRepository.HardDeleteAsync`, `IConfigurationStore.RemoveAsync` 保留物理语义，`MasterDetailCommandGroup` `BatchEnable/Disable→SetStatus` | 1.0/1.6/**1.3d** | 高（Breaking，需三阶段） | T2.1（错误码与删除语义联动） | 是 ADR-0027 | 新旧双路由 308 测 + `grep Delete.*Obsolete` |
| T2.3 | **分页/魔法值收敛** — `BaseApiController.ValidatePagination` 改引 `SystemConstants.MaxPageSize=100`，`MedicalCasesController Batch>100` 同常量，`MedicalCaseNumberOptions{Prefix="MC",Pad=3}` + `ReportDefaults{Top10/Max50}`，`$"MC{dateStr}"` 等 4处硬编码收敛 | R56-07,R56-08 | `SystemConstants.cs`, `BaseApiController.cs`, `MedicalCaseCommandService.cs`, `ReportsController.cs`, `PaginationService.cs` | 0.5/0.8/**0.6d** | 低 | 无 | 否 | grep 100 硬编码 0残留 + 单测生成编号 |
| T2.4 | **配置 KnownKeys 扫描化（OP-06）** — `ConfigurationPostProcessor`/`ConfigurationWritePolicy` 改 `configuration.AsEnumerable().Select(kv=>kv.Key)` 全量扫描，Sensitive/Configurable/ReadOnly 三级，`AddIdentity` 顺序断言 | R12-01,R19-04,R46,OP-06 | `ConfigurationPostProcessor.cs`, `ConfigurationWritePolicy.cs`, `AuthenticationServiceCollectionExtensions.cs` | 0.3/0.5/**0.5d** | 低 | 无 | 否 | 新增键占位符自动回退测 |
| T2.5 | **过期 TODO 清理与注释门禁** — 清 4处 TODO 加日期/人，实体注释保留what，超90天CI告警，`BaseEntity` 顶部增 `ApplicationUser` 手抄清单 checklist | R56-09,R9-03 | `ReportsModule.cs`, `ClinicalHomeViewModel.cs`, `PrescriptionItemViewModel.cs`, `BaseEntity.cs` | 0.2/0.4/**0.3d** | 低 | 无 | 否 | `grep TODO` 4→0 + CI grep |

**Sprint2 小计**: 4.0d | 依赖 Sprint1 的授权已定，错误/命名统一为后续所有接口收敛的前提

---

## Sprint 3: 契约与仓储收敛（第 11-15 天）

### 目标
收敛过度暴露的跨模块接口与重复的软删/分页逻辑，奠定仓储与DTO的统一契约。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时 | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|------|------|-----|-----|---------|
| T3.1 | **跨模块接口按需收敛（OP-05 核心）** — `ICatalogCrossModuleService` 4→1（仅 `GetDisabledHerbIds`，其余移内部），`IPatient` 3→1（仅 `GetBasicInfo`），`Registration` 直连 Repository 改走 `IMedicalCaseCrossModuleService.CreateAsync`，`IDbContextAccessor` 泛型化 | R12-04,R14-03,R20-01,R43 | `ICatalogCrossModuleService.cs`, `IPatientCrossModuleService.cs`, `IMedicalCaseCrossModuleService.cs`, `RegistrationCommandService.cs`, `IDbContextAccessor.cs` | 0.6/1.0/**0.8d** | 低 | T2.1（接口错误码已统一） | 否 | 跨模块 grep 0绕过 + arch `Module不引用AppDbContext` |
| T3.2 | **软删除/审计谓词与快照收敛（OP-04 代码侧）** — 新增 `IQueryable<T>.WhereActive()/WhereIncludingDeleted()`（基于 `EntityOptimizationExtensions`），`PagedResultMapper.FromEntities`，`AuditDiffBuilder<T>.Capture(old,new).BuildAuditLog()`，收敛 `IsDeleted 304行 + 审计 2处 + 分页 5处` | R56-01,R56-04,R9-03,R44 | `EntityOptimizationExtensions.cs`, `PagedResultMapper.cs`, `AuditDiffBuilder.cs`, `MedicalCaseCommandService.cs`, `Formula/Patient` 各1 | 0.8/1.3/**1.0d** | 中 | T2.2（软删命名已定） | 否 | `grep !IsDeleted` 降 80% + 审计同事务测 |
| T3.3 | **Repository 契约与生命周期对齐** — `IRepository<T> where T:BaseEntity` 收紧，`IUserRepository : IRepository<ApplicationUser>` 对齐，`BaseRepository<T,TDbContext>` 文档注明单参退化路径，`UpdateAsync` Detached 全列标记改为仅变更属性 | R54-02,R11-03,R54-01 | `IRepository.cs`, `IUserRepository.cs`, `BaseRepository.cs`, `ApplicationUser.cs` 注释 | 0.6/1.0/**0.8d** | 中 | T3.2（软删谓词） | 否 | `IRepository<string>` 编译失败测 + RowVersion 并发重放测 |
| T3.4 | **泛型/仓储继承改组合（R54-09）** — 抽 `RepositoryExecutionHelper` 静态 `ExecuteAsync/HandleException`，`EntityApiClientRepositoryBase` 改组合 `IEntityApiSegment`，`Registration/MedicalCase` 双套模式收敛为工厂 | R54-03,R54-09,R54-12 | `ApiClientRepositoryBase.cs`, `EntityApiClientRepositoryBase.cs`, `RepositoryExecutionHelper.cs` | 0.6/1.0/**0.8d** | 低 | T3.3（约束已紧） | 否 | 两套模式合并为1 + 单测 `HandleException` 一致性 |

**Sprint3 小计**: 3.4d | 接口收敛为性能批量提供了最小方法集（`GetExistingHerbIds` 单点）

---

## Sprint 4: 性能与结构重构（第 16-20 天）

### 目标
消除 N+1 与大索引缺失，拆解超长文件，使读路径可随数据量线性扩展。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时 | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|------|------|-----|-----|---------|
| T4.1 | **查询批量化（50→1）** — `MedicalCase 创建` 改 `GetExistingHerbIdsAsync(ids)` 单次 IN（`Except` 求缺失），`Catalog GetHerbPricesAsync` 批量，`Report 7聚合` 已验证索引后复核 | R15-01,R16-05,R49,OP-04 | `MedicalCaseCommandService.cs`, `CatalogCrossModuleService.cs`, `ReportRepository.cs` | 0.7/1.2/**0.9d** | 低 | T3.1（批量接口已收敛） | 否 | 50项处方单SQL录制 + 集成测 |
| T4.2 | **批量导入分批与索引** — `Catalog BatchImport 10000` 分批 500 事务 + partial 成功，补 `IX_Registrations_CreatedAt`/`IX_MedicalCases_CreatedAt`/`IX_FormulaHerbItems_FormulaId_IsDeleted`（如未落地则补） | R16-03,R17-03,R11-02,R49,OP-04 | `CatalogModule.cs`, `ReportRepository.cs`, `FormulaHerbItemConfiguration.cs`, Migration 单文件 | 0.7/1.2/**0.9d** | 中（迁移串行） | T4.1（批量路径） | 否 | 10000条 3s 内 + 执行计划索引命中 |
| T4.3 | **超长文件拆解** — `MedicalCaseCommandService 467→3文件`（`Command/EditGuard/AuditBuilder`）、`MedicalCaseQueryService 442→3类`（`List/Search/Recent`）、`ReportRepository 325` 按主题拆 `Income/Consultation/Herb` | R56-10,R55-01,R55-02 | `MedicalCase*Service*.cs` 5文件, `ReportRepository*.cs` 3文件 | 1.0/1.6/**1.3d** | 中 | T4.1（批量逻辑已稳） | 否 | 文件行数 <300 + 单测不变 + build 0/0 |
| T4.4 | **DTO 投影与深拷贝收敛（OP-07 前半）** — `MedicalCaseInputDto` 移除 `CreatedBy/CreatedAt` 只读，`HistoryCopy` 深拷贝 `PrescriptionItemModel.Clone`，报表复制重取最新 Herb 单价 | R10-01,R47,R38-05,R32-02 | `MedicalCaseInputDto.cs`, `HistoryCopyDialogViewModel.cs`, `ReportRepository` 辅 DTO | 0.5/0.8/**0.6d** | 低 | T3.4（仓储已稳） | 否 | 伪造 CreatedBy 422 + 深拷贝不污染测 |

**Sprint4 小计**: 3.7d | 性能收益立即可度量（N+1 90%+，导入不超时）

---

## Sprint 5: 可测试性 / 可观测 / 容错（第 21-25 天）— *测试先行*

### 目标
补齐测试与容错短板，闭环分布式追踪与资源生命周期，使系统在故障与高并发下可预期。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时 | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|------|------|-----|-----|---------|
| T5.1 | **报表与 Token 分支测试补全（测试先行）** — `ReportRepository 7聚合` InMemory 含 `doctorIdFilter` 分支，`TokenRefreshHandler AutoLoginFallback` 3分支 | R57-02 | `ReportRepositoryTests.cs`(新建, 14用例), `TokenRefreshHandlerTests.cs` 增 3用例 | 0.7/1.0/**0.8d** | 低 | 无（纯新增） | 否 | 7聚合全覆盖 + fallback 单测 |
| T5.2 | **SOLID/泛型/继承收敛** — `IRepository 约束` 落地后，`CrudServiceBase 7抽象` 拆 `IReadable/IWritable`，`IPrintService<T>` 保留，`NavigableViewModelBase` 冻结深度（`MasterDetail` 不再新增继承，`ListViewServices<T>` 组合），`IApiClientIdentity` 拆 `IAuthApiClient(4)+IUserManagement(9)` 保留 Facade | R54-02,R54-03,R54-05,R54-12,R55-03,R55-08 | `IRepository.cs`, `CrudServiceBase.cs`, `MasterDetailViewModelBase.cs`, `IApiClientIdentity.cs` + 新 `IAuthApiClient.cs` | 0.8/1.3/**1.0d** | 中 | T3.3,T2.2（约束/命名已定） | 否 | Arch `禁止 Module 引用 EF` + `ViewModel不直接注 IApiClient` |
| T5.3 | **可观测性闭环** — `AuthorizationMessageHandler` 透传 `X-Correlation-Id`，`CorrelationIdMiddleware` 无则生成并回写 `Response.Headers`，补 `ActivitySource` 裸Span（可选） | R57-04 | `AuthorizationMessageHandler.cs`, `CorrelationIdMiddleware.cs`/`CorrelationIdEnricher.cs` | 0.4/0.7/**0.5d** | 低 | T2.1（异常已带 correlationId） | 否 | 端到端 Header 透传集成测 |
| T5.4 | **容错与资源整改** — `TokenRefreshHandler` `new HttpClient` 改 `IHttpClientFactory.CreateClient("RefreshToken")` TypedClient（证书回调`ConfigurePrimaryHttpMessageHandler`），`IHttpClientFactory` 统一 `Timeout 15s + Retry/Timeout/CircuitBreaker`（Polly `Microsoft.Extensions.Http.Polly` 已引），`IApiHealthMonitor` 阈值接入 | R57-06,R57-08,R57-09,R57-10 | `TokenRefreshHandler.cs`, `Program.cs`/`HttpClientRegistrations.cs` | 0.6/1.0/**0.8d** | 中 | T5.1（刷新分支已测） | 否 | 30s→15s 统一 + `new HttpClient` grep 0 + Arch禁 new |
| T5.5 | **ViewModel/行为一致性收敛（顺带，低优）** — `INavigationService→NavigationCoordinator` Obsolete，`ViewModelLocator` 跨程序集映射表，`BoolToVisibilityConverter` 单处，`KeepAlive` 指引 | R25-02,R26-02,R26-03,R23-03 | `02-desktop.md`, `ViewModelLocator.cs`, `Converters/*` | 0.3/0.5/**0.4d** | 低 | 无 | 否 | 文档指引 + grep 双转换器 0 |

**Sprint5 小计**: 3.5d | 测试先行为 T5.2/T5.4 的重构提供了护航

---

## Sprint 6: 演进与文档收尾（第 26-30 天）

### 目标
将一次性优化沉淀为可复用扩展点与自动化门禁，收敛剩余 P3，完成健康度 A- 目标。

### 任务清单
| # | 任务 | 发现分组 | 涉及文件 | 工时 | 风险 | 依赖 | ADR | 验证方式 |
|---|------|---------|---------|------|------|-----|-----|---------|
| T6.1 | **扩展性模板与 SPI** — `dotnet new lybt-entity -n Inventory` 5模板（Entity/Repository/Service/Controller/ViewModel，15→5步），`IReportProvider`/`ICrossModuleReferenceChecker` SPI 注册表（新增报表/库存阈值仅加类） | R58-01,R58-02,R55-02,R55-04 | `templates/lybt-entity/*`, `IReportProvider.cs`, `ReportProviderRegistry.cs` | 0.8/1.3/**1.0d** | 低 | T4.3,T5.2（查询/接口已稳） | 否 | 新实体 5步脚手架生成测 |
| T6.2 | **技术债看板与依赖升级** — 建立 `docs/00-governance/tech-debt.md`（P1 9/P2 24/P3 19 列项归属规模工时），执行依赖升级：移除 `MediatR.Extensions.*→services.AddMediatR` 单包、`StyleCop beta→Roslyn`、确认移除 `System.Data.SqlClient` 旧驱动，`EFCore/Serilog` 等巡检 | R58-03,R58-08,R58-09 | `tech-debt.md`(新建), `Directory.Packages.props`, `Program.cs` MediatR 注册 | 0.5/0.9/**0.7d** | 低 | T5.4（Http/Polly已整） | 否 | `dotnet list package --outdated` + build 0/0 |
| T6.3 | **文档 SSOT 收敛（OP-08 核心，ADR-0026）** — 每个信息点定 SSOT（Shared 清单→`01-system-overview`，模块清单→`03-server`，View口径→导航目标+控件分表，ADR 增实现状态列，历史报告统一链 `13-project-master-plan §九`），`01-system-overview` 修 Shared/模块 7+8 清单，`decisions/README` 增 `0002 Superseded→0009` 等 | R1-01,R1-04,R2-02,R2-03,R3-01,R7-01,R8-04,R48,OP-08 | `01-system-overview.md`, `03-server.md`, `02-desktop.md`, `decisions/README.md`, `13-project-master-plan.md §九` | 0.6/1.0/**0.8d** | 低 | T1.1,T2.2（SSOT与授权/命名同源） | 是 ADR-0026 | `grep -r "Shared清单"` 单源 + 文档新增单测（链接校验） |
| T6.4 | **迁移排序文档与发布总结** — 产出 `phase2-migration-sequencing.md` 依赖序（见下图），每 OP 独立可发布性标注，合流 Phase2 的 13个P1/P2，更新本计划的实际工时与健康度 `B+→A-` 度量 | R58-05 | `docs/compose/plans/phase2-migration-sequencing.md`(可选併本文) | 0.2/0.4/**0.3d** | 低 | 全部 | 否 | 依赖图无环校验 |

**Sprint6 小计**: 2.8d | 文档与模板沉淀，健康度 A- 达成

---

## 依赖关系图

```
T1.1(授权SSOT) ──┬─→ T1.2(状态守卫) ──→ T1.3(批量/事务) ──→ T2.1(错误单点)
                 │                         │
                 └─→ T2.2(命名/Breaking预备) ←─┘
T1.1 ───────────→ T2.1 ──→ T3.1(接口收敛) ──→ T4.1(批量化 50→1) ──→ T4.2(分批+索引)
T2.2 ───────────→ T3.2(软删/审计) ─→ T3.3(仓储约束) ─→ T3.4(继承改组合) ─┐
                                                                   ├─→ T4.3(拆超长文件)
T2.3(常量)/T2.4(配置) ────────────────────────────────────────────┘
T5.1(测试先行) ──→ T5.2(SOLID/泛型) ──→ T5.4(容错/HttpClient) ──→ T6.1(模板/SPI)
T2.1 ───────────→ T5.3(Correlation闭环) ──┘
T4.3 ───────────→ T6.1 ──┐
T5.2 ───────────→ T6.1 ──┤
T5.4 ───────────→ T6.2(债看板/依赖) ─→ T6.3(文档SSOT, ADR-0026) ─→ T6.4(排序总结)
T4.4(DTO深拷贝) ─────────────────────────────────────→ T6.1
```

**关键路径**: `T1.1 → T1.2 → T2.1 → T3.1 → T4.1 → T4.2 → T4.3 → T6.1`（约 8d）  
**可并行**: Sprint2 的 T2.3/T2.4 与 T2.1/T2.2 并行；Sprint4 的 T4.4 与 T4.1-3 并行；Sprint5 的 T5.1 与 T5.3 并行  
**串行约束（迁移）**: 同库单迁移链 — T1.2、T4.2 各含 1 个 Migration，必须单 migration per build 串行提交（同 Batch E/F 已验证）

---

## Breaking Change 清单

| # | 任务 | 变更内容 | 影响面 | 应对策略 | 版本 |
|---|------|---------|-------|---------|------|
| BC-01 | T2.2 `IRepository.Delete→SoftDelete` | 接口方法更名，旧名 `[Obsolete("Use SoftDeleteAsync")]` 保留 1 版本 | 内部 12 仓储 + 20 调用点，**无外部 NuGet 消费者** | 三阶段：①新增四态方法+Obsolete旧名双存 ②客户端双适配 ③下版本删旧名；`ApiRoutes` 旧路由 308 转发 | v1 内兼容，v2 移除 |
| BC-02 | T5.2 `IApiClientIdentity 15→拆 Auth(4)+Users(9)` | 叶子接口拆分，Facade 保留 | 3 处 ViewModel 直接依赖 `IApiClientIdentity.LoginAsync` | Facade 保留 1 版本（`IApiClientIdentity` 转发至 `Auth/Users`），调用方按需改窄接口，Mock 减 60% | v1 内兼容 |
| BC-03 | T3.3 `IRepository<T> where T:class → where T:BaseEntity` | 泛型约束收紧 | `IRepository<string>` 将编译失败，但仓内无此用例 | 编译期即发现，无运行时影响；`IUserRepository` 同步对齐 | v1 内，重构 |
| BC-04 | T3.1/T4.1 `GetDisabledHerbIds 保留` 潜在签名收敛 | `ICatalog` 3 方法移除 | 仅 MedicalCase 一处调用，已在 T3.1 收敛 | 移除未被调用方法，零调用方，编译即发现 | 无感知 |

**结论**: 真 Breaking 仅 BC-01（且为内部接口，无外发包），均在 v1 内通过 Obsolete+双存兼容，无需升 Major；`Asp.Versioning 8.1.1 + ApiVersion("1")` 已就绪，新增端点均在 v1 内

---

## 风险缓解

| 风险 | 触发任务 | 缓解措施 | 责任 |
|------|---------|---------|------|
| **迁移串行冲突** | T1.2, T4.2 各 1 Migration | 每任务单 migration per build，`add` 后立即 `build 0/0` 再下一任务；`__EFMigrationsHistory` 连续性校验（参考 P2-3-7 不 Squash） | 执行人 |
| **Breaking 编译面大** | T2.2, T3.3 | 先在 `feature/delete-semantics` 分支全量 `grep` 预演，`Obsolete` 保留 1 版本，`git mv` 保持历史可追溯 | 架构师评审 |
| **并发重试引入新竞态** | T1.2 索引, T4.1 批量 | `AnyAsync→Insert` 竞态由过滤唯一索引兜底；批量 `IN` 需单测 `EmptyIds→0` 分支；`MedicalCase IsLocked` 用 `IMedicalCaseTimeService` 诊所时区 | 测试先行 |
| **HttpClient 工厂化 回归** | T5.4 `new HttpClient→Factory` | 保留 `HttpMessageHandler` 证书校验语义（`ConfigurePrimaryHttpMessageHandler`），`Timeout 15s` 与重试 `1/2/4s` 在 `ApiHealthCheckService` 先灰度 1 天 | 运维灰度 |
| **超长文件拆分 行为漂移** | T4.3 467/442→3文件 | 每拆一个类立即跑 `MedicalCase` 集成测 + `Mapperly` 编译期映射测，不混合其他改动 | 拆分配对评审 |
| **P1 误判阻塞发布** | 全量 P1 9项 | Sprint1-2 的 5 项 P1（授权/状态/批量契约/超长/手工HttpClient）中仅 `T2.2` 为 Breaking 需双存，其余 P1 均为内部重构，可分批发布不用等待全部 | PM 排期 |

---

## 附录 A: 发现→任务映射（去重 304→24）

| 任务 | 聚合的发现 | 数量 |
|------|-----------|------|
| T1.1 | R5-05,R6-03,R14-02,R17-04,R18-04,R20-04,R45(3) | 7 |
| T1.2 | R14-02,R14-04,R15-02,R20-04,R35-01,R41 | 6 |
| T1.3 | R15-05,R44,R34-02 | 3 |
| T1.4 | R12-03,R42 | 2 |
| T2.1 | R12-03,R13-02,R18-03,R20-05,R42,R55-07,R56-03 | 7 |
| T2.2 | R56-05,R56-06,R58-06,R54-02 | 4 |
| T2.3 | R56-07,R56-08 | 2 |
| T2.4 | R12-01,R19-04,R46 | 3 |
| T2.5 | R56-09,R9-03 | 2 |
| T3.1 | R12-04,R14-03,R20-01,R43 | 4 |
| T3.2 | R56-01,R56-04,R9-03,R44 | 4 |
| T3.3 | R54-01,R54-02,R11-03 | 3 |
| T3.4 | R54-03,R54-09,R54-12 | 3 |
| T4.1 | R15-01,R16-05,R49 | 3 |
| T4.2 | R16-03,R17-03,R11-02,R49 | 4 |
| T4.3 | R56-10,R55-01,R55-02 | 3 |
| T4.4 | R10-01,R47,R38-05,R32-02 | 4 |
| T5.1 | R57-02 | 2 |
| T5.2 | R54-02,R54-03,R54-05,R54-12,R55-03,R55-08 | 6 |
| T5.3 | R57-04 | 1 |
| T5.4 | R57-06,R57-08,R57-09 | 3 |
| T5.5 | R25-02,R26-02,R26-03,R23-03 | 4 |
| T6.1 | R58-01,R58-02,R55-02,R55-04 | 4 |
| T6.2 | R58-03,R58-08,R58-09 | 3 |
| T6.3 | R1-01,R1-04,R2-02,R2-03,R3-01,R7-01,R8-04,R48 | 8 |
| T6.4 | R58-05 | 1 |
| **合计** | **去重后 24 任务覆盖 96 项高优发现**（其余 208 项 P3/低ROI 随任务顺带收敛，不单独立项） | 96 |

## 附录 B: 验证清单（每 Sprint 复用）

```
dotnet build LYBTZYZS.sln --no-incremental -v minimal           # 0 错误 0 警告
dotnet test tests/LYBT.Tests.Architecture --no-build -v minimal  # 91/91
dotnet test tests/LYBT.Tests.Server --no-build -v minimal        # 已知 8 失败不阻塞（ConfigurationLoading×2 + LoginValidator×6）
dotnet test tests/LYBT.Tests.Desktop --no-build -v minimal       # 全过
grep -r "where T : class.*IEntity" src --include="*.cs" | wc -l  # 约束收敛度量
grep -r "!IsDeleted" src --include="*.cs" | wc -l                # 304 → <60 验证软删收敛
```

---

*本计划基于 1383 行审查报告逐条分组，遵循 `src/AGENTS.md` 三层单向依赖与 `13-project-master-plan.md` v2.1 阶段划分，可直接作为 Sprint Backlog 执行。*
