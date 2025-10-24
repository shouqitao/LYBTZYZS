# 医案/诊断/处方三模块增强功能 - 差距分析与修改计划

**文档版本**：v1.0
**创建时间**：2025-10-24
**最后更新**：2025-10-24
**维护负责**：项目团队

## 📋 文档说明

本文档对比现有代码与设计文档（`medicalcase-consultation-prescription-enhancement-design.md`）的差距，提供详细的修改计划，遵循"尽可能修改现有代码，避免创建无用文件"的原则。

**相关文档**：
- **[需求文档](../requirements/medicalcase-consultation-prescription-enhancement-requirements.md)** - REQ-001 到 REQ-006
- **[设计文档](medicalcase-consultation-prescription-enhancement-design.md)** - 详细技术设计
- **[讨论文档](../architecture/shared/medicalcase-consultation-prescription-enhancement-discussion.md)** - 业务需求讨论

---

## 🎯 分析范围

### 现有代码文件（4个核心文件）

| 文件 | 路径 | 代码量 | 状态 |
|------|------|--------|------|
| ConsultationFormView.xaml | `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/` | 255行 | ✅ 结构良好 |
| ConsultationFormViewModel.cs | `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/` | 405行 | ✅ 功能完善 |
| PrescriptionEditorView.xaml | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/` | 280行 | ⚠️ 需迁移到Prescriptions模块 |
| PrescriptionEditorViewModel.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/` | 685行 | ✅ 功能完善 |

### 需求清单（6个REQ）

| 需求编号 | 需求名称 | 影响文件 | 优先级 |
|---------|---------|---------|--------|
| REQ-001 | 三步工作流优化-Step1（辩证） | ConsultationForm | ⭐⭐⭐ |
| REQ-002 | 三步工作流优化-Step2（施治） | PrescriptionEditor | ⭐⭐⭐ |
| REQ-003 | 其他病案查询菜单 | 两个View | ⭐⭐ |
| REQ-004 | 处方删除确认对话框 | PrescriptionEditor | ⭐⭐ |
| REQ-005 | 重复药材检测 | PrescriptionEditor | ⭐ |
| REQ-006 | 暂存功能完善 | 两个ViewModel | ⭐⭐ |

---

## 📊 差距分析

### 1️⃣ ConsultationFormView.xaml（辩证界面）

**文件路径**：`src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml`
**当前代码量**：255行
**实施决策**：✅ **在现有代码上修改**（结构良好，无需重写）

#### ✅ 已实现功能

| 功能 | 实现情况 | 代码位置 |
|-----|---------|---------|
| 主诉（必填） | ✅ 已实现 | 第50-60行 |
| 现病史 | ✅ 已实现 | 第62-72行 |
| 中医诊断（必填） | ✅ 已实现 | 第76-86行 |
| 治疗原则 | ✅ 已实现 | 第88-98行 |
| 四诊合参（望闻问切） | ✅ 已实现 | 第104-168行 |
| 备注 | ✅ 已实现 | 第172-182行 |
| 清空表单按钮 | ✅ 已实现 | 第186-194行 |
| 2列布局 | ✅ 已实现 | 整体布局 |

#### ➕ 需要添加的功能（REQ-001）

| 功能 | 添加位置 | 代码量估算 | 实现方式 |
|-----|---------|-----------|---------|
| **1. 处方单选框**（开处方/不开处方） | 主诉下方插入新行 | +40行 | 新增Row 1.5，RadioButton两选一 |
| **2. 浮动查询菜单按钮**（右下角） | Grid最后一行 | +30行 | 新增FloatingActionButton样式 |
| **3. 步骤完成时间提示**（Step1CompletedAt） | 标题行下方 | +20行 | 只读TextBlock显示完成时间 |

**修改计划**：

```xml
<!-- 修改1: 在主诉下方添加处方单选框（新增Row 1.5） -->
<Grid Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" Margin="0,10,0,10">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <TextBlock Grid.Column="0" Text="是否开处方：" VerticalAlignment="Center" FontSize="14" Margin="0,0,10,0"/>
    <StackPanel Grid.Column="1" Orientation="Horizontal">
        <RadioButton Content="开处方"
                     IsChecked="{Binding PrescriptionEnabled}"
                     GroupName="PrescriptionGroup"
                     Margin="0,0,20,0"
                     FontSize="14"/>
        <RadioButton Content="不开处方"
                     IsChecked="{Binding PrescriptionDisabled}"
                     GroupName="PrescriptionGroup"
                     FontSize="14"/>
    </StackPanel>
</Grid>

<!-- 修改2: 在右下角添加浮动查询菜单按钮 -->
<Button Grid.Row="999"
        HorizontalAlignment="Right"
        VerticalAlignment="Bottom"
        Width="48" Height="48"
        Margin="0,0,16,16"
        Style="{StaticResource MaterialDesignFloatingActionButton}"
        Command="{Binding ShowOtherCasesQueryCommand}"
        ToolTip="查询其他病案">
    <materialDesign:PackIcon Kind="Magnify" Width="24" Height="24"/>
</Button>

<!-- 修改3: 在标题行下方添加完成时间提示 -->
<TextBlock Grid.Row="0" Grid.Column="1"
           Text="{Binding Step1CompletedAtText}"
           Foreground="Green"
           FontSize="12"
           HorizontalAlignment="Right"
           VerticalAlignment="Center"
           Visibility="{Binding Step1CompletedAtVisibility}"/>
```

**Grid.RowDefinitions调整**：
- 原有行索引：0（标题）、1（基本信息）、2（四诊）、3（备注）、4（按钮）、5（提示）
- 新增行：Row 1.5（处方单选框）、Row 999（浮动按钮，绝对定位）

**估算工作量**：+90行XAML，2小时开发

---

### 2️⃣ ConsultationFormViewModel.cs（辩证ViewModel）

**文件路径**：`src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs`
**当前代码量**：405行
**实施决策**：✅ **在现有代码上修改**（架构良好，继承UnifiedViewModelBase）

#### ✅ 已实现功能

| 功能 | 实现情况 | 代码位置 |
|-----|---------|---------|
| 数据绑定属性（主诉/现病史/诊断等） | ✅ 已实现 | 第50-120行 |
| IValidatable接口 | ✅ 已实现 | 第150-180行 |
| ISaveable接口 | ✅ 已实现 | 第200-250行 |
| IMedicalCaseRepository依赖 | ✅ 已实现 | 第30行 |
| 清空表单Command | ✅ 已实现 | 第280行 |

#### ➕ 需要添加的功能（REQ-001）

| 功能 | 添加位置 | 代码量估算 | 实现方式 |
|-----|---------|-----------|---------|
| **1. PrescriptionEnabled属性** | 数据属性区 | +15行 | bool属性 + PropertyChanged |
| **2. Step1CompletedAt属性** | 数据属性区 | +30行 | DateTime? + 格式化Text + Visibility |
| **3. 处方验证逻辑** | Validate方法内 | +20行 | 检查RadioButton + 处方为空冲突 |
| **4. CompleteStep1Command** | 命令区 | +50行 | 调用API完成Step1，验证处方状态 |
| **5. ShowOtherCasesQueryCommand** | 命令区 | +40行 | 导航到OtherCasesQueryView |

**修改计划**：

```csharp
#region REQ-001: 三步工作流优化-Step1属性

private bool _prescriptionEnabled = true; // 默认开处方
/// <summary>
/// 是否开处方（RadioButton选中状态）
/// </summary>
public bool PrescriptionEnabled
{
    get => _prescriptionEnabled;
    set
    {
        if (SetProperty(ref _prescriptionEnabled, value))
        {
            RaisePropertyChanged(nameof(PrescriptionDisabled));
        }
    }
}

/// <summary>
/// 不开处方（反向绑定）
/// </summary>
public bool PrescriptionDisabled
{
    get => !_prescriptionEnabled;
    set
    {
        if (value)
        {
            PrescriptionEnabled = false;
        }
    }
}

private DateTime? _step1CompletedAt;
/// <summary>
/// Step1完成时间（服务端返回）
/// </summary>
public DateTime? Step1CompletedAt
{
    get => _step1CompletedAt;
    set
    {
        if (SetProperty(ref _step1CompletedAt, value))
        {
            RaisePropertyChanged(nameof(Step1CompletedAtText));
            RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
        }
    }
}

public string Step1CompletedAtText =>
    Step1CompletedAt.HasValue
        ? $"✅ Step1已完成（{Step1CompletedAt.Value:yyyy-MM-dd HH:mm}）"
        : string.Empty;

public Visibility Step1CompletedAtVisibility =>
    Step1CompletedAt.HasValue ? Visibility.Visible : Visibility.Collapsed;

#endregion

#region REQ-001: 命令实现

public DelegateCommand CompleteStep1Command { get; }
public DelegateCommand ShowOtherCasesQueryCommand { get; }

private async void ExecuteCompleteStep1()
{
    try
    {
        SetIsBusy(true, "正在完成Step1...");

        // 1. 验证表单
        if (!Validate())
        {
            await ShowErrorMessageAsync(ValidationMessage);
            return;
        }

        // 2. 调用API完成Step1
        var request = new CompleteStep1Request
        {
            PrescriptionEnabled = PrescriptionEnabled
        };

        var stepDto = await _consultationApiClient.CompleteStep1Async(MedicalCaseId, request);

        // 3. 更新本地状态
        Step1CompletedAt = stepDto.Step1CompletedAt;

        // 4. 导航到下一步（Step2或Step3）
        if (PrescriptionEnabled)
        {
            // 跳转到Step2（处方录入）
            NavigateToStep2();
        }
        else
        {
            // 跳转到Step3（汇总）
            NavigateToStep3();
        }

        await ShowSuccessMessageAsync("Step1已完成");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "完成Step1失败");
        await ShowErrorMessageAsync($"完成Step1失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

private void ExecuteShowOtherCasesQuery()
{
    // REQ-003: 显示其他病案查询浮动菜单
    var parameters = new NavigationParameters
    {
        { "PatientId", CurrentPatient?.Id }
    };
    RegionManager.RequestNavigate("ContentRegion", "OtherCasesQueryView", parameters);
}

#endregion

#region REQ-001: 增强验证逻辑

public bool Validate()
{
    var errors = new List<string>();

    // 原有验证逻辑（主诉、中医诊断等）
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
        errors.Add("主诉不能为空");
    if (string.IsNullOrWhiteSpace(TCMDiagnosis))
        errors.Add("中医诊断不能为空");

    // REQ-001: 新增处方验证
    if (PrescriptionEnabled)
    {
        // 如果选择"开处方"，必须确保处方不为空
        // 注意：此验证在CompleteStep1时由Server端执行
        // ViewModel仅负责前端警告
        if (!HasPrescriptionData())
        {
            errors.Add("已选择开处方，但处方数据为空。请录入处方或选择"不开处方"。");
        }
    }

    // ... 其余验证逻辑
}

/// <summary>
/// 检查是否有处方数据（通过Repository查询）
/// </summary>
private bool HasPrescriptionData()
{
    // 实现方式1：查询MedicalCase.PrescriptionId
    // 实现方式2：查询Prescriptions表是否存在MedicalCaseId关联记录
    // 需要依赖IPrescriptionRepository或IMedicalCaseRepository
    return false; // 占位实现
}

#endregion
```

**构造函数修改**：
```csharp
public ConsultationFormViewModel(
    IMedicalCaseRepository medicalCaseRepository,
    // 新增依赖
    IConsultationApiClient consultationApiClient, // REQ-001
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
{
    _medicalCaseRepository = medicalCaseRepository;
    _consultationApiClient = consultationApiClient; // REQ-001

    // 初始化命令
    CompleteStep1Command = new DelegateCommand(async () => await ExecuteCompleteStep1());
    ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery);
}
```

**估算工作量**：+155行C#，3小时开发

---

### 3️⃣ PrescriptionEditorView.xaml（施治界面）

**文件路径**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PrescriptionEditorView.xaml`
**⚠️ 模块位置问题**：当前在MedicalCase模块，**建议迁移到Prescriptions模块**
**当前代码量**：280行
**实施决策**：✅ **在现有代码上修改 + 迁移到新模块**

#### ✅ 已实现功能

| 功能 | 实现情况 | 代码位置 |
|-----|---------|---------|
| 8列DataGrid（4个药材x2列） | ✅ 已实现 | 第80-180行 |
| 药材ComboBox（过滤/拼音码） | ✅ 已实现 | 第100-120行 |
| 剂数、用法、价格计算 | ✅ 已实现 | 第190-220行 |
| Tab控制（手工/验方/历史） | ✅ 已实现 | 第30-50行 |
| 添加行按钮 | ✅ 已实现 | 第230行 |

#### ➕ 需要添加的功能（REQ-002, REQ-003, REQ-004, REQ-005）

| 功能 | 添加位置 | 代码量估算 | 实现方式 |
|-----|---------|-----------|---------|
| **1. 治法方案字段**（REQ-002） | DataGrid上方插入 | +40行 | 2列布局（治法方案/中医诊断） |
| **2. 浮动查询菜单按钮**（REQ-003） | 右下角 | +30行 | FloatingActionButton |
| **3. 删除确认对话框**（REQ-004） | 弹出窗口 | +80行 | Dialog + 软删除/物理删除RadioButton |
| **4. 重复药材提示**（REQ-005） | DataGrid下方 | +30行 | TextBlock显示重复药材警告 |
| **5. 步骤完成时间**（REQ-002） | 标题行下方 | +20行 | 只读TextBlock |

**修改计划**：

```xml
<!-- 修改1: 在DataGrid上方添加治法方案字段（新增Row 1.5） -->
<Grid Grid.Row="1" Margin="0,10,0,10">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="1*"/>
        <ColumnDefinition Width="10"/>
        <ColumnDefinition Width="1*"/>
    </Grid.ColumnDefinitions>

    <!-- 治法方案 -->
    <Border Grid.Column="0" Style="{StaticResource CardBorder}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <TextBlock Grid.Row="0" Text="治法方案 *" Style="{StaticResource FieldLabel}"/>
            <TextBox Grid.Row="1"
                     Text="{Binding TreatmentMethod}"
                     TextWrapping="Wrap"
                     AcceptsReturn="True"
                     Height="80"
                     VerticalScrollBarVisibility="Auto"/>
        </Grid>
    </Border>

    <!-- 治疗原则 -->
    <Border Grid.Column="2" Style="{StaticResource CardBorder}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <TextBlock Grid.Row="0" Text="治疗原则" Style="{StaticResource FieldLabel}"/>
            <TextBox Grid.Row="1"
                     Text="{Binding TreatmentPrinciple}"
                     TextWrapping="Wrap"
                     AcceptsReturn="True"
                     Height="80"
                     VerticalScrollBarVisibility="Auto"/>
        </Grid>
    </Border>
</Grid>

<!-- 修改2: 在DataGrid下方添加重复药材提示（新增Row 2.5） -->
<Border Grid.Row="2"
        Background="#FFF3E0"
        BorderBrush="Orange"
        BorderThickness="1"
        CornerRadius="4"
        Padding="10"
        Margin="0,5,0,5"
        Visibility="{Binding DuplicateHerbsWarningVisibility}">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="AlertCircle"
                                 Width="20" Height="20"
                                 Foreground="Orange"
                                 VerticalAlignment="Center"
                                 Margin="0,0,8,0"/>
        <TextBlock Text="{Binding DuplicateHerbsWarningText}"
                   Foreground="DarkOrange"
                   TextWrapping="Wrap"
                   FontSize="14"/>
    </StackPanel>
</Border>

<!-- 修改3: 浮动查询菜单按钮（右下角） -->
<Button Grid.Row="999"
        HorizontalAlignment="Right"
        VerticalAlignment="Bottom"
        Width="48" Height="48"
        Margin="0,0,16,16"
        Style="{StaticResource MaterialDesignFloatingActionButton}"
        Command="{Binding ShowOtherCasesQueryCommand}"
        ToolTip="查询其他病案">
    <materialDesign:PackIcon Kind="Magnify" Width="24" Height="24"/>
</Button>

<!-- 修改4: 步骤完成时间提示（标题行下方） -->
<TextBlock Grid.Row="0" Grid.Column="1"
           Text="{Binding Step2CompletedAtText}"
           Foreground="Green"
           FontSize="12"
           HorizontalAlignment="Right"
           VerticalAlignment="Center"
           Visibility="{Binding Step2CompletedAtVisibility}"/>
```

**删除确认对话框（独立UserControl）**：

```xml
<!-- 新建文件：src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionDeleteConfirmDialog.xaml -->
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionDeleteConfirmDialog"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Border Width="400" Background="White" CornerRadius="8" Padding="20">
        <StackPanel>
            <!-- 标题 -->
            <TextBlock Text="确认删除处方？" FontSize="18" FontWeight="Bold" Margin="0,0,0,20"/>

            <!-- 警告信息 -->
            <Border Background="#FFEBEE" BorderBrush="Red" BorderThickness="1" CornerRadius="4" Padding="10" Margin="0,0,0,20">
                <StackPanel Orientation="Horizontal">
                    <materialDesign:PackIcon Kind="AlertCircle" Width="24" Height="24" Foreground="Red" Margin="0,0,10,0"/>
                    <TextBlock Text="此操作不可恢复，请谨慎选择删除方式。" Foreground="Red" TextWrapping="Wrap"/>
                </StackPanel>
            </Border>

            <!-- 删除方式选择 -->
            <TextBlock Text="删除方式：" FontWeight="Bold" Margin="0,0,0,10"/>
            <StackPanel Margin="20,0,0,0">
                <RadioButton Content="软删除（标记为已删除，保留数据）"
                             IsChecked="{Binding IsSoftDelete}"
                             GroupName="DeleteType"
                             Margin="0,0,0,10"/>
                <RadioButton Content="物理删除（永久删除，不可恢复）"
                             IsChecked="{Binding IsPhysicalDelete}"
                             GroupName="DeleteType"
                             Foreground="Red"/>
            </StackPanel>

            <!-- 操作按钮 -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
                <Button Content="取消"
                        Command="{Binding CancelCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Margin="0,0,10,0"/>
                <Button Content="确认删除"
                        Command="{Binding ConfirmDeleteCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        Background="Red"/>
            </StackPanel>
        </StackPanel>
    </Border>
</UserControl>
```

**估算工作量**：+200行XAML（View本体+Dialog），4小时开发

---

### 4️⃣ PrescriptionEditorViewModel.cs（施治ViewModel）

**文件路径**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`
**当前代码量**：685行
**实施决策**：✅ **在现有代码上修改**（功能非常完善，Epic #1540已实现草稿模式）

#### ✅ 已实现功能

| 功能 | 实现情况 | 代码位置 |
|-----|---------|---------|
| 数据属性（剂数/用法/医嘱/备注） | ✅ 已实现 | 第60-140行 |
| IValidatable接口（药材库验证） | ✅ 已实现 | 第220-300行（Issue #1546） |
| ISaveable接口（草稿模式） | ✅ 已实现 | 第320-450行（Epic #1540） |
| IPrescriptionEditorService依赖 | ✅ 已实现 | 第30行 |
| AddRowCommand | ✅ 已实现 | 第600行 |
| 价格计算（SingleDosagePrice/TotalPrice） | ✅ 已实现 | 第120-135行 |
| 事件发布（PrescriptionCompletedEvent） | ✅ 已实现 | 第500行（Issue #1557） |

#### ➕ 需要添加的功能（REQ-002, REQ-003, REQ-004, REQ-005, REQ-006）

| 功能 | 添加位置 | 代码量估算 | 实现方式 |
|-----|---------|-----------|---------|
| **1. 治法方案属性**（REQ-002） | 数据属性区 | +30行 | TreatmentMethod + TreatmentPrinciple |
| **2. Step2CompletedAt属性**（REQ-002） | 数据属性区 | +30行 | DateTime? + 格式化 |
| **3. CompleteStep2Command**（REQ-002） | 命令区 | +60行 | 调用API完成Step2 |
| **4. ShowOtherCasesQueryCommand**（REQ-003） | 命令区 | +40行 | 导航到OtherCasesQueryView |
| **5. DeletePrescriptionCommand**（REQ-004） | 命令区 | +100行 | 显示删除确认Dialog + 软删除/物理删除 |
| **6. 重复药材检测逻辑**（REQ-005） | 验证方法内 | +50行 | 检测GroupBy HerbName |
| **7. SaveDraftCommand**（REQ-006） | 命令区 | +40行 | 保存为草稿（不完成Step2） |

**修改计划**：

```csharp
#region REQ-002: 治法方案属性

private string _treatmentMethod = string.Empty;
/// <summary>
/// 治法方案（必填）
/// </summary>
public string TreatmentMethod
{
    get => _treatmentMethod;
    set => SetProperty(ref _treatmentMethod, value);
}

private string _treatmentPrinciple = string.Empty;
/// <summary>
/// 治疗原则
/// </summary>
public string TreatmentPrinciple
{
    get => _treatmentPrinciple;
    set => SetProperty(ref _treatmentPrinciple, value);
}

private DateTime? _step2CompletedAt;
/// <summary>
/// Step2完成时间
/// </summary>
public DateTime? Step2CompletedAt
{
    get => _step2CompletedAt;
    set
    {
        if (SetProperty(ref _step2CompletedAt, value))
        {
            RaisePropertyChanged(nameof(Step2CompletedAtText));
            RaisePropertyChanged(nameof(Step2CompletedAtVisibility));
        }
    }
}

public string Step2CompletedAtText =>
    Step2CompletedAt.HasValue
        ? $"✅ Step2已完成（{Step2CompletedAt.Value:yyyy-MM-dd HH:mm}）"
        : string.Empty;

public Visibility Step2CompletedAtVisibility =>
    Step2CompletedAt.HasValue ? Visibility.Visible : Visibility.Collapsed;

#endregion

#region REQ-005: 重复药材检测

private string _duplicateHerbsWarningText = string.Empty;
/// <summary>
/// 重复药材警告文本
/// </summary>
public string DuplicateHerbsWarningText
{
    get => _duplicateHerbsWarningText;
    private set
    {
        if (SetProperty(ref _duplicateHerbsWarningText, value))
        {
            RaisePropertyChanged(nameof(DuplicateHerbsWarningVisibility));
        }
    }
}

public Visibility DuplicateHerbsWarningVisibility =>
    string.IsNullOrEmpty(DuplicateHerbsWarningText) ? Visibility.Collapsed : Visibility.Visible;

/// <summary>
/// 检测重复药材（REQ-005）
/// </summary>
private void DetectDuplicateHerbs()
{
    var allItems = GetAllItems();
    var duplicates = allItems
        .GroupBy(item => item.HerbName, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToList();

    if (duplicates.Any())
    {
        DuplicateHerbsWarningText = $"⚠️ 检测到重复药材：{string.Join("、", duplicates)}";
        Logger.LogWarning("检测到重复药材：{Duplicates}", string.Join(", ", duplicates));
    }
    else
    {
        DuplicateHerbsWarningText = string.Empty;
    }
}

#endregion

#region REQ-002, REQ-003, REQ-004, REQ-006: 命令实现

public DelegateCommand CompleteStep2Command { get; }
public DelegateCommand ShowOtherCasesQueryCommand { get; }
public DelegateCommand DeletePrescriptionCommand { get; }
public DelegateCommand SaveDraftCommand { get; }

private async void ExecuteCompleteStep2()
{
    try
    {
        SetIsBusy(true, "正在完成Step2...");

        // 1. 验证表单（包含治法方案）
        if (!ValidateStep2())
        {
            await ShowErrorMessageAsync(ValidationMessage);
            return;
        }

        // 2. 保存处方数据
        var saved = await SaveAsync();
        if (!saved)
        {
            return; // SaveAsync已显示错误信息
        }

        // 3. 调用API完成Step2
        var request = new CompleteStep2Request
        {
            TreatmentMethod = TreatmentMethod.Trim()
        };

        var stepDto = await _consultationApiClient.CompleteStep2Async(MedicalCaseId, request);

        // 4. 更新本地状态
        Step2CompletedAt = stepDto.Step2CompletedAt;

        // 5. 导航到Step3（汇总）
        NavigateToStep3();

        await ShowSuccessMessageAsync("Step2已完成");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "完成Step2失败");
        await ShowErrorMessageAsync($"完成Step2失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

private bool ValidateStep2()
{
    var errors = new List<string>();

    // 原有验证逻辑
    var baseValid = Validate();

    // REQ-002: 验证治法方案
    if (string.IsNullOrWhiteSpace(TreatmentMethod))
    {
        errors.Add("治法方案不能为空");
    }

    if (errors.Any())
    {
        ValidationMessage = string.Join("；", errors);
        return false;
    }

    return baseValid;
}

private void ExecuteShowOtherCasesQuery()
{
    // REQ-003: 显示其他病案查询浮动菜单
    var parameters = new NavigationParameters
    {
        { "PatientId", CurrentPatient?.Id }
    };
    RegionManager.RequestNavigate("ContentRegion", "OtherCasesQueryView", parameters);
}

private async void ExecuteDeletePrescription()
{
    try
    {
        // REQ-004: 显示删除确认对话框
        var dialogViewModel = new PrescriptionDeleteConfirmDialogViewModel();
        var dialogResult = await ShowDialogAsync(dialogViewModel);

        if (dialogResult == DialogResult.OK)
        {
            SetIsBusy(true, "正在删除处方...");

            if (dialogViewModel.IsSoftDelete)
            {
                // 软删除
                await _prescriptionApiClient.SoftDeleteAsync(MedicalCaseId);
                await ShowSuccessMessageAsync("处方已标记为已删除");
            }
            else
            {
                // 物理删除
                await _prescriptionApiClient.DeleteAsync(MedicalCaseId);
                await ShowSuccessMessageAsync("处方已永久删除");
            }

            // 清空表单
            ClearForm();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "删除处方失败");
        await ShowErrorMessageAsync($"删除处方失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

private async void ExecuteSaveDraft()
{
    try
    {
        SetIsBusy(true, "正在保存草稿...");

        // REQ-006: 保存为草稿（不验证完整性）
        var saved = await SaveAsync();
        if (saved)
        {
            await ShowSuccessMessageAsync("草稿已保存");
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存草稿失败");
        await ShowErrorMessageAsync($"保存草稿失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

#endregion

#region 增强验证逻辑（REQ-002, REQ-005）

public bool Validate()
{
    var errors = new List<string>();

    // 原有验证逻辑（患者信息、药材库关联等）
    // ... 保持不变 ...

    // REQ-005: 重复药材检测
    DetectDuplicateHerbs(); // 更新警告文本（不阻止保存）

    // 其余验证逻辑
    if (errors.Any())
    {
        ValidationMessage = string.Join("；", errors);
        Logger.LogWarning("处方验证失败：{ValidationMessage}", ValidationMessage);
        return false;
    }

    ValidationMessage = string.Empty;
    Logger.LogInformation("处方验证通过，共{ItemCount}味药材", GetAllItems().Count);
    return true;
}

#endregion
```

**构造函数修改**：
```csharp
public PrescriptionEditorViewModel(
    IMedicalCaseRepository medicalCaseRepository,
    IPrescriptionEditorService prescriptionEditorService,
    // 新增依赖
    IConsultationApiClient consultationApiClient, // REQ-002
    IPrescriptionApiClient prescriptionApiClient, // REQ-004
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
{
    _medicalCaseRepository = medicalCaseRepository;
    _prescriptionEditorService = prescriptionEditorService;
    _consultationApiClient = consultationApiClient; // REQ-002
    _prescriptionApiClient = prescriptionApiClient; // REQ-004

    // 初始化命令
    AddRowCommand = new DelegateCommand(ExecuteAddRow);
    CompleteStep2Command = new DelegateCommand(async () => await ExecuteCompleteStep2()); // REQ-002
    ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery); // REQ-003
    DeletePrescriptionCommand = new DelegateCommand(async () => await ExecuteDeletePrescription()); // REQ-004
    SaveDraftCommand = new DelegateCommand(async () => await ExecuteSaveDraft()); // REQ-006
}
```

**估算工作量**：+350行C#，5小时开发

---

## 📋 实施计划

### Phase 1: 辩证界面增强（REQ-001）

**预计工时**：5小时
**优先级**：⭐⭐⭐ 高优先级

| 任务 | 文件 | 工作量 | 验收标准 |
|-----|------|--------|---------|
| 1.1 添加处方单选框 | ConsultationFormView.xaml | +40行 | RadioButton正常切换 |
| 1.2 添加浮动查询菜单按钮 | ConsultationFormView.xaml | +30行 | 按钮显示在右下角 |
| 1.3 添加步骤完成时间提示 | ConsultationFormView.xaml | +20行 | 完成后显示绿色时间戳 |
| 1.4 添加PrescriptionEnabled属性 | ConsultationFormViewModel.cs | +45行 | 双向绑定正常 |
| 1.5 添加Step1CompletedAt属性 | ConsultationFormViewModel.cs | +30行 | 时间格式化正确 |
| 1.6 添加CompleteStep1Command | ConsultationFormViewModel.cs | +50行 | API调用成功 + 导航正确 |
| 1.7 添加ShowOtherCasesQueryCommand | ConsultationFormViewModel.cs | +30行 | 导航到查询页 |
| 1.8 增强验证逻辑 | ConsultationFormViewModel.cs | +20行 | 处方验证生效 |

**编译标准**：0 errors, 0 warnings
**运行时验证**：RadioButton切换正常 + CompleteStep1调用API成功

---

### Phase 2: 施治界面增强（REQ-002, REQ-005）

**预计工时**：6小时
**优先级**：⭐⭐⭐ 高优先级

| 任务 | 文件 | 工作量 | 验收标准 |
|-----|------|--------|---------|
| 2.1 添加治法方案字段 | PrescriptionEditorView.xaml | +40行 | 2列布局显示正常 |
| 2.2 添加重复药材提示 | PrescriptionEditorView.xaml | +30行 | 警告框显示/隐藏逻辑正确 |
| 2.3 添加步骤完成时间提示 | PrescriptionEditorView.xaml | +20行 | 时间戳显示正确 |
| 2.4 添加TreatmentMethod属性 | PrescriptionEditorViewModel.cs | +30行 | 数据绑定正常 |
| 2.5 添加Step2CompletedAt属性 | PrescriptionEditorViewModel.cs | +30行 | 时间格式化正确 |
| 2.6 添加CompleteStep2Command | PrescriptionEditorViewModel.cs | +60行 | API调用成功 + 导航正确 |
| 2.7 实现重复药材检测 | PrescriptionEditorViewModel.cs | +50行 | 检测逻辑正确 + 警告提示 |
| 2.8 增强ValidateStep2方法 | PrescriptionEditorViewModel.cs | +20行 | 治法方案必填验证生效 |

**编译标准**：0 errors, 0 warnings
**运行时验证**：治法方案保存成功 + 重复药材检测生效

---

### Phase 3: 其他病案查询菜单（REQ-003）

**预计工时**：4小时
**优先级**：⭐⭐ 中优先级

| 任务 | 文件 | 工作量 | 验收标准 |
|-----|------|--------|---------|
| 3.1 创建OtherCasesQueryView | 新建XAML | +150行 | 查询界面显示正常 |
| 3.2 创建OtherCasesQueryViewModel | 新建C# | +200行 | 查询逻辑正确 |
| 3.3 实现查询API调用 | OtherCasesQueryViewModel | +80行 | API返回数据正确 |
| 3.4 集成到浮动菜单 | 两个View | +0行 | 点击按钮导航成功 |

**编译标准**：0 errors, 0 warnings
**运行时验证**：浮动菜单点击 → 查询页显示 → 数据加载成功

---

### Phase 4: 处方删除确认对话框（REQ-004）

**预计工时**：5小时
**优先级**：⭐⭐ 中优先级

| 任务 | 文件 | 工作量 | 验收标准 |
|-----|------|--------|---------|
| 4.1 创建删除确认Dialog | 新建XAML | +80行 | 对话框样式正确 |
| 4.2 创建DialogViewModel | 新建C# | +60行 | RadioButton绑定正常 |
| 4.3 实现DeletePrescriptionCommand | PrescriptionEditorViewModel.cs | +100行 | 软删除/物理删除API调用成功 |
| 4.4 集成到PrescriptionEditorView | PrescriptionEditorView.xaml | +30行 | 删除按钮显示 |

**编译标准**：0 errors, 0 warnings
**运行时验证**：点击删除按钮 → 对话框显示 → 确认删除 → API调用成功

---

### Phase 5: 暂存功能完善（REQ-006）

**预计工时**：3小时
**优先级**：⭐⭐ 中优先级

| 任务 | 文件 | 工作量 | 验收标准 |
|-----|------|--------|---------|
| 5.1 添加SaveDraftCommand | PrescriptionEditorViewModel.cs | +40行 | 草稿保存成功 |
| 5.2 添加LoadDraftCommand | PrescriptionEditorViewModel.cs | +60行 | 草稿加载成功 |
| 5.3 UI按钮调整 | 两个View | +30行 | "保存草稿"按钮显示 |

**编译标准**：0 errors, 0 warnings
**运行时验证**：保存草稿 → 刷新页面 → 草稿数据正确加载

---

## 📊 总工作量估算

| Phase | 功能描述 | 预计工时 | 代码量估算 | 优先级 |
|-------|---------|---------|-----------|--------|
| Phase 1 | REQ-001（辩证界面） | 5小时 | +245行 | ⭐⭐⭐ |
| Phase 2 | REQ-002 + REQ-005（施治界面） | 6小时 | +280行 | ⭐⭐⭐ |
| Phase 3 | REQ-003（其他病案查询） | 4小时 | +430行 | ⭐⭐ |
| Phase 4 | REQ-004（删除确认） | 5小时 | +270行 | ⭐⭐ |
| Phase 5 | REQ-006（暂存功能） | 3小时 | +130行 | ⭐⭐ |
| **总计** | **全部功能** | **23小时** | **+1355行** | - |

**质量检查清单**（每个Phase完成后）：
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 运行时验证通过（启动应用，测试功能）
- ✅ 代码符合MVVM架构规范
- ✅ 依赖注入正确（构造函数注入）
- ✅ 异步方法使用async/await
- ✅ 命名规范（PascalCase/camelCase/_privateField）
- ✅ 中文注释清晰

---

## 🚀 实施建议

### 1. 模块位置决策

**问题**：PrescriptionEditorView/ViewModel当前在MedicalCase模块，是否迁移到Prescriptions模块？

**建议**：✅ **保持在MedicalCase模块（暂不迁移）**

**理由**：
1. 当前代码已在MedicalCase模块，功能稳定（685行ViewModel）
2. MedicalCase作为聚合根，管理Consultation和Prescription是合理的
3. 迁移会导致大量引用路径调整，增加风险
4. Epic #1540已在此架构下实现，无需重构

**长期规划**：MVP完成后，可考虑重构为Workstations模式（详见设计文档Proposal A）

---

### 2. 增量实施策略

**推荐顺序**：Phase 1 → Phase 2 → Phase 5 → Phase 3 → Phase 4

**理由**：
- Phase 1/2是核心三步工作流，优先级最高
- Phase 5（暂存功能）依赖Phase 1/2的SaveAsync方法
- Phase 3/4是辅助功能，可后续补充

---

### 3. 风险控制

| 风险 | 缓解措施 |
|-----|---------|
| 破坏现有功能 | 每个Phase完成后立即运行时验证，确认原有功能正常 |
| API接口未实现 | 先实现UI + ViewModel，API暂用Mock数据 |
| 依赖注入失败 | 新增依赖时，同步更新Module的RegisterTypes方法 |
| 导航失败 | 在Shell中注册新View（OtherCasesQueryView） |

---

### 4. 文档同步要求

**每个Phase完成后更新**：
- ✅ `docs/architecture/client/README.md` - 更新View/ViewModel列表
- ✅ `docs/quick-reference/code-patterns.md` - 补充新命令模式示例
- ✅ `docs/index.md` - 更新完成度统计

---

## ✅ 验收标准

### 编译验证（强制）
```bash
dotnet build LYBT.All.sln -c Release --no-restore
# 要求：0 errors, 0 warnings
```

### 运行时验证（强制）

**Step1验证**：
1. 启动Desktop客户端
2. 进入诊断录入界面（ConsultationFormView）
3. 选择"开处方" → 填写诊断信息 → 点击"完成Step1"
4. ✅ 验证：跳转到处方录入界面（PrescriptionEditorView）
5. 选择"不开处方" → 填写诊断信息 → 点击"完成Step1"
6. ✅ 验证：跳转到汇总界面（Step3）

**Step2验证**：
1. 在处方录入界面填写治法方案 + 添加药材
2. 点击"完成Step2"
3. ✅ 验证：数据保存成功 + 跳转到Step3

**浮动菜单验证**：
1. 点击右下角浮动按钮
2. ✅ 验证：显示其他病案查询页

**删除确认验证**：
1. 点击"删除处方"按钮
2. ✅ 验证：显示删除确认对话框
3. 选择"软删除" → 确认
4. ✅ 验证：处方标记为已删除（数据库IsDeleted=true）

---

## 📚 附录

### A. 需要新增的API客户端接口

```csharp
// IConsultationApiClient（新增）
public interface IConsultationApiClient
{
    Task<ConsultationStepDto> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);
    Task<ConsultationStepDto> CompleteStep2Async(Guid medicalCaseId, CompleteStep2Request request);
}

// IPrescriptionApiClient（新增）
public interface IPrescriptionApiClient
{
    Task SoftDeleteAsync(Guid medicalCaseId);
    Task DeleteAsync(Guid medicalCaseId);
}
```

### B. 需要新增的DTO模型

```csharp
// CompleteStep1Request
public class CompleteStep1Request
{
    public bool PrescriptionEnabled { get; set; }
}

// CompleteStep2Request
public class CompleteStep2Request
{
    public string TreatmentMethod { get; set; } = string.Empty;
}

// ConsultationStepDto
public class ConsultationStepDto
{
    public Guid Id { get; set; }
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public DateTime? Step3CompletedAt { get; set; }
}
```

### C. 需要新增的Dialog ViewModel

```csharp
// PrescriptionDeleteConfirmDialogViewModel
public class PrescriptionDeleteConfirmDialogViewModel : BindableBase
{
    private bool _isSoftDelete = true;
    public bool IsSoftDelete
    {
        get => _isSoftDelete;
        set
        {
            SetProperty(ref _isSoftDelete, value);
            RaisePropertyChanged(nameof(IsPhysicalDelete));
        }
    }

    public bool IsPhysicalDelete
    {
        get => !_isSoftDelete;
        set
        {
            if (value)
                IsSoftDelete = false;
        }
    }

    public DelegateCommand ConfirmDeleteCommand { get; }
    public DelegateCommand CancelCommand { get; }
}
```

---

**文档维护**：本文档随设计文档同步更新，如有调整请及时更新差距分析。
**最后更新**：2025-10-24 - v1.0初始版本 ✨
