# 凌隐宝堂中医诊所项目 (LYBTZYZS) 工作流程

> **项目全称**: 凌隐宝堂中医诊所管理系统
> **项目简称**: LYBTZYZS
> **核心理念**: 深度思考 → 记忆检索 → 架构理解 → 任务规划 → 渐进执行 → 持续记录

## 🎯 UltraThink四阶段执行流程

### 阶段1: THINK（深度思考与信息收集）

```bash
# 1. 查询Graphiti记忆（必须）
mcp__graphiti-memory__search_memory_facts
  query: "[模块名] [功能名] [技术关键词]"
mcp__graphiti-memory__search_nodes
  query: "[组件名] [类名] [服务名]"
mcp__graphiti-memory__get_episodes
  max_episodes: 10

# 2. 理解项目架构
docs/explanation/architecture/{client|server|shared}/
docs/guides/requirement-driven-workflow.md
docs/reference/mvp-constraints.md

# 3. 验证流程一致性
对比 Graphiti记忆流程 vs 文档流程，发现不一致则更新文档
```

**详细指南**: 检索Graphiti记忆 `"LYBTZYZS-UltraThink详细执行指南"`

### 阶段2: PLAN（任务规划与清单生成）

```bash
# 1. 确定需要调用的Skills
大需求: requirements-generator → design-generator → task-breakdown →
        task-executor → pr-generator → task-reflector
小需求: task-executor → task-reflector

# 2. 使用TodoWrite生成任务清单
TodoWrite: [深度思考] → [任务规划] → [需求确认] → [方案设计] →
          [渐进执行] → [验证测试] → [用户确认] → [文档同步] →
          [Graphiti更新] → [环境清理] → [Issue关闭]
```

### 阶段3: EXECUTE（渐进执行与持续记录）

```bash
# 渐进执行原则
单一职责 + 小步快跑（≤2小时）+ 持续验证 + 及时记录

# 每个子任务完成后立即保存记忆
mcp__graphiti-memory__add_memory
  name: "{模块名}-{子任务名}-完成-{日期}"
  episode_body: "[使用子任务记忆模板]"
```

**详细模板**: 检索Graphiti记忆 `"LYBTZYZS-Graphiti记忆管理详细模板"`

### 阶段4: REFLECT（总结与归档）

```bash
# 1. 调用task-reflector生成总结
lybtzyzs-task-reflector

# 2. 保存完整记忆
mcp__graphiti-memory__add_memory
  name: "{模块名}-{任务类型}-完成-{日期}"
  episode_body: "[使用完整任务记忆模板 - 13部分]"

# 3. 文档更新检查
架构文档、开发流程、最佳实践、技术约束
```

## 📋 简化流程视图

### 大需求 (Epic)
🧠 THINK → 📖 查记忆/文档 → 📋 TodoWrite → 📝 需求确认 → 🎯 方案设计 → 📝 Epic创建 → 🔍 Issue分解 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 🔀 PR创建/审查/合并 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Epic关闭

### 小需求 (Issue)
🧠 THINK → 📖 查记忆/文档 → 📋 TodoWrite → 📝 需求确认 → 🎯 方案设计 → 📝 Issue创建 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Issue关闭

## 📖 详细文档索引

- **完整流程**: `docs/guides/requirement-driven-workflow.md`
- **文档模板**: `docs/templates/`
- **Graphiti记忆**:
  - `LYBTZYZS-UltraThink详细执行指南-2025-01-18`
  - `LYBTZYZS-Graphiti记忆管理详细模板-2025-01-18`
  - `LYBTZYZS-记忆管理操作规范-2025-01-18`

## 🛠️ 核心工具

### LYBTZYZS专用Skills
- `lybtzyzs-requirements-generator` - 需求确认文档
- `lybtzyzs-design-generator` - 方案设计文档
- `lybtzyzs-task-executor` - 自动执行Issue
- `lybtzyzs-pr-generator` - PR描述生成
- `lybtzyzs-task-reflector` - 任务反思总结
- `lybtzyzs-context-builder` - 上下文构建

### GitHub MCP工具
- `mcp__github__issue_write` - Issue管理（创建/更新/关闭）
- `mcp__github__pull_request_*` - PR管理
- `mcp__github_*` - 仓库操作

## 🧠 Graphiti记忆管理

### 三阶段记忆管理

**启动前 - RETRIEVE**:
```bash
search_memory_facts(query="{模块名} {技术关键词}")
search_nodes(query="{组件名} {类名}")
get_episodes(max_episodes=10)
```

**执行中 - RECORD**（每完成一个子任务）:
```bash
add_memory(
  name="{模块名}-{子任务名}-完成-{日期}",
  episode_body="[子任务记忆模板]"
)
```

**结束后 - ARCHIVE**:
```bash
add_memory(
  name="{模块名}-{任务类型}-完成-{日期}",
  episode_body="[完整任务记忆模板 - 13部分]"
)
```

### 记忆命名规范
格式: `{模块名}-{任务类型}-{简要描述}-{日期}`
示例: `FormulaDetailView-Bug修复-XAML绑定错误-2025-01-18`

### 记忆检索技巧
```bash
# 按模块: "FormulaDetailView"
# 按技术: "XAML 绑定 TwoWay"
# 按问题: "Bug NullReferenceException"
# 按时间: get_episodes(max_episodes=20)
```

**详细模板和规范**: 检索Graphiti记忆获取

## 🚨 核心约束

- **需求驱动**: 所有工作从需求确认开始
- **文档生成**: 重要文档必须调用skill生成
- **Graphiti第一大脑**: 决策和经验必须存储
- **用户确认**: 重要变更需用户同意后执行
- **环境清理**: 任务完成必须执行清理流程
- **Issue闭环**: 所有Issues必须手动关闭
- **PR检查**: 大需求PR合并后检查子Issues是否自动关闭

## 📦 项目配置

- **项目全称**: 凌隐宝堂中医诊所管理系统
- **项目简称**: LYBTZYZS
- **GitHub账户**: shouqitao (TonyShou)
- **仓库路径**: https://github.com/shouqitao/LYBTZYZS
- **项目类型**: 企业级中医诊所管理系统
