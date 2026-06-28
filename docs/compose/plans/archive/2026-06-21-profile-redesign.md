# Profile 页面重设计 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Completely rewrite the profile page with left-right split layout, make sidebar avatar clickable, unify password validation.

**Architecture:** Rewrite AccountSettingsControl.xaml + ViewModel. Add Button wrapper around sidebar avatar. Fix ChangePasswordRequest DTO MinimumLength 6→8.

**Tech Stack:** WPF + Prism + CommunityToolkit.Mvvm + HandyControl

---

## File Structure

```
Modified:
  src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml     ← 完全重写 (558→~200行)
  src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs   ← 重写 (382→~200行)
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml ← 头像区域加Button
  src/Shared/LYBT.Shared.Models/Contracts/Auth/ChangePasswordRequest.cs ← MinimumLength 6→8
```

---

## Task 1: 修复密码验证不一致

**Covers:** [S6]
**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/Contracts/Auth/ChangePasswordRequest.cs`

- [ ] **Step 1: Change MinimumLength from 6 to 8**

Find `[MinLength(6` in ChangePasswordRequest.cs and change to `[MinLength(8`.

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "fix(auth): unify password validation — ChangePasswordRequest MinimumLength 6→8"
```

---

## Task 2: 重写 AccountSettingsControl.xaml

**Covers:** [S3, S4, S5, S6]

**Files:**
- Modify: `src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml`

- [ ] **Step 1: Replace entire file with new left-right split layout**

New XAML structure (~200 lines):
```xml
<UserControl ...>
    <UserControl.Resources>
        <!-- Nav item style -->
        <Style x:Key="ProfileNavItemStyle" TargetType="RadioButton">
            <Setter Property="Foreground" Value="{DynamicResource SecondaryTextBrush}" />
            <Setter Property="FontSize" Value="14" />
            <Setter Property="Cursor" Value="Hand" />
            <Setter Property="Padding" Value="12,10" />
            <Setter Property="Margin" Value="0,2" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="RadioButton">
                        <Border x:Name="Bd" Background="Transparent" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter />
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsChecked" Value="True">
                                <Setter TargetName="Bd" Property="Background" Value="{DynamicResource LightPrimaryBrush}" />
                                <Setter Property="Foreground" Value="{DynamicResource PrimaryBrush}" />
                                <Setter Property="FontWeight" Value="SemiBold" />
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="Bd" Property="Background" Value="{DynamicResource BorderLightBrush}" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
        <!-- Input label style -->
        <Style x:Key="FieldLabelStyle" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13" />
            <Setter Property="Foreground" Value="{DynamicResource SecondaryTextBrush}" />
            <Setter Property="Margin" Value="0,0,0,6" />
        </Style>
        <!-- Read-only field style -->
        <Style x:Key="ReadOnlyFieldStyle" TargetType="TextBlock">
            <Setter Property="FontSize" Value="14" />
            <Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}" />
            <Setter Property="Margin" Value="0,0,0,16" />
        </Style>
    </UserControl.Resources>

    <Grid Background="{DynamicResource SecondaryRegionBrush}">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="260" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Left panel -->
        <Border Grid.Column="0" Background="White" BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,1,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <!-- Back button -->
                <Button Grid.Row="0" Command="{Binding GoBackCommand}" Content="← 返回"
                        Background="Transparent" BorderThickness="0" Cursor="Hand"
                        HorizontalAlignment="Left" Padding="16,12" FontSize="13"
                        Foreground="{DynamicResource SecondaryTextBrush}" />

                <!-- Avatar + name + role -->
                <StackPanel Grid.Row="1" HorizontalAlignment="Center" Margin="0,8,0,24">
                    <Border Width="64" Height="64" CornerRadius="32"
                            Background="{DynamicResource PrimaryBrush}" HorizontalAlignment="Center">
                        <TextBlock Text="{Binding CurrentUser.RealName, Converter={x:Static converters:Cvt.FirstChar}}"
                                   FontSize="24" FontWeight="Bold" Foreground="White"
                                   HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <TextBlock Text="{Binding CurrentUser.RealName}" FontSize="16" FontWeight="SemiBold"
                               Foreground="{DynamicResource PrimaryTextBrush}"
                               HorizontalAlignment="Center" Margin="0,12,0,4" />
                    <TextBlock Text="{Binding CurrentUser.Role, Converter={x:Static converters:Cvt.EnumDesc}}"
                               FontSize="12" Foreground="{DynamicResource SecondaryTextBrush}"
                               HorizontalAlignment="Center" />
                </StackPanel>

                <!-- Nav items -->
                <StackPanel Grid.Row="2" Margin="12,0">
                    <RadioButton Style="{StaticResource ProfileNavItemStyle}"
                                 Content="个人资料" IsChecked="{Binding IsProfileSelected}" GroupName="ProfileNav" />
                    <RadioButton Style="{StaticResource ProfileNavItemStyle}"
                                 Content="安全设置" IsChecked="{Binding IsPasswordSelected}" GroupName="ProfileNav" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- Right content -->
        <ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto" Padding="32,24">
            <Grid>
                <!-- Profile tab -->
                <StackPanel Visibility="{Binding IsProfileSelected, Converter={x:Static converters:Cvt.BoolToVis}}">
                    <TextBlock Text="个人资料" FontSize="22" FontWeight="Bold"
                               Foreground="{DynamicResource PrimaryTextBrush}" Margin="0,0,0,24" />

                    <TextBlock Text="姓名" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding EditRealName, UpdateSourceTrigger=PropertyChanged}"
                             Height="40" Padding="12,0" FontSize="14"
                             Margin="0,0,0,16" MaxLength="50" />

                    <TextBlock Text="手机号" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding EditPhoneNumber, UpdateSourceTrigger=PropertyChanged}"
                             Height="40" Padding="12,0" FontSize="14"
                             Margin="0,0,0,16" MaxLength="20" />

                    <TextBlock Text="邮箱" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding EditEmail, UpdateSourceTrigger=PropertyChanged}"
                             Height="40" Padding="12,0" FontSize="14"
                             Margin="0,0,0,24" MaxLength="100" />

                    <Border Height="1" Background="{DynamicResource BorderLightBrush}" Margin="0,0,0,16" />

                    <TextBlock Text="用户名" Style="{StaticResource FieldLabelStyle}" />
                    <TextBlock Text="{Binding CurrentUser.UserName}" Style="{StaticResource ReadOnlyFieldStyle}" />

                    <TextBlock Text="角色" Style="{StaticResource FieldLabelStyle}" />
                    <TextBlock Text="{Binding CurrentUser.Role, Converter={x:Static converters:Cvt.EnumDesc}}" Style="{StaticResource ReadOnlyFieldStyle}" />

                    <TextBlock Text="注册时间" Style="{StaticResource FieldLabelStyle}" />
                    <TextBlock Text="{Binding CurrentUser.CreatedAt, StringFormat=yyyy-MM-dd HH:mm}" Style="{StaticResource ReadOnlyFieldStyle}" />

                    <TextBlock Text="最后登录" Style="{StaticResource FieldLabelStyle}" />
                    <TextBlock Text="{Binding CurrentUser.LastLoginTime, StringFormat=yyyy-MM-dd HH:mm}" Style="{StaticResource ReadOnlyFieldStyle}" />

                    <StackPanel Orientation="Horizontal" Margin="0,16,0,0">
                        <Button Content="取消" Command="{Binding GoBackCommand}"
                                Padding="24,10" Margin="0,0,12,0" FontSize="14"
                                Background="{DynamicResource BorderBrush}" Foreground="{DynamicResource PrimaryTextBrush}"
                                BorderThickness="0" Cursor="Hand" />
                        <Button Content="保存修改" Command="{Binding SaveProfileCommand}"
                                Padding="24,10" FontSize="14"
                                Background="{DynamicResource PrimaryBrush}" Foreground="White"
                                BorderThickness="0" Cursor="Hand" />
                    </StackPanel>
                </StackPanel>

                <!-- Password tab -->
                <StackPanel Visibility="{Binding IsPasswordSelected, Converter={x:Static converters:Cvt.BoolToVis}}">
                    <TextBlock Text="安全设置" FontSize="22" FontWeight="Bold"
                               Foreground="{DynamicResource PrimaryTextBrush}" Margin="0,0,0,24" />

                    <TextBlock Text="当前密码" Style="{StaticResource FieldLabelStyle}" />
                    <PasswordBox x:Name="OldPasswordBox" Height="40" Padding="12,0" FontSize="14"
                                 Margin="0,0,0,16"
                                 behaviors:PasswordBoxHelper.BoundPassword="{Binding OldPassword, Mode=TwoWay}" />

                    <TextBlock Text="新密码 (至少8位)" Style="{StaticResource FieldLabelStyle}" />
                    <PasswordBox x:Name="NewPasswordBox" Height="40" Padding="12,0" FontSize="14"
                                 Margin="0,0,0,16"
                                 behaviors:PasswordBoxHelper.BoundPassword="{Binding NewPassword, Mode=TwoWay}" />

                    <TextBlock Text="确认新密码" Style="{StaticResource FieldLabelStyle}" />
                    <PasswordBox x:Name="ConfirmPasswordBox" Height="40" Padding="12,0" FontSize="14"
                                 Margin="0,0,0,24"
                                 behaviors:PasswordBoxHelper.BoundPassword="{Binding ConfirmPassword, Mode=TwoWay}" />

                    <Button Content="确认修改" Command="{Binding ChangePasswordCommand}"
                            Padding="24,10" FontSize="14" HorizontalAlignment="Left"
                            Background="{DynamicResource PrimaryBrush}" Foreground="White"
                            BorderThickness="0" Cursor="Hand" />
                </StackPanel>
            </Grid>
        </ScrollViewer>
    </Grid>
</UserControl>
```

Key points:
- Add `xmlns:converters`, `xmlns:behaviors` namespace declarations
- Left panel: back button, 64px avatar circle, name/role, RadioButton nav
- Right panel: ScrollViewer with profile/password StackPanels toggled by BoolToVis
- Profile tab: editable fields (RealName/Phone/Email) + read-only fields (UserName/Role/CreatedAt/LastLoginTime)
- Password tab: 3 PasswordBoxes with PasswordBoxHelper binding
- Save buttons with TCM brown theme

- [ ] **Step 2: Build + fix errors**

```bash
dotnet build LYBTZYZS.sln
```
Fix any missing xmlns references. The control is in `LYBT.Desktop.Shell` which references `Controls` (for converters) and `Infrastructure` (for behaviors).

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat(profile): rewrite AccountSettingsControl with left-right split layout"
```

---

## Task 3: 重写 AccountSettingsViewModel

**Covers:** [S5, S6]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`

- [ ] **Step 1: Rewrite ViewModel with clean structure**

Key changes from old VM:
- Keep `IsProfileSelected` / `IsPasswordSelected` as `[ObservableProperty]`
- Keep `CurrentUser` (UserDetailDto)
- Change editable field names to `EditRealName` / `EditPhoneNumber` / `EditEmail` (prefix with Edit to avoid confusion with CurrentUser fields)
- Keep `OldPassword` / `NewPassword` / `ConfirmPassword` as `[ObservableProperty]`
- Keep `SaveProfileCommand` — loads from CurrentUser into Edit* fields on init, saves Edit* back via API
- Keep `ChangePasswordCommand` — validates ≥8 chars, calls API, clears on success
- Keep `GoBackCommand` — navigates back
- Remove `IsBusy` / `IsLoading` if not essential (use simple error message display)
- Use `IAuthenticationService.GetCurrentUserAsync()` to load CurrentUser on navigated-to
- Use `IUserApi.ChangeProfileAsync()` and `IUserApi.ChangePasswordAsync()` for API calls
- Show toast on success via `IToastService` if available, otherwise set a status message

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(profile): rewrite AccountSettingsViewModel — clean Edit* fields, unified password validation"
```

---

## Task 4: 侧边栏头像可点击

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

- [ ] **Step 1: Wrap Row 1 user info area in a Button**

In SidebarControl.xaml, change Row 1 from plain `<Border>` to `<Button>` with `Command="{Binding EditProfileCommand, ElementName=Root}"`:

```xml
<!-- Row 1: User info (clickable → profile) -->
<Button Grid.Row="1" Command="{Binding EditProfileCommand, ElementName=Root}"
        Background="Transparent" BorderThickness="0" Cursor="Hand" HorizontalContentAlignment="Stretch"
        ToolTip="个人设置">
    <Border Padding="12,12">
        <!-- ... existing avatar + name + role content ... -->
    </Border>
</Button>
```

Add hover effect via Button.Style trigger (lighten background on mouse over).

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(profile): sidebar avatar clickable — opens account settings"
```

---

## Task 5: 最终验证

**Covers:** [S3, S4, S5, S6, S7, S8]

- [ ] **Step 1: Full build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors

- [ ] **Step 2: Manual verification**

Launch app, login, verify:
1. Click sidebar avatar → profile page opens
2. Left panel shows avatar, name, role, nav items
3. Edit name/phone/email → save → toast
4. Switch to 安全设置 → change password → success
5. Back button returns to previous view
6. Password validation shows ≥8 requirement

- [ ] **Step 3: Commit + Push**

```bash
git add -A && git commit -m "feat(profile): complete profile redesign — verified" && git push origin master
```
