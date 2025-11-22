# 凌隐宝堂中医诊所项目 (LYBTZYZS) 工作流程

> **项目全称**: 凌隐宝堂中医诊所管理系统
> **项目简称**: LYBTZYZS
> **核心理念**: 深度思考 → 记忆检索 → 架构理解 → 任务规划 → 渐进执行 → 持续记录

## 🎯 UltraThink四阶段执行流程

### 阶段1: THINK（深度思考与信息收集）

```bash
# 1. 启动深度推理（复杂任务优先使用）
mcp__sequential-thinking__sequentialthinking
  # 适用场景：架构设计、问题诊断、方案评估等需要严密逻辑推理的场景
  # 在推理过程中可穿插调用其他工具进行信息检索

# 2. 查询Graphiti记忆（必须）
mcp__graphiti-memory__search_memory_facts
  query: "[模块名] [功能名] [技术关键词]"
mcp__graphiti-memory__search_nodes
  query: "[组件名] [类名] [服务名]"
mcp__graphiti-memory__get_episodes
  max_episodes: 10

# 3. 实时信息检索（按需使用）
# 3.1 最新技术文档和最佳实践
mcp__tavily-mcp__tavily-search
  query: "[技术关键词] [问题描述] best practices"

# 3.2 .NET代码库语义搜索
mcp__netcontext-server__semantic_search
  query: "[功能描述] [类名] [方法名]"
  # 备选：mcp__serena__find_symbol / search_for_pattern

# 4. 理解项目架构
docs/explanation/architecture/{client|server|shared}/
docs/guides/requirement-driven-workflow.md
docs/reference/mvp-constraints.md

# 5. 验证流程一致性
对比 Graphiti记忆流程 vs 文档流程，发现不一致则更新文档
```

**工具使用详细指南**: `docs/guides/advanced-tools-usage-guide.md`

**Graphiti详细指南**: 检索Graphiti记忆 `"LYBTZYZS-UltraThink详细执行指南"`

### 阶段2: PLAN（任务规划与清单生成）

```bash
# 1. 确定需要调用的Skills
大需求: requirements-generator → design-generator → task-breakdown →
        task-executor → pr-generator → task-reflector
小需求: task-executor → task-reflector

# 2. 使用TodoWrite生成任务清单（必须）
# ⚠️ 复杂任务（≥3步骤或≥30分钟）必须使用TodoWrite跟踪进度
# 流程：深度思考 → 需求确认 → 方案设计 → 渐进执行 → 验证测试 → 文档同步 → Issue关闭
# 原则：完成即标记 | 始终1个in_progress | 任务完成后清空
```

### 阶段3: EXECUTE（渐进执行与持续记录）

```bash
# 渐进执行原则：单一职责 + 小步快跑（≤2小时）+ 持续验证 + 及时记录

# 执行前：sequential-thinking分析风险 | netcontext-server定位代码
# 执行中：tavily查技术方案 | sequential-thinking评估架构 | netcontext-server搜索实现
# 执行后：每完成一个子任务保存记忆到Graphiti
```

**执行中的工具使用场景和组合模式**: `docs/guides/advanced-tools-usage-guide.md`

**详细模板**: 检索Graphiti记忆 `"LYBTZYZS-Graphiti记忆管理详细模板"`

### 阶段4: REFLECT（总结与归档）

```bash
# 1. 调用task-reflector生成总结
lybtzyzs-task-reflector

# 2. 保存完整记忆（13部分模板）
add_memory(name="{模块名}-{任务类型}-完成-{日期}")

# 3. 文档更新检查：架构文档、开发流程、最佳实践、技术约束

# 4. 自动关闭Issue（满足4条标准）
# ✅ 验收完成 + ✅ 功能验证 + ✅ 文档同步 + ✅ 记忆保存
mcp__github__issue_write(method="update", state="closed", state_reason="completed")
# 注意：Epic Issue需确认所有子Issue已关闭

# 5. 推送代码到远程仓库（必须）
git push origin master
```

## 📋 简化流程视图

### 大需求 (Epic)
🧠 THINK → 📖 查记忆/文档 → 📋 TodoWrite → 📝 需求确认 → 🎯 方案设计 → 📝 Epic创建 → 🔍 Issue分解 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 🔀 PR创建/审查/合并 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Epic关闭

### 小需求 (Issue)
🧠 THINK → 📖 查记忆/文档 → 📋 TodoWrite → 📝 需求确认 → 🎯 方案设计 → 📝 Issue创建 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Issue关闭

## 📖 详细文档索引

- **完整流程**: `docs/guides/requirement-driven-workflow.md`
- **高级工具使用指南**: `docs/guides/advanced-tools-usage-guide.md`
- **文档模板**: `docs/templates/`
- **Graphiti记忆**:
  - `LYBTZYZS-UltraThink详细执行指南-2025-01-18`
  - `LYBTZYZS-Graphiti记忆管理详细模板-2025-01-18`
  - `LYBTZYZS-记忆管理操作规范-2025-01-18`

## 🛠️ 核心工具

### 深度推理工具
- **`sequential-thinking`** - 结构化深度推理（⚠️ 复杂任务核心工具）
  - 适用场景：架构设计、问题诊断、方案评估、技术选型

### 实时信息检索工具
- **`tavily-mcp`** - Web实时搜索
  - 适用场景：技术调研、错误解决方案查询、开源项目示例

### .NET代码分析工具
- **`netcontext-server`** - .NET代码库语义搜索
  - 适用场景：代码定位、架构分析、依赖追踪
  - 备选工具：`serena`（find_symbol、search_for_pattern）

### 任务管理工具
- **`TodoWrite`** - 任务清单管理（⚠️ 核心工具，复杂任务必用）
  - 使用场景：≥3步骤任务、≥30分钟任务、跨会话任务

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

**详细工具使用说明、决策树、组合模式**: `docs/guides/advanced-tools-usage-guide.md`

## 🧠 Graphiti记忆管理

### 三阶段记忆管理
- **RETRIEVE**（启动前）: search_memory_facts + search_nodes + get_episodes
- **RECORD**（执行中）: 每完成一个子任务保存记忆
- **ARCHIVE**（结束后）: 保存完整任务记忆（13部分）

### 记忆命名规范
格式: `{模块名}-{任务类型}-{简要描述}-{日期}`
示例: `FormulaDetailView-Bug修复-XAML绑定错误-2025-11-21`

**详细模板和规范**: 检索Graphiti记忆获取

## 🚨 核心约束

- **TodoWrite必用**: 复杂任务（≥3步骤或≥30分钟）必须使用TodoWrite工具跟踪进度，保持专注
- **需求驱动**: 所有工作从需求确认开始
- **文档生成**: 重要文档必须调用skill生成
- **Graphiti第一大脑**: 决策和经验必须存储
- **用户确认**: 重要变更需用户同意后执行
- **环境清理**: 任务完成必须执行清理流程
- **Issue自动关闭**: 满足4条标准（验收完成+功能验证+文档同步+记忆保存）立即自动关闭，无需询问用户
- **PR检查**: 大需求PR合并后检查子Issues是否自动关闭
- **问题清单管理**: 所有开放问题必须保存到Graphiti，问题必须有清单，问题一次提一个等待用户针对性回答
- **术语规范**: 严格遵守Consultation术语定义（见下文），不得混用
- **讨论阶段禁止代码**: 需求讨论、方案讨论阶段禁止输出示例代码，影响阅读和输出效率。仅在实现阶段输出代码

## 📝 问题清单管理规范

### 核心原则（用户要求，必须遵守）
1. ✅ **所有问题必须保存到Graphiti**，避免信息丢失
2. ✅ **问题必须有清单**，结构化管理
3. ✅ **问题一次提一个**，等待用户针对性回答
4. ✅ 每个问题提供明确的选项和推荐方案
5. ✅ 记录用户确认结果

### 问题管理流程
```
发现开放问题 → 保存到Graphiti清单 → 逐个提问 → 记录用户答案 → 更新方案
```

### 问题格式模板
```markdown
## 问题X：[问题标题]

**场景说明**：[详细描述问题背景]

**选项A**：[方案描述]
- 优点：...
- 缺点：...

**选项B**：[方案描述]
- 优点：...
- 缺点：...

**我的推荐**：选项X
**理由**：...

**用户确认**：[待确认/已确认选项X]
```

## 🏥 核心术语规范（严格执行）

### Consultation = 诊断（仅指医案的一部分）

**定义**：
- Consultation **只表示**医案（MedicalCase）中的"诊断"部分
- 包含：四诊（望闻问切）、主诉、现病史、诊断结论
- 对应实体：`ConsultationDto`、`ConsultationPanel`

**❌ 错误用法（严格禁止）**：
- ❌ 不能用Consultation表示"看诊"（整个过程）
- ❌ 不能用Consultation表示"看病"
- ❌ 不能用Consultation表示"接诊"
- ❌ 不能用Consultation表示"诊疗"

**❌ 错误命名示例**：
- ❌ ConsultationWorkspace（看诊工作台）
- ❌ StartConsultation（开始看诊）
- ❌ CompleteConsultation（完成看诊）
- ❌ ConsultationFlow（看诊流程）

**✅ 正确用法**：
- ✅ 看诊/看病 → **MedicalCase**（医案）
- ✅ 看诊流程 → **MedicalCase Workflow**
- ✅ 完成看诊 → **Complete MedicalCase**
- ✅ 开始看诊 → **Start/Create MedicalCase**
- ✅ 看诊工作台 → **MedicalCaseWorkspaceView**

**✅ 正确命名示例**：
- ✅ `MedicalCaseWorkspaceView`（医案工作台）
- ✅ `ConsultationPanel`（诊断区域组件）
- ✅ `ValidateConsultation()`（验证诊断数据）
- ✅ `CompleteMedicalCaseAsync()`（完成看诊）

### 医案结构
```
MedicalCase（医案 = 完整的一次看诊）
├─ Consultation（诊断部分）
│   ├─ 主诉
│   ├─ 现病史
│   ├─ 四诊（望闻问切）
│   └─ 诊断结论
└─ Prescription（处方部分）
    └─ 处方药材列表
```

**详细规范**: 检索Graphiti记忆 `"LYBTZYZS-术语规范-Consultation严格定义"`

## 📦 项目配置

- **项目全称**: 凌隐宝堂中医诊所管理系统
- **项目简称**: LYBTZYZS
- **GitHub账户**: shouqitao (TonyShou)
- **仓库路径**: https://github.com/shouqitao/LYBTZYZS
- **项目类型**: 企业级中医诊所管理系统

### 技术栈
- **前端**: WPF (.NET 8), Prism.DryIoc 9.0, Refit
- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0
- **数据库**: SQL Server
- **架构**: 前端MVVM+组件化 | 后端三层架构 | 统一IService接口
