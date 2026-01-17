---
name: lybtzyzs-workflow-orchestrator
description: LYBTZYZS项目自动化工作流编排引擎，实现从需求到上线的全流程自动化。状态机驱动、5个关键确认点、Skills自动编排、断点恢复。将自动化率从60%提升至85%。触发关键词：开始新需求、启动workflow、自动化开发流程、orchestrate development
---

# LYBTZYZS 工作流编排器

## 核心理念

**状态机驱动 + 5个关键确认点 + Skills自动编排**

将复杂的开发流程抽象为状态机，在关键节点获取用户确认，其余步骤自动执行。

## 任务复杂度分类

```
┌─────────────────────────────────────────────────────────────────┐
│                      任务复杂度判定树                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  任务描述 ──┬── 涉及架构变更？ ──Yes──► COMPLEX (OpenSpec完整流程)│
│             │                                                   │
│             └── 涉及多模块？ ──Yes──┬── 破坏性变更？ ──Yes──► COMPLEX│
│                                    │                            │
│                                    └── No ──► MEDIUM (PlanMode) │
│                                                                 │
│             └── 单模块内？ ──┬── 新功能？ ──► MEDIUM             │
│                             │                                   │
│                             └── Bug修复/小优化？ ──► SIMPLE      │
└─────────────────────────────────────────────────────────────────┘
```

| 复杂度 | 判定条件 | 工作流 | 预估耗时 |
|--------|----------|--------|----------|
| **SIMPLE** | 单文件修改、Bug修复、文档更新 | 直接执行 | < 30min |
| **MEDIUM** | 单模块新功能、跨2-3文件重构 | PlanMode | 1-4h |
| **COMPLEX** | 架构变更、跨模块、破坏性变更 | OpenSpec完整流程 | 1-5d |

## 工作流状态机

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                    WORKFLOW STATE MACHINE               │
                    └─────────────────────────────────────────────────────────┘

    ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
    │  INTAKE  │ ──►  │  DESIGN  │ ──►  │  PLAN    │ ──►  │ EXECUTE  │ ──►  │ DELIVER  │
    │          │      │          │      │          │      │          │      │          │
    │ 需求采集  │      │ 方案设计  │      │ 任务拆分  │      │ 代码实现  │      │ 质量交付  │
    └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘
         │                 │                 │                 │                 │
         ▼                 ▼                 ▼                 ▼                 ▼
    ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
    │ CP1:需求 │      │ CP2:设计 │      │ CP3:计划 │      │ (自动)   │      │ CP5:交付 │
    │ 确认     │      │ 确认     │      │ 确认     │      │          │      │ 确认     │
    └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘
                                                               │
                                                               ▼
                                                          ┌──────────┐
                                                          │ CP4:验证 │
                                                          │ (编译后) │
                                                          └──────────┘
```

### 5个关键确认点 (Checkpoints)

| CP | 阶段 | 确认内容 | 自动化前置 |
|----|------|----------|------------|
| **CP1** | 需求确认 | 需求理解是否正确 | brainstorm探索 |
| **CP2** | 设计确认 | 技术方案是否可行 | 架构分析、依赖检查 |
| **CP3** | 计划确认 | 任务拆分是否合理 | 工作量估算 |
| **CP4** | 验证确认 | 编译测试是否通过 | 自动编译、测试 |
| **CP5** | 交付确认 | 是否可以提交/合并 | code-review、PR生成 |

## 三种工作流详解

### 1. SIMPLE 工作流 (直接执行)

```
用户请求 ──► 复杂度判定(SIMPLE) ──► 直接实现 ──► 编译验证 ──► 完成
```

**适用**: Bug修复、文档更新、单行代码修改
**跳过**: CP1-CP3
**保留**: CP4(编译验证)、CP5(可选)

### 2. MEDIUM 工作流 (PlanMode)

```
用户请求 ──► 复杂度判定(MEDIUM) ──► EnterPlanMode ──► 方案设计
                                                        │
    ┌───────────────────────────────────────────────────┘
    ▼
CP2:设计确认 ──► ExitPlanMode ──► 代码实现 ──► 编译验证(CP4) ──► code-review ──► 完成(CP5)
```

**适用**: 单模块新功能、局部重构
**Skills链**: `brainstorm` → `EnterPlanMode` → `lybtzyzs-test-generator` → `lybtzyzs-code-review`

### 3. COMPLEX 工作流 (OpenSpec完整流程)

```
用户请求 ──► 复杂度判定(COMPLEX) ──► lybtzyzs-openspec-proposal
                                            │
    ┌───────────────────────────────────────┘
    ▼
CP1:需求确认 ──► lybtzyzs-openspec-design ──► CP2:设计确认
                                                    │
    ┌───────────────────────────────────────────────┘
    ▼
lybtzyzs-task-breakdown ──► CP3:计划确认 ──► lybtzyzs-openspec-apply
                                                    │
    ┌───────────────────────────────────────────────┘
    ▼
代码实现 ──► 编译验证(CP4) ──► lybtzyzs-code-review ──► lybtzyzs-arch-compliance
                                                              │
    ┌─────────────────────────────────────────────────────────┘
    ▼
lybtzyzs-pr-generator ──► CP5:交付确认 ──► lybtzyzs-openspec-archive-finalize
```

**Skills链完整版**:
1. `superpowers:brainstorm` - 需求探索
2. `lybtzyzs-openspec-proposal` - 生成proposal.md
3. `lybtzyzs-openspec-design` - 生成design.md + tasks.md
4. `lybtzyzs-task-breakdown` - 细化任务拆分
5. `lybtzyzs-openspec-apply` - 执行实现
6. `lybtzyzs-test-generator` - 生成测试
7. `lybtzyzs-code-review` - 代码审查
8. `lybtzyzs-arch-compliance` - 架构合规检查
9. `lybtzyzs-pr-generator` - 生成PR
10. `lybtzyzs-openspec-archive-finalize` - 归档完成

## 执行指南

### Step 1: 任务分析

收到用户请求后，立即进行复杂度判定：

```markdown
## 任务复杂度分析

**用户请求**: [原始请求]

**判定因素**:
- [ ] 涉及架构变更
- [ ] 跨多个模块
- [ ] 破坏性变更
- [ ] 新增外部依赖
- [ ] 影响数据库Schema

**判定结果**: [SIMPLE/MEDIUM/COMPLEX]
**选择工作流**: [直接执行/PlanMode/OpenSpec]
```

### Step 2: 根据复杂度选择路径

#### SIMPLE路径
```bash
# 1. 直接实现
# 2. 编译验证
dotnet build LYBT.All.sln

# 3. 完成
```

#### MEDIUM路径
```bash
# 1. 使用brainstorm探索（如果需求不明确）
# 调用 superpowers:brainstorm skill

# 2. 进入计划模式
# 使用 EnterPlanMode 工具

# 3. 设计方案，写入计划文件

# 4. 获取用户确认后退出计划模式
# 使用 ExitPlanMode 工具

# 5. 实现代码

# 6. 生成测试
# 调用 lybtzyzs-test-generator skill

# 7. 编译验证
dotnet build LYBT.All.sln
dotnet test

# 8. 代码审查
# 调用 lybtzyzs-code-review skill
```

#### COMPLEX路径
```bash
# 1. 需求探索
# 调用 superpowers:brainstorm skill

# 2. 创建OpenSpec提案
# 调用 lybtzyzs-openspec-proposal skill
# → 生成 openspec/changes/{change-id}/proposal.md

# 3. [CP1] 等待用户确认需求

# 4. 生成设计文档
# 调用 lybtzyzs-openspec-design skill
# → 生成 design.md + tasks.md

# 5. [CP2] 等待用户确认设计

# 6. 任务拆分
# 调用 lybtzyzs-task-breakdown skill (如需更细粒度)

# 7. [CP3] 等待用户确认计划

# 8. 执行实现
# 调用 lybtzyzs-openspec-apply skill

# 9. 生成测试
# 调用 lybtzyzs-test-generator skill

# 10. 编译验证
dotnet build LYBT.All.sln
dotnet test

# 11. [CP4] 验证结果确认

# 12. 质量检查
# 调用 lybtzyzs-code-review skill
# 调用 lybtzyzs-arch-compliance skill

# 13. 生成PR
# 调用 lybtzyzs-pr-generator skill

# 14. [CP5] 等待用户确认交付

# 15. 归档完成
# 调用 lybtzyzs-openspec-archive-finalize skill
```

## Skills触发规则

| 场景关键词 | 触发Skill | 优先级 |
|------------|-----------|--------|
| 新需求、新功能、我想做 | `lybtzyzs-openspec-proposal` | P0 |
| 确认提案、开始设计 | `lybtzyzs-openspec-design` | P0 |
| 确认设计、开始执行 | `lybtzyzs-openspec-apply` | P0 |
| 完成、归档、合并 | `lybtzyzs-openspec-archive-finalize` | P0 |
| 生成测试、写测试 | `lybtzyzs-test-generator` | P1 |
| 代码审查、review | `lybtzyzs-code-review` | P1 |
| 架构检查、合规 | `lybtzyzs-arch-compliance` | P1 |
| 生成PR、创建PR | `lybtzyzs-pr-generator` | P1 |
| 创建Issue | `lybtzyzs-issue-template` | P2 |
| WPF绑定、XAML | `wpf-desktop-dev` | P2 |

## 断点恢复

工作流支持从任意状态恢复：

1. **查看当前状态**:
   ```bash
   ls openspec/changes/  # 查看进行中的变更
   ```

2. **识别中断点**:
   - 有proposal.md无design.md → 从CP1后恢复
   - 有design.md无代码变更 → 从CP2后恢复
   - 有代码变更未编译 → 从CP4恢复

3. **恢复执行**:
   调用对应阶段的skill继续

## 质量门禁

每个工作流必须通过的检查：

| 检查项 | SIMPLE | MEDIUM | COMPLEX |
|--------|--------|--------|---------|
| 编译通过 | 必须 | 必须 | 必须 |
| 单元测试 | 可选 | 推荐 | 必须 |
| code-review | 可选 | 必须 | 必须 |
| arch-compliance | 跳过 | 可选 | 必须 |
| 文档更新 | 可选 | 推荐 | 必须 |

## 常见场景速查

| 用户说 | 复杂度 | 首个Skill |
|--------|--------|-----------|
| "修复这个Bug" | SIMPLE | 直接执行 |
| "添加一个按钮" | SIMPLE | 直接执行 |
| "优化这个方法的性能" | MEDIUM | EnterPlanMode |
| "给这个模块加个新功能" | MEDIUM | brainstorm → EnterPlanMode |
| "重构认证系统" | COMPLEX | lybtzyzs-openspec-proposal |
| "添加深色模式支持" | COMPLEX | lybtzyzs-openspec-proposal |
| "统一设计系统" | COMPLEX | lybtzyzs-openspec-proposal |
