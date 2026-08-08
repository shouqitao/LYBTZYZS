# Server 层方法级深审报告（Mimo 独立分析）

> 任务：A-30-S2｜派发：Mimo Code（只读审查）｜版本：v1.0
> 产出日期：2026-08-09｜基线 commit：`4a956a851`（任务书，代码树同 `7edf020f5`，与 S0/S1 一致，无代码变更）
> 任务书：`docs/compose/specs/task-a30-s2-server-audit-2026-08-08.md`｜依据：S0 基线（`method-audit-baseline/duplicates/dead-candidates-2026-08-08.md`）+ S1 Shared 结论（`method-audit-shared-2026-08-08.md`）
> 性质：**只读**，未修改任何 src/tests 代码，未 commit（报告由技术总监统一提交）

---

## 0. 方法说明（工具/基线/时间点）

| 项 | 说明 |
|----|------|
| 范围 | Server 10 项目：Core/LYBT.Infrastructure + 8 Modules（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports）+ Services/LYBT.WebAPI，方法总数 **970**（S0 基线口径，LocalWebAPI 属 Desktop 排除） |
| 复核方法 | 7 个并行符号级子审（每模块 1 + 死方法专项 1）：serena 符号引用 + 全仓 grep（src/ + tests/，含 Desktop 消费方）+ 源码走查；框架反射面（MediatR `Handle`/DI 构造/路由 action/`IExceptionHandler`/Mapperly 生成代码/XAML/EF 设计时）单独识别，不误判为死 |
| 判定标准 | A=依据充分（设计依据+真实调用链）；B=支撑性（private/内部 helper）；C=重复/分散（同职责 2+ 处实现）；D=死方法（0 调用）；E=可集中（机制应集中到 Shared.Logging/Shared.ExceptionHandling 等，沿用 S1 判定） |
| 时间点 | S0 生成 2026-08-09 02:10；本报告复核 2026-08-09（同基线代码树） |
| 已知盲区 | ① 路由可达但客户端 0 消费的 action 判 C 不判 D（需产品确认，见 §7 备注）；② Mapperly 生成代码在 `obj/` 不在扫描面，已人工核实（如 `UserCrossModuleMapper.g.cs`）；③ `nameof`/反射字符串已逐项排除 |

---

## 1. 10 项目方法分级总表（A/B/C/D/E 计数）

> C 计数单位为「组/处」（一组=2+ 方法重复），D 计数为符号（接口+实现成对计 2）；E 为「应迁移机制点」。

| 项目 | 类型数 | 方法数 | A | B | C 组/处 | D 符号 | E 机制点 | 关键结论 |
|------|-------|-------|----|----|---------|--------|---------|---------|
| LYBT.Infrastructure | 71 | 210 | ~60 | ~60 | 6 组 | 10 | 2 类+9 处+2 候 | 横切宿主；异常处理器应迁 Shared |
| LYBT.Module.Auth | 25 | 52 | ~40 | — | 10 处 | 0 | — | ComputeTokenHash×4/JwtService 重复；与 Users 高耦合 |
| LYBT.Module.Users | 37 | 91 | ~75 | — | 6 处 | 2 | — | 写残留 3 处（跨模块 Service）；2 接口成员死 |
| LYBT.Module.Patients | 29 | 53 | ~40 | — | 5 组 | 1 | — | 写全走 Handler ✓；引用检查读在 Handler |
| LYBT.Module.Herbs | 38 | 72 | ~55 | — | 8 组 | 2 | 1 | 与 Formula 模板重复 ~43% |
| LYBT.Module.Formula | 36 | 54 | ~45 | — | 8 组 | 0 | 1 | 同上 |
| LYBT.Module.MedicalCase | 22 | 173 | 127 | 12 | 5 组+6 端点 | 11 | 3 | 死链 11；验证管道丢失实锤 |
| LYBT.Module.Registration | 27 | 69 | ~55 | — | 5 组 | 3 | 1 | 读走 MediatR 分裂；跨模块写绕过 Handler |
| LYBT.Module.Reports | 11 | 46 | 23 | — | 1 组 | 0 | — | ReportRepository 跨模块直查 AppDbContext |
| LYBT.WebAPI | 34 | 150 | 44 | — | 6 组 | 0 | 6 | 日志横切 3 件套 E 级；6 端点客户端 0 消费 |
| **合计** | **330** | **970** | — | — | **~51 组** | **29 符号** | **E 迁移见 §2** | |

> 注：A/B 为子审近似值（各子审只对 C/D/E 全量，A/B 抽代表性）；C/D/E 为符号级确认值。

---

## 2. 横切机制方法（日志/异常/配置/常量）与 S1 专项衔接

### 2.1 日志（衔接 S1 §2，迁移清单 M1-M10 的 Server 侧落点）

| # | 源（文件:行号） | 方法 | 级 | 应迁移去向（S1 定案） |
|---|----------------|------|----|----------------------|
| L1 | `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs:29,58` | `AddMSSqlServerSinkWithColumnOptions`/`BuildColumnOptions` | **E** | Shared.Logging `Sinks/MssqlSinkConfiguration`（S1 M2）；⚠ 配置冲突：`appsettings.Production.json:73` `autoCreateSqlTable:true` vs 代码 `:43` `false`，迁移时统一 |
| L2 | `WebAPI/Filters/ApiLoggingFilter.cs:20,69,80` | `OnActionExecutionAsync`/`SanitizeParameters`/`SanitizeValue`（+ 构造 :15） | **E** | Shared.Logging `Http/ApiLoggingFilter` + `AddLybtApiLoggingFilter`（S1 M6）；`:25` TraceIdentifier 直读 → Provider 单点 |
| L3 | `WebAPI/Middleware/CorrelationIdMiddleware.cs:40,88,98` | `InvokeAsync`/`UseCorrelationId`/`GetCorrelationId` | **E** | Shared.Logging `Http/CorrelationIdMiddleware` + `UseLybtCorrelationId`（S1 M5）；`UseCorrelationId` 注册点 `UnifiedMiddlewareConfiguration.cs` 改调用 |
| L4 | `WebAPI/Program.cs:75-89,126-145` | 双阶段 Serilog 内联构建 | **E** | Shared.Logging `LoggingBootstrap`/`AddLybtLogging` 统一入口（S1 M7） |
| L5 | **TraceIdentifier/GetCorrelationId 直读 9 处** | `BusinessExceptionHandler.cs:58,70,79,80`、`SystemExceptionHandler.cs:57,67,74,189`、`BaseApiController.cs:58`（GetRequestId）、`ControllerBaseExtensions.cs:13`、`ProblemDetailsConfiguration.cs:31`、`ServiceCollectionExtensions.cs:203`、`UnifiedMiddlewareConfiguration.cs:57,70,71`、`ApiLoggingFilter.cs:25`、`CorrelationIdMiddleware.cs:59` | **C/E** | 收敛为 Shared.Logging `ICorrelationIdProvider` + `CorrelationIdHelper` 静态辅助（S1 Q4）；`GetCorrelationId` 已有 4 份同体实现（Infra 2 + WebAPI 2） |
| L6 | `Infrastructure/Serialization/SensitiveDataJsonConverterFactory.cs:25,48,68` + `SensitiveDataJsonConverter.cs:99,107,175` | `CanConvert`/`HasSensitiveProperties`/`CreateConverter` + `Read`/`Write`/`GetPropertyName` | **E候** | 复用 `SensitiveDataMasker`（已引用 `:141`）去重内部实现（S1 M9）；仅 WebAPI 注册（`ServiceCollectionExtensions.cs:169`），Desktop 未注册——迁移需确认双端接线 |
| 保留 | `Infrastructure/Logging/LogCleanupService.cs:31,63` | `ExecuteAsync`/`CleanupOldLogsAsync` | A/B | **不迁**：依赖 AppDbContext + SQL 原生删除（`:84-87`），属 Infrastructure.Data 合理宿主（S1 同判） |

### 2.2 异常（衔接 S1 §3，迁移清单 E1-E7 的 Server 侧落点）

| # | 源（文件:行号） | 方法 | 级 | 应迁移去向（S1 定案） |
|---|----------------|------|----|----------------------|
| E1 | `Infrastructure/ExceptionHandling/SystemExceptionHandler.cs:27,86,182` | `TryHandleAsync`/`GetExceptionInfo`/`GetCorrelationId` | **E** | 迁 Shared.ExceptionHandling `Handlers/SystemExceptionHandler`（S1 E1）；`GetExceptionInfo`（:86-180，11 分支）保留为**系统异常→HTTP 第二权威**映射表并文档标注（S1 E7）；`GetCorrelationId` 上移共享辅助 |
| E2 | `Infrastructure/ExceptionHandling/BusinessExceptionHandler.cs:24,70` | `TryHandleAsync`/`GetCorrelationId` | **E** | 迁 Shared.ExceptionHandling `Handlers/BusinessExceptionHandler`（S1 E2）；两处理器仅依赖 `IExceptionHandler` + Shared.Models/ExceptionHandling，无 Infrastructure 特有依赖 ✓ |
| E3 | `WebAPI/Extensions/ApiServiceCollectionExtensions.cs:73-74` | `AddExceptionHandler<BusinessExceptionHandler>()`/`AddExceptionHandler<SystemExceptionHandler>()` | **E** | 改调用 Shared.ExceptionHandling 提供的 `AddLybtExceptionHandlers()`（S1 E3，Business 先 System 后顺序保留） |
| E4 | `WebAPI/Configuration/ProblemDetailsConfiguration.cs:17,56,67` | `AddProblemDetailsConfiguration`/`MapStatusCodeToSeverity`/`GetProblemTypeUri` + `Infrastructure/ExceptionHandling/ProblemTypeUris.cs:32` | E候 | 依赖 ErrorSeverity/ProblemTypeUris，随 E1-E3 并入 Shared.ExceptionHandling 或留 WebAPI 薄壳；⚠ S1 遗留缺口：ProblemDetails 管线与 handler 路径并行注册、handler 路径不经过 ProblemDetails（双输出源），统一时二选一 |

### 2.3 配置/常量（衔接 S1 §4）

| # | 发现 | 证据 | 级 | 收敛方向 |
|---|------|------|----|---------|
| C1 | WebAPI 自建 `JsonOptions.cs:6`（别名 LybtJsonOptions 手动 Bind，`ServiceCollectionExtensions.cs:129-130`）未归 Shared | 同上 | **C** | 迁 Shared.Configuration（S1 §4.1 同判） |
| C2 | `Infrastructure/Configuration/`（Services/Stores/Validation，被 ConfigurationController 消费）与 Shared.Configuration 职责重叠 | `SystemConfigurationService.cs:33-118`（全 A，`JsonFileConfigurationStore.cs:27-126` 双端复用 `Program.cs:159`/`LocalWebApiProgram.cs:58`） | C/E | 统一到 Shared.Configuration，WebAPI 仅留 ProblemDetailsConfiguration 薄壳 |
| C3 | `PolicyConstants`（Infrastructure/Constants/PolicyConstants.cs:5-10，6 常量）使用点 | 82 处 `[Authorize(Policy=...)]` + `AuthenticationServiceCollectionExtensions.cs:112-132` 注册 | **A**（SSOT 合规） | 保留；⚠ **DoctorOrAdminOrReceptionist 双树风险**：Server 已注册（`:132`），但 LocalWebAPI 的 `LocalJwtConfig.cs:70-89` 缺该策略而 `LocalWebAPI/Controllers/{Patients,Registrations,Reports}Controller` 引用（`PatientsController.cs:20` 等）——S1 已标 C，属 Desktop 侧（S3）优先修复项，本报告交叉确认引用点 |
| C4 | `RoleConstants.SuperAdminUserType/DefaultUserType`（`RoleConstants.cs:16,21`）全仓 0 引用 | 同上 | **D** | 删除候选 |
| C5 | ErrorCode 使用一致性 | `ErrorCode.cs:17`（SSOT）+ `ErrorCodeExtensions.ToHttpStatusCode`（异常→HTTP 第一权威）；子类 `GetHttpStatusCode` override 绕过枚举映射（S1 §3-Q4 已判，本报告复核 MedicalCase 的 `BusinessException` 构造 16 处/`NotFoundException` 3 处使用点不变） | A/E | 沿用 S1 E6：删 6 子类 override，改默认 TypedErrorCode |
| C6 | 模块 Handler 错误文案硬编码未用 ErrorMessages | Patients 各 Handler（如 `CreatePatientCommandHandler.cs:29` "患者不存在"）vs `ErrorMessages.cs:45-49` 已定义；Registration 已用 `ErrorMessages.Get`（`CancelRegistrationCommandHandler.cs:30`） | **E** | 统一 ErrorMessages（S1 §4 同向） |
| C7 | 模块 DbContext 引导代码重复 | `PatientsModule.cs:29-36` 与 `RegistrationModule.cs:30-37` 逐行相同（DatabaseOptions+ConnectionStringResolver+UseSqlServer）；8 模块 OnModelCreating 各自注册配置 | **C** | 提取共享 `AddModuleDbContext<TContext>` 扩展 |

---

## 3. 跨模块方法清单

### 3.1 双轨机制（本阶段最重要发现）：统一门面 vs 6 个域接口并存

| 机制 | 文件 | 状态 |
|------|------|------|
| **统一门面** `ICrossModuleService`（10 方法）→ `CrossModuleService`（纯转发实现）| `Infrastructure/Services/CrossModule/ICrossModuleService.cs:10-47`、`CrossModuleService.cs:10-61` | 注册 `CrossModuleServiceExtensions.cs:15`（`DatabaseServiceCollectionExtensions.cs:125`/`LocalWebApiProgram.cs:66` 调用） |
| **域接口 ×6** | `IPatient/IHerb/IUser/IAuth/IMedicalCase/IRegistrationCrossModuleService`（同目录）| 各模块实现 + `AddScoped` 注册（`PatientsModule.cs:42`/`HerbsModule.cs:44`/`UsersModule.cs:46`/`AuthModule.cs:45`/`MedicalCaseModule.cs:55`/`RegistrationModule.cs:43`） |

- **重复面**：`ICrossModuleService` 的 User 段（`:34-46`）是 `IUserCrossModuleService`（`:12-36`）的 5/7 子集；Herb/Patient 段同理——门面方法全部为逐行转发（`CrossModuleService.cs:28-60`）。
- **覆盖不对称**：门面仅覆盖 3/6 域（Patient/Herb/User）；Auth/MedicalCase/Registration 域接口**不在门面内**，消费方需自行选择机制。
- **消费方分裂**：用门面的——MedicalCase 3 服务（`MedicalCaseServiceHelper.cs:26,59`/`MedicalCaseStateService.cs:34`/`MedicalCaseCommandService.cs:37`/`PrescriptionItemService.cs:22`）、Auth `LoginCommandHandler.cs:23`、Formula 2 Handler（`BatchImportFormulasCommandHandler.cs:13`/`ValidateFormulaHerbCommandHandler.cs:14`）、Registration `QuickVisitCommandHandler.cs:15`；用域接口的——Users 4 Handler → `IAuthCrossModuleService`、Patients 4 Handler → `IMedicalCaseCrossModuleService`、MedicalCase → `IRegistrationCrossModuleService`。**同模块内两风格并存**（Registration：`QuickVisitCommandHandler.cs:15` 门面 vs `StartVisitCommandHandler.cs:19` 域接口）。
- **C 级收敛方向**：二选一——推荐**保留 6 个域接口（ISP，D5-1 文档意图）、删除统一门面**（`ICrossModuleService`+`CrossModuleService`+`CrossModuleServiceExtensions`），消费方改注入域接口（如 `LoginCommandHandler:23` 改 `IUserCrossModuleService`）；或反向全量收敛进门面。⚠ 注意 `Infrastructure/README.md:157` 声称「CrossModuleService 实现 4 个 ISP 接口」与代码（单大接口）**文档/代码冲突**，收敛时一并修正。

### 3.2 「按 ID 取基本信息」查询三模块同型（C）

`UserCrossModuleService.GetUserBasicInfoAsync:29` / `PatientCrossModuleService.GetPatientBasicInfoAsync:24` / `HerbCrossModuleService.GetHerbBasicInfoAsync:25` —— 均 AsNoTracking + `!IsDeleted` + Select BasicDto 同构。收敛方向：若删门面则此模式仍合理（每域自有查询），仅需统一投影模板；不强制合并。

### 3.3 跨模块写方法（P07 边界内但绕过 Handler 管道）

跨模块接口中的**写方法**（非只读查询）——直接改目标模块 DbContext/仓储，不经 MediatR Handler（无 ValidationBehavior/审计）：

| 接口方法 | 实现 | 调用方 |
|---------|------|--------|
| `IUserCrossModuleService.UpdateLoginFailureAsync/ResetLoginStateAsync` | `UserCrossModuleService.cs:71,85` 直改 UsersDbContext | `LoginCommandHandler.cs:141,145` |
| `IAuthCrossModuleService.RevokeAllUserSessionsAsync/RecordSecurityAuditAsync` | `AuthCrossModuleService.cs:23,26` | Users 4 Handler（`DeleteUserCommandHandler.cs:16` 等） |
| `IMedicalCaseCrossModuleService.CreateMedicalCaseForRegistrationAsync` | `MedicalCaseCrossModuleService.cs:45` | Registration `QuickVisitCommandHandler`/`StartVisitCommandHandler` |
| `IRegistrationCrossModuleService.CompleteByMedicalCaseAsync/HandleMedicalCaseCancelledAsync/LinkRegistrationToMedicalCaseAsync` | `RegistrationCrossModuleService.cs:21,31,51`（UpdateAsync+SaveChangesAsync） | `MedicalCaseStateService.cs:331,341`/`MedicalCaseCommandService.cs:131`/`Deletion.cs:46` |

- **C 级判定**：蓝图 §2.2 仅豁免 MedicalCase/Reports 模块，未覆盖「跨模块写」——现状是 P10 豁免（跨模块服务注入模块 DbContext，A-28 P1-2 定案）的延伸，但**验证/审计管道确实丢失**。收敛方向：文档化「跨模块写为受控例外（跨域编排必须直写）」或在目标模块内补 Handler 入口 + `ISender.Send`（成本高，建议前者，需技术总监裁定）。
- **同职责双实现**：`AuthCrossModuleService.RevokeAllUserSessionsAsync:23` 与 `RevokeAllUserTokensCommandHandler:28`（本地 Handler）同职责——Service 版绕过 Handler 且不记 `AllTokensRevoked` 审计，行为分叉（C）。

### 3.4 调用链清晰度（P07）结论

- 依赖方向干净：Herbs 单向被 Formula/MedicalCase 消费；Patients/Users 被 MedicalCase/Auth/Registration 消费；无模块间直接项目引用（csproj 复核无违规）。
- **唯一 P07 边界外直查**：`ReportRepository` 注入共享 `AppDbContext`（`ReportRepository.cs:16`）直查 Registration/MedicalCase 表（`:24-211` 多处）——Reports 是聚合读模型、无自有 DbContext，属**文档化例外**（建议注明）或改走 ICrossModuleService。

---

## 4. Service/Handler 边界复核（蓝图 §2.2）

### 4.1 合规确认（A-28 双轨规范化真落地，主路径健康）

- **CQRS 6 模块写全走 Handler**：Auth/Users/Patients/Herbs/Formula/Registration 全部写操作（Create/Update/Delete/Status/Toggle/Restore/BatchDelete/BatchEnable/BatchDisable/BatchImport）经 `Sender.Send`（如 `PatientsController.cs:81,107,134`；`HerbsController.cs:78,110,138,...`；`FormulasController.cs:83,112,...`）；Service 读直查（`PatientService.cs:22-51` 纯 3 读方法、`UserService.cs:21,35,43`、`HerbService.cs`/`FormulaService.cs` 各 2 读方法）——**无写残留**（A-28 已删 14 处写方法与接口声明）。
- **Users 双控制器树共享 BaseUsersController**：WebAPI/LocalWebAPI 均继承，读走 `_userService`、写走 `Sender.Send`，为最佳实践样板。
- **MedicalCase 纯 Service 例外落地**：模块 0 个 MediatR Handler（`MedicalCaseModule.cs:42-61` 无 `AddMediatR`），写读全走 Service——蓝图例外属实。

### 4.2 违规/偏差清单（符号级）

| # | 位置 | 问题 | 级 |
|---|------|------|----|
| B1 | `RegistrationCrossModuleService.cs:21-60`（CompleteByMedicalCase/HandleMedicalCaseCancelled/LinkRegistrationToMedicalCase） | 跨模块**写**直调 `_registrationRepository.UpdateAsync/SaveChangesAsync`，绕过 Handler（蓝图 §2.2「禁止写绕过 Handler 直调 Repository」命中） | C |
| B2 | `UserCrossModuleService.cs:71,85,53`（UpdateLoginFailure/ResetLoginState/UpdateUserPasswordHash） | 写操作直改 UsersDbContext，绕过 Handler 与 IUserRepository | C |
| B3 | `AuthCrossModuleService.cs:23`（RevokeAllUserSessions） | 与 RevokeAllUserTokensCommandHandler 同职责双实现，Service 版无审计 | C |
| B4 | Registration **读全走 MediatR**：`GetRegistrationQueryHandler:23`/`GetRegistrationsQueryHandler:24`/`GetWaitingQueueQueryHandler:23` | 与 Patients/Herbs/Formula「读走 Service」**模块间口径分裂**；模块无 `IRegistrationService`（仅 AGENTS.md/README 文档残留声称有） | C/E |
| B5 | Patients **读逻辑在 Handler**：`CheckPatientReferenceQueryHandler.cs:20`/`BatchCheckPatientReferenceQueryHandler.cs:19` | 查询+跨模块引用统计走 MediatR 而非 Service | C |
| B6 | Auth **读逻辑在 Query Handler**：`ValidateTokenQueryHandler.cs:28` | JWT 校验+会话查询走 MediatR 而非 Service；LocalWebAPI 有平行 `LocalValidateTokenQueryHandler` | C |
| B7 | **MedicalCase 验证管道丢失（S1 风险实锤）** | `MedicalCaseInputDtoValidator`（Shared）经 `MedicalCaseModule.cs:58` 注册后**无任何注入点**（grep 0 命中），模块无 MediatR 管道 → `ValidationBehavior`（`Infrastructure/Validation/ValidationBehavior.cs:10`）不生效；`CreateFromInputDtoAsync:106-110` 仅手工校验 TcmDiagnosis；Save/Complete/Suspend 不写审计（仅 `CancelAsync → TryWriteCancelAuditAsync` `StateService:293`） | **C/E** |
| B8 | `SecurityAuditService.RecordEventAsync:19`、`AuthCrossModuleService.RecordSecurityAuditAsync:26` | 审计写操作在 Service 直通仓储，无 Handler 包装（横切定制品，需文档裁定） | C |
| B9 | `LoginCommandHandler.Handle:62,111` 内嵌跨模块读（GetUserByUsername/VerifyPassword） | 登录编排必要复合，**可接受例外**，建议注明 | — |
| B10 | `MedicalCaseCommandService.cs:283 AddPrintLogAsync` vs `:330 RecordPrintAsync` | 打印状态回写+日志构造 ~70% 重复（唯一差异 isSuccess 分支） | C |
| B11 | `MedicalCaseServiceHelper.cs:146 EnsureCanEdit` vs `:179 EnsureCanDelete` | 逻辑近同（isAdmin→UserId+状态→抛异常），仅文案差异 | C |
| B12 | `MedicalCaseCommandService.cs:236 UpdateConsultationFields` | 手动字段复制，重复领域方法 `MedicalCase.UpdateConsultation`（`StateService:206` 用） | C |
| B13 | `MedicalCaseQueryService.cs:223 GetPatientRecentMedicalCasesAsync` vs `MedicalCaseReferenceRepository.cs:34 GetRecentAsync` | 同一「按患者取最近医案」两套实现 | C |

---

## 5. 仓储方法复核（A-28 后）

### 5.1 BaseRepository 继承者（健康）

- `BaseRepository<TEntity,TDbContext>`（`Repositories/BaseRepository.cs:14-137`）精简为 4 模板方法（`GetByIdAsync:34`/`AddAsync:53`/`UpdateAsync:74`/`DeleteAsync:93`）+ `SaveChangesAsync:118`，**无旧 GetList/BatchDelete 模板残留**。
- 6 继承者复用无重复：PatientRepository:15 / HerbRepository:13 / FormulaRepository:13 / MedicalCaseRepository:19 / MedicalCaseReferenceRepository:12 / HerbReferenceRepository:15。
- 2 个模块 override 合理（业务特化）：`FormulaRepository.GetByIdAsync:24`（Include Herbs）、`MedicalCaseRepository.Update.cs:74`（状态守卫）。
- 例外仓储成立：`SystemLogRepository`（SystemLog 非 BaseEntity，`SystemLog.cs:8` 自定义 int Id，只读接口）——裸实现合理（B）。

### 5.2 裸实现仓储的 CRUD 方法重复（可收敛）

| 仓储 | 文件:行号 | 问题 | 级 |
|------|----------|------|----|
| `AuthSessionRepository` | `AddAsync:27`/`UpdateAsync:34` | 重写 BaseRepository 逻辑（Add→SaveChanges/Update→SaveChanges，返回 void 变体），未继承模板 | C |
| `SecurityAuditRepository` | `AddAsync:12`+`SaveChangesAsync:17` | 两段式 UoW，与 BaseRepository 单段式语义不同（故意设计需文档化） | C/E |
| `UserRepository` | `GetByIdAsync:22`/`UpdateAsync:80` | 与模板逻辑重复；缺 Add/Delete（模块不需要，可接受）；`GetByIdIncludingDeletedAsync:29`/`GetPagedAsync:37` 为合法特化 | C |
| `RegistrationRepository` | `Infrastructure/RegistrationRepository.cs:14-137` | 裸实现**无自动 Save**（Add/Update 不落库，需显式 `SaveChangesAsync:133`）——与 BaseRepository 自动 Save 双语义并存，导致 Handler 写法分裂（Patients Handler 无 SaveChanges、Registration Handler 每条显式调用） | C/E |

### 5.3 镜像/内联重复

- **Repository 镜像方法**：`GetByIdIncludingDeletedAsync`/`GetPagedAsync`/`ExistsByNameAsync` 在 Patient（`PatientRepository.cs:95,23,69`）/Herb（`HerbRepository.cs:21-77`）/Formula（`FormulaRepository.cs:32-90`）/User（`UserRepository.cs:29`）4 仓储同构（S0 duplicates 同签名组）——模板化收敛候选（E）。
- **分页内联 6 处**：`QueryablePagingExtensions.cs:15` 仅 MedicalCase 用，其余 `PatientRepository.cs:55-56`/`HerbRepository.cs:54-55`/`FormulaRepository.cs:67-68`/`UserRepository.cs:66-67`/`RegistrationRepository.cs:71-72`/`MedicalCaseRepository.AuditLogs.cs:21-22` 各自内联 `.Skip((page-1)*pageSize).Take(pageSize)`——`QueryablePagingExtensions` 应为唯一分页入口（C）。

---

## 6. C 级重复清单 + 收敛方向

### 6.1 横切机制重复（含 §2 已列，此处汇总去重）

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-横1 | GetCorrelationId/GetRequestId 4 实现 | `BusinessExceptionHandler.cs:70`/`SystemExceptionHandler.cs:182`/`BaseApiController.cs:58`/`ControllerBaseExtensions.cs:12` | Shared.Logging `CorrelationIdHelper` 单点（S1 Q4） |
| C-横2 | BaseApiController 10 个响应包装 ↔ ControllerBaseExtensions 同名 1:1 委托，命名不一致（NotFound→NotFoundResponse/Forbid→ForbidResponse） | `BaseApiController.cs:60-95` vs `ControllerBaseExtensions.cs:17-139` | 只留一层（推荐 ControllerBaseExtensions，消除 BaseApiController 委托） |
| C-横3 | ControllerBaseExtensions 内部 `HandleResult<T>:85` vs `HandleResult:121` 失败分支体重复；`Success:17` vs `Success<T>:24` | 同上 | 合并泛型+非泛型变体 |
| C-横4 | ValidationBehavior 注册 6× 重复 | `AuthModule.cs:51`/`RegistrationModule.cs:52`/`PatientsModule.cs:54`/`FormulaModule.cs:56`/`HerbsModule.cs:56`/`UsersModule.cs:52`（+`LocalWebApiProgram.cs:82`） | Shared 提供 `AddLybtValidation()`/`AddLybtMediatR()` 扩展（S1 E10 同向） |
| C-横5 | 模块 DbContext 引导代码 | `PatientsModule.cs:29-36` vs `RegistrationModule.cs:30-37` 逐行相同；8 模块 OnModelCreating 各自注册 | 共享 `AddModuleDbContext<TContext>` 扩展（见 §2.3-C7） |
| C-横6 | 跨模块门面双轨 | §3.1 | 保留域接口、删统一门面（或反之） |
| C-横7 | 跨模块写绕过 Handler 管道 | §3.3/B1-B3 | 文档化受控例外或补 Handler 入口（技术总监裁定） |

### 6.2 跨模块重复

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-跨1 | 「按 ID 取基本信息」三模块同型 | `UserCrossModuleService.cs:29`/`PatientCrossModuleService.cs:24`/`HerbCrossModuleService.cs:25` | 统一投影模板；随门面决策一并定 |
| C-跨2 | 引用检查逻辑重复（单条 vs 批量） | `HerbCrossModuleService.CheckHerbReferenceAsync:59`（只数 PrescriptionItems，且死）vs `CheckHerbReferenceQueryHandler:24-103`（数处方+验方+最近 5 条，覆盖前者）；`CheckPatientReferenceQueryHandler:20` vs `BatchCheckPatientReferenceQueryHandler:19` 逐条重复 | 删死版，保留 QueryHandler 单点；批量 Handler 委托单条逻辑 |
| C-跨3 | 计数类跨模块方法 | `MedicalCaseCrossModuleService.CountUnfinishedMedicalCasesAsync/CountMedicalCasesAsync/GetRecentMedicalCasesAsync` 已被 Patients/Registration 消费，**无重复实现** ✓ | 保留（最佳实践样板） |

### 6.3 模块内重复

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-内1 | `ComputeTokenHash` 4 份拷贝 | `LoginCommandHandler.cs:196`/`LogoutCommandHandler.cs:56`/`RefreshTokenCommandHandler.cs:120`/`ValidateTokenQueryHandler.cs:67`（SHA256→hex） | 收敛为 JwtService/共享辅助单方法 |
| C-内2 | `JwtService.GenerateToken` 两重载 ~90% 重复 | `JwtService.cs:79,130` | 合并（additionalClaims 参数化） |
| C-内3 | `JwtService.RefreshToken:225` vs `ValidateAutoLoginToken:303` 近整段重复 | 同上（ValidateLifetime 之外全同） | 抽公共 token 重建方法 |
| C-内4 | Auth 登录流空值校验 vs Shared 验证器 | `LoginCommandHandler.cs:59-60` 手写 vs `LoginRequestValidator`；`CreateUserCommandHandler.cs:32-38`/`UpdateUserCommandHandler.cs:29-30`/`ChangeProfileCommandHandler.cs:32-33` 同理 | 删 Handler 内手写校验，依赖管道 |
| C-内5 | UsersDbContext/AuthDbContext OnModelCreating 重复 Infrastructure 配置 | `UsersDbContext.cs:17`（内联 ToTable/长度/索引/HasQueryFilter :41 重复 `UserConfiguration.cs:13`）；`AuthDbContext.cs:51` 重复 HasQueryFilter | 统一走 Infrastructure `Data/Configurations/` 17 个 IEntityTypeConfiguration |
| C-内6 | Herbs/Formula Handler+Validator+Repository 模板族 | 见 §8.2（~43% 方法同构） | 泛型 CRUD 基类 / 模块合并 |
| C-内7 | MedicalCase 5 组模块内重复 | §4.2-B10~B13 | 合并方法/委托领域方法 |

---

## 7. D 级死方法清单（符号级确认）

> 复核方法：全仓 src/ + tests/（含 Desktop 消费方）符号级调用计数；接口方法按「接口成员+实现」成对确认；框架模式（MediatR `Handle`、DI 构造、路由 action、`IExceptionHandler`、Mapperly 生成代码、EF 设计时、XAML）已排除。
> ⚠️ 删除决策由技术总监审批；「仅测试引用」项须连带清理测试。

### 7.1 真死方法（生产代码 0 调用，29 符号）

| # | 项目 | 类 | 方法 | 行号 | 备注 |
|---|------|----|------|------|------|
| D1 | Infrastructure | IHealthCheckService + HealthCheckService | `GetOverallStatusAsync` | `Interfaces/IHealthCheckService.cs:21`/`Services/HealthCheckService.cs:91` | 接口+实现双死；`CheckDatabaseAsync`（活，`HealthController.cs:79`）非本方法 |
| D2 | Infrastructure/Herbs | IHerbCrossModuleService + HerbCrossModuleService | `GetHerbByNameOrPinyinAsync` | `CrossModule/IHerbCrossModuleService.cs:15`/`HerbCrossModuleService.cs:40` | 0 调用；门面未委托；注释「Sync 模块」不存在 |
| D3 | Infrastructure/Herbs | IHerbCrossModuleService + HerbCrossModuleService | `CheckHerbReferenceAsync` | `:18`/`:59` | 0 调用；被 CheckHerbReferenceQueryHandler 取代；连带 `ReferenceCheckResult.cs:6` record 死 |
| D4 | Infrastructure/Users | IUserCrossModuleService + UserCrossModuleService | `UpdateUserPasswordHashAsync` | `:18`/`UserCrossModuleService.cs:53` | 0 调用（写方法残留+死双属性） |
| D5 | Infrastructure/Users | IUserCrossModuleService + UserCrossModuleService | `UserExistsAsync` | `:21`/`:64` | 0 调用 |
| D6 | Users | UserCrossModuleMapper | `ToNonNullString`/`ToLockoutEnd`（private） | `:30,35` | **S0 误报翻案：存活**——Mapperly 生成代码调用（`UserCrossModuleMapper.g.cs:13,22,37,48`），不入死清单 |
| D7 | Patients | PatientRepository | `GetPagedAsync(int,int,string?,ct)` 3 参重载 | `Infrastructure/PatientRepository.cs:23-28` | 0 调用；仅 4 参重载被 `PatientService.cs:25` 消费 |
| D8 | Registration | RegistrationMapper | `ToListDtos(List<Registration>)` | `Mappers/RegistrationMapper.cs:21` | 0 调用；`GetRegistrationsQueryHandler:33` 用 `.Select(ToListDto)` |
| D9 | Registration | RegistrationConnectionManager | `CountConnections` | `Hubs/RegistrationConnectionManager.cs:28` | 生产 0 调用；仅测试（`RegistrationConnectionManagerTests.cs:48-49`） |
| D10 | Registration | RegistrationConnectionManager | `TryGetDoctorId(string,out Guid)` | `Hubs/RegistrationConnectionManager.cs:24` | 0 调用；`RegistrationHub.cs:63` 用私有同名重载 |
| D11 | MedicalCase | MedicalCaseMapper | `ToPrescriptionEntity`/`UpdatePrescriptionEntity` | `Mappers/MedicalCaseMapper.cs:105,120` | 生产 0 调用；仅测试（`MedicalCaseMapperTests.cs:284,303,337,358`）；`PrescriptionItemService:81` 手工构建 Prescription |
| D12 | MedicalCase | MedicalCaseQueryService | `GetConsultationListAsync`/`GetPrescriptionListAsync`/`GetBatchDetailDtosAsync`/`GetPatientConsultationsAsync`/`GetPatientPrescriptionsAsync` | `:94,112,382,392,420` | 唯一调用方=§7.2 的 0 消费端点（死链） |
| D13 | MedicalCase | MedicalCaseCommandService | `AddPrintLogAsync` | `:283` | 唯一调用方=死端点 AddPrintLog:241（RecordPrintAsync:330 存活） |
| D14 | MedicalCase | MedicalCaseRepository | `GetPatientConsultationsPagedAsync`/`GetPatientPrescriptionsPagedAsync`/`GetBatchWithDetailsAsync` | `:73,87,273` | 唯一调用方=死 Service 方法（D12） |
| D15 | Infrastructure | RoleConstants | `SuperAdminUserType`/`DefaultUserType` | `Constants/RoleConstants.cs:16,21` | 常量 0 引用 |

**已排除的 S0 候选（复核为活）**：`EntityOptimizationExtensions.ConfigureGlobalQueryFilter:58`（反射 `EntityOptimizationExtensions.cs:41`）、`ControllerBaseExtensions.CreateModuleErrorResponse:141`（`:99` 调用）、`SystemExceptionHandler.GetExceptionInfo:86`（`:45` 调用）、`FormulaMapper.ToHerbItemDto:71`（`FormulaMapper.cs:63` 类内调用）、`MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync:113`（`CommandService.cs:169`）、全部构造函数（DI/继承链）、全部 `Handle`（MediatR）、`SensitiveDataJsonConverter*`（JsonConverterFactory 注册 `ServiceCollectionExtensions.cs:169`）、`AppDbContextFactory.CreateDbContext`（EF 设计时）、3 个中间件构造（UseMiddleware 反射）。

### 7.2 「路由存活但客户端 0 消费」端点（判 C 不判 D，需产品确认）

| 端点 | 位置 | 状态 |
|------|------|------|
| `BaseMedicalCasesController.GetPatientConsultations/GetPatientPrescriptions/GetConsultations/GetPrescriptions/GetBatchDetails/AddPrintLog` | `:89,104,119,131,143,234` | 双端子类均未 override → **继承活动路由**；但 Desktop Refit 契约（`IMedicalCaseApi`/`IApiClientMedicalCases`）**无对应方法**，仅 newman/postman 集合覆盖 → 0 客户端消费。删除前需产品确认无外部 API 消费者；连带 §7.1-D12/D13/D14 死链整体清理 |
| `WebAPI HealthController.GetDetailedHealth:76` | 路由存活；0 Refit 契约、0 Desktop 调用 | 同上 |
| `WebAPI ReportsController.GetIncomeTrend:91/GetConsultationTrend:110/GetDoctorPerformance:129/GetHerbRanking:147/GetPatientFlow:166` | 路由存活；`IReportsApi.cs:8-21` 仅 3 个 daily 契约、LocalWebAPI 仅 3 个 daily、0 Desktop 调用 | 同上（与 A-16 结构审计一致） |

### 7.3 可疑项（无法完全确认）

| 项 | 说明 |
|----|------|
| `BaseCrudController.GetList:29/BatchDelete:68`（及 GetById/Delete/ToggleStatus/Restore）基类桩 | 全部子类 override → 基类方法体（`throw new NotSupportedException`）运行时不可达；但为子类 override 的**契约签名**，删除破坏编译；建议保留标注「防呆桩」，不按死方法删除 |
| `LocalWebAPI RegistrationsController.Create:37` | 无显式 [Http] 动词属性，依赖控制器级路由；可能产生动词歧义，非死方法，属路由健壮性观察项（Desktop 侧，S3） |

---

## 8. 合并候选分析（方法级证据）

### 8.1 模块内目录合并

| 项 | 现状 | 证据 | 方向 |
|----|------|------|------|
| Mappers 位置不统一 | Herbs/Formula/Patients/Users 在 `Application/Mappers/`（静态 partial，不走 DI）；MedicalCase/Registration 在模块根 `Mappers/`（实例 partial，单例注册） | `PatientMapper.cs:19` vs `RegistrationMapper.cs:16`；`MedicalCaseMapper.cs:21` | 统一目录 + 统一 DI 策略（E） |
| MedicalCase Services 目录 | 7 个 Service：`MedicalCasePrescriptionService`（单方法 71 行纯委托）→ 并入 CommandService 或 PrescriptionItemService（E）；`CommandService.SetPrescriptionFlagAsync:143`/`QueryService.GetListAsync:40` 接口成员仅内部调用 → 降 private/瘦接口 | 同上 | 7 文件→4 核心 |
| MedicalCase Mapper 瘦身 | 删 D11 两方法后，`PrescriptionItemService:81` 手工构建 Prescription 与 Mapper 可统一 Mapperly | 同上 | 统一映射（E） |
| WebAPI `Services/` 目录为空 | glob 0 命中 | — | 删空目录（E） |
| Auth `Models/` 目录为空、无 Controllers（控制器在 WebAPI）vs Users 有 `Controllers/BaseUsersController` | `AuthModule` 目录树 vs `UsersModule` | 布局规则统一（E） |
| Controllers 布局 | 仅 Users/MedicalCase/Registration 三模块有 `Controllers/Base*Controller.cs`，其余 5 模块无模块级控制器 | `src/Server/Modules/*/Controllers/` 仅 3 文件 | 统一「双树共享基类」或「WebAPI 持有」二选一（E） |

### 8.2 跨模块合并候选

**Herbs + Formula →「药材方剂」模块（可行，证据充分）**：

| 维度 | 数据 |
|------|------|
| 方法总量 | Herbs ≈86 + Formula ≈66 + 命令 record 19 ≈ **152** |
| 逐字/同构重复 | CRUD/Toggle Handler 5 对 + BatchEnable/Disable 2 对（48 行逐字）+ Validator 7 对 + Service 1 对（42 行孪生，`HerbService.cs:21-41` vs `FormulaService.cs:21-41`）+ Repository 3 镜像方法 + Mapper 3/4 = **≈62-65 方法跨模块同构（≈43%）** |
| 可消除方法 | 泛型基类收敛后可净消除 ≈45-50 方法 |
| 已存在收敛设施 | `BatchOperationHandlerBase`（`BatchOperationHandlerBase.cs:10-110`）已被 Users（`BatchEnableUsersCommandHandler.cs:10-50`）/Patients（`BatchDeletePatientsCommandHandler.cs:13-59`）采用——Herbs/Formula 的 Enable/Disable/Delete 差异正是设施覆盖的钩子差异 |
| 依赖方向 | 单向：Herbs 被 Formula 消费（经门面 4 方法）；合并后转模块内调用，门面 4 个委托可删 |
| DbContext | 同库同连接，`HerbsDbContext.cs:35,81-89` 已含 FormulaHerbItem/Formula 配置 → 单 DbContext 无迁移障碍 |
| 附带修正 | **FormulaCrossModuleService/FormulaReferenceRepository 不存在**（S0 任务前提勘误）：Formula 仅经门面消费 Herbs，无自有跨模块服务 |

**是否含 Patients**：方法级证据支持三合一「可」，但 Patients 无 BatchEnable/BatchDisable、`PatientService` 另有大体量，边际收益低于 Herbs+Formula；建议**分两步**：先 Herbs+Formula，Patients 视泛型 CRUD 基类落地效果再定。

**Auth × Users 合并（高耦合证据）**：登录流 `LoginCommandHandler` 大量依赖 Users 域（GetUserByUsername/VerifyPassword/UpdateLoginFailure/ResetLoginState）→ 合并可直接消除门面层；用户域 Mapperly 三件套（`AuthUserMapper`/`UserMapper`/`UserCrossModuleMapper`）映射同一实体族可并为一个共享映射器；命令族（Auth 6 + Users 10）均为瘦 Handler+验证器+Mapperly 组合。**建议**：Auth 与 Users 合并为「认证用户」模块（或 Users 吸收 Auth 的 JwtService/SecurityAudit 资产）。

**Registration 读路径统一**：Get/GetList/Queue QueryHandler 与 Patients 读 Service 功能等价 → 统一为「读走 Service」（与蓝图 §2.2 一致）消除 QueryHandler 样板（E）。

**BaseRegistrationsController 去重**：`StartVisit:98`/`Cancel:114`/`QuickVisit:130` 方法体被 Remote `RegistrationsController.cs:77/95/36` 原样覆写重复（Local 已走 base 委托）→ Remote 改为仅加 Attribute（C）。

**LocalWebAPI ReportsController**：`:32-42,54-63,75-79` 内联复刻 `ReportService.GetDailyIncomeAsync:20-31` 等 3 方法组装逻辑，而 `LocalWebApiProgram.cs:76` 已注册 `AddReportsModule`（IReportService 可解析）→ 改注入 IReportService（C，Desktop 侧落 S3/S4）。

### 8.3 Infrastructure 职责评估

- **过载点**：① `ExceptionHandling/` → 迁 Shared.ExceptionHandling（S1 定案）；② `Serialization/SensitiveDataJsonConverter*` → 可迁 Shared.Logging.Masking（E 候）；③ 其余目录（Web/Configuration/Caching/BatchOperations/Logging）被双端消费，属「跨层基础设施」非「模块私有」——**无该属模块的方法**。
- **必须保留的公共底座**：AppDbContext + 17 个 IEntityTypeConfiguration + Migrations 集中于此，是 S4 模块合并的保留前提。
- **合并红线**：Infrastructure 被 Desktop LocalWebAPI 直接引用（`LocalWebApiProgram.cs:52,58,66,87` 注册 SystemLogRepository/JsonFileConfigurationStore/CrossModuleService/HealthCheckService）——任何移动/合并必须保证 Desktop 侧兼容。
- **删减候选**：§7.1 死方法 + `ReferenceCheckResult` + `RoleConstants` 2 死常量，随 S4 一并清理。

---

## 9. 统计汇总

| 维度 | 数值 |
|------|------|
| 审查项目/方法 | 10 项目 / **970 方法**（Infrastructure 210 / Auth 52 / Users 91 / Patients 53 / Herbs 72 / Formula 54 / MedicalCase 173 / Registration 69 / Reports 46 / WebAPI 150） |
| C 级重复 | **~51 组/处**：横切 7（CorrelationId 4 实现、BaseApiController↔Extensions 双层、HandleResult 重复、ValidationBehavior 注册 6×、DbContext 引导 8×、门面双轨、跨模块写管道缺失）+ 跨模块 3 + 模块内 ~15（ComputeTokenHash×4、JwtService 2 对、验证器模板×7 对、Repository 镜像×4、MedicalCase 5 组等）+ 仓储 5 |
| D 级死方法 | **29 符号**（接口+实现成对）：真死 14 项 + MedicalCase 死链 9 项（D12-D14）+ Mapper 测试引用 2 项 + 常量 2 项 + 接口 2 项；另 6 端点为「路由存活/0 客户端消费」判 C 待产品确认；S0 候选 ~250 项中 90%+ 翻案为活（框架/DI/路由/生成代码） |
| E 级可集中 | 日志 5（L1-L5）+ 异常 4（E1-E4）+ 配置 3（C1-C3）+ 验证注册 1 + 错误文案 1 + Mapper 位置/目录 若干；**与 S1 衔接：Server 侧应迁移方法清单见 §2（L1-L6/E1-E4/C1-C3），全部纳入 S1 迁移计划 M2/M5/M6/M7/E1/E2/E3/E9 的 Server 落点** |
| Service/Handler 边界 | 主路径合规（CQRS 6 模块写走 Handler/读走 Service，A-28 落地）；**违规 13 项**（B1-B13）：跨模块写绕过管道 5、读在 Handler 3、Registration 读走 MediatR 模块分裂、MedicalCase 验证管道丢失实锤（最高风险）、模块内重复 4 |
| 仓储 | BaseRepository 模板健康（无重复）；**裸实现 CRUD 重复 4 仓储**（AuthSession/SecurityAudit/User/Registration）+ 分页内联 6 处 + 镜像方法 4 仓储 |
| 合并候选 | **Herbs+Formula 合并可行**（~43% 同构，净消除 45-50 方法）；**Auth+Users 合并可行**（凭证域高耦合）；Patients 视泛型基类效果分步；Registration 读路径统一；双树基类布局统一 |
| 遗留风险 | ① **MedicalCase 验证管道丢失**（B7）——`MedicalCaseInputDtoValidator` 无注入点，建议优先补 Service 层手动验证或引入局部管道；② 跨模块写绕过 Handler 无文档化例外（B1-B3）；③ DoctorOrAdminOrReceptionist 双树策略缺口（S1 已标，Desktop 侧优先）；④ 门面双轨（§3.1）需技术总监定方向；⑤ 6 死链端点删除需产品确认；⑥ `Infrastructure/README.md:157` 门面 ISP 描述与代码冲突，收敛时修正 |

**未 commit**：本报告由技术总监统一提交；S4 整合（合并蓝图 + 执行批次）与 C/D/E 清理执行需另立任务书并经审批。
