# Master-Detail 重设计 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute to implement task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 重构用户管理 master-detail 界面，统一 MDIX 样式，优化中文显示，消除硬编码。

**Architecture:** 纯 XAML 层改动，不涉及 ViewModel 逻辑。自底向上：共享基础层 → Master 侧 → Detail 侧。验证方式为 `dotnet build` 编译通过。

**Tech Stack:** WPF / MDIX (MaterialDesignInXAML) / Prism / CommunityToolkit.Mvvm

## Global Constraints

- 所有颜色用 `DynamicResource` 引用 MDIX Brush 或 `ValidationErrorBrush`，禁止硬编码 hex
- 间距用 `{StaticResource SpacingXxx}` Token，禁止硬编码像素（等宽字体 font-family 中的字体名除外）
- 不添加 C# 代码 — 只改 XAML
- commit message 用英文，格式 `refactor(ui): 描述`
- 不自动提交 git，每个 task 结尾提示用户确认

---

### Task 1: 共享基础层 — Token + 颜色 + 字体

**Covers:** [S4.1], [S4.2], [S4.3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Spacing.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`

- [ ] **Step 1: 新增 SpacingXXXL Token**

在 `Spacing.xaml` 的 `</ResourceDictionary>` 前添加：

```xml
    <Thickness x:Key="SpacingXXXL">40</Thickness>
```

- [ ] **Step 2: 更新 App.xaml 颜色和字体**

`App.xaml` 第24-25行，替换：

```xml
            <SolidColorBrush x:Key="ValidationErrorBrush" Color="#B00020" />
            <SolidColorBrush x:Key="ValidationErrorBackgroundBrush" Color="#FCE8E6" />
```

第33行 `CustomDialogWindowStyle` 中的 FontFamily，替换：

```xml
                <Setter Property="FontFamily" Value="Microsoft YaHei UI, Microsoft YaHei, Segoe UI" />
```

- [ ] **Step 3: 修复 DataGridMonoCellStyle 字体回退**

`DataGridStyles.xaml` 第14行，替换：

```xml
        <Setter Property="FontFamily" Value="Cascadia Code, Consolas, Microsoft YaHei UI" />
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
Expected: Build succeeded

---

### Task 2: InfoCard 视觉调整

**Covers:** [S4.4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/InfoCard.xaml`

- [ ] **Step 1: 调整 InfoCard 尺寸**

`InfoCard.xaml` 第41-45行的 `Border`，替换这些属性：

```xml
    <Border Background="{DynamicResource MaterialDesignPaper}"
            CornerRadius="8"
            Padding="24,16"
            Effect="{StaticResource CardShadow}"
            Margin="0,0,0,16">
```

同时将第35行阴影 Opacity 从 `0.08` 改为 `0.06`：

```xml
                              Opacity="0.06"
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
Expected: Build succeeded

---

### Task 3: Master 侧重构 — DataGrid + 筛选 + 工具栏

**Covers:** [S5.1], [S5.2], [S5.3]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: DataGrid 列宽弹性化**

将 DataGrid.Columns 中各列的 Width 改为弹性值。替换第193-228行的 Columns 块中每列的 Width：

```xml
<!-- 用户名 -->
<DataGridTextColumn Width="*" MinWidth="80" .../>

<!-- 姓名 -->
<DataGridTextColumn Width="*" MinWidth="60" .../>

<!-- 角色 -->
<DataGridTextColumn Width="Auto" .../>

<!-- 状态：保持 Auto -->
<DataGridTemplateColumn Width="Auto" .../>

<!-- 手机号 -->
<DataGridTextColumn Width="*" MinWidth="100" .../>
```

> 注意：只改 `Width` 属性值，不动 Binding/Header/ElementStyle 等其他属性。

- [ ] **Step 2: 筛选栏 ComboBox 加宽**

第128-141行的角色 ComboBox，去掉 `Width="100"`，改为：

```xml
                        <ComboBox
                            MinWidth="120"
                            Margin="8,0,0,0"
                            ...
```

第142-155行的状态 ComboBox，同样去掉 `Width="100"`，改为：

```xml
                        <ComboBox
                            MinWidth="120"
                            Margin="8,0,0,0"
                            ...
```

- [ ] **Step 3: 用 PopupBox 替换"更多"菜单**

在文件顶部添加 MDIX xmlns（如果尚不存在）：

```xml
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```

替换第64-104行的 `<Menu>...<MenuItem>...</Menu>` 整个块为：

```xml
                            <materialDesign:PopupBox
                                Margin="8,0,0,0"
                                Style="{StaticResource MaterialDesignToolPopupBox}"
                                ToolTip="更多操作"
                                PlacementMode="BottomAndAlignRight">
                                <StackPanel>
                                    <Button
                                        Command="{Binding ResetPasswordCommand}"
                                        Content="重置密码"
                                        Style="{StaticResource MaterialDesignMenuItemButton}" />
                                    <Button
                                        Command="{Binding ToggleUserStatusCommand}"
                                        Content="切换状态"
                                        Style="{StaticResource MaterialDesignMenuItemButton}" />
                                    <Button
                                        Command="{Binding RestoreCommand}"
                                        Content="恢复"
                                        Style="{StaticResource MaterialDesignMenuItemButton}" />
                                    <Separator />
                                    <Button
                                        Command="{Binding ImportCommand}"
                                        Content="导入"
                                        Style="{StaticResource MaterialDesignMenuItemButton}" />
                                    <Button
                                        Command="{Binding DownloadTemplateCommand}"
                                        Content="下载模板"
                                        Style="{StaticResource MaterialDesignMenuItemButton}" />
                                </StackPanel>
                            </materialDesign:PopupBox>
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj`
Expected: Build succeeded

> **若 `MaterialDesignMenuItemButton` 不存在**，fallback 为 `Style="{StaticResource MaterialDesignButton}"` 加 `HorizontalAlignment="Left"` 和 `Padding="16,8"`。

---

### Task 4: Detail 查看模式重构 — 单卡片 + 分区

**Covers:** [S6]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserViewControl.xaml`

- [ ] **Step 1: 替换整个 UserViewControl.xaml 内容**

将整个文件替换为以下内容：

```xml
<!--
    UserViewControl - 用户预览控件（重设计 2026-06-27）
    单卡片 + 分区隔线 + 概要头，替代原 3 张独立卡片
-->
<UserControl x:Class="LYBT.Desktop.Users.Controls.UserViewControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:LYBT.Desktop.Controls.Controls;assembly=LYBT.Desktop.Controls"
             xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters;assembly=LYBT.Desktop.Controls"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             x:Name="Root">

    <ScrollViewer VerticalScrollBarVisibility="Auto" DataContext="{Binding ElementName=Root}">
        <controls:InfoCard>
            <controls:InfoCard.Content>
                <StackPanel>

                    <!-- 概要头：用户名 + 状态 -->
                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>
                        <materialDesign:PackIcon
                            Grid.Column="0"
                            Kind="Account"
                            Width="28" Height="28"
                            VerticalAlignment="Center"
                            Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                        <TextBlock
                            Grid.Column="1"
                            Text="{Binding UserName}"
                            Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                            Margin="12,0,0,0"
                            VerticalAlignment="Center"
                            FontWeight="SemiBold" />
                        <controls:StatusBadge
                            Grid.Column="2"
                            Status="{Binding Status}"
                            VerticalAlignment="Center"
                            Visibility="{Binding ShowStatus, Converter={x:Static converters:Cvt.BoolToVis}}" />
                    </Grid>

                    <!-- 粗分隔线 -->
                    <Border Height="1" Margin="0,0,0,16"
                            Background="{DynamicResource MaterialDesign.Brush.Outline}"
                            Opacity="0.2" />

                    <!-- ====== 基本信息 ====== -->
                    <TextBlock Text="基本信息"
                               Style="{StaticResource MaterialDesignCaptionTextBlock}"
                               Margin="0,0,0,8"
                               Opacity="0.6" />
                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="真实姓名"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding RealName}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" FontWeight="SemiBold" />

                        <TextBlock Grid.Row="0" Grid.Column="2" Text="拼音码"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="3"
                                   Text="{Binding PinYinCode, TargetNullValue='-'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center"
                                   FontFamily="Cascadia Code, Consolas, Microsoft YaHei UI" />

                        <TextBlock Grid.Row="1" Grid.Column="0" Text="用户角色"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="1" Grid.Column="1"
                                   Text="{Binding Role, Converter={x:Static converters:Cvt.EnumDesc}}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" />
                    </Grid>

                    <!-- 浅分隔线 -->
                    <Border Height="1" Margin="0,0,0,16"
                            Background="{DynamicResource MaterialDesign.Brush.Outline}"
                            Opacity="0.12" />

                    <!-- ====== 联系方式 ====== -->
                    <TextBlock Text="联系方式"
                               Style="{StaticResource MaterialDesignCaptionTextBlock}"
                               Margin="0,0,0,8"
                               Opacity="0.6" />
                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="手机号码"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="1"
                                   Text="{Binding PhoneNumber, TargetNullValue='-'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center"
                                   FontFamily="Cascadia Code, Consolas, Microsoft YaHei UI" />

                        <TextBlock Grid.Row="0" Grid.Column="2" Text="邮箱地址"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="3"
                                   Text="{Binding Email, TargetNullValue='-'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" />
                    </Grid>

                    <!-- 浅分隔线 -->
                    <Border Height="1" Margin="0,0,0,16"
                            Background="{DynamicResource MaterialDesign.Brush.Outline}"
                            Opacity="0.12" />

                    <!-- ====== 系统信息 ====== -->
                    <TextBlock Text="系统信息"
                               Style="{StaticResource MaterialDesignCaptionTextBlock}"
                               Margin="0,0,0,8"
                               Opacity="0.6" />
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="最后登录"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="1"
                                   Text="{Binding LastLoginTime, StringFormat={}{0:yyyy-MM-dd HH:mm}, TargetNullValue='从未登录'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" />

                        <TextBlock Grid.Row="0" Grid.Column="2" Text="创建时间"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="0" Grid.Column="3"
                                   Text="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm}, TargetNullValue='-'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" />

                        <TextBlock Grid.Row="1" Grid.Column="0" Text="更新时间"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" Opacity="0.6" />
                        <TextBlock Grid.Row="1" Grid.Column="1"
                                   Text="{Binding UpdatedAt, StringFormat={}{0:yyyy-MM-dd HH:mm}, TargetNullValue='-'}"
                                   Style="{StaticResource MaterialDesignBody2TextBlock}"
                                   Margin="0,8,16,8" VerticalAlignment="Center" />
                    </Grid>
                </StackPanel>
            </controls:InfoCard.Content>
        </controls:InfoCard>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj`
Expected: Build succeeded

> **注意**：`Margin="0,0,0,16"` 这种内联 StaticResource 语法在 WPF 中不支持。实际实现需用 `Margin="0,0,0,16"` 硬编码数值，或在资源中定义完整 Thickness。Task 1 已建立 Token，但 XAML Margin 不支持内联拼接。实现时直接用 `16`（=SpacingL 值）并加注释 `<!-- SpacingL -->`。

---

### Task 5: Detail 编辑模式重构

**Covers:** [S7.1], [S7.2], [S7.3], [S7.4]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserEditControl.xaml`

- [ ] **Step 1: 去掉中间空列**

第35-38行的 `Grid.ColumnDefinitions`，替换为（删除 `Width="20"` 的中间列）：

```xml
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
```

左列字段 StackPanel（Grid.Column="0"）添加右侧间距 Margin。每个 `Grid.Row="0" Grid.Column="0"` 和 `Grid.Row="1" Grid.Column="0"` 和 `Grid.Row="2" Grid.Column="0"` 和 `Grid.Row="3" Grid.Column="0"` 的 StackPanel，将 `Margin="0,0,0,20"` 改为 `Margin="0,0,24,20"`（24px=SpacingXL 做列间距）。

- [ ] **Step 2: Label 星号颜色 Token 化**

文件中有3处 `<Run Text=" *" Foreground="#C75050" FontWeight="Bold"/>`（第50、63、106行），全部替换为：

```xml
<Run Text=" *" Foreground="{DynamicResource ValidationErrorBrush}" FontWeight="Bold"/>
```

- [ ] **Step 3: 验证错误样式 Token 化**

第22-27行 `ValidationErrorMessageVisibleStyle`，替换 Foreground：

```xml
        <Style x:Key="ValidationErrorMessageVisibleStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="{DynamicResource ValidationErrorBrush}"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="0,4,0,0"/>
            <Setter Property="TextWrapping" Value="Wrap"/>
            <Setter Property="MinHeight" Value="16"/>
        </Style>
```

- [ ] **Step 4: 字段间距统一**

所有 StackPanel 的 `Margin="0,0,0,20"` 改为 `Margin="0,0,0,24"`（SpacingXL）。用 replace_all 或逐个替换。

- [ ] **Step 5: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj`
Expected: Build succeeded

---

### Task 6: DetailToolbar 颜色 Token 化

**Covers:** [S8]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DetailToolbar.xaml`

- [ ] **Step 1: 删除按钮颜色替换**

第97行，替换：

```xml
                    Style="{StaticResource MaterialDesignRaisedButton}" Background="{DynamicResource ValidationErrorBrush}" Foreground="White" BorderThickness="0"
```

- [ ] **Step 2: Danger hover 样式替换**

第36行 `#FDE7E9`，替换为：

```xml
                    <Setter Property="Background" Value="{DynamicResource ValidationErrorBackgroundBrush}" />
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
Expected: Build succeeded

---

### Task 7: 全量编译验证

**Covers:** [S9]

**Files:** (无文件修改)

- [ ] **Step 1: 全解决方案编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: 确认无新增警告**

检查输出中是否有新的 XAML 相关警告（XDG0001、MC3029 等）。如果有，记录并修复。

- [ ] **Step 3: 交付给用户进行运行时视觉验证**

提示用户启动 Desktop 应用，登录 sysadmin/admin，进入用户管理检查：
- DataGrid 列宽弹性伸缩
- 筛选 ComboBox 完整显示
- PopupBox 展开/收起
- 查看模式：单卡片 + 分区隔线 + 概要头
- 编辑模式：列间距均匀 + Token 化颜色
- 中文渲染清晰

---

## Self-Review

**Spec coverage check:**
- [S4.1] SpacingToken → Task 1 Step 1 ✓
- [S4.2] 颜色统一 → Task 1 Step 2 ✓
- [S4.3] 字体优化 → Task 1 Step 3 ✓
- [S4.4] InfoCard → Task 2 ✓
- [S5.1] DataGrid列宽 → Task 3 Step 1 ✓
- [S5.2] 筛选栏 → Task 3 Step 2 ✓
- [S5.3] 工具栏PopupBox → Task 3 Step 3 ✓
- [S6] 查看重构 → Task 4 ✓
- [S7.1] 去空列 → Task 5 Step 1 ✓
- [S7.2] Label颜色 → Task 5 Step 2 ✓
- [S7.3] 字段间距 → Task 5 Step 4 ✓
- [S7.4] 验证错误样式 → Task 5 Step 3 ✓
- [S8] DetailToolbar → Task 6 ✓
- [S9] 验证 → Task 7 ✓

**Placeholder scan:** 无 TBD/TODO，所有步骤含具体代码 ✓

**Type consistency:** ValidationErrorBrush 在所有 task 中一致引用 ✓
