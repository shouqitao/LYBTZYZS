# 创建Issue命令 (/create-issue)

基于分析结果或用户需求，自动生成符合项目规范的GitHub Issue。

## 📋 执行流程

### 1️⃣ 收集Issue信息

#### 从对话中提取
- 问题描述或功能需求
- 重现步骤（如果是bug）
- 期望行为
- 相关模块
- 优先级评估

#### 从代码分析中提取
- 使用`mcp__serena__search_for_pattern`查找问题代码
- 使用`mcp__serena__find_referencing_symbols`评估影响范围
- 使用`git log`查找历史相关commits

### 2️⃣ 确定Issue类型和标签

#### Issue类型（type:*）
```
type:feature     - 新功能开发
type:bug         - Bug修复
type:refactor    - 代码重构
type:test        - 测试相关
type:documentation - 文档
type:performance - 性能优化
type:security    - 安全问题
```

#### 模块标签（module:*）
```
module:server    - Server端
module:desktop   - Desktop端
module:shared    - 共享层
module:tests     - 测试
```

#### 优先级标签（priority:*）
```
priority:p0  - 紧急（24小时）
priority:p1  - 高优先级（3天）
priority:p2  - 中优先级（1周）
priority:p3  - 低优先级（灵活）
```

### 3️⃣ 生成Issue内容

#### Bug Issue模板
```markdown
# Bug描述

{简洁明了的问题描述}

## 📊 环境信息

- **操作系统**：Windows 11 / Linux / macOS
- **.NET版本**：8.0.x
- **模块**：{受影响的模块}
- **分支**：{发现问题的分支}

## 🔍 重现步骤

1. {步骤1}
2. {步骤2}
3. {步骤3}

## ❌ 实际行为

{实际发生的错误行为，包含错误信息}

\`\`\`
{错误堆栈或日志}
\`\`\`

## ✅ 期望行为

{应该正确的行为}

## 📝 相关代码

**问题代码位置**：
- `src/path/to/file.cs:123`

\`\`\`csharp
// 问题代码示例
{代码片段}
\`\`\`

## 🔧 可能的解决方案

{初步分析的修复方向}

## 📚 相关Issue/PR

- Related #XXX
- Blocked by #YYY

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

#### Feature Issue模板
```markdown
# 功能需求

{功能的简要描述}

## 🎯 用户故事

作为 {角色}，
我希望 {功能}，
以便 {价值}。

## 📋 功能详细说明

### 核心功能
1. {功能点1}
2. {功能点2}
3. {功能点3}

### 用户界面
{UI描述或设计稿链接}

### 业务规则
- {规则1}
- {规则2}

## ✅ 验收标准

- [ ] {验收条件1}
- [ ] {验收条件2}
- [ ] {验收条件3}
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 集成测试通过
- [ ] 文档已更新

## 🔧 技术方案（可选）

{初步的技术实现思路}

## 📊 影响范围

**涉及模块**：
- {模块1}
- {模块2}

**API变更**：
- {如有API变更，列出}

## 📚 相关资料

- [设计文档](链接)
- [参考实现](链接)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

#### Refactor Issue模板
```markdown
# 重构需求

{重构的目标和动机}

## 📊 现状分析

### 当前问题
1. {问题1}
2. {问题2}
3. {问题3}

### 代码证据
\`\`\`csharp
// src/path/to/file.cs
{问题代码}
\`\`\`

### 影响
- **性能影响**：{量化}
- **维护成本**：{评估}
- **技术债务**：{评估}

## 🎯 重构目标

{期望达成的状态}

## 📋 重构计划

### Phase 1：{标题}
- [ ] {任务1}
- [ ] {任务2}

### Phase 2：{标题}
- [ ] {任务1}
- [ ] {任务2}

## ✅ 验收标准

- [ ] 所有测试通过
- [ ] 性能无回归（或提升）
- [ ] 架构测试通过
- [ ] 代码覆盖率不降低
- [ ] 文档已更新

## 📊 ROI预估

- **投入**：{工时}
- **收益**：{量化收益}
- **风险**：{风险评估}

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

### 4️⃣ 创建Issue

使用GitHub CLI自动创建：
```bash
gh issue create \
  --title "{Issue标题}" \
  --label "type:{type},module:{module},priority:{priority}" \
  --body "{生成的Issue内容}"
```

### 5️⃣ 关联相关Issue

自动添加关联：
```markdown
- Related #XXX
- Depends on #YYY
- Blocks #ZZZ
```

## 🎯 使用场景

- 发现Bug需要创建Issue
- 规划新功能
- 记录技术债务
- 创建重构任务
- 批量创建子Issue（Epic拆分）

## ⚡ 快速使用

### 创建Bug Issue
```
/create-issue bug 用户登录失败
```

### 创建Feature Issue
```
/create-issue feature 添加患者导出功能
```

### 创建Refactor Issue
```
/create-issue refactor Desktop.Services重构
```

### 从当前分析创建Issue
```
/create-issue from-analysis
```
（自动使用前面的/analyze-perf或/review-arch结果）

## 🧠 智能填充

Claude Code会智能填充以下内容：

### 自动识别
- **模块标签**：从代码路径推断（src/Server → module:server）
- **优先级**：从严重性推断（性能问题 → priority:p0）
- **类型**：从需求描述推断

### 自动关联
- 搜索相关Issue（使用`gh issue list --search`）
- 查找相关PR（使用`gh pr list --search`）
- 添加到相关Epic（如果存在）

### 自动补全
- 添加代码位置（从`mcp__serena`获取）
- 生成重现步骤（基于测试用例）
- 提取错误堆栈（从日志文件）

## 📚 Issue规范参考

- `docs/development/minimal-practice.md` - Issue驱动工作流
- `docs/development/github-labels-guide.md` - 标签使用规范

## 🔧 批量创建（Epic拆分）

从重构计划创建多个子Issue：
```
/create-issue from-plan refactor-plan-desktop-services.md
```

自动生成：
- 1个Epic Issue
- 4-6个Phase Issue（子Issue）
- 每个Phase包含详细任务清单
