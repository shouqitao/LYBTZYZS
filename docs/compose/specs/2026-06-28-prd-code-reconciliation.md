# PRD-代码对账矩阵（v1.0 已冻结）

> **用途**：把 8 模块审计发现的 28 个「未实现却标 ✅」+ 27 个「部分实现」整理成**产品决策清单**。每个决策簇你拍板后，PRD 据此更新为真实的 v1.0 范围，才能进入设计阶段。
> **来源**：`2026-06-28-business-modules-audit.md` + `2026-06-28-shell-audit-baseline.md`
> **决策方式**：每簇选 **A（补回到 PRD）** / **B（简化 PRD 对齐现状）** / **C（展开讨论）**
> **状态**：✅ **已冻结** — 全部决策完成
> **图例**：🔴完全未实现 / ⚠️部分实现

---

## [S1] 决策簇总览（10 簇）

| 簇 | 主题 | 涉及 US 数 | 性质 | 我的建议 |
|---|------|:---:|------|------|
| D1 | 医疗审计日志体系 | 1🔴 (MC-017) | **医疗合规** | **A 补回** |
| D2 | 打印保护/回写体系 | 2🔴 (PRINT-004+保护字段) | **医疗合规** | **A 补回** |
| D3 | Auth 安全模型 | 4🔴+4⚠️ (Token族/重放/审计/限流) | 安全 | A 补回核心，B 简化次要 |
| D4 | Restore 软删除恢复 | 4🔴 (Users/Patients/Herbs/Formulas) | 数据安全 | **A 补回**（基础设施已就绪） |
| D5 | 引用检查 BR-DEL-001 | 2模块 (Patients单删/Herbs全删) | **数据完整性** | **A 补回**（无争议，必须） |
| D6 | Excel 导入导出 | 2模块 (Herbs服务不存在/Patients端点404) | 效率 | C 讨论（JSON 替代？） |
| D7 | 权限策略错配 | 3模块 (Herbs/Registration/MedicalCases) | **安全 bug** | **A 修到 PRD**（无争议） |
| D8 | P0 数据/安全 Bug | 7 个 | **正确性** | **A 全修**（无争议） |
| D9 | 历史聚合查询 | 2🔴 (MC-008/009 诊断/处方历史) | 功能 | C 讨论 |
| D10 | 字段级加密 | 已定 | 安全 | ✅ 已决定拉回 v1.0 |

**无争议必做（A）**：D5、D7、D8 —— 这些是 bug/数据完整性，不是产品选择。
**医疗合规关键**：D1、D2 —— 决定系统能否用于真实诊所。
**需要你拍板的产品决策**：D3、D4、D6、D9。

---

## [S2] D1 — 医疗审计日志体系（MC-017）

**现状**：`MedicalCaseAuditLog` 实体在 `SimplifyDataModel`(2026-06-16) 迁移中**被删除**。Audit Service 不存在。PRD 要求"20 字段 diff 跟踪 + 异常隔离"零实现。

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 补回** | 恢复实体 + Audit Service + 字段 diff + `/audit-logs` 端点 | 中（~3-5 人日）；医疗纠纷可追溯 |
| **B 简化** | PRD 标 v2.0，v1.0 不做医案审计 | 医疗纠纷发生时**无法举证**（谁改了什么、何时） |

**我的建议：A**。医疗场景下"无审计=不可用"。这是诊所敢不敢用系统的前提。

---

## [S3] D2 — 打印保护/回写体系（PRINT-004 + 保护字段）

**现状**：`MedicalCasePrintLog` 实体已删；`IsPrinted/PrintVersion/PrintCount/LastPrintedAt` 字段在 `MedicalCaseModel` 中 **0 个**（PRD 9 处引用）。打印操作完全不可追溯。打印回写 Controller 不存在。

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 补回** | 恢复字段 + 打印回写端点 + ERR-30403/30404 打印保护 + PrintVersion 自增 | 中（~3-5 人日）；纸质与电子记录一致性可保证 |
| **B 简化** | PRD 标 v2.0，v1.0 不做打印保护 | 处方可重复打印无记录、打印后可随意改医案无追溯 |

**我的建议：A**。与 D1 同属医疗合规。处方是法律文书，打印必须可追溯。

---

## [S4] D3 — Auth 安全模型（4🔴 + 4⚠️）

**现状**：
- 🔴 AUTH-006 Token 族旋转撤销（核心反重放）— 完全未实现，`/refresh` 改为"过期 JWT 重签"
- 🔴 AUTH-007 安全审计日志（登录/登出/失败）— `ISecurityAuditService` 不存在
- 🔴 AUTH-013 本地登录限流 5/分 — LocalWebAPI 无 `[EnableRateLimiting]`
- 🔴 AUTH-010 AutoLoginToken 轮换 — 旧令牌永有效
- ⚠️ AUTH-004 令牌刷新（已禁用 RefreshTokenAsync）
- ⚠️ AUTH-008 登出不撤销令牌
- ⚠️ AUTH-009 本地自动登录（服务端不可撤销）
- ⚠️ AUTH-002 账户锁定（双轨：SignInManager vs AuthService 自实现）

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 完整补回** | Token 族 + 重放检测 + 安全审计 + 限流 + 撤销 | 大（~8-12 人日）；完整安全模型 |
| **B 简化版** | 仅补限流(AUTH-013,1行)+登出撤销+审计日志；族旋转标 v2.0 | 小（~2-3 人日）；本地模式 + 内网部署下风险可控 |
| **C 讨论** | 先评估威胁模型再决 | — |

**我的建议：B**。小型内网诊所，外部攻击面小；族旋转主要为防 token 被盗重放，内网威胁低。但**限流、登出撤销、审计日志**这三项低成本且必要，应补。族旋转/重放检测可标 v2.0。

---

## [S5] D4 — Restore 软删除恢复（4 模块 🔴）

**现状**：Users/Patients/Herbs/Formulas 四模块的 Restore 全部未接线。`BaseRepository.RestoreAsync`（泛型）+ `GetByIdIncludingDeletedAsync` 基础设施已就绪，仅缺 Service 方法 + Controller 端点 + Desktop 接线。Desktop `ExecuteRestoreAsync` 多处返回 null（死代码命令）。

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 补回** | 4 模块统一补 Service/Controller/Desktop 接线 | 小（~2-3 人日，基础设施已就绪）；误删可恢复 |
| **B 简化** | 删除 Desktop 死代码命令，PRD 标"v1.0 不支持恢复" | 误删患者/药材/验方=永久丢失 |

**我的建议：A**。成本极低（基础设施就绪），收益大（防误删灾难）。

---

## [S6] D5 — 引用检查 BR-DEL-001（必做，无争议）

**现状**：
- Patients 单删路径**完全无引用检查**（被医案引用的患者可删）；批量删反而有 → "单删比批删更不安全"
- Herbs `HerbReferenceRepository` 已注册但 Service **未注入/未使用**；删除/批量删除均不查

**这是数据完整性 bug，不是产品决策。必须 A 修。** 影响：删掉被处方引用的药材 → 历史处方显示"未知药材"；删掉被医案引用的患者 → 医案孤儿。

**建议：A，统一接线引用检查**（~1-2 人日）。

---

## [S7] D6 — Excel 导入导出（2 模块）

**现状**：
- Herbs：`IHerbImportExportService` 在 PRD/AGENTS/README 三处引用，**仓库无此文件**；EPPlus 仅 Formula 模块用。PRD 承诺"Excel 批量导入 400+ 味药材（数天→<1h）"——这是 Admin 初始化药材库的核心价值
- Patients：`PatientImportExportService.cs` 不存在；端点 404；Desktop Refit 照常调

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 补回 Excel** | 两模块补 EPPlus 导入/导出/模板 | 中（~4-6 人日） |
| **B 简化 JSON** | 用 JSON 导入导出替代 Excel（已有 Formula 模块的 JSON 实现可复用） | 小（~1-2 人日）；但 Admin 习惯 Excel，体验降级 |
| **C Herbs 补 Excel / Patients 标 v2.0** | 药材库初始化是刚需，患者导入非刚需 | 中（~2-3 人日） |

**我的建议：C**。药材 Excel 导入是 PRD 量化卖点（数天→<1h），必须补；患者导入优先级低，可标 v2.0。

---

## [S8] D7 — 权限策略错配（必做 bug，无争议）

| 模块 | 当前 | 应为 | 后果 |
|------|------|------|------|
| Herbs | `DoctorOrAdmin` | `DoctorOrReceptionist` | 前台无法查药材 |
| Registration 创建/取消 | `DoctorOrAdmin`（类级） | Receptionist 主体 | **前台无法挂号/取消**（核心职能瘫痪） |
| MedicalCases 创建 | `DoctorOrAdmin` | `Doctor-only` | Admin 可创建医案（违反铁律） |

**这是 bug，必须 A 修。** 影响：前台连本职工作都做不了。

**建议：A，按 PRD 权限矩阵修正**（~1 人日）。

---

## [S9] D8 — P0 数据/安全 Bug（必做，无争议）

| Bug | 模块 | 修复 |
|-----|------|------|
| BR-001 索引漏 Suspended | MedicalCases | 一行 SQL `[CaseStatus] IN (0,1)` |
| 分页+内存筛选错位 | Users | 筛选移到 DB 层 |
| 读卡去重第一环落空 | Patients | 搜索字段加 PhoneNumber/IdNumber |
| GetById 越权 | Formulas | 加所有权检查 |
| StartVisit 返回错 ID | Registration | 统一返回 MedicalCaseId |
| 本地 /refresh 无签名校验 | Auth | 加签名验证 |
| MC-LOCK 用 UtcNow | MedicalCases | 改 DateTime.Now 或显式时区 |

**全是正确性/安全 bug，必须 A 全修**（~2-3 人日）。

---

## [S10] D9 — 历史聚合查询（MC-008/009）

**现状**：PRD 要求"查询患者所有已完成医案的 Consultation 列表 / 处方历史"，代码只返回**当前医案的单条**（注释自承"当前架构下只有一条 Consultation"）。这是"复诊历史回顾"价值点（vision：复诊准备 5min→10s）的数据基础。

| 选项 | 内容 | 代价 |
|------|------|------|
| **A 补回** | 实现跨医案的 Consultation/Prescription 历史聚合查询 | 中（~2-3 人日）；复诊秒查价值兑现 |
| **B 简化** | v1.0 仅支持逐医案查看，不做聚合 | 复诊时医生需手动翻历史医案（退回部分纸质痛点） |

**我的建议：A**。这是 PRD 核心价值（复诊效率 2.5-4×）的数据支撑，不做则核心卖点落空。

---

## [S11] 其他未列入决策簇的 ⚠️ 部分（批量/模板端点等）

这些属"端点暴露"类小问题（Service 实现了但 Controller 没暴露端点），修复成本低，随对应模块 Phase② 一并处理：
- Formulas: `/export`、`/import-template`、`/batch-enable/disable` 端点缺失（Service 已实现）
- MedicalCases: `/batch-details`、`/permissions` 端点缺失
- 各种死代码清理（AuthService、Registration 孤儿方法）

**不需你单独拍板**，归入各模块 Phase②。

---

## [S12] 决策汇总表（已关闭）

| 簇 | 我的建议 | 你的决策 | 工作量 |
|---|------|------|:---:|
| D1 医疗审计日志 | A 补回 | **A 补回** ✅ | 3-5d |
| D2 打印保护/回写 | A 补回 | **A 补回** ✅ | 3-5d |
| D3 Auth 安全模型 | B 简化版 | **B+ 中等方案** ✅ | 4-6d |
| D4 Restore 恢复 | A 补回 | **A 补回** ✅ | 2-3d |
| D5 引用检查 | A 必做 | **A 必做** ✅ | 1-2d |
| D6 Excel 导入导出 | C（Herbs补/Patients标v2.0） | **Desktop Excel + API JSON** ✅ | 2-3d |
| D7 权限错配 | A 必做 | **A 必做** ✅ | 1d |
| D8 P0 Bug | A 必做 | **A 必做** ✅ | 2-3d |
| D9 历史聚合 | A 补回 | **A 补回** ✅ | 2-3d |
| D10 字段加密 | ✅ 已定拉回 | ✅ 已定 | — |

**B+ 中等方案**：补 Token 族旋转 + 限流 + 登出撤销 + 审计日志；重放检测标 v2.0。WebAPI 发布到公有云，安全需求升级。
**Desktop Excel + API JSON**：Desktop 端支持 Excel 导入导出（用户习惯），API 层用 JSON（标准化数据交换）。Herbs 药材补 Excel，Patients 导入标 v2.0。

---

## [S13] 决策后的下一步

全部决策已完成。后续执行：
1. 据此更新各模块 PRD 的 US 状态列（真实标注 ✅/❌/延期）
2. 修正 `SimplifyDataModel` 删除项的业务规则描述
3. PRD 冻结为真实 v1.0 范围
4. 进入各模块 Phase② 设计（含 Shell），基于真实基线
