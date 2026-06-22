# 侧边栏 Material Design 迁移 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用 MaterialDesignThemes M2 全面替换 HandyControl，侧边栏改为 ListBox + GroupStyle 可折叠导航。

**Architecture:** 移除 HC 主题（项目仅用 HC 颜色，0 处用 HC 控件），加载 M2 BundledTheme(Brown/Orange/Light)。侧边栏重写为 ListBox（M2 原生选中态、键盘导航、ripple 效果），用 CollectionViewSource GroupStyle 分组主页/诊疗/管理。保留折叠能力但用正确的 DataTrigger 模式（基础 Setter 为展开态，trigger 在 False 时动画到折叠态）。

**Tech Stack:** .NET 8 WPF, MaterialDesignThemes 5.3.2 (M2), Prism.DryIoc, CommunityToolkit.Mvvm, CollectionViewSource。

**Spec:** `docs/compose/specs/2026-06-22-sidebar-material-design-migration-design.md`

---

## 文件结构

| 文件 | 责任 | 操作 |
|------|------|------|
| `Directory.Packages.props` | 集中包版本管理 | 加 MaterialDesignThemes |
| `LYBT.Desktop.Controls.csproj` | Controls 项目 | 加 PackageReference |
| `LYBT.Desktop.Shell.csproj` | Shell 项目 | 加 PackageReference |
| `App.xaml` | 全局主题资源 | 替换 HC → M2 BundledTheme |
| `Themes/DesignSystem.xaml` | 项目令牌 | 删 Sidebar* 笔刷 + PrimaryColor 冲突 |
| `Controls/SidebarControl.xaml` | 侧边栏 UI | 完全重写（ListBox） |
| `Controls/SidebarControl.xaml.cs` | 侧边栏 code-behind | 大幅简化 DP |
| `ViewModels/MainWindowViewModel.cs` | 主窗口 VM | 简化 + 重命名 + 加 SelectedNavItem |
| `Views/MainWindow.xaml` | 主窗口 | 更新绑定 |

---

## Task 1: 安装 MaterialDesignThemes NuGet 包

**Covers:** [S3]

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
- Modify: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`

- [ ] **Step 1: 在 Directory.Packages.props 加版本声明**

在 `<ItemGroup Label="WPF and Desktop Packages">` 内（HandyControl 行附近）加：

```xml
<PackageVersion Include="MaterialDesignThemes" Version="5.3.2" />
```

- [ ] **Step 2: 在 LYBT.Desktop.Controls.csproj 加引用**

在 `<ItemGroup>` 包引用区域加：

```xml
<PackageReference Include="MaterialDesignThemes" />
```

（不带 Version，因中央包管理由 Directory.Packages.props 提供）

- [ ] **Step 3: 在 LYBT.Desktop.Shell.csproj 加引用**

同样加：

```xml
<PackageReference Include="MaterialDesignThemes" />
```

- [ ] **Step 4: 验证还原 + 编译**

Run: `dotnet restore LYBTZYZS.sln`
Expected: 包成功还原，无版本冲突

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors（M2 包不应破坏现有 HC 主题——还未替换主题）

- [ ] **Step 5: 提交**

```bash
git add Directory.Packages.props src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
git commit -m "chore: add MaterialDesignThemes 5.3.2 package reference"
```

---

## Task 2: 替换 App.xaml 主题（HC → M2）

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml`

- [ ] **Step 1: 修改 App.xaml**

完整替换 App.xaml 内容为：

```xaml
<prism:PrismApplication
    x:Class="LYBT.Desktop.Shell.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:prism="http://prismlibrary.com/"
    mc:Ignorable="d">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!--  1. Material Design 2 主题（替代 HandyControl） -->
                <materialDesign:BundledTheme
                    BaseTheme="Light"
                    PrimaryColor="Brown"
                    SecondaryColor="Orange" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />

                <!--  2. 设计系统 (项目特定令牌，覆盖 M2 默认值) -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/DesignSystem.xaml" />

                <!--  3. 矢量图标 Path data -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Icons.xaml" />

                <!--  4. 项目特定设计 Token (间距等) -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Theme.Light.xaml" />

                <!--  5. UI 统一化组件样式 -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/UnifiedComponents.xaml" />

                <!--  6. 首页共享样式 (Admin/Clinical) -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/HomePageStyles.xaml" />

                <!--  7. Shell 全局样式 -->
                <ResourceDictionary Source="Styles/Typography.xaml" />
                <ResourceDictionary Source="Styles/Controls.xaml" />

                <!--  8. 对话框窗口样式 -->
                <ResourceDictionary Source="Styles/DialogStyles.xaml" />

                <!--  9. 统一转换器资源字典 -->
                <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Converters/Converters.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</prism:PrismApplication>
```

变更点：
- 移除 `xmlns:hc="https://handyorg.github.io/handycontrol"` 声明
- 新增 `xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"`
- 用 `materialDesign:BundledTheme` + `MaterialDesign2.Defaults.xaml` 替换 HC 的 `SkinDefault.xaml` + `Theme.xaml`
- DesignSystem.xaml 加载顺序保持在 M2 之后（允许项目令牌覆盖 M2 默认值）

- [ ] **Step 2: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors。可能有 warning 关于 HC 残留引用——后续 Task 3 处理。

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/App.xaml
git commit -m "feat(theme): replace HandyControl with MaterialDesignThemes M2 (Brown/Orange/Light)"
```

---

## Task 3: 清理 DesignSystem.xaml（删 Sidebar 笔刷 + PrimaryColor 冲突）

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml`

- [ ] **Step 1: 删除 Sidebar 相关笔刷（行 12-18）**

删除整个 `<!-- Sidebar brushes (TCM brown sidebar) -->` 注释 + 6 个笔刷定义：

```xml
<!-- 删除以下内容 -->
<!-- Sidebar brushes (TCM brown sidebar) -->
<SolidColorBrush x:Key="SidebarBrush" Color="{StaticResource PrimaryColor}" />
<SolidColorBrush x:Key="SidebarTextBrush" Color="#FFFFFF" />
<SolidColorBrush x:Key="SidebarSecondaryTextBrush" Color="#D7CCC8" />
<SolidColorBrush x:Key="SidebarHoverBrush" Color="{StaticResource DarkPrimaryColor}" />
<SolidColorBrush x:Key="SidebarDividerBrush" Color="#6D4C41" />
<SolidColorBrush x:Key="SidebarAvatarBrush" Color="#8D6E63" />
```

- [ ] **Step 2: 删除与 M2 冲突的 Primary 色定义（行 5-10）**

删除以下 Color 和 Brush 定义（M2 BundledTheme 会提供同名的 Primary 色资源）：

```xml
<!-- 删除以下内容 -->
<Color x:Key="PrimaryColor">#5D4037</Color>
<Color x:Key="DarkPrimaryColor">#4E342E</Color>
<Color x:Key="LightPrimaryColor">#D7CCC8</Color>
<SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
<SolidColorBrush x:Key="DarkPrimaryBrush" Color="{StaticResource DarkPrimaryColor}" />
<SolidColorBrush x:Key="LightPrimaryBrush" Color="{StaticResource LightPrimaryColor}" />
```

> **注意**：M2 BundledTheme 会自动生成 `PrimaryColor`、`PrimaryBrush`、`DarkPrimaryColor`、`LightPrimaryBrush` 等同名资源。保留我们的会导致重复键冲突。

- [ ] **Step 3: 保留其他所有令牌**

以下**不删除**（项目语义令牌，不与 M2 冲突）：
- `AccentColor` / `AccentBrush` (#8D6E63)
- `SuccessBrush` / `WarningBrush` / `DangerBrush` / `InfoBrush` 及其 Color/Light/Dark 变体
- `BackgroundBrush` / `SurfaceBrush` / `BorderBrush`
- `PrimaryTextBrush` / `SecondaryTextBrush` / `DisabledTextBrush`
- 所有 Font / Spacing / Radius / Shadow 令牌
- `ProgressBarBrush` / `AccentOrangeBrush` / `BorderLightBrush` 等

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors。如果有 XAML 引用 `{StaticResource SidebarBrush}` 等被删令牌，编译会失败——需要逐一修复引用（grep 查找）。

- [ ] **Step 5: 清理 Sidebar 令牌的 XAML 引用**

Run: `grep -r "SidebarBrush\|SidebarTextBrush\|SidebarHoverBrush\|SidebarDividerBrush\|SidebarAvatarBrush\|SidebarSecondaryTextBrush" src/Client/Desktop --include="*.xaml"`

对每个引用文件，将 `{StaticResource SidebarBrush}` 替换为 `{DynamicResource MaterialDesign.Brush.SurfaceVariant}` 等 M2 对应资源。具体映射：

| 删除的令牌 | M2 替代 |
|----------|---------|
| `SidebarBrush` | `MaterialDesign.Brush.SurfaceVariant` |
| `SidebarTextBrush` | `MaterialDesign.Brush.OnSurface` |
| `SidebarSecondaryTextBrush` | `MaterialDesign.Brush.OnSurfaceVariant` |
| `SidebarHoverBrush` | `MaterialDesign.Brush.Surface`（hover 效果 ListBox 自带） |
| `SidebarDividerBrush` | `MaterialDesign.Brush.Outline` |
| `SidebarAvatarBrush` | 保留为自定义：在 DesignSystem.xaml 末尾加 `<SolidColorBrush x:Key="SidebarAvatarBrush" Color="#8D6E63" />`（项目特定） |

> 注：`SidebarAvatarBrush` 是项目特定的 avatar 背景色，建议保留。其他 5 个用 M2 对应资源替代。

- [ ] **Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml
git commit -m "refactor(theme): remove HC Sidebar brushes + Primary conflict, use M2 color roles"
```

---

## Task 4: MainWindowViewModel 简化 + 重命名 + 加 SelectedNavItem

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: 重命名 IsDrawerOpen → IsSidebarExpanded**

使用 IDE 重命名功能（或全局搜索替换）：
- `_isDrawerOpen` → `_isSidebarExpanded`
- `IsDrawerOpen` 属性 → `IsSidebarExpanded`
- `ToggleDrawerCommand` 方法/属性 → `ToggleSidebarCommand`

确保所有引用都更新（MainWindow.xaml 绑定会在 Task 7 处理）。

- [ ] **Step 2: 删除 3 个分组计算属性**

删除以下属性（约在文件 line 670-690）：

```csharp
// 删除这些
public ObservableCollection<NavigationItem> HomeNavItems => ...
public ObservableCollection<NavigationItem> BusinessNavItems => ...
public ObservableCollection<NavigationItem> AdminNavItems => ...
```

以及它们对应的 `OnPropertyChanged(nameof(HomeNavItems))` 等调用（2 处：登录成功后 + 登出时）。

- [ ] **Step 3: 新增 SelectedNavItem 属性**

在 `_navigationItems` 字段附近添加：

```csharp
[ObservableProperty]
private NavigationItem? _selectedNavItem;

partial void OnSelectedNavItemChanged(NavigationItem? value)
{
    if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
    {
        _navigationCoordinator.NavigateTo(viewName);
    }
}
```

- [ ] **Step 4: 新增 GroupedNavItems 属性**

```csharp
private ICollectionView? _groupedNavItems;

public ICollectionView GroupedNavItems
{
    get
    {
        if (_groupedNavItems == null)
        {
            _groupedNavItems = CollectionViewSource.GetDefaultView(NavigationItems);
            _groupedNavItems.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Group)));
        }
        return _groupedNavItems;
    }
}
```

确保文件顶部有 `using System.Windows.Data;`（CollectionViewSource 所在命名空间）。

- [ ] **Step 5: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --nologo`
Expected: 0 errors（可能有 MainWindow.xaml 绑定错误，因 IsDrawerOpen 改名了——下个 Task 修复）

- [ ] **Step 6: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "refactor(vm): rename IsDrawerOpen→IsSidebarExpanded, add SelectedNavItem + GroupedNavItems"
```

---

## Task 5+6: 重写 SidebarControl.xaml + 清理 code-behind（合并执行）

**Covers:** [S4] [S5] [S6]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs`

> **重要**：这两个文件必须在**同一 commit** 中修改，因为 XAML 编译时验证 code-behind 的 DP/事件处理器引用。分开提交会导致中间状态编译失败。

- [ ] **Step 1: 清理 SidebarControl.xaml.cs（先改 code-behind）**

在 `SidebarControl.xaml.cs` 中：

**删除**以下 DP（不再需要）：
- `HomeNavItems` / `HomeNavItemsProperty`
- `BusinessNavItems` / `BusinessNavItemsProperty`
- `AdminNavItems` / `AdminNavItemsProperty`
- `NavigationItemsSource` / `NavigationItemsSourceProperty`

**删除**事件处理器：`OnMoreButtonClick` 和 `OnCollapsedAvatarClick`

**新增**两个 DP（在原 `NavigationItemsSource` 位置）：

```csharp
#region GroupedNavItems - 分组导航视图

public ICollectionView GroupedNavItems
{
    get => (ICollectionView)GetValue(GroupedNavItemsProperty);
    set => SetValue(GroupedNavItemsProperty, value);
}

public static readonly DependencyProperty GroupedNavItemsProperty =
    DependencyProperty.Register(nameof(GroupedNavItems), typeof(ICollectionView),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion

#region SelectedNavItem - 当前选中导航项

public NavigationItem? SelectedNavItem
{
    get => (NavigationItem?)GetValue(SelectedNavItemProperty);
    set => SetValue(SelectedNavItemProperty, value);
}

public static readonly DependencyProperty SelectedNavItemProperty =
    DependencyProperty.Register(nameof(SelectedNavItem), typeof(NavigationItem),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion
```

需要 `using System.ComponentModel;` 和 `using System.Windows.Data;`。

**保留**以下 DP 不变：
- `IsExpanded`（默认 true）
- `CurrentUser` / `ApiStatus` / `CurrentTime` / `IsRemoteMode` / `ConnectionDisplay`
- `ToggleCommand` / `NavigateToHomeCommand` / `EditProfileCommand` / `LogoutCommand`

- [ ] **Step 2: 替换 SidebarControl.xaml 全部内容**

用以下 XAML 完整替换文件：

```xaml
<!--
    SidebarControl - M2 ListBox 侧边栏 (Material Design Migration 2026-06-22)

    特性：
    - ListBox + GroupStyle 分组导航（主页/诊疗/管理）
    - M2 原生 ListBoxItem 选中态（药丸形 Primary 背景）
    - 可折叠（220px ↔ 56px），单一折叠按钮始终可见
    - 用户卡 + 状态条

    绑定：
    - GroupedNavItems: ICollectionView (含 GroupDescriptions)
    - SelectedNavItem: NavigationItem? (TwoWay)
    - IsExpanded: bool (折叠状态)
    - ToggleCommand / EditProfileCommand / NavigateToHomeCommand / LogoutCommand
    - CurrentUser / ApiStatus / CurrentTime / IsRemoteMode / ConnectionDisplay
-->
<UserControl
    x:Class="LYBT.Desktop.Controls.Controls.SidebarControl"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
    xmlns:models="clr-namespace:LYBT.Desktop.Controls.Models"
    x:Name="Root">

    <UserControl.Resources>
        <ResourceDictionary>
            <CubicEase x:Key="CubicEase" />

            <!-- 导航项数据模板（展开态：图标 + 文字） -->
            <DataTemplate x:Key="NavItemTemplate">
                <StackPanel Orientation="Horizontal" Margin="16,0">
                    <Path
                        Width="20" Height="20"
                        Data="{Binding IconData}"
                        Stretch="Uniform"
                        Fill="{Binding RelativeSource={RelativeSource AncestorType=ListBoxItem}, Path=Foreground}" />
                    <TextBlock
                        Margin="12,0,0,0"
                        VerticalAlignment="Center"
                        FontSize="14"
                        Text="{Binding Title}"
                        Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
                </StackPanel>
            </DataTemplate>

            <!-- 分组标题模板 -->
            <DataTemplate x:Key="NavGroupHeaderTemplate">
                <TextBlock
                    Padding="16,12,0,4"
                    FontSize="11"
                    FontWeight="Medium"
                    Foreground="{DynamicResource MaterialDesign.Brush.OnSurfaceVariant}"
                    Text="{Binding Name}"
                    Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
            </DataTemplate>
        </ResourceDictionary>
    </UserControl.Resources>

    <!-- 根 Border：宽度动画。基础 Setter=220（展开态），trigger 在 False 时动画到 56 -->
    <Border x:Name="RootBorder" Background="{DynamicResource MaterialDesign.Brush.SurfaceVariant}">
        <Border.Style>
            <Style TargetType="Border">
                <Setter Property="Width" Value="220" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="False">
                        <DataTrigger.EnterActions>
                            <BeginStoryboard>
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetProperty="Width" To="56"
                                        Duration="0:0:0.2" EasingFunction="{StaticResource CubicEase}" />
                                </Storyboard>
                            </BeginStoryboard>
                        </DataTrigger.EnterActions>
                        <DataTrigger.ExitActions>
                            <BeginStoryboard>
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetProperty="Width" To="220"
                                        Duration="0:0:0.2" EasingFunction="{StaticResource CubicEase}" />
                                </Storyboard>
                            </BeginStoryboard>
                        </DataTrigger.ExitActions>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- Row 0: 品牌行 + 折叠按钮 -->
            <DockPanel Grid.Row="0" LastChildFill="False">
                <!-- 折叠按钮（始终可见，右上角） -->
                <Button
                    DockPanel.Dock="Right"
                    Width="40" Height="40"
                    Margin="8,8,8,0"
                    Command="{Binding ToggleCommand, ElementName=Root}"
                    Style="{StaticResource MaterialDesignIconButton}"
                    ToolTip="展开/收缩 (Ctrl+M)">
                    <materialDesign:PackIcon Kind="Menu" />
                </Button>

                <!-- 品牌行（点击回主页） -->
                <Button
                    DockPanel.Dock="Left"
                    Height="56"
                    Command="{Binding NavigateToHomeCommand, ElementName=Root}"
                    Style="{StaticResource MaterialDesignFlatButton}"
                    ToolTip="返回主页">
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="Home" Width="24" Height="24" VerticalAlignment="Center" />
                        <TextBlock
                            Margin="12,0,0,0"
                            VerticalAlignment="Center"
                            FontSize="16"
                            FontWeight="Bold"
                            Text="凌隐宝堂"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
                    </StackPanel>
                </Button>
            </DockPanel>

            <!-- Row 1: ListBox 导航 -->
            <ListBox
                Grid.Row="1"
                Margin="0,8"
                ItemsSource="{Binding GroupedNavItems, ElementName=Root}"
                SelectedItem="{Binding SelectedNavItem, ElementName=Root, Mode=TwoWay}"
                ItemTemplate="{StaticResource NavItemTemplate}"
                Style="{StaticResource MaterialDesignListBox}"
                Background="Transparent"
                BorderThickness="0"
                ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                <ListBox.GroupStyle>
                    <GroupStyle HeaderTemplate="{StaticResource NavGroupHeaderTemplate}" />
                </ListBox.GroupStyle>
            </ListBox>

            <!-- Row 2: 用户卡 + 状态条 -->
            <StackPanel Grid.Row="2">

                <!-- 用户卡 -->
                <Button
                    Command="{Binding EditProfileCommand, ElementName=Root}"
                    Style="{StaticResource MaterialDesignFlatButton}"
                    HorizontalContentAlignment="Stretch"
                    Margin="8,0"
                    Height="56"
                    ToolTip="个人资料">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Border
                            Grid.Column="0"
                            Width="32" Height="32"
                            Background="{StaticResource SidebarAvatarBrush}"
                            CornerRadius="16">
                            <TextBlock
                                HorizontalAlignment="Center" VerticalAlignment="Center"
                                FontSize="14" FontWeight="SemiBold"
                                Foreground="White"
                                Text="{Binding CurrentUser.RealName, ElementName=Root, Converter={x:Static converters:Cvt.FirstChar}}" />
                        </Border>
                        <StackPanel
                            Grid.Column="1"
                            Margin="12,0,0,0"
                            VerticalAlignment="Center"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                            <TextBlock
                                FontSize="13" FontWeight="SemiBold"
                                Foreground="{DynamicResource MaterialDesign.Brush.OnSurface}"
                                Text="{Binding CurrentUser.RealName, ElementName=Root}" />
                            <TextBlock
                                FontSize="11"
                                Foreground="{DynamicResource MaterialDesign.Brush.OnSurfaceVariant}"
                                Text="{Binding CurrentUser.Role, ElementName=Root, Converter={x:Static converters:Cvt.EnumDesc}}" />
                        </StackPanel>
                    </Grid>
                </Button>

                <!-- 分隔线 -->
                <Border Height="1" Margin="16,8" Background="{DynamicResource MaterialDesign.Brush.Outline}" />

                <!-- 状态条 -->
                <DockPanel Margin="16,0,16,8">
                    <!-- 退出按钮（始终可见） -->
                    <Button
                        DockPanel.Dock="Right"
                        Command="{Binding LogoutCommand, ElementName=Root}"
                        Style="{StaticResource MaterialDesignIconButton}"
                        ToolTip="退出登录"
                        Width="32" Height="32">
                        <materialDesign:PackIcon Kind="Logout" Foreground="{StaticResource DangerBrush}" />
                    </Button>

                    <!-- 状态信息（展开态完整） -->
                    <StackPanel
                        Orientation="Horizontal"
                        VerticalAlignment="Center"
                        Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                        <Ellipse
                            Width="10" Height="10"
                            Fill="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToColor}}" />
                        <TextBlock
                            Margin="8,0,0,0"
                            FontSize="11"
                            Foreground="{DynamicResource MaterialDesign.Brush.OnSurfaceVariant}"
                            Text="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToText}}" />
                        <TextBlock
                            Margin="6,0,0,0"
                            FontSize="11" FontWeight="Medium"
                            Foreground="{Binding IsRemoteMode, ElementName=Root, Converter={x:Static converters:Cvt.BoolToColor}, ConverterParameter=Green|OrangeRed}"
                            Text="{Binding ConnectionDisplay, ElementName=Root}" />
                    </StackPanel>

                    <!-- 状态信息（折叠态：仅圆点） -->
                    <Ellipse
                        Width="10" Height="10"
                        VerticalAlignment="Center"
                        HorizontalAlignment="Center"
                        Fill="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToColor}}"
                        Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.InverseBoolToVis}}"
                        ToolTip="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToText}}" />
                </DockPanel>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj --nologo`
Expected: 0 errors（若 `PackIcon` 缺失，确保 MaterialDesignThemes 已正确引用）

- [ ] **Step 4: 提交（XAML + code-behind 合并提交）**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs
git commit -m "feat(sidebar): rewrite SidebarControl as M2 ListBox with GroupStyle and fold animation"
```

---

## Task 7: 更新 MainWindow.xaml 绑定

> 注：原 Task 5 和 Task 6 已合并为上面的 Task 5+6。本任务编号保持不变以与 spec 对齐。

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

- [ ] **Step 1: 更新 SidebarControl 绑定**

定位 `<controls:SidebarControl ...>` 元素（约 line 79-93），替换为：

```xaml
<controls:SidebarControl
    Grid.Row="0"
    Grid.RowSpan="2"
    Grid.Column="0"
    ApiStatus="{Binding ApiStatus}"
    ConnectionDisplay="{Binding ConnectionModeDisplay}"
    CurrentTime="{Binding CurrentTime}"
    CurrentUser="{Binding CurrentUser}"
    EditProfileCommand="{Binding EditProfileCommand}"
    GroupedNavItems="{Binding GroupedNavItems}"
    SelectedNavItem="{Binding SelectedNavItem, Mode=TwoWay}"
    IsExpanded="{Binding IsSidebarExpanded}"
    IsRemoteMode="{Binding IsRemoteMode}"
    LogoutCommand="{Binding LogoutCommand}"
    NavigateToHomeCommand="{Binding NavigateToHomeCommand}"
    ToggleCommand="{Binding ToggleSidebarCommand}" />
```

变更点：
- 删除 `HomeNavItems` / `BusinessNavItems` / `AdminNavItems` / `NavigationItemsSource` 绑定
- 新增 `GroupedNavItems` + `SelectedNavItem` 绑定
- `IsDrawerOpen` → `IsSidebarExpanded`
- `ToggleDrawerCommand` → `ToggleSidebarCommand`

- [ ] **Step 2: 更新 Ctrl+M 快捷键绑定**

定位 `<KeyBinding Key="M" .../>`（约 line 33-35），更新 Command 绑定：

```xaml
<KeyBinding
    Key="M"
    Command="{Binding ToggleSidebarCommand}"
    Modifiers="Ctrl" />
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Views/MainWindow.xaml
git commit -m "feat(main): update MainWindow bindings for M2 sidebar (GroupedNavItems + SelectedNavItem + IsSidebarExpanded)"
```

---

## Task 8: 全量构建 + 视觉验证

**Covers:** [S8] [S9] [S10]

**Files:** (无代码改动，仅验证)

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors

- [ ] **Step 2: 运行 Desktop 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~NavigationItemTests|FullyQualifiedName~PureLogic" --nologo`
Expected: 全部通过（排除需 SQL Server 的 EndToEnd 测试）

- [ ] **Step 3: 手工 UI 验证清单**

启动 Desktop 应用，按以下清单逐项验证：

| # | 验证点 | 操作 | 预期 |
|---|--------|------|------|
| 1 | M2 主题生效 | 看按钮、文本框样式 | M2 ripple 效果、圆角、elevation |
| 2 | 侧边栏默认展开 | 登录后看侧边栏 | 220px 展开 |
| 3 | 品牌行 | 看顶部 | 🏠 Home icon + "凌隐宝堂" |
| 4 | 折叠按钮唯一 | 看右上角 | 仅一个 Menu 图标按钮 |
| 5 | ListBox 导航 | 看导航项 | 主页 + 诊疗组 + 管理组 |
| 6 | 选中态 | 点导航项 | 药丸形 Primary 背景高亮 |
| 7 | 折叠动画 | Ctrl+M 或点折叠按钮 | 200ms 平滑动画到 56px |
| 8 | 展开恢复 | 折叠后再点按钮 | 200ms 动画回 220px |
| 9 | 折叠态导航 | 折叠后点图标 | 仍可导航，文字隐藏 |
| 10 | 用户卡 | 点用户卡 | 跳转 AccountSettingsView |
| 11 | 退出按钮 | 点退出 | 触发登出 |
| 12 | 4 角色回归 | 用 admin/doctor/receptionist/sysadmin 登录 | 每角色看到正确的导航分组 |
| 13 | 视觉回归 | 检查登录页、患者列表、医案编辑 | M2 主题下不应破坏布局 |

- [ ] **Step 4: 提交验证记录（如有修复）**

如发现需要修复的问题，单独提交修复。

---

## Spec 覆盖检查

| Spec Section | 覆盖任务 |
|--------------|---------|
| [S1] 背景 | 全部（动机） |
| [S2] 决策 | 全部（约束遵守） |
| [S3] 主题迁移 | Task 1, 2, 3 |
| [S4] 侧边栏布局 | Task 5 |
| [S5] ListBox 数据绑定 | Task 4, 5, 6, 7 |
| [S6] 折叠/展开修复 | Task 5 |
| [S7] 文件改动清单 | 全部（按文件清单执行） |
| [S8] 实施顺序 | Task 1-8 顺序一致 |
| [S9] 风险控制 | Task 8（视觉回归检查） |
| [S10] 测试覆盖 | Task 8（构建 + 单元测试 + 手工 UI） |

所有 spec section 均被至少一个任务覆盖。✓

---

## 附录：M2 源码发现更新（2026-06-22 阅读源码后）

阅读 `MaterialDesignInXamlToolkit` 源码后发现 3 个关键改进。执行时 MUST 应用：

### A1. NavigationItem 模型：IconData → IconKind

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs
// 旧：public Geometry? IconData { get; set; }
// 新：public string IconKind { get; set; } = string.Empty;
```

PackIcon 用字符串 Kind（如 "Home"、"AccountGroup"），不用 Geometry Path data。

### A2. M2 原生导航样式（替代通用 ListBox style）

SidebarControl.xaml 中的 ListBox 用：
- `Style="{StaticResource MaterialDesignNavigationPrimaryListBox}"`（不是 `MaterialDesignListBox`）
  - 源码位置：`MaterialDesignTheme.ListBox.xaml:573`
  - 提供：药丸形选中态、Ripple、CircleEase 过渡、CornerRadius=4

折叠按钮用：
- `Style="{StaticResource MaterialDesignHamburgerToggleButton}"`（不是自定义 Button）
  - 内置汉堡↔X 动画

### A3. PackIcon 替代 Path（图标全部迁移）

```xaml
<!-- 旧 -->
<Path Data="{Binding IconData}" Stretch="Uniform" />

<!-- 新 -->
<materialDesign:PackIcon Kind="{Binding IconKind}" />
```

MainWindowViewModel.BuildNavigationItems 中图标赋值改为：

```csharp
// 旧
IconData = (Geometry)Application.Current.FindResource("IconHome")

// 新
IconKind = "Home"  // PackIcon Kind 字符串
```

常用 Kind 对照（MaterialDesignIcons）：Home, AccountGroup, Pill, Notebook, CalendarClock, Cog, Logout

### A4. 执行影响

以上更新影响 Task 4（VM icon 赋值）、Task 5+6（XAML + 模型），不影响 Task 1-3（主题迁移）和 Task 7-8（绑定 + 验证）。
