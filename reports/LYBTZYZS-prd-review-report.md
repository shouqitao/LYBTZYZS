# LYBTZYZS 临床医案管理系统 — PRD 深度审查报告

> **审查日期**: 2026-04-18
> **审查范围**: 01-product/, 02-requirements/, 03-architecture/ 全部核心文档
> **审查人**: 资深架构师兼产品经理（AI 审查）

---

## 1. 执行摘要

### 总体评分: **8.5 / 10**

这是一份**高质量、成熟度极高**的产品需求文档体系。文档覆盖面广、结构清晰、跨模块一致性显著高于同类项目平均水平。项目从 2025-12 至 2026-03 短短 3 个月内完成了 v1.0 全部功能（138 US 中 137 Done，1 Removed），同时维持了文档与代码的高度对齐。

### 核心发现

| 维度 | 评分 | 一句话总结 |
|------|------|-----------|
| PRD 完整性 | 8.5/10 | US 验收标准完整，边界条件充分，少数字段定义有歧义 |
| 文档一致性 | 8.0/10 | 角色权限矩阵汇总质量极高，但存在 3 处跨文档冲突 |
| 架构设计 | 9.0/10 | ADR 决策合理，DDD 聚合根设计干净，测试策略创新 |
| 业务逻辑深度 | 8.0/10 | 中医临床流程覆盖完整，少数场景未展开 |
| 缺失功能 | 7.5/10 | 核心流程完整，缺少数据统计/报表、操作引导等辅助功能 |

### 主要亮点

1. **角色权限矩阵**（role-permission-matrix.md）是整个文档体系中最出色的部分——从 7 个模块 PRD 汇总提取，逻辑自洽，错误码全覆盖
2. **DDD 聚合根设计**（MedicalCase）干净利落，从贫血模型到充血模型的演进路径清晰
3. **Testing Trophy 策略**（ADR-0003）是真正有价值的架构决策——零 mock 的 Server 测试在 .NET 社区不常见
4. **文档变更记录**完善，每个文件都有完整的版本历史和变更说明

### 主要风险

1. 3 处跨文档分歧需修复（详见第 3 节）
2. 中医临床场景有 2 个重要盲区未覆盖
3. 缺少面向新用户的操作引导设计

---

## 2. PRD 完整性评分

### 2.1 各模块完整性评分

| 模块 | US 总数 | 完整性评分 | 主要缺失 |
|------|--------|-----------|---------|
| Auth（认证） | 13 | 9.0/10 | 无显著缺失，Token 安全设计充分 |
| Users（用户） | 12 | 8.5/10 | SuperAdmin 密码恢复仅在 CLI，无 GUI 方案说明 |
| Patients（患者） | 13 | 9.0/10 | Excel 导入字段映射规则未完整列出 |
| Herbs（药材） | 13 | 8.5/10 | 分类体系（Category）的预设值未定义 |
| Formulas（验方） | 13 | 8.5/10 | 延迟绑定验证操作的具体 UI 流程未展开 |
| MedicalCase（医案） | 18 | 9.0/10 | 核心模块，文档最为详尽 |
| Registration（挂号） | 7 | 8.5/10 | 挂号队列超长无预警机制 |
| Sync（同步） | 8 | 8.0/10 | MedicalCase 同步延至 v2.0，同步冲突解决仅支持逐条手动 |
| Printing（打印） | 4 | 8.0/10 | 打印失败后的用户操作流程未定义 |
| NFR | - | 9.0/10 | 四维度覆盖完整，指标合理 |
| Error Handling | 8 | 8.5/10 | MCCEE 错误码体系设计规范 |
| Desktop Shell | 7 | 8.0/10 | 主题/布局自定义能力未提及 |

### 2.2 US 验收标准覆盖度

- **138 个 US**中，约 95% 有明确的 Acceptance Criteria（AC）
- AC 以可测试的断言形式书写（如 `Price=0 → 返回 400 验证失败`），质量高
- **缺失 AC 的 US**：US-SYS-001~009（系统健康）的 AC 较简略，仅描述功能无量化指标

### 2.3 数据字段定义审查

**总体**: 数据模型（data-model.md）字段定义完整，含类型、约束、默认值。

**发现的问题**:

| # | 问题 | 来源 | 严重度 |
|---|------|------|--------|
| F-01 | `Prescription.Discount` 类型在 medical-cases.md 描述为 `decimal(3,2)`（范围 0-1），但在 data-model.md 为 `decimal(5,4)`，精度不一致 | medical-cases.md vs data-model.md | 中 |
| F-02 | `Prescription.ReferencedFormulas` 在 medical-cases.md 描述为 JSON 数组格式（含 type/id/name/importedAt），但 data-model.md 仅写 `string(500)` 且描述为"逗号分隔"，格式定义矛盾 | medical-cases.md vs data-model.md | 高 |
| F-03 | `Patient.MaritalStatus` 和 `BloodType` 在 data-model.md 中定义为必填（`int`），但在 patients.md 的 US-PAT-001 创建患者 Business Rules 中未提及这两个字段为必填 | patients.md vs data-model.md | 中 |
| F-04 | `MedicalCaseAuditLog.OperationType` 在 medical-cases.md 描述为"使用 int 枚举存储（非 string）"，但 Data Model 表中类型写 `string`，MaxLength(20) | medical-cases.md 内部矛盾 | 中 |

### 2.4 业务流程覆盖度

**已覆盖的核心流程**:
- ✅ 首诊全流程（登记→医案→诊断→处方→保存→打印→完成）
- ✅ 复诊全流程（搜索患者→查看历史→复制处方→微调→完成）
- ✅ 验方创建与使用流程
- ✅ 药材管理流程（含批量导入）
- ✅ 双模式切换与同步
- ✅ BR-001 碰撞处理、BR-002 离开决策、BR-003 完成校验

**流程缺失**:
- ⚠️ **数据迁移首次上线流程**未定义（从纸质/Excel 到系统的初始数据导入操作手册）
- ⚠️ **系统升级/数据库迁移流程**仅在架构文档提及，无操作级 PRD

---

## 3. 分歧点清单

### 3.1 跨文档矛盾

#### D-01: `Prescription.ReferencedFormulas` 格式定义矛盾 🔴 高

| 文档 | 描述 |
|------|------|
| **medical-cases.md** Data Model 章节 | `string(1000)?`，格式为 **JSON 数组**: `[{"type":"formula","id":"uuid-1","name":"四君子汤","importedAt":"..."}]` |
| **data-model.md** Prescription 章节 | `string(500)`，描述为 **"引用验方 (逗号分隔)"** |

**影响**: 字段长度不一致（1000 vs 500），存储格式不一致（JSON vs 逗号分隔）。这会导致实现偏差。

**建议**: 以 medical-cases.md 为准（JSON 数组格式更灵活），同步更新 data-model.md。

---

#### D-02: `UserRole` 枚举值不一致 🔴 高

| 文档 | 描述 |
|------|------|
| **user-roles.md** | `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100` |
| **glossary.md** | `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100` |
| **data-model.md** | `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100` |
| **role-permission-matrix.md** Section 1.1 | 权限值: `SuperAdmin=100, Admin=80, Doctor=60, Receptionist=40` |

**影响**: role-permission-matrix.md 使用的是"权限值"（Permission Level），与 UserRole 枚举值不同。虽然文档有说明"权限值层级模型"，但容易混淆。Admin 权限值 80 vs UserRole 10，如果代码中混用会导致严重权限漏洞。

**建议**: 在 role-permission-matrix.md 中增加醒目注释，明确区分 UserRole 枚举值和 PermissionLevel 权限值，并说明代码中的使用场景。

---

#### D-03: `DecocteMethod` 煎法枚举值不一致 🟡 中

| 文档 | 枚举定义 |
|------|---------|
| **medical-cases.md** | Normal(0), DecocteFirst(1), DecocteLater(2), WrapDecoction(3), SeparateDecoction(4), MeltIn(5), TakeWithDecoction(6) — **7 个值** |
| **glossary.md** | Default(0), PreDecoct(1), PostDecoct(2) — **仅 3 个值** |
| **data-model.md** | Default(0), PreDecoct(1), PostAdd(2), MeltIn(3), TakeWithWater(4), WrapDecoct(5), SeparateDecoct(6) — **7 个值，但名称和映射不同** |

**影响**: 三处定义存在数量和映射不一致。glossary.md 严重滞后（仅 3 个值）；data-model.md 和 medical-cases.md 的值 2-6 映射不同（DecocteLater vs PostAdd, WrapDecoction vs MeltIn）。

**建议**: 以 medical-cases.md 为权威来源（业务定义最详细），同步更新 glossary.md 和 data-model.md。

---

#### D-04: user-roles.md 患者管理 Receptionist 权限描述不一致 🟡 中

| 文档 | Receptionist 对患者权限 |
|------|----------------------|
| **user-roles.md** 模块权限矩阵 | "禁止" |
| **patients.md** Section 2 | "创建、查看列表/详情、更新患者（CRU，无删除权限）" |
| **role-permission-matrix.md** | "CRU，无删除权限" |

**影响**: user-roles.md 的 API 级权限矩阵显示 Receptionist 对 `/api/v1/patients` 为"禁止"，但 patients.md 和 role-permission-matrix.md 明确允许 CRU。

**建议**: 更新 user-roles.md 模块权限矩阵，Receptionist 对患者管理应为"CRU（无删除）"。

---

#### D-05: user-roles.md 验方管理 Doctor 权限描述过于简略 🟡 中

| 文档 | Doctor 对验方列表权限 |
|------|---------------------|
| **user-roles.md** | "CRUD (受限)" |
| **formulas.md** / **role-permission-matrix.md** | "CRUD 自己的 + 查看共享验方（只读）" |

**影响**: user-roles.md 的"受限"过于模糊。

**建议**: 更新 user-roles.md 验方管理行，明确"CRUD 自己的 + 查看共享（只读）"。

---

### 3.2 文档内部矛盾

#### D-06: clinical-workflow.md 完成校验错误码与 medical-cases.md 不一致 🟡 中

| 文档 | 处方药材为空的错误码 |
|------|---------------------|
| **clinical-workflow.md** Section 2.7 | `ERR-30304` |
| **medical-cases.md** BR-003 | "处方至少包含一味药材"（无独立错误码，属于 ERR-30303 的子项） |

**影响**: 错误码引用不一致可能影响前端错误处理逻辑。

**建议**: 统一错误码引用。medical-cases.md 的 BR-003 表格中"处方存在性"和"处方药材"使用不同错误码是合理的，但 clinical-workflow.md 应与 medical-cases.md 保持一致。

---

## 4. 架构评审意见

### 4.1 ADR 逐条评审

| ADR | 标题 | 评审意见 | 评分 |
|-----|------|---------|------|
| 0001 | MedicalCase 聚合根 | **优秀**。聚合根边界清晰，充血模型演进路线合理。演进触发条件（500 行/5 人/性能瓶颈）务实。 | 9.5/10 |
| 0002 | 双模式架构 | **良好**。策略模式实现双模式简洁有效。已从 DataSource 抽象层演进到 Factory + Dual Repository，决策正确。**关注点**: Sync v1.0 不含 MedicalCase 同步，意味着本地模式下创建的医案无法同步到服务端——这对多医生场景是硬伤，但 PRD 已明确标注 v2.0。 | 8.0/10 |
| 0003 | 集成优先测试 | **优秀**。Testing Trophy 是 .NET 社区少见的实践。零 mock + 真实 DB + Respawn 的 Server 测试，加上 AntiMockRule 架构防护测试，确保测试价值最大化。从 5 项目到 3 项目的演进决策果断。 | 9.5/10 |
| 0004 | 用户上下文传递 | **优秀**。显式参数传递优于 Ambient Context，Service 层可独立测试。"禁止 Service 层注入 IHttpContextAccessor"是正确的架构约束。 | 9.0/10 |
| 0005 | SuperAdmin 归属 Auth | **合理**。SuperAdmin 与业务用户分离存储是安全最佳实践。DPAPI 加密 Token 存储 + SeedTool CLI 密码恢复满足运维需求。 | 8.5/10 |
| 0006 | ViewModel 组件化 | **良好**。Coordinator + Components 模式解决 ViewModel 膨胀问题。500 行阈值合理。 | 8.0/10 |
| 0007 | ViewModel 组合模式 | **合理**。选择组合而非继承是 SOLID 原则的正确应用。"两棵继承树"的存在需要更明确的文档说明（本 ADR 已部分覆盖）。 | 7.5/10 |
| 0008 | Token 安全防御性设计 | **优秀**。对"是否过度设计"给出了清晰的 4 点论证（安全无过度/面向扩展/沉没成本/审计合规）。FamilyId 重放检测机制设计精巧。 | 9.0/10 |

### 4.2 架构总体评价

**优势**:
- 三层架构（Server/Shared/Client）依赖方向严格单向，模块间通信通过 ICrossModuleService 和 IEventAggregator，无循环依赖
- MedicalCase 充血模型是正确的 DDD 实践
- Testing Trophy 策略在小型团队中投入产出比极高

**关注点**:
- 40+ 项目的解决方案规模对单人维护有挑战（但已有文档化缓解措施）
- ICrossModuleService 已标记 `[Obsolete]`，迁移进度需跟踪
- Sync 模块 v1.0 仅支持 Herb/Patient/Formula 三实体同步，MedicalCase 同步延至 v2.0——这意味着本地模式创建的医案**无法同步到服务端**，在多医生诊所场景下是功能缺口

---

## 5. 业务逻辑评审

### 5.1 中医临床场景覆盖度

| 场景 | 覆盖状态 | 来源 |
|------|---------|------|
| 望闻问切四诊合参 | ✅ | Consultation 实体含 PresentIllness/TongueDiagnosis/PulseDiagnosis/TcmDiagnosis |
| 辨证论治 | ✅ | TcmDiagnosis 为完成时必填字段 |
| 处方开具（药材+剂量+煎法） | ✅ | PrescriptionItem 含 Dosage/Unit/DecocteMethod |
| 验方导入处方 | ✅ | US-MC-016，含重复药材合并策略（MC-D17） |
| 历史处方复制 | ✅ | US-MC-018，价格实时获取 |
| 处方费用计算 | ✅ | MC-D14 公式完整 |
| 打印处方笺 | ✅ | A5 模板，含打印保护 |
| 复诊快速查阅 | ✅ | 跨医案搜索（US-MC-010） |
| 挂号排队 | ✅ | Registration 模块，前台/医生双模式 |
| 离线诊疗 | ✅ | 本地模式 + Sync |

### 5.2 未覆盖的中医场景

| # | 场景 | 严重度 | 说明 |
|---|------|--------|------|
| B-01 | **中药配伍禁忌检查** | 🔴 高 | 中医"十八反、十九畏"等配伍禁忌是开方安全的基本要求。当前处方模块无任何药材配伍检查逻辑。虽然 PRD scope 中未提及，但这属于行业安全底线。 |
| B-02 | **过敏史提醒** | 🟡 中 | Patient 有 AllergyHistory 字段，但开方流程中未定义自动提醒逻辑（如患者对某药材过敏时弹出警告）。 |
| B-03 | **中药炮制方法** | 🟢 低 | FormulaHerbItem 有 ProcessingMethod 字段，但 PrescriptionItem 无此字段。处方中无法指定药材炮制方法（如"酒炒当归"）。 |
| B-04 | **医嘱/用法模板** | 🟢 低 | Prescription.Usage 和 Advice 为自由文本，无预置模板。常见用法如"水煎服，日一剂，分两次温服"需每次手动输入。 |

### 5.3 数据校验规则审查

**覆盖良好的校验**:
- ✅ BR-001 同一患者单活跃医案约束（代码 + DB 唯一索引双重保障）
- ✅ BR-003 完成校验（诊断/处方标记/药材/帖数 5 项检查）
- ✅ 删除引用检查（患者/药材被引用时禁止删除）
- ✅ 患者禁用联动（禁止创建新医案、挂号）
- ✅ 打印保护（IsPrinted 修改需 EditReason）
- ✅ 锁定规则（隔天自动锁定，Admin 需 EditReason）

**缺失的校验**:
- ⚠️ 处方药材重复检查——MC-D17 定义了导入时的合并策略，但手工添加药材时的重复检查未明确
- ⚠️ Dosage 上限——仅定义 DosageCount 范围 1-100，单个药材 Dosage 无上限约束

---

## 6. 关键缺失项（按优先级排序）

### P0 — 必须修复

| # | 缺失项 | 说明 | 建议 |
|---|--------|------|------|
| P0-01 | **ReferencedFormulas 格式/长度矛盾** | D-01，存储格式和长度在两个文档中不一致 | 统一为 JSON 数组，长度 1000 |
| P0-02 | **DecocteMethod 枚举三文档不一致** | D-03，三处定义的数量和映射均不同 | 以 medical-cases.md 为准，同步其他 |
| P0-03 | **中药配伍禁忌检查** | B-01，行业安全底线 | v1.1 增加配伍禁忌数据表和检查逻辑 |

### P1 — 应当修复

| # | 缺失项 | 说明 | 建议 |
|---|--------|------|------|
| P1-01 | **user-roles.md Receptionist 患者权限错误** | D-04，与 patients.md/role-permission-matrix.md 矛盾 | 更新为 CRU |
| P1-02 | **UserRole 枚举值 vs PermissionLevel 混淆** | D-02，role-permission-matrix.md 权限值与枚举值不同 | 增加醒目注释区分 |
| P1-03 | **过敏史开方提醒** | B-02，字段已有但流程未衔接 | 在处方保存前自动匹配检查 |
| P1-04 | **MedicalCaseAuditLog.OperationType 类型矛盾** | F-04，描述 int 枚举但字段类型 string | 统一为 int 或 string，二选一 |
| P1-05 | **数据统计/运营报表** | PRD 中无任何统计功能（门诊量、常用药材、收入统计） | v1.1 规划基础统计面板 |
| P1-06 | **首次上线数据迁移指南** | 从纸质/Excel 到系统的操作手册 | 补充 06-operations/ 目录 |

### P2 — 可以改进

| # | 缺失项 | 说明 | 建议 |
|---|--------|------|------|
| P2-01 | **glossary.md DecocteMethod 严重滞后** | 仅 3 个值，实际 7 个 | 全面更新 |
| P2-02 | **操作引导/新手教程** | personas.md 描述李医生"遇到弹窗会紧张"，但无引导设计 | 添加首次登录引导流程 |
| P2-03 | **药材分类体系预设值** | Category 字段无预置分类列表 | 补充常用分类（解表药、清热药、补益药等） |
| P2-04 | **打印失败用户操作流程** | 打印失败后仅记录日志，无用户操作指引 | 增加重试/更换打印机指引 |
| P2-05 | **处方用法模板** | 常见用法需每次手动输入 | 预置常用用法模板 |
| P2-06 | **同步进度可视化** | SyncPhase 6 状态 FSM 已定义，但用户侧进度展示未展开 | 补充 SyncView UI 规格 |

---

## 7. 改进建议

### 7.1 文档层面

| # | 建议 | 优先级 | 工作量 |
|---|------|--------|--------|
| 1 | 创建**术语权威来源表**，明确每种枚举/常量以哪个文档为权威来源，其他文档引用 | P0 | 0.5d |
| 2 | 更新 glossary.md 使其与实际代码/PRD 完全同步 | P1 | 0.5d |
| 3 | 在 data-model.md 中标注与 medical-cases.md 字段定义的差异点 | P1 | 0.5d |
| 4 | role-permission-matrix.md 增加 UserRole 枚举值 vs PermissionLevel 对照表 | P1 | 0.5h |
| 5 | 为每个模块 PRD 增加 "数据字段权威来源" 声明，减少未来同步成本 | P2 | 1d |