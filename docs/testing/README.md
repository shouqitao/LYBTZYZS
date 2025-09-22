# 测试与覆盖率（凌隐宝堂 / LYBT）

## 命令
```bash
# 全量单元/集成测试（tests/）
dotnet test tests -c Release --no-build --settings .runsettings

# 架构测试（NetArchTest）
dotnet test tests/Architecture/LYBT.ArchTests.csproj

# 覆盖率（XPlat Code Coverage）
dotnet test tests -c Release --collect:"XPlat Code Coverage" --settings .runsettings --results-directory BIN/TestResults
```

## 范围与约束
- 覆盖公共 API 与关键业务路径；禁止 UI 依赖渗透到服务端
- 架构测试覆盖分层/依赖/边界（`tests/Architecture`）
- 测试与覆盖率产物统一：`BIN/TestResults/`（与 `.runsettings`、`tests/Directory.Build.targets` 一致）

## CI 指南
- 统一覆盖率输出目录：`BIN/TestResults/CoverageReport/`
- 原始产物：`BIN/TestResults/**/*.trx`、`BIN/TestResults/**/coverage.*.xml`
- 相关工作流：`.github/workflows/coverage-check.yml`、`test-coverage.yml`、`test.yml`、`ci.yml`

## 参考报告
- 覆盖率说明：`tests/COVERAGE.md`
- 最终覆盖率报告：`tests/TestCoverageReport_Final.md`、`tests/FinalTestCoverageReport.md`
- API 测试（Postman）：`tests/api/`

## SQL Server 测试后端（Test 配置）
- 配置文件示例：`docs/development/appsettings.Test.sample.json`
- 启用步骤：
  1) 复制示例到 WebAPI 目录并改名为 `appsettings.Test.json`
     - 位置：`src/Server/Services/LYBT.WebAPI/appsettings.Test.json`
     - 修改 `ConnectionStrings:Default` 指向你的 SQL Server（非 LocalDB）
  2) 在集成测试宿主加载 Test 配置
     - 方式 A：将环境设置为 `Test`，让宿主自动加载 `appsettings.Test.json`
     - 方式 B：在 `WebApplicationFactory<Program>` 中显式添加配置源（`AddJsonFile("appsettings.Test.json", optional: false)`）
  3) 运行测试与生成报告：
     ```bash
     dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults --settings .runsettings
     reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura
     ```
