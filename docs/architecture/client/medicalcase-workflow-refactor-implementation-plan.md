# 看诊流程架构重构实施计划

## 📋 元信息

- **Issue**: #1567
- **讨论文档**: `docs/architecture/client/medicalcase-fourstep-workflow-discussion.md`
- **预计工作量**: 6-7天
- **风险等级**: 中
- **优先级**: 高

---

## 🎯 重构目标

将"患者选择"从"看病流程"中分离，形成清晰的领域边界：

**现状**（4步混淆）：
```
患者选择 → 辨证 → 施治 → 完成
（Step 1）  （Step 2-4）
```

**目标**（独立分离）：
```
主页 → 患者选择界面（中枢）
          ↓
       看病流程（3步）
       辨证 → 施治 → 完成
          ↓
       返回患者选择界面
```

---

## 📊 Phase 1: 基础重构（2天）

### **目标**
- 重构FlowStep枚举为ConsultationStep（3步）
- 调整MedicalCaseFlowView为3步结构
- 删除SelectPatient相关逻辑

### **任务清单**

#### **Task 1.1: 创建ConsultationStep枚举（0.5天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/ConsultationStep.cs`

```csharp
namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 看病流程步骤枚举（重构自FlowStep，删除患者选择）
    /// Issue #1567 - 分离患者选择与看病流程
    /// </summary>
    public enum ConsultationStep
    {
        /// <summary>
        /// Step 1: 辨证 - 录入四诊信息、主诉、现病史、诊断结论
        /// </summary>
        Consultation = 1,

        /// <summary>
        /// Step 2: 施治 - 根据诊断结果开具中药处方
        /// </summary>
        Prescription = 2,

        /// <summary>
        /// Step 3: 完成 - 确认诊疗信息并归档
        /// </summary>
        Completion = 3
    }
}
```

**验收**：
- ✅ 文件创建成功
- ✅ 枚举包含3个值（Consultation, Prescription, Completion）
- ✅ 注释清晰，标注Issue #1567

---

#### **Task 1.2: 重构MedicalCaseFlowViewModel（1天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

**修改内容**：

1. **替换枚举类型**：
   ```csharp
   // 旧代码
   private FlowStep _currentStep = FlowStep.SelectPatient;

   // 新代码
   private ConsultationStep _currentStep = ConsultationStep.Consultation;
   ```

2. **删除SelectPatient相关逻辑**：
   ```csharp
   // ❌ 删除：患者选择相关属性
   // private string _selectedPatientName;
   // private string _selectedPatientInfo;
   // private PatientDto? _currentPatient;

   // ❌ 删除：OnPatientSelected事件处理方法

   // ❌ 删除：NavigateToStep中的SelectPatient分支
   ```

3. **调整步骤文本逻辑**：
   ```csharp
   private void UpdateCurrentStepText()
   {
       CurrentStepText = CurrentStep switch
       {
           ConsultationStep.Consultation => "辨证",
           ConsultationStep.Prescription => "施治",
           ConsultationStep.Completion => "完成",
           _ => string.Empty
       };
   }
   ```

4. **调整按钮文本逻辑**：
   ```csharp
   // NextButtonText
   public string NextButtonText => CurrentStep == ConsultationStep.Completion
       ? "完成病案"
       : "下一步";

   // PreviousButtonText（所有步骤都可以"上一步"）
   public string PreviousButtonText => "上一步";

   // 删除PreviousButtonBackground和PreviousButtonForeground（UI逻辑分离）
   ```

5. **调整CanExecutePreviousStep**：
   ```csharp
   private bool CanExecutePreviousStep()
   {
       // 所有步骤都可以返回上一步（3步内部自由往返）
       return CurrentStep > ConsultationStep.Consultation;
   }
   ```

6. **调整NavigateToStep方法**：
   ```csharp
   private void NavigateToStep(ConsultationStep step)
   {
       CurrentStep = step;

       switch (step)
       {
           case ConsultationStep.Consultation:
               _regionManager.RequestNavigate("WorkflowContentRegion", "ConsultationFormView",
                   new NavigationParameters
                   {
                       { "MedicalCaseId", MedicalCaseId },
                       { "CurrentPatient", CurrentPatient }
                   });
               break;

           case ConsultationStep.Prescription:
               _regionManager.RequestNavigate("WorkflowContentRegion", "PrescriptionEditorView",
                   new NavigationParameters
                   {
                       { "MedicalCaseId", MedicalCaseId },
                       { "CurrentPatient", CurrentPatient }
                   });
               break;

           case ConsultationStep.Completion:
               _regionManager.RequestNavigate("WorkflowContentRegion", "CompletionView",
                   new NavigationParameters
                   {
                       { "MedicalCaseId", MedicalCaseId }
                   });
               break;

           default:
               Logger.LogWarning("未知步骤：{Step}", step);
               break;
       }
   }
   ```

7. **新增属性：接收外部传入的患者信息和MedicalCaseId**：
   ```csharp
   // 这些属性将在OnNavigatedTo中从PatientSelectionViewModel传入
   public PatientDto? CurrentPatient { get; set; }
   public Guid MedicalCaseId { get; set; }
   ```

8. **调整OnNavigatedTo方法**：
   ```csharp
   public override void OnNavigatedTo(NavigationContext navigationContext)
   {
       base.OnNavigatedTo(navigationContext);

       // 接收从PatientSelectionViewModel传入的参数
       MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
       CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

       Logger.LogInformation("进入看病流程，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
           MedicalCaseId, CurrentPatient?.Name);

       // 默认导航到Step 1（辨证）
       NavigateToStep(ConsultationStep.Consultation);
   }
   ```

**验收**：
- ✅ 所有FlowStep引用替换为ConsultationStep
- ✅ 删除SelectPatient相关代码
- ✅ 编译通过，0 errors, 0 warnings
- ✅ 步骤文本正确显示（辨证/施治/完成）

---

#### **Task 1.3: 调整MedicalCaseFlowView.xaml（0.5天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`

**修改内容**：

1. **调整患者信息条显示逻辑**：
   ```xml
   <!-- 患者信息条：从Step 1开始就显示（因为已经选中了患者） -->
   <Border Grid.Row="1"
          Background="#E3F2FD"
          BorderBrush="#90CAF9"
          BorderThickness="0,0,0,1">
       <Grid Margin="20,0">
           <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
               <TextBlock Text="🔵 看病中  |  患者：" FontSize="14" Foreground="#333" FontWeight="Bold" />
               <TextBlock Text="{Binding CurrentPatient.Name}" FontSize="14" Foreground="#2E86AB" FontWeight="Bold" Margin="5,0" />
               <TextBlock FontSize="13" Foreground="#666" Margin="20,0,0,0">
                   <Run Text="{Binding CurrentPatient.Gender, Mode=OneWay}" />
                   <Run Text=" | " />
                   <Run Text="{Binding CurrentPatient.Age, Mode=OneWay}" />
                   <Run Text="岁 | " />
                   <Run Text="{Binding CurrentPatient.PhoneNumber, Mode=OneWay}" />
               </TextBlock>
           </StackPanel>
       </Grid>
   </Border>
   ```

2. **调整顶部导航栏标题**：
   ```xml
   <TextBlock Text="看病中 - 辨证/施治/完成"
             FontSize="18"
             FontWeight="Bold"
             HorizontalAlignment="Center"
             VerticalAlignment="Center" />
   ```
   或使用绑定：
   ```xml
   <TextBlock FontSize="18" FontWeight="Bold" HorizontalAlignment="Center" VerticalAlignment="Center">
       <Run Text="看病中 - " />
       <Run Text="{Binding CurrentStepText}" Foreground="#2E86AB" />
   </TextBlock>
   ```

3. **调整"返回主页"按钮为"返回患者选择"**：
   ```xml
   <Button Command="{Binding BackToPatientSelectionCommand}"
          Background="Transparent"
          BorderThickness="0"
          Cursor="Hand"
          Padding="10,5">
       <StackPanel Orientation="Horizontal">
           <TextBlock Text="← " FontSize="18" Foreground="#2E86AB" VerticalAlignment="Center" />
           <TextBlock Text="返回患者选择" FontSize="14" Foreground="#2E86AB" VerticalAlignment="Center" />
       </StackPanel>
   </Button>
   ```

4. **调整底部操作栏按钮**：
   ```xml
   <!-- 上一步按钮：所有步骤都可用，但Step 1禁用 -->
   <Button Content="{Binding PreviousButtonText}"
          Command="{Binding PreviousStepCommand}"
          Style="{StaticResource ActionButtonStyle}"
          Background="#4CAF50"
          Foreground="White"
          BorderThickness="0" />

   <!-- 下一步/完成按钮 -->
   <Button Content="{Binding NextButtonText}"
          Command="{Binding NextStepCommand}"
          Style="{StaticResource ActionButtonStyle}"
          Background="#4CAF50"
          Foreground="White"
          BorderThickness="0" />
   ```

**验收**：
- ✅ 患者信息条正确显示患者信息
- ✅ 顶部标题显示"看病中"状态
- ✅ "返回主页"改为"返回患者选择"
- ✅ 按钮样式正确

---

### **Phase 1 验收标准**

**编译验收**：
- ✅ `dotnet build LYBT.All.sln -c Release --no-restore` 通过
- ✅ 0 errors, 0 warnings

**功能验收**（手动测试）：
- ✅ 看病流程显示3步（辨证/施治/完成）
- ✅ 可以通过"上一步"/"下一步"在3步之间导航
- ✅ 患者信息条正确显示
- ✅ 按钮文本正确（"完成病案"在Step 3显示）

**代码质量**：
- ✅ 所有注释更新（标注Issue #1567）
- ✅ 代码符合编码规范（命名、缩进、注释）

---

## 📊 Phase 2: 患者选择独立化（2天）

### **目标**
- 创建独立的PatientSelectionView
- 从主页"看诊"按钮进入
- 实现MedicalCase创建逻辑

### **任务清单**

#### **Task 2.1: 创建PatientSelectionView（0.5天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml`

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.PatientSelectionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../../../Resources/ManagementModuleStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Grid Background="#F9F9F9">
        <Grid.RowDefinitions>
            <RowDefinition Height="60"/>  <!-- 顶部导航栏 -->
            <RowDefinition Height="*"/>   <!-- 主内容区（患者列表） -->
            <RowDefinition Height="80"/>  <!-- 底部操作栏 -->
        </Grid.RowDefinitions>

        <!-- 顶部导航栏 -->
        <Border Grid.Row="0" Background="White" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid Margin="20,0">
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <Button Command="{Binding BackToHomeCommand}"
                           Background="Transparent"
                           BorderThickness="0"
                           Cursor="Hand"
                           Padding="10,5">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="← " FontSize="18" Foreground="#2E86AB" VerticalAlignment="Center" />
                            <TextBlock Text="返回主页" FontSize="14" Foreground="#2E86AB" VerticalAlignment="Center" />
                        </StackPanel>
                    </Button>
                </StackPanel>

                <TextBlock Text="患者选择"
                          FontSize="18"
                          FontWeight="Bold"
                          HorizontalAlignment="Center"
                          VerticalAlignment="Center" />
            </Grid>
        </Border>

        <!-- 主内容区：患者列表 -->
        <Grid Grid.Row="1" Margin="20">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 搜索框 -->
                <RowDefinition Height="*"/>     <!-- 患者列表 -->
            </Grid.RowDefinitions>

            <!-- 搜索框 -->
            <Border Grid.Row="0" Background="White" Padding="10" Margin="0,0,0,10">
                <StackPanel Orientation="Horizontal">
                    <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                            Width="300"
                            Margin="0,0,10,0"
                            VerticalContentAlignment="Center"
                            Padding="8"
                            FontSize="14" />
                    <Button Content="搜索"
                           Command="{Binding SearchCommand}"
                           Style="{StaticResource PrimaryButtonStyle}"
                           Padding="20,8" />
                </StackPanel>
            </Border>

            <!-- 患者列表 -->
            <DataGrid Grid.Row="1"
                     ItemsSource="{Binding Patients}"
                     SelectedItem="{Binding SelectedPatient}"
                     Style="{StaticResource ModernDataGridStyle}"
                     ColumnHeaderStyle="{StaticResource DataGridColumnHeaderStyle}"
                     IsReadOnly="True"
                     SelectionMode="Single"
                     AutoGenerateColumns="False">

                <DataGrid.Columns>
                    <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120" />
                    <DataGridTextColumn Header="性别" Binding="{Binding Gender}" Width="60" />
                    <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="60" />
                    <DataGridTextColumn Header="电话" Binding="{Binding PhoneNumber}" Width="150" />
                    <DataGridTextColumn Header="上次就诊" Binding="{Binding LastVisitTime, StringFormat='yyyy-MM-dd'}" Width="120" />
                    <DataGridTextColumn Header="就诊次数" Binding="{Binding VisitCount}" Width="100" />
                    <DataGridTextColumn Header="过敏史" Binding="{Binding AllergyHistory}" Width="*" />
                </DataGrid.Columns>
            </DataGrid>
        </Grid>

        <!-- 底部操作栏 -->
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid Margin="20,0">
                <Button Content="开始诊断"
                       Command="{Binding StartConsultationCommand}"
                       Style="{StaticResource ActionButtonStyle}"
                       Background="#4CAF50"
                       Foreground="White"
                       BorderThickness="0"
                       Padding="30,10"
                       FontSize="16"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
            </Grid>
        </Border>

        <!-- 加载遮罩 -->
        <Grid Grid.Row="0" Grid.RowSpan="3"
             Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}"
             Background="#80000000">
            <Border Background="{StaticResource CardBackgroundBrush}"
                   CornerRadius="5"
                   Padding="20"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center">
                <StackPanel Orientation="Horizontal">
                    <ProgressBar Width="20" Height="20"
                                IsIndeterminate="True"
                                Margin="0,0,10,0" />
                    <TextBlock Text="加载中..."
                              VerticalAlignment="Center"
                              Foreground="{StaticResource TextPrimaryBrush}" />
                </StackPanel>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

**验收**：
- ✅ XAML文件创建成功
- ✅ UI布局正确（顶部导航栏 + 患者列表 + 底部操作栏）
- ✅ DataGrid绑定到Patients集合
- ✅ "开始诊断"按钮绑定到StartConsultationCommand

---

#### **Task 2.2: 创建PatientSelectionViewModel（1天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`

```csharp
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 患者选择ViewModel（Issue #1567 - 独立化患者选择）
    /// 作为"看诊"功能的中枢界面
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IPatientRepository _patientRepository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IRegionManager _regionManager;

        #endregion

        #region 属性

        private ObservableCollection<PatientDto> _patients = new();
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    StartConsultationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        #endregion

        #region 命令

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand StartConsultationCommand { get; }

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            IPatientRepository patientRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            SearchCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            StartConsultationCommand = new DelegateCommand(async () => await ExecuteStartConsultationAsync(), CanExecuteStartConsultation)
                .ObservesProperty(() => SelectedPatient);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 返回主页
        /// </summary>
        private void ExecuteBackToHome()
        {
            try
            {
                var homeViewName = SessionManager?.CurrentUser?.Role switch
                {
                    UserRole.Admin => "AdminHomeView",
                    UserRole.Doctor => "ClinicalHomeView",
                    _ => "ClinicalHomeView"
                };

                Logger.LogInformation("返回主页，导航到：{HomeView}", homeViewName);
                _regionManager.RequestNavigate("ContentRegion", homeViewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页时发生异常");
            }
        }

        /// <summary>
        /// 开始诊断（核心逻辑）
        /// Issue #1567 - 在这里创建MedicalCase，而不是在FlowViewModel中
        /// </summary>
        private async Task ExecuteStartConsultationAsync()
        {
            if (SelectedPatient == null)
            {
                await ShowErrorMessageAsync("请先选择患者");
                return;
            }

            try
            {
                SetIsBusy(true, "正在创建医案...");

                Logger.LogInformation("开始诊断，患者：{PatientName}（ID: {PatientId}）",
                    SelectedPatient.Name, SelectedPatient.Id);

                // 1. 检查是否有未完成的医案
                var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(SelectedPatient.Id);
                if (unfinishedCase != null)
                {
                    // TODO Phase 3: 显示确认对话框"该患者有未完成的医案，是否继续？"
                    Logger.LogInformation("检测到未完成的医案，ID: {MedicalCaseId}", unfinishedCase.Id);
                    // 暂时跳过，继续创建新医案
                }

                // 2. 创建MedicalCase
                var medicalCaseId = await CreateMedicalCaseAsync(SelectedPatient.Id);
                if (medicalCaseId == Guid.Empty)
                {
                    await ShowErrorMessageAsync("创建医案失败，请重试");
                    return;
                }

                Logger.LogInformation("医案创建成功，ID: {MedicalCaseId}", medicalCaseId);

                // 3. 导航到看病流程（MedicalCaseFlowView）
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCaseId },
                    { "CurrentPatient", SelectedPatient }
                };

                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

                Logger.LogInformation("已导航到看病流程，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始诊断时发生异常");
                await ShowErrorMessageAsync($"开始诊断失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteStartConsultation()
        {
            return SelectedPatient != null && !IsBusy;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载患者列表
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "加载患者列表...");

                Logger.LogInformation("加载患者列表，搜索关键字：{SearchText}", SearchText);

                // 调用API获取患者列表（带搜索）
                var patients = await _patientRepository.SearchAsync(SearchText);

                Patients = new ObservableCollection<PatientDto>(patients);

                Logger.LogInformation("患者列表加载完成，共 {Count} 条", Patients.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查患者是否有未完成的医案
        /// </summary>
        private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
        {
            try
            {
                // TODO Phase 3: 实现检查逻辑
                // 查询该患者的所有医案，Status=InProgress的即为未完成
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查未完成医案失败，PatientId: {PatientId}", patientId);
                return null;
            }
        }

        /// <summary>
        /// 创建MedicalCase
        /// </summary>
        private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
        {
            try
            {
                Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

                if (SessionManager == null || SessionManager.CurrentUser == null)
                {
                    Logger.LogError("SessionManager或CurrentUser为null，无法创建MedicalCase");
                    return Guid.Empty;
                }

                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = patientId,
                    DoctorId = SessionManager.CurrentUser.Id,
                    Status = MedicalCaseStatus.Active,
                    Remark = null
                };

                var createdDto = await _medicalCaseRepository.CreateAsync(createDto);

                Logger.LogInformation("MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);
                return createdDto.Id;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建MedicalCase失败，PatientId: {PatientId}", patientId);
                return Guid.Empty;
            }
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            Logger.LogInformation("进入患者选择界面");

            // 自动加载患者列表
            _ = LoadPatientsAsync();
        }

        #endregion
    }
}
```

**验收**：
- ✅ ViewModel创建成功
- ✅ StartConsultationCommand创建MedicalCase
- ✅ 导航到MedicalCaseFlowView，传递MedicalCaseId和CurrentPatient
- ✅ 编译通过

---

#### **Task 2.3: 注册PatientSelectionView到Prism模块（0.5天）**

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`

```csharp
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Views
        containerRegistry.RegisterForNavigation<MedicalCaseFlowView>();
        containerRegistry.RegisterForNavigation<PatientSelectionView>(); // ← 新增
        containerRegistry.RegisterForNavigation<ConsultationFormView>();
        containerRegistry.RegisterForNavigation<PrescriptionEditorView>();
        containerRegistry.RegisterForNavigation<CompletionView>();

        // 注册ViewModels
        containerRegistry.Register<MedicalCaseFlowViewModel>();
        containerRegistry.Register<PatientSelectionViewModel>(); // ← 新增

        // 注册Repositories
        containerRegistry.Register<IMedicalCaseRepository, MedicalCaseRepository>();
        // ...
    }
}
```

**验收**：
- ✅ PatientSelectionView注册成功
- ✅ 可以通过Region导航到PatientSelectionView

---

### **Phase 2 验收标准**

**编译验收**：
- ✅ `dotnet build LYBT.All.sln -c Release --no-restore` 通过
- ✅ 0 errors, 0 warnings

**功能验收**（手动测试）：
- ✅ 从主页点击"看诊"按钮，进入患者选择界面
- ✅ 患者选择界面显示患者列表
- ✅ 搜索功能正常
- ✅ 选中患者后，"开始诊断"按钮激活
- ✅ 点击"开始诊断"，创建MedicalCase并进入看病流程
- ✅ 看病流程显示患者信息（从PatientSelectionViewModel传入）

---

## 📊 Phase 3: 状态管理完善（1-2天）

### **目标**
- 实现"暂存医案"功能
- 实现"取消医案"功能
- 实现"完成病案"功能
- 所有退出操作返回患者选择界面
- 实现暂存恢复功能

### **任务清单**

#### **Task 3.1: 实现暂存医案功能（0.5天）**

**修改文件**: `MedicalCaseFlowViewModel.cs`

```csharp
/// <summary>
/// 暂存医案（保存WorkflowContext + 返回患者选择界面）
/// </summary>
private async void ExecuteSaveDraft()
{
    try
    {
        Logger.LogInformation("暂存医案，当前步骤：{CurrentStep}, MedicalCaseId: {MedicalCaseId}",
            CurrentStep, MedicalCaseId);

        SetIsBusy(true, "正在保存...");

        // 1. 调用当前Step的ISaveable接口保存数据
        if (CurrentStepViewModel is ISaveable saveable)
        {
            var success = await saveable.SaveAsync();
            if (!success)
            {
                Logger.LogWarning("当前步骤数据保存失败");
                await ShowErrorMessageAsync("保存失败，请检查数据");
                return;
            }
        }

        // 2. TODO Phase 3: 保存WorkflowContext到数据库
        // await _workflowStateRepository.SaveStateAsync(context);

        // 3. 更新MedicalCase状态为InProgress
        await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.InProgress);

        Logger.LogInformation("医案暂存成功");
        await ShowSuccessMessageAsync("医案已暂存");

        // 4. 返回患者选择界面
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "暂存医案失败");
        await ShowErrorMessageAsync($"暂存失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

**验收**：
- ✅ 点击"暂存医案"，保存数据
- ✅ MedicalCase.Status更新为InProgress
- ✅ 返回患者选择界面

---

#### **Task 3.2: 实现取消医案功能（0.5天）**

**修改文件**: `MedicalCaseFlowViewModel.cs`

```csharp
/// <summary>
/// 取消医案（确认对话框 + 返回患者选择界面）
/// </summary>
private async void ExecuteCancel()
{
    try
    {
        Logger.LogInformation("取消医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

        // 1. 显示确认对话框
        var confirmed = await ShowConfirmationAsync(
            "确定要取消本次医案吗？未保存的数据将丢失！",
            "取消医案");

        if (!confirmed)
        {
            Logger.LogInformation("用户取消了取消操作");
            return;
        }

        // 2. 更新MedicalCase状态为Cancelled
        await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Cancelled);

        // 3. TODO Phase 3: 删除草稿（如果有）
        // await _workflowStateRepository.DeleteStateAsync(MedicalCaseId);

        Logger.LogInformation("医案已取消");

        // 4. 返回患者选择界面
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "取消医案失败");
        await ShowErrorMessageAsync($"取消失败：{ex.Message}");
    }
}
```

**验收**：
- ✅ 点击"取消医案"，显示确认对话框
- ✅ 确认后，MedicalCase.Status更新为Cancelled
- ✅ 返回患者选择界面

---

#### **Task 3.3: 实现完成病案功能（0.5天）**

**修改文件**: `MedicalCaseFlowViewModel.cs`

```csharp
/// <summary>
/// 完成病案（Step 3点击"完成看诊"）
/// </summary>
private async Task ExecuteNextStepAsync()
{
    if (CurrentStep == ConsultationStep.Completion)
    {
        // Step 3: 完成病案
        try
        {
            SetIsBusy(true, "正在完成病案...");

            Logger.LogInformation("完成病案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

            // 1. 验证并保存当前步骤数据
            if (CurrentStepViewModel is IValidatable validatable)
            {
                if (!validatable.Validate())
                {
                    await ShowErrorMessageAsync(validatable.ValidationMessage);
                    return;
                }
            }

            if (CurrentStepViewModel is ISaveable saveable)
            {
                var success = await saveable.SaveAsync();
                if (!success)
                {
                    await ShowErrorMessageAsync("保存失败，请检查数据");
                    return;
                }
            }

            // 2. 更新MedicalCase状态为Completed
            await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);

            // 3. TODO Phase 3: 删除草稿（如果有）
            // await _workflowStateRepository.DeleteStateAsync(MedicalCaseId);

            Logger.LogInformation("病案已完成");
            await ShowSuccessMessageAsync("病案已完成");

            // 4. 返回患者选择界面
            _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "完成病案失败");
            await ShowErrorMessageAsync($"完成失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }

        return;
    }

    // 否则，正常前进到下一步
    // ...（现有的NextStep逻辑）
}
```

**验收**：
- ✅ Step 3点击"完成看诊"，保存数据
- ✅ MedicalCase.Status更新为Completed
- ✅ 返回患者选择界面

---

#### **Task 3.4: 实现暂存恢复功能（0.5天）**

**修改文件**: `PatientSelectionViewModel.cs`

```csharp
/// <summary>
/// 检查患者是否有未完成的医案
/// </summary>
private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
{
    try
    {
        Logger.LogInformation("检查患者未完成医案，PatientId: {PatientId}", patientId);

        // 查询该患者的所有医案，Status=InProgress的即为未完成
        var cases = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
        var unfinishedCase = cases.FirstOrDefault(c => c.Status == MedicalCaseStatus.InProgress);

        if (unfinishedCase != null)
        {
            Logger.LogInformation("检测到未完成医案，ID: {MedicalCaseId}", unfinishedCase.Id);
        }

        return unfinishedCase;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "检查未完成医案失败，PatientId: {PatientId}", patientId);
        return null;
    }
}

/// <summary>
/// 开始诊断（修改版：支持暂存恢复）
/// </summary>
private async Task ExecuteStartConsultationAsync()
{
    if (SelectedPatient == null)
    {
        await ShowErrorMessageAsync("请先选择患者");
        return;
    }

    try
    {
        SetIsBusy(true, "正在检查...");

        // 1. 检查是否有未完成的医案
        var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(SelectedPatient.Id);
        if (unfinishedCase != null)
        {
            // 显示确认对话框
            var resume = await ShowConfirmationAsync(
                $"该患者有未完成的医案（创建于 {unfinishedCase.CreatedAt:yyyy-MM-dd HH:mm}），是否继续看诊？",
                "未完成的医案",
                "继续看诊",
                "新建医案");

            if (resume)
            {
                // 继续看诊：加载WorkflowContext，跳转到上次的Step
                Logger.LogInformation("继续看诊，MedicalCaseId: {MedicalCaseId}", unfinishedCase.Id);

                // TODO Phase 3: 加载WorkflowContext
                // var context = await _workflowStateRepository.LoadStateAsync(unfinishedCase.Id);

                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", unfinishedCase.Id },
                    { "CurrentPatient", SelectedPatient },
                    // { "ResumeStep", context.CurrentStep } // TODO: 恢复到上次的步骤
                };

                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
                return;
            }
            else
            {
                // 新建医案：继续下面的创建逻辑
                Logger.LogInformation("用户选择新建医案");
            }
        }

        // 2. 创建新医案（原有逻辑）
        SetIsBusy(true, "正在创建医案...");
        var medicalCaseId = await CreateMedicalCaseAsync(SelectedPatient.Id);
        // ...（原有逻辑）
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "开始诊断时发生异常");
        await ShowErrorMessageAsync($"开始诊断失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

**验收**：
- ✅ 选中有未完成医案的患者，显示提示对话框
- ✅ 点击"继续看诊"，加载上次的MedicalCase
- ✅ 点击"新建医案"，创建新的MedicalCase

---

### **Phase 3 验收标准**

**功能验收**：
- ✅ 暂存医案：保存数据，Status=InProgress，返回患者选择界面
- ✅ 取消医案：显示确认对话框，Status=Cancelled，返回患者选择界面
- ✅ 完成病案：保存数据，Status=Completed，返回患者选择界面
- ✅ 选中有未完成医案的患者，显示提示
- ✅ 继续看诊：加载上次的数据（暂时跳过WorkflowContext加载）

---

## 📊 Phase 4: 测试与文档（1天）

### **任务清单**

#### **Task 4.1: 编写集成测试（0.5天）**

**文件**: `tests/Integration/LYBT.Desktop.MedicalCase.Tests/MedicalCaseWorkflowTests.cs`

```csharp
[Fact]
public async Task Should_CompleteFullWorkflow_WhenAllStepsValid()
{
    // Arrange: 创建测试患者
    var patient = await CreateTestPatientAsync();

    // Act: 执行完整流程
    // 1. 进入患者选择界面
    NavigateToPatientSelection();

    // 2. 选择患者并开始诊断
    SelectPatient(patient.Id);
    await StartConsultation();

    // 3. 完成辨证
    FillConsultationData();
    await NextStep();

    // 4. 完成施治
    FillPrescriptionData();
    await NextStep();

    // 5. 完成病案
    await CompleteMedicalCase();

    // Assert: 验证状态
    var medicalCase = await GetMedicalCaseAsync();
    Assert.Equal(MedicalCaseStatus.Completed, medicalCase.Status);

    // 验证返回到患者选择界面
    Assert.Equal("PatientSelectionView", CurrentView);
}

[Fact]
public async Task Should_SaveDraft_WhenPausedInMiddle()
{
    // Arrange
    var patient = await CreateTestPatientAsync();

    // Act
    NavigateToPatientSelection();
    SelectPatient(patient.Id);
    await StartConsultation();

    // 在Step 2暂停
    FillConsultationData();
    await NextStep();
    await SaveDraft();

    // Assert
    var medicalCase = await GetMedicalCaseAsync();
    Assert.Equal(MedicalCaseStatus.InProgress, medicalCase.Status);
    Assert.Equal("PatientSelectionView", CurrentView);
}
```

**验收**：
- ✅ 集成测试覆盖完整流程
- ✅ 测试通过率100%

---

#### **Task 4.2: 更新文档（0.5天）**

**更新文件**：
1. `docs/architecture/client/README.md` - 更新MedicalCase模块架构描述
2. `docs/development/client/medicalcase-workflow-guide.md` - 更新工作流指南
3. `docs/quick-reference/code-patterns.md` - 添加患者选择 + 看病流程的代码模式

**验收**：
- ✅ 文档与代码100%同步
- ✅ 所有引用更新（FlowStep → ConsultationStep）

---

## ✅ 总体验收标准

### **编译验收**
- ✅ `dotnet build LYBT.All.sln -c Release --no-restore` 通过
- ✅ 0 errors, 0 warnings

### **测试验收**
- ✅ 所有单元测试通过
- ✅ 所有集成测试通过
- ✅ 测试覆盖率 ≥ 80%

### **功能验收**
1. ✅ 主页"看诊"按钮 → 患者选择界面
2. ✅ 患者选择界面可返回主页
3. ✅ 选中患者 → "开始诊断" → 创建MedicalCase → 看病流程
4. ✅ 看病流程3步可自由往返
5. ✅ 暂存医案 → 返回患者选择界面
6. ✅ 取消医案 → 确认对话框 → 返回患者选择界面
7. ✅ 完成病案 → 返回患者选择界面
8. ✅ 选中有未完成医案的患者 → 提示"是否继续？"

### **质量验收**
- ✅ 代码符合编码规范
- ✅ 所有注释清晰（标注Issue #1567）
- ✅ 文档与代码100%同步

---

## 📅 时间线

| Phase | 内容 | 工作量 | 累计 |
|-------|------|--------|------|
| Phase 1 | 基础重构 | 2天 | 2天 |
| Phase 2 | 患者选择独立化 | 2天 | 4天 |
| Phase 3 | 状态管理完善 | 1-2天 | 5-6天 |
| Phase 4 | 测试与文档 | 1天 | 6-7天 |

**总计**：6-7天

---

## 🔄 Phase间依赖关系

```
Phase 1 (基础重构) ─┬─> Phase 2 (患者选择独立化)
                   │       ↓
                   └─> Phase 3 (状态管理完善)
                           ↓
                       Phase 4 (测试与文档)
```

- Phase 1和Phase 2可以部分并行（枚举重构完成后即可开始Phase 2）
- Phase 3依赖Phase 1和Phase 2完成
- Phase 4依赖所有Phase完成

---

## 🚨 风险与应对

### **风险1：FlowStep枚举重构影响范围大**
**应对**：
- 使用IDE全局搜索FlowStep引用
- 逐个文件检查和替换
- 每次修改后立即编译验证

### **风险2：导航逻辑复杂，容易出错**
**应对**：
- 先完成Phase 1和Phase 2的功能验证
- 每个Phase完成后立即手动测试
- 编写集成测试覆盖关键路径

### **风险3：草稿恢复逻辑需要WorkflowContext持久化**
**应对**：
- Phase 3暂时跳过WorkflowContext加载（标记TODO）
- 后续Phase可以补充IWorkflowStateRepository实现
- 当前版本只恢复MedicalCaseId和CurrentPatient

---

## 📚 参考资料

- 讨论文档：`docs/architecture/client/medicalcase-fourstep-workflow-discussion.md`
- GitHub Issue: #1567
- 架构指南：`docs/architecture/client/README.md`
- Epic #1494：医案流程四步走

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
