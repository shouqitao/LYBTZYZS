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
- 📝 `.claude/skills/lybtzyzs-doc-sync/` - 文档同步检查（⭐⭐⭐ 强化版）
  - **新增能力**：需求分析/设计文档前置检查、架构调整文档同步强制验证
  - **覆盖范围**：所有重要文档体系（Level 0-3、业务规则、技术决策）
  - **强制规则**：拒绝未读文档的需求分析、拒绝未同步文档的架构调整

> **📚 使用说明**：
> - Claude Code 会自动加载所有核心规则、模式定义和项目Skills
> - Skills通过符号链接同步到全局目录（首次需运行`scripts/setup-skills.ps1`）
> - 如需查看详细内容，请直接查阅 `.claude/` 目录中的对应文件
> - 所有模式定义基于 SuperClaude Framework 和 CCPM 最佳实践

---

## 1. 角色定位与必读资料

## 0.5 项目基础信息（固定参数）

### 📦 仓库信息

**GitHub仓库参数**（用于MCP工具调用）：
```
Owner: shouqitao
Repo:  LYBTZYZS
URL:   https://github.com/shouqitao/LYBTZYZS
```

> **⚠️ 重要**：使用GitHub MCP工具时，必须显式提供owner和repo参数：
> - ✅ 正确：`mcp__github__list_issues(owner="shouqitao", repo="LYBTZYZS")`
> - ❌ 错误：GitHub MCP不支持默认仓库配置，每次调用都必须提供参数

### 🔧 技术栈版本

**核心框架**：
- .NET: 8.0
- WPF: .NET 8.0
- ASP.NET Core: 8.0
- Entity Framework Core: 8.0.0
- Prism: 9.0.x
- Avalonia: 11.2.x (跨平台桌面端)

**数据库**：
- SQL Server 2022 Express (开发环境)
- SQL Server 2022 (生产环境)

**MCP工具栈**：
- serena: 代码语义分析与编辑
- filesystem: 文件系统操作
- github: GitHub API集成
- context7: 技术文档查询
- microsoft_docs_mcp: Microsoft官方文档
- sequential-thinking: 深度推理分析
- drawio: Draw.io图表创建与编辑（架构图、流程图、UML图）

---

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

## 1.6 需求讨论与文档化规范（三阶段流程）

**核心原则**：需求讨论 → 需求文档 → 设计文档 → Issue → 实施

### ⚠️ 强制性文档读取规则（⭐⭐⭐ 必须遵守）

**适用场景**：所有需求分析、设计文档生成、架构调整任务

#### 规则1：需求分析前必须先阅读文档体系

**执行流程**：
1. **拒绝未读文档的请求**：
   - 用户要求"生成需求文档"或"写需求分析"时，必须先拒绝
   - 提示："⚠️ 需求分析前必须先阅读文档体系，请确认是否已阅读相关文档？"

2. **强制文档阅读清单**：
   ```markdown
   📚 需求分析前必读文档：

   核心必读（100%必须）：
   - docs/index.md - 文档导航中心
   - docs/business-rules.md - 14条核心业务规则
   - docs/architecture/{server|client|shared}/README.md - 对应层架构指南

   模块相关（根据需求选择）：
   - docs/modules/{module-name}/README.md - 相关模块文档
   - docs/api/{module-name}-api.md - 相关API文档
   - docs/quick-reference/code-patterns.md - 代码模式参考
   ```

3. **验证文档阅读**：
   - 使用Read工具读取核心文档
   - 生成文档要点摘要，证明已理解
   - 用户确认后才继续需求分析

#### 规则2：设计文档前必须先阅读架构指南

**执行流程**：
1. **拒绝未读架构文档的设计请求**：
   - 用户要求"写设计文档"时，必须先确认已阅读对应架构指南
   - 提示："⚠️ 设计文档前必须先阅读架构指南，请确认是否已理解架构约束？"

2. **强制架构文档阅读**：
   - Server端设计 → 必读 `docs/architecture/server/README.md`
   - Client端设计 → 必读 `docs/architecture/client/README.md`
   - 跨端设计 → 必读 `docs/architecture/shared/README.md`
   - 深度设计 → 选读 `docs/deep/advanced-patterns.md`

#### 规则3：架构调整前必须先更新文档

**执行流程**：
1. **拒绝未同步文档的架构变更**：
   - 用户要求"重构XXX"或"新增YYY模块"时，必须先拒绝
   - 提示："⚠️ 架构调整前必须先更新文档，请确认是否已更新ADR和架构文档？"

2. **强制文档更新流程**：
   ```markdown
   🏗️ 架构调整文档同步流程：

   Step 1: 创建ADR（Architecture Decision Record）
   - 在 docs/architecture/decisions/ 创建 ADR-XXX.md
   - 记录架构决策背景、方案对比、后果分析
   - 状态标记：Proposed → Accepted → Implemented

   Step 2: 更新架构文档
   - 更新 docs/architecture/{server|client|shared}/README.md
   - 如新增模块，创建 docs/modules/{module-name}/README.md
   - 更新 docs/index.md 导航链接

   Step 3: 更新架构例外清单（如有违反）
   - 在 docs/architecture/exceptions.md 记录例外
   - 说明批准理由和补救措施

   Step 4: 确认后开始代码变更
   - 用户审查ADR和文档更新
   - 批准后创建Issue并开始实施
   ```

#### 违反处理：强制终止任务

**如果检测到以下违反行为，必须立即终止任务**：
- ❌ 未读取文档体系就生成需求文档
- ❌ 未读取架构指南就生成设计文档
- ❌ 未更新ADR和架构文档就进行架构调整
- ❌ 生成的需求/设计文档未引用相关架构文档和业务规则

**终止提示**：
```
⚠️ 任务终止：违反文档读取强制规则

原因：[具体违反行为]
要求：必须先完成文档阅读/更新流程
参考：CLAUDE.md 第1.6节 - 强制性文档读取规则
```

---

### 阶段1：需求讨论（Discussion）

**目标**：理解业务需求和痛点，确定核心目标

**流程**：
1. **创建讨论文档**：`docs/architecture/{client|server|shared}/{feature-name}-discussion.md`
2. **讨论内容**：
   - ❓ 问题（Q1, Q2, Q3...）：逐个提问，一问一答
   - ✅ 答案：用户回答后标记确认
   - 💡 简要说明：核心思路、方案对比、关键决策点
3. **讨论原则**（快速交流）：
   - ✅ 每次只提一个问题，等待用户回答
   - ✅ 用方案对比表、简洁图示快速呈现选项
   - ✅ 只讨论"做什么"和"为什么"，不讨论"怎么实现"
   - ❌ 禁止批量提问
   - ❌ **严禁编写代码**：不要写任何代码示例、XAML、C#、SQL等
   - ❌ **严禁详细技术说明**：不要写具体的类名、方法签名、参数列表
   - ❌ **严禁冗长分析**：避免长篇大论，保持讨论高效

**输出**：确认的业务需求和关键决策点

### 阶段2：需求文档（Requirements）

**目标**：形成正式的功能需求规格说明

**流程**：
1. **创建需求文档**：`docs/requirements/{feature-name}-requirements.md`
2. **文档内容**：
   - 📋 功能概述（业务价值、用户故事）
   - 🎯 业务目标（解决什么问题）
   - ✅ 验收标准（功能完成的判定条件）
   - 🔗 关联Issues（整合的现有Issues）
   - 📊 优先级和时间估算
3. **避免内容**：技术实现、代码细节、具体API设计

**输出**：需求文档作为唯一事实来源（Single Source of Truth）

**⚠️ 强制性规则**：
- ✅ **生成需求文档后,必须等待用户确认**,不得自动进入设计阶段
- ✅ **用户可能指出需求理解错误**,必须根据反馈修正需求文档
- ✅ **只有在用户明确确认需求文档后**,才能进入阶段3生成设计文档
- ❌ **禁止跳过确认环节**,避免基于错误需求生成错误设计

### 阶段3：设计文档（Design）

**前置条件**：⚠️ **用户已确认阶段2的需求文档无误**

**目标**：提供完整的技术设计和实施指导

**流程**：
1. **创建设计文档**：`docs/design/{feature-name}-design.md`
2. **文档内容**：
   - 🏗️ 架构设计（组件关系、数据流）
   - 🔧 技术方案（API端点、DTO设计、数据库Schema）
   - 📝 代码示例（关键逻辑的伪代码或真实代码）
   - 📊 Phase拆分（实施步骤和时间估算）
   - ✅ 质量标准（编译、测试、性能要求）
3. **设计深度**：
   - ✅ 具体到可以直接指导编码
   - ✅ 包含XAML布局、ViewModel属性、API接口等细节
   - ✅ 明确所有技术决策和约束

**输出**：设计文档作为编码的蓝图

### 阶段4：Issue创建与实施

**流程**：
1. **用户审查设计文档**：确认技术方案可行
2. **通过后创建Epic Issue**：引用设计文档
3. **按设计文档实施**：严格遵循设计，避免临时改动
4. **验证通过后合并**：运行时验证 + 编译通过

**文档存放**：
- 讨论文档 → `docs/architecture/{client|server|shared}/`
- 需求文档 → `docs/requirements/`
- 设计文档 → `docs/design/`

---

## 2. Issue 驱动工作流（单人开发优化版）

> **📖 简化工作流**：参见 `.claude/core/WORKFLOW-SIMPLIFIED.md`
> **📖 传统工作流**：参见 `.claude/core/WORKFLOW.md`（团队协作场景）

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
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证通过
- [ ] 功能完整可用（从用户视角）

## 📚 参考资料
[相关文档、验证报告、代码位置]
```

### 🔄 单人开发简化工作流

> **核心思想**：小Issue直接Master，Epic创建PR并及时合并

#### 🟢 小Issue → 直接提交Master（90%场景）

**判断标准**：
- ✅ 单一Bug修复（<5个文件）
- ✅ 代码量 <200行
- ✅ 单模块改动
- ✅ 开发时间 <2小时
- ✅ 不需要架构调整

**工作流程**：
```bash
# 1. 创建Issue
gh issue create --title "修复XXX问题" --body "..."

# 2. 在master上修改代码
git checkout master
git pull origin master

# 3. 编译 + 运行时验证（⚠️ 必须）
dotnet build LYBT.All.sln -c Release --no-restore
# 启动应用，测试修复功能，确认问题真正解决

# 4. 提交并关联Issue（自动关闭）
git add .
git commit -m "fix(module): 修复XXX问题

Fixes #1234

- 具体改动1
- 具体改动2
- 验证：功能已正常工作

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"

# 5. 推送到master（Issue自动关闭）
git push origin master
```

**关键点**：
- ✅ `Fixes #1234` 会自动关闭Issue
- ✅ 必须运行时验证，不能只编译通过
- ✅ 无需创建分支和PR

#### 🔴 Epic → 创建PR并及时合并（10%场景）

**判断标准**：
- ✅ 跨模块重构（Client+Server）
- ✅ 架构调整（循环依赖、三层对齐）
- ✅ 代码量 >200行
- ✅ 开发时间 >2小时
- ✅ 需要详细记录变更历史

**工作流程**：
```bash
# 1. 创建Epic分支
git checkout -b epic/issue-1234-description

# 2. 多次提交（每个子任务一次）
git commit -m "feat: 完成子功能A"
git commit -m "feat: 完成子功能B"

# 3. 运行时验证（⚠️ 必须）
dotnet build LYBT.All.sln -c Release --no-restore
# 启动应用，完整测试Epic功能

# 4. 创建PR
gh pr create --title "Epic #1234: XXX功能实现" --body "..."

# 5. ⚠️ 关键：1-3天内必须合并
gh pr merge --squash --delete-branch

# 6. 关闭Issue
gh issue close 1234
```

**关键约束**：
- ⚠️ PR必须在**1-3天内合并或关闭**，避免积压
- ⚠️ 创建PR前必须完成运行时验证
- ⚠️ 不允许PR长期挂起

### 2.3 渐进式Bug修复策略（避免反复开发）

> **问题场景**：修复"暂存病案"功能，发现病案可以暂存，但诊断记录未保存

#### 📋 推荐方案：扩展原Issue + 分阶段commit

**Step 1: 发现深层问题 → 更新Issue**
```markdown
## Issue #1234：暂存病案功能修复（扩展）

### 问题描述（更新）
~~暂存病案无法实现~~ → 已部分修复
新发现：病案可以暂存，但关联数据缺失

### 修复范围（扩展）
- [x] Phase 1: 病案状态暂存
- [ ] Phase 2: 诊断记录同步保存
- [ ] Phase 3: 处方数据同步保存

### 验收标准（从用户视角）
- [ ] 病案状态正确保存
- [ ] 诊断记录完整保存
- [ ] 处方数据完整保存
- [ ] 继续看诊时数据正确加载
```

**Step 2: 在master上分阶段commit**
```bash
# Phase 1修复
git commit -m "fix(medicalcase): 暂存病案 - Phase 1: 病案状态保存

Issue #1234 (Part 1/3)
- 验证：病案可以暂存"
git push origin master

# Phase 2修复
git commit -m "fix(medicalcase): 暂存病案 - Phase 2: 诊断记录保存

Issue #1234 (Part 2/3)
- 验证：诊断记录正确保存"
git push origin master

# Phase 3修复（关闭Issue）
git commit -m "fix(medicalcase): 暂存病案 - Phase 3: 处方数据保存

Issue #1234 (Part 3/3)
Fixes #1234
- 验证：完整功能可用"
git push origin master
```

**✅ 优势**：
1. 避免分支divergence（所有修复顺序进行）
2. Issue完整性（一个功能Bug对应一个Issue）
3. 渐进式验证（每个Phase独立commit）
4. 清晰的修复演进过程

**❌ 不推荐**：拆分成多个Issue（会导致PR积压和覆盖问题）

---

### 2.4 架构问题升级策略

> **判断标准**：发现小问题演变成架构问题时的处理流程

#### 场景A：仍是小修复（继续在原Issue）
```
发现：诊断记录字段映射缺失
影响：1个文件，AutoMapper配置
方案：补充字段映射
```
→ ✅ **继续在原Issue的下一个Phase修复**

#### 场景B：发现架构问题（升级为Epic）
```
发现：Consultation/Prescription生命周期管理混乱
影响：需要重构聚合根边界
方案：重构MedicalCase聚合根
```
→ ⚠️ **升级处理流程**：

**Step 1: 在原Issue中标记**
```markdown
## Issue #1234：暂存病案功能修复

### 架构问题发现
Phase 3发现聚合根边界混乱，需要重构。
已创建Epic #1240处理。

**临时方案**：Workaround保证功能可用
**长期方案**：等待Epic #1240重构完成

### 验收标准（调整）
- [x] 处方数据保存（临时方案）← 标记为临时
```

**Step 2: 创建Epic Issue**
```markdown
## Epic #1240：重构MedicalCase聚合根边界

### 问题来源
从Issue #1234发现的架构问题

### 实施计划
- [ ] 设计聚合根边界方案
- [ ] 更新Repository实现
- [ ] 移除Issue #1234的Workaround

### 影响范围
估计工作量：3-5天
```

**Step 3: 完成原Issue（用临时方案）**
```bash
git commit -m "fix(medicalcase): 暂存功能 - Phase 3临时方案

Fixes #1234
Related to Epic #1240

⚠️ 技术债：需要重构聚合根（Epic #1240）"
```

**Step 4: Epic分支处理重构**
```bash
git checkout -b epic/issue-1240-aggregate-refactor
# 多次commit完成重构
gh pr create --title "Epic #1240: 重构聚合根"
# ⚠️ 1-3天内必须合并
gh pr merge --squash --delete-branch
```

---

### 2.5 任务启动前置检查

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

### 2.6 完成标准与文档更新

#### ✅ 任务完成的三层验证标准

**Level 1 - 编译验证（必需）**：
- ✅ 0 errors, 0 warnings
- ✅ 所有引用正确
- ✅ 类型检查通过

**Level 2 - 静态分析（推荐）**：
- ✅ 代码逻辑正确
- ✅ 符合架构规范
- ✅ 没有明显Bug

**Level 3 - 运行时验证（⚠️ 强制）**：
- ✅ 启动应用（Client + Server）
- ✅ 执行具体操作场景
- ✅ 验证数据库状态
- ✅ 确认问题真正解决
- ✅ **从用户视角验证功能完整可用**

**❌ 禁止行为**：
- ❌ 只编译通过就认为完成
- ❌ 只写代码不测试运行
- ❌ 部分功能可用就关闭Issue

**✅ 正确的完成标准**：
```
验证通过 + push到master + Issue自动关闭 = 任务完成
```

#### 🔄 代码与文档并行开发要求

**强制性同步**：
- **实施前评估**：列出需要更新的文档清单
- **开发中同步**：代码变更后立即更新文档，不允许延迟
- **完成前检查**：确认所有相关文档已更新

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

### 核心原则（10条）
1. **验证优先**：对于任何"问题报告"，先验证问题真实性再实施修复，避免无效工作
2. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则
3. **最小充分交付**：遵循"完成导向、够用即好"，避免超前设计
4. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述
5. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档
6. **文档归位**：按 `documentation-guidelines.md` 与 `file-organization-guidelines.md` 存放，过时文档归档到 `docs/archive/`
7. **MVP 约束**：禁止私自扩展或新增功能；需先更新 MVP 文档/Issue
8. **输出归档**：报告/CSV/日志写入指定目录（`docs/reports/`、`scripts/analysis/outputs/`）
9. **安全与合规**：严格遵守技术黑名单（禁止 Redis、CQRS、Docker、GraphQL 等）
10. **⭐立足长期目标**：所有架构调整必须立足于未来3-5年演进目标，遵循**渐进式演进原则**（参见ADR-005）

### 长期目标原则（ADR-005）⭐v6.0新增

> **📖 完整定义**：参见 `docs/architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md`

**核心要求**：
- ✅ **立足未来3-5年**：架构设计必须考虑业务增长（10倍数据量、5倍团队规模）
- ✅ **渐进式演进**：避免推倒重来，每次演进成本5-15天（可控）
- ✅ **明确触发条件**：6个量化指标（业务规则数、Service方法长度、聚合根关系、状态机复杂度、团队规模、数据量）
- ✅ **Constitution可调整**：允许基于充分证据调整技术约束（需创建ADR + 更新例外清单）
- ❌ **禁止过早优化**：未达到触发条件前，保持简化实现

**7条长期架构原则**（ADR-005）：
1. **渐进式演进而非推倒重来**：Service层协调 → 富领域模型 → 领域事件（按需演进）
2. **架构边界清晰而非过度抽象**：Controller/Service/Repository职责严格分离
3. **业务规则集中管理**：所有业务规则在Service层验证，必须文档化
4. **聚合根边界稳定**：MedicalCase聚合根边界不随意调整
5. **技术选型符合Constitution**：当前禁止Redis/CQRS/MediatR，未来可基于证据调整
6. **演进触发条件明确**：6个量化指标 + 阈值（如业务规则 >20条触发演进）
7. **Constitution约束可调整**：允许基于充分证据（业务需求 + MVP替代方案评估 + ROI >2倍）调整

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

## 3.5 版本管理规范（⭐v6.1新增）

> **核心原则**：MVP阶段避免大版本频繁跳跃，优先通过小版本迭代演进。

### 📌 语义化版本策略

**当前策略**：保持 **1.x.x.x** 系列稳定演进

```
✅ 推荐版本演进路径：
- 1.0.0  → MVP首个版本
- 1.1.0  → 功能增量（新增模块/端点）
- 1.2.0  → 功能优化（性能/体验提升）
- 1.x.0  → 持续小版本迭代

❌ 避免大版本频繁跳跃：
- 1.0.0 → 2.0.0 → 3.0.0（频繁破坏性变更）
- v1 → v2 → v3（API版本混乱）
```

**替代方案**：
- **0.x.x.x 系列**：如果明确标记"开发阶段"，可使用0.x版本（MVP发布后升级到1.0.0）
- **1.x.x.x 系列**：1.0作为MVP版本，后续通过1.1/1.2/1.3迭代（当前推荐）

### 🔄 API版本管理原则

**API路由版本**：
- ✅ 当前保持 `/api/v1/*` 稳定
- ✅ MVP阶段通过功能扩展而非版本升级
- ⏸️ v2升级仅在重大里程碑（如MVP发布后的架构重构）

**禁止行为**：
- ❌ MVP阶段随意升级API版本（v1 → v2）
- ❌ 为小功能变更创建新版本
- ❌ 同时维护多个API版本（增加复杂度）

### 📋 版本升级触发条件

**允许大版本升级的场景**：
1. **重大架构重构**：如聚合根边界重设计、领域模型转换
2. **破坏性API变更**：必须修改现有端点契约
3. **技术栈重大升级**：如.NET 8 → .NET 10
4. **MVP发布后的里程碑**：1.0 → 2.0仅在重大产品迭代时

**小版本迭代的场景**（1.x → 1.y）：
1. 新增端点/功能（向后兼容）
2. Bug修复和性能优化
3. 文档和工具改进
4. 内部实现优化（不影响API契约）

### 📦 版本号统一管理

**统一版本号位置**：
```
version.txt（项目根目录）
---
1.0.0
```

**同步更新清单**（版本变更时必须全部同步）：
1. **version.txt**：项目唯一版本号定义（Single Source of Truth）
2. **项目文件**：
   - `src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`（AssemblyVersion）
   - `src/Client/Desktop/Shell/LYBT.Desktop.Shell/LYBT.Desktop.Shell.csproj`（AssemblyVersion）
3. **API Controller**：
   - `[ApiVersion("1")]` 保持v1稳定（除非重大变更）
4. **文档**：
   - `README.md`：项目版本说明
   - `docs/CHANGELOG.md`：版本变更记录
   - `docs/index.md`：文档版本标记

**版本变更流程**：
```bash
# 1. 更新version.txt
echo "1.1.0" > version.txt

# 2. 创建Issue说明版本变更原因
gh issue create --title "版本升级：1.0.0 → 1.1.0" --body "..."

# 3. 更新所有相关文件（上述清单）
# 4. 更新CHANGELOG.md记录变更内容
# 5. Commit统一提交
git commit -m "chore: 版本升级到1.1.0"

# 6. 创建Git Tag
git tag v1.1.0
git push origin v1.1.0
```

### ⚠️ 强制规则

**在CLAUDE.md明确禁止**：
- ❌ 未经充分评估的大版本升级
- ❌ MVP阶段的API版本频繁变更
- ❌ 仅为"看起来更现代"而升级版本
- ❌ 版本号不同步（代码、API、文档版本不一致）

**必须遵循**：
- ✅ 所有版本变更必须创建Issue说明原因
- ✅ 大版本升级需要ADR记录决策
- ✅ API版本升级需要影响评估报告
- ✅ **版本号变更必须同步所有位置**（version.txt → 代码 → 文档）

---

## 4. 编码与交付要求

### 4.1 核心质量标准

- **Issue 驱动开发**：无 Issue 禁止改动
- **编译质量标准**：所有代码提交前必须通过编译认证，要求 **0 errors, 0 warnings**
- **运行时验证标准**（⭐⭐⭐ 强制）：
  - ✅ 启动应用，执行真实操作场景
  - ✅ 验证数据库状态（必要时检查数据）
  - ✅ 从用户视角确认功能完整可用
  - ❌ 禁止只编译通过就提交
  - ❌ 禁止"看起来没问题"就关闭Issue
- **警告主动修复策略**：≤20个直接修复；>20个创建Issue跟踪

### 4.2 代码规范

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

### 4.3 测试与文档

- **测试**：新增/修改核心逻辑需补充单元或集成测试
- **文档同步**：改动涉及架构/接口/流程时更新对应 README/索引
- **脚本归档**：新增或调整自动化脚本时，必须放置在 `scripts/` 目录

### 4.4 提交规范

**Commit Message 格式**：
```bash
<type>(<scope>): <subject>

Fixes #1234  # 自动关闭Issue（小Issue）
Related to Epic #1234  # 关联Epic但不关闭

- 具体改动1
- 具体改动2
- 验证：功能已正常工作  # ⚠️ 必须包含验证说明

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

**Type 类型**：
- `feat`: 新功能
- `fix`: Bug修复
- `refactor`: 重构（不改变功能）
- `docs`: 文档更新
- `test`: 测试相关
- `chore`: 构建/工具配置

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
| **工作流工具** | spec-workflow, github, sequential-thinking, shrimp-task-manager, interactive-feedback | Spec流程、任务管理、推理、人机交互 | ⭐⭐⭐ |
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

### MCP 工具使用原则（⭐⭐⭐ 强制）

**核心原则**：优先使用MCP第三方优秀工具，提升效率和准确性

#### 1. 工具选择优先级

```
第1优先级：MCP 工具（跨平台、稳定、推荐）
  - filesystem: 文件读写、目录操作
  - serena: 代码语义分析、符号搜索
  - github: GitHub API集成（Issue/PR/Commit）
  - context7: 技术文档查询（最新官方文档）
  - microsoft_docs_mcp: Microsoft官方文档
  - sequential-thinking: 深度推理分析

第2优先级：Claude Code内置工具
  - Read/Write/Edit: 文件操作
  - Glob/Grep: 文件搜索和内容搜索
  - Bash: Shell命令执行

第3优先级：Shell命令
  - 仅在MCP工具无法满足需求时使用
```

#### 2. GitHub MCP 固定参数要求

**问题**：GitHub MCP工具不支持默认仓库配置，每次调用都必须显式提供参数。

**解决方案**：使用固定的仓库参数（见0.5章）：

```python
# ✅ 正确示例
mcp__github__list_issues(
    owner="shouqitao",
    repo="LYBTZYZS",
    state="OPEN"
)

mcp__github__create_issue(
    owner="shouqitao",
    repo="LYBTZYZS",
    title="修复XXX问题",
    body="问题描述..."
)

# ❌ 错误示例（会导致参数错误）
mcp__github__list_issues(state="OPEN")  # 缺少owner和repo
```

**常用GitHub MCP工具**：
- `list_issues`: 列出Issues
- `create_issue`: 创建Issue
- `update_issue`: 更新Issue
- `list_pull_requests`: 列出PR
- `create_pull_request`: 创建PR
- `merge_pull_request`: 合并PR
- `list_commits`: 列出提交
- `get_file_contents`: 获取文件内容

#### 3. Context7 使用规范

**推荐场景**：
- 查询最新技术文档（.NET 8、WPF、Prism等）
- 查询GitHub MCP Server使用方法
- 验证API用法和最佳实践

**使用流程**：
```
1. resolve-library-id: 查找库ID
2. get-library-docs: 获取文档（指定topic和tokens）
```

**示例**：
```python
# Step 1: 解析库ID
mcp__context7__resolve-library-id(libraryName="github mcp server")

# Step 2: 获取文档
mcp__context7__get-library-docs(
    context7CompatibleLibraryID="/github/github-mcp-server",
    topic="configuration environment variables",
    tokens=5000
)
```

#### 4. 工具组合模式

**研究模式**（查询最新文档）：
```
context7（查文档） → sequential-thinking（分析） → 应用到代码
```

**开发模式**（语义代码编辑）：
```
serena（符号搜索） → serena（代码编辑） → filesystem（验证）
```

**GitHub工作流**：
```
github（创建Issue） → 代码修改 → github（创建PR） → github（合并PR）
```

#### 5. interactive-feedback（人机交互反馈工具）⭐ v6.1新增

**工具库ID**：`/noopstudios/interactive-feedback-mcp`

**核心能力**：
- 提供人机交互反馈循环（Human-in-the-Loop）
- 在AI执行任务过程中获取用户确认和反馈
- 适用于需要用户参与决策的工作流

**使用场景**：
- ✅ 关键操作前需要用户确认（如删除文件、修改配置）
- ✅ 需要用户提供额外输入或选择方案
- ✅ 验证AI生成的代码或文档质量
- ✅ 多阶段任务中的中间检查点

**工具调用**：
```python
# 请求用户反馈
mcp__interactive-feedback__interactive_feedback(
    project_directory="/absolute/path/to/project",  # 项目绝对路径
    summary="简短的变更摘要（一行）"                # 变更描述
)
```

**典型工作流**：
```
1. AI执行操作（如代码生成、文件修改）
2. 调用interactive_feedback获取用户反馈
3. 根据反馈调整后续行动
4. 继续执行或回滚变更
```

**最佳实践**：
- 在关键决策点使用，避免频繁打断用户
- 提供清晰的summary说明做了什么变更
- 根据反馈及时调整策略

#### 6. shrimp-task-manager（智能任务管理工具）⭐ v6.1新增

**工具库ID**：`/cjo4m06/mcp-shrimp-task-manager`

**核心能力**：
- 为AI Agent提供结构化任务管理框架
- 支持任务规划、分解、执行、验证全流程
- 提供任务记忆和上下文管理
- 集成Git追踪任务历史

**核心操作**（6个主要工具）：

##### 6.1 任务规划（plan_task）
```python
mcp__shrimp-task-manager__plan_task(
    description="完整详细的任务问题描述，包含任务目标、背景及预期成果",
    requirements="可选：任务的特定技术要求、业务约束条件或质量标准",
    existingTasksReference=False  # 是否参考现有任务作为规划基础
)
```

**何时使用**：
- 开始新功能开发前
- 收到复杂需求时
- 需要系统性规划时

##### 6.2 任务分析（analyze_task）
```python
mcp__shrimp-task-manager__analyze_task(
    summary="结构化的任务摘要，包含任务目标、范围与关键技术挑战，最少10个字符",
    initialConcept="最少50个字符的初步解答构想，包含技术方案、架构设计和实施策略",
    previousAnalysis="可选：前次迭代的分析结果，用于持续改进方案"
)
```

**何时使用**：
- 需要深度分析技术方案
- 评估实施可行性
- 识别潜在风险

##### 6.3 任务拆分（split_tasks）
```python
mcp__shrimp-task-manager__split_tasks(
    updateMode="clearAllTasks",  # clearAllTasks | append | overwrite | selective
    tasksRaw="[{name:'任务名称', description:'详细描述', dependencies:[], ...}]",
    globalAnalysisResult="可选：任务最终目标，来自之前分析适用于所有任务的通用部分"
)
```

**拆分粒度控制**：
- 单个子任务：1-2工作日（8-16小时）
- 避免跨技术域（frontend + backend + database应拆分）
- 推荐6-8个子任务/批次
- 任务树深度≤3层

**何时使用**：
- 将Epic拆分为可执行的小任务
- 明确任务依赖关系
- 分配优先级和时间估算

##### 6.4 任务执行（execute_task）
```python
mcp__shrimp-task-manager__execute_task(
    taskId="UUID格式的任务ID"
)
```

**返回内容**：
- 任务详细信息（名称、描述、实施指南）
- 相关文件列表
- 验证标准
- 依赖关系

**何时使用**：
- 获取任务执行指导
- 查看实施细节
- 确认验证标准

##### 6.5 任务列表（list_tasks）
```python
mcp__shrimp-task-manager__list_tasks(
    status="all"  # all | pending | in_progress | completed
)
```

**何时使用**：
- 查看当前任务状态
- 选择下一个待执行任务
- 跟踪整体进度

##### 6.6 任务验证（verify_task）
```python
mcp__shrimp-task-manager__verify_task(
    taskId="UUID格式的任务ID",
    score=85,  # 0-100分，≥80分自动完成任务
    summary="任务完成摘要或缺失/修正部分说明，最少30个字"
)
```

**验证标准**（4个维度）：
1. **需求符合度（30%）**：功能完整性、约束遵守、边界处理
2. **技术质量（30%）**：架构一致性、代码健壮性、实现优雅性
3. **集成兼容性（20%）**：系统集成、互操作性、兼容性维护
4. **性能可扩展性（20%）**：性能优化、负载适应、资源管理

**何时使用**：
- 任务完成后的质量检查
- 确认是否达到验收标准
- 决定任务是否可以关闭

**高级功能**：

##### 深度思考（process_thought）
```python
mcp__shrimp-task-manager__process_thought(
    thought="思维内容",
    thought_number=1,
    total_thoughts=10,
    next_thought_needed=True,
    stage="Problem Definition"  # 可选：Information Gathering, Research, Analysis等
)
```

**使用场景**：
- 需要深度推理的复杂问题
- 多步骤分析（5-30步）
- 不确定性高的架构决策

##### 研究模式（research_mode）
```python
mcp__shrimp-task-manager__research_mode(
    topic="要研究的编程主题内容，应该明确且具体",
    currentState="当前Agent主要该执行的内容",
    nextSteps="后续的计划、步骤或研究方向",
    previousState=""  # 可选：之前的研究状态和内容摘要
)
```

**使用场景**：
- 技术调研（新框架、最佳实践）
- 方案比对（多种实现方式）
- 深度学习（复杂技术领域）

**工具协同示例**：

**完整Epic开发流程**：
```
1. plan_task（规划）
   → 生成任务规划指导

2. analyze_task（分析）
   → 深度技术方案分析

3. split_tasks（拆分）
   → 生成8个子任务（18-24小时）

4. list_tasks（查看）
   → 选择pending任务

5. execute_task（执行）
   → 获取实施指南

6. [编写代码、运行验证]

7. verify_task（验证）
   → 质量检查（≥80分完成）

8. 重复4-7直到所有任务完成
```

**与interactive-feedback协同**：
```
shrimp（规划任务）
  → interactive-feedback（用户确认方案）
  → shrimp（执行任务）
  → interactive-feedback（用户验证结果）
  → shrimp（完成任务）
```

**最佳实践**：
- ✅ 使用research_mode进行技术调研
- ✅ 使用process_thought进行深度分析
- ✅ 任务拆分遵循粒度控制（1-2天/任务）
- ✅ 验证评分客观公正（参考4个维度）
- ❌ 避免任务拆分过细（<4小时）或过大（>3天）
- ❌ 禁止跳过验证环节直接标记完成

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

### 8.1 当前可用的Skills（5个核心）

#### 🔴 MVP合规检查 (lybtzyzs-mvp-compliance)
- **自动检测**：技术黑名单（Redis/CQRS/MediatR/Docker/GraphQL）、依赖注入违规
- **建议确认**：过度设计（Event Sourcing、不必要抽象、过度工厂）

#### 🏗️ 架构合规检查 (lybtzyzs-arch-compliance)
- **自动检测**：依赖方向错误（Application→Presentation、Domain→Application）
- **建议确认**：聚合根边界、Repository粒度

#### 📝 文档同步检查 (lybtzyzs-doc-sync)
- **自动检测**：API端点变更、架构调整、数据模型变更
- **建议确认**：影响范围评估、文档更新清单

#### 📋 任务分解生成 (lybtzyzs-task-breakdown) ⭐ v1.0新增
- **核心能力**：从设计文档自动生成结构化任务分解清单
- **输入**：设计文档（docs/design/*.md）
- **输出**：标准化task文档（docs/tasks/*.md）
- **功能**：智能任务拆分、依赖关系分析、工作量估算、Phase划分
- **触发关键词**：任务分解、生成任务清单、task breakdown

#### 📝 Issue模板生成（批量模式）(lybtzyzs-issue-template) ⭐ v1.2增强
- **核心能力**：从task文档批量生成GitHub Issues
- **输入**：Task文档（docs/tasks/*.md）
- **输出**：批量GitHub Issues（自动关联Epic、标注依赖）
- **功能**：批量创建、依赖关系标注、Epic自动关联
- **触发关键词**：批量创建Issues、根据task文档生成Issues

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

**协同示例1：合规性检查**：
```
用户："检查MVP合规性"
  → Skills: lybtzyzs-mvp-compliance（自动触发）
    → 调用MCP工具: grep（扫描黑名单） + serena（代码分析） + sequential-thinking（设计评估）
    → 生成报告：违规项（自动） + 建议项（等待确认）
```

**协同示例2：任务分解与Issue创建**（v1.0新增工作流）：
```
设计文档 → lybtzyzs-task-breakdown → Task文档 → lybtzyzs-issue-template（批量） → GitHub Issues

具体流程：
1. 设计文档完成（docs/design/xxx-design.md）
2. task-breakdown生成任务清单（8个任务，18-24小时）
3. issue-template批量创建Issues（自动关联Epic #1494）
4. GitHub追踪Epic进度（8/8 Issues）

参考：.claude/skills/README.md
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
