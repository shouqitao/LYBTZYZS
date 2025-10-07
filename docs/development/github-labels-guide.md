# GitHub 标签使用指南

## 📋 标签体系概览

本仓库采用结构化的标签体系，便于 Issue 和 PR 的分类、检索和管理。

## 🏷️ 标签分类

### 1. 类型标签 (type:*)

用于标识改动的性质，**必选标签**之一。

| 标签 | 颜色 | 说明 | 使用场景 |
|------|------|------|----------|
| `type:feature` | 🟢 #22C55E | 新功能开发 | 添加新功能、新模块 |
| `type:bug` | 🔴 #DC2626 | Bug 修复 | 修复缺陷、异常行为 |
| `type:refactor` | 🟠 #F59E0B | 代码重构 | 不改变功能的代码优化 |
| `type:test` | 🟢 #26A641 | 测试相关改动 | 添加/修复测试代码 |
| `type:documentation` | 🔵 #0075CA | 文档改进 | 文档更新、注释完善 |
| `type:security` | 🔴 #b60205 | 安全相关改动 | 安全漏洞修复、权限调整 |

**使用示例**：
```bash
gh issue create --label "type:test,module:server"
```

### 2. 模块标签 (module:*)

用于标识改动影响的模块，**必选标签**之一。

| 标签 | 颜色 | 说明 |
|------|------|------|
| `module:server` | 🔵 #3B82F6 | Server 后端模块 |
| `module:desktop` | 🟣 #8B5CF6 | Desktop 客户端模块 |
| `module:shared` | 🔵 #0052CC | Shared 共享模块 |
| `module:tests` | 🟢 #10B981 | 测试模块 |

### 3. 优先级标签 (priority:*)

用于标识任务的紧急程度，**推荐使用**。

| 标签 | 颜色 | 说明 | SLA |
|------|------|------|-----|
| `priority:p0` | 🔴 #ff0000 | Critical - 紧急 | 24h |
| `priority:p1` | 🟠 #ff6600 | High - 高 | 3天 |
| `priority:p2` | 🟡 #ffaa00 | Medium - 中 | 1周 |
| `priority:p3` | ⚪ #94A3B8 | Low - 低 | 灵活 |

### 4. Epic 标签 (epic:*)

用于跟踪多个相关 Issue 的大型任务。

| 标签 | 说明 |
|------|------|
| `epic` | 通用 Epic 标记 |
| `epic:server-tests-coverage-20250921` | 服务端单元测试全覆盖 |
| `epic:PRD-all-compile-unification-20250922` | All 解决方案编译统一 |
| `epic:entity-consistency` | Server 实体一致性优化 |
| `epic:Production-CCPM` | Production 安全性 Epic |

### 5. 工作流标签

| 标签 | 颜色 | 说明 |
|------|------|------|
| `frontend` | 🔵 #06B6D4 | 前端相关（WPF/UI） |
| `backend` | 🔵 #0EA5E9 | 后端相关（服务端逻辑） |
| `refactor` | 🟠 #F59E0B | 重构任务 |
| `completed` | 🟢 #0e8a16 | 已完成 |
| `good first issue` | 🟣 #7057ff | 适合新手 |
| `help wanted` | 🟢 #008672 | 需要帮助 |

### 6. 其他标签

| 标签 | 说明 |
|------|------|
| `bug` | 默认 Bug 标签（建议用 `type:bug`） |
| `enhancement` | 默认增强标签（建议用 `type:feature`） |
| `documentation` | 默认文档标签（建议用 `type:documentation`） |
| `question` | 疑问讨论 |
| `duplicate` | 重复 Issue |
| `invalid` | 无效 Issue |
| `wontfix` | 不会修复 |

## 📐 标签使用规范

### 必选标签组合

创建 Issue 时，**至少**包含以下标签：
1. **类型标签**（type:*）- 说明改动性质
2. **模块标签**（module:*）- 说明影响范围

### 推荐标签组合

根据任务特点，添加以下标签：
1. **优先级标签**（priority:*）- 说明紧急程度
2. **Epic 标签**（epic:*）- 归属大型任务
3. **工作流标签** - 补充说明

### 常见场景示例

#### 场景 1：Server 模块 Bug 修复（高优先级）
```bash
gh issue create \
  --label "type:bug,module:server,priority:p1" \
  --title "fix(server): 修复用户认证失败问题"
```

#### 场景 2：Desktop 测试创建（中优先级）
```bash
gh issue create \
  --label "type:test,module:desktop,module:tests,priority:p2" \
  --title "test(desktop): 创建 Desktop 模块单元测试"
```

#### 场景 3：文档更新（低优先级）
```bash
gh issue create \
  --label "type:documentation,priority:p3" \
  --title "docs: 更新 API 文档"
```

#### 场景 4：Epic 子任务
```bash
gh issue create \
  --label "type:feature,module:server,epic:server-tests-coverage-20250921,priority:p1" \
  --title "feat(server): 实现 Patients 模块测试"
```

## 🔍 标签查询

### 查看所有标签
```bash
gh label list
```

### 查询特定标签的 Issue
```bash
# 查询所有测试相关 Issue
gh issue list --label "type:test"

# 查询 Server 模块的 Bug
gh issue list --label "type:bug,module:server"

# 查询高优先级未完成任务
gh issue list --label "priority:p1" --state open
```

### 筛选 Epic 相关 Issue
```bash
gh issue list --label "epic:server-tests-coverage-20250921"
```

## 🛠️ 标签管理

### 创建新标签
```bash
gh label create "type:example" \
  --description "示例标签" \
  --color "3B82F6"
```

### 更新标签
```bash
gh label edit "type:example" \
  --description "新的描述" \
  --color "22C55E"
```

### 删除标签
```bash
gh label delete "type:example"
```

## 📊 标签统计

查看标签使用统计：
```bash
# 统计各类型标签的 Issue 数量
for label in type:feature type:bug type:test type:refactor; do
  count=$(gh issue list --label "$label" --state all --json number --jq '. | length')
  echo "$label: $count"
done
```

## 🎯 最佳实践

### DO ✅

1. **明确标识类型和模块**
   ```bash
   # 好的示例
   gh issue create --label "type:test,module:server,priority:p2"
   ```

2. **使用结构化命名**
   - Epic: `epic:{name}-{date}`
   - 任务: `task:{epic-name}`

3. **保持标签简洁**
   - 单个 Issue 不超过 5 个标签

4. **优先级明确**
   - P0/P1 任务必须有 SLA 承诺

### DON'T ❌

1. **避免标签冗余**
   ```bash
   # 不推荐：同时使用 bug 和 type:bug
   gh issue create --label "bug,type:bug"

   # 推荐：只使用 type:bug
   gh issue create --label "type:bug"
   ```

2. **避免过度细分**
   - 不要为单一 Issue 创建 Epic

3. **避免优先级滥用**
   - 不要所有任务都标记为 P0

## 📚 相关文档

- [Issue 驱动工作流](./minimal-practice.md)
- [项目开发规范](./standards.md)
- [CLAUDE.md 工作约束](./../CLAUDE.md)

---

**最后更新**: 2025-10-07
**维护者**: Claude Code
