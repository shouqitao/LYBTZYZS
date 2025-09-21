# Repository Guidelines

## 项目结构与模块组织
- 明确项目结构：`src/Server`、`src/Client/Desktop`、`src/Shared`、`tests`、`docs`、`scripts`；构建产物统一输出 `BIN/`。
- 源码位于 `src/`：`Server`（Web API）、`Client/Desktop`（WPF）、`Shared`（DTO/接口/工具）。
- 测试在 `tests/`（单元/集成/架构），文档在 `docs/`，脚本在 `scripts/`。
- 统一产物输出到根目录 `BIN/`（见 `Directory.Build.props`）；SDK 固定在 `global.json`。

## 构建、测试与本地运行
- 还原：`dotnet restore LYBT.All.sln`
- 构建：`dotnet build LYBT.All.sln -c Release --no-restore`
- 运行 API：`dotnet run --project src/Server/Services/LYBT.WebAPI`
- 格式化：`dotnet format LYBT.All.sln`（遵循 `.editorconfig`）
- 单元测试：`dotnet test tests -c Release --no-build`
- 架构测试：`dotnet test tests/Architecture/LYBT.ArchTests.csproj`
- 覆盖率：`dotnet test tests -c Release --collect:"XPlat Code Coverage"`

## 代码风格与命名约定
- 缩进：C# 4 空格；XML/JSON/YAML 2 空格；UTF‑8、CRLF、去除行尾空白。
- using：`System.*` 优先，`using` 放在命名空间外，单行块尽量保持。
- 花括号/换行：左花括号换行；`else/catch/finally` 前换行。
- 命名：类型与非字段成员 PascalCase；接口前缀 `I`；私有字段 `_camelCase`；异步方法以 `Async` 结尾。
- 分析器：启用 StyleCop.Analyzers；修复警告或给出充分理由的抑制。

## 测试指南
- 框架：xUnit、FluentAssertions、Moq、Verify（快照）、NetArchTest；Coverlet 采集覆盖率。
- 位置/命名：放在 `tests/`，文件以 `*Tests.cs` 结尾；覆盖公共 API、边界与回归路径。
- 要求：所有测试必须通过；合并前必须通过架构测试；CI 收集覆盖率（无硬性阈值）。

## 提交与 Pull Request 规范
- 提交：Conventional Commits（例：`feat(patients): add basic CRUD`），变更原子且聚焦。
- PR：描述清晰、关联 Issue、附测试与文档更新；WPF/UI 变更附截图。
- 合规：遵守 Record‑Only 基线与 `/api/v1/*` 路由，不引入禁用框架；门禁见 `CONTRIBUTING.md`。

## 安全与配置提示
- 依赖版本集中在 `Directory.Packages.props`；勿提交密钥，使用本地 `appsettings.Development.json` 或环境变量。
- 优先使用 EF Core 隐式事务；显式事务保持最小化与小范围。
