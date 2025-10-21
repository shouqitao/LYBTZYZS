# PrescriptionEditor集成设计文档（Task #1499）

> **文档版本**: v1.0
> **创建日期**: 2025-10-20
> **关联Issue**: #1499 - Step 3 PrescriptionEditor实现（8列DataGrid）
> **Epic**: #1494 - 医案流程UI重构
> **作者**: Claude Code

---

## 📋 目录

- [1. 背景与目标](#1-背景与目标)
- [2. 现有代码分析](#2-现有代码分析)
- [3. 集成方案](#3-集成方案)
- [4. 详细设计](#4-详细设计)
- [5. 实施步骤](#5-实施步骤)
- [6. 验收标准](#6-验收标准)

---

## 1. 背景与目标

### 1.1 任务背景

**Epic #1494** 要求实现医案流程的完整UI，其中**Step 3 - 处方录入**是核心功能之一。

**Issue #1499** 定义了处方编辑器的详细需求：
- 8列DataGrid布局（每行4个药材）
- 三种录入模式：手工录入、验方导入、历史复制
- 自动价格计算（单剂价格 × 剂数 = 总价格）
- 支持拼音码快速输入
- 小屏幕兼容性（1366x768）
- 保存时创建Prescription并关联到MedicalCase

### 1.2 设计目标

- ✅ **最大程度复用现有代码**：避免重复造轮子
- ✅ **符合MVP原则**：够用即好，避免过度设计
- ✅ **架构一致性**：符合IValidatable和ISaveable接口规范
- ✅ **集成简洁**：与MedicalCaseFlowViewModel无缝集成

---

## 2. 现有代码分析

### 2.1 现有Prescriptions模块资源

**核心ViewModel**：

| 文件 | 行数 | 功能 | 是否可复用 |
|-----|------|------|-----------|
| `PrescriptionViewModel.cs` | 969行 | 完整的处方编辑逻辑 | ✅ **核心复用** |
| `PrescriptionItemViewModel.cs` | 178行 | 单个药材ViewModel | ✅ 已被PrescriptionViewModel使用 |
| `PrescriptionItemRow.cs` | 31行 | 8列DataGrid行模型 | ✅ 已被PrescriptionViewModel使用 |
| `PrescriptionEditorDialogViewModel.cs` | 604行 | 对话框版本（只编辑处方信息） | ❌ 不包含DataGrid逻辑 |

**Components组件**（已由PrescriptionViewModel依赖）：

| 组件 | 职责 | 位置 |
|-----|------|------|
| `PrescriptionDataManager.cs` | 数据管理（Items、ItemRows转换） | ViewModels/Components/ |
| `PrescriptionCalculator.cs` | 价格计算逻辑 | ViewModels/Components/ |
| `PrescriptionValidator.cs` | 验证逻辑 | ViewModels/Components/ |
| `PrescriptionCommandHandler.cs` | 命令处理（添加/删除药材） | ViewModels/Components/ |
| `PrescriptionEventCoordinator.cs` | 事件协调 | ViewModels/Components/ |

### 2.2 PrescriptionViewModel功能清单

**✅ 已实现的功能**（基于代码分析）：

1. **数据绑定**：
   - `PrescriptionItems`: 药材列表（线性）
   - `ItemRows`: 8列DataGrid行集合
   - `DosageCount`: 剂数
   - `Usage`: 用法
   - `MedicalAdvice`: 医嘱
   - `Remark`: 备注

2. **价格计算**：
   - `RecalculatePrice()`: 自动计算单剂价格和总价格

3. **命令支持**：
   - 添加/删除药材（通过PrescriptionCommandHandler）
   - 验方导入（通过PrescriptionEventCoordinator）
   - 历史复制（推测已实现）

4. **数据持久化**：
   - 依赖`IPrescriptionRepository`
   - 依赖`IMedicalCaseRepository`
   - 依赖`IHerbRepository`

### 2.3 差距分析

**Issue #1499需求 vs PrescriptionViewModel现有功能**：

| 需求 | PrescriptionViewModel支持 | 缺口 |
|-----|--------------------------|------|
| 8列DataGrid | ✅ ItemRows支持 | 无 |
| 药材ComboBox + 拼音码 | ✅ IHerbRepository支持 | 无 |
| 添加/删除行 | ✅ CommandHandler支持 | 无 |
| 手工录入 | ✅ 默认模式 | 无 |
| 验方导入 | ✅ EventCoordinator支持 | 无 |
| 历史复制 | 🔍 **需验证** | 可能需补充 |
| 自动价格计算 | ✅ Calculator支持 | 无 |
| **IValidatable接口** | ❌ **未实现** | **需补充** |
| **ISaveable接口** | ❌ **未实现** | **需补充** |
| 小屏幕兼容性 | 🔍 **需验证** | 可能需调整XAML |

**结论**：
- ✅ **核心功能已完整实现**：8列DataGrid、价格计算、验方导入
- ❌ **缺失接口适配**：需要实现IValidatable和ISaveable接口
- 🔍 **需补充细节**：历史复制、XAML小屏幕优化

---

## 3. 集成方案

### 3.1 方案选择

**方案1（推荐）：包装PrescriptionViewModel**

创建`PrescriptionEditorViewModel`（轻量级包装类），内部持有`PrescriptionViewModel`实例，实现IValidatable和ISaveable接口。

**优点**：
- ✅ 最大程度复用现有代码（969行 + 5个Components）
- ✅ 避免重复实现DataGrid、价格计算、验方导入等复杂逻辑
- ✅ 减少bug风险
- ✅ 符合MVP"够用即好"原则

**缺点**：
- ⚠️ 依赖链较长（PrescriptionViewModel → 5个Components → 3个Repository）
- ⚠️ 可能有冗余依赖（如果某些功能不需要）

**方案2（备选）：从零实现精简版**

创建全新的`PrescriptionEditorViewModel`，只实现Issue #1499的核心功能。

**优点**：
- ✅ 代码简洁，依赖清晰
- ✅ 更容易理解和维护

**缺点**：
- ❌ 重复实现8列DataGrid逻辑（约300-400行）
- ❌ 重复实现价格计算逻辑（约100行）
- ❌ 重复实现验方导入逻辑（约200行）
- ❌ 违反DRY原则
- ❌ 需要额外的测试覆盖

**决策：选择方案1**

理由：
1. **避免重复造轮子**：PrescriptionViewModel已经完整实现了所有需求功能
2. **减少开发时间**：只需创建适配层（约100-150行）
3. **减少bug风险**：复用经过测试的代码
4. **符合架构原则**：统一使用Prescriptions模块的标准实现

---

### 3.2 架构设计

```
MedicalCaseFlowViewModel (Step 3 导航)
          ↓
PrescriptionEditorViewModel (适配层 - NEW)
    ├─ 实现 IValidatable: 验证药材列表不为空
    ├─ 实现 ISaveable: 调用PrescriptionRepository.CreateAsync
    └─ 包装 PrescriptionViewModel (完整逻辑)
          ├─ ItemRows: 8列DataGrid数据
          ├─ DosageCount, Usage, MedicalAdvice, Remark
          ├─ RecalculatePrice(): 价格计算
          ├─ AddItemCommand, RemoveItemCommand
          ├─ ImportFormulaCommand: 验方导入
          └─ 5个Components (DataManager, Calculator, Validator, CommandHandler, EventCoordinator)
```

---

## 4. 详细设计

### 4.1 PrescriptionEditorViewModel接口定义

```csharp
/// <summary>
/// 处方编辑器ViewModel - Task #1499 Step 3实现
/// 包装PrescriptionViewModel并实现IValidatable和ISaveable接口
/// Epic #1494: 医案流程UI重构
/// </summary>
public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    #region 服务依赖

    private readonly PrescriptionViewModel _prescriptionViewModel;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    #endregion

    #region 数据属性

    /// <summary>
    /// 当前患者信息（从MedicalCaseFlowViewModel传递）
    /// </summary>
    public PatientDto? CurrentPatient { get; set; }

    /// <summary>
    /// 医疗案例ID（从MedicalCaseFlowViewModel传递）
    /// </summary>
    public Guid MedicalCaseId { get; set; }

    /// <summary>
    /// 内部PrescriptionViewModel实例（完整逻辑）
    /// </summary>
    public PrescriptionViewModel PrescriptionViewModel => _prescriptionViewModel;

    #endregion

    #region 委托属性（暴露PrescriptionViewModel核心属性）

    /// <summary>
    /// 处方项行集合（8列DataGrid绑定）
    /// </summary>
    public ObservableCollection<PrescriptionItemRow> ItemRows => _prescriptionViewModel.ItemRows;

    /// <summary>
    /// 剂数
    /// </summary>
    public int DosageCount
    {
        get => _prescriptionViewModel.DosageCount;
        set => _prescriptionViewModel.DosageCount = value;
    }

    /// <summary>
    /// 用法
    /// </summary>
    public string Usage
    {
        get => _prescriptionViewModel.Usage;
        set => _prescriptionViewModel.Usage = value;
    }

    /// <summary>
    /// 医嘱
    /// </summary>
    public string MedicalAdvice
    {
        get => _prescriptionViewModel.MedicalAdvice;
        set => _prescriptionViewModel.MedicalAdvice = value;
    }

    /// <summary>
    /// 单剂价格（自动计算）
    /// </summary>
    public decimal SingleDosagePrice => _prescriptionViewModel.SingleDosagePrice;

    /// <summary>
    /// 总价格（自动计算）
    /// </summary>
    public decimal TotalPrice => _prescriptionViewModel.TotalPrice;

    #endregion

    #region 委托命令（暴露PrescriptionViewModel核心命令）

    /// <summary>
    /// 添加药材行命令
    /// </summary>
    public DelegateCommand AddRowCommand => _prescriptionViewModel.AddRowCommand;

    /// <summary>
    /// 删除药材行命令
    /// </summary>
    public DelegateCommand<PrescriptionItemRow> DeleteRowCommand => _prescriptionViewModel.DeleteRowCommand;

    /// <summary>
    /// 验方导入命令
    /// </summary>
    public DelegateCommand ImportFormulaCommand => _prescriptionViewModel.ImportFormulaCommand;

    /// <summary>
    /// 历史复制命令
    /// </summary>
    public DelegateCommand ImportHistoryCommand => _prescriptionViewModel.ImportHistoryCommand;

    #endregion

    #region IValidatable实现

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// 验证处方数据（至少1个药材）
    /// </summary>
    public bool Validate()
    {
        if (CurrentPatient == null)
        {
            ValidationMessage = "请先选择患者";
            return false;
        }

        if (MedicalCaseId == Guid.Empty)
        {
            ValidationMessage = "MedicalCaseId不能为空";
            return false;
        }

        // 验证至少有1个药材
        var allItems = _prescriptionViewModel.GetAllItems(); // 从ItemRows获取所有非空药材
        if (allItems.Count == 0)
        {
            ValidationMessage = "请至少添加一个药材";
            return false;
        }

        // 验证每个药材的必填字段
        foreach (var item in allItems)
        {
            if (item.HerbId == Guid.Empty)
            {
                ValidationMessage = "存在未选择药材的行";
                return false;
            }

            if (item.Dosage <= 0)
            {
                ValidationMessage = $"药材 {item.HerbName} 的用量无效";
                return false;
            }
        }

        ValidationMessage = string.Empty;
        return true;
    }

    #endregion

    #region ISaveable实现

    /// <summary>
    /// 保存处方 - Task #1499: 创建Prescription并关联到MedicalCase
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        try
        {
            SetIsBusy(true, "正在保存处方...");

            // 1. 验证数据
            if (!Validate())
            {
                Logger.LogWarning("处方验证失败：{Message}", ValidationMessage);
                return false;
            }

            // 2. 构造PrescriptionCreateDto
            var allItems = _prescriptionViewModel.GetAllItems();
            var prescriptionDto = new PrescriptionCreateDto
            {
                MedicalCaseId = MedicalCaseId,
                PatientId = CurrentPatient!.Id,
                DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                DosageCount = DosageCount,
                Usage = Usage,
                Advice = MedicalAdvice,
                Remark = _prescriptionViewModel.Remark,
                Discount = 1.0m, // 默认无折扣
                Items = allItems.Select(item => new PrescriptionItemDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    Notes = item.Notes
                }).ToList()
            };

            // 3. 创建Prescription
            var result = await _prescriptionRepository.CreateAsync(prescriptionDto);
            Logger.LogInformation("处方创建成功，ID: {PrescriptionId}", result.Id);

            // 4. 更新MedicalCase关联Prescription
            await _medicalCaseRepository.UpdatePrescriptionIdAsync(MedicalCaseId, result.Id);
            Logger.LogInformation("MedicalCase.PrescriptionId已更新");

            await ShowSuccessMessageAsync("处方已保存");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方时发生异常");
            await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            return false;
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    #endregion

    #region 构造函数

    public PrescriptionEditorViewModel(
        PrescriptionViewModel prescriptionViewModel,
        IPrescriptionRepository prescriptionRepository,
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _prescriptionViewModel = prescriptionViewModel ?? throw new ArgumentNullException(nameof(prescriptionViewModel));
        _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

        Logger.LogInformation("PrescriptionEditorViewModel已初始化");
    }

    #endregion

    #region INavigationAware

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        try
        {
            // 接收患者信息和MedicalCaseId
            if (navigationContext.Parameters.ContainsKey("Patient"))
            {
                CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("Patient");
                Logger.LogInformation("接收到患者信息：{PatientName}", CurrentPatient.Name);
            }

            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                Logger.LogInformation("接收到MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
            }

            // 初始化PrescriptionViewModel的MedicalCaseId
            _prescriptionViewModel.MedicalCaseId = MedicalCaseId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到处方编辑器时发生异常");
        }
    }

    #endregion
}
```

### 4.2 PrescriptionEditorView XAML设计

```xaml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.PrescriptionEditorView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <!--
    注意：DataContext = PrescriptionEditorViewModel
    但DataGrid绑定到 PrescriptionViewModel.ItemRows（通过委托属性暴露）
    -->

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- Tab切换 -->
            <RowDefinition Height="*"/>    <!-- DataGrid -->
            <RowDefinition Height="Auto"/> <!-- 处方信息 -->
            <RowDefinition Height="Auto"/> <!-- 提示信息 -->
        </Grid.RowDefinitions>

        <!-- Row 0: Tab切换（手工录入/验方导入/历史复制） -->
        <TabControl Grid.Row="0" Height="40" SelectedIndex="0">
            <TabItem Header="手工录入"/>
            <TabItem Header="验方导入"/>
            <TabItem Header="历史复制"/>
        </TabControl>

        <!-- Row 1: 8列DataGrid（核心区域） -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding ItemRows}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  VirtualizingPanel.IsVirtualizing="True">
            <DataGrid.Columns>
                <!-- 药材1 + 用量1 -->
                <DataGridTemplateColumn Header="药材1" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <ComboBox ItemsSource="{Binding DataContext.PrescriptionViewModel.AllHerbs, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                      SelectedItem="{Binding Item1.Herb, UpdateSourceTrigger=PropertyChanged}"
                                      IsEditable="True"
                                      DisplayMemberPath="Name"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="用量1" Binding="{Binding Item1.Dosage}" Width="60"/>

                <!-- 药材2 + 用量2（重复） -->
                <!-- 药材3 + 用量3（重复） -->
                <!-- 药材4 + 用量4（重复） -->
            </DataGrid.Columns>
        </DataGrid>

        <!-- Row 2: 处方信息 -->
        <Border Grid.Row="2" BorderBrush="#E0E0E0" BorderThickness="1" Padding="15" Margin="0,10,0,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <!-- 剂数 -->
                <TextBlock Grid.Column="0" Text="剂数：" VerticalAlignment="Center" Margin="0,0,10,0"/>
                <TextBox Grid.Column="1" Text="{Binding DosageCount}" Width="100" HorizontalAlignment="Left"/>

                <!-- 用法 -->
                <TextBlock Grid.Column="2" Text="用法：" VerticalAlignment="Center" Margin="20,0,10,0"/>
                <TextBox Grid.Column="3" Text="{Binding Usage}"/>
            </Grid>
        </Border>

        <!-- Row 3: 提示信息 -->
        <Border Grid.Row="3" Background="#FFF3E0" BorderBrush="#FFB74D" BorderThickness="1" Padding="10">
            <StackPanel>
                <TextBlock Text="提示：" FontWeight="Bold"/>
                <TextBlock Text="• 填写完成后点击【下一步】完成看诊"/>
                <TextBlock Text="• 保存时将创建Prescription实体并关联到MedicalCase.PrescriptionId"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

## 5. 实施步骤

### Phase 1: 创建PrescriptionEditorViewModel（适配层）

1. ✅ 创建文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`
2. ✅ 实现IValidatable接口（验证至少1个药材）
3. ✅ 实现ISaveable接口（创建Prescription + 关联MedicalCase）
4. ✅ 委托PrescriptionViewModel的属性和命令
5. ✅ 实现INavigationAware（接收Patient和MedicalCaseId）

### Phase 2: 创建PrescriptionEditorView（UI）

1. ✅ 创建文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PrescriptionEditorView.xaml`
2. ✅ 实现8列DataGrid（绑定ItemRows）
3. ✅ 实现Tab切换UI（手工录入/验方导入/历史复制）
4. ✅ 实现处方信息区（剂数、用法、医嘱、价格显示）
5. ✅ 实现提示信息区

### Phase 3: 集成到MedicalCaseFlowViewModel

1. ✅ 修改`NavigateToStep(FlowStep.FillPrescription)`：
   ```csharp
   case FlowStep.FillPrescription:
       var prescriptionEditorViewModel = _containerProvider.Resolve<PrescriptionEditorViewModel>();
       prescriptionEditorViewModel.CurrentPatient = CurrentPatient;
       prescriptionEditorViewModel.MedicalCaseId = MedicalCaseId;
       CurrentStepViewModel = prescriptionEditorViewModel;
       Logger.LogInformation("PrescriptionEditorViewModel已创建");
       break;
   ```

### Phase 4: 注册服务

修改`MedicalCaseModule.cs`：
```csharp
containerRegistry.Register<PrescriptionEditorViewModel>();
containerRegistry.RegisterForNavigation<Views.PrescriptionEditorView>();
```

### Phase 5: 编译测试

1. ✅ 运行编译：`dotnet build LYBT.All.sln -c Release --no-restore`
2. ✅ 验证0 errors, 0 warnings
3. ✅ 手动测试三种录入模式
4. ✅ 验证价格自动计算
5. ✅ 验证Prescription创建和MedicalCase关联

### Phase 6: 创建PR

1. ✅ 创建功能分支：`feature/1499-prescription-editor`
2. ✅ 提交代码：遵循中文commit规范
3. ✅ 创建PR：关联Issue #1499
4. ✅ 等待审查和合并

---

## 6. 验收标准

### 6.1 功能验收

- [ ] 8列DataGrid布局正确显示
- [ ] 药材ComboBox支持拼音码过滤（继承自PrescriptionViewModel）
- [ ] 添加/删除行功能正常
- [ ] Tab切换功能正常（手工录入/验方导入/历史复制）
- [ ] 剂数、用法输入正常
- [ ] 自动计算价格正常（单剂价格 × 剂数 = 总价格）
- [ ] IValidatable验证正常（至少1个药材）
- [ ] ISaveable保存正常（创建Prescription + 关联MedicalCase）
- [ ] 小屏幕兼容性（1366x768下DataGrid完整可见）

### 6.2 代码质量

- [ ] 编译通过：**0 errors, 0 warnings**
- [ ] 符合MVVM架构规范
- [ ] 符合Client端三层架构（ViewModel、View）
- [ ] 实现IValidatable和ISaveable接口
- [ ] 委托属性和命令正确暴露PrescriptionViewModel
- [ ] 依赖注入正确配置（PrescriptionViewModel、Repositories）

### 6.3 架构合规

- [ ] 模块依赖正确（MedicalCase → Prescriptions）
- [ ] 不违反技术黑名单（无Redis/CQRS/MediatR/Docker）
- [ ] 符合MVP优先原则（复用现有代码，避免过度设计）
- [ ] 文件组织规范（ViewModels、Views目录）

---

## 7. 风险与对策

### 7.1 风险点

| 风险 | 影响 | 对策 |
|-----|------|------|
| PrescriptionViewModel依赖链过长 | 集成复杂度增加 | 使用容器自动注入，简化依赖管理 |
| 8列DataGrid性能问题（大量药材） | 小屏幕卡顿 | 启用VirtualizingPanel.IsVirtualizing=True |
| 历史复制功能未实现 | 验收失败 | 先实现手工录入和验方导入，历史复制作为可选功能 |
| Prescription创建API不存在 | SaveAsync失败 | 验证`IPrescriptionRepository.CreateAsync`是否存在 |

### 7.2 降级方案

如果**方案1（包装PrescriptionViewModel）**遇到无法解决的依赖问题，可降级到**方案2（从零实现精简版）**：
- 只实现8列DataGrid + 价格计算 + 基本保存
- 延后验方导入和历史复制功能
- 估算额外开发时间：2-3天

---

## 8. 参考资料

- Issue #1499: [Task-5] Step 3 - PrescriptionEditor实现（8列DataGrid）
- Epic #1494: 医案流程UI重构
- `docs/architecture/client/medical-case-flow-ui-layouts.md` (Section 5)
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemRow.cs`
- `.claude/core/PRINCIPLES.md`: 最小充分交付、增量优化、避免过度设计

---

**文档状态**: v1.0 初稿
**待审核**: 需用户确认方案选择和实施细节
**下一步**: 用户批准后启动Phase 1实施

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
