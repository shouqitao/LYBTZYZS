# 侧边栏展开/折叠一致性修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix sidebar expand/collapse inconsistency — every row adapts cleanly between 220px expanded and 56px collapsed states.

**Architecture:** Add DataTrigger on `IsExpanded` to each sidebar row's style/template. Text elements hide when collapsed, icons center when collapsed. Single file change: SidebarControl.xaml.

**Tech Stack:** WPF XAML + DataTrigger + DynamicResource

---

## Task 1: 统一 NavMenuItemStyle 折叠行为

**Covers:** [S4, S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

- [ ] **Step 1: Update NavMenuItemStyle to handle collapsed state**

The NavMenuItemStyle already has a DataTrigger for `IsExpanded=False` that sets `HorizontalContentAlignment=Center`. Add Padding override:

In the NavMenuItemStyle `<Style.Triggers>` section, update the existing DataTrigger to also set Padding=0:

```xml
<DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="False">
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="Padding" Value="0,0" />
</DataTrigger>
```

The ControlTemplate's `ItemBorder` uses `Padding="16,0"` hardcoded — change to `Padding="{TemplateBinding Padding}"` so the trigger can override it:

```xml
<Border x:Name="ItemBorder" Padding="{TemplateBinding Padding}" Background="{TemplateBinding Background}">
```

And add `<Setter Property="Padding" Value="16,0" />` to the base style setters.

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "fix(sidebar): NavMenuItemStyle adapts padding for collapsed state"
```

---

## Task 2: 用户信息区域 (Row 1) 折叠适配

**Covers:** [S3, S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

- [ ] **Step 1: Add collapsed DataTrigger to Row 1 Button style**

The Row 1 Button.Style already has a DataTrigger for `IsExpanded=False` setting `HorizontalContentAlignment=Center`. Verify it exists and works.

Additionally, the avatar Border has `Width="32" Height="32"` hardcoded. Add a DataTrigger to shrink it when collapsed. Since the avatar Border is inside the ContentTemplate (not a named element accessible from Style triggers), wrap the avatar size logic differently:

Change avatar Border from `Width="32" Height="32"` to use a Style with DataTrigger:

```xml
<Border Background="{DynamicResource SidebarAvatarBrush}" CornerRadius="16">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="Width" Value="32" />
            <Setter Property="Height" Value="32" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="False">
                    <Setter Property="Width" Value="28" />
                    <Setter Property="Height" Value="28" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <!-- TextBlock content unchanged -->
</Border>
```

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "fix(sidebar): avatar shrinks to 28px and centers in collapsed state"
```

---

## Task 3: 状态区域 (Row 4) 折叠适配

**Covers:** [S3, S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

- [ ] **Step 1: Verify collapsed state shows only API dot**

Row 4 already has two StackPanels:
- Collapsed: shows API dot + mini clock (`InverseBoolToVis`)
- Expanded: shows API dot+text + connection mode + full clock (`BoolToVis`)

Verify the collapsed StackPanel's `Visibility` uses `Cvt.InverseBoolToVis` and centers content. If the mini clock causes layout issues in 56px width, remove it — only show the API status dot when collapsed.

To simplify: in the collapsed StackPanel, remove the clock TextBlocks, keep only the API status Ellipse (centered).

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "fix(sidebar): collapsed state shows only API status dot, no clock"
```

---

## Task 4: 退出按钮 (Row 6) 折叠适配

**Covers:** [S3, S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

- [ ] **Step 1: Verify logout button centers when collapsed**

Row 6 logout Button uses `NavMenuItemStyle` which already has the DataTrigger for centering. The logout Path icon (`IconLogout`) should always be visible. The "退出登录" TextBlock should have `Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}"`.

Verify:
1. Path icon has no Visibility binding (always shows)
2. TextBlock has BoolToVis binding (hides when collapsed)
3. Button uses NavMenuItemStyle (gets Center trigger automatically)

If any of these are missing, add them.

- [ ] **Step 2: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "fix(sidebar): logout button adapts to collapsed state via NavMenuItemStyle"
```

---

## Task 5: 最终验证

**Covers:** [S2, S3, S4, S5]

- [ ] **Step 1: Full build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors

- [ ] **Step 2: Manual verification**

Launch app, login, verify BOTH states:

**Expanded (220px):**
- ☰ + "凌隐宝堂" visible
- Avatar(32px) + name + role visible
- Nav items: icon + text, left-aligned
- Status: API dot + text + clock visible
- Logout: icon + "退出登录" text

**Collapsed (56px) — click hamburger:**
- ☰ only (centered)
- Avatar(28px) only (centered), click → popup menu
- Nav items: icon only (centered)
- Status: API dot only (centered)
- Logout: icon only (centered)

- [ ] **Step 3: Commit + Push**

```bash
git add -A && git commit -m "fix(sidebar): verified expand/collapse consistency" && git push origin master
```
