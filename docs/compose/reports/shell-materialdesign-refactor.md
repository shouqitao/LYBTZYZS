---
feature: shell-materialdesign-refactor
status: delivered
specs:
  - docs/compose/specs/2026-06-22-shell-redesign-design.md
plans:
  - docs/compose/plans/2026-06-22-shell-materialdesign-refactor.md
  - docs/compose/plans/2026-06-22-shell-materialdesign-refactor-impl.md
  - docs/compose/plans/2026-06-22-shell-redesign-impl.md
branch: master
commits: caf108835..98097ab70
---

# Shell MaterialDesignInXaml 重构 — 最终报告

## What Was Built

登录后 Shell 完全重构为 MaterialDesignInXaml (MDIX) 5.3.2 原生架构。主窗口使用 `WindowStyle="None"` 全屏无边框，左侧为可折叠导航栏（60px 折叠/140px 展开），右侧为 Prism 内容区域，底部为状态栏（连接状态 + 实时时钟）。配色方案为 Brown + Amber（`PrimaryColor="Brown" SecondaryColor="Amber"`），符合中医诊所稳重温暖的调性。

新增 4 个服务：`ThemeService`（Light/Dark 主题切换）、`DialogHostService`（MDIX DialogHost 封装，替代 Prism IDialogService）、`SnackbarService`（MDIX Snackbar 通知）、以及对应的 `IDialogHostService`/`ISnackbarService` 接口。旧的自定义控件 `SidebarControl` 和 `GlobalStatusBar` 已删除（共减少 538 行代码）。

登录界面（LoginView）保持不变，仅将关闭按钮从 "✕" 图标改为 "退出" 文字按钮，并将 "凌隐宝堂中医诊所" 标题合并到左侧品牌区（"大医精诚" 上方）。

## Architecture

### 主窗口布局

```
Window (WindowStyle="None", WindowState="Maximized")
  └─ Grid
       ├─ LoginRegion (Visibility 绑定 IsNotLoggedIn)
       └─ Post-login Shell (Visibility 绑定 IsLoggedIn)
            └─ DialogHost (Identifier="RootDialog")
                 └─ Grid (双列 + 双行)
                      ├─ Column 0, RowSpan 2: 左侧边栏 (Width={Binding SidebarWidth})
                      │    └─ DockPanel (LastChildFill=True)
                      │         ├─ Top: 汉堡按钮 + Logo + 导航列表
                      │         ├─ Bottom: 主题切换 + 用户卡 + 退出按钮
                      │         └─ Fill: ListBox (MaterialDesignNavigationPrimaryListBox)
                      ├─ Column 1, Row 0: 内容区域 (ContentRegion)
                      └─ Column 1, Row 1: 状态栏 (连接状态 + 时间)
```

### 关键组件

| 组件 | 文件 | 职责 |
|------|------|------|
| MainWindow.xaml | `Shell/Views/MainWindow.xaml` | Grid 双列布局 + DockPanel 侧边栏 |
| MainWindowViewModel | `Shell/ViewModels/MainWindowViewModel.cs` | 侧边栏状态、主题、导航、所有服务注入 |
| ThemeService | `Shell/Services/ThemeService.cs` | PaletteHelper 封装，`ApplyTheme(bool)` + `InitializeThemeSync()` |
| DialogHostService | `Shell/Services/DialogHostService.cs` | `DialogHost.Show(view, "RootDialog")` + Prism VM 桥接 |
| SnackbarService | `Shell/Services/SnackbarService.cs` | ISnackbarMessageQueue 封装 |
| Snackbar 控件 | MainWindow.xaml 内 `<materialDesign:Snackbar>` | 提供 MessageQueue 实例 |

### ViewModel 关键属性

```csharp
[ObservableProperty] private double _sidebarWidth = 60;       // 折叠60px / 展开140px
[ObservableProperty] private bool _isSidebarExpanded = false;  // 默认折叠
[ObservableProperty] private bool _isDarkMode;                 // 主题切换

public Visibility NavTextVisibility => IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
public PackIconKind ApiStatusIcon => ApiStatus switch { ... };
public Brush ApiStatusColor => ApiStatus switch { ... };
public string CurrentUserInitial => ...;
public string CurrentUserRoleDisplay => ...;
public string CurrentTimeDisplay => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

partial void OnIsSidebarExpandedChanged(bool value) => SidebarWidth = value ? 140 : 60;
partial void OnIsDarkModeChanged(bool value) => _themeService.ApplyTheme(value);
```

### Design Decisions

- **持久化侧边栏而非 DrawerHost**：用户要求工具栏在左侧且可折叠，不是 Material Design 的抽屉模式。使用 Grid 双列 + `Width` 绑定实现宽度动画。
- **DockPanel LastChildFill=True**：ListBox 作为最后一个子元素填充剩余空间，解决导航项右边距不一致问题。
- **`MaterialDesignNavigationPrimaryListBox` 内置样式**：保留 MDIX 的选中/悬停效果，通过 `ItemContainerStyle` 覆盖 `Padding=0` 和 `HorizontalContentAlignment=Stretch`。
- **`OnIsSidebarExpandedChanged` 替代 Command**：ToggleButton 的 `IsChecked` TwoWay 绑定与 Command 冲突（绑定先执行导致 toggle 两次），改用 `partial void` 回调。
- **DialogHost Identifier 而非 FindName**：MDIX 的 `DialogHost` 用 `Identifier` 字符串路由，`FindName` 无法定位。
- **Prism→DialogHost 桥接**：`ConfirmationDialogViewModel` 的 `RequestClose` 事件需手动转发到 `DialogHost.CloseDialogCommand`。

## Usage

### 侧边栏折叠/展开
- 点击汉堡按钮或 `Ctrl+M` 切换
- 折叠：60px 宽，仅显示图标
- 展开：140px 宽，显示图标 + 文字

### 主题切换
- 侧边栏底部的 `MaterialDesignSwitchToggleButton`
- 绑定 `IsDarkMode`，通过 `ThemeService.ApplyTheme(bool)` 切换

### 对话框
```csharp
// 通过 DI 注入 IDialogHostService
var confirmed = await _dialogHostService.ShowConfirmationAsync("确定删除？");
```

### 通知
```csharp
// 通过 DI 注入 ISnackbarService
_snackbarService.ShowSuccess("保存成功");
_snackbarService.ShowError("操作失败");
```

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+M | 切换侧边栏 |
| Ctrl+N | 快速添加患者 |
| Ctrl+Shift+C | 快速开始看诊 |
| Ctrl+, | 显示设置 |
| Ctrl+Shift+H | 显示导航历史 |
| F1 | 帮助 |
| F6 | 循环切换区域 |
| Alt+Left/Right/Home | 后退/前进/主页 |

## Verification

- **全解决方案构建**：`dotnet build LYBTZYZS.sln` — 0 错误
- **架构测试**：81/82 通过（1 个预存失败）
- **桌面测试**：预存环境问题（端口 5000 占用），与本次重构无关
- **代码审查**：4 个严重 + 7 个重要 + 8 个次要问题全部修复
- **手动验证**：登录界面、侧边栏折叠/展开、导航、主题切换、状态栏、退出登录均正常

## Journey Log

> 以下记录影响了最终设计的关键决策点。

- [pivot] 从 DrawerHost 抽屉模式改为 Grid 双列持久化侧边栏 — 用户要求工具栏在左侧而非顶部，且需要折叠/展开而非抽出/收起
- [dead end] ToggleButton 同时绑定 `IsChecked`（TwoWay）和 `Command` — 绑定先于 Command 执行，导致 toggle 两次回到原状态；改用 `OnIsSidebarExpandedChanged` 回调
- [dead end] `materialDesign:Snackbar` 作为容器包裹 Grid 内容 — Snackbar 不是容器，`Message` 属性拒绝 Grid 类型；改为将 Snackbar 作为兄弟元素放置
- [lesson] MDIX 5.3.2 没有 `SnackbarHost` 控件和 `MaterialDesignLightDarkToggleButton` 样式 — 需验证样式名是否存在再使用
- [lesson] DockPanel 中未设置 `DockPanel.Dock` 的子元素默认为 `Left`，只占内容宽度不撑满 — ListBox 必须作为 `LastChildFill=True` 的最后一个子元素

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/specs/2026-06-22-shell-redesign-design.md` | 最终设计文档 | 左侧可折叠侧边栏 + 内容区 + 状态栏布局 |
| `docs/compose/plans/2026-06-22-shell-materialdesign-refactor.md` | 初版研究方案 | DrawerHost 架构（已废弃，被 Grid 双列替代） |
| `docs/compose/plans/2026-06-22-shell-materialdesign-refactor-impl.md` | 初版实施计划 | 9 个任务（已全部完成但布局后来被重新设计） |
| `docs/compose/plans/2026-06-22-shell-redesign-impl.md` | 最终实施计划 | 5 个任务（可折叠侧边栏布局） |
