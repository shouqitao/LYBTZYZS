# 文档编写与维护指南

- **维护人**：Thinker（ChatGPT）
- **最后更新**：2025-09-25

本文档定义项目文档的命名、结构、链接与审阅约定，确保知识体系统一可维护。

## 1. 命名规范
- `yyyy-mm-dd-主题.md`（推荐），例如：`2025-09-24-server-phase3-cache-governance-task.md`。
- 分类文档可使用语义化名称（如 `overview.md`、`README.md`），但需在目录索引中解释用途。
- 若需历史版本，使用 `-v2`、`-v3` 等后缀，并在文首写明变更摘要。

## 2. 目录与索引
- 新增文档后必须更新相关索引：`docs/index.md`、分册 `README/INDEX`、`docs/reports/INDEX.md` 或 `docs/tasks/README.md`。
- 归档文档移动到 `docs/reports/archive/`，并在 `docs/ARCHIVE.md` 记录。
- 避免在根目录散落临时文件，草稿放置 `.claude/` 或个人分支。

## 3. 内容要求
- 语言：默认中文描述，示例代码保持英文习惯。
- 结构：推荐“背景 → 目标 → 拆解 → 验收 → 附件/参考”。
- 链接：使用相对路径并附行号，如 `docs/reports/architecture-analysis-2025-09-25.md:42`。
- 元信息：文首注明发布日期、编写人/维护人、最近更新日期。

## 4. 审阅流程
- Thinker：负责架构/任务类文档的发布与审阅，维护 `docs/index.md`、`docs/architecture/`、`docs/prd/` 等核心索引，PRD 未经评审不得发布任务。
- Coder：负责实现总结、最佳实践、任务完成报告；每次交付后 24 小时内更新 `docs/tasks/completed/`、`docs/prds-summary/` 等文档，并同步所需索引。
- Code Reviewer：在代码评审或复盘中校验 PRD → 任务 → 总结链路，确认相关索引/链接有效；文档缺失需驳回或要求补齐。
- QA/PM：分别负责测试文档、PRD 验收记录，并确保需求变更留痕。
- 大规模改动需在任务/PRD 中立项，完成后附总结并更新索引与归档记录。

## 5. 工具与自动化（建议）
- Markdown lint：集成于 CI，使用 `markdownlint` 或 `remark-lint`。
- 链接检查：脚本定期扫描失效链接。
- 索引生成：后续可编写 PowerShell/Node 脚本，从 `docs/tasks`、`docs/reports` 自动生成列表。

## 6. 常见问题
- **命名冲突**：检查是否已有同主题文档；必要时在名称中加入子系统或阶段。
- **重复内容**：若多个文档结论一致，应合并或引用单一来源。
- **老旧资料**：超过两个月未更新的分析报告需评估是否归档。

---
请在新文档中引用本指南，并遵守上述约定。如需新增规范，请提交任务并在审阅通过后更新此文件。

