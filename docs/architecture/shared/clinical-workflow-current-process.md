# 就诊流程完整逻辑描述

> **文档版本**: v1.0
> **创建日期**: 2025-10-18
> **最后更新**: 2025-10-18
> **状态**: ✅ 基于现有架构描述
> **用途**: 需求讨论基础文档，避免上下文丢失

---

## 📋 文档目的

本文档是**需求讨论的基础文档**，记录当前就诊流程的完整逻辑。

**核心原则**：
- ✅ 所有需求讨论必须基于本文档进行
- ✅ 每明确一个问题或设计，立即更新本文档
- ✅ 本文档是唯一事实来源（Single Source of Truth）
- ✅ 避免上下文压缩导致关键信息丢失

**讨论规则**：
1. 提出问题 → 在文档中标记 `❓ [待讨论]`
2. 达成共识 → 更新文档标记 `✅ [已确认]`
3. 发现问题 → 标记 `❌ [当前问题]`
4. 改进方案 → 标记 `🔄 [改进方向]`

---

## 📚 目录

- [1. 核心架构逻辑](#1-核心架构逻辑)
- [2. UI层面用户操作流程](#2-ui层面用户操作流程)
- [3. 数据层面实体生命周期](#3-数据层面实体生命周期)
- [4. 流程可视化总结](#4-流程可视化总结)
- [5. 当前问题清单](#5-当前问题清单)
- [6. 改进方向](#6-改进方向)
- [7. 待讨论问题](#7-待讨论问题)

---

## 1. 核心架构逻辑

### 1.1 DDD聚合根设计 ✅ [已确认]

**核心原则**：**MedicalCase（医案）是聚合根**

```
MedicalCase（医案/就诊会话）= 一次完整的就诊记录
   ├── Consultation（诊断环节）- 1:1关系
   └── Prescription（处方环节）- 1:1关系
```

**关键概念定义**：

| 概念 | 定义 | 确认状态 |
|-----|------|---------|
| **医案（MedicalCase）** | 就诊会话/就诊实例（从患者到诊 → 完成看诊离开） | ✅ 已确认 |
| **诊断（Consultation）** | 就诊过程中的辩证环节（四诊、主诉、诊断结果） | ✅ 已确认 |
| **处方（Prescription）** | 就诊过程中的治疗方案（药材、剂量、用法） | ✅ 已确认 |

**错误理解（需避免）**：
- ❌ 医案 ≠ 疾病治疗周期（不跨越多次就诊）
- ❌ 医案 ≠ 挂号单位（挂号和医案是分离的）

### 1.2 实体关系 ✅ [已确认]

**1:1:1 严格关系**：
```
1个MedicalCase : 1个Consultation : 1个Prescription
```

**含义**：
- ✅ 一次就诊 = 一个诊断记录（不支持同一就诊多次修改诊断）
- ✅ 一次就诊 = 一个处方（不支持同一就诊开多个处方）
- ✅ 一次挂号 = 一次就诊（一号一诊，未来功能）

**代码验证**：
```csharp
// MedicalCaseDto
public Guid? ConsultationId { get; set; }  // ✅ 1:1关联
public Guid? PrescriptionId { get; set; }  // ✅ 1:1关联

// ConsultationDto
public Guid MedicalCaseId { get; set; }    // ✅ 属于医案

// PrescriptionDto
public Guid MedicalCaseId { get; set; }    // ✅ 属于医案
```

### 1.3 医案创建时机 ✅ [已确认]

**容器先于内容创建**：
```
1. 患者选择 → 立即创建MedicalCase（患者ID、医生ID、就诊日期）
             ↓ Status = Active, ConsultationId = null, PrescriptionId = null

2. 填写诊断 → 创建Consultation（关联MedicalCaseId）
             ↓ 更新 MedicalCase.ConsultationId = newConsultationId

3. 开处方 → 创建Prescription（关联MedicalCaseId）
           ↓ 更新 MedicalCase.PrescriptionId = newPrescriptionId

4. 完成看诊 → 更新 MedicalCase.Status = Completed
```

---

## 2. UI层面用户操作流程

### 2.1 完整5步流程

#### Step 1: HomeView - 医生主页

**当前功能** ✅ [已实现]：
```
HomeView (医生登录后首页)
├─ 顶部信息栏：医生姓名、今日统计、退出登录
├─ 主动作区：🩺 开始看诊（按钮）
├─ 导航命令组：
│  ├─ NavigateToPatientReceptionCommand
│  ├─ NavigateToMedicalCaseCommand
│  ├─ NavigateToPrescriptionQueryCommand
│  ├─ NavigateToHerbsCommand
│  ├─ NavigateToFormulasCommand
│  └─ EnterSystemManagementCommand（10+ 命令）
└─ 操作：点击"开始看诊" → 打开ClinicalWorkstation
```

**当前问题** ❌：
- ❌ 功能过载：10+ 导航命令，主次不清
- ❌ 缺少今日患者列表展示
- ❌ "开始看诊"按钮位置不突出
- ❌ 缺少快速统计信息（今日接诊数、待处理数）

**改进方向** 🔄：
```
改进后的HomeView设计：
├─ 主动作区（突出显示）：
│  ├─ 🩺 开始看诊（大按钮，绿色高亮）
│  ├─ 🔍 快速查找患者
│  └─ 📋 今日患者列表（点击直接进入看诊）
├─ 次要功能区（折叠菜单）：
│  └─ 患者管理、处方查询、数据统计、系统设置
└─ 信息展示区：
   ├─ 今日接诊：X 人
   ├─ 待完成处方：Y 个
   └─ 当前医生：姓名 + 退出登录
```

---

#### Step 2: PatientSelectionDialog - 患者选择对话框

**当前功能** ✅ [已实现，Issue #1457]：
```
PatientSelectionDialog (对话框)
├─ 搜索功能：支持姓名/拼音码/手机号
├─ 患者列表：ObservableCollection<PatientDto>
├─ 双击选择：DoubleClickSelectCommand
├─ 新建患者：NewPatientCommand（❌ TODO）
└─ 返回结果：SelectedPatient (PatientDto)
```

**调用代码**（ClinicalWorkstationViewModel.cs:332）：
```csharp
_dialogService.ShowDialog("PatientSelectionDialog", result =>
{
    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedPatient"))
    {
        _currentPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
        CurrentPatientName = _currentPatient.Name;
        // ❌ 缺少后续动作：创建病案或跳转到病案录入
    }
});
```

**当前问题** ❌：
- ❌ 患者选择后没有自动创建医案
- ❌ 患者选择后没有自动跳转到病案录入
- ❌ NewPatientCommand 是 TODO，未实现

**改进方向** 🔄：
```csharp
// 改进后的患者选择逻辑
_dialogService.ShowDialog("PatientSelectionDialog", async result =>
{
    if (result.Result == ButtonResult.OK)
    {
        _currentPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
        CurrentPatientName = _currentPatient.Name;

        // 🔄 检查是否有未完成的病案
        var unfinishedCase = await CheckUnfinishedMedicalCase(_currentPatient.Id);
        if (unfinishedCase != null)
        {
            // 继续上次病案
            NavigateToMedicalCase(unfinishedCase.Id);
        }
        else
        {
            // 🔄 创建新病案并跳转
            var newCase = await CreateMedicalCase(_currentPatient.Id);
            NavigateToMedicalCase(newCase.Id);
        }
    }
});
```

---

#### Step 3: ClinicalWorkstation - 临床工作台

**当前功能** ✅ [已实现，Issue #1463]：
```
ClinicalWorkstationView
├─ 顶部导航栏：当前患者名称、更换患者、退出
├─ 左侧菜单：
│  ├─ 病案录入（MedicalCaseEntryView）
│  ├─ 处方开具（PrescriptionView）
│  ├─ 患者管理（PatientManagementView）
│  └─ 历史记录（ConsultationManagementView）
├─ 主内容区：ClinicalContentRegion（Region容器）
└─ 导航逻辑：ExecuteNavigate(string targetView)
   ├─ "Diagnosis" → MedicalCaseEntryView
   ├─ "Prescription" → PrescriptionView
   ├─ "PatientManagement" → PatientManagementView
   └─ "History" → ConsultationManagementView
```

**当前问题** ❌：
- ❌ 缺少流程进度条（用户不知道当前在哪一步）
- ❌ 患者选择后默认导航到MedicalCaseEntryView，但未自动创建医案
- ❌ 流程不连贯，需要手动点击左侧菜单切换

**改进方向** 🔄：
```
改进后的ClinicalWorkstation：
├─ 流程进度条（新增）：
│  └─ [患者选择✓] → [病案录入●] → [处方开具] → [完成]
├─ 自动化操作（新增）：
│  ├─ 患者选择后自动创建医案
│  ├─ 病案保存后自动跳转到处方
│  └─ 处方保存后提示"是否继续看诊"
└─ 左侧菜单保持不变（支持手动跳转）
```

---

#### Step 4: MedicalCaseEntryView - 病案录入视图

**当前功能** ✅ [已实现，Issue #1463]：
```
MedicalCaseEntryView (在 ClinicalContentRegion 中)
├─ 患者信息：PatientName, MedicalCaseId（从导航参数获取）
├─ 四诊合参：
│  ├─ Inspection（望诊）
│  ├─ AuscultationOlfaction（闻诊）
│  ├─ Inquiry（问诊）
│  └─ Palpation（切诊）
├─ 诊断信息：
│  ├─ ChiefComplaint（主诉）
│  ├─ PresentIllness（现病史）
│  ├─ TCMDiagnosis（中医诊断）
│  └─ TreatmentPrinciple（治疗原则）
└─ 命令：
   ├─ SaveCommand - 保存诊疗记录
   ├─ PrescribeCommand - 验证 → 保存 → 导航到PrescriptionView
   ├─ ClearCommand - 清空表单
   └─ ImportHistoryCommand - 从历史导入（❌ TODO）
```

**核心逻辑**（Prescribe方法，Lines 379-405）：
```csharp
private async void Prescribe()
{
    // 验证数据
    if (!ValidateInput()) return;

    // 保存诊疗记录
    await SaveAsync();

    // 导航到处方录入
    NavigationParameters parameters = new();
    parameters.Add("MedicalCaseId", MedicalCaseId);
    parameters.Add("PatientName", PatientName);

    _regionManager.RequestNavigate("ClinicalContentRegion", "PrescriptionView", parameters);
}
```

**当前问题** ❌：
- ⚠️ `MedicalCaseId` 在 `OnNavigatedTo` 中初始化，但可能为空（如果患者选择后未创建医案）
- ❌ 缺少自动保存草稿功能
- ❌ `ImportHistoryCommand` 是 TODO，未实现

**改进方向** 🔄：
- 🔄 患者选择后自动创建医案，确保MedicalCaseId不为空
- 🔄 实现ImportHistory功能（从历史诊断导入数据）
- 🔄 添加自动保存草稿（定时或失焦时保存）

---

#### Step 5: PrescriptionView - 处方录入视图

**当前功能** ✅ [已实现]：
```
PrescriptionView (在 ClinicalContentRegion 中)
├─ 患者信息：PatientName, MedicalCaseId（从导航参数获取）
├─ 处方项管理：
│  ├─ PrescriptionItems: ObservableCollection<PrescriptionItemViewModel>
│  └─ ItemRows: ObservableCollection<PrescriptionItemRowViewModel>（表格编辑）
├─ 快捷操作：
│  ├─ ImportFormulaCommand - 导入验方（Issue #1366, #1354）
│  ├─ SearchHistoryCommand - 搜索历史处方（Issue #1374, #1476）
│  ├─ AddHerbCommand - 添加药材
│  └─ RemoveHerbCommand - 删除药材
├─ 价格计算：
│  ├─ SingleDosagePrice（单剂价格）
│  └─ TotalPrice（总价格 = 剂数 × 单剂价格）
└─ 命令：
   ├─ SaveCommand - 保存处方
   └─ ❌ 缺少 "完成看诊" 命令
```

**历史复制调用链**（Entry Method #3）：
```
PrescriptionView.xaml (搜索历史按钮)
  ↓ SearchHistoryCommand
PrescriptionCommandHandler.ExecuteSearchHistory()
  ↓ ShowDialog("PrescriptionSearchDialog")
PrescriptionSearchDialogViewModel
  ↓ 用户选择历史处方
  ↓ 返回 PrescriptionSearchResultDto
PrescriptionCommandHandler.OnHistorySelected
  ↓ ExecuteCopyFromHistory()
PrescriptionViewModel.ExecuteCopyFromHistory()
  ↓ 复制所有药材到当前处方
```

**当前问题** ❌：
- ❌ 缺少"完成看诊"按钮和命令
- ❌ 处方保存后没有提示下一步操作
- ❌ 流程不闭环，无法返回患者选择或主页

**改进方向** 🔄：
```csharp
// 新增 CompleteTreatmentCommand
private async void CompleteTreatment()
{
    // 1. 保存处方
    await SaveAsync();

    // 2. 更新医案状态为Completed
    await UpdateMedicalCaseStatus(MedicalCaseId, "Completed");

    // 3. 弹窗提示
    var result = _dialogService.ShowConfirmation(
        "处方已保存，是否继续看诊？",
        "完成看诊"
    );

    if (result == ButtonResult.Yes)
    {
        // 返回患者选择对话框（继续看下一位患者）
        NavigateToPatientSelection();
    }
    else
    {
        // 返回 HomeView
        NavigateToHome();
    }
}
```

---

## 3. 数据层面实体生命周期

### 3.1 MedicalCase 状态机

```
[创建阶段] 患者选择完成
   ↓ POST /api/medical-cases
MedicalCase:
   ├─ Id: Guid（新生成）
   ├─ PatientId: Guid（选中的患者ID）
   ├─ DoctorId: Guid（当前登录医生ID）
   ├─ VisitDate: DateTime（就诊日期，默认今天）
   ├─ Status: "Active"（进行中）
   ├─ ConsultationId: null（未填写诊断）
   └─ PrescriptionId: null（未开处方）

      ↓ 填写病案 → 点击"保存"或"下一步：开处方"

[诊断阶段] 创建Consultation
   ↓ POST /api/consultations
Consultation:
   ├─ Id: Guid（新生成）
   ├─ MedicalCaseId: Guid（关联医案）
   ├─ Inspection: string（望诊内容）
   ├─ AuscultationOlfaction: string（闻诊内容）
   ├─ Inquiry: string（问诊内容）
   ├─ Palpation: string（切诊内容）
   ├─ ChiefComplaint: string（主诉）
   ├─ PresentIllness: string（现病史）
   ├─ TCMDiagnosis: string（中医诊断）
   └─ TreatmentPrinciple: string（治疗原则）

   ↓ 更新 MedicalCase
   ↓ PUT /api/medical-cases/{id}
MedicalCase.ConsultationId = newConsultationId（关联诊断）

      ↓ 点击"开处方" → 导航到处方录入

[处方阶段] 创建Prescription
   ↓ POST /api/prescriptions
Prescription:
   ├─ Id: Guid（新生成）
   ├─ MedicalCaseId: Guid（关联医案）
   ├─ Dosages: int（剂数，例如7帖）
   ├─ Usage: string（用法医嘱，例如"水煎服，一日一剂"）
   ├─ SingleDosagePrice: decimal（单剂价格）
   ├─ TotalPrice: decimal（总价格 = 剂数 × 单剂价格）
   └─ Items: List<PrescriptionItem>（药材列表）
      ├─ HerbId: Guid（药材ID）
      ├─ HerbName: string（药材名称，例如"当归"）
      ├─ DosageAmount: decimal（剂量，例如15）
      └─ DosageUnit: string（单位，例如"克"）

   ↓ 更新 MedicalCase
   ↓ PUT /api/medical-cases/{id}
MedicalCase.PrescriptionId = newPrescriptionId（关联处方）

      ↓ 点击"完成看诊"

[完成阶段] 更新状态
   ↓ PUT /api/medical-cases/{id}/complete
MedicalCase.Status = "Completed"（已完成）

      ↓ 就诊结束

[最终状态]
MedicalCase:
   ├─ Status: "Completed"
   ├─ ConsultationId: 已关联（有值）
   ├─ PrescriptionId: 已关联（有值）
   └─ 1:1:1 完整关系建立 ✅
```

### 3.2 数据流向图

```
Client端                    Server端                    Database
────────────────────────────────────────────────────────────────────
HomeView
  ↓ 点击"开始看诊"
PatientSelectionDialog
  ↓ 选择患者
  ↓
ClinicalWorkstation      → POST /api/medical-cases  → MedicalCases表
  创建医案                  {                           ├─ Id
                            PatientId,                  ├─ PatientId
                            DoctorId,                   ├─ DoctorId
                            VisitDate,                  ├─ VisitDate
                            Status="Active"             ├─ Status="Active"
                          }                             ├─ ConsultationId=null
  ↓ 导航到                                              └─ PrescriptionId=null
MedicalCaseEntryView
  填写病案信息
  ↓ 点击"保存"或"开处方"
                         → POST /api/consultations  → Consultations表
  创建诊断                  {                           ├─ Id
                            MedicalCaseId,              ├─ MedicalCaseId
                            Inspection,                 ├─ Inspection
                            Inquiry,                    ├─ Inquiry
                            TCMDiagnosis,               ├─ TCMDiagnosis
                            ...                         └─ ...
                          }
                         → PUT /api/medical-cases/{id}
  更新医案                  {                         → MedicalCases表
                            ConsultationId            ├─ ConsultationId=已关联
                          }                           └─ ...
  ↓ 导航到
PrescriptionView
  填写处方信息
  ↓ 点击"保存"
                         → POST /api/prescriptions  → Prescriptions表
  创建处方                  {                           ├─ Id
                            MedicalCaseId,              ├─ MedicalCaseId
                            Dosages,                    ├─ Dosages
                            SingleDosagePrice,          ├─ SingleDosagePrice
                            TotalPrice,                 ├─ TotalPrice
                            Items: [...]                └─ Items (关联表)
                          }
                         → PUT /api/medical-cases/{id}
  更新医案                  {                         → MedicalCases表
                            PrescriptionId            ├─ PrescriptionId=已关联
                          }                           └─ ...
  ↓ 点击"完成看诊"
                         → PUT /api/medical-cases/{id}/complete
  完成就诊                  {                         → MedicalCases表
                            Status="Completed"        └─ Status="Completed"
                          }
  ↓
弹窗提示：是否继续看诊？
  ├─ 是 → 返回PatientSelectionDialog
  └─ 否 → 返回HomeView
```

---

## 4. 流程可视化总结

### 4.1 用户视角 vs 数据视角

```
用户视角（UI操作流程）：
选患者 → 填病案 → 开处方 → 完成
   ↓        ↓         ↓        ↓
数据视角（实体生命周期）：
MedicalCase → Consultation → Prescription → Status=Completed
  (容器)      (诊断内容)      (处方内容)      (关闭会话)
```

### 4.2 完整时间线

```
时间线：
T0: 医生登录 → HomeView
T1: 点击"开始看诊" → PatientSelectionDialog
T2: 选择患者 → 创建MedicalCase（Status=Active）
T3: 填写病案 → 创建Consultation + 更新MedicalCase.ConsultationId
T4: 开处方 → 创建Prescription + 更新MedicalCase.PrescriptionId
T5: 完成看诊 → 更新MedicalCase.Status=Completed
T6: 返回主页或继续看诊
```

---

## 5. 当前问题清单

### 5.1 架构层面

| 编号 | 问题描述 | 影响 | 优先级 |
|-----|---------|------|-------|
| ❌ ARCH-1 | 患者选择后没有自动创建医案 | 流程不完整，MedicalCaseId可能为空 | P0 |
| ❌ ARCH-2 | 病案保存后没有自动跳转到处方 | 需要手动点击左侧菜单 | P0 |
| ❌ ARCH-3 | 处方保存后没有完成看诊的闭环 | 流程中断，无法返回主页 | P0 |
| ❌ ARCH-4 | 缺少流程进度条 | 用户不知道当前在哪一步 | P1 |

### 5.2 UI/UX层面

| 编号 | 问题描述 | 影响 | 优先级 |
|-----|---------|------|-------|
| ❌ UX-1 | HomeView功能过载（10+导航命令） | 主次不清，易用性差 | P0 |
| ❌ UX-2 | "开始看诊"按钮位置不突出 | 主动作不明显 | P0 |
| ❌ UX-3 | 缺少今日患者列表展示 | 无法快速查看今日待诊患者 | P1 |
| ❌ UX-4 | 缺少快速统计信息 | 无法一眼看到今日接诊数 | P1 |

### 5.3 功能层面

| 编号 | 问题描述 | 影响 | 优先级 |
|-----|---------|------|-------|
| ❌ FUNC-1 | PatientSelectionDialog.NewPatientCommand是TODO | 无法快速新建患者 | P1 |
| ❌ FUNC-2 | MedicalCaseEntryView.ImportHistoryCommand是TODO | 无法从历史导入诊断 | P2 |
| ❌ FUNC-3 | 缺少自动保存草稿功能 | 意外关闭会丢失数据 | P2 |
| ❌ FUNC-4 | 缺少"完成看诊"命令 | 流程无法闭环 | P0 |

---

## 6. 改进方向

### 6.1 Phase 0：核心流程打通（高优先级）

**目标**：打通患者选择 → 病案创建 → 诊疗录入 → 处方开具的完整流程

| 任务编号 | 任务描述 | 工作量估算 |
|---------|---------|----------|
| TASK-P0-1 | 患者选择后自动创建医案 | 3h |
| TASK-P0-2 | 病案保存后自动跳转到处方 | 2h |
| TASK-P0-3 | 实现"完成看诊"功能 | 2h |
| TASK-P0-4 | 添加流程进度条 | 2h |
| **小计** | **Phase 0** | **9h** |

### 6.2 Phase 1：HomeView UI重新设计（高优先级）

**目标**：简化主页，突出"开始看诊"主动作

| 任务编号 | 任务描述 | 工作量估算 |
|---------|---------|----------|
| TASK-P1-1 | HomeView UI重新设计 | 2h |
| TASK-P1-2 | HomeViewModel逻辑调整 | 2h |
| TASK-P1-3 | 实现今日患者列表 | 2h |
| TASK-P1-4 | 实现今日统计数据 | 1h |
| **小计** | **Phase 1** | **7h** |

### 6.3 Phase 2：功能完善（中优先级）

**目标**：实现新建患者、历史导入、自动保存等功能

| 任务编号 | 任务描述 | 工作量估算 |
|---------|---------|----------|
| TASK-P2-1 | 实现NewPatientCommand | 3h |
| TASK-P2-2 | 实现ImportHistoryCommand | 3h |
| TASK-P2-3 | 实现自动保存草稿 | 2h |
| TASK-P2-4 | UI/UX细节优化 | 3h |
| **小计** | **Phase 2** | **11h** |

**总工作量**：27小时（约3-4天）

---

## 7. 待讨论问题

### 7.0 历史问题追溯（用户提出）

❓ [待讨论-Q0] **医案-诊断-处方关系混乱问题是否已澄清？**

**问题来源**：用户提出"之前开发过程中发现了一个问题：医案、诊断、处方关系出现混乱"

**调查时间**：2025-10-18

**调查结果**：

#### ✅ 关系已在架构层面澄清

**当前DTO定义（正确的1:1:1关系）**：
```csharp
// MedicalCaseDto (Lines 44-47)
public Guid? ConsultationId { get; set; }  // ✅ 1:1关联
public Guid? PrescriptionId { get; set; }  // ✅ 1:1关联

// ConsultationDto (Lines 17-19)
public Guid MedicalCaseId { get; set; }    // ✅ 属于医案（必须）

// PrescriptionDto (Lines 16-17)
public Guid MedicalCaseId { get; set; }    // ✅ 属于医案（必须）
```

**架构文档已确认**：
- ✅ `docs/architecture/shared/clinical-workflow-entity-relationships.md` 明确了1:1:1关系
- ✅ 明确了"容器先于内容创建"的原则
- ✅ 明确了MedicalCase是DDD聚合根

**重构记录**：
- ✅ 2025-10-18 git commit `97f0b731` - `refactor(desktop): 以MedicalCase为中心的激进架构重构 (#1463)`
- ✅ 该重构明确了MedicalCase为聚合根的架构设计

#### ⚠️ 发现历史遗留字段（未使用，但可能引起混淆）

**问题点1**：PrescriptionCreateDto的冗余字段
```csharp
// PrescriptionDtos.cs Line 196-197
[DisplayName("诊疗ID")]
public Guid? ConsultationId { get; set; }  // ⚠️ 历史遗留字段
```

**分析**：
- ❌ PrescriptionDto（实际数据模型）没有ConsultationId字段
- ❌ PrescriptionEntity（数据库实体）应该也没有此字段
- ⚠️ Service和Controller代码中未使用此字段
- ⚠️ 这是一个历史遗留的冗余字段，可能是早期设计的残留

**问题点2**：Consultation与MedicalCase共享主键的特殊设计
```csharp
// PrescriptionService.cs Line 468-469
/// <param name="targetConsultationId">目标诊疗记录ID（与MedicalCase共享主键）</param>
var targetMedicalCase = await _medicalCaseRepository.GetByIdAsync(targetConsultationId);
```

**分析**：
- ⚠️ Consultation和MedicalCase共享主键（ConsultationId = MedicalCaseId）
- ⚠️ 这是一个非常规的设计，可能增加理解难度
- ⚠️ 代码中有些地方用ConsultationId实际查询的是MedicalCase

#### 🎯 结论

| 问题 | 状态 | 说明 |
|-----|------|------|
| **架构设计关系** | ✅ 已澄清 | 1:1:1关系明确，MedicalCase是聚合根 |
| **DTO定义** | ✅ 已澄清 | MedicalCaseDto/ConsultationDto/PrescriptionDto关系正确 |
| **文档记录** | ✅ 已完成 | 实体关系文档已详细记录 |
| **代码重构** | ✅ 已完成 | #1463重构已实施 |
| **历史遗留字段** | ⚠️ 需清理 | PrescriptionCreateDto.ConsultationId未使用 |
| **共享主键设计** | ⚠️ 需说明 | Consultation和MedicalCase共享主键，需在文档中明确 |

#### 📋 建议后续动作

**优先级P2（技术债务清理）**：
1. 🔄 移除 `PrescriptionCreateDto.ConsultationId` 字段（历史遗留）
2. 🔄 在文档中明确说明"Consultation与MedicalCase共享主键"的设计决策
3. 🔄 统一代码中的命名（避免targetConsultationId实际查MedicalCase的混淆）

**是否创建Issue**：
- ⚠️ 建议创建技术债务Issue：`[Tech Debt] 清理Prescription相关的历史遗留字段`
- ⚠️ 建议创建文档Issue：`[Doc] 明确Consultation与MedicalCase共享主键的设计说明`

**用户决策**：❓ 待确认是否需要立即清理，或作为Phase 3技术债务处理

---

### 7.1 架构设计问题

❓ [待讨论-Q1] **医案创建时机的异常处理**
- **问题**：如果创建医案失败（网络问题、服务器错误），应该如何处理？
- **选项A**：重试创建，失败后提示用户
- **选项B**：允许离线创建，同步时上传
- **选项C**：阻止进入ClinicalWorkstation，返回HomeView
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

❓ [待讨论-Q2] **未完成医案的处理**
- **问题**：患者选择后，如果发现有未完成的医案（Status=Active），应该如何处理？
- **选项A**：自动继续上次医案（恢复数据）
- **选项B**：提示用户选择"继续上次"或"开始新医案"
- **选项C**：强制完成上次医案才能开始新医案
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

❓ [待讨论-Q3] **处方是否是必须的**
- **问题**：是否允许只填写诊断，不开处方就完成就诊？
- **场景**：患者只是来复诊，医生判断不需要调整处方
- **选项A**：允许，PrescriptionId可以为null
- **选项B**：不允许，必须开处方才能完成就诊
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

### 7.2 UI/UX设计问题

❓ [待讨论-Q4] **流程进度条的交互方式**
- **问题**：流程进度条是否允许用户点击跳转？
- **选项A**：只展示，不可点击（只作进度提示）
- **选项B**：可点击，允许用户跳转到任意步骤
- **选项C**：可点击，但只能跳转到已完成的步骤
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

❓ [待讨论-Q5] **"完成看诊"后的默认行为**
- **问题**：点击"完成看诊"后，应该默认做什么？
- **选项A**：弹窗询问"是否继续看诊"
- **选项B**：直接返回患者选择（假设继续看诊）
- **选项C**：直接返回HomeView（假设结束看诊）
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

### 7.3 数据模型问题

❓ [待讨论-Q6] **医案的编辑权限**
- **问题**：已完成的医案（Status=Completed）是否允许编辑？
- **场景**：医生发现诊断或处方有误，需要修改
- **选项A**：允许编辑，记录修改历史
- **选项B**：不允许编辑，创建新医案（关联旧医案）
- **选项C**：允许编辑，但需要特殊权限（如管理员）
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

❓ [待讨论-Q7] **诊断和处方的修改逻辑**
- **问题**：如果医生在处方阶段发现诊断有误，应该如何修改？
- **选项A**：允许返回病案录入视图修改（更新Consultation）
- **选项B**：在处方界面直接修改诊断
- **选项C**：不允许修改，只能重新创建医案
- **倾向**：❓ 待讨论
- **决策**：❓ 待确认

---

## 8. 文档变更记录

| 日期 | 版本 | 变更描述 | 修改人 |
|------|------|---------|-------|
| 2025-10-18 | v1.0 | 初始版本，基于现有架构描述 | Claude |

---

## 9. 参考文档

- **架构分析报告**：`docs/reports/clinical-workflow-analysis-2025-10-18.md`
- **实体关系文档**：`docs/architecture/shared/clinical-workflow-entity-relationships.md`
- **Client端架构指南**：`docs/architecture/client/README.md`
- **重构提交记录**：`git show 97f0b731`（Issue #1463）

---

**文档状态说明**：
- ✅ [已确认] - 已经与业务专家确认，可直接作为实施依据
- ❌ [当前问题] - 当前代码中存在的问题，需要修复
- 🔄 [改进方向] - 已提出改进方案，待实施
- ❓ [待讨论] - 需要与业务专家讨论确认的问题

**下一步**：逐个讨论"待讨论问题"章节的7个问题，明确后更新本文档。
