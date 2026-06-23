# Shell 重构设计方案

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/shell-materialdesign-refactor.md)

**Date**: 2026-06-22
**Status**: 已批准
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
│             │   状态栏 (连接状态+时间)    │
└─────────────┴──────────────────────────┘
```

- **左侧边栏**：可折叠导航栏（汉堡按钮切换）
- **内容区域**：Prism Region，占大部分空间
- **底部状态栏**：连接状态 + 时间，占小部分空间
- **顶部**：无内容（空）

---

## [S2] 左侧边栏（可折叠导航栏）

### 2.1 折叠/展开行为

| 状态 | 宽度 | 触发方式 |
|------|------|----------|
| 折叠 | ~60px | 汉堡按钮点击 |
| 展开 | ~280px | 汉堡按钮点击 |

### 2.2 内容分布

**折叠状态（60px）：**
```
┌──────┐
│ logo │  ← 顶部：logo图标（居中）
├──────┤
│  🏠  │  ← 中间：导航图标（垂直排列，居中）
│  👤  │
│  🌿  │
│  📋  │
│  ... │
├──────┤
│  👤  │  ← 底部：用户头像（居中）
│  🚪  │  ← 退出图标
└──────┘
```

**展开状态（280px）：**
```
┌──────────────────────┐
│ 🌿 凌隐宝堂           │  ← 顶部：logo + 品牌名
├──────────────────────┤
│ 🏠 主页               │  ← 中间：导航项（图标+文字）
│ 👤 患者管理            │
│ 🌿 药材管理            │
│ 📋 验方管理            │
│ ...                  │
├──────────────────────┤
│ 👤 张三               │  ← 底部：用户头像+名字+角色
│    医生               │
│ [退出登录]            │  ← 退出按钮
└──────────────────────┘
```

### 2.3 导航项

| 属性 | 折叠 | 展开 |
|------|------|------|
| 图标 | 显示（居中） | 显示（左侧） |
| 文字 | 隐藏 | 显示（图标右侧） |
| 选中态 | 图标高亮 | 整行高亮 |
| 悬停态 | 工具提示（Tooltip） | 背景变化 |

### 2.4 用户卡

| 属性 | 折叠 | 展开 |
|------|------|------|
| 头像 | 显示（居中，36x36） | 显示（左侧，36x36） |
| 名字 | 隐藏 | 显示 |
| 角色 | 隐藏 | 显示（小字） |
| 退出按钮 | 图标 | 图标+文字 |

---

## [S3] 底部状态栏

```
┌──────────────────────────────────────────┐
│ 🟢 远程模式          2026-06-22 14:30:00 │
└──────────────────────────────────────────┘
```

- **左侧**：连接状态图标（绿/橙/灰）+ 连接模式文字（远程/本地）
- **右侧**：当前时间（实时更新）
- **高度**：~32px
- **背景**：`MaterialDesign.Brush.Surface`

---

## [S4] 内容区域

- Prism Region（`ContentRegion`）
- 占据右侧上方大部分空间
- 背景：`MaterialDesign.Brush.Surface`
- 圆角：8px（可选）
- 内边距：12px

---

## [S5] 技术实现

### 5.1 布局结构

```xml
<Window Style="{StaticResource MaterialDesignWindow}">
  <materialDesign:DialogHost Identifier="RootDialog">
    <Grid>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />  <!-- 左侧边栏 -->
        <ColumnDefinition Width="*" />     <!-- 右侧内容 -->
      </Grid.ColumnDefinitions>
      <Grid.RowDefinitions>
        <RowDefinition Height="*" />       <!-- 内容区 -->
        <RowDefinition Height="Auto" />    <!-- 状态栏 -->
      </Grid.RowDefinitions>

      <!-- 左侧边栏 -->
      <Border Grid.Column="0" Grid.RowSpan="2"
              Width="{Binding SidebarWidth}">
        <!-- 导航内容 -->
      </Border>

      <!-- 内容区域 -->
      <ContentControl Grid.Column="1" Grid.Row="0"
                      prism:RegionManager.RegionName="ContentRegion" />

      <!-- 状态栏 -->
      <Border Grid.Column="1" Grid.Row="1" Height="32">
        <!-- 连接状态 + 时间 -->
      </Border>
    </Grid>
  </materialDesign:DialogHost>
</Window>
```

### 5.2 ViewModel 属性

```csharp
// 侧边栏宽度
[ObservableProperty]
private double _sidebarWidth = 60;  // 默认折叠

[RelayCommand]
private void ToggleSidebar()
{
    SidebarWidth = SidebarWidth == 60 ? 280 : 60;
    IsSidebarExpanded = !IsSidebarExpanded;
}

[ObservableProperty]
private bool _isSidebarExpanded = false;

// 导航项文字可见性
public Visibility NavTextVisibility =>
    IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
```

### 5.3 XAML 绑定

```xml
<!-- 导航项文字 -->
<TextBlock Text="{Binding Title}"
           Visibility="{Binding DataContext.NavTextVisibility,
                       RelativeSource={RelativeSource AncestorType=Window}}" />

<!-- 用户名 -->
<TextBlock Text="{Binding CurrentUserDisplayName}"
           Visibility="{Binding DataContext.NavTextVisibility,
                       RelativeSource={RelativeSource AncestorType=Window}}" />
```

---

## [S6] 配色方案

保持 Brown + Amber：
- PrimaryColor: Brown (#5D4037)
- SecondaryColor: Amber (#FFC107)

---

## [S7] 与 MDIX Demo 的差异

| 维度 | MDIX Demo | 本方案 |
|------|-----------|--------|
| 工具栏位置 | 顶部（ColorZone） | 左侧（可折叠边栏） |
| 导航方式 | DrawerHost（抽屉） | 持久化边栏（可折叠） |
| 顶部内容 | 标题+设置弹窗 | 无 |
| 状态栏 | 无 | 底部（连接+时间） |
| 折叠机制 | 无 | 汉堡按钮切换 60px/280px |

---

## 决策记录

| # | 决策 | 结论 |
|---|------|------|
| 1 | 工具栏位置 | 左侧（非顶部） |
| 2 | 折叠方式 | 汉堡按钮切换，非 DrawerHost |
| 3 | 折叠状态 | 60px（仅图标） |
| 4 | 展开状态 | 280px（图标+文字） |
| 5 | 用户卡 | 跟随折叠（头像 / 头像+名字+角色） |
| 6 | 顶部 | 无内容 |
| 7 | 状态栏 | 底部（连接状态+时间） |
| 8 | 配色 | Brown + Amber（不变） |
| 9 | MDIX 版本 | 5.x（当前 5.3.2） |
| 10 | DialogHost | 完全迁移（停用 Prism IDialogService） |
