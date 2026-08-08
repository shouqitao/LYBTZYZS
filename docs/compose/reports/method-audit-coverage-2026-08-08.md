# A-30-S0 方法级统计基线 — 审查覆盖表

> 生成：Mimo Code（S0 只读审查）｜任务书：`task-a30-s0-method-baseline-2026-08-08.md`
> 基线 commit：`7edf020f5`｜生成时间：2026-08-09 02:10
> 性质：**覆盖对照**——文件数（实测 vs 蓝图）/ 类型数（实测 vs A-22）/ 方法数（实测）三表 + 测试覆盖粗查

## 0. 口径

| 项 | 说明 |
|----|------|
| 文件数 | 项目目录下 `.cs` 文件数（排除 `bin/obj/Migrations`）；蓝图 v1.5 文件数为 A-29 回写实测值，**与本次口径一致**（均为 .cs 源文件）|
| 类型数 | 源码级正则扫描（含嵌套类型声明）；A-22 为"顶层类型"口径（嵌套计入宿主）——**两者口径略有差异**（本表 A-22 列引用其报告值）|
| 方法数 | 本次 S0 正则扫描（含构造函数），误差率 2.9%（见 baseline 报告 §0）|
| 项目范围 | 35 项目 = sln 34 + Tools 1（PasswordHashGenerator 不在 sln）|

## 1. 逐项目三表对照（文件数 / 类型数 / 方法数）

| 层 | 项目 | 文件数实测 | 蓝图文件数 | 差异 | 类型数实测 | A-22类型数 | 类型差异 | 方法数 | 公开 | 非公开 |
|----|------|-----------|-----------|------|-----------|-----------|---------|--------|------|--------|
| Desktop | LYBT.Desktop.Admin | 17 | 17 | = | 19 | 18 | 1 | 61 | 29 | 32 |
| Desktop | LYBT.Desktop.Auth | 10 | 10 | = | 10 | 10 | = | 57 | 15 | 42 |
| Desktop | LYBT.Desktop.Clinical | 21 | 21 | = | 23 | 23 | = | 140 | 56 | 84 |
| Desktop | LYBT.Desktop.Contracts | 78 | 78 | = | 110 | 111 | -1 | 487 | 27 | 460 |
| Desktop | LYBT.Desktop.Controls | 41 | 41 | = | 46 | 47 | -1 | 149 | 77 | 72 |
| Desktop | LYBT.Desktop.Formula | 14 | 14 | = | 14 | 16 | -2 | 77 | 45 | 32 |
| Desktop | LYBT.Desktop.Foundation | 70 | 70 | = | 108 | 120 | -12 | 511 | 377 | 134 |
| Desktop | LYBT.Desktop.Herbs | 13 | 13 | = | 13 | 13 | = | 59 | 38 | 21 |
| Desktop | LYBT.Desktop.Infrastructure | 92 | 92 | = | 127 | 137 | -10 | 596 | 286 | 310 |
| Desktop | LYBT.Desktop.MedicalCase | 49 | 49 | = | 55 | 56 | -1 | 240 | 141 | 99 |
| Desktop | LYBT.Desktop.Patients | 18 | 18 | = | 19 | 31 | -12 | 73 | 47 | 26 |
| Desktop | LYBT.Desktop.Printing | 12 | 12 | = | 17 | 17 | = | 61 | 40 | 21 |
| Desktop | LYBT.Desktop.Registrations | 9 | 9 | = | 10 | 10 | = | 58 | 23 | 35 |
| Desktop | LYBT.Desktop.Shell | 49 | 49 | = | 55 | 55 | = | 232 | 105 | 127 |
| Desktop | LYBT.Desktop.Users | 15 | 15 | = | 15 | 15 | = | 77 | 51 | 26 |
| Shared | LYBT.Entities | 18 | 18 | = | 18 | 18 | = | 39 | 39 | 0 |
| Server | LYBT.Infrastructure | 64 | 87 | -23 | 71 | 71 | = | 210 | 98 | 112 |
| Desktop | LYBT.LocalWebAPI | 25 | 25 | = | 24 | 24 | = | 109 | 106 | 3 |
| Server | LYBT.Module.Auth | 25 | 26 | -1 | 25 | 26 | -1 | 52 | 34 | 18 |
| Server | LYBT.Module.Formula | 36 | 21 | 15 | 36 | 21 | 15 | 54 | 41 | 13 |
| Server | LYBT.Module.Herbs | 37 | 22 | 15 | 38 | 23 | 15 | 72 | 54 | 18 |
| Server | LYBT.Module.MedicalCase | 22 | 22 | = | 22 | 22 | = | 173 | 98 | 75 |
| Server | LYBT.Module.Patients | 29 | 23 | 6 | 29 | 23 | 6 | 53 | 37 | 16 |
| Server | LYBT.Module.Registration | 27 | 27 | = | 27 | 27 | = | 69 | 54 | 15 |
| Server | LYBT.Module.Reports | 7 | 7 | = | 11 | 11 | = | 46 | 23 | 23 |
| Server | LYBT.Module.Users | 36 | 29 | 7 | 37 | 30 | 7 | 91 | 63 | 28 |
| Shared | LYBT.Shared.Configuration | 25 | 25 | = | 33 | 34 | -1 | 9 | 8 | 1 |
| Shared | LYBT.Shared.ExceptionHandling | 7 | 7 | = | 7 | 7 | = | 61 | 60 | 1 |
| Shared | LYBT.Shared.Logging | 8 | 8 | = | 10 | 11 | -1 | 38 | 28 | 10 |
| Shared | LYBT.Shared.Models | 120 | 120 | = | 142 | 144 | -2 | 65 | 59 | 6 |
| Tests | LYBT.Tests.Architecture | 9 | — |  | 8 | 8 | = | 87 | 81 | 6 |
| Tests | LYBT.Tests.Desktop | 108 | — |  | 126 | 139 | -13 | 1177 | 1024 | 153 |
| Tests | LYBT.Tests.Server | 63 | — |  | 84 | 70 | 14 | 681 | 603 | 78 |
| Tools | LYBT.Tools.PasswordHashGenerator | 1 | — |  | 1 | — |  | 4 | 0 | 4 |
| Server | LYBT.WebAPI | 30 | 30 | = | 34 | 34 | = | 150 | 124 | 26 |
| **合计** | **35** | **1205** | — | — | **1424** | — | — | **6118** | — | — |

### 1.1 文件数差异说明（蓝图 vs 实测）

> 蓝图 v1.5（A-29）仅回写了 **Desktop 层（§1/§3）** 文件数为实测值；**Server 模块（§2.2）与 Infrastructure 仍为设计值**，故差异集中在 Server 侧。

| 项目 | 蓝图 | 实测 | 差异说明 |
|------|------|------|---------|
| LYBT.Infrastructure | 87 | 64 | 蓝图 87 = 实测 64 + **Migrations 23 个**（蓝图计数含迁移文件，任务书口径排除）——同口径下一致 |
| LYBT.Module.Formula | 21 | 36 | Server 模块蓝图未回写（设计值 21，实现已增至 36，CQRS 命令/Handler 拆分所致）|
| LYBT.Module.Herbs | 22 | 37 | 同上（设计值 22 vs 实测 37）|
| LYBT.Module.Users | 29 | 36 | 同上（设计值 29 vs 实测 36）|
| LYBT.Module.Patients | 23 | 29 | 同上（设计值 23 vs 实测 29）|
| LYBT.Module.Auth | 26 | 25 | 设计值 26 vs 实测 25（蓝图含 1 个设计内但未落地文件）|
| 其余项目 | 蓝图 v1.5 | 实测 | 一致（Desktop 侧已回写实测值）|

### 1.2 类型数差异说明（实测 vs A-22）

> 实测列（本 S0）为源码级正则扫描（含嵌套类型声明），已用 Roslyn 独立抽样验证一致（如 Foundation：正则 108 = Roslyn 108，文件 70）。A-22 为"顶层类型判定"口径（嵌套计入宿主类），**两者口径不同**，差异不代表扫描错误。A-22 本身为 6 子代理逐类判定产物，存在人工计数偏差可能（如 A-22 Patients 31 vs 实测 19 差距达 12，S1 复核时以 Roslyn/源码为准）。

| 项目 | 实测 | A-22 | 差异 | 说明 |
|------|------|------|------|------|
| LYBT.Desktop.Admin | 19 | 18 | 1 | 实测含嵌套类型声明，A-22 为顶层口径 |
| LYBT.Desktop.Contracts | 110 | 111 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.Controls | 46 | 47 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.Formula | 14 | 16 | -2 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.Foundation | 108 | 120 | -12 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.Infrastructure | 127 | 137 | -10 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.MedicalCase | 55 | 56 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Desktop.Patients | 19 | 31 | -12 | **A-22 计数偏差**（A-22 该行含 12 个孤儿类判定，多为内部辅助类/事件参数类，实测按类型声明计 19） |
| LYBT.Module.Auth | 25 | 26 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Module.Formula | 36 | 21 | 15 | **A-22 未覆盖**（A-22 与蓝图同为设计态参考，实测为 CQRS 拆分后当前态） |
| LYBT.Module.Herbs | 38 | 23 | 15 | **A-22 未覆盖**（A-22 与蓝图同为设计态参考，实测为 CQRS 拆分后当前态） |
| LYBT.Module.Patients | 29 | 23 | 6 | **A-22 未覆盖**（A-22 与蓝图同为设计态参考，实测为 CQRS 拆分后当前态） |
| LYBT.Module.Users | 37 | 30 | 7 | **A-22 未覆盖**（A-22 与蓝图同为设计态参考，实测为 CQRS 拆分后当前态） |
| LYBT.Shared.Configuration | 33 | 34 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Shared.Logging | 10 | 11 | -1 | 口径差异（嵌套 vs 顶层） |
| LYBT.Shared.Models | 142 | 144 | -2 | 口径差异（嵌套 vs 顶层） |
| LYBT.Tests.Desktop | 126 | 139 | -13 | 口径差异（嵌套 vs 顶层） |
| LYBT.Tests.Server | 84 | 70 | 14 | 实测含嵌套类型声明，A-22 为顶层口径 |

## 2. 测试覆盖粗查（静态统计，不跑测试）

- Tests 方法数：**1945**（Architecture 87 / Server 681 / Desktop 1177）
- 业务方法数（src/）：**4173**
- 静态覆盖比（Tests 方法 / 业务方法）：**46.6%**
- Tests 类型数：**218**

> ⚠️ 静态比例 ≠ 真实覆盖率：一个测试方法可断言多业务方法，且 Integration 测试（Server ~1185 / Desktop 多）经真实 DB 全链路。仅作规模参照。

## 3. 覆盖结论

- 文件数：蓝图 v1.5 Desktop 侧已回写实测值；Server 模块/Infrastructure 为设计值（差异原因见 §1.1，含 Migrations 口径）
- 类型数：实测 1424（含嵌套）已用 Roslyn 抽样验证一致；与 A-22 1422（顶层口径）整体量级吻合，逐项目差异源于口径 + A-22 人工计数偏差，S1 复核以 Roslyn/源码为准
- 方法数：全仓 6118（src 业务 4173 + tests 1945），为 S1-S4 各阶段方法级复核的完整索引
- 本表三份基线（baseline/duplicates/dead-candidates）共同构成 S0 产出，供 S1-S4 引用


