��# CLAUDE.md

本文件定义 Claude Code 在仓库中的工作约束与执行流程，确保所有改动可追踪、可验证、符合项目标准。

## 📋 导入系统（Modular Architecture）

本文档采用模块化设计，核心规则与模式定义存放在 `.claude/` 目录中：

### 核心规则（Core Modules）
- 📐 `.claude/core/RULES.md` - 工具选择优先级、并行执行策略、代码安全、MVP约束
- 🎯 `.claude/core/PRINCIPLES.md` - 文档先行、最小充分交付、增量优化、记录与可追溯
- 🚩 `.claude/core/FLAGS.md` - 行为模式标志、思考强度分级（think/ultrathink）
- 🔄 `.claude/core/WORKFLOW.md` - Issue驱动工作流（创建→清单→分支→PR→合并→文档）
- 📁 `.claude/core/FILE-ORGANIZATION.md` - 文件创建规则与目录归档规范
- 🖥️ `.claude/core/TOOL-ENVIRONMENT.md` - 项目环境与Claude环境区分、命令对照
- 🚀 `.claude/core/QUICK-START.md` - 5分钟快速上手指南
- 📋 `.claude/core/SPEC-WORKFLOW.md` - Spec-Driven 开发流程（Steering→需求→设计→任务→实施→文档）
- 🔧 `.claude/core/MCP-TOOLS-ORCHESTRATION.md` - MCP 工具协同指南（工具分类、阶段映射、协同模式、实战案例）

### 工作模式（Specialized Modes）
- 🔍 `.claude/modes/code-review.md` - 代码审查模式（规范检查、架构合规、安全性、性能）
- 🏗️ `.claude/modes/architecture.md` - 架构审查模式（三层架构、依赖方向、架构测试）
- ⚡ `.claude/modes/performance.md` - 性能优化模式（N+1查询、内存泄漏、并发问题）
- 🔄 `.claude/modes/refactoring.md` - 重构规划模式（UltraThink 20-30步分析、Phase拆分）
- 🧪 `.claude/modes/testing.md` - 测试驱动模式（AAA模式、Mock配置、覆盖率分析）
- 📝 `.claude/modes/documentation.md` - 文档同步模式（变更检测、索引更新、链接验证）
- 🧠 `.claude/modes/research.md` - 深度研究模式（WebSearch + Context7 + Serena + Sequential-thinking）

### 项目专属Skills（Project-Specific Skills）
- 🔴 `.claude/skills/lybtzyzs-mvp-compliance/` - MVP合规检查（技术黑名单、过度设计检测）
- 🏗️ `.claude/skills/lybtzyzs-arch-compliance/` - 架构合规检查（三层架构、DDD边界验证）
- 📝 `.claude/skills/lybtzyzs-doc-sync/` - 文档同步检查（API变更检测、文档更新清单）

> **📚 使用说明**：
> - Claude Code 会自动加载所有核心规则、模式定义和项目Skills
> - Skills通过符号链接同步到全局目录（首次需运行`scripts/setup-skills.ps1`）
> - 如需查看详细内容，请直接查阅 `.claude/` 目录中的对应文件
> - 所有模式定义基于 SuperClaude Framework 和 CCPM 最佳实践

---

## 1. 角色定位与必读资料

- **定位**：Claude Code 作为智能顾问，负责方案筹划、代码实现、初步审查与文档同步；最终合并由人工审核决定。

### 📚 必读文档（开始任务前）

**核心文档**：
  - `README.md` - 项目权威概览
  - `docs/index.md` - 文档导航中心（v5.0三层对齐架构）
  - `.spec-workflow/steering/structure.md` - 项目结构与组织指南

**快速参考**（80%日常需求）：
  - `docs/quick-reference/` - API参考、配置模板、代码模式、问题解决、开发清单

**架构指南**（三层对齐）：
  - `docs/architecture/server/README.md` - Server端三层架构（8个模块、服务标准）⭐
  - `docs/architecture/client/README.md` - Client端MVVM架构（五层设计、UI标准）⭐
  - `docs/architecture/shared/README.md` - 共享架构（跨端组件、双轨认证）⭐

> **⚠️ 处理任务前必须先查阅 `docs/index.md` 定位相关文档，未理解文档禁止开始编码。**

---

## 1.5 Spec-Driven 与 Issue-Driven 双轨工作流

> **📖 详细流程**：参见 `.claude/core/SPEC-WORKFLOW.md`

**核心机制**：
- **🏛️ Constitution**：`.spec-workflow/steering/constitution.md` - 项目强制性原则（所有任务前必查）
- **✅ Quality Checklists**：`.spec-workflow/templates/checklists/` - 质量检查清单（通过率≥90%）

**工作流场景选择**：

| 场景类型 | Constitution | Checklist | Dashboard审批 | 说明 |
|---------|-------------|-----------|--------------|------|
| **MVP功能**（当前） | ✅ 必须 | ✅ 必须 | ❌ 跳过 | 简化流程，Epic #1343 |
| **重大功能/架构** | ✅ 必须 | ✅ 必须 | ✅ 必须 | MVP完成后启用 |
| **简单Bug/文档** | ✅ 快速 | ❌ 可选 | ❌ 跳过 | 最小流程 |

**当前MVP阶段核心工具**：
- Constitution：`.spec-workflow/steering/constitution.md`
- Checklists：`.spec-workflow/templates/checklists/`
- 任务流程：`docs/development/shared/task-workflow-checklist.md`
- GitHub Issues：Epic #1343（57个子任务）

---

## 1.6 需求讨论与文档化规范

**核心原则**：所有需求讨论必须形成Markdown文档（避免上下文丢失）

**流程**：
1. **讨论前**：创建文档 `docs/architecture/shared/{feature-name}-discussion.md`
2. **讨论中**：逐个问题标记（✅已确认/❌问题/🔄改进/❓待讨论）
3. **讨论后**：文档作为唯一事实来源（Single Source of Truth）

**讨论原则**（一问一答）：
- ✅ 每次只提一个问题（Q1/Q2/Q3），等待用户回答后更新文档
- ❌ 禁止批量提问（同时问Q3/Q4/Q5）

**文档存放**：
- 架构设计 → `docs/architecture/shared/`
- UI/UX设计 → `docs/architecture/client/`
- API设计 → `docs/architecture/server/`

---

## 2. Issue 驱动工作流

> **📖 完整工作流定义**：参见 `.claude/core/WORKFLOW.md`

### ⚠️ 强制性要求：所有任务必须GitHub Issue跟踪

**核心原则**：
- ✅ **所有代码变更**：必须先有GitHub Issue，无Issue禁止任何改动
- ✅ **所有文档修正**：必须先创建GitHub Issue，说明修正原因和范围
- ✅ **所有Bug修复**：必须先创建GitHub Issue，记录复现步骤和修复方案
- ✅ **所有重构优化**：必须先创建GitHub Issue，说明重构目标和影响范围
- ❌ **禁止无Issue工作**：任何"顺手修改"、"临时调整"都必须先创建Issue

**Issue模板要求**：
```markdown
## 📝 任务描述
[清晰描述要做什么]

## 🎯 目标
[要达成什么目标]

## ✅ 验收标准
- [ ] 标准1
- [ ] 标准2

## 📚 参考资料
[相关文档、验证报告、代码位置]
```

**工作流程**：
1. **创建Issue** → 2. **创建分支** → 3. **实现变更** → 4. **创建PR** → 5. **审查合并** → 6. **关闭Issue**

### 2.1 任务启动前置检查

#### 验证优先策略（v6.0新增）⭐⭐⭐
0. **问题验证优先于修复实施** - 避免无效工作的核心原则：
   - **原则**：对于报告中描述的"问题"，先验证问题是否真实存在，再决定是否修复
   - **方法**：使用grep/Read/Bash等工具对比契约、配置、依赖关系，生成验证报告
   - **决策**：
     - ✅ 如验证确认问题存在 → 创建Issue，按Issue驱动流程修复
     - ✅ 如验证证明问题不存在 → 标记为"已验证无需执行"，更新报告
     - ⚠️ 如验证无法确定（编译通过但需运行时验证）→ 标记为"条件执行"
   - **工具链**：sequential-thinking（深度分析） → grep/Read（对比验证） → 生成验证报告
   - **核心价值**：保持0警告基线、避免过度工程、聚焦真实问题

#### 质量检查（v6.0新增）⭐
1. **Constitution合规性检查** - 新功能/重构前必须检查：
   - 是否违反技术黑名单（Redis/CQRS/MediatR/Docker/GraphQL等）
   - 是否符合MVP优先原则（够用即好，避免过度设计）
   - 是否符合三层对齐架构规范
   - 参考：`.spec-workflow/steering/constitution.md`

#### 环境检查
2. `git pull` → 获取最新主分支
3. `dotnet build LYBT.All.sln -c Release --no-restore` → 若失败，优先修复再继续任务
4. `dotnet test LYBT.All.sln -c Release` → 记录基线失败项，评估是否影响任务
   - **推荐配置**：使用 `--settings tests/.runsettings` 启用VS2022兼容配置
   - **注意**：统一编译和测试使用 LYBT.All.sln 方案

### 2.2 完成后的文档系统更新

**🔄 代码与文档并行开发要求**：
- **强制性同步**：代码变更后必须立即更新相关文档，不允许延迟
- **影响评估**：实施前评估文档影响范围，列出需要更新的文档清单
- **及时更新**：开发过程中文档同步进行，不积压到项目结束

**📋 具体更新要求**：
- **架构文档**：更新 `docs/architecture/server/` 或 `docs/architecture/client/` 对应模块文档
- **开发指南**：更新 `docs/development/server/`、`docs/development/client/` 或 `docs/development/shared/` 相关指南
- **API文档**：更新 `docs/api/` 接口文档和Swagger规范
- **快速参考**：影响Level 1文档时，同步更新 `docs/quick-reference/` 相关内容
- **导航索引**：更新 `docs/index.md` 和相关README文档
- **模块文档**：更新对应模块的README和实施指南

---

## 3. 执行原则

> **📖 完整原则定义**：参见 `.claude/core/PRINCIPLES.md` 和 `.claude/core/FLAGS.md`

### 核心原则（9条）
1. **验证优先**：对于任何"问题报告"，先验证问题真实性再实施修复，避免无效工作
2. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则
3. **最小充分交付**：遵循"完成导向、够用即好"，避免超前设计
4. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述
5. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档
6. **文档归位**：按 `documentation-guidelines.md` 与 `file-organization-guidelines.md` 存放，过时文档归档到 `docs/archive/`
7. **MVP 约束**：禁止私自扩展或新增功能；需先更新 MVP 文档/Issue
8. **输出归档**：报告/CSV/日志写入指定目录（`docs/reports/`、`scripts/analysis/outputs/`）
9. **安全与合规**：严格遵守技术黑名单（禁止 Redis、CQRS、Docker、GraphQL 等）

### 文档架构原则（4条）⭐v5.0三层对齐
10. **Server/Client对齐**：文档架构必须保持server/client/shared三层对齐结构
11. **代码文档并行**：代码变更必须同步更新文档，不允许滞后
12. **路径一致性**：所有文档引用必须使用对齐后的新路径格式
13. **定期清理**：及时删除过时文档，保持文档体系精简高效

### 文件组织规范

> **📖 详细规则**：参见 `.claude/core/FILE-ORGANIZATION.md`

**核心原则**：
- ❌ 禁止在根目录创建临时文件
- ✅ 文档归档到 `docs/` 对应分类目录（Level 1/2/3）
- ✅ 脚本归档到 `scripts/` 对应功能目录
- ✅ 输出文件归档到 `docs/reports/` 或 `scripts/analysis/outputs/`
- ✅ Pre-commit hook 会自动检查根目录文件规范

### 高效执行策略
- **并行优先**：Issue 含多个独立子任务时，优先规划并行执行
- **思考强度分级**：
  - `think` (5-10步) → 单文件修改、简单Bug
  - `think hard` (10-15步) → 跨文件重构、中等功能
  - `think harder` (15-20步) → 跨模块需求、架构调整
  - `ultrathink` (20-30步) → 系统级影响、高不确定性

---

## 4. 编码与交付要求

- **Issue 驱动开发**：无 Issue 禁止改动
- **编译质量标准**：所有代码提交前必须通过编译认证，要求 **0 errors, 0 warnings**
- **警告主动修复策略**：≤20个直接修复；>20个创建Issue跟踪
- **语言统一**：代码注释、终端输出、提交信息均使用中文
- **Emoji使用规范**：
  - ❌ 代码中禁用Emoji（.cs/.json/.xml文件）
  - ✅ 文档中允许Emoji（.md文件、Issue/PR描述）
- **文件编码**：所有文本文件使用 `UTF-8 with BOM`
- **命名规范**：
  - 类型与公开成员：`PascalCase`
  - 私有字段：`_camelCase`
  - 常量：`UPPER_SNAKE_CASE`
  - 异步方法：`Async` 结尾
- **依赖注入**：仅用构造函数注入；禁止 `Container.Resolve`、`ServiceLocator`
- **异步约定**：涉及 I/O 必须 async/await，避免阻塞
- **文件体量**：单文件建议 ≤500 行，复杂逻辑拆分模块
- **测试**：新增/修改核心逻辑需补充单元或集成测试
- **文档同步**：改动涉及架构/接口/流程时更新对应 README/索引
- **脚本归档**：新增或调整自动化脚本时，必须放置在 `scripts/` 目录

---

## 5. 工具环境与命令

> **📖 详细说明**：参见 `.claude/core/TOOL-ENVIRONMENT.md`

### 两个环境的区分

| 环境 | 操作系统 | Shell | 用途 |
|------|---------|-------|------|
| **项目运行环境** | Windows 10/11 | PowerShell 7.x+ | 开发、编译、调试 |
| **Claude Code 环境** | Linux | `/usr/bin/bash` | 自动化命令执行 |

### 工具优先级（推荐）

```
⭐⭐⭐ MCP 工具（filesystem, git, serena）- 跨平台，推荐优先使用
⭐⭐ Bash 工具（cat, grep, find 等）- 标准 Unix 命令
⚠️ PowerShell 命令（Get-*, Select-* 等）- 仅项目环境可用
```

### 常用命令速查

```bash
# 项目构建（统一使用 LYBT.All.sln）
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test LYBT.All.sln -c Release
dotnet format LYBT.All.sln

# Claude Code 环境（Bash 或 MCP）
cat file.txt                    # 或 Read tool
grep "pattern" file.txt         # 或 Grep tool
find . -name "*.cs"             # 或 Glob tool
git status                      # 或 mcp__git__git_status
```

---

## 6. MCP 工具使用准则

> **📖 完整工具链参考**：
> - `.claude/core/RULES.md` - 工具选择优先级与执行策略
> - `.claude/core/MCP-TOOLS-ORCHESTRATION.md` - 工具协同指南（⭐ 必读）
> - `docs/development/mcp-tools-reference.md` - 工具快速参考

### 核心工具（优先使用）

| 工具类别 | 核心工具 | 能力 | 优先级 |
|---------|---------|------|--------|
| **开发工具** | serena, filesystem, git, ide | 语义代码编辑、文件操作、版本控制 | ⭐⭐⭐ |
| **知识工具** | context7, microsoft_docs_mcp, memory | 文档查询、知识管理 | ⭐⭐⭐ |
| **工作流工具** | spec-workflow, github, sequential-thinking | Spec流程、任务管理、推理 | ⭐⭐⭐ |
| **测试工具** | playwright | E2E测试、浏览器自动化 | ⭐⭐ |
| **时间工具** | time | 时间标准化 | ⭐⭐ |

### 工具协同流程

**深度分析模式**：
```
sequential-thinking（推理） → context7（验证） → serena（分析） → memory（记录）
```

**快速开发模式**：
```
serena（定位） → context7（查询） → serena（编辑） → ide（验证） → git（提交）
```

---

## 7. 工作模式（7种专业化模式）

> **📖 详细定义**：参见 `.claude/modes/` 目录

### 模式速查

| 模式 | 触发命令 | 用途 |
|-----|---------|------|
| 🔍 Code Review | `/code-review` | 代码规范、架构合规、安全性检查 |
| 🏗️ Architecture | `/review-arch` | 三层架构验证、依赖方向检查 |
| ⚡ Performance | `/analyze-perf` | N+1查询、内存泄漏、并发分析 |
| 🔄 Refactoring | `/refactor-plan` | UltraThink深度分析、Phase拆分 |
| 🧪 Testing | `/generate-tests` | AAA模式测试生成、Mock配置 |
| 📝 Documentation | `/update-docs` | 变更检测、文档同步、链接验证 |
| 🧠 Research | `/deep-research` | 多源研究（WebSearch + Context7） |

---

## 8. Claude Skills 使用指南

> **📖 详细说明**：参见 `.claude/skills/` 目录下各Skill的SKILL.md文件

### 8.1 当前可用的Skills（3个核心）

#### 🔴 MVP合规检查 (lybtzyzs-mvp-compliance)
- **自动检测**：技术黑名单（Redis/CQRS/MediatR/Docker/GraphQL）、依赖注入违规
- **建议确认**：过度设计（Event Sourcing、不必要抽象、过度工厂）

#### 🏗️ 架构合规检查 (lybtzyzs-arch-compliance)
- **自动检测**：依赖方向错误（Application→Presentation、Domain→Application）
- **建议确认**：聚合根边界、Repository粒度

#### 📝 文档同步检查 (lybtzyzs-doc-sync)
- **自动检测**：API端点变更、架构调整、数据模型变更
- **建议确认**：影响范围评估、文档更新清单

### 8.2 Skills 触发方式

**自动触发**（Claude根据description判断）：
- 用户提问包含关键词时自动加载对应Skill
- 例如："检查MVP合规性" → 自动触发 lybtzyzs-mvp-compliance

**手动触发**（明确指定）：
- 在任务描述中明确要求使用某个Skill
- 例如："使用架构合规Skill检查当前代码"

### 8.3 Skills 与 MCP工具/Modes 关系

| 对比维度 | Claude Skills | MCP工具 | Modes |
|---------|--------------|---------|-------|
| **性质** | 项目专属检查逻辑 | 通用能力（文件/代码/Git） | 通用工作流模式 |
| **定义位置** | `.claude/skills/` | Claude Code内置 | `.claude/modes/` |
| **触发方式** | 自动+手动 | 工具调用 | slash命令 |
| **协同关系** | Skills调用MCP工具 | 被Skills/Modes调用 | Modes调用MCP工具 |

**协同示例**：
```
用户："检查MVP合规性"
  → Skills: lybtzyzs-mvp-compliance（自动触发）
    → 调用MCP工具: grep（扫描黑名单） + serena（代码分析） + sequential-thinking（设计评估）
    → 生成报告：违规项（自动） + 建议项（等待确认）
```

---

## 9. 代码修复后的后台清理（Run-to-Completion Hygiene）

为避免测试通过后遗留的运行中后台进程或临时环境状态影响后续验证，完成修复并通过测试后，必须执行以下清理：

### 清理检查清单
- ✅ **终止临时进程**：停止为本次验证启动的 WebAPI/桌面端/脚本
- ✅ **释放资源与缓存**：清理内存缓存/临时文件/本地数据沙箱
- ✅ **还原配置与环境变量**：移除测试期设置的临时变量
- ✅ **关闭外部连接**：断开数据库连接、HTTP 调试代理、自动化会话
- ✅ **证据归档**：将需要保留的日志片段/截图/命令输出收敛到 PR 或 Issue 评论
- ✅ **端口检查**：确认 5001 等端口未被占用
- ✅ **文档同步**：如清理步骤依赖脚本或特定命令，在相关 README 中补充最小指引

---

## 附录：约束调整流程

以上约束如需调整，须先在 GitHub Issue 中提出并获批准，再同步更新本文档及相关标准。
