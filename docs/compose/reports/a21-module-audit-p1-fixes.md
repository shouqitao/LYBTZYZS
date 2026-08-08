---
feature: a21-module-audit-p1-fixes
status: delivered
specs:
  - docs/03-architecture/task-a21-module-audit-p1-fixes-2026-08-08.md
branch: master
commits: 1b19bce13..4ddcec6ed
---

# A-21 模块级审计 P1 修复批次 — Final Report

## What Was Built

A-21 落地模块级结构审计（2026-08-08 交叉验证）的 P1 修复批次，6 项独立清理/修复，每项独立验证 + 独立 commit + push 至 master。累计净删约 2057 行死代码/冗余（M3 -470、M2 -573、F-01+Tools -1014），修复 1 个真实运行时 bug（Shell 角色模块加载），消除 1 个机制性空转（领域事件），完成 3 处分层/职责收敛（VM 越层、Desktop 映射、LocalData 废弃）。

| 项 | 内容 | Commit |
|----|------|--------|
| M4 | Shell RoleDefinitionBase 模块名 bug 修复（AuthModule→AuthenticationModule） | `1b19bce13` |
| M5 | 3 个 VM 越层改走 Service 层（ReportsHome/AuditLog/RegistrationList） | `3d692c5f5` |
| M3 | 领域事件空转删除（12 事件 0 订阅者，13 处发布 + 基础设施） | `4ad350a39` |
| M2 | Desktop 映射统一（删 4 零引用 Mapper；12 手写映射点按行为等价评估） | `876581164` |
| C1 | LocalData 废弃（LocalDbContext 移出生产层，测试基建保留） | `2fa05ff63` |
| F-01+Tools | 删 FeatureToggle 机制 + Tools 3 项目（保留 PasswordHashGenerator） | `271bca70f` |

## Architecture

### M4 — RoleDefinitionBase 模块名（真实 bug）

`RoleDefinitionBase.SharedBaseModules` 的字符串直传 Prism `IModuleManager.LoadModule(moduleName)`，必须与 ModuleCatalog 注册的 IModule 类名精确匹配。原 `"AuthModule"` 与 `AuthenticationModule`（`LYBT.Desktop.Auth/AuthenticationModule.cs`）不匹配，每次登录 `LoadModulesForRoleAsync` 加载基础模块时抛异常被 catch 吞掉。逐一核对 8 个模块名（Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports/Sysadmin）与 ModuleCatalog 类名，仅此 1 处错误。消费方：`NavigationManager.BuildNavigationItems` 用 `RequiredModules.Contains(...)` 硬编码字符串匹配，不涉及 BaseModules，无需改动。

### M5 — VM 越层收敛

VM 层不再直连 IApiClient，改为注入 Service 接口（VM→Service→IApiClient 分层）：

- `ReportsHomeViewModel`：新建 `IReportService`（Contracts/Services）+ `ReportService`（MedicalCase/Reports/Services/），封装 `IApiClient.Reports` 3 个日报方法，ApiResponse→CommandResult 转换。
- `AuditLogViewModel`：新建 `IAuditLogService` + `AuditLogService`（MedicalCase/Services/），封装 `GetAuditLogsAsync`。
- `RegistrationListViewModel`：`IApiClientPatients` → 复用已下沉 Contracts 的 `IPatientService.GetByIdAsync`。

DI 注册 2 处（ReportsModule/MedicalCaseModule）；测试构造参数同步。行为等价：VM 逻辑零改动，仅换注入对象。

### M3 — 领域事件空转删除

Herb/Formula/Users/Patients/Registration/Auth 6 模块的 Created/Deleted/SessionCreated/SessionRevoked 等 12 类领域事件全仓 0 个 `INotificationHandler` 订阅者。技术总监拍板删除（YAGNI，未来需要时按 MediatR 模式再加）。删除 13 处发布代码（含 `_eventDispatcher`/`_publisher` 字段与构造注入清理）、12 个 Domain/Events 事件定义文件、SharedKernel/Events 基础设施 3 件套（IDomainEvent/IDomainEventDispatcher/InMemoryDomainEventDispatcher）、WebAPI DI 注册。BatchDeleteFormulasCommandHandler 的事件钩子（OnBatchCompletedAsync/_deletedNames/_operatorId）移除，基类默认空实现行为等价。异步跨模块通知实际走 SignalR `INotificationService`（B-10），AGENTS.md 同步修正。

### M2 — Desktop 映射统一

删除 4 个零引用 Mapper（HerbMapper/UserMapper/FormulaMapper + 级联死 FormulaHerbItemMapper，全仓 0 调用）。12 个手写 DTO↔Model 映射点逐文件核查：仅 1 处纯属性复制（`HerbItemControlViewModel.ToDto`），实测 Mapperly 4.3.1 无法解析 CommunityToolkit.Mvvm `[ObservableProperty]` 生成属性（RMG012，源生成器时序限制），按 A-18 P1-4 先例（Mapperly 无法等价表达则保留手写）保留；其余 11 处均含业务逻辑（PinYinCode 回退计算、绕 setter 防拼音、Trim、Guid.Empty→null、价格查表、集合过滤编排），转 Mapperly 会改变行为，违反「不重构业务逻辑」约束，保留手写。

### C1 — LocalData 废弃

`LocalDbContext`（休眠 204 行，生产 0 引用）移出 `LYBT.Desktop.Infrastructure/LocalData` 至测试项目 `LYBT.Tests.Desktop/_Infrastructure/`（git 识别 97% rename），UserJourney 测试基建（真实 LocalDB 数据操作）完整保留。生产 csproj 删除 LocalData Dependencies 组（LYBT.Entities/EF Core.SqlServer/EF Core.Design/BCrypt 仅被其使用；Microsoft.Extensions.Options 归位 Configuration 组——ConnectionSettings/ClinicSettingsService 在用）。架构测试 DM08 改写为守卫「生产层不得含 LocalDbContext」（防回迁）。CardReader 独立按决策留 P2。

### F-01 + Tools — 清理批次

FeatureToggle 机制（14 开关缩至 2 且无有效消费）：删 FeatureToggleOptions、2 处注册点（PrismConfigurationExtensions/ClientConfigurationExtensions）、死服务 PrescriptionSettingsService + IPrescriptionSettingsService（0 注入消费，唯一直读 FeatureToggles 节的引用方）、RegisterReloadableOptions 及其级联死 3 内部类、Shell appsettings FeatureToggles 节、测试断言。Tools：删 ApiTester/LoginTester/UserInfoVerifier 3 独立目录（sln 无条目），保留 PasswordHashGenerator（运维用）。

## Usage

无新用户接口。涉及行为变化：

- 登录后 Shell 模块加载不再静默失败（M4 修复前 `"AuthModule"` 加载抛异常被 catch，日志 `LoadModulesForRoleAsync` 记 failed）。
- 报表/审计日志/挂号接诊的数据获取路径改为 Service 层（对外行为不变）。
- 配置中不再存在 `FeatureToggles` 节（删除后 `PrescriptionSettingsService` 的 `DuplicateHerbMergeStrategy` 配置项随之移除；重复药材合并策略 UI 侧走 `DuplicateDosageStrategy` DP，不受影响）。

## Verification

每项独立验证（任务书要求）：

1. `dotnet build LYBTZYZS.sln --no-incremental -m:1`：**0 错误 0 警告**（6 项全部通过）
2. `dotnet test tests/LYBT.Tests.Architecture/`：**85/85**（6 项全部通过；DM08 改写后仍 85 条）
3. 相关单测：M5 RegistrationMasterDetailViewModelTests 6/6；M3 Server 单测 Herb/Formula 89 + User/Patient 72 + Registration/Auth 46 = 207/207；C1 FrameworkVerificationTests 10/10（含 LocalDbContext DI 解析）；T6 ConfigurationLoadingTests 21/21
4. Desktop 全量单测 84 失败为 C-01 已知环境项（需运行中 WebAPI），M2 用 stash 基线复现确认与改动无关

## Journey Log

- [lesson] 交叉验证清单与代码事实有偏差：任务书称「3 个零引用 Mapper」实为已写好的 Mapperly 死代码；「11 处事件发布」实为 13 处。执行前必须逐文件核实，不可按清单盲改。
- [dead end] HerbItemControlViewModel.ToDto 尝试转 Mapperly：RMG012 —— Mapperly 4.3.1 无法解析 CommunityToolkit.Mvvm `[ObservableProperty]` 生成属性（源生成器时序限制），行为等价不可达，回滚保留手写。
- [pivot] C1 LocalData 从「删除」调整为「移入测试项目」：UserJourney 测试基建（真实 LocalDB）依赖 LocalDbContext，且 Desktop 测试不能引用 Server 项目（无可替代 DbContext），移动是生产清理与测试保留的双赢解。
- [lesson] 删死代码时级联检查内部类/私有方法：RegisterReloadableOptions 删调用后，ConfigurationOptionsMonitor/OptionsMonitorWrapper/ChangeListenerDisposable 3 个内部类级联死，一并删除。

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/03-architecture/task-a21-module-audit-p1-fixes-2026-08-08.md` | 任务书 | 6 项任务与验证标准 |
| `docs/03-architecture/structure-audit-module-level-crosscheck-2026-08-08.md` | 审计依据 | P1 清单来源 |
| `docs/03-architecture/13-project-master-plan.md` | 项目总账 | §六 A-21 ⬜→✅ + §八 完成登记（6 commit） |
