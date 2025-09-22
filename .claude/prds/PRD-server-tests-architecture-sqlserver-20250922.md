name: PRD-server-tests-architecture-sqlserver-20250922
status: backlog
description: 完成后端测试架构，SQL Server 实体后端（连接串来自配置文件），全模块方法覆盖达成，服务器端收口

# PRD——后端测试架构完成与全模块覆盖（SQL Server 实体后端 · Phase 2.2）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.Server.sln`、`src/Server/*`、`tests/*`、`BIN/TestResults/*`

## 需求（Requirements）
- SQL Server 实体后端，连接字符串从配置文件读取（例如 appsettings.Test.json 的 ConnectionStrings:Default；拒绝 LocalDB）。
- 覆盖服务器端所有模块，公开方法全部被测试执行一次以上；Controllers 端点均有正例覆盖。
- 报告产物：Cobertura+HTML 至 `BIN/TestResults/coverage/`；方法覆盖（Methods）= 100%。

## 验收（Acceptance）
```bash
# 启动 SQL Server（示例：Docker）
$Env:SA_PASSWORD="<StrongPass>"
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=$Env:SA_PASSWORD" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# 在配置文件中设置连接字符串（非 LocalDB）
# 编辑 src/Server/Services/LYBT.WebAPI/appsettings.Test.json 的 ConnectionStrings:Default 指向上面的 SQL Server。

# 构建 + 测试 + 覆盖率
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release --no-restore
dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura
```
