# 用户管理 UI/UX 深度优化计划

> **For agentic workers:** Use compose:execute to implement.

**Goal:** 基于 ui-ux-pro-max 原则，优化用户管理界面的 DataGrid、搜索区、详情面板的视觉层次和交互反馈。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit

## Global Constraints

- MDIX 优先
- 不改变功能逻辑
- 颜色用 DynamicResource

---

## Task 1: DataGrid 行视觉优化

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`

- [ ] **Step 1: 优化 DataGrid 行悬停和选中样式**

在 `MasterDetailDataGridStyle` 中确认以下样式正确：
- 行悬停背景：`MaterialDesignPaper`（已修复，确认生效）
- 选中行背景：使用 `MaterialDesign.Brush.Primary` 加 Opacity=0.1
- 选中行文字：保持 `MaterialDesign.Brush.Foreground`
- 交替行背景：`MaterialDesignPaper`（已修复，确认不与主背景混淆）

如果交替行和主背景都是 `MaterialDesignPaper`（同色），交替行无效果。修改交替行为透明：
```xml
<Setter Property="AlternatingRowBackground" Value="Transparent" />
```

- [ ] **Step 2: 验证编译并提交**

---

## Task 2: 搜索区视觉分组

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: 搜索框添加 MDIX 样式**

检查 SearchBox 控件是否使用 MDIX TextBox 样式。如果 SearchBox 是自定义控件，确认其内部 TextBox 使用了 MDIX 样式。

- [ ] **Step 2: 筛选区域添加底部边框**

在筛选 StackPanel 下方添加细分隔线，与 DataGrid 视觉分离：
```xml
<!-- 在 Grid.Row="1" 的 Grid 底部添加 -->
<Border Grid.Row="1" Height="1" VerticalAlignment="Bottom"
        Background="{DynamicResource MaterialDesign.Brush.Outline}" Opacity="0.2" />
```

- [ ] **Step 3: 验证编译并提交**

---

## Task 3: 详情面板间距规范化

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

- [ ] **Step 1: Detail 区域 Margin 统一**

UserViewControl 和 UserEditControl 的 `Margin="16"` 统一为 `Margin="20"`（更舒适的留白）。

- [ ] **Step 2: 验证编译并提交**
