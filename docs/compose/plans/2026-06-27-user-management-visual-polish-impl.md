# 用户管理界面视觉细节优化计划

> **For agentic workers:** Use compose:execute to implement this plan.

**Goal:** 优化 UserMasterDetailControl 的视觉细节，统一为 MDIX 资源，改善间距和对齐。

**Architecture:** 替换 DataGrid 列样式中的旧画刷、添加 MDIX 控件样式、规范化间距。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit

## Global Constraints

- MDIX 优先：用 MDIX 内置样式
- 颜色用 DynamicResource
- 不改变功能逻辑，只改视觉

---

## Task 1: DataGrid 列样式 MDIX 化

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: 替换 DataGrid 列中的旧画刷**

用户名列（第 168-175 行）：
```xml
<!-- 修改前 -->
<Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}" />

<!-- 修改后 -->
<Setter Property="Foreground" Value="{DynamicResource MaterialDesign.Brush.Foreground}" />
```

姓名列（第 184-188 行）：同上替换。

角色列（第 197-200 行）：
```xml
<!-- 修改前 -->
<Setter Property="Foreground" Value="{DynamicResource SecondaryTextBrush}" />

<!-- 修改后 -->
<Setter Property="Foreground" Value="{DynamicResource MaterialDesign.Brush.Foreground}" />
<Setter Property="Opacity" Value="0.7" />
```

手机号列（第 218-225 行）：
```xml
<!-- 修改前 -->
<Setter Property="Foreground" Value="{DynamicResource SecondaryTextBrush}" />
<Setter Property="FontSize" Value="11" />

<!-- 修改后 -->
<Setter Property="Foreground" Value="{DynamicResource MaterialDesign.Brush.Foreground}" />
<Setter Property="Opacity" Value="0.7" />
<Setter Property="FontSize" Value="{StaticResource FontSizeXS}" />
```

- [ ] **Step 2: 替换右键菜单中的旧画刷**

第 261 行：
```xml
<!-- 修改前 -->
Foreground="{DynamicResource ErrorBrush}"

<!-- 修改后 -->
Foreground="{DynamicResource MaterialDesign.Brush.Error}"
```

- [ ] **Step 3: 替换分页区域中的旧画刷**

第 293 行和第 305 行：
```xml
<!-- 修改前 -->
Foreground="{DynamicResource SecondaryTextBrush}"

<!-- 修改后 -->
Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
Opacity="0.7"
```

注意：Opacity 需要加到 TextBlock 上，不能加到父容器上。

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 3`
Expected: 0 errors

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "fix: replace custom brushes with MDIX resources in UserMasterDetailControl DataGrid columns"
```

---

## Task 2: 添加 MDIX 控件样式

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: ComboBox 添加 MDIX 样式**

角色筛选 ComboBox（第 113-125 行）：
```xml
<!-- 添加 Style -->
<ComboBox
    Width="120"
    Margin="0,0,8,0"
    Style="{StaticResource MaterialDesignOutlinedComboBox}"
    IsEditable="False"
    ItemsSource="{Binding RoleOptions}"
    SelectedItem="{Binding SelectedRoleFilter}" />
```

状态筛选 ComboBox（第 126-138 行）：同样添加 `Style="{StaticResource MaterialDesignOutlinedComboBox}"`。

分页 ComboBox（第 295-301 行）：
```xml
<ComboBox
    Width="70"
    Margin="6,0"
    Style="{StaticResource MaterialDesignOutlinedComboBox}"
    VerticalAlignment="Center"
    IsEditable="False"
    ItemsSource="{Binding PageSizes}"
    SelectedItem="{Binding PageSize, Mode=TwoWay}" />
```

- [ ] **Step 2: CheckBox 添加 MDIX 样式**

第 139-143 行：
```xml
<CheckBox
    Margin="0,0,8,0"
    VerticalAlignment="Center"
    Style="{StaticResource MaterialDesignCheckBox}"
    Content="显示已禁用"
    IsChecked="{Binding ShowInactiveUsers}" />
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 3`
Expected: 0 errors

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "feat: add MDIX styles to ComboBox and CheckBox in UserMasterDetailControl"
```

---

## Task 3: 改善间距和分隔线

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: 工具栏按钮间距加宽**

所有按钮 `Margin="4,0,0,0"` → `Margin="8,0,0,0"`（第 52, 58, 64, 70, 79, 85 行）

- [ ] **Step 2: 替换 Separator 为 MDIX 风格分隔线**

第 76 行：
```xml
<!-- 修改前 -->
<Separator Margin="8,0" Style="{DynamicResource {x:Static ToolBar.SeparatorStyleKey}}" />

<!-- 修改后 -->
<Border Width="1" Margin="12,4" 
        Background="{DynamicResource MaterialDesign.Brush.Outline}" 
        Opacity="0.3" />
```

- [ ] **Step 3: 搜索区域间距规范化**

第 95 行：
```xml
<!-- 修改前 -->
<Grid Grid.Row="1" Margin="12,8">

<!-- 修改后 -->
<Grid Grid.Row="1" Margin="16,8">
```

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 3`
Expected: 0 errors

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "fix: improve spacing and dividers in UserMasterDetailControl"
```
