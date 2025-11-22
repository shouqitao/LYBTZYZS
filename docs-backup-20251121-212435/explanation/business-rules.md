# 业务规则文档

## 📋 文档信息

- **创建日期**：2025-01-24
- **适用范围**：医案-诊断-处方三大核心模块
- **维护原则**：所有业务规则必须在此文档中明确记录

---

## 🎯 一、概述

本文档统一管理系统中所有业务规则,包括数据约束、流程规则、权限规则等。所有业务规则都必须在此文档中明确定义,并在代码实现时保持一致。

### 规则分类

| 分类 | 编号前缀 | 说明 | 示例 |
|-----|---------|------|------|
| **数据约束规则** | DC-XXX | 数据完整性、唯一性、格式约束 | DC-001: 患者手机号唯一性 |
| **流程规则** | BF-XXX | 业务流程、状态流转约束 | BF-001: 医案状态流转规则 |
| **聚合根规则** | AR-XXX | DDD聚合根边界、事务规则 | AR-001: MedicalCase聚合根约束 |
| **权限规则** | AC-XXX | 角色权限、访问控制 | AC-001: 医生只能查看自己的医案 |
| **计算规则** | CR-XXX | 价格计算、数量计算等 | CR-001: 处方价格计算公式 |

---

## 📊 二、数据约束规则（DC-XXX）

### DC-001: 患者手机号唯一性

**规则描述**：
- 同一手机号只能对应一个患者记录
- 创建患者时必须验证手机号唯一性

**实现位置**：
- `Server`: `LYBT.Module.Patients/Validators/PatientCreateDtoValidator.cs`
- `Database`: `UNIQUE INDEX idx_patients_phone_number`

**违规处理**：
- 创建失败,提示"该手机号已被使用"

---

### DC-002: 处方编号格式约束

**规则描述**：
- 格式：`RX-YYYYMMDD-NNNN`
- 示例：`RX-20251024-0001`
- 日期部分：创建日期（8位）
- 序号部分：当天递增（4位，不足补0）

**实现位置**：
- `Server`: `LYBT.Module.Prescriptions/Services/PrescriptionService.cs:GeneratePrescriptionNumber()`
- Issue #1551

**违规处理**：
- 自动生成,不允许手动输入

---

### DC-003: 诊断必填字段约束

**规则描述**：
- 必填字段：主诉（ChiefComplaint）、中医诊断（TCMDiagnosis）
- 可选字段：现病史、四诊信息、治疗原则、备注

**实现位置**：
- `Server`: `LYBT.Module.Consultation/Validators/ConsultationUpdateDtoValidator.cs`
- `Desktop`: `LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs:Validate()`

**违规处理**：
- 保存时验证失败,提示"主诉不能为空"或"中医诊断不能为空"

---

### DC-004: 处方药材项剂量约束

**规则描述**：
- **剂量范围**：0.1g - 500g
- **默认值**：10g
- **验证时机**：剂量输入时实时验证

**实现位置**（Epic #2175 BF-002 Phase 4）：
- `Desktop`: `LYBT.Desktop.MedicalCase/ViewModels/PrescriptionItemViewModel.cs:ValidateDosage()`
- `Desktop`: `LYBT.Desktop.MedicalCase/Views/HerbCardControl.xaml` - UI绑定验证

**违规处理**：
- 剂量 < 0.1g：显示错误提示"剂量不能小于0.1g"
- 剂量 > 500g：显示错误提示"剂量不能大于500g"，记录警告日志
- UI禁用保存按钮直到剂量有效

**测试覆盖**（Epic #2175 Phase 4）：
- ✅ 单元测试：15个测试，100%通过率
- ✅ 边界条件测试：负数剂量、超大剂量、零剂量

---

## 🔄 三、流程规则（BF-XXX）

### BF-001: 医案状态流转规则

**规则描述**：
- **状态定义**：
  - `Active`：进行中（可编辑）
  - `Closed`：已完成（只读）

- **状态流转路径**：
  ```
  Active（创建） → Active（暂存） → Closed（完成/取消）
                ↓
              Closed（直接完成）
  ```

**实现位置**：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- `Desktop`: `LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

**流转触发**：
- **Active→Active**：暂存医案（SaveDraftCommand）
- **Active→Closed**：完成医案（ExecuteNextStepAsync at Step 3）或取消医案（CancelCommand）

**违规处理**：
- 已关闭的医案不允许重新激活
- 状态回退操作拒绝

---

### BF-002: 三步看诊流程规则

**规则描述**（Epic #1612架构）：
- **Step 1：辨证**（Consultation）
  - 填写四诊信息（望、闻、问、切）
  - 必填验证：主诉、中医诊断
  - 完成时间戳：`Consultation.Step1CompletedAt`
  - API端点：`PUT /api/v1/medicalcases/{id}/consultation`

- **Step 2：标记处方需求**（Prescription Flag）
  - 用户选择是否需要开处方（RadioBox：是/否）
  - 更新 `MedicalCase.NeedsPrescription` 标志
  - 完成时间戳：`Consultation.Step2CompletedAt`
  - API端点：`PUT /api/v1/medicalcases/{id}/prescription-flag`

- **Step 3：开处方/完成**（Prescription or Complete）
  - **分支A（需要处方）**：
    - 开具处方（调用 `POST /api/v1/medicalcases/{id}/prescriptions`）
    - 可选操作：验方导入、历史复制
    - 完成病案（调用 `PUT /api/v1/medicalcases/{id}/complete`）
  - **分支B（不需要处方）**：
    - 直接完成病案（调用 `PUT /api/v1/medicalcases/{id}/complete`）
    - 状态更新为 `MedicalCaseStatus.Completed`

**实现位置**（Epic #1612重构）：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- `Server`: `LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- `Desktop`: `LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`
- Epic #1612, Issue #1567

**验证逻辑**（Service层）：
```csharp
// CompleteAsync() - 完成病案
if (medicalCase.Consultation?.Step1CompletedAt == null)
    throw new InvalidOperationException("未完成辨证（Step 1）");
if (medicalCase.Consultation?.Step2CompletedAt == null)
    throw new InvalidOperationException("未标记处方需求（Step 2）");
if (medicalCase.NeedsPrescription && medicalCase.Prescription == null)
    throw new InvalidOperationException("已标记需要处方，但未开具处方");
```

**流程约束**：
- ✅ 允许前进（验证通过后）
- ✅ 允许后退（数据不丢失）
- ❌ 不允许跳步（必须按顺序完成Step 1 → Step 2 → Step 3）
- ✅ 动态流程（根据 `NeedsPrescription` 决定是否开处方）

**测试覆盖**（Epic #1612）：
- ✅ 单元测试：32个测试，82.6%行覆盖率
- ✅ 集成测试：18个测试，100%通过率
- ✅ E2E场景：4个业务场景（含动态流程测试）

**相关文档**：
- `docs/reference/modules/medical-case/README.md` - 三步流程详解
- `docs/reference/api/medicalcase-api.md` - API端点完整文档
- `docs/reports/e2e-test-coverage-analysis.md` - E2E测试报告

---

### BF-003: 未完成医案检测规则

**规则描述**：
- 患者选择后,自动检测是否有未完成医案（Status=Active）
- 如有未完成医案,弹出4选项对话框：
  1. **继续看诊**：加载旧医案 → Step 1
  2. **新建医案**：关闭旧医案（Status→Closed） → 创建新医案 → Step 1
  3. **仅关闭**：关闭旧医案 → 返回患者选择
  4. **取消**：什么都不做 → 返回患者选择

**实现位置**：
- `Server`: `LYBT.Module.MedicalCase/Interfaces/IMedicalCaseRepository.cs:GetPendingCasesAsync()`
- `Desktop`: `LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- `Desktop`: `LYBT.Desktop.Patients/Views/UnfinishedCaseDialog.xaml`
- Epic #1583

**业务价值**：
- 防止数据丢失
- 清晰的用户引导

---

### BF-004: 处方当天可改隔日锁定规则

**规则描述**：
- **编辑权限**：
  - 创建当天：可编辑（CreatedAt.Date == Today）
  - 隔日及以后：只读（CreatedAt.Date < Today）

- **锁定原因**：
  - 确保处方数据不被追溯修改
  - 符合医疗记录管理规范

**实现位置**：
- `Server`: `LYBT.Module.Prescriptions/Services/PrescriptionService.cs:UpdateAsync()`
- Issue #1423 RULE-3

**违规处理**：
- 编辑已锁定处方时抛出异常："处方已锁定,不能修改"

---

## 🏗️ 四、聚合根规则（AR-XXX）

### AR-001: MedicalCase聚合根约束

**规则描述**：
- **聚合根**：MedicalCase（医案）
- **聚合内实体**：Consultation（诊断）、Prescription（处方）
- **共享主键约束**：`Consultation.Id == MedicalCase.Id`

**架构原则**：
1. ✅ **写操作必须通过聚合根**：
   - 创建Consultation：`MedicalCaseService.CreateAsync()` 自动创建
   - 更新Consultation：`MedicalCaseService.UpdateConsultationAsync()`
   - 更新Prescription：`MedicalCaseService.UpdatePrescriptionAsync()`

2. ✅ **读操作可绕过聚合根**：
   - 允许直接调用 `ConsultationRepository.GetByIdAsync()`
   - 允许直接调用 `PrescriptionRepository.GetByPatientIdAsync()`

3. ✅ **事务边界**：
   - 聚合根内操作保证ACID
   - 一次SaveChanges保存MedicalCase + Consultation
   - 级联删除：删除MedicalCase时自动删除Consultation和Prescription

**实现位置**（Epic #1612重构）：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 14个聚合根协调方法
- `Server`: `LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs` - 预加载优化
- `Server`: `LYBT.WebAPI/Controllers/MedicalCaseController.cs` - 14个API端点
- `Database`: Foreign Key约束（ON DELETE CASCADE）
- Epic #1612, Issue #1563

**测试覆盖**（Epic #1612）：
- ✅ 单元测试：32个测试验证聚合根协调逻辑
- ✅ 集成测试：18个测试验证API端点
- ✅ 架构测试：预加载避免N+1查询

**架构文档**：
- `docs/explanation/architecture/shared/medicalcase-architecture-correction-plan-v2.md`
- `docs/reference/modules/medical-case/README.md` - 聚合根边界详解
- `docs/reference/quick-reference/code-patterns.md` - Service层聚合根协调模式

---

### AR-002: 防重复创建规则

**规则描述**：
- **约束**：同一患者同一天只能有一个Active状态的医案
- **检查时机**：创建新医案前（`MedicalCaseService.CreateAsync()`）

**验证逻辑**：
```csharp
var activeCasesToday = existingCases.Where(c =>
    c.Status == MedicalCaseStatus.Active &&
    c.CreatedAt.Date == DateTime.Today);

if (activeCasesToday.Any())
    return ValidationResult.Failure("同一患者同一天只能有一个进行中的医案");
```

**实现位置**：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseRules.cs:ValidateNewCaseCreation()`

**违规处理**：
- 创建失败,提示错误信息
- 建议用户"继续看诊"（加载旧医案）而非新建

---

### AR-003: 一诊一方规则

**规则描述**：
- **约束**：一个MedicalCase只能有一个Prescription（聚合根约束）
- **架构原则**：MedicalCase → Prescription为1:0..1关系
- **检查时机**：创建处方前（`MedicalCaseService.CreatePrescriptionAsync()`）

**验证逻辑**（Epic #1612实现）：
```csharp
// MedicalCaseService.CreatePrescriptionAsync
var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

// AR-003验证：一诊一方约束
if (medicalCase.Prescription != null)
{
    throw new InvalidOperationException(
        "该病案已有处方，请先删除现有处方或使用更新接口（AR-003约束）");
}

// 业务流程验证（BF-002）
if (medicalCase.Consultation?.Step1CompletedAt == null)
    throw new InvalidOperationException("未完成辨证（Step 1），无法开处方");
if (medicalCase.Consultation?.Step2CompletedAt == null)
    throw new InvalidOperationException("未标记处方需求（Step 2），无法开处方");
if (!medicalCase.NeedsPrescription)
    throw new InvalidOperationException("已标记不需要处方，无法开处方");
```

**实现位置**（Epic #1612重构）：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseService.cs:CreatePrescriptionAsync()`
- `Server`: `LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs:GetByIdWithDetailsAsync()`
- Epic #1612, Issue #1423 RULE-2

**违规处理**：
- 创建失败，抛出 `InvalidOperationException`，HTTP 422
- 建议用户删除现有处方（`DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}`）或使用更新接口（`PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}`）

**架构影响**（Epic #1612）：
- ✅ 符合聚合根约束（AR-001）：通过MedicalCase聚合根管理Prescription
- ✅ 符合三步流程（BF-002）：需完成Step 1（辨证）和Step 2（标记）才能开处方
- ✅ 测试覆盖：32个单元测试，18个集成测试，100%通过率

**相关文档**：
- `docs/reference/modules/medical-case/README.md` - 业务规则详解
- `docs/reference/api/medicalcase-api.md` - API端点文档
- `docs/reference/quick-reference/code-patterns.md` - Service层实现模式

---

## 💰 五、计算规则（CR-XXX）

### CR-001: 处方价格计算公式

**规则描述**：
- **单剂价格** = Σ(药材单价 × 药材剂量)
- **总价格** = 单剂价格 × 剂数
- **优惠后价格** = 总价格 × 折扣
- **节省金额** = 总价格 - 优惠后价格

**计算公式（C#）**：
```csharp
decimal CalculateTotalAmount(IEnumerable<PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
{
    var singleDosagePrice = items.Sum(item => item.UnitPrice * item.Dosage);
    var totalPrice = singleDosagePrice * dosageCount;
    var discountedPrice = totalPrice * discount;
    return discountedPrice;
}
```

**实现位置**：
- `Server`: `LYBT.Module.Prescriptions/Services/PrescriptionService.cs:CalculateTotalAmount()`
- `Desktop`: `LYBT.Desktop.Prescriptions/ViewModels/Components/PrescriptionCalculator.cs`

**约束条件**：
- 剂数 ≥ 1
- 折扣范围：0.1 ~ 1.0（10% ~ 100%）
- 价格精度：保留2位小数

---

### CR-002: 验方导入规则

**规则描述**：
- **导入来源**：验方库（Formula表）
- **导入内容**：FormulaItems → PrescriptionItems
- **字段映射**：
  - HerbId → HerbId（药材ID）
  - Dosage → Dosage（剂量）
  - Unit → Unit（单位）
  - Remark → Remark（备注）

**实现位置**：
- `Server`: `LYBT.Module.Prescriptions/Services/PrescriptionService.cs:ImportFormulaIntoPrescriptionAsync()`
- `Desktop`: `LYBT.Desktop.Prescriptions/ViewModels/SelectFormulaDialogViewModel.cs`
- Issue #1368

**业务逻辑**：
1. 清空现有处方项
2. 复制验方项到处方
3. 重新计算价格
4. 保存处方

---

### CR-003: 处方药材项价格计算

**规则描述**（Epic #2175 BF-002）：
- **单项金额计算**：ItemAmount = Dosage × UnitPrice
- **实时计算**：剂量或单价变更时自动重新计算
- **精度**：保留2位小数

**计算公式（C#）**：
```csharp
// PrescriptionItemViewModel.cs
private void CalculateAmount()
{
    ItemAmount = Dosage * UnitPrice;
}
```

**实现位置**：
- `Desktop`: `LYBT.Desktop.MedicalCase/ViewModels/PrescriptionItemViewModel.cs:CalculateAmount()`
- `Desktop`: `LYBT.Desktop.MedicalCase/Services/PrescriptionCalculator.cs` - 处方总价计算

**触发时机**：
- 用户选择药材时（UnitPrice自动填充）
- 用户修改剂量时（Dosage属性Changed事件）
- 用户修改单价时（UnitPrice属性Changed事件）

**关联规则**：
- **DC-004**：剂量必须在有效范围（0.1g - 500g）
- **CR-001**：处方总价 = Σ(各药材项ItemAmount) × 剂数 × 折扣

**测试覆盖**（Epic #2175 Phase 4）：
- ✅ 单元测试：7个价格计算测试，100%通过率
- ✅ 边界条件测试：零价格、小数剂量、小数单价

---

## 🔐 六、权限规则（AC-XXX）

### AC-001: 医生只能查看自己的医案

**规则描述**：
- **查询约束**：`GetByDoctorIdAsync(CurrentUser.Id)`
- **编辑约束**：只能编辑自己创建的医案

**实现位置**：
- `Server`: `LYBT.Module.MedicalCase/Services/MedicalCaseService.cs:GetByDoctorIdAsync()`
- `Desktop`: SessionManager（会话管理）

**例外场景**：
- 管理员角色可查看所有医案

---

### AC-002: 角色路由规则

**规则描述**：
- **医生角色（Doctor）**：登录后导航到 `ClinicalHomeView`
- **管理员角色（Admin）**：登录后导航到 `AdminHomeView`

**实现位置**：
- `Desktop`: `LoginViewModel.cs` + `RoleNavigationService.cs`
- Issue #1513

---

## 📝 七、规则实施检查清单

### 新增规则流程

1. [ ] 在本文档中添加规则定义
2. [ ] 分配规则编号（DC/BF/AR/AC/CR-XXX）
3. [ ] 在代码中实现验证逻辑
4. [ ] 编写单元测试（验证规则执行）
5. [ ] 更新相关文档（需求、设计、API文档）

### 修改规则流程

1. [ ] 评估影响范围（Server/Desktop/Database）
2. [ ] 更新本文档规则定义
3. [ ] 修改代码实现
4. [ ] 更新单元测试
5. [ ] 创建数据迁移脚本（如影响数据库）

---

## 🔍 八、规则验证矩阵

| 规则编号 | Server端验证 | Desktop端验证 | Database约束 | 单元测试 | 集成测试 | 测试覆盖率 | 风险等级 |
|---------|------------|-------------|------------|---------|---------|-----------|---------|
| DC-001 | ✅ Validator | ✅ ViewModel | ✅ UNIQUE | ❌ | ❌ | 0% | 🟡 中风险 |
| DC-002 | ✅ Service | ✅ ReadOnly | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |
| DC-003 | ✅ Validator | ✅ ViewModel | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |
| BF-001 | ✅ Service | ✅ ViewModel | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| BF-002 | ⚠️ Partial | ✅ ViewModel | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| BF-003 | ✅ Repository | ✅ ViewModel | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| BF-004 | ✅ Service | ⚠️ 缺失 | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| AR-001 | ✅ Service | ✅ Repository | ✅ FK | ❌ | ❌ | **0%** | 🔴 **高风险** |
| AR-002 | ✅ Rules | ❌ | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| AR-003 | ⚠️ Incomplete | ❌ | ❌ | ❌ | ❌ | **0%** | 🔴 **高风险** |
| CR-001 | ✅ Service | ✅ Calculator | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |
| CR-002 | ✅ Service | ✅ ViewModel | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |
| AC-001 | ✅ Service | ✅ SessionMgr | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |
| AC-002 | ❌ | ✅ Navigation | ❌ | ❌ | ❌ | 0% | 🟡 中风险 |

**说明**：
- ✅ 已实现
- ⚠️ 部分实现或存在问题
- ❌ 未实现

**测试覆盖率与风险等级**：
- **0%覆盖率 + 业务流程规则（BF-XXX）** = 🔴 高风险（状态机逻辑复杂，重构时容易破坏）
- **0%覆盖率 + 聚合根规则（AR-XXX）** = 🔴 高风险（架构约束，缺少自动化验证）
- **0%覆盖率 + 数据约束规则（DC-XXX）** = 🟡 中风险（有数据库约束或Validator保护）
- **0%覆盖率 + 计算规则（CR-XXX）** = 🟡 中风险（业务逻辑相对独立，影响范围可控）
- **0%覆盖率 + 访问控制规则（AC-XXX）** = 🟡 中风险（运行时验证，影响安全性）

**高风险规则补充测试计划**（Phase 4执行）：
- **BF-001/002/003/004**：编写集成测试覆盖状态机转换（目标覆盖率：60%+）
- **AR-001/002/003**：使用NetArchTest.Rules进行架构测试（目标：100%验证）

---

## ⚠️ 九、已知问题与改进建议

### 问题1：AR-003规则验证不完整

**问题描述**：
- 历史处方复制功能（CopyFromHistoryCommand）未验证"一诊断一处方"规则
- 可能导致同一Consultation有多个Prescription

**优先级**：中
**建议修复**：
- 在`PrescriptionRepository.CreateAsync()`前增加验证
- 或在`CopyFromHistoryCommand`执行前检查目标Consultation是否已有处方

---

### 问题2：测试覆盖率0%

**问题描述**：
- 所有业务规则均无单元测试
- 依赖手动验证,风险较高

**优先级**：高
**建议修复**：
- 为核心规则（AR-001, AR-002, AR-003, BF-001）编写单元测试
- 目标覆盖率：60%+

---

### 问题3：规则分散管理

**问题描述**：
- 部分规则在Service层（PrescriptionService.cs内嵌）
- 部分规则在静态类（MedicalCaseRules.cs）
- 缺乏统一的规则引擎

**优先级**：中
**建议改进**：
- 引入Specification模式封装业务规则
- 创建统一的RuleEngine管理所有规则

---

## 📚 十、参考资料

### 相关文档
- `docs/explanation/architecture/shared/medicalcase-architecture-correction-plan-v2.md` - 聚合根架构
- `docs/reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md` - 现状分析

### 相关Issues
- #1423: 处方业务规则（RULE-2, RULE-3）
- #1551: 处方自动编号
- #1563: MedicalCase聚合根重构
- #1567: 三步看诊流程
- #1583: 待看诊队列（未完成医案检测）

### 代码文件
- `LYBT.Module.MedicalCase/Services/MedicalCaseRules.cs`
- `LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- `LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

---

## 📅 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|-----|------|---------|------|
| 2025-01-24 | v1.0 | 初始版本,整合三模块核心业务规则 | Claude Code |
| 2025-11-20 | v1.1 | 添加Epic #2175 BF-002规则：DC-004处方药材项剂量约束、CR-003处方药材项价格计算 | Claude Code |

---

**维护责任**：所有新增或修改业务规则必须同步更新本文档。
