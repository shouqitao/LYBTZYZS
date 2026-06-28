# Master-Detail 重设计：用户管理界面

**日期**: 2026-06-27
**范围**: User 模块 master-detail 控件 + 共享控件基础优化
**方案**: B — 主从全面重构

---

## [S1] 问题

sysadmin/Admin 角色的用户管理界面（`UserMasterDetailControl`）观感差，具体表现：

1. **详情面板**：3 张独立 `InfoCard` 堆叠展示约 10 个字段，`Padding=40,32` 过大，垂直滚动过多，视觉嘈杂
2. **编辑表单**：Grid 中间用 `Width="20"` 空列做间距；Label 用 `<Run>` 拼接星号；验证错误色硬编码 `#C75050`
3. **中文字体**：全局仅 `Microsoft YaHei`；DataGrid 全局样式用 `Consolas`（中文回退差）
4. **硬编码**：颜色（`#C75050`、`#FDE7E9`）、间距（8/16/20/40px）散落各处，间距 Token（`SpacingXS~XXL`）几乎未被使用
5. **Master 列表**：DataGrid 列宽写死（100/80/70px），筛选 ComboBox 仅 100px 宽（"全部角色"截断）
6. **工具栏**：编辑高频操作藏在 AdditionalContent 里；`Menu`/`MenuItem` 做"更多"菜单视觉不协调

## [S2] 方案概要

三层重构，自底向上：

1. **共享基础层**：补全间距 Token，统一颜色到 MDIX Brush，优化中文字体栈，调整 `InfoCard` 内边距
2. **Master 侧**：DataGrid 列宽弹性化，筛选栏加宽，工具栏用 MDIX `PopupBox` 整合溢出操作
3. **Detail 侧**：查看模式从 3 卡片合并为单卡片 + 分区隔线 + 概要头；编辑模式去空列、Token 化、MDIX 错误色

## [S3] 范围

### 影响文件

| 文件 | 类型 | 改动级别 |
|------|------|----------|
| `Themes/Spacing.xaml` | 共享 | 新增 1 个 Token |
| `Shell/App.xaml` | 共享 | 颜色别名指向 MDIX Brush |
| `Themes/DataGridStyles.xaml` | 共享 | 字体回退 |
| `Controls/InfoCard.xaml` | 共享 | Padding/CornerRadius/Margin |
| `Controls/DetailToolbar.xaml` | 共享 | 删除按钮颜色 Token 化 |
| `Controls/UserMasterDetailControl.xaml` | User 模块 | 列宽、筛选栏、工具栏重构 |
| `Controls/UserViewControl.xaml` | User 模块 | 单卡片 + 分区重构 |
| `Controls/UserEditControl.xaml` | User 模块 | 去空列、Token 化 |

### 全局影响说明

`InfoCard` 被 Formula / Herbs / Patients / MedicalCase 模块共用。Padding 从 `40,32` → `24,16` 会全局生效，所有模块的详情面板都会变得更紧凑。这是预期行为——当前 Padding 过大是全局问题。

---

## [S4] 共享基础优化

### [S4.1] 间距 Token 补全

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Spacing.xaml`

新增 `SpacingXXXL`：

```xml
<Thickness x:Key="SpacingXXXL">40</Thickness>
```

完整 Token 表：

| Token | 值 | 用途 |
|-------|----|------|
| `SpacingXS` | 4 | 紧凑间距（图标与文字） |
| `SpacingS` | 8 | 标准小间距 |
| `SpacingM` | 12 | 中等间距 |
| `SpacingL` | 16 | 标准间距（字段间） |
| `SpacingXL` | 24 | 大间距（卡片内边距） |
| `SpacingXXL` | 32 | 分区间距 |
| **`SpacingXXXL`** | **40** | 卡片外边距/特殊间距 |

### [S4.2] 颜色统一

**文件**: `Shell/App.xaml`

将 `ValidationErrorBrush` 的颜色值对齐到 MDIX Error 色（MDIX Light 主题下为 `#B00020`）：

```xml
<!-- 对齐 MDIX Error 色，保持业务别名 -->
<SolidColorBrush x:Key="ValidationErrorBrush" Color="#B00020" />
<SolidColorBrush x:Key="ValidationErrorBackgroundBrush" Color="#FCE8E6" />
```

> **注意**：不直接引用 `MaterialDesign.Brush.Error` 作为 StaticResource（解析顺序问题），而是用相同色值。各控件用 `{DynamicResource ValidationErrorBrush}` 引用。

### [S4.3] 中文字体优化

**文件**: `Shell/App.xaml`

在 `CustomDialogWindowStyle` 和全局默认 Style 中设置：

```xml
<Setter Property="FontFamily" Value="Microsoft YaHei UI, Microsoft YaHei, Segoe UI" />
```

**文件**: `Themes/DataGridStyles.xaml`

去掉 DataGrid 全局的 `FontFamily="Consolas"`，改为继承全局字体。DataGrid 主体是中文内容，不该用等宽。

**等宽字体（局部使用）**：手机号、拼音码等字段保留等宽字体，但添加中文回退：

```xml
FontFamily="Cascadia Code, Consolas, Microsoft YaHei UI"
```

### [S4.4] InfoCard 视觉调整

**文件**: `Controls/InfoCard.xaml`

| 属性 | 当前 | 优化后 | 说明 |
|------|------|--------|------|
| `Padding` | `40,32` | `24,16` | 减少 40% 内边距 |
| `CornerRadius` | `16` | `8` | 符合 MDIX Card 规格 |
| `Margin`(底) | `0,0,0,20` | `0,0,0,16` | 使用 SpacingL |
| 阴影 Opacity | `0.08` | `0.06` | 更微妙 |

---

## [S5] Master 侧重构

**文件**: `Controls/UserMasterDetailControl.xaml`

### [S5.1] DataGrid 列宽弹性化

| 列 | 当前 | 优化后 |
|----|------|--------|
| 用户名 | `Width="100"` | `Width="*"` `MinWidth="80"` |
| 姓名 | `Width="80"` | `Width="*"` `MinWidth="60"` |
| 角色 | `Width="70"` | `Width="Auto"` |
| 状态 | `Width="70"` | `Width="Auto"` |
| 手机号 | `Width="*"` | `Width="*"` `MinWidth="100"` |

### [S5.2] 筛选栏优化

- ComboBox 宽度：`100` → 去掉 `Width`，设 `MinWidth="120"`，让内容撑开
- 控件间距：硬编码 `Margin="8,0,0,0"` → `Margin="{StaticResource SpacingS}"`
- 底部分割线：`Opacity="0.2"` → `Opacity="0.15"`

### [S5.3] 工具栏操作整合

将操作按频率分层，用 MDIX `PopupBox` 替代 `Menu`/`MenuItem`：

| 层级 | 操作 | 位置 |
|------|------|------|
| 高频 | 新增、刷新、编辑 | 工具栏主区（可见按钮） |
| 中频 | 重置密码、切换状态 | 右键菜单 + PopupBox |
| 低频 | 导入、模板、导出、删除 | PopupBox 溢出菜单 |

```xml
<materialDesign:PopupBox Style="{StaticResource MaterialDesignToolPopupBox}"
                         ToolTip="更多操作">
    <StackPanel>
        <Button Content="重置密码" Command="{Binding ResetPasswordCommand}"
                Style="{StaticResource MaterialDesignMenuItemButton}"/>
        <Button Content="切换状态" Command="{Binding ToggleUserStatusCommand}"
                Style="{StaticResource MaterialDesignMenuItemButton}"/>
        <Separator/>
        <Button Content="导入" Command="{Binding ImportCommand}"
                Style="{StaticResource MaterialDesignMenuItemButton}"/>
        <Button Content="下载模板" Command="{Binding DownloadTemplateCommand}"
                Style="{StaticResource MaterialDesignMenuItemButton}"/>
    </StackPanel>
</materialDesign:PopupBox>
```

编辑按钮从 AdditionalContent 移到 DataGridToolbar 主区（如果 DataGridToolbar 支持自定义按钮），或保留在 AdditionalContent 但视觉上与新增/刷新同级。

---

## [S6] Detail 查看模式重构

**文件**: `Controls/UserViewControl.xaml`

### 结构变化

**当前**：3 张独立 InfoCard（基本信息 / 联系信息 / 系统信息）垂直堆叠

**优化后**：1 张 InfoCard + 内部分区隔线 + 概要头

```
┌──────────────────────────────────────────┐
│  👤 zhang_san                     ● 启用  │  ← 概要头
│  ═════════════════════════════════════   │  ← 分隔线（粗）
│  基本信息                                 │  ← 区标题（CaptionTextBlock + 灰色）
│  真实姓名  张三      拼音码  ZS           │
│  用户角色  医师                           │
│  ─────────────────────────────────────   │  ← 浅分隔线
│  联系方式                                 │
│  手机号码  138xxxx   邮箱   a@b.c         │
│  ─────────────────────────────────────   │
│  系统信息                                 │
│  最后登录  06-20 14:30  创建  06-01       │
│  更新时间  06-25 10:00                    │
└──────────────────────────────────────────┘
```

### 实现细节

1. **概要头**：`PackIcon Kind="Account"` + `UserName`（SemiBold） + `StatusBadge`（右对齐）
2. **区标题**：`MaterialDesignCaptionTextBlock` + `Foreground="{DynamicResource MaterialDesign.Brush.ForegroundLight}"`（如果存在）或 Opacity 降低
3. **分隔线**：`Border Height="1"` + `Background="{DynamicResource MaterialDesign.Brush.Outline}"` + `Opacity="0.12"`
4. **Label 列宽**：去掉固定 `Width="80/100"`，用 `Width="Auto"` + `Margin="0,8,SpacingL,8"`
5. **值样式**：`FontWeight="SemiBold"` 突出（当前已部分使用，统一化）
6. **等宽字段**：拼音码、手机号用 `"Cascadia Code, Consolas, Microsoft YaHei UI"`

### 去掉的内容

- 3 张独立 `InfoCard` → 合并为 1 张
- 固定 Label 列宽 `Width="80"` / `Width="100"` → Auto
- 每个字段 `Margin="0,8,16,8"` 硬编码 → 用 Token 或统一 Style

---

## [S7] Detail 编辑模式重构

**文件**: `Controls/UserEditControl.xaml`

### 结构变化

保留两张 InfoCard（用户信息 + 备注），但优化内部布局。

### [S7.1] 去掉中间空列

**当前**：
```xml
<ColumnDefinition Width="*"/>
<ColumnDefinition Width="20"/>    <!-- hack 间距 -->
<ColumnDefinition Width="*"/>
```

**优化后**：
```xml
<ColumnDefinition Width="*"/>
<ColumnDefinition Width="*"/>
```
左列字段设 `Margin="0,0,24,0"`（SpacingXL）做列间距。

### [S7.2] Label 颜色 Token 化

**当前**：
```xml
<Run Text=" *" Foreground="#C75050" FontWeight="Bold"/>
```

**优化后**：
```xml
<Run Text=" *" Foreground="{DynamicResource ValidationErrorBrush}" FontWeight="Bold"/>
```

### [S7.3] 字段间距 Token 化

**当前**：`Margin="0,0,0,20"` 硬编码

**优化后**：`Margin="0,0,0,24"` → 使用 `SpacingXL` Token 绑定

### [S7.4] 验证错误样式

**当前**：本地定义 `ValidationErrorMessageVisibleStyle`，硬编码 `#C75050`

**优化后**：引用全局 `ValidationErrorBrush`：

```xml
<Style x:Key="ValidationErrorMessageVisibleStyle" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource ValidationErrorBrush}"/>
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="Margin" Value="0,4,0,0"/>
    <Setter Property="TextWrapping" Value="Wrap"/>
    <Setter Property="MinHeight" Value="16"/>
</Style>
```

---

## [S8] DetailToolbar 优化

**文件**: `Controls/DetailToolbar.xaml`

### 删除按钮颜色 Token 化

**当前**：
```xml
Background="#C75050" Foreground="White" BorderThickness="0"
```

**优化后**：
```xml
Background="{DynamicResource ValidationErrorBrush}" Foreground="White" BorderThickness="0"
```

### Danger hover 样式

**当前**：
```xml
<Trigger Property="IsMouseOver" Value="True">
    <Setter Property="Background" Value="#FDE7E9" />
</Trigger>
```

**优化后**：
```xml
<Trigger Property="IsMouseOver" Value="True">
    <Setter Property="Background" Value="{DynamicResource ValidationErrorBackgroundBrush}" />
</Trigger>
```

---

## [S9] 验证与测试

### 编译验证

```bash
dotnet build LYBTZYZS.sln
```

所有改动为 XAML 样式/布局调整，不涉及 ViewModel 逻辑变更，编译应通过。

### 视觉验证

启动 Desktop 应用，登录 sysadmin/admin 账户，进入用户管理：

1. **Master 侧**：
   - DataGrid 列宽随窗口弹性伸缩
   - 筛选 ComboBox 完整显示"全部角色"
   - 工具栏 PopupBox 展开/收起正常
2. **Detail 查看**：
   - 单卡片布局，分区隔线清晰
   - 概要头显示用户名 + 状态
   - 中文渲染清晰
3. **Detail 编辑**：
   - 表单两列间距均匀（无空列 hack）
   - 必填星号为 MDIX 错误色
   - 验证错误提示颜色统一
4. **全局**：
   - 其他模块（Formula/Herb/Patients）的 InfoCard 内边距变小，布局更紧凑

### 回归风险

| 风险点 | 影响 | 缓解 |
|--------|------|------|
| InfoCard Padding 全局变更 | Formula/Herbs/Patients/MedicalCase 详情面板 | 预期改善，非回归 |
| DataGrid 去掉 Consolas | DataGrid 中文字体回退 | 添加中文回退栈 |
| ValidationErrorBrush 色值变更 | 所有验证错误提示颜色 | `#C75050` → `#B00020`，视觉差异微小 |

---

## [S10] 实施顺序

1. **基础层**（S4）：Spacing Token → App.xaml 颜色 → DataGridStyles 字体 → InfoCard
2. **Master 侧**（S5）：DataGrid 列宽 → 筛选栏 → 工具栏 PopupBox
3. **Detail 查看**（S6）：UserViewControl 单卡片重构
4. **Detail 编辑**（S7）：UserEditControl 去空列 + Token 化
5. **DetailToolbar**（S8）：颜色 Token 化
6. **编译验证**：`dotnet build LYBTZYZS.sln`
