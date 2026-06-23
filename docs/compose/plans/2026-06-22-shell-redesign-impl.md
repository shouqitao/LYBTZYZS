# Shell 重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重构登录后 Shell 为左侧可折叠导航栏 + 内容区 + 底部状态栏布局

**Architecture:** 使用 Grid 双列布局实现持久化左侧边栏（非 DrawerHost），通过绑定控制宽度切换折叠/展开状态。底部状态栏显示连接状态和时间。

**Tech Stack:** MaterialDesignInXaml 5.3.2, Prism.Wpf 9.x, CommunityToolkit.Mvvm 8.x, .NET 8

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| MODIFY | `Shell/Views/MainWindow.xaml` | 完整重写：Grid 双列布局 + 可折叠侧边栏 + 状态栏 |
| MODIFY | `Shell/ViewModels/MainWindowViewModel.cs` | 添加 SidebarWidth、IsSidebarExpanded、ToggleSidebar、NavTextVisibility |
| MODIFY | `Shell/App.xaml` | 更新配色（如需调整） |
| KEEP | `Shell/Services/ThemeService.cs` | 已创建，不变 |
| KEEP | `Shell/Services/DialogHostService.cs` | 已创建，不变 |
| KEEP | `Shell/Services/SnackbarService.cs` | 已创建，不变 |
| KEEP | `Shell/App.xaml.cs` | 服务注册已添加，不变 |

---

## Task 1: 更新 MainWindowViewModel

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: 添加侧边栏属性**

```csharp
/// <summary>
/// 侧边栏宽度：60=折叠，280=展开
/// </summary>
[ObservableProperty]
private double _sidebarWidth = 60;

/// <summary>
/// 侧边栏是否展开
/// </summary>
[ObservableProperty]
private bool _isSidebarExpanded = false;

/// <summary>
/// 导航文字可见性（折叠时隐藏，展开时显示）
/// </summary>
public Visibility NavTextVisibility =>
    IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
```

- [ ] **Step 2: 添加 ToggleSidebar 命令**

替换或重命名现有的 `ToggleDrawer`：

```csharp
[RelayCommand]
private void ToggleSidebar()
{
    SidebarWidth = IsSidebarExpanded ? 60 : 280;
    IsSidebarExpanded = !IsSidebarExpanded;
}
```

- [ ] **Step 3: 更新 OnSelectedNavItemChanged**

导航后不关闭侧边栏（因为侧边栏是持久化的，不是抽屉）：

```csharp
partial void OnSelectedNavItemChanged(NavigationItem? value)
{
    if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
    {
        _navigationCoordinator.NavigateTo(viewName);
        // 侧边栏是持久化的，导航后不关闭
    }
}
```

- [ ] **Step 4: 添加 NotifyPropertyChangedFor**

更新 `IsSidebarExpanded` 属性：

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(NavTextVisibility))]
private bool _isSidebarExpanded = false;
```

- [ ] **Step 5: 添加 Status 栏属性**

```csharp
/// <summary>
/// 当前时间（实时更新）
/// </summary>
[ObservableProperty]
private string _currentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
```

- [ ] **Step 6: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 7: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat(shell): add sidebar collapse/expand and status bar properties"
```

---

## Task 2: 重写 MainWindow.xaml

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

- [ ] **Step 1: 替换整个 MainWindow.xaml**

```xml
<Window
    x:Class="LYBT.Desktop.Shell.Views.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
    xmlns:prism="http://prismlibrary.com/"
    xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters;assembly=LYBT.Desktop.Controls"
    Style="{StaticResource MaterialDesignWindow}"
    Title="{Binding Title}"
    MinWidth="1024" MinHeight="768"
    WindowState="Maximized"
    prism:ViewModelLocator.AutoWireViewModel="True">

    <Window.Resources>
        <converters:Cvt x:Key="Cvt" />
    </Window.Resources>

    <Window.InputBindings>
        <KeyBinding Key="N" Command="{Binding QuickAddPatientCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="C" Command="{Binding QuickStartMedicalCaseCommand}" Modifiers="Ctrl+Shift" />
        <KeyBinding Key="F1" Command="{Binding ShowHelpCommand}" />
        <KeyBinding Key="OemComma" Command="{Binding ShowSettingsCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="M" Command="{Binding ToggleSidebarCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="Left" Command="{Binding NavigateBackCommand}" Modifiers="Alt" />
        <KeyBinding Key="Right" Command="{Binding NavigateForwardCommand}" Modifiers="Alt" />
        <KeyBinding Key="Home" Command="{Binding NavigateToHomeCommand}" Modifiers="Alt" />
    </Window.InputBindings>

    <Grid>
        <!-- Login (unchanged) -->
        <Grid Visibility="{Binding IsNotLoggedIn, Converter={x:Static converters:Cvt.BoolToVis}}">
            <ContentControl prism:RegionManager.RegionName="LoginRegion" />
        </Grid>

        <!-- Post-login Shell -->
        <Grid Visibility="{Binding IsLoggedIn, Converter={x:Static converters:Cvt.BoolToVis}}">
            <materialDesign:DialogHost
                DialogTheme="Inherit"
                Identifier="RootDialog"
                SnackbarMessageQueue="{Binding ElementName=MainSnackbar, Path=MessageQueue}">

                <materialDesign:SnackbarHost Name="MainSnackbar">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <!-- 左侧边栏 -->
                            <ColumnDefinition Width="{Binding SidebarWidth}" />
                            <!-- 右侧内容 -->
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <!-- 内容区 -->
                            <RowDefinition Height="*" />
                            <!-- 状态栏 -->
                            <RowDefinition Height="32" />
                        </Grid.RowDefinitions>

                        <!-- ===== 左侧边栏 ===== -->
                        <Border Grid.Column="0" Grid.RowSpan="2"
                                Background="{DynamicResource MaterialDesign.Brush.Primary}"
                                Width="{Binding SidebarWidth}">
                            <DockPanel LastChildFill="False">

                                <!-- 顶部：Logo + 汉堡按钮 -->
                                <DockPanel DockPanel.Dock="Top" LastChildFill="False">
                                    <ToggleButton
                                        DockPanel.Dock="Right"
                                        IsChecked="{Binding IsSidebarExpanded}"
                                        Command="{Binding ToggleSidebarCommand}"
                                        Style="{StaticResource MaterialDesignHamburgerToggleButton}"
                                        Foreground="White"
                                        Margin="0,8,8,0" />
                                    <StackPanel Orientation="Horizontal" Margin="12,12,0,12"
                                                HorizontalAlignment="Center">
                                        <materialDesign:PackIcon Kind="Leaf" Width="24" Height="24"
                                                                 Foreground="White" />
                                        <TextBlock Margin="8,0,0,0" FontSize="14" FontWeight="Bold"
                                                   Foreground="White"
                                                   Text="凌隐宝堂"
                                                   Visibility="{Binding DataContext.NavTextVisibility,
                                                               RelativeSource={RelativeSource AncestorType=Window}}" />
                                    </StackPanel>
                                </DockPanel>

                                <!-- 分隔线 -->
                                <Separator DockPanel.Dock="Top"
                                           Background="{DynamicResource MaterialDesign.Brush.Primary.Light}" />

                                <!-- 导航列表 -->
                                <ListBox
                                    ItemsSource="{Binding NavigationItems}"
                                    SelectedItem="{Binding SelectedNavItem, Mode=TwoWay}"
                                    Background="Transparent"
                                    BorderThickness="0"
                                    Margin="0,8">
                                    <ListBox.ItemTemplate>
                                        <DataTemplate>
                                            <StackPanel Orientation="Horizontal"
                                                        Margin="{Binding DataContext.IsSidebarExpanded,
                                                            RelativeSource={RelativeSource AncestorType=Window},
                                                            Converter={x:Static converters:Cvt.BoolToMargin}}"
                                                        HorizontalAlignment="Center">
                                                <materialDesign:PackIcon Kind="{Binding IconKind}"
                                                                         Width="22" Height="22"
                                                                         Foreground="White"
                                                                         VerticalAlignment="Center"
                                                                         Margin="0,8" />
                                                <TextBlock Margin="12,0,0,0" FontSize="14"
                                                           Foreground="White"
                                                           VerticalAlignment="Center"
                                                           Text="{Binding Title}"
                                                           Visibility="{Binding DataContext.NavTextVisibility,
                                                                       RelativeSource={RelativeSource AncestorType=Window}}" />
                                            </StackPanel>
                                        </DataTemplate>
                                    </ListBox.ItemTemplate>
                                </ListBox>

                                <!-- 弹性空间 -->
                                <Border DockPanel.Dock="Bottom" />

                                <!-- 底部：用户卡 -->
                                <StackPanel DockPanel.Dock="Bottom" Margin="0,0,0,8">
                                    <!-- 分隔线 -->
                                    <Separator Margin="8,0"
                                               Background="{DynamicResource MaterialDesign.Brush.Primary.Light}" />

                                    <!-- 用户信息 -->
                                    <StackPanel Orientation="Horizontal" Margin="12,8"
                                                HorizontalAlignment="Center">
                                        <Border Width="32" Height="32" CornerRadius="16"
                                                Background="{StaticResource SidebarAvatarBrush}">
                                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center"
                                                       FontSize="12" FontWeight="Bold" Foreground="White"
                                                       Text="{Binding CurrentUserInitial}" />
                                        </Border>
                                        <StackPanel Margin="8,0,0,0" VerticalAlignment="Center"
                                                    Visibility="{Binding DataContext.NavTextVisibility,
                                                                RelativeSource={RelativeSource AncestorType=Window}}">
                                            <TextBlock FontSize="12" FontWeight="SemiBold"
                                                       Foreground="White"
                                                       Text="{Binding CurrentUserDisplayName}" />
                                            <TextBlock FontSize="10" Foreground="White" Opacity="0.7"
                                                       Text="{Binding CurrentUserRoleDisplay}" />
                                        </StackPanel>
                                    </StackPanel>

                                    <!-- 退出按钮 -->
                                    <Button Command="{Binding LogoutCommand}"
                                            Style="{StaticResource MaterialDesignFlatButton}"
                                            HorizontalAlignment="Center"
                                            Foreground="White"
                                            Padding="8,4">
                                        <StackPanel Orientation="Horizontal">
                                            <materialDesign:PackIcon Kind="Logout" Width="18" Height="18" />
                                            <TextBlock Margin="8,0,0,0" Text="退出"
                                                       Visibility="{Binding DataContext.NavTextVisibility,
                                                                   RelativeSource={RelativeSource AncestorType=Window}}" />
                                        </StackPanel>
                                    </Button>
                                </StackPanel>

                            </DockPanel>
                        </Border>

                        <!-- ===== 内容区域 ===== -->
                        <Border Grid.Column="1" Grid.Row="0" Margin="4"
                                Background="{DynamicResource MaterialDesign.Brush.Surface}"
                                CornerRadius="4">
                            <ContentControl prism:RegionManager.RegionName="ContentRegion" />
                        </Border>

                        <!-- ===== 底部状态栏 ===== -->
                        <Border Grid.Column="1" Grid.Row="1"
                                Background="{DynamicResource MaterialDesign.Brush.Surface}"
                                BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"
                                BorderThickness="0,1,0,0">
                            <DockPanel Margin="12,0">
                                <!-- 左侧：连接状态 -->
                                <StackPanel DockPanel.Dock="Left" Orientation="Horizontal"
                                            VerticalAlignment="Center">
                                    <materialDesign:PackIcon
                                        Kind="{Binding ApiStatusIcon}"
                                        Foreground="{Binding ApiStatusColor}"
                                        Width="14" Height="14" />
                                    <TextBlock Margin="6,0,0,0" FontSize="11"
                                               Text="{Binding ConnectionModeDisplay}" />
                                </StackPanel>

                                <!-- 右侧：时间 -->
                                <TextBlock DockPanel.Dock="Right"
                                           HorizontalAlignment="Right"
                                           VerticalAlignment="Center"
                                           FontSize="11"
                                           Text="{Binding CurrentTimeDisplay}" />
                            </DockPanel>
                        </Border>

                    </Grid>
                </materialDesign:SnackbarHost>
            </materialDesign:DialogHost>
        </Grid>
    </Grid>

</Window>
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/Views/MainWindow.xaml
git commit -m "feat(shell): rewrite MainWindow.xaml with collapsible left sidebar layout"
```

---

## Task 3: 添加 BoolToMargin 转换器

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/Cvt.cs`

- [ ] **Step 1: 添加转换器**

在 `Cvt` 类中添加：

```csharp
/// <summary>
/// bool → Margin：true=12,8,0,8（展开），false=0,8,0,8（折叠居中）
/// </summary>
public static readonly IValueConverter BoolToMargin =
    new FuncValueConverter<bool, Thickness>(b =>
        b ? new Thickness(12, 8, 0, 8) : new Thickness(0, 8, 0, 8));
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/Cvt.cs
git commit -m "feat(shell): add BoolToMargin converter for sidebar layout"
```

---

## Task 4: 更新时钟显示

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: 更新时钟 Tick 处理**

在 `OnTick` 方法中更新 `CurrentTimeDisplay`：

```csharp
private void OnTick(object? sender, ApplicationTickEventArgs e)
{
    Services.UiThreadDispatcher.InvokeAsync(() =>
    {
        CurrentTime = DateTime.Now;
        CurrentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    });
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat(shell): update clock tick to update status bar time display"
```

---

## Task 5: 最终验证

**Files:** None (verification only)

- [ ] **Step 1: 全解决方案构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: PASS

- [ ] **Step 2: 运行桌面测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: PASS（预存环境问题除外）

- [ ] **Step 3: 手动冒烟测试**

启动桌面应用，验证：
1. 登录界面保持不变
2. 登录后显示左侧可折叠导航栏
3. 汉堡按钮点击切换折叠/展开
4. 折叠时只显示图标
5. 展开时显示图标+文字
6. 导航项点击正常切换内容
7. 底部状态栏显示连接状态和时间
8. 深色模式切换正常
9. 退出登录返回登录界面

- [ ] **Step 4: 最终提交**

```bash
git add -A
git commit -m "feat(shell): complete Shell redesign with collapsible left sidebar"
```
