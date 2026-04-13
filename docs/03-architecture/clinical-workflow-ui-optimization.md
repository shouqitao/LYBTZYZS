# 中医看诊全流程 UI/UX 优化方案

> **文档版本**: v1.0  
> **创建日期**: 2026-04-10  
> **最后更新**: 2026-04-10  
> **状态**: 待评审  
> **目标**: 使看诊界面映射真实中医诊疗流程，提高医生操作效率

---

## 一、当前架构分析

### 1.1 现有临床工作区布局

```
┌─────────────────────────────────────────────────────────────────┐
│                    MedicalCaseWorkspaceView                      │
├──────────────────┬──────────────────────────────────────────────┤
│ 左侧 25%         │ 右侧 75%                                      │
│ (MinWidth=280)   │ BaseDetailContainer                           │
│                  │                                               │
│ ┌──────────────┐ │ ┌──────────────────────────────────────────┐ │
│ │ 读卡器状态    │ │ │ Header: 患者姓名 + 患者信息               │ │
│ └──────────────┘ │ ├──────────────────────────────────────────┤ │
│ ┌──────────────┐ │ │ EditContent (Compact 模式)                │ │
│ │ 患者信息卡片  │ │ │ ┌────────────────────────────────────┐   │ │
│ └──────────────┘ │ │ │ MedicalCaseEditControl               │   │ │
│ ┌──────────────┐ │ │ │ - 诊断区 (现病史/舌诊/脉诊/中医诊断)  │   │ │
│ │ 待诊队列      │ │ │ │ - 处方区 (HerbListControl 4列)        │   │ │
│ │               │ │ │ │ - 工具条 (套验方/历史处方/清空)        │   │ │
│ │               │ │ │ └────────────────────────────────────┘   │ │
│ │               │ │ ├──────────────────────────────────────────┤ │
│ │               │ │ │ FooterContent                             │ │
│ │               │ │ │ [备注输入框] [暂存] [打印] [导出PDF] [完成]│ │
│ └──────────────┘ │ └──────────────────────────────────────────┘ │
└──────────────────┴──────────────────────────────────────────────┘
```

### 1.2 核心问题诊断

| 问题 | 严重性 | 影响 | 根因 |
|------|--------|------|------|
| **IsEnabled 作用域错误** | 🔴 严重 | 新建医案时诊断区完全不可编辑 | `IsEnabled="{Binding IsPrescriptionEnabled}"` 绑定在整个 EditControl 上 |
| **诊断区布局不符合诊疗逻辑** | 🟡 中等 | 医生需反复滚动查找字段 | 3 行平铺，未映射"望闻问切"顺序 |
| **无处方决策引导** | 🟡 中等 | 医生可能忘记标记 NeedsPrescription | 无"是否需要处方"决策点 |
| **无完成校验前置提示** | 🟡 中等 | 点击"完成看诊"时才发现问题 | BR-003 校验未前置到编辑区 |
| **患者信息分散** | 🟢 低 | 需视线移动才能看到患者信息 | Header 有患者姓名，但性别/年龄/医案编号分散 |
| **价格计算不实时** | 🟢 低 | 修改药材后总价不准确 | 需手动触发价格重算 |

---

## 二、看诊全流程优化方案

### 2.1 医生看诊完整流程映射

根据 PRD `clinical-workflow.md`，完整看诊流程为：

```
患者到达 → 选择患者 → 创建医案 (Active)
  → 填写诊断 (望闻问切)
  → 处方决策 (是否需要处方)
  → 开具处方 (验方导入/历史复制/手工输入)
  → 聚合保存
  → 打印预览 (可选)
  → 完成看诊 (BR-003 校验)
```

**UI 优化目标**：每个步骤在界面上有明确的视觉区域，医生按顺序操作无需跳转。

### 2.2 优化后的界面布局

```
┌─────────────────────────────────────────────────────────────────────────┐
│  MedicalCaseWorkspaceView (Clinical 模式)                                │
├──────────────┬──────────────────────────────────────────────────────────┤
│ 左侧 280px   │ 右侧主工作区 (MinWidth=900)                               │
│              │                                                          │
│ 📡 读卡器    │ ┌──────────────────────────────────────────────────────┐ │
│ [状态]       │ │ 👤 张三 | 男 | 45岁 | MC20260410001 | 🟢 进行中        │ │ ← 患者信息条
│              │ └──────────────────────────────────────────────────────┘ │
│ 👤 患者卡片  │                                                          │
│ [姓名/性别]  │ ┌──────────────────────────────────────────────────────┐ │
│ [年龄/电话]  │ │ 📋 四诊采集                              [展开/折叠]  │ │ ← 诊断区 Step 1
│              │ │ ┌──────────────────────────────────────────────────┐ │ │
│ 📋 待诊队列  │ │ 现病史 (问)                                         │ │ │
│              │ │ ┌────────────────────────────────────────────────┐ │ │ │
│ 1. 李四      │ │ │ 多行文本框，支持语音输入                        │ │ │ │
│ 2. 王五      │ │ └────────────────────────────────────────────────┘ │ │ │
│ 3. 赵六      │ │                                                     │ │ │
│              │ │ 舌诊 (望)                    脉诊 (切)               │ │ │
│ [刷新]       │ │ ┌──────────────────────┐  ┌──────────────────────┐ │ │ │
│              │ │ │ 文本框                │  │ 文本框                │ │ │ │
│              │ │ │ [常用舌象▼]          │  │ [常用脉象▼]          │ │ │ │
│              │ │ └──────────────────────┘  └──────────────────────┘ │ │ │
│              │ └──────────────────────────────────────────────────┘ │ │
│              │                                                          │
│              │ ┌──────────────────────────────────────────────────────┐ │
│              │ │ 🎯 中医辨证 (必填) *                     [展开/折叠]  │ │ ← 诊断区 Step 2
│              │ │ ┌──────────────────────────────────────────────────┐ │ │
│              │ │ │ 中医诊断                                          │ │ │
│              │ │ │ ┌──────────────────────────────────────────────┐ │ │ │
│              │ │ │ │ 文本框 (ValidatingTextBoxStyle)               │ │ │ │
│              │ │ │ │ [常用证型▼]                                   │ │ │ │
│              │ │ │ └──────────────────────────────────────────────┘ │ │ │
│              │ │ └──────────────────────────────────────────────────┘ │ │
│              │ └──────────────────────────────────────────────────────┘ │
│              │                                                          │
│              │ ┌──────────────────────────────────────────────────────┐ │
│              │ │ 💊 处方决策                              [展开/折叠]  │ │ ← 处方决策 Step 3
│              │ │ 是否需要开具处方？                                     │ │
│              │ │ ● 需要处方  ○ 不需要处方  ○ 稍后决定                   │ │
│              │ └──────────────────────────────────────────────────────┘ │
│              │                                                          │
│              │ ┌──────────────────────────────────────────────────────┐ │
│              │ │ 📝 处方开具                            [套验方][历史] │ │ ← 处方区 Step 4
│              │ │ ┌──────────────────────────────────────────────────┐ │ │
│              │ │ │ 药名      剂量    煎法      单价      小计    操作 │ │ │
│              │ │ │ ┌─────┐  ┌────┐  ┌──────┐  ┌──────┐  ┌────┐  ┌─┐│ │ │
│              │ │ │ │黄芪  │  │30g │  │常规  │  │0.12  │  │3.6 │  │×││ │ │
│              │ │ │ └─────┘  └────┘  └──────┘  └──────┘  └────┘  └─┘│ │ │
│              │ │ │ ┌─────┐  ┌────┐  ┌──────┐  ┌──────┐  ┌────┐  ┌─┐│ │ │
│              │ │ │ │当归  │  │15g │  │后下  │  │0.25  │  │3.75│  │×││ │ │
│              │ │ │ └─────┘  └────┘  └──────┘  └──────┘  └────┘  └─┘│ │ │
│              │ │ │                                                   │ │ │
│              │ │ │ [+ 添加药材]                                      │ │ │
│              │ │ └──────────────────────────────────────────────────┘ │ │
│              │ │                                                       │ │
│              │ │ ┌──────────────────────────────────────────────────┐ │ │
│              │ │ │ 剂数: [7] 剂   用法: [水煎服▼]   折扣: [1.0]     │ │ │
│              │ │ │                                                   │ │ │
│              │ │ │ 单剂价: ¥45.00        总价: ¥315.00              │ │ │
│              │ │ └──────────────────────────────────────────────────┘ │ │
│              │ └──────────────────────────────────────────────────────┘ │
│              │                                                          │
│              │ ┌──────────────────────────────────────────────────────┐ │
│              │ │ ✅ 完整性检查                                         │ │ ← 校验提示 Step 5
│              │ │ ✓ 中医诊断已填写   ✓ 处方需求已标记                    │ │
│              │ │ ✓ 处方药材 2 味    ✓ 帖数 7 剂                        │ │
│              │ │                                                       │ │
│              │ │ 可以完成看诊                                           │ │
│              │ └──────────────────────────────────────────────────────┘ │
│              │                                                          │
│              │ ┌──────────────────────────────────────────────────────┐ │
│              │ │ [暂存医案]          [打印处方笺]        [完成看诊]      │ │ ← 底部操作栏
│              │ └──────────────────────────────────────────────────────┘ │
└──────────────┴──────────────────────────────────────────────────────────┘
```

### 2.3 关键优化点详解

#### 优化点 1：修复 IsEnabled 作用域 Bug（P0 紧急）

**当前问题**：
```xaml
<!-- MedicalCaseWorkspaceView.xaml 第 131 行 -->
<controls:MedicalCaseEditControl
    IsEnabled="{Binding IsPrescriptionEnabled}"  <!-- ❌ 禁用整个控件，包括诊断区 -->
    ... />
```

**修复方案**：
```xaml
<!-- 方案：将 IsEnabled 移到仅处方区 -->
<controls:MedicalCaseEditControl
    IsDiagnosisEnabled="True"                    <!-- ✅ 诊断区始终可编辑 -->
    IsPrescriptionEnabled="{Binding IsPrescriptionEnabled}"  <!-- ✅ 仅处方区禁用 -->
    ... />
```

**代码修改**：
- `MedicalCaseEditControl.xaml.cs`：添加 `IsDiagnosisEnabled` DependencyProperty
- `MedicalCaseEditControl.xaml`：诊断区绑定 `IsDiagnosisEnabled`，处方区绑定 `IsPrescriptionEnabled`

---

#### 优化点 2：诊断区分组折叠（望闻问切映射）

**当前布局**（3 行平铺）：
```xaml
<!-- 现病史 (整行) -->
<!-- 舌诊 (左) | 脉诊 (右) -->
<!-- 中医诊断* (整行) -->
```

**优化后布局**（分组折叠）：

```xaml
<!-- 四诊采集区 (可折叠，默认展开) -->
<Expander Header="📋 四诊采集" IsExpanded="True">
    <StackPanel>
        <!-- 现病史 (问) -->
        <TextBlock Text="现病史 (问)" FontWeight="Medium"/>
        <TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay}"
                 Style="{DynamicResource EditableTextBoxStyle}"
                 TextWrapping="Wrap"
                 MinHeight="80"/>
        
        <!-- 舌诊 (望) + 脉诊 (切) -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="16"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- 舌诊 -->
            <StackPanel Grid.Column="0">
                <TextBlock Text="舌诊 (望)" FontWeight="Medium"/>
                <TextBox Text="{Binding Consultation.TongueDiagnosis, Mode=TwoWay}"
                         Style="{DynamicResource EditableTextBoxStyle}"/>
                <ComboBox ItemsSource="{Binding CommonTongueTerms}"
                          Style="{DynamicResource FilterComboBox}"/>
            </StackPanel>
            
            <!-- 脉诊 -->
            <StackPanel Grid.Column="2">
                <TextBlock Text="脉诊 (切)" FontWeight="Medium"/>
                <TextBox Text="{Binding Consultation.PulseDiagnosis, Mode=TwoWay}"
                         Style="{DynamicResource EditableTextBoxStyle}"/>
                <ComboBox ItemsSource="{Binding CommonPulseTerms}"
                          Style="{DynamicResource FilterComboBox}"/>
            </StackPanel>
        </Grid>
    </StackPanel>
</Expander>

<!-- 中医辨证区 (必填，默认展开) -->
<Expander Header="🎯 中医辨证 (必填) *" IsExpanded="True">
    <StackPanel>
        <TextBox Text="{Binding Consultation.TcmDiagnosis, Mode=TwoWay}"
                 Style="{DynamicResource ValidatingTextBoxStyle}"/>
        <TextBlock Text="{Binding ErrorsSource[TcmDiagnosis]}"
                   Style="{DynamicResource ValidationErrorMessageVisibleStyle}"/>
        <ComboBox ItemsSource="{Binding CommonSyndromeTerms}"
                  Style="{DynamicResource FilterComboBox}"/>
    </StackPanel>
</Expander>
```

**常用词数据源**：
```csharp
// MedicalCaseEditControl.xaml.cs
public static readonly DependencyProperty CommonTongueTermsProperty =
    DependencyProperty.Register(nameof(CommonTongueTerms), typeof(IEnumerable<string>),
        typeof(MedicalCaseEditControl), new PropertyMetadata(null));

public IEnumerable<string> CommonTongueTerms
{
    get => (IEnumerable<string>)GetValue(CommonTongueTermsProperty);
    set => SetValue(CommonTongueTermsProperty, value);
}

// 类似定义 CommonPulseTerms 和 CommonSyndromeTerms
```

**常用词配置**（从配置加载）：
```csharp
// 方案 1: 硬编码（快速实现）
public static class TcmCommonTerms
{
    public static string[] TongueDiagnoses = { 
        "淡红舌", "红舌", "暗红舌", "紫暗舌", "胖大舌", "瘦薄舌", 
        "老舌", "嫩舌", "裂纹舌", "齿痕舌" 
    };
    
    public static string[] PulseDiagnoses = { 
        "浮脉", "沉脉", "迟脉", "数脉", "滑脉", "涩脉", 
        "弦脉", "紧脉", "虚脉", "实脉", "弱脉", "细脉" 
    };
    
    public static string[] TcmSyndromes = { 
        "风寒束表证", "风热犯肺证", "暑湿感冒证",
        "脾胃虚弱证", "脾胃湿热证", "胃阴不足证",
        "肝郁气滞证", "肝胆湿热证", "肝阳上亢证",
        "心脾两虚证", "心肺气虚证", "痰热壅肺证",
        "肾阴亏虚证", "肾阳不足证", "肾精不足证"
    };
}

// 方案 2: 从数据库加载（长期方案）
// 通过 ISystemConfigApi.GetTcmTermsAsync() 获取
```

---

#### 优化点 3：处方决策引导

**当前问题**：无处方需求标记入口

**优化方案**：在处方区顶部添加决策栏

```xaml
<!-- 处方决策区 -->
<Border Background="{DynamicResource SecondaryRegionBrush}"
        CornerRadius="8"
        Padding="16,12"
        Margin="0,0,0,12">
    <StackPanel>
        <TextBlock Text="💊 是否需要开具处方？" FontWeight="Medium" Margin="0,0,0,8"/>
        <StackPanel Orientation="Horizontal">
            <RadioButton Content="需要处方"
                         IsChecked="{Binding NeedsPrescription, Converter={StaticResource BoolToNullableTrueConverter}}"
                         Margin="0,0,16,0"/>
            <RadioButton Content="不需要处方"
                         IsChecked="{Binding NeedsPrescription, Converter={StaticResource BoolToNullableFalseConverter}}"
                         Margin="0,0,16,0"/>
            <RadioButton Content="稍后决定"
                         IsChecked="{Binding NeedsPrescription, Converter={StaticResource NullableIsNullConverter}}"
                         Margin="0,0,0,0"/>
        </StackPanel>
    </StackPanel>
</Border>
```

**交互逻辑**：
- 选择"需要处方" → 展开药材编辑区
- 选择"不需要处方" → 折叠药材编辑区，显示"本医案不开具处方"
- 选择"稍后决定" → 保持折叠，完成看诊时提示 BR-003 校验失败

---

#### 优化点 4：处方区实时价格计算

**当前问题**：价格计算不实时

**优化方案**：在 `PrescriptionEditorViewModel` 中添加自动重算逻辑

```csharp
public class PrescriptionEditorViewModel : ObservableObject
{
    private PrescriptionItem _prescription;
    
    public PrescriptionItem Prescription
    {
        get => _prescription;
        set
        {
            if (SetProperty(ref _prescription, value))
            {
                // 监听药材集合变更
                if (value?.Items != null)
                {
                    ((INotifyCollectionChanged)value.Items).CollectionChanged += OnItemsChanged;
                }
            }
        }
    }
    
    private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculatePrices();
        
        // 监听每个药材项的属性变更
        if (e.NewItems != null)
        {
            foreach (PrescriptionItemDto item in e.NewItems)
            {
                ((INotifyPropertyChanged)item).PropertyChanged += OnItemPropertyChanged;
            }
        }
    }
    
    private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PrescriptionItemDto.Dosage) 
                              or nameof(PrescriptionItemDto.UnitPrice))
        {
            RecalculatePrices();
        }
    }
    
    private void RecalculatePrices()
    {
        // 单剂价 = SUM(剂量 × 单价)
        SingleDosePrice = Prescription.Items.Sum(x => x.Dosage * x.UnitPrice);
        
        // 总价 = 单剂价 × 剂数 × 折扣
        TotalPrice = SingleDosePrice * Prescription.DosageCount * Prescription.Discount;
        
        OnPropertyChanged(nameof(SingleDosePrice));
        OnPropertyChanged(nameof(TotalPrice));
    }
}
```

---

#### 优化点 5：完整性检查提示

**当前问题**：无完成前校验提示

**优化方案**：添加实时完整性检查组件

```csharp
public class PrescriptionCompletenessChecker : ObservableObject
{
    private readonly IMedicalCaseWorkspaceContext _context;
    
    public ObservableCollection<CompletenessItem> Items { get; } = new();
    
    public bool IsComplete => Items.All(x => x.IsPass);
    
    public void Check()
    {
        Items.Clear();
        
        // 检查中医诊断
        Items.Add(new CompletenessItem
        {
            Label = "中医诊断",
            IsPass = !string.IsNullOrWhiteSpace(_context.Consultation?.TcmDiagnosis),
            Message = string.IsNullOrWhiteSpace(_context.Consultation?.TcmDiagnosis) 
                ? "未填写" : "已填写"
        });
        
        // 检查处方需求标记
        Items.Add(new CompletenessItem
        {
            Label = "处方需求",
            IsPass = _context.NeedsPrescription.HasValue,
            Message = _context.NeedsPrescription switch
            {
                true => "需要处方",
                false => "不需要处方",
                null => "未决策"
            }
        });
        
        // 检查处方药材（当 NeedsPrescription=true 时）
        if (_context.NeedsPrescription == true)
        {
            Items.Add(new CompletenessItem
            {
                Label = "处方药材",
                IsPass = _context.Prescription?.Items.Count > 0,
                Message = _context.Prescription?.Items.Count > 0 
                    ? $"{_context.Prescription.Items.Count} 味" 
                    : "未添加"
            });
            
            Items.Add(new CompletenessItem
            {
                Label = "帖数",
                IsPass = _context.Prescription?.DosageCount > 0,
                Message = _context.Prescription?.DosageCount > 0 
                    ? $"{_context.Prescription.DosageCount} 剂" 
                    : "未填写"
            });
        }
        
        OnPropertyChanged(nameof(IsComplete));
    }
}

public class CompletenessItem
{
    public string Label { get; init; }
    public bool IsPass { get; init; }
    public string Message { get; init; }
}
```

**XAML 绑定**：
```xaml
<!-- 完整性检查区 -->
<Border Background="{Binding CompletenessChecker.IsComplete, 
                      Converter={StaticResource BoolToBackgroundConverter}}"
        CornerRadius="8"
        Padding="12,8"
        Margin="0,12,0,0">
    <StackPanel>
        <TextBlock Text="✅ 完整性检查" FontWeight="Medium" Margin="0,0,0,8"/>
        <ItemsControl ItemsSource="{Binding CompletenessChecker.Items}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Margin="0,2">
                        <TextBlock>
                            <Run Text="{Binding IsPass, Converter={StaticResource BoolToCheckMark}}"/>
                            <Run Text="{Binding Label}"/>
                            <Run Text=":"/>
                            <Run Text="{Binding Message}"/>
                        </TextBlock>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <TextBlock Margin="0,8,0,0" FontWeight="Medium">
            <Run Text="{Binding CompletenessChecker.IsComplete, 
                          Converter={StaticResource BoolToCompleteMessage}}"/>
        </TextBlock>
    </StackPanel>
</Border>
```

---

#### 优化点 6：底部操作栏优化

**当前问题**：按钮分散在 Footer，未按场景分组

**优化方案**：按 Clinical/Management 模式分组按钮

```xaml
<!-- Clinical 模式底部按钮 -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
            Visibility="{Binding State.IsClinicalMode, Converter={StaticResource BoolToVis}}">
    <Button Content="暂存医案"
            Command="{Binding Commands.SuspendCommand}"
            Style="{DynamicResource WarningButton}"
            Margin="0,0,8,0"/>
    
    <Button Content="打印处方笺"
            Command="{Binding Commands.PrintCommand}"
            Style="{DynamicResource SecondaryButton}"
            IsEnabled="{Binding State.CanPrint}"
            Margin="0,0,8,0"/>
    
    <Button Content="导出PDF"
            Command="{Binding Commands.ExportPdfCommand}"
            Style="{DynamicResource SecondaryButton}"
            IsEnabled="{Binding State.CanPrint}"
            Margin="0,0,8,0"/>
    
    <Button Content="完成看诊"
            Command="{Binding Commands.CompleteCommand}"
            Style="{DynamicResource SuccessButton}"
            IsEnabled="{Binding State.CanComplete}"/>
</StackPanel>

<!-- Management 模式底部按钮 -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
            Visibility="{Binding State.IsManagementMode, Converter={StaticResource BoolToVis}}">
    <Button Content="保存医案"
            Command="{Binding SaveChangesCommand}"
            Style="{DynamicResource SuccessButton}"
            Visibility="{Binding State.ShowSaveButton, Converter={StaticResource BoolToVis}}"
            Margin="0,0,8,0"/>
    
    <Button Content="取消编辑"
            Command="{Binding Commands.CancelEditCommand}"
            Style="{DynamicResource SecondaryButton}"
            Visibility="{Binding State.IsEditing, Converter={StaticResource BoolToVis}}"
            Margin="0,0,8,0"/>
    
    <Button Content="打印处方笺"
            Command="{Binding Commands.PrintCommand}"
            Style="{DynamicResource SecondaryButton}"
            IsEnabled="{Binding State.CanPrint}"
            Margin="0,0,8,0"/>
</StackPanel>
```

---

## 三、实施优先级与工作量

### 3.1 优先级矩阵

| 优化项 | 看诊效率提升 | 实现难度 | 优先级 | 预计工作量 | 依赖 |
|--------|--------------|----------|--------|------------|------|
| **修复 IsEnabled 作用域** | 🔴 高 | 🟢 低 | P0 | 1 小时 | 无 |
| **诊断区分组折叠** | 🟡 中 | 🟢 低 | P0 | 2 小时 | 无 |
| **处方决策引导** | 🔴 高 | 🟢 低 | P0 | 1 小时 | 无 |
| **实时价格计算** | 🔴 高 | 🟡 中 | P0 | 2 小时 | 无 |
| **完整性检查提示** | 🟡 中 | 🟡 中 | P1 | 3 小时 | 处方决策 |
| **底部操作栏优化** | 🟡 中 | 🟢 低 | P1 | 2 小时 | 无 |
| **常用词快捷选择** | 🟡 中 | 🟢 低 | P1 | 2 小时 | 诊断区分组 |
| **患者信息条统一** | 🟢 低 | 🟢 低 | P2 | 1 小时 | 无 |

### 3.2 实施阶段

#### 阶段 1：核心 Bug 修复 + 诊断区优化（P0，预计 6 小时）

1. ✅ 修复 IsEnabled 作用域 Bug
2. ✅ 诊断区分组折叠（Expander）
3. ✅ 添加常用词快捷选择
4. ✅ 处方决策引导
5. ✅ 实时价格计算

#### 阶段 2：校验提示 + 操作栏优化（P1，预计 7 小时）

6. 完整性检查提示
7. 底部操作栏优化
8. 验证框架补全

#### 阶段 3：体验优化（P2，预计 1 小时）

9. 患者信息条统一

---

## 四、技术实现细节

### 4.1 文件修改清单

| 文件 | 修改内容 | 预计行数变化 |
|------|----------|--------------|
| `MedicalCaseEditControl.xaml` | 诊断区分组、处方决策、完整性检查 | +150 行 |
| `MedicalCaseEditControl.xaml.cs` | 添加常用词 DP、IsDiagnosisEnabled DP | +80 行 |
| `MedicalCaseWorkspaceView.xaml` | 底部操作栏优化、IsEnabled 修复 | +30 行，-10 行 |
| `PrescriptionEditorViewModel.cs` | 实时价格计算逻辑 | +50 行 |
| `MedicalCaseCommandsViewModel.cs` | 完整性检查器 | +100 行 |
| `TcmCommonTerms.cs` (新文件) | 常用词配置 | +50 行 |

### 4.2 数据流优化

**当前数据流**（三模型并存）：
```
MedicalCaseMasterDetailViewModel
├── Consultation (ConsultationItem)
├── Prescription (PrescriptionItem)
└── CurrentDetail (MedicalCaseDetailModel)
```

**优化后数据流**（统一数据源）：
```
MedicalCaseWorkspaceViewModel
├── State (WorkspaceState)
├── ConsultationEditor.Consultation (ConsultationItem)
├── PrescriptionEditor.Prescription (PrescriptionItem)
└── CurrentDetail (MedicalCaseDetailModel)
    └── 通过 Mapper 同步到子 VM
```

**关键改进**：
- 子 VM 通过 `IMedicalCaseWorkspaceContext` 接口访问父 VM 数据
- 避免直接操作 `CurrentDetail` 的子属性
- 保存时通过 `MedicalCaseCommandsViewModel` 聚合数据

---

## 五、测试策略

### 5.1 单元测试

| 测试项 | 测试内容 | 验证方法 |
|--------|----------|----------|
| IsEnabled 作用域 | 诊断区始终可编辑，处方区根据 IsPrescriptionEnabled 禁用 | 模拟新建医案场景 |
| 价格计算 | 药材变更/剂数变更/折扣变更时价格正确 | 断言 SingleDosePrice/TotalPrice |
| 完整性检查 | BR-003 校验项正确显示通过/失败 | 断言 IsComplete 属性 |
| 常用词加载 | 常用词列表正确加载 | 断言集合数量 |

### 5.2 UI 测试

| 测试项 | 测试方法 |
|--------|----------|
| 布局验证 | 1920x1080 / 1366x768 分辨率下无截断 |
| Tab 导航 | TabIndex 顺序：现病史→舌诊→脉诊→中医诊断→剂数→用法 |
| 折叠面板 | 展开/折叠状态正确保存 |
| 决策引导 | 选择"需要/不需要/稍后决定"时 UI 正确响应 |

### 5.3 集成测试

| 测试项 | 测试方法 |
|--------|----------|
| 完整看诊流程 | 患者选择→诊断→处方决策→开具处方→保存→完成 |
| BR-003 校验 | 缺失必填项时完成看诊失败，完整性检查显示失败项 |
| BR-002 离开决策 | 有未保存变更时弹窗正确 |

---

## 六、风险评估

### 6.1 技术风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Expander 折叠导致布局错乱 | 🟡 中 | 使用 Grid 固定行高，避免 Auto |
| 实时价格计算性能问题 | 🟢 低 | 药材列表通常<30 味，计算量小 |
| 常用词数据源加载失败 | 🟡 中 | 提供硬编码默认值，异步加载失败时降级 |

### 6.2 用户体验风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 折叠面板增加点击次数 | 🟡 中 | 默认展开必填区，可选区折叠 |
| 决策引导增加操作步骤 | 🟡 中 | 提供默认选项（需要处方） |

---

## 七、附录

### 附录 A: 中医常用术语参考

#### 舌诊常用词

| 分类 | 术语 |
|------|------|
| 舌色 | 淡红舌、红舌、暗红舌、紫暗舌、青紫舌 |
| 舌形 | 胖大舌、瘦薄舌、老舌、嫩舌、裂纹舌、齿痕舌 |
| 舌苔 | 薄白苔、白厚苔、黄苔、黄腻苔、灰黑苔、无苔 |

#### 脉象常用词

| 分类 | 术语 |
|------|------|
| 浮沉 | 浮脉、沉脉、伏脉 |
| 迟数 | 迟脉、数脉、疾脉 |
| 虚实 | 虚脉、实脉、弱脉、细脉 |
| 滑涩 | 滑脉、涩脉 |
| 弦紧 | 弦脉、紧脉、革脉 |

#### 常见证型

| 系统 | 证型 |
|------|------|
| 外感 | 风寒束表证、风热犯肺证、暑湿感冒证 |
| 脾胃 | 脾胃虚弱证、脾胃湿热证、胃阴不足证 |
| 肝胆 | 肝郁气滞证、肝胆湿热证、肝阳上亢证 |
| 心肺 | 心脾两虚证、心肺气虚证、痰热壅肺证 |
| 肾系 | 肾阴亏虚证、肾阳不足证、肾精不足证 |

### 附录 B: 处方用法选项

| 用法 | 说明 |
|------|------|
| 水煎服 | 常规水煎，每日 1 剂，分 2-3 次服用 |
| 水煎频服 | 少量多次频服，适用于呕吐/咽喉疾病 |
| 开水泡服 | 沸水冲泡，代茶饮 |
| 研末冲服 | 研成细末，开水冲服 |
| 外用 | 煎汤外洗/熏蒸/湿敷 |
| 打粉装胶囊 | 打粉后装入胶囊服用 |

---

*文档版本: v1.0 | 创建日期: 2026-04-10 | 状态: 待评审*
