---
name: OpenSpec: Archive
description: |
  归档已完成的OpenSpec变更。执行代码审查、提交推送、保存记忆、文档同步。
  触发lybtzyzs-openspec-archive-finalize skill完成归档流程。
category: OpenSpec
tags: [openspec, archive, finalize]
---

# OpenSpec 归档变更

**触发Skill**: 使用 `Skill(skill="lybtzyzs-openspec-archive-finalize", args="{change-id}")` 完成归档。

## 前置条件

执行前必须确认：
1. 执行阶段已完成（所有tasks已标记完成）
2. 编译验证通过
3. 用户已确认执行结果

## 工作流程

```
/openspec:archive {change-id}
        │
        ▼
┌─────────────────────────────────────┐
│ STEP 1: 代码审查                     │
│ - 检查归档变更涉及的文件             │
│ - 执行代码规范检查                   │
│ - 生成审查报告（评分>=8.0通过）       │
└─────────────────────────────────────┘
        │
   ┌────┴────┐
   │         │
 通过      失败
   │         │
   ▼         ▼
   │    输出问题报告
   │    停止流程
   │
   ▼
┌─────────────────────────────────────┐
│ STEP 2: 提交推送                     │
│ - git add -A                        │
│ - git commit (包含change-id)        │
│ - git push                          │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│ STEP 3: 保存Serena记忆               │
│ - 提取变更关键信息                   │
│ - 保存到.serena/memories/           │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│ STEP 4: 文档同步                     │
│ - 检测project.md是否需要更新        │
│ - 同步README.md                     │
│ - 更新CHANGELOG.md                  │
└─────────────────────────────────────┘
        │
        ▼
完成，输出归档报告
```

## 使用方式

```bash
# 归档指定变更
/openspec:archive unify-pending-query-api

# 也可以用自然语言触发
"归档提案 standardize-api-naming"
"确认完成，进入归档 unify-pending-query-api"
```

## 归档输出

### 归档报告

```markdown
## OpenSpec 归档完成处理报告

**变更ID**: {change-id}
**归档时间**: {timestamp}
**处理状态**: 成功/失败

---

### 1. 代码审查结果
**评分**: {score}/10
**问题统计**: 严重{n} / 警告{n} / 通过{n}

### 2. 提交推送结果
**提交哈希**: {commit-hash}
**推送状态**: 成功

### 3. Serena记忆保存
**记忆文件**: `.serena/memories/openspec-{change-id}-{date}.md`

### 4. 文档同步结果
**CHANGELOG.md**: 已更新
```

## 文档层级

归档时遵循文档层级：

```
openspec/project.md (权威源 - 手动维护)
        │
        ▼ 同步关键信息
README.md (用户入口)
        │
        ▼ 追加记录
CHANGELOG.md (变更历史)
```

## 可选参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `skip_review` | 跳过代码审查 | false |
| `skip_docs` | 跳过文档同步 | false |
| `dry_run` | 模拟运行，不实际提交 | false |

## 下一步

归档完成后，OpenSpec变更流程结束。变更已移动到 `changes/archive/` 目录。

---

**重要**: 此命令触发 `lybtzyzs-openspec-archive-finalize` skill，自动完成归档全流程。

<!-- SKILL_INVOKE: lybtzyzs-openspec-archive-finalize -->
