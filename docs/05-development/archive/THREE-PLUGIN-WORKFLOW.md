# OpenCode 三插件协同工作指南

> OpenSpec — 规范闭环 · Superpowers — 技能闭环 · OmO — 执行闭环
> 三重独立生命周期，通过 **tasks.md** 唯一握手点协作

---

## 核心原则：三闭环各司其职

```
┌──────────────────────────────────────────────────────────────┐
│                      OpenSpec 规范闭环                        │
│  /opsx:propose ──→ [人工确认] ──→ /opsx:archive             │
│  owns: proposal | design | tasks | specs                     │
│  dir:  openspec/changes/<id>/                                │
├──────────────────────────────────────────────────────────────┤
│                     Superpowers 技能闭环                      │
│  brainstorming ──→ writing-plans ──→ code-review ──→ verify  │
│  owns: 规划文档 | 审查报告                                    │
│  dir:  docs/superpowers/{plans,reviews}/                     │
│  触发: INSTALL.md 声明 → ulw 执行时自动加载                   │
├──────────────────────────────────────────────────────────────┤
│                       OmO 执行闭环                            │
│  ulw ──→ Sisyphus 多代理编排 ──→ [标记 tasks.md]              │
│  owns: 代理编排 | 代码变更                                    │
│  dir:  src/ | tests/                                         │
└──────────────────────────────────────────────────────────────┘

          三插件唯一握手点：tasks.md（OpenSpec 产出，OmO 执行，Superpowers 审查）
```

---

## 标准工作流：三阶段

### 阶段 1：提案（OpenSpec）

```
/opsx:propose <功能描述>
```

**产出**：
```
openspec/changes/<change-id>/
├── proposal.md    ← OpenSpec 拥有
├── design.md      ← OpenSpec 拥有
├── tasks.md       ← ★ 三插件握手点
└── specs/         ← OpenSpec 拥有
```

---

### 阶段 2：实施（OmO + Superpowers）

```
ulw 按 openspec/changes/<change-id>/tasks.md 实施
```

**内部流程**：

```
Sisyphus 读取 tasks.md + .opencode/INSTALL.md
  │
  ├─ 自动加载 Superpowers 技能（TDD | code-review | Comment Checker）
  │
  ├─ 逐条分解 task → 分派代理实施
  │   ├─ [TDD] 先写测试 → 实现 → 重构
  │   ├─ [code-reviewer] 审查打分（≥ 7/10 才放行）
  │   └─ 通过 → 标记 tasks.md [x]
  │
  └─ 审查报告 → docs/superpowers/reviews/
```

**如需微调**：
```
输入框加防抖，300ms 后再触发
```
AI 自动修改并重新审查。

---

### 阶段 3：归档（OpenSpec）

```
/opsx:archive
```

- 检查 tasks.md 全 `[x]` → 移入 `openspec/changes/archive/`
- 更新 spec delta

---

## 为什么不用 /start-work

`/start-work` 是 Sisyphus 内置的 boulder 执行命令，直接读取 `.sisyphus/plans/` 并逐条执行 checkbox，**不经过 Prometheus 面试也不触发 Superpowers 技能加载**。它绕过了三插件协同设计的核心机制。

正确做法：直接使用 `ulw`，它通过 `.opencode/INSTALL.md` 自动加载 Superpowers 技能。

---

## 三闭环状态一览

| 闭环 | 开启指令 | 关闭指令 | 产物目录 |
|------|---------|----------|----------|
| **OpenSpec 规范** | `/opsx:propose` | `/opsx:archive` + `/opsx:sync` | `openspec/changes/` |
| **Superpowers 技能** | INSTALL.md → ulw 自动加载 | code-review ≥ 7/10 | `docs/superpowers/` |
| **OmO 执行** | `ulw` | 全部 task `[x]` | `src/` `tests/` |

---

## 握手点：tasks.md 是唯一权威

```
  OpenSpec                        OmO                         Superpowers
     │                             │                              │
     ├─ create tasks.md ──────────┼──── read tasks.md ───────────┤
     │                             │                              │
     │                      Sisyphus dispatches              TDD skill
     │                         hephaestus writes              code-review
     │                             │                              │
     │                             ├── mark [x] in tasks.md ─────┤
     │                             │                              │
     ├─ check all [x] ←────────────┘                    review report
     │                                                          │
     ├─ /opsx:archive                                    → docs/superpowers/reviews/
```

---

## 指令速查表

| # | 指令 | 插件 | 功能 |
|---|------|------|------|
| 1 | `/opsx:propose <描述>` | OpenSpec | 创建提案闭环 |
| 2 | `ulw 按 openspec/changes/<id>/tasks.md 实施` | OmO + Superpowers | 多代理编排实施 |
| 3 | `/opsx:archive` | OpenSpec | 关闭规范闭环 |
| 4 | `/opsx:sync` | OpenSpec | 同步 spec delta（如有） |

### 辅助指令

| 指令 | 用途 |
|------|------|
| `@skill <技能名> <任务>` | 手动调用 Superpowers 技能 |
| `ulw <简短任务描述>` | 轻量变更直接 OmO 执行 |
| `/opsx:verify` | OpenSpec 验证变更 |

---

## 目录结构

```
LYBTZYZS/
├── .opencode/
│   ├── INSTALL.md            ← Superpowers 入口
│   ├── opencode.json         ← 插件配置
│   └── skills/               ← 自定义技能
│
├── openspec/                 ← ★ OpenSpec 闭环产物
│   └── changes/
│       ├── <change-id>/      ← 当前变更
│       │   ├── proposal.md
│       │   ├── design.md
│       │   ├── tasks.md      ← ★ 三插件唯一握手点
│       │   └── specs/
│       └── archive/          ← 已完成变更
│
├── docs/
│   ├── superpowers/          ← ★ Superpowers 闭环产物
│   │   ├── plans/            ← Prometheus 规划文档
│   │   └── reviews/          ← code-review 审查报告
│   └── plans/                ← 项目设计文档
│
├── src/                      ← ★ OmO 闭环产物（代码）
└── tests/                    ← ★ OmO 闭环产物（测试）
```

## 常见场景

### 场景 1：日常功能开发

```
/opsx:propose 药材库存增加预警阈值设置
→ 审阅 proposal / design / tasks
→ ulw 按 openspec/changes/xxx/tasks.md 实施
→ AI 干活，等待汇报
→ /opsx:archive
```

### 场景 2：修 Bug

```
/opsx:propose 修复处方打印特殊字符显示异常
→ ulw 按 tasks.md 实施
→ /opsx:archive
```

### 场景 3：轻量变更（跳过 OpenSpec）

```
ulw 给登录窗口的按钮加 loading 状态
```

---


## 注意事项

### 1. tasks.md 是单一事实来源

- OpenSpec 创建它，OmO 执行它，Superpowers 审查围绕它
- **任何一方不得创建独立的 tasks 文件**

### 2. 闭环必须按序关闭

```
propose → ulw(全[x]) → archive → sync
  ✅         ✅           ✅       ✅
```

### 3. 审查标准

- code-review ≥ 7/10 才允许标记 `[x]`
- 审查报告归档到 `docs/superpowers/reviews/<change-id>.md`

### 4. INSTALL.md 是 Superpowers 技能的加载入口

- OmO 的 Sisyphus 在 `ulw` 执行时读取 INSTALL.md
- 声明的技能（TDD、code-review、Comment Checker）自动触发
- 不要手动调用这些技能，除非需要特定参数

---

> 编写于 2026-06-04 | 基于 OpenCode v1.15+ · oh-my-openagent v4.7.5
