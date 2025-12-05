# Design: POC - 隐藏式Drawer导航布局

## Context

本POC验证方案D：使用隐藏式左侧Drawer导航替代顶部Header，实现工作区空间最大化。

### 空间效率对比

| 方案 | Shell层固定占用 | 空间效率 |
|-----|----------------|---------|
| 当前（上下布局） | 140px | 73.5% |
| 方案E（紧凑优化） | 96px | 77.6% |
| **方案D（Drawer）** | **0px** | **100%** |

### 目标布局

```
默认状态（Drawer隐藏）:
+------------------------------------------+
| [汉堡]                                    |  汉堡按钮悬浮在左上角
|                                          |
|                                          |
|           工作区占满整个窗口               |  100%空间
|                                          |
|                                          |
+------------------------------------------+

Drawer展开状态:
+--------+-------------------------------+
|        |                               |
| Logo   |                               |
| 时间   |     内容区（半透明遮罩）        |
| API    |     点击遮罩关闭Drawer         |
| 用户   |                               |
| 退出   |                               |
|        |                               |
+--------+-------------------------------+
  240px            (遮罩覆盖)
```

## Goals / Non-Goals

### Goals
- 验证WPF中Drawer导航的技术可行性
- 实现流畅的滑入/滑出动画
- 保留所有Header功能（时间、API状态、用户菜单、退出）
- 评估实际用户体验

### Non-Goals
- 不修改任何业务逻辑
- 不改变导航路由结构
- 不作为正式发布版本

## Decisions

### D1: Drawer实现方案

**决策**: 使用WPF原生控件 + Storyboard动画实现，不引入额外依赖。

**实现方式**:
```xml
<Grid>
    <!-- 主内容区 -->
    <ContentControl x:Name="ContentRegion" />

    <!-- 遮罩层（Drawer展开时显示） -->
    <Border x:Name="Overlay"
            Background="#80000000"
            Visibility="Collapsed"
            MouseLeftButtonDown="CloseDrawer" />

    <!-- Drawer面板 -->
    <Border x:Name="DrawerPanel"
            Width="240"
            HorizontalAlignment="Left"
            Background="White">
        <Border.RenderTransform>
            <TranslateTransform x:Name="DrawerTransform" X="-240" />
        </Border.RenderTransform>
        <!-- Drawer内容 -->
    </Border>

    <!-- 汉堡按钮（始终可见） -->
    <Button x:Name="HamburgerButton"
            HorizontalAlignment="Left"
            VerticalAlignment="Top"
            Click="ToggleDrawer" />
</Grid>
```

**替代方案**:
- MaterialDesignInXAML NavigationDrawer：功能完善，但增加依赖
- Syncfusion NavigationDrawer：商业组件，需授权
- 自定义UserControl：可控性强，本POC采用此方案

### D2: 动画参数

**决策**: 使用CubicEase缓动函数，300ms动画时长。

| 参数 | 值 |
|-----|-----|
| 动画时长 | 300ms |
| 缓动函数 | CubicEase (EaseOut) |
| Drawer宽度 | 240px |
| 遮罩透明度 | 50% (#80000000) |

### D3: 汉堡按钮设计

**决策**: 使用MaterialDesign Menu图标，固定在左上角。

**样式参数**:
| 参数 | 值 |
|-----|-----|
| 按钮大小 | 48x48px |
| 图标大小 | 24px |
| 位置 | 左上角，Margin="8" |
| 背景 | 透明（hover时显示） |
| 图标 | MaterialDesign Menu |

### D4: Drawer内部布局

```
+------------------+
|     [Logo]       |  48px
|  凌隐宝堂中医诊所  |
+------------------+
|                  |
|  当前时间 14:30  |  40px
|                  |
+------------------+
|  API状态: 正常   |  40px
+------------------+
|                  |
|  [用户头像]      |
|  张医生          |  80px
|  角色: 医生      |
|                  |
+------------------+
|  修改个人信息 >  |  48px
+------------------+
|  修改密码 >      |  48px
+------------------+
|                  |
|     [退出登录]   |  底部固定
+------------------+
```

## Risks / Trade-offs

### R1: 导航可发现性降低
- **风险**: 新用户可能不知道点击汉堡按钮
- **缓解**: 首次登录显示引导提示；汉堡按钮hover时显示tooltip

### R2: 额外点击成本
- **风险**: 每次查看时间/状态需要打开Drawer
- **缓解**: 考虑在内容区右上角保留小型状态指示器（可选）

### R3: 医疗用户接受度
- **风险**: 医疗系统用户习惯固定可见导航
- **缓解**: 这是POC，通过实际测试评估用户反馈

## Technical Implementation

### ViewModel扩展

```csharp
// MainWindowViewModel.cs 新增
public bool IsDrawerOpen
{
    get => _isDrawerOpen;
    set => SetProperty(ref _isDrawerOpen, value);
}

public ICommand ToggleDrawerCommand { get; }
public ICommand CloseDrawerCommand { get; }
```

### 动画Storyboard

```xml
<Storyboard x:Key="OpenDrawerStoryboard">
    <DoubleAnimation Storyboard.TargetName="DrawerTransform"
                     Storyboard.TargetProperty="X"
                     To="0" Duration="0:0:0.3">
        <DoubleAnimation.EasingFunction>
            <CubicEase EasingMode="EaseOut" />
        </DoubleAnimation.EasingFunction>
    </DoubleAnimation>
    <ObjectAnimationUsingKeyFrames Storyboard.TargetName="Overlay"
                                   Storyboard.TargetProperty="Visibility">
        <DiscreteObjectKeyFrame KeyTime="0" Value="{x:Static Visibility.Visible}" />
    </ObjectAnimationUsingKeyFrames>
</Storyboard>

<Storyboard x:Key="CloseDrawerStoryboard">
    <DoubleAnimation Storyboard.TargetName="DrawerTransform"
                     Storyboard.TargetProperty="X"
                     To="-240" Duration="0:0:0.3">
        <DoubleAnimation.EasingFunction>
            <CubicEase EasingMode="EaseIn" />
        </DoubleAnimation.EasingFunction>
    </DoubleAnimation>
    <ObjectAnimationUsingKeyFrames Storyboard.TargetName="Overlay"
                                   Storyboard.TargetProperty="Visibility">
        <DiscreteObjectKeyFrame KeyTime="0:0:0.3" Value="{x:Static Visibility.Collapsed}" />
    </ObjectAnimationUsingKeyFrames>
</Storyboard>
```

### 快捷键支持

```xml
<Window.InputBindings>
    <!-- Ctrl+M: 切换Drawer -->
    <KeyBinding Key="M" Modifiers="Ctrl" Command="{Binding ToggleDrawerCommand}" />
    <!-- Escape: 关闭Drawer -->
    <KeyBinding Key="Escape" Command="{Binding CloseDrawerCommand}" />
</Window.InputBindings>
```

## Open Questions

1. 是否需要在Drawer关闭时保留小型状态指示器（时间/API状态）？
2. Drawer展开时是否需要键盘焦点陷阱（accessibility）？
3. 是否需要支持手势滑动（触摸屏场景）？
