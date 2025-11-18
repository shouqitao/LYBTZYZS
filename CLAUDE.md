# 🔄 凌隐宝堂中医诊所项目 (LYBTZYZS) 工作流程调度

> **项目全称**: 凌隐宝堂中医诊所管理系统
> **项目简称**: LYBTZYZS
> **说明**: 在描述性场合统一使用"凌隐宝堂中医诊所项目"，技术文档中使用LYBTZYZS简称

## 📋 标准流程
**需求驱动开发流程**

### 🔄 大需求 (Epic) 流程
📋 需求分析 → 📝 需求确认 → 🎯 方案设计 → 📝 Epic创建 → 🔍 Issue分解 → ⚡ 任务执行 → ✅ 验证测试 → 👤 用户确认 → 🔀 PR创建 → 👀 PR审查 → 🔀 PR合并 → 📚 文档同步 → 🧠 Graphiti更新 → 🧹 环境清理 → ✅ Epic关闭

### 🔄 小需求 (Issue) 流程
📋 需求确认 → 🎯 方案设计 → 📝 Issue创建 → ⚡ 任务执行 → ✅ 验证测试 → 👤 用户确认 → 📚 文档同步 → 🧠 Graphiti更新 → 🧹 环境清理 → ✅ Issue关闭

## 📖 详细流程指南
→ **查看**: `docs/guides/requirement-driven-workflow.md` (完整需求驱动流程)
→ **模板**: `docs/templates/` (需求确认和方案设计模板)
→ **技能**: 调用相应LYBTZYZS Skills自动化生成文档

## 🛠️ 核心Skills调用 (凌隐宝堂中医诊所项目专用)
- `lybtzyzs-requirements-generator` - 生成需求确认文档
- `lybtzyzs-design-generator` - 生成方案设计文档
- `lybtzyzs-task-executor` - 自动执行GitHub Issue
- `lybtzyzs-pr-generator` - 生成Pull Request描述
- `lybtzyzs-task-reflector` - 任务完成反思总结
- `lybtzyzs-context-builder` - 构建任务执行所需的完整上下文

## 🔧 GitHub操作规范
- **默认工具**: 使用GitHub MCP工具进行所有GitHub操作
- **Issue管理**: `mcp__github__issue_write` (创建/更新/关闭)
- **PR管理**: `mcp__github__pull_request_*` 系列工具
- **仓库操作**: `mcp__github_*` 系列工具
- **认证要求**: 确保GitHub token有足够权限

## 🚨 核心约束
- **需求驱动**: 所有工作从需求确认开始
- **文档生成**: 重要文档必须调用skill生成
- **Graphiti记忆系统**: 决策和经验存储到Graphiti第一大脑
- **用户确认**: 重要变更需要用户同意后再执行
- **环境清理**: 任务完成必须执行清理流程
- **Issue闭环**: 所有Issues必须手动关闭，确保流程完整
- **PR检查**: 大需求合并PR后，检查Issues是否自动关闭，未关闭则手动关闭

## 🧠 记忆管理规范（Graphiti第一大脑）

### 任务开始前：查阅记忆
在开始任何任务之前，必须执行以下步骤：

1. **搜索相关记忆**
   - 使用 `mcp__graphiti-memory__search_memory_facts` 搜索任务相关的历史决策和经验
   - 搜索关键词：模块名、功能名、相关Issue编号、技术概念
   - 示例：搜索 "FormulaDetailView"、"XAML简化"、"绑定错误" 等

2. **检索相关节点**
   - 使用 `mcp__graphiti-memory__search_nodes` 查找相关实体和组件
   - 了解组件之间的关系和依赖

3. **获取最新记忆**
   - 使用 `mcp__graphiti-memory__get_episodes` 获取最近的任务执行记录
   - 查看类似任务的处理方式和注意事项

### 任务执行中：记录关键决策
在任务执行过程中，遇到以下情况应立即记录：
- 发现重要Bug及其根因
- 做出架构或设计决策
- 发现代码异味或反模式
- 总结技术要点或最佳实践
- 遇到难以解决的问题及解决方案

### 任务结束后：更新记忆
任务完成后，必须使用 `mcp__graphiti-memory__add_memory` 记录：

1. **必须记录的内容**
   - 任务摘要（时间、模块、关联Issue）
   - 遇到的问题及根本原因
   - 采取的解决方案和修复步骤
   - 技术要点和经验教训
   - Git提交历史和推送记录
   - 验证结果和测试场景

2. **记忆命名规范**
   - 格式：`{模块名}-{任务类型}-{日期}`
   - 示例：`FormulaDetailView简化Bug修复完成-2025-01-18`
   - 使用中文描述，便于搜索和理解

3. **记忆内容结构**
   ```markdown
   ## {任务标题}

   **时间**: YYYY-MM-DD
   **模块**: {模块名}
   **关联Issue**: #{Issue编号}

   ### 问题描述
   - 具体问题和现象
   - 错误信息和日志

   ### 根本原因
   - 问题的根本原因分析

   ### 解决方案
   - 采取的具体修复措施
   - 相关代码变更

   ### 技术要点
   - 关键技术概念
   - 最佳实践总结

   ### 验证结果
   - 测试场景和结果

   ### Git记录
   - 提交历史
   - 推送记录
   ```

### 记忆管理工具
- `mcp__graphiti-memory__add_memory` - 添加新记忆
- `mcp__graphiti-memory__search_memory_facts` - 搜索相关事实
- `mcp__graphiti-memory__search_nodes` - 搜索相关节点
- `mcp__graphiti-memory__get_episodes` - 获取历史记录
- `mcp__graphiti-memory__get_status` - 检查Graphiti状态

### 记忆应用场景
- ✅ Bug修复完成后，记录根因和解决方案
- ✅ 架构重构完成后，记录设计决策和经验
- ✅ 代码推送后，记录完整的提交历史
- ✅ 遇到技术难题时，记录问题和解决过程
- ✅ 发现最佳实践时，记录具体做法和效果

## 📦 项目配置信息
- **项目全称**: 凌隐宝堂中医诊所管理系统
- **项目简称**: LYBTZYZS
- **GitHub账户**: shouqitao (TonyShou)
- **仓库路径**: https://github.com/shouqitao/LYBTZYZS
- **项目类型**: 企业级中医诊所管理系统
