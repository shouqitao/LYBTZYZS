# LYBTZYZS Desktop端UI重构 Phase 4 - 现代化UI设计 PRD

**文档版本**: v1.0
**创建日期**: 2025-11-04
**Epic Issue**: #1814 (待创建)
**优先级**: 🟢 P2
**预计工期**: 1-2个月（160小时）
**依赖**: Phase 3完成（#1810）

---

## 📋 执行摘要

### 目标
标准化原生WPF组件，提升UI一致性和无障碍体验，遵循MVP原则（不引入第三方主题控件）。

### 范围
1. **统一样式库** - 创建Styles/Themes.xaml标准化组件样式
2. **中医文化配色主题** - 通过ResourceDictionary实现专业配色
3. **组件标准化** - DataGrid、TextBox、ComboBox、Dialog等
4. **主题切换** - 亮色/暗色主题动态切换
5. **无障碍改进** - 键盘导航、屏幕阅读器、对比度优化

### 成功指标
- ✅ UI一致性提升100%（所有组件遵循统一设计规范）
- ✅ 用户视觉满意度提升70%
- ✅ 无障碍得分符合WCAG AA标准
- ✅ 代码维护成本降低20%（统一样式库）

---

## 1. 设计系统

### 1.1 原生WPF组件标准化方案

**核心理念**:
- ✅ 遵循MVP原则（Constitution约束：先不引入第三方主题控件）
- ✅ 充分利用WPF内置控件能力
- ✅ 避免外部依赖和版本兼容问题
- ✅ 通过ResourceDictionary和ControlTemplate实现主题定制
- ✅ 性能更可控，调试更简单

**实施策略**:
- 创建统一的样式库（Styles/Themes.xaml）
- 使用ControlTemplate重新设计标准控件外观
- 通过Behavior和AttachedProperty扩展交互能力

### 1.2 中医文化配色方案

**配色设计**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/Colors.xaml -->

<ResourceDictionary>
    <!-- Primary Color - 深青色（代表中医"青主肝"） -->
    <Color x:Key="PrimaryColor">#006B5F</Color>
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>

    <!-- Secondary Color - 朱红色（代表"赤主心"，用于强调和操作按钮） -->
    <Color x:Key="SecondaryColor">#C8302A</Color>
    <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource SecondaryColor}"/>

    <!-- Accent Color - 温润土黄（代表"土主脾"） -->
    <Color x:Key="AccentColor">#D4A574</Color>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>

    <!-- Background - 亮色模式 -->
    <Color x:Key="LightBackgroundColor">#FAFAFA</Color>
    <SolidColorBrush x:Key="LightBackgroundBrush" Color="{StaticResource LightBackgroundColor}"/>

    <!-- Background - 暗色模式 -->
    <Color x:Key="DarkBackgroundColor">#121212</Color>
    <SolidColorBrush x:Key="DarkBackgroundBrush" Color="{StaticResource DarkBackgroundColor}"/>

    <!-- Text Colors -->
    <Color x:Key="PrimaryTextColor">#212121</Color>
    <Color x:Key="SecondaryTextColor">#757575</Color>
    <Color x:Key="DisabledTextColor">#BDBDBD</Color>

    <SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource PrimaryTextColor}"/>
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource SecondaryTextColor}"/>
    <SolidColorBrush x:Key="DisabledTextBrush" Color="{StaticResource DisabledTextColor}"/>
</ResourceDictionary>
```

**Typography（字体排版）**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/Typography.xaml -->

<ResourceDictionary>
    <!-- Font Families -->
    <FontFamily x:Key="PrimaryFont">Microsoft YaHei UI</FontFamily>
    <FontFamily x:Key="TitleFont">Microsoft YaHei UI Bold</FontFamily>
    <FontFamily x:Key="MonospaceFont">Consolas</FontFamily>

    <!-- Font Sizes -->
    <system:Double x:Key="FontSizeSmall">12</system:Double>
    <system:Double x:Key="FontSizeNormal">14</system:Double>
    <system:Double x:Key="FontSizeMedium">16</system:Double>
    <system:Double x:Key="FontSizeLarge">18</system:Double>
    <system:Double x:Key="FontSizeXLarge">24</system:Double>

    <!-- Text Styles -->
    <Style x:Key="H1TextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource TitleFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeXLarge}"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryTextBrush}"/>
    </Style>

    <Style x:Key="H2TextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource TitleFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeLarge}"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryTextBrush}"/>
    </Style>

    <Style x:Key="BodyTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryTextBrush}"/>
    </Style>
</ResourceDictionary>
```

---

## 2. 组件标准化

### 2.1 DataGrid标准化

**目标**: 统一列样式、分页控件、筛选控件

**实现**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/DataGridStyles.xaml -->

<ResourceDictionary>
    <!-- Standard DataGrid Style -->
    <Style x:Key="StandardDataGridStyle" TargetType="DataGrid">
        <Setter Property="AutoGenerateColumns" Value="False"/>
        <Setter Property="CanUserAddRows" Value="False"/>
        <Setter Property="CanUserDeleteRows" Value="False"/>
        <Setter Property="CanUserResizeRows" Value="False"/>
        <Setter Property="SelectionMode" Value="Single"/>
        <Setter Property="SelectionUnit" Value="FullRow"/>
        <Setter Property="GridLinesVisibility" Value="Horizontal"/>
        <Setter Property="HeadersVisibility" Value="Column"/>
        <Setter Property="Background" Value="{StaticResource LightBackgroundBrush}"/>
        <Setter Property="RowBackground" Value="White"/>
        <Setter Property="AlternatingRowBackground" Value="#F5F5F5"/>
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="RowHeight" Value="40"/>
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}"/>

        <!-- Column Header Style -->
        <Setter Property="ColumnHeaderStyle">
            <Setter.Value>
                <Style TargetType="DataGridColumnHeader">
                    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
                    <Setter Property="Foreground" Value="White"/>
                    <Setter Property="FontWeight" Value="Bold"/>
                    <Setter Property="Height" Value="45"/>
                    <Setter Property="Padding" Value="10,0"/>
                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
                    <Setter Property="VerticalContentAlignment" Value="Center"/>
                </Style>
            </Setter.Value>
        </Setter>

        <!-- Cell Style -->
        <Setter Property="CellStyle">
            <Setter.Value>
                <Style TargetType="DataGridCell">
                    <Setter Property="Padding" Value="10,0"/>
                    <Setter Property="BorderThickness" Value="0"/>
                    <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
                    <Style.Triggers>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter Property="Background" Value="{StaticResource AccentBrush}"/>
                            <Setter Property="Foreground" Value="White"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

**分页控件**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Controls/PaginationControl.xaml -->

<UserControl x:Class="LYBT.DesktopApp.Controls.PaginationControl">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="首页" Command="{Binding FirstPageCommand}"/>
        <Button Content="上一页" Command="{Binding PreviousPageCommand}"/>
        <TextBlock Text="{Binding CurrentPage}" VerticalAlignment="Center" Margin="10,0"/>
        <TextBlock Text="/" VerticalAlignment="Center" Margin="5,0"/>
        <TextBlock Text="{Binding TotalPages}" VerticalAlignment="Center" Margin="0,0,10,0"/>
        <Button Content="下一页" Command="{Binding NextPageCommand}"/>
        <Button Content="末页" Command="{Binding LastPageCommand}"/>
    </StackPanel>
</UserControl>
```

### 2.2 TextBox/ComboBox标准化

**目标**: 统一验证错误显示、占位符文本、帮助提示

**实现**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/InputStyles.xaml -->

<ResourceDictionary>
    <!-- Standard TextBox Style -->
    <Style x:Key="StandardTextBoxStyle" TargetType="TextBox">
        <Setter Property="Height" Value="35"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}"/>
        <Setter Property="BorderBrush" Value="#CCCCCC"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Background" Value="White"/>

        <!-- Validation Error Template -->
        <Setter Property="Validation.ErrorTemplate">
            <Setter.Value>
                <ControlTemplate>
                    <DockPanel>
                        <Border BorderBrush="Red" BorderThickness="2" DockPanel.Dock="Top">
                            <AdornedElementPlaceholder/>
                        </Border>
                        <TextBlock Text="{Binding [0].ErrorContent}"
                                   Foreground="Red"
                                   FontSize="12"
                                   Margin="0,2,0,0"/>
                    </DockPanel>
                </ControlTemplate>
            </Setter.Value>
        </Setter>

        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
            </Trigger>
            <Trigger Property="IsFocused" Value="True">
                <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
                <Setter Property="BorderThickness" Value="2"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Background" Value="#F0F0F0"/>
                <Setter Property="Foreground" Value="{StaticResource DisabledTextBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Watermark (Placeholder) Attached Property -->
    <Style x:Key="WatermarkedTextBoxStyle" TargetType="TextBox" BasedOn="{StaticResource StandardTextBoxStyle}">
        <!-- 使用AttachedProperty实现Watermark功能 -->
    </Style>
</ResourceDictionary>
```

### 2.3 Button标准化

**目标**: 统一按钮样式、hover效果、禁用状态

**实现**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/ButtonStyles.xaml -->

<ResourceDictionary>
    <!-- Primary Button Style -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
        <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}"/>
        <Setter Property="Height" Value="35"/>
        <Setter Property="MinWidth" Value="80"/>
        <Setter Property="Padding" Value="15,5"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>

        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>

        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#008B7D"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="#005A52"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Background" Value="#CCCCCC"/>
                <Setter Property="Foreground" Value="#888888"/>
                <Setter Property="Cursor" Value="Arrow"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Secondary Button Style -->
    <Style x:Key="SecondaryButtonStyle" TargetType="Button" BasedOn="{StaticResource PrimaryButtonStyle}">
        <Setter Property="Background" Value="White"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
    </Style>
</ResourceDictionary>
```

### 2.4 Dialog标准化

**目标**: 统一模态对话框窗口样式、按钮布局、标题栏

**实现**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/DialogStyles.xaml -->

<ResourceDictionary>
    <!-- Standard Dialog Window Style -->
    <Style x:Key="StandardDialogWindowStyle" TargetType="Window">
        <Setter Property="WindowStyle" Value="None"/>
        <Setter Property="AllowsTransparency" Value="True"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="ResizeMode" Value="NoResize"/>
        <Setter Property="SizeToContent" Value="WidthAndHeight"/>

        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Window">
                    <Border Background="White"
                            BorderBrush="{StaticResource PrimaryBrush}"
                            BorderThickness="1"
                            CornerRadius="8"
                            Effect="{StaticResource DropShadowEffect}">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="50"/> <!-- Title Bar -->
                                <RowDefinition Height="*"/>  <!-- Content -->
                                <RowDefinition Height="60"/> <!-- Button Bar -->
                            </Grid.RowDefinitions>

                            <!-- Title Bar -->
                            <Border Grid.Row="0"
                                    Background="{StaticResource PrimaryBrush}"
                                    CornerRadius="8,8,0,0">
                                <Grid>
                                    <TextBlock Text="{TemplateBinding Title}"
                                               Foreground="White"
                                               FontSize="{StaticResource FontSizeMedium}"
                                               FontWeight="Bold"
                                               VerticalAlignment="Center"
                                               Margin="20,0"/>
                                    <Button Content="×"
                                            HorizontalAlignment="Right"
                                            Style="{StaticResource CloseButtonStyle}"
                                            Command="{Binding CloseCommand}"/>
                                </Grid>
                            </Border>

                            <!-- Content -->
                            <ContentPresenter Grid.Row="1" Margin="20"/>

                            <!-- Button Bar -->
                            <Border Grid.Row="2"
                                    Background="#F5F5F5"
                                    CornerRadius="0,0,8,8">
                                <StackPanel Orientation="Horizontal"
                                            HorizontalAlignment="Right"
                                            Margin="20,0">
                                    <Button Content="确定"
                                            Style="{StaticResource PrimaryButtonStyle}"
                                            Command="{Binding OKCommand}"
                                            Margin="0,0,10,0"/>
                                    <Button Content="取消"
                                            Style="{StaticResource SecondaryButtonStyle}"
                                            Command="{Binding CancelCommand}"/>
                                </StackPanel>
                            </Border>
                        </Grid>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

---

## 3. 主题切换

### 3.1 亮色/暗色主题动态切换

**实现思路**:
```csharp
// src/Client/Desktop/LYBT.DesktopApp/Services/ThemeService.cs

public class ThemeService : IThemeService
{
    private readonly ResourceDictionary _lightTheme;
    private readonly ResourceDictionary _darkTheme;

    public ThemeService()
    {
        _lightTheme = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Styles/LightTheme.xaml")
        };

        _darkTheme = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Styles/DarkTheme.xaml")
        };
    }

    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;

        // 移除当前主题
        var existingTheme = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);

        if (existingTheme != null)
        {
            app.Resources.MergedDictionaries.Remove(existingTheme);
        }

        // 应用新主题
        var newTheme = theme == AppTheme.Light ? _lightTheme : _darkTheme;
        app.Resources.MergedDictionaries.Add(newTheme);

        // 保存用户偏好
        Properties.Settings.Default.Theme = theme.ToString();
        Properties.Settings.Default.Save();
    }
}

public enum AppTheme
{
    Light,
    Dark
}
```

**暗色主题配色**:
```xml
<!-- src/Client/Desktop/LYBT.DesktopApp/Styles/DarkTheme.xaml -->

<ResourceDictionary>
    <!-- Background -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="#121212"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="#1E1E1E"/>

    <!-- Text -->
    <SolidColorBrush x:Key="PrimaryTextBrush" Color="#E0E0E0"/>
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="#AAAAAA"/>

    <!-- Primary Color保持不变（中医色彩） -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#006B5F"/>
</ResourceDictionary>
```

---

## 4. 无障碍改进

### 4.1 键盘导航

**目标**: 所有操作支持键盘快捷键，Tab顺序符合逻辑

**实现**:
```xml
<!-- 为所有Button添加快捷键 -->
<Button Content="保存(_S)" Command="{Binding SaveCommand}">
    <Button.InputBindings>
        <KeyBinding Key="S" Modifiers="Ctrl" Command="{Binding SaveCommand}"/>
    </Button.InputBindings>
</Button>

<!-- 为Window添加全局快捷键 -->
<Window.InputBindings>
    <KeyBinding Key="Escape" Command="{Binding CancelCommand}"/>
    <KeyBinding Key="S" Modifiers="Ctrl" Command="{Binding SaveCommand}"/>
    <KeyBinding Key="N" Modifiers="Ctrl" Command="{Binding NewCommand}"/>
</Window.InputBindings>

<!-- 设置Tab顺序 -->
<TextBox TabIndex="1" .../>
<ComboBox TabIndex="2" .../>
<Button TabIndex="3" .../>
```

### 4.2 屏幕阅读器支持

**目标**: 所有按钮和输入框添加AutomationProperties

**实现**:
```xml
<!-- 为Button添加AutomationProperties -->
<Button Content="保存"
        AutomationProperties.Name="保存按钮"
        AutomationProperties.HelpText="保存当前数据"/>

<!-- 为TextBox添加AutomationProperties -->
<TextBox AutomationProperties.Name="患者姓名输入框"
         AutomationProperties.HelpText="请输入患者姓名"/>

<!-- 为DataGrid添加AutomationProperties -->
<DataGrid AutomationProperties.Name="患者列表"
          AutomationProperties.HelpText="显示所有患者信息的表格"/>
```

### 4.3 对比度优化

**目标**: 文本对比度≥4.5:1（WCAG AA标准）

**验证**:
- Primary Text (#212121) on White (#FFFFFF): 对比度 = 16.1:1 ✅
- Secondary Text (#757575) on White (#FFFFFF): 对比度 = 4.6:1 ✅
- Primary Button (#006B5F) with White text: 对比度 = 7.2:1 ✅

---

## 5. 实施计划

### 5.1 Phase 4 Timeline

| 周次 | 任务 | 工作量 | 依赖 |
|------|------|--------|------|
| Week 1-2 | 创建统一样式库（Colors、Typography、DataGrid、Input、Button、Dialog） | 40小时 | Phase 3完成 |
| Week 3-4 | 应用样式到所有现有界面（39个XAML视图） | 40小时 | Week 1-2完成 |
| Week 5-6 | 实现主题切换功能（亮色/暗色主题） | 32小时 | Week 3-4完成 |
| Week 7-8 | 无障碍改进（键盘导航、屏幕阅读器、对比度） | 48小时 | Week 5-6完成 |

**总工期**: 8周（160小时）

---

## 6. 验收标准

### 6.1 样式库完整性
- [ ] Colors.xaml已创建（Primary、Secondary、Accent、Background、Text颜色定义）
- [ ] Typography.xaml已创建（字体、字号、文本样式定义）
- [ ] DataGridStyles.xaml已创建（StandardDataGridStyle）
- [ ] InputStyles.xaml已创建（TextBox、ComboBox样式）
- [ ] ButtonStyles.xaml已创建（Primary、Secondary按钮样式）
- [ ] DialogStyles.xaml已创建（StandardDialogWindowStyle）

### 6.2 样式应用覆盖
- [ ] 所有DataGrid应用StandardDataGridStyle
- [ ] 所有TextBox应用StandardTextBoxStyle
- [ ] 所有Button应用PrimaryButtonStyle或SecondaryButtonStyle
- [ ] 所有Dialog应用StandardDialogWindowStyle
- [ ] 39个XAML视图全部应用统一样式

### 6.3 主题切换
- [ ] 亮色主题完整定义
- [ ] 暗色主题完整定义
- [ ] ThemeService实现并注册到DI容器
- [ ] 主题切换功能实现（设置界面）
- [ ] 用户偏好保存和加载
- [ ] 主题切换后UI立即更新

### 6.4 无障碍
- [ ] 所有按钮支持键盘快捷键
- [ ] Tab顺序符合逻辑（从上到下、从左到右）
- [ ] 所有按钮添加AutomationProperties.Name
- [ ] 所有输入框添加AutomationProperties.HelpText
- [ ] 文本对比度≥4.5:1（WCAG AA标准）
- [ ] 重要按钮对比度≥7:1（WCAG AAA标准）
- [ ] 屏幕阅读器测试通过

### 6.5 性能
- [ ] UI响应时间<100ms
- [ ] 主题切换时间<1秒
- [ ] 无内存泄漏

---

## 7. 相关文档

- **Phase 1 PRD**: `docs/requirements/ui-refactoring-phase1-prd.md`
- **Phase 2 PRD**: `docs/requirements/ui-refactoring-phase2-prd.md`
- **Phase 3 PRD**: `docs/requirements/ui-refactoring-phase3-prd.md`
- **重构计划**: `docs/reports/ui-ux-refactoring-plan-2025-11-04.md`
- **WPF文档**: [Microsoft WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- **WCAG标准**: [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

**文档状态**: ✅ 待创建GitHub Issues
**下一步**: 创建Phase 4的GitHub Issues（#1814 Epic + 7个子Issues）

**最后更新**: 2025-11-04
