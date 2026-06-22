# 侧边栏 Material Design 迁移 Design

> **日期**: 2026-06-22
> **状态**: Approved (brainstorm phase)
> **范围**: 全面替换 HandyControl → MaterialDesignThemes M2，重写侧边栏为 ListBox + GroupStyle 可折叠导航
> **前置**: 替代 `2026-06-22-sidebar-full-redesign-design.md`（该方案手写 Button 导航，bug 多发）

---

## [S1] 背景

上一轮侧边栏重设计（commit `f9f564536`）手写了 Button + BoolToVis + Storyboard 折叠机制，导致多个交互 bug（双汉堡图标、折叠后无法展开、动画初始加载问题）。用户决定采用成熟的 MaterialDesignInXAML 库替代手写实现。

### 关键发现

- 项目 104 个 XAML 文件中，**仅 App.xaml 1 处声明 `xmlns:hc=`**，**0 处实际使用 `<hc:>` 控件**
- HandyControl 在本项目中**仅作主题颜色提供者**，未使用任何 HC 专属控件
- 替换 HC → M2 的影响面远小于预期：只需替换主题资源，业务 XAML 无需改动

---

## [S2] 设计目标与决策

### 目标

1. 用 MaterialDesignThemes M2 替换 HandyControl 主题
2. 侧边栏改为 ListBox 导航（M2 原生选中态、键盘导航）
3. 保留折叠/展开能力，但用正确模式实现
4. 消除上一轮的全部交互 bug

### 用户决策（brainstorm 确认）

| 维度 | 决定 |
|------|------|
| 范围 | 全面替换 HC → M2 |
| 版本 | M2（稳定版，非 Material Design 3） |
| 配色 | Primary=Brown + Secondary=Orange（中医传统色调） |
| Drawer 类型 | Standard（侧边栏始终在布局中，非模态） |
| 折叠 | 保留（220px ↔ 56px 可切换） |
| 明暗模式 | 仅亮色 |
| VM 层 | 同步重写（简化） |
| 集成方案 | ListBox + GroupStyle 分组 |

---

## [S3] 主题迁移

### App.xaml 改动

```xaml
<!-- 移除 -->
< hc xmlns 声明 >
<ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml" />
<ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml" />

<!-- 新增 -->
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"

<materialDesign:BundledTheme
    BaseTheme="Light"
    PrimaryColor="Brown"
    SecondaryColor="Orange" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
```

### DesignSystem.xaml 令牌策略

| 当前令牌 | 策略 | M2 替代或保留理由 |
|---------|------|-----------------|
| `SidebarBrush` | 删除 | `MaterialDesign.Brush.SurfaceVariant` |
| `SidebarTextBrush` | 删除 | `MaterialDesign.Brush.OnSurfaceVariant` |
| `SidebarHoverBrush` | 删除 | ListBox hover 自带 M2 样式 |
| `SidebarDividerBrush` | 删除 | `MaterialDesign.Brush.Outline` |
| `SidebarAvatarBrush` | 保留 | 项目特定（avatar 背景色），映射到 `Primary` |
| `PrimaryBrush` | 重映射 | 指向 M2 `Primary` (Brown) |
| `DangerBrush` | 保留 | `MaterialDesign.Brush.ValidationError` 或 Red |
| `BorderLightBrush` | 删除 | `MaterialDesign.Brush.OutlineVariant` |

---

## [S4] 侧边栏布局

### 展开态（220px）

```
┌──────────────────────────┐
│ 🏠 凌隐宝堂          [☰] │  品牌行 (56px) + 折叠按钮
├──────────────────────────┤
│                          │
│  主页                     │  GroupStyle header
│  ╭────────────────────╮  │
│  │ 🏠 主页             │  │  M2 ListBoxItem 选中态
│  ╰────────────────────╯  │  (药丸形 Primary 背景)
│                          │
│  诊疗                     │
│  👥 患者管理              │
│  📋 医案管理              │
│  🌿 经验方管理            │
│                          │
│  管理                     │
│  ⚙️ 用户管理              │
│                          │
├──────────────────────────┤
│ ┌──┐ 张三                │  用户卡 (可点 → EditProfile)
│ │张│ 主治医生             │
│ └──┘                     │
├──────────────────────────┤
│ ● 远程  🕐 14:23         │  状态条
└──────────────────────────┘
```

### 折叠态（56px）

```
┌────┐
│ 🏠 │  仅 Logo
├────┤
│ 🏠 │  ListBox 图标-only
│ 👥 │
│ 📋 │
│ 🌿 │
│ ⚙️ │
├────┤
│ 张 │  仅 avatar
├────┤
│ ●  │  状态圆点
└────┘
```

### 关键元素规格

| 元素 | 展开态 | 折叠态 |
|------|--------|--------|
| 侧边栏总宽 | 220px | 56px |
| 品牌行 | 56px，Logo + "凌隐宝堂" | 56px，仅 Logo 居中 |
| 折叠按钮 | 始终可见，右上角 | 始终可见，固定位置 |
| ListBox 项 | 图标(20px) + 文字 | 仅图标居中 |
| 分组标题 | 11px 灰色 | 隐藏 |
| 用户卡 | avatar(32px) + 姓名 + 角色 | 仅 avatar(28px) |
| 状态条 | API + 模式 + 时钟 | 仅圆点 + tooltip |

---

## [S5] ListBox 数据绑定

### XAML 结构

```xaml
<ListBox
    ItemsSource="{Binding GroupedNavItems}"
    SelectedItem="{Binding SelectedNavItem, Mode=TwoWay}"
    Style="{StaticResource MaterialDesignListBox}">
    <ListBox.GroupStyle>
        <GroupStyle HeaderTemplate="{StaticResource NavGroupHeaderTemplate}" />
    </ListBox.GroupStyle>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <Path Data="{Binding IconData}" ... />
                <TextBlock Text="{Binding Title}"
                           Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### VM 层改动（MainWindowViewModel）

**新增**：

```csharp
[ObservableProperty]
private NavigationItem? _selectedNavItem;

public ICollectionView GroupedNavItems
{
    get
    {
        var view = CollectionViewSource.GetDefaultView(NavigationItems);
        if (!view.GroupDescriptions.OfType<PropertyGroupDescription>().Any())
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Group)));
        return view;
    }
}
```

**删除**：
- `HomeNavItems` / `BusinessNavItems` / `AdminNavItems` 计算属性
- 对应的 `OnPropertyChanged` 调用

**重命名**（保留但改名以更清晰）：
- `IsDrawerOpen` → `IsSidebarExpanded`（语义更明确）
- `ToggleDrawerCommand` → `ToggleSidebarCommand`

**保留**：
- `NavigationItems` 单一集合（VM 构造，CollectionViewSource 分组）
- `IsSidebarExpanded` 属性（侧边栏展开状态）
- `ToggleSidebarCommand` 命令（Ctrl+M 快捷键 + 折叠按钮绑定）
- `CurrentUser` + `ProfileUpdatedEvent` 跨 VM 同步
- `EditProfileCommand` / `NavigateToHomeCommand` / `LogoutCommand`
- `NavigationItem.Group` 字段（CollectionViewSource 用于 GroupBy）

### 选中态导航逻辑

```csharp
partial void OnSelectedNavItemChanged(NavigationItem? value)
{
    if (value?.ViewName is string viewName)
        _navigationCoordinator.NavigateTo(viewName);
}
```

---

## [S6] 折叠/展开修复

### Bug 消除矩阵

| 上轮 Bug | 根因 | 新设计修复 |
|---------|------|-----------|
| 两个汉堡图标 | 品牌行 + 独立按钮都用汉堡 Path | 品牌行只用 Logo（home icon），折叠按钮单独 |
| 折叠后无法展开 | Toggle Button `Visibility=BoolToVis` 隐藏了 | 折叠按钮 `Visibility` 永远 Visible |
| 动画初始加载 | `EnterActions` 不在初次加载时触发 | 基础 Setter 设为展开态宽度 220px，trigger 在 False 时动画到 56 |
| 信息丢失 | 手写 BoolToVis 不一致 | ListBox ItemTemplate 的 DataTrigger 统一控制 |
| 死 DP | `NavigationItemsSource` 暴露但无 XAML 引用 | 用标准 ListBox `ItemsSource`，无自定义 DP |

### 动画规格

```xaml
<Border x:Name="RootBorder">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="Width" Value="220" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="False">
                    <DataTrigger.EnterActions>
                        <BeginStoryboard>
                            <Storyboard>
                                <DoubleAnimation
                                    Storyboard.TargetProperty="Width"
                                    To="56" Duration="0:0:0.2"
                                    EasingFunction="{StaticResource CubicEase}" />
                            </Storyboard>
                        </BeginStoryboard>
                    </DataTrigger.EnterActions>
                    <DataTrigger.ExitActions>
                        <BeginStoryboard>
                            <Storyboard>
                                <DoubleAnimation
                                    Storyboard.TargetProperty="Width"
                                    To="220" Duration="0:0:0.2"
                                    EasingFunction="{StaticResource CubicEase}" />
                            </Storyboard>
                        </BeginStoryboard>
                    </DataTrigger.ExitActions>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
</Border>
```

---

## [S7] 文件改动清单

| 文件 | 改动类型 | 涉及 Phase |
|------|---------|-----------|
| `Directory.Packages.props` | 加 MaterialDesignThemes 5.3.2 | Phase 1 |
| `LYBT.Desktop.Controls.csproj` | 加 PackageReference | Phase 1 |
| `LYBT.Desktop.Shell.csproj` | 加 PackageReference | Phase 1 |
| `App.xaml` | 替换主题资源 | Phase 1 |
| `Themes/DesignSystem.xaml` | 重映射/删除自定义令牌 | Phase 1 |
| `ViewModels/MainWindowViewModel.cs` | 简化（删 IsDrawerOpen/3 分组，加 SelectedNavItem + GroupedNavItems） | Phase 2 |
| `Controls/SidebarControl.xaml` | 完全重写（ListBox + GroupStyle + 折叠） | Phase 3 |
| `Controls/SidebarControl.xaml.cs` | 大幅简化（删除多个 DP） | Phase 3 |
| `Views/MainWindow.xaml` | 更新绑定（IsDrawerOpen→IsSidebarExpanded, 新 GroupedNavItems）；保留 Ctrl+M 绑定到 ToggleSidebarCommand | Phase 4 |
| 其他业务 XAML (100+) | **无需改动**（M2 主题自动应用） | — |

---

## [S8] 实施顺序

```
Phase 1: 装包 + 主题切换
  ├─ 安装 MaterialDesignThemes 5.3.2
  ├─ App.xaml 替换 HC → M2 BundledTheme(Brown/Orange/Light)
  ├─ DesignSystem.xaml 重映射令牌
  └─ build + 启动验证

Phase 2: VM 层简化
  ├─ MainWindowViewModel: 删 IsDrawerOpen/ToggleDrawerCommand/3 分组属性
  ├─ 加 SelectedNavItem + GroupedNavItems (CollectionViewSource)
  ├─ 保留 ProfileUpdatedEvent + EditProfileCommand
  └─ build 验证

Phase 3: 侧边栏 XAML 重写
  ├─ SidebarControl.xaml: ListBox + GroupStyle + 折叠动画
  ├─ SidebarControl.xaml.cs: 简化 DP
  └─ build 验证

Phase 4: MainWindow.xaml 适配
  ├─ 更新 SidebarControl 绑定
  └─ build 验证

Phase 5: 全量验证
  ├─ dotnet build LYBTZYZS.sln (0 errors)
  ├─ dotnet test tests/LYBT.Tests.Desktop/
  ├─ 手工 UI 验证
  └─ 检查其他页面视觉回归
```

---

## [S9] 风险控制

| 风险 | 概率 | 应对 |
|------|------|------|
| M2 主题破坏登录页 | 中 | Phase 1 后立即检查；必要时局部覆盖 |
| 图标颜色不协调 | 低 | 用 `MaterialDesign.Brush.OnSurface` 自动适配 |
| CollectionViewSource 分组冲突 | 低 | GroupBy 在 view 层，过滤在 build 层 |
| 折叠动画初始加载 bug 重现 | 低 | 基础 Setter=展开态，trigger 在 False 动画 |
| HC 残留引用编译错误 | 中 | Phase 1 后 grep 清理 |

### 回滚策略

每 Phase 单独提交。某 Phase 出问题 → `git revert <sha>`。最坏情况回滚 Phase 1。

---

## [S10] 测试覆盖

| 类型 | 覆盖点 |
|------|--------|
| 单元测试 | NavigationItem.Group 默认值（已有）；SelectedNavItem 双向绑定 |
| 手工 UI | 4 角色登录 + 折叠/展开 + 导航选中 + 用户卡点击 + 编辑同步 |
| 视觉回归 | 登录页、患者列表、医案编辑、处方打印预览 |
| 构建 | dotnet build 0 errors |

---

## 变更记录

| 日期 | 变更 | 作者 |
|------|------|------|
| 2026-06-22 | 初始版本（brainstorm 完成） | MiMoCode Compose |
