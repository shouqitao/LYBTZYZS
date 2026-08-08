# 任务 A-24：C 级清理批次（类活但含死方法 + 机制残留）

> 依据：`structure-audit-perclass-mimo-2026-08-08.md` §C 级待决策项（63 个按簇归纳）
> 前置：A-23c 已清理 D=29 孤儿类（Patients 死组件链 12 类 + Infrastructure 孤儿 8 类已确认删除）
> 本批次处理 C 级剩余：**Server 模块死方法（22 个类）+ 9 个机制残留簇**

## 任务清单

### 第 1 项：Server 模块死方法清理（22 个类，删方法不删类）

从 `structure-audit-perclass-mimo-2026-08-08.md` §3 各模块表提取的 C 级死方法（技术总监已确认部分）：

| 类 | 死方法 | 项目 |
|----|--------|------|
| FormulaModel | RemoveHerb(:158) / MarkShared(:174) | Entities（类活，2 死方法）|
| HerbModel | UpdatePrice(:180) | Entities |
| FormulaDetailDto | GetHerbNamesList | Shared.Models |
| BaseCrudController | ExecuteBatchStatusAsync（0 引用）| Infrastructure |
| BaseClaimsHelper | IsAdmin / ParseUserRole | Infrastructure |
| QueryablePagingExtensions | SelectAsync | Infrastructure |
| ProblemDetailsConfiguration | UseStatusCodePagesWithProblemDetails（已内联）| Infrastructure |
| AuthSessionRepository + IAuthSessionRepository | GetByIdAsync(:20) / GetActiveSessionsAsync(:48) | Module.Auth |
| UserRepository + IUserRepository | AddAsync(:87) / ExistsByUserNameAsync(:80) | Module.Users |
| + 其余模块表标注的死方法（Read 报告 §3 逐模块核对，Method/Handler/Query 中 0 调用）| | |

**动作**：逐方法复证零引用（serena/codebase-memory）→ 删方法/瘦接口；类本身保留（A 级有依据）。

### 第 2 项：机制残留清理（9 簇）

| # | 残留 | 处置 |
|---|------|------|
| 4 | Shared.Logging `ServiceCollectionExtensions.AddSharedLogging` 双重载 0 调用（宿主手工注册冗余）| 删除扩展方法，保留 LoggingLevelManager/CorrelationId 类（宿主已在用）|
| 5 | Foundation 注册孤儿 `IApiService/ApiService/RequestDeduplicator`（注册 :103，生产 0 消费）| 删类 + 删注册 |
| 6 | Foundation 3 个惰性 AuthEvents（LoginSucceeded/LoginFailed/SessionExpired，0 发布 0 订阅）| 删除 |
| 7 | Contracts `UnfinishedCaseChoice`（随 UnfinishedCaseDialog 死链，A-23c 已删 Dialog）| 删除 |
| 8 | Tests.Desktop Traits 特性体系 18 类 + UserJourneyTestBaseShared（0 消费测试基建）| 评估：测试基建若无人用删，保留需说明理由 |
| 9 | `LocalWebApiProgram.RunAsync` 死方法（双入口合并）| 删除死入口 |
| 10 | Registration 命名空间复数漂移（LYBT.Desktop.Registrations vs 项目单数）| **不改命名空间**（A-08 命名统一未拍板），记录 P2 |
| 3 | Infrastructure Http 目录错层（LoggingHttpHandler 存活）| **A-23c 已下沉 Foundation**——验证是否完成，未完成则补 |

### 第 3 项：C-2 VM 越层剩余核对

Mimo 报告 §99-2 列「Desktop 6 处 VM 越层」——技术总监复核：3 处 A-18 有意设计（统一 IApiClient 豁免，DP10 守卫已豁免）、3 处 A-23a 已修。**本项只需验证 DP10 守卫生效后无新增违规**（跑架构测试即可）。

## 验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：86/86
3. 相关单测
4. 每项独立 commit + push

## 不做

- ❌ 不删 A 级类（只有死方法才删）
- ❌ 不改 Registration 命名空间（P2 记录）
- ❌ 不动 C-2 统一 IApiClient 注入（过渡豁免）
- ❌ 不改蓝图（A-24 完成后统一记入蓝图变更）
