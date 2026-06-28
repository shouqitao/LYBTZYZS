# 顶层 PRD (Product Requirements Document)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 执行摘要

凌隐宝堂中医诊所管理系统（LYBTZYZS）是面向小型中医诊所（1-3 名医生）的桌面诊疗管理系统，采用 **.NET 8 + WPF/Prism + ASP.NET Core + EF Core + SQL Server** 技术栈。系统解决中医医生在**复诊信息调阅**（纸质翻找 5-10 min → 拼音码搜索 10s）、**处方开具**（手写 10-15 min → 验方一键导入 1-2 min）、**经验方传承**（纸质笔记 → 数字化验方库 + 团队共享）三大核心痛点，覆盖从患者登记到处方打印的完整中医诊疗流程。

系统采用**双模式架构**（远程 SQL Server + 本地 SQL Server LocalDB），共享统一 Service/Repository 层，确保医生在任何网络条件下都能完成诊疗工作。**v1.0 远程库与本地库数据孤立不互通**（N1 决策），双向同步属 v2.0 规划。`MedicalCase`（医案）作为 DDD 唯一聚合根，聚合 `Consultation`（中医诊断）与 `Prescription`（处方），保证诊疗数据的原子性写入与事务一致性。

v1.0 包含 **9 个功能模块、136 个 User Stories**（Must / Should / Could 三级优先级），目标用户涵盖医生（Doctor）、管理员（Admin）、前台接待（Receptionist）与超级管理员（SuperAdmin）四类角色。详细角色画像与业务背景见 [`../01-product/02-personas.md`](../01-product/02-personas.md)，产品愿景与核心价值见 [`../01-product/01-vision.md`](../01-product/01-vision.md)。

## 问题陈述

小型中医诊所的日常诊疗长期依赖纸质流程，核心痛点集中在三个维度：

**信息碎片化**：患者档案、诊疗记录、处方信息分散在纸质病历、Excel 表格与医生个人笔记中。复诊时翻找纸质病历耗时 5-10 分钟，且随着病历累积（年增 5000-10000 份）检索效率持续下降。药材价格更新需逐条通知医生，导致处方用药价格不准确。

**经验方流失**：中医医生在长期诊疗中积累的经验方（验方）缺乏系统化手段。方剂的药材组成、剂量配比依赖个人记忆与纸质笔记，难以在团队间共享和传承；人员离职即造成知识断层。

**离线无保障**：医生外出诊疗（义诊、上门）或诊所网络故障时，无法访问患者历史数据与药材信息，诊疗连续性受到严重影响。手写处方不仅效率低（10-15 min/张），且费用计算易出错（错误率约 5%），引发患者投诉。

## 量化痛点

| 痛点 | 当前（纸质） | v1.0 目标 | 改善幅度 |
|------|------------|----------|---------|
| 复诊病历调阅 | 5-10 min/次，日均 6-12 次 | < 10 sec（拼音码搜索） | 30-60× |
| 处方开具（复诊） | 10-15 min（手写） | < 3 min（验方导入 + 微调） | 3-5× |
| 处方费用计算 | 2-3 min（手算），错误率 ~5% | 即时自动计算，零错误 | 100% 准确 |
| 处方打印出单 | 3-5 min（手写） | < 30 sec（A5 模板） | 6-10× |
| 完整复诊流程 | 25-40 min | < 10 min | 2.5-4× |
| 药材库初始化 | 数天（逐条录入） | < 1 小时（Excel 批量导入） | 数十倍 |
| 患者登记（身份证读卡） | 1-2 min（手写 18 位） | < 30 sec（华大 HD100 读卡） | 2-4× |
| 经验方可复用率 | 0%（仅个人笔记） | 100%（系统化管理 + 共享） | — |

**护栏指标**：数据完整性零丢失（聚合保存事务保证）；离线模式核心流程 100% 可用；100% 写操作有审计记录；0 未授权访问。

## 目标用户

系统服务于四类角色，权限层级递进（`PermissionLevel`：Receptionist=0、Doctor=1、Admin=10、SuperAdmin=100）。详细画像、日常工作流、痛点与成功标准见 [`../01-product/02-personas.md`](../01-product/02-personas.md)。

| 角色 | 使用频率 | 核心场景 |
|------|---------|---------|
| 前台接待 (Receptionist) | 每日 4-6h | 患者登记、身份证读卡、挂号分诊 |
| 医生 (Doctor) | 每日 6-8h | 诊疗、开方、验方积累、处方打印 |
| 管理员 (Admin) | 每日 1-2h | 药材库管理、用户管理、数据维护 |
| 超级管理员 (SuperAdmin) | 极低 | 系统初始化、诊断工具、配置管理 |

## 成功指标

### 主要指标（复诊效率）

复诊患者从到达诊室到处方打印的完整流程时间：**25-40 min → < 10 min（2.5-4× 提升）**。

### 次要指标

| 指标 | 目标 | 衡量方式 |
|------|------|---------|
| 处方费用计算准确率 | 100% | 零投诉 |
| 经验方数字化率 | 100% | 验方库条目数 |
| 患者档案电子化率 | 100% | 患者表记录数 |
| 药材库初始化时间 | < 1 小时 | 导入接口统计 |

### 技术指标（P95）

| 指标 | 目标 | 条件 |
|------|------|------|
| API 简单查询 | < 500ms | 标准 data volume（患者 5000 + 医案 25000） |
| API 列表查询 | < 1s | 分页 20 条/页 |
| API 聚合保存 | < 2s | MedicalCase + Consultation + Prescription |
| Desktop 启动 | < 5s | 双击到登录页 |
| Desktop 页面切换 | < 1s | 模块导航 |

详细非功能需求见 [`12-nfr.md`](12-nfr.md)。

## 范围

### v1.0 范围（9 模块、136 US）

| # | 模块 | US 数 | 核心能力 |
|---|------|-------|---------|
| 1 | 认证与会话 (Auth) | 13 | JWT 登录/登出、Token 旋转、重放攻击检测、双模式认证 |
| 2 | 用户管理 (Users) | 12 | CRUD、四级权限体系、密码策略、账户锁定、批量操作 |
| 3 | 患者管理 (Patients) | 13 | 档案 CRUD、拼音码搜索、身份证读卡、导入导出、敏感数据脱敏 |
| 4 | 药材管理 (Herbs) | 13 | 药材库 CRUD、分类管理、拼音检索、批量导入、引用检查 |
| 5 | 验方管理 (Formulas) | 13 | 经验方 CRUD、Draft↔Validated 状态机、药材绑定验证、共享机制 |
| 6 | 医案管理 (MedicalCases) | 18 | 聚合根。诊断 + 处方 + 打印完整生命周期，CQRS 模式 |
| 7 | 挂号管理 (Registration) | 7 | 前台排队 + 医生快速就诊，医案联动回写 |
| 8 | 处方打印 (Printing) | 4 | A5/A4 模板、PDF 导出、打印回写服务器 |
| 9 | 平台基础设施 (Platform) | 43 | Shell + Config + Error + Logging + Health + CardReader（含 SHELL-010~019 中 v1.0 的 8 项） |
| **合计** | | **136** | |

### 范围外（系统边界之外，线下流程）

系统边界止于**打印处方笺**。以下为线下流程，**不在系统范围内**（与 v2.0 延期不同——这些功能永不纳入本系统）：

| 流程 | 说明 |
|------|------|
| 收费 / 付费 | 患者凭打印的处方笺线下缴费，系统不处理资金往来 |
| 发药 | 由诊所药房线下完成（系统不联动药房） |
| 库存管理 | 药材进销存为独立业务域，本系统仅做记录式管理（无库存数量） |

> 医生完成诊疗→打印处方笺（含详情+价格）→患者持单线下付费→抓药。打印为系统侧终点。

### v2.0 延期范围

以下功能明确不在 v1.0 范围内：

| 功能 | 排除原因 |
|------|---------|
| 西医诊断与处方 | 产品定位为中医诊所专用 |
| 医保对接与费用结算 | 涉及第三方接口，复杂度高 |
| 药房发药管理 | 超出诊疗流程范围 |
| 库存进销存 | 独立业务域 |
| 排班与预约管理 | 小型诊所需求不强烈 |
| EMR 标准对接 | 标准对接成本高 |
| 移动端（iOS/Android） | 仅支持 Windows 桌面端 |
| MedicalCase 数据同步 | v1.0 远程/本地数据孤立（N1 决策），同步整模块延至 v2.0 |
| LocalDB 字段级加密 | 基于 LocalDB 重新设计（AES-256 + DPAPI），D10 拉回 v1.0 后部分实施 |
| 自动同步提示 | NetworkStatusService 待规划 |

## 权限矩阵

本节是系统授权的**唯一事实来源**（Single Source of Truth），合并自原独立的权限矩阵文档。各模块 PRD 的权限描述以此为准。

### 角色层级

| 角色 | PermissionLevel | 定位 | 数量 |
|------|----------------|------|------|
| SuperAdmin | 100 | 系统固定账号，数据库种子预置 | 1（固定） |
| Admin | 10 | 诊所管理员 | 1-2 |
| Doctor | 1 | 医生，核心诊疗操作者 | 1-5 |
| Receptionist | 0 | 前台接待 | 1-2 |

**层级规则**：`operator.PermissionLevel > target.PermissionLevel` 才允许操作目标用户（USER-D04）。

**SuperAdmin 特殊规则**（USER-D05）：作为操作者拥有 Admin 全部权限；作为目标**不可被任何人管理**（不可改角色、不可删除、不可禁用、不可重置密码）；Admin 用户列表中不可见；密码恢复仅通过 SeedTool CLI（运维操作）。

### 授权策略

系统定义 4 个授权策略（Authorization Policy），作为 API 端点的"门禁"：

| 策略 | 允许角色 | 典型端点 |
|------|---------|---------|
| `DoctorOrReceptionist` | Receptionist + Doctor + Admin + SuperAdmin | `/patients`、`/registrations`、`/herbs`、`/formulas`、`/medicalcases` |
| `AdminOrSuperAdmin` | Admin + SuperAdmin | `/users`（CRUD）、`/configuration`、`/diagnostics`、`/patients/{id}/status`、`/users/{id}/reset-password`、`/users/{id}/restore` |

另有 `AllowAnonymous`（登录、登出、健康探针、导入模板下载）与隐式 `Authenticated`（任意已认证用户：当前用户资料、修改密码）。

**检查顺序**（短路返回）：① 认证（401）→ ② 角色策略（403）→ ③ 资源归属（模块错误码 ERR-xxxxx）→ ④ 业务规则（422）。

### 模块权限矩阵

下表汇总每个模块的核心操作权限。`✓` = 允许；`✓*` = 允许但有限制（见注释）；`✗` = 拒绝。

| 模块 | 操作 | Receptionist | Doctor | Admin | SuperAdmin |
|------|------|:---:|:---:|:---:|:---:|
| **Auth** | 登录/登出/刷新 | ✓ | ✓ | ✓ | ✓ |
| **Users** | 查询/创建/编辑/删除 | ✗ | ✗ | ✓¹ | ✓¹ |
| **Users** | 重置密码/恢复 | ✗ | ✗ | ✗ | ✓ |
| **Users** | 自助（资料/密码） | ✓ | ✓ | ✓ | ✓ |
| **Patients** | 查询/创建/编辑 | ✓ | ✓ | ✓ | ✓ |
| **Patients** | 删除 | ✗ | ✓ | ✓ | ✓ |
| **Patients** | 启用/禁用 | ✗ | ✗ | ✓ | ✓ |
| **Herbs** | 查询 | ✗ | ✓ | ✓ | ✓ |
| **Herbs** | 创建/编辑/删除/启禁 | ✗ | ✓*² | ✓ | ✓ |
| **Formulas** | 查询 | ✗ | ✓*³ | ✓ | ✓ |
| **Formulas** | 创建/编辑/删除/启禁 | ✗ | ✓*² | ✓ | ✓ |
| **MedicalCases** | 创建 | ✗ | ✓ | ✗ | ✗ |
| **MedicalCases** | 查询 | ✗ | ✓*⁴ | ✓ | ✓ |
| **MedicalCases** | 编辑（Active） | ✗ | ✓*⁴ | ✓ | ✓ |
| **MedicalCases** | 编辑（Completed 当天） | ✗ | ✓*⁴⁵ | ✓⁵ | ✓⁵ |
| **MedicalCases** | 编辑（Completed 隔天+） | ✗ | ✗⁶ | ✓⁵ | ✓⁵ |
| **MedicalCases** | 完成/挂起/取消 | ✗ | ✓*⁴ | ✓ | ✓ |
| **Registration** | 前台创建（Waiting） | ✓ | ✗ | ✗ | ✗ |
| **Registration** | 医生快速就诊（QuickVisit） | ✗ | ✓ | ✗ | ✗ |
| **Registration** | 查询队列/历史 | ✓ | ✓*⁴ | ✓ | ✓ |
| **Registration** | 取消（仅 Waiting） | ✓*⁷ | ✗ | ✗ | ✗ |
| **Printing** | 打印/预览/导出 | ✗ | ✓ | ✗ | ✗ |
| **Platform/Config** | 查询/验证配置 | ✗ | ✗ | ✗ | ✓ |
| **Platform/Health** | 存活探针（/health） | ✓⁸ | ✓⁸ | ✓⁸ | ✓⁸ |
| **Platform/Health** | 详细检查（/details） | ✓ | ✓ | ✓ | ✓ |
| **Platform/Diagnostics** | 日志级别控制 | ✗ | ✗ | ✗ | ✓ |
| **Platform/CardReader** | 读卡 + 查找或创建患者 | ✓ | ✓ | ✓ | ✓ |

**注释**：

1. **Users Admin 权限**：受 USER-D04 层级规则约束（仅可操作权限值低于自己的用户）；Admin 列表过滤 sysadmin 与其他 Admin。
2. **Doctor 写操作归属限制**：Herbs 与 Formulas 的 Doctor 仅可操作**自己创建的**记录（`CreatedBy` / `UserId` 字段）；Restore 操作为 Admin-only。
3. **Formula Doctor 可见性**：Doctor 仅可见自己创建的 + `IsShared=true` 的共享验方；Admin 不受 `IsShared` 限制可见全部。
4. **Doctor 医案/挂号归属限制**：Doctor 仅可见/操作 `UserId=自己` 的医案与挂号；Receptionist 自动过滤禁用患者。
5. **Completed 医案编辑**：Admin/SuperAdmin 任何时间均可编辑已完成医案，但需提供 `EditReason` + UI 确认弹窗（MC-LOCK-03）。
6. **MC-LOCK 锁定规则**：`IsLocked = (Status == Completed) AND (CompletedAt.Date < Today)`，仅限制 Doctor；Admin/SuperAdmin 不受影响。
7. **Registration 取消**：Receptionist 仅可取消 `Source=Receptionist` 且 `Status=Waiting` 的挂号（REG-BR-001）。
8. **Health 匿名访问**：`/health` 与 `/ping` 为 `AllowAnonymous`，未认证也可访问；`/details` 需认证。

### 数据归属与共享

- **归属字段**：Herb 使用 `BaseEntity.CreatedBy`；Formula 与 MedicalCase 使用业务字段 `UserId`；Patient 无归属限制。
- **共享机制**：仅 Formula 支持 `IsShared` 标记（Doctor 可共享给团队，他人只读）；Herb/MedicalCase/Patient 无共享标记。
- **软删除可见性**：软删除数据默认被全局查询过滤器隐藏；Restore 操作仅 Admin/SuperAdmin 可执行（Doctor 无权）。
- **统一删除策略**（BR-DEL-001）：Patient 被 MedicalCase 引用、Herb 被 PrescriptionItem/FormulaItem 引用时禁止删除（返回 422，建议禁用）。

### 跨模块联动

- **医案完成** → Registration 自动 Completed（US-REG-005）
- **医案取消** → Registration 按 Source 联动：Receptionist 回退 Waiting（保留 MedicalCaseId 可恢复）；Doctor 自动 Cancelled（US-REG-006）
- **患者禁用** → 禁止创建新医案/挂号；历史医案可查阅
- **用户角色变更/禁用/删除** → 撤销所有 Token Family，强制重登录（AUTH-D07）
- **医生禁用** → 若有名下 Waiting 挂号则阻止禁用（REG-BR-006，先清后禁）

## 依赖与风险

### 技术依赖

| 依赖 | 说明 | 风险 |
|------|------|------|
| .NET 8 LTS | 运行时（2026-11 EOL） | 低（LTS 周期内） |
| SQL Server | 远程模式数据库 | 低（成熟稳定） |
| SQL Server LocalDB | 本地模式数据库 | 低 |
| Prism 9.0 | WPF MVVM 框架 | 低 |
| HandyControl | WPF UI 控件库 | 低 |
| QuestPDF | 处方 PDF 导出（Community license） | 低 |
| 华大 HD100 读卡器 | 身份证读卡硬件 | 中（单一供应商） |

### 主要风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 华大 HD100 读卡器停产/驱动不兼容 | 身份证读取不可用 | `ICardReaderService` 接口抽象化，支持替换硬件驱动 |
| 单人开发连续性 | 知识集中，项目延续性风险 | 完整文档体系 + 测试覆盖率 80%+ + ADR 决策记录 |
| 双模式数据一致性 | 本地与远程数据漂移 | v1.0 数据孤立（N1 决策）；v2.0 规划 Checksum 差异检测 + 同步工作流 |
| MedicalCase 聚合并发 | 多用户同时编辑同一医案 | 乐观并发令牌 + DbUpdateConcurrencyException 重试（最多 3 次） |

## 模块总览

详细 User Stories 见各模块文档：

| 模块 | 文档 | US 数 | 核心聚合根/特性 |
|------|------|:-----:|----------------|
| 认证与会话 | [`02-auth.md`](02-auth.md) | 13 | JWT + Token Family 旋转 + 重放攻击检测 |
| 用户管理 | [`03-users.md`](03-users.md) | 12 | 四级权限体系 + IDOR 防护 |
| 患者管理 | [`04-patients.md`](04-patients.md) | 13 | 拼音码 + 敏感数据脱敏 + 身份证读卡 |
| 药材管理 | [`05-herbs.md`](05-herbs.md) | 13 | Record-Only + 拼音检索 + 引用检查 |
| 验方管理 | [`06-formulas.md`](06-formulas.md) | 13 | Draft↔Validated 状态机 + 共享机制 |
| 医案管理 | [`07-medical-cases.md`](07-medical-cases.md) | 18 | **聚合根** + CQRS + BR-001 单活动医案 |
| 挂号管理 | [`08-registration.md`](08-registration.md) | 7 | 双 Source 模型 + 原子事务 + 医案联动 |
| 处方打印 | [`09-printing.md`](09-printing.md) | 4 | A5/A4 模板 + PDF 导出 + 打印回写 |
| 平台基础设施 | [`11-platform.md`](11-platform.md) | 43 | Shell + Config + Error + Logging + Health + CardReader |
| 非功能需求 | [`12-nfr.md`](12-nfr.md) | — | 性能/数据/可用性/安全/可维护性/兼容性 |
| **合计** | | **136** | |

### 相关文档

| 文档 | 路径 |
|------|------|
| 产品愿景 | [`../01-product/01-vision.md`](../01-product/01-vision.md) |
| 角色画像 | [`../01-product/02-personas.md`](../01-product/02-personas.md) |
| 业务术语表 | [`../01-product/03-glossary.md`](../01-product/03-glossary.md) |
| 系统架构 | [`../03-architecture/`](../03-architecture/) |
| API 参考 | [`../04-api-reference/`](../04-api-reference/) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-15 | v2.0 | 重建：合并原独立权限矩阵文档（448 行）为本文件 §权限矩阵；模块从 15 精简为 10；US 总数 138→136；统一采用 `US-` 编号；WHO/WHY 上下文迁移至 `../01-product/` |
| 2026-06-25 | v2.1 | 修正 US 总数 136→128（实际计数）；Platform 模块 US 数 37→35 |
| 2026-06-28 | v2.2 | 文档对齐：US 总数统一 136；Sync 模块移出 v1.0（9 模块）；Platform 35→43（含 SHELL-010~019 中 v1.0 的 8 项）；AccessToken 统一 60 分钟 |
