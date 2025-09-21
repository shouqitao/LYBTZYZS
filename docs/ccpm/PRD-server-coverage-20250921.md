# PRD｜服务端单元测试全覆盖与测试报告（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 文档日期：2025-09-21
- 负责人：后端负责人（待指派）
- 相关代码：`LYBT.Server.sln`（`src/Server/*`、`tests/*`）

## 背景与问题
- 现有测试覆盖分散且不均衡，部分关键路径（认证、处方、病历状态机、EF 配置）缺少系统化用例。
- 覆盖率采集和报告生成未形成标准化产物，团队无法快速评估质量红线。
- 架构门禁（Architecture Tests）已具备，但功能/边界/回归用例仍需补齐与量化。

## 目标（Goals）
- 单元测试覆盖服务端核心模块，达到“可发布级”的覆盖要求，并在 CI 中强制执行。
- 标准化覆盖率采集和报告生成（Cobertura + HTML），产出固定、可追溯。
- 用例体系覆盖：公共 API、边界条件、回归路径、错误与异常处理、并发与事务场景（可模拟）。

## 非目标（Non-Goals）
- 不对业务功能进行改造或新增（仅为可测试性必要的无侵入改动可纳入后续提案）。
- 不覆盖桌面端（WPF）测试；不覆盖性能/压力/端到端 UI 自动化（可在后续议题规划）。

## 用户与场景
- 开发与代码审查：提交变更后，能立刻看到覆盖率与断言质量，阻止风险代码合入。
- 测试负责人：可一键生成总览报告，定位薄弱模块与回归风险。
- 管理者：通过覆盖红线与报告链接，快速判断可发布性。

## 范围与边界
- 范围：`src/Server/*`（WebAPI、Modules、Infrastructure、Entities）及其在 `tests/*` 中的对应测试项目。
- 依赖：xUnit、FluentAssertions、Moq、Verify（快照）、Coverlet、ReportGenerator（本地或 CI 工具）。
- 环境：本地 `dotnet` 8，CI（待接入）；数据库模拟优先使用 SQLite In-Memory（更贴近关系型语义），或 EF InMemory（非关系型场景）。

## 成功指标（可量化）
- 线覆盖（Line）：整体≥90%，关键模块（Auth/Users/Prescriptions/MedicalCase）≥95%。
- 分支覆盖（Branch）：整体≥80%。
- 架构门禁（ArchTests）：100% 通过。
- 报告产物：HTML 与 Cobertura 文件可在本地与 CI 中稳定生成并归档。

## 验收标准（Acceptance Criteria）
- 执行 `dotnet test tests -c Release --no-build` 全部通过。
- 执行覆盖命令后在固定目录输出 HTML 与 Cobertura 报告（例如：`BIN/TestResults/coverage`）。
- CI 阶段若覆盖率低于阈值或测试失败，任务失败并给出具体模块与用例明细。

## 主要风险
- EF Core InMemory 与真实 SQL 语义差异（建议关键路径使用 SQLite In-Memory）。
- 并发与时间相关（非确定性）测试需注入时钟/随机源，避免脆弱性。
- 遗留代码存在难以隔离的耦合点，需通过接口/工厂抽象解藕（非功能性微调）。

## 里程碑
- M1：覆盖工具与报告通路打通，新增基础示例（2 天）。
- M2：模块级用例补齐至目标阈值（5–7 天，可并行）。
- M3：CI 红线与报告归档生效（1 天）。

---

# 附：用例覆盖矩阵（样例）
- Auth：登录/登出/刷新/令牌校验/锁定策略/异常路径
- Users：启禁用/重置密码/口令策略/个人资料/分页筛选
- Patients：创建/更新/查询/边界校验
- Herbs/Formula：映射与计算、价格精度、状态机
- MedicalCase/Consultation/Prescription：一对一关系、状态流转、并发控制（RowVersion）
- Infrastructure：配置绑定、缓存适配、日志与 DB 初始化
