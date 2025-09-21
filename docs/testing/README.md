# 测试与覆盖率

## 命令
```bash
# 单元测试（解决方案下 tests/ 全量）
dotnet test tests -c Release --no-build --settings .runsettings

# 架构测试（NetArchTest）
dotnet test tests/Architecture/LYBT.ArchTests.csproj

# 覆盖率（XPlat Code Coverage）
dotnet test tests -c Release --collect:"XPlat Code Coverage" --settings .runsettings
```

## 范围与约定
- 单测：覆盖核心业务逻辑与公共 API
- 集成：WebAPI 关键用例（可选，本仓示例化）
- 架构：命名/分层/依赖边界检查（tests/Architecture）
- 输出：测试与覆盖率产物统一输出至 `BIN/TestResults/`（由 `.runsettings` 与 `tests/Directory.Build.targets` 共同约定），按 `.gitignore` 不提交。

## CI 工件
- GitHub Actions 工件包含：
  - 统一覆盖率报告：`BIN/TestResults/CoverageReport/`
  - 原始结果与日志：`BIN/TestResults/**/*.trx`、`BIN/TestResults/**/coverage.*.xml`
  - 相关工作流：`.github/workflows/coverage-check.yml`、`test-coverage.yml`、`test.yml`、`ci.yml`

## 参考与报告
- 覆盖率说明: tests/COVERAGE.md
- 扩展测试脚本与报告: tests/TestCoverageReport_Final.md、tests/FinalTestCoverageReport.md
- API 测试（Postman）: tests/api/

