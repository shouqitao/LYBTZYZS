# 启动画面到登录界面 UI 重构 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 统一 SplashScreenWindow 和 LoginView 的视觉语言，从启动到登录形成连贯的品牌体验。

**Architecture:** Splash 改用与登录相同的深色背景 + 隶书"大医精诚" + 硬编码配色（不依赖 StaticResource）。保持 600×400 固定窗口尺寸。加入淡出过渡。

**Tech Stack:** WPF / XAML

---

### Task 1: 重写 SplashScreenWindow.xaml

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/SplashScreenWindow.xaml`

- [ ] **Step 1: 完整重写 XAML**

替换为深色背景版本，与 LoginView 视觉统一：

```xml
<Window x:Class="LYBT.Desktop.Shell.Views.SplashScreenWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="凌隐宝堂中医诊所"
        Height="420" Width="640"
        WindowStyle="None"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        AllowsTransparency="True"
        Background="Transparent">

    <Border CornerRadius="12" ClipToBounds="True">
        <Border.Background>
            <ImageBrush ImageSource="pack://application:,,,/LYBT.Desktop.Shell;component/Assets/Images/Backgrounds/img-login-background.jpg"
                        Stretch="UniformToFill" />
        </Border.Background>

        <Grid>
            <Border Background="#60000000" />

            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="大医精诚"
                           FontFamily="pack://application:,,,/LYBT.Desktop.Shell;component/Assets/Fonts/#LiSu"
                           FontSize="64" FontWeight="Bold"
                           Foreground="#F5E6C8" HorizontalAlignment="Center" Margin="0,0,0,16">
                    <TextBlock.Effect>
                        <DropShadowEffect BlurRadius="12" Opacity="0.4" ShadowDepth="3" Color="Black" />
                    </TextBlock.Effect>
                </TextBlock>

                <TextBlock Text="凌隐宝堂中医诊所"
                           FontSize="20" Foreground="#D7CCC8" HorizontalAlignment="Center" Margin="0,0,0,40" />
            </StackPanel>

            <StackPanel VerticalAlignment="Bottom" Margin="0,0,0,40">
                <TextBlock x:Name="StatusText"
                           Text="正在初始化系统..."
                           FontSize="14" Foreground="#BCAAA4" HorizontalAlignment="Center" Margin="0,0,0,12" />

                <ProgressBar x:Name="ProgressBar"
                             Width="360" Height="4"
                             IsIndeterminate="True" HorizontalAlignment="Center">
                    <ProgressBar.Foreground>
                        <SolidColorBrush Color="#8D6E63" />
                    </ProgressBar.Foreground>
                    <ProgressBar.Background>
                        <SolidColorBrush Color="#33FFFFFF" />
                    </ProgressBar.Background>
                </ProgressBar>
            </StackPanel>

            <TextBlock Text="© 2026 凌隐宝堂 · 版本 1.0.0"
                       FontSize="11" Foreground="#80BCAAA4"
                       HorizontalAlignment="Center" VerticalAlignment="Bottom" Margin="0,0,0,16" />
        </Grid>
    </Border>
</Window>
```

关键改动：
- 深色背景图（与登录相同）+ #60000000 遮罩
- 隶书"大医精诚" 64px + 暖金色 #F5E6C8（与登录一致）
- "凌隐宝堂中医诊所"副标题
- 进度条用棕色 #8D6E63（与 DesignSystem PrimaryBrush 一致）
- 所有颜色硬编码（不依赖 StaticResource）
- 删除 Canvas 十字图标
- 尺寸 600×400 → 640×420（更协调的比例）
- 版权更新为 2026

- [ ] **Step 2: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "ui(splash): rewrite with dark background + 隶书 + unified branding"
```

### Task 2: 添加 Splash→Login 淡出过渡

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/SplashScreenWindow.xaml` — 添加淡出动画
- Modify: `src/Client/Desktop/Shell/App.xaml.cs` — 延迟关闭 Splash 让淡出完成

- [ ] **Step 1: SplashScreenWindow 添加淡出 Storyboard**

在 Window.Resources 中添加：
```xml
<Window.Resources>
    <Storyboard x:Key="FadeOutStoryboard">
        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                         From="1" To="0" Duration="0:0:0.5" />
    </Storyboard>
</Window.Resources>
```

在 code-behind 中添加 `public void FadeOut()` 方法，调用 BeginStoryboard。

- [ ] **Step 2: App.xaml.cs 修改 Splash 关闭逻辑**

在 `ShowMainWindowAfterInitializationAsync` 方法中，将 `_splashScreen.Close()` 改为先淡出再关闭：
```csharp
if (_splashScreen != null)
{
    _splashScreen.FadeOut();
    await Task.Delay(500); // 等淡出完成
    _splashScreen.Close();
}
```

- [ ] **Step 3: 构建验证 + Commit**
