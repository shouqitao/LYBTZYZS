# A-22 设计蓝图逐项目验证报告（Mimo Code 独立分析）

> 独立分析者：Mimo Code｜任务书：`task-a22-blueprint-perclass-verify-2026-08-08.md`
> 日期：2026-08-08｜基线：蓝图 v1.0 `14-structure-design-blueprint.md` + 16 ADR + 架构文档 + 模块级审计（A-16/A-21 成果）
> 性质：**只读审计**，未修改任何代码，未 commit
> 方法：6 个并行审计子代理（general）逐类判定 + 主代理符号级复核（rg 全仓引用 + codebase-memory 图谱 + 源码走查）

---

## 0. 方法说明

| 项 | 说明 |
|----|------|
| 范围 | 34 项目全部顶层类型：**1444 个类型声明**（嵌套/内部类型计入宿主类，测试内 22 个嵌套辅助类排除） |
| 判定标准 | A=有明确依据（蓝图§/ADR/架构文档/US 需求）；B=隐含依据（支撑性实现，从属有依据类）；C=依据存疑（僵尸/冗余/错层）；D=无依据（孤儿类） |
| 验证工具 | rg 全仓符号级引用（含 DI 注册/EF 反射注册/Prism Module/XAML 绑定/测试引用）+ codebase-memory 图谱（1314 Class 节点）|
| 排除项 | MediatR Handler / EF EntityTypeConfiguration / Controller 路由 / Use*·Add* 扩展方法 / Prism Module / xUnit 测试类——框架反射注册视为存活 |
| 交叉验证 | 6 子代理产出与主代理独立复核（6 处 VM 越层逐一读源码验证；死代码项 rg 复证；A-21 已修项确认不重报）|

---

## 1. 全量汇总（34 项目 / 1422 类型判定）

| 层 | 项目 | 类型数 | A | B | C | D | 孤儿类（D） |
|----|------|-------|----|----|----|----|------------|
| Shared | LYBT.Entities | 18 | 18 | 0 | 0 | 0 | — |
| Shared | LYBT.Shared.Models | 144 | 126 | 18 | 0 | 0 | — |
| Shared | LYBT.Shared.Configuration | 34 | 19 | 14 | 0 | 1 | MonitoringOptions |
| Shared | LYBT.Shared.ExceptionHandling | 7 | 7 | 0 | 0 | 0 | — |
| Shared | LYBT.Shared.Logging | 11 | 7 | 3 | 1 | 0 | — |
| Server | LYBT.Infrastructure | 71 | 45 | 26 | 0 | 0 | — |
| Server | LYBT.WebAPI | 34 | 18 | 16 | 0 | 0 | — |
| Server | LYBT.Module.Auth | 26 | 22 | 2 | 2 | 0 | — |
| Server | LYBT.Module.Users | 30 | 26 | 2 | 2 | 0 | — |
| Server | LYBT.Module.Patients | 23 | 21 | 1 | 1 | 0 | — |
| Server | LYBT.Module.Herbs | 23 | 17 | 4 | 2 | 0 | — |
| Server | LYBT.Module.Formula | 21 | 17 | 2 | 2 | 0 | — |
| Server | LYBT.Module.MedicalCase | 22 | 9 | 1 | 12 | 0 | — |
| Server | LYBT.Module.Registration | 27 | 24 | 2 | 1 | 0 | — |
| Server | LYBT.Module.Reports | 11 | 9 | 2 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Contracts | 111 | 86 | 23 | 1 | 1 | IApiRouter |
| Desktop | LYBT.Desktop.Foundation | 120 | 76 | 32 | 10 | 2 | ApiErrorHandler, QueryStringBuilder |
| Desktop | LYBT.Desktop.Infrastructure | 137 | 95 | 32 | 1 | 9 | ApiResponseHelper, SensitiveInfoFilter, ResponsiveLayoutBehavior, ApiRouter, UnfinishedCaseDialog, UnfinishedCaseDialogViewModel, ConfigurationExtensions, RegistrationNavParams, BaseDialogWindow |
| Desktop | LYBT.Desktop.Controls | 47 | 24 | 22 | 0 | 1 | SuggestionType |
| Desktop | LYBT.Desktop.Printing | 17 | 12 | 5 | 0 | 0 | — |
| Desktop | LYBT.LocalWebAPI | 24 | 23 | 1 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Auth | 10 | 7 | 3 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Users | 15 | 10 | 5 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Patients | 31 | 14 | 5 | 0 | 12 | IMedicalCaseStartCoordinator, IPatientSearchCache, IPatientValidator, PatientListToDetailMapper, PatientSearchCache, CacheEntry, PatientSearchManager, SearchCompletedEventArgs, MedicalCaseStartCoordinator, StartResult, StartResultData, PatientValidator |
| Desktop | LYBT.Desktop.Herbs | 13 | 9 | 4 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Formula | 16 | 11 | 3 | 0 | 2 | FormulaItem, FormulaHerbItem |
| Desktop | LYBT.Desktop.MedicalCase | 56 | 43 | 12 | 0 | 1 | MedicalCaseChangeTracker |
| Desktop | LYBT.Desktop.Registration | 10 | 8 | 2 | 0 | 0 | — |
| Desktop | LYBT.Desktop.Admin | 18 | 7 | 7 | 4 | 0 | — |
| Desktop | LYBT.Desktop.Clinical | 23 | 11 | 11 | 1 | 0 | — |
| Desktop | LYBT.Desktop.Shell | 55 | 45 | 9 | 1 | 0 | — |
| Tests | LYBT.Tests.Architecture | 8 | 7 | 1 | 0 | 0 | — |
| Tests | LYBT.Tests.Server | 70 | 47 | 23 | 0 | 0 | — |
| Tests | LYBT.Tests.Desktop | 139 | 89 | 29 | 21 | 0 | — |
| **合计** | **34** | **1422** | **A=1009** | **B=322** | **C=62** | **D=29** | **29 个孤儿类** |

### 1.1 分层汇总

| 层 | 类型数 | A | B | C | D | A/B 占比 |
|----|-------|----|----|----|----|---------|
| SHARED（5 项目） | 214 | 177 | 35 | 1 | 1 | 99.1% |
| SERVER（10 项目） | 288 | 208 | 58 | 22 | 0 | 92.4% |
| DESKTOP（16 项目） | 703 | 481 | 176 | 18 | 28 | 93.5% |
| TESTS（3 项目） | 217 | 143 | 53 | 21 | 0 | 90.3% |

### 1.2 C/D 级孤儿类与待决策项全清单

**D 级孤儿类（29 个，建议删除/合并）**：

| 类 | 所在项目 | 证据 | 关联 |
|----|---------|------|------|
| MonitoringOptions | Shared.Configuration | 嵌套于 DatabaseOptions，全仓 0 读取/0 校验/0 文档 | 删或接慢查询监控 |
| IApiRouter | Desktop.Contracts | 仅 DI 注册 + 测试引用，生产 0 消费 | 与 ApiRouter 同删 |
| ApiErrorHandler | Desktop.Foundation | 0 引用（src+tests），异常映射实际走 ClientErrorMessageMapper | 新发现 |
| QueryStringBuilder | Desktop.Foundation | 0 引用，URL 构建由 HttpApiClientBase 内联 | 新发现 |
| ApiResponseHelper | Desktop.Infrastructure | 0 引用（Http 下沉议题中已死） | Http 目录 |
| SensitiveInfoFilter | Desktop.Infrastructure | 0 引用 | 模块审计 §D 复证 |
| ResponsiveLayoutBehavior | Desktop.Infrastructure | 0 引用，功能重复 Controls/ResponsiveLayoutHelper | 模块审计 §D 复证 |
| ApiRouter | Desktop.Infrastructure | 注册孤儿（ServiceCollectionExtensions:211），生产 0 消费 | 模块审计 §D 复证 |
| UnfinishedCaseDialog / UnfinishedCaseDialogViewModel | Desktop.Infrastructure | 注册孤儿（App.xaml.cs:102-103），ShowUnfinishedCaseDialogAsync 0 调用点 | 模块审计 §D 复证 |
| ConfigurationExtensions | Desktop.Infrastructure | AddInfrastructureConfiguration 空实现 + 0 调用点 | 模块审计 §D 复证 |
| RegistrationNavParams | Desktop.Infrastructure | 0 引用（同文件 UserManagementNavParams 存活） | 新发现 |
| BaseDialogWindow | Desktop.Infrastructure | 0 引用，Prism 默认对话框机制替代 | 新发现 |
| SuggestionType | Desktop.Controls | 0 生产引用（仅测试+README） | 新发现 |
| IMedicalCaseStartCoordinator / IPatientSearchCache / IPatientValidator | Desktop.Patients | 接口对应实现全部死代码，0 消费者（仅 PatientsModule 注册） | 4 死组件链 |
| PatientListToDetailMapper | Desktop.Patients | 内嵌 PatientRepository:145-152，0 调用 | 死 Mapper |
| PatientSearchCache / CacheEntry | Desktop.Patients | 195 行，0 消费者 | 4 死组件链 |
| PatientSearchManager / SearchCompletedEventArgs | Desktop.Patients | 299 行，0 消费者 | 4 死组件链 |
| MedicalCaseStartCoordinator / StartResult / StartResultData | Desktop.Patients | 197 行假实现（CheckUnfinishedCaseAsync 直接 return null），0 消费者 | 4 死组件链 |
| PatientValidator | Desktop.Patients | 182 行，0 消费者 | 4 死组件链 |
| FormulaItem / FormulaHerbItem | Desktop.Formula | 桌面模型 0 实例化（仅死类互引用） | 死簇残留 |
| MedicalCaseChangeTracker | Desktop.MedicalCase | 0 生产引用（仅测试实例化） | 模块审计 P1 |

**C 级待决策项（63 个，按簇归纳）**：
1. **Server 模块「类活但含死方法」22 个**（见 §3 各模块表）——处置建议均为删死方法/瘦接口，非删类
2. **Desktop 6 处 VM→IApiClient 越层**（Shell 1 + Admin 4 + Clinical 1，见 §4.2 重点核查项 2）
3. **Infrastructure Http 目录错层**（LoggingHttpHandler 存活应下沉 Foundation；ApiResponseHelper 死）
4. **Shared.Logging.ServiceCollectionExtensions.AddSharedLogging 双重载 0 调用**（与宿主手工注册冗余）
5. **Foundation 注册孤儿 IApiService/ApiService/RequestDeduplicator**（注册 :103，生产 0 消费）
6. **Foundation 3 个惰性 AuthEvents**（LoginSucceeded/LoginFailed/SessionExpired，0 发布 0 订阅）
7. **Contracts.UnfinishedCaseChoice**（随 UnfinishedCaseDialog 死链）
8. **Tests.Desktop Traits 特性体系 18 类 + UserJourneyTestBaseShared**（0 消费测试基建）
9. **LocalWebApiProgram.RunAsync 死方法**（双入口合并）
10. **Registration 命名空间复数漂移**（LYBT.Desktop.Registrations vs 项目单数）

---

## 2. SHARED 层（5 项目 / 214 类型）

> 权威文档：`08-shared.md`（A-18 P1-7 重写版）+ `06-error-handling.md` + `07-configuration.md`

### 2.1 LYBT.Entities（18 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AuthSession | A | 蓝图§1 实体清单；AppDbContext:43 / AuthDbContext:16 DbSet | 保留 |
| SecurityAuditLog | A | 蓝图§1；AppDbContext:74 / AuthDbContext:19 DbSet + SecurityAuditService 写入 | 保留 |
| BaseEntity | A | 08-shared「BaseEntity 通用字段」(Id/CreatedAt/RowVersion/IsDeleted)；10 实体派生 | 保留 |
| IAuditableEntity | A | EntityOptimizationExtensions 消费（审计字段统一） | 保留 |
| ISoftDeletable | A | EntityOptimizationExtensions（软删全局过滤）消费 | 保留 |
| SystemLog | A | AppDbContext:77 DbSet + SystemLogRepository + SystemLogConfiguration | 保留 |
| Consultation | A | 蓝图§1；AppDbContext:57 / MedicalCaseDbContext:20 DbSet | 保留 |
| FormulaHerbItem | A | 蓝图§1；HerbsDbContext:35 DbSet | 保留 |
| Formula | A | 蓝图§1；AppDbContext:68 / FormulaDbContext:12 DbSet | 保留 |
| Herb | A | 蓝图§1；AppDbContext:65 / HerbsDbContext:19 DbSet | 保留 |
| MedicalCaseAuditLog | A | 蓝图§1；AppDbContext:80 / MedicalCaseDbContext:32 DbSet | 保留 |
| MedicalCase | A | 蓝图§1 + ADR-0001 聚合根；唯一充血模型（Complete/Suspend/SoftDelete） | 保留 |
| MedicalCasePrintLog | A | 蓝图§1；打印规则（2026-08-03 定案） | 保留 |
| Patient | A | 蓝图§1；AppDbContext:48 / PatientsDbContext:14 / HerbsDbContext:32 DbSet | 保留 |
| PrescriptionItem | A | 蓝图§1；AppDbContext:62 / MedicalCaseDbContext:26 DbSet | 保留 |
| Prescription | A | 蓝图§1；AppDbContext:60 / MedicalCaseDbContext:23 DbSet | 保留 |
| Registration | A | 蓝图§1；AppDbContext:71 / RegistrationDbContext:14 DbSet | 保留 |
| ApplicationUser | A | 蓝图§1（User）；Identity UserManager 托管 | 保留 |

汇总：A=18 B=0 C=0 D=0；重点：3 个实体含死方法（FormulaModel.RemoveHerb/MarkShared :158/:174、HerbModel.UpdatePrice :180 零调用）→ 类保留，下次清理批次删方法。

### 2.2 LYBT.Shared.Models（144 类型）——八目录归属

> 八目录：Attributes(3) / Contracts(~90) / Enums(14) / Primitives(7) / Utilities(8) / Validators(9) / DTOs(2) / Extensions(1)

| 目录 | 类型 | 依据等级 | 设计依据 | 处置 |
|------|------|---------|---------|------|
| Attributes | SensitiveDataAttribute / SensitiveDataType / MaskingMode | A | 08-shared §SensitiveDataAttribute + NFR-SEC-004 分级；PatientModel [SensitiveData] 实标 + JsonConverterFactory/Serilog 双消费 | 保留 |
| Contracts/Auth | AutoLoginRequest / LoginRequest / LoginResponse / LogoutRequest / RefreshTokenRequest / ChangePasswordRequest / ValidateTokenResponse / SecurityAuditEvent | A | API 契约双端共享（蓝图§1.1）；AuthController/LoginCommandHandler/IApiClientAuth/LocalWebAPI 全链消费 | 保留 |
| Contracts/Common | ApiResponse+ApiResponse\<T\> / Result+Result\<T\> / PagedResult\<T\> / OperationResultDto / ImportResultDto / BatchDeleteInputDto / ErrorDetail / BatchOperationFailureItem / HealthCheckResponse / HealthStatusDto / HealthStatistics / HerbBasicDto / PatientBasicDto / IAuditable / IEntityInputDto | A | 蓝图§1.1 点名（ApiResponse 契约信封/Result 服务返回/IEntityInputDto A-06）；全仓消费 | 保留 |
| Contracts/Consultation | ConsultationDetailDto / ConsultationInputDto | A | 08-shared；MedicalCase 模块消费 | 保留 |
| Contracts/Diagnostics | EnableDebugModeRequest / SetLoggingLevelRequest | A | 08-shared；双端 DiagnosticsController 消费 | 保留 |
| Contracts/Health | DatabaseHealthCheckResult / HealthStatus | A | 08-shared；HealthCheckService/HealthController 消费 | 保留 |
| Contracts/Formula | 11 类（InputDto/ListDto/DetailDto/HerbItemDto×2/Import×4/ValidateFormulaHerbInputDto） | A | 08-shared + Formula 模块全链消费 | 保留 |
| Contracts/Herbs | 11 类（含 IHerbItem/IHerbItemEditable） | A | 08-shared；Herbs 模块 + HerbItemControlViewModel 消费 | 保留 |
| Contracts/MedicalCase | 12 类 | A | 08-shared；三 Service + 13b-api-endpoints + Clinical 工作台消费 | 保留 |
| Contracts/Patients | 9 类 | A | 08-shared（Excel 导入 B-03）；Patients 模块 + MedicalCase 跨模块消费 | 保留 |
| Contracts/Prescriptions | 4 类 | A | 08-shared；处方服务/打印链消费 | 保留 |
| Contracts/Registration | 5 类 | A | 08-shared + US-REG-008/D-01；Registration 模块消费 | 保留 |
| Contracts/Reports | 9 类 | A | 08-shared + B-04；ReportService:20-108 全量消费 | 保留 |
| Contracts/Users | 7 类 | A | 08-shared；Users 模块 + 桌面用户管理消费 | 保留 |
| DTOs/Users | UserBasicDto / UserCredentialDto | A | 08-shared；CrossModuleService/AuthUserMapper 消费 | 保留 |
| Enums | UserRole/Gender/HerbRole/DecocteMethod/FormulaType/FormulaValidationStatus/MedicalCaseStatus/MedicalCaseQueryType/RegistrationSource/RegistrationStatus/ReportGranularity/DuplicateStrategy/PasswordStrength/CommonStatus | A | 蓝图§1.1「共享单源样板」；双端引用（UserRole 40+/DecocteMethod 30+/CommonStatus 100+） | 保留 |
| Primitives | ErrorCode / ErrorCategory / ErrorSeverity / ErrorCodeExtensions / ErrorMessages / ValidationConstants / UserConstants | A | 蓝图§1.1 ErrorCode SSOT + 06-error-handling MCCEE | 保留 |
| Extensions | DtoConversionExtensions | B | 支撑性扩展（DetailDto→InputDto）；存活（MedicalCaseCommandService:46 ToInputDto） | 记录归属 |
| Utilities | CacheExtensions / PasswordHelper(+3 嵌套) / PasswordPolicyValidator / Policy / PinYinHelper / MedicalCaseBusinessRules | B | 支撑性工具（Cache/密码策略/拼音/业务规则库）；全被消费 | 记录归属 |
| Validators | LoginRequestValidator + 7 个模块 DtoValidator | B | 支撑性 FluentValidation；Server Module.cs 注册（Herbs 注册未执行已记模块审计 P1） | 记录归属 |

汇总：A=126 B=18 C=0 D=0；重点：零孤儿（含 2 个易误判类 DtoConversionExtensions/CacheExtensions 经扩展方法存活）；FormulaDetailDto.GetHerbNamesList 类内死方法。

### 2.3 LYBT.Shared.Configuration（34 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| ConnectionStringResolver | A | 蓝图§1.1 三级回退（Database:ConnectionString→DefaultConnection→CONNECTION_STRING）；8 模块引用 | 保留 |
| ApiClientOptions/CardReaderOptions/ClientSessionOptions/ClinicSettingsOptions/OfflineModeOptions | A | 07-configuration Client Options；ClientConfigurationExtensions/CardReaderService 消费 | 保留 |
| JwtOptions | A | 07-configuration Common；JwtService/JwtOptionsValidator 消费 | 保留 |
| AppInfoOptions/CorsOptions/DatabaseOptions/DefaultPasswordOptions/DesktopUpdateOptions/LocalJwtOptions/LoggingOptions/MemoryCacheOptions/SecurityOptions/SessionOptions/SwaggerOptions/SystemAdminOptions | A | 07-configuration Server Options；各自消费点验证 | 保留 |
| ConnectionPoolOptions/RetryPolicyOptions | B | DatabaseOptions 嵌套；Validator+DI 消费 | 记录归属 |
| LogCleanupOptions | B | LoggingOptions 嵌套；LogCleanupService:24 消费（F-08） | 记录归属 |
| RateLimitingOptions/RateLimitOptions/LoginRateLimitOptions/ApiRateLimitOptions/AccountLockoutOptions | B | SecurityOptions 嵌套；AccountLockout 实消费，GlobalLimit/WhitelistedIPs 属性死 | 记录归属 |
| DatabaseOptionsValidator/JwtOptionsValidator/LocalJwtOptionsValidator/SecurityOptionsValidator | B | IValidateOptions + ValidateOnStart | 记录归属 |
| ClientConfigurationExtensions/ServerConfigurationExtensions | B | DI 扩展 | 记录归属 |
| **MonitoringOptions** | **D** | **0 引用**（rg 全仓；Validator/DI 均不读；07-configuration 无文档） | **建议删除**（或接慢查询监控） |

汇总：A=19 B=14 C=0 D=1；孤儿类：[MonitoringOptions]；另：RateLimitingOptions.GlobalLimit/WhitelistedIPs 属性级死代码（限流中间件用硬编码）。

### 2.4 LYBT.Shared.ExceptionHandling（7 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AppException | A | 蓝图§1.1 异常继承树；BusinessExceptionHandler 多态匹配 | 保留 |
| BusinessException | A | 06-error-handling（400）；Handler + 模块 Service 抛出 | 保留 |
| ValidationException | A | 06-error-handling（400 字段级）；ValidationBehavior 消费 | 保留 |
| NotFoundException | A | 06-error-handling（404 + 静态工厂）；多处抛出 | 保留 |
| ConflictException | A | 06-error-handling（409）；BR-001 医案约束/挂号冲突/身份证重复 | 保留 |
| ApiException | A | 06-error-handling（502/503）+ ADR-0009/0010 本地降级 | 保留 |
| UnauthorizedException | A | 06-error-handling（401）+ ADR-0004；**当前 0 生产抛出点（8 个静态工厂无调用方，401 走 SystemExceptionHandler 映射路径）** | 保留+建议核查 Auth 401 语义 |

汇总：A=7 B=0 C=0 D=0；重点：UnauthorizedException 文档-实现偏差（见 §6）。

### 2.5 LYBT.Shared.Logging（11 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| ICorrelationIdProvider | A | 08-shared §Logging + 06-error-handling 提供者体系 | 保留 |
| ActivityCorrelationIdProvider | A | 08-shared「Desktop 使用」+ A-18 P1-3 单机制；DesktopSerilogConfiguration 消费 | 保留 |
| CorrelationIdEnricher | A | 06-error-handling 富集器（LogContext 优先） | 保留 |
| CorrelationIdEnricherExtensions | B | WithCorrelationId 接线 | 记录归属 |
| LoggerConfigurationExtensions | A | 08-shared 两阶段启动（UseSharedLogging）；DesktopSerilogConfiguration 消费（2 个 WriteTo* 模板方法死） | 保留 |
| LoggingLevelManager | A | 08-shared §日志级别管理 + Diagnostics 端点；双端 Program + DiagnosticsController 消费 | 保留 |
| DebugModeInfo | B | LoggingLevelManager 数据载体 | 记录归属 |
| SensitiveDataDestructuringPolicy | A | 08-shared 脱敏（IDestructuringPolicy 框架回调） | 保留 |
| SensitiveDataMasker | A | 08-shared 脱敏层次；ApiLoggingFilter/LoggingHttpHandler 等消费 | 保留 |
| SanitizingJsonConverter | B | SensitiveDataMasker 私有嵌套 | 记录归属 |
| **ServiceCollectionExtensions** | **C** | **AddSharedLogging 双重载 0 调用点**——08-shared 设计声明的 DI 入口成僵尸，宿主（Program/LocalWebApiProgram）手工注册 LoggingLevelManager/CorrelationId | **待决策：接线或删除** |

汇总：A=7 B=3 C=1 D=0；重点：AddSharedLogging 文档-实现漂移；AsyncLocalCorrelationIdProvider 已删（A-18 P1-3）无残留。

---

## 3. SERVER 层（10 项目 / 288 类型）

### 3.1 LYBT.Infrastructure（71 类型）

> 职责归属（任务书重点核查项 3）：Data 6 + Web 5 + Services 12（CrossModule 9+HealthCheck+BaseService×2）+ Extensions 1 + Validation 1 + ExceptionHandling 3 + Interfaces 4 + EF Config/设计时 16 + 常量 4，全部对位蓝图 §2.1。

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AppDbContext / AppDbContextFactory / DatabaseInitializationService | A | 蓝图§2.1 唯一迁移链所有者（A-20 方案 A）+ 幂等迁移；迁移链闭环（AppDbContext+设计时工厂+初始化服务） | 保留 |
| BaseRepository\<TEntity,TDbContext\> / IRepository\<T\> | A | 蓝图§2.1 点名（A-20 后注入模块 DbContext）；03-server §BaseRepository 5 方法 | 保留 |
| BaseApiController / BaseCrudController / ControllerBaseExtensions | A | 蓝图§2.1 + A-14 Controller 继承三路径；BaseCrudController.ExecuteBatchStatusAsync 死（0 引用）→ 删死方法 | 保留+删死方法 |
| BaseClaimsHelper | B | 03-server §Web；GetCurrentUserId 活（BaseUsersController:179），IsAdmin/ParseUserRole 死→ 删 2 死方法 | 记录归属+删死方法 |
| OperatorAccessor / OperatorInfo | B | ADR-0004 操作者提取；BaseApiController.GetOperator 调用 | 记录归属 |
| BatchOperationHandlerBase | A | 蓝图§2.1（Q-01 批处理泛型化/模板方法）；6 子类消费 | 保留 |
| ICrossModuleService + IAuth/Herb/MedicalCase/Patient/Registration/UserCrossModuleService + CrossModuleService | A | 蓝图§2.1 模块间通信唯一通道（P07）+ 03-server 域接口 | 保留 |
| ValidationBehavior\<TReq,TRes\> | A | 蓝图§2.1 点名（FluentValidation 管道 2026-08-06）；6 模块+LocalWebAPI 注册 | 保留 |
| BusinessExceptionHandler / SystemExceptionHandler / ProblemTypeUris | A/B | 蓝图§2.1 异常→HTTP 映射（403/404/409/501 已对齐） | 保留 |
| DbContextAccessor / IDbContextAccessor | A | 蓝图§0.3 P10 落地 | 保留 |
| CacheInvalidationService / ICacheInvalidationService | A | 蓝图§2.1 缓存 + 03-server §缓存策略 | 保留 |
| LogCleanupService / ISystemLogRepository / SystemLogRepository | A/B | 蓝图§2.1 日志清理 + 11d-observability | 保留 |
| HealthCheckService / IHealthCheckService | A | 03-server §Services；HealthController 消费 | 保留 |
| ConfigurationWritePolicy / ISystemConfigurationService / SystemConfigurationService / IConfigurationStore / JsonFileConfigurationStore / ProductionConfigurationValidator（+4 支撑类型） | A/B | 蓝图§2.3 B-02 配置修改+热更新；双宿主共享 | 保留 |
| ApiVersionConstants / HttpHeaderConstants / PolicyConstants / RoleConstants | B | 常量（03-server/06-error-handling/04-permissions） | 记录归属 |
| 14 个 EF EntityTypeConfiguration + BaseEntityConfiguration\<T\> | A/B | 03-server §EF Core 配置模式；ApplyConfigurationsFromAssembly 反射注册 | 保留 |
| EntityOptimizationExtensions | B | 全局软删过滤器/索引 | 记录归属 |
| BaseService / BaseService\<T\> | B | 03-server §BaseService（MedicalCase 三 Service 继承） | 记录归属 |
| SensitiveDataJsonConverterFactory / SensitiveDataJsonConverter\<T\> | B | Issue #2254 API 脱敏 | 记录归属 |
| QueryablePagingExtensions | B | 分页扩展；**SelectAsync 死（0 引用）→ 删** | 记录归属+删死方法 |
| ReferenceCheckResult | B | 统一跨模块引用检查 DTO | 记录归属 |

汇总：A=45 B=26 C=0 D=0；重点：3 处死方法（SelectAsync/BaseClaimsHelper.IsAdmin·ParseUserRole/ExecuteBatchStatusAsync）归入所属类建议删除；ConfigureGracefulShutdown 已不存在（死代码发现已解决）。

### 3.2 LYBT.WebAPI（34 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| Program | A | 蓝图§2.3 组合根（两阶段 Serilog + 中间件 6 阶段） | 保留 |
| AuthController / UsersController / PatientsController / HerbsController / FormulasController / MedicalCasesController / RegistrationsController / ReportsController / ConfigurationController / DeployController / DiagnosticsController / HealthController | A | 蓝图§2.3 12 Controller + 13b-api-endpoints（端点全对位）；继承体系（Base*Controller） | 保留 |
| UnifiedMiddlewareConfiguration | A | 蓝图§2.3 中间件 6 阶段 + 03-server §中间件管道顺序逐条一致 | 保留 |
| CorrelationIdMiddleware(+Ext) | A | 蓝图§2.3 点名（W3C traceparent）；06-error-handling DOC3-14 五步对齐 | 保留 |
| ClaimsNormalizationMiddleware(+Ext) / SecurityHeadersMiddleware(+Ext) | A | 03-server 中间件 4.2/1.4 | 保留 |
| SqlServerHealthCheck | A | 03-server 阶段6 MapHealthChecks | 保留 |
| ApiLoggingFilter | B | LOG-014 API 日志 | 记录归属 |
| DatabaseStartupDiagnostics | B | 启动诊断 HostedService | 记录归属 |
| ApiServiceCollectionExtensions / AuthenticationServiceCollectionExtensions / DatabaseServiceCollectionExtensions / ServiceCollectionExtensions / EnvironmentAwareHosting / SerilogMSSqlServerExtensions / UnifiedApplicationInitialization | B | DI 注册扩展（API 版本/Swagger/JWT/策略/DbContext） | 记录归属 |
| ProblemDetailsConfiguration | B | RFC 7807；**UseStatusCodePagesWithProblemDetails 死（0 引用，已内联于 UnifiedMiddlewareConfiguration）→ 删** | 记录归属+删死方法 |
| JsonOptions / LybtJsonContext / RestartConfirmDto | B | 配置/源生成上下文/Deploy DTO | 记录归属 |

汇总：A=18 B=16 C=0 D=0；重点：2 处死方法（ProblemDetailsConfiguration.UseStatusCodePagesWithProblemDetails；ConfigureGracefulShutdown 已删）；文档滞后 2 处（03-server「ICrossModuleAuthService 未实现」实际已落地为 IAuthCrossModuleService；WebAPI AGENTS.md「14 controllers」实际 12 个）。

---

## 4. SERVER-MODULES（8 模块 / 183 类型）

> 分层规则依据：蓝图 §2.4（Controllers/Application/Domain/Infrastructure/Interfaces/Services/Mappers）+ §2.2 各模块结构模式。

### 4.1 LYBT.Module.Auth（26 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AuthModule | A | 蓝图§2.2 Auth CQRS；DI 注册 | 保留 |
| LoginCommand(+Handler) / LogoutCommand(+Handler) / RefreshTokenCommand(+Handler) / AutoLoginCommand(+Handler) / ValidateTokenQuery(+Handler) / RevokeAllUserTokensCommand(+Handler) | A | 端点 /auth/login|logout|refresh|auto-login|validate（AuthController 派发）+ US-AUTH-001/012/013 + ADR-0008 Token 族 | 保留 |
| JwtService / IJwtService | A | 蓝图§2.4 Services；4 Handler 注入 | 保留 |
| SecurityAuditService / ISecurityAuditService / SecurityAuditRepository / ISecurityAuditRepository | A | 蓝图§2.4；5 Handler 注入 | 保留 |
| AuthCrossModuleService | A | 蓝图§2.1 域接口；Users 3 Handler 消费（Token 撤销 AUTH-D06/D07） | 保留 |
| AuthUserMapper | A | 蓝图§2.4 Mappers（Mapperly）；LoginCommandHandler:30,41 注入 | 保留 |
| AuthDbContext | A | 蓝图§2.2 独立 DbContext（ADR-0017） | 保留 |
| LoginRequestValidator | B | 蓝图§2.4 Validators（与 Shared 同名类双注册为 P2） | 保留 |
| ValidateTokenResult | B | Query 返回记录 | 保留 |
| AuthSessionRepository / IAuthSessionRepository | C | 类活；**死方法 GetByIdAsync(:20)/GetActiveSessionsAsync(:48) 0 调用** | 删 2 死方法 |

汇总：A=22 B=2 C=2 D=0；重点：ValidateTokenQuery 走 MediatR 违 A-14 为已知 P1（非类依据问题）。

### 4.2 LYBT.Module.Users（30 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| UsersModule | A | 蓝图§2.2 Users CQRS | 保留 |
| CreateUserCommand(+Handler) / DeleteUserCommand(+Handler) / RestoreUserCommand(+Handler) / ToggleUserStatusCommand(+Handler) / ChangePasswordCommand(+Handler) / ResetPasswordCommand(+Handler) / BatchDeleteUsersCommand(+Handler) / BatchEnableUsersCommand(+Handler) / BatchDisableUsersCommand(+Handler) | A | 端点全对位（BaseUsersController:69-296）+ 用户管理 US + 层级恢复 08-04 决策 | 保留 |
| UserMapper | A | 蓝图§2.4 Mappers（Mapperly，A-18 P1-4 已修） | 保留 |
| UserService / IUserService | A | 蓝图§2.4 Services；查询走 Service 符合 A-14 | 保留 |
| UserRepository / IUserRepository | C | 类活；**死方法 AddAsync(:87)/ExistsByUserNameAsync(:80) 0 调用**（创建走 UserManager） | 删 2 死方法 |
| UserCrossModuleService | A | 蓝图§2.1；Infrastructure CrossModuleService:14 委托 | 保留 |
| IdentitySeedData | A | Program:242 + LocalWebApiProgram:145（角色+SuperAdmin，ADR-0005） | 保留 |
| BaseUsersController | A | 蓝图§2.3 Controller 继承体系 | 保留 |
| UsersDbContext | A | 蓝图§2.2 独立 DbContext | 保留 |
| CreateUserValidator / ResetPasswordResult | B | 支撑性 | 保留 |

汇总：A=26 B=2 C=2 D=0；重点：GetPagedAsync role/status 死参数 P2。

### 4.3 LYBT.Module.Patients（23 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| PatientsModule | A | 蓝图§2.2 + PatientsDbContext（A-20 新建） | 保留 |
| CreatePatientCommand(+Handler) / DeletePatientCommand(+Handler) / TogglePatientStatusCommand(+Handler) / BatchDeletePatientsCommand(+Handler) / BatchImportPatientsCommand(+Handler) | A | 端点全对位 + US-PAT-001 + Excel 导入 B-03 | 保留 |
| CheckPatientReferenceQuery(+Handler) / BatchCheckPatientReferenceQuery(+Handler) | A | 端点 check-reference（经 IMedicalCaseCrossModuleService 合法跨模块） | 保留 |
| PatientMapper | A | 蓝图§2.4 Mappers（Mapperly）；8 处消费 | 保留 |
| PatientService / IPatientService | A | 蓝图§2.4 Services | 保留 |
| PatientRepository / IPatientRepository | A | 蓝图§2.4；**注入 PatientsDbContext（A-20 P0 已修，不再违反 ADR-0017）** | 保留 |
| PatientsDbContext | A | 蓝图§2.2 独立 DbContext | 保留 |
| PatientCrossModuleService | C | 类活（GetPatientBasicInfoAsync 被 2 处消费）；**死方法 GetPatientsBasicInfoAsync(:41)/PatientExistsAsync(:73)/CheckPatientReferenceAsync(:80) 0 调用**（被 MediatR Query 替代，双实现并存） | 删 3 死方法（以 QueryHandler 为准） |
| CreatePatientValidator | B | 蓝图§2.4 Validators | 保留 |

汇总：A=21 B=1 C=1 D=0。

### 4.4 LYBT.Module.Herbs（23 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| HerbsModule | A | 蓝图§2.2 Herbs CQRS | 保留 |
| CreateHerbCommand(+Handler) / DeleteHerbCommand(+Handler) / BatchDeleteHerbsCommand(+Handler) / BatchImportHerbsCommand(+Handler) | A | 端点全对位 + US-HERB-001 + B-11 | 保留 |
| CheckHerbReferenceQuery(+Handler) / BatchCheckHerbReferenceQuery | A | 端点 check-reference（双 Query 单 Handler 非 1:1 为 P2） | 保留 |
| HerbDtoMapper | A | 蓝图§2.4 Mappers（Mapperly）；6 处消费 | 保留 |
| HerbService / IHerbService | A | 蓝图§2.4 Services | 保留 |
| HerbCrossModuleService | A | 蓝图§2.1（经 IDbContextAccessor 直连 AppDbContext 为 P2 灰色地带） | 保留 |
| HerbsDbContext | A | 蓝图§2.2 独立 DbContext | 保留 |
| HerbRepository / IHerbRepository | C | 类活；**死方法 GetByNameOrPinyinAsync(:108)/GetAllAsync(:128)/GetByCategoryAsync(:137) 生产 0 调用**（GetByNameOrPinyinAsync 仅测试引用） | 删 3 死方法 |
| HerbReferenceRepository / IHerbReferenceRepository | B | 支撑性（BaseRepository\<Herb\> 类型参数无意义 P2） | 记录归属 |
| CreateHerbValidator / HerbBatchImportCommandValidator | B | 蓝图§2.4 Validators（命名错位 P2） | 保留 |

汇总：A=17 B=4 C=2 D=0。

### 4.5 LYBT.Module.Formula（21 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| FormulaModule | A | 蓝图§2.2 Formula CQRS | 保留 |
| CreateFormulaCommand(+Handler) / DeleteFormulaCommand(+Handler) / BatchDeleteFormulasCommand(+Handler) / BatchImportFormulasCommand(+Handler) / ValidateFormulaHerbCommand(+Handler) | A | 端点全对位 + B-12/B-13 | 保留 |
| GetPendingValidationQuery(+Handler) | A | 端点 pending-validation（B-13 服务端已通） | 保留 |
| FormulaDtoMapper | A | 蓝图§2.4 Mappers（Mapperly）；6 处消费 | 保留 |
| FormulaService / IFormulaService | A | 蓝图§2.4 Services | 保留 |
| FormulaDbContext | A | 蓝图§2.2 独立 DbContext | 保留 |
| FormulaRepository / IFormulaRepository | C | 类活；**死方法 GetAllWithHerbsAsync(:119)/GetByCategoryWithHerbsAsync(:129) 0 调用** | 删 2 死方法 |
| CreateFormulaValidator / FormulaBatchImportCommandValidator | B | 蓝图§2.4 Validators | 保留 |

汇总：A=17 B=2 C=2 D=0；重点：5 写操作走 Service 违 A-14 已知 P1。

### 4.6 LYBT.Module.MedicalCase（22 类型）——Service 化例外（蓝图已文档化，非问题）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| MedicalCaseModule | A | 蓝图§2.2 Service 化例外 | 保留 |
| BaseMedicalCasesController | A | 蓝图§2.3 Controller 继承体系（12 方法） | 保留 |
| MedicalCaseDbContext | A | 蓝图§2.2 独立 DbContext（A-20 新建） | 保留 |
| MedicalCaseMapper | A | 蓝图§2.4 Mappers（Mapperly） | 保留 |
| MedicalCaseCrossModuleService | A | 蓝图§2.1；Patients 4 Handler + Registration 2 Handler 消费 | 保留 |
| MedicalCaseServiceHelper | A | 蓝图§2.4 Services（static 工具）；3 Service 消费 | 保留 |
| PrescriptionItemService / MedicalCasePrescriptionService | A/B | 蓝图§2.4 子服务拆分（U3-1） | 保留 |
| MedicalCaseReferenceRepository / IMedicalCaseReferenceRepository | A | 蓝图§2.4 | 保留 |
| MedicalCaseCommandService(+Deletion.cs) | C | 类活（Save/SaveWithDetail/Delete/BatchDelete/SetPrescriptionFlagWithDetail/AddPrintLog/RecordPrint 被 Controller 消费）；**死方法 6 个（CreateAsync/UpdateConsultationAsync/CreatePrescriptionAsync/CopyHistoricalPrescriptionAsync/UpdatePrescriptionAsync/DeletePrescriptionAsync）**——修正模块审计 §B「8 个」：SetPrescriptionFlagAsync 经 WithDetail(:370) 委托存活 | 删 6 死方法 |
| MedicalCaseQueryService | C | 类活（9 方法被消费）；**实体泄漏死方法 GetByIdAsync(:39)/GetBatchAsync(:285)** | 删实体版 |
| MedicalCaseStateService / IMedicalCaseStateService | C | 类活（UpdateStatus/Complete/Suspend/Cancel 被消费）；**死方法 CloseCaseAsync(:169)** | 删死方法 |
| MedicalCaseRepository(+AuditLogs/PendingCases/Update.cs) / IMedicalCaseRepository | C | 类活（6 方法被消费）；**死方法 QueryAsync(:169)/GetByPatientIdWithDetailsAsync(:102)** | 删 2 死方法 |
| IMedicalCaseCommandService / IMedicalCaseQueryService | C | 接口活；**死成员共 8 个（Command 6 + Query 实体版 2）** | 瘦接口 |

汇总：A=9 B=1 C=12 D=0；重点：死代码集中于接口实体泄漏（11 符号）；`[诊断]` 调试日志残留 P2。

### 4.7 LYBT.Module.Registration（27 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| RegistrationModule | A | 蓝图§2.2 CQRS + SignalR | 保留 |
| CreateRegistrationCommand(+Handler) / CancelRegistrationCommand(+Handler) / QuickVisitCommand(+Handler) / StartVisitCommand(+Handler) | A | 端点全对位 + US-REG-001~005 + D-01 + ADR-0013 推送 | 保留 |
| GetRegistrationQuery(+Handler) / GetRegistrationsQuery(+Handler) / GetWaitingQueueQuery(+Handler) | A | 端点 + US-REG-006 候诊队列 | 保留 |
| RegistrationRepository / IRegistrationRepository | A | 蓝图§2.4；4 Handler 注入 | 保留 |
| RegistrationDbContext | A | 蓝图§2.2 独立 DbContext（A-20 新建） | 保留 |
| RegistrationHub / RegistrationConnectionManager | A | ADR-0013 SignalR；Program:255 MapHub | 保留 |
| NotificationService / INotificationService | A | ADR-0013；3 Handler 消费 | 保留 |
| RegistrationCrossModuleService | A | 蓝图§2.1；MedicalCase 2 Service 注入 | 保留 |
| BaseRegistrationsController | A | 蓝图§2.3 继承体系 | 保留 |
| CreateRegistrationValidator / QuickVisitCommandValidator | B | 蓝图§2.4 Validators | 保留 |
| RegistrationMapper | C | Mapper 活（ToListDto/ToDetailDto 消费）；**死方法 ToEntity(:42) 0 调用**（CreateHandler:43-57 手工 new 绕过） | 改用手写消死代码 |

汇总：A=24 B=2 C=1 D=0；重点：StartVisit/Cancel 缺 Validator 为 P2。

### 4.8 LYBT.Module.Reports（11 类型）——只读聚合特例（蓝图已文档化）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| ReportsModule | A | 蓝图§2.2 只读聚合特例 | 保留 |
| ReportRepository / IReportRepository | A | 蓝图§2.4（注入 AppDbContext 属 A-18 P1-7 已文档化设计）；8 组聚合查询全消费 | 保留 |
| ReportService / IReportService | A | 蓝图§2.4 Services；8 方法全被 ReportsController 消费 | 保留 |
| ReportDayValueDto / ReportDayCountDto / DoctorPerformancePointDto / PatientFlowPointDto | A | 只读聚合投影形状；Repository/Service 全消费 | 保留 |
| ReportTimeBuckets (record struct + static class) | B | ReportService 内部支撑 | 记录归属 |

汇总：A=9 B=2 C=0 D=0；**重点：模块级审计 §B「Domain 4 record 死代码」已解决**——Domain 目录已删（A-21 清理），对应 DTO 以 DailyIncomeDto 等形态活在 Shared.Models 且全被消费。

### SERVER-MODULES 小结

8 模块 183 类型：A=145 B=16 C=22 D=0，无孤儿类。C 级全部为「类活但含死方法」：Repository/接口死方法 9 处 + MedicalCase 接口实体泄漏 11 符号 + PatientCrossModuleService 3 死方法 + RegistrationMapper.ToEntity 1。处置建议均为删死方法/瘦接口，零删除成本。

---

## 5. DESKTOP 层（16 项目 / 703 类型）

### 5.1 DESKTOP-CORE（6 项目 / 456 类型）

#### 5.1.1 LYBT.Desktop.Contracts（111 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| 11 Refit 接口（IAuthApi…IUserApi） | A | 蓝图§3.1 A-18 方案 A（Refit 特性 internal 化）；Foundation Refit 适配器消费 | 保留 |
| IApiClient + 12 子接口 + IEntityApiSegment | A | 蓝图§3.1 唯一契约面（0.4-2 契约单一）；Refit/HttpClient/Switching + Repository 消费 | 保留 |
| 6 Repository 接口（IFormula…IUserRepository） | A | 05-dual-mode「Repository 接口 6 个」 | 保留 |
| 12 Service 接口（IFormulaService…IRegistrationService + IMedicalCaseQuery/Command/LifecycleService + IAuditLogService/IReportService） | A | 蓝图§3.5 MVVM 分层（A-21 M5 强制）；模块 Service 实现 | 保留 |
| ILoginCoordinator / INavigationCoordinator / IViewModelServices / IToastService / IStartupStep·Pipeline / IApiHealthMonitor 族 / IAuthenticationStateMachine / ISessionManager / IConnectionModeService / ICurrentUserProvider / IEmbeddedLocalWebApiService / IUserActivityTracker 等基础契约 | A/B | 02-desktop 服务契约；Shell 实现 + 模块 VM 消费 | 保留 |
| EditState / WorkspaceMode / MedicalCaseNavigationParameters / BreadcrumbItem | A | 导航契约下沉（A-18 P1-5） | 保留 |
| CacheEvents 族 / AuthState·Event 族 / IActiveConsultationService 族 / PerformanceMonitor 族 | A/B | 02-desktop 事件架构 | 保留 |
| CommandResult / CommandResult\<T\> | A | A-18 统一 Result 载荷 | 保留 |
| IFormulaSearchProvider / IHerbSearchProvider | A | 跨模块搜索（0.4-1 模块自治合法跨模块服务） | 保留 |
| IRoleDefinition / IRoleRegistry | A | 角色契约已在 Contracts（A-18 下沉） | 保留 |
| **IApiRouter** | **D** | 注册孤儿：仅 Shell ServiceCollectionExtensions:211 + ApiRouterTests，生产 0 消费 | 建议删除 |
| **UnfinishedCaseChoice** | **C** | 仅被死链 UnfinishedCaseDialogViewModel 引用 | 随死链删除 |

汇总：A=86 B=23 C=1 D=1。

#### 5.1.2 LYBT.Desktop.Foundation（120 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| RefitApiClient / HttpClientApiClient / SwitchingApiClient / HttpApiClientBase / HttpClientApiClientExtensions / LocalWebApiHttpClientFactory | A | 蓝图§3.1 + ADR-0009 URL 驱动双轨 + 05-dual-mode SwitchingApiClient | 保留 |
| 22 个 adapter（{X}ApiClient + {X}HttpApiClient ×11 域） | A | M2 双实现=双轨对称设计已文档化（非问题）；RefitApiClient:68-108 / HttpClientApiClient:50-80 接线 | 保留 |
| AuthenticationService / AuthenticationStateMachine / AuthStateChangedPubSubEvent | A | ADR-0005 + US-AUTH 状态机 | 保留 |
| 10 个 AuthEvents + Payload（LoginStarted/LogoutStarted/LogoutCompleted/ServerLogoutFailed/PendingLogoutsCleared/PasswordChanged/SessionExtended/TokenRefreshSucceeded/TokenRefreshFailed/ProfileUpdated） | A/B | 02-desktop 事件清单（12 事件文档化）；AuthenticationService:288 等发布订阅 | 保留 |
| **LoginSucceededEvent/LoginFailedEvent/SessionExpiredEvent + Payload + LoginFailureReason/SessionExpiredReason** | **C** | 文档声明但生产 0 发布 0 订阅（惰性事件，AuthStateChangedPubSubEvent 才是实际通知） | 待决策 |
| Token 管理族（ITokenManager/TokenManager/ITokenStorageService/TokenStorageService/ITokenValidator/LocalTokenValidator/ITokenRefreshHandler/TokenRefreshTypes/UsernameStorage 族） | A/B | US-AUTH 族（02-desktop §凭证存储/Token 刷新）；LoginCoordinator/TokenRefreshHandler/SignalRClient 消费 | 保留 |
| CredentialVault 族（ICredentialVault/CredentialVault/DpapiPhotoStorageService/IPhotoStorageService/CredentialStorage/VaultStorage/VaultEntry/OldCredentialFormat/DpapiProtector） | A/B | US-AUTH-009 + C2 照片 DPAPI | 保留 |
| LogoutService 族 / TokenLifecycleService 族 | A/B | US-AUTH-011 登出队列/Token 生命周期 | 保留 |
| ApiClientRepositoryBase / EntityApiClientRepositoryBase | A | 05-dual-mode Repository 统一层；6 模块仓储继承 | 保留 |
| DesktopCacheManager / IDesktopCacheManager | A | 缓存失效事件；5 模块 VM 消费 | 保留 |
| ClientErrorMessageMapper | A | US-ERR-005/006 错误消息映射 | 保留 |
| ApiHealthCheckService 族 / ModuleLoadingService 族 / ApplicationStateService 族 | A/B | 02-desktop 健康检查/模块加载/连接状态 | 保留 |
| AuthorizationMessageHandler / TokenRefreshHandler / RetryPolicyExtensions / RetryPolicyOptions | A/B | ADR-0009 认证管线 + US-AUTH-011 | 保留 |
| ConnectionModeService | A | ADR-0009 双轨模式 | 保留 |
| **ApiErrorHandler** | **D** | **0 引用**（src+tests）；异常映射实际走 ClientErrorMessageMapper + Repository 内联 | 建议删除或接通 |
| **IApiService / ApiService / RequestDeduplicator** | **C** | 注册孤儿（Shell ServiceCollectionExtensions:103），生产 0 消费 | 待决策 |
| **QueryStringBuilder** | **D** | **0 引用**；URL 构建由 HttpApiClientBase.BuildPagedUrl 内联 | 建议删除 |

汇总：A=76 B=32 C=10 D=2；重点：2 个模块审计未覆盖死类 + 1 注册孤儿 + 3 惰性事件（新发现）。

#### 5.1.3 LYBT.Desktop.Infrastructure（137 类型）——职责归属重点

| 职责簇 | 类 | 依据等级 | 处置 |
|--------|----|---------|------|
| ViewModels | MasterDetailViewModelBase / DialogViewModelBase / HerbItemViewModelBase / NavigableViewModelBase×4 / ValidatableModelBase / ChildViewModelBase / MasterDetailCommandGroup / ICommandHost / ServiceEventBridge / IServiceEventCallback / BaseStatusHandler | A/B | 02-desktop §ViewModel 基类体系 + ADR-0007；模块继承/消费 | 保留 |
| Views/Windows | UnfinishedCaseDialog（D）/ UnfinishedCaseDialogViewModel（D）/ BaseDialogWindow（D） | D | 注册孤儿/0 引用 | 建议删除 |
| Navigation | NavigationCoordinator / NavigationServices / NavigationHistoryService / RegionMonitor / ModuleLazyLoader / IModuleLazyLoader 等 | A | 02-desktop §导航模式；NavigationCoordinator 消费 | 保留 |
| Behaviors | DataGridSelectionBehavior / PasswordBoxHelper / ResponsiveLayoutBehavior（D） | A/D | 5 模块 XAML 消费；ResponsiveLayoutBehavior 0 引用 | 保留 2 / 删 1 |
| Roles | RoleDefinitionBase / RoleRegistry / Admin/Doctor/Receptionist/SuperAdminRoleDefinition | A | 02-desktop §条件模块加载（A-21 M4 已修模块名） | 保留 |
| Security | SensitiveInfoFilter（D） | D | 0 引用 | 建议删除 |
| Helpers | TaskExtensions / PrivacyHelper | A/B | 20+ VM 消费 / 读卡器脱敏 | 保留 |
| Commands | IApplicationCommands / ApplicationCommands | A | 全局命令；MenuManager 消费 | 保留 |
| Events | CaseEvents / PatientEvents / EventSubscriptionManager | A/B | 02-desktop 事件清单；WorkspaceViewModel:309 消费 | 保留 |
| Constants | CommonOptions / RegionNames / SystemConstants / FilePaths / FileExtensions / ViewNames | A | 常量定义 | 保留 |
| Logging | DesktopSerilogConfiguration | A | 08-shared；App.xaml.cs 消费 | 保留 |
| Configuration | ConfigurationExtensions（D） | D | AddInfrastructureConfiguration 空实现 + 0 调用 | 建议删除 |
| Extensions | ViewModelServicesExtensions | A | DI 注册 | 保留 |
| Interfaces/Models | 服务契约 + UserManagementNavParams / RegistrationNavParams（D） | A/D | RegistrationNavParams 0 引用 | 删 1 |
| Http | ApiResponseHelper（D）/ LoggingHttpHandler（C） | C/D | **Http 目录仍滞留 Infrastructure**：LoggingHttpHandler 存活（UnifiedApiClientExtensions:86）应下沉 Foundation/Http；ApiResponseHelper 死 | LoggingHttpHandler 下沉；ApiResponseHelper 删 |
| CardReader | CardReaderModule / CardReaderServiceCollectionExtensions / ICardReader / CardReaderFactory / CardReaderService / ICardReaderService / HuaDaHD100 / MockCardReader / IPatientCardReaderIntegration / CardReaderIntegration 族 / HuaDaNativeMethods / CardReadResult 族 / CardType 等 24 类 | A | **存活**：Clinical 4 VM + Patients 模块消费 | 保留，标注「待独立 P2 已记录」 |
| LocalData | — | — | **生产层已清理确认**（LocalData 目录不存在，LocalDbContext 仅测试 `_Infrastructure` 夹具）——A-21 C1 生效 | ✅ 已清理 |
| Services | ActiveConsultationService / ApplicationTickService / AsyncExecutor / ClinicSettingsService / CommonDialogService / ConnectionSettingsService / DetailEditorService / DialogManager / ErrorHandler / ListViewServices / LoadingStateManager / MasterDetailServices / PaginationService / SearchService / SelectionService / SessionManager / UserActivityTracker / UserNotificationService / ViewModelServices / WpfUiThreadDispatcher + 13 接口 | A | WPF 服务层；ViewModelServicesExtensions 注册 + 模块 VM 消费（CommonDialogService 删 ShowUnfinishedCaseDialogAsync 死方法） | 保留 |
| Notifications/Toast | NotificationService 族 / ToastService / AdornerLayer | A/B | 通知/Toast | 保留 |
| Validation | ValidationErrorsAccessor / ValidationHasErrorsAccessor | A | 表单验证（UI-D06） | 保留 |
| ApiRouter | ApiRouter（D） | D | 注册孤儿 | 建议删除 |

汇总：A=95 B=32 C=1 D=9；孤儿类：[ApiResponseHelper、SensitiveInfoFilter、ResponsiveLayoutBehavior、ApiRouter、UnfinishedCaseDialog、UnfinishedCaseDialogViewModel、ConfigurationExtensions、RegistrationNavParams、BaseDialogWindow]；重点：**Http 目录滞留 C 待决策；CardReader 存活 A 待独立 P2；LocalData 已清理**。

#### 5.1.4 LYBT.Desktop.Controls（47 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| BadgeType/StatusBadge/BaseDetailContainer/BreadcrumbBar/DataGridToolbar/DetailToolbar/EmptyState/InfoCard/LoadingOverlay/MasterDetailLayout/SearchBox/UnifiedPaginationBar/MasterDetailControlBase/FormulaViewControl/ToastControl/NavigationItem/PatientInfoCardControl/PatientDisplayModel/PatientCardDisplayMode/HerbItemControl 族/HerbListControl 族/ResponsiveLayoutHelper/ScreenSizeCategory/DuplicateDosageStrategy 族 | A | ADR-0006 组件解耦；模块 XAML 消费（rg XAML 复核） | 保留 |
| 13 转换器 + BindingProxy + BreadcrumbItem（内嵌） | B | 支撑性 XAML 转换器/绑定桥 | 记录归属 |
| **SuggestionType** | **D** | 0 生产引用（仅测试+README） | 建议删除 |

汇总：A=24 B=22 C=0 D=1；重点：死项目引用 Controls→Foundation（csproj 层问题，不判类）。

#### 5.1.5 LYBT.Desktop.Printing（17 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| PrintingModule / IPrintService / PrintOptions / PaperSize / PrintOrientation / ExportFormat | A/B | 蓝图§3.1 + 打印规则（2026-08-03 定案）；App.xaml.cs 注册 + PrescriptionPrintHandler 消费 | 保留 |
| PrescriptionPrintModel / PrescriptionItemPrintModel | A/B | 10-printing-architecture 数据模型 | 保留 |
| PrescriptionDocumentBuilder / PrescriptionPdfExporter / PrescriptionPreviewWindowBuilder / PrescriptionPrintExecutor / PrescriptionPrintService | A | 打印服务链 | 保留 |
| 4 模板 | A | 10-printing-architecture §模板 | 保留 |

汇总：A=12 B=5 C=0 D=0；死项目引用 Printing→Infrastructure（csproj 层问题）。

#### 5.1.6 LYBT.LocalWebAPI（24 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| LocalWebApiProgram | A（含 C 注记） | ADR-0010 薄宿主；CreateBuilder/CreateApplication/InitializeDatabaseAsync 被 EmbeddedLocalWebApiService:51-64 消费；**RunAsync(:149-155) 0 调用=死方法（双入口重复）** | 类保留；RunAsync 待决策删除 |
| 12 Controllers | A | ADR-0010 复用 Server Service/Handler + 13b-api-endpoints 契约 | 保留 |
| LocalAutoLoginCommand(+Handler) / LocalLoginCommand(+Handler) / LocalRefreshTokenCommand(+Handler) / LocalValidateTokenQuery(+Handler) | A | 本地 Auth CQRS 合理例外（任务书判定保留）；MediatR 注册 | 保留 |
| LocalJwtConfig | A | ADR-0010 本地 JWT 简化（1 年长效 Token） | 保留 |
| LocalAuthHelpers | B | Handler 内部辅助 | 保留 |
| LocalWebApiSeedData | A | 双种子（05-dual-mode）；Program:146 消费 | 保留（英文样例数据 P2） |

汇总：A=23 B=1 C=0 D=0。

### 5.2 DESKTOP-MODULES（7 模块）

#### 5.2.1 LYBT.Desktop.Auth（10 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AuthenticationModule | A | 蓝图§3.2 Auth + ADR-0005 | 保留 |
| LoginViewModel / FirstRunSetupViewModel / ServerConfigViewModel / ConnectionStatusViewModel | A | ADR-0005 登录流程 + B-07 向导；注入 ILoginCoordinator 无越层 | 保留 |
| LoginCredentialsViewModel | B | LoginViewModel 子组件 | 记录归属 |
| ConnectionTestStatus | A | FirstRunSetup 连接测试枚举 | 保留 |
| 3 个 View（code-behind） | B | 视图配套（PasswordBox 双向同步不可绑定，合理） | 保留 |

汇总：A=7 B=3 C=0 D=0；无越层（模块审计 5/5 复证）。

#### 5.2.2 LYBT.Desktop.Users（15 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| UsersModule | A | 蓝图§3.2 Users | 保留 |
| UserMasterDetailViewModel / UserEditorViewModel | A | 用户管理 UI；注入 IUserService 无越层 | 保留 |
| RemoteUserService | A | Service 层守序 | 保留 |
| UserRepository | A | Entity 泛型基类 | 保留 |
| UserDetailModel / UserEditContext / UserItem | A | 模型（UserEditContext 与 UserDetailModel 同构双份 P2） | 保留+注记 |
| UserPasswordHandler / UserStatusHandler / IUserPasswordHandler / IUserStatusHandler | A/B | Handler 组件 | 保留 |
| 3 个 Control + UserViewControl | B | XAML 配套 | 保留 |

汇总：A=10 B=5 C=0 D=0；无越层；A-21 M2 已删 UserMapper（复核确认）。

#### 5.2.3 LYBT.Desktop.Patients（31 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| PatientsModule | A | 蓝图§3.2（**:47-53 仍注册 4 死组件**） | 保留+清理注册 |
| PatientMasterDetailViewModel / PatientEditorViewModel / PatientCardReaderViewModel | A | 患者管理 UI + 读卡器（PRD-15）；注入 IPatientService 守序 | 保留 |
| PatientService / PatientRepository | A | Service/Repository 层 | 保留 |
| PatientDetailModel / PatientDetailDisplayModel / PatientEditContext / PatientItem / ImportWizardStep / ImportProgressInfo | A | 模型 + Excel 导入（B-03） | 保留 |
| PatientCardReaderIntegration | A | 读卡器集成（Clinical 3 VM + Patients 消费，存活） | 保留 |
| PatientStatusHandler / IPatientStatusHandler | A/B | Handler 组件 | 保留 |
| **MedicalCaseStartCoordinator / IMedicalCaseStartCoordinator / StartResult / StartResultData** | **D** | 197 行假实现（CheckUnfinishedCaseAsync:75 return null），0 消费者 | 建议删除 |
| **PatientSearchManager / SearchCompletedEventArgs** | **D** | 299 行，0 消费者 | 建议删除 |
| **PatientSearchCache / IPatientSearchCache / CacheEntry** | **D** | 195 行，0 消费者 | 建议删除 |
| **PatientValidator / IPatientValidator** | **D** | 182 行，0 消费者 | 建议删除 |
| **PatientListToDetailMapper** | **D** | 内嵌 PatientRepository:145-152，0 调用 | 建议删除 |
| 4 个 Control + PatientViewControl | B | XAML 配套（PatientSelectionControl.xaml.cs:43-72 反射 GetPropertyValue 脆弱越层 P2） | 保留+注记 |

汇总：A=14 B=5 C=0 D=12；**4 死组件链全部仍在（~680 行），为全仓最大孤儿簇**。

#### 5.2.4 LYBT.Desktop.Herbs（13 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| HerbsModule | A | 蓝图§3.2 Herbs | 保留 |
| HerbMasterDetailViewModel / HerbEditorViewModel | A | 注入 IHerbService 无越层 | 保留 |
| RemoteHerbService / HerbRepository / HerbSearchProvider | A | Service/Repository/搜索 | 保留 |
| HerbDetailModel / HerbEditContext | A | 模型 | 保留 |
| HerbStatusHandler / IHerbStatusHandler | A/B | Handler | 保留 |
| 3 个 Control | B | XAML 配套 | 保留 |

汇总：A=9 B=4 C=0 D=0；无越层；A-21 M2 已删 HerbMapper（复核确认）。

#### 5.2.5 LYBT.Desktop.Formula（16 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| FormulaModule | A | 蓝图§3.2（M2 后仅注册 FormulaDetailModelMapper） | 保留 |
| FormulaMasterDetailViewModel / FormulaEditorViewModel / FormulaHerbItemViewModel | A | 注入 IFormulaService 无越层 | 保留 |
| FormulaService / FormulaRepository / FormulaSearchProvider | A | Service/Repository/搜索 | 保留 |
| FormulaDetailModel / FormulaEditContext / FormulaDetailModelMapper | A | 模型 + Mapperly | 保留 |
| FormulaStatusHandler / IFormulaStatusHandler | A/B | Handler | 保留 |
| **FormulaItem / FormulaHerbItem** | **D** | 0 实例化（仅死类互引用 ~440 行） | 建议删除 |
| 2 个 Control | B | XAML 配套 | 保留 |

汇总：A=11 B=3 C=0 D=2；死簇残留 2 模型（A-21 M2 已删 4 Mapper 复核确认）。

#### 5.2.6 LYBT.Desktop.MedicalCase（56 类型，最大模块）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| MedicalCaseModule | A | 蓝图§3.2 核心模块（6 子目录） | 保留 |
| MedicalCaseMasterDetailViewModel / ConsultationEditorViewModel / PrescriptionEditorViewModel / MedicalCaseCommandsViewModel / AuditLogViewModel / ReportsHomeViewModel / FormulaImportDialogViewModel / HistoryCopyDialogViewModel / UnsavedChangesDialogViewModel | A | 医案工作台；**M5 已修 2 处越层（AuditLog→IAuditLogService :30 / ReportsHome→IReportService :35，符号级复证）** | 保留 |
| MedicalCaseService / MedicalCaseCommandService / MedicalCaseQueryService / MedicalCaseLifecycleService / AuditLogService / ReportService | A | Service 层（MedicalCaseService:237-349 6 死方法 P1 注记） | 保留+清理死方法 |
| MedicalCaseRepository | A | 仓储（手写基类口径 P2） | 保留 |
| EditModeStateMachine / IEditModeStateMachine / EditStateChangedEventArgs / WorkspaceEditEvent / WorkspaceEditState / EditType / CompletenessCheck / WorkspaceState | A | US-MC-011 状态机（锁+转换表驱动） | 保留 |
| IMedicalCaseService / IMedicalCaseWorkspaceContext / IMedicalCaseDataProvider / IDataProvider / IValidatable | A | 工作台契约 | 保留 |
| MedicalCaseDetailModel / ConsultationItem / PrescriptionItemViewModel / MedicalCaseEditContext | A | 模型（**MedicalCaseDetailModel 命名空间陈旧分裂 P2；PrescriptionItemViewModel 落错层在 Models/Items P2**） | 保留+注记 |
| 4 Mapper（MedicalCaseDetailModelMapper/ConsultationMapper/PrescriptionMapper/MedicalCaseCloneMapper） | A | Mapperly（DI 风格分裂 7 处 new/static 为 P2） | 保留 |
| PrescriptionPrintHandler / PrintResult | A | 打印规则（2026-08-03） | 保留 |
| WorkflowStepIndicator / WorkflowStep | A | 工作流步骤控件 | 保留 |
| ReportsModule / ReportsHomeView | A | B-04 报表子模块（程序集内第二个 Prism 模块） | 保留 |
| MasterDetailWorkspaceHost / MasterDetailWorkspaceContext | B | private 嵌套适配器 | 保留 |
| **MedicalCaseChangeTracker** | **D** | 0 生产引用（仅测试） | 建议删除 |
| 6 个 View/Control/Dialog（code-behind） | B | XAML 配套 | 保留 |

汇总：A=43 B=12 C=0 D=1；孤儿类：[MedicalCaseChangeTracker]；M5 修复 2 处越层复证通过，其余 0 越层。

#### 5.2.7 LYBT.Desktop.Registration（10 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| RegistrationModule | A | 蓝图§3.2 精简结构 | 保留 |
| RegistrationListViewModel | A | **M5 已修**：注入 IRegistrationService + IPatientService（:79-81，无 IApiClient，符号级复证） | 保留 |
| RegistrationCreateDialogViewModel / RemoteRegistrationService / RegistrationRepository | A | Service/Repository | 保留 |
| SignalRClient / ISignalRClient | A | US-REG-008 实时推送 | 保留 |
| RegistrationRefreshedEvent | A | PubSub 刷新事件 | 保留 |
| 2 个 View/Dialog | B | XAML 配套 | 保留 |

汇总：A=8 B=2 C=0 D=0；重点：**命名空间复数漂移确认**（8 个 .cs 用 LYBT.Desktop.Registrations.* 复数 vs 项目单数，C 级组织问题）。

### 5.3 ROLES + SHELL

#### 5.3.1 LYBT.Desktop.Admin（18 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| AdminModule / SysadminModule | A | 蓝图§3.3 Admin 角色工作台（引 5 模块，csproj 复核一致） | 保留 |
| AdminHomeViewModel / SystemSettingsService / ISystemSettingsService / StatusCard / DashboardStatus | A | 管理员主页 + D2 诊所配置化 | 保留 |
| **SysadminHomeViewModel** | **C** | **VM 直接注入 IApiClientAuth（:27），越层**（A-21 M5 清单外） | 待决策（改 Service） |
| **LogLevelControlViewModel** | **C** | **VM 直接注入 IApiClient（:25，:43 直调 Diagnostics.GetLoggingStatusAsync），越层** | 待决策（改 Service） |
| **DeploymentViewModel** | **C** | **VM 直接注入 IApiClient（:27），越层** | 待决策（改 Service） |
| **SystemSettingsViewModel** | **C** | **VM 直接注入 IApiClient（:145），越层** | 待决策（改 Service） |
| SystemSettings（嵌套）/ 6 个 View | B | 配置类/XAML 配套 | 保留 |

汇总：A=7 B=7 C=4 D=0；**4 个 VM 直连 IApiClient（Sysadmin 3 + 设置 1），全部在 A-21 M5 修复清单之外**。

#### 5.3.2 LYBT.Desktop.Clinical（23 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| ClinicalModule | A | 蓝图§3.3 Clinical 工作台（引 5 模块） | 保留 |
| ClinicalHomeViewModel / ClinicalWorkspaceViewModel / MedicalCaseWorkspaceViewModel / ReceptionistHomeViewModel / PendingQueueViewModel / CardReaderViewModel | A | 临床/前台/队列/读卡器工作台；Service 注入守序 | 保留 |
| PatientSelectionWorkspaceContext / HistoryItem / RegistrationQueueItem | A | 工作台上下文/模型 | 保留 |
| **PatientSelectionViewModel** | **C** | **VM 直接注入 IApiClientPatients + IApiClientMedicalCases（:108-109），越层——且与 IMedicalCaseService/IRegistrationService 混用** | 待决策（改 Service） |
| WorkspaceNavigationHandler / WorkspaceStateManager | B | internal 导航/状态管理 | 保留 |
| 10 个 View | B | XAML 配套（薄包装引用模块 Control） | 保留 |

汇总：A=11 B=11 C=1 D=0；**1 个 VM 直连 IApiClient 双接口**。

#### 5.3.3 LYBT.Desktop.Shell（55 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| App | A | PrismApplication 组合根（蓝图§3.4；显式 ModuleCatalog + RegisterDialog + 角色模块加载） | 保留 |
| MainWindowViewModel / MainWindow | A | 主窗口（Region 字面量 "LoginRegion"/"ContentRegion" 与 RegionNames 常量漂移 P2） | 保留+注记 |
| AccountSettingsControl / AccountSettingsView / AccountSettingsViewModel | A/C | **AccountSettingsViewModel 直接注入 IApiClientUsers（:71），越层 C**；控件/视图保留 | VM 待决策（改 IUserService） |
| LoginCoordinator / LoginStateManager / ILoginStateManager / SessionLifecycleManager / SessionBasedCurrentUserProvider / SessionState / SessionDiagnostics / SessionStateChangedEventArgs | A | 登录协调 + 会话生命周期（02-desktop） | 保留 |
| ApplicationBootstrapper / IApplicationBootstrapper / StartupPipeline / 4 StartupStep（ErrorHandling/ModuleCoordinator/ApiHealthCheck/LocalWebApi）+ ExecutionUnit | A | 02-desktop §启动管线（A-21 M4 已修 AuthModule 名；ApiHealthCheckStartupStep fire-and-forget 空转 P2 观察项） | 保留+注记 |
| EmbeddedLocalWebApiService | A | ADR-0010 本地宿主（:18-19 硬编码 LocalDB 连接串 P2） | 保留+注记 |
| UnifiedApiClientExtensions / ServiceCollectionExtensions / PrismConfigurationExtensions / DataSourceRegistrationExtensions / LoggingRegistrationExtensions / AppStartupOrchestrator | A | 统一 IApiClient 注册（A-18 方案 A） | 保留 |
| MenuManager / NavigationManager / StatusBarManager / ThemeService / ShellServices / ShellEventServices / ShellEventCoordinator / ShellDialogHelper / ApiHealthMonitor / UserActivityTracker 相关接口与实现 | A | US-SHELL 族（菜单/导航/状态栏/主题/事件） | 保留 |
| 3 Dialog VM（Confirmation/Input/MessageDialogViewModel）+ MessageType + 3 Dialog View | A/B | App.xaml.cs:99-101 RegisterDialog | 保留 |
| NativeMethods | B | P/Invoke 辅助 | 保留 |
| StringResources | B | Designer 生成资源 | 保留 |
| LocalWebApiHttpClientFactory | B | private 嵌套 HttpClient 工厂 | 保留 |

汇总：A=45 B=9 C=1 D=0；**1 个 VM 直连 IApiClientUsers（AccountSettingsViewModel，A-21 清单外）**。

### DESKTOP 层总体小结

**MVVM 越层复查（任务书重点核查项 2）**：A-21 M5 修复 3 处（AuditLogViewModel/ReportsHomeViewModel/RegistrationListViewModel，主代理+子代理双重复证确认改走 Service）**后，仍有 6 个 VM 直接注入 IApiClient\***，全部位于 A-21 未覆盖的 Shell/Roles 层：

| # | VM | 位置 | 注入 | 修复路径 |
|---|----|------|------|---------|
| 1 | AccountSettingsViewModel | Shell/ViewModels/:71 | IApiClientUsers | 改注入 IUserService（已有 ChangeProfileAsync/ChangePasswordAsync） |
| 2 | PatientSelectionViewModel | Clinical/ViewModels/:108-109 | IApiClientPatients + IApiClientMedicalCases | 已注入 IMedicalCaseService/IPatientService，去掉直连 |
| 3 | SystemSettingsViewModel | Admin/ViewModels/:145 | IApiClient | 走 IClinicSettingsService 或新增 Service |
| 4 | SysadminHomeViewModel | Admin/Sysadmin/:27 | IApiClientAuth | 走 IAuthService 或 HealthCheckService |
| 5 | LogLevelControlViewModel | Admin/Sysadmin/:25 | IApiClient（Diagnostics） | 新增 IDiagnosticsService 或走 LoggingLevelManager 端点 Service |
| 6 | DeploymentViewModel | Admin/Sysadmin/:27 | IApiClient | 新增 IDeploymentService |

**死代码复核**：Patients 4 死组件链 12 类（~680 行）+ Formula 死簇 2 类（~440 行）+ MedicalCaseChangeTracker 均在；A-21 M2 已删 4 个 Mapper 确认不复存在。

---

## 6. TESTS 层（3 项目 / 217 顶层类型）

### 6.1 LYBT.Tests.Architecture（8 类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| ArchTests / ServerArchTests / DesktopLayerArchTests / AggregateRootArchTests / CustomControlArchTests / AntiMockRuleTests / LocalWebApiPatternTests | A | 蓝图§4 架构守卫强制约束（P01-P22/DP01-09/MC/A/B/AM/CC/AR） | 保留 |
| TestAssemblies | B | 守卫程序集清单支撑 | 记录归属 |

汇总：A=7 B=1 C=0 D=0；守卫真空（P01c 扫描 Server 程序集查 ViewModels 恒通过、A07 Mapperly 守卫空集放行）为守卫自身缺陷（模块审计 §F 一致），非测试层依据问题。

### 6.2 LYBT.Tests.Server（70 顶层类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| 47 个业务测试类（Unit 8 模块全覆盖 + Integration + Entities） | A | ADR-0003 Integration-first；蓝图§4 | 保留 |
| 23 个夹具类（ServerFixture/Respawn 夹具/域夹具/构建器/断言扩展） | B | 支撑性测试基建 | 记录归属 |

汇总：A=47 B=23 C=0 D=0。

### 6.3 LYBT.Tests.Desktop（139 顶层类型）

| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档+证据） | 处置 |
|----|---------|------------------------------|------|
| 89 个业务测试类（Unit/Integration/LocalWebAPI/Roles） | A | ADR-0003 + 蓝图§4 | 保留 |
| 29 个夹具类（含 LocalDbContext 测试夹具——A-21 C1 移入） | B | 支撑性（LocalDbContext 归属记录） | 保留 |
| **ApiRouterTests / MedicalCaseChangeTrackerTests** | **C** | 测试目标为生产死类（ApiRouter/MedicalCaseChangeTracker）——生产删除时须同步处理 | 待决策（随生产删除） |
| **UserJourneyTestBaseShared** | **C** | 全仓 0 子类消费，僵尸测试基类 | 待决策 |
| **Traits 特性体系 18 类型**（10 特性 + 3 discoverer + TestTraits/Categories/Speed/Dependencies/Platforms） | **C** | 全仓零应用（无 CI/runsettings 消费），僵尸测试基建 | 待决策（整簇删除） |

汇总：A=89 B=29 C=21 D=0；残留核查通过：AutoMode/DetectBestMode/FeatureToggle/UserMapper/HerbMapper 测试侧 0 残留；F-01 清理已落地。

---

## 7. 任务书重点核查项结论

| # | 核查项 | 结论 |
|---|--------|------|
| 1 | Server 模块内分层类职责依据（CQRS Handler 对应 US） | ✅ **全部对应**：8 模块 183 类 100% 可追溯蓝图/ADR/US（A=145），每个 Command/Query/Handler 均对照 WebAPI/LocalWebAPI 控制器 `Sender.Send` 派发点验证；C 级 22 个均为「类活但含死方法」，处置=删方法/瘦接口非删类 |
| 2 | Desktop MVVM 分层（A-21 M5 后遗漏越层） | ❌ **发现 6 处遗漏越层**（Shell 1 + Admin 4 + Clinical 1，见 §5 小结）；A-21 M5 修的 3 处（Modules 层）确认生效；**架构测试守卫未覆盖 Roles/Shell 层 VM 注入面**（P01c 只扫 Server 程序集）——建议补守卫 |
| 3 | Infrastructure 14 类职责归属 | ✅ **判定完成**：Http 目录仍滞留 Infrastructure（LoggingHttpHandler 存活应下沉 Foundation → C 待决策；ApiResponseHelper 死 → D 删）；CardReader 存活（被 Clinical+Patients 消费）→ 保留标注待独立 P2；LocalData 生产层已清理（A-21 C1 生效）✅；模块审计 §D 6 项复核：5 项确认为死代码、1 项（Http）判 C |
| 4 | Shared.Models 八目录归属 | ✅ **144 类型全部分配**：Attributes(3)/Contracts(~90)/Enums(14)/Primitives(7)/Utilities(8)/Validators(9)/DTOs(2)/Extensions(1)，零孤儿；八目录全链有消费 |
| 5 | C/D 级孤儿类排查 | ✅ **D=29**（全清单见 §1.2）：Desktop 侧 28 个（Patients 12 死组件链 + Infrastructure 9 + Foundation 2 + Formula 2 + Contracts/Controls/MedicalCase 各 1）+ Shared.Configuration 1；**C=62**（Server 死方法 22 + Desktop 越层 6 + 测试基建 21 + 其余 13） |

## 8. 处置建议（按批次）

**P1 建议删除（已符号级确认死代码，低风险）**：
- Desktop：ApiRouter+IApiRouter / UnfinishedCaseDialog 系列+UnfinishedCaseChoice / SensitiveInfoFilter / ResponsiveLayoutBehavior / ConfigurationExtensions 空实现 / Patients 4 死组件链 12 类（~680 行）/ FormulaItem+FormulaHerbItem（~440 行）/ MedicalCaseChangeTracker / ApiResponseHelper / QueryStringBuilder / RegistrationNavParams / BaseDialogWindow / SuggestionType
- Shared：MonitoringOptions
- 同步：ApiRouterTests / MedicalCaseChangeTrackerTests / 相关 DI 注册（PatientsModule:47-53 / ServiceCollectionExtensions:103,211 / App.xaml.cs:102-103）

**P1 建议修复**：
- 6 处 VM→IApiClient 越层改走 Service（修复路径见 §5 小结表）
- 架构测试补守卫：Roles/Shell 层 VM 不得注入 IApiClient*（修复 P01c 只扫 Server 的真空）

**P2 待决策**：
- Server 模块死方法/瘦接口 22 处（含 MedicalCase 11 符号、Repository 死方法 9 处、PatientCrossModuleService 3、RegistrationMapper.ToEntity）
- Infrastructure Http 目录下沉 Foundation（LoggingHttpHandler）
- Shared.Logging AddSharedLogging 接线或删除；UnauthorizedException 401 语义核查
- Foundation IApiService/ApiService 接通或删除；3 个惰性 AuthEvents
- Tests.Desktop Traits 18 类 + UserJourneyTestBaseShared 删除决策
- LocalWebApiProgram.RunAsync 双入口合并；Registration 命名空间复数对齐；MedicalCase 命名空间分裂对齐

**文档滞后项（蓝图 v1.0 基线不动，记录偏差）**：
- 03-server.md「ICrossModuleAuthService 未实现」→ 实际已落地为 IAuthCrossModuleService
- WebAPI AGENTS.md「14 controllers」→ 实际 12 个
- 蓝图 §2.2 文件数核对：Auth 26/Users 30(+1 类型数)/Patients 23/Herbs 23(+1)/Formula 21/MedicalCase 22/Registration 27/Reports 11(+4)——Reports 因 A-21 删 Domain 后为 11 类型（蓝图写 7 文件，类型数含投影 record）
- 蓝图 §3.2 文件数与实际一致（MedicalCase 50/Shell 49）

## 9. 变更记录

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.0 | 2026-08-08 | 初版：A-22 逐项目逐 class 验证（34 项目 1422 类型，A=1009 B=322 C=62 D=29） |
