# 架构适配性审查 计划（MCP 优先）

## 目标
- 评估架构适配性，输出缺失/过度设计清单与整改优先级。

## 步骤与里程碑
1. 结构扫描与清单（T+0.5 天）
2. 分层与边界分析（T+1 天）
3. 非功能能力对照（T+1 天）
4. 缺失与过度设计矩阵（T+1.5 天）
5. 最终报告与索引检查（T+2 天）

## 工具（优先使用 MCP）
- repo/index, repo/grep, symbol/index, dependency/graph, dependency/cycles, ci/config/read, test/coverage/read

## 产出物
- docs/architecture/overview.md
- docs/tasks/completed/architecture-audit-report.md
- 附件：mcp-commands.log（命令与关键输出摘录）

## 约束
- 禁止修改业务逻辑代码；仅更新 docs/ 与日志。
- 结论需具备“命令 → 摘要 → 证据（文件:起始行）”。

