# PRD｜服务端单元测试全覆盖与测试报告（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 文档日期：2025-09-21
- 负责人：待指派（后端负责人）
- 相关范围：`LYBT.Server.sln`（`src/Server/*`、`tests/*`）

## 1. 背景与动机
- 现有测试覆盖分散且不均衡，部分关键路径（认证、处方、病历状态机、EF 配置）缺少系统化用例。
- 缺少标准化覆盖率与报告产物，团队难以及时评估质量红线与回归风险。
- 架构门禁（Architecture Tests）已有，但功能/边界/回归用例需补齐并量化纳入 CI 门禁。

## 2. 目标（Goals）
以“可发布级”为标准，建立稳定、可量化的服务端单元测试体系：
- G1：核心模块（Auth/Users/Patients/Herbs/Formula/Consultation/MedicalCase/Prescriptions、Infrastructure）单元测试覆盖到位。
- G2：覆盖率采集与报告生成标准化（Cobertura+HTML），产物固定在统一目录，CI 可消费。
- G3：在 CI 中启用覆盖率阈值门禁（低于阈值失败），形成持续质量红线。

## 3. 非目标（Non-Goals）
- 不新增业务功能；不对既有业务流程做破坏性调整（仅为可测试性必要的微重构另行提案）。
- 不覆盖桌面端（WPF）测试；不覆盖性能/压力/端到端 UI 自动化。

## 4. 使用者与关键场景
- 开发者：提交代码后，快速看到覆盖率与断言质量，阻止风险代码合入。
- 测试负责人：一键生成总览报告，定位薄弱模块与回归风险。
- 管理者：通过覆盖率红线与报告链接，判断“是否可发布”。

## 5. 需求范围（Scope）
- 范围内：`src/Server/*` 模块与其在 `tests/*` 中对应的单元/集成测试；覆盖公共 API、边界条件、回归路径、异常/错误映射、并发/事务（可模拟）。
- 范围外：桌面端（WPF）；性能/压力/端到端 UI 自动化；大规模架构改造。

## 6. 技术与运维约束
- 语言/框架：.NET 8；xUnit、FluentAssertions、Moq、Verify（快照）。
- 覆盖工具：Coverlet（XPlat Code Coverage）+ ReportGenerator。
- 数据库策略：
  - 关系型语义优先 SQLite In-Memory + 迁移（支持唯一索引/外键/过滤索引等）。
  - 纯内存逻辑可用 EF InMemory（速度更快）。
- 安全/配置：不得引入真实密钥；使用测试配置或注入替身（IOptions、IClock 等）。

## 7. 成功指标（可量化）
- Line Coverage（整体）：≥ 90%
- Line Coverage（关键模块 Auth/Users/MedicalCase/Prescriptions）：≥ 95%
- Branch Coverage（整体）：≥ 80%
- 架构门禁（ArchTests）：100% 通过
- 报告产物：Cobertura + HTML 报告稳定生成并归档

## 8. 验收标准（Acceptance Criteria）
- AC1：本地执行 `dotnet test tests -c Release --no-build` 全部通过。
- AC2：执行覆盖命令后在固定目录输出 HTML 与 Cobertura 报告，例如：`BIN/TestResults/coverage/index.html` 与 `coverage.cobertura.xml`。
- AC3：CI 阶段低于阈值（Line<90% 或 Branch<80%）则失败，并展示具体模块明细/报告链接。
- AC4：关键路径（认证、用户启禁/重置密码、病历/问诊/处方一对一关系、并发 RowVersion、价格精度、异常映射）均有用例覆盖。

## 9. 里程碑与交付
- M1（工具与通路，2 天）：打通 Coverlet + ReportGenerator，固化输出目录；样例报告可打开。
- M2（模块补齐，5–7 天）：各模块服务层/映射/异常路径用例补齐，达成覆盖指标。
- M3（CI 红线，1 天）：接入覆盖阈值门禁并归档报告，失败时阻断合并。

## 10. 风险与缓解
- R1：SQLite 与 SQL Server 语义差异 → 关键约束增补集成测试/容器化 SQL Server 验证。
- R2：非确定性（时间/随机/并发） → 注入 IClock/RandomProvider；增设超时/重试策略。
- R3：遗留耦合导致难以隔离 → 倾向接口隔离与工厂注入（微重构），与业务无关。

## 11. 验证步骤（本地示例）
```bash
# 构建
dotnet restore LYBT.Server.sln

# 运行测试并收集覆盖率（示例）
dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults

# 生成报告（示例）
reportgenerator \
  -reports:BIN/TestResults/**/coverage.cobertura.xml \
  -targetdir:BIN/TestResults/coverage \
  -reporttypes:Html;Cobertura
```

## 12. 依赖与资源
- 工具：Coverlet、ReportGenerator（本地或 CI 容器镜像）
- 开发资源：后端与测试工程师若干，可按模块并行推进

## 13. 变更影响与回滚
- 属非功能性质量建设；仅新增测试与配置/脚本。
- 如出现 CI 阈值引发阻断，可临时降低阈值或分模块豁免（需审批，限定时效）。

## 14. 开放问题（待澄清）
- O1：覆盖阈值是否需要分模块细化（如 Auth/MedicalCase 达 95%，其余 90%）？
- O2：报告归档位置与保留周期（制品库/对象存储/内部门户）？
- O3：是否需要在发布说明中附带本次覆盖摘要表（模块/行覆盖/分支覆盖）？
