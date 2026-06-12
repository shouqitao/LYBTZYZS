# PRD 审计与 v2.0 方向讨论

> **编制日期**: 2026-04-27
> **目的**: 基于 138 US 全量实现现状，讨论 v2.0 开发方向
> **前置阅读**: roadmap.md, user-story-map.md, dual-mode.md

---

## 一、现状总览

### v1.0 已完成

| 指标 | 数值 |
|------|------|
| 总 US | 138 (15 模块) |
| Must Have | 51/51 ✅ (100%) |
| Should Have | 54 中 46+ 已实现 |
| Could Have | 33 中 33 已实现 |
| 测试用例 | 2021+ (Server 1185 + Desktop 760 + Architecture 76) |
| 技术债务 | 全部清零 (CODE-01 ~ CODE-37) |
| Sprint | 1~6 全部完成 |

**结论**: v1.0-rc 已达成，v1 核心功能已基本实现。

### 当前架构

```
┌─────────────────────────────────────────────────────┐
│                   Desktop (WPF/Prism)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │  Remote  │  │  Local   │  │  LocalWebAPI     │  │
│  │  Refit   │  │  Repos   │  │  (嵌入式 Kestrel) │  │
│  │  HTTP →  │  │  EF Core │  │  + SQLite        │  │
│  │  Server  │  │  LocalDB │  │  + HTTP Proxy    │  │
│  └────┬─────┘  └────┬─────┘  └────────┬─────────┘  │
│       │              │                 │             │
│       └──────────────┴─────────────────┘             │
│                    工厂模式路由                        │
│         IConnectionModeProvider.CurrentMode           │
└─────────────────────────────────────────────────────┘
          │                              │
          ▼                              ▼
   Server WebAPI                    SQLite 单文件
   (SQL Server)                    (%LocalAppData%/LYBT/)
```

**关键变更**: Local 模式的实现从 LocalDB 改为 LocalWebAPI（嵌入式 Kestrel + SQLite），但架构上仍然是"双模式"（Remote / Local），LocalWebAPI 是 Local 模式的实现方式，不是第三种模式。

---

## 二、需要讨论的 5 个问题（深化版）

### Q1: v2.0 范围 — 哪些 Should/Could 项需要优先实现？

**背景**: roadmap.md 中 54 个 Should Have 和 33 个 Could Have 大部分已实现，但以下项目前状态不明确：

#### Should Have 待确认项（请逐一确认是否已实现）

| US 编号 | 模块 | 名称 | 当前状态 | 需要你确认 |
|---------|------|------|----------|-----------|
| US-PAT-006 | Patients | 患者恢复 | 代码有 RestoreAsync 方法 | 功能是否完整可用？ |
| US-HERB-007 | Herbs | 药材恢复 | 代码有 RestoreAsync | 功能是否完整可用？ |
| US-FORM-007 | Formulas | 验方恢复 | 代码有 RestoreAsync | 功能是否完整可用？ |
| US-USER-006 | Users | 用户恢复 | 代码有 RestoreAsync | 功能是否完整可用？ |
| US-MC-008 | MedicalCase | 医案取消 | 已实现 CancelMedicalCaseAsync | 是否满足 PRD 要求？ |
| US-MC-014 | MedicalCase | 锁定规则 | 已实现打印保护 | 是否满足 PRD 要求？ |
| US-MC-017 | MedicalCase | 待诊队列 | 已实现 PendingQueueView | 是否满足 PRD 要求？ |
| US-PRINT-002 | Printing | 打印预览 | WPF FixedDocument 已实现 | 预览功能是否完整？ |
| US-PRINT-003 | Printing | 版本管理 | PrintVersion 已实现 | 是否满足 PRD 要求？ |
| US-SYNC-001~005 | Sync | 同步基础 | 6 个端点已实现 | 同步工作流是否完整可用？ |

#### Could Have 待确认项

| US 编号 | 模块 | 名称 | 当前状态 | 需要你确认 |
|---------|------|------|----------|-----------|
| US-PAT-006~012 | Patients | 恢复/批量/导入/导出 | 代码已实现 | 功能是否完整可用？ |
| US-HERB-007,010,012,013 | Herbs | 恢复/JSON导入/模板/引用 | 代码已实现 | 功能是否完整可用？ |
| US-FORM-007,011,013 | Formulas | 恢复/批量导入/模板 | 代码已实现 | 功能是否完整可用？ |
| US-MC-012 | MedicalCase | 审计日志 | MedicalCaseAuditController 已实现 | 审计功能是否完整？ |
| US-USER-006,007 | Users | 恢复/批量删除 | 代码已实现 | 功能是否完整可用？ |
| US-SYS-001~009 | Health | 健康检查/诊断 | HealthController + DiagnosticsController | 是否满足 PRD 要求？ |
| US-LOG-004~006 | Logging | 日志级别/清理 | Serilog 已实现 | 日志清理是否自动执行？ |
| US-PRINT-004 | Printing | 打印日志 | MedicalCasePrintController 已实现 | 是否满足 PRD 要求？ |

**我的建议**: 
- 如果上述 US 都已实现 → v2.0 从零开始规划新功能
- 如果部分未实现 → 先补齐 v1.0 遗留，再规划 v2.0
- **请你确认**: 上述列表中，哪些是你认为"还没做完"或"做得不够好"的？

---

### Q2: LocalWebAPI 定位 — 替代还是共存？

**背景**: 当前代码中存在两套 Local 模式实现：

| 实现方式 | 技术栈 | 优势 | 劣势 |
|----------|--------|------|------|
| **LocalDB** (旧) | SQL Server LocalDB + EF Core | 与 Server 端 SQL Server 完全兼容 | 需要安装 LocalDB，部署复杂 |
| **LocalWebAPI** (新) | 嵌入式 Kestrel + SQLite + HTTP Proxy | 零依赖，单文件部署，统一数据访问层 | 内存占用略高 (Kestrel ~5-10MB) |

**当前状态**: 
- `ConnectionMode` 枚举有 3 个值: `Remote`, `Local`, `LocalWebAPI`
- 工厂模式支持三种路由
- LocalWebAPI 代码已完整实现（DbContext + Controllers + Host + Proxy Repos）

**需要你决策**:

| 选项 | 说明 | 影响 |
|------|------|------|
| **A. 完全替代** | LocalWebAPI 替代 LocalDB，删除 Local 相关代码 | 代码量减少 ~30%，但失去 LocalDB 备份路径 |
| **B. 并行共存** | 保留两种 Local 实现，用户可选择 | 代码量增加，但给用户更多选择 |
| **C. 渐进迁移** | 默认 LocalWebAPI，保留 Local 作为 fallback | 折中方案，降低迁移风险 |

**我的建议**: **选项 A (完全替代)**。理由：
1. SQLite 零部署优势明显，LocalDB 安装门槛高
2. 维护两套 Local 实现成本过高（6 对 Repository）
3. LocalWebAPI 已完整实现，可以独立工作

---

### Q3: Sync 同步模块 — 冲突解决 UI 是否必须在 v2.0 完成？

**背景**: Sync 模块 8 个 US 中，核心同步流程已实现：

| US 编号 | 名称 | 实现状态 |
|---------|------|----------|
| US-SYNC-001 | 获取元数据 | ✅ 已实现 |
| US-SYNC-002 | 比对差异 | ✅ 已实现 (SHA256) |
| US-SYNC-003 | 上传本地数据 | ✅ 已实现 |
| US-SYNC-004 | 下载服务器数据 | ✅ 已实现 |
| US-SYNC-005 | 冲突检测 | ✅ 已实现 |
| US-SYNC-006 | 同步删除 | ✅ 已实现 |
| US-SYNC-007 | 完整同步工作流 | ✅ 已实现 (6 状态 FSM) |
| US-SYNC-008 | 模式切换 | ✅ 已实现 |

**但以下细节需要确认**:

| 关注点 | 现状 | 问题 |
|--------|------|------|
| 冲突解决 UI | SyncView 有状态消息和重试按钮 | 是否有独立的冲突对比对话框？ |
| 字段级对比 | SyncConflictDetailDto 已定义 | UI 是否展示 ChangedFields 详情？ |
| MedicalCase 冲突 | Server Wins 策略已实现 | 是否需要手动选择保留本地/服务器版本？ |

**需要你决策**:

| 选项 | 说明 | 工作量 |
|------|------|--------|
| **A. 当前够用** | 现有 SyncView 已满足基本同步需求 | 0 (无需额外工作) |
| **B. 增强冲突 UI** | 增加冲突对比对话框，字段级高亮显示 | 2-3 周 |
| **C. MedicalCase 手动冲突** | 医案冲突时弹出选择界面 | 3-4 周 |

**我的建议**: **选项 A (当前够用)**。理由：
1. v1.0 同步基础已完整，冲突概率低（仅 MedicalCase 可能冲突）
2. 冲突解决 UI 属于增强功能，不影响核心业务
3. 可在 v2.0 后期根据用户反馈决定是否增强

---

### Q4: 身份证读卡器 — 华大 HD100 硬件是否可用？

**背景**: CardReader 模块 2 个 US 代码已实现：

| US 编号 | 名称 | 实现状态 |
|---------|------|----------|
| US-CARD-001 | 读卡器连接与读取 | ✅ 代码已实现 |
| US-CARD-002 | 读卡数据填充 | ✅ 代码已实现 |

**但硬件集成状态不明**:

| 关注点 | 现状 | 问题 |
|--------|------|------|
| 华大 HD100 硬件 | 代码有 CardReaderOptions 配置 | 是否已采购？是否已连接测试？ |
| 降级链 | IdNumber → Name+BirthDate → MultipleCandidates → NoMatch | 降级链逻辑已实现，但未在真实硬件上验证 |
| 照片加密 | DPAPI 加密存储 | 代码已实现，但未与读卡器照片联动测试 |

**需要你确认**:

| 问题 | 选项 |
|------|------|
| 华大 HD100 是否已采购？ | 是 / 否 / 计划中 |
| 是否可在开发环境连接测试？ | 是 / 否 |
| 如果硬件不可用，是否接受 Mock 测试覆盖？ | 是 / 否 |

**我的建议**: 如果硬件暂不可用，先用 Mock 覆盖读卡流程，确保代码逻辑正确，待硬件到位后再做集成测试。

---

### Q5: 下一步方向 — 先补缺口还是新功能？

**背景**: v1.0 已 99.3% 完成，剩余工作分为两类：

#### 类型 A: 现有功能完善（补缺口）

| 项目 | 工作量 | 优先级 |
|------|--------|--------|
| LocalWebAPI ADR 补充 | 1 天 | 高 |
| 文档数据更新 (1654→2021 tests) | 1 天 | 高 |
| Sync 冲突 UI 增强 (如果选 B/C) | 2-4 周 | 中 |
| 读卡器硬件集成测试 | 1-2 周 | 中 |
| PRD 文档重组 (modules/ + cross-cutting/) | 2-3 天 | 中 |

#### 类型 B: v2.0 新功能（需你定义）

目前 PRD 中没有 v2.0 的明确需求。可能的方向：

| 方向 | 说明 | 需要你提供 |
|------|------|-----------|
| 移动端 | 微信小程序/APP 预约 | 产品需求 |
| 多诊所 | 连锁诊所管理 | 产品需求 |
| 医保对接 | 医保结算接口 | 产品需求 + 接口文档 |
| 数据分析 | 就诊统计/药材消耗分析 | 产品需求 |
| 在线问诊 | 远程视频问诊 | 产品需求 |
| 药品库存 | 药材库存管理/预警 | 产品需求 |

**需要你回答**:

1. v2.0 你最想做什么功能？（请列出 3-5 个）
2. 这些功能的优先级如何排序？
3. 是否有外部依赖（如医保接口文档）？

---

## 三、建议的讨论流程

```
Step 1: 确认 Q1 (Should/Could 实现状态)  →  明确 v1.0 是否真正完成
Step 2: 确认 Q2 (LocalWebAPI 定位)        →  决定代码清理方向
Step 3: 确认 Q3 (Sync 冲突 UI)            →  决定是否投入 2-4 周
Step 4: 确认 Q4 (读卡器硬件)              →  决定测试策略
Step 5: 讨论 Q5 (v2.0 新功能)             →  制定 v2.0 PRD
Step 6: 根据以上结论，更新 roadmap.md      →  形成可执行的 Sprint 计划
```

---

## 四、文档更新计划（讨论后执行）

根据你的回答，我将执行以下更新：

| 文档 | 更新内容 |
|------|----------|
| `roadmap.md` | 更新测试数据 (1654→2021)，添加 v2.0 Sprint 规划 |
| `dual-mode.md` | 更新架构描述，明确 LocalWebAPI 是 Local 模式的实现方式 |
| `decisions/0009-localwebapi-embedded.md` | 新增 ADR，记录 LocalWebAPI 架构决策 |
| `prd.md` | 更新为 v2.0，反映当前实现状态 |
| `docs/02-requirements/` | 按 modules/ + cross-cutting/ 重组（如果需要） |

---

**请逐一回答 Q1-Q5，我将根据你的答复更新文档和开发方向。**
