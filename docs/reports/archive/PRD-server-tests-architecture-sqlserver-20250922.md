# PRD——后端测试架构完成与全模块覆盖（SQL Server 实体后端 · Phase 2.2）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.Server.sln`、`src/Server/*`、`tests/*`、`BIN/TestResults/*`

## 背景（Problem & Context）
- 已实现“可编译 + 可运行交付（Phase 2）”。当前目标是“完成后端测试架构，并以 SQL Server 实体后端覆盖到服务器侧所有模块与方法”。
- 明确要求：
  - 测试后端一律使用 SQL Server（不使用 LocalDB）。
  - 服务器端所有业务模块均需纳入测试范围。
  - 最终达到“服务器端方法全部被测试执行成功”的交付口径（见验收标准的函数/方法覆盖指标）。

## 目标（Goals）
- 测试架构完善：单元（Unit）+ 集成（Integration）+ 架构（Architecture）三层测试架构收口，形成可复用基座（Fixture/Factory/数据供给策略）。
- 覆盖范围：服务器端全部模块（Auth/Users/Patients/Herbs/Formula/MedicalCase/Consultation/Prescriptions/Infrastructure）控制器、服务、仓储的公开方法全部被测试执行一次以上。
- 报告产物：Cobertura XML + HTML 报告稳定生成并归档 `BIN/TestResults/coverage/`。
- 交付收口：完成后冻结服务器端 API 与契约（除紧急修复外），转入下一阶段交付。

## 非目标（Non-Goals）
- 不涉及 UI/E2E 自动化与性能/安全渗透测试（后续 PRD 处理）。
- 不引入大规模重构；为测试可达性所需的轻微解耦可纳入。

## 范围（Scope）
- In Scope：服务器端所有模块（Controllers/Services/Repositories），中间件/配置/安全策略/异常映射，DbContext 迁移与约束校验。
- Out of Scope：桌面端测试、前端 UI、部署与打包。

## 需求（Requirements）
- R1 测试基座（SQL Server 实体后端，连接字符串来自配置文件）
  - 数据库：使用 SQL Server Developer 或 Docker 容器镜像 `mcr.microsoft.com/mssql/server:2022-latest`（拒绝 LocalDB）。
  - 连接字符串：从配置文件读取（例如 `src/Server/Services/LYBT.WebAPI/appsettings.Test.json` 的 `ConnectionStrings:Default`）。
  - WebApplicationFactory/TestServer 在测试环境加载上述配置文件（可使用环境名 `Test` 或显式添加配置源），不通过环境变量传入连接串。
  - Fixture 策略：
    - 每次测试运行使用“唯一数据库名”或“独立连接上下文”，启动前执行迁移 `db.Database.Migrate()`。
    - 用例结束可保留数据用于排查；如需清理，优先按库名 Drop 或使用 Respawn 清空。
- R2 覆盖所有模块
  - Controllers：每个控制器每个公开端点至少 1 条正例（2xx），必要的 4xx/401/403 负例覆盖。
  - Services/Repositories：每个公开方法至少 1 条正例用例；必要时补充异常/边界用例（空/极值/冲突/并发）。
  - Middleware/Config/Security：异常处理中间件统一格式；安全头/授权策略最小用例；配置缺省/非法的回退逻辑。
- R3 报告与指标
  - 采集：
    ```bash
    dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
    reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura
    ```
  - 指标（以 ReportGenerator/Cobertura 指标为准）：
    - 函数/方法覆盖（Methods）= 100%（服务器端程序集；公开方法全部被执行）
    - 行覆盖（Line）≥ 85%（总体）
    - 分支覆盖（Branch）≥ 70%（总体）
- R4 文档与可复现
  - 在 `docs/testing/README.md` 与 `docs/runbook.md` 增补“SQL Server 测试后端（配置文件连接串）与覆盖率归档”段落与故障排障（端口/证书/权限）。
  - 在 `docs/reports/` 输出“本次交付测试报告”并在 `docs/prds-summary/` 输出“任务总结”。

## 成功度量（Success Metrics）
- 使用 SQL Server 的全部集成用例稳定可跑（本地/CI 一致）。
- 方法覆盖（Methods）= 100%；Controllers 端点全部 2xx 正例可达；架构测试 100% 通过。
- 报告产物可在 `BIN/TestResults/coverage/index.html` 查看；Cobertura XML 存在。

## 验收标准（Acceptance Criteria）
```bash
# 1) 启动 SQL Server（示例：Docker 容器）
# Windows PowerShell
$Env:SA_PASSWORD="<StrongPass>"
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=$Env:SA_PASSWORD" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# 2) 在配置文件中设置连接字符串（非 LocalDB）
# 编辑 src/Server/Services/LYBT.WebAPI/appsettings.Test.json 的 ConnectionStrings:Default 指向上面的 SQL Server。

# 3) 还原/构建
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release --no-restore

# 4) 测试 + 覆盖率
dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura
```

## 里程碑（Milestones）
- 提交 1：搭建 SQL Server 测试基座（Fixture/Factory/迁移策略，配置文件连接串），所有模块纳入测试清单
- 提交 2：补齐 Controllers/Services/Repositories 全公开方法正例用例，完成方法覆盖 100%
- 提交 3：完善负例/边界/并发用例，生成并归档覆盖率报告；提交“测试报告 + 任务总结”
- 提交 4：服务器端收口（冻结 API 与契约；后续仅接收阻断性修复）

## 风险与缓解（Risks & Mitigations）
- SQL Server 环境准备复杂 → 提供 Docker/Developer 可选路径与脚本；权限/证书在测试连接串中放宽（仅测试环境）
- 并发/约束导致偶发失败 → 唯一库名隔离 + 严格迁移/清理策略；对并发冲突做显式断言
- 构建时间增长 → 并行优化测试顺序；区分 Unit/Integration 标签以便分层执行

## 测试计划（Testing）
- Unit：xUnit + FluentAssertions + Moq
- Integration（SQL Server）：WebApplicationFactory/TestServer + EF Core SqlServer + 迁移策略（连接串来自配置文件）
- Architecture：NetArchTest（命名/边界/禁用框架）
- 覆盖率与报告：XPlat Code Coverage + ReportGenerator（Cobertura + HTML）

## 交付物（Deliverables）
- 完整测试架构与基座（可复用）
- “方法覆盖 100%（服务器端公开方法）”与覆盖率报告（`BIN/TestResults/coverage/`）
- 测试报告与任务总结（`docs/reports/`、`docs/prds-summary/`）

