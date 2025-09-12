# Claude Code CLI 通用引导（仅需首次加载）

Purpose
- 为本仓库的所有任务提供统一的执行与报告要求。

Scope
- 适用于 `D:\source\LYBTZYZS` 仓库内所有 `prompts\T-*.prompt.md` 任务。

Working Directory
- 默认工作目录：`D:\source\LYBTZYZS`。
- 若任务文件内另有指定，以任务文件为准。

Global Rules
- 严格遵循仓库 `AGENTS.md`（构建、风格、命名、脚本优先）。
- 优先使用仓库脚本：`scripts\\build.bat`、`scripts\\build-check.bat 3`、`scripts\\run-smoke-tests.bat`。
- 构建/运行环境信息需在报告中提供（`dotnet --info`、OS 概要）。
- 日志最小披露：聚焦错误/告警摘要；避免泄露密钥或完整连接串。
- 遇到阻碍（SDK/证书/权限/端口占用等）时，尽快在报告中量化与定位。

Reports
- 报告保存到任务中指定的 `Report-Path`；默认目录：`D:\source\LYBTZYZS\.claudereports`。
- 命名：`R-<序号>-<主题>.md`（与任务文件保持同一 `<序号>` 与 `<主题>`）。
- 报告内容遵循任务文件的 `Required Artifacts` 与 `Report Outline`。

Execution Preferences
- 首选 Debug 配置：`dotnet build <*.sln> -c Debug`。
- StyleCop 告警目标为 0；若无法达成，请统计并提出消除计划与优先级。
- HTTPS 证书按需使用：`scripts\\generate-ssl-certificates.bat`。
- 长时进程（如 API）可在独立终端后台运行；完成后终止。

Failure Handling
- 若某步骤失败：
  - 记录关键错误片段与返回码；
  - 进行一次合理的回退/替代尝试（例如脚本失败则改用等效 `dotnet` 命令）；
  - 在报告“结论/建议”中给出下一步方案与预计成本。

Output Style
- 中文为主；使用清晰的小节与要点列表。
- 统计口径清晰（错误/告警/通过率等提供数量与样例）。

Usage
- 第一次使用本仓库工作流时，先在 Claude Code CLI 中加载本文件；
- 之后直接按需加载具体任务文件（`T-*.prompt.md`）。
