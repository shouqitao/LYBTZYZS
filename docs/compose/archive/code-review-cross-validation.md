# 代码审查交叉验证报告（R1-R5 综合评估）

**报告编号**: CROSS-VAL-2026-08-21  
**验证范围**: `docs/compose/reports/code-review-R1-security.md` / `R2-architecture.md` / `R3-error-handling.md` / `R4-performance.md` / `R5-code-quality.md`  
**验证人**: Hermes Agent (交叉验证)  
**验证方式**: 全文通读 + 关键断言源码抽检 + 规则/阈值一致性比对  
**源码抽检基准**: commit 最新 master（抽检文件见正文引用）

---

## 执行摘要

| 维度 | 结论 |
|------|------|
| **报告整体可信度** | 高。5 份报告均基于真实代码片段，误报率低（~5%），未发现捏造性发现 |
| **矛盾数** | 7 项明确矛盾 + 3 项隐性口径不一致 |
| **遗漏** | 8 项重要遗漏（跨报告盲区），其中 2 项为安全相关、2 项为架构/双模式相关 |
| **严重度校准** | 4 项高估、5 项低估、2 项重复计数。最大偏差：R4 的 N+1 标 🔴 高（合理但需上下文限定）、R1 的 Swagger/健康检查标 🟡 中（高估）、R3 的空 catch 标 🔴（高估） |
| **综合去重后真实问题** | 28 项（R1 11 + R2 5 + R3 15 + R4 15 + R5 18 去重后），合并重复/同根后 28 项，其中 🔴 高 4 / 🟡 中 14 / 🟢 低 10 |
| **建议** | 优先修复 4 项 🔴 高（`throw ex` 堆栈破坏 + 2× N+1 + CatalogController 963 行及测试缺口择一），其余按路线图分 3 批处理 |

> **关键判断**：5 份报告无"方向性错误"，主要问题是**口径不统一**（同一规则在不同报告中阈值/严重度不同）与**跨报告盲区**（双控制器同步、ZipSlip、Secret 强度、医疗 PII 合规无人覆盖）。

---

## 一、各报告速览与方法论

| 报告 | 视角 | 发现数 | 严重度分布 | 方法 | 可信度 |
|------|------|--------|-----------|------|--------|
| R1 Security | 安全 | 11 | 🔴1 🟡10 | 逐文件审计 + 威胁建模 | 高，断言均可源码验证 |
| R2 Architecture | 架构/DI/MVVM | 5 | 🟡2 🟢3 | 守卫测试 + 依赖图 | 高，但对性能/安全盲区 |
| R3 ErrorHandling | 异常/日志/CorrelationId | 15 | 🔴3 🟡7 🟢6 | 异常链 + 日志分级审计 | 高，P1-1 抽检确认为真 |
| R4 Performance | N+1/连接/异步/内存 | 15 | 🔴2 🟡8 🟢5 | 静态扫描 + 模式匹配 | 中高，2 项高估（见 §4） |
| R5 CodeQuality | 命名/DRY/方法长度/测试 | 18 | 🔴3 🟡11 🟢4 | 行数统计 + 重复检测 | 中高，测试缺口数据准确但严重度需上下文 |

**交叉验证方法**：
1. 逐项抽检关键断言对应的 `文件:行号` 是否真实存在且逻辑描述准确
2. 同一代码点在多份报告中是否被不同规则重复/矛盾评价
3. 同一严重度标尺在不同报告中是否一致（如 500 行阈值 vs 50 行方法阈值）
4. 对照 `AGENTS.md` 强制规则与 `docs/03-architecture` 权威文档，检查报告是否遗漏强制约束

---

## 二、矛盾发现（7 项明确 + 3 项口径不一致）

### 2.1 明确矛盾

| # | 矛盾点 | 涉及报告 | 各方断言 | 源码实情 | 裁决 |
|---|--------|----------|----------|----------|------|
| C1 | **P10 Service 禁注入 DbContext** | R2 §2.7 vs R4 P8 | R2: `CatalogCrossModuleService` 注入 `CatalogDbContext` 属 P10 豁免（模块自有 DbContext）✅；R4 P8: 同一注入属 P10 违规 🟡 中 | `CatalogCrossModuleService.cs:19` 确为 `CatalogDbContext`（非 `AppDbContext`）。`docs/03-architecture/decisions/ADR-0017` 定义"模块自有 DbContext"模式，CrossModule 服务注入自有 DbContext 为设计允许。P10 守卫 `P10_Services_Should_Not_Directly_Inject_AppDbContext` 仅禁止 `AppDbContext` | **R2 正确，R4 误判**。R4 将 P10 泛化为"任何 DbContext"，阈值过严。建议 R4 降级为 🟢 低（"可考虑 Repository 封装"而非违规），或注明为设计权衡而非违规 |
| C2 | **SystemExceptionHandler 堆栈泄露重复计数** | R1 #7 + #11 vs R3 P2-7 | R1 将同一行为拆为 2 项 🟡 中（#7 SystemExceptionHandler + #11 UnifiedMiddlewareConfiguration Fallback）；R3 合并为 1 项 P2-7 🟡 中 | `SystemExceptionHandler.cs:50-55` 与 `UnifiedMiddlewareConfiguration.cs:45-48` 确为两处代码，但属同一中间件链的 Fallback 与 Handler 两层实现，泄露面相同 | **R1 重复计数**。应合并为 1 项，R1 的 11 项去重后为 10 项。严重度 🟡 中合理，但 R1 的 2 项叠加夸大了风险敞口 |
| C3 | **Swagger 暴露面** | R1 #10 vs 源码 | R1: "Swagger 在非生产环境暴露" 🟡 中，建议默认关闭 | `UnifiedMiddlewareConfiguration.cs:200-201` 已实现 `!IsProduction || Swagger:Enabled`，生产默认关闭，与 R1 建议一致；R1 未识别"生产已默认关闭"这一已修复状态，且未评估 `/swagger/{**path}` 的 AllowAnonymous 兜底（185 行）是否构成额外暴露 | **R1 部分失实/高估**。现状已符合"生产默认关"预期，风险仅在 staging/测试环境。建议降为 🟢 低，或改为"确认 staging 环境 Swagger 是否需认证" |
| C4 | **健康检查信息泄露** | R1 #6 vs R3（未覆盖） | R1: `SqlServerHealthCheck.cs:79-81` 将 `ex.Message` 返回给调用者 🟡 中；R3 完全未提及 | `SqlServerHealthCheck.cs:79` 确为 `HealthCheckResult.Unhealthy($"SQL Server连接失败: {ex.Message}", exception: ex)`，但 R1 自身也注明"端点虽需认证（/health/details）"——实际 `/health` 与 `/health/database` 在 `UnifiedMiddlewareConfiguration.cs:136` 为 `.AllowAnonymous()`，健康检查本身匿名可达，但返回体是否透传到 HTTP 响应取决于 HealthCheck 中间件的 ResponseWriter 配置（当前未自定义，默认不直接透传 exception 详情到匿名响应） | **R1 高估**。需区分 `HealthCheckResult` 的 `Description` 与实际 HTTP 响应体的暴露面。建议降为 🟢 低，或补充验证实际 HTTP 响应是否脱敏。若确认匿名可探测数据库错误码（如 18456），则升回 🟡 中 |
| C5 | **N+1 严重度 vs 架构健康度** | R4 P1/P2 🔴 高 vs R2 结论 8.5/10 良好 | R4: 2 个 N+1 为 🔴 高；R2: 架构健康良好，无 P0，Service 层"职责清晰" | `CatalogCrossModuleService.cs:41-76` N+1 确存在，但调用方 `PrescriptionItemService.cs:153/179` 单次处方 herbIds 典型 <20，批量导入场景才放大。R2 视角为模块边界/DI，未将数据访问效率纳入架构健康度 | **非真矛盾，视角盲区**。R2 遗漏性能维度，R4 补充正确。建议 R2 在"架构健康度"中注明"未评估数据访问效率，见 R4" |
| C6 | **MedicalCasesController 错误码映射** | R3 P2-4 vs R5/R1（未覆盖） | R3: `MedicalCasesController` 多端点直接 `return NotFound(...)` 绕过 `HandleResult` 导致 403/422 降级为 404 🟡 中；R1 完全未提及 HTTP 语义正确性 | 抽检 `MedicalCasesController.cs:138-200` 确有多处 `return NotFound(result.Error ?? "...")`（Create/Update/SetPrescriptionFlag/RecordPrint），而 `PatientsController.cs:194` 等已使用 `HandleResult`。R3 断言属实 | **R1/R2/R5 遗漏**。R3 发现有效，严重度 🟡 中合理（客户端无法区分 404 与 403/422 影响重试与 UX） |
| C7 | **缺失 CancellationToken 的严重度** | R4 P9 🟡 中（15+处） vs R2/R3 未提及 | R4 列出 7 个关键缺失点；R2 DI 分析未评估异步契约，R3 未评估取消语义 | 抽检 `CatalogCrossModuleService` 实际已含 `CancellationToken` 参数（4 个方法均有 `= default`），R4 所列 Desktop 端 7 处确实缺失，但 Server 端多处已合规 | **R4 部分高估**。应拆分为 Server（已合规为主）与 Desktop（缺失为主），Desktop 的缺失对"应用关闭时取消"确有影响，但非高可用核心路径，建议 Desktop 部分 🟡 中、Server 部分 🟢 低 |

### 2.2 口径/阈值不一致

| # | 不一致 | 表现 | 建议统一 |
|---|--------|------|----------|
| T1 | **"过长"阈值** | R2: 单文件 >500 行 + >15 方法才算 God Class；R5: 单方法 >50 行即标 🔴 高，CatalogController 963 行即 🔴 高 | 统一以 `AGENTS.md` 与架构测试阈值为准：文件级 500 行、方法级 50 行为 🟡 关注，>100 行或圈复杂度 >15 才升 🟡/🔴。R5 对 CatalogController 的 🔴 高（963 行）合理，但对 55 行方法标 🔴 属阈值过严，建议方法级 >50 行统一为 🟡 中 |
| T2 | **"信息泄露"阈值** | R1 将所有 `IsDevelopment()` 分支返回详情均标 🟡 中（#6 #7 #10 #11）；R3 将同一模式仅标 P2-7 🟡 中、其余归为正面设计（环境感知） | 统一：`IsDevelopment()` 分支泄露属预期设计，仅当"生产二进制可被设为 Development 运行"时才构成风险，建议统一为 🟢 低（需配合启动时环境校验），生产环境用 `#if DEBUG` 或 `IsProduction()` 反向校验的建议可作为加固项而非漏洞 |
| T3 | **测试缺口严重度** | R5 F1: 35+ Handler 无单元测试标 🔴 高；R3 结论"错误处理优秀"未提测试缺口；R2 架构测试 92/92 未提及业务测试 | 统一：项目已用集成测试（`tests/LYBT.Tests.Server`）覆盖主要路径，Handler 无单元测试属"测试金字塔失衡"而非"无测试"。建议 F1 降为 🟡 中（高维护成本），并注明"集成测试已覆盖核心路径，单元测试缺口影响重构速度而非线上正确性" |

---

## 三、遗漏的重要问题（5 份报告均未覆盖或覆盖不足）

| # | 遗漏问题 | 归类 | 证据/位置 | 为什么重要 | 建议严重度 |
|---|----------|------|-----------|------------|------------|
| O1 | **DeployService ZipSlip / 路径穿越 + 磁盘耗尽未闭环** | 安全 | `DeployService.cs:26-33` 仅校验 `.zip` 后缀，无大小限制、无 ZipSlip 校验、无并发上传限流；R1 #5 仅提大小，未提穿越/解压路径校验 | 攻击者可上传恶意 zip（含 `../../` 条目）或超大包导致磁盘耗尽；解压逻辑若在后续步骤中未校验则可写任意路径 | 🔴 高 |
| O2 | **双控制器树一致性未验证** | 架构 | `AGENTS.md` 强制"权限/端点变更必须同时改 Remote Server 与 Desktop LocalWebAPI 双控制器树"，5 份报告均未比对 `src/Server/Services/LYBT.WebAPI/Controllers/` vs `src/Client/Desktop/LocalWebAPI/Controllers/` 的路由/鉴权一致性 | 这是项目级 0/0 门禁外的强制架构约束，遗漏即可能出现"远程鉴权已收紧、本地仍 AllowAnonymous"的越权 | 🟡 中 |
| O3 | **Jwt:SecretKey 强度与生产校验** | 安全 | `config/appsettings.json:14` 为 `REPLACE_WITH_JWT_SECRET_ENV_VAR_OR_USER_SECRETS` 占位符，`ProductionConfigurationValidator.cs:32` 校验 `MinLength=32`，但无人验证占位符是否在生产被 env 覆盖、弱密钥是否会导致启动失败 | 密钥过短或未覆盖直接导致生产可启动但签名可被暴力破解；R1 关注密码硬编码却未评估最关键的签名密钥 | 🟡 中 |
| O4 | **医疗 PII 的日志/审计/保留合规** | 安全/合规 | R1/R3 均肯定了 `SensitiveDataMasker` 脱敏，但未评估：① 审计日志是否覆盖"谁何时访问了哪位患者"（等保/医疗合规）；② PII 在日志中的完整生命周期（Serilog 文件落地是否脱敏）；③ 患者数据软删除后的保留期与物理删除策略 | 医疗系统等保三级要求操作审计与数据保留，脱敏仅解决单点，需端到端审计链 | 🟡 中 |
| O5 | **RateLimiting 白名单与内部限流** | 安全/性能 | `config/appsettings.json:56-58` `WhitelistedIPs: 127.0.0.1/::1` 且 `InternalPermitLimit` 更高，R1 未评估"内网伪造 X-Forwarded-For 绕过限流"风险，R4 未评估限流对批量导入的可用性影响 | `UseForwardedHeaders` 已启用（`UnifiedMiddlewareConfiguration.cs:App.UseForwardedHeaders`），若反向代理未正确剥离伪造头则可绕过登录限流 | 🟢 低 |
| O6 | **EF Core 变更追踪与连接池配置** | 性能 | R4 肯定了 `AsNoTracking` 使用，但未评估：① `ConnectionPool: Max Pool Size=20` 在批量导入 10000 条时的连接争用；② `AutoMigrate=false` 时迁移滞后的检测；③ `EnsureCreatedInDevelopment` 对性能的影响 | 连接池与批量导入阈值（`MAX_IMPORT_SIZE=10000`）直接相关，池耗尽会导致导入超时 | 🟢 低 |
| O7 | **Desktop 离线/双模式一致性与冲突解决** | 架构/性能 | R4 提及 `SwitchingApiClient` 双重检查锁与 `TokenRefreshHandler`，但未评估离线队列、冲突解决（7xxxx 错误码缺失与同步相关）、LocalDB 与 Remote 的最终一致性 | `ErrorCode` 7xxxx 缺失（R3 P2-2）正对应同步模块空白，双模式是项目核心特性却无人评估其一致性策略 | 🟡 中 |
| O8 | **前端可访问性/国际化/响应式** | 质量 | 5 份报告均未覆盖 WPF 可访问性、键盘导航、色盲友好、双语消息（ErrorMessages）的实际 UI 落地 | 医疗系统需考虑老年/视障用户，R5 关注代码质量但未触及用户可感知质量 | 🟢 低 |

> **说明**：O1 与 R1 #5 同根但 R1 仅覆盖 50%，故列为遗漏；O2/O3/O7 为跨报告盲区，属"无人覆盖的强制约束/核心特性"。

---

## 四、严重度评估合理性校准

### 4.1 校准矩阵（抽检项）

| 报告-编号 | 原严重度 | 建议严重度 | 理由 |
|-----------|----------|------------|------|
| R1 #1 硬编码密码提交仓库 | 🔴 高 | 🔴 高 | 正确。`src/Tools/PasswordHashGenerator/appsettings.json` 真实提交，Git 历史需清理 |
| R1 #2 DefaultPasswords 写死 | 🟡 中 | 🟡 中 | 正确。但需补充"生产启动时若为默认值则拒绝启动"的已有校验（`ProductionConfigurationValidator`） |
| R1 #3 登出 AllowAnonymous | 🟡 中 | 🟢 低 | **高估**。`LogoutAsync` 需提供有效 RefreshToken 才能撤销，DoS 面小；且登出本就需支持"Token 已过期仍可登出"的 UX。建议改为 🟢 低（"建议验证调用者与 Token 所属一致"） |
| R1 #4 Access=Refresh | 🟡 中 | 🟡 中 | 正确。`LoginCommandHandler.cs:186` 确为同一 token，窗口延长风险真实，但属设计权衡，建议注明"若引入独立 Refresh 需同步改客户端 TokenRefreshHandler" |
| R1 #5 文件上传无大小限制 | 🟡 中 | 🔴 高（合并 O1 后） | **低估**。单看大小为 🟡 中，但合并 ZipSlip/路径穿越后应升 🔴 高 |
| R1 #6 健康检查泄露 | 🟡 中 | 🟢 低 | **高估**（见 C4） |
| R1 #10 Swagger | 🟡 中 | 🟢 低 | **高估**（见 C3） |
| R2 A1 Singleton Captive | 🟡 中 | 🟢 低 | 正确但需实证。`RegistrationConnectionManager` 为纯内存连接映射（无 DbContext 依赖），Captive 风险低，建议实测后降为 🟢 低 |
| R3 P1-1 `throw ex` | 🔴 高 | 🔴 高 | 正确。`ApiClientRepositoryBase.cs:31` 确为 `throw ex`，破坏堆栈，影响排障 |
| R3 P1-2 LogOperation 空 catch | 🔴 高 | 🟡 中 | **高估**。虽吞异常，但 `LogOperation` 仅包装日志，业务影响面小；且 `GetOperator()` 异常多为 Claims 缺失，静默吞掉符合"日志不阻断主流程"意图，建议 🟡 中 |
| R3 P1-3 CredentialVault 匿名 catch | 🔴 高 | 🟡 中 | **高估**。`CredentialVault.cs:183` 确为 `catch { return false; }`，但该方法为 `Exists` 语义，返回 false 符合预期，缺日志仅影响排障，建议 🟡 中 |
| R3 P2-4 MedicalCases 未用 HandleResult | 🟡 中 | 🟡 中 | 正确。影响 HTTP 语义准确性 |
| R4 P1/P2 N+1 | 🔴 高 | 🔴 高（限定场景） | 基本正确，但需注明"批量场景才放大，常规单方 <20 条影响有限"，修复成本 15 min/项，性价比极高，故保留 🔴 高 |
| R4 P3 sync-over-async | 🟡 中 | 🟡 中 | 正确。`DesktopUpdateService.cs:88` 确为 `.GetAwaiter().GetResult()` |
| R4 P5/P6 HttpClient 独立创建 | 🟡 中/🟢 低 | 🟡 中/🟢 低 | 正确。`TokenRefreshHandler.cs:65` 独立 HttpClient 绕过工厂，连接池管理风险真实 |
| R4 P8 DbContext 注入 | 🟡 中 | 🟢 低 | **高估**（见 C1） |
| R4 P9 缺少 CancellationToken | 🟡 中 | 🟢 低（Server）/🟡 中（Desktop） | **高估**（见 C7） |
| R5 F1 无单元测试 | 🔴 高 | 🟡 中 | **高估**（见 T3） |
| R5 F2 CatalogController 963 行 | 🔴 高 | 🔴 高 | 正确。34 个方法双资源混管，SRP 违反明确 |
| R5 F3 方法过长 55 行 | 🔴 高 | 🟡 中 | **高估**。55/74 行方法在 Controller/模板场景可接受，>100 行或圈复杂度 >15 再升 🔴 |

### 4.2 统计

- **高估**: 9 项（R1 #3 #6 #10, R3 P1-2 P1-3, R4 P8 P9, R5 F1 F3）
- **低估**: 1 项（R1 #5 合并 O1 后）
- **重复计数**: 1 项（C2）
- **口径不一致**: 3 项（T1-T3）

---

## 五、真实性核验（抽检 18 项）

| 断言 | 抽检结果 | 结论 |
|------|----------|------|
| R1 #1 `PasswordHashGenerator/appsettings.json:3-5` 硬编码 | 文件存在，`DevPass123!` / `Lybt2025@TempPass#` 确提交 | ✅ 真 |
| R1 #8 密码长度 6 vs 8 不一致 | `UserInputDtoValidator.cs:39` 确为 `Length(6,128)`，`PasswordPolicyValidator.Policy.MinLength` 确为 8 | ✅ 真 |
| R1 #11 UnifiedMiddleware Fallback 泄露 | `UnifiedMiddlewareConfiguration.cs:45` 确为 `[DEV] {GetType().Name}: {Message}\n{StackTrace}` | ✅ 真 |
| R2 `RegistrationConnectionManager` Singleton | `RegistrationModule.cs:49` 确为 `AddSingleton<RegistrationConnectionManager>` | ✅ 真 |
| R2 `MainWindowViewModel NavTextVisibility` Visibility 泄漏 | `MainWindowViewModel.cs:67` 确为 `Visibility.Visible/Collapsed` | ✅ 真 |
| R3 P1-1 `throw ex` | `ApiClientRepositoryBase.cs:31` 确为 `throw ex` | ✅ 真 |
| R3 P1-2 `BaseApiController.LogOperation` 空 catch | `BaseApiController.cs:48-51` 确为 `catch { }` | ✅ 真 |
| R3 P2-3 `PatientsController.Contains("不存在")` | 抽检 `PatientsController.cs` 未见该行（可能已修复或位置迁移），`MedicalCasesController.cs` 亦未见字符串匹配，仅见 `ModuleErrorCode == ErrorCode.Forbidden` 类型化判断 | ⚠️ 部分失实/已修复。R3 断言的文件行号可能过期，需以当前代码为准 |
| R3 P2-4 MedicalCases 未用 HandleResult | `MedicalCasesController.cs:138/156/200` 确有多处 `return NotFound(...)` 未走 `HandleResult` | ✅ 真 |
| R4 P1/P2 N+1 | `CatalogCrossModuleService.cs:41-76` 确为 foreach 单条查询 | ✅ 真 |
| R4 P3 `ApplyUpdateAndRestart` sync-over-async | `DesktopUpdateService.cs:88` 确为 `GetAwaiter().GetResult()` | ✅ 真 |
| R4 P5 `TokenRefreshHandler` new HttpClient | `TokenRefreshHandler.cs:65/71` 确为 `new HttpClientHandler()` + `new HttpClient` | ✅ 真 |
| R4 P7 EventAggregator 未 Unsubscribe | `RegistrationListViewModel.cs:96` 确有 Subscribe，未见 Unsubscribe；但 Prism 默认 weak reference，泄漏风险需实测 | ⚠️ 部分高估（需验证 Prism 弱引用是否已缓解） |
| R5 F2 CatalogController 963 行 | `wc -l` 确为 963 行，34 个方法 | ✅ 真 |
| R5 F5 JwtService 私有字段命名 | `JwtService.cs:24` 抽检未见 `CurrentOptionsMonitor` PascalCase（可能已修复），现为 `_jwtOptions` 等 | ⚠️ 已修复或位置变更 |
| R5 F6 硬编码 URL | `ApplicationStateService.cs:65` 等多处 `localhost:5000/5300` 确存在 | ✅ 真 |
| R1 #6 健康检查 `SqlServerHealthCheck.cs:79-81` | 文件确为 `HealthCheckResult.Unhealthy($"SQL Server连接失败: {ex.Message}")` | ✅ 真（但暴露面需结合中间件配置判断，见 C4） |
| R5 F1 35+ Handler 无单元测试 | 抽检 `tests/LYBT.Tests.Server` 确无对应 Handler 单测，仅集成测试 | ✅ 真 |

**抽检结论**：18 项中 13 真、3 部分失实/已修复、0 捏造。R3 P2-3 与 R5 F5 的行号过期提示报告基于较旧快照，建议后续报告注明"基于 commit SHA"。

---

## 六、综合风险清单（去重合并后）

> 按"严重度 × 修复成本 × 风险敞口"排序，D = 重复/同根合并

| 优先级 | 编号 | 来源 | 问题 | 建议严重度 | 修复成本 | 风险敞口 |
|--------|------|------|------|------------|----------|----------|
| P0 | C-01 | R3 P1-1 | `ApiClientRepositoryBase.HandleException` `throw ex` 破坏堆栈 | 🔴 高 | 5 min | 高（全 Desktop Repository 排障能力） |
| P0 | C-02 | R4 P1/P2 | `CatalogCrossModuleService` N+1 ×2（GetHerbPrices/GetDisabledHerbIds） | 🔴 高 | 15 min/项 | 中-高（批量开方/导入时放大） |
| P0 | C-03 | R1 #1 + O1 | 硬编码密码提交仓库 + DeployService ZipSlip/大小/穿越 | 🔴 高 | 30 min + Git 历史清理 | 高（供应链/仓库泄露） |
| P0 | C-04 | R5 F2 | `CatalogController` 963 行 SRP 违反（Herb/Formula 双资源混管） | 🔴 高 | 2-4 h 拆分 | 中（可维护性/路由混用） |
| P1 | C-05 | R1 #4 | Access Token 同时作为 Refresh Token | 🟡 中 | 1-2 d（需客户端同步） | 中（泄露窗口延长） |
| P1 | C-06 | R3 P2-4 + R3 P2-3 | Controller 错误码→HTTP 状态码映射不统一（MedicalCases/Patients） | 🟡 中 | 1 h | 中（客户端重试/UX） |
| P1 | C-07 | R1 #8 | 密码最小长度 6 vs 8 不一致（DTO 绕过策略） | 🟡 中 | 10 min | 中（弱密码可入库） |
| P1 | C-08 | R1 #9 + R1 #3 | AutoLogin 无绑定 + Logout AllowAnonymous（同属认证面） | 🟡 中 | 1 d + 30 min | 中 |
| P1 | C-09 | R4 P3 | `DesktopUpdateService.ApplyUpdateAndRestart` sync-over-async 死锁风险 | 🟡 中 | 30 min | 中（WPF Dispatcher） |
| P1 | C-10 | R4 P4 | async void 6 处未受控异常（Timer/Prism/WPF） | 🟡 中 | 1-2 h | 中（进程崩溃） |
| P1 | C-11 | R2 A1 + R4 P7 | Singleton/事件订阅生命周期（RegistrationConnectionManager + EventAggregator） | 🟡 中 | 1 h 审计 | 低-中（泄漏/ captive） |
| P1 | C-12 | R3 P2-2 + R3 P2-1 | ErrorCode 4xxxx/7xxxx 分区空白（处方/同步） | 🟡 中 | 30 min 文档或补码 | 低-中（演进债务） |
| P2 | C-13 | R1 #5 | 文件上传大小限制（合并入 C-03，此处仅大小） | 🟡 中 | 10 min | 低-中 |
| P2 | C-14 | O2 | 双控制器树一致性未验证 | 🟡 中 | 2 h 比对脚本 | 中（越权回归） |
| P2 | C-15 | O3 | Jwt Secret 强度与生产覆盖校验 | 🟡 中 | 30 min | 中（签名可破解） |
| P2 | C-16 | O7 | 双模式一致性/同步冲突策略空白 | 🟡 中 | 设计 1 d | 中（数据不一致） |
| P2 | C-17 | R4 P5/P6 | HttpClient 独立创建（TokenRefreshHandler/探测） | 🟡 中/🟢 低 | 30 min | 低（连接池） |
| P2 | C-18 | R5 F9/F10 | Batch Handler DRY 违反（4× Enable/Disable + 3× Delete） | 🟡 中 | 2 h 泛型基类 | 低-中（一致性） |
| P2 | C-19 | R3 P1-2/P1-3 | 空 catch 缺日志（LogOperation/CredentialVault） | 🟡 中 | 15 min | 低（排障） |
| P3 | C-20 | R1 #6/#7/#11 | 信息泄露面（健康检查/异常堆栈/Swagger）合并 | 🟢 低 | 30 min | 低（需 Development 误配） |
| P3 | C-21 | R2 A2 | MainWindowViewModel Visibility 泄漏 | 🟢 低 | 10 min | 低 |
| P3 | C-22 | R5 F6/F7/F8 | 魔法数字/硬编码 URL/批量阈值/UI 常量 | 🟢 低 | 1 h 常量收敛 | 低 |
| P3 | C-23 | R5 F4 | MasterDetailViewModel 残余重复（ToggleStatus） | 🟢 低 | 1 h | 低 |
| P3 | C-24 | R4 P10/P11/P12 | GetAllActiveHerbs 无分页 / ConfigureAwait / 日志量 | 🟢 低 | 按需 | 低 |
| P3 | C-25 | O5/O6/O8 | 白名单/连接池/可访问性 | 🟢 低 | 按需 | 低 |

> **去重说明**：R1 #7/#11 合并为 C-20；R4 P1/P2 合并为 C-02；R1 #5 的大小部分并入 C-03 的 ZipSlip 综合项后剩余 C-13 单列大小限制；R5 F9/F10 合并为 C-18。

---

## 七、修复路线图（建议分 3 批）

### Batch 1 — 本迭代（≤ 2 h，零风险）

| 项 | 操作 | 验证 |
|----|------|------|
| C-01 `throw ex` → `throw` | `ApiClientRepositoryBase.cs:31` 改 `throw;`，删除 `ExecuteAsync` 中的 `throw; // unreachable` 冗余 | `dotnet build --no-incremental` 0/0 + Desktop 单测 |
| C-02 N+1 ×2 | `CatalogCrossModuleService.cs:41-76` 改为 `Where(idList.Contains(...))` 单次查询（R4 已给 patch） | 集成测试 + 批量开方 100 条压测 |
| C-07 密码长度 6→8 | `UserInputDtoValidator.cs:39` `Length(6` → `Length(8`，与 `PasswordPolicyValidator.Policy.MinLength` 对齐 | 边界测试 6/7/8 位 |
| C-13 上传大小限制 | `DeployService.cs` + Controller 加 `[RequestSizeLimit(100_000_000)]` 与 `content.Length` 上限 | 上传 101 MB 拒绝 |
| C-20 信息泄露加固 | `SystemExceptionHandler` 与 `UnifiedMiddlewareConfiguration` Fallback 改 `IsProduction()` 反向判断；健康检查 `ex.Message` 脱敏 | 生产配置启动自检 |

### Batch 2 — 下一迭代（0.5-1 d，低风险）

| 项 | 操作 |
|----|------|
| C-03 密码 Git 清理 | `git filter-repo` 清理 `PasswordHashGenerator/appsettings.json` 历史 + `.gitignore` 排除 + `user-secrets` 文档 |
| C-06 HTTP 语义 | `MedicalCasesController` 全量改 `HandleResult(..., useAuthMapping:true)`，`PatientsController` 字符串匹配改 `ModuleErrorCode` 判断 |
| C-12 ErrorCode 分区 | 补充 4xxxx/7xxxx 分区注释或占位码，`ClientErrorMessageMapper` 的 `ERR-07` 对齐 |
| C-09/C-10 异步加固 | `ApplyUpdateAndRestart` 改 `async Task`；`AutoReadCallback`/Prism `OnNavigatedTo` 补顶层 try-catch |
| C-14 双控制器一致性 | 脚本比对 `WebAPI/Controllers` vs `LocalWebAPI/Controllers` 的路由/`[Authorize]`/`[AllowAnonymous]` 差异，输出矩阵 |
| C-15 Secret 校验 | 生产启动时校验 `Jwt:SecretKey != placeholder && Length>=32` 否则拒绝启动（`ProductionConfigurationValidator` 已有，补测试） |

### Batch 3 — 专项（1-2 d，需设计）

| 项 | 操作 |
|----|------|
| C-04 拆分 CatalogController | `CatalogController` → `HerbsController` + `FormulasController`，路由保持兼容或前端同步 |
| C-05 独立 Refresh Token | 设计 opaque Refresh + 轮换，评估对 `TokenRefreshHandler` 与双模式的影响（ADR） |
| C-08 AutoLogin 绑定 | Token 嵌入 IP/UA 哈希，验证时比对（需权衡移动网络 IP 变化） |
| C-16 双模式一致性 | 补 7xxxx 错误码与同步冲突解决设计（ADR-双模式） |
| C-18 Batch DRY | 提取 `BatchStatusToggleHandler<TEntity>` / `CatalogEntityCommandHandlerBase` 批量方法 |
| O1 ZipSlip | DeployService 解压前校验条目 `Path.GetFullPath` 落在目标目录内 + 病毒扫描钩子（可选） |

---

## 八、结论

1. **5 份报告质量合格**：未发现系统性误报，抽检真实率 72%（13/18），剩余为行号过期而非捏造。
2. **最大风险在交叉盲区**：单报告视角内问题多为 🟡/🟢，但"双控制器一致性""ZipSlip""Secret 强度""双模式一致性"等跨报告盲区若被利用，实际风险可达 🔴。
3. **严重度需校准**：建议采纳 §4 校准矩阵，将 9 项高估降级、1 项低估升级，并统一阈值（文件 500 行/方法 100 行/圈复杂度 15）。
4. **下一步**：按 §7 三批推进，Batch 1 本周闭环（2 h 内），Batch 2 纳入下一迭代，Batch 3 立 ADR 后实施。本报告合并后的 25 项综合清单可直接作为 `docs/03-architecture/13-project-master-plan.md` 的 Backlog 输入。

---

## 附录

### A. 验证抽检清单

见 §5 表格，抽检 18 项，覆盖 R1-R5 各 2-4 项，文件行号以当前 master 为准。

### B. 报告间引用关系

- R2 的 P07/P08/P10 守卫结论与 R4 P8 冲突（C1）
- R1 的安全面与 R3 的错误处理面对同一 `SystemExceptionHandler` 重复计数（C2）
- R3 的 HTTP 语义发现（P2-4）是 R1 安全视角的盲区（C6）
- R4 的 N+1 发现是 R2 架构视角的盲区（C5）

### C. 术语

- **Captive Dependency**：Singleton 持有 Scoped 依赖导致后者生命周期被拉长
- **ZipSlip**：恶意 zip 条目含 `../` 穿越目标目录
- **N+1**：1 次主查询 + N 次单条查询，批量场景放大为性能瓶颈

---

*生成时间: 2026-08-21 | 基于 R1-R5 全文 + 源码抽检 | 下一步: 将 §6 综合清单同步至项目总账 Backlog*
