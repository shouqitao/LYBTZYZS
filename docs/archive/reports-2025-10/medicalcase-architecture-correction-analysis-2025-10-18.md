# MedicalCase架构纠正分析报告

> **报告类型**: 架构审查与纠正方案
> **创建日期**: 2025-10-18
> **分析范围**: Server端 + Desktop端
> **优先级**: P0（关键架构问题，必须立即修正）
> **分析方法**: Sequential-thinking (10步深度分析) + 代码审查

---

## 📋 执行摘要

### 核心问题

**用户明确指出的架构错误**：
> "就诊过程中，MedicalCase才是主架构。出现Consultation是主架构是之前的技术债务。之前设计的开发是的理解错误。这个需要强势修正的。而不是追求最小改动原则。"

**DDD架构原则违反**：
- ❌ Consultation被错误地当作聚合根（独立模块、独立API、独立CRUD）
- ❌ Prescription被错误地当作聚合根（独立模块、独立API、独立CRUD）
- ✅ MedicalCase才是正确的聚合根（1:1:1关系：MedicalCase包含Consultation和Prescription）

**影响范围**：
- Server端：3个模块（MedicalCase、Consultation、Prescriptions）
- API层：3个Controller（MedicalCaseController、ConsultationController、PrescriptionsController）
- Desktop端：3个模块（LYBT.Desktop.MedicalCase、LYBT.Desktop.Consultation、LYBT.Desktop.Prescriptions）
- 文档：多个文档使用了错误的术语（"就诊主界面"、"ConsultationView主框架"等）

---

## 1. 架构问题详细分析

### 1.1 Server端问题

#### 问题1：ConsultationModule独立存在（技术债务）

**发现**：
- 文件：`src/Server/Modules/LYBT.Module.Consultation/ConsultationModule.cs`
- 问题：Consultation作为独立模块注册服务

**DDD原则违反**：
- Consultation不应该是独立模块
- Consultation应该是MedicalCase聚合根的**组成部分**（实体或值对象）
- 独立模块意味着可以独立创建、修改、删除，破坏了聚合根边界

**正确设计**：
- Consultation应该定义在`LYBT.Module.MedicalCase`内部
- ConsultationRepository应该是私有的，仅供MedicalCaseService使用
- 不应该暴露独立的IConsultationService接口

---

#### 问题2：ConsultationController暴露完整CRUD API

**发现**：
- 文件：`src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs`
- 路由：`/api/v1/consultations`

**存在的API**：
1. ❌ `POST /api/v1/consultations` - CreateConsultation（Line 89）
   - 虽然已标记`[Obsolete]`（Line 88）
   - 但仍然可以调用，只是发出警告
   - 注释明确指出架构问题（Line 81-84）

2. ❌ `GET /api/v1/consultations` - GetConsultations（Line 35）
   - 分页查询诊疗记录
   - 没有Obsolete标记

3. ❌ `GET /api/v1/consultations/{id}` - GetById（Line 59）
   - 获取单个诊疗详情
   - 没有Obsolete标记

4. ❌ `PUT /api/v1/consultations/{id}` - UpdateConsultation（Line 126）
   - 更新诊疗信息
   - 没有Obsolete标记

5. ❌ `DELETE /api/v1/consultations/{id}` - DeleteConsultation（Line 159）
   - 删除诊疗记录
   - 没有Obsolete标记

6. ❌ `GET /api/v1/consultations/medicalcase/{medicalCaseId}` - GetByMedicalCaseId（Line 189）
   - 根据医案ID查询诊疗记录
   - 没有Obsolete标记
   - **这个API暴露了1:N关系的假设，与用户说的1:1:1关系矛盾**

**DDD原则违反**：
- 独立的CRUD API破坏了聚合根的事务边界
- 客户端可以绕过MedicalCase直接修改Consultation
- 数据一致性无法保证（Consultation可能独立存在，没有MedicalCase）

**正确设计**：
- 所有Consultation操作应该通过`MedicalCaseController`进行
- 例如：`PUT /api/v1/medicalcases/{id}/consultation` 更新MedicalCase的Consultation部分
- ConsultationController应该完全废弃，不仅仅是标记Obsolete

---

#### 问题3：PrescriptionsController独立CRUD API（更严重）

**发现**：
- 文件：`src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`
- 路由：`/api/v1/prescriptions`

**存在的API**：
1. ❌ `POST /api/v1/prescriptions` - Add（Line 97）
   - 独立创建处方
   - **没有任何Obsolete标记**
   - 与Consultation问题相同，但更严重（连警告都没有）

2. ❌ `GET /api/v1/prescriptions` - GetList（Line 41）
   - 分页查询处方
   - 没有Obsolete标记

3. ❌ `PUT /api/v1/prescriptions/{id}` - Update（Line 126）
   - 独立更新处方
   - 没有Obsolete标记

4. ❌ `DELETE /api/v1/prescriptions/{id}` - Delete（Line 162）
   - 独立删除处方
   - 没有Obsolete标记

5. ⚠️ `POST /api/v1/prescriptions/{sourcePrescriptionId}/clone-to/{targetConsultationId}` - ClonePrescriptionTo（Line 310）
   - 克隆处方到指定Consultation
   - **参数是targetConsultationId，而非targetMedicalCaseId**
   - 说明代码仍然以Consultation为中心，而非MedicalCase

**DDD原则违反**：
- Prescription应该是MedicalCase的组成部分，不能独立创建
- 克隆处方的目标应该是MedicalCase，而非Consultation
- 独立的CRUD API完全违反了聚合根原则

---

#### 问题4：正确的API已实现，但未完全替代旧API

**发现**：
- 文件：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`

**正确的API**：
1. ✅ `POST /api/v1/medicalcases/with-details` - CreateWithDetails（Line 115）
   - 接受`MedicalCaseWithDetailsCreateDto`
   - 包含：MedicalCase + Consultation + Prescription（可选）
   - 一次性原子操作创建完整的聚合根
   - **这是正确的设计！**

**问题**：
- 正确的API已经实现，但旧的错误API仍然存在
- 客户端可以选择调用旧API或新API，导致混乱
- 没有强制性迁移计划

---

### 1.2 Desktop端问题

#### 问题5：LYBT.Desktop.Consultation独立模块（技术债务）

**发现**：
- 模块：`src/Client/Desktop/Modules/LYBT.Desktop.Consultation`
- ViewModel：`ConsultationManagementViewModel.cs`
- View：`ConsultationManagementView.xaml`

**功能分析**：
- 用途：管理历史诊疗记录（查询、浏览、搜索）
- 不是用于创建新诊疗，而是查看历史
- Line 17注释："只处理显示和基本操作诊疗记录，不处理复杂的临床流程"

**架构问题**：
1. ❌ 调用`IConsultationRepository.GetPagedAsync()`（Line 141）
   - 直接查询Consultation，绕过MedicalCase聚合根
   - 违反DDD原则：应该通过MedicalCase查询Consultation

2. ⚠️ 功能未完成：
   - ViewDetails、ViewPrescription、Print等功能都是"开发中"
   - 说明这是半成品模块

**正确设计**：
- 不应该有独立的ConsultationManagement模块
- 应该改为MedicalCaseManagement模块
- 查询历史病案时，一并查询Consultation和Prescription（聚合根完整加载）

---

#### 问题6：LYBT.Desktop.Prescriptions独立模块（7个ViewModel）

**发现**：
- 模块：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions`
- ViewModels：
  1. PrescriptionViewModel.cs
  2. PrescriptionEditorDialogViewModel.cs
  3. PrescriptionManagementViewModel.cs
  4. PrescriptionsMainViewModel.cs
  5. FormulaTemplateDialogViewModel.cs
  6. HerbSelectionDialogViewModel.cs
  7. PrescriptionSearchDialogViewModel.cs

**架构问题**：
- Prescription作为独立模块，有完整的CRUD功能
- 7个ViewModel说明功能非常复杂，远超"组成部分"的定位
- PrescriptionManagementViewModel可能直接调用PrescriptionRepository，绕过MedicalCase

**正确设计**：
- Prescription相关的UI组件应该是MedicalCase模块的一部分
- PrescriptionEditorDialogViewModel应该在MedicalCaseEntryViewModel中使用
- 不应该有独立的PrescriptionManagement模块

---

#### 问题7：正确的ViewModel已实现，但命名和文档仍有问题

**发现**：
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseEntryViewModel.cs`
- Line 18注释："**Issue #1463: 以MedicalCase为中心的激进重构**"
- Line 274注释："**Issue #1463: 使用聚合根模式**"

**正确的实现**：
```csharp
// Line 316: 一次性原子操作创建MedicalCase + Consultation
var result = await _medicalCaseRepository.CreateWithDetailsAsync(
    medicalCaseDto,
    consultationDto,
    null // 暂无处方
);
```

**好消息**：
- MedicalCaseEntryViewModel已经正确实现了聚合根模式
- Issue #1463明确提出"激进重构"
- 团队已经意识到架构问题

**仍需修正的问题**：
1. UI/UX文档仍使用错误术语：
   - ❌ "ConsultationView主框架"
   - ❌ "就诊主界面"
   - ❌ "核心就诊界面"

2. 应该改为：
   - ✅ "MedicalCaseView核心布局"
   - ✅ "病案录入界面"
   - ✅ "病案主界面"

---

## 2. 正确的DDD架构设计

### 2.1 聚合根关系

```
MedicalCase（聚合根，Aggregate Root）
├─ Id（主键）
├─ PatientId（外键）
├─ DoctorId（外键）
├─ CreatedAt
├─ UpdatedAt
└─ 组成部分（Entities within Aggregate）
    ├─ Consultation（1:1关系，共享主键）
    │   ├─ Id = MedicalCase.Id（共享主键）
    │   ├─ ChiefComplaint（主诉）
    │   ├─ PresentIllness（现病史）
    │   ├─ TCMDiagnosis（中医诊断）
    │   ├─ Inspection（望诊）
    │   ├─ AuscultationOlfaction（闻诊）
    │   ├─ Inquiry（问诊）
    │   ├─ Palpation（切诊）
    │   └─ TreatmentPrinciple（治疗原则）
    │
    └─ Prescription（1:1关系，共享主键）
        ├─ Id = MedicalCase.Id（共享主键）
        ├─ PrescriptionNo（处方编号）
        ├─ Dosage（剂数）
        ├─ Remark（备注）
        └─ PrescriptionItems（处方明细，1:N）
            ├─ HerbId
            ├─ HerbName
            ├─ Quantity
            └─ Unit
```

### 2.2 正确的API设计

**创建完整病案**：
```http
POST /api/v1/medicalcases/with-details
Content-Type: application/json

{
  "medicalCase": {
    "patientId": "xxx",
    "doctorId": "xxx",
    "chiefComplaint": "头痛三日"
  },
  "consultation": {
    "chiefComplaint": "头痛三日",
    "presentIllness": "患者三日前...",
    "tcmDiagnosis": "风寒感冒",
    "inspection": "面色苍白",
    "auscultationOlfaction": "语声低微",
    "inquiry": "恶寒发热",
    "palpation": "脉浮紧",
    "treatmentPrinciple": "辛温解表"
  },
  "prescription": {
    "dosage": 7,
    "remark": "水煎服",
    "items": [
      { "herbId": "xxx", "herbName": "麻黄", "quantity": 9, "unit": "g" },
      { "herbId": "xxx", "herbName": "桂枝", "quantity": 6, "unit": "g" }
    ]
  }
}
```

**更新Consultation部分**：
```http
PUT /api/v1/medicalcases/{id}/consultation
Content-Type: application/json

{
  "chiefComplaint": "头痛三日，加重一日",
  "tcmDiagnosis": "风寒感冒，气虚"
}
```

**更新Prescription部分**：
```http
PUT /api/v1/medicalcases/{id}/prescription
Content-Type: application/json

{
  "dosage": 14,
  "items": [...]
}
```

**查询完整病案**：
```http
GET /api/v1/medicalcases/{id}/with-details

Response:
{
  "medicalCase": { ... },
  "consultation": { ... },
  "prescription": { ... }
}
```

### 2.3 正确的Desktop端架构

**模块结构**：
```
LYBT.Desktop.MedicalCase（病案模块，包含所有相关功能）
├─ ViewModels/
│   ├─ MedicalCaseEntryViewModel.cs（病案录入，包含Consultation + Prescription）
│   ├─ MedicalCaseManagementViewModel.cs（病案管理，查询历史）
│   ├─ MedicalCaseDetailViewModel.cs（病案详情，展示完整信息）
│   └─ Dialogs/
│       ├─ PrescriptionEditorDialogViewModel.cs（处方编辑对话框）
│       ├─ FormulaTemplateDialogViewModel.cs（验方导入对话框）
│       └─ PrescriptionSearchDialogViewModel.cs（历史处方搜索对话框）
│
├─ Views/
│   ├─ MedicalCaseEntryView.xaml（病案录入界面）
│   │   ├─ 患者信息条（PatientInfoBar）
│   │   ├─ 诊断区（ConsultationSectionControl）
│   │   └─ 处方区（PrescriptionSectionControl）
│   ├─ MedicalCaseManagementView.xaml（病案管理界面）
│   └─ MedicalCaseDetailView.xaml（病案详情界面）
│
└─ Controls/
    ├─ PatientInfoBar.xaml（患者信息条UserControl）
    ├─ ConsultationSectionControl.xaml（诊断区UserControl）
    └─ PrescriptionSectionControl.xaml（处方区UserControl）
```

**移除的模块**：
- ❌ LYBT.Desktop.Consultation（整个模块删除）
- ❌ LYBT.Desktop.Prescriptions（移动到MedicalCase模块内部）

---

## 3. 修正方案（分阶段执行）

### Phase 1：Server端API废弃与迁移（1-2天）

#### Step 1.1：标记所有旧API为Obsolete

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs`

**修改**：
- 所有方法添加`[Obsolete("...", true)]`（error=true，编译时报错）
- 保留GetById用于向后兼容（error=false）

**代码**：
```csharp
[HttpPost]
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation(...)

[HttpPut("{id}")]
[Obsolete("请使用 PUT /api/medicalcases/{id}/consultation 更新诊断信息。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(...)

[HttpDelete("{id}")]
[Obsolete("请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除诊疗和处方）。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse>> DeleteConsultation(...)
```

#### Step 1.2：标记PrescriptionsController所有创建/更新/删除API为Obsolete

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`

**修改**：
```csharp
[HttpPost]
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案（含处方）。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add(...)

[HttpPut("{id}")]
[Obsolete("请使用 PUT /api/medicalcases/{id}/prescription 更新处方。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(...)

[HttpDelete("{id}")]
[Obsolete("请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除）。此端点已废弃。", true)]
public async Task<ActionResult<ApiResponse>> Delete(...)
```

**保留的API（只读查询，不违反聚合根原则）**：
- ✅ `GET /api/v1/prescriptions` - GetList（历史查询）
- ✅ `GET /api/v1/prescriptions/{id}` - GetById（详情查询）
- ✅ `GET /api/v1/prescriptions/patient/{patientId}/recent` - GetPatientRecentPrescriptions

#### Step 1.3：扩展MedicalCaseController，添加子实体更新API

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`

**新增API**：
```csharp
/// <summary>
/// 更新病案的诊断信息
/// </summary>
[HttpPut("{id}/consultation")]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(
    Guid id,
    [FromBody] ConsultationUpdateDto dto)
{
    // 实现：加载MedicalCase聚合根 → 更新Consultation → 保存
}

/// <summary>
/// 更新病案的处方信息
/// </summary>
[HttpPut("{id}/prescription")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> UpdatePrescription(
    Guid id,
    [FromBody] PrescriptionUpdateDto dto)
{
    // 实现：加载MedicalCase聚合根 → 更新Prescription → 保存
}
```

---

### Phase 2：Desktop端模块重构（2-3天）

#### Step 2.1：废弃LYBT.Desktop.Consultation模块

**行动**：
1. 将`ConsultationManagementViewModel`的功能迁移到`MedicalCaseManagementViewModel`
2. 删除`LYBT.Desktop.Consultation`模块
3. 更新导航引用（如果有菜单项引用ConsultationManagementView）

#### Step 2.2：重构LYBT.Desktop.Prescriptions模块

**选项A：移动到MedicalCase模块内部**（推荐）
```
LYBT.Desktop.MedicalCase/
└─ Prescription/（子目录）
    ├─ ViewModels/
    │   ├─ PrescriptionEditorViewModel.cs
    │   ├─ FormulaTemplateDialogViewModel.cs
    │   └─ PrescriptionSearchDialogViewModel.cs
    └─ Views/
        ├─ PrescriptionEditorControl.xaml
        ├─ FormulaTemplateDialog.xaml
        └─ PrescriptionSearchDialog.xaml
```

**选项B：保留模块，但明确为辅助模块**
- 重命名为`LYBT.Desktop.PrescriptionHelpers`
- 明确注释：此模块仅提供UI辅助组件，不处理业务逻辑
- 所有业务逻辑调用MedicalCaseService

#### Step 2.3：重命名和术语修正

**文件重命名**（如有必要）：
- 可能无需重命名文件（MedicalCaseEntryView已经正确）
- 重点是修正文档和注释

**术语修正**：
- 所有"ConsultationView主框架" → "MedicalCaseView核心布局"
- 所有"就诊主界面" → "病案录入界面"
- 所有"核心就诊界面" → "病案主界面"

---

### Phase 3：文档更新（0.5-1天）

#### Step 3.1：更新架构讨论文档

**文件**：`docs/architecture/client/clinical-workflow-ux-design-discussion.md`

**修改**：
- Section 2.1：修正导航结构描述
- Section 2.2：标题从"核心就诊界面布局设计（ConsultationView）"改为"病案录入界面布局设计（MedicalCaseView）"
- Section 3.1：View结构修正
- Section 5：Task清单修正

#### Step 3.2：更新架构澄清文档

**文件**：`docs/architecture/client/consultation-view-architecture-clarification.md`

**当前状态**：
- 已记录用户的强势修正决策
- 已明确MedicalCase是聚合根

**后续行动**：
- 添加"修正完成记录"章节
- 记录Phase 1-3的执行结果

#### Step 3.3：更新Client端架构文档

**文件**：`docs/architecture/client/README.md`

**修改**：
- 明确MedicalCase是唯一的病案聚合根
- 更新模块列表（移除Consultation模块）
- 添加DDD聚合根设计说明

---

### Phase 4：数据库Schema验证（可选，0.5天）

**验证内容**：
1. MedicalCase、Consultation、Prescription表是否使用共享主键（1:1:1）？
2. 外键约束是否正确设置（级联删除）？
3. 是否存在孤立的Consultation或Prescription记录？

**如果Schema错误**：
- 创建数据库迁移脚本
- 清理孤立数据
- 修正外键约束

---

## 4. 需要修正的文件清单

### Server端（8个文件）

**标记Obsolete**：
1. `src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs`
   - 所有方法标记`[Obsolete("...", true)]`
   - 除GetById保留向后兼容（error=false）

2. `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`
   - Add、Update、Delete标记`[Obsolete("...", true)]`
   - 查询方法保留

**扩展API**：
3. `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
   - 新增`PUT /api/v1/medicalcases/{id}/consultation`
   - 新增`PUT /api/v1/medicalcases/{id}/prescription`

**模块清理**（Phase 2后期，如需要）：
4. `src/Server/Modules/LYBT.Module.Consultation/ConsultationModule.cs`
   - 标记整个模块为Obsolete
   - 或移动到LYBT.Module.MedicalCase内部

5. `src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionsModule.cs`
   - 标记整个模块为Obsolete
   - 或移动到LYBT.Module.MedicalCase内部

**服务层修正**（如需要）：
6. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
   - 新增`UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto)`
   - 新增`UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)`

7. `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseService.cs`
   - 接口定义

8. `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
   - Repository实现（如需要）

---

### Desktop端（多个文件）

**模块重构**：
9. `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/`
   - **整个模块标记废弃或删除**
   - ConsultationManagementViewModel功能迁移到MedicalCaseManagementViewModel

10. `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/`
    - **选项A：移动到LYBT.Desktop.MedicalCase/Prescription/目录**
    - **选项B：保留但重命名为PrescriptionHelpers，明确为辅助模块**

**ViewModel修正**（术语和注释）：
11. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseEntryViewModel.cs`
    - 注释已经正确（Issue #1463）
    - 无需修改

12. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseManagementViewModel.cs`
    - 整合ConsultationManagement功能
    - 查询历史病案时，加载完整聚合根

**View修正**（XAML注释）：
13. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml`
    - 更新注释和文档字符串

---

### 文档（6个文件）

**架构讨论文档**：
14. `docs/architecture/client/clinical-workflow-ux-design-discussion.md`
    - Section 2.1：修正导航结构
    - Section 2.2：标题从"ConsultationView"改为"MedicalCaseView"
    - Section 3.1：View结构修正
    - Section 5：Task清单修正
    - 全文搜索替换错误术语

15. `docs/architecture/client/consultation-view-architecture-clarification.md`
    - 添加"Phase 1-3修正完成记录"章节

**Client端架构文档**：
16. `docs/architecture/client/README.md`
    - 明确MedicalCase是聚合根
    - 更新模块列表
    - 添加DDD设计说明

**Server端架构文档**（如有相关表述）：
17. `docs/architecture/server/README.md`
    - 明确MedicalCase是聚合根
    - 更新模块说明

**开发策略文档**（如有相关表述）：
18. `docs/architecture/shared/mvp-development-strategy-discussion.md`
    - 检查是否有错误术语

**本次新建的文档**：
19. `docs/architecture/client/consultation-view-architecture-clarification.md`
    - 已创建，记录用户纠正决策
    - 后续更新执行结果

---

## 5. 执行计划与时间线

### Week 1：Server端修正（P0优先）

**Day 1-2**：
- [ ] Step 1.1：标记ConsultationController所有方法为Obsolete（error=true）
- [ ] Step 1.2：标记PrescriptionsController创建/更新/删除方法为Obsolete（error=true）
- [ ] Step 1.3：扩展MedicalCaseController，添加子实体更新API
- [ ] 编译验证（确保所有旧API调用被编译器捕获）
- [ ] 单元测试：新API功能测试

**Day 3**：
- [ ] Desktop端调用点修正（如果有直接调用旧API的地方）
- [ ] 集成测试：完整流程测试
- [ ] 创建PR，代码审查

### Week 2：Desktop端重构（P0优先）

**Day 1**：
- [ ] Step 2.1：废弃LYBT.Desktop.Consultation模块
- [ ] 功能迁移到MedicalCaseManagementViewModel
- [ ] 更新导航配置

**Day 2-3**：
- [ ] Step 2.2：重构LYBT.Desktop.Prescriptions模块
- [ ] 选择方案A或B
- [ ] 移动文件、更新命名空间引用
- [ ] 测试所有处方相关功能

**Day 4**：
- [ ] Step 2.3：术语修正（代码注释、XAML注释）
- [ ] 完整的UI测试
- [ ] 创建PR，代码审查

### Week 3：文档更新与验证（P1）

**Day 1**：
- [ ] Step 3.1：更新clinical-workflow-ux-design-discussion.md
- [ ] Step 3.2：更新consultation-view-architecture-clarification.md
- [ ] Step 3.3：更新Client端架构文档

**Day 2**：
- [ ] Step 4：数据库Schema验证（可选）
- [ ] 清理孤立数据
- [ ] 修正外键约束（如需要）

**Day 3**：
- [ ] 完整的回归测试
- [ ] 用户验收测试（UAT）
- [ ] 文档审查

---

## 6. 风险评估与缓解措施

### 风险1：旧API客户端仍在使用（高风险）

**影响**：Desktop端或其他客户端仍在调用ConsultationController.CreateConsultation等旧API

**缓解措施**：
- Phase 1标记Obsolete时，使用`error=true`（编译时报错，而非警告）
- 编译Desktop端项目，确保所有旧API调用被捕获
- 全局搜索`/api/v1/consultations`和`/api/v1/prescriptions`（排除只读查询）
- 集成测试覆盖所有API调用路径

### 风险2：数据库Schema不匹配（中风险）

**影响**：如果Consultation和Prescription不使用共享主键，可能存在1:N关系而非1:1

**缓解措施**：
- Phase 4执行数据库Schema验证
- 如果发现1:N关系，需要数据库迁移脚本
- 清理孤立数据（Consultation或Prescription没有对应的MedicalCase）

### 风险3：现有功能破坏（中风险）

**影响**：重构过程中可能破坏现有的就诊录入、处方管理功能

**缓解措施**：
- Phase 1、Phase 2分别创建独立PR，逐步合并
- 每个Phase完成后执行完整的回归测试
- 保留旧API（标记Obsolete但不删除），确保向后兼容
- UAT阶段让用户验证核心流程

### 风险4：文档更新不彻底（低风险）

**影响**：遗漏部分文档，团队仍使用错误术语

**缓解措施**：
- 全局搜索关键词："ConsultationView主框架"、"就诊主界面"、"核心就诊界面"
- 使用regex搜索：`Consultation.*主|Consultation.*核心|就诊.*主界面`
- Code Review时重点检查文档部分

---

## 7. 验收标准

### 7.1 Server端验收标准

- [ ] ConsultationController所有创建/更新/删除方法标记`[Obsolete("...", true)]`
- [ ] PrescriptionsController所有创建/更新/删除方法标记`[Obsolete("...", true)]`
- [ ] MedicalCaseController新增`PUT /api/v1/medicalcases/{id}/consultation`
- [ ] MedicalCaseController新增`PUT /api/v1/medicalcases/{id}/prescription`
- [ ] Desktop端项目编译通过（无Obsolete错误）
- [ ] 单元测试覆盖新增API
- [ ] 集成测试验证完整流程（创建病案 → 更新诊断 → 更新处方）

### 7.2 Desktop端验收标准

- [ ] LYBT.Desktop.Consultation模块已废弃或删除
- [ ] ConsultationManagementViewModel功能已迁移到MedicalCaseManagementViewModel
- [ ] LYBT.Desktop.Prescriptions模块已重构（移动到MedicalCase内部或重命名）
- [ ] 所有处方相关功能正常工作
- [ ] UI术语修正完成（代码注释、XAML注释）
- [ ] 完整的UI测试通过（病案录入 → 诊断录入 → 处方录入 → 保存 → 查询历史）

### 7.3 文档验收标准

- [ ] `clinical-workflow-ux-design-discussion.md`术语修正完成
- [ ] `consultation-view-architecture-clarification.md`添加执行结果记录
- [ ] `docs/architecture/client/README.md`更新完成
- [ ] `docs/architecture/server/README.md`更新完成（如需要）
- [ ] 全局搜索无遗漏的错误术语

### 7.4 UAT验收标准（用户验收）

- [ ] 用户能够顺利完成病案录入（患者选择 → 诊断录入 → 处方录入 → 保存）
- [ ] 用户能够查询历史病案（包含完整的诊断和处方信息）
- [ ] 用户能够更新病案的诊断信息
- [ ] 用户能够更新病案的处方信息
- [ ] 用户反馈：新术语"病案录入"比"就诊录入"更清晰（可选）

---

## 8. 下一步行动

### 立即行动（今天）

1. **用户确认修正方案**：
   - 是否同意Phase 1-4的修正方案？
   - 选择Desktop端重构方案（A移动到MedicalCase内部 vs B保留但重命名）？
   - 是否需要立即执行，还是先完成其他Q2-Q4讨论？

2. **创建GitHub Issues**：
   - Epic Issue：【架构纠正】MedicalCase聚合根强势修正（P0）
   - Issue 1：Server端API废弃与迁移（P0，1-2天）
   - Issue 2：Desktop端模块重构（P0，2-3天）
   - Issue 3：文档更新（P1，0.5-1天）
   - Issue 4：数据库Schema验证（P2，可选，0.5天）

3. **开始Phase 1执行**：
   - 如果用户同意，立即开始标记Obsolete和扩展API
   - 创建功能分支：`feature/architecture-correction-medicalcase-aggregate`

### 后续行动（本周内）

4. **完成Phase 1-2**：
   - Server端API修正（Day 1-2）
   - Desktop端重构（Day 3-5）

5. **文档更新**：
   - Phase 3文档更新（Day 6）

6. **UAT验收**：
   - Phase 4验收与测试（Day 7）

---

## 9. 附录

### 附录A：DDD聚合根原则

**聚合根（Aggregate Root）定义**：
- 聚合是一组相关对象的集合，作为数据修改的单元
- 聚合根是聚合的根实体，外部对象只能引用聚合根
- 所有对聚合内对象的访问必须通过聚合根进行

**聚合根边界规则**：
1. **事务边界**：聚合内的所有对象在同一个事务中修改
2. **一致性边界**：聚合内的所有对象保持一致性约束
3. **外部引用**：外部对象只能持有聚合根的ID引用，不能直接引用聚合内的子实体

**本项目应用**：
- ✅ MedicalCase是聚合根
- ✅ Consultation和Prescription是聚合内的子实体
- ✅ 外部只能通过MedicalCase.Id访问，不能直接访问Consultation或Prescription
- ✅ 创建、更新、删除操作必须通过MedicalCase聚合根进行
- ❌ 不允许独立的ConsultationRepository.Create()或PrescriptionRepository.Create()

---

### 附录B：共享主键模式（Shared Primary Key）

**1:1关系的实现方式**：

**方案1：共享主键（推荐）**
```sql
CREATE TABLE MedicalCases (
    Id UUID PRIMARY KEY,
    PatientId UUID NOT NULL,
    DoctorId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);

CREATE TABLE Consultations (
    Id UUID PRIMARY KEY,  -- 与MedicalCases.Id相同
    ChiefComplaint NVARCHAR(500),
    TCMDiagnosis NVARCHAR(500),
    FOREIGN KEY (Id) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);

CREATE TABLE Prescriptions (
    Id UUID PRIMARY KEY,  -- 与MedicalCases.Id相同
    PrescriptionNo NVARCHAR(50),
    Dosage INT,
    FOREIGN KEY (Id) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);
```

**优点**：
- 1:1关系强制保证（一个MedicalCase对应一个Consultation）
- 查询效率高（JOIN性能好）
- 级联删除自动处理

**方案2：外键引用（不推荐，允许1:N）**
```sql
CREATE TABLE Consultations (
    Id UUID PRIMARY KEY,
    MedicalCaseId UUID NOT NULL,
    FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id)
);
```

**缺点**：
- 不能强制1:1关系（一个MedicalCase可能有多个Consultation）
- 需要应用层保证唯一性

---

### 附录C：Issue #1463参考

**Issue #1463：以MedicalCase为中心的激进重构**

从MedicalCaseEntryViewModel的注释可以看出，团队已经进行了部分重构：
- Line 18："Issue #1463: 以MedicalCase为中心的激进重构"
- Line 274："Issue #1463: 使用聚合根模式"
- Line 316：使用`CreateWithDetailsAsync`一次性创建聚合根

**本次架构纠正**与Issue #1463的关系：
- Issue #1463已完成Desktop端的MedicalCaseEntryViewModel重构
- 本次任务是**完成Issue #1463未完成的部分**：
  - Server端API废弃
  - Desktop端Consultation和Prescription模块清理
  - 文档术语修正
- 可以视为**Issue #1463 Phase 2**

---

### 附录D：关键代码片段

**正确的聚合根创建（MedicalCaseEntryViewModel.cs）**：
```csharp
// Line 316-320
var result = await _medicalCaseRepository.CreateWithDetailsAsync(
    medicalCaseDto,
    consultationDto,
    null // 暂无处方
);
```

**正确的API设计（MedicalCaseController.cs）**：
```csharp
// Line 115-141
[HttpPost("with-details")]
public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> CreateWithDetails(
    [FromBody] MedicalCaseWithDetailsCreateDto dto)
{
    var result = await _medicalCaseService.CreateWithDetailsAsync(
        dto.MedicalCase,
        dto.Consultation,
        dto.Prescription);

    return HandleServiceResult(result, "医疗案例创建成功");
}
```

**错误的API（需废弃）**：
```csharp
// ConsultationController.cs Line 89
[HttpPost]
[Obsolete("不推荐使用。请通过 POST /api/medicalcases 创建医疗案例...", false)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation(...)
{
    var result = await _consultationService.CreateAsync(dto);
    // ❌ 独立创建Consultation，违反聚合根原则
}
```

---

## 📌 报告总结

**核心发现**：
1. ✅ 正确的架构已部分实现（Issue #1463，MedicalCaseEntryViewModel + MedicalCaseController）
2. ❌ 旧的错误架构尚未完全清理（ConsultationModule、ConsultationController、Prescriptions独立模块）
3. ⚠️ 新旧架构混杂，需要强势修正

**修正范围**：
- Server端：8个文件（标记Obsolete + 扩展API）
- Desktop端：多个文件（模块重构 + 术语修正）
- 文档：6个文件（术语修正 + 架构说明）

**执行时间**：
- Phase 1（Server端）：1-2天
- Phase 2（Desktop端）：2-3天
- Phase 3（文档）：0.5-1天
- Phase 4（Schema验证）：0.5天（可选）
- **总计**：5-7天

**下一步行动**：
1. 用户确认修正方案
2. 创建GitHub Epic Issue + 子Issues
3. 立即开始Phase 1执行

---

**报告结束**
