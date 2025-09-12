# 任务提示词模板（复制本文件并替换占位符）

- Task-ID: T-<序号>
- Title: <简要标题>
- Priority: <High|Medium|Low>
- Repo-Root-Path: D:\source\LYBTZYZS
- Report-Path: D:\source\LYBTZYZS\.claudereports\R-<序号>-<主题>.md
- Owner: Claude Code CLI 执行者
- Deadline: <可选>
- Tools: Shell, Git, Dotnet, Python（按需）
## Meta Rules
- Working Directory: `D:\source\LYBTZYZS`（任务内未另行指定时默认）
- Scripts First: 优先使用仓库脚本（`scripts\\build.bat`、`scripts\\build-check.bat 3`、`scripts\\run-smoke-tests.bat`）。
- Reports: 报告保存到任务 `Report-Path`（默认 `D:\source\LYBTZYZS\.claudereports`），命名与任务序号/主题对应。
- StyleCop: 目标 0 告警；无法达成时请量化、分组并提出消除计划与优先级。
- Secrets: 禁止泄露任何密钥/连接串；配置走 `appsettings.Development.json` + 环境变量。
- Logs: 只收录必要的错误/告警摘要；必要时对敏感信息打码。
- Failure Handling: 记录返回码与关键错误片段；可进行一次合理替代尝试；在结论中给出后续方案与预计成本。
- Output Style: 中文为主；使用清晰小节与要点列表；统计口径清晰（数量+样例）。
- Long-running: API 等长时进程可在独立终端运行；完成后终止。
- Build Pref: 默认使用 Debug 配置：`dotnet build <*.sln> -c Debug`。## Context
<提供必要背景/代码位置/已知限制>

## Objective
<明确目标，可度量的完成标准>

## Constraints
- 遵循 AGENTS.md 与仓库脚本约定
- StyleCop 无告警（如无法达成需在报告中说明原因与数量）
- 禁止提交密钥；配置走本地 Development 配置与环境变量

## Acceptance Criteria
- <列出验证点，例如构建成功、冒烟通过、日志无严重错误等>

## Steps
1) <步骤1：包含要执行的命令、路径、注意事项>
2) <步骤2>
3) <步骤3>

## Required Artifacts in Report
- 执行环境：`dotnet --info`、OS 信息简述
- 构建结果：关键日志摘要，错误/告警数量（含 StyleCop）
- 测试/冒烟结果：通过/失败统计与关键失败样本
- 结论：是否满足验收标准；主要风险与建议
- 附录：必要的命令输出（截断到关键部分）

## Report Outline
1. 概述（任务、时间、提交/分支、执行人）
2. 环境与前置条件
3. 执行步骤与结果（逐步小结）
4. 核心结论与建议（下一步方向）
5. 附录（命令与日志片段）
