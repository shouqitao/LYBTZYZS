# Desktop UI/UX 设计规范

> **文档性质**：设计态（系统应该是什么）——SSOT  
> **版本**：v1.0.0 | 2026-08-14  
> **适用范围**：LYBTZYZS Desktop（WPF/Prism.DryIoc/MaterialDesignInXAML）  
> **参考基线**：当前代码库 XAML 视图 + 控件 + 主题系统

---

## 目录

1. [设计原则](#1-设计原则)
2. [技术栈与工具链](#2-技术栈与工具链)
3. [主题与色彩系统](#3-主题与色彩系统)
4. [页面布局规范](#4-页面布局规范)
5. [导航规范](#5-导航规范)
6. [交互规范](#6-交互规范)
7. [业务流程 UX](#7-业务流程-ux)
8. [共享控件规范](#8-共享控件规范)
9. [对话框规范](#9-对话框规范)
10. [状态反馈系统](#10-状态反馈系统)
11. [键盘快捷键](#11-键盘快捷键)
12. [无障碍与国际化](#12-无障碍与国际化)
13. [当前 UX 问题清单](#13-当前-ux-问题清单)

---

## 1. 设计原则

### 1.1 核心原则

| 原则 | 定义 | 代码体现 |
|------|------|----------|
| **一致性** | 同类操作使用相同样式、间距、反馈模式 | 共享控件库 + MDIX 内置样式 + Spacing Token |
| **简洁** | 界面元素最小化，操作路径最短 | 三栏/主从布局，减少层级 |
| **反馈** | 每个操作都有可见的即时反馈 | LoadingOverlay / Snackbar / Toast / StatusBadge |
| **容错** | 操作可撤销，破坏性操作需确认 | ConfirmationDialog / UnsavedChangesDialog / IConfirmNavigationRequest |
| **角色感知** | 界面根据用户角色动态调整 | Role-based 模块加载，按钮 Visibility 按角色绑定 |

### 1.2 设计约束

- **MDIX 内置样式优先**：不自定义 ControlTemplate（项目 AGENTS.md 红线）
- **DynamicResource 全局**：所有颜色/间距用 DynamicResource 引用，支持运行时主题切换
- **控件库不可修改外部资源**：共享控件不合并 App 级资源字典，避免加载顺序冲突
- **无硬编码颜色**：所有色彩从主题资源字典获取

---

## 2. 技术栈与工具链

| 层级 | 技术 | 版本/备注 |
|------|------|-----------|
| UI 框架 | WPF (.NET 8) | `net8.0-windows` |
| 组件库 | MaterialDesignInXAML (MDIX) | 5.3.2 |
| MVVM | Prism.DryIoc | 区域导航、DI、模块化 |
| 数据绑定 | CommunityToolkit.Mvvm | `[ObservableProperty]` / `[RelayCommand]` |
| 打印 | QuestPDF | A5 处方笺 |
| 主题 | BundledTheme (Light) | Primary=Brown, Secondary=Amber |

---

## 3. 主题与色彩系统

### 3.1 表面层次（Surface Hierarchy）

4 级 Surface 系统，解决 MDIX Light 主题全白无层次问题：

| 层级 | Token | 亮色值 | 暗色值 | 用途 |
|------|-------|--------|--------|------|
| L0 | `SurfaceLevel0Brush` | `#FAF8F5`（暖灰白） | `#1E1E1E` | 页面/容器底色 |
| L1 | `SurfaceLevel1Brush` | `#FFFFFF` | `#2D2D30` | 面板/工具栏 |
| L2 | `SurfaceLevel2Brush` | `#FFFFFF` | `#2D2D30` | 浮起卡片 + Elevation1 |
| L3 | `SurfaceLevel3Brush` | `#FFFFFF` | `#3F3F46` | 模态弹窗 + Elevation3 |

> **设计意图**：暖灰底色（`#FAF8F5`）让白色面板/卡片自然浮起，同时保持中医温暖质感。

### 3.2 投影系统（Elevation）

| Token | 用途 | 参数 |
|-------|------|------|
| `Elevation1` | 卡片（InfoCard 等浮起元素） | Blur=8, Depth=1, Opacity=0.06 |
| `Elevation2` | 面板（Master/Detail 面板边缘） | Blur=12, Depth=2, Opacity=0.08 |
| `Elevation3` | 弹窗（Dialog） | Blur=24, Depth=8, Opacity=0.12 |

### 3.3 TCM 品牌色彩

| Token | 亮色值 | 暗色值 | 语义 |
|-------|--------|--------|------|
| `TcmGreenBrush` | `#2E8B57` | `#4CAF50` | 药材/确认/成功 |
| `TcmGoldBrush` | `#DAA520` | `#FFD54F` | 处方/警告/重要 |
| `TcmOrangeBrush` | `#E65100` | `#FF9800` | 编辑/进行中 |

### 3.4 状态徽章色彩

| 状态 | 背景色 | 前景色 | 暗色背景 | 暗色前景 |
|------|--------|--------|----------|----------|
| Success | `#E8F5E9` | `#228B22` | `#1B5E20` | `#81C784` |
| Danger | `#FDE7E9` | `#A4262C` | `#B71C1C` | `#EF9A9A` |
| Warning | `#FFF8E1` | `#B8860B` | `#7A5900` | `#FFD54F` |
| Info | `#E0F7FA` | `#3D5A80` | `#0D47A1` | `#81D4FA` |
| Neutral | `#F5F5F5` | `#757575` | `#3F3F46` | `#BDBDBD` |

### 3.5 间距 Token

| Token | 值 | 用途 |
|-------|-----|------|
| `SpacingXS` | 4px | 紧凑间距 |
| `SpacingS` | 8px | 小间距 |
| `SpacingM` | 12px | 中等间距 |
| `SpacingL` | 16px | 标准间距 |
| `SpacingXL` | 24px | 大间距 |
| `SpacingXXL` | 32px | 超大间距 |
| `SpacingXXXL` | 40px | 页面级间距 |

### 3.6 功能卡片样式

| 样式 Key | 用途 | 特征 |
|----------|------|------|
| `FunctionCardStyle` | 普通功能卡片 | MinWidth=200, MinHeight=180, CornerRadius=12, 悬浮投影增强 |
| `PrimaryFunctionCardStyle` | 主功能卡片 | MinWidth=320, MinHeight=200, CornerRadius=16, 渐变背景 |
| `StatsCardStyle` | 统计卡片 | MinWidth=200, MinHeight=200, CornerRadius=12, 轻投影 |
| `CardIconStyle` | 卡片图标 | Width=48, Height=48, Primary色填充 |
| `CardTitleStyle` | 卡片标题 | FontSize=18, SemiBold, 居中 |

### 3.7 图标系统

- **导航图标**：MDIX `PackIcon`（Kind 枚举），24×24 viewport
- **卡片图标**：自定义 SVG Geometry（`Themes/Icons.xaml`），Path 绑定
- **状态图标**：MDIX PackIcon（ApiHealthy/ApiUnhealthy 等）

---

## 4. 页面布局规范

### 4.1 主窗口（MainWindow）

```
┌──────────────────────────────────────────────────────┐
│                    MaterialDesignWindow               │
│               WindowStyle=None / Maximized            │
│              MinWidth=1024 / MinHeight=768             │
├──────────┬───────────────────────────────────────────┤
│ 侧边栏   │              ContentRegion               │
│ (Primary │                                           │
│  Brush)  │                                           │
│          │                                           │
│ ☰ 汉堡   │                                           │
│ 🌿 品牌  │                                           │
│ ──────── │                                           │
│ 导航列表  │                                           │
│          │                                           │
│ ──────── │                                           │
│ 🔑 账户  │                                           │
│ 🌙 主题  │                                           │
│ 🚪 退出  │                                           │
├──────────┴───────────────────────────────────────────┤
│ 🟢 API状态 │ 远程模式 │ 👤 用户名 │ ⏰ 时间         │  (32px Status Bar)
└──────────────────────────────────────────────────────┘
```

**规格**：
- 窗口：`WindowStyle=None`, `WindowState=Maximized`, `MinWidth=1024`, `MinHeight=768`
- 侧边栏：宽度由 `SidebarWidth` 属性控制，支持汉堡按钮展开/收起
- 侧边栏背景：`MaterialDesign.Brush.Primary`（Brown）
- 内容区：`ContentRegion`，Margin=4
- 状态栏：高度 32px，右侧显示 API 状态 + 连接模式 + 用户名 + 时间

### 4.2 列表页（Master-Detail 布局）

```
┌──────────────────────────────────────────────────────┐
│ 面包屑导航 (BreadcrumbBar)                           │
├──────────────────┬─────┬─────────────────────────────┤
│ Master (列表)    │ ║   │ Detail (详情/编辑)          │
│ ┌──────────────┐ │ ║   │ ┌─────────────────────────┐ │
│ │ 工具栏        │ │ ║   │ │ BaseDetailContainer     │ │
│ │ (新增/刷新)   │ │ ║   │ │  Header: 返回+标题+编辑 │ │
│ ├──────────────┤ │ ║   │ │  Content: View/Edit切换 │ │
│ │ DataGrid      │ │ ║   │ │  Footer: 保存/取消      │ │
│ │ (列表数据)    │ │ ║   │ └─────────────────────────┘ │
│ ├──────────────┤ │ ║   │                             │
│ │ 分页栏        │ │ ║   │                             │
│ └──────────────┘ │ ║   │                             │
└──────────────────┴─────┴─────────────────────────────┘
│ L0暖灰底(#FAF8F5) │ Elevation1 │ L0暖灰底+浮动卡片    │
└──────────────────────────────────────────────────────┘
```

**Master 区域规格**：
- 背景：`SurfaceLevel1Brush`（白色）
- 右边框：`DividerBrush`
- 投影：`Elevation1`
- 最小宽度：280px
- 工具栏：`DataGridToolbar`（新增/刷新/导出 + 批量操作）

**Detail 区域规格**：
- 背景：透明（显示 L0 底色）
- 最小宽度：400px
- 空状态时显示 `EmptyState` 控件

**GridSplitter**：
- 宽度：12px（含拖动指示器）
- 视觉：1px 分割线 + 4px 圆角指示器
- 悬停：Primary 色 + 指示器 0.3 透明度
- 拖动：Primary 色 + 指示器 0.6 透明度

### 4.3 首页（Home View）

```
┌──────────────────────────────────────────────────────┐
│                页面标题 (FontSize=24, Bold)           │
│              副标题 (FontSize=14, BodyLight)           │
│                                                      │
│  ┌────────────────┐  ┌────────────────┐              │
│  │  PrimaryCard   │  │  StatsCard     │              │
│  │  渐变背景      │  │  今日统计      │              │
│  │  大图标+标题   │  │  数字+标签     │              │
│  └────────────────┘  └────────────────┘              │
│                                                      │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │
│  │Card 1│ │Card 2│ │Card 3│ │Card 4│ │Card 5│      │
│  │图标  │ │图标  │ │图标  │ │图标  │ │图标  │      │
│  │标题  │ │标题  │ │标题  │ │标题  │ │标题  │      │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘      │
└──────────────────────────────────────────────────────┘
```

**布局**：
- 全屏居中 `StackPanel`（水平 + 垂直居中）
- 主卡片行：PrimaryFunctionCard（渐变） + StatsCard（统计）
- 辅助卡片行：FunctionCard（UniformGrid 或 Horizontal StackPanel）
- 卡片间距：Margin=12

### 4.4 详情页（BaseDetailContainer）

```
┌──────────────────────────────────────────────────────┐
│ 面包屑: 患者管理 › 患者详情                           │
├──────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────┐ │
│ │ ← 返回    患者详情              [编辑] [自定义操作]│ │  Header Card
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│  ┌──────────────────────────────────────────────────┐│
│  │  查看模式内容 (ViewContent)                       ││
│  │  / 编辑模式内容 (EditContent)                     ││
│  │                                                  ││
│  │  ← 带淡入淡出 + Y轴滑动过渡动画 (0.25s)          ││
│  └──────────────────────────────────────────────────┘│
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │                    [取消]  [保存]                  │ │  Footer (编辑模式)
│ └──────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────┘
```

**Header Card**：
- 背景：`MaterialDesignPaper`
- 圆角：16px
- 内边距：32,24
- 投影：`CardShadow`（Blur=16, Depth=2, Opacity=0.08）

**Content 过渡动画**：
- 时长：0.25s（`CubicEase EaseOut`）
- 查看→编辑：Y 从 +8 到 0 + 淡入
- 编辑→查看：Y 从 -8 到 0 + 淡入

**Footer Card**：
- 仅编辑模式显示（带过渡动画，Y 从 +16 到 0）
- 背景/圆角/投影同 Header

**页面加载动画**：Opacity 从 0 到 1，0.3s CubicEase

### 4.5 临床工作台（ClinicalWorkspaceView）

```
┌──────────────────────────────────────────────────────┐
│ ┌──────────┬─┬──────────────────────────────────────┐│
│ │ 患者选择  │║│  患者信息条 (Primary背景)              ││
│ │ 列表      │║│  姓名 | 性别 | 年龄                   ││
│ │ 320px    │║├──────────────────────────────────────┤│
│ │          │║│  📋 请从左侧选择患者                    ││
│ │ Patient  │║│     双击患者或选中后点击「开始看诊」     ││
│ │ Selection│║├──────────────────────────────────────┤│
│ │ Control  │║│  历史就诊记录 (可折叠 Expander)        ││
│ │          │║├──────────────────────────────────────┤│
│ │          │║│              [开始看诊] [新建患者]     ││
│ └──────────┴─┴──────────────────────────────────────┘│
└──────────────────────────────────────────────────────┘
```

**三栏布局**：
- 左栏：320px（Min=240, Max=600），`PatientSelectionControl`
- 分隔条：GridSplitter，5px
- 右栏：`*`，四行 Grid（信息条 + 空状态 + 历史 + 操作）

---

## 5. 导航规范

### 5.1 导航架构

```
MainWindow
├── LoginRegion → LoginView
└── ContentRegion → (Role Module Views)
    ├── Admin Home → AdminHomeView
    ├── Clinical Home → ClinicalHomeView
    └── Clinical Workspace → ClinicalWorkspaceView
```

### 5.2 侧边栏导航

- **导航方式**：`ListBox` + `ListBoxItem` + MDIX 导航样式
- **选中态**：`MaterialDesignNavigationPrimaryListBoxItem` 默认样式
- **展开态**：图标 + 文字（水平排列，居中）
- **收起态**：仅图标（汉堡按钮触发 `ToggleSidebarCommand`）
- **文字可见性**：`NavTextVisibility` 属性，由 `IsSidebarExpanded` 控制
- **导航项**：`NavigationItems` 集合，每项含 `Title` + `IconKind`
- **选中绑定**：`SelectedItem` → `SelectedNavItem`（双向绑定）

### 5.3 区域导航

- **框架**：Prism Region-based Navigation
- **入口**：`INavigationCoordinator.NavigateTo(viewName, parameters)`
- **目标区域**：`ContentRegion`（MainWindow 内的 ContentControl）
- **防抖**：300ms 内不重复导航到同一视图
- **超时**：导航超时 10s 自动取消
- **历史**：通过 Prism Journal 支持后退/前进

### 5.4 面包屑导航

- **控件**：`BreadcrumbBar`
- **位置**：详情页 Header 上方
- **格式**：`Level1 › Level2 › Level3`
- **交互**：非当前层级可点击跳转，当前层级禁用
- **样式**：FontSize=13, BodyLight 色，悬停变 Primary 色

### 5.5 导航行为

| 行为 | 实现 |
|------|------|
| 后退 | `Alt+Left` / 详情页 `← 返回` 按钮 |
| 前进 | `Alt+Right` |
| 回首页 | `Alt+Home` |
| 首页切换 | `Ctrl+M`（汉堡按钮） |
| 未保存退出 | `IConfirmNavigationRequest` → `UnsavedChangesDialog` |

---

## 6. 交互规范

### 6.1 按钮状态

| 按钮类型 | 样式 Key | 背景 | 前景 | 用途 |
|----------|----------|------|------|------|
| 主要操作 | `MaterialDesignRaisedButton` | Primary | White | 新增/保存/确认 |
| 次要操作 | `MaterialDesignOutlinedButton` | Transparent | Foreground | 刷新/编辑/取消 |
| 危险操作 | 自定义 `DangerButtonStyle` | Transparent | Error | 删除 |
| 批量删除 | `MaterialDesignRaisedButton` | `ValidationErrorBrush` | White | 批量删除 |
| 功能卡片 | `MaterialDesignFlatButton` | Transparent | IdealForeground | 卡片点击 |

**按钮尺寸规范**：

| 场景 | 高度 | 内边距 | 字号 |
|------|------|--------|------|
| 登录按钮 | 52px | - | 16px |
| 工具栏按钮 | 36px | - | 14px |
| Footer 保存/取消 | Auto | 32,12 | 15px |
| 卡片内按钮 | Auto | Auto | - |
| 对话框按钮 | 32px | 16,8 | 14px |

### 6.2 表单验证

- **验证样式**：MDIX 内置 `MaterialDesignOutlinedTextBox`
- **错误提示**：`ValidationErrorBrush`（`#B00020`）+ `ValidationErrorBackgroundBrush`（`#FCE8E6`）
- **验证触发**：`UpdateSourceTrigger=PropertyChanged`（实时验证）
- **密码框**：`PasswordBoxHelper.BoundPassword` 行为附加属性

### 6.3 加载状态

**LoadingOverlay 控件**：
- 半透明遮罩：`#20000000`
- 进度条：`ProgressBar` Indeterminate，Width=200, Height=4
- 文本：`LoadingText` 属性，默认 14px, BodyLight 色
- 绑定：`IsLoading` → `IsOverlayVisible`

**BaseDetailContainer 加载**：
- 全屏遮罩：`DarkOpacityBrush`
- 加载文本：`LoadingMessage`，48px 省略号 + 16px 消息

### 6.4 空状态

**EmptyState 控件**：
- 图标：64×64，Path Geometry，BodyLight 色，Opacity=0.7
- 标题：16px, Medium, BodyLight
- 副标题：14px, BodyLight（可选）
- 操作按钮：`MaterialDesignRaisedButton`（可选）

**使用场景**：
- 列表无数据：`Title="暂无数据"`
- 详情未选择：`Title="请选择一个项目"`
- 搜索无结果：`Title="未找到匹配结果"`

### 6.5 状态徽章

**StatusBadge 控件**：
- 预设类型：Success / Danger / Warning / Info / Neutral
- 最小宽度：48px
- 内边距：8,4
- 圆角：4px
- 字号：12px, Medium
- 颜色：来自 Surfaces.xaml 主题资源

### 6.6 分页

**UnifiedPaginationBar 控件**：
- 每页条数选择：ComboBox，Width=80
- 页码显示：`当前页 / 总页数`
- 按钮：首页/上一页/下一页/末页（MDIX PackIcon）
- 总记录数：`共 N 条记录`

---

## 7. 业务流程 UX

### 7.1 登录流程

```
启动 → SplashScreen → LoginView
                         │
                    ┌────┴────┐
                    │ 背景图   │ 左侧品牌区（大医精诚 + 标语）
                    │ + 毛玻璃 │ 右侧登录卡片（440px宽）
                    └────┬────┘
                         │
                    用户名/密码 → 记住选项 → 登录按钮
                         │
                    ┌────┴────┐
                    │ 模式徽章 │ 远程(Green) / 本地(Gold)
                    │ 切换按钮 │ 切换到本地/远程
                    │ 配置按钮 │ 服务器配置
                    └─────────┘
                         │
                    加载遮罩（ProgressBar + "正在登录..."）
                         │
                    成功 → 加载角色模块 → 导航到角色首页
                    失败 → ErrorMessage（红色文本）
```

**规格**：
- 登录卡片：Width=440, Padding=40,36, CornerRadius=12, SurfaceLevel1Brush
- 输入框：Height=50, FontSize=15, Outlined 样式
- 记住选项：CheckBox, FontSize=13
- 关闭按钮：右上角 "X"，Opacity=0.4

### 7.2 挂号流程

```
Admin/Clinical首页 → 挂号队列卡片
                          │
                    RegistrationListView
                    ┌─────┴──────┐
                    │ 标题栏      │
                    │ 工具栏      │ 新增/刷新 + 快速就诊/接诊/取消
                    │ DataGrid    │ 排队号/患者/医生/费用/来源/状态/时间
                    │ 状态栏      │ 共N条 + 错误信息
                    └─────┬──────┘
                          │
                    新增 → RegistrationCreateDialog
                          │ 患者搜索(autocomplete) + 医生选择 + 挂号费
                          │
                    接诊 → StartVisit → 创建MedicalCase → 导航到MedicalCaseWorkspace
```

### 7.3 接诊流程（Clinical Workspace）

```
ClinicalHomeView → 开始接诊(PrimaryFunctionCard)
                         │
                   PatientSelectionView (三栏)
                   ┌──────┴──────────────────────┐
                   │ 左: 读卡器+待诊队列           │
                   │ 中: PatientSelectionControl  │
                   │ 右: 患者信息卡               │
                   └──────┬──────────────────────┘
                          │ 选择患者 + 双击
                          │
                   ClinicalWorkspaceView (三栏)
                   ┌──────┴──────────────────────┐
                   │ 左: 患者选择列表 (320px)      │
                   │ 右: 患者信息条 + 工作区       │
                   │     └ 历史就诊(Expander)      │
                   │     └ 操作按钮[开始看诊]      │
                   └──────┬──────────────────────┘
                          │ 开始看诊
                          │
                   MedicalCaseWorkspace
                   ┌──────┴──────────────┐
                   │ WorkflowStepIndicator│ 步骤进度条
                   │ MedicalCaseEditControl│ 诊断+处方编辑
                   │ 保存/打印            │
                   └─────────────────────┘
```

### 7.4 处方流程

```
MedicalCaseEditControl
     │
     ├── 诊断信息 (Consultation)
     │   └ 中医四诊 + 辨证论治
     │
     ├── 处方信息 (Prescription)
     │   ├── 药材选择 (HerbListControl + HerbItemControl)
     │   │   └ 君臣佐使角色 + 剂量 + 用法
     │   ├── 验方导入 (FormulaImportDialog)
     │   │   └ 从验方库选择模板
     │   └ 历史复制 (HistoryCopyDialog)
     │       └ 从历史处方复制
     │
     └── 打印 (QuestPDF)
         └ A5 处方笺模板
```

### 7.5 报表流程

```
AdminHomeView/ClinicalHomeView → 统计报表卡片
                                       │
                               ReportsHomeView
                               ┌────────┴────────┐
                               │ 今日/本周/本月统计 │
                               │ 接诊量/处方量      │
                               │ 药材使用频率        │
                               └─────────────────┘
```

---

## 8. 共享控件规范

### 8.1 控件清单

| 控件 | 位置 | 用途 |
|------|------|------|
| `MasterDetailLayout` | Controls/ | 左右分割主从布局容器 |
| `BaseDetailContainer` | Controls/ | 详情页容器（Header+Content+Footer） |
| `DataGridToolbar` | Controls/ | 列表工具栏（新增/刷新/导出/批量） |
| `DetailToolbar` | Controls/ | 详情工具栏（编辑/保存/取消/删除） |
| `SearchBox` | Controls/ | 搜索输入框 |
| `StatusBadge` | Controls/ | 状态徽章 |
| `InfoCard` | Controls/ | 信息卡片（查看模式） |
| `EmptyState` | Controls/ | 空状态提示 |
| `LoadingOverlay` | Controls/ | 加载遮罩 |
| `BreadcrumbBar` | Controls/ | 面包屑导航 |
| `UnifiedPaginationBar` | Controls/ | 统一分页栏 |
| `PatientInfoCardControl` | Controls/ | 患者信息卡 |
| `ToastControl` | Controls/Toast/ | Toast 消息提示 |
| `WorkflowStepIndicator` | MedicalCase/Controls/ | 工作流步骤指示器 |
| `HerbListControl` | Controls/HerbList/ | 药材列表 |
| `HerbItemControl` | Controls/HerbItem/ | 单个药材项 |
| `FormulaViewControl` | Controls/FormulaView/ | 验方查看 |
| `PatientSelectionControl` | Patients/Controls/ | 患者选择 |

### 8.2 控件使用规范

**MasterDetailLayout**：
```xml
<controls:MasterDetailLayout 
    HasSelection="{Binding HasSelection}"
    MasterWidth="300" DetailWidth="*">
    <controls:MasterDetailLayout.MasterContent>
        <!-- 列表内容 -->
    </controls:MasterDetailLayout.MasterContent>
    <controls:MasterDetailLayout.DetailContent>
        <!-- 详情内容 -->
    </controls:MasterDetailLayout.DetailContent>
    <controls:MasterDetailLayout.EmptyContent>
        <controls:EmptyState Title="请选择项目"/>
    </controls:MasterDetailLayout.EmptyContent>
</controls:MasterDetailLayout>
```

**BaseDetailContainer**：
```xml
<views:BaseDetailContainer
    Title="患者详情"
    IsEditMode="{Binding IsEditMode}"
    GoBackCommand="{Binding GoBackCommand}"
    SwitchToEditCommand="{Binding SwitchToEditModeCommand}"
    SaveCommand="{Binding SubmitCommand}"
    CancelCommand="{Binding CancelCommand}"
    ShowEditButton="True"
    NavigationPath="{Binding NavigationPath}">
    <views:BaseDetailContainer.ViewContent>
        <!-- 查看模式 -->
    </views:BaseDetailContainer.ViewContent>
    <views:BaseDetailContainer.EditContent>
        <!-- 编辑模式 -->
    </views:BaseDetailContainer.EditContent>
</views:BaseDetailContainer>
```

**DataGridToolbar**：
```xml
<controls:DataGridToolbar
    CreateCommand="{Binding CreateCommand}"
    RefreshCommand="{Binding RefreshCommand}"
    ExportCommand="{Binding ExportCommand}">
    <controls:DataGridToolbar.AdditionalContent>
        <!-- 自定义按钮 -->
    </controls:DataGridToolbar.AdditionalContent>
</controls:DataGridToolbar>
```

---

## 9. 对话框规范

### 9.1 对话框类型

| 类型 | 控件 | 用途 |
|------|------|------|
| 确认对话框 | `ConfirmationDialog` | 删除/危险操作确认 |
| 消息对话框 | `MessageDialog` | Success/Error/Warning/Info 消息 |
| 输入对话框 | `InputDialog` | 获取用户输入 |
| 未保存变更 | `UnsavedChangesDialog` | 编辑后未保存退出确认 |
| 挂号创建 | `RegistrationCreateDialog` | 新建挂号 |
| 验方导入 | `FormulaImportDialog` | 从验方库导入处方 |
| 历史复制 | `HistoryCopyDialog` | 复制历史处方 |

### 9.2 对话框规格

- **框架**：Prism DialogService（UserControl + DialogWindow 容器）
- **最小尺寸**：MinWidth=350-400, MinHeight=150-200
- **窗口样式**：`CustomDialogWindowStyle`（WindowStyle=None, NoResize, SizeToContent=WidthAndHeight）
- **内边距**：Margin=20-24
- **按钮**：Width=80-100, Height=32, FontSize=14

### 9.3 MessageDialog 类型样式

| 类型 | 图标背景 | 图标符号 |
|------|----------|----------|
| Success | `#2E8B57` | ✔ |
| Error | `MaterialDesign.Brush.Error` | ✖ |
| Warning | `#DAA520` | ! |
| Info | `#5B8FA8` | i |

### 9.4 ConfirmationDialog

- 图标：48×48 Image
- 消息：14px, TextWrapping
- 删除选项：RadioButton（软删除/物理删除）
- 按钮：确认 + 取消

---

## 10. 状态反馈系统

### 10.1 反馈层次

| 层次 | 机制 | 用途 | 持续时间 |
|------|------|------|----------|
| L1 | Snackbar | 操作成功/失败提示 | 3-5s 自动消失 |
| L2 | Toast | 非阻塞消息提示 | 带动画淡入淡出 |
| L3 | LoadingOverlay | 加载中状态 | 操作完成时消失 |
| L4 | MessageDialog | 需要用户注意的消息 | 手动关闭 |
| L5 | StatusBadge | 列表项状态标识 | 持续显示 |
| L6 | 状态栏 | API 状态/连接模式 | 持续显示 |

### 10.2 Toast 控件

- 位置：顶部居中
- 动画：淡入（0.3s）+ Y轴滑入（-30→0）
- 消失：淡出（0.2s）+ Y轴滑出（0→-30）
- 圆角：8px
- 内边距：16,12
- 最大宽度：400px
- 阴影：Blur=12, Opacity=0.25

### 10.3 WorkflowStepIndicator

- 步骤圆：28×28, CornerRadius=14
- 步骤数字：14px, SemiBold, White
- 步骤标签：13px, Medium
- 进度点：4×4, CornerRadius=2
- 激活动画：Opacity 0.6→1 + Scale 0.95→1（0.3s CubicEase）

---

## 11. 键盘快捷键

### 11.1 全局快捷键（MainWindow）

| 快捷键 | 命令 | 用途 |
|--------|------|------|
| `Ctrl+N` | QuickAddPatientCommand | 快速新增患者 |
| `Ctrl+Shift+C` | ShowHelpCommand | 帮助 |
| `Ctrl+OemComma` | ShowSettingsCommand | 设置 |
| `Ctrl+M` | ToggleSidebarCommand | 侧边栏展开/收起 |
| `Alt+Left` | NavigateBackCommand | 后退 |
| `Alt+Right` | NavigateForwardCommand | 前进 |
| `Alt+Home` | NavigateToHomeCommand | 回首页 |
| `F1` | ShowHelpCommand | 帮助 |

### 11.2 详情页快捷键（BaseDetailContainer）

| 快捷键 | 命令 | 用途 |
|--------|------|------|
| `Ctrl+S` | SaveCommand | 保存 |
| `Ctrl+P` | PrintCommand | 打印 |
| `Escape` | CancelCommand | 取消/返回 |
| `F1` | HelpCommand | 帮助 |

### 11.3 登录页快捷键

| 快捷键 | 命令 | 用途 |
|--------|------|------|
| `Enter` | LoginCommand | 登录（密码框触发） |

---

## 12. 无障碍与国际化

### 12.1 无障碍

- **AutomationProperties.Name**：输入框已设置（如用户名、密码）
- **Tab 导航**：MDIX 控件默认支持
- **高对比度**：DynamicResource 支持主题切换

### 12.2 国际化

- **字体**：`Microsoft YaHei UI, Microsoft YaHei, Segoe UI`
- **登录品牌字体**：`LiSu`（隶书）
- **文案**：全部中文，无英文业务文案
- **图标**：MDIX PackIcon（通用图标）+ 自定义 SVG Geometry（业务图标）

---

## 13. 当前 UX 问题清单

### A 级（结构性问题）

| ID | 问题 | 位置 | 影响 | 建议 |
|----|------|------|------|------|
| A-1 | 部分视图未使用 MasterDetailLayout | MedicalCaseManagementView 等薄包装 | 布局不一致 | 统一使用 MasterDetailLayout |
| A-2 | 首页卡片布局硬编码 | AdminHomeView/ClinicalHomeView | 响应式差 | 改用 WrapPanel 或自适应 Grid |
| A-3 | MessageDialog 图标样式内联硬编码颜色 | MessageDialog.xaml | 主题切换不一致 | 改用 TCM 品牌色资源 |
| A-4 | PendingQueueView 状态徽章硬编码 `#5B8FA8` | PendingQueueView.xaml | 暗色主题不一致 | 改用 StatusBadge 控件 |

### B 级（UX 优化）

| ID | 问题 | 位置 | 建议 |
|----|------|------|------|
| B-1 | 登录页关闭按钮为纯 "X" 文字 | LoginView.xaml | 改用 MDIX PackIcon |
| B-2 | 临床工作台空状态使用 Emoji 📋 | ClinicalWorkspaceView.xaml | 改用 Path 图标 |
| B-3 | 读卡器连接状态使用 Ellipse + 硬编码绿色 | PatientSelectionView.xaml | 改用 StatusBadge 控件 |
| B-4 | DataGridToolbar 批量删除按钮颜色硬编码 | DataGridToolbar.xaml | 使用 ValidationErrorBrush 资源 |

### C 级（细节规范化）

| ID | 问题 | 位置 | 建议 |
|----|------|------|------|
| C-1 | 部分 TextBlock 使用 Opacity=0.7 代替 BodyLight | 多处 | 统一使用 BodyLight 前景色 |
| C-2 | LoadingOverlay 遮罩颜色硬编码 `#20000000` | LoadingOverlay.xaml | 使用 DarkOpacityBrush |
| C-3 | 对话框按钮宽度不统一（80px vs 100px） | 多个 Dialog | 统一按钮尺寸 |
| C-4 | BaseDetailContainer 加载文本使用 "..." 而非动画 | BaseDetailContainer.xaml | 改用 ProgressBar 或省略号动画 |

---

## 附录 A：文件路径索引

| 类别 | 路径 |
|------|------|
| 主窗口 | `Shell/Views/MainWindow.xaml` |
| 应用入口 | `Shell/App.xaml` |
| 主题-品牌色 | `Core/LYBT.Desktop.Controls/Themes/TcmBrands.xaml` |
| 主题-表面层次 | `Core/LYBT.Desktop.Controls/Themes/Surfaces.xaml` |
| 主题-间距 | `Core/LYBT.Desktop.Controls/Themes/Spacing.xaml` |
| 主题-图标 | `Core/LYBT.Desktop.Controls/Themes/Icons.xaml` |
| 主题-功能卡片 | `Core/LYBT.Desktop.Controls/Themes/Functions.xaml` |
| 主题-DataGrid | `Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml` |
| 控件-MasterDetail | `Core/LYBT.Desktop.Controls/Controls/MasterDetailLayout.xaml` |
| 控件-详情容器 | `Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml` |
| 控件-工具栏 | `Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml` |
| 控件-面包屑 | `Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml` |
| 控件-分页 | `Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml` |
| 控件-空状态 | `Core/LYBT.Desktop.Controls/Controls/EmptyState.xaml` |
| 控件-加载遮罩 | `Core/LYBT.Desktop.Controls/Controls/LoadingOverlay.xaml` |
| 控件-状态徽章 | `Core/LYBT.Desktop.Controls/Controls/StatusBadge.xaml` |
| 控件-信息卡片 | `Core/LYBT.Desktop.Controls/Controls/InfoCard.xaml` |
| 控件-Toast | `Core/LYBT.Desktop.Controls/Controls/Toast/ToastControl.xaml` |
| 对话框-确认 | `Shell/Dialogs/Views/ConfirmationDialog.xaml` |
| 对话框-消息 | `Shell/Dialogs/Views/MessageDialog.xaml` |
| 对话框-输入 | `Shell/Dialogs/Views/InputDialog.xaml` |
| 对话框-未保存 | `Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` |
| 登录 | `Modules/LYBT.Desktop.Auth/Views/LoginView.xaml` |
| Admin首页 | `Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml` |
| Clinical首页 | `Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` |
| 临床工作台 | `Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` |
| 患者选择 | `Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` |
| 挂号列表 | `Modules/LYBT.Desktop.Registrations/Views/RegistrationListView.xaml` |
| 步骤指示器 | `Modules/LYBT.Desktop.MedicalCase/Controls/WorkflowStepIndicator.xaml` |
| 导航协调器 | `Core/LYBT.Desktop.Infrastructure/Navigation/NavigationCoordinator.cs` |
| ViewModel基类 | `Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs` |

---

## 附录 B：资源加载顺序

```
App.xaml
├── materialDesign:BundledTheme (Light, Brown, Amber)
├── MaterialDesign2.Defaults.xaml (MDIX 默认样式)
├── Surfaces.xaml (表面层次 + 状态徽章 + 投影)
├── TcmBrands.xaml (TCM 品牌色)
├── Icons.xaml (矢量图标)
├── Spacing.xaml (间距 Token)
├── Functions.xaml (功能卡片样式)
├── DataGridStyles.xaml (DataGrid 单元格样式)
├── Converters.xaml (值转换器)
└── App 级自定义样式 (ValidationErrorBrush, CustomDialogWindowStyle)
```

**规则**：
- 共享控件**不合并** App 级资源字典
- DynamicResource 运行时沿视觉树向上查找
- StaticResource 编译时从 Application.Resources 解析
