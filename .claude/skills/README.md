# LYBTZYZS Project Skills 说明文档

> **📅 最后更新**：2025-11-29
> **📦 Skills版本**：v1.2（新增openspec-archive-finalize归档后自动化）

---

## 📋 Skills 总览

本项目共有**7个核心Skills**，分为4类：

### 1️⃣ 合规性检查Skills（3个）

| Skill | 功能 | 触发关键词 |
|-------|------|-----------|
| **lybtzyzs-mvp-compliance** | MVP合规检查 | 检查MVP合规性、MVP约束 |
| **lybtzyzs-arch-compliance** | 架构合规检查 | 架构合规性、三层架构 |
| **lybtzyzs-doc-sync** | 文档同步检查 | 文档同步、文档更新 |

### 2️⃣ 文档生成Skills（1个）⭐ v1.1新增

| Skill | 功能 | 触发关键词 |
|-------|------|-----------|
| **lybtzyzs-design-generator** | 设计文档生成（需求→设计） | 生成设计文档、创建设计、需求转设计 |

### 3️⃣ 任务管理Skills（2个）⭐ v1.0新增

| Skill | 功能 | 触发关键词 |
|-------|------|-----------|
| **lybtzyzs-task-breakdown** | 任务分解生成 | 任务分解、生成任务清单、task breakdown |
| **lybtzyzs-issue-template** | Issue模板生成（单模式+批量模式） | 创建Issue、批量创建Issues |

### 4️⃣ OpenSpec工作流Skills（1个）⭐ v1.2新增

| Skill | 功能 | 触发关键词 |
|-------|------|-----------|
| **lybtzyzs-openspec-archive-finalize** | 归档后自动化：代码审查→提交推送→保存记忆→同步文档 | 归档完成、archive finalize、openspec完成 |

---

## 🎯 核心工作流：从需求到Issue（v1.1完整版）⭐

### 完整流程（5步全自动化）

```
Step 1: 需求文档完成
└─> docs/requirements/xxx-requirements.md
    └─> 包含架构约束章节

Step 2: 设计文档生成（⭐⭐⭐ v1.1新增）
└─> lybtzyzs-design-generator
    ├─> 强制读取架构文档
    ├─> 生成API端点/DTO/代码示例/Phase
    └─> docs/explanation/xxx-design.md

Step 3: 架构验证（⭐ 自动触发）
└─> lybtzyzs-design-arch-validator（自动）
    └─> ✅ 0违规

Step 4: 任务分解（⭐ v1.0）
└─> lybtzyzs-task-breakdown
    └─> docs/tasks/xxx-tasks.md（8个任务）

Step 5: 批量创建Issues（⭐ v1.0增强）
└─> lybtzyzs-issue-template（批量模式）
    └─> GitHub创建8个Issues（自动关联Epic、标注依赖）

Step 6: 实施跟踪
└─> Issue-Driven开发
    └─> Epic自动追踪进度（X/8 Issues完成）
```

### 使用示例

**用户命令**：
```
"根据设计文档生成任务清单并批量创建Issues"
```

**执行过程**：
```
Claude:
✓ 读取设计文档: docs/explanation/medicalcase-enhancement-design.md
✓ 触发lybtzyzs-task-breakdown
  - 识别3个Phase
  - 拆分为8个任务
  - 分析依赖关系
  - 生成task文档: docs/tasks/medicalcase-enhancement-tasks.md

✓ 触发lybtzyzs-issue-template（批量模式）
  - 读取task文档
  - 批量创建8个Issues
  - 标注依赖关系
  - 关联Epic #1494

✅ 完成！
- Task文档：docs/tasks/medicalcase-enhancement-tasks.md
- 创建了8个Issues（#1601-#1608）
- Epic #1494已自动追踪
```

---

## 📝 Skills 详细说明

### 🔴 1. lybtzyzs-mvp-compliance

**功能**：检查代码是否符合MVP原则和Constitution约束

**能力**：
- 自动检测：技术黑名单（Redis/CQRS/MediatR/Docker/GraphQL）
- 自动检测：依赖注入违规（ServiceLocator模式）
- 建议确认：过度设计（Event Sourcing、不必要抽象）

**使用场景**：
- 新功能开发前检查技术选型
- 重构前评估方案合规性
- 代码审查阶段

**触发关键词**：检查MVP合规性、MVP约束、技术黑名单

---

### 🏗️ 2. lybtzyzs-arch-compliance

**功能**：验证代码是否符合三层对齐架构规范

**能力**：
- 自动检测：依赖方向错误（Application→Presentation）
- 自动检测：聚合根边界违规
- 建议确认：Repository粒度、Service职责

**使用场景**：
- 设计文档完成后的架构预检
- 代码实施阶段的架构验证
- 重构后的架构合规性检查

**触发关键词**：架构合规性、三层架构、聚合根

---

### 📝 3. lybtzyzs-doc-sync

**功能**：检测代码变更并生成文档更新清单

**能力**：
- 自动检测：API端点变更、架构调整、数据模型变更
- 建议确认：影响范围评估、文档更新清单

**使用场景**：
- 代码提交前的文档同步检查
- PR审查阶段的文档完整性验证
- 架构调整后的文档更新提醒

**触发关键词**：文档同步、文档更新、检查文档

---

### 🎨 4. lybtzyzs-design-generator ⭐⭐⭐ v1.1新增

**功能**：从需求文档自动生成完整的技术设计文档

**能力**：
- 需求解析：自动提取业务需求、架构约束、验收标准
- 架构设计生成：组件关系图、数据流、聚合根边界
- API端点设计：Write/Read/Helper层分离，遵循聚合根原则
- DTO设计：请求/响应DTO和映射关系
- 数据库Schema：表结构调整和迁移脚本
- 代码示例生成：Controller/Service/Repository/ViewModel完整代码
- Phase拆分：3-5个Phase，每个Phase包含任务清单和时间估算
- 架构合规性自动验证：生成后自动触发lybtzyzs-design-arch-validator

**使用场景**：
- 需求文档完成后，生成技术设计蓝图
- Epic需要详细的技术设计方案
- 跨模块重构需要完整的设计文档
- 新功能开发需要API端点和DTO设计

**触发关键词**：
- 生成设计文档
- 创建设计
- 从需求生成设计
- design generation
- 需求转设计

**强制性文档阅读规则**（⭐⭐⭐ 最高优先级）：
1. **拒绝未读架构文档的设计生成请求**
2. **强制读取Level 0-2架构文档**：
   - Level 0: 需求文档（包含架构约束章节）
   - Level 1: docs/index.md, docs/business-rules.md, docs/architecture/{server|client|shared}/README.md
   - Level 2: 根据功能类型读取详细架构文档
3. **生成文档阅读摘要，等待用户确认后继续**

**输入**：
- 需求文档路径（docs/requirements/*.md）
- 需求文档必须包含"架构约束"章节

**输出**：
- 设计文档（docs/explanation/*.md）
- 自动触发lybtzyzs-design-arch-validator验证报告

**示例**：
```
用户："根据需求文档生成设计文档"

Claude:
📚 Step 1: 架构文档阅读（强制）
⚠️ 设计文档生成前必须先阅读架构指南

正在读取架构文档：
✓ docs/index.md
✓ docs/business-rules.md
✓ docs/architecture/server/README.md
✓ docs/requirements/medicalcase-refactoring-requirements.md

📝 架构文档要点摘要：
- v2.0三层架构：Presentation → Application → Data Access
- MedicalCase聚合根：Write操作必须通过聚合根
- 14条业务规则：AR-001（聚合根约束）、BF-002（三步流程）等

用户确认后继续？

用户：确认

✓ Step 2: 需求解析
✓ 提取6个业务需求（REQ-001 - REQ-006）
✓ 提取5个架构要求（ARCH-001 - ARCH-005）

✓ Step 3: 设计文档生成
✓ 生成架构设计（组件关系图、数据流）
✓ 生成API端点（13个：Write 8 + Read 5）
✓ 生成DTO设计（7个DTO + AutoMapper配置）
✓ 生成数据库Schema（2个字段 + Migration脚本）
✓ 生成代码示例（5个：Controller/Service/Repository/ViewModel/AutoMapper）
✓ 生成Phase拆分（3个Phase，10-13天）

✓ 写入设计文档：docs/explanation/medicalcase-refactoring-design.md

⚠️ Step 4: 架构合规性自动验证
✓ 触发lybtzyzs-design-arch-validator
✓ 检查Write Layer端点：8个，全部遵循聚合根原则
✓ 检查Read Layer端点：5个，全部独立
✓ 检查架构约束引用：✅ 已引用
✓ 检查业务规则引用：✅ 已引用AR-001、BF-002等

✅ 架构合规性验证通过！

📊 生成统计：
- 设计文档：docs/explanation/medicalcase-refactoring-design.md
- API端点数：13个
- DTO数量：7个
- 代码示例：5个
- Phase数量：3个
- 总工作量：10-13天

🎯 下一步：使用lybtzyzs-task-breakdown生成任务清单
```

**详细文档**：`.claude/skills/lybtzyzs-design-generator/SKILL.md`

---

### 📋 5. lybtzyzs-task-breakdown ⭐ v1.0新增

**功能**：从设计文档自动生成结构化任务分解清单

**能力**：
- 智能任务拆分（按Phase/模块/职责）
- 依赖关系分析（Repository → Service → Controller）
- 工作量估算（2-4小时/任务）
- Phase划分和关键路径识别
- 输出标准化task文档

**输入**：
- 设计文档（docs/explanation/*.md）
- 或Epic描述 + 简要技术方案

**输出**：
- Task文档（docs/tasks/*.md）
- 包含任务清单、依赖关系、工作量估算

**使用场景**：
- 设计文档完成后，需要拆分成可执行的子任务
- Epic拆分成多个实施步骤
- 重构计划分解成多个阶段

**触发关键词**：
- 任务分解
- 生成任务清单
- task breakdown
- 拆分任务
- 分解设计

**示例**：
```
用户："根据设计文档生成任务清单"

Claude:
✓ 读取: docs/explanation/medicalcase-enhancement-design.md
✓ 识别3个Phase
✓ 拆分为8个任务
✓ 生成: docs/tasks/medicalcase-enhancement-tasks.md

📊 任务统计：
- 总任务数：8个
- 总工作量：18-24小时
- 关键路径：5个任务

💡 下一步：批量生成Issues
```

**详细文档**：`.claude/skills/lybtzyzs-task-breakdown/SKILL.md`

---

### 📝 6. lybtzyzs-issue-template ⭐ v1.2增强

**功能**：生成标准化GitHub Issue（单模式）+ 从task文档批量生成Issues（批量模式）

#### 单Issue模式（v1.0/1.1）

**能力**：
- 根据Issue类型生成标准化模板（Feature/Bug/Refactor等）
- 自动关联Epic和Milestone
- 生成验收标准清单
- 直接在GitHub上创建Issue

**使用场景**：
- 创建单个Bug Fix或Feature任务
- 规范化Issue格式

**触发关键词**：创建Issue、新建任务、Issue模板

#### 批量模式（v1.2新增）⭐

**能力**：
- 从task文档批量生成GitHub Issues
- 自动识别并标注任务间依赖关系
- Epic自动关联
- 批量创建（一次性创建N个）

**输入**：
- Task文档（docs/tasks/*.md）
- 必须符合lybtzyzs-task-breakdown的输出格式

**输出**：
- 批量GitHub Issues（自动关联Epic、标注依赖）
- 控制台显示所有Issue URL和依赖关系

**使用场景**：
- task文档生成后，批量创建Epic的子Issues
- Epic拆分成多个子任务
- 批量任务创建（避免逐个手动创建）

**触发关键词**：
- 批量创建Issues
- 根据task文档生成Issues
- 批量生成Issues

**示例**：
```
用户："根据task文档批量生成Issues"

Claude:
✓ 读取: docs/tasks/medicalcase-enhancement-tasks.md
✓ 识别Epic: #1494
✓ 解析8个任务

✓ 批量创建Issues...
  ✅ Issue #1601: Task 1.1 - 创建Repository
  ✅ Issue #1602: Task 1.2 - 实现Service（依赖#1601）
  ...
  ✅ Issue #1608: Task 3.2 - 实现View（依赖#1607）

📊 创建统计：
- 成功创建：8个Issues
- 关联Epic：#1494
- 依赖关系已标注
```

**详细文档**：
- 单模式：`.claude/skills/lybtzyzs-issue-template/SKILL.md`
- 批量模式：`.claude/skills/lybtzyzs-issue-template/BATCH-MODE.md`

---

## 🔄 Skills协同关系

### 合规性检查Skills（独立使用）

```
代码变更 → lybtzyzs-mvp-compliance → 合规性报告
代码变更 → lybtzyzs-arch-compliance → 架构报告
代码变更 → lybtzyzs-doc-sync → 文档更新清单
```

### 任务管理Skills（串联使用）⭐

```
设计文档
  ↓
lybtzyzs-task-breakdown
  ↓
Task文档（docs/tasks/*.md）
  ↓
lybtzyzs-issue-template（批量模式）
  ↓
GitHub Issues（批量创建）
```

### 完整工作流（全Skills协同）⭐ v1.1完整版

```
需求文档（docs/requirements/*.md）
  ↓
lybtzyzs-requirements-arch-guard（需求阶段架构守护）
  ↓
lybtzyzs-design-generator（⭐⭐⭐ v1.1新增：需求→设计）
  ├─> 强制读取架构文档（Level 0-2）
  ├─> 生成API/DTO/代码示例/Phase
  └─> docs/explanation/*.md
  ↓
lybtzyzs-design-arch-validator（⭐ 自动触发：设计阶段架构验证）
  ↓
lybtzyzs-task-breakdown（任务分解）⭐
  ↓
Task文档（docs/tasks/*.md）
  ↓
lybtzyzs-issue-template（批量Issue创建）⭐
  ↓
GitHub Issues（批量创建，自动关联Epic）
  ↓
实施阶段（Issue-Driven开发）
  ↓
lybtzyzs-arch-compliance（代码架构验证）
lybtzyzs-mvp-compliance（MVP合规检查）
  ↓
lybtzyzs-doc-sync（文档同步检查）
  ↓
完成
```

---

## 📊 性能指标

| Skill | 执行时间 | 备注 |
|-------|---------|------|
| lybtzyzs-mvp-compliance | <10秒 | 基于代码扫描 |
| lybtzyzs-arch-compliance | <15秒 | 包含依赖分析 |
| lybtzyzs-doc-sync | <10秒 | 基于git diff |
| lybtzyzs-design-generator | 30-45秒 | 包含架构文档阅读+设计生成+自动验证 |
| lybtzyzs-task-breakdown | <12秒 | 包含设计文档解析 |
| lybtzyzs-issue-template（单） | <7秒 | 单个Issue创建 |
| lybtzyzs-issue-template（批量8个） | <24秒 | 批量创建8个Issues |

---

## 🎯 最佳实践

### 合规性检查Skills

1. **MVP合规检查**：新功能开发前运行，避免使用黑名单技术
2. **架构合规检查**：设计阶段和实施阶段都要运行，确保架构一致性
3. **文档同步检查**：代码提交前运行，确保文档同步

### 文档生成Skills（v1.1新增）⭐

4. **设计文档生成**：需求文档完成后立即运行，生成完整技术设计
5. **架构文档强制阅读**：生成前必须阅读Level 0-2架构文档，不允许跳过
6. **设计文档人工审查**：自动验证通过后，人工审查API端点和Phase拆分是否合理
7. **设计修正流程**：如验证失败，修正设计文档后重新验证，直到通过

### 任务管理Skills

8. **任务分解**：设计文档完成并通过架构验证后立即运行
9. **Task文档审查**：生成后人工审查，调整不合理的任务拆分
10. **批量Issue创建**：Task文档审查通过后再批量创建，避免返工
11. **分批创建**：如果任务数>20个，建议分批创建（按Phase）

---

## 📚 参考文档

- **Skills定义**：`.claude/skills/` 目录下各Skill的SKILL.md文件
- **CLAUDE.md**：`CLAUDE.md` 第8章 - Claude Skills 使用指南
- **Task文档格式**：`.claude/skills/lybtzyzs-task-breakdown/SKILL.md`
- **批量Issue创建**：`.claude/skills/lybtzyzs-issue-template/BATCH-MODE.md`

---

## 🔄 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v0.9 | 2025-10-22 | 初始版本，3个合规性检查Skills |
| v1.0 | 2025-10-26 | 新增task-breakdown Skill，增强issue-template批量模式 |
| v1.1 | 2025-10-26 | 新增design-generator Skill（需求→设计全自动化），自动触发design-arch-validator |
| v1.2 | 2025-11-29 | 新增openspec-archive-finalize Skill（归档后自动化），配套PostToolUse Hook |

---

### 📦 7. lybtzyzs-openspec-archive-finalize ⭐ v1.2新增

**功能**：OpenSpec归档完成后的自动化流程

**工作流程**：
```
归档完成 → 代码审查 → 提交推送 → 保存Graphiti记忆 → 同步文档
```

**能力**：
- 代码审查：检查归档变更涉及的代码质量（调用lybtzyzs-code-review逻辑）
- 提交推送：审查通过后自动commit并push到远程仓库
- 保存记忆：将变更关键信息保存到Graphiti知识图谱
- 同步文档：更新docs系统文档保持同步

**触发方式**：
1. **自动触发**：通过PostToolUse Hook，在`/openspec:archive`命令完成后自动提醒执行
2. **手动触发**：使用关键词"归档完成"、"archive finalize"、"openspec完成"

**Hook配置**：
```json
// .claude/settings.json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "SlashCommand",
        "hooks": [
          {"type": "command", "command": "bash .claude/scripts/hooks/openspec-archive-post.sh"}
        ]
      }
    ]
  }
}
```

**使用场景**：
- OpenSpec归档（/openspec:archive）完成后自动触发
- 手动执行归档后处理流程
- 批量归档后需要统一处理

**详细文档**：`.claude/skills/lybtzyzs-openspec-archive-finalize/SKILL.md`

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-11-29
