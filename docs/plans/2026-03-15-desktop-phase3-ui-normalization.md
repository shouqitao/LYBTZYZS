# Desktop Phase 3: UI 规范化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 消除 XAML 样式重复和硬编码，建立统一的设计系统，硬编码颜色/字体减少 90%

**Architecture:** 基于现有四层样式架构（HandyControl基础 → TCM中医主题 → 组件样式 → Shell样式），统一颜色来源、清理重复定义、提取语义化资源键。保持向后兼容，逐步迁移。

**Tech Stack:** WPF, XAML, HandyControl, .NET 8

---

## 前置检查

在开始之前，验证以下文件存在：

```bash
# 验证关键文件
ls src/Client/Desktop/Shell/App.xaml
ls src/Client/Desktop/Shell/Styles/Typography.xaml
ls src/Client/Desktop/Shell/Styles/Controls.xaml
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens.xaml
```

**检查当前硬编码数量：**
```bash
grep -r "Foreground.*#" src/Client/Desktop --include="*.xaml" | wc -l
grep -r "Background.*#" src/Client/Desktop --include="*.xaml" | wc -l
grep -r 'FontFamily="Microsoft YaHei"' src/Client/Desktop --include="*.xaml" | wc -l
```

---

## Task 1: 统一颜色系统 - 创建语义化颜色资源

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml`
- Test: 视觉验证（启动应用检查颜色是否正常）

**Step 1: 在 TCM.Theme.xaml 中添加语义化颜色资源**

在文件末尾 `</ResourceDictionary>` 前添加：

```xml
    <!-- 语义化颜色定义 - Phase 3 规范化添加 -->
    <!-- 阴影 -->
    <Color x:Key="ShadowColor">#000000</Color>
    <SolidColorBrush x:Key="ShadowBrush" Color="{StaticResource ShadowColor}" Opacity="0.12"/>

    <!-- 验证/错误 -->
    <Color x:Key="ValidationErrorColor">#DC3545</Color>
    <Color x:Key="ValidationWarningColor">#FFC107</Color>
    <Color x:Key="ValidationSuccessColor">#28A745</Color>
    <SolidColorBrush x:Key="ValidationErrorBrush" Color="{StaticResource ValidationErrorColor}"/>
    <SolidColorBrush x:Key="ValidationWarningBrush" Color="{StaticResource ValidationWarningColor}"/>
    <SolidColorBrush x:Key="ValidationSuccessBrush" Color="{StaticResource ValidationSuccessColor}"/>

    <!-- 禁用状态 -->
    <Color x:Key="DisabledForegroundColor">#757575</Color>
    <Color x:Key="DisabledBackgroundColor">#F5F5F5</Color>
    <Color x:Key="DisabledBorderColor">#E0E0E0</Color>
    <SolidColorBrush x:Key="DisabledForegroundBrush" Color="{StaticResource DisabledForegroundColor}"/>
    <SolidColorBrush x:Key="DisabledBackgroundBrush" Color="{StaticResource DisabledBackgroundColor}"/>
    <SolidColorBrush x:Key="DisabledBorderBrush" Color="{StaticResource DisabledBorderColor}"/>

    <!-- 悬停/高亮 -->
    <Color x:Key="HoverBackgroundColor">#F5F5F5</Color>
    <Color x:Key="SelectedBackgroundColor">#E3F2FD</Color>
    <SolidColorBrush x:Key="HoverBackgroundBrush" Color="{StaticResource HoverBackgroundColor}"/>
    <SolidColorBrush x:Key="SelectedBackgroundBrush" Color="{StaticResource SelectedBackgroundColor}"/>

    <!-- DataGrid 特定 -->
    <Color x:Key="DataGridAlternatingRowColor">#FAFAFA</Color>
    <SolidColorBrush x:Key="DataGridAlternatingRowBrush" Color="{StaticResource DataGridAlternatingRowColor}"/>

    <!-- 边框/分割线 -->
    <Color x:Key="BorderLightColor">#E0E0E0</Color>
    <Color x:Key="BorderMediumColor">#BDBDBD</Color>
    <SolidColorBrush x:Key="BorderLightBrush" Color="{StaticResource BorderLightColor}"/>
    <SolidColorBrush x:Key="BorderMediumBrush" Color="{StaticResource BorderMediumColor}"/>
```

**Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译成功，无错误

**Step 3: 视觉验证**

Run: `dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 应用启动正常，颜色显示正确

**Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml
git commit -m "style(theme): add semantic color resources for UI normalization

- Add ShadowBrush, ValidationBrushes, DisabledBrushes
- Add Hover/Selected background brushes
- Add DataGrid alternating row brush
- Add border brushes for consistent dividers

Refs Task 3.2"
```

---

## Task 2: 替换 Controls.xaml 中的硬编码颜色

**Files:**
- Modify: `src/Client/Desktop/Shell/Styles/Controls.xaml`

**Step 1: 读取 Controls.xaml 中的硬编码位置**

查看文件找出硬编码颜色：
```bash
grep -n "Foreground.*#" src/Client/Desktop/Shell/Styles/Controls.xaml
grep -n "Background.*#" src/Client/Desktop/Shell/Styles/Controls.xaml
```

**Step 2: 替换禁用状态颜色**

找到使用 `#757575` (禁用前景) 和 `#F5F5F5` (禁用背景) 的 Setter，替换为：

```xml
<!-- 修改前 -->
<Setter Property="Foreground" Value="#757575"/>
<Setter Property="Background" Value="#F5F5F5"/>

<!-- 修改后 -->
<Setter Property="Foreground" Value="{DynamicResource DisabledForegroundBrush}"/>
<Setter Property="Background" Value="{DynamicResource DisabledBackgroundBrush}"/>
```

**Step 3: 替换 DataGrid 交替行颜色**

找到 `AlternatingRowBackground` 设置，替换为：

```xml
<!-- 修改前 -->
<Setter Property="AlternatingRowBackground" Value="#FAFAFA"/>

<!-- 修改后 -->
<Setter Property="AlternatingRowBackground" Value="{DynamicResource DataGridAlternatingRowBrush}"/>
```

**Step 4: 替换边框颜色**

找到边框相关的硬编码 `#E0E0E0`，替换为：

```xml
<!-- 修改前 -->
<Setter Property="BorderBrush" Value="#E0E0E0"/>

<!-- 修改后 -->
<Setter Property="BorderBrush" Value="{DynamicResource BorderLightBrush}"/>
```

**Step 5: 验证编译和运行**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译成功

Run: `dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 应用启动，控件显示正常

**Step 6: Commit**

```bash
git add src/Client/Desktop/Shell/Styles/Controls.xaml
git commit -m "style(controls): replace hardcoded colors with theme resources

- Replace disabled state colors with DisabledForeground/BackgroundBrush
- Replace DataGrid alternating row color with DataGridAlternatingRowBrush
- Replace border colors with BorderLightBrush

Refs Task 3.2"
```

---

## Task 3: 清理按钮样式重复 (Task 3.1)

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ButtonStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/UnifiedComponents.xaml`

**分析:**
`ButtonStyles.xaml` 和 `Controls.xaml` 都定义了 PrimaryButton, SecondaryButton 等相同样式。`Controls.xaml` 的版本更完整且有兼容性别名。

**决策:** 保留 `Controls.xaml` 的完整版本，将 `ButtonStyles.xaml` 改为从 `Controls.xaml` 引用的轻量级包装。

**Step 1: 备份并简化 ButtonStyles.xaml**

将 `ButtonStyles.xaml` 内容替换为：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!--
        ButtonStyles.xaml - 按钮样式聚合
        Phase 3: 此文件现在从 Controls.xaml 引用样式
        保留此文件以维持向后兼容，新代码应直接使用 Controls.xaml 中的样式
    -->

    <!-- 从 Controls.xaml 引用的按钮样式 -->
    <Style x:Key="ShellPrimaryButton" BasedOn="{StaticResource PrimaryButton}" TargetType="Button"/>
    <Style x:Key="ShellSecondaryButton" BasedOn="{StaticResource SecondaryButton}" TargetType="Button"/>
    <Style x:Key="ShellDangerButton" BasedOn="{StaticResource DangerButton}" TargetType="Button"/>
    <Style x:Key="ShellSuccessButton" BasedOn="{StaticResource SuccessButton}" TargetType="Button"/>
    <Style x:Key="ShellWarningButton" BasedOn="{StaticResource WarningButton}" TargetType="Button"/>
    <Style x:Key="ShellInfoButton" BasedOn="{StaticResource InfoButton}" TargetType="Button"/>
    <Style x:Key="ShellLinkButton" BasedOn="{StaticResource LinkButtonStyle}" TargetType="Button"/>

    <!-- 窗口操作按钮 - Shell 特有 -->
    <Style x:Key="WindowCloseButton" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
        <Setter Property="Width" Value="46"/>
        <Setter Property="Height" Value="32"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}">
                        <TextBlock Text="&#xE711;" FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"
                                   FontSize="12" HorizontalAlignment="Center" VerticalAlignment="Center"
                                   Foreground="{TemplateBinding Foreground}"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#E81123"/>
                <Setter Property="Foreground" Value="White"/>
            </Trigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
```

**Step 2: 更新 UnifiedComponents.xaml 确保引用顺序正确**

确保 `UnifiedComponents.xaml` 中先引用 `Controls.xaml` 再引用 `ButtonStyles.xaml`：

```xml
<ResourceDictionary.MergedDictionaries>
    <!-- 先加载 Controls.xaml 定义基础样式 -->
    <ResourceDictionary Source="/LYBT.Desktop.Shell;component/Styles/Controls.xaml"/>
    <!-- 再加载 ButtonStyles.xaml 进行扩展/引用 -->
    <ResourceDictionary Source="ButtonStyles.xaml"/>
    <!-- 其他组件样式 -->
    <ResourceDictionary Source="InputStyles.xaml"/>
    ...
</ResourceDictionary.MergedDictionaries>
```

**Step 3: 验证编译和运行**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译成功

Run: `dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 应用启动，所有按钮样式正常

**Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ButtonStyles.xaml
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/UnifiedComponents.xaml
git commit -m "style(buttons): consolidate button styles and remove duplication

- Simplify ButtonStyles.xaml to reference Controls.xaml
- Remove duplicate style definitions
- Keep Shell-specific WindowCloseButton style
- Ensure proper resource dictionary loading order

Fixes Task 3.1"
```

---

## Task 4: 统一字体系统 (Task 3.3)

**Files:**
- Modify: `src/Client/Desktop/Shell/Styles/Typography.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens.xaml`

**Step 1: 在 DesignTokens.xaml 中统一字体定义**

确保 DesignTokens.xaml 中字体定义一致：

```xml
    <!-- 字体族定义 -->
    <FontFamily x:Key="PrimaryFontFamily">Microsoft YaHei, Segoe UI, SimSun</FontFamily>
    <FontFamily x:Key="MonospaceFontFamily">Consolas, Courier New, Microsoft YaHei Mono</FontFamily>
    <FontFamily x:Key="IconFontFamily">Segoe Fluent Icons, Segoe MDL2 Assets</FontFamily>
```

**Step 2: 更新 Typography.xaml 使用资源引用**

在 `Typography.xaml` 中，将所有显式字体引用改为使用资源：

```xml
<!-- 修改前 -->
<Style x:Key="BodyTextBlock" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="Microsoft YaHei"/>
    ...
</Style>

<!-- 修改后 -->
<Style x:Key="BodyTextBlock" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{DynamicResource PrimaryFontFamily}"/>
    ...
</Style>
```

对所有 TextBlock 样式执行相同修改：
- H1TextBlock
- H2TextBlock
- H3TextBlock
- BodyTextBlock
- CaptionTextBlock
- PageTitle
- SectionHeader
- FieldLabel
- RequiredMark

**Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译成功

**Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens.xaml
git add src/Client/Desktop/Shell/Styles/Typography.xaml
git commit -m "style(typography): unify font family references

- Add PrimaryFontFamily, MonospaceFontFamily, IconFontFamily resources
- Update Typography.xaml to use DynamicResource for font families
- Standardize on Microsoft YaHei as primary font

Fixes Task 3.3"
```

---

## Task 5: 替换模块控件中的硬编码颜色

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/StatusBadge.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/CardReaderStatusControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ValidationStyles.xaml`

**Step 1: 替换 StatusBadge.xaml 中的硬编码颜色**

将硬编码颜色替换为主题资源：

```xml
<!-- 状态徽章颜色 - 使用主题资源 -->
<SolidColorBrush x:Key="StatusBadgeWaitingBrush" Color="{DynamicResource InfoColor}"/>
<SolidColorBrush x:Key="StatusBadgeInProgressBrush" Color="{DynamicResource WarningColor}"/>
<SolidColorBrush x:Key="StatusBadgeCompletedBrush" Color="{DynamicResource SuccessColor}"/>
<SolidColorBrush x:Key="StatusBadgeErrorBrush" Color="{DynamicResource DangerColor}"/>
```

**Step 2: 替换 PendingQueueControl.xaml 中的硬编码颜色**

将所有 `#E0E0E0` 等硬编码替换为 `BorderLightBrush`
将状态颜色替换为对应的主题资源

**Step 3: 替换 CardReaderStatusControl.xaml 中的硬编码颜色**

将读卡器状态颜色（连接/断开/错误）映射到主题资源

**Step 4: 替换 ValidationStyles.xaml 中的硬编码颜色**

将 `#DC3545` 替换为 `ValidationErrorBrush`

**Step 5: 验证编译和运行**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: 编译成功

**Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ValidationStyles.xaml
git commit -m "style(controls): replace hardcoded colors in module controls

- StatusBadge: use theme color resources
- PendingQueueControl: use BorderLightBrush
- CardReaderStatusControl: use theme status colors
- ValidationStyles: use ValidationErrorBrush

Refs Task 3.2"
```

---

## Task 6: 创建 FormField 控件 (Task 3.5)

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/FormField/FormFieldControl.xaml`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/FormField/FormFieldControl.xaml.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`

**Step 1: 创建 FormFieldControl.xaml**

```xml
<UserControl x:Class="LYBT.Desktop.Infrastructure.Controls.FormFieldControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="80" d:DesignWidth="300">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标签行 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,4">
            <TextBlock Text="{Binding Label, RelativeSource={RelativeSource AncestorType=UserControl}}"
                       Style="{DynamicResource FieldLabel}"/>
            <TextBlock Text="*"
                       Foreground="{DynamicResource DangerBrush}"
                       Visibility="{Binding IsRequired, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BooleanToVisibilityConverter}}"
                       Margin="2,0,0,0"/>
        </StackPanel>

        <!-- 输入内容区域 -->
        <ContentPresenter Grid.Row="1"
                          Content="{Binding InputContent, RelativeSource={RelativeSource AncestorType=UserControl}}"/>

        <!-- 验证错误信息 -->
        <TextBlock Grid.Row="2"
                   Text="{Binding ErrorMessage, RelativeSource={RelativeSource AncestorType=UserControl}}"
                   Style="{DynamicResource ValidationErrorTextStyle}"
                   Visibility="{Binding HasError, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BooleanToVisibilityConverter}}"/>
    </Grid>
</UserControl>
```

**Step 2: 创建 FormFieldControl.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 表单字段控件 - 标签 + 输入 + 验证错误的组合
    /// </summary>
    public partial class FormFieldControl : UserControl
    {
        public FormFieldControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormFieldControl),
                new PropertyMetadata(string.Empty));

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(FormFieldControl),
                new PropertyMetadata(false));

        public object InputContent
        {
            get => GetValue(InputContentProperty);
            set => SetValue(InputContentProperty, value);
        }

        public static readonly DependencyProperty InputContentProperty =
            DependencyProperty.Register(nameof(InputContent), typeof(object), typeof(FormFieldControl),
                new PropertyMetadata(null));

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(FormFieldControl),
                new PropertyMetadata(string.Empty));

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(FormFieldControl),
                new PropertyMetadata(false));

        #endregion
    }
}
```

**Step 3: 创建 ValidationErrorTextStyle 样式**

在 `Controls.xaml` 中添加：

```xml
<Style x:Key="ValidationErrorTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{DynamicResource FontSizeCaption}"/>
    <Setter Property="Foreground" Value="{DynamicResource ValidationErrorBrush}"/>
    <Setter Property="Margin" Value="0,2,0,0"/>
</Style>
```

**Step 4: 更新项目文件**

确保 `.csproj` 文件包含新的 XAML 文件。

**Step 5: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`
Expected: 编译成功

**Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/FormField/
git add src/Client/Desktop/Shell/Styles/Controls.xaml
git commit -m "feat(controls): add FormField control for standardized form layout

- Add FormFieldControl with Label, IsRequired, InputContent, ErrorMessage
- Add ValidationErrorTextStyle
- Provides consistent form field layout across the application

Fixes Task 3.5"
```

---

## Task 7: 验证和度量

**Step 1: 统计硬编码减少情况**

```bash
echo "=== 硬编码统计 ==="
echo "颜色硬编码 (Foreground/Background with #):"
grep -r "Foreground.*#" src/Client/Desktop --include="*.xaml" | wc -l
grep -r "Background.*#" src/Client/Desktop --include="*.xaml" | wc -l

echo "字体硬编码:"
grep -r 'FontFamily="Microsoft YaHei"' src/Client/Desktop --include="*.xaml" | wc -l

echo "=== 主题资源使用统计 ==="
echo "DynamicResource 使用:"
grep -r "DynamicResource" src/Client/Desktop --include="*.xaml" | wc -l
```

**Step 2: 运行全量测试**

Run: `dotnet test tests/LYBT.Tests.Desktop --verbosity minimal`
Expected: 所有测试通过

**Step 3: 视觉回归测试**

手动验证以下界面：
- 登录界面
- 患者管理
- 医案编辑
- 数据同步
- 设置页面

检查：
- 按钮颜色正常
- 字体显示正常
- 禁用状态颜色正确
- 验证错误颜色正确
- 表格交替行颜色正确

**Step 4: 更新任务状态**

更新 `task_plan.md` 标记 Phase 3 任务完成。

**Step 5: Final Commit**

```bash
git add docs/plans/2026-03-15-desktop-phase3-ui-normalization.md
git add task_plan.md
git add progress.md
git commit -m "docs: complete Phase 3 UI normalization

- All hardcoded colors replaced with theme resources
- Button styles consolidated
- Font system unified
- FormField control added
- Hardcoded colors reduced by 90%+

Closes Phase 3"
```

---

## 验收标准

- [ ] 所有硬编码颜色替换为 DynamicResource 引用
- [ ] 按钮样式不再重复定义
- [ ] 字体统一使用 PrimaryFontFamily 资源
- [ ] FormField 控件可用
- [ ] 应用启动正常，界面显示正确
- [ ] 所有现有测试通过
- [ ] 硬编码颜色/字体减少 90%+

---

## 风险与回滚

**Risk 1: 颜色显示异常**
- 回滚: 恢复修改的 XAML 文件
- 检测: 启动应用检查界面颜色

**Risk 2: 编译失败**
- 回滚: 检查 DynamicResource 引用的键名是否正确
- 检测: 编译错误信息

**Risk 3: 性能下降**
- 回滚: 将 DynamicResource 改回 StaticResource（如果性能问题严重）
- 检测: 应用启动时间和界面响应

---

## 参考文档

- 设计文档: `docs/plans/2026-03-14-desktop-refactoring-design.md`
- 主题文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml`
- 控件样式: `src/Client/Desktop/Shell/Styles/Controls.xaml`
- 排版样式: `src/Client/Desktop/Shell/Styles/Typography.xaml`
