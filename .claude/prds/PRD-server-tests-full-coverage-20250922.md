name: PRD-server-tests-full-coverage-20250922
status: backlog
description: 后端单元测试全覆盖（使用 SQL Server 作为测试后端）

# PRD——后端单元测试全覆盖（SQL Server 实体后端）· Phase 2.1

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.Server.sln`、`tests/*`、`BIN/TestResults/*`

## 需求（Requirements）
- 使用 SQL Server 作为集成测试后端（开发机 LocalDB/Developer；CI 容器）
- 覆盖率目标：Line ≥ 90%（总体），关键模块 ≥ 95%；Branch ≥ 80%
- ArchTests 全通过；报告归档于 `BIN/TestResults/coverage/`

## 验收（Acceptance）
```bash
setx TEST_SQLSERVER_CONNSTR "Server=(localdb)\MSSQLLocalDB;Database=LYBT_Test_%RANDOM%;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release --no-restore
dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura
```
