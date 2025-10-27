# Prescriptions模块View合并建议清单

**任务编号**：Task 1.3 (#1679)
**分析日期**：2025-10-27
**前置任务**：Task 1.2（功能交集分析表）
**目标**：提供可执行的合并建议清单，包含详细步骤、影响评估和风险控制

---

## 🎯 推荐方案：方案C - 仅合并 PrescriptionView + PrescriptionEditorDialog

**方案代号**：Prescription-Unification-Phase1
**优先级**：🔴 P0（强烈推荐）
**预计工作量**：2-3天
**风险等级**：🟢 低
**预期收益**：减少171行代码（33%），统一处方编辑体验

---

## 📋 1. 合并步骤详细清单

### Step 1: 准备阶段（0.5天）

#### 1.1 创建Epic分支
```bash
git checkout master
git pull origin master
git checkout -b epic/issue-1676-phase1-view-merge
```

#### 1.2 备份现有文件
```bash
# 创建备份目录
mkdir -p backup/phase1-view-merge

# 备份View文件
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml \
   backup/phase1-view-merge/
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml.cs \
   backup/phase1-view-merge/
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml \
   backup/phase1-view-merge/
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml.cs \
   backup/phase1-view-merge/

# 备份ViewModel文件
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs \
   backup/phase1-view-merge/
cp src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs \
   backup/phase1-view-merge/
```

#### 1.3 代码冻结通知
- [ ] 通知团队：Prescriptions模块代码冻结（如有团队成员）
- [ ] 创建Issue留言：开始执行合并任务

---

### Step 2: 设计统一View布局（0.5天）

#### 2.1 确定统一View的功能清单

**必须保留的功能**（来自两个View的并集）：

| 功能 | 来源 | 优先级 |
|-----|------|--------|
| 处方编号显示 | 两者共有 | P0 |
| 患者信息显示 | 两者共有 | P0 |
| 诊断输入 | 两者共有 | P0 |
| 剂数输入 | 两者共有 | P0 |
| 用法选择 | View独有 | P0 |
| 医嘱输入 | View独有 | P0 |
| **8列快速输入模式** | View独有 | P0 |
| **列表编辑模式** | Editor独有 | P1 |
| 布局切换按钮 | 新增 | P1 |
| 状态管理（5种状态） | Editor独有 | P1 |
| 查看/编辑模式切换 | Editor独有 | P1 |
| 历史处方下拉框 | View独有 | P0 |
| 添加药材 | 两者共有 | P0 |
| 导入验方 | 两者共有 | P0 |
| 清空处方 | View独有 | P0 |
| 编辑/删除药材 | Editor独有 | P1 |
| 保存草稿 | View独有 | P0 |
| 保存处方 | 两者共有 | P0 |
| 预览处方 | Editor独有 | P1 |
| 关闭/取消 | 两者共有 | P0 |

#### 2.2 设计XAML布局结构

**文件名**：`PrescriptionUnifiedView.xaml`

**布局结构**（5行）：
```
Row 0: 标题栏（患者信息 + 处方编号 + 状态）
Row 1: 基本信息区（诊断、剂数、用法、价格、布局切换按钮）
Row 2: 药材列表区（动态切换：8列模式 ↔ 列表模式）
Row 3: 医嘱区
Row 4: 底部操作区（保存草稿、保存、预览、关闭）
```

#### 2.3 设计布局切换逻辑

**状态属性**：
```csharp
public enum PrescriptionLayoutMode
{
    QuickEntry,  // 8列快速输入
    DetailedList // 列表详细编辑
}

public PrescriptionLayoutMode CurrentLayoutMode { get; set; }
```

**切换按钮**：
```xaml
<ToggleButton IsChecked="{Binding IsDetailedListMode}"
              ToolTip="切换到列表模式/8列模式">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="{Binding LayoutModeIcon}" FontSize="16" />
        <TextBlock Text="{Binding LayoutModeText}" Margin="5,0,0,0" />
    </StackPanel>
</ToggleButton>
```

---

### Step 3: 实现统一View的XAML（1天）

#### 3.1 创建新View文件
```bash
# 创建XAML文件
touch src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionUnifiedView.xaml
touch src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionUnifiedView.xaml.cs
```

#### 3.2 实现XAML布局

**关键代码段**：

##### 标题栏（Row 0）
```xaml
<Border Grid.Row="0" Background="#34495E" Padding="15,10">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <!-- 左：标题和患者信息 -->
        <StackPanel Grid.Column="0" Orientation="Horizontal">
            <TextBlock Text="处方编辑" FontSize="20" FontWeight="Bold" Foreground="White" />
            <TextBlock Text="{Binding PatientInfo}" Margin="20,0,0,0" Foreground="#BDC3C7" />
        </StackPanel>

        <!-- 中：处方编号 -->
        <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Center">
            <TextBlock Text="处方编号：" Foreground="#BDC3C7" />
            <TextBlock Text="{Binding PrescriptionNumber}" Foreground="#3498DB" FontWeight="SemiBold" />
        </StackPanel>

        <!-- 右：状态下拉框（可选显示） -->
        <ComboBox Grid.Column="2" SelectedValue="{Binding Status}"
                  Visibility="{Binding ShowStatusSelector, Converter={StaticResource BooleanToVisibilityConverter}}"
                  MinWidth="120">
            <ComboBoxItem Content="草稿" Tag="0" />
            <ComboBoxItem Content="已确认" Tag="1" />
            <ComboBoxItem Content="已发药" Tag="2" />
            <ComboBoxItem Content="已完成" Tag="3" />
            <ComboBoxItem Content="已取消" Tag="4" />
        </ComboBox>
    </Grid>
</Border>
```

##### 基本信息区（Row 1）
```xaml
<Border Grid.Row="1" Background="#F8F9FA" Padding="15">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <!-- 诊断 -->
        <StackPanel Grid.Column="0" Margin="0,0,10,0">
            <TextBlock Text="诊断" FontWeight="Bold" />
            <TextBox Text="{Binding Diagnosis}" Height="60" TextWrapping="Wrap" />
        </StackPanel>

        <!-- 剂数 -->
        <StackPanel Grid.Column="1" Margin="10,0">
            <TextBlock Text="剂数" FontWeight="Bold" />
            <TextBox Text="{Binding DosageCount}" />
        </StackPanel>

        <!-- 用法 -->
        <StackPanel Grid.Column="2" Margin="10,0">
            <TextBlock Text="用法" FontWeight="Bold" />
            <ComboBox IsEditable="True" Text="{Binding Usage}">
                <ComboBoxItem>水煎服，日一剂，分早晚服</ComboBoxItem>
                <ComboBoxItem>水煎服，日一剂，分三次服</ComboBoxItem>
            </ComboBox>
        </StackPanel>

        <!-- 价格 -->
        <StackPanel Grid.Column="3" Margin="10,0">
            <TextBlock Text="价格信息" FontWeight="Bold" />
            <TextBlock>
                <Run Text="总价：" />
                <Run Text="{Binding TotalPrice, StringFormat=C}" FontWeight="Bold" Foreground="#C0392B" />
            </TextBlock>
        </StackPanel>

        <!-- 布局切换按钮 -->
        <StackPanel Grid.Column="4" Margin="10,0,0,0" VerticalAlignment="Center">
            <TextBlock Text="布局模式" FontSize="10" Foreground="Gray" Margin="0,0,0,2" />
            <ToggleButton IsChecked="{Binding IsDetailedListMode}"
                          ToolTip="切换输入模式"
                          Height="32">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{Binding LayoutModeIcon}" FontSize="14" Margin="0,0,5,0" />
                    <TextBlock Text="{Binding LayoutModeText}" FontSize="12" />
                </StackPanel>
            </ToggleButton>
        </StackPanel>
    </Grid>
</Border>
```

##### 药材列表区（Row 2，动态切换）
```xaml
<Border Grid.Row="2" Background="White">
    <Grid>
        <!-- 8列快速输入模式 -->
        <DataGrid ItemsSource="{Binding ItemRows}"
                  Visibility="{Binding IsDetailedListMode, Converter={StaticResource InverseBooleanToVisibilityConverter}}"
                  AutoGenerateColumns="False">
            <DataGrid.Columns>
                <!-- 药材1, 用量1, 药材2, 用量2, 药材3, 用量3, 药材4, 用量4 -->
                <!-- 完整实现参考PrescriptionView.xaml -->
            </DataGrid.Columns>
        </DataGrid>

        <!-- 列表详细编辑模式 -->
        <DataGrid ItemsSource="{Binding PrescriptionItems}"
                  Visibility="{Binding IsDetailedListMode, Converter={StaticResource BooleanToVisibilityConverter}}"
                  AutoGenerateColumns="False">
            <DataGrid.Columns>
                <!-- 药材名称、规格、单位、数量、单价、金额、用法、操作 -->
                <!-- 完整实现参考PrescriptionEditorDialog.xaml -->
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Border>
```

##### 医嘱区（Row 3）
```xaml
<Border Grid.Row="3" Background="#F8F9FA" Padding="15">
    <StackPanel>
        <TextBlock Text="医嘱" FontWeight="Bold" />
        <TextBox Text="{Binding Advice}" Height="60" TextWrapping="Wrap" />
    </StackPanel>
</Border>
```

##### 底部操作区（Row 4）
```xaml
<Border Grid.Row="4" Background="#2C3E50" Padding="15">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="保存草稿" Command="{Binding SaveDraftCommand}"
                Margin="0,0,10,0" />
        <Button Content="保存处方" Command="{Binding SavePrescriptionCommand}"
                Margin="0,0,10,0" />
        <Button Content="预览" Command="{Binding PreviewCommand}"
                Margin="0,0,10,0"
                Visibility="{Binding ShowPreviewButton, Converter={StaticResource BooleanToVisibilityConverter}}" />
        <Button Content="关闭" Command="{Binding CloseCommand}" />
    </StackPanel>
</Border>
```

#### 3.3 实现Code-Behind

**PrescriptionUnifiedView.xaml.cs**：
```csharp
public partial class PrescriptionUnifiedView : UserControl
{
    public PrescriptionUnifiedView()
    {
        InitializeComponent();
    }

    // 保留PrescriptionView的事件处理器
    private void HerbComboBox_Loaded(object sender, RoutedEventArgs e) { /* 拼音码过滤 */ }
    private void QuantityTextBox_PreviewKeyDown(object sender, KeyEventArgs e) { /* 焦点跳转 */ }
}
```

---

### Step 4: 合并ViewModel（1天）

#### 4.1 创建统一ViewModel

**文件名**：`PrescriptionUnifiedViewModel.cs`

**合并策略**：
- 基础：以`PrescriptionViewModel`为主体
- 增强：增加`PrescriptionEditorDialogViewModel`的状态管理和查看模式
- 新增：布局切换逻辑

**核心属性清单**：

```csharp
public class PrescriptionUnifiedViewModel : BindableBase
{
    // ===== 基础属性（两者共有） =====
    public string PatientInfo { get; set; }
    public string PrescriptionNumber { get; set; }
    public string Diagnosis { get; set; }
    public int DosageCount { get; set; }
    public string Usage { get; set; }
    public decimal TotalPrice { get; set; }

    // ===== PrescriptionView独有属性 =====
    public string Advice { get; set; }
    public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; } // 8列模式
    public ObservableCollection<RecentPrescriptionDto> RecentPrescriptions { get; set; }
    public RecentPrescriptionDto SelectedRecentPrescription { get; set; }

    // ===== PrescriptionEditorDialog独有属性 =====
    public PrescriptionStatus Status { get; set; } // 0=草稿, 1=已确认, 2=已发药, 3=已完成, 4=已取消
    public bool IsViewMode { get; set; } // true=查看模式, false=编辑模式
    public ObservableCollection<PrescriptionItemDto> PrescriptionItems { get; set; } // 列表模式

    // ===== 新增属性（布局切换） =====
    public PrescriptionLayoutMode CurrentLayoutMode { get; set; }
    public bool IsDetailedListMode => CurrentLayoutMode == PrescriptionLayoutMode.DetailedList;
    public string LayoutModeIcon => IsDetailedListMode ? "📋" : "⚡";
    public string LayoutModeText => IsDetailedListMode ? "列表模式" : "快速模式";

    // ===== 新增属性（条件显示） =====
    public bool ShowStatusSelector { get; set; } // 是否显示状态选择器
    public bool ShowPreviewButton { get; set; } // 是否显示预览按钮

    // ===== 命令（合并后） =====
    public DelegateCommand AddHerbCommand { get; }
    public DelegateCommand ImportFormulaCommand { get; } // 统一命名
    public DelegateCommand ClearAllCommand { get; }
    public DelegateCommand<PrescriptionItemDto> EditHerbCommand { get; }
    public DelegateCommand<PrescriptionItemDto> RemoveHerbCommand { get; }
    public DelegateCommand SaveDraftCommand { get; }
    public DelegateCommand SavePrescriptionCommand { get; }
    public DelegateCommand PreviewCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DelegateCommand ToggleLayoutModeCommand { get; } // 新增
}
```

#### 4.2 实现布局切换逻辑

```csharp
private void OnToggleLayoutMode()
{
    if (CurrentLayoutMode == PrescriptionLayoutMode.QuickEntry)
    {
        // 切换到列表模式：转换ItemRows → PrescriptionItems
        SyncQuickEntryToList();
        CurrentLayoutMode = PrescriptionLayoutMode.DetailedList;
    }
    else
    {
        // 切换到8列模式：转换PrescriptionItems → ItemRows
        SyncListToQuickEntry();
        CurrentLayoutMode = PrescriptionLayoutMode.QuickEntry;
    }

    RaisePropertyChanged(nameof(IsDetailedListMode));
    RaisePropertyChanged(nameof(LayoutModeIcon));
    RaisePropertyChanged(nameof(LayoutModeText));
}

private void SyncQuickEntryToList()
{
    PrescriptionItems.Clear();
    foreach (var row in ItemRows)
    {
        if (!string.IsNullOrEmpty(row.Item1.HerbName))
            PrescriptionItems.Add(ConvertToItemDto(row.Item1));
        if (!string.IsNullOrEmpty(row.Item2.HerbName))
            PrescriptionItems.Add(ConvertToItemDto(row.Item2));
        if (!string.IsNullOrEmpty(row.Item3.HerbName))
            PrescriptionItems.Add(ConvertToItemDto(row.Item3));
        if (!string.IsNullOrEmpty(row.Item4.HerbName))
            PrescriptionItems.Add(ConvertToItemDto(row.Item4));
    }
}

private void SyncListToQuickEntry()
{
    ItemRows.Clear();
    var items = PrescriptionItems.ToList();
    for (int i = 0; i < items.Count; i += 4)
    {
        var row = new PrescriptionItemRow
        {
            Item1 = i < items.Count ? ConvertToQuickItem(items[i]) : new QuickEntryItem(),
            Item2 = i + 1 < items.Count ? ConvertToQuickItem(items[i + 1]) : new QuickEntryItem(),
            Item3 = i + 2 < items.Count ? ConvertToQuickItem(items[i + 2]) : new QuickEntryItem(),
            Item4 = i + 3 < items.Count ? ConvertToQuickItem(items[i + 3]) : new QuickEntryItem()
        };
        ItemRows.Add(row);
    }
}
```

#### 4.3 实现模式切换逻辑

```csharp
public void SetViewMode(bool isViewMode)
{
    IsViewMode = isViewMode;
    ShowStatusSelector = !isViewMode; // 查看模式不显示状态选择器
    ShowPreviewButton = !isViewMode; // 查看模式不显示预览按钮

    // 禁用所有编辑命令
    SaveDraftCommand.RaiseCanExecuteChanged();
    SavePrescriptionCommand.RaiseCanExecuteChanged();
    AddHerbCommand.RaiseCanExecuteChanged();
    // ...
}
```

#### 4.4 重构保存逻辑

```csharp
private async Task OnSavePrescriptionAsync()
{
    // 根据当前布局模式同步数据
    if (CurrentLayoutMode == PrescriptionLayoutMode.QuickEntry)
        SyncQuickEntryToList(); // 确保PrescriptionItems是最新的

    // 验证
    if (!ValidatePrescription())
        return;

    // 保存
    var dto = new PrescriptionDto
    {
        PrescriptionNumber = PrescriptionNumber,
        Diagnosis = Diagnosis,
        DosageCount = DosageCount,
        Usage = Usage,
        Advice = Advice,
        Status = Status,
        Items = PrescriptionItems.ToList()
    };

    await _prescriptionRepository.SaveAsync(dto);

    // 关闭或刷新
    _dialogService.Close();
}
```

---

### Step 5: 更新导航逻辑（0.5天）

#### 5.1 更新PrescriptionsMainViewModel

**变更**：
```csharp
// 旧代码
private void OnCreateNewPrescription()
{
    _regionManager.RequestNavigate("PrescriptionRegion", nameof(PrescriptionView));
}

// 新代码
private void OnCreateNewPrescription()
{
    _regionManager.RequestNavigate("PrescriptionRegion", nameof(PrescriptionUnifiedView));
}
```

#### 5.2 更新PrescriptionManagementViewModel

**变更**：
```csharp
// 旧代码
private void OnEditPrescription(PrescriptionDto prescription)
{
    _dialogService.ShowDialog(nameof(PrescriptionEditorDialog),
        new DialogParameters { { "Prescription", prescription } });
}

// 新代码
private void OnEditPrescription(PrescriptionDto prescription)
{
    var parameters = new NavigationParameters
    {
        { "Prescription", prescription },
        { "Mode", "Edit" },
        { "LayoutMode", "DetailedList" } // 从管理界面进入默认用列表模式
    };
    _regionManager.RequestNavigate("PrescriptionRegion", nameof(PrescriptionUnifiedView), parameters);
}
```

#### 5.3 更新PrescriptionUnifiedViewModel的导航参数处理

```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    // 获取参数
    var prescription = navigationContext.Parameters.GetValue<PrescriptionDto>("Prescription");
    var mode = navigationContext.Parameters.GetValue<string>("Mode");
    var layoutMode = navigationContext.Parameters.GetValue<string>("LayoutMode");

    // 设置模式
    if (mode == "View")
        SetViewMode(true);
    else if (mode == "Edit")
        SetViewMode(false);

    // 设置布局
    if (layoutMode == "DetailedList")
        CurrentLayoutMode = PrescriptionLayoutMode.DetailedList;
    else
        CurrentLayoutMode = PrescriptionLayoutMode.QuickEntry;

    // 加载数据
    if (prescription != null)
        LoadPrescription(prescription);
}
```

---

### Step 6: 删除旧View和ViewModel（0.25天）

#### 6.1 删除文件

```bash
# 删除View文件
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml.cs
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml.cs

# 删除ViewModel文件
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs
```

#### 6.2 更新Prism注册

**PrescriptionsModule.cs**：
```csharp
// 旧代码（删除）
containerRegistry.RegisterForNavigation<PrescriptionView, PrescriptionViewModel>();
containerRegistry.RegisterDialog<PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();

// 新代码
containerRegistry.RegisterForNavigation<PrescriptionUnifiedView, PrescriptionUnifiedViewModel>();
```

---

### Step 7: 编译验证（0.25天）

#### 7.1 编译检查

```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

**预期结果**：0 errors, 0 warnings

**可能的编译错误**：
- ❌ 缺少using引用 → 补充命名空间
- ❌ 命令绑定错误 → 检查Command名称
- ❌ 导航名称不匹配 → 检查View注册

#### 7.2 修复编译错误

按编译器提示逐个修复，确保0 errors。

---

### Step 8: 运行时验证（0.5天）

#### 8.1 启动应用

```bash
# 启动Server端
cd src/Server/Services/LYBT.WebAPI
dotnet run

# 启动Desktop端
cd src/Client/Desktop/Shell/LYBT.Desktop.Shell
dotnet run
```

#### 8.2 功能验证清单

| 场景 | 测试步骤 | 预期结果 | 实际结果 |
|-----|---------|---------|---------|
| **场景1：诊疗中快速开方** | 1. 创建医案<br>2. 进入处方模块<br>3. 8列快速录入药材<br>4. 保存处方 | ✅ 流畅开方，数据保存成功 | [ ] |
| **场景2：布局切换** | 1. 8列模式录入3味药材<br>2. 点击切换按钮<br>3. 查看列表模式 | ✅ 数据正确转换，3味药材显示完整 | [ ] |
| **场景3：列表模式编辑** | 1. 切换到列表模式<br>2. 添加新药材<br>3. 编辑规格单价<br>4. 保存 | ✅ 编辑成功，价格计算正确 | [ ] |
| **场景4：历史处方编辑** | 1. 进入历史管理<br>2. 点击编辑按钮<br>3. 查看处方详情 | ✅ 列表模式打开，数据完整 | [ ] |
| **场景5：状态管理** | 1. 打开已保存处方<br>2. 修改状态为"已发药"<br>3. 保存 | ✅ 状态更新成功 | [ ] |
| **场景6：查看模式** | 1. 以查看模式打开处方<br>2. 尝试编辑 | ✅ 所有编辑控件禁用 | [ ] |
| **场景7：医嘱保存** | 1. 录入医嘱<br>2. 保存处方<br>3. 重新打开 | ✅ 医嘱正确保存和显示 | [ ] |
| **场景8：历史复制** | 1. 打开历史处方下拉框<br>2. 选择一个历史处方<br>3. 查看药材列表 | ✅ 药材自动填充到8列 | [ ] |

#### 8.3 验证通过标准

- ✅ 所有8个场景测试通过
- ✅ 无运行时异常
- ✅ 数据保存和加载正确
- ✅ 布局切换流畅无卡顿

---

### Step 9: 提交和推送（0.25天）

#### 9.1 提交代码

```bash
git add .
git commit -m "feat(prescriptions): 合并PrescriptionView和PrescriptionEditorDialog

Epic #1676 Phase 1 完成

- 创建PrescriptionUnifiedView统一处方编辑界面
- 合并PrescriptionViewModel和PrescriptionEditorDialogViewModel
- 支持8列快速输入模式 ↔ 列表详细编辑模式切换
- 保留所有核心功能：
  * 8列横向快速录入（拼音码过滤+焦点跳转）
  * 列表完整编辑（规格/单价/金额显示）
  * 历史处方快速复制
  * 状态管理（草稿→已确认→已发药→已完成）
  * 查看/编辑模式切换
  * 医嘱字段保留
  * 预览功能保留
- 删除旧View: PrescriptionView, PrescriptionEditorDialog
- 更新导航逻辑
- 减少代码171行（33%）

验证：
- 编译通过（0 errors, 0 warnings）
- 运行时验证通过（8个场景全部测试通过）
- 用户操作流程无破坏性变化

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

#### 9.2 推送到远程

```bash
git push origin epic/issue-1676-phase1-view-merge
```

#### 9.3 创建Pull Request

```bash
gh pr create --title "Epic #1676 Phase 1: 合并PrescriptionView和PrescriptionEditorDialog" \
  --body "## 📋 变更概述

合并`PrescriptionView`和`PrescriptionEditorDialog`为统一的`PrescriptionUnifiedView`，减少代码冗余33%。

## 🎯 实现内容

- ✅ 创建`PrescriptionUnifiedView`统一界面
- ✅ 支持布局切换（8列快速模式 ↔ 列表详细模式）
- ✅ 保留所有核心功能（详见commit message）
- ✅ 删除旧View和ViewModel
- ✅ 更新导航逻辑

## ✅ 验收标准

- [x] 编译通过（0 errors, 0 warnings）
- [x] 8个功能场景运行时验证通过
- [x] 用户操作流程无破坏性变化
- [x] 代码减少171行（33%）

## 📊 影响分析

- 技术风险：🟢 低
- 用户体验影响：🟢 最小
- 工作量：2-3天（实际完成）

## 🔗 相关Issue

- Epic #1676: Desktop层架构重构与代码膨胀治理
- Task 1.1-1.4: Phase 1代码膨胀分析完成

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## 📄 2. 代码变更范围说明

### 2.1 新增文件（2个）

| 文件路径 | 文件类型 | 行数 | 说明 |
|---------|---------|------|------|
| `LYBT.Desktop.Prescriptions/Views/PrescriptionUnifiedView.xaml` | XAML | ~400行 | 统一处方编辑View |
| `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionUnifiedViewModel.cs` | C# | ~600行 | 统一处方ViewModel |

**总计新增**：~1000行

### 2.2 删除文件（4个）

| 文件路径 | 文件类型 | 行数 | 说明 |
|---------|---------|------|------|
| `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml` | XAML | 355行 | 旧快速开方View |
| `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml.cs` | C# | ~50行 | Code-behind |
| `LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml` | XAML | 166行 | 旧编辑对话框 |
| `LYBT.Desktop.Prescriptions/Views/PrescriptionEditorDialog.xaml.cs` | C# | ~20行 | Code-behind |
| `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs` | C# | ~500行 | 旧ViewModel |
| `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs` | C# | ~400行 | 旧ViewModel |

**总计删除**：~1491行

### 2.3 修改文件（3个）

| 文件路径 | 变更类型 | 变更行数 | 说明 |
|---------|---------|---------|------|
| `LYBT.Desktop.Prescriptions/PrescriptionsModule.cs` | 修改 | ~5行 | 更新Prism注册 |
| `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionsMainViewModel.cs` | 修改 | ~3行 | 更新导航目标 |
| `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionManagementViewModel.cs` | 修改 | ~10行 | 更新编辑逻辑 |

**总计修改**：~18行

### 2.4 净代码变化

```
新增：~1000行
删除：~1491行
修改：~18行
------------------
净减少：~491行（实际减少约33%）
```

**注**：最终行数以实际实现为准，此处为估算值。

---

## ⚠️ 3. 风险缓解措施

### 风险1：布局切换时数据丢失

**风险等级**：🟡 中
**影响范围**：用户录入的药材数据可能在切换布局时丢失

**缓解措施**：
1. ✅ 实现双向同步逻辑（`SyncQuickEntryToList` + `SyncListToQuickEntry`）
2. ✅ 在切换前自动保存当前模式数据
3. ✅ 增加单元测试验证数据同步
4. ✅ 运行时验证场景2测试

**验证方法**：
```csharp
[Test]
public void ToggleLayoutMode_ShouldPreserveData()
{
    // Arrange
    var viewModel = new PrescriptionUnifiedViewModel();
    viewModel.ItemRows.Add(new PrescriptionItemRow
    {
        Item1 = new QuickEntryItem { HerbName = "当归", Quantity = 10 }
    });

    // Act
    viewModel.OnToggleLayoutMode();

    // Assert
    Assert.AreEqual(1, viewModel.PrescriptionItems.Count);
    Assert.AreEqual("当归", viewModel.PrescriptionItems[0].HerbName);
    Assert.AreEqual(10, viewModel.PrescriptionItems[0].Quantity);
}
```

---

### 风险2：导航参数不匹配

**风险等级**：🟡 中
**影响范围**：从Management编辑处方时可能无法正确传递参数

**缓解措施**：
1. ✅ 明确定义NavigationParameters契约
2. ✅ 增加参数验证和默认值
3. ✅ 运行时验证场景4测试

**验证代码**：
```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    // 安全获取参数，提供默认值
    var prescription = navigationContext.Parameters.GetValue<PrescriptionDto>("Prescription");
    var mode = navigationContext.Parameters.GetValue<string>("Mode") ?? "Edit";
    var layoutMode = navigationContext.Parameters.GetValue<string>("LayoutMode") ?? "QuickEntry";

    // 验证prescription
    if (prescription == null)
    {
        _logger.LogWarning("Prescription参数为null，创建新处方");
        InitializeNewPrescription();
    }
    else
    {
        LoadPrescription(prescription);
    }
}
```

---

### 风险3：性能问题（大量药材切换）

**风险等级**：🟢 低
**影响范围**：处方包含>30味药材时，布局切换可能卡顿

**缓解措施**：
1. ✅ 使用异步转换（`async/await`）
2. ✅ 增加加载提示（ProgressBar）
3. ✅ 限制单个处方最大药材数量（如50味）

**优化代码**：
```csharp
private async Task OnToggleLayoutModeAsync()
{
    IsLoading = true;
    LoadingMessage = "正在切换布局...";

    await Task.Run(() =>
    {
        if (CurrentLayoutMode == PrescriptionLayoutMode.QuickEntry)
            SyncQuickEntryToList();
        else
            SyncListToQuickEntry();
    });

    CurrentLayoutMode = CurrentLayoutMode == PrescriptionLayoutMode.QuickEntry
        ? PrescriptionLayoutMode.DetailedList
        : PrescriptionLayoutMode.QuickEntry;

    IsLoading = false;
}
```

---

### 风险4：用户习惯改变

**风险等级**：🟡 中
**影响范围**：用户习惯对话框编辑，改为全屏页面需适应

**缓解措施**：
1. ✅ 提供用户手册或操作指引
2. ✅ 保留快捷键支持（如Esc关闭）
3. ✅ 增加"首次使用提示"
4. ⚠️ 可选：提供配置项让用户选择默认布局

**首次使用提示**：
```csharp
private void ShowFirstTimeGuide()
{
    if (!_settingsService.GetSetting<bool>("PrescriptionUnifiedView.GuideShown"))
    {
        _dialogService.ShowInfoAsync("新功能提示",
            "处方编辑界面已优化！\n\n" +
            "• 点击右上角切换按钮可在"快速模式"和"列表模式"间切换\n" +
            "• 快速模式适合诊疗中快速开方\n" +
            "• 列表模式适合详细编辑和查看\n\n" +
            "您的操作习惯不会改变，所有功能都已保留。");

        _settingsService.SetSetting("PrescriptionUnifiedView.GuideShown", true);
    }
}
```

---

## 🔄 4. 回滚方案

### 4.1 快速回滚（Git Revert）

**适用场景**：发现严重Bug，需要立即回滚

**执行步骤**：
```bash
# 1. 找到合并commit的SHA
git log --oneline | grep "feat(prescriptions): 合并PrescriptionView"

# 2. Revert该commit
git revert <commit-sha>

# 3. 推送到远程
git push origin epic/issue-1676-phase1-view-merge
```

**影响**：
- ✅ 立即恢复到旧版本
- ✅ Git历史保留完整
- ⚠️ 需要重新编译和部署

---

### 4.2 完整回滚（从备份恢复）

**适用场景**：需要完全回到合并前状态

**执行步骤**：
```bash
# 1. 恢复备份文件
cp backup/phase1-view-merge/PrescriptionView.xaml \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/
cp backup/phase1-view-merge/PrescriptionView.xaml.cs \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/
cp backup/phase1-view-merge/PrescriptionEditorDialog.xaml \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/
cp backup/phase1-view-merge/PrescriptionEditorDialog.xaml.cs \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/
cp backup/phase1-view-merge/PrescriptionViewModel.cs \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/
cp backup/phase1-view-merge/PrescriptionEditorDialogViewModel.cs \
   src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/

# 2. 删除新View
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionUnifiedView.xaml
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionUnifiedView.xaml.cs
git rm src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionUnifiedViewModel.cs

# 3. 还原Prism注册
# 手动编辑PrescriptionsModule.cs，恢复旧的注册代码

# 4. 提交回滚
git add .
git commit -m "revert: 回滚PrescriptionView合并

原因：[说明回滚原因]

恢复文件：
- PrescriptionView.xaml/cs
- PrescriptionEditorDialog.xaml/cs
- PrescriptionViewModel.cs
- PrescriptionEditorDialogViewModel.cs"

# 5. 推送
git push origin epic/issue-1676-phase1-view-merge
```

---

### 4.3 部分回滚（仅回滚布局切换功能）

**适用场景**：布局切换有Bug，但其他功能正常

**执行步骤**：
1. 注释掉布局切换按钮
2. 固定使用8列模式或列表模式
3. 暂时禁用`ToggleLayoutModeCommand`

**代码修改**：
```xaml
<!-- 隐藏布局切换按钮 -->
<ToggleButton Visibility="Collapsed" ... />
```

```csharp
// 禁用切换功能
public DelegateCommand ToggleLayoutModeCommand { get; }

public PrescriptionUnifiedViewModel()
{
    // ToggleLayoutModeCommand = new DelegateCommand(OnToggleLayoutMode);
    ToggleLayoutModeCommand = new DelegateCommand(() => { /* 暂时禁用 */ });
}
```

---

## 📊 5. 影响评估报告

### 5.1 技术影响

| 影响维度 | 评估 | 说明 |
|---------|------|------|
| **代码复杂度** | 🟡 中度增加 | 单个View复杂度增加，但总体代码量减少 |
| **可维护性** | ✅ 提升 | 统一入口，减少重复代码 |
| **性能** | 🟢 无明显影响 | 布局切换可能有微小延迟（<100ms） |
| **测试覆盖** | ⚠️ 需补充 | 需要增加布局切换的单元测试 |
| **依赖关系** | 🟢 无变化 | 不影响其他模块 |

### 5.2 用户体验影响

| 影响维度 | 评估 | 说明 |
|---------|------|------|
| **操作流程** | 🟢 无变化 | 导航路径保持一致 |
| **学习成本** | 🟡 微增 | 需要了解布局切换功能 |
| **效率提升** | ✅ 提升 | 可以根据场景选择最佳布局 |
| **错误率** | 🟢 无明显影响 | 保留所有验证逻辑 |
| **满意度** | ✅ 预期提升 | 更灵活的编辑方式 |

### 5.3 业务影响

| 影响维度 | 评估 | 说明 |
|---------|------|------|
| **数据完整性** | 🟢 无影响 | 保存逻辑不变 |
| **业务流程** | 🟢 无影响 | 不改变业务规则 |
| **报表统计** | 🟢 无影响 | 数据结构不变 |
| **合规性** | 🟢 无影响 | 符合医疗处方规范 |

### 5.4 成本效益分析

| 项目 | 估算值 |
|-----|-------|
| **开发成本** | 2-3天（1人） |
| **测试成本** | 0.5天 |
| **部署成本** | 0.25天 |
| **培训成本** | 0（提供首次使用提示） |
| **总成本** | **3-4天** |
| | |
| **收益（代码减少）** | 171行（33%） |
| **收益（维护成本）** | 每次修改节约30%时间 |
| **收益（用户体验）** | 灵活性提升 |
| **ROI** | **高** |

---

## ✅ 验收标准检查清单

### 功能验收

- [ ] **8列快速模式**：可以横向录入药材，拼音码过滤正常，焦点跳转正常
- [ ] **列表详细模式**：可以查看和编辑完整药材信息（规格/单价/金额）
- [ ] **布局切换**：8列↔列表切换流畅，数据完整保留
- [ ] **历史处方复制**：下拉框选择历史处方，药材自动填充到当前布局
- [ ] **状态管理**：可以修改处方状态（草稿→已确认→已发药→已完成）
- [ ] **查看/编辑模式**：查看模式下所有编辑控件禁用
- [ ] **医嘱保存**：医嘱字段可以正常保存和加载
- [ ] **保存功能**：保存草稿、保存正式处方、预览、关闭功能正常
- [ ] **导航集成**：从诊疗流程和历史管理都能正确打开

### 质量验收

- [ ] **编译通过**：0 errors, 0 warnings
- [ ] **无警告**：无Code Analysis警告
- [ ] **单元测试**：新增测试覆盖率>70%
- [ ] **运行时验证**：8个场景全部通过
- [ ] **性能测试**：布局切换<100ms，30味药材无卡顿

### 文档验收

- [ ] **代码注释**：关键逻辑有中文注释
- [ ] **README更新**：更新模块文档说明新View
- [ ] **变更日志**：记录到CHANGELOG.md

---

## 📋 附录：清单速查表

### A. 文件清单

**新增**：
- `PrescriptionUnifiedView.xaml`
- `PrescriptionUnifiedView.xaml.cs`
- `PrescriptionUnifiedViewModel.cs`

**删除**：
- `PrescriptionView.xaml`
- `PrescriptionView.xaml.cs`
- `PrescriptionEditorDialog.xaml`
- `PrescriptionEditorDialog.xaml.cs`
- `PrescriptionViewModel.cs`
- `PrescriptionEditorDialogViewModel.cs`

**修改**：
- `PrescriptionsModule.cs`
- `PrescriptionsMainViewModel.cs`
- `PrescriptionManagementViewModel.cs`

### B. 命令清单

**保留命令**（12个）：
- AddHerbCommand
- ImportFormulaCommand（统一命名）
- ClearAllCommand
- EditHerbCommand
- RemoveHerbCommand
- SaveDraftCommand
- SavePrescriptionCommand
- PreviewCommand
- CloseCommand

**新增命令**（1个）：
- ToggleLayoutModeCommand

### C. 属性清单

**核心属性**（20个）：
- PatientInfo
- PrescriptionNumber
- Diagnosis
- DosageCount
- Usage
- Advice
- TotalPrice
- Status
- IsViewMode
- CurrentLayoutMode
- IsDetailedListMode
- ItemRows（8列模式）
- PrescriptionItems（列表模式）
- RecentPrescriptions
- SelectedRecentPrescription
- ShowStatusSelector
- ShowPreviewButton
- LayoutModeIcon
- LayoutModeText
- IsLoading

---

**报告生成时间**：2025-10-27
**任务状态**：✅ 已完成
**关联Issue**：#1679（Task 1.3 - 输出合并建议清单）

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
