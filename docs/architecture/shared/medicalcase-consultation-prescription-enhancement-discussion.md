# 医案/诊断/处方功能深化讨论

> **文档版本**: v1.0
> **创建日期**: 2025-10-24
> **状态**: 🔄 讨论中
> **讨论目标**: 基于现状分析，优化核心看诊流程用户体验和数据完整性

---

## 📚 相关文档

- **[需求文档](../../requirements/medicalcase-consultation-prescription-enhancement-requirements.md)** - ✅ 已完成（基于本讨论文档）
- **[设计文档](../../design/medicalcase-consultation-prescription-enhancement-design.md)** - ✅ 已完成（详细技术设计和实施方案）⭐
- **[差距分析文档](../../design/medicalcase-consultation-prescription-gap-analysis.md)** - ✅ 已完成（现有代码与设计的差距及修改计划）⭐⭐
- **[三模块现状分析报告](../../reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md)** - 20430行代码完整统计
- **[业务规则文档](../../business-rules.md)** - 14条核心业务规则
- **[看诊流程实体关系](clinical-workflow-entity-relationships.md)** - 1:1:1关系、DDD聚合根设计

---

## 🎯 讨论原则

✅ **本文档仅讨论**：
- 做什么（What）
- 为什么（Why）
- 业务场景和用户需求

❌ **本文档不讨论**：
- 怎么实现（How）- 技术细节留给设计文档
- 具体代码实现
- 技术选型

📋 **讨论流程**：
- 每次提出一个问题（❓ Q1, Q2, Q3...）
- 提供方案对比表
- 等待用户回答
- 记录决策（✅ A1, A2, A3...）

---

## 🎨 Part A: 用户体验优化

### ❓ Q1: 三步看诊流程是否符合实际操作习惯？

**背景信息**：

当前系统实现了"三步看诊流程"（Issue #1567）：
- **Step 1 - 辨证**：填写四诊信息（望闻问切、主诉、中医诊断）
- **Step 2 - 施治**：开具处方（药材、剂量、用法）
- **Step 3 - 完成**：确认医案汇总，关闭医案

**流程约束**：
- ✅ 允许前进（验证通过后）
- ✅ 允许后退（数据不丢失）
- ❌ 不允许跳步（必须按顺序）

**方案对比**：

| 方案 | 流程特点 | 优点 | 缺点 | 适用场景 |
|-----|---------|------|------|---------|
| **A. 严格顺序**（当前） | 辨证→施治→完成<br/>不可跳步 | • 数据完整性高<br/>• 流程规范 | • 缺乏灵活性<br/>• 可能影响效率 | 标准初诊流程 |
| **B. 灵活跳转** | 任意步骤可直接跳转 | • 操作灵活<br/>• 效率高 | • 可能遗漏必填项<br/>• 数据不完整风险 | 复诊、简单问诊 |
| **C. 智能引导** | 默认顺序+特殊场景允许跳步 | • 平衡规范与灵活性 | • 规则复杂<br/>• 开发成本高 | 综合场景 |

**核心问题**：

在实际看诊中，是否遇到过**"必须先填诊断才能开处方"**的不便？

例如：
- 复诊患者：已有诊断记录，只需调整处方
- 简单症状：快速开方，诊断可简化
- 急诊场景：先开处方稳定病情，后补充诊断

请分享您的真实使用场景和期望流程。

---

### ✅ A1: 动态流程 + 开处方决策点

**流程设计**：

```
Step 1（辩证）界面上有RadioBox：
  ○ 开处方（默认选中）
  ○ 不开处方

点击"完成辩证"或"下一步"时：
  → 检查RadioBox选择：
    ├─ 选择"开处方" + 处方为空：
    │   → 提示："处方为空，如果不需要开处方请关闭处方按钮"
    │   → 停留在Step 1
    │
    ├─ 选择"开处方" + 处方不为空：
    │   → 进入Step 2（施治）
    │   → 继续编辑处方或直接进入Step 3
    │
    └─ 选择"不开处方"：
        → 直接进入Step 3（总结）

在 Step 3 可以：
• 回看 Step 1、Step 2（如果之前开过处方）
• 修改"是否开处方"决策：
  - 从"不开处方"改成"开处方" → 重新进入Step 2
  - 从"开处方"改成"不开处方" → 二次确认+用户选择删除方式
```

**UI设计**：
- RadioBox位置：辩证界面底部，"完成辩证"按钮上方
- RadioBox默认值：开处方（选中状态）
- 提示信息：Toast或MessageBox

**业务场景**：
- ✅ 只诊断不开方：健康咨询、病情评估、体质分析（选择"不开处方"）
- ✅ 先诊断后追加处方：初诊时观察，复诊时开方（Step 3修改决策）
- ✅ 取消处方决策：病情变化，不再需要用药（Step 3修改决策）

---

### ✅ A2: 处方删除策略 - 二次确认 + 用户选择删除方式

**确认对话框设计**：

```
⚠️ 确认取消处方

当前病案已开具处方，取消后处方数据将会：

○ 软删除（推荐）
  • 处方标记为"已作废"
  • 数据保留可追溯
  • 支持恢复操作

○ 物理删除
  • 永久删除处方数据
  • 无法恢复
  • 数据库更干净

[取消] [确认删除]
```

**业务规则**：
- ✅ 默认选中"软删除"（安全优先）
- ✅ 用户可主动选择"物理删除"
- ✅ 物理删除需二次确认（双重保险）

**数据处理**：
- 软删除：`Prescription.IsActive = false, CancelledAt = DateTime.Now`
- 物理删除：`DbContext.Remove(prescription)`

---

### ❓ Q3: 其他患者病案查询功能

**背景信息**：

医生在看诊过程中需要参考历史病案：
1. **当前患者历史**：查看最近5次病案（已有功能）
2. **其他患者病案**：查询相似病例作为参考

**核心需求**：
- 辩证阶段：查看其他患者的诊断，了解治疗思路
- 施治阶段：查看其他患者的处方，参考用药方案并导入

---

### ✅ A3-1: 查询入口 - 悬浮菜单 + 统一弹窗

**功能位置**：
- ✅ Step 1（辩证）：右下角悬浮菜单
- ✅ Step 2（施治）：右下角悬浮菜单
- ❌ Step 3（总结）：不需要

**弹窗结构**：
```
┌─ 查询其他患者病案 ──────────────────────────┐
│ ┌─ 查询条件 ─┐  ┌─ 病案列表 ─┐            │
│ │ 姓名：      │  │ 张** 女 35岁│            │
│ │ 电话：      │  │ 李** 男 42岁│            │
│ │ 辩证结果：  │  │ ...        │            │
│ │   (包含匹配) │  └───────────┘            │
│ │ [查询]     │                             │
│ └───────────┘                             │
│ ┌─ 详情（复用Step 3总结控件）──────────┐   │
│ │ 【辩证信息】                         │   │
│ │ 四诊：...                            │   │
│ │ 辩证结果：肾阳虚、脾胃虚寒            │   │
│ │                                      │   │
│ │ 【施治信息】                         │   │
│ │ 处方：黄芪 30g、熟地黄 15g...        │   │
│ │ 用法：...                            │   │
│ └─────────────────────────────────────┘   │
│                                           │
│ [关闭] [导入处方]  ← 仅Step 2可用         │
└──────────────────────────────────────────┘
```

**"导入处方"按钮状态**：
- Step 1（辩证）：按钮禁用或隐藏（提示："辩证阶段不可导入"）
- Step 2（施治）：按钮可用

---

### ✅ A3-2: 查询条件 - 姓名 + 电话 + 辩证结果（模糊匹配）

**查询表单**：
```
姓名：[_________]
电话：[_________]
辩证结果：[_________] (包含匹配)

[查询] [重置]
```

**匹配规则示例**：
- 历史数据：`"肾阳虚、脾胃虚寒"`
- 输入：`"肾阳虚"` → ✅ 匹配
- 输入：`"脾胃"` → ✅ 匹配
- 输入：`"肝火"` → ❌ 不匹配

---

### ✅ A3-3: 病案列表显示 - 包含主诉

**列表每行显示**：
```
张** | 女 | 35岁 | 2025-01-20
诊断：肾阳虚、脾胃虚寒
主诉：腰膝酸软、畏寒肢冷
```

---

### ✅ A3-4: 处方导入 - 追加模式 + 自动取大值

**导入流程**：
```
点击"导入处方"
  → 关闭弹窗
  → 追加历史处方到当前编辑区
  → 检测重复药材
    → 系统自动保留较大剂量
    → 逐个弹窗提示重复项
```

**冲突提示弹窗**：
```
⚠️ 检测到重复药材

"红枣"已存在
（系统将自动保留较大剂量）

[确定]
```
→ 点击确定后，继续弹出下一个重复药材

**剂量处理规则**：
- 当前剂量：30g
- 导入剂量：20g
- 最终保留：30g（取大值）

---

### ❓ Q4: Step 3 进入条件和完成状态管理

**背景信息**：

根据之前确认的流程：
- Step 1（辩证）⟷ Step 2（施治）可动态切换
- Step 3（总结）用于病案汇总、打印报告

**你的建议**：
- 辩证和施治都设计"完成XX"按钮（手动触发）
- 未完成前不允许进入 Step 3
- 流程：1→3 或 1→2→3

**核心问题**：

**Q4-1: "完成辩证"和"完成施治"按钮的触发条件**

| 方案 | 触发逻辑 | 验证内容 | 优点 | 缺点 |
|-----|---------|---------|------|------|
| **A. 数据验证** | 必填字段都填写后<br/>按钮才可用 | • 四诊信息完整<br/>• 辩证结果非空 | • 保证数据完整性<br/>• 自动验证 | • 可能限制灵活性 |
| **B. 手动确认** | 按钮始终可用<br/>医生自行判断 | • 无强制验证<br/>• 医生负责 | • 灵活性高<br/>• 信任医生判断 | • 可能遗漏必填项 |
| **C. 提示验证** | 按钮可用<br/>点击时验证 | • 缺少必填项时弹窗提示<br/>• 可选择继续或返回 | • 平衡灵活性和规范性 | • 需要二次交互 |

---

### ✅ A4-1: "完成辩证"和"完成施治"按钮 - 数据验证

**触发条件**：
- 必填字段都填写后，按钮才可用
- 辩证必填：四诊信息、辩证结果
- 施治必填：处方药材列表（至少1味）、用法用量

**按钮状态**：
- 数据不完整：按钮禁用（灰色）
- 数据完整：按钮可用（蓝色）

---

### ✅ A4-2: Step 3 进入条件 - 提交完成验证

**进入条件**：

**情况1：不开处方**
```
Step 1（辩证）提交完成 → 可进入 Step 3
```

**情况2：需要开方**
```
Step 1（辩证）提交完成 + Step 2（施治）提交完成 → 可进入 Step 3
```

**验证逻辑**：
- ❌ 辩证未提交 → 禁止进入 Step 3，提示："请先完成辩证"
- ❌ 辩证已提交 + 选择"开处方" + 施治未提交 → 禁止进入 Step 3，提示："请先完成施治"
- ✅ 辩证已提交 + 不开处方 → 允许进入 Step 3
- ✅ 辩证已提交 + 施治已提交 → 允许进入 Step 3

**"提交完成"定义**：
- 点击"完成辩证"按钮 → 辩证提交完成
- 点击"完成施治"按钮 → 施治提交完成

---

### ✅ A4-3: Step 3 修改"是否开处方"后 - 重置完成状态与数据恢复

**场景1：从"否"改成"是"（重新开启处方）**
```
当前状态：辩证已完成 → 不开处方 → Step 3
修改决策：改为"开处方" → 进入 Step 2
```

**Step 2 界面行为**：
- **检测是否有已软删除的处方**：
  - ✅ 有（之前选择了软删除）：
    - 显示原处方数据（IsActive仍为false）
    - 医生可以在原基础上修改
  - ❌ 没有（之前选择了物理删除或从未开方）：
    - 显示空白处方
    - 从头开始编辑

**状态变化**：
- 辩证：已完成 → **未完成**
- 施治：**未完成**
- 需要：重新提交辩证 + 完成施治 → 才能返回 Step 3

**Step 2 提交"完成施治"后**：
```
点击"完成施治"
  → 如果是软删除的处方：Prescription.IsActive = true（恢复激活）
  → 如果是空白处方：创建新的Prescription
  → 完成施治状态 = 已完成
  → 可以进入 Step 3
```

---

**场景2：从"是"改成"否"（取消处方）**
```
当前状态：辩证已完成 → 施治已完成 → Step 3
修改决策：改为"不开处方"
```

**行为**：参考 A2 的处方删除策略（用户选择软删除或物理删除）

**设计原因**：
- 修改流程决策是重大变更
- 软删除允许后续恢复处方数据（避免重复录入）
- 物理删除适用于彻底放弃处方的场景
- 重置完成状态确保医生重新审视辩证内容
- 保证数据一致性和完整性

---

## 📊 Part B: 数据完整性验证

### ❓ Q5: 一诊断一处方规则验证漏洞

**背景信息**：

根据[现状分析报告](../../reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md)，发现以下问题：

**业务规则**：
- **RULE-2**：一诊断一处方（一个Consultation只能有一个Prescription）

**当前实现**：
```csharp
// PrescriptionService.CreatePrescriptionAsync
var existingPrescriptions = await _prescriptionRepository
    .GetPrescriptionsByConsultationIdAsync(consultationId);

if (existingPrescriptions.Any())
    throw new InvalidOperationException("该诊断已有处方，不能重复创建");
```

**潜在漏洞**：
1. ⚠️ **历史处方复制时未验证**：ClonePrescriptionAsync 可能绕过检查
2. ⚠️ **Desktop端直接查询**：可能通过Repository绕过聚合根验证
3. ⚠️ **验方导入时未验证**：ImportFormulaIntoPrescriptionAsync 可能重复创建

---

**核心问题**：

**Q5-1: 这个规则在当前业务场景下是否仍然有效？**

考虑到我们刚才讨论的新流程：
- Step 2（施治）可以反复编辑
- 支持"取消处方"然后重新开方
- 支持历史处方导入

**可能的情况**：

**场景A：严格的一诊断一处方**
```
一个Consultation对象 → 最多一个Prescription对象（包括已作废的）
```
- 取消处方后，如果要重新开方，必须"恢复"原处方而非创建新的
- 历史复制和验方导入都是"更新"现有处方，而非创建新处方

**场景B：宽松的一诊断一有效处方**
```
一个Consultation对象 → 可以有多个Prescription对象，但只有一个IsActive=true
```
- 取消处方后，可以创建新的处方（旧的IsActive=false）
- 历史复制和验方导入可以创建新处方（自动作废旧的）
- 保留完整的处方变更历史

---

### ✅ A5: 严格的一诊断一处方

**核心规则**：
```
一个Consultation对象 → 最多一个Prescription对象（严格1:1关系）
```

**实现约束**：

**1. 数据约束**：
- Prescription表的ConsultationId字段：Unique索引
- 确保数据库层面的唯一性

**2. 取消处方逻辑**（参考A2和A4-3）：
```
Step 3 → 修改"是否开处方"从"是"改成"否"
  → 用户选择：
    - 软删除：IsActive = false，保留数据
    - 物理删除：删除Prescription记录
```

**3. 重新开方逻辑**（参考A4-3）：
```
Step 3 → 修改"是否开处方"从"否"改成"是" → 进入Step 2
  → 检测已有Prescription：
    - 如果有（IsActive=false）：显示原数据，允许修改
    - 如果无：显示空白，从头开始
  → 提交"完成施治"后：
    - 软删除恢复：IsActive = true
    - 空白创建：创建新Prescription
```

**4. 当前患者历史处方导入逻辑**：
```
Step 2 → 查看当前患者历史（最近5次）→ 选择某次处方 → 导入
  → 如果当前已有Prescription：
    - 追加模式（参考A3-4）
    - 检测重复药材，自动取大值
  → 如果当前无Prescription：
    - 创建新Prescription，填充导入的药材
```

**5. 其他患者历史处方导入逻辑**（参考A3）：
```
Step 2 → 悬浮菜单 → 查询其他患者 → 导入处方
  → 同上：追加到现有Prescription或创建新Prescription
```

**6. 验方导入逻辑**：
```
Step 2 → 导入验方
  → 同上：追加到现有Prescription或创建新Prescription
```

**设计优势**：
- ✅ 数据模型简洁，真正的1:1关系
- ✅ 符合DDD聚合根设计（Consultation拥有唯一的Prescription）
- ✅ 软删除机制允许数据恢复，避免重复录入
- ✅ 物理删除选项满足彻底删除的需求
- ✅ 简化查询逻辑（每个Consultation最多一条Prescription）

**需要修复的漏洞**：
1. ✅ CreatePrescriptionAsync：增强检测逻辑，检查已存在的处方（包括IsActive=false的）
2. ✅ 历史导入和验方导入：改为追加/更新现有Prescription，而非创建新记录
3. ✅ 增加恢复逻辑：重新开方时，如果检测到软删除的处方，恢复IsActive=true

---

### ❓ Q6: Prescription表外键设计优化

**背景信息**：

根据[现状分析报告](../../reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md)，当前数据模型设计：

**当前实现**：
```sql
-- Consultation表（共享主键）
CREATE TABLE Consultations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,  -- 与MedicalCases.Id相同
    CONSTRAINT FK_Consultation_MedicalCase
        FOREIGN KEY (Id) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);

-- Prescriptions表（外键关联）
CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    MedicalCaseId UNIQUEIDENTIFIER NOT NULL,  -- ⚠️ 直接关联MedicalCase
    CONSTRAINT FK_Prescription_MedicalCase
        FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);
```

**问题分析**：

根据1:1:1关系（MedicalCase : Consultation : Prescription = 1:1:1）和聚合根设计：
- Consultation通过共享主键关联MedicalCase（Id == MedicalCaseId）
- Prescription通过MedicalCaseId关联MedicalCase（外键）
- **Prescription没有直接关联ConsultationId**

**潜在问题**：
1. 业务逻辑上，Prescription属于Consultation（一诊断一处方）
2. 数据模型上，Prescription直接跳过Consultation关联MedicalCase
3. 可能导致数据不一致（如果Consultation和Prescription的MedicalCaseId不匹配）

---

**核心问题**：Prescription表应该如何设计外键？

**方案A：当前方案（直接关联MedicalCase）**
```sql
Prescriptions.MedicalCaseId → MedicalCases.Id
```
**优点**：
- 查询简单（通过MedicalCaseId直接查Prescription）
- 符合聚合根设计（MedicalCase管理Prescription）

**缺点**：
- 跳过了Consultation，不符合业务语义（一诊断一处方）
- Prescription和Consultation之间没有显式关联

---

**方案B：共享主键链（推荐）**
```sql
-- Consultation共享主键
Consultation.Id == MedicalCase.Id

-- Prescription共享主键
Prescription.Id == Consultation.Id == MedicalCase.Id

CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CONSTRAINT FK_Prescription_Consultation
        FOREIGN KEY (Id) REFERENCES Consultations(Id) ON DELETE CASCADE
);
```

**优点**：
- ✅ 真正的1:1:1关系（三表共享主键）
- ✅ 符合业务语义（Prescription属于Consultation）
- ✅ 数据一致性强（级联删除自动维护）
- ✅ 简化查询（通过Id即可关联三表）

**缺点**：
- 需要迁移现有数据
- 需要调整Service层查询逻辑

---

**方案C：双外键（ConsultationId + MedicalCaseId）**
```sql
CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ConsultationId UNIQUEIDENTIFIER NOT NULL,
    MedicalCaseId UNIQUEIDENTIFIER NOT NULL,  -- 冗余但便于查询
    CONSTRAINT FK_Prescription_Consultation
        FOREIGN KEY (ConsultationId) REFERENCES Consultations(Id),
    CONSTRAINT FK_Prescription_MedicalCase
        FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id)
);
```

**优点**：
- 显式关联Consultation
- 保留MedicalCaseId便于查询

**缺点**：
- MedicalCaseId冗余（可通过Consultation.Id == MedicalCase.Id获取）
- 数据一致性风险（ConsultationId和MedicalCaseId可能不匹配）

---

---

### ✅ A6: 三表共享主键设计

**核心规则**：
```
MedicalCase.Id == Consultation.Id == Prescription.Id（1:1:1共享主键）
```

**数据库Schema调整**：
```sql
-- Consultation表（共享主键，保持不变）
CREATE TABLE Consultations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CONSTRAINT FK_Consultation_MedicalCase
        FOREIGN KEY (Id) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);

-- Prescriptions表（改为共享主键）
CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    -- ❌ 删除：MedicalCaseId字段
    -- ✅ 新增：共享主键外键
    CONSTRAINT FK_Prescription_Consultation
        FOREIGN KEY (Id) REFERENCES Consultations(Id) ON DELETE CASCADE
);

-- Unique索引（确保一诊断一处方）
CREATE UNIQUE INDEX UX_Prescription_Id ON Prescriptions(Id);
```

**Entity层调整**：
```csharp
public class PrescriptionEntity : AuditableEntity
{
    public Guid Id { get; set; }  // 共享主键，与Consultation.Id相同
    // ❌ 删除：public Guid MedicalCaseId { get; set; }
    // ❌ 删除：public Guid ConsultationId { get; set; }

    // 导航属性
    public ConsultationEntity Consultation { get; set; } = null!;
}
```

**Repository层调整**：
```csharp
// 通过共享主键查询
var prescription = await _context.Prescriptions
    .Include(p => p.Consultation)
        .ThenInclude(c => c.MedicalCase)
    .FirstOrDefaultAsync(p => p.Id == consultationId);
```

**设计优势**：
- ✅ 真正的1:1:1关系，数据模型简洁
- ✅ 符合业务语义（Prescription属于Consultation）
- ✅ 级联删除自动维护（删除Consultation自动删除Prescription）
- ✅ 简化查询逻辑（通过Id即可关联三表）
- ✅ 消除冗余字段（MedicalCaseId）

**迁移策略**（Long-term Epic）：
1. Phase 1：创建数据迁移脚本，将现有Prescription.Id改为对应的Consultation.Id
2. Phase 2：删除MedicalCaseId字段，调整外键约束
3. Phase 3：更新Service层查询逻辑
4. Phase 4：更新Desktop端ViewModel和Repository调用
5. Phase 5：全量测试，确保数据一致性

**注意**：此调整属于架构优化，不影响当前MVP功能，可作为独立Epic在MVP完成后实施。

---

## 🚀 Part C: 长期改进计划

**以下问题暂不深入讨论，作为未来Epic规划参考**：

1. **测试覆盖率提升**（当前0% → 目标60%+）
   - 核心业务规则单元测试
   - 聚合根行为测试
   - 集成测试

2. **代码重构**
   - PrescriptionService拆分（当前1008行）
   - 统一规则引擎（Specification模式）

3. **架构优化**
   - 共享主键vs外键一致性
   - 冗余字段清理

---

## 📝 讨论记录

| 问题 | 状态 | 决策 | 后续行动 |
|-----|------|------|---------|
| Q1: 三步流程 | ✅ 已确认 | 动态流程+开处方决策点 | ✅ 需求文档已完成 |
| Q2: 处方删除策略 | ✅ 已确认 | 二次确认+用户选择删除方式 | ✅ 需求文档已完成 |
| Q3: 其他患者查询 | ✅ 已确认 | 悬浮菜单+统一弹窗+追加导入 | ✅ 需求文档已完成 |
| Q4: Step 3进入条件 | ✅ 已确认 | 数据验证+提交完成+重置状态+数据恢复 | ✅ 需求文档已完成 |
| Q5: 一诊断一处方 | ✅ 已确认 | 严格1:1关系+软删除恢复机制 | ✅ 需求文档已完成 |
| Q6: 外键设计优化 | ✅ 已确认 | 三表共享主键（Long-term Epic） | ✅ 需求文档已完成 |

**需求文档**：`docs/requirements/medicalcase-consultation-prescription-enhancement-requirements.md` ✅

---

**维护说明**：每个问题讨论完成后，更新状态为✅已确认，并在"决策"列记录结论。
