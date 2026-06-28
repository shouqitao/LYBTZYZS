# Shell 全页面设计审计报告

**日期**: 2026-06-27
**范围**: Desktop 应用全部 31 个 XAML 视图

---

## 审计结论

### 已符合 DESIGN.md v2 的页面（28 个）

以下页面已使用 MDIX Token、无硬编码颜色、布局符合规范：

| 文件 | 说明 |
|------|------|
| AccountSettingsControl.xaml | ✅ 用 DynamicResource，2列布局 |
| MessageDialog.xaml | ✅ 颜色编码图标 |
| InputDialog.xaml | ✅ 标准输入 |
| ConfirmationDialog.xaml | ✅ 标准确认 |
| SysadminHomeView.xaml | ✅ 用 materialDesign:Card + ElevationAssist |
| LogLevelControlView.xaml | ✅ 日志级别控制 |
| 所有 ManagementView（Admin/Clinical） | ✅ 薄包装，已升级的模块控件自动继承 |
| RegistrationListView.xaml | ✅ 标准列表 |
| MedicalCaseMasterDetailView.xaml | ✅ 薄包装 |
| MedicalCaseWorkspaceView.xaml | ✅ 临床工作台 |
| ClinicalWorkspaceView.xaml | ✅ 临床工作区 |
| PendingQueueView.xaml | ✅ 待诊队列 |
| PatientSelectionView.xaml | ✅ 患者选择 |

### 需要修复的页面（3 个）

---

## 问题 1: LoginView.xaml (182行)

**路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`

### 硬编码颜色（13 处）

| 行 | 硬编码值 | 应替换为 |
|----|----------|----------|
| 16 | `Background="#FAFAFA"` | `{DynamicResource MaterialDesignPaper}` |
| 17 | `BorderBrush="#E0E0E0"` | `{DynamicResource MaterialDesign.Brush.Outline}` |
| 34 | `Background="#40000000"` | 保留（半透明遮罩，合理） |
| 45 | `Foreground="#FFFFFF"` | 保留（深色背景上白字，合理） |
| 50 | `Foreground="#F5E6C8"` | 自定义暖金色，保留但注释 |
| 53 | `Foreground="#D7CCC8"` | `{DynamicResource MaterialDesign.Brush.Primary.Light}` |
| 60 | `Background="#FFFFFF"` | `{DynamicResource SurfaceLevel1Brush}` |
| 69 | `Foreground="#8D6E63"` | `{DynamicResource MaterialDesign.Brush.Primary}` |
| 87,93 | `Foreground="#999999"` | `{DynamicResource MaterialDesign.Brush.Foreground}` Opacity=0.6 |
| 117 | `Foreground="#C75050"` | `{DynamicResource ValidationErrorBrush}` |
| 114 | `Background="#5D4037"` | `{DynamicResource MaterialDesign.Brush.Primary.Dark}` |
| 124 | `Content="📱 切换到本地"` | 去掉 emoji，用 PackIcon |
| 136 | `Content="🌐 切换到远程"` | 去掉 emoji，用 PackIcon |
| 154 | `Content="⚙ 配置"` | 去掉 emoji，用 PackIcon |

### 自定义输入框样式

`LoginInputStyle` 应改为使用 `MaterialDesignOutlinedTextBox`：
```xml
<!-- 当前 -->
<Style x:Key="LoginInputStyle" TargetType="Control">
    <Setter Property="Background" Value="#FAFAFA" />
    <Setter Property="BorderBrush" Value="#E0E0E0" />
</Style>

<!-- 应改为 -->
<TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}" />
```

---

## 问题 2: AdminHomeView.xaml (170行)

**路径**: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml`

### 硬编码渐变色

`PrimaryFunctionCardStyle` 第 45-48 行：
```xml
<!-- 当前 -->
<GradientStop Offset="0" Color="#D7CCC8" />
<GradientStop Offset="1" Color="#4E342E" />

<!-- 应改为 -->
<GradientStop Offset="0" Color="{DynamicResource Primary200}" />
<GradientStop Offset="1" Color="{DynamicResource Primary800}" />
```

### 硬编码投影色

第 27 行和第 40 行 `Color="#5D4037"` → `{DynamicResource MaterialDesign.Brush.Primary.Dark}`

### 固定尺寸卡片

`FunctionCardStyle` `Width="200" Height="180"` — 建议改为 `MinWidth="200" MinHeight="180"` + `Width="*"` 以支持响应式。

---

## 问题 3: MainWindow.xaml (206行)

**路径**: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

### 内容区背景

第 184 行 `Background="{DynamicResource MaterialDesign.Brush.Surface}"` — MDIX Surface 在 Light 主题下是白色，与 L0 暖灰底冲突。应改为 `Transparent` 让 L0 底色透出，或改为 `{DynamicResource SurfaceLevel0Brush}`。

### 状态栏背景

第 191 行 `Background="{DynamicResource MaterialDesign.Brush.Surface}"` — 同上问题。

---

## 修复优先级

| 优先级 | 文件 | 影响范围 | 工作量 |
|--------|------|----------|--------|
| P0 | LoginView.xaml | 所有用户首次接触的页面 | 中（13处颜色 + 输入框样式） |
| P1 | AdminHomeView.xaml | Admin 角色首页 | 小（渐变色 + 投影色） |
| P1 | MainWindow.xaml | 全局 Shell 背景 | 小（2处 Surface → L0） |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-06-27 | 初始审计报告 |
