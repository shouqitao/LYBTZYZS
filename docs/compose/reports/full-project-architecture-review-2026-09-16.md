# 全项目架构与设计模式深度审查报告

> 日期：2026-09-16 ｜ 范围：Server + Desktop + Shared + Tests
> 审查方式：4 个并行只读侦察代理分区扫描 + 主代理汇总
> 基线 Commit：`e42c5e477`
> 关联：`docs/03-architecture/13-project-master-plan.md`（§九 决策）、`13c-current-status.md`（§三 状态）

---

## 一、总体评估

| 分区 | 健康度 | 关键结论 |
|------|--------|----------|
| **Server 模块架构** | B+ 良好 | P07/P08 边界干净、MedicalCase 聚合根正确、ADR-0030 已落地；最大缺口是 MedicalCases 未 CQRS 化 |
| **Desktop 架构** | B 中上 | 分层清晰、无 ServiceLocator、EventAggregator 统一；最大债是 ClinicalModule 强制拉起 4 个模块 + ModuleLazyLoader 映射错误 |
| **Shared 契约层** | B 中上 | 五项目职责清晰、DTO 纪律好；主要问题是 ErrorCode→HTTP 映射遗漏 + Entities 反向依赖 ExceptionHandling |
| **横切关注点** | B 中上 | 双 JWT 隔离、FallbackPolicy fail-closed、两阶段 Serilog 已落地；DoctorOnly 策略与文档硬冲突、Server 缓存空转、SensitiveDataLoggerProvider 有缺陷 |
| **测试架构** | C+ 待改善 | 架构测试~99 条质量高；但 `LYBT.Tests.E2E` 是纯表演性测试（8 用例零真实链路）、双端策略一致性覆盖不足 |

**综合判断**：架构骨架健康，模块化纪律（P07/P08/P10 + 架构测试门禁）执行到位。问题集中在三类：(1) **策略/文档/代码三方不一致**（DoctorOnly、ErrorCode 映射、测试文档）；(2) **基础设施空转**（Server 缓存有失效无生产、领域事件 ADR-0018 零落地）；(3) **生命周期/一致性缺口**（MedicalCaseEditContext Singleton、Local 无禁用拦截、Token 旋转无事务）。

---

## 二、P1 严重问题汇总（9 项）

> 按影响范围排序，每项含位置与建议。

### P1-1 `DoctorOnly` 策略与 SSOT 文档硬冲突

- **现象**：双端代码均 `RequireRole(Doctor)`，不含 SuperAdmin/Admin；`09-security-architecture.md:121,147` 写的是 `RequireRole("SuperAdmin", "Admin", "Doctor")`
- **影响**：Admin/SuperAdmin 无法创建医案、接诊、打印处方
- **位置**：`AuthenticationServiceCollectionExtensions.cs:146-148`、`LocalJwtConfig.cs:91-93`
- **建议**：对照 `01-product/04-permissions.md` 决策——若「仅 Doctor」正确则改文档；若文档正确则双端补角色。决策后补权限测试

### P1-2 `LYBT.Tests.E2E` 为表演性测试

- **现象**：8 个「核心业务场景」全部是 `Task.Delay` + 恒真断言，零真实链路
- **位置**：`tests/LYBT.Tests.E2E/Scenarios/CoreScenariosTests.cs`
- **建议**：直接删除项目（Desktop E2E 基建已覆盖同等场景），或用 `E2ETestBase` 重写

### P1-3 Server 端缓存有失效无生产

- **现象**：`AddOutputCache()` + `UseResponseCaching()` + `CacheInvalidationService` 均已注册，但全仓 0 处 `[OutputCache]`/`[ResponseCache]` 特性
- **位置**：`DatabaseServiceCollectionExtensions.cs:67`、`UnifiedMiddlewareConfiguration.cs:167`
- **建议**：二选一——(a) 对报表/目录类只读端点标 `[OutputCache]` + Tag；(b) 移除空转基建

### P1-4 Local 模式无禁用用户令牌拦截

- **现象**：Remote 有 `OnTokenValidated` 查库拦截禁用用户；Local 无对应 Events
- **影响**：禁用用户在 Local 模式令牌有效期内（最长 1 年）仍可用
- **位置**：`LocalJwtConfig.ConfigureServices` vs `AuthenticationServiceCollectionExtensions.cs:93-113`
- **建议**：对齐 Remote，补 `OnTokenValidated` + 双端测试

### P1-5 MedicalCases 模块未采用 MediatR CQRS

- **现象**：Controller 直接注入 Service 调用，无 CommandHandler/QueryHandler；与 ADR-0017 目标形态不一致
- **位置**：`MedicalCasesController.cs:34-38`、`MedicalCaseModule.cs`
- **建议**：新建 `Application/Commands/` + `Application/Queries/`，逐步迁移，保持 Service 作为 Handler 内部实现

### P1-6 ErrorCode→HTTP 状态码映射系统性遗漏

- **现象**：11 个业务错误码无映射分支，落入默认 500
- **遗漏码**：`FormulaHerbItemAlreadyValidated`(60203)、`FormulaSystemHerbNotFound`(60204)、`FormulaPendingValidationListFailed`(60205)、`FormulaBatchItemNotFound`(60303)、`FormulaBatchItemError`(60304)、`HerbBatchItemNotFound`(50204)、`HerbBatchItemDeletedOrMissing`(50205)、`HerbBatchItemError`(50206)、`PatientNotDeleted`(20702)、`HerbNotDeleted`(50104)、`FormulaNotDeleted`(60107)
- **位置**：`ErrorCodeExtensions.cs:12-137`
- **建议**：补映射分支——业务规则违反→422、"未删除无需恢复"→409/400、批量项不存在→404

### P1-7 Entities 层反向依赖 ExceptionHandling

- **现象**：`RegistrationModel.Complete()` 直接 `throw new BusinessException`，领域实体引用基础设施异常
- **位置**：`RegistrationModel.cs:4,101`
- **建议**：改由 Service 层在调用前校验状态（与 MedicalCase.Complete() 对齐）

### P1-8 ClinicalModule 强制拉起 4 个 OnDemand 模块 + ModuleLazyLoader 映射错误

- **现象**：`ClinicalModule` 声明 4 个 `ModuleDependency`（Patients/MedicalCase/Registration/CardReader），而 ClinicalModule 是 WhenAvailable → 所有角色启动均加载这 4 个模块；`ModuleLazyLoader.ViewToModuleMap` 将多个视图映射到错误模块
- **位置**：`ClinicalModule.cs:11-14`、`ModuleLazyLoader.cs:19-38`
- **建议**：将 ClinicalModule 改为 OnDemand，同时修复 ViewToModuleMap 使每个视图映射到实际注册它的模块

### P1-9 双端策略一致性架构测试覆盖不足

- **现象**：`Should_Have_Same_Auth_Policy_As_Remote` 仅校验 Registrations 3 方法；MedicalCases/Herbs/Formulas/Patients/Users 无自动守卫
- **位置**：`LocalWebApiPatternTests.cs`
- **建议**：反射枚举 Remote/Local 同名控制器全部 `[Authorize]`，逐方法比对 Policy，纳入 Architecture 测试

### P1-10 `SensitiveDataLoggerProvider` 实现缺陷

- **现象**：结构化参数循环是死代码；全部日志重写进静态 `Serilog.Log.Write` 导致双重输出；`BeginScope` 返回 null
- **位置**：`SensitiveDataLoggerProvider.cs:26,39-47,62,68`
- **建议**：删除死代码循环；改为委托下一个 provider + 仅对含敏感内容的消息走脱敏重写；`BeginScope` 委托真实 scope

---

## 三、P2 中等问题汇总（按分区）

### Server（7 项）

| # | 问题 | 位置 |
|---|------|------|
| S-1 | 领域事件（ADR-0018）完全未实现，跨模块通知走同步接口直调 | 全代码库 |
| S-2 | 跨模块服务暴露写方法（`CreateMedicalCaseForRegistrationAsync`） | `IMedicalCaseCrossModuleService.cs:25` |
| S-3 | CatalogDbContext Herb 配置双轨维护（内联 50 行 vs Infrastructure 配置类） | `CatalogDbContext.cs:50-78` vs `HerbConfiguration.cs` |
| S-4 | IdentityDbContext ApplicationUser 配置双轨维护 | `IdentityDbContext.cs:62-82` vs `UserConfiguration.cs` |
| S-5 | 模块级 DbContext 无审计字段自动化（AppDbContext 有 SetAuditFields，模块级无） | 各模块 DbContext |
| S-6 | 模块内部结构不一致（Application/ 层有无不一） | 各模块根目录 |
| S-7 | Reports 模块直接注入 AppDbContext（P10 豁免无独立 ADR） | `ReportRepository.cs:14-18` |

### Desktop（6 项）

| # | 问题 | 位置 |
|---|------|------|
| D-1 | MedicalCaseEditContext Singleton 生命周期错配（跨会话状态残留） | `MedicalCaseModule.cs:41` |
| D-2 | AppStartupOrchestrator 使用 IContainerProvider 服务定位器 | `AppStartupOrchestrator.cs:16,65-69` |
| D-3 | Service 层直接调用 MessageBox.Show | `AppStartupOrchestrator.cs:52`、`DesktopUpdateStartupStep.cs:51,63,68` |
| D-4 | 内容页无角色守卫（NavigationCoordinator 不校验角色） | `NavigationCoordinator.NavigateTo` |
| D-5 | SuperAdminRoleDefinition 不含 AdminModule | `SuperAdminRoleDefinition.cs:15-23` |
| D-6 | 模块内 Singleton 状态在角色切换时不重置 | `LoginCoordinator.LogoutAsync` |

### Shared（13 项）

| # | 问题 | 位置 |
|---|------|------|
| H-1 | 子实体基类继承两套模式并存 | `FormulaHerbItem.cs` / `PrescriptionItem.cs` / `AuthSessionModel.cs` vs `MedicalCasePrintLog.cs` |
| H-2 | MedicalCaseDetailDto 重复时区转换逻辑（硬编码 "Asia/Shanghai"） | `MedicalCaseDetailDto.cs:99-111` vs `MedicalCaseTime.cs` |
| H-3 | AppException 双错误码 + string 可变属性 | `AppException.cs:15-20` |
| H-4 | ValidationException 硬编码 422 绕过 TypedErrorCode 映射 | `ValidationException.cs:11` |
| H-5 | Result\<T\> 三重兼容别名（Data/Value、ErrorMessage/Error/Message） | `Result.cs:19-28` |
| H-6 | ValidationConstants.UserNameMaxLength=50 与实际校验 32 不一致 | `ValidationConstants.cs:56` vs `UserInputDtoValidator.cs:23` |
| H-7 | DefaultPasswordOptions 明文密码无占位符校验 | `DefaultPasswordOptions.cs:15-31` |
| H-8 | SensitiveDataLoggerProvider.BeginScope 返回 null | `SensitiveDataLoggerProvider.cs:26` |
| H-9 | SensitiveDataLoggerProvider 双条日志 | `SensitiveDataLoggerProvider.cs:56-57` |
| H-10 | ApplicationUser 手抄审计字段无编译期保障 | `ApplicationUser.cs:61-88` |
| H-11 | MedicalCaseTime 静态初始化不可运行时变更 | `MedicalCaseTime.cs:10` |
| H-12 | Shared/README.md 列出已合并的废弃项目 | `README.md:18-24` |
| H-13 | Shared/AGENTS.md 未列 LYBT.Entities | `AGENTS.md:11-16` |

### 横切关注点（9 项）

| # | 问题 | 位置 |
|---|------|------|
| X-1 | `04-testing.md` 与实际测试基建脱节（Respawn 已删、3→4 项目） | `docs/05-development/04-testing.md` |
| X-2 | Local JWT 校验强度弱于 Remote（ValidateIssuer/Audience=false） | `LocalJwtConfig.cs:53-54` |
| X-3 | 异常双轨未收口（ApiResponse vs ProblemDetails，ADR-0020 仍「提议」） | 全局 |
| X-4 | UseExceptionHandler 手动遍历 Handler 链 | `UnifiedMiddlewareConfiguration.cs:38-47` |
| X-5 | Token 旋转无事务（旧 Logout 与新 Add 之间崩溃丢会话） | `RefreshTokenCommandHandler.cs:96-111` |
| X-6 | 安全基线表与正文矛盾（DPAPI vs 进程内存） | `09-security-architecture.md:47,239` |
| X-7 | ArchTests P01b 排除清单膨胀至 12 控制器 | `ArchTests.cs:41-56` |
| X-8 | LoginCommandHandler 配置非热更新（IOptions 快照） | `LoginCommandHandler.cs:39-41` |
| X-9 | Desktop 全量测试超时（.runsettings 20 分钟不够） | `.runsettings` |

---

## 四、P3 轻微问题索引

| 分区 | 数量 | 代表性问题 |
|------|:----:|-----------|
| Server | 5 | Registration 目录名与程序集名不一致、跨模块接口定义在 Infrastructure 而非消费方、ADR-0017 模块模板 Domain/ 目录不存在 |
| Desktop | 8 | 三轨 ViewModel 映射并存、导航参数反射转字典、Options 启动冻结热重载失效、侧边栏不持久化、LocalDB 连接串硬编码 |
| Shared | 13 | PrescriptionItem.Amount 的 [Column] 误导、HerbModel XML 注释用 HTML 实体、ErrorCode 旧编号跳号、PagedResult 混入 ErrorMessage |
| 横切 | 5 | 外部异常字符串全名匹配、DesktopExceptionHandler fire-and-forget、Sysadmin Disabled 分支不写审计 |

---

## 五、架构纪律良好实践（值得保持）

| 实践 | 状态 |
|------|------|
| P07 模块间零引用（编译时约束 + 架构测试） | ✅ 6 模块 csproj 干净 |
| P10 Service 禁注入 AppDbContext | ✅ 架构测试强制 |
| Desktop 无 ServiceLocator/ContainerLocator | ✅ 全仓 0 处 |
| Desktop EventAggregator 统一（无 Messenger） | ✅ 全仓 0 处 WeakReferenceMessenger |
| MedicalCase 聚合根设计（含充血域方法） | ✅ 架构测试 AR-001 验证 |
| ADR-0030 跨聚合写入策略（两步写+幂等+补偿） | ✅ StartVisitCommandHandler 正确落地 |
| 双 JWT 隔离（ADR-0024）+ LocalJwtOptionsValidator | ✅ 生产环境强校验 |
| FallbackPolicy fail-closed | ✅ Remote 端默认拒绝 |
| 两阶段 Serilog 引导 | ✅ Bootstrap → Final |
| Desktop 传输层缓存读写一体（ADR-0029） | ✅ CachingHttpMessageHandler |
| 架构测试门禁（~99 条） | ✅ 质量高、覆盖广 |
| 4 项目测试层次（Server/Desktop/Architecture/E2E） | ⚠️ E2E 需重写或删除 |

---

## 六、改进建议路线图

### 短期（1-2 周）——P1 修复

| # | 行动 | 工作量 | 关联 |
|---|------|:------:|------|
| 1 | 决策 DoctorOnly 语义并同步代码/文档 | S | P1-1 |
| 2 | 删除或重写 LYBT.Tests.E2E | S | P1-2 |
| 3 | 补全 ErrorCode→HTTP 映射（11 个码） | S | P1-6 |
| 4 | 修复 SensitiveDataLoggerProvider | S | P1-10 |
| 5 | 补 Local OnTokenValidated 禁用拦截 | M | P1-4 |
| 6 | 扩展双端策略一致性架构测试至全树 | M | P1-9 |

### 中期（1-2 月）——P2 + 架构一致性

| # | 行动 | 工作量 | 关联 |
|---|------|:------:|------|
| 7 | MedicalCases 模块 CQRS 化 | L | P1-5 |
| 8 | 修复 ModuleLazyLoader 映射 + ClinicalModule 改 OnDemand | M | P1-8 |
| 9 | 解除 Entities→ExceptionHandling 依赖 | S | P1-7 |
| 10 | 消除 EF Core 配置双轨维护（Herb + ApplicationUser） | S | S-3,S-4 |
| 11 | MedicalCaseEditContext 改 Transient/Scoped | S | D-1 |
| 12 | Server 端点加 OutputCache 或删除空转基建 | M | P1-3 |
| 13 | 实现领域事件基础框架（ADR-0018） | L | S-1 |
| 14 | 统一模块内部结构（Application/ 层） | M | S-6 |
| 15 | 同步测试文档与基建现实 | S | X-1 |
| 16 | Token 旋转加事务 | S | X-5 |

### 长期（v2.0）——架构演进

| # | 行动 | 关联 |
|---|------|------|
| 17 | 跨模块接口移至消费方（依赖倒置） | Server P3 |
| 18 | 补充 Outbox 模式（领域事件可靠投递） | S-1 |
| 19 | 收口异常契约（ADR-0020 从「提议」到「已接受」） | X-3 |
| 20 | 治理 ArchTests 白名单（到期复审机制） | X-7 |

---

## 七、与既有审查的关系

| 既有审查 | 本次覆盖关系 |
|----------|-------------|
| `frontend-architecture-audit-2026-09-16.md` | L1-L6（HTTP→ViewModel）细节不重复；本次 Desktop 侧重更高层（模块组合/DI/Roles/启动） |
| `architecture-optimization-2026-09-16.md` | 已实施的 6 项优化（CancellationToken/错误层/事务/缓存/HttpClient/主题）不重复审查 |
| `vm-layer-postrefactor-audit-2026-09-14.md` | ViewModel 层重构后状态不重复 |

---

*报告版本: v1.0 | 生成日期: 2026-09-16 | 审查基线: e42c5e477*
