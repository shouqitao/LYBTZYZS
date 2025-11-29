---
name: OpenSpec: Archive
description: Archive a deployed OpenSpec change and update specs.
category: OpenSpec
tags: [openspec, archive]
---
<!-- OPENSPEC:START -->
**Guardrails**
- Favor straightforward, minimal implementations first and add complexity only when it is requested or clearly required.
- Keep changes tightly scoped to the requested outcome.
- Refer to `openspec/AGENTS.md` (located inside the `openspec/` directory—run `ls openspec` or `openspec update` if you don't see it) if you need additional OpenSpec conventions or clarifications.

**Steps**
1. Determine the change ID to archive:
   - If this prompt already includes a specific change ID (for example inside a `<ChangeId>` block populated by slash-command arguments), use that value after trimming whitespace.
   - If the conversation references a change loosely (for example by title or summary), run `openspec list` to surface likely IDs, share the relevant candidates, and confirm which one the user intends.
   - Otherwise, review the conversation, run `openspec list`, and ask the user which change to archive; wait for a confirmed change ID before proceeding.
   - If you still cannot identify a single change ID, stop and tell the user you cannot archive anything yet.
2. Validate the change ID by running `openspec list` (or `openspec show <id>`) and stop if the change is missing, already archived, or otherwise not ready to archive.
3. Run `openspec archive <id> --yes` so the CLI moves the change and applies spec updates without prompts (use `--skip-specs` only for tooling-only work).
4. Review the command output to confirm the target specs were updated and the change landed in `changes/archive/`.
5. Validate with `openspec validate --strict` and inspect with `openspec show <id>` if anything looks off.
6. **归档后处理** - 执行 `lybtzyzs-openspec-archive-finalize` skill完成以下自动化流程:
   - 代码审查：检查归档变更涉及的代码质量
   - 提交推送：审查通过后自动commit并push到远程仓库
   - 保存记忆：将变更关键信息保存到Graphiti知识图谱
   - 同步文档：更新docs系统文档保持同步

   如果审查未通过，输出问题报告并停止，用户需修复后手动执行skill。

**Reference**
- Use `openspec list` to confirm change IDs before archiving.
- Inspect refreshed specs with `openspec list --specs` and address any validation issues before handing off.
- 归档后处理skill: `.claude/skills/lybtzyzs-openspec-archive-finalize/SKILL.md`
<!-- OPENSPEC:END -->
