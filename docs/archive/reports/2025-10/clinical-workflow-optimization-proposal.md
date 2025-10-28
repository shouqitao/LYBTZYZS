# 诊疗工作台看诊流程优化方案

**创建日期**: 2025-10-18  
**Issue**: #1454 修复后的架构优化建议  
**状态**: 提案 (Proposal)  
**优先级**: P0 - 核心业务流程  

---

## 📋 执行摘要 (Executive Summary)

**问题现状**: Issue #1454修复了诊疗录入导航错误(DiagnosisView不存在),但发现更深层的架构问题:
- ✅ **已解决**: 导航到ConsultationManagementView不再空白
- ❌ **新发现**: ConsultationManagementView是**历史记录查看器**,不是诊疗录入表单
- ❌ **架构缺失**: 缺少完整的看诊流程(患者选择 → 诊疗录入 → 处方开具)

**优化目标**: 建立完整的临床工作流程,优先实现**看诊逻辑**(根据用户要求"看诊逻辑优先")

---

## 🎯 一、问题分析

### 1.1 当前架构现状

#### ✅ 已实现模块
| 模块 | 功能 | 状态 |
|------|------|------|
| **Consultation** | 诊疗记录管理 | ✅ 历史查看完整 |
| **Prescriptions** | 处方管理 | ✅ 录入/查看完整 |
| **Patients** | 患者管理 | ⚠️ 仅详情/导入 |

#### ❌ 缺失功能
| 缺失项 | 影响 | 优先级 |
|--------|------|--------|
| **诊疗录入视图** (ConsultationEntryView) | 无法进行四诊录入 | P0 |
| **患者选择流程** (PatientSelectionDialog) | 无法选择就诊患者 | P0 |
| **医疗案例创建** (MedicalCase流程) | 诊疗与处方关联断裂 | P1 |

### 1.2 数据流分析

#### 现有数据模型结构
```
ConsultationDto (诊疗记录)
├─ MedicalCaseId (医疗案例ID) ⭐ 核心关联
├─ PatientId (患者ID)
├─ UserId (医生ID)
├─ 四诊数据
│  ├─ Inspection (望诊)
│  ├─ AuscultationOlfaction (闻诊)
│  ├─ Inquiry (问诊)
│  └─ Palpation (切诊)
├─ ChiefComplaint (主诉)
├─ PresentIllness (现病史)
├─ TCMDiagnosis (中医诊断)
└─ TreatmentPrinciple (治疗原则)

PrescriptionViewModel
└─ 需要 MedicalCaseId 才能初始化 ⭐
```

#### 理想的数据流
```
1. 患者选择 → 创建/获取 PatientId
2. 创建医疗案例 → 生成 MedicalCaseId
3. 诊疗录入 → 保存 ConsultationDto (关联 MedicalCaseId)
4. 处方开具 → PrescriptionViewModel(MedicalCaseId)
5. 完成看诊 → 更新诊疗状态
```

### 1.3 用户路径断点

#### 当前ClinicalWorkstationView菜单
| 菜单项 | 导航目标 | 实际功能 | 问题 |
|--------|---------|---------|------|
| 诊断录入 | ConsultationManagementView | 历史记录查看 | ❌ 名称与功能不符 |
| 处方管理 | PrescriptionView | 处方编辑 | ⚠️ 缺少MedicalCaseId |
| 患者管理 | PatientManagementView | TODO | ❌ 未实现 |
| 历史记录 | HistoryView | TODO | ❌ 未实现 |

---

## 🚀 二、优化方案

### 2.1 核心设计原则

1. **MVP优先** - 够用即好,避免过度设计
2. **看诊逻辑优先** - 先实现核心临床流程,后续再补充高级功能
3. **数据流清晰** - 患者 → 医疗案例 → 诊疗/处方 的关联明确
4. **用户体验连贯** - 减少跳转,流程自然流畅

### 2.2 新增组件清单

#### Phase 1: 核心看诊流程 (MVP) ⭐ 优先

| 组件 | 类型 | 功能 | 工作量 |
|------|------|------|--------|
| **PatientSelectionDialog** | Dialog | 患者选择/新建 | 2天 |
| **ConsultationEntryView** | View | 诊疗录入表单 | 3天 |
| **ConsultationEntryViewModel** | ViewModel | 四诊数据管理 | 2天 |
| **MedicalCaseService** | Service | 医疗案例自动创建 | 1天 |

**小计**: 约8工作日

#### Phase 2: 流程优化 (后续)

| 组件 | 类型 | 功能 | 工作量 |
|------|------|------|--------|
| PatientQuickInfoPanel | Component | 患者信息快速展示 | 1天 |
| ConsultationHistoryPanel | Component | 患者历史诊疗记录 | 1天 |
| WorkflowStatusIndicator | Component | 看诊流程进度提示 | 0.5天 |

**小计**: 约2.5工作日

### 2.3 菜单结构重构

#### 重构前 (问题)
```
临床工作台
├─ 诊断录入 → ConsultationManagementView (历史记录,名称不符!)
├─ 处方管理 → PrescriptionView (缺少患者上下文)
├─ 患者管理 → TODO
└─ 历史记录 → TODO
```

#### 重构后 (清晰)
```
临床工作台
├─ 看诊录入 → ConsultationEntryView (新建,四诊录入)
│  └─ 自动流转到 → PrescriptionView (带MedicalCaseId)
├─ 处方管理 → PrescriptionManagementView (独立处方查看)
├─ 诊疗历史 → ConsultationManagementView (重命名,明确用途)
└─ 患者信息 → PatientDetailView (只读查看)
```

#### 顶部工具栏新增
```
[患者选择按钮] → PatientSelectionDialog
  ├─ 快速搜索 (拼音码/姓名/手机)
  ├─ 常用患者列表
  └─ 新建患者入口
```

### 2.4 完整看诊流程设计

#### 流程图
```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: 选择患者                                                  │
├─────────────────────────────────────────────────────────────────┤
│ ClinicalWorkstation → [选择患者] 按钮                              │
│   ↓                                                              │
│ PatientSelectionDialog 打开                                      │
│   ├─ 搜索现有患者 (拼音码/姓名)                                     │
│   │   → 选择 → 返回 PatientId                                     │
│   └─ 新建患者 → PatientQuickCreateForm → 保存 → 返回 PatientId      │
│   ↓                                                              │
│ 工作台顶部显示: "当前患者: 张三 | 男 | 35岁"                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Step 2: 诊疗录入                                                  │
├─────────────────────────────────────────────────────────────────┤
│ 点击 [看诊录入] 菜单                                              │
│   ↓                                                              │
│ ConsultationEntryView 打开                                       │
│   ├─ 自动创建 MedicalCase (PatientId + UserId)                    │
│   ├─ 四诊录入区域                                                 │
│   │   ├─ 望诊 (面色、舌象等)                                       │
│   │   ├─ 闻诊 (声音、气味)                                        │
│   │   ├─ 问诊 (主诉、现病史、既往史)                                │
│   │   └─ 切诊 (脉象)                                             │
│   ├─ 中医诊断区域                                                 │
│   │   ├─ 病名诊断 (下拉选择 + 手动输入)                             │
│   │   └─ 证型诊断 (多选)                                          │
│   └─ 治疗原则 (生成建议 + 可编辑)                                   │
│   ↓                                                              │
│ [保存诊疗记录] → ConsultationDto 存入数据库                         │
│   ↓                                                              │
│ 弹窗提示: "诊疗记录已保存,是否开具处方?"                             │
│   ├─ [是] → 自动跳转 PrescriptionView (MedicalCaseId)             │
│   └─ [否] → 停留在诊疗录入页                                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Step 3: 处方开具                                                  │
├─────────────────────────────────────────────────────────────────┤
│ PrescriptionView(MedicalCaseId) 初始化                            │
│   ↓                                                              │
│ 自动加载:                                                         │
│   ├─ 患者信息 (从 MedicalCase)                                    │
│   ├─ 诊疗记录 (最新的 Consultation)                                │
│   └─ 历史处方 (PatientId 关联)                                     │
│   ↓                                                              │
│ 处方录入 (8列DataGrid,已实现)                                      │
│   ├─ 药材选择 (拼音码过滤,已实现)                                   │
│   ├─ 用量设置 (Enter跳转,已实现)                                   │
│   ├─ 验方导入 (已实现)                                            │
│   └─ 历史处方复制 (已实现)                                         │
│   ↓                                                              │
│ [保存处方] → PrescriptionDto 存入数据库 (关联 MedicalCaseId)         │
│   ↓                                                              │
│ [打印处方] / [完成看诊] → 更新 Consultation.Status = Completed      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Step 4: 历史查看                                                  │
├─────────────────────────────────────────────────────────────────┤
│ 点击 [诊疗历史] 菜单                                              │
│   ↓                                                              │
│ ConsultationManagementView (已存在,重命名菜单项)                    │
│   ├─ 筛选: 当前患者 / 全部患者                                     │
│   ├─ 查看详情 → ConsultationDetailView (只读)                     │
│   ├─ 查看处方 → PrescriptionDetailView (只读)                     │
│   └─ 复制病历 → 新建 Consultation (预填充数据)                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ 三、实施计划

### 3.1 Phase 1: 核心MVP (8工作日)

#### Issue拆分建议

| Issue编号 | 任务名称 | 依赖 | 工作量 |
|----------|---------|------|--------|
| #TBD-1 | 创建PatientSelectionDialog患者选择对话框 | 无 | 2天 |
| #TBD-2 | 创建ConsultationEntryView诊疗录入视图 | #TBD-1 | 3天 |
| #TBD-3 | 实现MedicalCaseService自动案例管理 | 无 | 1天 |
| #TBD-4 | 重构ClinicalWorkstation菜单导航 | #TBD-2, #TBD-3 | 1天 |
| #TBD-5 | 集成PrescriptionView工作流跳转 | #TBD-2, #TBD-3 | 1天 |

#### 技术实施细节

##### 1. PatientSelectionDialog

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml`

**关键功能**:
```csharp
public class PatientSelectionDialogViewModel : UnifiedViewModelBase
{
    // 搜索框 - 支持拼音码过滤
    public string SearchKeyword { get; set; }
    
    // 患者列表 - 最近就诊 + 搜索结果
    public ObservableCollection<PatientDto> Patients { get; set; }
    
    // 选中患者
    public PatientDto? SelectedPatient { get; set; }
    
    // 命令
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand NewPatientCommand { get; } // 快速新建患者
}
```

**UI结构** (参考Prescriptions模块的FormulaSelectionDialog):
```xml
<Grid>
    <!-- 搜索框 -->
    <TextBox Text="{Binding SearchKeyword}" Watermark="输入姓名/拼音码/手机号搜索"/>
    
    <!-- 患者列表 -->
    <DataGrid ItemsSource="{Binding Patients}" SelectedItem="{Binding SelectedPatient}">
        <DataGrid.Columns>
            <DataGridTextColumn Header="姓名" Binding="{Binding Name}"/>
            <DataGridTextColumn Header="性别" Binding="{Binding Gender}"/>
            <DataGridTextColumn Header="年龄" Binding="{Binding Age}"/>
            <DataGridTextColumn Header="手机" Binding="{Binding PhoneNumber}"/>
            <DataGridTextColumn Header="最近就诊" Binding="{Binding LastVisitDate}"/>
        </DataGrid.Columns>
    </DataGrid>
    
    <!-- 操作按钮 -->
    <StackPanel Orientation="Horizontal">
        <Button Content="新建患者" Command="{Binding NewPatientCommand}"/>
        <Button Content="确定" Command="{Binding ConfirmCommand}"/>
        <Button Content="取消" IsCancel="True"/>
    </StackPanel>
</Grid>
```

##### 2. ConsultationEntryView

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationEntryView.xaml`

**ViewModel结构**:
```csharp
public class ConsultationEntryViewModel : UnifiedViewModelBase
{
    // 关联数据
    private Guid _medicalCaseId;
    private PatientDto? _currentPatient;
    
    // 四诊数据绑定 (对应 ConsultationDto)
    public string Inspection { get; set; }           // 望诊
    public string AuscultationOlfaction { get; set; } // 闻诊
    public string Inquiry { get; set; }               // 问诊
    public string Palpation { get; set; }             // 切诊
    
    // 主诉与病史
    public string ChiefComplaint { get; set; }
    public string PresentIllness { get; set; }
    
    // 诊断与治疗
    public string TCMDiagnosis { get; set; }
    public string TreatmentPrinciple { get; set; }
    
    // 命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand SaveAndPrescribeCommand { get; } // 保存并开处方
    public DelegateCommand ClearCommand { get; }
}
```

**UI布局** (四诊分区):
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- 患者信息栏 -->
        <RowDefinition Height="*"/>    <!-- 四诊录入区 -->
        <RowDefinition Height="Auto"/> <!-- 诊断与治疗 -->
        <RowDefinition Height="Auto"/> <!-- 操作按钮 -->
    </Grid.RowDefinitions>
    
    <!-- Row 0: 患者信息 -->
    <Border Grid.Row="0" Background="LightBlue" Padding="10">
        <TextBlock Text="{Binding PatientInfo}" FontSize="14" FontWeight="Bold"/>
    </Border>
    
    <!-- Row 1: 四诊录入 (4列布局) -->
    <Grid Grid.Row="1">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <!-- 望诊 -->
        <GroupBox Grid.Column="0" Header="望诊">
            <TextBox Text="{Binding Inspection}" AcceptsReturn="True" TextWrapping="Wrap"/>
        </GroupBox>
        
        <!-- 闻诊 -->
        <GroupBox Grid.Column="1" Header="闻诊">
            <TextBox Text="{Binding AuscultationOlfaction}" AcceptsReturn="True"/>
        </GroupBox>
        
        <!-- 问诊 -->
        <GroupBox Grid.Column="2" Header="问诊">
            <StackPanel>
                <Label Content="主诉:"/>
                <TextBox Text="{Binding ChiefComplaint}" Height="60" AcceptsReturn="True"/>
                <Label Content="现病史:"/>
                <TextBox Text="{Binding PresentIllness}" Height="120" AcceptsReturn="True"/>
            </StackPanel>
        </GroupBox>
        
        <!-- 切诊 -->
        <GroupBox Grid.Column="3" Header="切诊">
            <TextBox Text="{Binding Palpation}" AcceptsReturn="True"/>
        </GroupBox>
    </Grid>
    
    <!-- Row 2: 诊断与治疗 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal">
        <GroupBox Header="中医诊断" Width="400">
            <TextBox Text="{Binding TCMDiagnosis}"/>
        </GroupBox>
        <GroupBox Header="治疗原则" Width="400">
            <TextBox Text="{Binding TreatmentPrinciple}"/>
        </GroupBox>
    </StackPanel>
    
    <!-- Row 3: 操作按钮 -->
    <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="保存诊疗记录" Command="{Binding SaveCommand}"/>
        <Button Content="保存并开处方" Command="{Binding SaveAndPrescribeCommand}"/>
        <Button Content="清空" Command="{Binding ClearCommand}"/>
    </StackPanel>
</Grid>
```

##### 3. MedicalCaseService

**文件位置**: `src/Client/Desktop/Infrastructure/Services/MedicalCaseService.cs`

**核心逻辑**:
```csharp
public interface IMedicalCaseService
{
    /// <summary>
    /// 为患者创建或获取当日医疗案例
    /// </summary>
    Task<Guid> GetOrCreateTodayMedicalCaseAsync(Guid patientId, Guid doctorId);
}

public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    
    public async Task<Guid> GetOrCreateTodayMedicalCaseAsync(Guid patientId, Guid doctorId)
    {
        // 1. 检查今天是否已有案例
        var today = DateTime.Today;
        var existingCase = await _repository.GetTodayMedicalCaseAsync(patientId, doctorId);
        
        if (existingCase != null)
        {
            return existingCase.Id;
        }
        
        // 2. 创建新案例
        var newCase = new CreateMedicalCaseDto
        {
            PatientId = patientId,
            UserId = doctorId,
            VisitDate = DateTime.Now,
            Status = MedicalCaseStatus.InProgress
        };
        
        var created = await _repository.CreateAsync(newCase);
        return created.Id;
    }
}
```

##### 4. ClinicalWorkstation导航重构

**修改文件**: `ClinicalWorkstationViewModel.cs`

**新增属性**:
```csharp
// 当前选中的患者
private PatientDto? _currentPatient;
public PatientDto? CurrentPatient
{
    get => _currentPatient;
    set
    {
        if (SetProperty(ref _currentPatient, value))
        {
            UpdatePatientInfo();
        }
    }
}

// 当前医疗案例ID
private Guid _currentMedicalCaseId;
public Guid CurrentMedicalCaseId
{
    get => _currentMedicalCaseId;
    set => SetProperty(ref _currentMedicalCaseId, value);
}
```

**修改SelectPatientCommand**:
```csharp
private async void ExecuteSelectPatient()
{
    try
    {
        Logger.LogInformation("打开患者选择对话框");
        
        // 打开对话框 (需要实现IDialogService)
        var result = await _dialogService.ShowDialogAsync<PatientSelectionDialog>();
        
        if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedPatient"))
        {
            CurrentPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
            
            // 自动创建或获取医疗案例
            CurrentMedicalCaseId = await _medicalCaseService.GetOrCreateTodayMedicalCaseAsync(
                CurrentPatient.Id,
                SessionManager.CurrentUser.Id);
            
            Logger.LogInformation("患者已选择: {PatientName}, MedicalCaseId: {MedicalCaseId}",
                CurrentPatient.Name, CurrentMedicalCaseId);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "选择患者失败");
        ShowErrorMessage($"选择患者失败：{ex.Message}");
    }
}
```

**修改导航映射** (ExecuteNavigate方法):
```csharp
private void ExecuteNavigate(string targetView)
{
    try
    {
        // 检查是否已选择患者 (除了历史查看外都需要)
        if (targetView != "History" && CurrentPatient == null)
        {
            ShowWarningMessage("请先选择患者");
            return;
        }
        
        Logger.LogInformation($"Navigating to clinical module: {targetView}");
        UpdateSelectionState(targetView);
        
        // 修改后的视图映射
        string viewName = targetView switch
        {
            "ConsultationEntry" => "ConsultationEntryView",  // ✅ 新增诊疗录入
            "Prescription" => "PrescriptionView",
            "History" => "ConsultationManagementView",       // ✅ 重命名:历史记录
            "PatientInfo" => "PatientDetailView",           // ✅ 新增:患者信息
            _ => "ConsultationEntryView"
        };
        
        // 传递导航参数
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", CurrentMedicalCaseId },
            { "PatientId", CurrentPatient?.Id ?? Guid.Empty }
        };
        
        _regionManager.RequestNavigate("ClinicalContentRegion", viewName, parameters);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, $"Failed to navigate to {targetView}");
        ShowErrorMessage($"导航失败：{ex.Message}");
    }
}
```

**修改XAML菜单** (`ClinicalWorkstationView.xaml`):
```xml
<!-- 修改前: -->
<RadioButton CommandParameter="Diagnosis">
    <TextBlock Text="诊断录入"/>
</RadioButton>

<!-- 修改后: -->
<RadioButton CommandParameter="ConsultationEntry">
    <TextBlock Text="看诊录入"/>
</RadioButton>

<RadioButton CommandParameter="Prescription">
    <TextBlock Text="处方管理"/>
</RadioButton>

<RadioButton CommandParameter="History">
    <TextBlock Text="诊疗历史"/>
</RadioButton>

<RadioButton CommandParameter="PatientInfo">
    <TextBlock Text="患者信息"/>
</RadioButton>
```

##### 5. PrescriptionView工作流集成

**修改**: `ConsultationEntryViewModel.cs`

**SaveAndPrescribeCommand实现**:
```csharp
private async void ExecuteSaveAndPrescribe()
{
    try
    {
        SetIsBusy(true, "正在保存诊疗记录...");
        
        // 1. 保存诊疗记录
        var consultation = new CreateConsultationDto
        {
            MedicalCaseId = _medicalCaseId,
            PatientId = _currentPatient.Id,
            UserId = SessionManager.CurrentUser.Id,
            Inspection = Inspection,
            AuscultationOlfaction = AuscultationOlfaction,
            Inquiry = Inquiry,
            Palpation = Palpation,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            TCMDiagnosis = TCMDiagnosis,
            TreatmentPrinciple = TreatmentPrinciple,
            StartTime = DateTime.Now,
            ConsultationStatus = ConsultationStatus.InProgress
        };
        
        var saved = await _consultationRepository.CreateAsync(consultation);
        
        // 2. 导航到处方页面
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", _medicalCaseId },
            { "PatientId", _currentPatient.Id }
        };
        
        NavigateTo("ClinicalContentRegion", "PrescriptionView", parameters);
        
        await ShowSuccessMessageAsync("诊疗记录已保存,正在打开处方页面...");
        Logger.LogInformation("诊疗记录保存成功,已跳转到处方页面");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存诊疗记录并开处方失败");
        await ShowErrorMessageAsync("保存失败,请稍后重试");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

### 3.2 Phase 2: 流程优化 (可选,后续)

- 患者信息快速面板 (顶部固定显示)
- 诊疗历史快速查看 (侧边栏)
- 看诊流程进度指示器
- 智能诊断建议 (基于历史数据)

---

## 📊 四、预期收益

### 4.1 用户体验提升

| 指标 | 改进前 | 改进后 | 提升幅度 |
|------|--------|--------|---------|
| 完整看诊流程可用性 | ❌ 0% | ✅ 100% | +100% |
| 诊疗录入界面可用性 | ❌ 空白页面 | ✅ 完整表单 | N/A |
| 患者选择便捷性 | ❌ TODO | ✅ 快速搜索 | N/A |
| 工作流连贯性 | ⚠️ 断裂 | ✅ 自动流转 | N/A |

### 4.2 代码架构改善

- ✅ 菜单项名称与实际功能一致 (解决#1454根本问题)
- ✅ 数据流清晰 (Patient → MedicalCase → Consultation → Prescription)
- ✅ 模块职责明确 (录入/查看分离)
- ✅ 符合MVP原则 (够用即好,避免过度设计)

---

## ⚠️ 五、风险与注意事项

### 5.1 技术风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| IDialogService未实现 | 患者选择弹窗无法打开 | 先实现基础DialogService |
| MedicalCase自动创建逻辑复杂 | 数据关联错误 | 详细单元测试 |
| 导航参数传递失败 | MedicalCaseId丢失 | 日志记录+错误处理 |

### 5.2 实施约束

- ✅ **必须遵守**: MVP原则,不引入新技术栈 (禁止Redis/CQRS/MediatR等)
- ✅ **必须遵守**: 三层架构规范 (参考 `docs/architecture/client/README.md`)
- ✅ **必须遵守**: Issue驱动开发 (每个组件创建对应Issue)

### 5.3 依赖检查

**前置条件**:
- ✅ Server端API已支持 Consultation CRUD
- ✅ Server端API已支持 MedicalCase CRUD
- ✅ Patients模块基础功能存在

**待确认**:
- ❓ IDialogService是否已实现? (需要检查 Infrastructure)
- ❓ PatientRepository是否支持拼音码搜索?

---

## 📝 六、后续行动

### 6.1 立即行动 (今日)

1. ✅ **完成本优化方案文档** (当前任务)
2. ⏳ **向用户确认方案** (等待用户反馈)
3. ⏳ **检查IDialogService实现** (代码探查)

### 6.2 用户确认后

1. 创建Epic: "临床工作台看诊流程完整实现"
2. 拆分5个子Issue (参见 3.1 Phase 1)
3. 依次实施 (预计8工作日)

### 6.3 验收标准

**完整看诊流程可演示**:
```
1. 医生登录 → 临床工作台
2. 点击[选择患者] → 搜索"张三" → 确定
3. 顶部显示 "当前患者: 张三 | 男 | 35岁"
4. 点击[看诊录入] → 填写四诊数据 → [保存并开处方]
5. 自动跳转PrescriptionView → 显示患者信息 → 录入药材 → 保存
6. 点击[诊疗历史] → 看到刚才的诊疗记录
```

---

## 📚 七、参考资料

- 📐 **三层架构规范**: `docs/architecture/client/README.md`
- 📋 **Issue工作流**: `CLAUDE.md` 第2节
- 🎯 **MVP原则**: `.spec-workflow/steering/constitution.md`
- 🔧 **Prescriptions模块参考**: Epic #1445 (已完成的模块重构案例)

---

**文档版本**: v1.0  
**下一步**: 等待用户确认后创建Epic并拆分Issue  
**预计完成时间**: 2周 (10个工作日,含测试)
