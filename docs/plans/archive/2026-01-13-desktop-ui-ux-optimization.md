# Desktop UI/UX 完整优化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 统一Desktop层设计系统，实现深色模式支持，提升用户体验成熟度

**Architecture:** 采用纯原生WPF ResourceDictionary热切换方案实现主题切换，建立单一设计Token数据源（Colors/Typography/Spacing），通过ThemeService管理Light/Dark/System三种模式。不引入任何第三方主题插件。

**Tech Stack:** WPF + Prism + .NET 8 + ResourceDictionary动态切换

---

## Phase 1: 设计Token基础设施

### Task 1.1: 创建DesignTokens目录结构

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/DesignTokens/` (directory)
- Create: `src/Client/Desktop/Shell/Resources/Themes/` (directory)

**Step 1: 创建目录**

```bash
mkdir -p src/Client/Desktop/Shell/Resources/DesignTokens
mkdir -p src/Client/Desktop/Shell/Resources/Themes
```

**Step 2: 验证目录存在**

Run: `ls src/Client/Desktop/Shell/Resources/`
Expected: 显示 DesignTokens 和 Themes 目录

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/
git commit -m "chore: create DesignTokens and Themes directory structure"
```

---

### Task 1.2: 创建Colors.Light.xaml (浅色主题颜色Token)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Light.xaml`

**Step 1: 创建浅色主题颜色资源字典**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Light Theme      -->
    <!-- ========================================== -->

    <!-- === Brand Colors === -->
    <Color x:Key="BrandPrimary">#0078D4</Color>
    <Color x:Key="BrandPrimaryHover">#106EBE</Color>
    <Color x:Key="BrandPrimaryPressed">#005A9E</Color>
    <Color x:Key="BrandAccent">#2E8B57</Color>
    <Color x:Key="BrandAccentHover">#3CB371</Color>

    <!-- === Semantic Colors === -->
    <Color x:Key="SemanticSuccess">#107C10</Color>
    <Color x:Key="SemanticSuccessLight">#DFF6DD</Color>
    <Color x:Key="SemanticWarning">#F7630C</Color>
    <Color x:Key="SemanticWarningLight">#FFF4CE</Color>
    <Color x:Key="SemanticError">#D13438</Color>
    <Color x:Key="SemanticErrorLight">#FDE7E9</Color>
    <Color x:Key="SemanticInfo">#00B7C3</Color>
    <Color x:Key="SemanticInfoLight">#E0F7FA</Color>

    <!-- === Surface Colors === -->
    <Color x:Key="SurfaceBackground">#F5F5F5</Color>
    <Color x:Key="SurfaceCard">#FFFFFF</Color>
    <Color x:Key="SurfaceElevated">#FFFFFF</Color>
    <Color x:Key="SurfaceOverlay">#000000</Color>
    <Color x:Key="SurfaceOverlayOpacity">0.4</Color>

    <!-- === Border Colors === -->
    <Color x:Key="BorderDefault">#E1E1E1</Color>
    <Color x:Key="BorderStrong">#8A8886</Color>
    <Color x:Key="BorderSubtle">#F0F0F0</Color>

    <!-- === Text Colors === -->
    <Color x:Key="TextPrimary">#201F1E</Color>
    <Color x:Key="TextSecondary">#605E5C</Color>
    <Color x:Key="TextTertiary">#8A8886</Color>
    <Color x:Key="TextDisabled">#C8C6C4</Color>
    <Color x:Key="TextOnAccent">#FFFFFF</Color>

    <!-- === SolidColorBrushes (for DynamicResource binding) === -->
    <SolidColorBrush x:Key="BrandPrimaryBrush" Color="{StaticResource BrandPrimary}"/>
    <SolidColorBrush x:Key="BrandPrimaryHoverBrush" Color="{StaticResource BrandPrimaryHover}"/>
    <SolidColorBrush x:Key="BrandPrimaryPressedBrush" Color="{StaticResource BrandPrimaryPressed}"/>
    <SolidColorBrush x:Key="BrandAccentBrush" Color="{StaticResource BrandAccent}"/>

    <SolidColorBrush x:Key="SemanticSuccessBrush" Color="{StaticResource SemanticSuccess}"/>
    <SolidColorBrush x:Key="SemanticSuccessLightBrush" Color="{StaticResource SemanticSuccessLight}"/>
    <SolidColorBrush x:Key="SemanticWarningBrush" Color="{StaticResource SemanticWarning}"/>
    <SolidColorBrush x:Key="SemanticWarningLightBrush" Color="{StaticResource SemanticWarningLight}"/>
    <SolidColorBrush x:Key="SemanticErrorBrush" Color="{StaticResource SemanticError}"/>
    <SolidColorBrush x:Key="SemanticErrorLightBrush" Color="{StaticResource SemanticErrorLight}"/>
    <SolidColorBrush x:Key="SemanticInfoBrush" Color="{StaticResource SemanticInfo}"/>
    <SolidColorBrush x:Key="SemanticInfoLightBrush" Color="{StaticResource SemanticInfoLight}"/>

    <SolidColorBrush x:Key="SurfaceBackgroundBrush" Color="{StaticResource SurfaceBackground}"/>
    <SolidColorBrush x:Key="SurfaceCardBrush" Color="{StaticResource SurfaceCard}"/>
    <SolidColorBrush x:Key="SurfaceElevatedBrush" Color="{StaticResource SurfaceElevated}"/>

    <SolidColorBrush x:Key="BorderDefaultBrush" Color="{StaticResource BorderDefault}"/>
    <SolidColorBrush x:Key="BorderStrongBrush" Color="{StaticResource BorderStrong}"/>
    <SolidColorBrush x:Key="BorderSubtleBrush" Color="{StaticResource BorderSubtle}"/>

    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}"/>
    <SolidColorBrush x:Key="TextTertiaryBrush" Color="{StaticResource TextTertiary}"/>
    <SolidColorBrush x:Key="TextDisabledBrush" Color="{StaticResource TextDisabled}"/>
    <SolidColorBrush x:Key="TextOnAccentBrush" Color="{StaticResource TextOnAccent}"/>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Light.xaml
git commit -m "feat(theme): add Colors.Light.xaml design tokens"
```

---

### Task 1.3: 创建Colors.Dark.xaml (深色主题颜色Token)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Dark.xaml`

**Step 1: 创建深色主题颜色资源字典**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Dark Theme       -->
    <!-- ========================================== -->

    <!-- === Brand Colors === -->
    <Color x:Key="BrandPrimary">#4CC2FF</Color>
    <Color x:Key="BrandPrimaryHover">#7AD4FF</Color>
    <Color x:Key="BrandPrimaryPressed">#2EB8FF</Color>
    <Color x:Key="BrandAccent">#50C878</Color>
    <Color x:Key="BrandAccentHover">#6FD98F</Color>

    <!-- === Semantic Colors === -->
    <Color x:Key="SemanticSuccess">#6CCB5F</Color>
    <Color x:Key="SemanticSuccessLight">#1E3A1E</Color>
    <Color x:Key="SemanticWarning">#FFB347</Color>
    <Color x:Key="SemanticWarningLight">#3D2E1E</Color>
    <Color x:Key="SemanticError">#FF6B6B</Color>
    <Color x:Key="SemanticErrorLight">#3D1E1E</Color>
    <Color x:Key="SemanticInfo">#4DD0E1</Color>
    <Color x:Key="SemanticInfoLight">#1E3A3D</Color>

    <!-- === Surface Colors === -->
    <Color x:Key="SurfaceBackground">#1E1E1E</Color>
    <Color x:Key="SurfaceCard">#2D2D2D</Color>
    <Color x:Key="SurfaceElevated">#383838</Color>
    <Color x:Key="SurfaceOverlay">#000000</Color>
    <Color x:Key="SurfaceOverlayOpacity">0.6</Color>

    <!-- === Border Colors === -->
    <Color x:Key="BorderDefault">#404040</Color>
    <Color x:Key="BorderStrong">#606060</Color>
    <Color x:Key="BorderSubtle">#333333</Color>

    <!-- === Text Colors === -->
    <Color x:Key="TextPrimary">#FFFFFF</Color>
    <Color x:Key="TextSecondary">#B3B3B3</Color>
    <Color x:Key="TextTertiary">#808080</Color>
    <Color x:Key="TextDisabled">#4D4D4D</Color>
    <Color x:Key="TextOnAccent">#000000</Color>

    <!-- === SolidColorBrushes (for DynamicResource binding) === -->
    <SolidColorBrush x:Key="BrandPrimaryBrush" Color="{StaticResource BrandPrimary}"/>
    <SolidColorBrush x:Key="BrandPrimaryHoverBrush" Color="{StaticResource BrandPrimaryHover}"/>
    <SolidColorBrush x:Key="BrandPrimaryPressedBrush" Color="{StaticResource BrandPrimaryPressed}"/>
    <SolidColorBrush x:Key="BrandAccentBrush" Color="{StaticResource BrandAccent}"/>

    <SolidColorBrush x:Key="SemanticSuccessBrush" Color="{StaticResource SemanticSuccess}"/>
    <SolidColorBrush x:Key="SemanticSuccessLightBrush" Color="{StaticResource SemanticSuccessLight}"/>
    <SolidColorBrush x:Key="SemanticWarningBrush" Color="{StaticResource SemanticWarning}"/>
    <SolidColorBrush x:Key="SemanticWarningLightBrush" Color="{StaticResource SemanticWarningLight}"/>
    <SolidColorBrush x:Key="SemanticErrorBrush" Color="{StaticResource SemanticError}"/>
    <SolidColorBrush x:Key="SemanticErrorLightBrush" Color="{StaticResource SemanticErrorLight}"/>
    <SolidColorBrush x:Key="SemanticInfoBrush" Color="{StaticResource SemanticInfo}"/>
    <SolidColorBrush x:Key="SemanticInfoLightBrush" Color="{StaticResource SemanticInfoLight}"/>

    <SolidColorBrush x:Key="SurfaceBackgroundBrush" Color="{StaticResource SurfaceBackground}"/>
    <SolidColorBrush x:Key="SurfaceCardBrush" Color="{StaticResource SurfaceCard}"/>
    <SolidColorBrush x:Key="SurfaceElevatedBrush" Color="{StaticResource SurfaceElevated}"/>

    <SolidColorBrush x:Key="BorderDefaultBrush" Color="{StaticResource BorderDefault}"/>
    <SolidColorBrush x:Key="BorderStrongBrush" Color="{StaticResource BorderStrong}"/>
    <SolidColorBrush x:Key="BorderSubtleBrush" Color="{StaticResource BorderSubtle}"/>

    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}"/>
    <SolidColorBrush x:Key="TextTertiaryBrush" Color="{StaticResource TextTertiary}"/>
    <SolidColorBrush x:Key="TextDisabledBrush" Color="{StaticResource TextDisabled}"/>
    <SolidColorBrush x:Key="TextOnAccentBrush" Color="{StaticResource TextOnAccent}"/>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Dark.xaml
git commit -m "feat(theme): add Colors.Dark.xaml design tokens"
```

---

### Task 1.4: 创建Typography.xaml (字体Token)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/DesignTokens/Typography.xaml`

**Step 1: 创建字体资源字典**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Typography       -->
    <!-- Theme-independent font tokens             -->
    <!-- ========================================== -->

    <!-- === Font Family === -->
    <FontFamily x:Key="FontFamilyPrimary">Segoe UI Variable, Segoe UI, Microsoft YaHei UI, sans-serif</FontFamily>
    <FontFamily x:Key="FontFamilyMono">Cascadia Code, Consolas, monospace</FontFamily>

    <!-- === Type Scale (based on 14px body) === -->
    <sys:Double x:Key="TypeCaption">12</sys:Double>
    <sys:Double x:Key="TypeBody">14</sys:Double>
    <sys:Double x:Key="TypeBodyLarge">16</sys:Double>
    <sys:Double x:Key="TypeSubtitle">18</sys:Double>
    <sys:Double x:Key="TypeTitle">20</sys:Double>
    <sys:Double x:Key="TypeHeadline">24</sys:Double>
    <sys:Double x:Key="TypeDisplay">32</sys:Double>

    <!-- === Font Weights === -->
    <FontWeight x:Key="WeightLight">Light</FontWeight>
    <FontWeight x:Key="WeightRegular">Normal</FontWeight>
    <FontWeight x:Key="WeightMedium">Medium</FontWeight>
    <FontWeight x:Key="WeightSemiBold">SemiBold</FontWeight>
    <FontWeight x:Key="WeightBold">Bold</FontWeight>

    <!-- === Line Heights (as multipliers) === -->
    <sys:Double x:Key="LineHeightTight">1.2</sys:Double>
    <sys:Double x:Key="LineHeightNormal">1.4</sys:Double>
    <sys:Double x:Key="LineHeightRelaxed">1.6</sys:Double>

    <!-- === Legacy Compatibility (map old names to new) === -->
    <sys:Double x:Key="FontSizeSmall">{StaticResource TypeCaption}</sys:Double>
    <sys:Double x:Key="FontSizeMedium">{StaticResource TypeBody}</sys:Double>
    <sys:Double x:Key="FontSizeLarge">{StaticResource TypeBodyLarge}</sys:Double>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/DesignTokens/Typography.xaml
git commit -m "feat(theme): add Typography.xaml design tokens"
```

---

### Task 1.5: 创建Spacing.xaml (间距Token)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/DesignTokens/Spacing.xaml`

**Step 1: 创建间距资源字典**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Spacing          -->
    <!-- Theme-independent spacing tokens          -->
    <!-- Based on 4px grid system                  -->
    <!-- ========================================== -->

    <!-- === Spacing Scale (uniform) === -->
    <sys:Double x:Key="SpaceXXS">2</sys:Double>
    <sys:Double x:Key="SpaceXS">4</sys:Double>
    <sys:Double x:Key="SpaceSM">8</sys:Double>
    <sys:Double x:Key="SpaceMD">12</sys:Double>
    <sys:Double x:Key="SpaceLG">16</sys:Double>
    <sys:Double x:Key="SpaceXL">24</sys:Double>
    <sys:Double x:Key="SpaceXXL">32</sys:Double>
    <sys:Double x:Key="SpaceXXXL">48</sys:Double>

    <!-- === Thickness (for Margin/Padding) === -->
    <Thickness x:Key="SpacingXXS">2</Thickness>
    <Thickness x:Key="SpacingXS">4</Thickness>
    <Thickness x:Key="SpacingSM">8</Thickness>
    <Thickness x:Key="SpacingMD">12</Thickness>
    <Thickness x:Key="SpacingLG">16</Thickness>
    <Thickness x:Key="SpacingXL">24</Thickness>
    <Thickness x:Key="SpacingXXL">32</Thickness>

    <!-- === Horizontal Spacing === -->
    <Thickness x:Key="SpacingHorizontalXS">4,0</Thickness>
    <Thickness x:Key="SpacingHorizontalSM">8,0</Thickness>
    <Thickness x:Key="SpacingHorizontalMD">12,0</Thickness>
    <Thickness x:Key="SpacingHorizontalLG">16,0</Thickness>

    <!-- === Vertical Spacing === -->
    <Thickness x:Key="SpacingVerticalXS">0,4</Thickness>
    <Thickness x:Key="SpacingVerticalSM">0,8</Thickness>
    <Thickness x:Key="SpacingVerticalMD">0,12</Thickness>
    <Thickness x:Key="SpacingVerticalLG">0,16</Thickness>

    <!-- === Corner Radius === -->
    <CornerRadius x:Key="RadiusNone">0</CornerRadius>
    <CornerRadius x:Key="RadiusXS">2</CornerRadius>
    <CornerRadius x:Key="RadiusSM">4</CornerRadius>
    <CornerRadius x:Key="RadiusMD">8</CornerRadius>
    <CornerRadius x:Key="RadiusLG">12</CornerRadius>
    <CornerRadius x:Key="RadiusXL">16</CornerRadius>
    <CornerRadius x:Key="RadiusFull">9999</CornerRadius>

    <!-- === Border Thickness === -->
    <Thickness x:Key="BorderThin">1</Thickness>
    <Thickness x:Key="BorderMedium">2</Thickness>
    <Thickness x:Key="BorderThick">4</Thickness>

    <!-- === Component Sizes === -->
    <sys:Double x:Key="ButtonHeightSmall">28</sys:Double>
    <sys:Double x:Key="ButtonHeightMedium">36</sys:Double>
    <sys:Double x:Key="ButtonHeightLarge">44</sys:Double>

    <sys:Double x:Key="InputHeight">36</sys:Double>
    <sys:Double x:Key="DataGridRowHeight">40</sys:Double>
    <sys:Double x:Key="ListItemHeight">44</sys:Double>

    <sys:Double x:Key="IconSizeSmall">16</sys:Double>
    <sys:Double x:Key="IconSizeMedium">20</sys:Double>
    <sys:Double x:Key="IconSizeLarge">24</sys:Double>

    <!-- === Legacy Compatibility === -->
    <Thickness x:Key="DefaultMargin">{StaticResource SpacingMD}</Thickness>
    <Thickness x:Key="DefaultPadding">{StaticResource SpacingSM}</Thickness>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/DesignTokens/Spacing.xaml
git commit -m "feat(theme): add Spacing.xaml design tokens"
```

---

### Task 1.6: 创建Theme.Light.xaml (浅色主题入口)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/Themes/Theme.Light.xaml`

**Step 1: 创建浅色主题入口文件**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Light Theme      -->
    <!-- This is the theme entry point             -->
    <!-- ========================================== -->

    <ResourceDictionary.MergedDictionaries>
        <!-- Color tokens for light theme -->
        <ResourceDictionary Source="../DesignTokens/Colors.Light.xaml"/>
    </ResourceDictionary.MergedDictionaries>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/Themes/Theme.Light.xaml
git commit -m "feat(theme): add Theme.Light.xaml entry point"
```

---

### Task 1.7: 创建Theme.Dark.xaml (深色主题入口)

**Files:**
- Create: `src/Client/Desktop/Shell/Resources/Themes/Theme.Dark.xaml`

**Step 1: 创建深色主题入口文件**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ========================================== -->
    <!-- LYBTZYZS Design System - Dark Theme       -->
    <!-- This is the theme entry point             -->
    <!-- ========================================== -->

    <ResourceDictionary.MergedDictionaries>
        <!-- Color tokens for dark theme -->
        <ResourceDictionary Source="../DesignTokens/Colors.Dark.xaml"/>
    </ResourceDictionary.MergedDictionaries>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Resources/Themes/Theme.Dark.xaml
git commit -m "feat(theme): add Theme.Dark.xaml entry point"
```

---

### Task 1.8: 更新App.xaml资源引用

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml`

**Step 1: 读取现有App.xaml结构**

Run: 使用Read工具查看App.xaml的MergedDictionaries部分

**Step 2: 在MergedDictionaries开头添加新资源引用**

在现有资源之前添加：
```xml
<!-- Design System Tokens (theme-independent) -->
<ResourceDictionary Source="Resources/DesignTokens/Typography.xaml"/>
<ResourceDictionary Source="Resources/DesignTokens/Spacing.xaml"/>

<!-- Current Theme (will be switched by ThemeService) -->
<ResourceDictionary Source="Resources/Themes/Theme.Light.xaml"/>
```

**Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 4: 验证应用启动**

Run: 启动应用确认无XAML解析错误

**Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/App.xaml
git commit -m "feat(theme): integrate design tokens into App.xaml"
```

---

## Phase 2: ThemeService实现

### Task 2.1: 创建IThemeService接口

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IThemeService.cs`

**Step 1: 创建接口文件**

```csharp
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 主题模式枚举
/// </summary>
public enum ThemeMode
{
    /// <summary>浅色主题</summary>
    Light,
    /// <summary>深色主题</summary>
    Dark,
    /// <summary>跟随系统</summary>
    System
}

/// <summary>
/// 主题变更事件参数
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public ThemeMode OldTheme { get; }
    public ThemeMode NewTheme { get; }
    public ThemeMode ActualTheme { get; }

    public ThemeChangedEventArgs(ThemeMode oldTheme, ThemeMode newTheme, ThemeMode actualTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
        ActualTheme = actualTheme;
    }
}

/// <summary>
/// 主题服务接口
/// 负责管理应用程序的主题切换
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 获取当前设置的主题模式（可能是System）
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// 获取实际应用的主题（Light或Dark，不会是System）
    /// </summary>
    ThemeMode ActualTheme { get; }

    /// <summary>
    /// 设置主题模式
    /// </summary>
    void SetTheme(ThemeMode theme);

    /// <summary>
    /// 主题变更事件
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// 初始化主题服务（从配置加载保存的主题）
    /// </summary>
    Task InitializeAsync();
}
```

**Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IThemeService.cs
git commit -m "feat(theme): add IThemeService interface"
```

---

### Task 2.2: 实现ThemeService

**Files:**
- Create: `src/Client/Desktop/Shell/Services/ThemeService.cs`

**Step 1: 创建ThemeService实现**

```csharp
using System.Windows;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 主题服务实现
/// 使用ResourceDictionary热切换实现Light/Dark主题
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private ThemeMode _currentTheme = ThemeMode.Light;
    private ThemeMode _actualTheme = ThemeMode.Light;

    private const string ThemeSettingKey = "AppTheme";
    private const string LightThemePath = "Resources/Themes/Theme.Light.xaml";
    private const string DarkThemePath = "Resources/Themes/Theme.Dark.xaml";

    public ThemeMode CurrentTheme => _currentTheme;
    public ThemeMode ActualTheme => _actualTheme;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            // 从用户设置加载保存的主题
            var savedTheme = LoadSavedTheme();
            _currentTheme = savedTheme;

            // 应用主题
            Application.Current.Dispatcher.Invoke(() =>
            {
                ApplyTheme(savedTheme);
            });

            // 监听系统主题变更
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
        });

        _logger.LogInformation("ThemeService initialized with theme: {Theme}", _currentTheme);
    }

    public void SetTheme(ThemeMode theme)
    {
        if (_currentTheme == theme) return;

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        ApplyTheme(theme);
        SaveTheme(theme);

        _logger.LogInformation("Theme changed from {OldTheme} to {NewTheme}", oldTheme, theme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme, _actualTheme));
    }

    private void ApplyTheme(ThemeMode theme)
    {
        var actualTheme = theme == ThemeMode.System ? GetSystemTheme() : theme;
        _actualTheme = actualTheme;

        var mergedDicts = Application.Current.Resources.MergedDictionaries;

        // 查找并替换主题字典
        ResourceDictionary? themeDict = null;
        int themeIndex = -1;

        for (int i = 0; i < mergedDicts.Count; i++)
        {
            var source = mergedDicts[i].Source?.ToString() ?? "";
            if (source.Contains("Theme.Light") || source.Contains("Theme.Dark"))
            {
                themeDict = mergedDicts[i];
                themeIndex = i;
                break;
            }
        }

        if (themeIndex >= 0)
        {
            mergedDicts.RemoveAt(themeIndex);

            var newThemePath = actualTheme == ThemeMode.Dark ? DarkThemePath : LightThemePath;
            var newThemeDict = new ResourceDictionary
            {
                Source = new Uri(newThemePath, UriKind.Relative)
            };

            mergedDicts.Insert(themeIndex, newThemeDict);

            _logger.LogDebug("Applied theme: {Theme}", actualTheme);
        }
        else
        {
            _logger.LogWarning("Theme dictionary not found in MergedDictionaries");
        }
    }

    private ThemeMode GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read system theme preference");
        }

        return ThemeMode.Light;
    }

    private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _currentTheme == ThemeMode.System)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ApplyTheme(ThemeMode.System);
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(
                    _currentTheme, _currentTheme, _actualTheme));
            });
        }
    }

    private ThemeMode LoadSavedTheme()
    {
        try
        {
            // TODO: 从配置服务加载
            // 暂时使用默认值
            return ThemeMode.Light;
        }
        catch
        {
            return ThemeMode.Light;
        }
    }

    private void SaveTheme(ThemeMode theme)
    {
        try
        {
            // TODO: 保存到配置服务
            _logger.LogDebug("Theme preference saved: {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save theme preference");
        }
    }
}
```

**Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Services/ThemeService.cs
git commit -m "feat(theme): implement ThemeService with ResourceDictionary hot-swap"
```

---

### Task 2.3: 注册ThemeService到DI容器

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Step 1: 添加ThemeService注册**

在服务注册部分添加：
```csharp
services.AddSingleton<IThemeService, ThemeService>();
```

**Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/Shell.csproj --no-restore -v q`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(theme): register ThemeService in DI container"
```

---

### Task 2.4: 在AccountSettingsView添加主题切换UI

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/AccountSettingsView.xaml`
- Modify: `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`

**Step 1: 在ViewModel添加主题切换命令和属性**

```csharp
// 在AccountSettingsViewModel中添加:

[ObservableProperty]
private ThemeMode _selectedTheme;

public IEnumerable<ThemeMode> AvailableThemes { get; } = Enum.GetValues<ThemeMode>();

partial void OnSelectedThemeChanged(ThemeMode value)
{
    _themeService.SetTheme(value);
}

// 在构造函数中初始化:
_selectedTheme = _themeService.CurrentTheme;
```

**Step 2: 在View添加主题选择ComboBox**

```xml
<!-- 在AccountSettingsView.xaml中添加主题设置区域 -->
<GroupBox Header="外观设置" Margin="0,16,0,0">
    <StackPanel Margin="8">
        <TextBlock Text="主题模式" FontWeight="SemiBold" Margin="0,0,0,8"/>
        <ComboBox ItemsSource="{Binding AvailableThemes}"
                  SelectedItem="{Binding SelectedTheme}"
                  Width="200"
                  HorizontalAlignment="Left">
            <ComboBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock>
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding}" Value="Light">
                                        <Setter Property="Text" Value="浅色"/>
                                    </DataTrigger>
                                    <DataTrigger Binding="{Binding}" Value="Dark">
                                        <Setter Property="Text" Value="深色"/>
                                    </DataTrigger>
                                    <DataTrigger Binding="{Binding}" Value="System">
                                        <Setter Property="Text" Value="跟随系统"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>
                </DataTemplate>
            </ComboBox.ItemTemplate>
        </ComboBox>
    </StackPanel>
</GroupBox>
```

**Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln --no-restore -v q`
Expected: Build succeeded

**Step 4: 测试主题切换**

Run: 启动应用，导航到账户设置，切换主题
Expected: 主题实时切换，UI颜色变化

**Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/Views/AccountSettingsView.xaml
git add src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs
git commit -m "feat(theme): add theme switching UI in AccountSettingsView"
```

---

## Phase 3-6 任务概要

由于篇幅限制，Phase 3-6的详细步骤将在后续文档中补充。主要任务包括:

### Phase 3: DynamicResource迁移 (5天)
- Task 3.1-3.10: 迁移Shell层XAML (~30文件)
- Task 3.11-3.20: 迁移Infrastructure层XAML
- Task 3.21-3.40: 迁移Modules层XAML
- Task 3.41-3.45: 清理硬编码颜色

### Phase 4: 组件样式统一 (3天)
- Task 4.1: 统一Button样式定义
- Task 4.2: 统一TextBox样式定义
- Task 4.3: 统一DataGrid样式定义
- Task 4.4: 移除Colors.xaml重复定义
- Task 4.5: 清理CommonStyles.xaml冲突

### Phase 5: UX增强 (4天)
- Task 5.1-5.2: 实现ToastControl + INotificationService
- Task 5.3: 添加动画资源Animations.xaml
- Task 5.4-5.6: 优化TabIndex
- Task 5.7-5.10: 添加快捷键支持

### Phase 6: 测试与收尾 (2天)
- Task 6.1: 全功能回归测试
- Task 6.2: 性能测试
- Task 6.3: 更新CLAUDE.md文档

---

**预计总工作量**: 19天 (含20%缓冲约23个工作日)

**验证命令**:
```bash
# 每个Task后验证编译
dotnet build LYBTZYZS.sln -c Release --no-restore

# Phase 3后验证迁移完成度
Get-ChildItem -Recurse -Include *.xaml | Select-String "StaticResource.*Brush" | Measure-Object

# 最终验证
# 1. 应用启动无错误
# 2. 浅色/深色主题切换正常
# 3. 所有功能回归测试通过
```
