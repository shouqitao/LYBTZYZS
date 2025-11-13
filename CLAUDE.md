# CLAUDE.md v8.0 - Graphiti优先版

本文件定义 Claude Code 在仓库中的工作约束与执行流程，**核心原则：Graphiti作为"第一大脑"**。

---

## 🚀 核心原则

### ⭐ Graphiti 优先工作流（强制执行）

```
任务开始
  ↓
📖 RETRIEVE（知识检索） → 🛠️ EXECUTE（遵循规则） → 💾 STORE（知识沉淀）
  ↓                        ↓                        ↓
检索项目知识              严格遵循检索结果          沉淀新知识
  ↓
任务完成
```

**核心规则**：
- ✅ **任务前必须检索**：从 Graphiti 检索 Preference、Procedure、Requirement
- ✅ **执行中严格遵循**：冲突时优先级 Preference > Procedure > Requirement
- ✅ **任务后必须存储**：决策、新规则、历史教训存入 Graphiti

---

## 0. 核心信息速查

### 📦 GitHub 仓库（MCP 工具必需参数）

```python
owner = "shouqitao"
repo = "LYBTZYZS"
```

> ⚠️ GitHub MCP 工具不支持默认仓库，每次调用必须显式提供 owner 和 repo

### 🔧 技术栈

**核心框架**：.NET 8.0, WPF, ASP.NET Core, EF Core 8.0, Prism 8.x

**数据库**：SQL Server 2022

**MCP 工具**（按优先级排序）：
1. **graphiti-memory**（⭐第一大脑）：项目长期知识库
2. **serena**：代码语义分析与编辑
3. **filesystem**：文件系统操作
4. **github**：Issue/PR管理
5. **context7**：技术文档查询（最新官方文档）
6. **sequential-thinking**：深度推理分析

---

## 1. ⭐ Graphiti 优先原则（元规则）

### 1.1 三阶段工作流（强制）

#### 阶段1：RETRIEVE（任务前检索）

**何时检索**：任何任务开始前

**检索步骤**：
```markdown
1. 确定任务类型（新功能/Bug修复/架构调整/代码审查/文档更新/性能优化）
2. 使用 search_nodes 检索相关实体
   - Preference（偏好）：编码规范、命名规范、技术栈选择
   - Procedure（流程）：Issue工作流、验证流程、PR流程
   - Requirement（约束）：MVP黑名单、架构触发指标、质量标准
3. 使用 search_facts 检索实体关系
   - 模块依赖、架构层次、文档位置
4. 过滤实体类型，获取精准结果
5. 整理为"任务上下文"
```

**检索策略矩阵**：

| 任务类型 | 检索实体类型 | 关键词示例 | 过滤器 |
|---------|------------|-----------|--------|
| 新功能开发 | Preference, Procedure, Requirement | "编码规范", "Issue工作流", "MVP约束" | category: coding_style, issue_workflow, mvp_constraint |
| Bug修复 | Procedure, Decision | "验证流程", "历史Bug模式" | category: testing, predicate: "修复" |
| 架构调整 | Requirement, Fact | "架构触发指标", "模块依赖" | category: architecture_rule, dependency |
| 代码审查 | Preference, Procedure | "命名规范", "代码审查流程" | category: naming, code_review |
| 文档更新 | Procedure, Fact | "文档同步流程", "文档位置" | category: documentation, location |
| 性能优化 | Requirement, Decision | "性能指标", "历史优化决策" | category: quality_standard, impact: "性能" |

**检索示例**：
```python
# 示例1：新功能开发
search_nodes(
    query="编码规范 命名规范",
    entity_types=["Preference"],
    max_nodes=10
)

search_nodes(
    query="Issue工作流 验证流程",
    entity_types=["Procedure"],
    max_nodes=5
)

search_nodes(
    query="MVP约束 技术黑名单",
    entity_types=["Requirement"],
    max_nodes=5
)

# 示例2：检索模块依赖关系
search_facts(
    query="模块依赖 架构层次",
    max_facts=20
)
```

---

#### 阶段2：EXECUTE（遵循规则）

**执行原则**：
1. **严格遵循**检索到的 Preference 和 Procedure
2. **冲突时优先级**：Preference > Procedure > Requirement
3. **发现新规则**时，实时记录（add_memory）
4. **遇到例外情况**时，记录 Decision 并说明理由

**禁止行为**：
- ❌ 未从 Graphiti 检索就开始任务
- ❌ 忽略检索到的 Preference 或 Procedure
- ❌ 只编译通过就关闭 Issue（必须运行时验证）

---

#### 阶段3：STORE（知识沉淀）

**何时存储**：
- ✅ 用户表达偏好、需求、流程时 → 立即存储
- ✅ 任务完成后 → 存储决策理由、新发现的规则
- ✅ 遇到 Bug 后 → 存储修复模式、根因分析
- ✅ 架构调整后 → 更新模块依赖、架构层次

**⚠️ 强制更新要求**（Critical）：
1. **必须成功**：Graphiti 知识库更新是**强制性的**，不是可选的
   - ❌ 禁止跳过 Graphiti 更新步骤
   - ❌ 禁止"稍后更新"或"手动更新"
   - ✅ 任务完成前必须确认 Graphiti 更新成功

2. **异常处理**：
   - **服务不可访问**：唯一允许跳过的情况（先调用 `get_status` 验证）
   - **参数错误**：分析错误原因，修正参数后**立即重试**，直到成功
   - **JSON 格式错误**：改用 `source="text"` 纯文本格式重试
   - **连接超时**：重试 3 次，失败后记录到本地文件并通知用户

3. **验证机制**：
   - 调用 `add_memory` 后，等待返回 "queued for processing" 消息
   - 如果返回错误，**立即分析错误类型并重试**
   - 不允许"部分成功"状态 - 要么全部成功，要么全部重试

**存储策略**：
```python
# 示例1：存储新偏好
add_memory(
    name="编码偏好",
    episode_body='{"name": "异步规范", "category": "coding_style", "description": "I/O操作必须使用async/await", "priority": 9, "applies_to": ["Server", "Client"]}',
    source="json",
    source_description="用户偏好",
    group_id="lybtzyzs_project"
)

# 示例2：存储决策记录
add_memory(
    name="Issue决策",
    episode_body='{"issue_number": 1234, "decision": "使用EF Core而非Dapper", "rationale": "项目规模小,EF Core开发效率高", "impact": "Repository层", "timestamp": "2025-11-11T10:00:00Z"}',
    source="json",
    source_description="Issue #1234 技术选型决策",
    group_id="lybtzyzs_project"
)

# 示例3：存储Bug修复模式
add_memory(
    name="Bug修复模式",
    episode_body='{"bug_type": "NullReferenceException", "root_cause": "未检查导航属性为null", "solution": "使用?.运算符或显式Include", "affected_modules": ["Patients", "MedicalCase"]}',
    source="json",
    source_description="Bug修复历史教训",
    group_id="lybtzyzs_project"
)
```

**长文本拆分规则**：
- 每条 episode_body 不超过 200 字（中文）
- 复杂内容使用 JSON 结构化存储
- 使用 add_episode_bulk 批量存储

---

### 1.2 实体类型说明

Graphiti 知识图谱中的 5 种核心实体类型：

| 实体类型 | 用途 | 示例 | 检索关键词 |
|---------|-----|------|-----------|
| **Preference** | 项目开发偏好 | 编码规范、命名规范、技术栈 | "编码规范", "命名规范", "技术栈" |
| **Procedure** | 开发流程规范 | Issue工作流、验证流程、PR流程 | "工作流", "验证流程", "PR流程" |
| **Requirement** | 项目约束限制 | MVP技术黑名单、架构触发指标 | "MVP约束", "技术黑名单", "架构触发指标" |
| **Fact** | 项目事实关系 | 模块依赖、架构层次、文件位置 | "模块依赖", "架构层次", "文档位置" |
| **Decision** | 项目决策记录 | Issue决策、重构决策、Bug修复 | "Issue决策", "重构历史", "Bug模式" |

---

## 2. 快速导航

### 2.1 必读文档（任务前查阅）

**核心三文档**（⭐优先级最高）：
1. **README.md** - 项目权威概览
2. **docs/index.md** - 文档导航中心（v5.0 三层对齐架构）
3. **.spec-workflow/steering/structure.md** - 项目结构指南

**架构指南**（三层对齐）：
- `docs/explanation/architecture/server/README.md` - Server 端三层架构
- `docs/explanation/architecture/client/README.md` - Client 端 MVVM 架构
- `docs/explanation/architecture/shared/README.md` - 共享架构

> ⚠️ **强制要求**：处理任务前必须先查阅 `docs/index.md` 定位相关文档，未理解文档禁止开始编码

**完整文档索引** → `.claude/README.md`

---

## 3. 双轨工作流（小需求 vs 大需求）

**核心规则**：
- ✅ **所有改动必须有 GitHub Issue** - 无 Issue 禁止任何代码变更

**小需求（90%）**：<5 文件, <200 行, <2 小时 → 直接修改
```
创建 Issue → 从 Graphiti 检索规则 → 修改代码 → 验证 → 提交
```

**大需求（10%）**：跨模块, >200 行, >2 小时 → 自动化流程
```
创建 Issue → 调用 lybtzyzs-workflow-orchestrator skill → 14 状态自动化流程
```

**触发关键词**："复杂需求"、"新功能开发"、"Epic 任务"、"跨模块重构"

---

## 4. MCP 工具协同

### 4.1 工具优先级

**工具链**（按优先级）：
```
检索知识        分析代码        查文档        提交代码
    ↓              ↓              ↓             ↓
Graphiti  →    Serena    →   Context7   →   GitHub
（第一大脑）   （代码分析）   （技术文档）   （版本控制）
```

**使用原则**：
1. **Graphiti 优先**：任务前先检索，任务后先存储
2. **Serena 辅助**：代码语义分析、符号级编辑
3. **Context7 补充**：查询最新官方文档（.NET、WPF、EF Core）
4. **GitHub 强制**：所有代码变更必须关联 Issue

---

### 4.2 Graphiti 工具使用

**可用工具**：
- `add_memory`：存储知识（支持 text、JSON、message 格式）
- `search_nodes`：搜索实体节点（Preference、Procedure、Requirement 等）
- `search_facts`：搜索事实关系（模块依赖、架构层次等）
- `get_episodes`：获取历史 episodes
- `delete_episode`：删除 episode
- `clear_graph`：清空图谱（⚠️谨慎使用）

**最佳实践**：
```python
# 1. 任务前检索（组合查询）
preferences = search_nodes(query="编码规范", entity_types=["Preference"], max_nodes=10)
procedures = search_nodes(query="验证流程", entity_types=["Procedure"], max_nodes=5)
facts = search_facts(query="模块依赖", max_facts=20)

# 2. 任务后存储（JSON格式）
add_memory(
    name="Issue决策",
    episode_body='{"issue": 1234, "decision": "...", "rationale": "..."}',
    source="json",
    group_id="lybtzyzs_project"
)

# 3. 批量存储（高效）
from graphiti_core.utils.bulk_utils import RawEpisode
add_episode_bulk(
    bulk_episodes=[RawEpisode(...), RawEpisode(...)],
    group_id="lybtzyzs_project"
)
```

---

## 5. 紧急参考

### 5.1 常用命令

```bash
# 统一使用 LYBT.All.sln
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test LYBT.All.sln -c Release --settings tests/.runsettings
```

### 5.2 禁止行为（⚠️严格执行）

**工作流违规**：
- ❌ 未从 Graphiti 检索就开始任务
- ❌ 未创建 Issue 就修改代码
- ❌ 只编译通过就关闭 Issue（必须运行时验证）

**代码质量违规**：
- ❌ 编译有 errors 或 warnings
- ❌ 未测试边界条件
- ❌ 部分功能可用就提交

**知识管理违规**：
- ❌ 任务完成后未存储决策到 Graphiti
- ❌ 发现新规则未立即记录
- ❌ Bug 修复后未沉淀修复模式

---

## 6. 核心标准（从 Graphiti 检索详细规则）

### 6.1 执行原则（10条）

**完整定义** → Graphiti 检索 `Preference: "执行原则"`

速查版本：
1. 验证优先
2. 文档先行
3. 最小充分交付
4. 增量优化
5. 记录与可追溯
6. 文档归位
7. MVP 约束
8. 输出归档
9. 安全与合规
10. 立足长期目标

### 6.2 编码与验证标准

**完整规范** → Graphiti 检索 `Preference: "编码规范"`

速查版本：
- 语言：中文（注释、输出、提交信息）
- 编码：UTF-8 with BOM
- 命名：PascalCase（类型）、_camelCase（私有字段）
- 依赖注入：仅构造函数注入
- 异步：I/O 必须 async/await

**验证流程** → Graphiti 检索 `Procedure: "验证流程"`

速查版本：
1. 编译：0 errors, 0 warnings
2. 启动应用（Client + Server）
3. 执行真实操作场景
4. 验证数据库状态
5. 从用户视角确认功能完整可用

---

**最后更新**：2025-11-13（v8.0 Graphiti优先精简版）

**核心变更**：
- ✅ 精简到200行以内
- ✅ 重点针对Graphiti调用指南
- ✅ 强化三阶段工作流（RETRIEVE → EXECUTE → STORE）
- ✅ 明确强制更新要求
- ✅ 通过context7和ref查询最佳实践