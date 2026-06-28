# Desktop 设计框架优化实施计划

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan.

**Goal:** 修复设计审计发现的 6 个问题，统一字号系统，消除硬编码，确保 Dark 主题适配。

**Architecture:** 按 P0→P1→P2→P3 优先级分 4 个 Task 执行，每个 Task 独立可编译。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit (MDIX)

## Global Constraints

- MDIX 优先：用 MDIX 内置样式，不自定义 ControlTemplate
- 间距用 Token（SpacingXS~XXXL），不硬编码
- 颜色用 DynamicResource，确保 Dark 主题适配
- 每个 Task 完成后必须通过 `dotnet build` 验证
- **执行 Plan 必须经用户确认**

---

## File Structure

| 操作 | 文件 | 职责 |
|------|------|------|
| Modify | `src/Client/Desktop/Shell/Styles/Controls.xaml` | 修复硬编码白色和字体 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml` | 删除旧字号和重叠样式 |
| Modify | `src/Client/Desktop/Shell/App.xaml` | 移除空占位符 |
| Delete | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Theme.Light.xaml` | 空文件清理 |
| Modify | ~20 个视图文件 | 硬编码字号替换为 Token |

---

## Task 1: 修复硬编码白色背景（P0）

**Covers:** 设计审计问题 4 — Dark 主题适配

**Files:**
- Modify: `src/Client/Desktop/Shell/Styles/Controls.xaml`

- [ ] **Step 1: 搜索所有硬编码白色**

```bash
rg -n 'Background="White"' src/Client/Desktop/ --include="*.xaml"
```

- [ ] **Step 2: 替换 Controls.xaml 中的硬编码白色**

```xml
<!-- 修改前 -->
<Setter Property="Background" Value="White" />

<!-- 修改后 -->
<Setter Property="Background" Value="{DynamicResource MaterialDesignBody}" />
```

- [ ] **Step 3: 替换其他文件中的硬编码白色**

同样的替换方式，应用到所有搜索到的文件。

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "fix: replace hardcoded White background with MDIX DynamicResource"
```

---

## Task 2: 统一字号系统（P1）

**Covers:** 设计审计问题 1 — 双字号系统并存

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml`

- [ ] **Step 1: 删除 DesignSystem.xaml 中的旧字号定义**

删除以下行：
```xml
<sys:Double x:Key="FontSizeCaption">12</sys:Double>
<sys:Double x:Key="FontSizeBody">14</sys:Double>
<sys:Double x:Key="FontSizeSubtitle">20</sys:Double>
<sys:Double x:Key="FontSizeTitle">28</sys:Double>
<sys:Double x:Key="FontSizeDisplay">40</sys:Double>
<sys:Double x:Key="FontSizeNormal">14</sys:Double>
```

Typography.xaml 的新命名（FontSizeXS/SM/MD/LG/XL/XXL）保留。

- [ ] **Step 2: 全局替换旧字号引用**

| 旧 Token | 新 Token |
|----------|---------|
| `FontSizeCaption` | `FontSizeXS` |
| `FontSizeBody` | `FontSizeMD` |
| `FontSizeSubtitle` | `FontSizeXL` |
| `FontSizeTitle` | `FontSizeXXL` |
| `FontSizeDisplay` | `FontSizeXXL` |
| `FontSizeNormal` | `FontSizeMD` |

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "refactor: unify font size tokens to Typography naming (XS/SM/MD/LG/XL/XXL)"
```

---

## Task 3: 删除重叠样式和空占位符（P2）

**Covers:** 设计审计问题 2 + 3

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml`
- Modify: `src/Client/Desktop/Shell/App.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Theme.Light.xaml`

- [ ] **Step 1: 删除 DesignSystem.xaml 中的重叠样式**

删除以下样式定义（Typography.xaml 中有等价替代）：
- `HeaderText` → 用 `PageTitle` 替代
- `TitleText` → 用 `H2TextBlock` 替代
- `LabelText` → 用 `FieldLabel` 替代

- [ ] **Step 2: 替换重叠样式引用**

| 旧样式 | 新样式 |
|--------|--------|
| `{StaticResource HeaderText}` | `{StaticResource PageTitle}` |
| `{StaticResource TitleText}` | `{StaticResource H2TextBlock}` |
| `{StaticResource LabelText}` | `{StaticResource FieldLabel}` |

- [ ] **Step 3: 从 App.xaml 移除 Theme.Light.xaml 引用**

```xml
<!-- 删除这行 -->
<ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Theme.Light.xaml" />
```

- [ ] **Step 4: 删除 Theme.Light.xaml 文件**

```bash
rm src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Theme.Light.xaml
```

- [ ] **Step 5: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "refactor: remove duplicate styles and empty Theme.Light.xaml placeholder"
```

---

## Task 4: 修复硬编码字号和字体（P2）

**Covers:** 设计审计问题 5 + 6

**Files:**
- Modify: ~20 个视图文件（全局搜索替换）

- [ ] **Step 1: 搜索所有硬编码字号**

```bash
rg -n 'FontSize="[0-9]+"' src/Client/Desktop/ --include="*.xaml" | grep -v "StaticResource\|DynamicResource"
```

- [ ] **Step 2: 替换硬编码字号为 Token**

| 硬编码值 | Token |
|---------|-------|
| `FontSize="12"` | `FontSize="{StaticResource FontSizeXS}"` |
| `FontSize="13"` | `FontSize="{StaticResource FontSizeSM}"` |
| `FontSize="14"` | `FontSize="{StaticResource FontSizeMD}"` |
| `FontSize="16"` | `FontSize="{StaticResource FontSizeLG}"` |
| `FontSize="20"` | `FontSize="{StaticResource FontSizeXL}"` |
| `FontSize="24"` | `FontSize="{StaticResource FontSizeXXL}"` |
| `FontSize="28"` | `FontSize="{StaticResource FontSizeXXL}"` |

- [ ] **Step 3: 搜索硬编码 FontFamily**

```bash
rg -n 'FontFamily="Microsoft YaHei"' src/Client/Desktop/ --include="*.xaml"
```

- [ ] **Step 4: 替换硬编码字体**

```xml
<!-- 修改前 -->
FontFamily="Microsoft YaHei"

<!-- 修改后 -->
FontFamily="{DynamicResource PrimaryFontFamily}"
```

- [ ] **Step 5: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 6: 搜索残留硬编码**

```bash
rg -n 'FontSize="[0-9]+"' src/Client/Desktop/ --include="*.xaml" | grep -v "StaticResource\|DynamicResource"
rg -n 'FontFamily="Microsoft YaHei"' src/Client/Desktop/ --include="*.xaml"
```
Expected: 0 结果

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "refactor: replace hardcoded FontSize and FontFamily with design tokens"
```

---

## Task 5: 完整构建验证

**Covers:** 所有任务的集成验证

- [ ] **Step 1: 完整解决方案构建**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL (0 错误)

- [ ] **Step 2: 搜索所有已修复问题的残留**

```bash
# 硬编码白色
rg -n 'Background="White"' src/Client/Desktop/ --include="*.xaml"
# 旧字号 Token
rg -n 'FontSizeCaption|FontSizeBody|FontSizeSubtitle|FontSizeTitle|FontSizeDisplay|FontSizeNormal' src/Client/Desktop/ --include="*.xaml"
# 硬编码字号
rg -n 'FontSize="[0-9]+"' src/Client/Desktop/ --include="*.xaml" | grep -v "StaticResource\|DynamicResource"
# 硬编码字体
rg -n 'FontFamily="Microsoft YaHei"' src/Client/Desktop/ --include="*.xaml"
```
Expected: 全部 0 结果

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: complete desktop design framework optimization"
```
