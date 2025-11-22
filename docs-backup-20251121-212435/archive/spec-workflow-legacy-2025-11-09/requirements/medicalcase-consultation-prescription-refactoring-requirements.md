# 医案/诊断/处方模块重构需求文档

> **文档版本**: v1.0
> **创建日期**: 2025-10-26
> **状态**: ✅ 需求整合完成，待用户审查
> **优先级**: 高（MVP核心功能 + 架构合规）

---

## 📚 关联文档

### 前置文档
- **[架构重构方案](../explanation/design/medicalcase-consultation-prescription-architecture-refactoring-plan.md)** - 激进版架构重构方案（9个违规修复）
- **[需求讨论文档](../explanation/architecture/shared/medicalcase-consultation-prescription-enhancement-discussion.md)** - 业务需求深化讨论（Q1-Q6）
- **[功能增强需求](./medicalcase-consultation-prescription-enhancement-requirements.md)** - 业务功能需求（REQ-001至REQ-006）
- **[现状分析报告](../reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md)** - 20430行代码统计分析
- **[架构合规分析报告](../reports/architecture-compliance-analysis-2025-10-24.md)** - 9个架构违规检测

### 后续文档
- **[设计文档](../explanation/design/medicalcase-consultation-prescription-refactoring-design.md)** - 待生成（需求确认后）⭐
- **[差距分析文档](../explanation/design/medicalcase-consultation-prescription-gap-analysis.md)** - 待生成（设计完成后）⭐⭐

---

## 🎯 一、重构背景与目标

### 1.1 问题背景

#### 业务功能问题
当前系统已实现"三步看诊流程"（Issue #1567），但在实际使用中发现以下用户体验和数据完整性问题：

1. **流程灵活性不足**：辨证和施治阶段不能灵活切换，医生无法根据实际情况调整流程
2. **处方取消机制缺失**：无法在完成后取消或修改处方决策
3. **历史参考功能缺失**：无法查询和参考其他患者的诊疗方案
4. **数据一致性风险**：一诊断一处方规则验证不完整
5. **数据模型冗余**：Prescription表外键设计存在冗余

#### 架构合规问题
根据[架构合规分析报告](../reports/architecture-compliance-analysis-2025-10-24.md)，当前代码存在**9个架构违规项**：

| 违规ID | 位置 | 类型 | 严重性 | 影响 |
|--------|------|------|--------|------|
| V1 | ConsultationController.CompleteStep1 | Write绕过聚合根 | 🔴 Critical | Issue #1589 Phase 1实施但违规 |
| V2 | ConsultationService双Repository | 职责不清 | 🔴 Critical | 违反Single Responsibility |
| V3 | PrescriptionsController.PhysicalDelete | Write绕过聚合根 | 🟠 High | 破坏聚合根边界 |
| V4 | PrescriptionsController.SoftDelete | Write绕过聚合根 | 🟠 High | 破坏聚合根边界 |
| V5 | PrescriptionsController.ImportFormula | Write绕过聚合根 | 🔴 Critical | 破坏聚合根边界 |
| V6 | IConsultationRepository | 职责不清 | 🟡 Medium | 包含Write方法但应只读 |
| V7 | IPrescriptionRepository | 职责不清 | 🟡 Medium | 包含Write方法但应只读 |
| V8 | ConsultationDto.MedicalCase | 冗余导航 | 🟡 Medium | 增加序列化复杂度 |
| V9 | PrescriptionDto.MedicalCase | 冗余导航 | 🟡 Medium | 增加序列化复杂度 |

### 1.2 重构目标

**核心原则**：
- ✅ 不考虑向后兼容性，直接实施正确的v2.0三层架构
- ✅ 业务功能增强与架构合规同步实施
- ✅ 彻底清除所有架构违规，建立清晰的聚合根边界

**业务价值**：
- ✅ **提升医生工作效率**：灵活的流程切换和历史参考功能
- ✅ **降低数据录入成本**：处方导入和恢复机制避免重复录入
- ✅ **保障数据完整性**：严格的1:1:1关系和软删除机制
- ✅ **优化数据模型**：消除冗余字段，简化查询逻辑

**架构价值**：
- ✅ **清晰的聚合根边界**：所有Write操作通过MedicalCase聚合根
- ✅ **明确的Service职责**：ConsultationService/PrescriptionService改为Read-only
- ✅ **简化的Repository接口**：移除Write方法，符合Write/Read Layer分离
- ✅ **规范的DTO设计**：移除冗余导航属性，降低序列化复杂度

### 1.3 用户故事

**作为医生，我希望能够：**

1. 在辨证和施治阶段自由切换，根据实际情况调整诊疗流程
2. 在完成诊疗后，能够取消或重新开启处方决策
3. 查询其他患者的相似病例，参考诊断和处方方案
4. 导入历史处方时，系统自动处理重复药材
5. 在总结阶段，能够清晰看到完整的病案报告
6. 打印完整的病案报告（包括辨证和施治内容）

**作为开发者，我希望能够：**

1. 所有Write操作都通过MedicalCase聚合根，清晰的架构边界
2. ConsultationService/PrescriptionService职责明确，只负责Read Layer查询
3. Repository接口简洁，符合Read-only原则
4. Controller层API端点规范，符合RESTful和聚合根模式

---

## 📋 二、功能需求（业务层面）

### 需求1：动态流程与开处方决策点

**需求编号**：REQ-001
**优先级**：P0（高）
**关联决策**：A1（需求讨论文档）
**架构影响**：需要修复V1违规（ConsultationController.CompleteStep1）

#### 功能描述

1. **辨证和施治可动态切换**：
   - 用户可以在Step 1（辨证）和Step 2（施治）之间自由切换
   - 切换时数据不丢失，允许反复查看和调整

2. **开处方决策RadioBox**：
   - **UI位置**：Step 1（辨证）界面底部，"完成辩证"按钮上方
   - **选项**：
     - ○ 开处方（默认选中）
     - ○ 不开处方
   - **默认值**：开处方（选中状态）

3. **点击"完成辩证"或"下一步"时的验证逻辑**：
   - **场景1：选择"开处方" + 处方为空**
     - 提示："处方为空，如果不需要开处方请关闭处方按钮"
     - 停留在Step 1，不允许进入下一步
   - **场景2：选择"开处方" + 处方不为空**
     - 进入Step 2（施治）
     - 可以继续编辑处方或直接进入Step 3
   - **场景3：选择"不开处方"**
     - 直接进入Step 3（总结）

4. **Step 3可回看和修改**：
   - 在Step 3可以回看Step 1和Step 2的内容
   - 可以修改"是否开处方"决策：
     - 从"不开处方"改成"开处方"：重新进入Step 2
     - 从"开处方"改成"不开处方"：触发处方删除流程（见REQ-002）

#### 架构约束

**⚠️ 关键约束**：
- ❌ **禁止使用**：`POST /api/v1/consultations/{medicalCaseId}/complete-step1`（V1违规端点）
- ✅ **必须使用**：`POST /api/v1/medicalcases/{id}/complete-step1`（聚合根端点）
- ✅ **Write操作**：必须通过MedicalCaseController和MedicalCaseService
- ✅ **Read操作**：可以通过ConsultationController和ConsultationService

**API设计要求**：
```csharp
// ✅ 正确：通过聚合根实现
[HttpPost("{id}/complete-step1")]
public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(
    Guid id,
    [FromBody] CompleteStep1Request request)
{
    var result = await _medicalCaseService.CompleteStep1Async(id, request);
    return HandleServiceResult(result);
}
```

#### 验收标准

**功能验收**：
- [ ] Step 1界面底部有"是否开处方"RadioBox
- [ ] RadioBox默认选中"开处方"
- [ ] RadioBox位置在"完成辩证"按钮上方
- [ ] 选择"开处方" + 处方为空时，点击"完成辩证"弹出提示
- [ ] 选择"开处方" + 处方不为空时，点击"完成辩证"进入Step 2
- [ ] 选择"不开处方"时，点击"完成辩证"直接进入Step 3
- [ ] Step 1和Step 2之间可以自由切换，数据不丢失
- [ ] Step 3可以回看Step 1和Step 2的内容
- [ ] Step 3可以修改"是否开处方"决策

**架构验收**：
- [ ] ConsultationController.CompleteStep1端点已删除（V1修复）
- [ ] MedicalCaseController.CompleteStep1端点已实现
- [ ] 所有Step 1完成逻辑通过MedicalCaseService
- [ ] 运行lybtzyzs-arch-compliance检查：V1违规消除

---

### 需求2：处方删除策略

**需求编号**：REQ-002
**优先级**：P0（高）
**关联决策**：A2（需求讨论文档）
**架构影响**：需要修复V3、V4违规（PrescriptionsController删除端点）

#### 功能描述

在Step 3修改"是否开处方"从"是"改成"否"时，弹出确认对话框：

**对话框内容**：
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

#### 业务规则

- 默认选中"软删除"（安全优先）
- 用户可主动选择"物理删除"
- 软删除：`Prescription.IsActive = false, CancelledAt = DateTime.Now`
- 物理删除：`DbContext.Remove(prescription)`

#### 架构约束

**⚠️ 关键约束**：
- ❌ **禁止使用**：
  - `DELETE /api/v1/prescriptions/{id}`（V3违规端点）
  - `DELETE /api/v1/prescriptions/{id}/soft`（V4违规端点）
- ✅ **必须使用**：
  - `DELETE /api/v1/medicalcases/{id}/prescription/clear`（聚合根端点，软删除）
  - 物理删除通过MedicalCase聚合根级联删除实现

**API设计要求**：
```csharp
// ✅ 正确：通过聚合根实现清空处方
[HttpDelete("{id}/prescription/clear")]
public async Task<ActionResult<ApiResponse>> ClearPrescription(Guid id)
{
    var result = await _medicalCaseService.ClearPrescriptionAsync(id);
    return HandleServiceResult(result);
}
```

#### 验收标准

**功能验收**：
- [ ] 修改"是否开处方"从"是"到"否"时弹出确认对话框
- [ ] 对话框包含软删除和物理删除两个选项
- [ ] 默认选中软删除
- [ ] 软删除：处方数据保留但标记为已作废
- [ ] 物理删除：处方数据永久删除
- [ ] 用户可以取消操作

**架构验收**：
- [ ] PrescriptionsController.PhysicalDelete端点已删除（V3修复）
- [ ] PrescriptionsController.SoftDelete端点已删除（V4修复）
- [ ] MedicalCaseController.ClearPrescription端点已实现
- [ ] 所有处方删除逻辑通过MedicalCaseService
- [ ] 运行lybtzyzs-arch-compliance检查：V3、V4违规消除

---

### 需求3：其他患者病案查询功能

**需求编号**：REQ-003
**优先级**：P1（中）
**关联决策**：A3-1, A3-2, A3-3, A3-4（需求讨论文档）
**架构影响**：需要修复V5违规（PrescriptionsController.ImportFormula）

#### 功能描述

1. **查询入口**：
   - Step 1（辨证）：右下角悬浮菜单
   - Step 2（施治）：右下角悬浮菜单
   - Step 3（总结）：不需要

2. **弹窗结构**：
   - 左侧：查询条件 + 病案列表
   - 右侧：病案详情（复用Step 3的总结控件）

3. **查询条件**：
   - 姓名（模糊匹配）
   - 电话（模糊匹配）
   - 辩证结果（包含匹配，如输入"肾阳虚"可匹配"肾阳虚、脾胃虚寒"）

4. **病案列表显示**：
```
张** | 女 | 35岁 | 2025-10-24
诊断：肾阳虚、脾胃虚寒
主诉：腰膝酸软、畏寒肢冷
```

5. **导入处方功能**：
   - Step 1（辨证）：按钮禁用或隐藏（提示："辨证阶段不可导入"）
   - Step 2（施治）：按钮可用，点击后导入处方

6. **处方导入逻辑**：
   - 追加模式：导入的处方追加到当前编辑区
   - 重复检测：检测到重复药材时，逐个弹窗提示
   - 剂量处理：自动保留较大剂量

**冲突提示弹窗**：
```
⚠️ 检测到重复药材

"红枣"已存在
（系统将自动保留较大剂量）

[确定]
```

#### 架构约束

**⚠️ 关键约束**：
- ❌ **禁止使用**：`POST /api/v1/prescriptions/{prescriptionId}/import-formula/{formulaId}`（V5违规端点）
- ✅ **必须使用**：`POST /api/v1/medicalcases/{id}/prescription/import-formula/{formulaId}`（聚合根端点）
- ✅ **Write操作**：导入处方必须通过MedicalCaseController和MedicalCaseService
- ✅ **Read操作**：查询病案列表可以通过ConsultationController

**API设计要求**：
```csharp
// ✅ 正确：通过聚合根实现导入配方
[HttpPost("{id}/prescription/import-formula/{formulaId}")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(
    Guid id,
    Guid formulaId)
{
    var result = await _medicalCaseService.ImportFormulaIntoPrescriptionAsync(id, formulaId);
    return HandleServiceResult(result);
}
```

#### 验收标准

**功能验收**：
- [ ] Step 1和Step 2的右下角有悬浮菜单按钮
- [ ] 点击悬浮菜单打开查询弹窗
- [ ] 弹窗包含查询条件、病案列表、病案详情三个区域
- [ ] 查询条件：姓名、电话、辩证结果（包含匹配）
- [ ] 病案列表显示：患者基本信息、诊断、主诉
- [ ] 病案详情复用Step 3的总结控件
- [ ] Step 1时"导入处方"按钮禁用或隐藏
- [ ] Step 2时"导入处方"按钮可用
- [ ] 导入处方时追加到当前编辑区
- [ ] 检测到重复药材时逐个弹窗提示
- [ ] 重复药材自动保留较大剂量

**架构验收**：
- [ ] PrescriptionsController.ImportFormula端点已删除（V5修复）
- [ ] MedicalCaseController.ImportFormulaIntoPrescription端点已实现
- [ ] 所有处方导入逻辑通过MedicalCaseService
- [ ] 运行lybtzyzs-arch-compliance检查：V5违规消除

---

### 需求4：Step 3进入条件与完成状态管理

**需求编号**：REQ-004
**优先级**：P0（高）
**关联决策**：A4-1, A4-2, A4-3（需求讨论文档）
**架构影响**：涉及V1修复（CompleteStep1逻辑）

#### 功能描述

1. **"完成辩证"和"完成施治"按钮**：
   - 触发条件：必填字段都填写后，按钮才可用
   - 辩证必填：四诊信息、辩证结果
   - 施治必填：处方药材列表（至少1味）、用法用量
   - 按钮状态：
     - 数据不完整：按钮禁用（灰色）
     - 数据完整：按钮可用（蓝色）

2. **Step 3进入条件**：
   - **情况1：不开处方**
     ```
     Step 1（辩证）提交完成 → 可进入Step 3
     ```
   - **情况2：需要开方**
     ```
     Step 1（辩证）提交完成 + Step 2（施治）提交完成 → 可进入Step 3
     ```
   - **验证逻辑**：
     - ❌ 辩证未提交 → 禁止进入Step 3，提示："请先完成辩证"
     - ❌ 辩证已提交 + 选择"开处方" + 施治未提交 → 禁止进入Step 3，提示："请先完成施治"
     - ✅ 辩证已提交 + 不开处方 → 允许进入Step 3
     - ✅ 辩证已提交 + 施治已提交 → 允许进入Step 3

3. **Step 3修改"是否开处方"后的状态变化**：

   **场景1：从"否"改成"是"（重新开启处方）**
   ```
   当前状态：辩证已完成 → 不开处方 → Step 3
   修改决策：改为"开处方" → 进入Step 2
   ```
   - **Step 2界面行为**：
     - 检测是否有已软删除的处方：
       - ✅ 有（之前选择了软删除）：显示原处方数据（IsActive仍为false），医生可以在原基础上修改
       - ❌ 没有（之前选择了物理删除或从未开方）：显示空白处方，从头开始编辑
   - **状态变化**：
     - 辩证：已完成 → **未完成**
     - 施治：**未完成**
     - 需要：重新提交辩证 + 完成施治 → 才能返回Step 3
   - **Step 2提交"完成施治"后**：
     ```
     点击"完成施治"
       → 如果是软删除的处方：Prescription.IsActive = true（恢复激活）
       → 如果是空白处方：创建新的Prescription
       → 完成施治状态 = 已完成
       → 可以进入Step 3
     ```

   **场景2：从"是"改成"否"（取消处方）**
   ```
   当前状态：辩证已完成 → 施治已完成 → Step 3
   修改决策：改为"不开处方"
   ```
   - **行为**：参考REQ-002的处方删除策略（用户选择软删除或物理删除）

#### 架构约束

**⚠️ 关键约束**：
- ✅ **CompleteStep1逻辑**：必须通过MedicalCaseService实现
- ✅ **状态持久化**：使用数据库字段（Consultation.CompletedAt, Prescription.CompletedAt）
- ✅ **软删除恢复**：通过MedicalCaseService实现IsActive恢复逻辑

#### 验收标准

**功能验收**：
- [ ] "完成辩证"按钮：必填字段完整后才可用
- [ ] "完成施治"按钮：必填字段完整后才可用
- [ ] 辩证未提交时无法进入Step 3
- [ ] 选择"开处方"但施治未提交时无法进入Step 3
- [ ] 辩证已提交且不开处方时可以进入Step 3
- [ ] 辩证和施治都已提交时可以进入Step 3
- [ ] Step 3修改"是否开处方"从"否"到"是"时：
  - [ ] 检测已软删除的处方并显示原数据
  - [ ] 检测不到处方时显示空白
  - [ ] 辩证和施治状态重置为未完成
  - [ ] 提交"完成施治"后恢复或创建处方
- [ ] Step 3修改"是否开处方"从"是"到"否"时触发REQ-002流程

**架构验收**：
- [ ] CompleteStep1逻辑通过MedicalCaseService实现
- [ ] 状态持久化使用数据库字段
- [ ] 软删除恢复逻辑正确实现

---

### 需求5：严格的一诊断一处方规则

**需求编号**：REQ-005
**优先级**：P0（高）
**关联决策**：A5（需求讨论文档）
**架构影响**：涉及V3、V4、V5违规修复

#### 功能描述

**核心规则**：
```
一个Consultation对象 → 最多一个Prescription对象（严格1:1关系）
```

**实现约束**：

1. **数据约束**：
   - Prescription表的ConsultationId字段：Unique索引
   - 确保数据库层面的唯一性

2. **取消处方逻辑**（参考REQ-002和REQ-004）：
   - Step 3 → 修改"是否开处方"从"是"改成"否"
   - 用户选择：软删除（IsActive=false）或物理删除（删除记录）

3. **重新开方逻辑**（参考REQ-004）：
   - Step 3 → 修改"是否开处方"从"否"改成"是" → 进入Step 2
   - 检测已有Prescription：
     - 如果有（IsActive=false）：显示原数据，允许修改
     - 如果无：显示空白，从头开始
   - 提交"完成施治"后：
     - 软删除恢复：IsActive = true
     - 空白创建：创建新Prescription

4. **当前患者历史处方导入逻辑**：
   - Step 2 → 查看当前患者历史（最近5次）→ 选择某次处方 → 导入
   - 如果当前已有Prescription：追加模式（参考REQ-003），检测重复药材，自动取大值
   - 如果当前无Prescription：创建新Prescription，填充导入的药材

5. **其他患者历史处方导入逻辑**（参考REQ-003）：
   - Step 2 → 悬浮菜单 → 查询其他患者 → 导入处方
   - 同上：追加到现有Prescription或创建新Prescription

6. **验方导入逻辑**：
   - Step 2 → 导入验方
   - 同上：追加到现有Prescription或创建新Prescription

#### 需要修复的漏洞

1. CreatePrescriptionAsync：增强检测逻辑，检查已存在的处方（包括IsActive=false的）
2. 历史导入和验方导入：改为追加/更新现有Prescription，而非创建新记录
3. 增加恢复逻辑：重新开方时，如果检测到软删除的处方，恢复IsActive=true

#### 架构约束

**⚠️ 关键约束**：
- ✅ **所有处方创建/更新/删除**：必须通过MedicalCaseService
- ✅ **Unique约束**：数据库层面确保一诊断一处方
- ✅ **导入逻辑**：改为追加/更新，而非创建新记录

#### 验收标准

**功能验收**：
- [ ] 数据库Prescription表有ConsultationId的Unique索引
- [ ] 一个Consultation最多只能有一个Prescription记录
- [ ] 取消处方时支持软删除和物理删除
- [ ] 重新开方时检测软删除的处方并恢复
- [ ] 历史处方导入时追加到现有Prescription
- [ ] 验方导入时追加到现有Prescription
- [ ] 所有导入操作检测重复药材并自动取大值
- [ ] CreatePrescriptionAsync检测已存在的处方（包括IsActive=false）

**架构验收**：
- [ ] 所有处方Write操作通过MedicalCaseService
- [ ] 数据库Unique约束生效
- [ ] 导入逻辑改为追加/更新模式

---

### 需求6：三表共享主键设计（Long-term Epic）

**需求编号**：REQ-006
**优先级**：P2（低，MVP完成后实施）
**关联决策**：A6（需求讨论文档）
**架构影响**：数据库Schema重构

#### 功能描述

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

#### 迁移策略

1. Phase 1：创建数据迁移脚本，将现有Prescription.Id改为对应的Consultation.Id
2. Phase 2：删除MedicalCaseId字段，调整外键约束
3. Phase 3：更新Service层查询逻辑
4. Phase 4：更新Desktop端ViewModel和Repository调用
5. Phase 5：全量测试，确保数据一致性

#### 验收标准

- [ ] 三表共享主键：MedicalCase.Id == Consultation.Id == Prescription.Id
- [ ] Prescription表不再包含MedicalCaseId字段
- [ ] Prescription表通过Id外键关联Consultations表
- [ ] 数据迁移脚本测试通过，无数据丢失
- [ ] Service层查询逻辑更新完成
- [ ] Desktop端ViewModel和Repository调用更新完成
- [ ] 全量测试通过

**注意**：此需求属于架构优化，不影响当前MVP功能，可作为独立Epic在MVP完成后实施。

---

## 🏗️ 三、架构需求（技术层面）

### 架构需求1：清理违规API端点

**需求编号**：ARCH-001
**优先级**：P0（高）
**关联违规**：V1, V3, V4, V5
**修复策略**：直接删除，不保留向后兼容

#### 删除清单

**ConsultationController清理**：
```csharp
// ❌ 直接删除（不保留）
// POST /api/v1/consultations/{medicalCaseId}/complete-step1
public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(...)
```

**PrescriptionsController清理**：
```csharp
// ❌ 直接删除以下3个端点
// DELETE /api/v1/prescriptions/{id}
public async Task<ActionResult<ApiResponse>> PhysicalDelete(Guid id)

// DELETE /api/v1/prescriptions/{id}/soft
public async Task<ActionResult<ApiResponse>> SoftDelete(Guid id)

// POST /api/v1/prescriptions/{prescriptionId}/import-formula/{formulaId}
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(...)
```

**已标记Obsolete的端点处理**：
```csharp
// ✅ 已正确标记为Obsolete，保持现状
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案（含处方）。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add(...)

[Obsolete("请使用 PUT /api/medicalcases/{id}/prescription 更新处方信息。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(...)
```

#### 验收标准

- [ ] ConsultationController.CompleteStep1端点已删除
- [ ] PrescriptionsController.PhysicalDelete端点已删除
- [ ] PrescriptionsController.SoftDelete端点已删除
- [ ] PrescriptionsController.ImportFormula端点已删除
- [ ] 已标记Obsolete的端点保持不变
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] Swagger文档更新，不再显示删除的端点

---

### 架构需求2：重构Service层职责边界

**需求编号**：ARCH-002
**优先级**：P0（高）
**关联违规**：V2（ConsultationService双Repository）
**修复策略**：ConsultationService/PrescriptionService改为Read-only

#### ConsultationService重构

**当前问题**：
```csharp
// ❌ 职责不清：同时依赖两个Repository
public class ConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMedicalCaseRepository _medicalCaseRepository; // 移除
}
```

**重构方案**：
```csharp
// ✅ 明确职责：只负责Read Layer查询
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultationService> _logger;

    // 构造函数：移除IMedicalCaseRepository
    public ConsultationService(
        IConsultationRepository repository,
        IMapper mapper,
        ILogger<ConsultationService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    // ✅ 保留Read-only方法
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(...)
    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(...)
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(...)
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(...)

    // ❌ 删除所有Write方法（迁移到MedicalCaseService）
    // CreateAsync() - 删除
    // UpdateAsync() - 删除
    // DeleteAsync() - 删除
    // CompleteStep1Async() - 删除
}
```

#### PrescriptionService重构

**类似ConsultationService重构**：
- 移除IMedicalCaseRepository依赖
- 保留Read-only方法
- 删除所有Write方法

#### MedicalCaseService扩展

**新增Write方法**（迁移自ConsultationService/PrescriptionService）：

```csharp
// ✅ 所有Write操作通过MedicalCase聚合根
public class MedicalCaseService
{
    // REQ-001: 完成辩证步骤
    public async Task<ServiceResult<ConsultationStepDto>> CompleteStep1Async(
        Guid medicalCaseId,
        CompleteStep1Request request)

    // REQ-001: 重置诊疗步骤
    public async Task<ServiceResult> ResetConsultationStepsAsync(Guid medicalCaseId)

    // REQ-002: 清空处方（替代Delete）
    public async Task<ServiceResult> ClearPrescriptionAsync(Guid medicalCaseId)

    // REQ-003: 从配方导入处方
    public async Task<ServiceResult<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
        Guid medicalCaseId,
        Guid formulaId)
}
```

#### 验收标准

- [ ] ConsultationService移除IMedicalCaseRepository依赖
- [ ] ConsultationService只包含Read-only方法
- [ ] PrescriptionService移除IMedicalCaseRepository依赖
- [ ] PrescriptionService只包含Read-only方法
- [ ] MedicalCaseService新增所有Write方法
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行lybtzyzs-arch-compliance检查：V2违规消除

---

### 架构需求3：重构Repository接口

**需求编号**：ARCH-003
**优先级**：P0（高）
**关联违规**：V6（IConsultationRepository）、V7（IPrescriptionRepository）
**修复策略**：移除Write方法，改为Read-only接口

#### IConsultationRepository简化

**当前问题**：接口职责不清，包含Write方法

**重构方案**：
```csharp
// ✅ 明确为Read-only Repository
public interface IConsultationRepository
{
    // Read-only查询方法
    Task<Consultation?> GetByIdAsync(Guid id);
    Task<Consultation?> GetByIdWithDetailsAsync(Guid id);
    Task<Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(int page, int pageSize, string? keyword = null);
    Task<IEnumerable<Consultation>> FindAsync(Expression<Func<Consultation, bool>> predicate);

    // ❌ 删除Write方法（不再需要）
    // Task<Consultation> AddAsync(Consultation entity) - 删除
    // Task<Consultation> UpdateAsync(Consultation entity) - 删除
    // Task<bool> DeleteAsync(Guid id) - 删除
}
```

**理由**：
- Consultation的Write操作通过MedicalCaseRepository实现
- ConsultationRepository只负责独立查询场景
- 符合Write/Read Layer分离原则

#### IPrescriptionRepository简化

**类似IConsultationRepository重构**

#### IMedicalCaseRepository无需改动

**当前设计已正确**：
- 包含完整的CRUD方法（聚合根Repository）
- GetByIdWithDetailsAsync预加载Consultation/Prescription
- 符合v2.0架构

#### 验收标准

- [ ] IConsultationRepository只包含Read-only方法
- [ ] IConsultationRepository不包含AddAsync/UpdateAsync/DeleteAsync
- [ ] IPrescriptionRepository只包含Read-only方法
- [ ] IPrescriptionRepository不包含AddAsync/UpdateAsync/DeleteAsync
- [ ] IMedicalCaseRepository保持不变
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行lybtzyzs-arch-compliance检查：V6、V7违规消除

---

### 架构需求4：清理DTO冗余属性

**需求编号**：ARCH-004
**优先级**：P1（中）
**关联违规**：V8（ConsultationDto.MedicalCase）、V9（PrescriptionDto.MedicalCase）
**修复策略**：移除冗余导航属性，保留必要的计算属性

#### ConsultationDto清理

**当前问题**：
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }

    // ❌ 冗余导航属性
    public virtual MedicalCaseDto? MedicalCase { get; set; }

    // ✅ 保留计算属性（业务需要）
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
}
```

**重构方案**：
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }

    // ✅ 保留必要的计算属性
    public string PatientName { get; set; }
    public string DoctorName { get; set; }

    // 诊疗字段...
    public string? ChiefComplaint { get; set; }
    public string? TCMDiagnosis { get; set; }
    // ...

    // ❌ 移除MedicalCase导航属性
}
```

**AutoMapper配置调整**：
```csharp
CreateMap<Consultation, ConsultationDto>()
    .ForMember(dest => dest.PatientName,
        opt => opt.MapFrom(src => src.MedicalCase.PatientName))
    .ForMember(dest => dest.DoctorName,
        opt => opt.MapFrom(src => src.MedicalCase.DoctorName));
```

#### PrescriptionDto清理

**类似ConsultationDto重构**

#### 验收标准

- [ ] ConsultationDto不再包含MedicalCase导航属性
- [ ] ConsultationDto保留PatientName/DoctorName计算属性
- [ ] PrescriptionDto不再包含MedicalCase导航属性
- [ ] PrescriptionDto保留必要的计算属性
- [ ] AutoMapper配置更新完成
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行lybtzyzs-arch-compliance检查：V8、V9违规消除

---

### 架构需求5：MedicalCaseController扩展

**需求编号**：ARCH-005
**优先级**：P0（高）
**关联需求**：REQ-001至REQ-005
**修复策略**：新增聚合根Write端点，替代违规端点

#### 新增聚合根Write端点

**所有功能通过MedicalCaseController实现**：

```csharp
/// <summary>
/// 完成辩证步骤（Step 1）
/// REQ-001 - 架构合规版本
/// </summary>
[HttpPost("{id}/complete-step1")]
[ProducesResponseType(typeof(ApiResponse<ConsultationStepDto>), 200)]
public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(
    Guid id,
    [FromBody] CompleteStep1Request request)
{
    var result = await _medicalCaseService.CompleteStep1Async(id, request);
    return HandleServiceResult(result);
}

/// <summary>
/// 重置诊疗步骤
/// REQ-001 - 架构合规版本
/// </summary>
[HttpPut("{id}/reset-consultation-steps")]
[ProducesResponseType(typeof(ApiResponse), 200)]
public async Task<ActionResult<ApiResponse>> ResetConsultationSteps(Guid id)
{
    var result = await _medicalCaseService.ResetConsultationStepsAsync(id);
    return HandleServiceResult(result);
}

/// <summary>
/// 清空处方内容
/// REQ-002 - 架构合规版本（替代Delete）
/// </summary>
[HttpDelete("{id}/prescription/clear")]
[ProducesResponseType(typeof(ApiResponse), 200)]
public async Task<ActionResult<ApiResponse>> ClearPrescription(Guid id)
{
    var result = await _medicalCaseService.ClearPrescriptionAsync(id);
    return HandleServiceResult(result);
}

/// <summary>
/// 从配方导入处方
/// REQ-003 - 架构合规版本
/// </summary>
[HttpPost("{id}/prescription/import-formula/{formulaId}")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(
    Guid id,
    Guid formulaId)
{
    var result = await _medicalCaseService.ImportFormulaIntoPrescriptionAsync(id, formulaId);
    return HandleServiceResult(result);
}
```

#### 验收标准

- [ ] MedicalCaseController新增CompleteStep1端点
- [ ] MedicalCaseController新增ResetConsultationSteps端点
- [ ] MedicalCaseController新增ClearPrescription端点
- [ ] MedicalCaseController新增ImportFormulaIntoPrescription端点
- [ ] 所有端点都调用MedicalCaseService
- [ ] Swagger文档更新，显示新端点
- [ ] 编译通过（0 errors, 0 warnings）

---

## 📊 四、实施顺序与依赖关系

### 实施策略

**核心原则**：
- ✅ 从底层到上层（Repository → Service → Controller → Client）
- ✅ 先修复架构违规，再实施业务功能
- ✅ 每个阶段完成后立即验证

### Step 1: Server端架构重构（8-10小时）

**顺序**（从底层到上层）：

1. **Repository接口清理**（1小时）- ARCH-003
   - 修改IConsultationRepository/IPrescriptionRepository
   - 移除Write方法声明
   - **验证**：编译通过

2. **DTO清理**（1小时）- ARCH-004
   - 移除ConsultationDto/PrescriptionDto冗余属性
   - 更新AutoMapper配置
   - **验证**：编译通过

3. **Service层重构**（3-4小时）- ARCH-002
   - ConsultationService移除IMedicalCaseRepository
   - PrescriptionService移除IMedicalCaseRepository
   - MedicalCaseService新增Write方法（REQ-001至REQ-005）
   - **验证**：编译通过 + 单元测试通过

4. **Controller层清理**（3-4小时）- ARCH-001 + ARCH-005
   - ConsultationController删除CompleteStep1（V1）
   - PrescriptionsController删除3个违规端点（V3, V4, V5）
   - MedicalCaseController新增5个端点
   - **验证**：编译通过 + Swagger文档正确

**阶段验收**：
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行lybtzyzs-arch-compliance检查：所有违规消除
- [ ] 单元测试通过率 ≥60%
- [ ] Swagger文档更新完成

---

### Step 2: Client端同步修改（4-6小时）

**顺序**：

1. **ApiClient层修改**（2-3小时）
   - MedicalCaseApiClient新增方法（对应5个新端点）
   - ConsultationApiClient移除废弃方法调用
   - PrescriptionApiClient移除废弃方法调用
   - **验证**：编译通过

2. **ViewModel层修改**（2-3小时）
   - ConsultationFormViewModel修改CompleteStep1调用路径
   - PrescriptionFormViewModel修改导入配方调用路径
   - **验证**：编译通过

**阶段验收**：
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] Desktop Client启动成功
- [ ] 所有ViewModel正确调用新API

---

### Step 3: 业务功能实施（9-14天）

**顺序**（按优先级）：

1. **REQ-001 + REQ-004**（4-6天）- 动态流程 + Step 3管理
   - 实施RadioBox控件（1天）
   - 实施"完成辩证"/"完成施治"按钮逻辑（2天）
   - 实施Step 3进入条件验证（1天）
   - 实施状态恢复逻辑（1-2天）
   - **验证**：手动测试 + 运行时验证

2. **REQ-002**（1-2天）- 处方删除策略
   - 实施确认对话框（0.5天）
   - 实施软删除/物理删除逻辑（1天）
   - 实施MedicalCaseService.ClearPrescriptionAsync（0.5天）
   - **验证**：手动测试 + 数据库验证

3. **REQ-005**（1-2天）- 一诊断一处方规则
   - 实施数据库Unique索引（0.5天）
   - 实施CreatePrescriptionAsync检测逻辑（1天）
   - 实施导入逻辑追加模式（0.5天）
   - **验证**：单元测试 + 集成测试

4. **REQ-003**（3-4天）- 其他患者查询
   - 实施悬浮菜单（0.5天）
   - 实施查询弹窗UI（1天）
   - 实施查询逻辑（1天）
   - 实施处方导入逻辑（1-1.5天）
   - **验证**：手动测试 + 运行时验证

**阶段验收**：
- [ ] 所有功能需求验收标准通过
- [ ] 运行时验证通过
- [ ] 数据库状态正确
- [ ] 用户体验符合预期

---

### Step 4: 编译与验证（2-3小时）

1. **编译验证**（0.5小时）
   - 0 errors, 0 warnings
   - 所有废弃API端点已删除
   - 所有Repository接口符合Read-only定义

2. **运行时验证**（1-1.5小时）
   - 启动WebAPI，验证Swagger文档
   - 启动Desktop Client，验证诊疗工作流
   - 测试所有REQ-001至REQ-005功能

3. **数据库验证**（0.5-1小时）
   - 确认CompletedAt字段正确保存
   - 确认软删除/物理删除正确执行
   - 确认Unique约束生效

**阶段验收**：
- [ ] 编译标准：0 errors, 0 warnings
- [ ] 架构合规标准：lybtzyzs-arch-compliance检查通过
- [ ] 功能标准：所有业务功能正常工作
- [ ] 数据标准：数据一致性和完整性验证通过

---

### Step 5: 文档更新（1-2小时）

1. **更新设计文档**（0.5-1小时）
   - 创建新的设计文档（基于本需求文档）
   - 更新API文档
   - 更新数据库Schema文档

2. **更新报告**（0.5-1小时）
   - 更新architecture-compliance-analysis-2025-10-24.md（标记为已修复）
   - 创建"架构重构复盘"文档

**阶段验收**：
- [ ] 设计文档创建完成
- [ ] 架构合规性报告更新完成
- [ ] 所有文档链接正确

---

## 📊 五、总工作量估算

| 阶段 | 工作量 | 说明 |
|-----|--------|------|
| Server端架构重构 | 8-10小时 | Repository + Service + Controller |
| Client端同步修改 | 4-6小时 | ApiClient + ViewModel |
| 业务功能实施 | 9-14天 | REQ-001至REQ-005（按优先级） |
| 编译与验证 | 2-3小时 | 编译 + 运行时 + 数据库 |
| 文档更新 | 1-2小时 | 设计文档 + 报告 |
| **总计（不含REQ-006）** | **10-15天** | **MVP阶段完整实施** |
| REQ-006（Long-term） | 5-7天 | 三表共享主键（独立Epic） |
| **总计（包含REQ-006）** | **15-22天** | **完整实施** |

---

## ✅ 六、验收标准总览

### 6.1 功能完整性

| 需求编号 | 需求名称 | 优先级 | 验收通过率 |
|---------|---------|--------|----------|
| REQ-001 | 动态流程与开处方决策点 | P0 | 待测试 |
| REQ-002 | 处方删除策略 | P0 | 待测试 |
| REQ-003 | 其他患者病案查询功能 | P1 | 待测试 |
| REQ-004 | Step 3进入条件与完成状态管理 | P0 | 待测试 |
| REQ-005 | 严格的一诊断一处方规则 | P0 | 待测试 |
| REQ-006 | 三表共享主键设计 | P2 | 待实施 |

### 6.2 架构合规性

| 违规ID | 位置 | 类型 | 修复需求 | 验收状态 |
|--------|------|------|---------|---------|
| V1 | ConsultationController.CompleteStep1 | Write绕过聚合根 | ARCH-001 | 待修复 |
| V2 | ConsultationService双Repository | 职责不清 | ARCH-002 | 待修复 |
| V3 | PrescriptionsController.PhysicalDelete | Write绕过聚合根 | ARCH-001 | 待修复 |
| V4 | PrescriptionsController.SoftDelete | Write绕过聚合根 | ARCH-001 | 待修复 |
| V5 | PrescriptionsController.ImportFormula | Write绕过聚合根 | ARCH-001 | 待修复 |
| V6 | IConsultationRepository | 职责不清 | ARCH-003 | 待修复 |
| V7 | IPrescriptionRepository | 职责不清 | ARCH-003 | 待修复 |
| V8 | ConsultationDto.MedicalCase | 冗余导航 | ARCH-004 | 待修复 |
| V9 | PrescriptionDto.MedicalCase | 冗余导航 | ARCH-004 | 待修复 |

### 6.3 测试覆盖要求

- 单元测试覆盖率：≥60%（核心业务逻辑）
- 集成测试：关键流程端到端测试
- 手动测试：完整的用户场景测试

### 6.4 编译质量标准

- ✅ 编译通过：0 errors, 0 warnings
- ✅ 所有废弃API端点已删除
- ✅ 所有Repository接口符合Read-only定义

### 6.5 架构合规标准

- ✅ 运行lybtzyzs-arch-compliance检查：0违规
- ✅ 所有Write操作通过MedicalCase聚合根
- ✅ ConsultationService/PrescriptionService只有Read方法

### 6.6 功能标准

- ✅ 所有REQ-001至REQ-005功能正常工作
- ✅ CompletedAt字段正确保存到数据库
- ✅ 软删除/物理删除逻辑正确执行
- ✅ Client端诊疗工作流无异常

### 6.7 文档标准

- ✅ 设计文档创建完成
- ✅ 架构合规性报告标记为"已修复"
- ✅ API文档更新完成

---

## 🔗 七、关联Issues

### 7.1 已有Issues

- #1567：三步看诊流程（已完成，需要增强）
- #1423：处方业务规则（RULE-2, RULE-3）
- #1563：MedicalCase聚合根重构（已完成）
- #1589：Epic架构违规修复（架构重构方案来源）

### 7.2 需要创建的Issues

- [ ] Epic：医案/诊断/处方模块重构（包含REQ-001至REQ-005 + ARCH-001至ARCH-005）
- [ ] Epic：三表共享主键架构优化（REQ-006，Long-term）

---

## 📝 八、备注

### 8.1 设计流程改进

根据Epic #1589的教训，在CLAUDE.md中新增**设计阶段架构合规性检查（强制环节）**：

**适用场景**：
- 所有新功能设计（Epic/Feature Issue）
- 所有架构调整（重构/模块拆分）
- 所有API端点设计（新增/修改）

**检查清单**：
1. **架构文档引用**：
   - ✅ 设计文档必须引用相关架构文档
   - ✅ 对于MedicalCase相关功能，必须引用架构合规文档

2. **聚合根原则验证**：
   - ✅ Write操作：通过MedicalCase聚合根
   - ✅ Read操作：可独立查询
   - ✅ Helper操作：不修改状态

3. **架构合规Skill检查**：
   - ✅ 设计完成后，运行`lybtzyzs-arch-compliance` Skill
   - ✅ 解决所有检测到的违规项
   - ✅ 在设计文档中记录检查结果

4. **设计评审要素**：
   - ✅ API端点路径符合Write/Read/Helper分层
   - ✅ Service层职责清晰
   - ✅ Repository使用符合聚合根原则

### 8.2 实施注意事项

1. **REQ-001至REQ-005应作为一个Epic整体实施**，确保流程一致性
2. **REQ-006可作为独立Epic**，在MVP完成后优化架构时实施
3. **所有需求实施前需要编写单元测试**，确保代码质量
4. **所有需求实施后需要更新相关文档**（设计文档、API文档、用户手册）
5. **不考虑向后兼容性**，直接删除违规端点，Client端同步修改

### 8.3 成功标准

**本次重构成功的标志**：
1. ✅ 9个架构违规全部修复
2. ✅ REQ-001至REQ-005功能通过合规API实现
3. ✅ 设计流程改进防止未来违规
4. ✅ 无向后兼容性包袱，架构清晰
5. ✅ 文档完整，可作为最佳实践案例

---

**维护责任**：需求变更时必须同步更新本文档，并通知相关干系人。

---

**最后更新**: 2025-10-26
**维护者**: 项目架构团队
