# 任务：服务器端测试架构落地纠偏

- **发布日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 背景
server-testing-architecture-completion-report.md 声称测试架构全面完成并达到 100% 覆盖。然而实际验证发现：
- 新增的 ServerIntegrationTests 无法还原/编译（dotnet test 出现 NU1605 包降级错误、NU1504 重复引用、SQL Server 配置未生效）。
- ppsettings.Test.json 仍默认连接 LocalDB，与“强制使用 SQL Server”要求不符。
- 多个 Microsoft.Extensions.* 包解析到 8.0.0，存在安全告警且与 EF Core 8.0.17 要求不一致。
- 覆盖率脚本尚不能成功产出报告。

需要针对上述差异制定纠偏计划，使测试架构真正落地并可持续运行。

## 目标
1. 解决服务器端测试项目的包版本冲突与还原失败问题。
2. 落实 SQL Server 非 LocalDB 的测试环境配置，支持本地/CI 运行。
3. 确保覆盖率脚本可执行并生成报告，接入 CI 验证。
4. 更新测试架构文档，使其与真实实现同步。

## 工作内容
1. **包版本与项目配置修复**
   - 清理 	ests/IntegrationTests/ServerIntegrationTests 中重复的 coverlet.collector 引用。
   - 在 Directory.Packages.props 中统一 Microsoft.Extensions.* 系列版本，使之满足 EF Core 8.0.17 依赖（至少 8.0.2）。
   - 处理 System.Text.Json 8.0.0 等安全漏洞告警。
   - 修复 LYBT.Tests.Configuration.csproj、LYBT.ServerIntegrationTests.csproj 的引用顺序，确保 dotnet restore/test 可执行。

2. **SQL Server 测试环境落地**
   - 为 ppsettings.Test.json 提供真实 SQL Server 连接示例（可使用本地 Docker SQL Server 容器或可控实例）。
   - 在测试基类中增加连接验证和失败提示。
   - 更新 README/测试文档，说明如何启动数据库、设置连接字符串。

3. **测试执行与覆盖率验证**
   - 运行并通过 dotnet test（Integration、Unit）确保无错误。
   - 更新 GenerateCoverageReport.ps1/VerifyCoverage.ps1，确认脚本能在本地生成报告并返回正确退出码。
   - 记录覆盖率结果与剩余差距。

4. **文档同步**
   - 根据实际情况更新 server-testing-architecture-completion-report.md，标注“已完成/进行中/待办”。
   - 在 README 的“测试现状”章节补充服务器端最新进度。

## 交付物
- 修复后的测试项目 csproj、Directory.Packages.props、ppsettings.Test.json。
- 可执行的 dotnet test 与覆盖率脚本（附运行截图或日志）。
- 更新后的测试架构文档、README 调整说明。
- 若无法一次完成，提供后续拆分任务清单。

## 验收标准
- dotnet test tests/IntegrationTests/ServerIntegrationTests -c Release 与相关单测命令无错误。
- SQL Server 测试环境配置可直接按文档复现；不再依赖 LocalDB。
- 覆盖率脚本生成 HTML 报告，并输出覆盖率摘要。
- 文档中不再出现与实际不符的“已完成/100%覆盖”描述。

> 完成后请在 docs/tasks/completed/2025-09-24-server-testing-架构纠偏-summary.md 中总结改动、测试记录与后续建议。
