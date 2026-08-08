# 任务 A-26：蓝图对齐 + 模式收敛 + 定义收敛（全面审查）

> 派发对象：Mimo Code 独立审查
> 版本：v1.0 | 日期：2026-08-08
> 用户决策：方案 A（蓝图对齐 + 模式收敛 + 定义收敛，全面）
> 技术总监将与另一份独立分析交叉验证，本任务书为派发基线。

## 任务

LYBTZYZS 全项目**收敛审查**（只读，禁止修改任何代码）：以 `docs/03-architecture/14-structure-design-blueprint.md`（v1.2，SSOT 设计态）为基准，核对代码当前态，统一设计模式、统一定义、统一设计，输出一份**收敛任务清单**（P0/P1/P2，每项带证据 + 统一方向建议）。

## 范围

- ✅ `src/Shared` + `src/Server` + `src/Client`（32 个项目）
- ❌ 排除：`tests/`、`docs/`、`src/Tools/`、`Migrations/`（迁移历史不算）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜最近提交 `fae082ae7`（文档规范收敛）

## 背景（必读）

- **蓝图已定稿**：`docs/03-architecture/14-structure-design-blueprint.md` v1.2 是全项目结构的唯一权威设计文档（SSOT）——每个 project 的职责、每个 class 的设计依据，都可在此追溯。文档定义**设计态**（系统应该是什么），代码实现**当前态**（系统现在是什么）。**本任务核心 = 逐模块核对「蓝图设计态 vs 代码当前态」偏差。**
- 已完成的收敛（可引用为已知，不重复审计，但**需核实是否真落地**）：
  - A-16 结构审计（交叉验证）：三层边界健康、双轨真共享
  - A-17 P0 本地 CRUD 断裂修复
  - A-18 P1 机制收敛：契约双套统一（IApiClient 唯一面）、领域客户端双实现收敛、CorrelationId 删 AsyncLocal 侧、手写 Mapper 改 Mapperly、Desktop 模块引用补架构测试
  - A-19 移除 Auto 模式（用户自主切换）
  - A-20 DbContext 全独立（Patients/MedicalCase/Registration 3 个新建，5 Repository 注入切换）
  - A-21 模块级审计 P1 修复（M2 Desktop 映射统一 Mapperly、M3 领域事件空转清理、M4 Shell 模块名 bug、M5 3 VM 越层改走 Service、C1 Infrastructure 职责过载、F-01 FeatureToggle 删）
  - A-23 LayerGuard 清理、A-24 CLevel 清理、A-25 遗留清理（项目名统一复数 + 样例中文化）
  - Q-02/Q-03 命名空间/using 收敛、Q-04 死代码清理
- 相关报告（可引用为已知）：`docs/compose/reports/structure-audit-*.md`、`docs/compose/reports/webapi-deep-analysis-*.md`、`docs/compose/reports/code-review-correctness-security-*.md`、`docs/compose/reports/dead-code-analysis-*.md`
- 架构约束参考：P07 模块间禁止直接引用 / P08 跨模块必须用接口 / P10 Service 禁注入 AppDbContext / DP07 Desktop 模块隔离 / MVVM VM 不直连 IApiClient（A-21 M5）

## 聚焦 3 大维度

### 维度 1：蓝图对齐（设计态 vs 当前态）

逐项目核对蓝图 §1（Shared 5 项目）/ §2（Server 10 项目）/ §3（Desktop 16 项目）/ §4（Tests 3 项目）的：
- **项目存在性**：蓝图列出的项目是否都存在？文件数是否匹配（蓝图给了文件数参考）？
- **职责一致性**：每个 project 的职责描述 vs 代码实际内容（有没有职责漂移、越权、空转）？
- **结构模式一致性**：蓝图 §2.4（Server 分层规则）vs 各模块实际目录结构；蓝图 §3.5（Desktop 分层规则）vs 各模块实际目录结构。
- **依赖规则**：蓝图 §0.3 的 8 条依赖规则，逐条验证代码是否遵守（架构测试已有守卫，找守卫没覆盖的例外）。
- **设计原则**：蓝图 §0.4 的 6 条原则（模块自治/契约单一/双轨设计/映射单一/共享单源/拒绝屎山）逐条找反例。

产出：**偏差清单**——每条 = 蓝图声明（引用蓝图位置）vs 代码实际（文件:行号）+ 偏差类型（蓝图过时需更新 / 代码偏离需修复 / 蓝图模糊需澄清）。

### 维度 2：模式收敛（统一设计模式）

同一类设计问题，找**多套模式并存**，输出「统一方向建议」：

| 模式族 | 检查点 | 已知候选（须核实 + 找新） |
|--------|--------|---------------------------|
| Controller 继承 | 三种继承路径是否清晰一致（BaseApiController / BaseCrudController / BaseMedicalCasesController）；每模块用哪种、为什么 | A-14 文档化过，核实是否所有 Controller 都走这三条路，有无裸 Controller |
| Service/Handler 模式 | 各模块是 CQRS（MediatR Handler）还是 Service 化（MedicalCase 例外）？边界是否清晰？同模块内是否混用 | MedicalCase 是唯一 Service 化模块（A-03/A-14 定案）；找其他模块内「既有 Handler 又有 Service」的混用 |
| 跨模块通信 | 是否全部走 ICrossModuleService 接口？有无模块直接引用（P07 违反） | IAuthCrossModuleService / IUserCrossModuleService 等 |
| 仓储模式 | Repository 是否泛型化统一？有无手写重复仓储 | A-06 泛型化后；A-20 后 Repository 注入自己模块 DbContext |
| 映射 | Mapperly 是否全覆盖？有无手写映射残留 | A-18 P1-4 / A-21 M2 后；Desktop DTO↔Model |
| DbContext | 模块 DbContext 是否都独立注入？有无复用 AppDbContext 的模块 Repository | A-20 后 5 独立 + 4 复用（Auth/Users/Herbs/Formula/Reports）——核实蓝图 §2.2 表格 |
| 批处理 | BatchOperationHandlerBase + 各 Handler 是否统一 | Q-01 泛型化后 |
| 验证 | FluentValidation 管道是否统一入口 | ValidationBehavior 2026-08-06 补 |
| 异常 | 异常→HTTP 映射是否单点（SystemExceptionHandler）？有无绕过直接返回状态码 | 2026-08-08 对齐批次后 |
| 响应信封 | ApiResponse<T> 是否所有 Controller 都包？有无裸返回 | 契约强制 |

### 维度 3：定义收敛（统一定义）

同一事物多种叫法/定义，输出「统一定义建议」：

| 定义族 | 检查点 | 已知候选（须核实 + 找新） |
|--------|--------|---------------------------|
| 命名空间 | 模块/目录/项目名单复数是否统一（Q-03 后） | LYBT.Module.* / LYBT.Desktop.* 复数化；查残留单数 |
| 类型命名 | 同类事物命名惯例（Service/Manager/Handler/Helper/Provider 混用）、后缀规范（Dto/Request/Response/Command/Query） | 列出所有尾部不一致 |
| DTO/契约 | 同一概念是否多处定义（双套残留）？Contracts/ApiClient 是否唯一 | A-18 后；查 Contracts/Api/*（Refit 特性）是否真删净 |
| 枚举/常量 | 共享单源（样板 Gender）——查双端重复定义、魔法数字 | Shared/Models/Enums 单源 |
| 接口命名 | IXxxService / IXxxRepository / ICrossModuleService 前缀惯例 | 全仓接口命名 |
| 错误码/异常 | ErrorCode 枚举 SSOT；AppException 层次统一 | 06-error-handling |
| 状态/术语 | 业务术语（Consultation/MedicalCase/Registration/Prescription）使用是否一致 | 术语表 docs/01-product/03-glossary.md |
| 配置文件 | appsettings 键命名、环境分层是否一致 | 07-configuration |

## 深度要求（强制）

1. **符号级验证**：必须用 MCP 工具（serena 符号引用 / codebase-memory 图谱 / codegraph 调用链）确认，grep 只做初筛。
2. **每个发现写推理链**：为什么判定是问题（判断依据，不是"看起来怪"）。
3. **追根溯源**：发现现象要追到根因层（如「有别名」追到「命名空间撞名」），不停在表面症状。
4. **结合蓝图**：每类问题对照蓝图对应章节，标出「蓝图定义 vs 代码现状」偏差。
5. **收敛建议必须具体**：每条建议给出统一方向（统一成什么、以哪个为准、为什么），不写"建议统一"这种空话。

## 硬性约束

1. **只读审查**，禁止修改任何代码文件。
2. 每发现必须有证据：`文件:行号` + 引用链/调用方。
3. 严重度分级：P0（红线违反/高风险/设计态与当前态严重背离）/ P1（应修，模式/定义不一致）/ P2（可后议）；顺带发现单列不评级。
4. 产出单一报告：`docs/compose/reports/structure-convergence-mimo-2026-08-08.md`（独立分析者 #2 命名，与技术总监报告区分）。
5. **不要 commit**：审查为只读，报告文件写入 docs/ 后由技术总监统一提交。

## 明确不做（防发散）

- ❌ 不修任何 bug、不改任何代码
- ❌ 顺带发现**只记录不深挖**（深挖是修复阶段的事）
- ❌ 不评 UI/交互
- ❌ 不写修复方案细节（只写问题 + 统一方向建议）
- ❌ 不质疑已定案的架构决策（双轨、模块自治、MedicalCase Service 化、用户自主切换等是已拍板设计，只审落地质量）

## 报告结构（structure-convergence-mimo-2026-08-08.md）

```
# 收敛审查报告（Mimo Code 独立分析）
## 0. 方法说明（工具/范围/时间点/基线 commit）
## 1. 蓝图对齐偏差清单（维度 1）——每项：蓝图位置 + 代码实际 + 偏差类型 + 严重度
## 2. 模式收敛清单（维度 2）——每项：模式族 + 存活双方证据 + 统一方向建议 + 严重度
## 3. 定义收敛清单（维度 3）——每项：定义族 + 冲突证据 + 统一定义建议 + 严重度
## 4. 收敛任务清单汇总（按 P0/P1/P2 排序，可直接派发的任务粒度）
## 5. 顺带发现附录（不评级）
## 6. 统计汇总（蓝图核对率 / 发现数 / 建议数）
```
