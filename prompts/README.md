# Prompts 目录

此目录用于存放“Claude Code CLI”可直接调用的任务提示词文件。整体工作流：

1) 我（AI）在此处生成任务提示词文件（T-序号-主题.prompt.md）。
2) 你在 Claude Code CLI 中加载并执行该提示词，完成任务并产出报告。
3) 报告请保存到仓库根目录下的 `.claudereports` 目录，按规范命名（R-序号-主题.md）。
4) 我会读取报告，分析结果并生成下一个任务。

命名规范：
- 任务文件：`T-<序号>-<主题>.prompt.md`（例如：`T-0001-initial-build-and-smoke.prompt.md`）
- 报告文件：`R-<序号>-<主题>.md`（例如：`R-0001-initial-build-and-smoke.md`）
- 报告目录：`D:\source\LYBTZYZS\.claudereports`（已存在）

Meta 规则（已内置于每个任务文件开头）：
- 默认工作目录：`D:\source\LYBTZYZS`（任务未另行指定时）
- 脚本优先：`scripts\\build.bat`、`scripts\\build-check.bat 3`、`scripts\\run-smoke-tests.bat`
- 报告：保存至任务指定的 Report-Path，命名与任务序号/主题一致
- StyleCop：目标 0 告警；无法达成需量化并附计划
- 保密：禁止泄露密钥/连接串；使用 Development 配置 + 环境变量
- 日志：只收录必要摘要；敏感信息打码
- 失败处理：记录返回码+错误片段，做一次合理替代尝试，并给出后续方案

约定与要求：
- 严格遵循仓库 AGENTS.md 中的构建与风格要求（StyleCop 零告警、提交前执行脚本等）。
- 执行脚本和命令时尽量使用仓库提供的脚本（如 `scripts\\build-check.bat`）。
- 如果任务需要长时间运行的进程（如 API），可在单独终端中启动并在完成后终止。

执行方法（示例）：
- 在 Claude Code CLI 中加载某个 `T-xxxx-*.prompt.md` 文件，按其中步骤执行；
- 生成报告并保存为对应的 `R-xxxx-*.md` 到 `.claudereports` 目录；
- 通知我继续下一步分析与任务生成。
