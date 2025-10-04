# PRD——后端单元测试全覆盖与报告交付（SQL Server 实体后端 · Phase 2.1）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.Server.sln`、`tests/*`、`BIN/TestResults/*`

## 背景（Problem & Context）
- 当前进入完整开发阶段，无生产数据顾虑，期望在集成测试中直接使用 SQL Server 以获得与生产一致的关系型行为（外键、事务、并发、约束等）。
- 目标是在“可编译已达成”的基础上，交付“后端测试全覆盖 + 报告归档”，并以 SQL Server 为集成测试后端。

## 目标（Goals）
- 覆盖率目标：
  - Line Coverage ≥ 90%（总体）；关键模块（Auth/Users/Prescriptions/MedicalCase）≥ 95%
  - Branch Coverage ≥ 80%（总体，建议值）
- 架构测试：100% 通过
- 实体后端：所有集成测试使用 SQL Server（开发机 LocalDB/Developer，CI 使用容器化 SQL Server）
- 产物：Cobertura XML + HTML 报告归档于 `BIN/TestResults/coverage/`

## 非目标（Non-Goals）
- 不使用 EF InMemory 或 Sqlite In-Memory 作为主路径
- 不做领域大规模重构；必要时仅做测试可达性的轻微解耦

## 需求（Requirements）
- R1 测试数据库供应（开发机）
  - 推荐 LocalDB（Windows）：`(localdb)\MSSQLLocalDB`
  - 连接串建议（环境变量注入）：
    - `TEST_SQLSERVER_CONNSTR="Server=(localdb)\MSSQLLocalDB;Database=LYBT_Test_%RANDOM%;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"`
  - 每次运行采用“唯一库名”（时间戳/Guid/RANDOM），测试开始迁移建库，运行后删除或留存供排查
- R2 测试数据库供应（CI）
  - 使用 Testcontainers for .NET（优先）或 GitHub Actions 服务容器拉起 `mcr.microsoft.com/mssql/server:2022-latest`
  - 示例环境：
    - `SA_PASSWORD`：强密码（Secrets）
    - 连接串：`Server=localhost,<port>;User Id=sa;Password=${{ secrets.SA_PASSWORD }};TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`
- R3 迁移与重置策略
  - 测试启动前对目标库执行 `db.Database.Migrate()`（或 CLI `dotnet ef database update` 指向测试库）
  - 套件/用例级隔离：首选“按库隔离”（唯一 DB 名称）；如需更快，可考虑使用 Respawn 清空数据
- R4 数据准备与断言
  - 随机数据：Bogus 生成；必要处最小种子
  - 断言：FluentAssertions；异常/边界可选 Verify 快照
- R5 覆盖率与报告
  - 命令：
    ```bash
    dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
    reportgenerator \
      -reports:BIN/TestResults/**/coverage.cobertura.xml \
      -targetdir:BIN/TestResults/coverage \
      -reporttypes:Html;Cobertura
    ```
  - 报告产物：`BIN/TestResults/**/coverage.cobertura.xml`、`BIN/TestResults/coverage/index.html`
- R6 文档与可复现
  - 在 `docs/testing/README.md` 与 `docs/runbook.md` 增补“SQL Server 测试后端与覆盖率归档”指令
  - CI 工作流可选新增步骤：拉起 SQL Server、设置 `TEST_SQLSERVER_CONNSTR`、执行测试与报告生成

## 成功度量（Success Metrics）
- SQL Server 集成测试稳定可跑；本地/CI 一致
- 线路覆盖率 ≥ 90%，关键模块 ≥ 95%；分支覆盖 ≥ 80%
- ArchTests 全通过；Cobertura+HTML 报告稳定生成

## 验收标准（Acceptance Criteria）
```bash
# 1) 还原/构建
setx TEST_SQLSERVER_CONNSTR "Server=(localdb)\MSSQLLocalDB;Database=LYBT_Test_%RANDOM%;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release --no-restore

# 2) 全量测试 + 覆盖率（使用 TEST_SQLSERVER_CONNSTR）
dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings

# 3) 报告生成
reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura

# 4) 产物位置
# Cobertura: BIN/TestResults/**/coverage.cobertura.xml
# HTML:      BIN/TestResults/coverage/index.html
```

## 里程碑（Milestones）
- 提交 1：为各模块补足测试骨架，串联 TEST_SQLSERVER_CONNSTR，迁移建库流程达成
- 提交 2：关键模块覆盖率达标（≥ 95%）；其余模块补齐至总体 Line ≥ 90%
- 提交 3：生成并归档覆盖率报告；提交总结与后续建议

## 风险与缓解（Risks & Mitigations）
- 开发机未装 SQL Server/LocalDB → 提供安装指引或改用 Developer/容器
- 证书/加密导致连接失败 → 使用 `TrustServerCertificate=True;Encrypt=False`（仅测试环境）
- 数据冲突/脏数据 → 唯一库名 + 按库隔离；必要时运行后清理

## 测试计划（Testing）
- 单元/集成：xUnit + FluentAssertions + Moq + SQL Server（LocalDB/容器）
- 架构：NetArchTest
- 报告：Coverlet + ReportGenerator（Cobertura + HTML）

## 交付物（Deliverables）
- 覆盖率达标的测试用例
- 归档的 Cobertura XML + HTML 报告（`BIN/TestResults/coverage/`）
- “后端测试覆盖率报告（2025-09-22）”与“任务总结”（`docs/reports/`、`docs/prds-summary/`）

