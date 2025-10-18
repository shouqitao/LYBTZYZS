# 看诊流程架构深度分析报告

**分析时间**: 2025-10-18
**分析目的**: 完整梳理现有看诊流程架构，为UI/UX重新设计提供依据
**核心目标**: 实现从患者选择到处方开具的完整闭环流程

---

## 📋 执行摘要

### 核心发现

**✅ 现有架构已有良好基础**：
- ✅ HomeView - 主页界面（医生登录后第一屏）
- ✅ ClinicalWorkstation - 临床工作台（看诊主界面）
- ✅ PatientSelectionDialog - 患者选择对话框（Issue #1457，Epic #1456）
- ✅ MedicalCaseEntryView - 病案录入视图（Issue #1463）
- ✅ PrescriptionView - 处方录入视图
- ✅ ClinicalNavigator - 临床导航服务

**⚠️ 关键问题**：
- ❌ **导航流程不完整**：患者选择后无法自动创建病案
- ❌ **UI/UX 不符合看诊习惯**：缺少流程引导
- ❌ **HomeView 功能过载**：按钮过多，主次不清

### 推荐实施路径（3个Phase）

**Phase 0（高优先级，2-3天）**：
1. 重新设计 HomeView UI/UX - 突出"开始看诊"主动作
2. 实现完整的看诊流程导航：患者选择 → 病案创建 → 诊疗录入 → 处方开具
3. 简化ClinicalWorkstation界面，移除冗余功能

**Phase 1（中优先级，3-4天）**：
1. 优化各个录入界面的UI/UX（病案、诊疗、处方）
2. 实现流程状态管理和进度提示
3. 添加快捷操作和历史数据导入

**Phase 2（低优先级，2-3天）**：
1. 实现高级功能（搜索、统计、打印）
2. 性能优化和用户体验提升
3. 单元测试和集成测试

---

## 🏗️ 现有架构分析

### 1. HomeView - 主页界面

**文件位置**：
- ViewModel: `src/Client/Desktop/Shell/ViewModels/HomeViewModel.cs`
- View: `src/Client/Desktop/Shell/Views/HomeView.xaml`

**现有功能**：
```csharp
// 核心命令
public DelegateCommand StartConsultationCommand { get; }  // 开始诊疗 → ClinicalWorkstationView
public DelegateCommand LogoutCommand { get; }

// 导航命令组（过多）
public DelegateCommand NavigateToPatientReceptionCommand { get; }
public DelegateCommand NavigateToMedicalCaseCommand { get; }
public DelegateCommand NavigateToPrescriptionQueryCommand { get; }
public DelegateCommand NavigateToHerbsCommand { get; }
public DelegateCommand NavigateToFormulasCommand { get; }
public DelegateCommand EnterSystemManagementCommand { get; }
// ... 更多导航命令
```

**问题诊断**：
- ❌ 功能过载：10+ 导航命令，主次不清
- ❌ 缺少今日患者列表展示
- ❌ "开始诊疗"按钮位置不突出
- ❌ 缺少快速统计信息（今日接诊数、待处理数）

**改进建议**：
```
✅ 主动作区（突出显示）：
   - 开始看诊（大按钮，醒目）
   - 快速查找患者
   - 今日患者列表（点击直接进入看诊）

✅ 次要功能区（折叠菜单）：
   - 患者管理
   - 处方查询
   - 数据统计
   - 系统设置

✅ 信息展示区：
   - 今日接诊：X 人
   - 待完成处方：Y 个
   - 当前医生：姓名 + 退出登录
```

---

### 2. ClinicalWorkstationView - 临床工作台

**文件位置**：
- ViewModel: `src/Client/Desktop/Workstations/ClinicalWorkstation/ViewModels/ClinicalWorkstationViewModel.cs`
- View: `src/Client/Desktop/Workstations/ClinicalWorkstation/Views/ClinicalWorkstationView.xaml`
- Navigator: `src/Client/Desktop/Workstations/ClinicalWorkstation/Navigation/ClinicalNavigator.cs`

**现有功能**：
```csharp
// 患者选择（Issue #1457）
public ICommand SelectPatientCommand { get; }  // 打开 PatientSelectionDialog

// 导航系统（Issue #1463）
private void ExecuteNavigate(string targetView)
{
    string viewName = targetView switch
    {
        "Diagnosis" => "MedicalCaseEntryView",      // 病案录入
        "Prescription" => "PrescriptionView",       // 处方录入
        "PatientManagement" => "PatientManagementView",
        "History" => "ConsultationManagementView",  // 历史记录
        _ => "MedicalCaseEntryView"
    };
}

// 当前状态
private PatientDto? _currentPatient;
private string _currentPatientName = "未选择";
```

**流程分析**：
```
当前流程：
HomeView
  → 点击"开始诊疗" → ClinicalWorkstation
  → 点击"选择患者" → PatientSelectionDialog
  → 选择患者 → 返回 PatientDto
  → 默认导航到 MedicalCaseEntryView (病案录入)

缺失环节：
❌ 患者选择后没有自动创建病案
❌ 病案创建后没有自动跳转到诊疗录入
❌ 诊疗录入完成后没有自动跳转到处方录入
❌ 缺少流程进度提示
```

**改进建议**：
```
完整流程设计：
ClinicalWorkstation.OnPatientSelected(PatientDto patient)
  → 检查是否有未完成的病案
    - 有 → 继续上次病案
    - 无 → 创建新病案
  → 自动导航到 MedicalCaseEntryView (病案录入)
  → 显示流程进度：[患者选择✓] → [病案录入] → [诊疗录入] → [处方开具]

MedicalCaseEntryView.OnSaved()
  → 自动导航到 Prescription 区域
  → 传递 MedicalCaseId

PrescriptionView.OnSaved()
  → 提示"处方已保存，是否继续看诊？"
  → 是 → 返回患者选择
  → 否 → 返回 HomeView
```

---

### 3. PatientSelectionDialog - 患者选择对话框

**文件位置**：
- ViewModel: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionDialogViewModel.cs`
- View: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml`

**现有功能**（✅ 完整实现，Issue #1457）：
```csharp
// 搜索功能
public string SearchKeyword { get; }  // 支持姓名/拼音码/手机号
public DelegateCommand SearchCommand { get; }

// 患者列表
public ObservableCollection<PatientDto> Patients { get; }
public PatientDto? SelectedPatient { get; }

// 快捷操作
public DelegateCommand<PatientDto> DoubleClickSelectCommand { get; }  // 双击选择
public DelegateCommand NewPatientCommand { get; }  // 快速新建患者（TODO）

// 返回结果
public event Action<IDialogResult>? RequestClose;
```

**调用流程**：
```csharp
// ClinicalWorkstationViewModel.cs - Line 332
_dialogService.ShowDialog("PatientSelectionDialog", result =>
{
    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedPatient"))
    {
        _currentPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
        CurrentPatientName = _currentPatient.Name;
        // ⚠️ 缺少后续动作：创建病案或跳转到病案录入
    }
});
```

**改进建议**：
```csharp
✅ 患者选择后的自动动作：
_dialogService.ShowDialog("PatientSelectionDialog", async result =>
{
    if (result.Result == ButtonResult.OK)
    {
        _currentPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
        CurrentPatientName = _currentPatient.Name;

        // 检查是否有未完成的病案
        var unfinishedCase = await CheckUnfinishedMedicalCase(_currentPatient.Id);
        if (unfinishedCase != null)
        {
            // 继续上次病案
            NavigateToMedicalCase(unfinishedCase.Id);
        }
        else
        {
            // 创建新病案并跳转
            var newCase = await CreateMedicalCase(_currentPatient.Id);
            NavigateToMedicalCase(newCase.Id);
        }
    }
});
```

---

### 4. MedicalCaseEntryView - 病案录入视图

**文件位置**：
- ViewModel: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseEntryViewModel.cs`
- View: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml`

**现有功能**：
```csharp
// 患者信息
public PatientDto? CurrentPatient { get; }
public string PatientName { get; }
public Guid MedicalCaseId { get; }

// 四诊合参
public string Inspection { get; }           // 望诊
public string AuscultationOlfaction { get; } // 闻诊
public string Inquiry { get; }               // 问诊
public string Palpation { get; }             // 切诊

// 诊断信息
public string ChiefComplaint { get; }        // 主诉
public string PresentIllness { get; }        // 现病史
public string TCMDiagnosis { get; }          // 中医诊断
public string TreatmentPrinciple { get; }    // 治疗原则

// 命令
public DelegateCommand SaveCommand { get; }
public DelegateCommand PrescribeCommand { get; }  // 开处方 → PrescriptionView
public DelegateCommand ClearCommand { get; }
public DelegateCommand ImportHistoryCommand { get; }
```

**核心逻辑 - Prescribe 方法**（Lines 379-405）：
```csharp
private void Prescribe()
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

**问题诊断**：
- ⚠️ `MedicalCaseId` 在 `OnNavigatedTo` 中初始化，但可能为空
- ⚠️ 缺少自动保存草稿功能
- ⚠️ 缺少历史诊断导入的完整实现（ImportHistory 是 TODO）

---

### 5. PrescriptionView - 处方录入视图

**文件位置**：
- ViewModel: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs`
- View: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml`

**现有功能**（已实现）：
```csharp
// 处方项管理
public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; }
public ObservableCollection<PrescriptionItemRowViewModel> ItemRows { get; }  // 表格编辑

// 历史处方复制（Issue #1374, #1476）
public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions { get; }
public DelegateCommand SearchHistoryCommand { get; }  // 打开 PrescriptionSearchDialog

// 验方导入（Issue #1366, #1354）
public DelegateCommand ImportFormulaCommand { get; }  // 打开 FormulaTemplateDialog

// 价格计算
public decimal SingleDosagePrice { get; }
public decimal TotalPrice { get; }

// 命令
public DelegateCommand SaveCommand { get; }
public DelegateCommand AddHerbCommand { get; }
public DelegateCommand<PrescriptionItemViewModel> RemoveHerbCommand { get; }
```

**完整调用链（Entry Method #3 - 历史复制）**：
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

---

## 🔄 完整看诊流程设计（推荐）

### 理想流程图

```
┌─────────────────────────────────────────────────────────────────┐
│ Phase 0: 入口设计                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  HomeView (医生登录后首页)                                       │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  [顶部信息栏]                                           │    │
│  │  医生：李医生  |  今日接诊：5人  |  待处方：2个  |  [退出] │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [主动作区 - 大按钮]                                    │    │
│  │      🩺 开始看诊（绿色高亮）                             │    │
│  │      🔍 快速查找患者                                     │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [今日患者列表]                                          │    │
│  │  王女士 | 45岁 | 10:30 预约 | [继续看诊]                │    │
│  │  张先生 | 32岁 | 11:00 预约 | [开始看诊]                │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [次要功能 - 折叠菜单]                                  │    │
│  │  ▶ 患者管理  ▶ 处方查询  ▶ 数据统计  ▶ 系统设置         │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  点击"开始看诊" ↓                                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Phase 1: 患者选择                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PatientSelectionDialog (对话框)                                 │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  选择患者                                         [×]    │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  搜索：[__________]  [搜索]  [新建患者]                │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [患者列表 DataGrid]                                    │    │
│  │  王女士  |  45岁  |  电话：138****  |  最近：10-15      │    │
│  │  张先生  |  32岁  |  电话：139****  |  最近：10-10      │    │
│  │  ... (双击或点击确定选择)                               │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │                              [确定]  [取消]              │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  选择患者后 ↓                                                   │
│  1. 检查是否有未完成的病案                                       │
│     - 有 → 继续上次病案                                         │
│     - 无 → 自动创建新病案                                       │
│  2. 导航到 ClinicalWorkstation                                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Phase 2: 临床工作台（看诊主界面）                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ClinicalWorkstationView                                         │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  [顶部导航栏]                                           │    │
│  │  当前患者：王女士（45岁）  |  [更换患者]  |  [退出]      │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [流程进度条]                                            │    │
│  │  [患者选择✓] → [病案录入●] → [诊疗录入] → [处方开具]     │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [左侧菜单]  │  [主内容区 - ClinicalContentRegion]       │    │
│  │  ● 病案录入  │  (显示 MedicalCaseEntryView)             │    │
│  │  ○ 诊疗录入  │                                          │    │
│  │  ○ 处方开具  │                                          │    │
│  │  ○ 历史记录  │                                          │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Phase 3: 病案录入                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  MedicalCaseEntryView (在 ClinicalContentRegion 中)              │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  患者：王女士  |  年龄：45岁  |  就诊日期：2025-10-18     │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [四诊合参]                                              │    │
│  │  望诊：[__________________________________]               │    │
│  │  闻诊：[__________________________________]               │    │
│  │  问诊：[__________________________________]               │    │
│  │  切诊：[__________________________________]               │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [诊断信息]                                              │    │
│  │  主诉：[__________________________________]               │    │
│  │  现病史：[______________________________]                │    │
│  │  中医诊断：[____________________________]                │    │
│  │  治疗原则：[____________________________]                │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [导入历史]  [清空]  [保存]  [下一步：开处方]           │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  点击"下一步：开处方" ↓                                         │
│  1. 验证必填字段                                                │
│  2. 保存诊疗记录                                                │
│  3. 自动导航到 PrescriptionView                                 │
│  4. 更新流程进度条：[患者选择✓] → [病案录入✓] → [处方开具●]     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Phase 4: 处方开具                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PrescriptionView (在 ClinicalContentRegion 中)                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  患者：王女士  |  诊断：心脾两虚证                       │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [快捷操作]                                              │    │
│  │  [导入验方]  [搜索历史...]  [添加药材]  [清空]          │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [处方网格 - 4行6列表格编辑]                            │    │
│  │  当归 15g  |  黄芪 30g  |  党参 15g  |  白术 10g         │    │
│  │  茯苓 15g  |  远志 10g  |  酸枣仁 15g |  [空]            │    │
│  │  ...                                                     │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  剂数：[7] 帖  |  单价：￥28.50  |  总价：￥199.50       │    │
│  │  医嘱：[水煎服，一日一剂，早晚温服，饭后服]               │    │
│  ├────────────────────────────────────────────────────────┤    │
│  │  [保存]  [打印]  [完成看诊]                             │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  点击"完成看诊" ↓                                               │
│  1. 保存处方                                                    │
│  2. 提示"处方已保存，是否继续看诊？"                             │
│     - 是 → 返回患者选择（继续看下一位患者）                     │
│     - 否 → 返回 HomeView                                        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 实施任务清单（按Phase分组）

### Phase 0：HomeView UI/UX重新设计（高优先级）

**目标**：简化主页，突出"开始看诊"主动作，提升易用性

#### TASK-PHASE0-1：HomeView UI 重新设计（UI设计，2h）
- [ ] 设计新的 HomeView.xaml 布局
  - 顶部信息栏（医生姓名、今日统计、退出登录）
  - 主动作区（开始看诊大按钮、快速查找患者）
  - 今日患者列表（显示预约患者，点击直接进入看诊）
  - 次要功能折叠菜单（患者管理、处方查询、数据统计、系统设置）

#### TASK-PHASE0-2：HomeViewModel 逻辑调整（Backend，2h）
- [ ] 移除冗余的导航命令（保留核心功能）
- [ ] 实现今日患者列表加载（调用 PatientRepository）
- [ ] 实现今日统计数据加载（接诊数、待处方数）
- [ ] 优化 StartConsultationCommand 逻辑（直接打开患者选择对话框）

#### TASK-PHASE0-3：创建 GitHub Issue（规划，0.5h）
- [ ] 创建 Epic：`[Epic] 看诊流程UI/UX重新设计`
- [ ] 创建子 Issue：
  - `[Phase0-1] HomeView UI重新设计`
  - `[Phase0-2] HomeViewModel逻辑优化`

**工作量估算**：4.5 小时

---

### Phase 1：ClinicalWorkstation 流程完善（高优先级）

**目标**：打通患者选择 → 病案创建 → 诊疗录入 → 处方开具的完整流程

#### TASK-PHASE1-1：患者选择后自动创建病案（Backend，3h）
- [ ] 实现 `ClinicalWorkstationViewModel.OnPatientSelected()` 方法
  - 检查患者是否有未完成的病案（调用 MedicalCaseRepository）
  - 有 → 继续上次病案（导航到 MedicalCaseEntryView）
  - 无 → 自动创建新病案（调用 MedicalCaseRepository.CreateAsync）
  - 传递 MedicalCaseId 和 Patient 到 MedicalCaseEntryView

#### TASK-PHASE1-2：MedicalCaseEntryView 流程优化（Backend，2h）
- [ ] 修改 `Prescribe()` 方法逻辑
  - 验证必填字段
  - 自动保存诊疗记录
  - 导航到 PrescriptionView
  - 传递 MedicalCaseId 和 PatientName
- [ ] 实现 `ImportHistory` 功能（从历史诊断导入数据）

#### TASK-PHASE1-3：PrescriptionView 完成看诊流程（Backend，2h）
- [ ] 添加"完成看诊"按钮和命令
- [ ] 实现 `CompleteTreatment()` 方法
  - 保存处方
  - 弹窗提示"处方已保存，是否继续看诊？"
  - 是 → 返回患者选择对话框
  - 否 → 返回 HomeView

#### TASK-PHASE1-4：流程进度条实现（UI，2h）
- [ ] 在 ClinicalWorkstationView 添加流程进度条 UI
- [ ] 实时更新进度状态：
  - 患者选择 → 病案录入 → 诊疗录入 → 处方开具
- [ ] 点击进度条可快速跳转到对应步骤

#### TASK-PHASE1-5：PatientSelectionDialog 新建患者功能（Backend，3h）
- [ ] 实现 `NewPatientCommand` 逻辑（当前是 TODO）
- [ ] 打开快速新建患者对话框
- [ ] 创建患者后自动选中并返回

#### TASK-PHASE1-6：创建 GitHub Issue（规划，0.5h）
- [ ] 创建子 Issue：
  - `[Phase1-1] 患者选择后自动创建病案`
  - `[Phase1-2] MedicalCaseEntryView流程优化`
  - `[Phase1-3] PrescriptionView完成看诊流程`
  - `[Phase1-4] 流程进度条实现`
  - `[Phase1-5] PatientSelectionDialog新建患者功能`

**工作量估算**：12.5 小时

---

### Phase 2：各界面 UI/UX 优化（中优先级）

**目标**：优化各个录入界面的用户体验，提升录入效率

#### TASK-PHASE2-1：MedicalCaseEntryView UI 优化（UI，3h）
- [ ] 优化四诊合参输入框布局（更符合中医习惯）
- [ ] 添加常用术语快捷输入（下拉提示）
- [ ] 实现历史诊断导入 UI（点击历史记录一键导入）
- [ ] 添加字段验证提示（必填字段高亮）

#### TASK-PHASE2-2：PrescriptionView 表格编辑优化（UI，4h）
- [ ] 完善 Entry Method #1（表格智能编辑）
  - 实现 PrescriptionItemRowViewModel 模型
  - 设计 8 列 DataGrid XAML
  - 实现焦点自动跳转逻辑（Enter 键）
  - 实现拼音码过滤（ComboBox）

#### TASK-PHASE2-3：验方导入和历史复制 UI 优化（UI，2h）
- [ ] 优化 FormulaTemplateDialog 界面（仅显示已验证验方）
- [ ] 优化 PrescriptionSearchDialog 界面（搜索结果展示）
- [ ] 添加导入成功提示和动画效果

#### TASK-PHASE2-4：快捷键支持（Backend，2h）
- [ ] 实现全局快捷键：
  - Ctrl+N: 新建患者
  - Ctrl+S: 保存当前数据
  - Ctrl+P: 打印处方
  - Ctrl+Enter: 下一步/完成

#### TASK-PHASE2-5：创建 GitHub Issue（规划，0.5h）
- [ ] 创建子 Issue：
  - `[Phase2-1] MedicalCaseEntryView UI优化`
  - `[Phase2-2] PrescriptionView表格编辑优化`
  - `[Phase2-3] 验方导入和历史复制UI优化`
  - `[Phase2-4] 快捷键支持`

**工作量估算**：11.5 小时

---

## 📊 总工作量估算

| Phase | 任务数 | 工作量（小时） | 预计完成时间 |
|-------|-------|--------------|------------|
| **Phase 0** | 2 | 4.5 | 0.5-1天 |
| **Phase 1** | 5 | 12.5 | 1.5-2天 |
| **Phase 2** | 4 | 11.5 | 1.5-2天 |
| **总计** | **11** | **28.5** | **3.5-5天** |

---

## 🎯 成功标准

### 1. 流程完整性（P0）
- [x] 患者选择功能正常
- [ ] 患者选择后自动创建病案
- [ ] 病案录入完成后自动跳转到处方
- [ ] 处方保存后可选择继续看诊或返回主页

### 2. UI/UX 友好性（P0）
- [ ] HomeView 主次分明，"开始看诊"按钮突出
- [ ] 流程进度条实时更新
- [ ] 所有界面响应速度 < 2 秒
- [ ] 表单验证提示清晰

### 3. 数据准确性（P0）
- [ ] 病案数据正确保存
- [ ] 处方数据正确关联
- [ ] 价格计算准确无误

### 4. 用户反馈（P1）
- [ ] 医生实际试用反馈良好
- [ ] 录入效率提升 30%+

---

## 📚 参考文档

- **架构指南**：`docs/architecture/client/README.md`
- **Epic #1456**：看诊流程完整实现
- **Issue #1457**：临床工作台患者选择功能
- **Issue #1463**：激进重构 - 导航到MedicalCaseEntryView
- **Phase 2 调查报告**：`docs/reports/phase2-code-investigation-2025-10-18.md`

---

**报告完成时间**：2025-10-18
**下一步**：用户确认实施路径，创建 Phase 0 GitHub Issues 并开始开发
