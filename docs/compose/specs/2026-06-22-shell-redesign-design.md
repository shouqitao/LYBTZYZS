# Shell 重构设计方案

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/shell-materialdesign-refactor.md)

**Date**: 2026-06-22
**Status**: 已批准并实施
**Scope**: 登录后 Shell 完整重构，使用 MaterialDesignInXaml + Prism + CommunityToolkit.Mvvm

---

## [S1] 整体布局

```
┌─────────────┬──────────────────────────┐
│             │                          │
│   左侧边栏   │                          │
│   (可折叠)   │     内容区域              │
│             │     (Prism Region)        │
│             │                          │
│             ├──────────────────────────┤
│             │  状态栏 (右对齐)           │
│             │  连接状态 | 用户名 | 时间   │
└─────────────┴──────────────────────────┘
```

- **无标题栏**：`WindowStyle="None"`，全屏无边框
- **左侧边栏**：可折叠导航栏（汉堡按钮切换），60px/140px
- **内容区域**：Prism Region，占大部分空间
- **底部状态栏**：连接状态 + 用户名 + 时间，全部右对齐

---

## [S2] 左侧边栏（可折叠导航栏）

### 2.1 折叠/展开行为

| 状态 | 宽度 | 触发方式 |
|------|------|----------|
| 折叠 | 60px | 汉堡按钮或 Ctrl+M |
| 展开 | 140px | 汉堡按钮或 Ctrl+M |

### 2.2 内容分布

**折叠状态（60px）：**
```
┌──────┐
│  ≡   │  ← 汉堡按钮（居中）
│  🌿  │  ← Logo 图标（居中）
├──────┤
│  🏠  │  ← 导航图标（垂直排列，居中）
│  👤  │
│  🌿  │
│  ... │
├──────┤
│ ✏ 账户│  ← 账户设置按钮（仅图标）
│──────│
│  🌗  │  ← 主题切换
│──────│
│ 🚪 退出│  ← 退出登录（仅图标）
└──────┘
```

**展开状态（140px）：**
```
┌──────────────┐
│     ≡        │  ← 汉堡按钮（靠右）
│  🌿 凌隐宝堂  │  ← Logo + 品牌名（居中）
├──────────────┤
│  🏠 主页      │  ← 导航项（图标+文字，居中）
│  👤 患者管理   │
│  🌿 药材管理   │
│  ...         │
├──────────────┤
│ ✏ 账户       │  ← 账户设置（图标+文字）
│──────────────│
│  🌗          │  ← 主题切换
│──────────────│
│ 🚪 退出      │  ← 退出登录（图标+文字）
└──────────────┘
```

### 2.3 导航项

| 属性 | 折叠 | 展开 |
|------|------|------|
| 图标 | 显示（居中） | 显示（左侧） |
| 文字 | 隐藏 | 显示（图标右侧） |
| 选中态 | 图标高亮 | 整行高亮 |
| 悬停态 | 工具提示（Tooltip） | 背景变化 |

导航项通过 `BuildNavigationItems(UserRole role)` 动态构建，根据角色加载不同模块。

### 2.4 底部工具区

底部三个按钮之间用分隔线隔开，每个按钮独立区域：
- **账户设置**：`EditProfileCommand`，图标 `AccountEdit`
- **主题切换**：`IsDarkMode` 绑定 `MaterialDesignSwitchToggleButton`
- **退出登录**：`LogoutCommand`，图标 `Logout`

---

## [S3] 底部状态栏

```
┌──────────────────────────────────────────────────┐
│                                    🟢 远程模式  👤 张三  14:30:00 │
└──────────────────────────────────────────────────┘
```

- **右对齐**：所有信息靠右（`DockPanel.LastChildFill="False"` + `DockPanel.Dock="Right"`）
- **内容顺序**：连接状态图标 + 连接模式文字 | 用户图标 + 用户名 | 时间
- **高度**：32px
- **背景**：`MaterialDesign.Brush.Surface`

---

## [S4] 内容区域

- Prism Region（`ContentRegion`）
- 占据右侧上方大部分空间
- 背景：`MaterialDesign.Brush.Surface`
- 圆角：4px
- 内边距：4px

---

## [S5] 登录界面

- **保持不变**：登录表单、背景图片、品牌区
- **关闭按钮**：从 "✕" 图标改为 "退出" 文字按钮，位于卡片右上角
- **"凌隐宝堂中医诊所"标题**：合并到左侧品牌区（"大医精诚"上方）
- **"大医精诚"字号**：210px，列比例 2*:1*

---

## [S6] 技术实现

### 6.1 布局结构

```xml
<Window Style="{StaticResource MaterialDesignWindow}" WindowStyle="None">
  <Grid>
    <!-- Login (unchanged) -->
    <Grid Visibility="{Binding IsNotLoggedIn, ...}">
      <ContentControl prism:RegionManager.RegionName="LoginRegion" />
    </Grid>

    <!-- Post-login Shell -->
    <Grid Visibility="{Binding IsLoggedIn, ...}">
      <materialDesign:DialogHost Identifier="RootDialog">
        <Grid>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="{Binding SidebarWidth}" />  <!-- 左侧边栏 -->
            <ColumnDefinition Width="*" />                       <!-- 右侧内容 -->
          </Grid.ColumnDefinitions>
          <Grid.RowDefinitions>
            <RowDefinition Height="*" />       <!-- 内容区 -->
            <RowDefinition Height="32" />       <!-- 状态栏 -->
          </Grid.RowDefinitions>

          <!-- 左侧边栏 -->
          <Border Grid.Column="0" Grid.RowSpan="2" Background="{DynamicResource MaterialDesign.Brush.Primary}">
            <DockPanel LastChildFill="True">
              <!-- Top: 汉堡按钮 + Logo + 导航列表 -->
              <!-- Bottom: 账户设置 + 主题切换 + 退出 -->
            </DockPanel>
          </Border>

          <!-- 内容区域 -->
          <Border Grid.Column="1" Grid.Row="0" Margin="4" CornerRadius="4">
            <ContentControl prism:RegionManager.RegionName="ContentRegion" />
          </Border>

          <!-- 状态栏 -->
          <Border Grid.Column="1" Grid.Row="1">
            <DockPanel LastChildFill="False" Margin="12,0">
              <StackPanel DockPanel.Dock="Right">连接+用户+时间</StackPanel>
            </DockPanel>
          </Border>
        </Grid>
      </materialDesign:DialogHost>
      <materialDesign:Snackbar x:Name="MainSnackbar" .../>
    </Grid>
  </Grid>
</Window>
```

### 6.2 ViewModel 关键属性

```csharp
// 侧边栏
[ObservableProperty] private double _sidebarWidth = 60;
[ObservableProperty] private bool _isSidebarExpanded = false;
[RelayCommand] private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
partial void OnIsSidebarExpandedChanged(bool value) => SidebarWidth = value ? 140 : 60;

// 主题
[ObservableProperty] private bool _isDarkMode;
partial void OnIsDarkModeChanged(bool value) => _themeService.ApplyTheme(value);

// 状态栏
[ObservableProperty] private string _currentTimeDisplay;
public string CurrentUserDisplayName { get; }
public string CurrentUserInitial { get; }
public string CurrentUserRoleDisplay { get; }
public PackIconKind ApiStatusIcon { get; }
public Brush ApiStatusColor { get; }
```

### 6.3 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+M | 切换侧边栏 |
| Ctrl+N | 快速添加患者 |
| Ctrl+Shift+C | 快速开始看诊 |
| Ctrl+Shift+H | 显示导航历史 |
| Ctrl+, | 显示设置 |
| F1 | 帮助 |
| F6 | 循环切换区域 |
| Alt+Left/Right/Home | 后退/前进/主页 |

---

## [S7] 配色方案

- PrimaryColor: Brown (#5D4037)
- SecondaryColor: Amber (#FFC107)
- 侧边栏背景：`MaterialDesign.Brush.Primary`
- 侧边栏文字/图标：白色
- 状态栏背景：`MaterialDesign.Brush.Surface`

---

## [S8] 已知坑与解决方案

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| ToggleButton 与 Command 冲突 | IsChecked TwoWay 绑定先于 Command 执行 | 使用 `OnIsSidebarExpandedChanged` 回调 |
| 导航项左右间距不一致 | DockPanel LastChildFill=True + ListBox Left docking | ListBox 作为 LastChild + ItemContainerStyle Padding=0 |
| Snackbar 注册阻塞登录 | try-catch 包裹注册+初始化 | 拆成两个独立 try-catch |
| 端口 5100 被占用 | Hyper-V 排除范围 5041-5140 | 改为端口 5300 |
| 状态栏内容左对齐 | DockPanel LastChildFill=True 忽略 Dock=Right | 设置 LastChildFill=False |

---

## 决策记录

| # | 决策 | 结论 |
|---|------|------|
| 1 | 工具栏位置 | 左侧（非顶部） |
| 2 | 折叠方式 | 汉堡按钮切换（非 DrawerHost） |
| 3 | 折叠宽度 | 60px（仅图标） |
| 4 | 展开宽度 | 140px（图标+文字） |
| 5 | 用户信息位置 | 底部状态栏（右对齐） |
| 6 | 账户设置位置 | 侧边栏底部工具区（与导航分开） |
| 7 | 顶部 | 无内容 |
| 8 | 状态栏 | 底部（连接+用户+时间，右对齐） |
| 9 | 配色 | Brown + Amber |
| 10 | MDIX 版本 | 5.x（当前 5.3.2） |
| 11 | DialogHost | 完全迁移（停用 Prism IDialogService） |
| 12 | 标题栏 | 隐藏（WindowStyle=None） |
| 13 | 登录关闭按钮 | "退出" 文字（非 ✕ 图标） |
| 14 | LocalWebAPI 端口 | 5300（避免 Hyper-V 排除范围） |
