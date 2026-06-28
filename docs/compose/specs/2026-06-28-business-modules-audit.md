# 业务模块审计汇总报告（8 模块全景）

> **状态**：基于 8 个并行 explore subagent 的代码级审计，每个模块「PRD vs 代码」逐 US 核验。
> **日期**：2026-06-28
> **姊妹文档**：Shell 审查基线 `2026-06-28-shell-audit-baseline.md`、Shell 需求 Round 1 `2026-06-28-shell-requirements-round1.md`
> **核心结论**：PRD 系统性虚报「✅已实现」，8 个模块**全部**不满足自身 PRD 验收标准，**全部判定不可进入 Phase②设计**。

---

## [S1] 总览记分牌

| 模块 | PRD US | 完全达标 ✅ | 部分 ⚠️ | 未实现 🔴 | Phase② 准入 | 复杂度 |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| **Auth** | 13 | 0-2 | 4 | **4** | ❌ | 4/10 |
| **Users** | 12 | 8 | 3 | **1** | ⚠️ 有条件 | 5/10 |
| **Patients** | 13 | 4 | 4 | **5** | ❌ | 6/10 |
| **Herbs** | 13 | 3 | 3 | **7** | ❌ | 中低 |
| **Formulas** | 13 | 7 | 3 | **3** | ❌ | 3/5 |
| **MedicalCases**（聚合根）| 18 | 8 | 6 | **4** | ❌ | 8/10 |
| **Registration** | 7 | 2 | 2 | **3** | ❌ | 6/10 |
| **Printing** | 4 | 1 | 2 | **1** | ❌ | 6/10 |
| **合计** | **93** | ~33 | 27 | **28** | **0/8 通过** | — |

**约 1/3 US 未达验收标准，其中 28 个完全未实现却标 ✅。** 这不是「接近完成有 bug」，是「功能大面积缺失被乐观状态标记掩盖」。

---

## [S2] 跨模块系统性缺失（不是个案，是模式）

### 模式 A：Restore（软删除恢复）全线断裂
- **Users**：Server 无端点、Desktop `ExecuteRestoreAsync` 返回 null、VM 仍触发 = 死代码
- **Patients**：Controller 无 restore 路由、Service 无 RestoreAsync（DTO 已设计无人调用）
- **Herbs**：Service 无 RestoreAsync、Controller 无 restore 端点
- **Formulas**：Server 无 /restore、Desktop `ExecuteRestoreAsync` 是 `Task.FromResult(null)` 空操作
- **共性**：`BaseRepository.RestoreAsync`（泛型能力）+ `GetByIdIncludingDeletedAsync` 基础设施齐备，**但 4 个模块都忘记接线**。软删了就找不回来。

### 模式 B：引用检查（BR-DEL-001 数据完整性）缺失
- **Patients**：单删路径**完全无引用检查**（被医案引用的患者可删）；批量删反而有 → "单删比批删更不安全"
- **Herbs**：`HerbReferenceRepository` 已注册但 Service **未注入/未使用**；删除/批量删除均不检查 → 被处方引用的药材可静默软删
- **后果**：破坏"保护已开处方/医案完整性"的核心不变量

### 模式 C：Excel 导入/导出大面积缺失
- **Herbs**：`IHerbImportExportService` 在 PRD/AGENTS/README 三处引用，**仓库中无此文件**；EPPlus 仅 Formula 模块用
- **Patients**：`PatientImportExportService.cs` 不存在；Controller 无 import/export/template 路由；**Desktop Refit 客户端照常调用 → 运行时 404**
- **共性**："DTO/契约/调用方齐备，唯独 Server 实现缺失"的契约撕裂

### 模式 D：权限策略系统性错配
| 模块 | 当前策略 | PRD 要求 | 后果 |
|------|---------|---------|------|
| Herbs | `DoctorOrAdmin` | `DoctorOrReceptionist` | Receptionist 无法查药材（开方/收费核实受阻） |
| Registration 创建/取消 | `DoctorOrAdmin`（类级） | Receptionist 主体 | **前台无法挂号/取消**（核心职能瘫痪） |
| MedicalCases 创建 | `DoctorOrAdmin` | **Doctor-only** | Admin 可创建医案（违反"仅 Doctor 可创建"铁律） |

### 模式 E：打印保护/审计体系整体缺失
- **Printing US-004**（打印回写）：`MedicalCasePrintController` 不存在、`MedicalCasePrintLog` 实体已删、客户端无 API 调用 → **打印操作完全不可追溯**
- **MedicalCases MC-017**（审计日志）：`MedicalCaseAuditLog` 实体已删、Audit Service 不存在 → **医疗纠纷无法追溯**（20 字段 diff 零实现）
- **共性**：`IsPrinted/PrintVersion/PrintCount/LastPrintedAt` 字段在 `MedicalCaseModel` 中 **0 个**，但 PRD 9 处引用

### 模式 F：Service 层被绕过（死代码 Service）
- **Auth**：`AuthService`（227 行）注册为 Scoped，但 WebAPI/LocalWebAPI 两个 Controller **都绕过它直接用 UserManager/SignInManager** → 整个 Service 是装饰用死代码
- **Registration**：`QuickVisitAsync`（Service）无调用方 = 死代码；`CompleteByMedicalCaseAsync`/`HandleMedicalCaseCancelledAsync` 因 MedicalCase 直接 ProjectReference Registration 而成孤儿

---

## [S3] 架构违规

1. **模块间直接引用**：`Module.MedicalCase` csproj 直接 `ProjectReference Module.Registration`（违反 AGENTS.md「Modules MUST NOT reference each other」）。架构测试未覆盖此依赖方向。
2. **WebAPI/LocalWebAPI Controller 重复**：UsersController 610 vs 599 行几乎完全复制（违反 ADR-0010「统一服务层」承诺）。
3. **跨模块操作非事务性**：MedicalCase↔Registration 联动无 `IDbContextTransaction`，任一失败导致数据不一致（医案已取消但挂号未回退）。
4. **契约/实现撕裂**：Desktop Refit 客户端调用 Server 不存在的端点（Patients 导入导出、Herbs 引用检查）→ 运行时 404。

---

## [S4] 关键转折点：`SimplifyDataModel` 迁移（2026-06-16）

```
2026-06-15  PRD v2.0 发布（描述完整 Audit/Print/Token族/Restore 体系）
2026-06-16  SimplifyDataModel 迁移：删除 MedicalCaseAuditLog / MedicalCasePrintLog /
            IsPrinted / PrintVersion 等实体与字段
2026-06-25  PRD v2.1 修正一些数值，但未同步删除已弃用的 Audit/Print 业务规则
```

**这是代码与文档分道扬镳的时刻。** 代码做了"简化"，PRD 没跟上。后续每个标 ✅ 但实际缺失的 US，根源多在此。

---

## [S5] 数据正确性 / 安全 Bug（P0，独立于功能缺失）

| Bug | 模块 | 影响 |
|-----|------|------|
| BR-001 DB 索引仅覆盖 Active 不覆盖 Suspended | MedicalCases | 并发下两个 Suspended 医案可同时创建，**违反核心铁律** |
| 分页+内存筛选错位 | Users | TotalCount=过滤前、Items=过滤后，前端数量不一致 |
| 读卡去重第一环永远落空 | Patients | IdNumber 搜索在数据层被丢弃 → 重复建档 |
| Formula GetById 越权 | Formulas | Doctor 可读他人非共享验方（违反 403 验收） |
| StartVisit 返回 RegistrationId 当 MedicalCaseId | Registration | Desktop 导航到不存在的医案 |
| 本地 /refresh 接受任意 JWT 无签名校验 | Auth | 本地模式安全模型漏洞 |
| MC-LOCK 用 UtcNow.Date | MedicalCases | UTC+8 下北京 8:00 前误判锁定 |

---

## [S6] 文档失实清单（PRD/AGENTS.md/README 描述了不存在的代码）

| 文档声称 | 实际 |
|---------|------|
| Auth AGENTS.md: TokenRevocationService/AutoLoginService/SecurityAuditService | 全部不存在 |
| Herbs README/AGENTS: IHerbImportExportService/CheckReferenceAsync | 全部不存在 |
| MedicalCases AGENTS: MedicalCaseAuditService/PermissionService/PrintService | 全部不存在 |
| PRD 01: HandyControl 依赖 | 已移除（迁 MDIX） |
| PRD 各模块 US 状态列 | 大面积虚标 ✅ |

---

## [S7] 战略含义：必须先做「PRD-代码对账」

8 个模块的缺口不是"修 bug"，而是**产品决策**：每个缺失/简化的功能，都要回答一个问题——

> **是被故意简化（→ 更新 PRD 反映现状），还是意外丢失（→ 按 PRD 重新实现）？**

特别是 `SimplifyDataModel` 删掉的体系（审计日志、打印保护、Token 族），这些是**医疗合规核心**，不能默认"简化即正确"。

**建议的对账矩阵**（每个缺失 US 一行）：

| US | 现状 | 选项 A：补回实现 | 选项 B：更新 PRD 为简化版 | 决策 |
|----|------|----------------|------------------------|------|
| MC-017 审计日志 | 实体已删 | 重做（医疗合规必需） | 标 v2.0 | ❓ |
| PRINT-004 打印回写 | 实体已删 | 重做 | 标 v2.0 | ❓ |
| AUTH-006 Token 族旋转 | 未实现 | 重做 | 标简化版 | ❓ |
| ... | | | | |

**这个对账必须由你（产品负责人）拍板**，因为每个决策影响 v1.0 范围与医疗合规性。

---

## [S8] 与 Shell 审查的呼应

Shell 审查发现的「双轨/三套/硬编码」是**架构层**问题；本次业务模块审计揭露的是**功能层**问题。两层问题叠加，印证了你的诊断——**「想到哪写到哪」**——且范围比预期大：不只 Shell，整个项目都需要基于真实代码重新对齐需求。

**下一步路径**（待你确认）：
1. 先做 **PRD-代码对账**（S7 矩阵），逐 US 决策补回/简化
2. 对账后 PRD 冻结为真实 v1.0 范围
3. 再进入各模块的 Phase②设计（含 Shell）
