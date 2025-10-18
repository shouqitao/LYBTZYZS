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

### 📚 v5.0 文档体系（三层对齐架构）

**必读文档**：
  - `README.md` - 项目权威概览
  - `docs/index.md` - 文档导航体系（v5.0彻底重构版，三层对齐架构）
  - `.spec-workflow/steering/structure.md` - 项目结构与组织指南

**Level 1：快速参考（解决80%日常需求）**：
  - `docs/quick-reference/README.md` - 快速参考中心导航
  - `docs/quick-reference/api-reference.md` - API快速参考
  - `docs/quick-reference/config-templates.md` - 配置模板
  - `docs/quick-reference/code-patterns.md` - 代码模式
  - `docs/quick-reference/troubleshooting.md` - 问题解决方案
  - `docs/quick-reference/development-checklist.md` - 开发检查清单

**Level 2：架构指南（三层对齐）**：
  - `docs/architecture/README.md` - 架构总览（Server/Client/Shared对齐）⭐v5.0核心
  - `docs/architecture/server/README.md` - Server端三层架构（8个模块、服务标准）⭐v5.0
  - `docs/architecture/client/README.md` - Client端MVVM架构（五层设计、UI标准）⭐v5.0
  - `docs/architecture/shared/README.md` - 共享架构（跨端组件、双轨认证）⭐v5.0
  - `docs/development/README.md` - 开发指南总览
  - `docs/development/server/README.md` - Server端开发规范
  - `docs/development/client/README.md` - Client端开发规范
  - `docs/development/shared/README.md` - 共享开发规范

**Level 3：深度参考（5%深度需求）**：
  - `docs/deep/` - 高级设计模式、性能优化、测试策略、部署指南、API最佳实践
  - `docs/api/README.md` - 完整API文档
  - `docs/modules/README.md` - 8个业务模块详细说明

> **⚠️ 处理任务前必须先查阅相关文档，未理解文档禁止开始编码或给出建议。**
>
> **v5.0 架构设计标准（三层对齐）**：
> - **Server端**：必须遵循 `docs/architecture/server/README.md`（三层架构、8个模块、服务标准）
> - **Client端**：必须遵循 `docs/architecture/client/README.md`（MVVM五层、WPF标准、UI规范）
> - **共享层**：必须遵循 `docs/architecture/shared/README.md`（跨端组件、双轨认证、技术决策）
> - **核心原则**：所有文档严格对应代码架构，100%准确同步

---

## 1.5 Spec-Driven 与 Issue-Driven 双轨工作流（方案3：分场景使用）

本项目采用 **Spec-Driven + Issue-Driven** 双轨开发模式，结合 **Constitution + Quality Checklists** 双重质量保障机制（借鉴spec-workflow-mcp和spec-kit最佳实践）。

### 质量保障机制（v6.0增强）⭐新增

**核心机制**：
- **🏛️ Constitution（项目宪法）**：`.spec-workflow/steering/constitution.md`
  - 定义项目强制性原则（架构、代码质量、安全、开发流程）
  - 分级执行（MUST/SHOULD/MAY）
  - 所有新功能/重构前必须先检查Constitution合规性

- **✅ Quality Checklists（质量检查清单）**：`.spec-workflow/templates/checklists/`
  - 必选清单：`requirements-checklist.md`、`security-checklist.md`
  - 可选清单：`ux-checklist.md`、`performance-checklist.md`、`accessibility-checklist.md`
  - 每个Spec目录创建对应checklists/子目录，复制模板并逐项验证
  - Implementation完成后必须通过Checklist验证（≥90%）才能提交PR

---

### 工作流分场景策略（方案3）⭐重要

根据任务类型选择合适的流程：

#### 场景1：MVP功能实现（当前阶段 #1343，57个任务）🎯

**简化流程**（无需spec-workflow-mcp工具，无需Dashboard审批）：

```
Constitution检查 → Issue创建 →
创建Spec目录 + 复制Checklist模板 →
（可选）编写requirements.md/design.md →
填写Checklist（自我验证，≥90%） →
开发实施（参考Checklist） →
Checklist最终验证（≥90%） →
文档同步 →
创建PR（附Checklist结果） → PR审查 → 合并
```

**特点**：
- ✅ 保留Constitution检查（质量底线）
- ✅ 保留Checklist验证（多维度质量保障）
- ❌ **跳过Dashboard审批**（加快迭代速度）
- ❌ **不需要启动spec-workflow-mcp工具**
- ⚡ 快速迭代，适合单人开发

---

#### 场景2：重大功能/架构调整（MVP完成后）

**完整流程**（启用spec-workflow-mcp工具 + Dashboard审批）：

```
Spec-Driven（前置思考 + Dashboard审批）
  ├─ 阶段0：Constitution检查
  ├─ 阶段1：创建requirements.md + Checklist
  ├─ 阶段2：Dashboard审批requirements.md ⭐启用
  ├─ 阶段3：创建design.md + 更新Checklist
  ├─ 阶段4：Dashboard审批design.md ⭐启用
  ├─ 阶段5：创建tasks.md + 可选Checklist
  └─ 阶段6：Dashboard审批tasks.md → 生成GitHub Issues ⭐启用
                    ↓
Issue-Driven（执行实施 + 质量验证）
  ├─ 开发实施（参考Checklist）
  ├─ 测试验证 + Checklist验证（≥90%）
  └─ 创建PR → 审查 → 合并
```

**判断标准**（满足任一条件则启用完整流程）：
- ❓ 是否影响核心架构（三层架构调整）？
- ❓ 是否涉及数据模型重大变更（新增表/重要字段）？
- ❓ 是否引入新技术栈/第三方库？
- ❓ 是否影响安全模型（双轨认证变更）？
- ❓ 是否为跨模块重构（影响≥3个模块）？

**工具要求**：
- ✅ 启动spec-workflow-mcp工具
- ✅ 访问Dashboard审批 (http://localhost:3000)
- ✅ 完整Spec-Driven流程

---

#### 场景3：简单Bug/文档更新

**最小流程**（Issue驱动）：

```
Constitution检查（快速） → Issue创建 → 代码修复 → PR → 合并
```

**特点**：
- ✅ 快速响应
- ❌ 无需Spec
- ❌ 无需Checklist（简单任务）
- ✅ 保留Constitution基本检查

---

### 当前MVP阶段工作流（#1343，57个任务）

**适用场景1流程**，具体步骤：

#### 阶段0：任务启动
- [ ] Constitution合规性检查（技术黑名单、MVP原则、三层架构）
- [ ] Issue已存在（来自Epic #1343的子任务）
- [ ] 环境检查（git pull、build、test）

#### 阶段1：创建Spec结构（手动，无需工具）
- [ ] 创建目录：`.spec-workflow/specs/{spec-name}/checklists/`
- [ ] 复制模板：`requirements-checklist.md`、`security-checklist.md`
- [ ] 可选：复制`ux-checklist.md`、`performance-checklist.md`（根据功能类型）

#### 阶段2：需求与设计（简化，可选）
- [ ] **可选**：编写`requirements.md`（简单功能可跳过，直接填Checklist）
- [ ] **可选**：编写`design.md`（简单功能可跳过）
- [ ] **必须**：填写`requirements-checklist.md`（自我验证）
- [ ] **必须**：填写`security-checklist.md`（自我验证）
- [ ] **跳过**：~~Dashboard审批~~（MVP阶段不需要）

#### 阶段3：开发实施
- [ ] 创建功能分支：`feature/{issue-id}-{description}`
- [ ] 参考Checklist要求实施代码
- [ ] 编写单元测试（覆盖率≥80%核心逻辑）
- [ ] 本地验证（build + test + 功能测试）

#### 阶段4：质量验证
- [ ] 填写Checklist实施阶段检查项
- [ ] 计算通过率（必选≥90%，可选≥80%）
- [ ] 文档同步（如影响架构/API）

#### 阶段5：PR提交
- [ ] 创建PR，附上Checklist验证结果摘要
- [ ] PR描述包含：
  - Checklist通过率
  - 未通过项说明
  - 测试覆盖率
  - 文档变更清单
- [ ] PR审查 → 合并 → Issue自动关闭

---

### 核心工具与使用指引

**当前MVP阶段（场景1）**：
- **🏛️ Constitution**：`.spec-workflow/steering/constitution.md`（必读）
- **✅ Checklists**：`.spec-workflow/templates/checklists/`（必用）
- **📋 任务流程**：`docs/development/shared/task-workflow-checklist.md`（执行参考）
- **🔄 GitHub Issues**：Epic #1343及57个子任务
- **🔧 MCP工具**：serena、filesystem、git、sequential-thinking、context7

**MVP完成后（场景2）**：
- **📋 spec-workflow-mcp**：启动Dashboard审批
- **🔄 Issues同步**：`spec-workflow-mcp: manage-tasks`
- **🌐 Dashboard**：http://localhost:3000

---

### 双轨模式使用场景总结

| 场景类型 | Constitution | Checklist | Dashboard审批 | spec-workflow-mcp工具 | 说明 |
|---------|-------------|-----------|--------------|---------------------|------|
| **MVP功能** | ✅ 必须 | ✅ 必须 | ❌ 跳过 | ❌ 不需要 | 当前阶段（#1343） |
| **重大功能/架构** | ✅ 必须 | ✅ 必须 | ✅ 必须 | ✅ 启动 | MVP完成后 |
| **复杂Bug** | ✅ 必须 | ✅ 推荐 | ❌ 跳过 | ❌ 不需要 | 根据影响范围 |
| **简单Bug** | ✅ 快速 | ❌ 不需要 | ❌ 跳过 | ❌ 不需要 | 最小流程 |
| **文档更新** | ✅ 快速 | ❌ 不需要 | ❌ 跳过 | ❌ 不需要 | 直接Issue |

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

---

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
   - **实战案例**：参见 `docs/reports/contract-verification-report-2025-10-18.md`
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

### 2.3 PR 与代码审查（关键流程 - v6.0质量增强）
1. **分支与提交**：基于 Issue 建分支，提交信息用中文、包含清单编号
2. **Checklist验证**（⭐新增必选步骤）：
   - 完成 `.spec-workflow/specs/{spec-name}/checklists/` 下所有检查项
   - 必选清单通过率必须≥90%（requirements + security）
   - 可选清单根据功能类型选择（ux/performance/accessibility）
   - 在PR描述中附上Checklist验证结果摘要
3. **PR 模板**：Claude 自动生成草稿（含关单关键字、编译摘要、Checklist摘要）
4. **AI 审查**：GitHub Copilot 初审（自动） + Claude Code 二审（评论模式，可选）
5. **合并与关闭**：人工审核后合并，Workflow 自动关单

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

### 核心原则（9条）
1. **验证优先**：对于任何"问题报告"，先验证问题真实性再实施修复，避免无效工作（v6.0新增⭐⭐⭐）
   - 使用 sequential-thinking 深度分析 → grep/Read 对比验证 → 生成验证报告
   - 决策：问题存在→修复；问题不存在→标记"已验证无需执行"；不确定→标记"条件执行"
   - 实战案例：`docs/reports/contract-verification-report-2025-10-18.md`
2. **文档先行**：方案、审查、实现均以 `docs/` 现有规范为最高准则
3. **最小充分交付**：遵循"完成导向、够用即好"，避免超前设计
4. **增量优化**：禁止无指令的推倒重写；建议以 diff 形式描述
5. **记录与可追溯**：任何决策、范围变化须回写至 Issue/文档
6. **文档归位**：按 `documentation-guidelines.md` 与 `file-organization-guidelines.md` 存放，过时文档归档到 `docs/archive/`
7. **MVP 约束**：禁止私自扩展或新增功能；需先更新 MVP 文档/Issue
8. **输出归档**：报告/CSV/日志写入指定目录（`docs/reports/`、`scripts/analysis/outputs/`）
9. **安全与合规**：严格遵守技术黑名单（禁止 Redis、CQRS、Docker、GraphQL 等）

### 文档架构原则（4条）⭐v5.0三层对齐
9. **Server/Client对齐**：文档架构必须保持server/client/shared三层对齐结构（v5.0彻底重构）
10. **代码文档并行**：代码变更必须同步更新文档，不允许滞后（100%准确同步）
11. **路径一致性**：所有文档引用必须使用对齐后的新路径格式（17个核心文档）
12. **定期清理**：及时删除过时文档，保持文档体系精简高效（已删除50+过时文档）

### 文件组织规范

> **📖 详细规则**：参见 `.claude/core/FILE-ORGANIZATION.md`

**核心原则（v5.0三层对齐架构）**：
- ❌ 禁止在根目录创建临时文件（文档/脚本/输出/截图）
- ✅ 文档归档到 `docs/` 对应分类目录，严格遵循Server/Client/Shared三层对齐架构
- ✅ **Level 1**：快速参考 `docs/quick-reference/`（80%日常需求）
- ✅ **Level 2**：架构指南 `docs/architecture/server|client|shared/`（15%学习需求）
- ✅ **Level 2**：开发指南 `docs/development/server|client|shared/`（15%学习需求）
- ✅ **Level 3**：深度参考 `docs/deep/`、`docs/api/`、`docs/modules/`（5%深度需求）
- ✅ **归档目录**：`docs/archive/`（过时文档/旧清单/历史报告）
  - `docs/archive/tasks/` - 已完成或废弃的任务清单
  - `docs/archive/reports/` - 历史验证报告（可选）
  - `docs/archive/specs/` - 已实施完成的Spec文档（可选）
- ✅ 脚本归档到 `scripts/` 对应功能目录
- ✅ 输出文件归档到 `docs/reports/`（当前报告） 或 `scripts/analysis/outputs/`（分析输出）
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
- **编译质量标准**：所有代码提交前必须通过编译认证，要求 **0 errors, 0 warnings**
  - 使用 `dotnet build LYBT.All.sln -c Release --no-restore` 验证
  - 任何警告（CS8xxx、CS0xxx 等）必须在提交前修复
  - 禁止提交包含编译警告的代码
- **警告主动修复策略**（v6.0新增）：
  - ✅ **少量警告直接修复**：如果编译警告≤20个，或警告类型相对较少（如仅1-2种类型），不管是否本次任务引入，都应尽可能在当前任务中直接修复
  - ⚠️ **大量警告需Issue跟踪**：如果警告数量>20个，或警告类型复杂多样，必须创建单独的GitHub Issue进行跟踪处理，不应在当前任务中强行修复
  - 🎯 **判断标准**：
    - 警告数量：≤20个 → 直接修复；>20个 → 创建Issue
    - 警告类型：1-2种类型 → 直接修复；≥3种复杂类型 → 创建Issue
    - 修复复杂度：简单修改（如添加null检查）→ 直接修复；需要架构调整 → 创建Issue
  - 📋 **Issue模板**（大量警告时）：标题格式 `[Tech Debt] 修复XX类型编译警告（N个）`，包含警告清单和影响范围
- **语言统一**：代码注释、终端输出、提交信息均使用中文
- **Emoji使用规范**（v6.0新增）：
  - ❌ **代码中禁用Emoji**：C#代码（.cs文件）、配置文件（.json/.xml）、数据库字符串中不允许使用Emoji字符
  - ✅ **文档中允许Emoji**：Markdown文档（.md文件）、CLAUDE.md、README、Issue/PR描述中可以使用Emoji增强可读性
  - 🎯 **示例**：
    - 代码注释：`// 验证失败`（正确） vs `// ❌ 验证失败`（错误）
    - 文档标题：`## 验证优先策略（v6.0新增）⭐⭐⭐`（正确，文档允许）
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

---

## 📌 v5.0 文档体系快速参考

### 文档体系结构（三层对齐）
```
docs/
├── index.md                          # 文档中心导航（入口）
├── quick-reference/                  # Level 1: 快速参考（80%需求）
│   ├── README.md                     # 快速参考中心
│   ├── api-reference.md              # API快速参考
│   ├── config-templates.md           # 配置模板
│   ├── code-patterns.md              # 代码模式
│   ├── troubleshooting.md            # 问题解决
│   └── development-checklist.md      # 开发清单
├── architecture/                     # Level 2: 架构指南（15%需求）
│   ├── README.md                     # 架构总览
│   ├── server/README.md              # Server端架构⭐
│   ├── client/README.md              # Client端架构⭐
│   └── shared/README.md              # 共享架构⭐
├── development/                      # Level 2: 开发指南（15%需求）
│   ├── README.md                     # 开发总览
│   ├── server/README.md              # Server端开发
│   ├── client/README.md              # Client端开发
│   └── shared/README.md              # 共享开发
├── deep/                             # Level 3: 深度参考（5%需求）
│   ├── advanced-patterns.md          # 高级设计模式
│   ├── performance-optimization.md   # 性能优化
│   ├── testing-strategies.md         # 测试策略
│   ├── deployment-guide.md           # 部署指南
│   └── api-design-best-practices.md  # API最佳实践
├── api/README.md                     # Level 3: 完整API文档
├── modules/README.md                 # Level 3: 8个业务模块
└── support/                          # Level 4: 支撑体系
    ├── documentation-metrics.md      # 文档使用指标
    └── documentation-maintenance.md  # 文档维护指南
```

### 核心特性
- ✅ **100%代码同步**：所有文档基于实际代码分析创建
- ✅ **三层对齐架构**：Server/Client/Shared严格对应代码结构
- ✅ **17个核心文档**：从100+精简到17个，删除50+过时文档
- ✅ **80/15/5需求分层**：快速参考解决80%日常需求
- ✅ **3次点击定位**：任何文档3次点击内找到
- ✅ **双轨认证系统**：Users表 + AdminSecrets表物理隔离
- ✅ **8个业务模块**：Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula

### 工作流快速参考（v6.0质量增强 - 方案3分场景）

**当前MVP阶段**（#1343，57个任务）：
- **Constitution检查**：任务前必查 `.spec-workflow/steering/constitution.md`（技术黑名单、MVP原则、架构）
- **Quality Checklists**：从 `.spec-workflow/templates/checklists/` 复制到Spec目录，填写验证（≥90%）
- **简化流程**：Constitution检查 → Spec目录 → Checklist验证 → 开发 → PR（无需Dashboard审批）
- **spec-workflow-mcp工具**：**MVP阶段不需要启动**，手动创建Spec目录结构即可
- **任务流程参考**：`docs/development/shared/task-workflow-checklist.md`

**MVP完成后**（重大功能/架构调整）：
- **完整Spec-Driven流程**：requirements → design → tasks → Dashboard审批 → Issue
- **启动spec-workflow-mcp**：`npx -y @pimzino/spec-workflow-mcp@latest D:\source\repos\LYBTZYZS --dashboard`
- **Dashboard审批**：访问 http://localhost:3000 进行文档审批
- **判断标准**：影响架构/数据模型/新技术栈/安全模型/跨模块重构

**通用指引**：
- **Issue管理**：所有任务必须有GitHub Issue
- **核心MCP工具**：serena、filesystem、git、sequential-thinking、context7
- **时间标准**：使用 `time` MCP工具获取标准时间
- **工具协同**：参考 `.claude/core/MCP-TOOLS-ORCHESTRATION.md`
- **文档导航**：从 `docs/index.md` 开始，选择对应Level

### 架构标准速查
- **Server端**：`docs/architecture/server/README.md` - 三层架构、8个模块
- **Client端**：`docs/architecture/client/README.md` - MVVM五层、WPF标准
- **共享层**：`docs/architecture/shared/README.md` - 跨端组件、双轨认证
- **快速查询**：`docs/quick-reference/` - 日常开发80%问题