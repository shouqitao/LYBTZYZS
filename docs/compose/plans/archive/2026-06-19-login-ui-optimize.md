# 登录界面 UI 优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 基于 ui-ux-pro-max 设计系统优化登录界面，使卡片比例协调且占据适当视觉权重。

**Architecture:** 增大卡片尺寸 + 恢复紧凑标签 + 平衡品牌区视觉重量。单文件改动，无架构变更。

**Tech Stack:** WPF / XAML

---

### Task 1: 优化登录卡片尺寸 + 恢复标签 + 平衡品牌区

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`

- [ ] **Step 1: 增大卡片 MaxWidth + Padding**

```xml
<!-- MaxWidth 500→580, Padding 40,36→48,40 -->
<Border Grid.Column="1" MaxWidth="580" Padding="48,40" HorizontalAlignment="Center" VerticalAlignment="Center"
        Background="#FFFFFF" CornerRadius="12">
```

- [ ] **Step 2: 缩小品牌区文字平衡视觉重量**

```xml
<!-- 标题 72→56, 副标题 36→28, slogan 20→16 -->
<TextBlock Text="凌隐宝堂" FontSize="56" FontWeight="Bold" Foreground="#FFFFFF" Margin="0,0,0,12">
<TextBlock Text="中医诊疗管理系统" FontSize="28" FontWeight="SemiBold" Foreground="#D7CCC8" Margin="0,0,0,24">
<TextBlock Text="传承经典 · 数字化诊疗" FontSize="16" Foreground="#BCAAA4" />
```

- [ ] **Step 3: 恢复紧凑表单标签（UX 规范要求）**

用小字号标签（13px）+ 输入框高度增加到 52px：

```xml
<TextBlock Text="用户名" FontSize="13" Foreground="#999999" Margin="0,0,0,6" />
<TextBox Height="52" Padding="14,0" VerticalContentAlignment="Center"
         FontSize="15" Background="#FAFAFA" BorderBrush="#E0E0E0" BorderThickness="1.5"
         Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,16" />

<TextBlock Text="密码" FontSize="13" Foreground="#999999" Margin="0,0,0,6" />
<PasswordBox x:Name="PasswordBox" Height="52" Padding="14,0" VerticalContentAlignment="Center"
             FontSize="15" Background="#FAFAFA" BorderBrush="#E0E0E0" BorderThickness="1.5"
             behaviors:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
             Margin="0,0,0,16">
```

- [ ] **Step 4: 登录按钮高度微调**

```xml
<!-- 50→54 -->
<Button Height="54" Command="{Binding LoginCommand}" Content="登 录"
```

- [ ] **Step 5: 构建验证 + 提交**

```bash
dotnet build LYBTZYZS.sln --no-restore
git add -A && git commit -m "ui(login): optimize card size + restore labels + balance brand weight"
```

### 优化对照表

| 属性 | 当前 | 优化后 | 理由 |
|------|------|--------|------|
| 卡片 MaxWidth | 500 | **580** | 占屏幕比例从 26%→30% |
| 卡片 Padding | 40,36 | **48,40** | 增加内呼吸感 |
| 品牌标题 | 72px | **56px** | 平衡视觉重量 |
| 品牌副标题 | 36px | **28px** | 同上 |
| 品牌 slogan | 20px | **16px** | 同上 |
| 表单标签 | 无 | **13px 恢复** | UX 规范 HIGH 级要求 |
| 输入框高度 | 48px | **52px** | 配合标签增加可用性 |
| 登录按钮 | 50px | **54px** | 触摸目标 ≥44px 规范 |

**最终比例**: 580 × ~480 ≈ **1.2:1**（标准登录卡片比例）
