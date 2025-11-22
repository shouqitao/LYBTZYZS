# CLAUDE.md 优化方案

**文档版本**: v1.0
**创建日期**: 2025-10-28
**关联Issue**: #1711
**状态**: 待审核

---

## 📋 执行摘要

当前CLAUDE.md文档约9000行，信息密度过高，查找困难。本方案基于**Diátaxis文档框架**和**Node.js Best Practices**，将CLAUDE.md重构为模块化、层次化的文档体系，预期主文档精简至500行（减少94%），查找效率提升75%。

**关键指标**：
- 主文档行数：9000 → 500（减少94%）
- 查找时间：2-3分钟 → 30秒（提升75%）
- 新手学习时间：2小时 → 30分钟（减少75%）
- 文档数量：1个 → 15个模块（模块化）

---

## 🔍 当前问题分析

### 1. 信息过载
- 单文件包含所有规则、原则、工作流、工具说明
- 需要大量滚动才能找到所需内容
- 信息密度高，认知负担大

### 2. 查找困难
- 缺少清晰的导航和索引
- 相关内容分散在多个章节
- 常见场景查找时间2-3分钟

### 3. 重复内容
- 主文档引用了`.claude/`模块，但仍包含详细说明
- 约30%内容重复，违反Single Source of Truth原则

### 4. 混合目的
- 新手指南、高级参考、操作手册混在一起
- 缺少"Tutorial → How-to → Reference → Explanation"的渐进路径

### 5. 维护困难
- 新增内容不知道放在哪
- 容易造成文档持续膨胀
- 无定期清理机制

---

## 🎨 优化方案

### 核心原则

采用**Diátaxis文档框架**，将文档分为四种类型：

| 文档类型 | 目的 | 特征 | 目标读者 |
|---------|------|------|---------|
| **Tutorial** | 学习导向 | 实践性、渐进式、立即可见效果 | 新手 |
| **How-to** | 任务导向 | 解决问题、步骤清晰、可操作 | 日常开发者 |
| **Reference** | 信息导向 | 结构化、速查、准确完整 | 所有用户 |
| **Explanation** | 理解导向 | 概念解释、设计决策、深度理解 | 高级用户/架构师 |

### 架构原则

基于**Node.js Best Practices**：
1. **组件化**：每个文档模块自包含，边界清晰
2. **明确入口**：CLAUDE.md作为index，只暴露公共接口
3. **层次化**：入口层（CLAUDE.md）→ 指南层（guides/）→ 参考层（reference/）→ 概念层（explanation/）

---

## 📁 新的文档结构

```
CLAUDE.md (核心入口 - 精简到 ~500行)
├─ 项目概述（50行）
├─ 快速开始（80行）→ .claude/core/QUICK-START.md
├─ 文档导航索引（150行）
├─ 核心规则引用（100行）
└─ 紧急参考（120行）

.claude/
├─ README.md (完整文档索引 - ~500行)
│   ├─ 概览（文档体系说明）
│   ├─ 快速导航（按角色/场景/类型）
│   ├─ 完整目录树
│   └─ 场景映射表
│
├─ core/ (核心规则 - 已存在)
│   ├─ RULES.md - 工具选择优先级、并行执行策略
│   ├─ PRINCIPLES.md - 10条核心原则
│   ├─ FLAGS.md - 行为模式标志、思考强度分级
│   ├─ WORKFLOW.md - Issue驱动工作流
│   ├─ FILE-ORGANIZATION.md - 文件创建规则
│   ├─ TOOL-ENVIRONMENT.md - 项目环境与Claude环境区分
│   ├─ QUICK-START.md - 5分钟快速上手
│   ├─ SPEC-WORKFLOW.md - Spec-Driven开发流程
│   └─ MCP-TOOLS-ORCHESTRATION.md - MCP工具协同指南
│
├─ guides/ (操作指南 - 新增)
│   ├─ getting-started.md (~300行)
│   │   - 前置准备、第一个任务、常见问题、下一步
│   ├─ issue-workflow.md (~400行)
│   │   - 简化工作流（小Issue → Master）
│   │   - Epic工作流（PR模式）
│   │   - 渐进式Bug修复策略
│   │   - 架构问题升级策略
│   ├─ git-operations.md (~200行)
│   │   - Commit Message格式
│   │   - 分支操作、PR创建与合并
│   ├─ testing.md (~250行)
│   │   - 编译验证（0 errors, 0 warnings）
│   │   - 运行时验证（强制要求）
│   │   - 测试执行与清理
│   ├─ documentation.md (~300行)
│   │   - 文档架构（Level 0-3）
│   │   - 代码与文档并行开发
│   │   - 强制性文档读取规则
│   └─ spec-workflow.md (~350行)
│       - Spec-Driven完整流程
│       - 需求讨论、需求文档、设计文档
│
├─ reference/ (参考手册 - 新增)
│   ├─ project-info.md (~200行)
│   │   - GitHub仓库参数（owner/repo/URL）
│   │   - 技术栈版本（.NET 8、SQL Server 2022）
│   │   - 目录结构速查、关键配置文件
│   ├─ commands.md (~400行)
│   │   - 项目构建命令（dotnet build/test/format）
│   │   - Git命令速查
│   │   - MCP vs Bash对照表
│   ├─ coding-standards.md (~500行)
│   │   - 质量标准（0警告、运行时验证）
│   │   - 命名规范、文件组织
│   │   - Commit Message格式
│   │   - 代码示例（正确/错误对比）
│   └─ mcp-tools.md (~800行)
│       - 按类别组织：开发/知识/工作流/测试工具
│       - 每个工具：用途、方法、参数、示例
│       - 工具协同模式
│
├─ explanation/ (概念解释 - 新增)
│   ├─ architecture-philosophy.md (~600行)
│   │   - Claude Code角色定位
│   │   - 三层对齐架构（Server/Client/Shared）
│   │   - 文档先行原则、依赖方向
│   ├─ mvp-philosophy.md (~400行)
│   │   - MVP核心原则（够用即好）
│   │   - 技术黑名单（Redis/CQRS/Docker/GraphQL）
│   │   - Constitution约束
│   └─ long-term-vision.md (~700行)
│       - 立足未来3-5年的架构原则
│       - 渐进式演进路径
│       - 演进触发条件（6个量化指标）
│       - 版本管理策略
│
├─ modes/ (工作模式 - 已存在)
│   ├─ code-review.md - 代码审查模式
│   ├─ architecture.md - 架构审查模式
│   ├─ performance.md - 性能优化模式
│   ├─ refactoring.md - 重构规划模式
│   ├─ testing.md - 测试驱动模式
│   ├─ documentation.md - 文档同步模式
│   └─ research.md - 深度研究模式
│
└─ skills/ (项目专属Skills - 已存在)
    ├─ lybtzyzs-mvp-compliance/ - MVP合规检查
    ├─ lybtzyzs-arch-compliance/ - 架构合规检查
    ├─ lybtzyzs-doc-sync/ - 文档同步检查
    ├─ lybtzyzs-task-breakdown/ - 任务分解生成
    ├─ lybtzyzs-issue-template/ - Issue模板生成
    ├─ lybtzyzs-code-review/ - 代码规范审查
    ├─ lybtzyzs-test-generator/ - 测试用例生成
    └─ lybtzyzs-pr-generator/ - PR描述生成
```

---

## 🔄 详细内容映射

### CLAUDE.md 各节内容迁移计划

| 当前章节 | 行数估算 | 目标位置 | 优先级 |
|---------|---------|---------|--------|
| 0.5 项目基础信息 | 100 | reference/project-info.md | P1 |
| 1 角色定位与必读资料 | 300 | explanation/architecture-philosophy.md | P2 |
| 1.5 Spec-Driven工作流 | 200 | guides/spec-workflow.md | P1 |
| 1.6 需求讨论规范 | 400 | guides/spec-workflow.md | P1 |
| 2 Issue驱动工作流 | 2000 | guides/issue-workflow.md | P0 |
| 2.3 渐进式Bug修复 | 300 | guides/issue-workflow.md | P0 |
| 2.4 架构问题升级 | 200 | guides/issue-workflow.md | P0 |
| 2.5 任务启动前置检查 | 100 | guides/testing.md | P1 |
| 2.6 完成标准与文档更新 | 300 | guides/testing.md + documentation.md | P1 |
| 3 执行原则 | 800 | explanation/（保留core/PRINCIPLES.md引用） | P2 |
| 3.5 版本管理规范 | 400 | explanation/long-term-vision.md | P2 |
| 4 编码与交付要求 | 600 | reference/coding-standards.md | P1 |
| 5 工具环境与命令 | 400 | reference/commands.md | P1 |
| 6 MCP工具使用准则 | 1500 | reference/mcp-tools.md | P1 |
| 7 工作模式 | 200 | 保留modes/目录引用 | P2 |
| 8 Claude Skills使用 | 400 | guides/skills-usage.md (新建) | P1 |
| 9 代码修复后清理 | 200 | guides/testing.md | P1 |

**优先级说明**：
- P0：最高优先级（日常开发必需）
- P1：高优先级（常用参考）
- P2：中等优先级（概念理解）

---

## 🛠️ 实施路径

### Phase 1：基础架构搭建（2-3小时）

**目标**：创建新目录结构和空模板文件

**任务清单**：
1. 创建新目录：
   ```bash
   mkdir -p .claude/guides
   mkdir -p .claude/reference
   mkdir -p .claude/explanation
   ```

2. 创建空模板文件（含标题和结构占位符）：
   ```bash
   # guides/
   touch .claude/guides/getting-started.md
   touch .claude/guides/issue-workflow.md
   touch .claude/guides/git-operations.md
   touch .claude/guides/testing.md
   touch .claude/guides/documentation.md
   touch .claude/guides/spec-workflow.md
   touch .claude/guides/skills-usage.md

   # reference/
   touch .claude/reference/project-info.md
   touch .claude/reference/commands.md
   touch .claude/reference/coding-standards.md
   touch .claude/reference/mcp-tools.md

   # explanation/
   touch .claude/explanation/architecture-philosophy.md
   touch .claude/explanation/mvp-philosophy.md
   touch .claude/explanation/long-term-vision.md
   ```

3. 创建`.claude/README.md`骨架（含目录树和场景映射表）

4. 备份当前CLAUDE.md：
   ```bash
   cp CLAUDE.md CLAUDE.md.backup
   ```

**验收标准**：
- ✅ 所有目录和模板文件已创建
- ✅ `.claude/README.md`骨架完成
- ✅ 备份文件存在

---

### Phase 2：内容迁移与重组（4-6小时）

**目标**：将CLAUDE.md内容按Diátaxis分类迁移到新文档

**子阶段2.1：guides/迁移（优先级P0-P1）**

1. **guides/issue-workflow.md**（从2节迁移，~400行）
   - 简化工作流（小Issue → Master）
   - Epic工作流（PR模式）
   - 渐进式Bug修复策略（2.3节）
   - 架构问题升级策略（2.4节）

2. **guides/testing.md**（从2.5-2.6、9节迁移，~250行）
   - 任务启动前置检查（2.5节）
   - 完成标准与验证（2.6节）
   - 代码修复后清理（9节）

3. **guides/git-operations.md**（从2.4节提取，~200行）
   - Commit Message格式
   - 分支操作
   - PR创建与合并

4. **guides/spec-workflow.md**（从1.5、1.6节迁移，~350行）
   - Spec-Driven完整流程
   - 需求讨论、需求文档、设计文档

5. **guides/documentation.md**（从2.6、1.6节提取，~300行）
   - 文档架构（Level 0-3）
   - 代码与文档并行开发
   - 强制性文档读取规则

6. **guides/getting-started.md**（新建，整合入门内容，~300行）
   - 前置准备：环境检查、工具安装
   - 第一个任务：从Issue到PR的完整流程
   - 常见问题：编译失败、测试失败、Git操作
   - 下一步：进阶指南索引

7. **guides/skills-usage.md**（从8节迁移，~400行）
   - Claude Skills触发方式
   - 核心Skills使用（MVP合规、架构合规、文档同步等）

**子阶段2.2：reference/迁移（优先级P1）**

1. **reference/project-info.md**（从0.5节迁移，~200行）
   - GitHub仓库参数（owner/repo/URL）
   - 技术栈版本（.NET 8、SQL Server 2022）
   - 目录结构速查
   - 关键配置文件位置

2. **reference/commands.md**（从5节迁移，~400行）
   - 项目构建命令（dotnet build/test/format）
   - Git命令速查
   - 文件操作命令
   - MCP vs Bash对照表

3. **reference/coding-standards.md**（从4节迁移，~500行）
   - 质量标准（0警告、运行时验证）
   - 命名规范（PascalCase/camelCase）
   - 文件组织（UTF-8 BOM、≤500行）
   - Commit Message格式
   - 代码示例（正确/错误对比）

4. **reference/mcp-tools.md**（从6节迁移，~800行）
   - 按类别组织：开发工具/知识工具/工作流工具/测试工具
   - 每个工具：名称、用途、常用方法、参数说明、示例
   - 工具协同模式：研究模式、快速开发模式
   - 特殊工具详解：interactive-feedback、shrimp-task-manager

**子阶段2.3：explanation/迁移（优先级P2）**

1. **explanation/architecture-philosophy.md**（从1节、3.5节迁移，~600行）
   - Claude Code角色定位（智能顾问 vs 自动执行）
   - 三层对齐架构（Server/Client/Shared）
   - 文档先行原则
   - 依赖方向与模块边界

2. **explanation/mvp-philosophy.md**（从3节迁移，~400行）
   - MVP核心原则（够用即好、避免过度设计）
   - 技术黑名单（Redis/CQRS/Docker/GraphQL）
   - Constitution约束（→ .spec-workflow/steering/constitution.md）
   - 质量 vs 速度平衡

3. **explanation/long-term-vision.md**（从3.5节、ADR-005迁移，~700行）
   - 立足未来3-5年的架构原则
   - 渐进式演进路径（Service协调 → 富领域模型 → 领域事件）
   - 演进触发条件（6个量化指标）
   - Constitution可调整机制（ADR + 例外清单）
   - 版本管理策略（v6.1新增内容）

**验收标准**：
- ✅ 所有内容已迁移到新文档
- ✅ 每个文档符合Diátaxis类型定义
- ✅ 文档长度符合限制（Tutorial ≤500行，How-to ≤600行，Reference ≤800行，Explanation ≤700行）
- ✅ 无内容遗漏

---

### Phase 3：精简主文档与验证（2-3小时）

**目标**：重写CLAUDE.md为核心入口，验证完整性

**任务清单**：

1. **重写CLAUDE.md**（目标500行）：

   **Section 1: 项目概述**（50行）
   - 项目名称：良医本草坐诊系统（LYBTZYZS）
   - GitHub仓库：shouqitao/LYBTZYZS
   - 技术栈：.NET 8、WPF、SQL Server 2022
   - Claude Code定位：智能顾问

   **Section 2: 快速开始**（80行）
   - 5分钟入门 → `.claude/core/QUICK-START.md`
   - 首次任务流程：
     ```
     读文档 → 查Issue → 编译 → 修改 → 验证 → 提交
     ```
   - 常见命令速查（3-5个最常用）

   **Section 3: 文档导航索引**（150行）
   - **按角色导航**：
     * 新手 → `guides/getting-started.md`
     * 日常开发者 → `guides/issue-workflow.md`, `guides/git-operations.md`
     * 高级用户/架构师 → `explanation/architecture-philosophy.md`, `explanation/long-term-vision.md`

   - **按场景导航**：
     * 我要修Bug → `guides/issue-workflow.md`
     * 我要开发新功能 → `guides/spec-workflow.md`
     * 我要重构代码 → `modes/refactoring.md`
     * 我要查工具用法 → `reference/mcp-tools.md`
     * 我要理解架构 → `explanation/architecture-philosophy.md`

   - **按文档类型导航**：
     * Tutorial（教程）→ `guides/getting-started.md`
     * How-to（操作指南）→ `guides/`
     * Reference（参考手册）→ `reference/`
     * Explanation（概念解释）→ `explanation/`

   **Section 4: 核心规则引用**（100行）
   - Constitution约束 → `.spec-workflow/steering/constitution.md`
   - 核心原则 → `.claude/core/PRINCIPLES.md`
   - 工作流程 → `.claude/core/WORKFLOW.md`
   - 强制规则：
     * ✅ Issue驱动开发（无Issue禁止改动）
     * ✅ 0警告标准（编译必须0 errors, 0 warnings）
     * ✅ 运行时验证（启动应用，测试真实场景）

   **Section 5: 紧急参考**（120行）
   - GitHub仓库参数：
     ```
     owner: shouqitao
     repo:  LYBTZYZS
     ```
   - MCP工具优先级（3层）：
     * 第1优先级：MCP工具（filesystem, serena, github, context7）
     * 第2优先级：Claude Code内置工具（Read/Write/Edit, Glob/Grep）
     * 第3优先级：Shell命令（仅在MCP无法满足时使用）

   - 质量标准（3层验证）：
     * Level 1：编译验证（0 errors, 0 warnings）
     * Level 2：静态分析（代码逻辑、架构规范）
     * Level 3：运行时验证（⚠️ 强制）

   - 常见错误处理：
     * 编译失败 → `guides/testing.md`
     * Git操作 → `guides/git-operations.md`
     * 工具使用 → `reference/mcp-tools.md`

2. **完善`.claude/README.md`**（~500行）：

   **概览**（50行）
   - 文档体系说明（Diátaxis四类）
   - 使用建议（根据角色/场景选择）

   **快速导航**（100行）
   - 按角色：新手/日常开发者/架构师
   - 按场景：Bug修复/功能开发/架构调整/文档更新
   - 按文档类型：Tutorial/How-to/Reference/Explanation

   **完整目录树**（200行）
   ```
   core/ - 核心规则
   ├─ RULES.md - 工具选择、执行策略
   ├─ PRINCIPLES.md - 10条核心原则
   ├─ WORKFLOW.md - Issue驱动工作流
   └─ ...

   guides/ - 操作指南
   ├─ getting-started.md - 新手指南
   ├─ issue-workflow.md - Issue工作流详解
   └─ ...

   reference/ - 参考手册
   ├─ mcp-tools.md - MCP工具完整参考
   ├─ commands.md - 命令速查表
   └─ ...

   explanation/ - 概念解释
   ├─ architecture-philosophy.md - 架构哲学
   └─ ...
   ```

   **场景映射表**（150行）
   | 我想... | 查看文档 | 时间估算 |
   |--------|---------|---------|
   | 修复一个Bug | `guides/issue-workflow.md` → `reference/commands.md` | 5分钟 |
   | 开发新功能 | `guides/spec-workflow.md` → `explanation/mvp-philosophy.md` | 15分钟 |
   | 重构代码 | `modes/refactoring.md` → `explanation/long-term-vision.md` | 10分钟 |
   | 更新文档 | `guides/documentation.md` | 5分钟 |
   | 理解架构 | `explanation/architecture-philosophy.md` | 20分钟 |
   | 查询工具 | `reference/mcp-tools.md` | 2分钟 |

   **从旧版本迁移指南**（50行）
   - 旧版本章节 → 新版本位置对照表
   - 常见查找场景示例

3. **验证完整性**：

   **链接完整性检查**：
   ```bash
   # 使用grep检查所有Markdown文件中的链接
   find .claude -name "*.md" -exec grep -H "\[.*\](.*\.md)" {} \;

   # 验证每个链接的目标文件是否存在
   ```

   **内容完整性检查**：
   - ✅ 对比CLAUDE.md.backup和新文档，确认无内容遗漏
   - ✅ 检查每个Diátaxis类型至少有3个文档
   - ✅ 验证常见场景（Bug修复/功能开发/架构调整）的完整文档链

   **用户体验测试**：
   - ✅ 模拟新手第一次使用：能否在5分钟内找到快速开始指南？
   - ✅ 模拟Bug修复场景：能否在30秒内找到Issue工作流？
   - ✅ 模拟工具查询：能否在30秒内找到MCP工具用法？

**验收标准**：
- ✅ CLAUDE.md ≤ 500行
- ✅ `.claude/README.md`完整且结构清晰
- ✅ 所有链接可达，无死链
- ✅ 用户体验测试通过

---

## ✅ 验收标准

### 1. 主文档精简度
- **目标**：CLAUDE.md ≤ 500行
- **当前**：9000行
- **减少**：94%
- **验证方法**：`wc -l CLAUDE.md`

### 2. 查找效率
- **目标**：常见场景查找时间 ≤ 30秒
- **当前**：2-3分钟
- **提升**：75%
- **验证方法**：用户体验测试（模拟3个常见场景）

### 3. 模块化完整性
- **目标**：所有内容分类到Diátaxis四类，无遗漏
- **验证方法**：对比CLAUDE.md.backup和新文档，确认无内容丢失

### 4. 链接完整性
- **目标**：所有交叉引用可达，无死链
- **验证方法**：使用grep检查所有Markdown文件中的链接

### 5. 用户满意度
- **目标**：新手能在5分钟内找到第一个任务的指导
- **验证方法**：模拟新手使用场景，记录查找时间

---

## 📊 预期效果

### 量化指标

| 指标 | 优化前 | 优化后 | 改善幅度 |
|-----|-------|-------|---------|
| 主文档行数 | 9000行 | 500行 | -94% |
| 查找时间（常见场景） | 2-3分钟 | 30秒 | +75% |
| 新手学习时间 | 2小时 | 30分钟 | +75% |
| 重复内容比例 | ~30% | 0% | -100% |
| 文档数量 | 1个 | 15个模块 | +1400% |
| 维护成本 | 高 | 低 | - |

### 用户体验改进

**场景1：新手第一次使用**
- **旧版本**：打开CLAUDE.md → 9000行 → 不知从何读起 → 全部阅读（耗时1-2小时）
- **新版本**：打开CLAUDE.md → 看到"快速开始"链接 → 点击`QUICK-START.md` → 5分钟上手

**场景2：修复Bug时查找工作流**
- **旧版本**：打开CLAUDE.md → 搜索"Issue" → 找到2节 → 滚动阅读2000行 → 找到步骤
- **新版本**：打开CLAUDE.md → 看到"我要修Bug" → 点击`guides/issue-workflow.md` → 直接看到简化工作流

**场景3：查询MCP工具用法**
- **旧版本**：打开CLAUDE.md → 搜索"MCP" → 找到6节 → 滚动阅读1500行 → 找到工具
- **新版本**：打开CLAUDE.md → 看到"紧急参考"中的MCP工具优先级 → 需要详细？→ 点击`reference/mcp-tools.md` → 按类别查找

**场景4：理解架构决策**
- **旧版本**：打开CLAUDE.md → 分散在1节、3节、3.5节 → 跳转阅读 → 难以建立完整理解
- **新版本**：打开`.claude/README.md` → 按文档类型导航 → 点击`explanation/` → 依次阅读架构哲学、MVP哲学、长期愿景

---

## ⚠️ 风险与缓解措施

### 风险分析

| 风险类型 | 影响程度 | 概率 | 缓解措施 |
|---------|---------|------|---------|
| 迁移工作量大 | 高 | 高 | 分阶段实施（3个Phase，各2-3小时） |
| 链接维护困难 | 中 | 中 | 使用grep检查所有链接的完整性 |
| 过渡期混乱 | 中 | 中 | 保留CLAUDE.md.backup至少2周，提供"从旧版本迁移"指南 |
| 过度拆分 | 低 | 低 | 遵循Diátaxis框架，避免过细拆分 |

### 缓解措施

1. **分阶段实施**：
   - Phase 1：基础架构（2-3小时）
   - Phase 2：内容迁移（4-6小时）
   - Phase 3：精简与验证（2-3小时）
   - 总计：8-12小时，分3次完成

2. **保留备份**：
   - `CLAUDE.md.backup`保留至少2周
   - 确保可回滚到旧版本

3. **提供迁移指南**：
   - 在`.claude/README.md`中添加"从旧版本迁移"指南
   - 提供旧章节 → 新位置对照表

4. **验证机制**：
   - 使用grep检查所有链接完整性
   - 对比backup文件，确认无内容遗漏
   - 用户体验测试（模拟3个常见场景）

---

## 📝 文档维护策略

### 预防措施

1. **单一职责原则**：
   - 每个文档只负责一种文档类型（Tutorial/How-to/Reference/Explanation）
   - 不混合不同类型的内容

2. **长度限制**：
   - Tutorial: ≤500行
   - How-to: ≤600行
   - Reference: ≤800行
   - Explanation: ≤700行
   - 超过限制需拆分成子文档

3. **更新检查点**：
   - 每次添加内容前，检查是否属于当前文档的职责范围
   - 如不属于，创建新文档或调整现有文档

4. **定期审查**：
   - 每季度审查一次文档体系
   - 删除过时内容，合并重复内容

### 内容添加规则

| 内容类型 | 目标位置 | 处理方式 |
|---------|---------|---------|
| 新增操作步骤 | `guides/` | 如果与现有指南相关，合并；否则创建新文件 |
| 新增工具说明 | `reference/mcp-tools.md` | 按类别添加 |
| 新增架构决策 | `explanation/` | 创建新的ADR或更新现有文档 |
| 新增快速参考 | `CLAUDE.md`的"紧急参考"章节 | ≤10行，超出则移到reference/ |

### 协同维护

- **代码变更时**：同步更新`reference/coding-standards.md`
- **工作流调整时**：同步更新`guides/issue-workflow.md`
- **架构演进时**：同步更新`explanation/long-term-vision.md`

### 版本控制

- **重大更新**（如Diátaxis重构）→ CLAUDE.md标注版本号（v7.0）
- **小幅修改** → 在`.claude/CHANGELOG.md`记录变更历史

---

## 🚀 下一步行动

### 1. 用户确认
- [ ] 审查优化方案
- [ ] 确认文档结构合理性
- [ ] 确认实施时间安排

### 2. 创建Issue并关联Task
- [ ] 在Issue #1711中添加详细的Task清单
- [ ] 关联3个子任务（Phase 1-3）

### 3. 实施Phase 1（2-3小时）
- [ ] 创建新目录结构
- [ ] 创建空模板文件
- [ ] 创建`.claude/README.md`骨架
- [ ] 备份当前CLAUDE.md

### 4. 实施Phase 2（4-6小时）
- [ ] guides/迁移（优先级P0-P1）
- [ ] reference/迁移（优先级P1）
- [ ] explanation/迁移（优先级P2）

### 5. 实施Phase 3（2-3小时）
- [ ] 重写CLAUDE.md（目标500行）
- [ ] 完善`.claude/README.md`
- [ ] 验证完整性

### 6. 收集反馈与持续优化
- [ ] 收集用户（Claude Code）的使用反馈
- [ ] 根据反馈调整文档结构
- [ ] 建立季度审查机制

---

## 📚 参考资料

1. **Diátaxis文档框架**
   - 库ID：`/evildmp/diataxis-documentation-framework`
   - 核心概念：Tutorial, How-to, Reference, Explanation
   - 网站：https://diataxis.fr/

2. **Node.js Best Practices**
   - 库ID：`/goldbergyoni/nodebestpractices`
   - 组织原则：组件化、明确入口、层次化架构
   - 网站：https://github.com/goldbergyoni/nodebestpractices

3. **当前.claude/模块**
   - 已有良好的模块化基础（core/、modes/、skills/）
   - 本方案在现有基础上扩展（guides/、reference/、explanation/）

---

## 📄 附录

### A. Diátaxis四种文档类型详解

| 文档类型 | 目的 | 内容 | 形式 | 类比 |
|---------|------|------|------|------|
| **Tutorial** | 学习 | 学习导向的课程 | 带领读者完成一系列步骤 | 教孩子做饭 |
| **How-to** | 任务 | 解决问题的指南 | 提供解决特定问题的步骤 | 食谱 |
| **Reference** | 信息 | 技术描述 | 干燥、准确、完整的信息 | 百科全书 |
| **Explanation** | 理解 | 解释和讨论 | 提供背景和上下文 | 关于烹饪历史的文章 |

### B. 文档长度限制理由

- **Tutorial（≤500行）**：新手注意力有限，超过500行会失去焦点
- **How-to（≤600行）**：单个任务不应太复杂，否则需要拆分成多个How-to
- **Reference（≤800行）**：速查表应该快速定位，超过800行需要分类或拆分
- **Explanation（≤700行）**：深度解释应聚焦单一主题，超过700行说明主题太广

### C. 场景映射表完整版

| 场景 | 角色 | 文档路径 | 时间 |
|-----|------|---------|------|
| 第一次使用Claude Code | 新手 | `CLAUDE.md` → `QUICK-START.md` | 5分钟 |
| 修复一个Bug | 日常开发者 | `guides/issue-workflow.md` | 10分钟 |
| 开发新功能 | 日常开发者 | `guides/spec-workflow.md` | 15分钟 |
| 提交代码 | 日常开发者 | `guides/git-operations.md` | 5分钟 |
| 执行测试 | 日常开发者 | `guides/testing.md` | 5分钟 |
| 更新文档 | 日常开发者 | `guides/documentation.md` | 5分钟 |
| 查询MCP工具 | 所有用户 | `reference/mcp-tools.md` | 2分钟 |
| 查询命令 | 所有用户 | `reference/commands.md` | 2分钟 |
| 查询编码规范 | 所有用户 | `reference/coding-standards.md` | 3分钟 |
| 理解架构设计 | 架构师 | `explanation/architecture-philosophy.md` | 20分钟 |
| 理解MVP原则 | 架构师 | `explanation/mvp-philosophy.md` | 15分钟 |
| 理解长期愿景 | 架构师 | `explanation/long-term-vision.md` | 25分钟 |

---

**文档结束**

如有任何疑问或建议，请在Issue #1711中讨论。
