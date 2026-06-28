# Master-Detail MDIX 优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用 MDIX 模式优化 5 个 master-detail 控件（Users/Patients/Herbs/Formula/MedicalCase）的视觉与结构——统一分页、引入间距 Token、工具栏分组、PackIcon、Detail 过渡。

**Architecture:** 纯 XAML/控件层改动，不触碰 VM 逻辑。共享层（LYBT.Desktop.Controls）新增/翻新组件，各模块控件采用。Users 先作参考样本，验证后套用其余 4 个。

**Tech Stack:** WPF / Prism / MaterialDesignInXAMLToolkit (MDIX) / CommunityToolkit.Mvvm

## Global Constraints

- **包版本统一在 `Directory.Packages.props`**，不新增包（MaterialDesignThemes 已引用）
- **间距用 Token**：`{StaticResource SpacingXS/S/M/L/XL/XXL}`（仅对称间距，如 Padding）；**非对称 Margin 必须硬编码**（如 `Margin="8,0,0,0"`），因 WPF 不允许 markup extension 闭合花括号后追加文本
- **Converter 用 x:Static**：`Converter={x:Static converters:Cvt.Xxx}`，不用 StaticResource
- **图标用 MDIX PackIcon**：`<materialDesign:PackIcon Kind="..."/>`，命名空间 `xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"`
- **UI 控件 MDIX 优先**：用 MDIX 内置样式（Button/TextBox/DataGrid/Menu），不自定义 ControlTemplate
- **无注释/无 Emoji**：除非用户要求（XAML 现有注释保留）
- **跨模块禁止**：Server/Desktop 模块间禁止直接引用
- **验证命令**：`dotnet build LYBTZYZS.sln`（每个任务结束必须通过）
- **不动 VM 层**：本次不改 MasterDetailViewModelBase / PaginationService / ErrorHandler 逻辑

**Spec:** `docs/compose/specs/2026-06-27-master-detail-mdix-optimization-design.md`

---

## File Structure

### 新建（共享层 LYBT.Desktop.Controls）
| 文件 | 职责 |
|------|------|
| `Themes/Spacing.xaml` | 间距 Token 字典（SpacingXS~XXL） |
| `Themes/DataGridStyles.xaml` | DataGrid 列共享 ElementStyle |
| `Converters/BoolToIntConverter.cs` | bool→int（Transitioner SelectedIndex 用） |

### 翻新（共享层）
| 文件 | 改动 |
|------|------|
| `Controls/UnifiedPaginationBar.xaml(.cs)` | 加 PageSizes DP、用 Spacing Token、PackIcon |
| `Converters/ConverterInstances.cs` | 注册 `Cvt.BoolToInt` |
| `Shell/App.xaml` | 合并 Spacing.xaml + DataGridStyles.xaml |

### 删除（死代码）
| 文件 | 原因 |
|------|------|
| `Controls/UnifiedManagementTable.xaml(.cs)` | 零消费，设计不完整 |
| `Controls/UnifiedManagementToolBar.xaml(.cs)` | 零消费，被 DataGridToolbar 取代 |

### 改造（5 个模块，MasterDetailControl.xaml）
| 文件 | 改动 |
|------|------|
| `LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml` | 参考样本，全量改造 |
| `LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml` | 套用 |
| `LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml` | 套用 |
| `LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml` | 套用 |
| `LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` | 套用 |

---

## Task 1: 死代码清理 + 间距 Token 字典

**Covers:** S3.1, S3.2

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementToolBar.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementToolBar.xaml.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Spacing.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml`

**Interfaces:**
- Produces: `SpacingXS/S/M/L/XL/XXL` Thickness 资源键，供后续所有任务引用

- [ ] **Step 1: 删除死代码文件**

删除上述 4 个文件（UnifiedManagementTable.xaml/.cs + UnifiedManagementToolBar.xaml/.cs）。这些是零消费死代码，删除前已用 grep 确认无引用。

- [ ] **Step 2: 创建 Spacing.xaml**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Thickness x:Key="SpacingXS">4</Thickness>
    <Thickness x:Key="SpacingS">8</Thickness>
    <Thickness x:Key="SpacingM">12</Thickness>
    <Thickness x:Key="SpacingL">16</Thickness>
    <Thickness x:Key="SpacingXL">24</Thickness>
    <Thickness x:Key="SpacingXXL">32</Thickness>
</ResourceDictionary>
```

- [ ] **Step 3: 注册到 App.xaml**

在 `src/Client/Desktop/Shell/App.xaml` 的 `<ResourceDictionary.MergedDictionaries>` 中，在 `Icons.xaml` 之后添加：

```xml
<ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Spacing.xaml" />
```

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED（无找不到类型错误——死代码确无引用）

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Spacing.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml.cs src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementToolBar.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementToolBar.xaml.cs src/Client/Desktop/Shell/App.xaml
git commit -m "refactor(controls): add Spacing token dictionary and remove dead Unified* code"
```

---

## Task 2: 翻新 UnifiedPaginationBar

**Covers:** S3.3

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml.cs` (加 PageSizes DP)
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml` (Token + PackIcon + 绑定 PageSizes)

**Interfaces:**
- Consumes: `SpacingS` Token（Task 1）
- Produces: `UnifiedPaginationBar.PageSizes` DP（`IEnumerable`），供 Task 4-8 绑定 VM 的 `PageSizes`

- [ ] **Step 1: 加 PageSizes DP 到 code-behind**

在 `UnifiedPaginationBar.xaml.cs` 的 `TotalCountProperty` 之后添加：

```csharp
public System.Collections.IEnumerable PageSizes { get => (System.Collections.IEnumerable)GetValue(PageSizesProperty); set => SetValue(PageSizesProperty, value); }
public static readonly DependencyProperty PageSizesProperty = DependencyProperty.Register(nameof(PageSizes), typeof(System.Collections.IEnumerable), typeof(UnifiedPaginationBar), new PropertyMetadata(null));
```

注：DP 类型用 `IEnumerable`（非泛型），与 VM 基类 `PaginationService.PageSizes`（`IReadOnlyList<int>`）兼容——`IReadOnlyList<int>` 实现 `IEnumerable<int>` → `IEnumerable`。

- [ ] **Step 2: 重写 UnifiedPaginationBar.xaml**

完整替换为（用 Spacing Token + PackIcon + PageSizes 绑定）：

```xml
<UserControl x:Class="LYBT.Desktop.Controls.Controls.UnifiedPaginationBar"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters"
             x:Name="Root">
    <Border Background="{DynamicResource MaterialDesignPaper}"
            BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"
            BorderThickness="0,1,0,0"
            Padding="16,8">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="每页" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesign.Brush.Foreground}" Opacity="0.7" />
                <ComboBox ItemsSource="{Binding PageSizes, RelativeSource={RelativeSource AncestorType=UserControl}}"
                          SelectedItem="{Binding PageSize, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
                          Style="{StaticResource MaterialDesignOutlinedComboBox}"
                          Width="80" Margin="8,0" VerticalAlignment="Center" />
                <TextBlock Text="条" VerticalAlignment="Center" Foreground="{DynamicResource MaterialDesign.Brush.Foreground}" Opacity="0.7" />
            </StackPanel>

            <TextBlock Grid.Column="2" VerticalAlignment="Center" Style="{StaticResource MaterialDesignCaptionTextBlock}">
                <Run Text="共" />
                <Run FontWeight="Medium" Text="{Binding TotalCount, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=OneWay}" />
                <Run Text="条记录" />
            </TextBlock>

            <StackPanel Grid.Column="3" Orientation="Horizontal" VerticalAlignment="Center">
                <Button Command="{Binding FirstPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Style="{StaticResource MaterialDesignFlatButton}" ToolTip="首页"
                        Visibility="{Binding FirstPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={x:Static converters:Cvt.NullToVis}}"
                        Content="{materialDesign:PackIcon Kind=PageFirst}" />
                <Button Command="{Binding PreviousPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Style="{StaticResource MaterialDesignFlatButton}" ToolTip="上一页"
                        Content="{materialDesign:PackIcon Kind=ChevronLeft}" />
                <TextBlock VerticalAlignment="Center" Margin="8,0">
                    <Run Text="{Binding CurrentPage, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=OneWay}" />
                    <Run Text=" / " />
                    <Run Text="{Binding TotalPages, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=OneWay}" />
                </TextBlock>
                <Button Command="{Binding NextPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Style="{StaticResource MaterialDesignFlatButton}" ToolTip="下一页"
                        Content="{materialDesign:PackIcon Kind=ChevronRight}" />
                <Button Command="{Binding LastPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Style="{StaticResource MaterialDesignFlatButton}" ToolTip="末页"
                        Visibility="{Binding LastPageCommand, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={x:Static converters:Cvt.NullToVis}}"
                        Content="{materialDesign:PackIcon Kind=PageLast}" />
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

注：`Content="{materialDesign:PackIcon Kind=Xxx}"` 是 MDIX 简写标记扩展。移除了原来的 `<x:Array>` 硬编码页大小。

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml.cs
git commit -m "refactor(controls): refurbish UnifiedPaginationBar with spacing tokens and PackIcon"
```

---

## Task 3: DataGrid 共享样式 + BoolToInt 转换器

**Covers:** S4.2, S6.1

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToIntConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/ConverterInstances.cs` (注册 Cvt.BoolToInt)
- Modify: `src/Client/Desktop/Shell/App.xaml` (合并 DataGridStyles.xaml)

**Interfaces:**
- Produces: `DataGridTextCellStyle` / `DataGridSecondaryCellStyle` / `DataGridMonoCellStyle` 样式键 + `Cvt.BoolToInt` 转换器，供 Task 4-8 引用

- [ ] **Step 1: 创建 BoolToIntConverter.cs**

`src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToIntConverter.cs`：

```csharp
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Controls.Converters;

/// <summary>
/// bool -> int (false=0, true=1)，用于 Transitioner.SelectedIndex 绑定 IsEditMode
/// </summary>
public class BoolToIntConverter : IValueConverter
{
    public static readonly BoolToIntConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? (b ? 1 : 0) : 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 1;
}
```

- [ ] **Step 2: 注册到 Cvt 静态类**

在 `ConverterInstances.cs` 的 `Cvt` 类中，`BoolToVis` 之后添加：

```csharp
/// <summary>
/// Bool -> int (false=0, true=1)，用于 Transitioner.SelectedIndex
/// </summary>
public static readonly IValueConverter BoolToInt = new BoolToIntConverter();
```

- [ ] **Step 3: 创建 DataGridStyles.xaml**

`src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- 标准正文列：前景色 + 垂直居中 + 内边距 -->
    <Style x:Key="DataGridTextCellStyle" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{DynamicResource MaterialDesign.Brush.Foreground}" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="Padding" Value="8,0" />
    </Style>

    <!-- 次要列：低透明度 -->
    <Style x:Key="DataGridSecondaryCellStyle" TargetType="TextBlock"
           BasedOn="{StaticResource DataGridTextCellStyle}">
        <Setter Property="Opacity" Value="0.7" />
    </Style>

    <!-- 等宽列：编码/手机号 -->
    <Style x:Key="DataGridMonoCellStyle" TargetType="TextBlock"
           BasedOn="{StaticResource DataGridTextCellStyle}">
        <Setter Property="FontFamily" Value="Consolas" />
        <Setter Property="FontSize" Value="12" />
        <Setter Property="Opacity" Value="0.7" />
    </Style>

    <!-- 加粗姓名列 -->
    <Style x:Key="DataGridEmphasisCellStyle" TargetType="TextBlock"
           BasedOn="{StaticResource DataGridTextCellStyle}">
        <Setter Property="FontWeight" Value="SemiBold" />
    </Style>
</ResourceDictionary>
```

- [ ] **Step 4: 注册到 App.xaml**

在 `App.xaml` 的 `MergedDictionaries` 中，Spacing.xaml 之后添加：

```xml
<ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/DataGridStyles.xaml" />
```

- [ ] **Step 5: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToIntConverter.cs src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/ConverterInstances.cs src/Client/Desktop/Shell/App.xaml
git commit -m "feat(controls): add DataGrid shared styles and BoolToInt converter"
```

---

## Task 4: UserMasterDetailControl 参考实现（全量改造）

**Covers:** S4.1, S4.3, S5.1, S5.2, S6.1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml` (全量改造)

**Interfaces:**
- Consumes: `UnifiedPaginationBar` (Task 2)、`DataGridStyles` + `Cvt.BoolToInt` (Task 3)、`Spacing` Token (Task 1)
- Produces: 本控件作为 Task 5-8 的参考模板

本任务对 `UserMasterDetailControl.xaml` 做五处改造。逐项执行。

- [ ] **Step 1: 加 PackIcon 命名空间**

在根 `<controls:MasterDetailControlBase>` 标签的属性中添加（与 xmlns:behaviors 同级）：

```xml
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```

- [ ] **Step 2: 重写工具栏 AdditionalContent（方案 A：溢出菜单）**

将 `DataGridToolbar` 的 `<controls:DataGridToolbar.AdditionalContent>` 内容替换为：

```xml
<controls:DataGridToolbar.AdditionalContent>
    <StackPanel Orientation="Horizontal">
        <!-- 主操作：编辑 -->
        <Button Margin="8,0,0,0"
                Padding="12,6"
                Command="{Binding EditCommand}"
                Style="{StaticResource MaterialDesignOutlinedButton}"
                ToolTip="编辑">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Pencil" VerticalAlignment="Center" />
                <TextBlock Text="编辑" Margin="8,0,0,0" />
            </StackPanel>
        </Button>

        <!-- 次要操作：溢出菜单 -->
        <Menu Margin="8,0,0,0" Background="Transparent">
            <MenuItem>
                <MenuItem.Header>
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="DotsHorizontal" VerticalAlignment="Center" />
                        <TextBlock Text="更多" Margin="8,0,0,0" />
                    </StackPanel>
                </MenuItem.Header>
                <MenuItem Command="{Binding ResetPasswordCommand}">
                    <MenuItem.Icon>
                        <materialDesign:PackIcon Kind="Key" />
                    </MenuItem.Icon>
                    <MenuItem.Header>重置密码</MenuItem.Header>
                </MenuItem>
                <MenuItem Command="{Binding ToggleUserStatusCommand}">
                    <MenuItem.Icon>
                        <materialDesign:PackIcon Kind="ToggleSwitchOutline" />
                    </MenuItem.Icon>
                    <MenuItem.Header>切换状态</MenuItem.Header>
                </MenuItem>
                <MenuItem Command="{Binding RestoreCommand}">
                    <MenuItem.Icon>
                        <materialDesign:PackIcon Kind="Undo" />
                    </MenuItem.Icon>
                    <MenuItem.Header>恢复</MenuItem.Header>
                </MenuItem>
                <Separator />
                <MenuItem Command="{Binding ImportCommand}">
                    <MenuItem.Icon>
                        <materialDesign:PackIcon Kind="Import" />
                    </MenuItem.Icon>
                    <MenuItem.Header>导入</MenuItem.Header>
                </MenuItem>
                <MenuItem Command="{Binding DownloadTemplateCommand}">
                    <MenuItem.Icon>
                        <materialDesign:PackIcon Kind="FileDocumentOutline" />
                    </MenuItem.Icon>
                    <MenuItem.Header>模板</MenuItem.Header>
                </MenuItem>
            </MenuItem>
        </Menu>
    </StackPanel>
</controls:DataGridToolbar.AdditionalContent>
```

注：`DownloadTemplateCommand` 从原工具栏移入菜单；`ExportCommand` 由 DataGridToolbar 自带导出按钮保留。

- [ ] **Step 3: 筛选区 Token 化**

将筛选区 `StackPanel` 的硬编码 `Margin="0,0,8,0"` 统一为 `Margin="8,0,0,0"`（非对称 margin 硬编码，见 Global Constraints），`Padding="8,2"` 改为 `Padding="8,2"`（保持）。

- [ ] **Step 4: DataGrid 列样式替换**

将每个 `<DataGridTextColumn.ElementStyle>` 内联 Style 删除，列改为引用共享样式：

```xml
<!-- 用户名（强调） -->
<DataGridTextColumn Width="100" Binding="{Binding UserName}" Header="用户名"
                    ElementStyle="{StaticResource DataGridEmphasisCellStyle}" />
<!-- 姓名（标准） -->
<DataGridTextColumn Width="80" Binding="{Binding RealName}" Header="姓名"
                    ElementStyle="{StaticResource DataGridTextCellStyle}" />
<!-- 角色（次要） -->
<DataGridTextColumn Width="70" Binding="{Binding Role, Converter={x:Static converters:Cvt.EnumDesc}}" Header="角色"
                    ElementStyle="{StaticResource DataGridSecondaryCellStyle}" />
<!-- 手机号（等宽） -->
<DataGridTextColumn Width="*" Binding="{Binding PhoneNumber}" Header="手机号"
                    ElementStyle="{StaticResource DataGridMonoCellStyle}" />
```

- [ ] **Step 5: 内联分页替换为 UnifiedPaginationBar**

删除 `<Border Grid.Row="3" ...>分页控件...</Border>`（约 70 行），替换为：

```xml
<controls:UnifiedPaginationBar
    Grid.Row="3"
    CurrentPage="{Binding CurrentPage}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize}"
    TotalCount="{Binding TotalCount}"
    PageSizes="{Binding PageSizes}"
    FirstPageCommand="{Binding GoToFirstPageCommand}"
    PreviousPageCommand="{Binding GoToPreviousPageCommand}"
    NextPageCommand="{Binding GoToNextPageCommand}"
    LastPageCommand="{Binding GoToLastPageCommand}" />
```

注：`PageSizes` 在 VM 基类 `PaginationService` 暴露为 `IReadOnlyList<int>`，控件 DP 是 `IList`，兼容。

- [ ] **Step 6: ContextMenu 图标改 PackIcon**

将 ContextMenu 中所有 `<TextBlock FontFamily="Segoe Fluent Icons" Text="&#x...;">` 替换为 `<materialDesign:PackIcon Kind="...">`：

```xml
<MenuItem Command="{Binding EditCommand}" Header="编辑">
    <MenuItem.Icon><materialDesign:PackIcon Kind="Pencil" /></MenuItem.Icon>
</MenuItem>
<MenuItem Command="{Binding ResetPasswordCommand}" Header="重置密码">
    <MenuItem.Icon><materialDesign:PackIcon Kind="Key" /></MenuItem.Icon>
</MenuItem>
<MenuItem Command="{Binding ToggleUserStatusCommand}" Header="切换状态">
    <MenuItem.Icon><materialDesign:PackIcon Kind="ToggleSwitchOutline" /></MenuItem.Icon>
</MenuItem>
<MenuItem Command="{Binding RestoreCommand}" Header="恢复用户">
    <MenuItem.Icon><materialDesign:PackIcon Kind="Undo" /></MenuItem.Icon>
</MenuItem>
<MenuItem Command="{Binding DeleteCommand}" Header="删除">
    <MenuItem.Icon><materialDesign:PackIcon Kind="Delete" Foreground="{DynamicResource MaterialDesign.Brush.Error}" /></MenuItem.Icon>
</MenuItem>
```

- [ ] **Step 7: Detail 区 Transitioner 替代 Visibility 切换**

将 `<userControls:UserViewControl ... Visibility="{Binding IsEditMode, Converter={x:Static Cvt.InverseBoolToVis}}"/>` 和 `<userControls:UserEditControl ... Visibility="{Binding IsEditMode, Converter={x:Static Cvt.BoolToVis}}"/>` 两个重叠控件替换为：

```xml
<materialDesign:Transitioner Grid.Row="1"
                              SelectedIndex="{Binding IsEditMode, Converter={x:Static converters:Cvt.BoolToInt}}">
    <materialDesign:TransitionerSlide>
        <userControls:UserViewControl
            Margin="{StaticResource SpacingXXL}"
            CreatedAt="{Binding CurrentDetail.CreatedAt}"
            Email="{Binding CurrentDetail.Email}"
            LastLoginTime="{Binding CurrentDetail.LastLoginTime}"
            PhoneNumber="{Binding CurrentDetail.PhoneNumber}"
            PinYinCode="{Binding CurrentDetail.PinYinCode}"
            RealName="{Binding CurrentDetail.RealName}"
            Role="{Binding CurrentDetail.Role}"
            ShowStatus="True"
            Status="{Binding CurrentDetail.Status}"
            UpdatedAt="{Binding CurrentDetail.UpdatedAt}"
            UserName="{Binding CurrentDetail.UserName}" />
    </materialDesign:TransitionerSlide>
    <materialDesign:TransitionerSlide>
        <userControls:UserEditControl
            Margin="{StaticResource SpacingXXL}"
            User="{Binding UserEditor.User}"
            ErrorsSource="{Binding UserEditor.User.Errors}"
            IsUserNameReadOnly="{Binding IsUserNameReadOnly}"
            ShowStatus="True"
            RoleOptions="{Binding RoleOptions}"
            StatusOptions="{Binding StatusOptions}" />
    </materialDesign:TransitionerSlide>
</materialDesign:Transitioner>
```

- [ ] **Step 8: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 9: 验证桌面测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: 全部通过（VM 逻辑未改）

- [ ] **Step 10: 提交**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml
git commit -m "feat(users): adopt MDIX master-detail optimization with toolbar grouping and Transitioner"
```

---

## Task 5: PatientMasterDetailControl 改造

**Covers:** S4.1, S4.3, S5.2, S6.1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`

**共享改造（与 Task 4 相同的步骤，逐项应用）：**
1. 加 `xmlns:materialDesign`
2. 内联分页 → `<controls:UnifiedPaginationBar/>`（同 Task 4 Step 5 绑定）
3. DataGrid 列 → 共享样式（Patient 列：姓名=Emphasis、性别/年龄=Secondary、手机=Mono）
4. ContextMenu 图标 → PackIcon（编辑=Pencil、查看医案=FileSearch、新建医案=PlusBox、恢复=Undo、删除=Delete）
5. 筛选区 Margin → Spacing Token
6. Detail 区 → Transitioner（PatientViewControl / PatientEditControl，用 `Cvt.BoolToInt`）

**模块专属（AdditionalContent 溢出菜单）——Patient 的工具栏额外按钮：**

```xml
<controls:DataGridToolbar.AdditionalContent>
    <StackPanel Orientation="Horizontal">
        <Button Margin="8,0,0,0" Padding="12,6"
                Command="{Binding EditCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                ToolTip="编辑">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Pencil" VerticalAlignment="Center" />
                <TextBlock Text="编辑" Margin="8,0,0,0" />
            </StackPanel>
        </Button>
        <Menu Margin="8,0,0,0" Background="Transparent">
            <MenuItem>
                <MenuItem.Header>
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="DotsHorizontal" VerticalAlignment="Center" />
                        <TextBlock Text="更多" Margin="8,0,0,0" />
                    </StackPanel>
                </MenuItem.Header>
                <MenuItem Command="{Binding RestoreCommand}">
                    <MenuItem.Icon><materialDesign:PackIcon Kind="Undo" /></MenuItem.Icon>
                    <MenuItem.Header>恢复</MenuItem.Header>
                </MenuItem>
                <MenuItem Command="{Binding ImportCommand}">
                    <MenuItem.Icon><materialDesign:PackIcon Kind="Import" /></MenuItem.Icon>
                    <MenuItem.Header>导入</MenuItem.Header>
                </MenuItem>
            </MenuItem>
        </Menu>
    </StackPanel>
</controls:DataGridToolbar.AdditionalContent>
```

注：Patient 工具栏额外按钮较少（编辑/恢复/导入），无模板/重置密码/切换状态。

- [ ] **Step 1-6**: 同 Task 4 Step 1-7，应用上述模块专属 + 共享改造
- [ ] **Step 7: 验证编译** — `dotnet build LYBTZYZS.sln`
- [ ] **Step 8: 验证测试** — `dotnet test tests/LYBT.Tests.Desktop/`
- [ ] **Step 9: 提交** — `git commit -m "feat(patients): adopt MDIX master-detail optimization"`

---

## Task 6: HerbMasterDetailControl 改造

**Covers:** S4.1, S4.3, S5.2, S6.1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`

**共享改造（同 Task 4）：** 加命名空间、分页替换、DataGrid 列样式、ContextMenu PackIcon、筛选 Token、Detail Transitioner。

**模块专属（AdditionalContent）——Herb 的工具栏额外按钮（编辑/导入）：**

```xml
<controls:DataGridToolbar.AdditionalContent>
    <StackPanel Orientation="Horizontal">
        <Button Margin="8,0,0,0" Padding="12,6"
                Command="{Binding EditCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                ToolTip="编辑">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Pencil" VerticalAlignment="Center" />
                <TextBlock Text="编辑" Margin="8,0,0,0" />
            </StackPanel>
        </Button>
        <Button Margin="8,0,0,0" Padding="12,6"
                Command="{Binding ImportCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                ToolTip="导入">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Import" VerticalAlignment="Center" />
                <TextBlock Text="导入" Margin="8,0,0,0" />
            </StackPanel>
        </Button>
    </StackPanel>
</controls:DataGridToolbar.AdditionalContent>
```

注：Herb 工具栏只有 2 个额外按钮，无需溢出菜单，直接保留。

**Herb ContextMenu 图标：** 编辑=Pencil、恢复=Undo、删除=Delete。
**Herb DataGrid 列：** 药材名=Emphasis、拼音=Mono、类别/单位=Secondary、价格=Mono。

- [ ] **Step 1-6**: 应用改造
- [ ] **Step 7: 验证编译** — `dotnet build LYBTZYZS.sln`
- [ ] **Step 8: 验证测试** — `dotnet test tests/LYBT.Tests.Desktop/`
- [ ] **Step 9: 提交** — `git commit -m "feat(herbs): adopt MDIX master-detail optimization"`

---

## Task 7: FormulaMasterDetailControl 改造

**Covers:** S4.1, S4.3, S5.2, S6.1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`

**共享改造（同 Task 4）。**

**模块专属（AdditionalContent）——Formula 的工具栏额外按钮（编辑/恢复/导入/导出模板）：**

```xml
<controls:DataGridToolbar.AdditionalContent>
    <StackPanel Orientation="Horizontal">
        <Button Margin="8,0,0,0" Padding="12,6"
                Command="{Binding EditCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                ToolTip="编辑">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Pencil" VerticalAlignment="Center" />
                <TextBlock Text="编辑" Margin="8,0,0,0" />
            </StackPanel>
        </Button>
        <Menu Margin="8,0,0,0" Background="Transparent">
            <MenuItem>
                <MenuItem.Header>
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="DotsHorizontal" VerticalAlignment="Center" />
                        <TextBlock Text="更多" Margin="8,0,0,0" />
                    </StackPanel>
                </MenuItem.Header>
                <MenuItem Command="{Binding RestoreCommand}">
                    <MenuItem.Icon><materialDesign:PackIcon Kind="Undo" /></MenuItem.Icon>
                    <MenuItem.Header>恢复</MenuItem.Header>
                </MenuItem>
                <Separator />
                <MenuItem Command="{Binding ImportCommand}">
                    <MenuItem.Icon><materialDesign:PackIcon Kind="Import" /></MenuItem.Icon>
                    <MenuItem.Header>导入</MenuItem.Header>
                </MenuItem>
            </MenuItem>
        </Menu>
    </StackPanel>
</controls:DataGridToolbar.AdditionalContent>
```

**Formula ContextMenu 图标：** 编辑=Pencil、恢复=Undo、删除=Delete。
**Formula DataGrid 列：** 验方名=Emphasis、分类=Secondary、备注=Secondary。

- [ ] **Step 1-6**: 应用改造
- [ ] **Step 7: 验证编译** — `dotnet build LYBTZYZS.sln`
- [ ] **Step 8: 验证测试** — `dotnet test tests/LYBT.Tests.Desktop/`
- [ ] **Step 9: 提交** — `git commit -m "feat(formula): adopt MDIX master-detail optimization"`

---

## Task 8: MedicalCaseMasterDetailControl 改造

**Covers:** S4.1, S4.3, S5.2, S6.1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml`

**共享改造（同 Task 4），但 MedicalCase 特殊：不支持新建（CreateCommand 应为空/null，DataGridToolbar 的 NullToVis 会隐藏新增按钮）。**

**模块专属（AdditionalContent）——MedicalCase 工具栏通常无额外按钮（只读管理），AdditionalContent 可留空或仅放查看操作：**

```xml
<controls:DataGridToolbar.AdditionalContent>
    <!-- 医案为只读管理，无额外操作；留空 -->
</controls:DataGridToolbar.AdditionalContent>
```

**MedicalCase ContextMenu 图标：** 查看=Pencil、复制=ContentCopy、取消（删除）=Cancel。
**MedicalCase DataGrid 列：** 患者姓名=Emphasis、医生=Secondary、诊断=Secondary、日期=Mono。

**Detail 区注意：** MedicalCase 的 Detail 区结构较复杂（含 ConsultationEditor / PrescriptionEditor 子 VM），Transitioner 仅用于顶层查看↔编辑切换，若该控件无 IsEditMode 双态（医案多为只读查看），则**跳过 Transitioner 改造**，仅做分页/样式/PackIcon。

- [ ] **Step 1: 检查 MedicalCase Detail 区是否有 IsEditMode 双态**
  读 `MedicalCaseMasterDetailControl.xaml` 的 Detail 区，若仅查看模式（无 EditControl），跳过 Step Transitioner。
- [ ] **Step 2-5**: 应用分页/DataGrid样式/PackIcon/Token 改造
- [ ] **Step 6: 验证编译** — `dotnet build LYBTZYZS.sln`
- [ ] **Step 7: 验证测试** — `dotnet test tests/LYBT.Tests.Desktop/`
- [ ] **Step 8: 提交** — `git commit -m "feat(medicalcase): adopt MDIX master-detail optimization"`

---

## 收尾：全量验证

- [ ] **全量构建** — `dotnet build LYBTZYZS.sln`，BUILD SUCCEEDED
- [ ] **桌面测试** — `dotnet test tests/LYBT.Tests.Desktop/`，全绿
- [ ] **视觉验证** — 启动 Desktop，Admin 角色台，逐个进入 用户/患者/药材/验方/医案 管理，验证：
  - 分页栏 PackIcon 按钮、页大小切换、页码显示
  - 工具栏溢出菜单展开
  - 筛选区无错位
  - 视图↔编辑滑动过渡（Users/Patients/Herbs/Formula）
  - ContextMenu 图标
