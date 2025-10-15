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

> **📚 使用说明**：
> - Claude Code 会自动加载所有核心规则与模式定义
> - 如需查看详细内容，请直接查阅 `.claude/` 目录中的对应文件
> - 所有模式定义基于 SuperClaude Framework 和 CCPM 最佳实践

---

## 1. 角色定位与必读资料

- **定位**：Claude Code 作为智能顾问，负责方案筹划、代码实现、初步审查与文档同步；最终合并由人工审核决定。
- **必读文档**：
  - `README.md`（项目权威概览）
  - `docs/index.md`（文档导航体系，v4.0精简版）
  - `docs/architecture/server/design-standard.md`（Server端三层架构标准）⭐v4.0更新
  - `docs/architecture/client/unified-design-standard.md`（Client端MVVM标准）
  - `docs/development/shared/documentation-guidelines.md`（文档编写与维护指南）⭐v4.0更新

- **快速参考**（v4.0新增）：
  - `docs/quick-reference/README.md` - 快速参考文档中心（解决80%日常需求）
  - `docs/quick-reference/api_reference.md` - API快速参考
  - `docs/quick-reference/config_templates.md` - 配置模板
  - `docs/quick-reference/code_patterns.md` - 代码模式
  - `docs/quick-reference/troubleshooting.md` - 问题解决方案
  - `docs/quick-reference/development_checklist.md` - 开发检查清单

> **⚠️ 处理任务前必须先查阅相关文档，未理解文档禁止开始编码或给出建议。**
>
> **架构设计标准**：
> - Server 端开发必须遵循 `docs/architecture/server/design-standard.md`（三层架构、禁止CQRS、接口统一位置）⭐v4.0对齐架构
> - Client 端开发必须遵循 `docs/architecture/client/unified-design-standard.md`（MVVM三层、依赖注入标准、AutoMapper强制、代码模板）
> - 共享架构标准参考 `docs/architecture/shared/README.md`（跨端架构决策、技术标准）⭐v4.0新增

---

## 1.5 Spec-Driven 与 Issue-Driven 双轨工作流

本项目采用 **Spec-Driven + Issue-Driven** 双轨开发模式，充分利用 spec-workflow-mcp 工具链实现"想清楚再做"的高质量开发流程。

### 工作流关系图

```
阶段划分：

Spec-Driven（前置思考阶段）
  ├─ 阶段 1：项目初始化
  │   └─ 创建 Steering Documents (product.md, tech.md, structure.md)
  │
  ├─ 阶段 2：需求分析
  │   └─ 创建 requirements.md → Dashboard 审批
  │
  ├─ 阶段 3：设计
  │   └─ 创建 design.md → Dashboard 审批
  │
  └─ 阶段 4：任务分解
      └─ 创建 tasks.md → Dashboard 审批 → 生成 GitHub Issues
                    ↓
Issue-Driven（执行实施阶段）
  ├─ 阶段 5：开发实施
  │   └─ 基于 GitHub Issues 进行代码开发
  │
  ├─ 阶段 6：测试验证
  │   └─ 单元测试 + 集成测试 + E2E 测试
  │
  └─ 阶段 7：PR 审查与文档
      └─ 创建 PR → 代码审查 → 合并 → 文档更新
```

### 双轨模式使用场景

| 场景类型 | Spec-Driven | Issue-Driven | 说明 |
|---------|------------|-------------|------|
| **新功能开发** | ✅ 必须 | ✅ 必须 | 完整流程：Spec → Issue → 实现 |
| **重大重构** | ✅ 必须 | ✅ 必须 | 需求+设计审批后分解任务 |
| **Bug 修复** | ❌ 可选 | ✅ 必须 | 简单 Bug 直接 Issue，复杂 Bug 需 Spec |
| **文档更新** | ❌ 不需要 | ✅ 必须 | 直接创建 Issue |
| **性能优化** | ✅ 推荐 | ✅ 必须 | 建议先设计方案再实施 |

### 核心工具与使用指引

- **📋 Spec-Workflow**：完整工作流指南参见 `.claude/core/SPEC-WORKFLOW.md`
- **🔄 Issues 同步**：`spec-workflow-mcp: manage-tasks` 同步 GitHub Issues 到 tasks.md
- **🔧 MCP 工具协同**：工具选择与协同模式参见 `.claude/core/MCP-TOOLS-ORCHESTRATION.md`
- **🔄 Issue 管理**：Issue 驱动流程参见下方"## 2. Issue 驱动工作流"

### Dashboard 访问

```bash
# 自动启动（推荐）
# MCP 配置中添加 --AutoStartDashboard 参数

# 手动启动
npx -y @pimzino/spec-workflow-mcp@latest D:\source\repos\LYBTZYZS --dashboard

# 默认地址：http://localhost:3000
```

---

## 2. Issue 驱动工作流

> **📖 完整工作流定义**：参见 `.claude/core/WORKFLOW.md`

### 2.1 任务启动前置检查
1. `git pull` → 获取最新主分支
2. `dotnet build LYBT.All.sln -c Release --no-restore` → 若失败，优先修复再继续任务
3. `dotnet test LYBT.All.sln -c Release` → 记录基线失败项，评估是否影响任务
   - **推荐配置**：使用 `--settings tests/.runsettings` 启用VS2022兼容配置
   - **注意**：统一编译和测试使用 LYBT.All.sln 方案

### 2.2 GitHub Issues 创建流程（从 Spec 到 Issue）

#### 2.2.1 批量创建 Issues（Epic + 子任务）
1. **从 tasks.md 生成 Issues**：
   - 创建 Epic Issue：`[Epic] 功能名称 (SPEC-编号)`
   - 为每个 Task 创建子 Issue：`[Spec: feature-name] [类型-N] 任务描述`
   - 关联 Spec 文档链接（requirements.md / design.md / tasks.md）

2. **Issue 内容标准**：
   ```
   ## 📋 关联 Spec
   - Epic: #链接
   - 需求文档: path/to/requirements.md
   - 设计文档: path/to/design.md
   - 任务文档: path/to/tasks.md

   ## 📝 任务描述
   [详细描述]

   ## ✅ 验收标准
   - [ ] 标准1
   - [ ] 标准2

   ## 🔗 依赖任务
   - Depends on: #链接

   ## ⏱️ 工作量估算
   X小时/天

   ## 📚 参考资料
   ```

3. **标签体系**：
   - 必选：`type:task/epic`, `module:*`
   - 推荐：`priority:*`, `epic:*`

#### 2.2.2 更新 tasks.md 添加 Issue 链接
- 为每个任务添加 Issue 链接：`- [ ] Task N: 描述 (#编号)`
- 在文档顶部添加 Epic Issue 链接
- 保持任务状态与 Issue 同步

#### 2.2.3 Issue 生命周期管理
- **单一事实源**：所有改动必须先有 GitHub Issue（含验收标准）
- **模块化清单**：生成带前缀的条目（`[SRV-1]`、`[CLI-1]`、`[DOC-1]`）
- **状态标签**：`status:todo` → `status:in-progress` → `status:done`
- **自动化**：PR关联校验、关单兜底、状态同步

### 2.3 PR 与代码审查（关键流程）
1. **分支与提交**：基于 Issue 建分支，提交信息用中文、包含清单编号
2. **PR 模板**：Claude 自动生成草稿（含关单关键字、编译摘要）
3. **AI 审查**：GitHub Copilot 初审（自动） + Claude Code 二审（评论模式，可选）
4. **合并与关闭**：人工审核后合并，Workflow 自动关单

### 2.4 完成后的文档系统更新

**🔄 代码与文档并行开发要求**：
- **强制性同步**：代码变更后必须立即更新相关文档，不允许延迟
- **影响评估**：实施前评估文档影响范围，列出需要更新的文档清单
- **及时更新**：开发过程中文档同步进行，不积累到项目结束

**📋 具体更新要求**：
- **架构文档**：更新 `docs/architecture/server/` 或 `docs/architecture/client/` 对应模块文档
- **开发指南**：更新 `docs/development/server/`、`docs/development/client/` 或 `docs/development/shared/` 相关指南
- **API文档**：更新 `docs/api/` 接口文档和Swagger规范
- **快速参考**：影响Level 1文档时，同步更新 `docs/quick-reference/` 相关内容
- **导航索引**：更新 `docs/index.md` 和相关README文档
- **模块文档**：更新对应模块的README和实施指南

**✅ 文档同步检查清单**：
- [ ] 架构设计文档是否反映最新代码结构
- [ ] 开发指南是否包含最新的开发流程
- [ ] API文档是否与实际接口一致
- [ ] 快速参考是否包含新增API或配置
- [ ] 导航链接是否有效且指向正确路径
- [ ] 所有相关README是否已更新

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

### 文档架构原则（4条）⭐v4.0新增
9. **Server/Client对齐**：文档架构必须保持server/client/shared三层对齐结构
10. **代码文档并行**：代码变更必须同步更新文档，不允许滞后
11. **路径一致性**：所有文档引用必须使用对齐后的新路径格式
12. **定期清理**：及时删除过时文档，保持文档体系精简高效

### 文件组织规范

> **📖 详细规则**：参见 `.claude/core/FILE-ORGANIZATION.md`

**核心原则**：
- ❌ 禁止在根目录创建临时文件（文档/脚本/输出/截图）
- ✅ 文档归档到 `docs/` 对应分类目录，遵循Server/Client/Shared对齐架构
- ✅ 架构文档：`docs/architecture/server/`、`docs/architecture/client/`、`docs/architecture/shared/`
- ✅ 开发指南：`docs/development/server/`、`docs/development/client/`、`docs/development/shared/`
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
- **文档同步**：改动涉及架构/接口/流程时更新对应 README/索引，遵循Server/Client/Shared对齐路径
- **脚本归档**：新增或调整自动化脚本时，必须放置在 `scripts/` 目录
- **文档影响评估**：实施前必须评估需要更新的文档清单，代码变更后立即执行文档更新
- **路径标准化**：所有新增文档必须遵循对齐架构路径，禁止随意放置

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

**Spec-Driven 完整流程**：
```
spec-workflow（需求/设计） → sequential-thinking（分析） → github（任务） →
serena（开发） → git（提交） → github（PR） → filesystem（文档）
```

> **💡 详细工具协同模式**：参见 `.claude/core/MCP-TOOLS-ORCHESTRATION.md`，包含：
> - 工具分类与能力矩阵
> - 7 个阶段的工具映射
> - 5 种协同模式详解
> - 10 个核心工具使用指南
> - 完整实战案例

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
- **新功能/重构**：必须先走 Spec-Driven 流程（requirements → design → tasks → 审批）
- **Issue 管理**：所有任务默认创建在 GitHub 上
- **Dashboard**：访问 http://localhost:3000 进行 Spec 文档审批
- **核心工具**：积极使用 `sequential-thinking`、`serena`、`spec-workflow` MCP 工具
- **时间标准**：所有时间相关操作使用 `time` MCP 工具获取标准时间
- **工具协同**：参考 `.claude/core/MCP-TOOLS-ORCHESTRATION.md` 选择最优工具组合