# GitHub Issue 管理规范

> **版本**: 1.0
> **制定日期**: 2025-10-07
> **关联**: [minimal-practice.md](./minimal-practice.md)

---

## 📋 Issue 类型

### 1. 单任务 Issue
**特征**: 一个 Issue 对应一个 PR，完成即关闭

**PR 引用语法**:
```markdown
Closes #123
Fixes #123
Resolves #123
```

**示例**:
```markdown
## 关联 Issue

Fixes #995

## 实现内容
修复 HerbDto JSON 序列化冲突问题
```

---

### 2. 多阶段 Epic Issue
**特征**: 一个 Issue 包含多个 Phase，需多个 PR 逐步完成

**PR 引用语法** ⚠️ **重要**:
```markdown
# Phase 1-4 的 PR 使用（不会关闭 Issue）:
Part of #1013
Refs #1013

# 只在最后一个 Phase 使用（自动关闭 Issue）:
Closes #1013
```

**示例**:

#### ❌ 错误示例 - 提前关闭 Issue

**Issue #1013**: Client端业务模块统一设计规范（5个Phase）

```markdown
# PR #1016 (Phase 4)
## 关联 Issue

Closes #1013  ❌ 错误！导致 Phase 5 未完成就关闭 Issue

## 实现内容
Phase 4: 统一 DTO 映射策略
```

**问题**: Issue #1013 在 Phase 4 完成后就被自动关闭，但 Phase 5 尚未开始。

---

#### ✅ 正确示例 - 分阶段引用

**Issue #1013**: Client端业务模块统一设计规范（5个Phase）

```markdown
# PR #1014 (Phase 1-2)
## 关联 Issue

Part of #1013  ✅ 正确！不会关闭 Issue

## 实现内容
Phase 1: 文档与标准制定
Phase 2: 清理冗余目录
```

```markdown
# PR #1015 (Phase 3)
## 关联 Issue

Part of #1013  ✅ 正确！不会关闭 Issue

## 实现内容
Phase 3: 统一依赖注入模式
```

```markdown
# PR #1016 (Phase 4)
## 关联 Issue

Part of #1013  ✅ 正确！不会关闭 Issue

## 实现内容
Phase 4: 统一 DTO 映射策略
```

```markdown
# PR #1017 (Phase 5 - 最后一个)
## 关联 Issue

Closes #1013  ✅ 正确！所有 Phase 完成后关闭

## 实现内容
Phase 5: 验证与文档更新
```

---

## 🔄 Epic Issue 模板

创建多阶段 Issue 时，使用以下模板：

```markdown
# [EPIC] 任务标题

## 目标
简要描述整体目标

## 背景
说明为什么需要这个改造

## 实施计划

### Phase 1: 阶段1名称 (预计工期)
- [ ] 子任务 1
- [ ] 子任务 2

### Phase 2: 阶段2名称 (预计工期)
- [ ] 子任务 1
- [ ] 子任务 2

### Phase 3: 阶段3名称 (预计工期)
- [ ] 子任务 1
- [ ] 子任务 2

## 验收标准

- [ ] 验收标准 1
- [ ] 验收标准 2
- [ ] 验收标准 3

## 预期工期

总计 X-Y 天

## 优先级

P1/P2/P3

---

## 📝 PR 提交规范

⚠️ **重要**: 各 Phase 的 PR 使用 `Part of #<Issue号>`，**只在最后一个 Phase 使用 `Closes #<Issue号>`**

## 进度跟踪

- [ ] Phase 1 - PR #xxx (状态)
- [ ] Phase 2 - PR #xxx (状态)
- [ ] Phase 3 - PR #xxx (状态)
```

---

## 🚨 常见错误

### 错误 1: 中间 Phase 使用 Closes
```markdown
# PR #1016 (Phase 4/5)
Closes #1013  ❌ 导致 Issue 提前关闭
```

**后果**: Issue 在未完成所有 Phase 时就被关闭

**修复**: 重新打开 Issue
```bash
gh issue reopen <Issue号>
```

---

### 错误 2: 最后 Phase 未关闭 Issue
```markdown
# PR #1017 (Phase 5/5 - 最后一个)
Part of #1013  ⚠️ Issue 不会自动关闭，需人工关闭
```

**后果**: Issue 保持打开状态

**修复**: 人工关闭 Issue
```bash
gh issue close <Issue号> --comment "所有 Phase 已完成"
```

---

### 错误 3: 忘记引用 Issue
```markdown
# PR #1015
## 实现内容
重构 ViewModel 构造函数
```

**后果**: PR 和 Issue 无法关联，难以追踪进度

**修复**: 在 PR 描述中补充
```markdown
Part of #1013
```

---

## 📊 Issue 状态管理

### 标签使用

| 标签 | 用途 | 何时添加 |
|------|------|---------|
| `epic` | 多阶段大型任务 | 创建时 |
| `status: todo` | 待开始 | 创建时自动添加 |
| `status: in-progress` | 进行中 | 开始第一个 Phase 时 |
| `status: blocked` | 被阻塞 | 遇到阻塞时 |
| `status: done` | 已完成 | 关闭时自动添加 |
| `phase-1`, `phase-2`, ... | 阶段标识 | 每个 Phase 开始时 |

### Phase 完成后的更新

每个 Phase 的 PR 合并后，在 Issue 中更新进度：

```markdown
## 进度跟踪

- [x] Phase 1 - PR #1014 ✅ 已合并 (2025-10-07)
- [x] Phase 2 - PR #1014 ✅ 已合并 (2025-10-07)
- [x] Phase 3 - PR #1015 ✅ 已合并 (2025-10-07)
- [x] Phase 4 - PR #1016 ✅ 已合并 (2025-10-07)
- [ ] Phase 5 - 待执行
```

---

## 🔗 GitHub 自动关闭语法

### 自动关闭 Issue 的关键字

在 PR 描述或提交信息中使用以下关键字会**自动关闭** Issue：

| 关键字 | 示例 | 效果 |
|--------|------|------|
| `Closes` | `Closes #123` | PR 合并后关闭 #123 |
| `Fixes` | `Fixes #123` | PR 合并后关闭 #123 |
| `Resolves` | `Resolves #123` | PR 合并后关闭 #123 |
| `Close` | `Close #123` | PR 合并后关闭 #123 |
| `Fix` | `Fix #123` | PR 合并后关闭 #123 |
| `Resolve` | `Resolve #123` | PR 合并后关闭 #123 |

### 不会关闭 Issue 的关键字

| 关键字 | 示例 | 效果 |
|--------|------|------|
| `Part of` | `Part of #123` | 仅引用，不关闭 |
| `Refs` | `Refs #123` | 仅引用，不关闭 |
| `Related to` | `Related to #123` | 仅引用，不关闭 |
| `See` | `See #123` | 仅引用，不关闭 |

---

## ✅ 最佳实践

### 1. Epic Issue 创建时
- 明确标注各 Phase
- 预估每个 Phase 的工期
- 列出清晰的验收标准
- 添加 `epic` 标签

### 2. 每个 Phase 的 PR
- 标题包含 `[Phase X]` 前缀
- 使用 `Part of #<Issue号>` 引用
- 在 PR 描述中说明完成的 Phase 内容
- 合并后更新 Issue 进度

### 3. 最后一个 Phase 的 PR
- 使用 `Closes #<Issue号>` 自动关闭
- 确认所有验收标准已满足
- 在 PR 描述中总结整体完成情况

### 4. 遇到问题时
- 及时在 Issue 中更新阻塞原因
- 添加 `status: blocked` 标签
- 说明预计解决时间

---

## 📚 参考资料

- [Linking a pull request to an issue (GitHub Docs)](https://docs.github.com/en/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue)
- [About task lists (GitHub Docs)](https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/about-task-lists)
- [项目最小实践](./minimal-practice.md)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
