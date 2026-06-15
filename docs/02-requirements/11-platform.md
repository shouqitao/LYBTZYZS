# 平台基础设施 (Platform)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建
> 合并原 Shell + Configuration + Error Handling + Logging & Audit + Health & Diagnostics + Card Reader 六个模块

## 合并说明（冗余去除）

原 6 模块合计 37 个 US（Shell 7 + Configuration 4 + Error Handling 8 + Logging 7 + Health 9 + CardReader 2），合并为本文件时去除 2 个与其他 US 完全重叠的冗余项，保留 **35 US**：

| 移除项 | 原因 | 并入 |
|--------|------|------|
| ~~US-SHELL-002 启动闪屏~~ | 闪屏是启动流程的组成部分，与启动单实例同属一个启动管线 | US-SHELL-001 应用启动（单实例） |
| ~~US-SHELL-006 全局异常处理~~ | 与 `AppDomain.UnhandledException` + Dispatcher 级全局异常处理完全重复 | US-ERR-001 全局异常处理（Dispatcher+AppDomain） |

## 模块概述

平台基础设施为所有业务模块提供运行基座：Desktop 壳程序（Prism 9.0 模块化、启动流水线、角色导航、双模式切换）、服务端配置与生产验证、分层异常处理与中文友好消息、Serilog 结构化日志与审计、健康检查与运行时诊断、身份证读卡器硬件集成。

**双模式总则**：`SwitchingApiClient` 将 localhost 请求路由到嵌入式 `LocalWebAPI`，否则走 Refit 远程；`LocalWebAPI` 复用全部服务端模块的 Service 层；`LocalDbBackupService` 仅本地模式运行。

---

## Shell

> 原 7 US（US-SHELL-001~007），合并去除 2 个冗余后保留 **5 US**：US-SHELL-001, 003, 004, 005, 007。

Shell 采用 Prism 9.0 模块化架构，作为 WPF 客户端宿主，负责应用全生命周期：单实例互斥锁（`Global\LYBTZYZS_Shell_Instance`）、启动闪屏、两阶段 Serilog 引导、按角色动态加载模块（`ApplicationBootstrapper.LoadModulesForRoleAsync`）、页面导航与菜单系统。

### US-SHELL-001: 应用启动（单实例）

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 应用单实例启动并看到启动闪屏与进度反馈，**以便** 避免多开导致的数据竞争，并在启动失败时获得明确提示。

**验收标准**:
- [ ] 已有实例运行时拒绝第二次启动（`Mutex` 命名 `Global\LYBTZYZS_Shell_Instance`）
- [ ] 启动时显示 Splash Screen（Logo + 进度条 + 当前步骤名），至少显示 1 秒避免闪烁
- [ ] 启动步骤失败 → 错误对话框 + "重试"/"退出"
- [ ] API 不可达 → 提示并提供"切换到本地模式"按钮
- [ ] 两阶段 Serilog 引导：先 bootstrap logger 捕获早期错误，再切换最终 logger

**业务规则**:
1. 单实例互斥锁 `Global\LYBTZYZS_Shell_Instance`。
2. 启动闪屏与应用启动同属一个启动管线（原 US-SHELL-002 已并入）。
3. 两阶段 Serilog bootstrap 在 WebAPI 与 Desktop 均生效。
4. Debug 模式运行上限 120 分钟。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 启动步骤含 API 连通性检查 |
| 本地 | 跳过 API 步骤，初始化本地数据库 |

**实现参考**: `src/Client/Desktop/Shell/App.xaml.cs:41`、`Services/Bootstrap/ApplicationBootstrapper.cs:35`

---

### US-SHELL-003: 角色基础模块加载

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 登录后系统按我的角色自动加载对应功能模块，**以便** 我直接进入工作台而不需手动配置，且无越权菜单。

**验收标准**:
- [ ] Admin 登录 → 加载管理模块，导航到管理工作台
- [ ] Doctor 登录 → 加载临床模块，导航到临床工作台
- [ ] Receptionist 登录 → 加载患者管理 + 读卡器模块
- [ ] 登出 → 清除会话与导航历史，返回登录页

**业务规则**:
1. `ApplicationBootstrapper.LoadModulesForRoleAsync` 按角色过滤 Prism 模块。
2. 菜单可见性矩阵：系统设置仅 SuperAdmin；药材/用户管理 Admin+；医案/验方 Doctor+；患者管理全部角色。
3. 角色层级：Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（模块加载逻辑与模式无关） |

**实现参考**: `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs:35`

---

### US-SHELL-004: 账户设置（个人资料+密码）

**角色**: 所有用户
**优先级**: Could
**状态**: ✅ 已实现

**作为** 用户，**我想要** 查看和修改我的个人信息与密码，**以便** 保持账户信息准确与安全。

**验收标准**:
- [ ] 点击账户设置 → 显示 `AccountSettingsControl`
- [ ] 修改密码 → 弹出对话框（旧密码 + 新密码 + 确认密码）
- [ ] 保存个人资料 → 调用 API 更新（IDOR 防护：仅本人）

**业务规则**:
1. 个人资料编辑：显示名称/电话/邮箱（关联 [03-users.md](03-users.md) US-USER-008）。
2. 修改密码需旧密码（关联 US-USER-009）。
3. 登录信息（最后登录时间/IP）只读。
4. 入口：`MenuManager.EditProfileCommand`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 修改通过 API 提交 |
| 本地 | 修改提交到本地 Service 层 |

**实现参考**: `AccountSettingsControl`、`MenuManager.EditProfileCommand`

---

### US-SHELL-005: 菜单导航

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 在功能模块间快速切换并能回退到上一页，**以便** 高效地在患者/医案/验方间流转而不丢失上下文。

**验收标准**:
- [ ] `NavigateTo(viewName, params)` → ContentRegion 显示目标视图
- [ ] `NavigateBack()` → 返回上一视图（Alt+左箭头）
- [ ] 导航历史最多 20 条，登出时清空
- [ ] 导航参数正确传递到目标 ViewModel
- [ ] 不同角色登录 → 菜单项按可见性矩阵显示/隐藏

**业务规则**:
1. 基于 Prism Region 导航（`NavigationCoordinator` 封装）。
2. 全局快捷键：Ctrl+N 新建患者、Ctrl+S 保存、F5 刷新、Ctrl+P 打印。
3. 主题切换：浅色/深色一键切换。
4. 前进导航与面包屑已实现。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 全部菜单可用 |
| 本地 | 部分需服务端的菜单禁用 |

**实现参考**: `NavigationCoordinator`、`MenuManager`、Prism Region 定义

---

### US-SHELL-007: 双模式连接切换

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 手动切换远程/本地工作模式，**以便** 根据网络环境选择合适模式，外出看诊离线工作。

**验收标准**:
- [ ] 切换到本地 → Repository 使用 LocalXxxRepository（LocalDB）
- [ ] 切换到远程 → Repository 使用 Refit HTTP API
- [ ] 本地有未完成医案（Active/Suspended）时切换到远程 → 阻断并提示（ERR-70506）
- [ ] 切换失败 → 自动回退到切换前模式
- [ ] 切换成功 → 状态栏显示模式标识

**业务规则**:
1. 切换由 `IConnectionModeProvider.SwitchModeAsync`（5 步）驱动。
2. 本地→远程前置检查：无 Active/Suspended 医案 + 网络连通 + Token 有效（SYNC-D01）。
3. `SwitchingApiClient` 路由 localhost → 嵌入式 `LocalWebAPI`，否则 → 远程。
4. 异常捕获返回 `ModeSwitchResult.Failed`，自动回退。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用（切换操作本身） |
| 本地 | 不适用（切换操作本身） |

**实现参考**: `IConnectionModeProvider.SwitchModeAsync`、`SwitchingApiClient`、`ModeSwitchValidator`

---

## Configuration

> 原 4 US，保留 **4 US**：US-CFG-001~004。

配置模块基于 ASP.NET Core Options 模式，提供强类型绑定 + DataAnnotation 验证 + 分环境覆盖 + 生产启动验证。运行时配置查询通过 `ConfigurationController`（`SuperAdminOnly`）暴露。

### US-CFG-001: 查询所有配置

**角色**: 超级管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 通过 API 查询系统全部配置项，**以便** 在不接触服务器文件的情况下掌握当前运行参数。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 返回当前生效的全部配置（合并 appsettings + 环境变量）
- [ ] 敏感字段（密钥/密码）不明文返回

**业务规则**:
1. 端点受 `SuperAdminOnly` 策略保护。
2. 配置节名称通过 `ConfigurationSections` 常量统一管理。
3. 敏感配置（SecretKey/Password）脱敏展示。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET` 服务端配置 |
| 本地 | 完全一致（LocalWebAPI 复用 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs:15`、`ISystemConfigurationService`

---

### US-CFG-002: 查询单个配置

**角色**: 超级管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 按配置节名查询单个配置详情，**以便** 精确定位需要调整的参数。

**验收标准**:
- [ ] 指定配置节名 → 返回该节的强类型配置对象
- [ ] 节名不存在 → 返回 404
- [ ] 非 SuperAdmin → 403

**业务规则**:
1. 支持 14 个 Options 类（8 服务端 + 1 共享 + 4 客户端 + 1 WebAPI）。
2. 客户端 `ClinicSettings`（Name/Department/Address/Phone）驱动处方打印标题区。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端读取 |
| 本地 | 完全一致 |

**实现参考**: `ConfigurationController.cs:15`、Options 类（`LYBT.Shared.Configuration`）

---

### US-CFG-003: 验证生产配置

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 生产环境启动时自动验证关键配置项，**以便** 配置缺失时立即报错并给出修复指引，而非运行时出现莫名异常。

**验收标准**:
- [ ] Production + 连接字符串缺失 → 启动失败，退出码 1
- [ ] Production + JWT 密钥过短 → 启动失败
- [ ] Development + 连接字符串缺失 → 正常启动（不触发验证）
- [ ] 错误输出含具体配置路径与对应环境变量设置命令

**业务规则**:
1. 仅 `ASPNETCORE_ENVIRONMENT=Production` 时触发 `ProductionConfigurationValidator`。
2. 严重级别：Critical（连接字符串/JWT 密钥）阻止启动；Important（默认密码/SystemAdmin）警告；Optional（AllowedHosts）建议。
3. 验证失败 → 控制台输出 + Fatal 日志 + `Environment.Exit(1)`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端启动时验证 |
| 本地 | 不适用 |

**实现参考**: `ProductionConfigurationValidator`、`Program.cs`

---

### US-CFG-004: 功能开关

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 通过 `FeatureToggles` 控制 Desktop 功能可见性与行为策略，**以便** 分阶段发布功能或调整处理策略。

**验收标准**:
- [ ] `FeatureToggle=false` → 对应功能按钮/菜单项完全隐藏（Collapsed）
- [ ] `OverwriteConflicts=false` → 同步冲突时不自动覆盖
- [ ] 客户端配置变更需重启 Desktop 生效

**业务规则**:
1. v1.0 `FeatureToggleOptions` 仅含 `OverwriteConflicts` 与 `DuplicateHerbMergeStrategy`（"Max"）。
2. 早期定义的 18 个布尔开关已废弃，UI 可见性由各模块 ViewModel 按角色/业务状态自管。
3. FeatureToggle 仅控 UI 层，API 端点不受影响。
4. 仅 Logging 配置支持热更新，其余需重启。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 客户端 appsettings.json 配置 API 连接 |
| 本地 | `ApiClient` 配置无效，使用本地数据源 |

**实现参考**: `FeatureToggleOptions`、`ApiClientOptions`、`ClinicSettingsOptions`

---

## Error Handling

> 原 8 US，保留 **8 US**：US-ERR-001~008（含原 US-SHELL-006 全局异常处理合并到 US-ERR-001）。

异常处理采用分层架构：服务端 `IExceptionHandler` 链式处理器（`BusinessExceptionHandler` → `SystemExceptionHandler`）将异常转为 `ApiResponse` JSON；客户端 `DesktopExceptionHandler` 全局兜底 + `ClientErrorMessageMapper` 映射中文消息。`AppException` 体系含 6 种具体异常，各自映射 HTTP 状态码。

### US-ERR-001: 全局异常处理（Dispatcher+AppDomain）

**角色**: 系统
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统，**我想要** 全局捕获所有未处理异常（AppDomain + Dispatcher + TaskScheduler 级），**以便** 异常不致崩溃应用且始终被记录（原 US-SHELL-006 全局异常处理已并入本 US）。

**验收标准**:
- [ ] Desktop 端 `AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException` 全局注册
- [ ] 服务端 `BusinessExceptionHandler`（优先）→ `SystemExceptionHandler`（兜底）链式处理
- [ ] 业务异常记 Warning；系统异常记 Error
- [ ] 日志含 ExceptionType/ErrorCode/CorrelationId/RequestPath/UserId

**业务规则**:
1. 处理器链顺序：`BusinessExceptionHandler` 仅处理 `AppException` 子类，其余传递给 `SystemExceptionHandler`。
2. `SafeExecuteAsync` 包裹异步操作，异常自动转为 `ServiceResult.Failure`。
3. 级联故障防护：handler 自身异常时由下一 handler 兜底；最大重抛 3 次。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 中间件自动注册，所有 API 端点生效 |
| 本地 | Desktop 全局异常兜底处理本地操作异常 |

**实现参考**: `LYBT.Shared.ExceptionHandling/`（`BusinessExceptionHandler`、`SystemExceptionHandler`）、`DesktopExceptionHandler`

---

### US-ERR-002: 中文友好错误消息

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 操作失败时看到中文友好提示而非英文技术错误，**以便** 理解问题原因并决定是否重试。

**验收标准**:
- [ ] 服务端返回 errorCode → 用户看到对应中文消息（如"密码不正确"）
- [ ] HTTP 401 → "登录已过期，请重新登录"
- [ ] 未知错误码 → "操作失败，请稍后重试"
- [ ] 业务错误不含追踪码，系统错误含追踪码

**业务规则**:
1. `ClientErrorMessageMapper` 覆盖 HTTP 状态码 + 7 模块 90+ 业务错误码。
2. 优先级：业务错误码 > HTTP 状态码 > 通用兜底。
3. 错误码分区：1xxxx 认证、2xxxx 患者、3xxxx 医案、5xxxx 药材、6xxxx 验方、7xxxx 同步。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 解析服务端 ProblemDetails 的 errorCode |
| 本地 | 解析本地操作异常类型 |

**实现参考**: `ClientErrorMessageMapper`、`ExceptionMessageMapper`

---

### US-ERR-003: 追踪 ID（TraceId）

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 系统错误消息附带 8 位短追踪码，**以便** 用户反馈问题时通过追踪码快速定位具体异常。

**验收标准**:
- [ ] 系统错误消息 → 含追踪码（8 位时间戳+随机数）
- [ ] 业务错误消息 → 不含追踪码
- [ ] 日志可通过追踪码检索到对应异常详情

**业务规则**:
1. 追踪码格式：8 位短码（时间戳+随机数）。
2. 仅 Error/Critical 级别附加追踪码。
3. 展示格式："如需帮助，请提供追踪码: XXXXXXXX"。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同步记录到服务端日志（经 CorrelationId 关联） |
| 本地 | 记录到本地日志文件 |

**实现参考**: `DesktopExceptionHandler`（追踪码生成）、Serilog 日志

---

### US-ERR-004: CorrelationId 端到端追踪

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 每个请求/操作携带 CorrelationId 贯穿全链路，**以便** 故障发生时快速追踪一个请求在系统中的完整链路。

**验收标准**:
- [ ] 服务端请求日志的 CorrelationId 与请求头 `X-Correlation-Id` 一致
- [ ] Desktop 端日志含 AsyncLocal 注入的 CorrelationId
- [ ] ProblemDetails 响应含 correlationId 字段

**业务规则**:
1. 服务端：从 `HttpContext.Request.Headers["X-Correlation-Id"]` 获取，回退到 `TraceIdentifier`。
2. Desktop：从 `AsyncLocal<string>` 获取（`AsyncLocalCorrelationIdProvider`）。
3. `CorrelationIdEnricher` 自动富集每条日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 端 AsyncLocal 注入） |

**实现参考**: `AsyncLocalCorrelationIdProvider`、`CorrelationIdEnricher`、CorrelationId 中间件

---

### US-ERR-005: 生产环境堆栈屏蔽

**角色**: 安全管理者
**优先级**: Must
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 生产环境错误响应隐藏堆栈跟踪与异常类型，**以便** 防止内部实现泄露扩大攻击面。

**验收标准**:
- [ ] Production 环境 ProblemDetails 不含 `stackTrace` / `exceptionType`
- [ ] Development 环境额外包含 `stackTrace`
- [ ] ValidationException 额外含 `errors` 字典

**业务规则**:
1. 环境感知：仅 Development 返回堆栈。
2. 标准字段：type/title/status/detail/instance。
3. 扩展字段：errorCode/correlationId/traceId/timestamp。
4. ConflictException 额外含 entityType/entityId。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端按环境决定返回字段 |
| 本地 | Desktop 使用 `ClientProblemDetails` 解析 |

**实现参考**: `BusinessExceptionHandler`、`SystemExceptionHandler`、`ClientProblemDetails`

---

### US-ERR-006: 验证错误统一格式（422）

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 验证错误以统一格式返回（含字段级 errors 字典），**以便** 客户端能在表单内精确高亮错误字段。

**验收标准**:
- [ ] `ValidationException` → 400 + `errors` 字典（字段名 → 错误消息数组）
- [ ] 支持多字段错误收集（链式 `AddError`）
- [ ] 字段级错误同时在表单内显示 + 顶部错误摘要

**业务规则**:
1. `ValidationException`(HTTP 400, Category=Validation) 附带 `Errors` 字典 + `FieldName`。
2. `FluentValidation.ValidationException` 映射为 400 "验证失败"。
3. `ValidationException` 支持链式 `AddError` 收集多字段错误。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端统一格式返回 |
| 本地 | 完全一致（LocalWebAPI 复用 handler） |

**实现参考**: `ValidationException`、`BusinessExceptionHandler`

---

### US-ERR-007: 业务异常分类

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 使用分层的异常类型体系抛出业务异常，**以便** 每种异常自动映射到正确 HTTP 状态码与错误类别。

**验收标准**:
- [ ] `BusinessException` → 400
- [ ] `NotFoundException` → 404（含 ResourceType/ResourceId）
- [ ] `ConflictException` → 409（含 EntityType/EntityId/版本）
- [ ] `UnauthorizedException` → 401（含 FailureReason）
- [ ] 每类提供静态工厂方法（如 `NotFoundException.User(guid)`）

**业务规则**:
1. `AppException` 基类含 ErrorCode/TypedErrorCode/UserMessage/ShowDetailToUser。
2. 6 种具体异常：Business/NotFound/Conflict/Validation/Unauthorized/Api。
3. 每类提供按实体分组的工厂方法（User/Patient/Herb/Formula/MedicalCase/...）。
4. A1 统一异常体系重构：Service 层采用 throw 域异常，消除 47 处 `InvalidOperationException`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端抛出，中间件处理 |
| 本地 | Desktop 直接捕获处理 |

**实现参考**: `AppException` 体系（`LYBT.Shared.ExceptionHandling`）、`ExceptionFactory`

---

### US-ERR-008: 异常层级（Validation/NotFound/Conflict → Business）

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 异常类型遵循清晰的继承层级与严重度分级，**以便** 日志级别与用户通知方式能根据严重度自动适配。

**验收标准**:
- [ ] `ValidationException`/`NotFoundException`/`ConflictException` → Toast 红色（不自动消失）
- [ ] `UnauthorizedException` → 对话框（手动关闭）
- [ ] `HttpRequestException`/`TimeoutException` → Toast 黄色（5 秒，可重试）
- [ ] 未知异常 → 对话框 + 追踪码

**业务规则**:
1. 继承层级：`Exception → AppException → {Business, NotFound, Conflict, Validation, Unauthorized, Api}`。
2. 严重度分级：Information(0)/Warning(1)/Error(2)/Critical(3)。
3. 严重度映射日志级别，并决定通知类型（Toast vs 对话框）。
4. 遵循 UI 通知规范：业务错误用 Toast，系统错误用对话框。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | API 调用异常的 Desktop 展示 |
| 本地 | 本地操作异常的 Desktop 展示 |

**实现参考**: `ErrorSeverity`、`ErrorCategory`、`DesktopExceptionHandler`

---

## Logging & Audit

> 原 7 US，保留 **7 US**：US-LOG-001~007。

日志与审计模块基于 Serilog 提供结构化日志、安全审计、敏感数据脱敏、运行时级别管理与自动清理。服务端输出到 Console + File + SQL Server（SystemLog 表），Desktop 输出到 Console + File（`%LOCALAPPDATA%/LYBTZYZS/logs/`）。

### US-LOG-001: 结构化日志（Serilog）

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 系统生成含 CorrelationId 等上下文属性的结构化日志，**以便** 通过 CorrelationId 快速追踪请求完整链路。

**验收标准**:
- [ ] 每条日志含 CorrelationId 属性
- [ ] 自动注入 MachineName、ThreadId
- [ ] 日志文件按天滚动

**业务规则**:
1. 使用 Serilog 框架。
2. 服务端输出：Console + File（按天滚动）+ SQL Server（SystemLog 表）。
3. Desktop 输出：Console + File（`lybt-desktop-{Date}.log`，保留 30 个，单文件 10MB）。
4. 默认级别 Information，可通过 DiagnosticsController 动态调整。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 写 Console + File + SQL Server |
| 本地 | Desktop 写 Console + File |

**实现参考**: Serilog 配置（`Program.cs`）、`SystemLog` 表

---

### US-LOG-002: 两阶段 Serilog 引导

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 启动阶段用 bootstrap logger 捕获早期错误再切换最终 logger，**以便** 启动失败也有日志可查。

**验收标准**:
- [ ] 启动早期使用 `logs/bootstrap-.log`（按天滚动，保留 7 天）
- [ ] 依赖注入就绪后切换最终 logger
- [ ] WebAPI 与 Desktop 均实现两阶段引导

**业务规则**:
1. 两阶段 bootstrap 在 WebAPI + Desktop 均生效。
2. bootstrap logger 在配置系统就绪前捕获启动错误。
3. 切换后最终 logger 接管全部后续日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 同样两阶段） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Program.cs`、`src/Client/Desktop/Shell/App.xaml.cs:41`

---

### US-LOG-003: 敏感数据脱敏

**角色**: 安全管理者
**优先级**: Should
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 日志中敏感数据（手机号/身份证号/密码/Token）被自动脱敏，**以便** 即使日志文件泄露也不暴露患者隐私。

**验收标准**:
- [ ] `[SensitiveData(ContactInfo)]` 手机号 → `138****1234`
- [ ] `[SensitiveData(IdentityInfo)]` 身份证号 → `110********1234`
- [ ] `MaskingMode.Full` → "[已隐藏]"
- [ ] 文本中 `password=abc123` → `password=[REDACTED]`
- [ ] Bearer Token → `Bearer [REDACTED]`

**业务规则**:
1. 属性级脱敏：`SensitiveDataAttribute` + `SensitiveDataMasker`。
2. 敏感数据类型：PersonalInfo/MedicalInfo/ContactInfo/IdentityInfo/FinancialInfo。
3. 脱敏模式：Default/Partial/Full/Hash。
4. 文本级脱敏：正则检测密码/Token/连接字符串/Bearer Token。
5. URI 脱敏：查询参数 password/token/key/secret → ***。
6. `SensitiveDataDestructuringPolicy` 在 Serilog 析构时触发。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 同策略） |

**实现参考**: `SensitiveDataAttribute`、`SensitiveDataMasker`、`SensitiveDataDestructuringPolicy`

---

### US-LOG-004: 审计日志（可配置保留期）

**角色**: 安全管理者
**优先级**: Should
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 所有认证相关安全事件记录到独立审计表，**以便** 追溯谁在何时执行了什么安全操作，满足医疗行业合规。

**验收标准**:
- [ ] 登录成功 → EventType="Login", Success=true
- [ ] 登录失败 → EventType="LoginFailed", Success=false, ErrorMessage 非空
- [ ] Token 刷新 → EventType="RefreshToken"
- [ ] 记录含 IpAddress 与 UserAgent
- [ ] 审计日志仅追加，不可修改/删除

**业务规则**:
1. 事件类型：Login/Logout/RefreshToken/TokenRevoked/LoginFailed/PasswordChange/UserDisabled 等。
2. `SecurityAuditLog` 独立表，与 SystemLog 分离。
3. 保留期 `SecurityOptions.AuditRetentionDays`，默认 365 天，范围 30-3650。
4. UserId 可选（LoginFailed 可能无已知用户）。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 写入 SQL Server `SecurityAuditLogs` 表 |
| 本地 | 不适用（本地模式无完整认证流程） |

**实现参考**: `SecurityAuditService`、`SecurityAuditLog` 实体、`SecurityOptions.AuditRetentionDays`

---

### US-LOG-005: 日志级别动态调整

**角色**: 运维人员
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 不重启应用动态调整日志级别，**以便** 排查问题时临时开启 Debug，完成后自动恢复。

**验收标准**:
- [ ] `EnableDebugMode` → LevelSwitch 降低到指定级别
- [ ] `DisableDebugMode` → 恢复 DefaultLevel
- [ ] Timer 到期 → 自动恢复
- [ ] 并发调用线程安全

**业务规则**:
1. `LoggingLevelManager` 持有全局 `LoggingLevelSwitch` 单例。
2. 默认 Information；调试模式临时降低 + Timer 自动恢复。
3. 手动 `SetLevel` 无自动过期。
4. 所有操作 `lock` 保护，实现 `IDisposable`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 通过 `DiagnosticsController` API 管理 |
| 本地 | Desktop 直接使用 `LoggingLevelManager` |

**实现参考**: `LoggingLevelManager`（API 详见 [Health & Diagnostics](#health-diagnostics) US-SYS-005~009）

---

### US-LOG-006: CorrelationId 注入

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** CorrelationId 自动注入每条日志与 API 请求日志，**以便** 跨模块跨层关联同一请求的所有日志。

**验收标准**:
- [ ] 每个 API 请求生成 started + completed/failed 日志对
- [ ] completed 日志含准确耗时毫秒数
- [ ] 含 password/token 的参数被脱敏
- [ ] CorrelationId 与中间件注入值一致

**业务规则**:
1. `ApiLoggingFilter`（`IAsyncActionFilter`）全局注册。
2. started：`[API] >>> {Action} started`；completed：`[API] <<< {Action} completed in {Duration}ms`。
3. 参数记录（Debug 级别）：敏感字段自动检测脱敏，复杂对象显示类型名，字符串截断 100 字符。
4. CorrelationId 经 `CorrelationIdEnricher` 自动注入。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 全局启用 |
| 本地 | 不适用（Desktop 无 Controller） |

**实现参考**: `ApiLoggingFilter`、`CorrelationIdEnricher`

---

### US-LOG-007: 日志自动清理（默认 365 天）

**角色**: 运维人员
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 系统自动清理过期日志且保留 Error/Fatal，**以便** 磁盘不被过期日志耗尽，严重错误永久可查。

**验收标准**:
- [ ] 启动延迟 5 分钟后首次执行
- [ ] 90 天前的 Information/Warning 日志被删除
- [ ] 90 天前的 Error/Fatal 日志保留
- [ ] 365 天前的安全审计日志被删除
- [ ] 清理过程数据库仍可正常读写

**业务规则**:
1. 系统日志清理（`LogCleanupService`）：每 24 小时，默认保留 90 天，仅清理 Warning 及以下，Error/Fatal 永久保留。
2. 分批删除每批 1000 条，批间延迟 100ms，避免锁表。
3. 安全审计清理：每日凌晨 3:00，保留 365 天（可配）。
4. 清理失败异常隔离，不影响主流程；可通过 `Lybt:Logging:Cleanup` 禁用。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 自动运行 |
| 本地 | 不适用（Desktop 日志由文件滚动策略管理） |

**实现参考**: `LogCleanupService`、`SecurityOptions.AuditRetentionDays`、`Lybt:Logging:Cleanup` 配置节

---

## Health & Diagnostics

> 原 9 US，保留 **9 US**：US-SYS-001~009。

健康检查与运行时诊断提供系统可观测性：匿名轻量探活（`/health`、`/ping`）、认证详细检查（`/details`，含 DB 状态）、SuperAdmin 专属日志级别管理（`/diagnostics/*`）。调试模式硬上限 120 分钟，到期自动恢复。

### US-SYS-001: 匿名存活探针（/health）

**角色**: 监控系统
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维/监控系统，**我想要** 通过轻量匿名端点检查服务是否存活，**以便** 实时掌握运行状态并在异常时立即告警。

**验收标准**:
- [ ] 匿名请求 → 200 + `{"status":"Healthy","timestamp":"..."}`
- [ ] 服务运行中始终返回 Healthy
- [ ] 不执行任何数据库或外部依赖检查

**业务规则**:
1. 匿名访问，不需要认证。
2. 返回 status + timestamp(UTC)。
3. 此端点不查 DB，保证最轻量。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health` |
| 本地 | 不适用（纯客户端无服务端探针） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs:19`

---

### US-SYS-002: Ping 端点（/ping）

**角色**: 负载均衡器
**优先级**: Could
**状态**: ✅ 已实现

**作为** 负载均衡器，**我想要** 通过最轻量端点探测服务可达，**以便** 快速进行流量分发决策。

**验收标准**:
- [ ] 匿名请求 → 200 + `{"message":"pong","timestamp":"..."}`
- [ ] 不执行任何业务逻辑

**业务规则**:
1. 匿名访问。
2. 返回 message("pong") + timestamp(UTC)。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health/ping` |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`

---

### US-SYS-003: 详细健康检查（/details，含 DB）

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** Admin，**我想要** 查看含数据库状态的详细健康报告，**以便** 在用户报障前主动发现数据库连接或迁移问题。

**验收标准**:
- [ ] 未认证 → 401
- [ ] DB 正常 + 无待执行迁移 → 200 + status="Healthy"
- [ ] DB 正常 + 有待执行迁移 → 503 + status="Degraded"
- [ ] DB 连接失败 → 503 + status="Unhealthy"
- [ ] 返回 database.duration 耗时毫秒数

**业务规则**:
1. 需认证（Bearer Token）。
2. 检查 `CanConnectAsync` + 待执行迁移数（InMemory 跳过迁移检查）。
3. 无待执行迁移 → Healthy；有 → Degraded；连接失败 → Unhealthy。
4. 返回数据库检查耗时。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health/details` |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`、数据库健康检查服务

---

### US-SYS-004: 健康状态 503 返回

**角色**: 监控系统
**优先级**: Should
**状态**: ✅ 已实现

**作为** 监控系统，**我想要** 健康检查在系统 Degraded/Unhealthy 时返回 503，**以便** 上游系统能根据 HTTP 状态码做故障转移决策。

**验收标准**:
- [ ] Healthy → HTTP 200
- [ ] Degraded（有待执行迁移） → HTTP 503
- [ ] Unhealthy（DB 连接失败） → HTTP 503
- [ ] 超时返回 Degraded（而非 Unhealthy）避免不必要的故障转移

**业务规则**:
1. Healthy 返回 200，Degraded/Unhealthy 返回 503。
2. 超时级联防护：超时返回 Degraded，上游可继续路由流量。
3. diagnostics API 对审计日志查询失败返回 Degraded，不抛 500。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `/health/details` 按状态返回 200/503 |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`

---

### US-SYS-005: 日志级别状态查询

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 查询当前日志级别配置与调试模式状态，**以便** 判断是否需要调整。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 默认状态 → `isDebugModeActive=false`、`currentLevel=defaultLevel`
- [ ] 调试模式激活 → 返回 startedAt/expiresAt/remainingMinutes

**业务规则**:
1. 仅 SuperAdmin 可访问。
2. 返回 currentLevel/defaultLevel/isDebugModeActive/debugModeStartedAt/debugModeExpiresAt/remainingMinutes。
3. remainingMinutes 仅调试模式激活时返回。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/diagnostics/logging/status` |
| 本地 | 不适用 |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-006: 启用调试模式（定时，≤120 分钟）

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 临时降低日志级别捕获诊断信息，**以便** 不重启服务排查生产问题，到期自动恢复。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 默认参数 → Debug 级别，30 分钟后自动恢复
- [ ] `durationMinutes=150` → 自动截断为 120
- [ ] 返回 previousLevel/currentLevel/startedAt/expiresAt

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 目标级别 Verbose/Debug/Information（默认 Debug）。
3. 持续时间 1-120 分钟（默认 30），超 120 自动截断。
4. 到期 Timer 自动恢复默认级别。
5. 新调试模式覆盖前一次（停旧 Timer，设新 Timer）；操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/debug/enable` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-007: 禁用调试模式

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 手动禁用调试模式恢复默认级别，**以便** 排查完成后立即恢复正常配置，不必等自动过期。

**验收标准**:
- [ ] 调试模式激活时禁用 → 恢复 defaultLevel
- [ ] 未激活时禁用 → 无副作用，返回当前状态
- [ ] 返回 previousLevel/currentLevel

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 恢复默认级别，停止自动过期 Timer，清除调试状态。
3. 操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/debug/disable` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-008: 设置显式日志级别

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 直接设置指定日志级别，**以便** 按运维需要精确控制日志输出粒度。

**验收标准**:
- [ ] level 为空 → 400 "日志级别不能为空"
- [ ] level 无效 → 400 "无效的日志级别" + validLevels
- [ ] level="Warning" → 200 + previousLevel + currentLevel="Warning"

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 支持级别：Verbose/Debug/Information/Warning/Error/Fatal。
3. 此操作不设自动过期（与调试模式不同）；操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/level` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-009: 调试模式自动过期

**角色**: 系统
**优先级**: Could
**状态**: ✅ 已实现

**作为** 系统，**我想要** 调试模式到期自动恢复默认级别，**以便** 防止生产环境长期 Debug 导致性能下降与磁盘耗尽。

**验收标准**:
- [ ] Timer 到期 → 自动调用 DisableDebugMode
- [ ] 最长 120 分钟，超时自动截断
- [ ] 过期后 isDebugModeActive=false

**业务规则**:
1. 硬上限 120 分钟（SYS-D02）。
2. Timer 机制到期自动恢复 DefaultLevel。
3. 与手动禁用共用恢复路径。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server Timer 自动触发 |
| 本地 | 不适用 |

**实现参考**: `LoggingLevelManager`（Timer + `IDisposable`）

---

## Card Reader

> 原 2 US，保留 **2 US**：US-CARD-001~002。

身份证读卡器模块通过策略模式抽象多厂商读卡器接口（当前：华大 HD100 + Mock），实现一次刷卡自动填充患者信息并按 PRD-15 降级链匹配/创建患者。纯客户端硬件交互，不区分远程/本地模式。

### US-CARD-001: 身份证读卡（初始化+读取+自动读）

**角色**: 前台 / 医生 / 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 通过身份证读卡器一键读取患者身份信息，**以便** 不必手动输入 18 位身份证号与其他身份字段，提升登记效率与准确性。

**验收标准**:
- [ ] 连接成功 → `IsConnected=true`
- [ ] 读卡成功 → 返回姓名/身份证号/性别/出生日期/住址
- [ ] 设备断开 → `ConnectionStateChanged` 事件触发
- [ ] 卡片插入 → `CardDetected` 事件触发
- [ ] 读卡失败 → `CardReadError` 事件，含 ErrorCode 与 ErrorMessage
- [ ] 支持自动轮询读卡（`StartAutoRead(intervalMs)` + 重复去重）

**业务规则**:
1. 策略模式 `ICardReader`，支持多厂商；`ICardReaderFactory.AutoDetectReaderAsync` 自动检测。
2. 读取信息含：姓名/性别/民族/出生日期/身份证号/住址；可选保存证件照片（DPAPI 加密）。
3. 设备参数从 `CardReaderOptions`（appsettings `["CardReader"]`）读取。
4. DEBUG 模式回退 `MockCardReader`；`InitializeAsync` 失败不阻塞应用启动。
5. P/Invoke `AccessViolationException` 捕获转错误码 -100，不传播崩溃。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用（纯客户端硬件交互） |
| 本地 | 不适用（纯客户端硬件交互） |

**实现参考**: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Services/ICardReaderService.cs:10`、`HuaDaHD100CardReader`、`MockCardReader`

---

### US-CARD-002: 患者去重查找或创建（PRD-15）

**角色**: 前台 / 医生
**优先级**: Should
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 读卡后系统自动匹配已有患者或快速创建新患者（PRD-15 降级链），**以便** 一次刷卡完成录入与匹配，消除重复建档。

**验收标准**:
- [ ] IdNumber 精确匹配 → `ExactMatch`，加载已有患者 + 就诊历史
- [ ] Name+BirthDate 模糊匹配 → `FuzzyMatch`
- [ ] 多条命中 → `MultipleCandidates`，UI 显示候选列表
- [ ] 未命中 → `NoMatch`，`QuickCreatePatient` 创建新患者
- [ ] 新创建 → `IsNewlyCreated=true`；已有 → `false`

**业务规则**:
1. `MatchPatientAsync` 实现完整降级链：ExactMatch → FuzzyMatch → MultipleCandidates → NoMatch。
2. `PatientMatchType` 枚举驱动 UI 分支。
3. 读卡数据自动映射：姓名→Name、身份证号→IdNumber、出生日期→BirthDate、性别→Gender。
4. 照片通过 `DpapiPhotoStorageService`（DPAPI LocalMachine 加密）存储于 `{AppDataLocal}/LYBT/photos/`。
5. 在患者列表页通过 `ReadCardCommand` 触发。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 读卡后通过 API 查询/创建患者 |
| 本地 | 读卡后通过本地数据源查询/创建患者 |

**实现参考**: `IPatientCardReaderIntegration`、`MatchPatientAsync`、`PatientMatchType`、`DpapiPhotoStorageService`

---

## 依赖

| 依赖 | 说明 |
|------|------|
| [02-auth.md](02-auth.md) | Shell 登录协调、审计日志事件来源 |
| [03-users.md](03-users.md) | 账户设置关联修改密码/个人资料 |
| [07-medical-cases.md](07-medical-cases.md) | MedicalCaseAuditLog 归属医案模块 |
| [09-printing.md](09-printing.md) | ClinicSettings 驱动打印标题区 |
