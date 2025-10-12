# Claude Code 快速开始指南

> **🚀 5分钟快速上手 Claude Code 开发工作流**

## 📋 核心概念

### 1. Issue 驱动开发

所有代码改动必须先有 GitHub Issue：

```
Issue → 任务清单 → 功能分支 → 编码 → PR → 审查 → 合并 → 文档更新
```

### 2. 模块化文档

`.claude/` 目录包含所有核心规则：

- `.claude/core/` - 核心规则与原则
- `.claude/modes/` - 7种专业工作模式

###3. 工具优先级

```
MCP 工具 > Bash 工具（标准 Unix 命令） > PowerShell 命令（仅项目环境）
```

---

## 🎯 第一次使用

### Step 1: 阅读核心文档

```bash
# 必读（按顺序）
1. CLAUDE.md                    # 总览和核心约束
2. .claude/core/WORKFLOW.md     # Issue 驱动工作流
3. .claude/core/PRINCIPLES.md   # 执行原则
4. docs/development/standards.md # 编码规范
```

### Step 2: 理解工作流程

**标准流程示例**：

```bash
# 1. 创建 Issue
gh issue create --title "添加患者搜索功能" --body "..." --label "type:feature"

# 2. 生成任务清单
[SRV-1] 设计 Repository 接口
[SRV-2] 实现搜索逻辑
[SRV-3] 添加单元测试
[DOC-1] 更新 API 文档

# 3. 创建功能分支
git checkout -b feature/patient-search-1234

# 4. 逐个完成任务
# ...编码...

# 5. 提交 PR
gh pr create --title "添加患者搜索功能" --body "Closes #1234"

# 6. CI/CD 通过后合并
gh pr merge 1234 --squash
```

### Step 3: 选择合适的工作模式

| 任务类型 | 使用模式 | 命令 |
|---------|---------|------|
| 代码审查 | Code Review Mode | `/code-review` |
| 架构验证 | Architecture Mode | `/review-arch` |
| 性能分析 | Performance Mode | `/analyze-perf` |
| 重构规划 | Refactoring Mode | `/refactor-plan` |
| 生成测试 | Testing Mode | `/generate-tests` |
| 文档同步 | Documentation Mode | `/update-docs` |
| 深度研究 | Research Mode | `/deep-research` |

---

## 💡 常用场景

### 场景1：修复 Bug

```
1. 创建 Issue（type:bug，priority:p0/p1）
2. 生成清单：[SRV-1] 定位问题，[SRV-2] 修复代码，[SRV-3] 补充测试
3. 创建分支：bugfix/fix-null-reference-1234
4. 修复 → 提交 → PR → 合并
```

### 场景2：添加新功能

```
1. 创建 Issue（type:feature，epic:xxx）
2. 生成清单：[SRV-1~5] 服务端任务，[CLI-1~3] 客户端任务，[DOC-1~2] 文档任务
3. 评估并行性：SRV 和 CLI 可并行，DOC 最后
4. 创建分支：feature/new-feature-1234
5. 编码 → 测试 → 提交 → PR → 合并
```

### 场景3：重构代码

```
1. 使用 /refactor-plan 进行深度分析（UltraThink 20-30步）
2. 创建 Issue（type:refactor，拆分为多个 Phase）
3. 逐个 Phase 执行：Phase 1 → PR → 合并 → Phase 2 → ...
4. 确保向后兼容，不破坏现有测试
```

---

## 🛠️ 工具使用技巧

### MCP 工具（推荐）

```bash
# 文件操作
Read tool                       # 读取文件
Write tool                      # 创建文件
Edit tool                       # 编辑文件

# 代码搜索
Grep tool(pattern, type)        # 内容搜索
Glob tool(pattern)              # 文件名搜索
mcp__serena__find_symbol(name)  # 语义搜索

# Git 操作
mcp__git__git_status()
mcp__git__git_diff()
mcp__git__git_commit(message)

# 深度思考
mcp__sequential-thinking__sequentialthinking()
```

### Bash 工具（标准 Unix 命令）

```bash
# 简单操作
cat CLAUDE.md                   # 读取文件
grep "pattern" file.txt         # 搜索
find . -name "*.cs"             # 查找文件
wc -l file.txt                  # 统计行数
git status                      # Git 状态
```

### 注意事项

- ❌ **禁止**在 Bash 工具中使用 PowerShell 命令（`Get-*`, `Select-*` 等）
- ✅ **优先**使用 MCP 工具（跨平台，API 统一）
- ✅ **备选**使用标准 Unix 命令（cat, grep, find 等）

---

## 📚 深入学习路径

### 初级（必读）

1. `CLAUDE.md` - 核心约束总览
2. `.claude/core/WORKFLOW.md` - Issue 驱动工作流
3. `.claude/core/FILE-ORGANIZATION.md` - 文件组织规范
4. `.claude/core/TOOL-ENVIRONMENT.md` - 工具环境说明

### 中级（推荐）

1. `.claude/core/PRINCIPLES.md` - 执行原则
2. `.claude/core/RULES.md` - 工具选择优先级
3. `.claude/modes/` - 7种工作模式详解
4. `docs/development/standards.md` - 编码规范

### 高级（深入）

1. `docs/architecture/server-module-design-standard.md` - Server 架构标准
2. `docs/architecture/client/unified-design-standard.md` - Client 架构标准
3. `docs/development/mcp-tools-reference.md` - MCP 工具完整参考
4. `.claude/modes/refactoring.md` - 重构规划模式（UltraThink）

---

## ⚡ 快速命令参考

```bash
# Issue 管理
gh issue create                 # 创建 Issue
gh issue list                   # 列出 Issues
gh issue close 1234             # 关闭 Issue

# PR 管理
gh pr create                    # 创建 PR
gh pr list                      # 列出 PRs
gh pr view 1234                 # 查看 PR
gh pr merge 1234 --squash       # 合并 PR

# 构建测试
dotnet build LYBT.All.sln       # 构建所有项目
dotnet test LYBT.Server.sln -c Release  # 运行 Server 测试
dotnet format LYBT.All.sln      # 格式化代码

# Git 操作
git checkout -b feature/xxx     # 创建功能分支
git add .                       # 暂存更改
git commit -m "[SRV-1] 提交信息"  # 提交
git push -u origin feature/xxx  # 推送分支
```

---

## 🆘 常见问题

### Q1: Issue 清单前缀怎么用？

A: 根据模块类型使用前缀：
- `[SRV-1]` - Server 端任务
- `[CLI-1]` - Client 端任务
- `[DOC-1]` - 文档任务

### Q2: 什么时候使用 UltraThink？

A: 系统级重构、复杂架构决策、高不确定性任务（20-30步深度分析）。

### Q3: Bash 工具报错"command not found"？

A: 检查是否使用了 PowerShell 命令（如 `Get-Content`），改用标准 Unix 命令（如 `cat`）或 MCP 工具。

### Q4: 如何并行执行多个任务？

A: 在 Issue 清单中标注可并行项，使用 `sequential-thinking` 工具评估依赖关系。

---

## 下一步

- ✅ 完成第一个 Issue
- ✅ 探索一种工作模式（如 `/code-review`）
- ✅ 阅读架构标准文档
- ✅ 熟悉 MCP 工具链

**祝你使用愉快！🎉**
