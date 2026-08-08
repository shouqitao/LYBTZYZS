# WebApi 架构深度分析报告（Mimo Code 独立分析）

> 分析日期: 2026-08-07 | 分析者: Mimo Code Agent（独立审计，非 Hermes Agent 复本）
> 分析范围: `src/Server` 全部源码（WebAPI 入口、Controllers、8 个业务模块、Infrastructure、Middleware、Extensions）
> 约束: 只分析不修改代码；所有结论基于实际代码（文件:行号），不基于文档描述。
> 基线对照: `docs/compose/reports/webapi-deep-analysis-2026-08-07.md`（Hermes Agent v1.0）

---

## 零、总体评价

架构质量 **良好（7.5/10）**。模块化单体 + MediatR CQRS + 三层分层边界清晰，架构测试强制约束有效（Service 不直注 DbContext、模块互不引用均有代码佐证）。未发现需要推倒重建的架构级缺陷。

但本审计发现 **2 个 Hermes 报告遗漏的正确性问题**（其中 1 个为"缓存从未生效"的误导性配置），以及 **4 个 P1 级功能/安全隐患**（Token 过期错位、RefreshToken 500 崩溃、SignalR 分组越权、批量导入改错对象）。Hermes 报告整体方向正确但部分建议基于文档而非代码验证，详见 §四 对比表。

---

## 一、架构模式发现

### A-01 [P2] 「每模块独立 DbContext」约定被实际破坏

- **位置**: `ReportsModule.cs:23`（注册 ReportsDbContext）、`ReportRepository.cs:14`（注入 AppDbContext）；`AuthModule.cs:28`（注册 AuthDbContext）、`AuthSessionRepository.cs:12`（注入 AuthDbContext）vs `SecurityAuditRepository.cs:9`（注入 AppDbContext）
- **问题**: Server/AGENTS.md 声明"每模块独立 DbContext（per-module data isolation）"，但实际仅 Formula 模块有独立 DbContext；Reports 模块注册了 `ReportsDbContext` 却从未使用（`ReportRepository` 注入的是 `AppDbContext`，`ReportsDbContext.cs` 无 DbSet、无 OnModelCreating，是空壳）；Auth 模块同时注册 AuthDbContext 和注入 AppDbContext，同模块内两个 DbContext 混用，隔离性自相矛盾。
- **后果**: (1) `ReportsDbContext` 空壳注册浪费 DI 注册项且误导后续开发者；(2) Auth 模块的 `AuthSession` 索引配置（AuthDbContext.cs:31-33 建了 UserId/TokenHash/ExpiryTime 索引）与 AppDbContext 的 `AuthSessionConfiguration.cs`（已按 #1765 删除索引）**互相冲突**，且模块内无 Migrations 目录——索引永不落地，`GetByTokenHashAsync` 实际全表扫描。
- **建议**: 删除 `ReportsDbContext` 与 `AuthDbContext`，统一使用 `AppDbContext`（与 Registration/Patients 模块一致）；在 AppDbContext 侧为 `AuthSessions.TokenHash` 补建索引迁移。
- **预估工时**: 0.5d

### A-02 [P2] Auth 模块隐藏依赖 Patients 模块（注册顺序耦合）

- **位置**: `LoginCommandHandler.cs:24`（注入 `ICrossModuleService`）、`PatientsModule.cs:35`（注册 `ICrossModuleService → CrossModuleService` 的位置）
- **问题**: `ICrossModuleService` 门面接口定义在 `Core/LYBT.Infrastructure/Services/CrossModule`，但其实现 `CrossModuleService` 注册在 **PatientsModule**（业务模块）中。Auth 模块在概念上只依赖 Core 层，实际解析却依赖 Patients 模块先完成注册——若模块注册顺序调整（ServiceCollectionExtensions.cs:87-123 的 RegisterBusinessModules 中 Auth 最先、Patients 第四），或将来 Patients 模块被移除，Auth 会在运行时解析失败。
- **建议**: 将 `CrossModuleService` 门面实现移至 `LYBT.Infrastructure`（Core 层）注册，业务模块只注册各自的 `IXxxCrossModuleService` 具体实现。
- **预估工时**: 0.5d

### A-03 [P2] 双写路径架构不一致（MediatR vs Service 直连）

- **位置**: `FormulaModule.cs:49-57`（注册 MediatR）与 `FormulasController.cs:120,172,192`（Update/Toggle/Restore 直连 `IFormulaService`）；对比 Patients/Users 模块同样混用
- **问题**: 同一模块内 Create/Delete/BatchImport 走 MediatR 管道（含 FluentValidation），而 Update/Toggle/Restore/BatchEnable 走 Service 直连仓储，绕过 MediatR pipeline → FluentValidation 验证器只对命令路径生效。Controller 用 `TryDeserializeDto` 手动验证补齐，但验证规则存在两套（DataAnnotations + FluentValidation），Shared DTO 验证器（如 `PatientInputDtoValidator`/`HerbInputDtoValidator`）实际永不执行。
- **建议**: 统一写路径——要么全部走 MediatR，要么显式在 Service 入口调用 FluentValidation；移除 Shared 层永不执行的 DTO Validator 注册（`AuthModule.cs:59` 的 Shared 版 LoginRequestValidator 已确认是死代码，见 C-03）。
- **预估工时**: 1d

### A-04 [P2] MedicalCase 接口泄漏聚合实体（CQRS 读/写边界不清）

- **位置**: `IMedicalCaseQueryService.cs:190` 行、`IMedicalCaseCommandService.cs:188` 行（过宽）；`IMedicalCaseQueryService.GetByIdAsync` 返回 `Task<MedicalCase?>`（实体）
- **问题**: 查询接口大量暴露领域实体而非 DTO，CQRS 查询侧应只返回 DTO；两接口各近 200 行，方法数过多，职责过宽。Services 共 2,356 行（QueryService 565 / CommandService 487 / StateService 362 / PrescriptionService 335 / ItemService 220 / Helper 198 / Deletion 130），模块总体 4,658 行，为第二大模块（Users 1,761）的 2.6 倍。
- **建议**: 查询侧方法改返回 DTO；评估 QueryService 按"列表查询 / 详情聚合"再拆分。**注意**：`GetListDtoAsync` 返回的 `MedicalCaseListDto` 由 Mapperly 映射后需手工补 computed 属性（`HasConsultation/HasPrescription`，MedicalCaseQueryService.cs:86-91），拆分时保持该行为不变。
- **预估工时**: 1-2d（中等风险，Desktop Refit 契约约束）

---

## 二、性能发现

### P-01 [P1] OutputCache 在全部标注端点上从未生效（Hermes 遗漏）

- **位置**: `DatabaseServiceCollectionExtensions.cs:66-102`（6 个缓存策略）、`HerbsController.cs:39`、`PatientsController.cs:39`、`FormulasController.cs:39`、`MedicalCasesController.cs:50`
- **问题**: **ASP.NET Core 8 的 OutputCache 默认策略明确不缓存"认证请求"的响应**（官方文档 "Responses to authenticated requests aren't cached"）。上述 4 个标注 `[OutputCache(...)]` 的 GET 端点全部带 `[Authorize(Policy = ...)]`（医生/管理员角色），因此缓存**从未命中过**——标注是纯装饰。而 `AddBasePolicy(builder => builder.Expire(5min))` 又对所有未标注意的端点默认生效，但那些端点要么是 POST（不缓存）要么未标注（默认策略缓存 5 分钟，其中 `[Authorize]` 的仍不缓存）。
- **影响**: 缓存配置给维护者造成"已有缓存"的错觉（Hermes 报告 O-06 正是在此错觉上建议"加强缓存"，方向完全错误）；对已认证高频查询（医案列表）零收益。
- **建议**: (1) 删除这些无效标注或改为显式缓存策略并 `VaryByValue` 用户维度（注意安全：医疗数据列表按 operatorId 过滤，即使要缓存也必须以用户为缓存键）；(2) 真正的高频只读列表若需缓存，优先在 Service 层用 `IMemoryCache`（按用户+查询参数做键），而非 OutputCache。
- **预估工时**: 0.5d

### P-02 [P2] 医案查询内存分页（全量加载后 Skip/Take）

- **位置**: `MedicalCaseQueryService.cs:219-229`（SearchMedicalCasesAsync）、`:318-323`（QueryByPatientAsync）、`:250-262`（GetPatientRecentMedicalCasesAsync）、`:438-464`（GetPatientConsultationsAsync）、`:473-500`（GetPatientPrescriptionsAsync）
- **问题**: 这些方法先 `ToListAsync` 全量拉取再在内存中分页；而 Repository 已提供 DB 层分页的 `QueryPagedAsync`（`MedicalCaseRepository.cs:192`）却**未被 Service 消费**。患者历史数据多时内存/响应膨胀。
- **建议**: Service 侧改用 `QueryPagedAsync` / `GetByPatientIdPagedAsync`，删除内存分页版本。
- **预估工时**: 0.5d

### P-03 [P2] AsNoTracking 使用不足（全 Server 仅 5 处）

- **位置**: `MedicalCaseRepository.cs:38-45`（GetDetailQuery 默认跟踪）、`PatientRepository.cs:42-64`、`RegistrationRepository.cs:37-73`（GetPagedAsync 无 AsNoTracking）
- **问题**: 只读查询默认进入变更跟踪，EF Core 需维护 identity map 快照，列表/详情场景属纯读，开销浪费。`RegistrationRepository.GetWaitingQueueAsync`（:85-98）已正确使用 `AsNoTracking`，但同文件其他只读方法未用，标准不一。
- **建议**: 在只读查询统一加 `.AsNoTracking()`；注意 `UpdateAsync` 用 `_context.Update(entity)` 的路径（如 `MedicalCaseRepository.Update.cs`）不要加，避免 detached 实体状态混乱。
- **预估工时**: 0.5d（低风险高收益）

### P-04 [P2] 批量导入逐条查库（N+1）

- **位置**: `BatchImportPatientsCommandHandler.cs:61-101`、`BatchImportHerbsCommandHandler.cs:52-101`、`BatchImportFormulasCommandHandler.cs:52,101`
- **问题**: 每条记录 `ExistsByNameAsync`（查库）+ `AddAsync`（落库）各一次往返；10000 条上限下最坏 2 万次 DB 往返。
- **建议**: 预加载导入集内所有重名候选一次，内存判定；落库改为批量 Add（一次 SaveChanges）或 `AddRange`。
- **预估工时**: 0.5d

### P-05 [P2] Reports 多轮查询与内存聚合

- **位置**: `ReportRepository.cs:32-43`（先 Select caseIds 再 Contains，两轮）、`:113-154`（GetDoctorPerformanceAsync 4 次独立聚合后内存 join）、`:169-173`（GetPatientFlowByDayAsync 全量加载再分组）
- **问题**: 报表查询对全量数据做聚合，多次往返 + 内存 join；数据量大时（数月运营）内存与延迟上升。
- **建议**: 单次 LINQ 聚合下推（`GroupJoin` 或单条 SQL）或接受现状（诊所规模数据量小，优先级低）。**建议先测量再优化**。
- **预估工时**: 1d（低优先级）

### P-06 [P3] SensitiveDataJsonConverterFactory 静态缓存无上限

- **位置**: `SensitiveDataJsonConverterFactory.cs:19`（`_hasSensitivePropertiesCache` 静态 Dictionary）
- **问题**: 类型检查结果缓存永久增长（按反序列化遇到的类型数），长时间运行类型多时内存缓慢增长。有 lock 保护正确性，但无容量上限。
- **建议**: 改用 `ConcurrentDictionary<Type,bool>` + 按需清理，或接受（类型集有限）。
- **预估工时**: 0.25d

---

## 三、安全发现

### S-01 [P1] LoginCommandHandler 硬编码 Token 过期时间（60 分钟）与 JWT 配置错位

- **位置**: `LoginCommandHandler.cs:157`（`var tokenExpireMinutes = 60;`）
- **问题**: JWT 实际过期由 `JwtOptions.AccessTokenExpirationMinutes` 控制（appsettings 基础配置 480，Production 30），但 `AuthSession.ExpiryTime`（LoginCommandHandler.cs:168）与响应 `ExpiresAt`（:163）按硬编码 60 计算。后果：生产环境 JWT 30 分钟过期，但客户端按 ExpiresAt=60 判断续期——30~60 分钟窗口内客户端仍持过期 Token 请求，全部 401；基础配置下 JWT 8 小时有效但会话 60 分钟被判失效，`RefreshToken` 会拒绝本可用的会话。
- **建议**: 删除硬编码，统一读取 `IOptions<JwtOptions>.Value.AccessTokenExpirationMinutes`。
- **预估工时**: 0.25d

### S-02 [P1] RefreshToken 在无会话记录时抛异常返回 500

- **位置**: `RefreshTokenCommandHandler.cs:89-94`
- **问题**: 当 `oldSession == null`（会话记录缺失/被清理，但 JWT 签名仍有效）时，`AuthSession.Create(oldSession?.UserId ?? Guid.Empty, ...)` 传入 `Guid.Empty`，而 `AuthSessionModel.cs:79-80` 对 `userId == Guid.Empty` 抛 `ArgumentException`——未被捕获，走 SystemExceptionHandler 返回 500。语义上应返回 `AuthTokenInvalid`（401/422）。
- **建议**: 对 `oldSession == null` 分支显式返回 `Result.Failure(ErrorCode.AuthTokenInvalid, "会话不存在，请重新登录")`。
- **预估工时**: 0.25d

### S-03 [P1] RegistrationHub 分组越权（任意医生可订阅他人实时通知）

- **位置**: `RegistrationHub.cs:24-30,49-58`
- **问题**: `OnConnectedAsync` 直接信任 query string `doctorId` 并将连接加入 `doctor-{doctorId}` 分组，**未校验该值等于当前登录用户**。任何 Doctor/Admin（`[Authorize(Policy = DoctorOrAdmin)]`）可伪造 `?doctorId=<他人ID>` 连接，实时接收该医生的患者挂号/状态变更推送（患者姓名、挂号状态等敏感信息）。
- **建议**: 在 `OnConnectedAsync` 从 `Context.User` 提取当前用户 ID 并与 query 的 doctorId 比对，不一致则断开（`Context.Abort()`）；或直接忽略 query，用认证身份的 ID 入组。
- **预估工时**: 0.25d

### S-04 [P1] 批量导入 DuplicateStrategy.Update 可能改错对象

- **位置**: `BatchImportPatientsCommandHandler.cs:72`（`GetPagedAsync(1, 1, dto.Name, ...)`）
- **问题**: 用 `GetPagedAsync` 的关键字匹配定位重复患者，但该查询匹配 Name/PinYin/Phone 三元组（`PatientRepository.cs:52-55`），且只取第一条（按 Name 排序）。存在同名不同人（或同电话/同拼音）时，Update 策略会更新**错误患者**记录——数据完整性风险（身份证号、电话被覆盖）。
- **建议**: 新增精确 `GetByNameAsync(string name)`（仅 Name 精确匹配），导入更新路径使用之；Herbs/Formula 导入同理检查（它们按 HerbName/Name 精确匹配，风险较低）。
- **预估工时**: 0.5d

### S-05 [P2] 角色解析失败静默降级为 Doctor

- **位置**: `JwtService.cs:258-259,336-337`、`OperatorAccessor.cs:49-61`
- **问题**: Token 中角色值非法时静默降级为 `UserRole.Doctor`（有业务权限的医生角色），而非拒绝请求。若签发端异常或 Token 被篡改（签名仍有效但 claim 异常），攻击者可能以 Doctor 身份获得业务能力。
- **建议**: 角色解析失败应返回鉴权失败，而非默认 Doctor。
- **预估工时**: 0.25d

### S-06 [P3] 调试日志残留（SQL 语句与诊断日志）

- **位置**: `MedicalCaseRepository.cs:260-261`（`query.ToQueryString()` 记入日志）、`:238-257`（大量 `[诊断]` 前缀日志）
- **问题**: 生产环境日志会记录完整 SQL（含参数化前的表达式结构），且 `ToQueryString()` 有编译开销；`[诊断]` 调试日志为 P0 Bug 排查遗留，属噪音。
- **建议**: 移除 `[诊断]` 日志与 SQL 记录（或降为 Trace 并在生产关闭）。
- **预估工时**: 0.25d

### S-07 [P3] CORS 配置允许空 origin 数组时行为未验证

- **位置**: `ApiServiceCollectionExtensions.cs:29-30`（`Get<string[]>() ?? Array.Empty<string>()`）
- **问题**: `AllowedOrigins` 未配置时 `WithOrigins()` 空数组——CORS 策略对无 Origin 头请求（WPF/桌面客户端本就不带 Origin）实际不拦截，属低风险；但 Swagger 页面若同源访问也不受影响。建议保持现状（桌面客户端不受 CORS 约束），仅确认 `AllowCredentials` 与具体 origin 组合无通配符冲突。
- **预估工时**: 无需改动，仅确认

---

## 四、代码质量发现

### Q-01 [P2] 批处理命令三模块同构重复

- **位置**: `BatchDeleteUsersCommandHandler.cs:22-64`、`BatchDeletePatientsCommandHandler.cs:22-77`、`BatchDeleteHerbsCommandHandler.cs:22-71`（循环→GetById→SoftDelete→Update→累计 BatchOperationResultDto 完全同构）；`BatchEnable/Disable` 各模块同样复制
- **问题**: 3 模块 × 3 操作 = 9 个近乎复制的 Handler，且 Users 版逐条 SaveChanges（`BaseRepository.UpdateAsync` 每次 SaveChanges），N+1 + 多事务。
- **建议**: 下沉泛型 `BatchSoftDeleteHandler<TEntity>` / `BatchStatusHandler<TEntity>`（Core/Infrastructure 层），业务 Handler 只传实体与状态规则；批量操作合并单次 SaveChanges。
- **预估工时**: 1d

### Q-02 [P2] TryDeserializeDto 双轨验证（12 处调用）

- **位置**: `ControllerBaseExtensions.cs:153-180`；调用处：`HerbsController.cs:79,105`、`PatientsController.cs:82,106`、`FormulasController.cs:84,108`、`MedicalCasesController.cs:101,136`、`BaseUsersController.cs:67,83`、`RegistrationsController.cs:61`（共 12 处）
- **问题**: `[FromBody] object dto` 绕过模型绑定，手动 `Serialize→Deserialize→DataAnnotations` 验证。双重序列化开销 + 与管道 FluentValidation 双轨并行，规则分散。
- **建议**: 改 `[FromBody] TInputDto` 强类型绑定 + 全局 `[ApiController]` 自动 400（已配置 `InvalidModelStateResponseFactory`），移除 TryDeserializeDto。**注意**：需验证强类型绑定 + `SuppressModelStateInvalidFilter=false` 的响应格式与 Desktop 客户端契约兼容（Hermes 报告 O-02 同样指出，一致）。
- **预估工时**: 0.5-1d（中等风险，涉及 6 个 Controller 契约验证）

### Q-03 [P3] 死代码与无效注册清单

| 位置 | 问题 |
|------|------|
| `AuthModule.cs:59` | Shared 版 `LoginRequestValidator`（`IValidator<LoginRequest>`）注册但 `ValidationBehavior` 只解析 `IValidator<LoginCommand>`，永不执行 |
| `FormulaRepository.cs:119-136` | `GetAllWithHerbsAsync`/`GetByCategoryWithHerbsAsync` 无调用方 |
| `MedicalCaseQueryService.cs:295` `GetBatchAsync` | 无调用方（接口成员 :156 同步删除） |
| `HerbRepository.cs:108-143` | `GetAllAsync`/`GetByCategoryAsync`/`GetByNameOrPinyinAsync` 服务器端无调用者 |
| `CheckHerbReferenceQueryHandler.cs:51`、`CheckPatientReferenceQueryHandler.cs:36` | `CanDelete=true` 恒真，引用检查结果中该字段无实际含义 |
| `ReportsModule.cs:23` | 空壳 `ReportsDbContext` 注册（见 A-01） |
| `AuthController.cs:165-169` | `Get()` 返回 `BusinessFail("方法不允许")` 的死端点（会占 `GET api/v1/auth` 根路由） |

- **建议**: 删除上表死代码。**预估工时**: 0.5d

### Q-04 [P3] 空 catch 14 处（部分合理部分需记录）

- **位置**: `BaseApiController.cs:51`（LogOperation 吞日志异常，合理）、`JwtService.cs:215,294,372`（验证失败返回 null，合理但应记 Debug）、`IdentitySeedData.cs:93-97`（掩盖真实错误）、`BatchImportPatientsCommandHandler.cs:104-116`（导入失败原因全丢，只记"导入失败"）、`BatchDeleteUsersCommandHandler.cs:59` 等
- **建议**: 批处理类空 catch 至少记录 `_logger.LogWarning(ex, ...)`，保留失败明细字段。
- **预估工时**: 0.25d

### Q-05 [P3] 命名与结构不一致

- **位置**: 目录 `LYBT.Module.Formula` vs namespace `LYBT.Module.Formulas`（多处 using 混用）；`FormulaBatchImportCommandValidator.cs:4` 验证器命名空间 `LYBT.Module.Formula.Application.Validators` 与他者拼写不同；`AuthSessionModel.cs` 文件名 vs 类名 `AuthSession`（文档与代码对模型命名有出入）
- **建议**: 统一 namespace 与文件命名。**预估工时**: 0.25d（低优先级）

### Q-06 [P3] Program.cs 过长（335 行）且职责混杂

- **位置**: `Program.cs` 全文
- **问题**: 承载热更新检查（42-64）、Serilog 两阶段初始化（66-150）、配置注册（152-164）、密码验证（166-167, 279-332）、Identity 注册（173-184）、Kestrel（225-229）、应用初始化（234-248）、中间件（250-255）。可读性一般。
- **建议**: 提取 `ValidateDefaultPasswordConfiguration` 到独立类（已存在 `PasswordPolicyValidator`，可直接复用）；热更新逻辑独立方法。**预估工时**: 0.5d（Hermes O-07 已提出，一致）

---

## 五、与 Hermes 报告对比（重点差异标注）

| Hermes 编号 | Hermes 结论 | Mimo 复核 | 判定 |
|---|---|---|---|
| O-05 SplitQuery | "MedicalCase 32 处 Include 建议加 AsSplitQuery" | **`DatabaseServiceCollectionExtensions.cs:140` 已全局配置 `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)`**，无需逐查询添加 | ❌ Hermes 遗漏全局配置，建议冗余 |
| O-06 缓存加强 | "OutputCache 已应用于 4 个 GET 列表端点，建议加强缓存" | **ASP.NET Core 8 默认策略不缓存认证请求**，4 个端点全带 `[Authorize]`，缓存从未生效；建议不是"加强"而是"修正或删除" | ❌ Hermes 基于表面配置误判，方向相反 |
| O-04 AsNoTracking | "查询未统一 AsNoTracking" | ✅ 确认（全 Server 仅 5 处） | ✅ 一致 |
| O-01 MedicalCase 过重 | "4,658 行，QueryService 565 行" | ✅ 确认（Services 2,356 行，QueryService 565 行） | ✅ 一致 |
| O-02 TryDeserializeDto | "12+ 处重复" | ✅ 确认（12 处调用） | ✅ 一致 |
| O-08 继承层级过深 | "4 层继承需评估" | ⚠️ 实际为 3 层业务继承（BaseApi→BaseCrud→BaseMedicalCases→MedicalCases 为 4 层但 BaseMedicalCases 仅 216 行，含状态流转），风险评级可降为 P3 | ⚠️ 部分一致，严重度可下调 |
| O-10 CORS | "生产环境收紧" | ✅ 确认 WPF 不受 CORS 约束，维持现状 | ✅ 一致 |
| O-07 Program.cs 过长 | "335 行需瘦身" | ✅ 确认 | ✅ 一致 |
| —（Hermes 未覆盖） | — | **S-01 Token 过期硬编码 60 分钟错位**、**S-02 RefreshToken 无会话 500**、**S-03 SignalR 分组越权**、**S-04 批量导入改错对象**、**A-01 空壳 DbContext/索引漂移**、**A-02 Auth 隐藏依赖 Patients**、**P-01 OutputCache 从未生效**、**P-02 医案内存分页** | 🆕 本报告新发现 |

---

## 六、优化建议汇总表（按优先级排序）

| 优先级 | 编号 | 建议 | 类型 | 预估工时 |
|--------|------|------|------|---------|
| P1 | S-01 | 统一 Token 过期时间来源（删硬编码 60） | 安全/正确性 | 0.25d |
| P1 | S-02 | RefreshToken 无会话返回 401 而非 500 | 安全/健壮性 | 0.25d |
| P1 | S-03 | RegistrationHub 校验 doctorId == 登录用户 | 安全（越权） | 0.25d |
| P1 | S-04 | 批量导入 Update 用精确 Name 匹配 | 数据完整性 | 0.5d |
| P1 | P-01 | 修正/删除无效 OutputCache 配置，缓存按用户维度设计 | 性能（误导性配置） | 0.5d |
| P2 | A-01 | 删除空壳 ReportsDbContext，统一 AuthSession 走 AppDbContext 并补索引迁移 | 架构/性能 | 0.5d |
| P2 | A-02 | CrossModuleService 门面移至 Core 层注册 | 架构（模块解耦） | 0.5d |
| P2 | A-03 | 统一写路径（MediatR 或 Service+FluentValidation 二选一） | 架构一致性 | 1d |
| P2 | A-04 | 医案查询接口改返 DTO，评估 QueryService 再拆分 | 可维护性 | 1-2d |
| P2 | P-02 | 医案内存分页改 DB 层分页（消费 QueryPagedAsync） | 性能 | 0.5d |
| P2 | P-03 | 只读查询统一 AsNoTracking | 性能 | 0.5d |
| P2 | P-04 | 批量导入预加载重名集，批量落库 | 性能 | 0.5d |
| P2 | S-05 | 角色解析失败拒绝而非降级 Doctor | 安全 | 0.25d |
| P2 | Q-01 | 批处理 Handler 下沉泛型模板 | 代码质量 | 1d |
| P2 | Q-02 | 移除 TryDeserializeDto 双轨验证 | 代码质量 | 0.5-1d |
| P3 | P-05/P-06 | 报表聚合下推 / 缓存字典并发化 | 性能 | 1.25d |
| P3 | S-06/S-07 | 移除调试日志 SQL / 确认 CORS 空数组行为 | 安全 | 0.25d |
| P3 | Q-03/Q-04/Q-05/Q-06 | 死代码清理 / 空 catch 补日志 / 命名统一 / Program.cs 瘦身 | 代码质量 | 1.5d |

**合计预估: 约 11-13 人日**

---

## 七、不建议优化的（维持现状）

| 项目 | 理由 |
|------|------|
| 拆分 MedicalCasesController | 状态流转仅 4 方法，ROI 不合理（与 Hermes 结论一致） |
| 删除 MediatR | 命令路径验证+审计收益已验证，混合模式可接受（与 Hermes 一致） |
| 引入 Redis 分布式缓存 | 单机诊所规模，MemoryCache 足够，复杂度不值 |
| 报表查询提前优化 | 诊所数据量小，先测量再动手 |
| 引入源生成 JSON 之外的序列化方案 | 已有 System.Text.Json 源生成（LybtJsonContext），足够 |

---

*报告版本: v1.0 | 分析者: Mimo Code | 保存日期: 2026-08-07*
*依据: ASP.NET Core 8 OutputCache 官方文档（认证请求默认不缓存）+ 全部 Server 源码逐文件核读*
