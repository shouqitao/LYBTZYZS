# 凌隐宝堂中医诊所管理系统 -- 产品需求文档 (PRD)

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **产品阶段**: v1.0 核心诊疗流程

---

## 1. Executive Summary

凌隐宝堂中医诊所管理系统解决中医医生在 **复诊患者信息调阅** (纸质翻找 5-10 min -> 拼音码搜索 10s)、**处方开具** (手写 10-15 min -> 验方一键导入 1-2 min)、**经验方传承** (纸质笔记/口头 -> 数字化验方库 + 团队共享) 三大核心痛点。系统覆盖从患者登记到处方打印的完整中医诊疗流程，并支持远程/本地双模式运行，让医生在任何网络条件下都能完成诊疗工作。

v1.0 包含 15 个功能模块、138 个 User Stories (Must 51 + Should 54 + Could 33)，目标用户为小型中医诊所 (1-3 名医生) 的医生、管理员和前台接待。

---

## 2. Problem Statement

### 2.1 问题描述

小型中医诊所 (1-3 名医生) 在日常诊疗中面临以下核心痛点:

**信息管理碎片化**: 患者档案、诊疗记录、处方信息分散在纸质病历、Excel 表格和医生个人笔记中。复诊时难以快速调阅历史诊疗信息，影响诊疗质量和效率。

**经验方难以积累复用**: 中医医生在长期诊疗中积累的经验方缺乏系统化管理手段。方剂的药材组成、剂量配比依赖个人记忆，难以在团队间共享和传承。

**处方开具效率低**: 手写处方需要反复查阅药材价格和剂量，计算费用耗时。复诊患者需要重新书写整张处方，无法复用历史处方。

**离线诊疗无保障**: 医生外出诊疗或诊所网络故障时，无法访问患者历史数据和药材信息，影响诊疗连续性。

### 2.2 目标用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 中医医生 | 复诊时翻找纸质病历耗时 5-10 分钟 | 每日诊疗效率降低 30%+ |
| 中医医生 | 手写处方计算费用易出错 | 患者投诉、诊所信誉受损 |
| 中医医生 | 经验方散落在个人笔记中 | 知识无法传承，人员离职即丢失 |
| 诊所管理员 | 药材价格更新需逐条通知医生 | 处方用药价格不准确 |
| 前台接待 | 患者登记依赖手写表格 | 信息录入重复、字迹辨认困难 |

### 2.3 量化证据汇总

| 来源模块 | 证据 | 量化数据 |
|----------|------|---------|
| 医案管理 | 复诊翻阅纸质病历 | 每次 5-10 分钟，日均 6-12 次 (复诊占比 ~40%) |
| 医案管理 | 处方手动计算错误率 | ~5% (多味药 x 帖数 x 折扣) |
| 医案管理 | 手写处方字迹辨认 | 药房配药确认电话日均 3-5 次 |
| 患者管理 | 纸质病历查找时间 | 每次 1-3 分钟，年积累 5000-10000 份后检索更慢 |
| 药材管理 | 初始化药材库 (300-500 种) | 手工逐条录入需数天 |
| 验方管理 | 经验方依赖个人记忆/笔记 | 人员离职即丢失，团队无法复用 |
| 前台登记 | 手动输入身份证号 18 位 | 出错率 ~5%，单次登记 1-2 分钟 |

---

## 3. Target Users & Personas

### 3.1 角色定义

详细权限矩阵见 [user-roles.md](../01-product/04-user-roles.md)。

### 3.2 核心角色画像

详细 Proto-Persona (含日常时间线、痛点、成功标准) 见 [personas.md](../01-product/02-personas.md)。

| 角色 | 代表人物 | 使用频率 | 核心需求 |
|------|---------|---------|---------|
| 中医医生 (Doctor) | 李医生 | 每日 6-8h | 快速调阅患者历史、高效开方、积累验方 |
| 诊所管理员 (Admin) | 王主任 | 每日 1-2h | 药材库管理、数据审核、用户管理 |
| 前台接待 (Receptionist) | 小张 | 每日 4-6h | 快速登记、身份证读卡、挂号分诊 |
| 超级管理员 (SuperAdmin) | (系统角色) | 极低 | 系统初始化、诊断工具 |

### 3.3 Jobs-to-Be-Done

核心用户任务分析见 [jtbd.md](../01-product/03-jtbd.md)。3 个角色共 10 个 JTBD，覆盖诊疗、管理、前台三大场景。

**Top 3 JTBD:**
1. **JTBD-D01**: 复诊时 30 秒内调出患者全部历史医案 (对应 US-PAT-002, US-MC-002)
2. **JTBD-D02**: 验方一键导入处方，开方时间从 5-10 分钟缩短到 1-2 分钟 (对应 US-MC-016)
3. **JTBD-D03**: 复制历史处方微调，避免重复录入 (对应 US-MC-018)

---

## 4. Strategic Context

### 4.1 业务目标

| 目标 | 描述 | 对应模块 |
|------|------|---------|
| 患者档案电子化 | 完整患者信息库，支持拼音码快速检索和历史回溯 | Patients, CardReader |
| 诊疗流程标准化 | 覆盖望闻问切、辨证论治、开具处方的完整中医诊疗流程 | MedicalCase |
| 处方与验方规范化 | 处方管理 + 经验方积累，支持验方复用和药材配伍管理 | Formulas, MedicalCase |
| 药材库统一管理 | 中药材分类、价格、启用状态统一维护 | Herbs |
| 支持离线诊疗 | 本地模式支持无网络环境完整诊疗，事后同步 | Sync, 双模式架构 |

详细业务目标见 [vision.md](../01-product/01-vision.md)。

### 4.2 Why Now

1. **纸质流程效率瓶颈**: 随着患者数量增长，纸质病历检索成本线性增长，电子化势在必行
2. **中医经验传承**: 老中医退休后经验方面临失传风险，数字化积累刻不容缓
3. **监管趋势**: 卫生部门对中医诊所信息化管理要求日趋严格
4. **技术成熟**: .NET 8 + WPF + SQL Server 技术栈成熟稳定，适合桌面诊疗场景

---

## 5. Solution Overview

### 5.1 系统架构

- **技术栈**: .NET 8 + WPF/Prism (桌面端) + ASP.NET Core (服务端) + EF Core + SQL Server (远程 + LocalDB)
- **架构模式**: 三层架构 (Controller -> Service -> Repository) + MVVM (View -> ViewModel -> Repository)
- **核心设计**: MedicalCase 作为唯一聚合根 (DDD)，包含 Consultation (1:1) + Prescription (1:0..1)
- **双模式**: 远程 (SQL Server) + 本地 (SQL Server LocalDB)，共享 Service/Repository 层

### 5.2 核心业务流程

```
患者登记 -> 创建医案 -> 中医诊断 (望闻问切/辨证) -> 处方决策 -> 开具处方 -> 聚合保存 -> 打印 -> 完成
```

完整端到端流程 (含异常路径、分支流程、并发处理) 见 [clinical-workflow.md](../01-product/06-clinical-workflow.md)。

### 5.3 模块概览

| 模块 | 核心能力 |
|------|---------|
| 认证 (Auth) | JWT 登录/登出、自动登录、Token 滑动刷新、重放攻击检测 |
| 用户管理 (Users) | CRUD、角色分配 (四级权限体系)、密码策略、账户锁定 |
| 患者管理 (Patients) | 档案 CRUD、拼音码搜索、身份证读卡、导入导出、状态管理 |
| 药材管理 (Herbs) | 药材库 CRUD、分类管理、价格维护、批量导入、启用/禁用 |
| 验方管理 (Formulas) | 经验方 CRUD、药材组成编辑、共享管理、验证状态流转 |
| 医案管理 (MedicalCase) | 聚合根。诊断+处方+打印的完整生命周期管理 |
| 数据同步 (Sync) | 本地与远程双向同步 (药材/患者/验方)，SHA256 差异检测 |
| 打印 (Printing) | MedicalCase 聚合根能力。A5/A4 处方打印、版本管理、打印日志 |
| 身份证读卡器 (CardReader) | 华大 HD100 读卡器集成、患者信息自动填充 |
| 系统健康 (Health) | 数据库/磁盘/内存健康检查、运行时诊断 |
| 异常处理 (Error) | 全局异常兜底、结构化错误码 (MCCEE 体系) |
| 日志审计 (Logging) | 结构化日志 (Serilog)、安全审计、敏感数据脱敏 |
| Desktop Shell | WPF 壳程序、Prism 模块加载、导航、主题、状态栏 |
| 配置参数 (Config) | 应用配置、分页默认值、会话超时、缓存策略 |

---

## 6. Success Metrics

### 6.1 Primary Metric (主要优化指标)

**复诊效率**: 复诊患者从到达诊室到处方打印的完整流程时间

| 环节 | 当前 (纸质) | v1.0 目标 | 改善幅度 |
|------|-----------|----------|---------|
| 患者信息调阅 | 5-10 min | < 10 sec | 30-60x |
| 诊断记录 | 3-5 min (手写) | 2-3 min (电子化) | 1.5-2x |
| 处方开具 (复诊) | 10-15 min (手写) | < 3 min (复制+调整) | 3-5x |
| 费用计算 | 2-3 min (手算) | 即时 (自动) | - |
| 打印出单 | 3-5 min (手写处方) | < 30 sec | 6-10x |
| **完整流程** | **25-40 min** | **< 10 min** | **2.5-4x** |

### 6.2 Secondary Metrics

| 指标 | 当前 (纸质流程) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 处方费用计算准确率 | ~90% (手工计算) | 100% (系统自动计算) | 零投诉 |
| 经验方可复用率 | 0% (仅在个人笔记) | 100% (系统化管理) | 验方库条目数 |
| 患者登记时间 (身份证读卡) | 1-2 min (手写) | < 30 sec (刷卡) | 操作日志 |
| 药材库初始化时间 | 数天 (逐条录入) | < 1 小时 (批量导入) | 导入接口统计 |

### 6.3 Guardrail Metrics (护栏指标)

| 指标 | 底线 | 说明 |
|------|------|------|
| 数据完整性 | 0 数据丢失 | 聚合保存事务性保证 (MedicalCase 原子写入) |
| 系统可用性 | 离线模式核心流程 100% 可用 | 本地模式不依赖网络 |
| 操作可追溯性 | 100% 写操作有审计记录 | 字段级变更 diff + EditReason |
| 数据安全 | 0 未授权访问 | 角色权限 + Token 认证 + 敏感数据加密 |

### 6.4 技术指标

详细技术指标见 [nfr.md](nfr.md)。核心摘要:

| 指标 | 目标 (P95) | 条件 |
|------|-----------|------|
| API 简单查询 | < 500ms | 标准数据量 (患者 5000 + 医案 25000) |
| API 列表查询 | < 1s | 分页 20 条/页 |
| API 聚合保存 | < 2s | MedicalCase + Consultation + Prescription |
| Desktop 启动 | < 5s | 双击到登录页 |
| Desktop 页面切换 | < 1s | 模块导航 |

### 6.5 质量指标

| 指标 | 目标 |
|------|------|
| 测试覆盖 | Server 1017 + Integration 27 + Desktop 307 + Arch 76 = 1427 tests |
| 测试策略 | Testing Trophy: 零 mock (Server)，真实 Repository (Desktop) |
| 代码-PRD 对齐率 | 100% (持续审计) |

---

## 7. Requirements Index

### 7.1 User Stories (138 US, 15 模块)

| 模块 | 文件 | US 编号范围 | 总数 | Must | Should | Could |
|------|------|------------|------|------|--------|-------|
| 认证与会话管理 | [auth.md](auth.md) | US-AUTH-001 ~ 013 | 13 | 8 | 5 | 0 |
| 用户管理 | [users.md](users.md) | US-USER-001 ~ 012 | 12 | 5 | 5 | 2 |
| 患者管理 | [patients.md](patients.md) | US-PAT-001 ~ 013 | 13 | 4 | 2 | 7 |
| 药材管理 | [herbs.md](herbs.md) | US-HERB-001 ~ 013 | 13 | 5 | 4 | 4 |
| 验方管理 | [formulas.md](formulas.md) | US-FORM-001 ~ 013 | 13 | 6 | 4 | 3 |
| 医案管理 | [medical-cases.md](medical-cases.md) | US-MC-001 ~ 018 | 18 | 9 | 8 | 1 |
| 数据同步 | [sync.md](sync.md) | US-SYNC-001 ~ 008 | 8 | 1 | 7 | 0 |
| 打印 | [printing.md](printing.md) | US-PRINT-001 ~ 004 | 4 | 0 | 3 | 1 |
| 身份证读卡器 | [card-reader.md](card-reader.md) | US-CARD-001 ~ 002 | 2 | 0 | 2 | 0 |
| 系统健康与诊断 | [health-diagnostics.md](health-diagnostics.md) | US-SYS-001 ~ 009 | 9 | 0 | 0 | 9 |
| 异常处理策略 | [error-handling.md](error-handling.md) | US-ERR-001 ~ 008 | 8 | 0 | 6 | 2 |
| 日志与审计 | [logging.md](logging.md) | US-LOG-001 ~ 007 | 7 | 0 | 4 | 3 |
| Desktop Shell | [desktop-shell.md](desktop-shell.md) | US-SHELL-001 ~ 007 | 7 | 5 | 1 | 1 |
| 配置参数 | [configuration.md](configuration.md) | US-CFG-001 ~ 004 | 4 | 2 | 2 | 0 |
| 挂号管理 | [registration.md](registration.md) | US-REG-001 ~ 007 | 7 | 6 | 1 | 0 |
| **合计** | | | **138** | **51** | **54** | **33** |

### 7.2 非功能性需求

- [nfr.md](nfr.md) -- 性能/数据量/可用性/安全

### 7.3 UI/UX 规范

- [ui-patterns.md](ui-patterns.md) -- 列表/表单/对话框/状态反馈/快捷键/无障碍

### 7.4 术语规范

- [glossary.md](../01-product/07-glossary.md) -- 中英文术语对照

### 7.5 用户故事地图

- [user-story-map.md](user-story-map.md) -- 4 个核心 Narrative 的 Jeff Patton 故事地图 (含 Release Slices + Gap 分析)

### 7.6 发布路线图

- [roadmap.md](roadmap.md) -- v1.0 Sprint 分配 + Release 验收标准 (基于 Code-PRD 审计)

---

## 8. Out of Scope

### 8.1 v1.0 明确不包含

| 功能 | 排除原因 |
|------|---------|
| 西医诊断和处方 | 产品定位为中医诊所专用 |
| 医保对接和费用结算 | 涉及第三方接口对接，复杂度高，v2.0+ 考虑 |
| 药房发药管理 | 超出诊疗流程范围 |
| 库存进销存 | 独立业务域，v2.0+ 考虑 |
| 排班和预约管理 | 小型诊所需求不强烈 |
| 电子病历 (EMR) 标准对接 | 标准对接成本高，v2.0+ 考虑 |
| 移动端 (iOS/Android) | 仅支持 Windows 桌面端 |
| MedicalCase 数据同步 | 聚合根多表级联同步复杂度极高，独立 Epic 规划 |
| ~~PDF 处方导出~~ | ~~v2.0 规划~~ **已完成** (Sprint 6, QuestPDF 2025.4.0) |
| 自动同步提示 | v2.0 规划 (NetworkStatusService) |
| ~~诊所信息配置化~~ | ~~v2.0 规划~~ **已完成** (Sprint 6, clinic-settings.json + reloadOnChange 热更新) |
| LocalDB 字段级加密 | v2.0 规划 (AES-256 + DPAPI，基于 LocalDB 重新设计) |

### 8.2 v2.0 路线图

详见 [vision.md](../01-product/01-vision.md) "版本路线图" 章节。

---

## 9. Dependencies & Risks

### 9.1 技术依赖

| 依赖 | 说明 | 风险等级 |
|------|------|---------|
| .NET 8 LTS | 运行时，2026-11 EOL | 低 (LTS 周期内) |
| SQL Server | 远程模式数据库 | 低 (成熟稳定) |
| SQL Server LocalDB | 本地模式数据库 (SYNC-D02 已完成迁移) | 低 |
| Prism 9.0 | WPF MVVM 框架 | 低 |
| HandyControl | WPF UI 控件库 | 低 |
| 华大 HD100 读卡器 | 身份证读卡硬件 | 中 (单一硬件供应商) |

### 9.2 架构风险

| 风险 | 影响 | 缓解措施 | 状态 |
|------|------|---------|------|
| SYNC-D02 双模式重构 | 废弃 IDataSource 层，统一 Service/Repository | Factory + Dual Repository 模式; 6 个 Repository 接口迁移到 Contracts | **已完成** (Sprint 6) |
| 本地模式 SQLite -> LocalDB 迁移 | 消除 SQL 方言差异 | 随 SYNC-D02 统一迁移到 SQL Server LocalDB | **已完成** (Sprint 6) |
| Code-PRD 偏差 | 28 个 OPEN 项 (2026-02-28 审计) | 持续审计 + Sprint 内修复 | **已关闭** (Sprint 5 审计项清零) |
| 华大 HD100 读卡器停产/驱动不兼容 | 身份证读取功能不可用 | CardReader 模块接口抽象化，支持替换硬件驱动 | 监控中 |
| 单人开发连续性 | 知识集中在个人，项目延续性风险 | PRD + 架构文档体系完善；代码测试覆盖率 80%+；关键决策记录在 docs/ | 持续缓解 |

### 9.3 已知技术债务

来源: [2026-02-28 全量审计报告](../plans/archive/2026-02-28-code-vs-prd-full-audit-report.md)

| 类别 | 数量 | 优先级分布 |
|------|------|-----------|
| TODO-CODE | 42 项 | 4 CRITICAL + 7 HIGH + 16 MEDIUM + 15 LOW |
| TODO-PRD | 17 项 | 4 HIGH + 8 MEDIUM + 5 LOW |
| TODO-DEAD-CODE | 14 项 | 10 确认删除 + 4 待确认 |

---

## 10. Open Questions

| ID | 问题 | 状态 | 决策时间 |
|----|------|------|---------|
| OQ-01 | SYNC-D02 实施时机: Sprint 4 还是延后? | CLOSED: Sprint 4 | 2026-02-28 |
| OQ-02 | 本地模式数据库: SQLite vs SQL Server LocalDB | CLOSED: LocalDB | 2026-03-05 |
| OQ-03 | MedicalCase 同步 Epic 是否进入 v1.0? | CLOSED: 延期到 v2.0 | 2026-02-21 |
| OQ-04 | Registration (挂号) 模块是否作为独立实体? | CLOSED: 纳入 v1.0，设计见 clinical-workflow.md | 2026-03-06 |
| OQ-05 | Mock 框架统一 (Moq -> NSubstitute): 迁移时机 | CLOSED: Desktop 测试已完成清理 (48 mock tests removed) | 2026-03-05 |
| OQ-06 | 4 个 CRITICAL 代码问题修复时机 (CODE-01~04) | CLOSED: CODE-01/02 已修复; CODE-03/04 分配到 Sprint 2 | 2026-03-06 |

---

## 相关文档

| 文档 | 路径 | 说明 |
|------|------|------|
| 产品愿景 | [vision.md](../01-product/01-vision.md) | 产品愿景、业务目标、系统边界、版本路线图 |
| 术语表 | [glossary.md](../01-product/07-glossary.md) | 中英文术语对照 |
| 用户角色 | [user-roles.md](../01-product/04-user-roles.md) | 角色定义、权限矩阵 |
| 用户画像 | [personas.md](../01-product/02-personas.md) | Proto-Persona (3 角色日常时间线、痛点、成功标准) |
| JTBD 分析 | [jtbd.md](../01-product/03-jtbd.md) | Jobs-to-Be-Done (3 角色 10 个 JTBD + 覆盖度审查) |
| 临床工作流 | [clinical-workflow.md](../01-product/06-clinical-workflow.md) | 端到端流程 (含异常路径) |
| 用户故事地图 | [user-story-map.md](user-story-map.md) | Jeff Patton 故事地图 (4 Narrative + Release Slices) |
| 数据模型 | [data-model.md](../03-architecture/04-data-model.md) | 实体关系定义 |
| 系统架构 | [system-overview.md](../03-architecture/01-system-overview.md) | 三层架构、模块结构 |
| Code-PRD 审计 | [审计报告](../plans/archive/2026-02-28-code-vs-prd-full-audit-report.md) | 最近一次全量审计 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 顶层 PRD 框架，10 章节结构 |
| 2026-03-06 | v1.1 | Requirements Index: FR->US 编号迁移 (131 FR -> 131 US)，同步模块 PRD 全面重写 |
| 2026-03-06 | v1.2 | PRD 深化: S1 升级为痛点导向; S2 新增量化证据汇总; S3 新增 Personas/JTBD 链接; S6 重构为 Primary/Secondary/Guardrail; S7 新增 MoSCoW 统计 (45/53/33) + 故事地图链接; S10 状态更新 (4 CLOSED); 相关文档新增 personas/jtbd/user-story-map |
| 2026-03-06 | v1.3 | S7 新增 7.6 发布路线图链接 (roadmap.md) |
| 2026-03-06 | v1.4 | S9 补充运营风险 (读卡器/单人开发); S10 关闭 OQ-04 (Registration 纳入 v1.0) + OQ-06 (CRITICAL 对齐 roadmap) |
| 2026-03-06 | v1.5 | 新增 Registration 模块 (7 US: 6 Must + 1 Should); 总量 131->138 US, 14->15 模块 |
| 2026-03-09 | v1.6 | S8 Out of Scope: PDF 导出 + 诊所信息配置化标记已完成 (Sprint 6); SQLite 更新为 LocalDB; S9 Risks: SYNC-D02/LocalDB 迁移/Code-PRD 偏差状态更新为已完成/已关闭 |
