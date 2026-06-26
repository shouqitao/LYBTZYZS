# LYBT Desktop 设计框架现状分析

> 日期: 2026-06-26 | 基于 frontend-design skill 分析

---

## 1. 主题体系 (Theme System)

### 1.1 基础主题

**Material Design In XAML Toolkit (MDIX)** — Material Design 2

| 配置项 | 值 |
|--------|-----|
| BaseTheme | Light |
| PrimaryColor | Brown (#5D4037) |
| SecondaryColor | Amber |
| Defaults | MaterialDesign2.Defaults.xaml |
| 主题切换 | `ThemeService` 通过 `PaletteHelper.SetTheme()` 支持 Dark/Light |

### 1.2 资源加载链 (App.xaml)

```
1. MDIX BundledTheme (Light/Brown/Amber)
2. MaterialDesign2.Defaults.xaml (MDIX 控件默认样式)
3. DesignSystem.xaml (项目 Token: 颜色/字体/间距/圆角/阴影)
4. Icons.xaml (矢量图标 Geometry)
5. Theme.Light.xaml (空占位符 — 历史遗留)
6. UnifiedComponents.xaml (聚合器 → ButtonStyles + InputStyles + DataGridStyles + PanelStyles + PreviewStyles + ValidationStyles)
7. HomePageStyles.xaml (首页卡片样式)
8. Typography.xaml (排版系统)
9. Controls.xaml (Shell 控件样式)
10. DialogStyles.xaml (对话框样式)
11. Converters.xaml (转换器)
```

### 1.3 主题切换

```csharp
// ThemeService.cs — 通过 MDIX PaletteHelper 全局切换
theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
_paletteHelper.SetTheme(theme);
```

**Sysadmin 暗色主题：** `SysadminDarkTheme.xaml` 定义 Ops* 语义别名，映射到 MDIX Dark 资源。

---

## 2. 颜色系统 (Color System)

### 2.1 主色阶

| Token | 色值 | 用途 |
|-------|------|------|
| `PrimaryColor` | #5D4037 (Brown 700) | 主色 — 按钮、侧边栏、强调 |
| `DarkPrimaryColor` | #4E342E (Brown 900) | 深色变体 — 渐变结束色 |
| `LightPrimaryColor` | #D7CCC8 (Brown 100) | 浅色变体 — 渐变开始色、背景 |
| `AccentColor` | #8D6E63 (Brown 400) | 强调色 — 头像、辅助 |

### 2.2 语义色

| Token | 色值 | 用途 |
|-------|------|------|
| `SuccessColor` | #2E8B57 | 成功状态 |
| `WarningColor` | #DAA520 | 警告状态 |
| `DangerColor` | #C75050 | 危险/错误 |
| `InfoColor` | #5B8FA8 | 信息提示 |

**每个语义色都有 3 个变体：** Light（背景）/ Standard（图标）/ Dark（深色模式）

### 2.3 中性色

| Token | 色值 | 用途 |
|-------|------|------|
| `BackgroundColor` | #FAFAFA | 页面背景 |
| `SurfaceColor` | #FFFFFF | 卡片/面板背景 |
| `BorderColor` | #E0E0E0 | 边框 |
| `PrimaryTextColor` | #212121 | 主文字 |
| `SecondaryTextColor` | #757575 | 次要文字 |
| `DisabledTextColor` | #BDBDBD | 禁用文字 |

### 2.4 MDIX 资源使用

视图层直接引用 MDIX 原生资源键：

| MDIX 资源键 | 用途 |
|------------|------|
| `MaterialDesignPaper` | 页面/窗口背景 |
| `MaterialDesignBody` | 卡片背景、主文字 |
| `MaterialDesignBodyLight` | 次要文字 |
| `MaterialDesignOutline` | 边框 |
| `MaterialDesignDivider` | 分隔线、禁用背景 |
| `MaterialDesign.Brush.Primary` | 侧边栏背景 |
| `MaterialDesign.Brush.Primary.Light` | 分隔线（侧边栏内） |
| `MaterialDesign.Brush.Surface` | 内容区域背景 |
| `MaterialDesign.Brush.Foreground` | 文字前景 |
| `MaterialDesign.Brush.Outline` | 边框 |
| `MaterialDesignBrush.Success` | 成功 |
| `MaterialDesignBrush.Error` | 错误 |
| `MaterialDesignBrush.Warning` | 警告 |

---

## 3. 排版系统 (Typography)

### 3.1 字体

| Token | 值 |
|-------|-----|
| `PrimaryFontFamily` | `Segoe UI Variable, Segoe UI, Microsoft YaHei UI` |

### 3.2 字号系统（存在两套）

**DesignSystem.xaml（旧）：**

| Token | 值 | 用途 |
|-------|-----|------|
| `FontSizeCaption` | 12 | 说明文字 |
| `FontSizeBody` | 14 | 正文 |
| `FontSizeSubtitle` | 20 | 副标题 |
| `FontSizeTitle` | 28 | 标题 |
| `FontSizeDisplay` | 40 | 大标题 |

**Typography.xaml（新）：**

| Token | 值 | 用途 |
|-------|-----|------|
| `FontSizeXS` | 12 | 极小 |
| `FontSizeSM` | 13 | 小 |
| `FontSizeMD` | 14 | 中（基准） |
| `FontSizeLG` | 16 | 大 |
| `FontSizeXL` | 20 | 特大 |
| `FontSizeXXL` | 24 | 超大 |

⚠️ **两套字号系统并存**，部分视图用旧 Token，部分用新 Token。

### 3.3 文本样式 (Typography.xaml)

| 样式 Key | FontSize | FontWeight | 用途 |
|----------|----------|------------|------|
| `H1TextBlock` | 20 | Bold | 一级标题 |
| `H2TextBlock` | 16 | Bold | 二级标题 |
| `BodyTextBlock` | 14 | Normal | 正文 |
| `CaptionTextBlock` | 12 | Normal | 说明（次要色） |
| `LabelTextBlock` | 13 | Medium | 表单标签 |
| `PageTitle` | 24 | Bold | 页面标题 |
| `SectionHeader` | 16 | Bold | 区块标题 |
| `PanelTitle` | 16 | Bold | 面板标题 |
| `FieldLabel` | 13 | Normal | 字段标签（次要色） |
| `HintText` | 12 | Normal | 提示（MaterialDesignBodyLight） |
| `StatusText` | 13 | — | 状态文本（继承） |
| `SuccessText` | 13 | — | 成功（继承 StatusText） |
| `WarningText` | 13 | — | 警告（继承 StatusText） |
| `ErrorText` | 13 | — | 错误（继承 StatusText） |

**全局 TextBlock 默认样式：** FontSize=14, PrimaryFontFamily, PrimaryTextBrush

### 3.4 DesignSystem.xaml 内嵌样式

| 样式 Key | 类型 | 用途 |
|----------|------|------|
| `HeaderText` | TextBlock | 28px Bold |
| `TitleText` | TextBlock | 18px SemiBold |
| `LabelText` | TextBlock | 14px Medium（次要色） |

⚠️ **与 Typography.xaml 的 H1/H2/Label 功能重叠**。

---

## 4. 间距与圆角 (Spacing & Radius)

### 4.1 间距 Token

| Token | 值 | 用途 |
|-------|-----|------|
| `SpacingXS` | 4 | 极小间距 |
| `SpacingSM` | 8 | 小间距 |
| `SpacingMD` | 12 | 中间距 |
| `SpacingLG` | 16 | 大间距 |
| `SpacingXL` | 24 | 特大间距 |
| `SpacingXXL` | 32 | 超大间距 |
| `SpacingXXXL` | 48 | 最大间距 |

### 4.2 圆角 Token

| Token | 值 | 用途 |
|-------|-----|------|
| `RadiusSM` | 4 | 小圆角（按钮、输入框） |
| `RadiusMD` | 8 | 中圆角（卡片、面板） |
| `RadiusLG` | 12 | 大圆角（功能卡片） |

### 4.3 阴影 Token

| Token | BlurRadius | Opacity | ShadowDepth | 用途 |
|-------|-----------|---------|-------------|------|
| `CardShadow` | 12 | 0.12 | 4 | 卡片 |
| `FloatingShadow` | 16 | 0.16 | 8 | 悬浮元素 |
| `SubtleShadow` | 4 | 0.08 | 1 | 微妙阴影 |

---

## 5. 组件体系 (Component System)

### 5.1 UnifiedComponents.xaml 聚合器

加载顺序：
```
Converters → PreviewStyles → ValidationStyles → DesignSystem → ButtonStyles → InputStyles → DataGridStyles → PanelStyles
```

### 5.2 按钮样式 (ButtonStyles.xaml)

| 样式 Key | 特征 |
|----------|------|
| `PrimaryButton` | 棕色背景，白色文字 |
| `SecondaryButton` | 透明背景，棕色边框 |
| `SuccessButton` | 绿色背景 |
| `DangerButton` | 红色背景 |
| `WarningButton` | 黄色背景 |
| `InfoButton` | 蓝色背景 |

所有按钮统一：CornerRadius=RadiusSM, FontSize=FontSizeBody

### 5.3 输入样式 (InputStyles.xaml)

| 样式 Key | 特征 |
|----------|------|
| `StandardTextBox` | 36px 高，圆角 2px |
| `FormTextBox` | 最小 36px，圆角 |
| 悬停 | 边框变 PrimaryBrush |
| 聚焦 | 边框变 PrimaryBrush + 2px |
| 禁用 | 背景变 MaterialDesignDivider |

### 5.4 面板样式 (PanelStyles.xaml)

| 样式 Key | 特征 |
|----------|------|
| `ToolBarContainer` | 工具栏容器 |
| `ContentContainer` | 内容容器（白底、边框、8px 圆角） |
| `PanelCard` | 面板卡片 |
| `ActionBar` | 操作栏 |
| `Divider` | 1px 分隔线 |
| `Card` | 通用卡片 |

### 5.5 首页卡片样式 (HomePageStyles.xaml)

| 样式 Key | 尺寸 | 特征 |
|----------|------|------|
| `FunctionCardStyle` | 200x180 | 白底 + 边框 + 12px 圆角 + 阴影，悬停变棕框 |
| `PrimaryFunctionCardStyle` | 320x200 | 棕色渐变背景 + 强阴影 |
| `StatsCardStyle` | 200x200 | 白底 + 边框 + 阴影 |
| `CardIconStyle` | 48x48 | Path 矢量图标，棕色填充 |
| `CardTitleStyle` | 18px | SemiBold，主文字色 |
| `TransparentCardButton` | — | 透明背景按钮（卡片内点击容器） |

### 5.6 DataGrid 样式 (DataGridStyles.xaml)

- 交替行背景：`MaterialDesignBody`
- 悬停行背景：`MaterialDesignPaper`
- 列头背景：`MaterialDesignBody`，主文字色
- 边框：`MaterialDesignOutline`

---

## 6. 布局体系 (Layout System)

### 6.1 MainWindow 结构

```
Window (MaterialDesignWindow, WindowStyle=None, Maximized)
├── LoginRegion (登录前)
└── Post-login Shell (登录后)
    └── DialogHost (RootDialog)
        └── Grid (2 列 + 2 行)
            ├── Sidebar (Column 0, RowSpan 2)
            │   Background: MaterialDesign.Brush.Primary
            │   ├── Hamburger ToggleButton
            │   ├── Logo + "凌隐宝堂"
            │   ├── Navigation ListBox (MaterialDesignNavigationPrimaryListBox)
            │   └── Bottom: 账户设置 + 主题切换 + 退出
            ├── ContentRegion (Column 1, Row 0)
            │   Background: MaterialDesign.Brush.Surface
            │   Margin: 4, CornerRadius: 4
            └── StatusBar (Column 1, Row 1, Height: 32)
                Background: MaterialDesign.Brush.Surface
                Border: MaterialDesign.Brush.Outline
                ├── API 状态图标 + 颜色
                ├── 连接模式显示
                ├── 当前用户
                └── 当前时间
```

### 6.2 最小分辨率

- **目标：** 1920x1080 (Full HD)
- **最小：** 1024x768
- **默认：** Maximized

### 6.3 角色视图布局

| 角色 | 布局 | 背景 |
|------|------|------|
| Admin | 3x3 居中卡片网格 | `MaterialDesignPaper` |
| Clinical | 同 Admin | `MaterialDesignPaper` |
| Receptionist | 同 Admin | `MaterialDesignPaper` |
| Sysadmin | 2x2 卡片网格 + 导航按钮 | `MaterialDesignPaper` |

---

## 7. 图标系统 (Icon System)

### 7.1 矢量图标 (Icons.xaml)

使用 WPF `Geometry` 路径数据，Material Design 风格：

| 图标 Key | 用途 |
|----------|------|
| `IconHome` | 主页 |
| `IconPatients` | 患者管理 |
| `IconHerbs` | 药材管理 |
| `IconFormula` | 验方管理 |
| `IconMedicalCase` | 医案管理 |
| `IconUsers` | 用户管理 |
| `IconReports` | 统计报表 |
| `IconSettings` | 系统设置 |
| `IconLogout` | 退出 |
| `IconApiHealthy` | API 正常 |
| `IconApiUnhealthy` | API 异常 |
| `IconRegistration` | 挂号 |
| `IconCard` | 卡片 |
| `IconRefresh` | 刷新 |
| `IconStethoscope` | 听诊器 |

### 7.2 MDIX PackIcon

侧边栏使用 MDIX 内置 `PackIcon`：
- `Leaf` — 品牌 Logo
- `AccountEdit` — 账户设置
- `Logout` — 退出
- 各种导航图标通过 `Binding IconKind` 动态绑定

---

## 8. 已知问题

| # | 问题 | 严重度 | 影响范围 |
|---|------|--------|---------|
| 1 | 双字号系统并存（FontSizeCaption vs FontSizeXS） | 中 | 全局 |
| 2 | DesignSystem 内嵌样式与 Typography 重叠（HeaderText vs H1TextBlock） | 低 | 全局 |
| 3 | Theme.Light.xaml 是空占位符 | 低 | 加载链冗余 |
| 4 | Controls.xaml 中硬编码 `Background="White"` | 中 | Dark 主题适配 |
| 5 | 部分视图硬编码字号（如 FontSize="28"）而非使用 Token | 低 | 一致性 |
| 6 | Controls.xaml 中硬编码 `FontFamily="Microsoft YaHei"` 而非使用 `PrimaryFontFamily` | 低 | 一致性 |

---

## 9. 设计框架总览图

```
┌─────────────────────────────────────────────────────────────┐
│                      App.xaml (全局资源)                      │
├─────────────────────────────────────────────────────────────┤
│  MDIX BundledTheme (Light / Brown / Amber)                  │
│  └── MaterialDesign2.Defaults.xaml                          │
├─────────────────────────────────────────────────────────────┤
│  DesignSystem.xaml                                          │
│  ├── 颜色: Primary(#5D4037) / Accent / Semantic             │
│  ├── 字号: Caption(12) / Body(14) / Subtitle(20) / Title(28)│
│  ├── 间距: XS(4) / SM(8) / MD(12) / LG(16) / XL(24) / XXL(32)│
│  ├── 圆角: SM(4) / MD(8) / LG(12)                          │
│  ├── 阴影: Card / Floating / Subtle                        │
│  └── 内嵌样式: HeaderText / TitleText / LabelText           │
├─────────────────────────────────────────────────────────────┤
│  UnifiedComponents.xaml (聚合器)                             │
│  ├── ButtonStyles.xaml (Primary/Secondary/Success/Danger...) │
│  ├── InputStyles.xaml (Standard/Form TextBox)               │
│  ├── DataGridStyles.xaml                                   │
│  └── PanelStyles.xaml (ToolBar/Content/PanelCard/ActionBar) │
├─────────────────────────────────────────────────────────────┤
│  HomePageStyles.xaml                                        │
│  ├── FunctionCardStyle (200x180, 阴影+悬停)                  │
│  ├── PrimaryFunctionCardStyle (320x200, 渐变)               │
│  ├── CardIconStyle (48x48 Path)                            │
│  └── CardTitleStyle (18px SemiBold)                        │
├─────────────────────────────────────────────────────────────┤
│  Typography.xaml                                            │
│  ├── 字号: XS(12) / SM(13) / MD(14) / LG(16) / XL(20) / XXL(24)│
│  ├── 样式: H1/H2/Body/Caption/Label/PageTitle/SectionHeader │
│  ├── 状态: StatusText/SuccessText/WarningText/ErrorText     │
│  └── 全局 TextBlock 默认样式                                 │
├─────────────────────────────────────────────────────────────┤
│  Icons.xaml (Geometry 矢量图标 ×15)                         │
├─────────────────────────────────────────────────────────────┤
│  Controls.xaml (Shell 控件: StandardTextBox/FormTextBox...) │
├─────────────────────────────────────────────────────────────┤
│  DialogStyles.xaml (对话框样式)                              │
└─────────────────────────────────────────────────────────────┘
```
