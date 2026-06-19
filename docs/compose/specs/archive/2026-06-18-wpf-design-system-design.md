# WPF 设计系统统一 — 设计规格

> 日期: 2026-06-18
> 子项目: A (设计系统) — 全面重构的第一步

## [S1] 问题

当前 WPF 客户端存在三套颜色体系并存的问题：
- **HandyControl 标准色**（`PrimaryColor=#2E8B57` 绿色）— 覆盖在 `TCM.Theme.xaml`
- **LYBT 自定义色**（`LYBTPrimaryBrush=#2563EB` 蓝色）— 定义在 `Colors.xaml`，几乎未使用
- **TCM 棕色系**（`LYBTTcmPrimaryBrush=#5D4037`）— 定义在 `Colors.xaml`，仅登录界面使用

DesignTokens.xaml 定义了间距/字号/圆角系统，但几乎未被引用——各处仍在使用硬编码数值（如 `Margin="28,0,0,20"`、`FontSize="14"`）。

资源文件分散在 21 个文件中，无单一权威来源。

## [S2] 目标

1. 统一为一套以 TCM 棕色系为主色的设计 token 系统
2. 所有界面元素（登录、管理、工作台、对话框）使用同一套颜色
3. 间距/字号/圆角统一使用 DesignTokens，消除硬编码数值
4. 合并冗余资源文件，建立清晰的文件结构

## [S3] 色彩系统

### 主色阶（TCM 棕色系）

| Token | 色值 | 用途 |
|-------|------|------|
| `PrimaryColor` / `LYBTPrimaryBrush` | `#5D4037` | 主品牌色——按钮、导航高亮、标题 |
| `PrimaryDarkColor` | `#4E342E` | hover/pressed 状态 |
| `PrimaryLightColor` | `#BCAAA4` | 禁用状态、浅色背景 |
| `AccentColor` | `#8D6E63` | 强调色——链接、图标、次要按钮 |

### 功能色

| Token | 色值 | 用途 |
|-------|------|------|
| `SuccessColor` | `#2E8B57` | 成功状态（保留木绿） |
| `WarningColor` | `#DAA520` | 警告状态 |
| `DangerColor` | `#C75050` | 错误/删除 |
| `InfoColor` | `#5B8FA8` | 信息提示 |

### 中性色

| Token | 色值 | 用途 |
|-------|------|------|
| `BackgroundBrush` | `#FAFAFA` | 全局背景 |
| `SurfaceBrush` | `#FFFFFF` | 卡片/面板背景 |
| `BorderBrush` | `#E0E0E0` | 分割线/边框 |
| `TextPrimary` | `#212121` | 主文字 |
| `TextSecondary` | `#757575` | 次要文字 |
| `TextDisabled` | `#BDBDBD` | 禁用文字 |

### 暗色遮罩

| Token | 色值 | 用途 |
|-------|------|------|
| `DarkMaskBrush` | `#20000000` | 全屏遮罩层 |

## [S4] 字号/间距/圆角系统

沿用现有 `DesignTokens.xaml` 定义，不做修改。所有 XAML 中的硬编码数值应替换为 token 引用。

### 关键 token

| 类别 | Token | 值 |
|------|-------|-----|
| 字号 | `FontSizeBody` | 14 |
| 字号 | `FontSizeCaption` | 12 |
| 字号 | `FontSizeSubtitle` | 20 |
| 字号 | `FontSizeTitle` | 28 |
| 间距 | `SpacingSmall` | 4 |
| 间距 | `SpacingMedium` | 12 |
| 间距 | `SpacingLarge` | 16 |
| 间距 | `SpacingXLarge` | 24 |
| 圆角 | `CornerRadiusSmall` | 4 |
| 圆角 | `CornerRadiusMedium` | 8 |
| 圆角 | `CornerRadiusLarge` | 12 |

## [S5] 文件结构

### 合并前（21 文件，3 个位置）

```
Core/Infrastructure/Themes/     (15 files)
  TCM.Theme.xaml, DesignTokens.xaml, Theme.Light.xaml,
  UnifiedComponents.xaml, HomePageStyles.xaml, MedicalCaseStyles.xaml,
  ButtonStyles.xaml, DataGridStyles.xaml, PanelStyles.xaml,
  InputStyles.xaml, ValidationStyles.xaml, PreviewStyles.xaml,
  BreadcrumbStyles.xaml, NavigationHistoryPanelStyles.xaml,
  NavigationSuggestionsPanelStyles.xaml

Shell/Resources/                (3 files)
  Colors.xaml, Spacing.xaml, LayoutStyles.xaml

Shell/Styles/                   (3 files)
  Controls.xaml, Typography.xaml, DialogStyles.xaml
```

### 合并后（1 个权威文件 + 按功能分组的样式）

```
Core/Infrastructure/Themes/
  DesignSystem.xaml          ← 唯一权威：所有颜色 token + 字号 + 间距 + 圆角
  ComponentStyles.xaml       ← 所有控件样式（合并 ButtonStyles + Controls + UnifiedComponents 等）
  PageStyles.xaml            ← 页面级样式（合并 HomePageStyles + MedicalCaseStyles + PanelStyles 等）

Shell/Styles/
  DialogStyles.xaml          ← 保持（对话框样式与 Shell 耦合）

# 删除：Colors.xaml, Spacing.xaml, LayoutStyles.xaml, Typography.xaml, Theme.Light.xaml,
#        PreviewStyles.xaml, ValidationStyles.xaml, NavigationHistoryPanelStyles.xaml,
#        NavigationSuggestionsPanelStyles.xaml
```

## [S6] HandyControl 集成策略

HandyControl 作为控件库基底层保留。策略：

1. **覆盖关键色**：在 `DesignSystem.xaml` 中覆盖 HandyControl 的 `PrimaryColor`、`DangerColor` 等标准键
2. **不创建别名**：不再使用 `LYBTPrimaryBrush` 等自定义键，直接用 HandyControl 标准键 `PrimaryBrush`
3. **删除冗余**：移除 `Colors.xaml` 中的所有 `LYBT*Brush` 定义，替换引用为标准键

## [S7] 迁移策略

采用**渐进式替换**：

1. 先创建 `DesignSystem.xaml` 并在 `App.xaml` 中加载
2. 在 `DesignSystem.xaml` 中同时定义新旧两套键（兼容期）
3. 逐文件将 `LYBTPrimaryBrush` → `PrimaryBrush` 等替换
4. 替换硬编码数值为 DesignTokens 引用
5. 最后删除旧键定义

## [S8] 不在范围内

- **UI 布局重设计**（子项目 B 工作流优化中处理）
- **新增控件**（不在设计系统统一范围内）
- **打印模板样式**（PrescriptionPrintTemplate 等保持独立）
