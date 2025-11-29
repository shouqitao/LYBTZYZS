---
name: lybtzyzs-openspec-archive-finalize
description: OpenSpec归档完成后的自动化流程：代码审查→提交推送→保存记忆→同步文档。可手动调用或通过hook自动触发。触发关键词：归档完成、archive finalize、openspec完成、归档后处理
---

# OpenSpec 归档完成处理器

## 核心能力

1. **代码审查** - 检查归档变更涉及的代码质量（调用lybtzyzs-code-review）
2. **提交推送** - 审查通过后自动commit并push到远程仓库
3. **保存记忆** - 将变更关键信息保存到Graphiti知识图谱
4. **同步文档** - 更新docs系统文档保持同步

## 何时使用

- OpenSpec归档（/openspec:archive）完成后自动触发
- 手动执行归档后处理流程
- 批量归档后需要统一处理

## 工作流程

```
┌──────────────────────────────────────────────────────────────┐
│                    归档完成触发                               │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 1: 代码审查                                            │
│  ─────────────────                                          │
│  • 获取归档变更涉及的文件（git diff HEAD~1）                   │
│  • 执行代码规范检查（命名/MVVM/DI/异步/注释）                   │
│  • 检查架构合规性                                             │
│  • 生成审查报告                                               │
└──────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
               审查通过              审查失败
                    │                   │
                    ▼                   ▼
┌───────────────────────┐    ┌─────────────────────────────────┐
│  STEP 2: 提交推送     │    │  输出问题报告，停止流程           │
│  ─────────────────    │    │  用户需手动修复后重新执行         │
│  • git add -A         │    └─────────────────────────────────┘
│  • git commit         │
│  • git push           │
└───────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 3: 保存Graphiti记忆                                    │
│  ─────────────────────────                                  │
│  • 提取变更关键信息（change-id, 影响范围, 技术决策）            │
│  • 构建知识节点（实体、关系、事实）                            │
│  • 保存到知识图谱（group_id=LYBTZYZS）                        │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 4: 同步文档                                            │
│  ─────────────────                                          │
│  • 检查docs/目录相关文档是否需要更新                           │
│  • 更新API文档（如有接口变更）                                 │
│  • 更新架构文档（如有架构变更）                                │
│  • 更新CHANGELOG.md                                          │
│  • 提交文档更新                                               │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│  完成：输出处理报告                                           │
└──────────────────────────────────────────────────────────────┘
```

## 输入要求

**必需**：
- 无（自动检测最近归档的变更）

**可选**：
- `change_id` - 指定变更ID（如不指定，使用最近归档的变更）
- `skip_review` - 跳过代码审查（默认false，仅紧急情况使用）
- `skip_docs` - 跳过文档同步（默认false）
- `dry_run` - 模拟运行，不实际提交（默认false）

## 输出格式

```markdown
## OpenSpec 归档完成处理报告

**变更ID**: refactor-medicalcase-api
**归档时间**: 2025-11-29 15:30 CST
**处理状态**: 成功

---

### 1. 代码审查结果

**评分**: 9.2/10（优秀）
**检查文件**: 12个
**问题统计**:
- 严重问题: 0
- 警告: 2
- 通过: 45

**详细结果**: 见 [审查报告](#code-review-details)

---

### 2. 提交推送结果

**提交哈希**: a1b2c3d
**提交信息**:
```
feat(MedicalCase): 重构医案API接口

- 统一编辑模式切换逻辑
- 优化RowVersion并发处理
- 添加状态机管理

Related: openspec refactor-medicalcase-api
```
**推送状态**: 成功 (master -> origin/master)

---

### 3. Graphiti记忆保存

**节点创建**: 3个
- MedicalCaseAPI (Entity)
- RowVersionConcurrency (Concept)
- EditModeStateMachine (Pattern)

**事实记录**: 5条
- MedicalCase模块重构了API接口设计
- 引入了状态机管理编辑模式
- ...

**Group ID**: LYBTZYZS

---

### 4. 文档同步结果

**更新文件**:
- `docs/reference/api/medicalcase.md` - API接口更新
- `docs/explanation/architecture/medicalcase-flow.md` - 架构说明更新
- `CHANGELOG.md` - 添加变更记录

**提交哈希**: d4e5f6g

---

## 后续建议

1. 验证线上功能正常
2. 通知相关团队成员
3. 更新相关Issue状态

---

**处理完成时间**: 2025-11-29 15:32 CST
**处理耗时**: 2分15秒
```

## 技术实现

**使用的工具链**:

1. **Bash (git)** - 获取变更文件、提交推送
2. **Grep** - 快速代码扫描
3. **mcp__serena** - 深度代码分析
4. **mcp__graphiti-memory** - 知识图谱保存
   - `add_memory` - 添加变更记录
   - `search_memory_facts` - 检查是否已存在
5. **mcp__filesystem** - 文件读写操作
6. **mcp__time** - 获取当前时间

**实现逻辑**:

```
1. 获取归档信息
   └─ openspec list --json → 最近归档的change-id
   └─ git diff HEAD~1 --name-only → 变更文件列表

2. 代码审查（调用lybtzyzs-code-review skill逻辑）
   └─ Grep快速扫描 → ServiceLocator/.Wait()/命名规范
   └─ serena深度分析 → MVVM模式/依赖关系
   └─ 生成评分报告

3. 提交推送（如审查通过）
   └─ git add -A
   └─ git commit -m "[生成的提交信息]"
   └─ git push origin master

4. 保存Graphiti记忆
   └─ 构建episode内容（变更摘要）
   └─ add_memory(name, episode_body, group_id="LYBTZYZS")

5. 同步文档
   └─ 检测变更影响的文档
   └─ 更新相关文档内容
   └─ 提交文档更新
```

## 代码审查标准

沿用 `lybtzyzs-code-review` skill的标准：

| 检查项 | 通过条件 |
|-------|---------|
| 命名规范 | PascalCase/\_camelCase符合标准 |
| MVVM模式 | ViewModel不直接操作UI |
| DI规范 | 无ServiceLocator反模式 |
| 异步模式 | 无.Wait()/.Result阻塞 |
| 中文注释 | 公开API有中文注释 |
| 架构合规 | 三层对齐、依赖方向正确 |

**通过标准**: 评分 >= 8.0/10 且无严重问题

## Graphiti记忆结构

```json
{
  "name": "OpenSpec归档: {change-id}",
  "episode_body": {
    "type": "openspec-archive",
    "change_id": "refactor-medicalcase-api",
    "summary": "重构医案API接口设计",
    "affected_modules": ["MedicalCase", "Consultation"],
    "key_decisions": [
      "引入状态机管理编辑模式",
      "统一RowVersion并发处理"
    ],
    "commit_hash": "a1b2c3d",
    "archived_at": "2025-11-29T15:30:00+08:00"
  },
  "group_id": "LYBTZYZS",
  "source": "json",
  "source_description": "OpenSpec归档自动记录"
}
```

## 限制条件

- 需要git仓库且有远程origin配置
- 需要Graphiti MCP服务可用
- 代码审查仅支持.NET/C#代码
- 文档同步仅处理docs/目录下的Markdown文件

## 错误处理

| 场景 | 处理方式 |
|-----|---------|
| 代码审查失败 | 输出问题报告，停止流程，不提交代码 |
| git push失败 | 保留本地提交，提示手动解决冲突 |
| Graphiti不可用 | 跳过记忆保存，继续其他步骤 |
| 文档同步失败 | 记录失败原因，继续完成流程 |

## 最佳实践

1. **归档前确保代码质量** - 在归档前先执行代码审查
2. **检查未提交更改** - 确保工作区干净再执行归档
3. **定期验证Graphiti连接** - 确保记忆服务可用
4. **文档同步时仔细审查** - 自动生成的文档可能需要人工调整

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-11-29 | 初始版本 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-11-29
