# 就诊流程UI原型图文档

> **文档类型**: UI/UX详细设计规范
> **创建日期**: 2025-10-19
> **最后更新**: 2025-10-19
> **状态**: 设计完成，待开发实施
> **基准分辨率**: 1920x1080（推荐），最低支持1366x768
> **关联文档**: `clinical-workflow-ux-design-discussion.md`
> **Epic**: #1343 MVP就诊功能

---

## 📋 文档目的

本文档提供就诊流程所有UI界面的详细原型图和样式规范，确保设计与开发100%对齐。包括：

1. **HomeView** - 简单Dashboard（登录后首页）
2. **PatientSelectionDialog** - 患者选择对话框（已实现，需优化）
3. **ConsultationView** - 核心就诊界面（三段式布局）
4. **PrescriptionEditor** - 处方编辑器（三种录入方式）
5. **全局样式系统** - ResourceDictionary定义

所有布局图使用ASCII格式，精确标注尺寸、边距、颜色代码。

---

## 1️⃣ 全局样式系统（ResourceDictionary）

### 1.1 色彩规范（Styles/Colors.xaml）

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 主色调 -->
    <SolidColorBrush x:Key="PrimaryColor" Color="#2196F3"/>      <!-- 蓝色 -->
    <SolidColorBrush x:Key="PrimaryDarkColor" Color="#1976D2"/>  <!-- 深蓝 -->
    <SolidColorBrush x:Key="PrimaryLightColor" Color="#BBDEFB"/> <!-- 浅蓝 -->

    <!-- 辅助色 -->
    <SolidColorBrush x:Key="SecondaryColor" Color="#757575"/>    <!-- 灰色 -->
    <SolidColorBrush x:Key="SuccessColor" Color="#4CAF50"/>      <!-- 绿色 -->
    <SolidColorBrush x:Key="WarningColor" Color="#FF9800"/>      <!-- 橙色 -->
    <SolidColorBrush x:Key="ErrorColor" Color="#F44336"/>        <!-- 红色 -->

    <!-- 背景色 -->
    <SolidColorBrush x:Key="BackgroundColor" Color="#F5F5F5"/>   <!-- 浅灰背景 -->
    <SolidColorBrush x:Key="ContentBgColor" Color="#FFFFFF"/>    <!-- 白色内容背景 -->
    <SolidColorBrush x:Key="SurfaceColor" Color="#FAFAFA"/>      <!-- 表面色 -->

    <!-- 文本色 -->
    <SolidColorBrush x:Key="TextPrimaryColor" Color="#212121"/>  <!-- 主文本 -->
    <SolidColorBrush x:Key="TextSecondaryColor" Color="#757575"/><!-- 次要文本 -->
    <SolidColorBrush x:Key="TextHintColor" Color="#BDBDBD"/>     <!-- 提示文本 -->

    <!-- 边框色 -->
    <SolidColorBrush x:Key="BorderColor" Color="#E0E0E0"/>       <!-- 默认边框 -->
    <SolidColorBrush x:Key="DividerColor" Color="#EEEEEE"/>      <!-- 分隔线 -->

    <!-- 交互状态色 -->
    <SolidColorBrush x:Key="HoverColor" Color="#E3F2FD"/>        <!-- 鼠标悬停 -->
    <SolidColorBrush x:Key="PressedColor" Color="#BBDEFB"/>      <!-- 按下状态 -->
    <SolidColorBrush x:Key="SelectedColor" Color="#BBDEFB"/>     <!-- 选中状态 -->
    <SolidColorBrush x:Key="FocusColor" Color="#2196F3"/>        <!-- 焦点边框 -->

    <!-- DataGrid斑马纹 -->
    <SolidColorBrush x:Key="AlternateRowColor" Color="#F9F9F9"/> <!-- 偶数行背景 -->
</ResourceDictionary>
```

**色彩使用规则**：
- **主色调（#2196F3）**：主要按钮、标题栏、选中状态
- **成功色（#4CAF50）**：成功提示、保存确认
- **警告色（#FF9800）**：警告提示、验证失败
- **错误色（#F44336）**：错误提示、必填项未填

---

### 1.2 字体排版规范（Styles/Typography.xaml）

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 标题样式 -->
    <Style x:Key="H1TextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="20"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
    </Style>

    <Style x:Key="H2TextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
    </Style>

    <Style x:Key="H3TextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
    </Style>

    <!-- 正文样式 -->
    <Style x:Key="BodyTextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="FontWeight" Value="Regular"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
        <Setter Property="LineHeight" Value="21"/>  <!-- 1.5倍行高 -->
    </Style>

    <!-- 说明文字样式 -->
    <Style x:Key="CaptionTextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="FontWeight" Value="Regular"/>
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryColor}"/>
    </Style>

    <!-- 提示文字样式 -->
    <Style x:Key="HintTextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Microsoft YaHei"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="FontWeight" Value="Regular"/>
        <Setter Property="Foreground" Value="{StaticResource TextHintColor}"/>
        <Setter Property="FontStyle" Value="Italic"/>
    </Style>
</ResourceDictionary>
```

**字体使用规则**：
- **H1（20px Bold）**：页面主标题（如"就诊主界面"）
- **H2（16px Bold）**：区块标题（如"诊断信息"、"处方录入"）
- **H3（14px SemiBold）**：子标题（如DataGrid列头）
- **Body（14px Regular）**：正文、输入框内容
- **Caption（12px Regular）**：说明文字、统计信息
- **Hint（12px Italic）**：占位符提示（Placeholder）

---

### 1.3 控件样式规范（Styles/Controls.xaml）

#### Button样式

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 主按钮样式（蓝色背景+白色文字） -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryColor}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="FontWeight" Value="Regular"/>
        <Setter Property="Height" Value="36"/>
        <Setter Property="MinWidth" Value="100"/>
        <Setter Property="Padding" Value="20,8"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderThickness="0"
                            CornerRadius="4">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryDarkColor}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryDarkColor}"/>
                            <Setter Property="Opacity" Value="0.9"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Background" Value="{StaticResource SecondaryColor}"/>
                            <Setter Property="Opacity" Value="0.5"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 次按钮样式（白色背景+蓝色边框） -->
    <Style x:Key="SecondaryButton" TargetType="Button">
        <Setter Property="Background" Value="White"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryColor}"/>
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryColor}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Height" Value="36"/>
        <Setter Property="MinWidth" Value="100"/>
        <Setter Property="Padding" Value="20,8"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{StaticResource HoverColor}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="{StaticResource PressedColor}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 成功按钮样式（绿色背景+白色文字） -->
    <Style x:Key="SuccessButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{StaticResource SuccessColor}"/>
    </Style>

    <!-- 警告按钮样式（红色背景+白色文字） -->
    <Style x:Key="DangerButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{StaticResource ErrorColor}"/>
    </Style>
</ResourceDictionary>
```

**Button尺寸规范**：
- **高度**：36px（标准）、32px（紧凑）
- **最小宽度**：100px
- **内边距**：左右20px，上下8px
- **圆角**：4px
- **字体**：14px Regular

---

#### TextBox样式

```xml
<!-- 标准文本框样式 -->
<Style x:Key="StandardTextBox" TargetType="TextBox">
    <Setter Property="Background" Value="White"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderColor}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Height" Value="36"/>
    <Setter Property="Padding" Value="10,8"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TextBox">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="2">
                    <ScrollViewer x:Name="PART_ContentHost"
                                  Margin="{TemplateBinding Padding}"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsFocused" Value="True">
                        <Setter Property="BorderBrush" Value="{StaticResource FocusColor}"/>
                        <Setter Property="BorderThickness" Value="2"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="BorderBrush" Value="{StaticResource PrimaryColor}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- 多行文本框样式 -->
<Style x:Key="MultilineTextBox" TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}">
    <Setter Property="TextWrapping" Value="Wrap"/>
    <Setter Property="AcceptsReturn" Value="True"/>
    <Setter Property="VerticalScrollBarVisibility" Value="Auto"/>
    <Setter Property="MinHeight" Value="80"/>
    <Setter Property="VerticalContentAlignment" Value="Top"/>
</Style>
```

**TextBox尺寸规范**：
- **单行高度**：36px
- **多行最小高度**：80px
- **内边距**：左右10px，上下8px
- **圆角**：2px
- **默认边框**：1px solid #E0E0E0
- **焦点边框**：2px solid #2196F3

---

#### DataGrid样式

```xml
<!-- 标准DataGrid样式 -->
<Style x:Key="StandardDataGrid" TargetType="DataGrid">
    <Setter Property="AutoGenerateColumns" Value="False"/>
    <Setter Property="CanUserAddRows" Value="False"/>
    <Setter Property="CanUserDeleteRows" Value="False"/>
    <Setter Property="IsReadOnly" Value="True"/>
    <Setter Property="SelectionMode" Value="Single"/>
    <Setter Property="GridLinesVisibility" Value="Horizontal"/>
    <Setter Property="HeadersVisibility" Value="Column"/>
    <Setter Property="Background" Value="White"/>
    <Setter Property="AlternatingRowBackground" Value="{StaticResource AlternateRowColor}"/>
    <Setter Property="RowHeight" Value="40"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderColor}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="HorizontalGridLinesBrush" Value="{StaticResource DividerColor}"/>
</Style>

<!-- DataGrid列头样式 -->
<Style x:Key="DataGridColumnHeaderStyle" TargetType="DataGridColumnHeader">
    <Setter Property="Background" Value="#E8E8E8"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryColor}"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Height" Value="44"/>
    <Setter Property="HorizontalContentAlignment" Value="Center"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Padding" Value="10,0"/>
</Style>

<!-- DataGrid单元格样式（居中） -->
<Style x:Key="CenteredCellStyle" TargetType="TextBlock">
    <Setter Property="HorizontalAlignment" Value="Center"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>

<!-- DataGrid单元格样式（左对齐） -->
<Style x:Key="LeftAlignedCellStyle" TargetType="TextBlock">
    <Setter Property="HorizontalAlignment" Value="Left"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="Padding" Value="10,0,0,0"/>
</Style>
```

**DataGrid尺寸规范**：
- **行高**：40px
- **列头高度**：44px
- **边框**：1px solid #E0E0E0
- **斑马纹**：偶数行 #F9F9F9
- **鼠标悬停**：#E3F2FD
- **选中行**：#BBDEFB

---

### 1.4 样式文件组织

```
src/Client/Desktop/Modules/LYBT.Desktop.Consultation/
├─ Styles/
│  ├─ Colors.xaml          # 色彩定义
│  ├─ Typography.xaml      # 字体排版
│  ├─ Controls.xaml        # 控件样式（Button、TextBox、DataGrid）
│  └─ Converters.xaml      # 值转换器（如BoolToVisibility）
└─ App.xaml                # 全局引用
```

**App.xaml引用示例**：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/Colors.xaml"/>
            <ResourceDictionary Source="Styles/Typography.xaml"/>
            <ResourceDictionary Source="Styles/Controls.xaml"/>
            <ResourceDictionary Source="Styles/Converters.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## 2️⃣ HomeView - 简单Dashboard

### 2.1 布局原型图（1920x1080基准）

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ 顶部栏（高度60px，背景#2196F3）                                                  │
│ ┌─────────────────┬────────────────────────────────────────────────────────┐ │
│ │ 中医诊疗系统     │                                  王医生 | 2025-10-19 │ 退出 │ │
│ │ (H1, White)     │                                  (14px, White)         │ │
│ └─────────────────┴────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ ┌─────────────────┬──────────────────────────────────────────────────────┐ │
│ │ 左侧导航菜单     │ 主内容区域（ContentControl，动态加载View）              │ │
│ │ (宽度200px)     │                                                       │ │
│ │                 │  ┌───────────────────────────────────────────────┐  │ │
│ │ ▶ 首页          │  │                                                │  │ │
│ │   患者管理      │  │ 欢迎使用中医诊疗系统                             │  │ │
│ │   就诊管理      │  │ (H1TextBlock, Margin=0,40,0,20)                │  │ │
│ │   处方管理      │  │                                                │  │ │
│ │   药材管理      │  │ 医生：王医生                                     │  │ │
│ │   验方管理      │  │ 当前日期：2025年10月19日 星期六                  │  │ │
│ │   统计报表      │  │ (BodyTextBlock, Margin=0,0,0,60)               │  │ │
│ │                 │  │                                                │  │ │
│ │                 │  │ ┌───────────────────────────────────────────┐ │  │ │
│ │                 │  │ │ 快速操作                                    │ │  │ │
│ │                 │  │ │ (H2TextBlock, Margin=0,0,0,20)             │ │  │ │
│ │                 │  │ ├───────────────────────────────────────────┤ │  │ │
│ │                 │  │ │                                            │ │  │ │
│ │                 │  │ │  [开始接诊]  (PrimaryButton, 150x50px)     │ │  │ │
│ │                 │  │ │  (Ctrl+N快捷键)                            │ │  │ │
│ │                 │  │ │                                            │ │  │ │
│ │                 │  │ └───────────────────────────────────────────┘ │  │ │
│ │                 │  │                                                │  │ │
│ │                 │  │ ┌───────────────────────────────────────────┐ │  │ │
│ │                 │  │ │ 今日统计（可选）                             │ │  │ │
│ │                 │  │ │ (H2TextBlock, Margin=0,40,0,20)            │ │  │ │
│ │                 │  │ ├───────────────────────────────────────────┤ │  │ │
│ │                 │  │ │                                            │ │  │ │
│ │                 │  │ │ 今日接诊：3人                               │ │  │ │
│ │                 │  │ │ 开具处方：3张                               │ │  │ │
│ │                 │  │ │ (CaptionTextBlock)                         │ │  │ │
│ │                 │  │ │                                            │ │  │ │
│ │                 │  │ └───────────────────────────────────────────┘ │  │ │
│ │                 │  │                                                │  │ │
│ │                 │  └───────────────────────────────────────────────┘  │ │
│ │                 │                                                       │ │
│ └─────────────────┴──────────────────────────────────────────────────────┘ │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 尺寸标注

| 元素 | 宽度 | 高度 | 边距/内边距 | 颜色 |
|-----|------|------|-----------|------|
| **顶部栏** | 100% | 60px | Padding=20,15 | Background=#2196F3 |
| **左侧菜单** | 200px | 100%-60px | - | Background=#FAFAFA |
| **主内容区** | 100%-200px | 100%-60px | Padding=40 | Background=#F5F5F5 |
| **开始接诊按钮** | 150px | 50px | Margin=0,20,0,0 | Background=#2196F3 |
| **快速操作卡片** | Auto | Auto | Padding=20, Margin=0,0,0,40 | Background=White, Border=1px #E0E0E0 |
| **今日统计卡片** | Auto | Auto | Padding=20, Margin=40,0,0,0 | Background=White, Border=1px #E0E0E0 |

### 2.3 交互逻辑

**导航流程**：
```
登录成功
    ↓
HomeView（Dashboard）显示
    ↓
点击[开始接诊]按钮 或 左侧菜单"就诊管理"
    ↓
NavigationService.NavigateTo<PatientSelectionViewModel>()
    ↓
PatientSelectionDialog显示
```

**快捷键**：
- `Ctrl+N`：开始接诊（等同于点击[开始接诊]按钮）
- `F5`：刷新今日统计（可选）

### 2.4 ViewModel伪代码

```csharp
public class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _doctorName;
    private DateTime _currentDate;
    private int _todayConsultationCount; // 可选

    public DelegateCommand StartConsultationCommand { get; }

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        StartConsultationCommand = new DelegateCommand(OnStartConsultation);

        // 初始化数据
        LoadTodayStatistics(); // 可选
    }

    private void OnStartConsultation()
    {
        _navigationService.NavigateTo<PatientSelectionViewModel>();
    }

    private async void LoadTodayStatistics()
    {
        // 可选：加载今日统计
        TodayConsultationCount = await _consultationRepository.GetTodayCountAsync();
    }
}
```

---

## 3️⃣ PatientSelectionDialog - 患者选择对话框（优化版）

### 3.1 布局原型图（800x600对话框）

```
┌──────────────────────────────────────────────────────────────────────────┐
│ 选择患者 (H2TextBlock, Margin=0,0,0,15)                                    │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│ ┌────────────────────────────────────────┬──────┬──────┬──────────────┐ │
│ │ [支持姓名/拼音码/手机号搜索]            │ 搜索 │ 刷新 │ [新建患者]   │ │
│ │ (HintTextBlock作为Placeholder)         │      │      │              │ │
│ └────────────────────────────────────────┴──────┴──────┴──────────────┘ │
│                                                                          │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │ 姓名      │ 性别 │ 年龄 │ 手机号码     │ 最近就诊      │             │ │
│ │ (居中)    │ (居中)│(居中)│ (居中)       │ (居中)        │             │ │
│ ├──────────┼──────┼──────┼──────────────┼───────────────┤             │ │
│ │ 张三      │ 男   │ 45岁 │ 138xxxx1234  │ 2025-10-15    │  ← 选中行  │ │
│ │           │      │      │              │               │  (#BBDEFB) │ │
│ ├──────────┼──────┼──────┼──────────────┼───────────────┤             │ │
│ │ 李四      │ 女   │ 32岁 │ 139xxxx5678  │ 2025-10-10    │  ← 偶数行  │ │
│ │           │      │      │              │               │  (#F9F9F9) │ │
│ ├──────────┼──────┼──────┼──────────────┼───────────────┤             │ │
│ │ 王五      │ 男   │ 58岁 │ 150xxxx9012  │ 2025-10-08    │  ← 奇数行  │ │
│ │           │      │      │              │               │  (White)   │ │
│ └──────────┴──────┴──────┴──────────────┴───────────────┘             │ │
│                                                                          │
│                                               [确定]   [取消]            │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.2 优化要点（Issue #1457代码基础上）

**需要优化的4处**：

1. **虚拟化优化** - 添加1行代码：
```xml
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          VirtualizingPanel.ScrollUnit="Pixel">
```

2. **拼音码提示增强** - 搜索框Placeholder：
```xml
<TextBox>
    <TextBox.Style>
        <Style TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}">
            <Style.Triggers>
                <Trigger Property="Text" Value="">
                    <Setter Property="Background">
                        <Setter.Value>
                            <VisualBrush Stretch="None" AlignmentX="Left">
                                <VisualBrush.Visual>
                                    <TextBlock Text="支持姓名/拼音码/手机号搜索"
                                               Foreground="{StaticResource TextHintColor}"
                                               FontStyle="Italic"
                                               Margin="10,0,0,0"/>
                                </VisualBrush.Visual>
                            </VisualBrush>
                        </Setter.Value>
                    </Setter>
                </Trigger>
            </Style.Triggers>
        </Style>
    </TextBox.Style>
</TextBox>
```

3. **新建患者功能开发** - 创建快速新建对话框：
```
QuickCreatePatientDialog（300x400对话框）
┌──────────────────────────────────────┐
│ 快速新建患者 (H2TextBlock)            │
├──────────────────────────────────────┤
│                                      │
│ 姓名（必填）：  [__________]          │
│ 性别（必填）：  ○男  ●女              │
│ 年龄：         [__________]          │
│ 手机号：       [__________]          │
│                                      │
│                    [保存]   [取消]    │
└──────────────────────────────────────┘
```

4. **布局对齐检查** - 与UI讨论文档一致，无需调整

---

## 4️⃣ ConsultationView - 核心就诊界面（三段式布局）

### 4.1 完整布局原型图（1920x1080基准）

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ ┌────────────────────────────────────────────────────────────────────────────────┐ │
│ │ 患者信息条（PatientInfoBar，高度60px，背景#BBDEFB，固定顶部）                      │ │
│ │ ┌───────────────────────────────────────────────────────────────────────────┐ │ │
│ │ │ 姓名：张三 | 性别：男 | 年龄：45岁 | 电话：138xxxx1234 | [查看历史就诊]      │ │ │
│ │ │ (BodyTextBlock, FontWeight=SemiBold, 左右Margin=20)                        │ │ │
│ │ └───────────────────────────────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────────────────────────────────────┐ │
│ │ 诊断区（Expander可折叠，背景White，Border=1px #E0E0E0，Margin=20,10）           │ │
│ │ ┌───────────────────────────────────────────────────────────────────────────┐ │ │
│ │ │ ▼ 诊断信息 (H2TextBlock, ExpanderHeader)                                   │ │ │
│ │ ├───────────────────────────────────────────────────────────────────────────┤ │ │
│ │ │                                                                            │ │ │
│ │ │ ┌─────────────────────────────┬─────────────────────────────────────────┐ │ │ │
│ │ │ │ 主诉（必填）：                │ 现病史（必填）：                         │ │ │ │
│ │ │ │ (H3TextBlock)               │ (H3TextBlock)                           │ │ │ │
│ │ │ │ ┌────────────────────────┐  │ ┌────────────────────────────────────┐ │ │ │ │
│ │ │ │ │ [头痛三日...]           │  │ │ [患者三日前出现头痛...]             │ │ │ │
│ │ │ │ │ (MultilineTextBox)      │  │ │ (MultilineTextBox)                  │ │ │ │
│ │ │ │ │ MinHeight=80px          │  │ │ MinHeight=80px                      │ │ │ │
│ │ │ │ └────────────────────────┘  │ └────────────────────────────────────┘ │ │ │ │
│ │ │ └─────────────────────────────┴─────────────────────────────────────────┘ │ │ │
│ │ │                                                                            │ │ │
│ │ │ ┌──────────────────────────────────────────────────────────────────────┐ │ │ │
│ │ │ │ 中医诊断（必填）：                                                      │ │ │ │
│ │ │ │ (H3TextBlock)                                                         │ │ │ │
│ │ │ │ ┌──────────────────────────────────────────────────────────────────┐ │ │ │ │
│ │ │ │ │ [肝郁脾虚...]                                                      │ │ │ │ │
│ │ │ │ │ (MultilineTextBox, MinHeight=60px)                                │ │ │ │ │
│ │ │ │ └──────────────────────────────────────────────────────────────────┘ │ │ │ │
│ │ │ └──────────────────────────────────────────────────────────────────────┘ │ │ │
│ │ │                                                                            │ │ │
│ │ │ ┌───────────────────────────────────────────────────────────────────────┐ │ │ │
│ │ │ │ ▶ 其他四诊（选填，默认折叠） (Expander)                                  │ │ │ │
│ │ │ │ ┌─────────────────────────────┬─────────────────────────────────────┐ │ │ │ │
│ │ │ │ │ 望诊：[TextBox]              │ 闻诊：[TextBox]                     │ │ │ │ │
│ │ │ │ ├─────────────────────────────┼─────────────────────────────────────┤ │ │ │ │
│ │ │ │ │ 问诊：[TextBox]              │ 切诊：[TextBox]                     │ │ │ │ │
│ │ │ │ ├─────────────────────────────┴─────────────────────────────────────┤ │ │ │ │
│ │ │ │ │ 治疗原则：[TextBox]                                                 │ │ │ │ │
│ │ │ │ └─────────────────────────────────────────────────────────────────┘ │ │ │ │
│ │ │ └───────────────────────────────────────────────────────────────────────┘ │ │ │
│ │ └───────────────────────────────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────────────────────────────────────┐ │
│ │ 处方区（主要工作区，背景White，Border=1px #E0E0E0，Margin=20,10,20,20）          │ │
│ │ ┌───────────────────────────────────────────────────────────────────────────┐ │ │
│ │ │ [📝 手工录入]  [📋 验方导入]  [🕐 历史复制]  (TabControl)                    │ │ │
│ │ │ ─────────────  ────────────  ─────────────  (选中Tab下划线#2196F3)        │ │ │
│ │ ├───────────────────────────────────────────────────────────────────────────┤ │ │
│ │ │                                                                            │ │ │
│ │ │ [处方录入内容 - 见第5节详细设计]                                            │ │ │
│ │ │                                                                            │ │ │
│ │ └───────────────────────────────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ 底部操作栏（固定底部，高度60px，背景#FAFAFA，Border-Top=1px #E0E0E0）              │
│ ┌────────────────────────────────────────────────────────────────────────────────┐ │
│ │ [保存草稿] [完成就诊] [打印处方] [取消]           最后保存：15:30 (右对齐)        │ │
│ │ (SecondaryButton) (SuccessButton) (SecondaryButton) (DangerButton)            │ │
│ └────────────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 尺寸标注

| 元素 | 宽度 | 高度 | 边距/内边距 | 颜色 |
|-----|------|------|-----------|------|
| **患者信息条** | 100% | 60px | Padding=20,15 | Background=#BBDEFB |
| **诊断区Expander** | 100%-40px | Auto | Margin=20,10, Padding=20 | Background=White, Border=1px #E0E0E0 |
| **主诉/现病史** | 50%-10px | MinHeight=80px | Margin=0,10,10,0 | - |
| **中医诊断** | 100% | MinHeight=60px | Margin=0,10,0,0 | - |
| **其他四诊Expander** | 100% | Auto | Margin=10,10,0,0 | - |
| **处方区** | 100%-40px | Auto | Margin=20,10,20,20, Padding=20 | Background=White, Border=1px #E0E0E0 |
| **底部操作栏** | 100% | 60px | Padding=20,12 | Background=#FAFAFA, Border-Top=1px #E0E0E0 |

### 4.3 响应式布局策略

**1920x1080（推荐分辨率）**：
- 诊断区默认展开
- 其他四诊默认折叠
- 处方区充分显示（高度约500px）

**1366x768（最低支持分辨率）**：
- 诊断区默认折叠（只显示Header）
- 用户可手动展开
- 处方区高度约350px，使用ScrollViewer

---

## 5️⃣ PrescriptionEditor - 处方编辑器（三种录入方式）

### 5.1 Tab1 - 手工录入（Entry Method #1）

#### 布局原型图

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [📝 手工录入]  (选中Tab，下划线#2196F3，2px)                                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ DataGrid（8列布局，一行4个药材）                                           │ │
│ │ ┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐             │ │
│ │ │药材1  │用量1  │药材2  │用量2  │药材3  │用量3  │药材4  │用量4  │             │ │
│ │ │(120px)│(60px) │(120px)│(60px) │(120px)│(60px) │(120px)│(60px) │             │ │
│ │ │(居中) │(居中) │(居中) │(居中) │(居中) │(居中) │(居中) │(居中) │             │ │
│ │ ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤             │ │
│ │ │黄芪   │15g    │红枣   │3个    │五味子 │6g     │细辛   │6g     │ ← 第1行   │ │
│ │ │(ComboBox)│(TextBox)│(ComboBox)│(TextBox)│(ComboBox)│(TextBox)│(ComboBox)│(TextBox)│ │
│ │ ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤             │ │
│ │ │当归   │10g    │白芍   │15g    │川芎   │6g     │熟地   │20g    │ ← 第2行   │ │
│ │ │      │       │       │       │       │       │       │       │ (#F9F9F9)│ │
│ │ ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤             │ │
│ │ │党参   │12g    │茯苓   │10g    │甘草   │6g     │       │       │ ← 第3行   │ │
│ │ │      │       │       │       │       │       │       │       │ (White)  │ │
│ │ └──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘             │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ [+添加行] [删除选中行] [清空全部]           药材总数：11味  总剂量：123g     │ │
│ │ (SecondaryButton)                           (CaptionTextBlock, 右对齐)    │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### DataGrid列定义（精确尺寸）

| 列名 | 控件类型 | 宽度 | 对齐方式 | 焦点顺序 |
|-----|---------|------|---------|---------|
| **药材1** | ComboBox(可编辑) | 120px | Center | TabIndex=1 |
| **用量1** | TextBox | 60px | Center | TabIndex=2 |
| **药材2** | ComboBox(可编辑) | 120px | Center | TabIndex=3 |
| **用量2** | TextBox | 60px | Center | TabIndex=4 |
| **药材3** | ComboBox(可编辑) | 120px | Center | TabIndex=5 |
| **用量3** | TextBox | 60px | Center | TabIndex=6 |
| **药材4** | ComboBox(可编辑) | 120px | Center | TabIndex=7 |
| **用量4** | TextBox | 60px | Center | TabIndex=8 |

**总宽度**：(120+60) * 4 = 720px

#### 拼音码ComboBox示例

```xml
<DataGridTemplateColumn Header="药材1" Width="120">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <ComboBox IsEditable="True"
                      Text="{Binding Item1.HerbName, UpdateSourceTrigger=PropertyChanged}"
                      ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                      DisplayMemberPath="Name"
                      SelectedValuePath="Id"
                      SelectedValue="{Binding Item1.HerbId}"
                      PreviewKeyDown="Herb_PreviewKeyDown"
                      TextChanged="Herb_TextChanged">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel>
                            <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                            <TextBlock Text="{Binding PinyinCode}"
                                       FontSize="10"
                                       Foreground="{StaticResource TextHintColor}"/>
                        </StackPanel>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**拼音码过滤逻辑**（ViewModel）：

```csharp
private string _searchText = string.Empty;
private ObservableCollection<HerbDto> _filteredHerbs = new();

public ObservableCollection<HerbDto> FilteredHerbs
{
    get => _filteredHerbs;
    set => SetProperty(ref _filteredHerbs, value);
}

private void Herb_TextChanged(object sender, TextChangedEventArgs e)
{
    var comboBox = sender as ComboBox;
    _searchText = comboBox.Text;

    if (_searchText.Length < 2)
    {
        FilteredHerbs.Clear();
        return;
    }

    // 过滤药材列表（名称或拼音码匹配）
    var filtered = AllHerbs
        .Where(h => h.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                   h.PinyinCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
        .Take(5)  // 限制显示前5个
        .ToList();

    FilteredHerbs.Clear();
    foreach (var herb in filtered)
    {
        FilteredHerbs.Add(herb);
    }

    comboBox.IsDropDownOpen = FilteredHerbs.Any();
}
```

#### 焦点跳转流程图

```
用户操作流程（一行录入）：

1. 点击"药材1"Cell（或按Tab键跳转）
   ↓
2. 输入"黄芪"或拼音码"hq"
   ↓ (自动弹出匹配列表)
3. 按Tab键选择 或 按Enter键确认
   ↓ (触发Herb_PreviewKeyDown事件)
4. 焦点自动跳转到"用量1"Cell
   ↓
5. 输入"15g"
   ↓
6. 按Enter键确认
   ↓ (触发Quantity_PreviewKeyDown事件)
7. 焦点自动跳转到"药材2"Cell
   ↓
8. 重复步骤2-7（药材2 → 用量2 → 药材3 → 用量3 → 药材4 → 用量4）
   ↓
9. 第4个药材完成后，焦点跳转到下一行第1个药材
   ↓
10. 如果没有下一行，自动添加新行
```

**焦点跳转代码示例**：

```csharp
private void Herb_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem != null)
        {
            // 跳转到对应的用量Cell
            MoveFocusToQuantityCell(comboBox);
            e.Handled = true;
        }
    }
}

private void Quantity_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        // 跳转到下一个药材Cell
        MoveFocusToNextHerbCell(sender as TextBox);
        e.Handled = true;
    }
}

private void MoveFocusToQuantityCell(ComboBox herbComboBox)
{
    var row = DataGrid.ItemContainerGenerator.ContainerFromItem(herbComboBox.DataContext) as DataGridRow;
    if (row != null)
    {
        // 根据当前ComboBox的列索引，找到对应的用量Cell
        // TabIndex: 药材1(1) → 用量1(2), 药材2(3) → 用量2(4), ...
        var nextTabIndex = herbComboBox.TabIndex + 1;
        var quantityCell = FindCellByTabIndex(row, nextTabIndex);
        quantityCell?.Focus();
    }
}

private void MoveFocusToNextHerbCell(TextBox quantityTextBox)
{
    var row = DataGrid.ItemContainerGenerator.ContainerFromItem(quantityTextBox.DataContext) as DataGridRow;
    if (row != null)
    {
        var currentTabIndex = quantityTextBox.TabIndex;

        // 如果是第4个用量(TabIndex=8)，跳转到下一行第1个药材
        if (currentTabIndex == 8)
        {
            var nextRowIndex = DataGrid.Items.IndexOf(quantityTextBox.DataContext) + 1;

            if (nextRowIndex >= DataGrid.Items.Count)
            {
                // 自动添加新行
                AddNewRow();
            }

            var nextRow = DataGrid.ItemContainerGenerator.ContainerFromIndex(nextRowIndex) as DataGridRow;
            var firstHerbCell = FindCellByTabIndex(nextRow, 1);
            firstHerbCell?.Focus();
        }
        else
        {
            // 跳转到同行下一个药材
            var nextTabIndex = currentTabIndex + 1;
            var nextHerbCell = FindCellByTabIndex(row, nextTabIndex);
            nextHerbCell?.Focus();
        }
    }
}
```

---

### 5.2 Tab2 - 验方导入（Entry Method #2）

#### 布局原型图

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [📋 验方导入]  (选中Tab，下划线#2196F3，2px)                                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ 搜索验方：[__________________]  [搜索]  (SearchTextBox + SecondaryButton)  │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ 验方列表 (ListView, Height=300px)                                         │ │
│ │ ┌─────────────────────────────────────────────────────────────────────┐ │ │
│ │ │ ▶ 四君子汤                                                           │ │ │
│ │ │   组成：人参15g、白术12g、茯苓12g、甘草6g                             │ │ │
│ │ │   主治：脾胃气虚证                                                    │ │ │
│ │ │   (CaptionTextBlock, Foreground=#757575)                            │ │ │
│ │ ├─────────────────────────────────────────────────────────────────────┤ │ │
│ │ │ ▶ 六味地黄丸 (选中项，Background=#BBDEFB)                             │ │ │
│ │ │   组成：熟地黄24g、山萸肉12g、山药12g、泽泻9g、牡丹皮9g、茯苓9g       │ │ │
│ │ │   主治：肾阴亏虚证                                                    │ │ │
│ │ ├─────────────────────────────────────────────────────────────────────┤ │ │
│ │ │ ▶ 逍遥散                                                             │ │ │
│ │ │   组成：柴胡15g、当归12g、白芍12g、白术12g、茯苓12g...               │ │ │
│ │ │   主治：肝郁脾虚证                                                    │ │ │
│ │ └─────────────────────────────────────────────────────────────────────┘ │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ [导入选中验方]  (PrimaryButton, 右对齐)                                    │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ 提示：导入后药材会自动添加到手工录入表格，可继续编辑调整                       │
│ (HintTextBlock)                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### ListView ItemTemplate

```xml
<ListView ItemsSource="{Binding AvailableFormulas}"
          SelectedItem="{Binding SelectedFormula}"
          Height="300"
          VirtualizingPanel.IsVirtualizing="True">
    <ListView.ItemTemplate>
        <DataTemplate>
            <Border Padding="15,10" Margin="0,5">
                <StackPanel>
                    <TextBlock Text="{Binding Name}"
                               Style="{StaticResource H3TextBlock}"/>
                    <TextBlock Margin="0,5,0,0">
                        <Run Text="组成：" Foreground="{StaticResource TextSecondaryColor}"/>
                        <Run Text="{Binding CompositionSummary}"/>
                    </TextBlock>
                    <TextBlock Margin="0,5,0,0">
                        <Run Text="主治：" Foreground="{StaticResource TextSecondaryColor}"/>
                        <Run Text="{Binding Indication}"/>
                    </TextBlock>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

#### 交互逻辑

```csharp
public class PrescriptionEditorViewModel
{
    private FormulaDto? _selectedFormula;

    public ObservableCollection<FormulaDto> AvailableFormulas { get; set; }
    public FormulaDto? SelectedFormula
    {
        get => _selectedFormula;
        set
        {
            if (SetProperty(ref _selectedFormula, value))
            {
                ImportFormulaCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DelegateCommand ImportFormulaCommand { get; }

    private async void OnImportFormula()
    {
        if (SelectedFormula == null) return;

        var result = await _prescriptionRepository.ImportFormulaAsync(
            CurrentPrescription.Id,
            SelectedFormula.Id
        );

        if (result.IsSuccess)
        {
            MessageBox.Show($"已导入验方"{SelectedFormula.Name}"（{SelectedFormula.Herbs.Count}味药材）");
            await RefreshPrescriptionItems();  // 刷新Tab1表格

            // 自动切换到Tab1（手工录入）
            SelectedTabIndex = 0;
        }
        else
        {
            MessageBox.Show($"导入失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

---

### 5.3 Tab3 - 历史复制（Entry Method #3）

#### 布局原型图

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [🕐 历史复制]  (选中Tab，下划线#2196F3，2px)                                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ 当前患者历史处方：                                                          │ │
│ │ (H3TextBlock, Margin=0,0,0,10)                                            │ │
│ │ ┌──────────────────────────────────────────────────┬────────────────────┐ │ │
│ │ │ [2025-10-15 - 逍遥散加减（12味）- 肝郁脾虚]       │  [复制]             │ │ │
│ │ │ (ComboBox, MinWidth=400px)                       │  (PrimaryButton)   │ │ │
│ │ └──────────────────────────────────────────────────┴────────────────────┘ │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ 或                                                                        │ │
│ │ (CaptionTextBlock, TextAlignment=Center, Margin=0,20,0,20)               │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ [🔍 全局查询其他患者处方]  (SecondaryButton)                                 │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│ 提示：复制后药材会自动添加到手工录入表格，可继续编辑调整                       │
│ (HintTextBlock)                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### 全局查询对话框（PrescriptionSearchDialog，800x500）

```
┌──────────────────────────────────────────────────────────────────────────┐
│ 历史处方查询 (H2TextBlock, Margin=0,0,0,15)                                │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │ 患者姓名：  [__________]  (SearchTextBox)                          │ │
│ │ 症状关键词：[__________]  (SearchTextBox)                          │ │
│ │                                              [查询]  (PrimaryButton) │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │ 查询结果 (DataGrid, Height=300px)                                   │ │
│ │ ┌────────┬───────────┬───────────────┬──────────────┐             │ │
│ │ │ 患者   │ 日期      │ 诊断          │ 药材数量      │             │ │
│ │ │ (80px) │ (100px)   │ (200px)       │ (80px)       │             │ │
│ │ ├────────┼───────────┼───────────────┼──────────────┤             │ │
│ │ │ 张三   │ 2025-10-15│ 肝郁脾虚      │ 12味         │  ← 选中行  │ │
│ │ │        │           │               │              │  (#BBDEFB) │ │
│ │ ├────────┼───────────┼───────────────┼──────────────┤             │ │
│ │ │ 李四   │ 2025-10-10│ 肝郁气滞      │ 10味         │             │ │
│ │ │        │           │               │              │  (#F9F9F9) │ │
│ │ ├────────┼───────────┼───────────────┼──────────────┤             │ │
│ │ │ 王五   │ 2025-10-08│ 脾胃虚弱      │ 8味          │             │ │
│ │ │        │           │               │              │  (White)   │ │
│ │ └────────┴───────────┴───────────────┴──────────────┘             │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│                                               [复制]   [关闭]            │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

#### 交互逻辑

```csharp
public class PrescriptionEditorViewModel
{
    private PrescriptionSearchResultDto? _selectedHistoryPrescription;

    public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions { get; set; }
    public PrescriptionSearchResultDto? SelectedHistoryPrescription
    {
        get => _selectedHistoryPrescription;
        set
        {
            if (SetProperty(ref _selectedHistoryPrescription, value))
            {
                CopyHistoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DelegateCommand CopyHistoryCommand { get; }
    public DelegateCommand OpenGlobalSearchCommand { get; }

    private async void OnCopyHistory()
    {
        if (SelectedHistoryPrescription == null) return;

        var result = await _prescriptionRepository.ClonePrescriptionAsync(
            SelectedHistoryPrescription.PrescriptionId,
            CurrentPrescription.Id
        );

        if (result.IsSuccess)
        {
            MessageBox.Show($"已复制历史处方（{SelectedHistoryPrescription.HerbCount}味药材）");
            await RefreshPrescriptionItems();  // 刷新Tab1表格

            // 自动切换到Tab1（手工录入）
            SelectedTabIndex = 0;
        }
        else
        {
            MessageBox.Show($"复制失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnOpenGlobalSearch()
    {
        var dialog = new PrescriptionSearchDialog();
        var result = dialog.ShowDialog();

        if (result == true && dialog.SelectedPrescription != null)
        {
            // 同样调用ClonePrescriptionAsync
            // ...
        }
    }
}
```

---

## 6️⃣ 快捷键汇总

### 全局快捷键

| 快捷键 | 功能 | 使用场景 |
|-------|------|---------|
| **Ctrl+N** | 新增患者 / 开始接诊 | HomeView、PatientSelectionDialog |
| **Ctrl+F** | 搜索患者 | PatientSelectionDialog |
| **Ctrl+S** | 保存草稿 | ConsultationView |
| **Ctrl+Enter** | 完成就诊 | ConsultationView |
| **Esc** | 取消 / 返回 | 所有对话框 |
| **F5** | 刷新 | PatientSelectionDialog、验方列表 |

### 处方录入快捷键

| 快捷键 | 功能 | 使用场景 |
|-------|------|---------|
| **Tab** | 列间切换 | DataGrid导航 |
| **Enter** | 确认选择并跳转 | 药材ComboBox、用量TextBox |
| **Ctrl+↓** | 添加新行 | DataGrid末尾 |
| **Delete** | 删除当前行 | DataGrid选中行 |
| **Ctrl+D** | 复制当前行 | DataGrid选中行（可选） |

### 快捷键实现示例

```csharp
public class ConsultationViewModel
{
    public ConsultationViewModel()
    {
        // 注册全局快捷键
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        // Ctrl+S: 保存草稿
        InputBindings.Add(new KeyBinding(SaveDraftCommand, Key.S, ModifierKeys.Control));

        // Ctrl+Enter: 完成就诊
        InputBindings.Add(new KeyBinding(CompleteConsultationCommand, Key.Enter, ModifierKeys.Control));

        // Esc: 取消
        InputBindings.Add(new KeyBinding(CancelCommand, Key.Escape, ModifierKeys.None));
    }
}
```

---

## 7️⃣ 工作量估算与任务分解

基于本原型图文档，重新估算开发任务：

| 任务 | 工作量 | 说明 |
|-----|-------|------|
| **Task 1**: 全局样式系统 | 4-6小时 | 创建Colors.xaml、Typography.xaml、Controls.xaml |
| **Task 2**: 导航框架 | 4-6小时 | INavigationService、MainViewModel、DataTemplate注册 |
| **Task 3**: HomeView（Dashboard） | 2-3小时 | 简单布局 + 开始接诊按钮 + 可选统计 |
| **Task 4**: PatientSelectionDialog优化 | 6-8小时 | 虚拟化 + 拼音码提示 + 新建患者功能 |
| **Task 5**: ConsultationView主框架 | 4-6小时 | 三段式布局（患者信息+诊断+处方） |
| **Task 6**: 诊断表单 | 6-8小时 | 8个字段 + 验证 + Expander折叠 |
| **Task 7**: 处方录入#1（手工录入） | 12-16小时 | 8列DataGrid + 拼音码过滤 + 焦点跳转 |
| **Task 8**: 处方录入#2（验方导入） | 4-6小时 | 验方列表 + 导入逻辑 |
| **Task 9**: 处方录入#3（历史复制） | 8-10小时 | 历史下拉框 + 全局查询对话框 |
| **Task 10**: 流程控制与验证 | 4-6小时 | 保存草稿、完成就诊、打印、取消 |
| **总计** | **54-75小时** | **约7-10个工作日** |

---

## 8️⃣ 文档变更记录

| 日期 | 版本 | 变更描述 | 修改人 |
|------|------|---------|-------|
| 2025-10-19 | v1.0 | 初始版本，完成所有原型图和样式规范 | Claude |

---

## 9️⃣ 下一步行动

### ✅ 已完成
1. ✅ 创建UI原型图文档（本文档）

### 📋 待执行

**立即行动**：
1. 创建GitHub Epic Issue：`[Epic] 就诊流程UI/UX实现`
2. 创建子Issues（Task 1-10）
3. 开始Task 1：全局样式系统（4-6小时）

**后续里程碑**：
- **Milestone 1**（2天）：Task 1-2（样式系统 + 导航框架）
- **Milestone 2**（1天）：Task 3-4（HomeView + 患者选择优化）
- **Milestone 3**（5-7天）：Task 5-10（核心就诊界面 + 处方录入）

---

**📌 重要提醒**：
- 所有UI实现必须严格遵循本原型图文档
- 所有尺寸、颜色、字体必须与规范一致
- 开发前必须阅读对应章节的详细布局图
- 如有调整需求，必须先更新本文档再实施
