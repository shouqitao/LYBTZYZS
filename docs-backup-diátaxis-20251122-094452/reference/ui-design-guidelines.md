# WPF UI 设计规范

**版本**: 1.0
**更新日期**: 2025-11-10
**适用范围**: LYBTZYZS 项目所有 WPF 客户端视图

---

## 📋 目录

1. [设计原则](#设计原则)
2. [颜色规范](#颜色规范)
3. [间距规范](#间距规范)
4. [圆角规范](#圆角规范)
5. [阴影效果](#阴影效果)
6. [字体规范](#字体规范)
7. [按钮样式](#按钮样式)
8. [卡片布局](#卡片布局)
9. [XAML 资源模板](#xaml-资源模板)
10. [完整视图模板](#完整视图模板)

---

## 设计原则

### 核心理念

- **简洁大方**: 避免过度装饰，保持界面清爽
- **直观明了**: 用户一眼就能理解界面功能
- **一致性**: 所有模块使用统一的设计语言
- **现代化**: 采用卡片式布局、柔和的阴影、合理的留白

### 参考标准

- 用户模块的 `UserDetailView.xaml` 作为设计标准模板
- 遵循 Material Design 的间距和层次理念
- WPF 最佳实践（Grid 布局、资源复用、样式分离）

---

## 颜色规范

### 基础色板

| 用途 | 颜色值 | 示例 | 说明 |
|-----|--------|------|------|
| **页面背景** | `#F8FAFC` | ![#F8FAFC](https://via.placeholder.com/60x20/F8FAFC/000000?text=+) | 浅灰蓝，舒适柔和 |
| **卡片背景** | `#FFFFFF` | ![#FFFFFF](https://via.placeholder.com/60x20/FFFFFF/000000?text=+) | 纯白 |
| **标题栏背景** | `#F9FAFB` | ![#F9FAFB](https://via.placeholder.com/60x20/F9FAFB/000000?text=+) | 浅灰，区分内容区域 |

### 文字颜色

| 用途 | 颜色值 | 对比度 | 说明 |
|-----|--------|--------|------|
| **主标题** | `#1E293B` | AAA | 深灰蓝，清晰醒目 |
| **次级文本** | `#64748B` | AA+ | 中灰蓝，柔和不刺眼 |
| **表单标签** | `#374151` | AA+ | 深灰，易读 |
| **占位符/禁用** | `#9CA3AF` | AA | 浅灰，弱化显示 |
| **标题栏文字** | `#111827` | AAA | 几乎黑色，最强对比 |

### 功能色

| 功能 | 颜色值 | Hover | Pressed | 说明 |
|-----|--------|-------|---------|------|
| **主操作（蓝色）** | `#3B82F6` | `#2563EB` | `#1D4ED8` | 蓝色系，主要操作 |
| **警告（橙色）** | `#F59E0B` | `#D97706` | `#B45309` | 橙色系，重置/警告 |
| **成功（绿色）** | `#10B981` | `#059669` | `#047857` | 绿色系，确认/成功 |
| **信息（靛蓝）** | `#6366F1` | `#4F46E5` | `#4338CA` | 靛蓝系，信息提示 |
| **危险（红色）** | `#EF4444` | `#DC2626` | `#B91C1C` | 红色系，删除/危险 |

### 交互状态

| 状态 | 背景色 | 前景色 | 说明 |
|-----|--------|--------|------|
| **按钮 Hover** | `#F1F5F9` | `#475569` | 浅灰背景，深灰文字 |
| **输入框聚焦** | `#FFFFFF` | `#1E293B` | 边框变蓝 `#3B82F6` |
| **只读输入框** | `#F9FAFB` | `#6B7280` | 灰色背景，区分可编辑 |

---

## 间距规范

### 页面级间距

```xaml
<!-- 页面外边距 -->
<Grid Margin="40,28" Background="#F8FAFC">
    <!-- 内容 -->
</Grid>
```

| 位置 | 值 | 说明 |
|-----|---|------|
| **页面左右边距** | `40` | 留出足够呼吸空间 |
| **页面上下边距** | `28` | 顶部距离窗口边缘 |
| **卡片间距** | `28` | 卡片之间的垂直间距 |

### 卡片内部间距

```xaml
<!-- 操作栏卡片 -->
<Border Padding="32,24">

<!-- 标题栏 -->
<Border Padding="32,20">

<!-- 内容区域 -->
<Grid Margin="40,32">
```

| 位置 | 值 | 说明 |
|-----|---|------|
| **操作栏 Padding** | `32,24` | 水平32，垂直24 |
| **标题栏 Padding** | `32,20` | 水平32，垂直20 |
| **内容区 Margin** | `40,32` | 水平40，垂直32 |

### 组件间距

| 组件 | 间距 | 说明 |
|-----|------|------|
| **表单字段间距** | `Margin="0,0,0,24"` | 字段之间垂直间距 24 |
| **按钮之间** | `Margin="0,0,8,0"` | 水平间距 8 |
| **标签与输入框** | `ColumnWidth="140"` | 标签固定宽度 140 |
| **图标与文字** | `Margin="0,0,8,0"` | 图标右侧间距 8 |

---

## 圆角规范

| 元素 | 圆角值 | 说明 |
|-----|--------|------|
| **卡片** | `16` | 主卡片圆角 |
| **顶部卡片标题栏** | `16,16,0,0` | 仅顶部圆角 |
| **按钮（次要）** | `8` | 返回、取消等次要按钮 |
| **按钮（主要）** | `10` | 功能按钮（保存、重置） |
| **输入框** | `8` | 表单输入框 |
| **头像** | `50%` | 圆形头像（值为宽度的一半） |

---

## 阴影效果

### CardShadow（标准卡片阴影）

```xaml
<DropShadowEffect x:Key="CardShadow"
                  BlurRadius="16"
                  ShadowDepth="2"
                  Opacity="0.08"
                  Color="#000000"/>
```

| 参数 | 值 | 说明 |
|-----|---|------|
| **BlurRadius** | `16` | 模糊半径，柔和扩散 |
| **ShadowDepth** | `2` | 阴影偏移，轻微下沉 |
| **Opacity** | `0.08` | 不透明度，微妙可见 |
| **Color** | `#000000` | 黑色 |

**效果**: 卡片轻微"浮起"，层次感清晰但不突兀

### 特殊元素阴影

```xaml
<!-- 头像阴影（带色彩） -->
<DropShadowEffect BlurRadius="12"
                  ShadowDepth="2"
                  Opacity="0.15"
                  Color="#3B82F6"/>
```

---

## 字体规范

### 字号层级

| 用途 | FontSize | FontWeight | 示例 |
|-----|----------|-----------|------|
| **页面标题** | `26` | `SemiBold` | 用户详情、药材详情 |
| **卡片标题** | `19` | `SemiBold` | 基本信息、用户信息 |
| **按钮文字** | `15` | `Medium` | 保存、取消、返回 |
| **表单标签** | `15` | `Medium` | 用户名、药材名称 |
| **输入框内容** | `15` | `Regular` | 用户输入的文字 |
| **图标** | `14-16` | - | Emoji 图标 |

### 字体系列

- **默认**: 系统默认字体（Windows: 微软雅黑）
- **等宽**: `Consolas` 或 `Courier New`（用于代码或数字）

---

## 按钮样式

### 返回按钮（次要操作）

```xaml
<Button Background="Transparent"
        BorderThickness="0"
        Foreground="#64748B"
        FontSize="15"
        FontWeight="Medium"
        Cursor="Hand"
        Padding="14,10">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="8"
                                Padding="{TemplateBinding Padding}">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="◀" FontSize="14" Margin="0,0,8,0"/>
                                <TextBlock Text="返回"/>
                            </StackPanel>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#F1F5F9" />
                                <Setter Property="Foreground" Value="#475569" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Button.Style>
</Button>
```

**特点**: 透明背景，hover 显示浅灰背景

### 功能按钮（主要操作）

```xaml
<Button Background="#F59E0B"
        Foreground="White"
        BorderThickness="0"
        Height="48"
        MinWidth="130"
        FontSize="15"
        FontWeight="Medium"
        Cursor="Hand">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="10"
                                Padding="24,12">
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                <TextBlock Text="🔐" FontSize="16" Margin="0,0,8,0"/>
                                <ContentPresenter VerticalAlignment="Center" />
                            </StackPanel>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#D97706" />
                            </Trigger>
                            <Trigger Property="IsPressed" Value="True">
                                <Setter Property="Background" Value="#B45309" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Button.Style>
    <TextBlock Text="重置密码" />
</Button>
```

**特点**: 纯色背景，hover 变深，pressed 更深，带图标

---

## 卡片布局

### 标准三段式卡片

```
┌────────────────────────────────────────┐
│  顶部操作栏卡片                          │
│  - 返回按钮 + 标题 + 功能按钮            │
└────────────────────────────────────────┘
              ↓ 间距 28
┌────────────────────────────────────────┐
│  ┌──────────────────────────────────┐  │
│  │ 标题栏（浅灰背景）                │  │
│  └──────────────────────────────────┘  │
│  ┌──────────────────────────────────┐  │
│  │ 内容区域                          │  │
│  │ - 表单字段                        │  │
│  │ - 数据展示                        │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

### 层级结构

```
页面（Grid Margin="40,28"）
  └── 卡片1（Border CornerRadius="16" Margin="0,0,0,28"）
      ├── 标题栏（Border Background="#F9FAFB" CornerRadius="16,16,0,0" Padding="32,20"）
      └── 内容区（Grid Margin="40,32"）
  └── 卡片2（Border CornerRadius="16" Margin="0,0,0,28"）
      ├── 标题栏
      └── 内容区
```

---

## XAML 资源模板

### 标准资源定义

```xaml
<UserControl.Resources>
    <ResourceDictionary>
        <!-- 卡片阴影 -->
        <DropShadowEffect x:Key="CardShadow"
                          BlurRadius="16"
                          ShadowDepth="2"
                          Opacity="0.08"
                          Color="#000000"/>

        <!-- 可选：头像阴影 -->
        <DropShadowEffect x:Key="AvatarShadow"
                          BlurRadius="12"
                          ShadowDepth="2"
                          Opacity="0.15"
                          Color="#3B82F6"/>

        <!-- 单行文本框样式 -->
        <Style x:Key="ModernTextBoxStyle" TargetType="TextBox">
            <Setter Property="FontSize" Value="15"/>
            <Setter Property="Padding" Value="14,12"/>
            <Setter Property="BorderBrush" Value="#D1D5DB"/>
            <Setter Property="Background" Value="White"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TextBox">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="8"
                                Padding="{TemplateBinding Padding}">
                            <ScrollViewer x:Name="PART_ContentHost" />
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsFocused" Value="True">
                                <Setter Property="BorderBrush" Value="#3B82F6" />
                            </Trigger>
                            <Trigger Property="IsReadOnly" Value="True">
                                <Setter Property="Background" Value="#F9FAFB" />
                                <Setter Property="Foreground" Value="#6B7280" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- 多行文本框样式 -->
        <Style x:Key="ModernMultilineTextBoxStyle" TargetType="TextBox">
            <Setter Property="FontSize" Value="15"/>
            <Setter Property="Padding" Value="14,12"/>
            <Setter Property="BorderBrush" Value="#D1D5DB"/>
            <Setter Property="Background" Value="White"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="TextWrapping" Value="Wrap"/>
            <Setter Property="AcceptsReturn" Value="True"/>
            <Setter Property="VerticalScrollBarVisibility" Value="Auto"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TextBox">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="8"
                                Padding="{TemplateBinding Padding}">
                            <ScrollViewer x:Name="PART_ContentHost"
                                          VerticalScrollBarVisibility="{TemplateBinding VerticalScrollBarVisibility}" />
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsFocused" Value="True">
                                <Setter Property="BorderBrush" Value="#3B82F6" />
                            </Trigger>
                            <Trigger Property="IsReadOnly" Value="True">
                                <Setter Property="Background" Value="#F9FAFB" />
                                <Setter Property="Foreground" Value="#6B7280" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ResourceDictionary>
</UserControl.Resources>
```

### 通用转换器（如需要）

```xaml
<!-- 首字符转换器（用于头像） -->
<infrastructure:FirstCharacterConverter x:Key="FirstCharacterConverter"
    xmlns:infrastructure="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure" />

<!-- 布尔转可见性转换器 -->
<BooleanToVisibilityConverter x:Key="BoolToVisConverter" />
```

---

## 完整视图模板

### 1. Detail View（查看视图）

**文件名**: `XxxDetailView.xaml`
**用途**: 查看详细信息
**特点**: 只读，带返回按钮和功能按钮

```xaml
<UserControl x:Class="LYBT.Desktop.Xxx.Views.XxxDetailView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <UserControl.Resources>
        <ResourceDictionary>
            <DropShadowEffect x:Key="CardShadow"
                              BlurRadius="16"
                              ShadowDepth="2"
                              Opacity="0.08"
                              Color="#000000"/>
        </ResourceDictionary>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <Grid Margin="40,28" Background="#F8FAFC">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" /> <!-- 可选：底部按钮栏 -->
            </Grid.RowDefinitions>

            <!-- 顶部操作栏 -->
            <Border Grid.Row="0"
                    Background="White"
                    CornerRadius="16"
                    Padding="32,24"
                    Margin="0,0,0,28"
                    Effect="{StaticResource CardShadow}">
                <Grid MinHeight="64">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>

                    <!-- 返回按钮 -->
                    <Button Grid.Column="0"
                            Command="{Binding BackCommand}"
                            Background="Transparent"
                            BorderThickness="0"
                            Foreground="#64748B"
                            FontSize="15"
                            FontWeight="Medium"
                            Cursor="Hand"
                            Padding="14,10">
                        <Button.Style>
                            <Style TargetType="Button">
                                <Setter Property="Template">
                                    <Setter.Value>
                                        <ControlTemplate TargetType="Button">
                                            <Border Background="{TemplateBinding Background}"
                                                    CornerRadius="8"
                                                    Padding="{TemplateBinding Padding}">
                                                <StackPanel Orientation="Horizontal">
                                                    <TextBlock Text="◀" FontSize="14" Margin="0,0,8,0"/>
                                                    <TextBlock Text="返回"/>
                                                </StackPanel>
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property="IsMouseOver" Value="True">
                                                    <Setter Property="Background" Value="#F1F5F9" />
                                                    <Setter Property="Foreground" Value="#475569" />
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </Button.Style>
                    </Button>

                    <!-- 标题 -->
                    <TextBlock Grid.Column="1"
                               Text="XXX详情"
                               FontSize="26"
                               FontWeight="SemiBold"
                               Foreground="#1E293B"
                               Margin="20,0,0,0"
                               VerticalAlignment="Center" />

                    <!-- 功能按钮（可选） -->
                    <StackPanel Grid.Column="2" Orientation="Horizontal">
                        <!-- 根据需求添加功能按钮 -->
                    </StackPanel>
                </Grid>
            </Border>

            <!-- 内容卡片 -->
            <Border Grid.Row="1"
                    Background="White"
                    CornerRadius="16"
                    Effect="{StaticResource CardShadow}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <!-- 标题栏 -->
                    <Border Grid.Row="0"
                            Background="#F9FAFB"
                            CornerRadius="16,16,0,0"
                            Padding="32,20">
                        <TextBlock Text="基本信息"
                                   FontSize="19"
                                   FontWeight="SemiBold"
                                   Foreground="#111827" />
                    </Border>

                    <!-- 内容区域 -->
                    <StackPanel Grid.Row="1" Margin="40,32">
                        <!-- 字段展示 -->
                        <Grid Margin="0,0,0,24">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="140" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>

                            <TextBlock Grid.Column="0"
                                       Text="字段名称"
                                       FontSize="15"
                                       FontWeight="Medium"
                                       Foreground="#374151"
                                       VerticalAlignment="Center" />

                            <TextBlock Grid.Column="1"
                                       Text="{Binding FieldValue}"
                                       FontSize="15"
                                       Foreground="#1E293B"
                                       VerticalAlignment="Center" />
                        </Grid>

                        <!-- 更多字段... -->
                    </StackPanel>
                </Grid>
            </Border>

            <!-- 底部按钮栏（可选，用于编辑操作） -->
            <Border Grid.Row="2"
                    Background="White"
                    CornerRadius="16"
                    Padding="32,24"
                    Effect="{StaticResource CardShadow}"
                    Margin="0,28,0,0">
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <!-- 取消按钮 -->
                    <Button Command="{Binding CancelCommand}"
                            Background="White"
                            BorderBrush="#D1D5DB"
                            BorderThickness="1"
                            Foreground="#374151"
                            FontSize="15"
                            FontWeight="Medium"
                            Height="48"
                            MinWidth="120"
                            Cursor="Hand"
                            Margin="0,0,16,0">
                        <Button.Style>
                            <Style TargetType="Button">
                                <Setter Property="Template">
                                    <Setter.Value>
                                        <ControlTemplate TargetType="Button">
                                            <Border Background="{TemplateBinding Background}"
                                                    BorderBrush="{TemplateBinding BorderBrush}"
                                                    BorderThickness="{TemplateBinding BorderThickness}"
                                                    CornerRadius="10"
                                                    Padding="24,12">
                                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property="IsMouseOver" Value="True">
                                                    <Setter Property="Background" Value="#F9FAFB" />
                                                    <Setter Property="BorderBrush" Value="#9CA3AF" />
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </Button.Style>
                        <TextBlock Text="取消" />
                    </Button>

                    <!-- 保存修改按钮 -->
                    <Button Command="{Binding SaveCommand}"
                            Background="#3B82F6"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="15"
                            FontWeight="Medium"
                            Height="48"
                            MinWidth="120"
                            Cursor="Hand">
                        <Button.Style>
                            <Style TargetType="Button">
                                <Setter Property="Template">
                                    <Setter.Value>
                                        <ControlTemplate TargetType="Button">
                                            <Border Background="{TemplateBinding Background}"
                                                    CornerRadius="10"
                                                    Padding="24,12">
                                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property="IsMouseOver" Value="True">
                                                    <Setter Property="Background" Value="#2563EB" />
                                                </Trigger>
                                                <Trigger Property="IsPressed" Value="True">
                                                    <Setter Property="Background" Value="#1D4ED8" />
                                                </Trigger>
                                                <Trigger Property="IsEnabled" Value="False">
                                                    <Setter Property="Background" Value="#D1D5DB" />
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </Button.Style>
                        <TextBlock Text="保存修改" />
                    </Button>
                </StackPanel>
            </Border>
        </Grid>
    </ScrollViewer>
</UserControl>
```

**说明**: 
- Grid.Row="2" 的底部按钮栏是**可选的**，仅在 Detail View 支持编辑操作时使用
- 如果是纯查看视图，可以省略第三行（Grid.Row="2"）和底部按钮栏
- 取消按钮：白色背景 + 灰色边框，hover 时背景变浅灰
- 保存按钮：蓝色背景，hover 时颜色加深，禁用时变灰

### 2. Create View（创建视图）

**文件名**: `XxxCreateView.xaml`
**用途**: 创建新记录
**特点**: 表单输入，带保存/取消按钮

```xaml
<UserControl x:Class="LYBT.Desktop.Xxx.Views.XxxCreateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <UserControl.Resources>
        <ResourceDictionary>
            <DropShadowEffect x:Key="CardShadow"
                              BlurRadius="16"
                              ShadowDepth="2"
                              Opacity="0.08"
                              Color="#000000"/>
        </ResourceDictionary>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <Grid Margin="40,28" Background="#F8FAFC">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- 顶部操作栏 -->
            <Border Grid.Row="0"
                    Background="White"
                    CornerRadius="16"
                    Padding="32,24"
                    Margin="0,0,0,28"
                    Effect="{StaticResource CardShadow}">
                <Grid MinHeight="64">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <!-- 返回按钮 -->
                    <Button Grid.Column="0"
                            Command="{Binding CancelCommand}"
                            Background="Transparent"
                            BorderThickness="0"
                            Foreground="#64748B"
                            FontSize="15"
                            FontWeight="Medium"
                            Cursor="Hand"
                            Padding="14,10">
                        <!-- 同 Detail View 的返回按钮样式 -->
                    </Button>

                    <!-- 标题 -->
                    <TextBlock Grid.Column="1"
                               Text="{Binding PageTitle}"
                               FontSize="26"
                               FontWeight="SemiBold"
                               Foreground="#1E293B"
                               Margin="20,0,0,0"
                               VerticalAlignment="Center" />
                </Grid>
            </Border>

            <!-- 表单内容 -->
            <Border Grid.Row="1"
                    Background="White"
                    CornerRadius="16"
                    Effect="{StaticResource CardShadow}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <!-- 标题栏 -->
                    <Border Grid.Row="0"
                            Background="#F9FAFB"
                            CornerRadius="16,16,0,0"
                            Padding="32,20">
                        <TextBlock Text="XXX信息"
                                   FontSize="19"
                                   FontWeight="SemiBold"
                                   Foreground="#111827" />
                    </Border>

                    <!-- 表单字段 -->
                    <StackPanel Grid.Row="1" Margin="40,32">
                        <!-- 文本输入框示例 -->
                        <Grid Margin="0,0,0,24">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="140" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>

                            <TextBlock Grid.Column="0"
                                       Text="字段名称 *"
                                       FontSize="15"
                                       FontWeight="Medium"
                                       Foreground="#374151"
                                       VerticalAlignment="Center" />

                            <TextBox Grid.Column="1"
                                     Text="{Binding FieldValue, UpdateSourceTrigger=PropertyChanged}"
                                     FontSize="15"
                                     Padding="14,12"
                                     BorderBrush="#D1D5DB"
                                     Background="White"
                                     BorderThickness="1"
                                     VerticalContentAlignment="Center">
                                <TextBox.Style>
                                    <Style TargetType="TextBox">
                                        <Setter Property="Template">
                                            <Setter.Value>
                                                <ControlTemplate TargetType="TextBox">
                                                    <Border Background="{TemplateBinding Background}"
                                                            BorderBrush="{TemplateBinding BorderBrush}"
                                                            BorderThickness="{TemplateBinding BorderThickness}"
                                                            CornerRadius="8">
                                                        <ScrollViewer x:Name="PART_ContentHost"
                                                                      Margin="0"
                                                                      VerticalAlignment="Center" />
                                                    </Border>
                                                    <ControlTemplate.Triggers>
                                                        <Trigger Property="IsFocused" Value="True">
                                                            <Setter Property="BorderBrush" Value="#3B82F6" />
                                                        </Trigger>
                                                    </ControlTemplate.Triggers>
                                                </ControlTemplate>
                                            </Setter.Value>
                                        </Setter>
                                    </Style>
                                </TextBox.Style>
                            </TextBox>
                        </Grid>

                        <!-- 更多表单字段... -->
                    </StackPanel>

                    <!-- 底部操作按钮 -->
                    <Border Grid.Row="2"
                            Background="#F9FAFB"
                            CornerRadius="0,0,16,16"
                            Padding="40,24"
                            BorderBrush="#E5E7EB"
                            BorderThickness="0,1,0,0">
                        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                            <Button Command="{Binding CancelCommand}"
                                    Content="取消"
                                    Background="Transparent"
                                    Foreground="#64748B"
                                    BorderThickness="0"
                                    Height="48"
                                    MinWidth="100"
                                    FontSize="15"
                                    Cursor="Hand"
                                    Margin="0,0,12,0">
                                <!-- 样式同返回按钮 -->
                            </Button>

                            <Button Command="{Binding SaveCommand}"
                                    Background="#3B82F6"
                                    Foreground="White"
                                    BorderThickness="0"
                                    Height="48"
                                    MinWidth="120"
                                    FontSize="15"
                                    FontWeight="Medium"
                                    Cursor="Hand">
                                <Button.Style>
                                    <Style TargetType="Button">
                                        <Setter Property="Template">
                                            <Setter.Value>
                                                <ControlTemplate TargetType="Button">
                                                    <Border Background="{TemplateBinding Background}"
                                                            CornerRadius="10"
                                                            Padding="24,12">
                                                        <ContentPresenter HorizontalAlignment="Center"
                                                                          VerticalAlignment="Center" />
                                                    </Border>
                                                    <ControlTemplate.Triggers>
                                                        <Trigger Property="IsMouseOver" Value="True">
                                                            <Setter Property="Background" Value="#2563EB" />
                                                        </Trigger>
                                                        <Trigger Property="IsPressed" Value="True">
                                                            <Setter Property="Background" Value="#1D4ED8" />
                                                        </Trigger>
                                                        <Trigger Property="IsEnabled" Value="False">
                                                            <Setter Property="Background" Value="#9CA3AF" />
                                                        </Trigger>
                                                    </ControlTemplate.Triggers>
                                                </ControlTemplate>
                                            </Setter.Value>
                                        </Setter>
                                    </Style>
                                </Button.Style>
                                <TextBlock Text="保存" />
                            </Button>
                        </StackPanel>
                    </Border>
                </Grid>
            </Border>
        </Grid>
    </ScrollViewer>
</UserControl>
```

### 3. Edit View（编辑视图）

**文件名**: `XxxEditView.xaml`
**用途**: 编辑现有记录
**特点**: 与 Create View 基本相同，部分字段可能只读

```xaml
<!-- 结构与 Create View 完全相同，区别在于： -->
<!-- 1. ViewModel 绑定到现有数据 -->
<!-- 2. 某些字段可能是只读的（如 ID、创建时间） -->
<!-- 3. 标题显示为"编辑XXX"而非"新建XXX" -->
```

**只读字段示例**:

```xaml
<TextBox Grid.Column="1"
         Text="{Binding UserName, Mode=OneWay}"
         FontSize="15"
         Padding="14,12"
         BorderBrush="#E5E7EB"
         Background="#F9FAFB"
         BorderThickness="1"
         IsReadOnly="True"
         VerticalContentAlignment="Center">
    <!-- 只读样式：灰色背景，不可编辑 -->
</TextBox>
```

---

## 使用指南

### 新建视图步骤

1. **复制模板**: 从上述完整模板复制对应类型（Detail/Create/Edit）
2. **修改命名空间**: 替换 `Xxx` 为实际模块名（如 `Herbs`、`Patients`）
3. **调整字段**: 根据实际业务需求添加/删除字段
4. **绑定 ViewModel**: 确保 Command 和 Property 绑定正确
5. **测试样式**: 运行应用，验证样式是否符合规范

### 常见问题

**Q: 阴影看不见？**
A: 检查 `Opacity` 值是否过低（建议 0.08），确保卡片背景是白色

**Q: 按钮 hover 没效果？**
A: 确保使用了 `<ControlTemplate.Triggers>` 定义 hover 状态

**Q: 间距不一致？**
A: 严格遵循间距规范表，使用标准值

**Q: 颜色对比度不够？**
A: 使用规范中定义的颜色值，它们已经过对比度测试

---

## 更新日志

| 版本 | 日期 | 说明 |
|-----|------|------|
| 1.0 | 2025-11-10 | 初始版本，基于 UserDetailView 标准化 |

---

## 参考资源

- [Material Design - Color System](https://material.io/design/color)
- [WPF Best Practices 2024](https://blog.postsharp.net/wpf-best-practices-2024)
- [Tailwind CSS Color Palette](https://tailwindcss.com/docs/customizing-colors)（颜色值参考）

---

**维护者**: LYBTZYZS 开发团队
**联系方式**: GitHub Issues
