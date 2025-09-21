- Task-ID: T-0001
- Title: Initial build + smoke validation
- Priority: High
- Repo-Root-Path: D:\source\LYBTZYZS
- Report-Path: D:\source\LYBTZYZS\.claudereports\R-0001-initial-build-and-smoke.md
- Owner: Claude Code CLI 执行者
- Tools: Shell, Git, Dotnet, Python
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
仓库为 .NET 8（Web API + WPF），提供脚本与测试。目标是完成一次最小可用的本地构建与关键冒烟，形成基线报告。

## Objective
- 成功还原/构建全量解决方案（Debug 配置）。
- 尝试启动 API 并通过关键冒烟测试。
- StyleCop 告警为 0（若不为 0，请在报告中量化、分组并给出消除计划）。

## Constraints
- 遵循 AGENTS.md 规范与脚本优先策略。
- 禁止泄露密钥；使用 `appsettings.Development.json` + 环境变量管理配置。
- HTTPS 证书按需使用 `scripts\\generate-ssl-certificates.bat` 生成。

## Acceptance Criteria
- `dotnet build LYBT.All.sln -c Debug` 成功完成，返回码为 0。
- `scripts\\build-check.bat 3` 成功完成，无新增告警。
- API 可启动；`tests\\simple_api_test.py` 关键用例通过（如果无法启动，请在报告中说明阻碍并附带日志）。

## Steps
1) 记录环境信息：
   - 运行：`dotnet --info`，收集 OS/SDK 版本要点。

2) 还原并构建（全量）：
   - `dotnet restore`
   - `dotnet build LYBT.All.sln -c Debug`
   - 或使用：`scripts\\build.bat`
   - 快速检查：`scripts\\build-check.bat 3`

3) 启动 API（新终端或后台）：
   - 可选生成证书：`scripts\\generate-ssl-certificates.bat`
   - 启动：`dotnet run --project src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
   - 待 API 启动就绪后，在另一终端执行冒烟：
     - `scripts\\run-smoke-tests.bat` 或 `python tests\\simple_api_test.py`
   - 完成后终止 API 进程。

4) 汇总 StyleCop 告警：
   - 若存在，统计总数、Top 规则、涉及文件清单样本，并提出 2-3 条优先修复建议。

5) 生成报告并保存：
   - 路径：`D:\source\LYBTZYZS\.claudereports\\R-0001-initial-build-and-smoke.md`
   - 严格按照下方 Report 内容要求输出。

## Required Artifacts in Report
- `dotnet --info` 摘要（版本/Runtime/OS）
- 构建结果与关键日志摘要（含错误/告警计数、StyleCop 统计）
- 冒烟/接口测试通过率与失败样本（若可执行）
- 主要阻碍与建议（含是否满足验收标准）
- 后续工作的优先级建议（如修复 StyleCop、完善脚本、补充测试等）

## Report Outline
1. 概述（任务、时间、提交/分支、执行人）
2. 环境与前置条件
3. 执行步骤与结果（逐步小结）
4. 核心结论与建议（下一步方向）
5. 附录（命令与日志片段，聚焦关键信息）
