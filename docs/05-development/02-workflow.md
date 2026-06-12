# 开发流程规范

> **工具链**: OpenSpec（规格）+ Superpowers（方法）+ GSD（流程）
> **辅助**: GitNexus（代码智能）
> **版本**: v2.0 — 新增 OpenSpec + Superpowers 结合指南

---

## 分工速记

| 工具 | 责任 | 一句话 | 核心技能 |
|------|------|--------|----------|
| **OpenSpec** | 提案 → 设计 → 验收条件 → 任务 | "做正确的事" | explore, propose, apply, archive |
| **Superpowers** | 探索 → 规划 → 执行 → 审查的方法 | "用正确的方法" | brainstorming, write-plan, TDD, code-review |
| **GSD** | 阶段切换 → 状态追踪 → 进度检查 | "不跳阶段" | plan-phase, execute-phase, verify |
| **GitNexus** | 代码搜索 → 影响分析 → 变更检测 | "不做错的事" | query, impact, detect-changes |

---

## 标准流程一览

```
  1. 接手         →  openspec list + git status
  2. 探索 (可选)   →  openspec-explore + superpowers:brainstorming + GitNexus
  3. 规划 (可选)   →  openspec-propose + superpowers:write-plan
                    或 task_plan.md（轻量）
  4. 执行         →  openspec-apply + superpowers:TDD/debug
                    [GitNexus impact → 实现 → build → test → detect-changes] × N
  5. 验证         →  superpowers:code-review + verification
                    dotnet test → openspec archive
```

---

## OpenSpec + Superpowers 结合指南

### 结合原则

```
┌─────────────────────────────────────────────────────────────────┐
│                     完整开发流程                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 1. 探索阶段                                              │   │
│  │    openspec-explore + superpowers:brainstorming          │   │
│  │    • OpenSpec 提供上下文感知（已有变更、主规格）           │   │
│  │    • Superpowers 提供多视角探索、方案对比                 │   │
│  │    • 产出：澄清的需求、确定的方向                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            ↓                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 2. 规划阶段                                              │   │
│  │    openspec-propose + superpowers:writing-plans          │   │
│  │    • OpenSpec 生成 proposal/design/specs/tasks           │   │
│  │    • Superpowers 细化任务拆分、补充实施细节               │   │
│  │    • 产出：完整的变更目录，可执行的任务列表               │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            ↓                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 3. 执行阶段                                              │   │
│  │    openspec-apply + superpowers:test-driven-development  │   │
│  │    • OpenSpec 逐条执行任务，标记完成                      │   │
│  │    • Superpowers TDD 处理复杂逻辑                         │   │
│  │    • GitNexus 影响分析 + 变更检测                         │   │
│  │    • 产出：可工作的代码，任务逐步完成                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            ↓                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 4. 审查阶段                                              │   │
│  │    superpowers:requesting-code-review                    │   │
│  │    superpowers:verification-before-completion            │   │
│  │    • 代码审查：逻辑、安全、性能                           │   │
│  │    • 完成前验证：测试覆盖、规格符合度                     │   │
│  │    • 产出：审查报告，修复建议                             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            ↓                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 5. 归档阶段                                              │   │
│  │    openspec-sync-specs + openspec-archive                │   │
│  │    • 同步 Delta Specs 到主规格                            │   │
│  │    • 归档变更，保留完整记录                               │   │
│  │    • 产出：更新后的主规格，归档的变更                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 阶段对照表

| 阶段 | OpenSpec 提供 | Superpowers 提供 | 结合效果 |
|------|--------------|-----------------|----------|
| **探索** | 上下文感知（已有变更、主规格） | 多视角探索、方案对比 | 需求更清晰，方案更合理 |
| **规划** | 形式化规格（SHALL/Scenario） | 任务拆分、实施细节 | 既有验收标准，又有执行路径 |
| **执行** | 任务追踪、进度管理 | TDD、系统性调试 | 代码质量高，进度可控 |
| **审查** | 规格符合度检查 | 代码审查、安全/性能分析 | 既符合需求，又高质量 |
| **归档** | Delta Specs 同步、变更归档 | — | 知识沉淀，可追溯 |

### 最佳实践

1. **探索先行**：不确定方案时，先用 `openspec-explore` + `superpowers:brainstorming`
2. **规格驱动**：复杂功能用 `openspec-propose` 生成完整规格（proposal/design/specs/tasks）
3. **TDD 复杂逻辑**：执行阶段对复杂逻辑用 `superpowers:test-driven-development`
4. **审查必做**：完成实现后用 `superpowers:requesting-code-review` 审查
5. **及时归档**：完成后用 `openspec-archive` 同步规格、归档变更

---

## 阶段 1：接手 (Session Start)

**目标**: 确认"当前做到哪了、下一步做什么"

### 执行步骤

```bash
# 1. 检查 OpenSpec 活跃变更
openspec list --json

# 2. 检查 git 状态
git status
git log --oneline -3

# 3. 检查规划文件（如果有）
Read task_plan.md        # planning-with-files 自动注入，需要时 Read
Read findings.md         # 同上

# 4. 检查 GSD 阶段状态
ls .opencode/state/        # 可选的，确认是否有进行中的阶段
```

### 解释

- `openspec list --json` 返回所有活跃的 change。如果有，说明有未完成的规格工作
- `git status` 看有没有未提交的改动
- 如果存在 `task_plan.md`，说明上次在执行 planning-files 流程，Read 恢复上下文

---

## 阶段 2：探索 (Explore)

**目标**: 理解代码、确认影响范围、决定方案

**入口**: 新功能 / 不熟悉的 Bug / 不确定怎么改的重构

### 执行步骤

```bash
# ── 第一步：OpenSpec 上下文感知 ──
Skill("openspec-explore")
# OpenSpec 自动检查已有变更：openspec list --json
# 如果有相关变更，会自动读取 proposal/design/specs 作为上下文

# ── 第二步：GitNexus 代码搜索 ──
# 通过概念搜索执行流（比 grep 更准确）
gitnexus_query({query: "医案查询"})
gitnexus_query({query: "患者搜索+分页"})
gitnexus_query({query: "验方导入处方"})
# query 返回按执行流分组的代码，直接知道哪些文件参与了流程

# ── 深入看某个函数 / 类 ──
gitnexus_context({name: "MedicalCaseRepository"})
gitnexus_context({name: "GetPagedAsync"})
gitnexus_context({name: "IApiRouter"})

# ── 算改动影响范围（改之前必须做）──
gitnexus_impact({target: "函数/类名", direction: "upstream"})

# ── 第三步：Superpowers 方案探索 ──
# 不确定方案时，用 brainstorming 多视角探索
Skill("superpowers:brainstorming")
# 传入参数描述要探索的问题
# 例如："需要实现 URL 驱动连接切换，支持运行时动态切换远程/本地模式"
```

### OpenSpec + Superpowers 结合点

| 动作 | OpenSpec | Superpowers | 效果 |
|------|---------|-------------|------|
| 检查已有变更 | `openspec list --json` | — | 避免重复劳动 |
| 读取变更上下文 | 自动读取 proposal/design/specs | — | 理解决策背景 |
| 探索方案 | 提供项目约束和规范 | brainstorming 多视角分析 | 方案更合理 |
| 捕获决策 | 提供 design.md/specs 存储位置 | — | 决策可追溯 |

### 关卡

如果 `gitnexus_impact` 返回 **HIGH** 或 **CRITICAL** 风险，必须确认后再继续。

---

## 阶段 3：规划 (Plan)

**目标**: 确定"先做什么、再做什么"

**入口**: 修改涉及 3+ 文件，或不确定技术方案

### 路径 A：OpenSpec + Superpowers（推荐）

```bash
# 1. OpenSpec 生成完整规格
Skill("openspec-propose") "feature-name"
# 自动生成：
#   openspec/changes/{date}-{feature-name}/
#     ├── proposal.md      ← 需求背景（Why）
#     ├── design.md        ← 技术方案（How）
#     ├── specs/spec.md    ← 验收条件（SHALL/Scenario）
#     └── tasks.md         ← 实施清单（What）

# 2. 检查生成的状态
openspec status --change "feature-name" --json

# 3. （可选）Superpowers 细化任务拆分
# 如果 OpenSpec 生成的 tasks 不够细致，用 write-plan 补充
Skill("superpowers:writing-plans")
# 读取 tasks.md，进一步拆分实施细节
# 将细化后的任务写回 tasks.md

# 4. 确认规格就绪
openspec instructions apply --change "feature-name" --json
# 返回 applyRequires 中所有 artifact 的状态
# 全部 "done" 即可开始实施
```

**OpenSpec 写文件顺序**：`proposal → design → specs → tasks`

**读文件顺序**：`proposal → design → tasks`（先读背景，再看做什么）

#### OpenSpec + Superpowers 结合点

| 步骤 | OpenSpec | Superpowers | 结合效果 |
|------|---------|-------------|----------|
| 生成规格 | `openspec-propose` 一键生成 4 个 artifact | — | 快速产出完整规格 |
| 细化任务 | tasks.md 提供任务框架 | `writing-plans` 拆分实施细节 | 任务更可执行 |
| 补充验收条件 | specs/spec.md 提供 SHALL 格式 | brainstorming 补充边界场景 | 验收更完整 |

### 路径 B：Planning-Files（轻量，适合快速任务）

```bash
# 创建 task_plan.md（阶段分解 + 任务列表）
Write("task_plan.md", "# Task Plan: 标题
## Goal
...
## Phases
| # | Phase | Status |
|---|-------|--------|
| 1 | ... | pending |
| 2 | ... | pending |
## Errors Encountered
| Error | Attempt | Resolution |
")

# 创建 findings.md（研究发现）
Write("findings.md", "# Findings
## 关键发现
...

## 架构信息
...
")

# 创建 progress.md（执行日志）
Write("progress.md", "# Progress Log
## Session {date}
- ...
")
```

### 路径选择

| 对比 | OpenSpec | Planning-Files |
|------|----------|----------------|
| 验收条件 | 形式化 (SHALL/Scenario) | 自由描述 |
| CLI 管理 | `openspec new/archive` | 手动 |
| 归档 | `openspec archive` | 手动移入 `docs/plans/archive/` |
| 适合 | 新功能、需要明确验收标准的变更 | 重构、修正、小型任务 |

---

## 阶段 4：执行 (Execute)

**目标**: 逐条完成任务，产出可工作的代码

### 执行循环

```
对 tasks.md 中每条 [ ] 任务：
  1. 算影响     → GitNexus impact
  2. 实现        → 修改代码（复杂逻辑用 TDD）
  3. 编译        → dotnet build
  4. 跑测试      → dotnet test --filter ...
  5. 检测范围    → GitNexus detect-changes
  6. 标记完成    → tasks.md 中 [ ] → [x]
```

### 具体指令

```bash
# ── 启动执行 ──
Skill("openspec-apply")
# OpenSpec 自动：
#   1. 读取 proposal/design/specs/tasks 作为上下文
#   2. 显示当前进度（N/M tasks complete）
#   3. 逐条执行 pending 任务
#   4. 每条任务完成后自动标记 [x]

# ── 每条任务之前：算影响范围（必做）──
gitnexus_impact({target: "要修改的类/函数名", direction: "upstream"})

# 例：
gitnexus_impact({target: "PatientRepository", direction: "upstream"})
gitnexus_impact({target: "CreatePatientAsync", direction: "upstream"})

# ── 实现阶段（按复杂度选择方法）──

# 方法 1：TDD（复杂逻辑推荐）
Skill("superpowers:test-driven-development")
# 技能指导：写测试骨架 → 实现 → 验证 → 重构
# 适合：业务逻辑复杂、边界条件多、需要高测试覆盖的场景

# 方法 2：直接实现（简单任务）
Edit("src/.../SomeFile.cs")
# 改代码匹配周围风格
# 适合：简单的 CRUD、配置修改、typo 修复

# 方法 3：系统性调试（Bug 修复）
Skill("superpowers:systematic-debugging")
# 技能指导：假设 → 验证 → 定位 → 修复
# 适合：不确定根因的 Bug

# ── 编译验证（必做）──
dotnet build src/Client/Desktop/LYBT.Desktop.sln
# 有 error 必须先修复，不能带着 error 继续

dotnet build LYBTZYZS.sln
# 全局编译确保不破坏其他模块

# ── 跑相关测试（必做）──
# 按模块：
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Patient"
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~MedicalCase"
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Herb"

# 按方法名：
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~TestMethodName"

# ── 每个提交之前：检测变更范围（必做）──
gitnexus_detect_changes()
# 返回 changed symbols + affected processes + risk summary
# 确认只影响了预期中的代码

# ── 遇到阻塞时 ──
# openspec-apply 会自动暂停并报告问题
# 可以选择：
#   1. 更新 tasks.md 中的任务描述
#   2. 更新 design.md 中的技术方案
#   3. 跳过当前任务，继续下一个
```

### OpenSpec + Superpowers 结合点

| 场景 | OpenSpec | Superpowers | 结合效果 |
|------|---------|-------------|----------|
| 逐条执行任务 | `openspec-apply` 追踪进度 | — | 任务管理自动化 |
| 复杂逻辑实现 | tasks.md 定义任务范围 | `test-driven-development` 保证质量 | 代码质量高 |
| Bug 修复 | — | `systematic-debugging` 定位根因 | 修复更准确 |
| 遇到阻塞 | 自动暂停，建议更新 artifact | — | 及时调整方案 |

### 硬性规则

| 规则 | 为什么 |
|------|--------|
| 改任何符号前跑 `impact` | 不知道影响范围就是盲改 |
| 提交前跑 `detect-changes` | 防止改了不该改的东西 |
| `dotnet build` 0 errors | 断编译比修 bug 更麻烦 |
| 一次只做一个任务 | 多个任务混在一起，回滚和排查都难 |
| 不修改任务范围外的代码 | 保持每个 change 内聚 |
| 复杂逻辑用 TDD | 测试覆盖保证正确性 |

---

## 阶段 5：验证 (Verify)

**目标**: 确认"做对了、没漏掉"

### 5.1 代码审查

```bash
# 快速审查（一行就叫）
/review

# 正规审查（有 checklist + 打分）
Skill("code-review:code-review")

# 多视角审查（推荐）
Skill("superpowers:requesting-code-review")
# 从正确性、安全性、性能、可维护性等多维度审查

# 完成前验证
Skill("superpowers:verification-before-completion")
# 检查：测试覆盖是否充分、规格是否符合、有无遗漏
```

### 5.2 测试

```bash
# 全量测试
dotnet test LYBTZYZS.sln

# 按项目
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Architecture/
```

### 5.3 归档

```bash
# ── OpenSpec 归档（推荐）──
Skill("openspec-archive")
# 自动执行：
#   1. 检查 artifact 和 task 完成状态
#   2. 评估 delta specs 同步状态
#   3. 同步 delta specs 到主规格（openspec-sync-specs）
#   4. 移动到 archive 目录

# 或手动同步 specs（如果需要先同步再归档）
Skill("openspec-sync-specs")
# 将 change 中的 specs 同步到 openspec/specs/<capability>/spec.md

# ── Planning-Files 归档──
mkdir -p docs/plans/archive/
mv task_plan.md docs/plans/archive/{date}-{feature-name}-plan.md
mv findings.md docs/plans/archive/{date}-{feature-name}-findings.md
mv progress.md docs/plans/archive/{date}-{feature-name}-progress.md

# ── 全局检查 ──
git status                     # 确认所有文件已追踪
git diff --stat                # 确认改动范围
```

### OpenSpec + Superpowers 结合点

| 步骤 | OpenSpec | Superpowers | 结合效果 |
|------|---------|-------------|----------|
| 代码审查 | 检查规格符合度 | `requesting-code-review` 多维度审查 | 既符合需求，又高质量 |
| 完成前验证 | specs/spec.md 作为验收标准 | `verification-before-completion` 系统化检查 | 不遗漏验收条件 |
| 同步规格 | `openspec-sync-specs` 合并 delta → main | — | 主规格保持最新 |
| 归档 | `openspec-archive` 完整归档 | — | 知识沉淀，可追溯 |

---

## 场景速查（带指令）

### 场景 A：修 typo / 一行改动

```
1. 接手    → git status
2. 执行    → Edit("文件路径") + dotnet build
3. 提交    → git add . && git commit -m "fix: ..."
```

**工具组合**：无（直接编辑）

### 场景 B：修 Bug

```
1. 接手    → git status
2. 理解    → Skill("openspec-explore")     # 理解问题上下文
              gitnexus_query({query: "出错功能"})
              gitnexus_context({name: "相关函数"})
3. 规划    → Skill("openspec-propose") "fix-bug-name"  # 如果涉及多文件
              或直接进入执行
4. 执行    → Skill("openspec-apply")       # 按任务执行
              gitnexus_impact → Edit → dotnet build
              dotnet test ... --filter "TestMethodName"
              gitnexus_detect_changes()
5. 验证    → Skill("superpowers:verification-before-completion")
6. 归档    → Skill("openspec-archive")
```

**工具组合**：openspec-explore + GitNexus + openspec-propose/apply + verification

### 场景 C：新增功能（多模块）

```
1. 接手    → openspec list --json + git status
2. 探索    → Skill("openspec-explore")     # 理解需求，探索方案
              Skill("superpowers:brainstorming")  # 多视角探索
              gitnexus_query({query: "功能概念"})
3. 规划    → Skill("openspec-propose") "feature-x"  # 生成完整规格
              Skill("superpowers:writing-plans")     # 细化任务拆分（可选）
              openspec status --change "feature-x" --json
4. 执行    → Skill("openspec-apply")       # 逐条执行任务
              复杂逻辑 → Skill("superpowers:test-driven-development")
              gitnexus_impact → 实现 → dotnet build → test → [x]
5. 审查    → Skill("superpowers:requesting-code-review")
              Skill("superpowers:verification-before-completion")
6. 验证    → dotnet test LYBTZYZS.sln
              gitnexus_detect_changes()
7. 归档    → Skill("openspec-archive")
```

**工具组合**：完整流程 — explore + brainstorming + propose + writing-plans + apply + TDD + code-review + verification + archive

### 场景 D：重构

```
1. 接手    → git status（确保干净）
2. 理解    → Skill("openspec-explore")     # 理解重构范围
              gitnexus_impact({target: "每个要改的符号", direction: "upstream"})
3. 规划    → Skill("superpowers:writing-plans")  # 分阶段计划
              或 Skill("openspec-propose") "refactor-name"
4. 执行    → Skill("superpowers:executing-plans")  # 按计划执行
              每阶段：
                gitnexus_impact → 重构 → dotnet build
                dotnet test ... → 确认不破坏功能
                gitnexus_detect_changes()
5. 验证    → Skill("superpowers:verification-before-completion")
              dotnet test LYBTZYZS.sln
6. 归档    → Skill("openspec-archive")（如果用了 OpenSpec）
```

**工具组合**：openspec-explore + writing-plans/executing-plans + verification + archive

### 场景 E：技术调研 / 方案探索

```
1. 探索    → Skill("openspec-explore")     # OpenSpec 上下文感知
              Skill("superpowers:brainstorming")  # 多视角探索
              gitnexus_query({query: "调研主题"})
2. 捕获    → 如果有明确结论：
              Skill("openspec-propose") "exploration-result"  # 记录决策
              或更新 design.md / specs
3. 归档    → Skill("openspec-archive")（如果创建了 change）
```

**工具组合**：openspec-explore + brainstorming + 可选 propose/archive

---

## 技能速查表

### OpenSpec Skills

| 技能 | 作用 | 什么时候用 |
|------|------|------------|
| `Skill("openspec-explore")` | 探索模式（只思考不实现） | 不确定方案时，理清需求和技术方向 |
| `Skill("openspec-propose")` | 一键生成完整规格 | 开始新功能/多文件修改时 |
| `Skill("openspec-apply")` | 按任务执行 | 有 tasks.md 后，逐条实施 |
| `Skill("openspec-archive")` | 归档变更 | 实现完成，同步 specs 并归档 |
| `Skill("openspec-sync-specs")` | 同步 delta specs 到 main | 归档前同步规格 |

### OpenSpec CLI

| 命令 | 作用 |
|------|------|
| `openspec list --json` | 查看所有活跃 change |
| `openspec new change "name"` | 创建新 change 目录 |
| `openspec status --change "name" --json` | 查看 change 状态 |
| `openspec instructions apply --change "name" --json` | 获取实施指引 |
| `openspec archive "name"` | 归档已完成的 change |
| `openspec schemas --json` | 列出可用 schema |

### Superpowers Skills

| 技能 | 作用 | 什么时候用 |
|------|------|------------|
| `Skill("superpowers:brainstorming")` | 多视角探索方案 | 不确定怎么做时 |
| `Skill("superpowers:writing-plans")` | 写实施计划 | 需要细化任务拆分时 |
| `Skill("superpowers:executing-plans")` | 按计划执行 | 有详细计划时 |
| `Skill("superpowers:test-driven-development")` | TDD | 复杂逻辑实现 |
| `Skill("superpowers:systematic-debugging")` | 系统性调试 | 不确定根因的 Bug |
| `Skill("superpowers:requesting-code-review")` | 代码审查 | 完成实现后 |
| `Skill("superpowers:verification-before-completion")` | 完成前验证 | 确认没遗漏 |

### GitNexus

| 指令 | 作用 | 什么时候用 |
|------|------|------------|
| `gitnexus_query({query: "概念"})` | 搜索执行流 | 找相关代码时 |
| `gitnexus_context({name: "符号"})` | 符号全景 | 看函数/类的调用关系时 |
| `gitnexus_impact({target:"符号", direction:"upstream"})` | 影响范围 | **改任何符号前** |
| `gitnexus_detect_changes()` | 变更检测 | **任何提交前** |
| `gitnexus_rename({symbol_name:"旧名", new_name:"新名"})` | 智能重命名 | 跨文件重命名时 |

### 内置工具

| 工具 | 作用 | 什么时候用 |
|------|------|------------|
| `Read(path)` | 读文件 | 任何时候 |
| `Write(path, content)` | 写文件 | 创建新文件 |
| `Edit(path, old, new)` | 改文件 | 修改已有文件 |
| `Glob(pattern)` | 搜文件名 | 找文件路径 |
| `Grep(pattern)` | 搜文件内容 | 已知关键字 |
| `Bash(command)` | 运行 shell | 编译、测试、git |
| `TaskCreate/TaskUpdate` | 任务管理 | 追踪执行进度 |

---

## 附录：工具选择决策树

```
要做什么？
├── 修 typo / 一行改动 → 直接 Edit + build
├── 修 Bug
│   ├── 简单 Bug（知道根因）→ 直接 Edit + test
│   └── 复杂 Bug（不确定根因）→ systematic-debugging
├── 新增功能
│   ├── 单文件 → 直接 Edit + test
│   └── 多文件 / 多模块 → openspec-propose + apply
├── 重构
│   ├── 小范围 → writing-plans + executing-plans
│   └── 大范围 → openspec-propose + apply
└── 技术调研 → openspec-explore + brainstorming

什么时候用 OpenSpec？
├── 修改涉及 3+ 文件 ✓
├── 需要明确验收标准 ✓
├── 多模块协作 ✓
└── 简单修改 ✗（直接编辑更快）

什么时候用 Superpowers？
├── 不确定方案 → brainstorming
├── 需要拆分任务 → writing-plans
├── 复杂逻辑 → test-driven-development
├── 不确定 Bug 根因 → systematic-debugging
└── 完成实现后 → requesting-code-review + verification-before-completion
```
