# WPF 设计系统统一 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将三套并存的颜色系统统一为一套 TCM 棕色系设计 token，合并 21 个分散的资源文件为 3 个权威文件。

**Architecture:** 先创建 `DesignSystem.xaml` 作为唯一权威 token 来源，在 App.xaml 中加载；然后用兼容期策略逐步将旧键引用替换为标准键；最后删除冗余文件和旧键。

**Tech Stack:** WPF / XAML / HandyControl / Prism

---

### Task 1: 创建 DesignSystem.xaml 权威文件

**Covers:** [S3], [S4], [S5]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignSystem.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml` — 在 MergedDictionaries 中加入 DesignSystem.xaml（放在 TCM.Theme.xaml 之后，作为最终权威）

- [ ] **Step 1: 创建 DesignSystem.xaml**

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- ========== 色彩系统 ========== -->

    <!-- 主色阶 (TCM 棕色系) -->
    <Color x:Key="PrimaryColor">#5D4037</Color>
    <Color x:Key="DarkPrimaryColor">#4E342E</Color>
    <Color x:Key="LightPrimaryColor">#D7CCC8</Color>

    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="DarkPrimaryBrush" Color="{StaticResource DarkPrimaryColor}" />
    <SolidColorBrush x:Key="LightPrimaryBrush" Color="{StaticResource LightPrimaryColor}" />

    <!-- 强调色 -->
    <Color x:Key="AccentColor">#8D6E63</Color>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />

    <!-- 功能色 -->
    <Color x:Key="SuccessColor">#2E8B57</Color>
    <Color x:Key="WarningColor">#DAA520</Color>
    <Color x:Key="DangerColor">#C75050</Color>
    <Color x:Key="InfoColor">#5B8FA8</Color>

    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}" />
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="DangerBrush" Color="{StaticResource DangerColor}" />
    <SolidColorBrush x:Key="InfoBrush" Color="{StaticResource InfoColor}" />

    <!-- 浅色功能色背景 -->
    <Color x:Key="LightSuccessColor">#E8F5E9</Color>
    <Color x:Key="LightWarningColor">#FFF8E1</Color>
    <Color x:Key="LightDangerColor">#FDE7E9</Color>
    <Color x:Key="LightInfoColor">#E0F7FA</Color>

    <SolidColorBrush x:Key="LightSuccessBrush" Color="{StaticResource LightSuccessColor}" />
    <SolidColorBrush x:Key="LightWarningBrush" Color="{StaticResource LightWarningColor}" />
    <SolidColorBrush x:Key="LightDangerBrush" Color="{StaticResource LightDangerColor}" />
    <SolidColorBrush x:Key="LightInfoBrush" Color="{StaticResource LightInfoColor}" />

    <!-- 深色功能色 -->
    <Color x:Key="DarkSuccessColor">#228B22</Color>
    <Color x:Key="DarkWarningColor">#B8860B</Color>
    <Color x:Key="DarkDangerColor">#A4262C</Color>
    <Color x:Key="DarkInfoColor">#3D5A80</Color>

    <SolidColorBrush x:Key="DarkSuccessBrush" Color="{StaticResource DarkSuccessColor}" />
    <SolidColorBrush x:Key="DarkWarningBrush" Color="{StaticResource DarkWarningColor}" />

    <!-- 中性色 -->
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="BorderColor">#E0E0E0</Color>

    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}" />
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}" />

    <!-- 文字色 -->
    <Color x:Key="PrimaryTextColor">#212121</Color>
    <Color x:Key="SecondaryTextColor">#757575</Color>
    <Color x:Key="DisabledTextColor">#BDBDBD</Color>

    <SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource PrimaryTextColor}" />
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource SecondaryTextColor}" />
    <SolidColorBrush x:Key="DisabledTextBrush" Color="{StaticResource DisabledTextColor}" />

    <!-- 暗色遮罩 -->
    <Color x:Key="DarkMaskColor">#20000000</Color>
    <SolidColorBrush x:Key="DarkMaskBrush" Color="{StaticResource DarkMaskColor}" />

    <!-- 阴影色 -->
    <Color x:Key="ShadowColor">#000000</Color>
    <SolidColorBrush x:Key="ShadowBrush" Color="{StaticResource ShadowColor}" />

    <!-- ========== TCM 棕色兼容别名 (迁移期保留，Task 5 删除) ========== -->
    <SolidColorBrush x:Key="LYBTTcmPrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="LYBTTcmSecondaryBrush" Color="{StaticResource DarkPrimaryColor}" />
    <SolidColorBrush x:Key="LYBTTcmDarkBrush" Color="#3E2723" />
    <SolidColorBrush x:Key="LYBTTcmAccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="LYBTTcmLightBrush" Color="#BCAAA4" />
    <SolidColorBrush x:Key="LYBTTcmSurfaceBrush" Color="#F8F5F3" />
    <SolidColorBrush x:Key="LYBTTcmHoverBrush" Color="#EFEBE9" />

    <!-- LYBT 旧别名 (迁移期保留，Task 5 删除) -->
    <SolidColorBrush x:Key="LYBTPrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="LYBTSuccessBrush" Color="{StaticResource SuccessColor}" />
    <SolidColorBrush x:Key="LYBTWarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="LYBTErrorBrush" Color="{StaticResource DangerColor}" />
    <SolidColorBrush x:Key="LYBTInfoBrush" Color="{StaticResource InfoColor}" />
    <SolidColorBrush x:Key="LYBTBackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="LYBTSurfaceBrush" Color="{StaticResource SurfaceColor}" />
    <SolidColorBrush x:Key="LYBTBorderBrush" Color="{StaticResource BorderColor}" />
    <SolidColorBrush x:Key="LYBTTextPrimaryBrush" Color="{StaticResource PrimaryTextColor}" />
    <SolidColorBrush x:Key="LYBTTextSecondaryBrush" Color="{StaticResource SecondaryTextColor}" />
    <SolidColorBrush x:Key="LYBTTextDisabledBrush" Color="{StaticResource DisabledTextColor}" />

    <!-- ========== 字号系统 ========== -->
    <FontFamily x:Key="PrimaryFontFamily">Segoe UI Variable, Segoe UI, Microsoft YaHei UI</FontFamily>
    <sys:Double x:Key="FontSizeCaption">12</sys:Double>
    <sys:Double x:Key="FontSizeBody">14</sys:Double>
    <sys:Double x:Key="FontSizeSubtitle">20</sys:Double>
    <sys:Double x:Key="FontSizeTitle">28</sys:Double>
    <sys:Double x:Key="FontSizeDisplay">40</sys:Double>

    <!-- ========== 间距系统 ========== -->
    <Thickness x:Key="SpacingXSmall">4</Thickness>
    <Thickness x:Key="SpacingSmall">8</Thickness>
    <Thickness x:Key="SpacingMedium">12</Thickness>
    <Thickness x:Key="SpacingLarge">16</Thickness>
    <Thickness x:Key="SpacingXLarge">24</Thickness>
    <Thickness x:Key="SpacingXXLarge">32</Thickness>

    <!-- ========== 圆角系统 ========== -->
    <CornerRadius x:Key="CornerRadiusSmall">4</CornerRadius>
    <CornerRadius x:Key="CornerRadiusMedium">8</CornerRadius>
    <CornerRadius x:Key="CornerRadiusLarge">12</CornerRadius>

    <!-- ========== 阴影系统 ========== -->
    <DropShadowEffect x:Key="CardShadow" BlurRadius="12" Direction="270" Opacity="0.12" ShadowDepth="4" Color="#000000" />
    <DropShadowEffect x:Key="FloatingShadow" BlurRadius="16" Direction="270" Opacity="0.16" ShadowDepth="8" Color="#000000" />
    <DropShadowEffect x:Key="SubtleShadow" BlurRadius="4" Direction="270" Opacity="0.08" ShadowDepth="1" Color="#000000" />

</ResourceDictionary>
```

- [ ] **Step 2: 在 App.xaml 中加载 DesignSystem.xaml**

在 `App.xaml` 的 `MergedDictionaries` 中，在 `TCM.Theme.xaml` 之后添加：

```xml
<ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/DesignSystem.xaml" />
```

放在 TCM.Theme.xaml 之后确保 DesignSystem 的值覆盖 TCM.Theme 的旧定义。

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignSystem.xaml src/Client/Desktop/Shell/App.xaml
git commit -m "feat(design-system): create unified DesignSystem.xaml with TCM brown color tokens"
```

### Task 2: 删除 Colors.xaml 旧定义

**Covers:** [S3], [S6]

**Files:**
- Delete: `src/Client/Desktop/Shell/Resources/Colors.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml` — 移除 Colors.xaml 引用

- [ ] **Step 1: 从 App.xaml 移除 Colors.xaml 引用**

删除这行：
```xml
<ResourceDictionary Source="Resources/Colors.xaml" />
```

- [ ] **Step 2: 删除 Colors.xaml**

```bash
git rm src/Client/Desktop/Shell/Resources/Colors.xaml
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors（DesignSystem.xaml 中的兼容别名会接管 Colors.xaml 的所有键）

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor(design-system): remove Colors.xaml, superseded by DesignSystem.xaml"
```

### Task 3: 删除 DesignTokens.xaml（已合并到 DesignSystem.xaml）

**Covers:** [S4], [S5]

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml` — 如果有 DesignTokens.xaml 引用则移除

- [ ] **Step 1: 检查 App.xaml 是否引用 DesignTokens.xaml**

如果没有引用，跳过此步。如果有引用则删除。

- [ ] **Step 2: 删除 DesignTokens.xaml**

```bash
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens.xaml
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor(design-system): remove DesignTokens.xaml, merged into DesignSystem.xaml"
```

### Task 4: 全局替换 LYBT*Brush → 标准键

**Covers:** [S6], [S7]

**Files:** 所有引用 `LYBT*Brush` 或 `LYBTTcm*Brush` 的 .xaml 文件

- [ ] **Step 1: 找到所有引用**

Run: `rg -l "LYBTPrimaryBrush|LYBTSuccessBrush|LYBTWarningBrush|LYBTErrorBrush|LYBTInfoBrush|LYBTBackgroundBrush|LYBTSurfaceBrush|LYBTBorderBrush|LYBTTextPrimaryBrush|LYBTTextSecondaryBrush|LYBTTextDisabledBrush" src/Client/Desktop -t xml`

Run: `rg -l "LYBTTcmPrimaryBrush|LYBTTcmSecondaryBrush|LYBTTcmDarkBrush|LYBTTcmAccentBrush|LYBTTcmLightBrush|LYBTTcmSurfaceBrush|LYBTTcmHoverBrush" src/Client/Desktop -t xml`

- [ ] **Step 2: 全局替换**

对每个文件执行以下替换（使用 search-and-replace）：

| 旧键 | 新键 |
|------|------|
| `LYBTPrimaryBrush` | `PrimaryBrush` |
| `LYBTSuccessBrush` | `SuccessBrush` |
| `LYBTWarningBrush` | `WarningBrush` |
| `LYBTErrorBrush` | `DangerBrush` |
| `LYBTInfoBrush` | `InfoBrush` |
| `LYBTBackgroundBrush` | `BackgroundBrush` |
| `LYBTSurfaceBrush` | `SurfaceBrush` |
| `LYBTBorderBrush` | `BorderBrush` |
| `LYBTTextPrimaryBrush` | `PrimaryTextBrush` |
| `LYBTTextSecondaryBrush` | `SecondaryTextBrush` |
| `LYBTTextDisabledBrush` | `DisabledTextBrush` |
| `LYBTTcmPrimaryBrush` | `PrimaryBrush` |
| `LYBTTcmSecondaryBrush` | `DarkPrimaryBrush` |
| `LYBTTcmDarkBrush` | `DarkPrimaryBrush` |
| `LYBTTcmAccentBrush` | `AccentBrush` |
| `LYBTTcmLightBrush` | `LightPrimaryBrush` |
| `LYBTTcmSurfaceBrush` | `SurfaceBrush` |
| `LYBTTcmHoverBrush` | `LightPrimaryBrush` |

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor(design-system): replace all LYBT*/TCM* brush keys with standard tokens"
```

### Task 5: 删除兼容别名 + 清理冗余 Theme 文件

**Covers:** [S5], [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignSystem.xaml` — 删除兼容别名区块
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml` (已被 DesignSystem 取代)
- Delete: `src/Client/Desktop/Shell/Resources/Spacing.xaml` (间距已在 DesignSystem 中)
- Delete: `src/Client/Desktop/Shell/Resources/LayoutStyles.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml` — 移除已删除文件的引用

- [ ] **Step 1: 从 DesignSystem.xaml 删除兼容别名区块**

删除 `<!-- TCM 棕色兼容别名 -->` 和 `<!-- LYBT 旧别名 -->` 两个区块。

- [ ] **Step 2: 删除冗余文件**

```bash
git rm src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/TCM.Theme.xaml
git rm src/Client/Desktop/Shell/Resources/Spacing.xaml
git rm src/Client/Desktop/Shell/Resources/LayoutStyles.xaml
```

- [ ] **Step 3: 从 App.xaml 移除已删文件的 MergedDictionaries 引用**

- [ ] **Step 4: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor(design-system): remove compatibility aliases and redundant theme files"
```

### Task 6: 最终构建 + 可视化验证

**Covers:** [S2], [S8]

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 验证无遗留旧键**

Run: `rg "LYBTPrimaryBrush|LYBTTcmPrimaryBrush|LYBTSuccessBrush" src/Client/Desktop`
Expected: 0 results

- [ ] **Step 3: 提交最终状态**

```bash
git add -A
git commit -m "feat(design-system): unified design system complete — single source of truth"
```
