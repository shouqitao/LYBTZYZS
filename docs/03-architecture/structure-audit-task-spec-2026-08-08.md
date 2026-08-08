# 结构审计任务书（structure-audit-2026-08-08）

> 派发对象：Mimo Code 独立审计
> 版本：v1.0 | 日期：2026-08-08
> 技术总监将与另一份独立分析交叉验证，本任务书为派发基线。

## 任务

LYBTZYZS 全项目结构设计健康度审计（**只读**，禁止修改任何代码）。

## 范围

- ✅ `src/Shared` + `src/Server` + `src/Client`（34 个项目）
- ❌ 排除：`tests/`、`docs/`、`src/Tools/`、`Migrations/`（迁移历史不算）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜最近提交 `97445a6f3`（死代码清理完成）

## 背景（必读）

- 原始设计意图：三层 = WebApi(Server) + Shared(公共) + Desktop(Client)。数据流 = 数据 → 分层 → API → Desktop HttpClient 解析 → ViewModel → View。
- Shared 层职责：公共类型单源（如 Gender 枚举只在 `Shared/Models/Enums/Gender.cs` 定义一次，双端引用——这是样板，审计目标是找**反例**）。
- Dual-Mode 是**设计意图**（非缺陷）：Remote(5000, SQL Server) + Local(5300, LocalDB) 双轨，`SwitchingApiClient` 切换。审计双轨**实现质量**，不质疑双轨存废。
- 最近已完成：死代码清理 2 轮（Serena + codebase-memory 交叉验证，`97445a6f3`），命名空间统一复数（Q-03，`27b65e42b`），using 别名清理（Q-02，`5de43132b`）。这些可引用为「已知」，但死代码要求**独立重扫找新问题**。
- 相关报告（可引用为已知）：`docs/03-architecture/dead-code-analysis-2026-08-08.md`、`dead-code-analysis-codebase-memory-2026-08-08.md`、`namespace-consistency-*.md`、`webapi-deep-analysis-*.md`。

## 聚焦 7 类

### 1. 分层/依赖违规
引用方向错误、循环依赖、Shared 被 Server/Client 反向引用、模块越界。
架构约束参考：P07 模块间禁止直接引用 / P08 跨模块必须用接口 / P10 Service 禁注入 AppDbContext（仅 Repository/Base 可）。
产出：项目依赖图（谁引用谁，标注违规边）。

### 2. Shared 归属问题
该共享却两端各定义一份、该下沉 Shared 却留在端侧、公共类型重复定义。
样板：Gender 单源。找反例：枚举/常量/DTO/工具类两端重复。

### 3. 多机制并存（全量清单）
同一功能多套实现。已知候选（须核实 + 找新）：
- 日志 CorrelationId：AsyncLocalCorrelationIdProvider vs ActivityCorrelationIdProvider
- DbContext：5 模块独立 + 5 复用 AppDbContext
- 密码：BCrypt PasswordHelper vs Identity PBKDF2
- 映射：Mapperly 编译期 + 手动映射
- 批量操作：BatchOperationHandlerBase + BatchImport 3 Handler + Service 版 BatchEnable/Disable
- API 契约双套：`Contracts/Api/*`（Refit 特性）vs `Contracts/ApiClient/*`（Unified 无特性）
- 异常处理：异常工厂（已删）vs 直接 throw；业务异常 vs ProblemDetails
每项标注：存活双方 + 证据 + 各自活跃度（引用计数）。

### 4. 死代码
零引用类型/方法/常量。**独立重扫**，不抄上轮结论；上轮结论仅可引用为「已知」。

### 5. 命名/组织一致性
同类事物不同命名惯例（Manager/Service/Handler 混用、单复数、目录形态），只列证据不判对错。

### 6. 顺带发现
审计过程中顺带发现的**任何非结构类疑点**——正确性可疑、设计缺陷、代码异味、潜在 bug。
**只记录不深挖**：证据 + 疑点 + 为什么觉得有问题。

### 7. 数据流逐层走查（重点）
沿数据链路每一层走查，每层回答 4 问：
1. **职责**：这一层该干什么？实际干了什么？（越层/重复/空转）
2. **转换点**：Entity↔DTO↔ViewModel 的映射在哪发生？由谁负责？（Mapperly vs 手动）
3. **路径**：远程/本地两条路径是否对称？差异在哪？
4. **异常传导**：数据出错（404/403/409/500）如何从 API 传回 ViewModel 显示？

**双轨对称性 6 检查点**（本专项命门）：
| # | 检查点 | 具体问题 |
|---|--------|---------|
| 1 | Controller 对称性 | WebAPI（14 个）vs LocalWebAPI（12 个）是否 1:1？缺的 2 个是刻意还是遗漏？ |
| 2 | 业务逻辑共享 | LocalWebAPI 引用 7 个 Server 模块——Handler/Service 是真复用，还是各写一套？ |
| 3 | 数据访问一致性 | Remote=AppDbContext(SQL Server) vs Local=LocalDB——两套 DbContext 的 schema/迁移链是否漂移？ |
| 4 | 切换逻辑 | SwitchingApiClient 分支是否对称？本地模式是否绕过验证/业务规则/审计？ |
| 5 | 种子数据 | LocalWebApiSeedData vs IdentitySeedData 是否一致？ |
| 6 | 契约双套 | `Contracts/Api/*`（Refit）vs `Contracts/ApiClient/*`（Unified）——哪套真在用？合并方向？ |

建议至少走查 4 条真实业务链：**患者链 / 挂号接诊链 / 处方打印链 / 配置链**。

## 深度要求（强制）

1. **符号级验证**：必须用 MCP 工具（serena 符号引用 / codebase-memory 图谱 / codegraph 调用链）确认，grep 只做初筛。
2. **每个发现写推理链**：为什么判定是问题（判断依据，不是"看起来怪"）。
3. **追根溯源**：发现现象要追到根因层（如「有别名」追到「命名空间撞名」），不停在表面症状。
4. **顺带发现记录不丢**：审计过程中想到/看到的一切疑点，无论是否属于 7 类，都进报告附录，不许因"不在任务范围"丢弃。
5. **结合文档**：每类问题对照权威文档（08-shared / 03-server / 05-dual-mode / 13a/13b/13c…），标出「文档定义 vs 代码现状」偏差。文档是设计态，代码是当前态。

## 硬性约束

1. **只读审计**，禁止修改任何代码文件。
2. 每发现必须有证据：`文件:行号` + 引用链/调用方。
3. 严重度分级：P0（红线违反/高风险）/ P1（应修）/ P2（可后议）；顺带发现单列不评级。
4. 产出单一报告：`docs/03-architecture/structure-audit-mimo-2026-08-08.md`（独立分析者 #2 命名，与技术总监报告区分）。
5. **不要 commit**：审计为只读，报告文件写入 docs/ 后由技术总监统一提交。

## 明确不做（防发散）

- ❌ 不修任何 bug、不改任何代码
- ❌ 顺带发现**只记录不深挖**（深挖是修复阶段的事）
- ❌ 不评 UI/交互
- ❌ 不写修复方案细节（只写问题 + 建议方向）
- ❌ 不质疑 Dual-Mode 存废（双轨是设计意图，只审实现质量）

## 报告结构（structure-audit-mimo-2026-08-08.md）

```
# 结构审计报告（Mimo Code 独立分析）
## 0. 方法说明（工具/范围/时间点）
## 1. 项目依赖图 + 违规边
## 2. 按 7 类分节的问题清单（每项：证据 + 推理链 + 严重度 + 建议方向）
## 3. 数据流走查（4 条链 × 每层 4 问 + 双轨对称性 6 检查点）
## 4. 顺带发现附录（不评级）
## 5. 与文档偏差清单（文档 vs 代码）
## 6. 统计汇总
```
