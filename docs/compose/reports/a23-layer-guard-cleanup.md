---
feature: a23-layer-guard-cleanup
status: delivered
specs:
  - docs/03-architecture/task-a23-layer-guard-cleanup-2026-08-08.md
plans:
  - docs/03-architecture/task-a23-layer-guard-cleanup-2026-08-08.md
branch: master
commits: d3290b321..9f22657ec
---

# A-23 越层修复 + 守卫补全 + 孤儿类清理 — Final Report

## What Was Built

A-23 分三部分落地，消除 Desktop MVVM 分层中剩余的 VM→IApiClient 真越层、补上缺失的架构守卫、并清理全仓 29 个零引用孤儿类（约 2780 行）：

1. **A-23a（3 个 VM 真越层修复）**：AccountSettingsViewModel（`IApiClientUsers`→`IUserService`）、PatientSelectionViewModel（`IApiClientPatients`+`IApiClientMedicalCases`→`IPatientService`+`IMedicalCaseQueryService`）、SysadminHomeViewModel（`IApiClientAuth`→新建 `IAuthHealthService`）。行为等价，未改业务逻辑。
2. **A-23b（架构守卫补全）**：新增 DP10 守卫「Desktop ViewModel 禁止注入 `IApiClient*` 子接口」（统一 `IApiClient` 作为 A-18 过渡期豁免），堵住 A-21 漏抓的守卫真空。
3. **A-23c（孤儿类 D=29 清理）**：删除 Patients 死组件链 12 类、Infrastructure 9 类、Foundation 2 类、Formula 2 类、Contracts/Controls/MedicalCase 各 1 类、Shared.Configuration 1 类；同步清理死链方法（`ShowUnfinishedCaseDialogAsync`）、注册孤儿（App.xaml.cs / ServiceCollectionExtensions / PatientsModule）、死类测试（ApiRouterTests / MedicalCaseChangeTrackerTests）；`LoggingHttpHandler` 下沉 Infrastructure→Foundation。

## Architecture

修复后 Desktop 分层统一为：**VM → Service 接口（Contracts/Services）→ Repository（模块内）→ IApiClient（统一/子接口）→ 双轨适配（Refit/Http）**。

### 关键组件

| 组件 | 位置 | 作用 |
|------|------|------|
| `IUserService` | `Contracts/Services/IUserService.cs` | 用户资料/密码管理（ChangeProfileAsync/ChangePasswordAsync） |
| `IPatientService` | `Contracts/Services/IPatientService.cs` | 患者查询（GetPatientsPagedAsync/GetByIdAsync） |
| `IMedicalCaseQueryService` | `Contracts/Services/IMedicalCaseQueryService.cs` | 医案读操作（新增 `GetPendingCasesAsync`） |
| `IAuthHealthService` | `Contracts/Services/IAuthHealthService.cs`（新建） | 封装 `IApiClientAuth.HealthCheckAsync` |
| `AuthHealthService` | `Admin/Sysadmin/Services/`（新建） | IAuthHealthService 实现，SysadminModule 注册 |
| `MedicalCaseRepository` | `MedicalCase/Repositories/` | 新增 `GetPendingCasesAsync`（经统一 IApiClient 路由） |

### Design Decisions

- **统一 IApiClient 保留为过渡豁免**：A-18 有意设计的 3 个 VM（SystemSettings/LogLevelControl/Deployment）注入统一 `IApiClient`，技术总监判断长期目标是 VM 全部走 Service，但本次只守「子接口注入」硬违规，统一接口过渡允许（A-23b 任务书明示）。
- **GetPendingCasesAsync 补链而非复用 GetUnfinishedCaseByPatientIdAsync**：两者语义不同（Pending 含 Suspended+Active 列表 vs Unfinished 单条 Detail），为行为等价在 Contracts 接口 + Repository 链补齐方法。
- **IAuthHealthService 返回 `ApiResponse<HealthCheckResponse>` 而非 bool**：VM 现用 `healthResp.Success`，透传封装保持行为 100% 等价（含异常语义），避免 Service 吞异常改变 dashboard 状态行为。

## Usage

- VM 注入 Service 接口（`IUserService`/`IPatientService`/`IMedicalCaseQueryService`/`IAuthHealthService`），禁止注入 `IApiClient` 子接口（DP10 守卫强制）。
- 新增服务自动经模块 DI 注册（`SysadminModule`/`MedicalCaseModule`/`UsersModule`/`PatientsModule`），无需额外配置。
- `dotnet test tests/LYBT.Tests.Architecture/` 运行 86 条架构守卫（含 DP10）。

## Verification

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | 3 次均 0 错误 0 警告 |
| 架构测试 | A-23a/b/c 后 85/85 → 86/86（新增 DP10） |
| 相关单测 | PatientSelectionViewModel 11/11；Desktop 全量失败数与基线一致（存量环境项：STA 线程/LocalWebAPI TCP/LastVisitTime 断言，stash 基线复现） |
| Git | 3 个独立 commit + 总账更新，均已 push 至 Gitee |

## Journey Log

- [lesson] Desktop 全量测试在本环境必然超时中止（TestSessionTimeout 300s），两次运行中止点不同导致总数不可比；验证以「相关单测 + stash 基线失败集合对比」为准，避免误判删除引入回归。
- [lesson] `Interfaces/` 目录整删后仍有文件残留 using（PatientsModule/PatientStatusHandler/2 测试文件），编译错误暴露后逐个清理；同名类干扰（Server 端 CreatePatientValidator、Shared 实体 FormulaHerbItem、各 `*ConfigurationExtensions`）在删除前必须逐一排除。
- [lesson] LoggingHttpHandler 下沉需同步补 csproj 项目引用（Foundation 原先无 `LYBT.Shared.Logging` 引用）。

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/03-architecture/task-a23-layer-guard-cleanup-2026-08-08.md` | 任务书（唯一计划） | 3 项任务定义 + 验证标准 |
| `docs/03-architecture/structure-audit-perclass-mimo-2026-08-08.md` | 孤儿类 D=29 清单来源 | §1.2 全清单 + 逐类证据 |
| `docs/03-architecture/structure-audit-perclass-crosscheck-2026-08-08.md` | 越层 3 处技术总监复核 | §四 |
| `docs/03-architecture/13-project-master-plan.md` | 项目总账 | 状态表已更新（A-23 行） |
