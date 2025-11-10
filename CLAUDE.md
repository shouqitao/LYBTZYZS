# CLAUDE.md

本文件定义 Claude Code 在仓库中的工作约束与执行流程，确保所有改动可追踪、可验证、符合项目标准。

## 🚀 快速导航

### 📚 完整文档索引
**文档中心** → [.claude/README.md](.claude/README.md)

### 🎯 按场景快速查找

| 场景 | 推荐文档 | 时间 |
|-----|---------|------|
| 🆕 首次使用 | [getting-started.md](.claude/guides/getting-started.md) | 5分钟 |
| 🐛 修复Bug | [issue-workflow.md](.claude/guides/issue-workflow.md) | 参考 |
| ✨ 开发功能 | [spec-workflow.md](.claude/guides/spec-workflow.md) | 参考 |
| 🔍 代码审查 | [code-review.md](.claude/modes/code-review.md) | 工作模式 |
| 📝 更新文档 | [documentation.md](.claude/guides/documentation.md) | 参考 |
| 🧪 编写测试 | [testing.md](.claude/guides/testing.md) | 参考 |

### 🔗 核心资源

- **项目概览**: [README.md](README.md)
- **文档导航**: [docs/index.md](docs/index.md)
- **项目结构**: [.spec-workflow/steering/structure.md](.spec-workflow/steering/structure.md)
- **Constitution**: [.spec-workflow/steering/constitution.md](.spec-workflow/steering/constitution.md)

---

## 📋 模块化架构说明

本文档采用模块化设计，详细规则存放在 `.claude/` 目录：

**核心规则**: `.claude/core/` - RULES.md, PRINCIPLES.md, FLAGS.md, WORKFLOW.md 等

**工作模式**: `.claude/modes/` - code-review, architecture, performance, refactoring 等

**项目Skills**: `.claude/skills/` - mvp-compliance, arch-compliance, doc-sync 等

> **💡 提示**: Skills通过符号链接同步到全局目录（首次需运行`scripts/setup-skills.ps1`）

---

## 0. 核心信息速查

### 📦 GitHub仓库（MCP工具必需参数）

```python
owner = "shouqitao"
repo = "LYBTZYZS"
```

> ⚠️ GitHub MCP工具不支持默认仓库，每次调用必须显式提供owner和repo参数

### 🔧 技术栈

**核心框架**: .NET 8.0, WPF, ASP.NET Core, EF Core 8.0, Prism 8.x, Avalonia 11.2

**数据库**: SQL Server 2022

**MCP工具**: serena, filesystem, github, context7, microsoft_docs_mcp, sequential-thinking, graphiti-memory

**完整信息** → [.claude/reference/project-info.md](.claude/reference/project-info.md)

---

## 1. 工作流

### 1.1 必读文档（任务前）

**核心三文档**:
- `README.md` - 项目权威概览
- `docs/index.md` - 文档导航中心（v5.0三层对齐架构）
- `.spec-workflow/steering/structure.md` - 项目结构指南

**架构指南**（三层对齐）:
- `docs/explanation/architecture/server/README.md` - Server端三层架构
- `docs/explanation/architecture/client/README.md` - Client端MVVM架构
- `docs/explanation/architecture/shared/README.md` - 共享架构

> ⚠️ **强制要求**: 处理任务前必须先查阅 `docs/index.md` 定位相关文档，未理解文档禁止开始编码

**📐 Server/Client 职责划分原则**（⭐ 项目宗旨）:

**核心原则**: 业务模块的业务实现需要综合 Server 端和 Client 端考虑，**不是所有功能都放在 Server 端实现**。

**职责划分**:
- **Server 端**: 负责数据持久化、核心业务规则、数据校验、实体关系维护
- **Client 端**: 负责工作流编排、UI 逻辑、用户交互流程、业务流程控制

**设计决策时考虑**:
- ✅ 数据一致性约束 → Server 端
- ✅ 多步骤业务流程 → Client 端协调
- ✅ 用户交互逻辑 → Client 端
- ✅ 实体聚合根操作 → Server 端

> 💡 **示例**: 三步诊疗流程（辨证→开方标记→处方）由 Client 端 ViewModel 编排，Server 端提供原子化的数据操作接口


### 1.2 双轨工作流（小需求 vs 大需求）

**核心规则**:
- ✅ **所有改动必须有GitHub Issue** - 无Issue禁止任何代码变更
- ✅ **小需求 → 直接修改**（90%）: <5文件, <200行, <2小时，直接编码实现
- ✅ **大需求 → 自动化流程**（10%）: 跨模块, >200行, >2小时，启用自动化工作流系统

#### 小需求：直接修改模式

**适用场景**:
- Bug修复（<5文件修改）
- 简单功能调整（<200行代码）
- 文档更新
- 配置调整

**执行流程**:
1. 创建GitHub Issue描述问题
2. 直接修改代码（使用serena/filesystem等MCP工具）
3. 验证（编译 + 测试 + 运行）
4. 提交代码到master分支

**标准提交格式**:
```bash
git commit -m "fix(module): 修复XXX问题

Fixes #1234

- 具体改动1
- 具体改动2
- 验证：功能已正常工作

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

#### 大需求：自动化工作流模式

**适用场景**:
- 新功能开发（跨多个模块）
- 架构重构（>200行代码）
- Epic级任务（需拆分为多个子Issue）

**执行流程**:
```bash
用户提需求
  → 调用 lybtzyzs-workflow-orchestrator skill
  → 14状态自动化流程（需求→设计→任务→实施→质量→归档）
  → 5个人工确认点（需求确认、设计审查、任务审查、质量把关、反思审查）
  → 完成
```

**自动化工作流系统**:
- **Orchestrator**: lybtzyzs-workflow-orchestrator（14状态编排引擎）
- **自动化率**: 85%（仅5个必要人工确认点）
- **预期提效**: 需求→Issue耗时从4-6小时降至30分钟
- **完整文档**: [AUTOMATION-SYSTEM-SUMMARY.md](.claude/skills/AUTOMATION-SYSTEM-SUMMARY.md)

**触发方式**:
用户明确说明是"复杂需求"、"新功能开发"、"Epic任务"时，调用 lybtzyzs-workflow-orchestrator skill

---

## 2. 核心标准

### 2.1 执行原则（10条）

**完整定义** → [.claude/core/PRINCIPLES.md](.claude/core/PRINCIPLES.md)

1. **验证优先**: 先验证问题真实性再实施修复
2. **文档先行**: 以 `docs/` 现有规范为最高准则
3. **最小充分交付**: 完成导向、够用即好
4. **增量优化**: 禁止无指令的推倒重写
5. **记录与可追溯**: 决策须回写至Issue/文档
6. **文档归位**: 按规范存放，过时文档归档
7. **MVP约束**: 禁止私自扩展或新增功能
8. **输出归档**: 报告/日志写入指定目录
9. **安全与合规**: 严格遵守技术黑名单
10. **立足长期目标**: 架构调整立足3-5年演进（ADR-005）

**长期目标原则**: 渐进式演进（5-15天/次） + 6个量化触发指标 + Constitution可调整（需ADR）

### 2.2 编码与验证标准

**详细规范** → [.claude/reference/coding-standards.md](.claude/reference/coding-standards.md), [.claude/guides/testing.md](.claude/guides/testing.md)

**核心质量标准**:
- **编译**: 0 errors, 0 warnings
- **运行时验证（⚠️强制）**:
  1. 启动应用（Client + Server）
  2. 执行真实操作场景
  3. 验证数据库状态
  4. 从用户视角确认功能完整可用

**禁止行为**:
- ❌ 只编译通过就认为完成
- ❌ 部分功能可用就关闭Issue
- ❌ 未测试边界条件

**代码规范要点**:
- 语言: 中文（注释、输出、提交信息）
- 编码: UTF-8 with BOM
- 命名: PascalCase（类型）、_camelCase（私有字段）
- 依赖注入: 仅构造函数注入
- 异步: I/O必须async/await

### 2.3 版本管理

**详细规范** → [.claude/guides/issue-workflow.md#版本管理规范](.claude/guides/issue-workflow.md#版本管理规范)

**核心策略**:
- ✅ MVP阶段保持 **1.x.x.x** 系列稳定演进
- ✅ 通过功能扩展而非版本升级
- ❌ 避免大版本频繁跳跃

**升级触发条件**: 重大架构重构、破坏性API变更、技术栈重大升级、MVP发布后里程碑

---

## 3. 工具快速参考

### 3.1 常用命令

**完整参考** → [.claude/reference/commands.md](.claude/reference/commands.md)

```bash
# 统一使用 LYBT.All.sln
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test LYBT.All.sln -c Release --settings tests/.runsettings
```

### 3.2 工具优先级

**详细说明** → [.claude/core/TOOL-ENVIRONMENT.md](.claude/core/TOOL-ENVIRONMENT.md)

```
⭐⭐⭐ MCP工具（filesystem, serena, github, context7） - 跨平台，优先使用
⭐⭐ Bash工具（cat, grep, find） - 标准Unix命令
⚠️ PowerShell命令（Get-*, Select-*） - 仅项目环境可用
```

### 3.3 MCP工具协同

**完整参考** → [.claude/reference/mcp-tools.md](.claude/reference/mcp-tools.md)

**核心工具**:
- **serena**: 代码语义分析与编辑
- **filesystem**: 文件系统操作
- **github**: GitHub API（⚠️必须显式owner/repo）
- **context7**: 技术文档查询（最新官方文档）
- **sequential-thinking**: 深度推理分析
- **graphiti-memory**: 时序感知知识图谱（项目知识"第二大脑"）

**协同模式**:
```
深度分析: sequential-thinking → context7 → serena → graphiti-memory
快速开发: serena → context7 → serena → git
知识积累: graphiti-memory（决策→存储→查询→复用）
```

#### Graphiti Memory 工具（⚠️必选工具）

**核心能力**: 时序感知知识图谱，自动追踪时间戳，混合语义搜索

**强制使用场景** (⚠️必须实时使用):
1. 长期对话上下文 - 跨会话知识连续性
2. 技术决策记录 - 架构选型、设计方案
3. 问题诊断历史 - Bug根因、解决方案
4. 代码关系映射 - 模块依赖、接口调用
5. 用户偏好学习 - 编码风格、命名习惯

**存储触发**: 决策后、完成Issue后、遇Bug后、架构讨论后、Review发现模式后

**查询时机**: 新任务前、遇类似问题、架构设计、Code Review

> 💡 **核心原则**: Graphiti是项目知识的"第二大脑"，所有重要信息必须实时归档

**详细使用指南** → [graphiti-memory.md](.claude/reference/graphiti-memory.md)

---

## 4. 工作模式与Skills

### 4.1 工作模式（7种）

**详细定义** → [.claude/modes/](.claude/modes/)

| 模式 | 触发命令 | 用途 |
|-----|---------|------|
| 🔍 Code Review | `/code-review` | 规范、架构、安全检查 |
| 🏗️ Architecture | `/review-arch` | 三层架构验证 |
| ⚡ Performance | `/analyze-perf` | 性能分析与优化 |
| 🔄 Refactoring | `/refactor-plan` | 重构规划与Phase拆分 |
| 🧪 Testing | `/generate-tests` | 测试生成与验证 |
| 📝 Documentation | `/update-docs` | 文档同步与更新 |
| 🧠 Research | `/deep-research` | 深度技术研究 |

### 4.2 Claude Skills（13个）

**完整指南** → [.claude/guides/skills-usage.md](.claude/guides/skills-usage.md)

#### ⭐ 核心编排引擎

**lybtzyzs-workflow-orchestrator** - 自动化工作流编排引擎 🔴 核心
- **功能**: 14状态自动化流程（需求→设计→任务→实施→质量→归档）
- **触发**: 大需求开发（用户明确说明"复杂需求"、"新功能"、"Epic任务"）
- **自动化率**: 85%（仅5个人工确认点）
- **人工确认点**: 需求确认、设计审查、任务审查、质量把关、反思审查
- **配置**: `.claude/config/workflow-orchestrator.json`（4种场景配置）
- **完整文档**: [AUTOMATION-SYSTEM-SUMMARY.md](.claude/skills/AUTOMATION-SYSTEM-SUMMARY.md)

#### 业务Skills（11个）

**需求与设计**:
- **lybtzyzs-requirements-generator**: 需求文档生成（用户需求→需求讨论文档）
- **lybtzyzs-design-generator**: 设计文档生成（需求→设计）
- **lybtzyzs-design-arch-validator**: 设计架构验证

**合规与质量**:
- **lybtzyzs-mvp-compliance**: MVP合规检查（技术黑名单、过度设计）
- **lybtzyzs-arch-compliance**: 架构合规检查（三层架构、依赖方向）
- **lybtzyzs-doc-sync**: 文档同步检查（强制读取规则、变更检测）
- **lybtzyzs-quality-reporter**: 质量报告生成（PR质量评分、自动合并决策）

**任务管理**:
- **lybtzyzs-task-breakdown**: 任务分解（设计文档→task清单）
- **lybtzyzs-issue-template**: Issue批量生成（task文档→GitHub Issues）
- **lybtzyzs-task-executor**: 任务自动执行（Issue→代码→验证→提交）
- **lybtzyzs-task-tracker**: 任务状态追踪（GitHub双向同步、Epic进度）
- **lybtzyzs-task-reflector**: 任务反思改进（技术债务、知识归档）

#### 使用指南

**小需求（90%）**: 无需调用Skills，直接使用serena/filesystem等MCP工具修改代码

**大需求（10%）**: 自动调用 **lybtzyzs-workflow-orchestrator**，启动完整自动化流程

**触发关键词**: "复杂需求"、"新功能开发"、"Epic任务"、"跨模块重构"

---

## 5. 架构哲学

### 5.1 三层对齐架构

**完整说明** → [.claude/explanation/architecture-philosophy.md](.claude/explanation/architecture-philosophy.md)

**核心理念**: Server端（三层） + Client端（MVVM五层） + Shared（跨端共享）

**演进路径**: Service层协调 → 富领域模型 → 领域事件 → CQRS

**6个触发指标**:
1. 业务规则: >20条 → 富领域模型
2. Service方法: >200行 → 领域服务拆分
3. 聚合根关系: >3层 → 重新设计边界
4. 状态机: >8状态 → 状态机模式
5. 团队规模: >5人 → CQRS分离读写
6. 数据量: >100万 → 缓存/读写分离

**当前状态**: MVP阶段（业务规则~14条，Service<100行，团队1人，数据<1万）

### 5.2 MVP约束

**完整说明** → [.claude/explanation/mvp-philosophy.md](.claude/explanation/mvp-philosophy.md)

**核心原则**: 够用即好 + 拒绝超前设计 + 简单直接 + 快速交付

**技术黑名单（MVP阶段严格禁止）**:
- ❌ 分布式: Redis, RabbitMQ/Kafka, Docker, 微服务
- ❌ 过度设计: CQRS, MediatR, Event Sourcing, DDD富领域模型
- ❌ 过度抽象: 多层抽象接口, 过度工厂/策略模式
- ❌ 前端框架: GraphQL, React/Vue（Desktop）, Blazor（Desktop）

**允许技术栈**: .NET 8, EF Core, SQL Server, WPF, Prism, ASP.NET Core, xUnit, NSubstitute

**Constitution可调整条件**: 充分业务证据 + MVP替代方案评估 + ROI >2倍

### 5.3 长期愿景（3-5年）

**完整路径图** → [.claude/explanation/long-term-vision.md](.claude/explanation/long-term-vision.md)

**演进路径**: MVP（2025）→ 富领域模型（2026）→ 领域事件（2027）→ CQRS（2028）

**渐进式演进**: 每次演进5-15天（可控） + 明确触发条件 + Constitution可调整

---

## 6. 文档维护

### 6.1 文档同步（强制）

**完整指南** → [.claude/guides/documentation.md](.claude/guides/documentation.md)

**强制流程**:
- 实施前: 列出文档更新清单
- 开发中: 代码变更后立即更新文档
- 完成前: 确认所有文档已更新

**更新范围**: 架构文档、开发指南、API文档、快速参考、导航索引

### 6.2 环境清理

**完整指南** → [.claude/guides/testing.md#验证后的环境清理](.claude/guides/testing.md#验证后的环境清理)

**清理清单**: 终止临时进程 + 释放资源缓存 + 还原配置 + 关闭外部连接 + 证据归档 + 端口检查

### 6.3 约束调整流程

**详细流程** → [.claude/explanation/mvp-philosophy.md#附录](.claude/explanation/mvp-philosophy.md#附录)

**调整流程**: GitHub Issue提出并获批 → 创建ADR → 更新Constitution → 同步更新相关文档

---

## 📚 快速索引

### 按任务类型

| 任务 | 推荐文档 |
|-----|---------|
| 🆕 首次使用 | [getting-started.md](.claude/guides/getting-started.md) |
| 🐛 修复Bug | [issue-workflow.md](.claude/guides/issue-workflow.md) |
| ✨ 开发功能 | [spec-workflow.md](.claude/guides/spec-workflow.md) |
| 🔄 代码重构 | [refactoring.md](.claude/modes/refactoring.md) |
| 🧪 编写测试 | [testing.md](.claude/guides/testing.md) |
| 📝 更新文档 | [documentation.md](.claude/guides/documentation.md) |
| 🔍 代码审查 | [code-review.md](.claude/modes/code-review.md) |
| 🏗️ 架构设计 | [architecture-philosophy.md](.claude/explanation/architecture-philosophy.md) |
| 🧠 知识管理 | [graphiti-memory.md](.claude/reference/graphiti-memory.md) |

---

**最后更新**: 2025-11-11（v6.2 Graphiti集成版 - 知识图谱"第二大脑"）

**变更历史**:
- v6.2（2025-11-11）: 集成Graphiti Memory知识图谱，移除停用MCP工具（shrimp/interactive-feedback/spec-workflow），优化底部导航
- v6.1.1（2025-10-28）: 平衡精简优化，再减少25%，保留关键示例
- v6.1（2025-10-28）: 重构为模块化架构，创建15个专门文档，主文档精简94%
- v6.0（2025-10-20）: 新增版本管理规范、验证优先策略、长期目标原则
- v5.0（2025-10-15）: 文档架构三层对齐、新增Spec-Driven工作流
