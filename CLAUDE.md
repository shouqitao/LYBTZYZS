# CLAUDE.md

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

### 工作模式（Specialized Modes）
- 🔍 `.claude/modes/code-review.md` - 代码审查模式（规范检查、架构合规、安全性、性能）
- 🏗️ `.claude/modes/architecture.md` - 架构审查模式（三层架构、依赖方向、架构测试）
- ⚡ `.claude/modes/performance.md` - 性能优化模式（N+1查询、内存泄漏、并发问题）
- 🔄 `.claude/modes/refactoring.md` - 重构规划模式（UltraThink 20-30步分析、Phase拆分）
- 🧪 `.claude/modes/testing.md` - 测试驱动模式（AAA模式、Mock配置、覆盖率分析）
- 📝 `.claude/modes/documentation.md` - 文档同步模式（变更检测、索引更新、链接验证）
- 🧠 `.claude/modes/research.md` - 深度研究模式（WebSearch + Context7 + Serena + Sequential-thinking）

> **📚 使用说明**：
> - Claude Code 会自动加载所有核心规则与模式定义
> - 如需查看详细内容，请直接查阅 `.claude/` 目录中的对应文件
> - 所有模式定义基于 SuperClaude Framework 和 CCPM 最佳实践

---

## 1. 角色定位与必读资料

- **定位**：Claude Code 作为智能顾问，负责方案筹划、代码实现、初步审查与文档同步；最终合并由人工审核决定。
- **必读文档**：
  - `README.md`（项目权威概览）
  - `docs/index.md`（文档导航体系）
  - `docs/development/standards.md`（编码规范）
  - `docs/architecture/server-module-design-standard.md`（Server端三层架构标准）
  - `docs/architecture/client/unified-design-standard.md`（Client端MVVM标准）
  - `docs/development/minimal-practice.md`（Issue→清单→PR 工作法）
  - `docs/development/documentation-guidelines.md`（文档编写与维护指南）
  - `docs/PROJECT-STATUS-2025-09-27.md`（实时项目状态）

> **⚠️ 处理任务前必须先查阅相关文档，未理解文档禁止开始编码或给出建议。**
>
> **架构设计标准**：
> - Server 端开发必须遵循 `server-module-design-standard.md`（三层架构、禁止CQRS、接口统一位置）
> - Client 端开发必须遵循 `client/unified-design-standard.md`（MVVM三层、依赖注入标准、AutoMapper强制、代码模板）

---

## 2. Issue 驱动工作流

> **📖 完整工作流定义**：参见 `.claude/core/WORKFLOW.md`

### 2.1 任务启动前置检查
1. `git pull` → 获取最新主分支
2. `dotnet build LYBT.All.sln -c Release --no-restore` → 若失败，优先修复再继续任务
3. `dotnet test LYBT.All.sln -c Release` → 记录基线失败项，评估是否影响任务
   - **推荐配置**：使用 `--settings tests/.runsettings` 启用VS2022兼容配置
   - **注意**：统一编译和测试使用 LYBT.All.sln 方案

### 2.2 Issue 生命周期（核心要点）
- **单一事实源**：所有改动必须先有 GitHub Issue（含验收标准）
- **模块化清单**：生成带前缀的条目（`[SRV-1]`、`[CLI-1]`、`[DOC-1]`）
- **标签体系**：必选标签（type:*、module:*）+ 推荐标签（priority:*、epic:*）
- **状态标签**：`status:todo` → `status:in-progress` → `status:done`
- **自动化**：PR关联校验、关单兜底、状态同步

### 2.3 PR 与代码审查（关键流程）
1. **分支与提交**：基于 Issue 建分支，提交信息用中文、包含清单编号
2. **PR 模板**：Claude 自动生成草稿（含关单关键字、编译摘要）
3. **AI 审查**：GitHub Copilot 初审（自动） + Claude Code 二审（评论模式，可选）
4. **合并与关闭**：人工审核后合并，Workflow 自动关单

### 2.4 完成后的文档系统更新
- 更新相关模块文档（`docs/architecture/modules/<module>/README.md`）
- 更新需求/功能清单（`docs/issues/`）
- 更新 API/流程/标准文档（`docs/api/`、`docs/development/`、`docs/architecture/`）
- 归档分析报告（`docs/reports/` + `INDEX.md`）
- 更新导航索引（`docs/index.md`）

---

## 3. 执行原则

> **📖 完整原则定义**：参见 `.claude/core/PRINCIPLES.md` 和 `.claude/core/FLAGS.md`

### 核心原则（8条）
1. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则
2. **最小充分交付**：遵循"完成导向、够用即好"，避免超前设计
3. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述
4. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档
5. **文档归位**：按 `documentation-guidelines.md` 与 `file-organization-guidelines.md` 存放
6. **MVP 约束**：禁止私自扩展或新增功能；需先更新 MVP 文档/Issue
7. **输出归档**：报告/CSV/日志写入指定目录（`docs/reports/`、`scripts/analysis/outputs/`）
8. **安全与合规**：严格遵守技术黑名单（禁止 Redis、CQRS、Docker、GraphQL 等）

### 文件组织规范

> **📖 详细规则**：参见 `.claude/core/FILE-ORGANIZATION.md`

**核心原则**：
- ❌ 禁止在根目录创建临时文件（文档/脚本/输出/截图）
- ✅ 文档归档到 `docs/` 对应分类目录
- ✅ 脚本归档到 `scripts/` 对应功能目录
- ✅ 输出文件归档到 `docs/reports/` 或 `scripts/analysis/outputs/`
- ✅ Pre-commit hook 会自动检查根目录文件规范

### 高效执行策略
- **并行优先**：Issue 含多个独立子任务时，优先规划并行执行（标注可并行项 + `sequential-thinking` 评估依赖）
- **思考强度分级**：
  - `think` (5-10步) → 单文件修改、简单Bug
  - `think hard` (10-15步) → 跨文件重构、中等功能
  - `think harder` (15-20步) → 跨模块需求、架构调整
  - `ultrathink` (20-30步) → 系统级影响、高不确定性

---

## 4. 编码与交付要求

- **Issue 驱动开发**：无 Issue 禁止改动
- **语言统一**：代码注释、终端输出、提交信息均使用中文
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

> **📖 完整工具链参考**：`.claude/core/RULES.md` + `docs/development/mcp-tools-reference.md`

### 核心工具（优先使用）

- **filesystem / git / serena** - 文件操作、版本控制、语义代码编辑（⭐⭐⭐ 推荐）
- **context7** - 查询库文档与权威资料
- **sequential-thinking** - 结构化推理与任务拆解
- **memory** - 知识图谱存储
- **time** - 时间标准化

### AI 协同流程

Context7（资料） → Sequential-thinking（拆解） → Serena（代码编辑） → Git（记录）

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

**使用说明**：
- 自动识别：Claude 根据用户请求自动选择模式
- 强制指定：使用 slash 命令（如 `/refactor-plan`）
- 模式组合：复杂任务可串联多个模式（Performance → Issue → Refactoring → PR）

---

## 9. 代码修复后的后台清理（Run-to-Completion Hygiene）

为避免测试通过后遗留的运行中后台进程或临时环境状态影响后续验证，完成修复并通过测试后，必须执行以下清理：

### 清理检查清单
- ✅ **终止临时进程**：停止为本次验证启动的 WebAPI/桌面端/脚本（如 `dotnet run`）
- ✅ **释放资源与缓存**：清理内存缓存/临时文件/本地数据沙箱（`BIN/`, `logs/`, `TestResults/` 等）
- ✅ **还原配置与环境变量**：移除测试期设置的临时变量（如 `ASPNETCORE_URLS`）、测试密钥/连接串
- ✅ **关闭外部连接**：断开数据库连接、HTTP 调试代理、自动化会话
- ✅ **证据归档**：将需要保留的日志片段/截图/命令输出收敛到 PR 或 Issue 评论
- ✅ **端口检查**：确认 5001 等端口未被占用
- ✅ **文档同步**：如清理步骤依赖脚本或特定命令，在 `docs/development/minimal-practice.md` 或相关 README 中补充最小指引

---

## 附录：约束调整流程

以上约束如需调整，须先在 GitHub Issue 中提出并获批准，再同步更新本文档及相关标准。

**📌 快速参考**：
- Issue 默认创建在 GitHub 上
- 积极使用 `sequential-thinking` MCP 工具和 `serena` MCP 工具
- 所有用到时间的地方使用 `time` MCP 工具获取最新时间