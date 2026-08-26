# 桌面应用固定布局框架调研 — 3 方案

> 日期: 2026-08-22 | 方式: web_search 4 轮（Prism 导航 / 桌面布局规范 / 医疗软件 UI / MDIX）+ 代码分析（`MainWindow.xaml` 全文 178 行 / `MainWindowViewModel` / `desktop-design-spec.md` §4-5/§11）
> 本文档只呈现 3 个方案与事实，不做推荐。选择权在用户。

## 0. 现状基线（代码实际状态）

`src/Client/Desktop/Shell/Views/MainWindow.xaml`（已通读全文）：

```
┌──────────────────────────────────────────────────┐
│ Grid (2 列 × 2 行)                                │
│ ┌──────────┬───────────────────────────────────┐ │
│ │ 侧边栏    │ 内容区 ContentRegion              │ │
│ │ (Primary │ (Border Margin=4)                 │ │
│ │  Brush)  │                                   │ │
│ │ ☰ 汉堡   │                                   │ │
│ │ 🌿 品牌   │                                   │ │
│ │ ──────── │                                   │ │
│ │ ListBox  │                                   │ │
│ │ 导航 8项  │                                   │ │
│ │ ──────── │                                   │ │
│ │ 🔑/🌙/🚪 │                                   │ │
│ └──────────┼───────────────────────────────────┤ │
│            │ 状态栏 32px (API|模式|用户|时间)    │ │
│            └───────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

**固定元素（现状）**：
| 元素 | 位置 | 规格 |
|------|------|------|
| 侧边栏 | Grid 列 0，RowSpan 2 | 宽 `{SidebarWidth}` 可折叠（汉堡 `Ctrl+M`），Primary 背景，DockPanel 内：顶汉堡+品牌+分隔、中 ListBox 导航、底 账户/主题/退出 |
| 内容区 | 列 1 行 0 | `ContentRegion`（Prism Region），Margin 4，CornerRadius 4 |
| 状态栏 | 列 1 行 1 | 高 32px，DockPanel 右：`ApiStatusIcon/ConnectionModeDisplay/CurrentUserDisplayName/CurrentTimeDisplay` |
| 登录层 | 顶层 Grid | `LoginRegion`，`IsNotLoggedIn` 可见 |
| DialogHost | 包 Shell | `Identifier=RootDialog`，Snackbar 右下 |

**观察**：
- 顶层无 `BreadcrumbBar`（§4.2 文档要求但 MainWindow 未实现——设计稿 gap 分析第 2 页已记录）
- 侧边栏无分组折叠（§4.4/5.2 文档要求“临床/目录/管理”，现状平铺 8 项）
- 无 `GridSplitter` 拖拽调宽（仅固定 `SidebarWidth` + 汉堡收起）
- 状态栏信息与文档 §4.1 一致（右对齐单行）
- 窗口 `WindowStyle=None/WindowState=Maximized/MinWidth=1024/MinHeight=768`

---

## 方案 1：整体直排（Whole-Page Scroll）— 保持现有 + 补缺

### 布局结构图
```
┌──────────────────────────────────────────────────┐
│ 顶部: 汉堡 | 面包屑 BreadcrumbBar (新增) | 搜索 | 🔔 | 头像 │  48px 头栏 (新增)
├──────────┬───────────────────────────────────────┤
│ 侧边栏   │  ContentRegion (单区域，无子轨)        │
│ 分组折叠 │  页面自行滚动（ScrollViewer 每页）      │
│ 临床/目录│                                       │
│ 管理     │                                       │
├──────────┴───────────────────────────────────────┤
│ 状态栏 32px (API|模式|用户|时间)                  │
└──────────────────────────────────────────────────┘
```

### 固定元素规格
| 元素 | 规格 |
|------|------|
| 头栏 | 48px，新增：面包屑（Level1›Level2›Level3 可点击）+ 全局搜索 + 通知 + 头像 |
| 侧边栏 | 展开 240px / 收起 64px 图标轨；分组折叠（临床/目录/管理） |
| 内容区 | 单 Region，页面内 `ScrollViewer`，无固定底部（除状态栏） |
| 状态栏 | 32px 右对齐（现状保留） |

### 可变区域
- 内容区整页：每页自管滚动，无全局 `ScrollViewer` 嵌套（防双重滚动条）。

### 优缺点
| 优点 | 缺点 |
|------|------|
| 改动最小（补头栏+分组即可，其余现状） | 列表页与详情页各自滚动，无全局滚动一致 |
| 页内滚动自由，长表单（医案工作台）可用 | 面包屑需每页提供 `NavigationPath`（31 页改动面） |
| 与现状代码兼容（Region 单轨） | 长页面顶栏不可见（滚动后丢失上下文） |

### 适用场景
- 想保持现状结构、仅补齐规范缺口（§4.2/4.4 面包屑+分组）；页面内容以表单/详情为主（本地单机诊所，页面短）。

---

## 方案 2：固定头栏 + 可折叠侧栏 + 状态栏（三栏端规则）— 主流桌面模式

### 布局结构图
```
┌──────────────────────────────────────────────────┐
│ 头栏 48px: 汉堡 | 面包屑 | 全局搜索 | 🔔 | 头像   │  ← 固定
├──────────┬───────────────────────────────────────┤
│ 侧边栏   │  ContentRegion (可内含 SplitView)     │  ← 固定 | 内容可变
│ 分组折叠 │  ┌──────────┬───────────────┐        │
│ (临床/   │  │ Master   │ Detail(可滚)  │        │  ← 列表页内主从
│  目录/   │  └──────────┴───────────────┘        │
│  管理)   │  ← GridSplitter 拖拽 (12px)          │
├──────────┴───────────────────────────────────────┤
│ 状态栏 32px: API | 模式 | 用户 | 时间             │  ← 固定
└──────────────────────────────────────────────────┘
```

### 固定元素规格
| 元素 | 规格 |
|------|------|
| 头栏 | 48px 固定，`Grid.Row=0`：汉堡（折叠侧栏 `Ctrl+M`）+ 面包屑（§5.4 格式 Level1›2›3）+ 搜索 + 通知 + 头像下拉 账户/主题/退出 |
| 侧边栏 | 固定 240px / 收起 64px，分组折叠（临床/目录/管理），底账户/退出（当前已有可上移或保留） |
| 主从分割 | `MasterDetailLayout` + `GridSplitter` 12px（悬停 Primary 0.3/拖动 0.6，§4.2） |
| 状态栏 | 32px 固定（现状保留） |

### 可变区域
- 内容区 `ContentRegion`：列表页=Master-Detail（可拖拽），首页=卡片网格（`WrapPanel` 自适应 §A-2），医案工作台=三段（患者卡/辨证/处方）。
- 滚动仅发生在 Detail 区（Master 固定，Detail `ScrollViewer`）。

### 优缺点
| 优点 | 缺点 |
|------|------|
| 主流（Marigold/Windows App SDK/KDE HIG 一致：持久左栏+顶栏+内容区） | 改动面较大（头栏新增一次，面包屑 31 页补 `NavigationPath`） |
| 顶栏常驻（滚动不失上下文），医疗软件“命令中心”规范（Health Gorilla 模式） | 小屏 1024 下头栏 + 侧栏占 320px，内容区窄（MinWidth 已 1024） |
| Master 固定 + Detail 滚动，符合列表-编辑高频（挂-诊链） | 医案工作台需在 Detail 内再分栏（嵌套层次） |
| 与 MDIX `DrawerHost`/`NavigationPrimaryListBox` 直接对应（MD3 demo 同构） | — |

### 适用场景
- 想要标准桌面布局（顶栏+侧栏+状态栏），列表-详情高频、管理多页面的系统；与设计稿 `main-window.png`（顶部搜索+头像+侧栏 8 项）基本一致，仅需补面包屑。

---

## 方案 3：可折叠全屏（Off-Canvas 抽屉 + 图标轨）— MDIX DrawerHost 原生

### 布局结构图
```
Window (MaterialDesignWindow, None)
┌──────────────────────────────────────────────────┐
│ 头栏 56px: ☰(DrawerHost IsLeftDrawerOpen) | 面包屑 | 搜索 | 🔔 | 头像 │
├──────────────────────────────────────────────────┤
│ 内容区 ContentRegion（全宽，无固定侧栏占位）      │
│  ┌───────────────┐                              │
│  │ Master-Detail │                              │
│  └───────────────┘                              │
├──────────────────────────────────────────────────┤
│ 状态栏 24px: API|模式|用户|时间（精简）           │
└──────────────────────────────────────────────────┘
    ↑
DrawerHost.IsLeftDrawerOpen=true: 
┌──────────────┐
│ 抽屉 280px   │
│ 分组折叠     │
│ 导航 8 项    │
│ 账户/主题    │
└──────────────┘
```

### 固定元素规格
| 元素 | 规格 |
|------|------|
| DrawerHost | MDIX 原生（PR #2628 `MaterialDesign3NavigationDrawerPrimaryListBoxItem`），`IsLeftDrawerOpen` 绑定汉堡，开=抽屉 280px 覆盖/关=内容全宽 |
| 头栏 | 56px 固定（抽屉打开时汉堡变关闭） |
| 内容区 | 全宽无侧栏占位（充分利用 1024 - 状态栏） |
| 状态栏 | 24px 精简（仅 API+模式，用户上移头栏头像） |

### 可变区域
- 内容区全宽：列表页 Master-Detail（Detail 更宽），首页卡片自适应。
- 侧栏从不占固定空间，仅在打开时覆盖（`ZIndex` 浮层）。

### 优缺点
| 优点 | 缺点 |
|------|------|
| 内容区最大化（1024 屏 Master 380 + Detail 更充裕） | 导航不可见时需要多一次点击（隐藏导航降低可发现性，违医疗软件“持久导航”规范） |
| MDIX `DrawerHost` 原生，零自定义 ControlTemplate（AGENTS 红线友好） | 抽屉覆盖内容（非挤压），切换页面时需记住关闭 |
| 贴 MD3 设计语言（NavigationDrawer 官方 demo 先例） | 状态栏信息减少（当前用户/时间移头栏）改动面中 |
| 适合窄窗口/单屏小诊所 | 高频导航者（医生多页跳转）额外开销 |

### 适用场景
- 想要内容区最大化、导航低频（医生进入工作台后长时间停留）、或窗口可能缩小的场景；MDIX 原生能力优先。

---

## 对比总表

| 维度 | 方案 1 整体直排 | 方案 2 三栏端规则 | 方案 3 抽屉全屏 |
|------|----------------|------------------|----------------|
| 头栏 | 48px 新增 | 48px 固定 | 56px 固定 |
| 侧边栏 | 固定 240/64 分组 | 固定 240/64 分组 | DrawerHost 280 覆盖 |
| 内容滚动 | 页内自滚 | Detail 区滚（Master 固定） | 页内/Detail 自滚 |
| 状态栏 | 32px 保现状 | 32px 保现状 | 24px 精简 |
| 面包屑 | 补（31 页 `NavigationPath`） | 补（同左） | 补（同左） |
| 主从拖拽 | 无 | GridSplitter 12px | 无（饮 Detail 宽） |
| 改动量 | 小（头栏+分组） | 中（头栏+面包屑+分割） | 中（抽屉重构） |
| MDIX 依赖 | 现有 ListBox 导航 | NavigationPrimaryListBox | DrawerHost + MD3 Drawer |
| 医疗规范符合度 | 中 | 高（持久导航命令中心） | 低（隐藏导航） |
| 极窄屏适配 | 一般 | 一般 | 最灵活 |
| 与设计稿一致 | 部分（缺面包屑分组） | 高（main-window.png） | 低（无固定侧栏） |

---

## 事实与来源（供决策）
- 医疗诊所软件侧边导航为**主导模式**，持久可见导航保持跨模块定向；可折叠最大化工作区（Health Gorilla `layout-and-navigation`）。
- 桌面布局规范（Marigold / Windows App SDK / KDE HIG）一致：**持久左栏 + 全宽顶栏 + 可滚内容区**；状态信息置于内容区底部或顶部 InfoBar，避免冗余条（现状 32px 状态栏已符合）。
- Prism：`RegionManager.RegionName` 挂 `ContentControl`/`ContentRegion`；`INavigationAware` + `IRegionMemberLifetime` 管理视图生命周期（现状已用 `INavigationCoordinator` + KeepAlive）。
- MDIX：`DrawerHost` 抽屉 + `MaterialDesign3NavigationDrawerPrimaryListBoxItem`（PR #2628）+ `MainDemo.Wpf` 参考；`MaterialDesignNavigationPrimaryListBox` 已用于现状侧边栏。
- 项目约束：MDIX 内置样式优先（不自定义 ControlTemplate）、DynamicResource 全局、`MinWidth=1024`、`WindowStyle=None`。

**决策所需信息（由用户提供）**：窗口是否可能 <1024 宽？医生高频导航还是长期停留？倾向 MDIX 原生还是自绘布局？