# Shell 页面修复实施计划

> **For agentic workers:** Use compose:subagent or compose:execute to implement task-by-task.

**Goal:** 将 3 个 Shell 页面的硬编码颜色替换为 MDIX Token，重构登录页输入框，清理 emoji。

**Architecture:** 纯 XAML 改动，不涉及 ViewModel/C# 逻辑。

**Tech Stack:** WPF / MDIX

## Global Constraints

- 颜色用 `DynamicResource` 引用 Token，禁止 hex 硬编码
- 输入框用 MDIX 内置样式，不用自定义 ControlTemplate
- 保留合理的设计选择（背景图遮罩、金色书法标题、模式徽章语义色）

---

### Task 1: LoginView 颜色 Token 化

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`

- [ ] **Step 1: 替换 LoginInputStyle 中的硬编码色**

`LoginInputStyle`（第 11-19 行）中：
- `Background="#FAFAFA"` → `Background="{DynamicResource MaterialDesignPaper}"`
- `BorderBrush="#E0E0E0"` → `BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"`

- [ ] **Step 2: 替换品牌区域颜色**

第 53 行 `Foreground="#D7CCC8"` → `Foreground="{DynamicResource MaterialDesign.Brush.Primary.Light}"`
第 69 行 `Foreground="#8D6E63"` → `Foreground="{DynamicResource MaterialDesign.Brush.Primary}"`

- [ ] **Step 3: 替换标签和输入区颜色**

第 87 行 `Foreground="#999999"` → `Foreground="{DynamicResource MaterialDesign.Brush.Foreground}" Opacity="0.6"`
第 93 行同样处理。

第 60 行 `Background="#FFFFFF"` → `Background="{DynamicResource SurfaceLevel1Brush}"`

- [ ] **Step 4: 替换按钮和错误信息颜色**

第 114 行 `Background="#5D4037"` → `Background="{DynamicResource MaterialDesign.Brush.Primary.Dark}"`
第 117 行 `Foreground="#C75050"` → `Foreground="{DynamicResource ValidationErrorBrush}"`
第 124 行 `Foreground="#8D6E63"` → `Foreground="{DynamicResource MaterialDesign.Brush.Primary}"`
第 140 行 `Foreground="#AAAAAA"` → `Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"` + Opacity="0.4"
第 154 行同样处理。

- [ ] **Step 5: 清理 emoji**

第 124 行 `"📱 切换到本地"` → `"切换到本地"`
第 136 行 `"🌐 切换到远程"` → `"切换到远程"`
第 154 行 `"⚙ 配置"` → `"配置"`

- [ ] **Step 6: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Auth/LYBT.Desktop.Auth.csproj --nologo`
Expected: 0 errors

---

### Task 2: AdminHomeView Token 化

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml`

- [ ] **Step 1: 替换渐变色**

第 46 行 `Color="#D7CCC8"` → `Color="{DynamicResource Primary200}"`
第 47 行 `Color="#4E342E"` → `Color="{DynamicResource Primary800}"`

- [ ] **Step 2: 替换投影色**

搜索所有 `Color="#5D4037"` 替换为 `Color="{DynamicResource MaterialDesign.Brush.Primary.Dark}"`

- [ ] **Step 3: 卡片响应式**

`FunctionCardStyle` 中 `Width="200"` → 删除此行，添加 `MinWidth="200"`
`Height="180"` → 删除此行，添加 `MinHeight="180"`

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Admin/LYBT.Desktop.Admin.csproj --nologo`
Expected: 0 errors

---

### Task 3: MainWindow 背景修复

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

- [ ] **Step 1: 内容区背景透明化**

第 184 行 `Background="{DynamicResource MaterialDesign.Brush.Surface}"` → `Background="Transparent"`

- [ ] **Step 2: 状态栏背景透明化**

第 191 行 `Background="{DynamicResource MaterialDesign.Brush.Surface}"` → `Background="Transparent"`

- [ ] **Step 3: 编译验证**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --nologo`
Expected: 0 errors

---

### Task 4: 全量验证

- [ ] **Step 1: 全解决方案编译**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors

- [ ] **Step 2: 硬编码扫描**

Run: `rg "#C75050|#5D4037|#8D6E63|#D7CCC8" --include "*.xaml" src/Client/Desktop/Shell/ src/Client/Desktop/Roles/ src/Client/Desktop/Modules/LYBT.Desktop.Auth/`
Expected: 0 matches
