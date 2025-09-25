# PRD 实施汇总（凌隐宝堂中医诊所 / LYBTZYZS，2025-09-21）

本页聚合已完成 PRD 的关键结论与验收口径，作为交付核对清单。

## 关联 PRD
- Desktop 快速修复：`docs/reports/archive/ccpm/PRD-desktop-20250921.md`
- Server 测试覆盖：`docs/reports/archive/ccpm/PRD-server-coverage-20250921.md`

## 技术约束（统一）
- 输出：根目录 `BIN/`
- 路由：小写 `/api/v1/*`
- JSON：System.Text.Json（Refit 使用 SystemTextJsonContentSerializer）
- 依赖：集中于 `Directory.Packages.props`

## 交付口径
- 还原/构建：`dotnet restore LYBT.All.sln` → `dotnet build LYBT.All.sln -c Release --no-restore`
- 运行：WebAPI 可启动（`https://localhost:7001`）；Desktop 可选启动
- 测试：`dotnet test tests -c Release --no-build` 全量通过
- 覆盖率：`BIN/TestResults/**/coverage.*.xml` 产出完整，HTML 可选

## 关键整改（摘录）
- 桌面：引入 `Microsoft.Extensions.ObjectPool`，移除 `UseWindowsForms`，统一 XML 文档输出路径
- JSON 统一：清理 `Refit.Newtonsoft.Json` 依赖与配置
- 路由：统一小写端点（示例：`api/v1/users`）

以上内容与 PRD 同步维护；偏离需先更新 PRD 后实施。

