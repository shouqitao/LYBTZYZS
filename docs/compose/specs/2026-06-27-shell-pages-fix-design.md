# LoginView + AdminHomeView + MainWindow 修复设计

**日期**: 2026-06-27
**范围**: 3 个页面的硬编码颜色 Token 化 + 登录页输入框重构

---

## [S1] 问题

审计发现 3 个页面不符合 DESIGN.md v2 规范：

1. **LoginView**: 13 处硬编码颜色、emoji 按钮、自定义输入框未用 MDIX
2. **AdminHomeView**: 硬编码渐变色和投影色、固定尺寸卡片
3. **MainWindow**: 内容区和状态栏的 Surface 背景与 L0 暖灰底冲突

## [S2] LoginView 修复

### 颜色替换表

| 位置 | 当前值 | 替换为 |
|------|--------|--------|
| LoginInputStyle Background | `#FAFAFA` | `{DynamicResource MaterialDesignPaper}` |
| LoginInputStyle BorderBrush | `#E0E0E0` | `{DynamicResource MaterialDesign.Brush.Outline}` |
| 品牌副标题 Foreground | `#D7CCC8` | `{DynamicResource MaterialDesign.Brush.Primary.Light}` |
| 标语 Foreground | `#8D6E63` | `{DynamicResource MaterialDesign.Brush.Primary}` |
| 标签文字 Foreground ×2 | `#999999` | `{DynamicResource MaterialDesign.Brush.Foreground}` + Opacity="0.6" |
| 登录按钮 Background | `#5D4037` | `{DynamicResource MaterialDesign.Brush.Primary.Dark}` |
| 错误信息 Foreground | `#C75050` | `{DynamicResource ValidationErrorBrush}` |
| 切换按钮 Foreground | `#8D6E63` | `{DynamicResource MaterialDesign.Brush.Primary}` |
| 配置按钮 Foreground | `#AAAAAA` | `{DynamicResource MaterialDesign.Brush.Foreground}` + Opacity="0.4" |
| 登录卡片 Background | `#FFFFFF` | `{DynamicResource SurfaceLevel1Brush}` |

### 保留不动

- 背景图片 + `#40000000` 半透明遮罩
- 白色品牌文字（深色背景上可读）
- `#F5E6C8` 金色书法标题（LiSu 字体专用色）
- `#DAA520` / `#2E8B57` 模式徽章（语义色）
- `#40000000` 加载遮罩背景

### 输入框重构

将自定义 `LoginInputStyle` 替换为 MDIX 标准样式：

当前：
```xml
<Style x:Key="LoginInputStyle" TargetType="Control">
    <Setter Property="Height" Value="50" />
    <Setter Property="Padding" Value="14,0" />
    <Setter Property="Background" Value="#FAFAFA" />
    <Setter Property="BorderBrush" Value="#E0E0E0" />
</Style>
```

改为：删除 `LoginInputStyle`，输入框和密码框直接使用 `MaterialDesignOutlinedTextBox` / `MaterialDesignOutlinedPasswordBox`。

### emoji 清理

- `"📱 切换到本地"` → `"切换到本地"`
- `"🌐 切换到远程"` → `"切换到远程"`
- `"⚙ 配置"` → `"配置"`

---

## [S3] AdminHomeView 修复

### 颜色替换

| 位置 | 当前值 | 替换为 |
|------|--------|--------|
| PrimaryFunctionCardStyle 渐变起点 | `#D7CCC8` | `{DynamicResource Primary200}` |
| PrimaryFunctionCardStyle 渐变终点 | `#4E342E` | `{DynamicResource Primary800}` |
| FunctionCardStyle 投影色 | `#5D4037` | `{DynamicResource MaterialDesign.Brush.Primary.Dark}` |
| PrimaryFunctionCardStyle 投影色 | `#5D4037` | `{DynamicResource MaterialDesign.Brush.Primary.Dark}` |

### 卡片尺寸响应式

`FunctionCardStyle` 从固定尺寸改为弹性：
```xml
<!-- 当前 -->
<Setter Property="Width" Value="200" />
<Setter Property="Height" Value="180" />

<!-- 改为 -->
<Setter Property="MinWidth" Value="200" />
<Setter Property="MinHeight" Value="180" />
```

---

## [S4] MainWindow 修复

### 内容区背景

第 184 行：
```xml
<!-- 当前 -->
Background="{DynamicResource MaterialDesign.Brush.Surface}"

<!-- 改为 -->
Background="Transparent"
```

让 L0 暖灰底色透出，内容区不再有独立白色背景。

### 状态栏背景

第 191 行同样改为 `Transparent`。

---

## [S5] 验证

编译验证：`dotnet build LYBTZYZS.sln --nologo` → 0 errors
