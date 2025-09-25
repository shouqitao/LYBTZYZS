# 快速交付指南（技术/功能紧缩）

目标：在保证架构与质量门禁的前提下，以最小范围、最短路径交付。

## 范围与边界
- 仅在 PRD 既定范围内变更：`docs/reports/archive/ccpm/PRD-desktop-20250921.md`、`docs/reports/archive/ccpm/PRD-server-coverage-20250921.md`
- 路由/序列化/产物路径不得更改：`/api/v1/*`、System.Text.Json、`BIN/`
- 依赖变更必须通过 `Directory.Packages.props` 集中管理

## 最小可交付
- 可编译：`dotnet build LYBT.All.sln -c Release --no-restore`
- 可运行：WebAPI 可启动；Desktop 可选启动
- 可验证：核心测试通过；覆盖率产物输出到 `BIN/TestResults`

## 工作流（建议）
1. 建立变更清单（按 PRD R1–R3 列点）
2. 每个小点单独提交（Conventional Commits）
3. 本地验证：还原 → 构建 → 运行 → 测试/覆盖率
4. 更新文档：`docs/prds-summary/PRD-SUMMARY.md` 对应小节勾选完成

## 验收清单
- 构建无警告（CI 模式 TreatWarningsAsErrors=true）
- API 路由小写一致，OpenAPI 正常导出
- JSON 统一 System.Text.Json（Refit 使用 SystemTextJsonContentSerializer）
- 产物统一 `BIN/`；覆盖率与 TRX 存档齐全

## 非目标
- 不新增 UI/大改领域模型；不引入新框架
- 不修改 PRD 以外模块边界


