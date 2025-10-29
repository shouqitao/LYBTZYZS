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

**核心框架**: .NET 8.0, WPF, ASP.NET Core, EF Core 8.0, Prism 9.0, Avalonia 11.2

**数据库**: SQL Server 2022

**MCP工具**: serena, filesystem, github, context7, microsoft_docs_mcp, sequential-thinking, shrimp-task-manager, interactive-feedback, drawio

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

### 1.2 Issue驱动工作流

**完整指南** → [.claude/guides/issue-workflow.md](.claude/guides/issue-workflow.md)

**核心规则**:
- ✅ **所有改动必须有GitHub Issue** - 无Issue禁止任何代码变更
- ✅ **小Issue → 直接Master**（90%）: <5文件, <200行, <2小时
- ✅ **Epic → 创建PR**（10%）: 跨模块, >200行, >2小时, ⚠️1-3天内合并

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

### 1.3 Spec-Driven工作流

**完整指南** → [.claude/guides/spec-workflow.md](.claude/guides/spec-workflow.md)

**核心流程**: Constitution检查 → 需求讨论 → 需求文档 → 设计文档 → Issue创建 → 实施

**强制机制**:
- **Constitution**: `.spec-workflow/steering/constitution.md` - 所有任务前必查
- **Quality Checklists**: `.spec-workflow/templates/checklists/` - 通过率≥90%

**三阶段文档化**:
1. **需求讨论** → `docs/explanation/architecture/{client|server|shared}/*-discussion.md` (❌禁止代码)
2. **需求文档** → `docs/explanation/requirements/*-requirements.md` (⚠️必须等待用户确认)
3. **设计文档** → `docs/explanation/design/*-design.md` (包含架构、API、代码、Phase拆分)

**文档读取规则**:
- ⚠️ 需求分析前: 必读 docs/index.md, business-rules.md, 架构指南
- ⚠️ 设计文档前: 必读对应架构指南
- ⚠️ 架构调整前: 必须创建ADR并更新架构文档

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
- **shrimp-task-manager**: 任务管理（规划→分析→拆分→执行→验证）
- **interactive-feedback**: 人机交互反馈

**协同模式**:
```
深度分析: sequential-thinking → context7 → serena → memory
快速开发: serena → context7 → serena → ide → git
任务管理: shrimp（规划→分析→拆分→执行→验证）
```

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

### 4.2 Claude Skills（5个）

**完整指南** → [.claude/guides/skills-usage.md](.claude/guides/skills-usage.md)

- **lybtzyzs-mvp-compliance**: MVP合规检查（技术黑名单、过度设计）
- **lybtzyzs-arch-compliance**: 架构合规检查（三层架构、依赖方向）
- **lybtzyzs-doc-sync**: 文档同步检查（强制读取规则、变更检测）
- **lybtzyzs-task-breakdown**: 任务分解（设计文档→task清单）
- **lybtzyzs-issue-template**: Issue批量生成（task文档→GitHub Issues）

**触发方式**: 自动（关键词）或手动（明确指定）

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

### 按角色

| 角色 | 推荐路径 |
|-----|---------|
| 🆕 新手开发者 | [getting-started](.claude/guides/getting-started.md) → [issue-workflow](.claude/guides/issue-workflow.md) |
| 👨‍💻 日常开发 | [commands](.claude/reference/commands.md) → [coding-standards](.claude/reference/coding-standards.md) |
| 🏗️ 架构师 | [architecture-philosophy](.claude/explanation/architecture-philosophy.md) → [mvp-philosophy](.claude/explanation/mvp-philosophy.md) |
| 📝 文档维护 | [documentation](.claude/guides/documentation.md) → [doc-sync](.claude/guides/skills-usage.md) |

---

**最后更新**: 2025-10-28（v6.1.1 平衡精简版 - 从531行优化至400行）

**变更历史**:
- v6.1.1（2025-10-28）: 平衡精简优化，再减少25%，保留关键示例
- v6.1（2025-10-28）: 重构为模块化架构，创建15个专门文档，主文档精简94%
- v6.0（2025-10-20）: 新增版本管理规范、验证优先策略、长期目标原则
- v5.0（2025-10-15）: 文档架构三层对齐、新增Spec-Driven工作流
