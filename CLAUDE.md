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

## 📦 项目配置信息
- **项目全称**: 凌隐宝堂中医诊所管理系统
- **项目简称**: LYBTZYZS
- **GitHub账户**: shouqitao (TonyShou)
- **仓库路径**: https://github.com/shouqitao/LYBTZYZS
- **项目类型**: 企业级中医诊所管理系统
