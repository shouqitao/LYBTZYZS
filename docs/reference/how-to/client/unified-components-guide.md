# 统一组件库使用指南

**Issue #1840 - Desktop端管理界面UI统一化**

## 概述

本文档介绍Desktop端统一组件库的使用方法,包括统一样式系统和5个核心组件。统一组件库旨在提供一致的UI体验,减少代码重复,提高开发效率。

## 目录

1. [统一样式系统](#统一样式系统)
2. [核心组件](#核心组件)
   - [UnifiedManagementToolBar](#unifiedmanagementtoolbar)
   - [UnifiedManagementTable](#unifiedmanagementtable)
   - [UnifiedStatusBadge](#unifiedstatusbadge)
   - [UnifiedPaginationBar](#unifiedpaginationbar)
3. [最佳实践](#最佳实践)

---

## 统一样式系统

### 位置

- 资源字典: `LYBT.Desktop.Infrastructure/Themes/UnifiedComponents.xaml`
- 应用级引用: `LYBT.Desktop.Shell/App.xaml`

### Type Ramp - 字体大小系统

使用统一的字体大小层级系统,确保UI一致性:

| 级别 | 字号/行高 (epx) | 使用场景 | 资源键 |
|-----|---------------|---------|--------|
| **Caption** | 12/16 | 辅助说明文本 | `FontSizeCaption`, `LineHeightCaption` |
| **Body** | 14/20 | 默认正文 | `FontSizeBody`, `LineHeightBody` |
| **Subtitle** | 20/28 | 小标题 | `FontSizeSubtitle`, `LineHeightSubtitle` |
| **Title** | 28/36 | 页面标题 | `FontSizeTitle`, `LineHeightTitle` |
| **Display** | 40/52 | 大标题 | `FontSizeDisplay`, `LineHeightDisplay` |
| **Large Display** | 68/92 | 超大标题 | `FontSizeLargeDisplay`, `LineHeightLargeDisplay` |

**示例**:
```xaml
<TextBlock Text="页面标题"
           FontSize="{StaticResource FontSizeTitle}"
           LineHeight="{StaticResource LineHeightTitle}" />
```

### Spacing System - 间距系统

所有间距遵循**4 epx增量规则**:

| 名称 | 数值 (epx) | 使用场景 | 资源键 |
|-----|-----------|---------|--------|
| **XSmall** | 4 | 紧凑元素间距 | `SpacingXSmall` |
| **Small** | 8 | 小间距 | `SpacingSmall` |
| **Medium** | 12 | 默认间距 | `SpacingMedium` |
| **Large** | 16 | 大间距 | `SpacingLarge` |
| **XLarge** | 24 | 区块间距 | `SpacingXLarge` |
| **XXLarge** | 32 | 页面级间距 | `SpacingXXLarge` |

**示例**:
```xaml
<StackPanel Margin="{StaticResource SpacingLarge}">
    <TextBlock Text="标题" Margin="{StaticResource SpacingMedium}" />
    <Button Content="确定" Padding="{StaticResource SpacingSmall}" />
</StackPanel>
```

### Corner Radius - 圆角系统

| 名称 | 数值 (epx) | 使用场景 | 资源键 |
|-----|-----------|---------|--------|
| **Small** | 4 | 按钮、输入框 | `CornerRadiusSmall` |
| **Medium** | 8 | 卡片、面板 | `CornerRadiusMedium` |
| **Large** | 12 | 对话框、窗口 | `CornerRadiusLarge` |

### Color System - 颜色系统

#### 主题色

| 颜色 | RGB | 用途 | 资源键 |
|-----|-----|------|--------|
| **Primary** | #0078D4 | 主操作按钮 | `PrimaryBrush` |
| **Secondary** | #6C757D | 次要操作 | `SecondaryBrush` |
| **Success** | #34A853 | 成功状态 | `SuccessBrush` |
| **Warning** | #FBBC04 | 警告状态 | `WarningBrush` |
| **Danger** | #EA4335 | 危险操作 | `DangerBrush` |
| **Info** | #4285F4 | 信息提示 | `InfoBrush` |

#### 中性色

| 颜色 | RGB | 用途 | 资源键 |
|-----|-----|------|--------|
| **Neutral** | #9E9E9E | 禁用/中性状态 | `NeutralBrush` |
| **NeutralLight** | #E0E0E0 | 边框、分隔线 | `NeutralLightBrush` |
| **NeutralDark** | #616161 | 深色文本 | `NeutralDarkBrush` |

### Button Styles - 按钮样式

```xaml
<!-- 主要按钮 -->
<Button Content="保存" Style="{StaticResource PrimaryButton}" />

<!-- 次要按钮 -->
<Button Content="取消" Style="{StaticResource SecondaryButton}" />

<!-- 危险按钮 -->
<Button Content="删除" Style="{StaticResource DangerButton}" />

<!-- 成功按钮 -->
<Button Content="确认" Style="{StaticResource SuccessButton}" />

<!-- 警告按钮 -->
<Button Content="警告" Style="{StaticResource WarningButton}" />

<!-- 信息按钮 -->
<Button Content="详情" Style="{StaticResource InfoButton}" />
```

### TextBox Styles - 输入框样式

```xaml
<!-- 搜索框 -->
<TextBox Width="280"
         Text="{Binding SearchText}"
         Style="{StaticResource SearchTextBox}" />
```

### ComboBox Styles - 下拉框样式

```xaml
<!-- 筛选下拉框 -->
<ComboBox ItemsSource="{Binding FilterOptions}"
          SelectedItem="{Binding SelectedFilter}"
          Style="{StaticResource FilterComboBox}"
          Width="120" />
```

### DataGrid Styles - 表格样式

```xaml
<!-- 基础表格样式 (自动启用虚拟化) -->
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}"
          Style="{StaticResource BaseDataGridStyle}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" />
        <DataGridTextColumn Header="年龄" Binding="{Binding Age}" />
    </DataGrid.Columns>
</DataGrid>
```

---

## 核心组件

### UnifiedManagementToolBar

**用途**: 统一的管理工具栏,提供搜索、筛选、操作按钮区域。

**位置**: `LYBT.Desktop.Infrastructure.Controls.UnifiedManagementToolBar`

#### 依赖属性

| 属性 | 类型 | 说明 | 绑定模式 |
|-----|------|------|---------|
| `SearchText` | `string` | 搜索文本 | TwoWay |
| `SearchCommand` | `ICommand` | 搜索命令 | OneWay |
| `FilterContent` | `object` | 筛选区内容(插槽) | OneWay |
| `ActionButtons` | `object` | 操作按钮区内容(插槽) | OneWay |

#### 使用示例

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- 工具栏 -->
        <RowDefinition Height="*" />      <!-- 内容区 -->
    </Grid.RowDefinitions>

    <!-- 工具栏 -->
    <controls:UnifiedManagementToolBar
        Grid.Row="0"
        SearchText="{Binding SearchText, Mode=TwoWay}"
        SearchCommand="{Binding SearchCommand}">

        <!-- 筛选区插槽 -->
        <controls:UnifiedManagementToolBar.FilterContent>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="状态:" VerticalAlignment="Center" />
                <ComboBox ItemsSource="{Binding StatusOptions}"
                          SelectedItem="{Binding SelectedStatus}"
                          Style="{StaticResource FilterComboBox}"
                          Width="120" />
            </StackPanel>
        </controls:UnifiedManagementToolBar.FilterContent>

        <!-- 操作按钮区插槽 -->
        <controls:UnifiedManagementToolBar.ActionButtons>
            <StackPanel Orientation="Horizontal">
                <Button Content="+ 新建"
                        Command="{Binding CreateCommand}"
                        Style="{StaticResource PrimaryButton}"
                        Margin="{StaticResource SpacingSmall}" />
                <Button Content="导出"
                        Command="{Binding ExportCommand}"
                        Style="{StaticResource SecondaryButton}" />
            </StackPanel>
        </controls:UnifiedManagementToolBar.ActionButtons>
    </controls:UnifiedManagementToolBar>
</Grid>
```

---

### UnifiedManagementTable

**用途**: 统一的数据表格组件,支持虚拟化、选中绑定、空状态显示、批量选择。

**位置**: `LYBT.Desktop.Infrastructure.Controls.UnifiedManagementTable`

**Issue #2150**: 添加批量删除功能支持(checkbox列、全选、Ctrl+A快捷键)

#### 依赖属性

| 属性 | 类型 | 说明 | 绑定模式 |
|-----|------|------|---------|
| `ItemsSource` | `IEnumerable` | 数据源 | OneWay |
| `SelectedItem` | `object` | 单选选中项 | TwoWay |
| `SelectedItems` | `IList` | 多选选中项集合(批量操作) | TwoWay |
| `ShowCheckBoxColumn` | `bool` | 是否显示CheckBox选择列 | OneWay |
| `ShowEmptyState` | `bool` | 是否显示空状态 | OneWay |
| `EmptyStateText` | `string` | 空状态文本 | OneWay |
| `SelectAllCommand` | `ICommand` | 全选/取消全选命令 | OneWay |

#### 使用示例

**基础示例(单选模式)**:
```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Patients}"
    SelectedItem="{Binding SelectedPatient, Mode=TwoWay}"
    EmptyStateText="暂无患者数据">

    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120" />
        <DataGridTextColumn Header="性别" Binding="{Binding Gender}" Width="80" />
        <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="80" />
        <DataGridTextColumn Header="手机号" Binding="{Binding Phone}" Width="140" />

        <!-- 状态列使用模板 -->
        <DataGridTemplateColumn Header="状态" Width="100">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <controls:UnifiedStatusBadge
                        Text="{Binding StatusText}"
                        Type="{Binding StatusType}" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

**批量删除模式(Issue #2150)**:
```xaml
<!-- 表格 - 启用批量选择 -->
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    SelectedItems="{Binding SelectedItems, Mode=TwoWay}"
    ShowCheckBoxColumn="True"
    EmptyStateText="暂无数据">

    <controls:UnifiedManagementTable.Columns>
        <!-- 数据列定义 -->
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="*" />
        <DataGridTextColumn Header="描述" Binding="{Binding Description}" Width="2*" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>

<!-- 批量操作按钮 -->
<StackPanel Orientation="Horizontal" Margin="{StaticResource SpacingMedium}">
    <TextBlock Text="{Binding SelectedItems.Count, StringFormat='已选择 {0} 项'}"
               VerticalAlignment="Center"
               Margin="{StaticResource SpacingSmall}" />
    <Button Content="批量删除"
            Command="{Binding BatchDeleteCommand}"
            Style="{StaticResource DangerButton}"
            Padding="{StaticResource SpacingSmall}" />
</StackPanel>
```

**ViewModel示例**:
```csharp
using System.Collections.ObjectModel;
using LYBT.Desktop.Models.ViewModels.Base;

public class HerbManagementViewModel : UnifiedListViewModelBase<HerbDto>
{
    // 继承自UnifiedListViewModelBase的SelectedItems属性会自动绑定

    // 批量删除命令已由基类提供: BatchDeleteCommand
    // 只需实现抽象方法OnExecuteBatchDeleteAsync

    protected override async Task OnExecuteBatchDeleteAsync(List<HerbDto> items)
    {
        // 业务逻辑: 逐个删除
        var successCount = 0;
        var failureCount = 0;

        foreach (var item in items)
        {
            try
            {
                var success = await _herbRepository.DeleteAsync(item.Id);
                if (success) successCount++;
                else failureCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                Logger.LogError(ex, "删除中药材失败: {Name}", item.Name);
            }
        }

        // 显示结果反馈
        var message = $"批量删除完成!\\n\\n成功：{successCount}个\\n失败：{failureCount}个";
        await ShowSuccessMessageAsync(message);
    }
}
```

**快捷键支持(Issue #2160)**:
- **Ctrl+A**: 全选/取消全选(自动启用)
- **表头CheckBox**: 点击全选/部分选中/未选中三态切换

**⚠️ 重要提示**:
- 设置`ShowCheckBoxColumn="True"`自动添加checkbox列和全选功能
- `SelectedItems`绑定到ViewModel的ObservableCollection<T>
- 继承`UnifiedListViewModelBase`即可获得批量删除基础支持
- 子类只需实现`OnExecuteBatchDeleteAsync`方法

---

### UnifiedStatusBadge

**用途**: 统一的状态标签组件,支持多种状态类型和颜色。

**位置**: `LYBT.Desktop.Infrastructure.Controls.UnifiedStatusBadge`

#### BadgeType 枚举

```csharp
public enum BadgeType
{
    Success,   // 成功 - 绿色
    Warning,   // 警告 - 黄色
    Danger,    // 危险 - 红色
    Info,      // 信息 - 蓝色
    Neutral    // 中性 - 灰色
}
```

#### 依赖属性

| 属性 | 类型 | 说明 | 绑定模式 |
|-----|------|------|---------|
| `Text` | `string` | 状态文本 | OneWay |
| `Type` | `BadgeType` | 标签类型 | OneWay |

#### 使用示例

```xaml
<!-- 成功状态 -->
<controls:UnifiedStatusBadge Text="已完成" Type="Success" />

<!-- 警告状态 -->
<controls:UnifiedStatusBadge Text="待审核" Type="Warning" />

<!-- 危险状态 -->
<controls:UnifiedStatusBadge Text="已取消" Type="Danger" />

<!-- 信息状态 -->
<controls:UnifiedStatusBadge Text="处理中" Type="Info" />

<!-- 中性状态 -->
<controls:UnifiedStatusBadge Text="草稿" Type="Neutral" />

<!-- 在DataGrid中使用 -->
<DataGridTemplateColumn Header="状态">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <controls:UnifiedStatusBadge
                Text="{Binding StatusText}"
                Type="{Binding StatusBadgeType}" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**ViewModel示例**:
```csharp
using LYBT.Desktop.Infrastructure.Controls;

public class PatientViewModel
{
    public string StatusText => Status switch
    {
        PatientStatus.Active => "正常",
        PatientStatus.Inactive => "停诊",
        PatientStatus.Deleted => "已删除",
        _ => "未知"
    };

    public BadgeType StatusBadgeType => Status switch
    {
        PatientStatus.Active => BadgeType.Success,
        PatientStatus.Inactive => BadgeType.Warning,
        PatientStatus.Deleted => BadgeType.Danger,
        _ => BadgeType.Neutral
    };
}
```

---

### UnifiedPaginationBar

**用途**: 统一的分页工具栏,提供页码控制、每页大小选择等功能。

**位置**: `LYBT.Desktop.Infrastructure.Controls.UnifiedPaginationBar`

#### 依赖属性

| 属性 | 类型 | 说明 | 绑定模式 |
|-----|------|------|---------|
| `CurrentPage` | `int` | 当前页码(从1开始) | TwoWay |
| `TotalPages` | `int` | 总页数 | OneWay |
| `PageSize` | `int` | 每页显示数量 | TwoWay |
| `TotalCount` | `int` | 总记录数 | OneWay |
| `PreviousPageCommand` | `ICommand` | 上一页命令 | OneWay |
| `NextPageCommand` | `ICommand` | 下一页命令 | OneWay |
| `PageSizeChangedCommand` | `ICommand` | 页大小改变命令 | OneWay |

#### 使用示例

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- 工具栏 -->
        <RowDefinition Height="*" />      <!-- 表格 -->
        <RowDefinition Height="Auto" />  <!-- 分页栏 -->
    </Grid.RowDefinitions>

    <!-- 工具栏 -->
    <controls:UnifiedManagementToolBar Grid.Row="0" ... />

    <!-- 表格 -->
    <controls:UnifiedManagementTable Grid.Row="1" ... />

    <!-- 分页栏 -->
    <controls:UnifiedPaginationBar
        Grid.Row="2"
        CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
        TotalPages="{Binding TotalPages}"
        PageSize="{Binding PageSize, Mode=TwoWay}"
        TotalCount="{Binding TotalCount}"
        PreviousPageCommand="{Binding PreviousPageCommand}"
        NextPageCommand="{Binding NextPageCommand}"
        PageSizeChangedCommand="{Binding PageSizeChangedCommand}" />
</Grid>
```

**ViewModel示例**:
```csharp
public class PatientManagementViewModel : BindableBase
{
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _pageSize = 20;
    private int _totalCount;

    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        private set => SetProperty(ref _totalPages, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                CurrentPage = 1; // 重置到第一页
                _ = LoadDataAsync();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }

    public PatientManagementViewModel()
    {
        PreviousPageCommand = new DelegateCommand(
            () => CurrentPage--,
            () => CurrentPage > 1);

        NextPageCommand = new DelegateCommand(
            () => CurrentPage++,
            () => CurrentPage < TotalPages);
    }

    private async Task LoadDataAsync()
    {
        // 加载分页数据
        var result = await _service.GetPatientsAsync(CurrentPage, PageSize);

        TotalCount = result.TotalCount;
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        // 更新命令可用性
        PreviousPageCommand.RaiseCanExecuteChanged();
        NextPageCommand.RaiseCanExecuteChanged();
    }
}
```

---

## 最佳实践

### 1. 完整的管理页面布局

```xaml
<UserControl x:Class="LYBT.Desktop.Patients.Views.PatientManagementView"
             xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 工具栏 -->
            <RowDefinition Height="*" />      <!-- 表格 -->
            <RowDefinition Height="Auto" />  <!-- 分页栏 -->
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <controls:UnifiedManagementToolBar
            Grid.Row="0"
            SearchText="{Binding SearchText, Mode=TwoWay}"
            SearchCommand="{Binding SearchCommand}">

            <controls:UnifiedManagementToolBar.FilterContent>
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="性别:" VerticalAlignment="Center"
                               Margin="{StaticResource SpacingSmall}" />
                    <ComboBox ItemsSource="{Binding GenderOptions}"
                              SelectedItem="{Binding SelectedGender}"
                              Style="{StaticResource FilterComboBox}"
                              Width="100" />

                    <TextBlock Text="状态:" VerticalAlignment="Center"
                               Margin="{StaticResource SpacingMedium,0,StaticResource SpacingSmall,0}" />
                    <ComboBox ItemsSource="{Binding StatusOptions}"
                              SelectedItem="{Binding SelectedStatus}"
                              Style="{StaticResource FilterComboBox}"
                              Width="120" />
                </StackPanel>
            </controls:UnifiedManagementToolBar.FilterContent>

            <controls:UnifiedManagementToolBar.ActionButtons>
                <StackPanel Orientation="Horizontal">
                    <Button Content="+ 新建患者"
                            Command="{Binding CreateCommand}"
                            Style="{StaticResource PrimaryButton}"
                            Margin="{StaticResource SpacingSmall}" />
                    <Button Content="导入"
                            Command="{Binding ImportCommand}"
                            Style="{StaticResource SecondaryButton}"
                            Margin="{StaticResource SpacingSmall}" />
                    <Button Content="导出"
                            Command="{Binding ExportCommand}"
                            Style="{StaticResource SecondaryButton}" />
                </StackPanel>
            </controls:UnifiedManagementToolBar.ActionButtons>
        </controls:UnifiedManagementToolBar>

        <!-- 表格 -->
        <controls:UnifiedManagementTable
            Grid.Row="1"
            ItemsSource="{Binding Patients}"
            SelectedItem="{Binding SelectedPatient, Mode=TwoWay}"
            EmptyStateText="暂无患者数据,点击右上角"新建患者"开始使用">

            <controls:UnifiedManagementTable.Columns>
                <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120" />
                <DataGridTextColumn Header="性别" Binding="{Binding GenderText}" Width="80" />
                <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="80" />
                <DataGridTextColumn Header="手机号" Binding="{Binding Phone}" Width="140" />
                <DataGridTextColumn Header="身份证号" Binding="{Binding IdCard}" Width="180" />

                <DataGridTemplateColumn Header="状态" Width="100">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <controls:UnifiedStatusBadge
                                Text="{Binding StatusText}"
                                Type="{Binding StatusBadgeType}" />
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <DataGridTextColumn Header="创建时间"
                                    Binding="{Binding CreatedAt, StringFormat=yyyy-MM-dd HH:mm}"
                                    Width="140" />

                <DataGridTemplateColumn Header="操作" Width="180">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource InfoButton}"
                                        Padding="{StaticResource SpacingSmall}"
                                        Margin="{StaticResource SpacingXSmall}" />
                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource DangerButton}"
                                        Padding="{StaticResource SpacingSmall}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </controls:UnifiedManagementTable.Columns>
        </controls:UnifiedManagementTable>

        <!-- 分页栏 -->
        <controls:UnifiedPaginationBar
            Grid.Row="2"
            CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
            TotalPages="{Binding TotalPages}"
            PageSize="{Binding PageSize, Mode=TwoWay}"
            TotalCount="{Binding TotalCount}"
            PreviousPageCommand="{Binding PreviousPageCommand}"
            NextPageCommand="{Binding NextPageCommand}" />
    </Grid>
</UserControl>
```

### 2. 命名空间引用

在XAML文件顶部添加命名空间引用:

```xaml
<UserControl ...
             xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">
```

### 3. 样式优先级

1. **优先使用统一组件** - 使用UnifiedManagementToolBar、UnifiedManagementTable等
2. **其次使用统一样式** - 使用PrimaryButton、FilterComboBox等预定义样式
3. **最后自定义样式** - 仅在必要时创建自定义样式

### 4. 间距规范

- 使用统一间距系统: SpacingXSmall (4), SpacingSmall (8), SpacingMedium (12), SpacingLarge (16)
- 禁止硬编码数值,如 `Margin="10"`,应使用 `Margin="{StaticResource SpacingMedium}"`

### 5. 颜色规范

- 使用统一颜色系统: PrimaryBrush, SecondaryBrush, SuccessBrush等
- 禁止硬编码颜色值,如 `Background="#0078D4"`,应使用 `Background="{StaticResource PrimaryBrush}"`

### 6. 字体规范

- 使用Type Ramp系统: FontSizeCaption, FontSizeBody, FontSizeTitle等
- 保持行高一致: 同时设置FontSize和LineHeight

---

## 转换器支持

统一组件库提供以下转换器(已在App.xaml中全局注册):

| 转换器 | 用途 | 资源键 |
|-------|------|--------|
| `NullToVisibilityConverter` | null→Collapsed, 非null→Visible | `NullToVisibilityConverter` |
| `InverseNullToVisibilityConverter` | null→Visible, 非null→Collapsed | `InverseNullToVisibilityConverter` |
| `EnumDescriptionConverter` | 枚举值→Description特性文本 | `EnumDescriptionConverter` |

**使用示例**:
```xaml
<!-- 空状态显示 -->
<TextBlock Text="暂无数据"
           Visibility="{Binding Items, Converter={StaticResource InverseNullToVisibilityConverter}}" />

<!-- 枚举显示 -->
<TextBlock Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}" />
```

---

## 迁移指南

### 从旧版界面迁移到统一组件

#### 1. 替换工具栏区域

**旧版代码**:
```xaml
<Border Style="{StaticResource ToolBarContainer}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <StackPanel Grid.Column="0" Orientation="Horizontal">
            <TextBox x:Name="SearchBox" Width="280" ... />
            <Button Content="搜索" ... />
            <!-- 筛选控件 -->
        </StackPanel>

        <StackPanel Grid.Column="1" Orientation="Horizontal">
            <Button Content="新建" ... />
            <Button Content="导出" ... />
        </StackPanel>
    </Grid>
</Border>
```

**新版代码**:
```xaml
<controls:UnifiedManagementToolBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchCommand="{Binding SearchCommand}">

    <controls:UnifiedManagementToolBar.FilterContent>
        <!-- 筛选控件 -->
    </controls:UnifiedManagementToolBar.FilterContent>

    <controls:UnifiedManagementToolBar.ActionButtons>
        <StackPanel Orientation="Horizontal">
            <Button Content="新建" ... />
            <Button Content="导出" ... />
        </StackPanel>
    </controls:UnifiedManagementToolBar.ActionButtons>
</controls:UnifiedManagementToolBar>
```

#### 2. 替换表格

**旧版代码**:
```xaml
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}"
          AutoGenerateColumns="False"
          ... >
```

**新版代码**:
```xaml
<controls:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem}"
    EmptyStateText="暂无数据">
    <controls:UnifiedManagementTable.Columns>
        <!-- 列定义 -->
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

#### 3. 替换状态显示

**旧版代码**:
```xaml
<Border Background="{Binding StatusColor}"
        CornerRadius="4"
        Padding="8,4">
    <TextBlock Text="{Binding StatusText}" Foreground="White" />
</Border>
```

**新版代码**:
```xaml
<controls:UnifiedStatusBadge
    Text="{Binding StatusText}"
    Type="{Binding StatusBadgeType}" />
```

---

## 常见问题

### Q1: UnifiedManagementTable的列如何定义?

A: 直接使用`UnifiedManagementTable.Columns`属性,它实际上是内部DataGrid的Columns集合:

```xaml
<controls:UnifiedManagementTable ...>
    <controls:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" />
    </controls:UnifiedManagementTable.Columns>
</controls:UnifiedManagementTable>
```

### Q2: 如何自定义分页栏的页大小选项?

A: 当前版本使用固定选项(10/20/50/100),如需自定义,请在Issue中提出需求。

### Q3: UnifiedStatusBadge的颜色可以自定义吗?

A: BadgeType已涵盖常用场景。特殊需求可通过扩展BadgeType枚举或直接使用Border+TextBlock实现。

### Q4: 如何处理表格的双击事件?

A: 在UnifiedManagementTable上添加MouseDoubleClick事件处理:

```xaml
<controls:UnifiedManagementTable
    x:Name="DataTable"
    ItemsSource="{Binding Items}"
    MouseDoubleClick="DataTable_MouseDoubleClick" />
```

```csharp
private void DataTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (DataTable.SelectedItem != null)
    {
        // 处理双击逻辑
    }
}
```

---

## 相关文档

- [统一设计系统规范](../../explanation/architecture/client/unified-design-system.md)
- [组件开发规范](./infrastructure-usage.md)
- [MVVM模式实践](../../explanation/architecture/client/mvvm-architecture.md)

---

**更新日期**: 2025-11-06
**版本**: v1.0
**Issue**: #1840
