# Shell层基础功能深度检查计划

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/2026-07-14-shell-deep-inspection-report.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 对Shell层进行全面深度检查，验证2026-06-28审计发现的问题是否已修复，识别新问题，评估当前代码质量

**Architecture:** 系统性检查Shell层的核心功能模块：启动管线、模块加载、导航系统、会话管理、健康检查、主题服务、对话框系统、DI注册、代码质量和测试覆盖

**Tech Stack:** .NET 8, WPF/Prism, CommunityToolkit.Mvvm, MaterialDesignInXAML, Serilog

## Global Constraints

- 检查过程中不做任何代码修改，仅记录发现
- 所有发现必须有具体的文件路径和行号
- 发现按严重度分类：Critical/Major/Minor/Info
- 每个发现必须有修复建议

---

## Task 1: 启动管线检查

**Covers:** 启动管线双轨初始化问题

**Files:**
- Read: `src/Client/Desktop/Shell/App.xaml.cs`
- Read: `src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs`
- Read: `src/Client/Desktop/Shell/Services/Startup/StartupPipeline.cs`
- Read: `src/Client/Desktop/Shell/Services/Startup/Steps/*.cs`

**Interfaces:**
- Consumes: IStartupPipeline, IStartupStep
- Produces: 启动管线检查报告

- [ ] **Step 1: 检查App.xaml.cs启动流程**
  
  检查点：
  - OnStartup是否正确调用base.OnStartup
  - OnInitialized是否正确触发AppStartupOrchestrator
  - 是否存在重复初始化

- [ ] **Step 2: 检查AppStartupOrchestrator注册步骤**
  
  检查点：
  - RegisterSteps是否注册了所有必要的启动步骤
  - 步骤顺序是否正确
  - 是否有重复注册的步骤

- [ ] **Step 3: 检查StartupPipeline实现**
  
  检查点：
  - ExecuteAsync是否按顺序执行步骤
  - 是否有并发执行的风险
  - 错误处理是否完善

- [ ] **Step 4: 检查所有StartupStep实现**
  
  检查点：
  - ErrorHandlingStartupStep
  - ModuleCoordinatorStartupStep
  - CoreServicesStartupStep
  - LocalWebApiStartupStep
  - ApiHealthCheckStartupStep
  - WarmupStartupStep
  
  每个步骤：
  - ExecuteAsync逻辑是否正确
  - 是否有副作用
  - 错误处理是否完善

- [ ] **Step 5: 验证启动管线无重复执行**
  
  对比2026-06-28审计发现：
  - "ApplicationInitializationService.InitializeCoreServicesAsync内部跑ErrorHandling→Warmup→ModuleCoordinator"
  - "管线又有独立step各跑一遍"
  
  检查当前代码是否仍有此问题

- [ ] **Step 6: 记录发现**

  格式：
  ```
  [严重度] 文件:行号 - 问题描述
  修复建议: xxx
  ```

---

## Task 2: 模块加载检查

**Covers:** 模块加载双轨+登录路径硬编码问题

**Files:**
- Read: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`
- Read: `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs`
- Read: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/RoleRegistry.cs`
- Read: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Roles/IRoleRegistry.cs`

**Interfaces:**
- Consumes: IRoleRegistry, IModuleManager, IApplicationBootstrapper
- Produces: 模块加载检查报告

- [ ] **Step 1: 检查LoginCoordinator.LoadModulesForUserAsync**
  
  检查点：
  - 是否仍有硬编码的模块列表
  - 是否绕过RoleRegistry
  - Doctor/Receptionist/Clinical/Sysadmin角色是否能正确加载模块

- [ ] **Step 2: 检查ApplicationBootstrapper.LoadModulesForRoleAsync**
  
  检查点：
  - 是否使用RoleRegistry获取模块列表
  - 模块加载是否有性能监控
  - 错误处理是否完善

- [ ] **Step 3: 检查RoleRegistry实现**
  
  检查点：
  - GetModulesForRole是否返回正确的模块列表
  - 各角色定义是否完整
  - 是否有遗漏的角色

- [ ] **Step 4: 对比两个模块加载路径**
  
  检查点：
  - LoginCoordinator路径 vs ApplicationBootstrapper路径
  - 哪个是主路径
  - 是否有冲突

- [ ] **Step 5: 记录发现**

---

## Task 3: 导航系统检查

**Covers:** 导航系统功能完整性

**Files:**
- Read: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`
- Read: `src/Client/Desktop/Shell/Services/NavigationManager.cs`
- Read: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (导航相关部分)

**Interfaces:**
- Consumes: INavigationCoordinator, IRegionManager, ISessionManager
- Produces: 导航系统检查报告

- [ ] **Step 1: 检查NavigationCoordinator**
  
  检查点：
  - NavigateTo是否正确工作
  - 后退/前进功能是否正常
  - 面包屑导航是否正确
  - Region订阅是否正确

- [ ] **Step 2: 检查NavigationManager**
  
  检查点：
  - BuildNavigationItems是否根据角色正确构建导航项
  - 导航项分组是否正确
  - 选中项变更是否触发导航

- [ ] **Step 3: 检查ViewNames常量**
  
  检查点：
  - 所有导航用的ViewName是否都有定义
  - 是否有硬编码的视图名

- [ ] **Step 4: 检查Region管理**
  
  检查点：
  - Region名称是否统一管理
  - 是否有重复定义
  - Region清理是否正确

- [ ] **Step 5: 记录发现**

---

## Task 4: 会话管理检查

**Covers:** 会话生命周期管理

**Files:**
- Read: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`
- Read: `src/Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs`
- Read: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISessionManager.cs`

**Interfaces:**
- Consumes: ISessionManager, IAuthenticationService
- Produces: 会话管理检查报告

- [ ] **Step 1: 检查SessionManager实现**
  
  检查点：
  - CurrentUser属性是否线程安全
  - SetSession/ClearSession是否正确触发事件
  - HasPermission/HasRole是否正确实现

- [ ] **Step 2: 检查SessionLifecycleManager**
  
  检查点：
  - 会话生命周期是否正确管理
  - 会话过期处理是否正确
  - 并发会话处理是否正确

- [ ] **Step 3: 检查会话事件**
  
  检查点：
  - SessionExpired事件是否正确触发
  - SessionChanged事件是否正确触发
  - 事件订阅是否正确清理

- [ ] **Step 4: 记录发现**

---

## Task 5: 健康检查系统检查

**Covers:** 健康检查系统统一性

**Files:**
- Read: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`
- Read: `src/Client/Desktop/Foundation/HealthCheck/ApiHealthMonitor.cs`
- Read: `src/Client/Desktop/Shell/Services/StatusBarManager.cs`
- Read: `src/Client/Desktop/Shell/Services/Startup/Steps/ApiHealthCheckStartupStep.cs`

**Interfaces:**
- Consumes: IHealthCheckCoordinator, IApiHealthMonitor, IApplicationStateService
- Produces: 健康检查系统检查报告

- [ ] **Step 1: 检查HealthCheckCoordinator**
  
  检查点：
  - 健康检查逻辑是否正确
  - 是否与其他健康检查系统冲突
  - 定时器管理是否正确

- [ ] **Step 2: 检查ApiHealthMonitor**
  
  检查点：
  - 断路器逻辑是否正确
  - 状态变更事件是否正确触发
  - 是否与HealthCheckCoordinator冲突

- [ ] **Step 3: 检查ApiHealthCheckStartupStep**
  
  检查点：
  - 启动时健康检查是否正确执行
  - 超时处理是否正确
  - 是否与其他健康检查系统冲突

- [ ] **Step 4: 检查StatusBarManager**
  
  检查点：
  - 是否正确订阅健康状态变更
  - UI更新是否在UI线程
  - 资源清理是否正确

- [ ] **Step 5: 验证是否仍存在三套健康检查并存问题**
  
  对比2026-06-28审计发现：
  - "HealthCheckCoordinator(Tick, 写IApplicationStateService属性)"
  - "ApiHealthMonitor(Timer+断路器, StatusChanged事件)"
  - "ApiHealthCheckStartupStep(Task.Run一次性, 写IApplicationStateService)"
  
  检查当前代码是否仍有此问题

- [ ] **Step 6: 记录发现**

---

## Task 6: 主题服务检查

**Covers:** 主题切换功能

**Files:**
- Read: `src/Client/Desktop/Shell/Services/ThemeService.cs`
- Read: `src/Client/Desktop/Shell/Services/IThemeService.cs`
- Read: `src/Client/Desktop/Shell/Services/MenuManager.cs` (主题切换部分)

**Interfaces:**
- Consumes: IThemeService
- Produces: 主题服务检查报告

- [ ] **Step 1: 检查ThemeService实现**
  
  检查点：
  - ToggleTheme是否正确切换主题
  - ApplyTheme是否正确应用主题
  - IsDarkMode状态是否正确同步

- [ ] **Step 2: 检查主题持久化**
  
  检查点：
  - 主题选择是否持久化
  - 应用重启后是否保持主题

- [ ] **Step 3: 检查MenuManager中的主题切换**
  
  检查点：
  - 是否与ThemeService冲突
  - 硬编码颜色是否存在

- [ ] **Step 4: 记录发现**

---

## Task 7: 对话框系统检查

**Covers:** 对话框迁移完成度

**Files:**
- Read: `src/Client/Desktop/Shell/App.xaml.cs` (对话框注册部分)
- Read: `src/Client/Desktop/Infrastructure/Views/*.xaml`
- Read: `src/Client/Desktop/Infrastructure/ViewModels/*.cs`

**Interfaces:**
- Consumes: IDialogService, ICommonDialogService
- Produces: 对话框系统检查报告

- [ ] **Step 1: 检查对话框注册**
  
  检查点：
  - 是否仍使用Prism IDialogService
  - 是否已迁移到MDIX DialogHost
  - 注册是否正确

- [ ] **Step 2: 检查对话框实现**
  
  检查点：
  - ConfirmationDialog
  - MessageDialog
  - InputDialog
  - UnfinishedCaseDialog
  
  每个对话框：
  - 是否使用MDIX样式
  - 是否有硬编码颜色
  - 是否正确关闭

- [ ] **Step 3: 检查ContainerLocator使用**
  
  对比2026-06-28审计发现：
  - "DialogHostService/AppStartupOrchestrator/MainWindow.xaml.cs三处ContainerLocator"
  
  检查当前代码是否仍有此问题

- [ ] **Step 4: 记录发现**

---

## Task 8: DI注册检查

**Covers:** 依赖注入正确性

**Files:**
- Read: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- Read: `src/Client/Desktop/Shell/Extensions/LoggingRegistrationExtensions.cs`
- Read: `src/Client/Desktop/Shell/App.xaml.cs` (RegisterTypes部分)

**Interfaces:**
- Consumes: IContainerRegistry
- Produces: DI注册检查报告

- [ ] **Step 1: 检查RegisterAllServices**
  
  检查点：
  - 是否注册了所有必要的服务
  - 生命周期是否正确（Singleton/Transient/Scoped）
  - 是否有重复注册

- [ ] **Step 2: 检查日志注册**
  
  检查点：
  - RegisterLogging是否注册了所有Logger
  - 是否有遗漏
  - LoggerFactory是否正确注册

- [ ] **Step 3: 检查Foundation层服务注册**
  
  检查点：
  - AuthenticationService
  - TokenStorageService
  - TokenManager
  - 其他Foundation服务
  
  每个服务：
  - 生命周期是否正确
  - 是否有重复注册

- [ ] **Step 4: 检查ContainerLocator使用**
  
  搜索整个Shell层：
  - 是否仍有ContainerLocator.Container.Resolve
  - 是否有命名Resolve

- [ ] **Step 5: 记录发现**

---

## Task 9: 代码质量检查

**Covers:** 代码质量、死代码、硬编码

**Files:**
- Read: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Read: `src/Client/Desktop/Shell/Services/MenuManager.cs`
- Read: `src/Client/Desktop/Shell/Services/StatusBarManager.cs`

**Interfaces:**
- Consumes: 各Shell层文件
- Produces: 代码质量检查报告

- [ ] **Step 1: 检查MainWindowViewModel**
  
  检查点：
  - 行数是否仍超过1000行
  - 构造函数参数数量
  - 是否有死代码
  - 是否有硬编码值

- [ ] **Step 2: 检查MenuManager**
  
  检查点：
  - 是否有接口
  - 是否有硬编码颜色
  - 命令初始化是否正确

- [ ] **Step 3: 检查未使用的using**
  
  使用serena_get_diagnostics_for_file检查：
  - MainWindowViewModel.cs
  - MenuManager.cs
  - StatusBarManager.cs
  
  统计IDE0005警告数量

- [ ] **Step 4: 检查硬编码值**
  
  搜索：
  - 硬编码URL
  - 硬编码密码
  - 硬编码端口号
  - 硬编码颜色值

- [ ] **Step 5: 记录发现**

---

## Task 10: 测试覆盖检查

**Covers:** 测试覆盖完整性

**Files:**
- Read: `tests/LYBT.Tests.Desktop/Unit/Shell/*.cs`
- Glob: `tests/LYBT.Tests.Desktop/**/*Shell*`
- Glob: `tests/LYBT.Tests.Desktop/**/*Navigation*`
- Glob: `tests/LYBT.Tests.Desktop/**/*Session*`

**Interfaces:**
- Consumes: 测试文件
- Produces: 测试覆盖检查报告

- [ ] **Step 1: 列出所有Shell相关测试**
  
  搜索：
  - StartupPipelineTests
  - StartupStepsTests
  - DpapiPhotoStorageServiceTests
  - 其他Shell相关测试

- [ ] **Step 2: 检查测试覆盖范围**
  
  检查点：
  - 启动管线是否有测试
  - 模块加载是否有测试
  - 导航系统是否有测试
  - 会话管理是否有测试
  - 健康检查是否有测试

- [ ] **Step 3: 识别缺失的测试**
  
  对照Shell层核心类：
  - AppStartupOrchestrator - 无测试
  - ApplicationBootstrapper - 无测试
  - NavigationCoordinator - 无测试
  - NavigationManager - 无测试
  - SessionManager - 无测试
  - MenuManager - 无测试
  - StatusBarManager - 无测试

- [ ] **Step 4: 记录发现**

---

## Task 11: 汇总报告

**Covers:** 所有检查结果汇总

**Files:**
- Write: `docs/compose/reports/2026-07-14-shell-deep-inspection-report.md`

**Interfaces:**
- Consumes: Task 1-10的检查结果
- Produces: 完整的检查报告

- [ ] **Step 1: 汇总所有发现**
  
  按严重度分类：
  - Critical: 必须立即修复
  - Major: 需要尽快修复
  - Minor: 建议修复
  - Info: 仅供参考

- [ ] **Step 2: 与2026-06-28审计对比**
  
  检查点：
  - 哪些问题已修复
  - 哪些问题仍存在
  - 是否有新问题

- [ ] **Step 3: 生成修复优先级建议**
  
  按收益/成本比排序修复建议

- [ ] **Step 4: 保存报告**

---

## Self-Review

### 1. 覆盖完整性
- [ ] 启动管线 ✓ (Task 1)
- [ ] 模块加载 ✓ (Task 2)
- [ ] 导航系统 ✓ (Task 3)
- [ ] 会话管理 ✓ (Task 4)
- [ ] 健康检查 ✓ (Task 5)
- [ ] 主题服务 ✓ (Task 6)
- [ ] 对话框系统 ✓ (Task 7)
- [ ] DI注册 ✓ (Task 8)
- [ ] 代码质量 ✓ (Task 9)
- [ ] 测试覆盖 ✓ (Task 10)
- [ ] 汇总报告 ✓ (Task 11)

### 2. 无占位符
所有步骤都有具体的检查点和验证方法，无TBD/TODO。

### 3. 类型一致性
所有文件路径和符号名称与代码库一致。

---

## Execution Handoff

Plan saved. How would you like to execute it?

- **Subagent, always**: Fresh subagent per task — remember for future sessions
- **Subagent, this time**: Fresh subagent per task — just this once
- **Inline, always**: Execute in this session — remember for future sessions
- **Inline, this time**: Execute in this session — just this once
