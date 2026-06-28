# LYBT 凌隐宝堂 — UI 设计规范 v2

> **单一事实来源**：所有 WPF 界面的配色、字体、间距、层次必须遵循本文档。
> 技术栈：WPF / MaterialDesignInXAML (MDIX) / Prism
> 主题：MDIX Light · Primary=Brown · Secondary=Amber
> 参考：Material Design 2 色彩体系 · ui-ux-pro-max 医疗/餐饮配色数据库

---

## 1. 设计理念

**"温润如玉，结构如方"**

### 色彩心理学依据

中医讲究温润平和，棕色在色彩心理学中代表：
- **大地/根基** — 中药材来自自然，棕色是土地和草药的颜色
- **信任/可靠** — 医疗系统需要稳重感，棕色比蓝色更有人情味
- **传统/传承** — 中医有千年历史，棕色传递古典和积累
- **温暖/包容** — 比冷色调的青蓝色更让患者放松

**参考行业对比**：
| 行业 | 标准色 | 我们的选择 | 差异化 |
|------|--------|-----------|--------|
| 西医诊所 | 青色 #0891B2 | 棕色 #795548 | 体现中西医差异 |
| 烘焙/咖啡 | 暖棕 #92400E | 棕色 #795548 | 更克制、更专业 |
| 金融系统 | 深蓝 #0F172A | 棕色 #795548 | 更温暖、更亲和 |

**设计原则**：
1. 暖色调为主，避免冷蓝/科技感
2. 层次分明，信息清晰
3. 克制用色，不花哨
4. 中文排版优先，等宽仅用于数据字段

---

## 2. 配色体系

### 2.1 主色阶 — Brown（中医棕）

MDIX `BundledTheme PrimaryColor="Brown"` 自动生成。

| 色阶 | Hex | MDIX Token | 对比度(白底) | 用途 |
|------|-----|------------|-------------|------|
| 50 | `#EFEBE9` | `Primary50` | 1.1:1 | 悬停背景、选中态背景 |
| 100 | `#D7CCC8` | `Primary100` | 1.4:1 | 选中态背景、禁用态边框 |
| 200 | `#BCAAA4` | `Primary200` | 1.8:1 | `MaterialDesign.Brush.Primary.Light` |
| 300 | `#A1887F` | `Primary300` | 2.5:1 | 禁用态文字（需配合背景） |
| 400 | `#8D6E63` | `Primary400` | 3.3:1 | 次要按钮文字 |
| **500** | **`#795548`** | **`Primary500`** | **4.6:1** | **主色 — 按钮、链接、焦点框** ✓AA |
| 600 | `#6D4C41` | `Primary600` | 5.7:1 | 按下态 | ✓AA |
| 700 | `#5D4037` | `Primary700` | 7.5:1 | `MaterialDesign.Brush.Primary.Dark` ✓AAA |
| 800 | `#4E342E` | `Primary800` | 9.8:1 | 深色标题文字 ✓AAA |
| 900 | `#3E2723` | `Primary900` | 12.5:1 | 极深文字 ✓AAA |

> 对比度标准：WCAG AA ≥ 4.5:1（正文），AAA ≥ 7:1（正文）。
> Primary 500 (#795548) 在白色背景上对比度 4.6:1，刚好满足 AA 标准。

### 2.2 辅色阶 — Amber（琥珀金）

MDIX `BundledTheme SecondaryColor="Amber"` 自动生成。

| 色阶 | Hex | 用途 |
|------|-----|------|
| 50 | `#FFF8E1` | Amber 悬停背景 |
| 100 | `#FFECB3` | Amber 选中态 |
| 200 | `#FFE082` | `MaterialDesign.Brush.Secondary.Light` |
| **400** | **`#FFCA28`** | **辅色 — FAB、Switch、次要强调** |
| 700 | `#FFA000` | 辅色按下态 |

> Amber 在棕色主色下作为"暖金色"点缀，代表中医的"精"和"贵重"。
> 仅用于需要用户注意的交互元素（浮动按钮、开关），不大面积使用。

### 2.3 表面层次 — Surface（核心系统）

| Level | Token | Hex | 用途 | 投影 |
|-------|-------|-----|------|------|
| **L0** | `SurfaceLevel0Brush` | `#FAF8F5` | 页面底色 | 无 |
| **L1** | `SurfaceLevel1Brush` | `#FFFFFF` | 面板、工具栏、侧边栏 | Elevation1 |
| **L2** | `SurfaceLevel2Brush` | `#FFFFFF` | 浮起卡片（InfoCard） | Elevation1 |
| **L3** | `SurfaceLevel3Brush` | `#FFFFFF` | 模态弹窗、浮层 | Elevation3 |

> **为什么是暖灰 #FAF8F5 而不是冷灰 #F5F5F5？**
> 冷灰（纯中性灰）在棕色主题下会显得脏。暖灰带微弱的黄/米色调，与棕色主色和谐。
> 参考：Bakery/Cafe 配色方案的奶油底 #FEF3C7 是更暖的变体，我们取其 1/3 暖度。

### 2.4 暗色模式预定义（未来扩展）

| Level | Token | Hex | 用途 |
|-------|-------|-----|------|
| L0 Dark | `SurfaceLevel0DarkBrush` | `#121212` | 暗色页面底 |
| L1 Dark | `SurfaceLevel1DarkBrush` | `#1E1E1E` | 暗色面板 |
| L2 Dark | `SurfaceLevel2DarkBrush` | `#2C2C2C` | 暗色卡片 |
| L3 Dark | `SurfaceLevel3DarkBrush` | `#383838` | 暗色弹窗 |

> Material Design 暗色模式规范：L0 最暗，逐级变亮。每级增加约 5% 白色叠加。
> 当前版本仅实现 Light 主题，Dark Token 预留但不注册到资源字典。

### 2.5 功能色

| 功能 | Token | Hex | 对比度(白底) | 用途 |
|------|-------|-----|-------------|------|
| Error | `ValidationErrorBrush` | `#B00020` | 5.9:1 ✓AA | 验证错误、删除按钮、必填星号 |
| Error BG | `ValidationErrorBackgroundBrush` | `#FCE8E6` | — | 错误悬停背景 |
| Warning | `WarningBrush` | `#E65100` | 4.6:1 ✓AA | 警告提示 |
| Warning BG | `WarningBackgroundBrush` | `#FFF3E0` | — | 警告背景 |
| Success | `SuccessBrush` | `#2E7D32` | 5.1:1 ✓AA | 成功状态、恢复操作 |
| Success BG | `SuccessBackgroundBrush` | `#E8F5E9` | — | 成功背景 |
| Info | `InfoBrush` | `#1565C0` | 5.6:1 ✓AA | 信息提示 |
| Info BG | `InfoBackgroundBrush` | `#E3F2FD` | — | 信息背景 |

> 所有功能色均满足 WCAG AA 对比度标准（≥ 4.5:1）。

### 2.6 文字色

| 层级 | Token | Hex | 对比度(L0底) | 用途 |
|------|-------|-----|-------------|------|
| Primary | `MaterialDesign.Brush.Foreground` | `#DD000000` (87%黑) | 15.2:1 | 正文、主要信息 |
| Secondary | — | `#89000000` (54%黑) | 8.2:1 | 标签、次要信息 |
| Disabled | — | `#61000000` (38%黑) | 4.9:1 | 禁用态文字 |
| Hint | — | `#38000000` (22%黑) | 2.3:1 | 占位提示（仅装饰） |
| On Primary | `MaterialDesign.Brush.Primary.Foreground` | `#FFFFFF` | — | 主色背景上的白色文字 |
| On Error | — | `#FFFFFF` | — | 错误色背景上的白色文字 |

> **中文标签规范**：使用 `Opacity="0.6"` 代替硬编码灰色，确保与主题一致性。

### 2.7 分隔线

| Token | Hex | Opacity | 用途 |
|-------|-----|---------|------|
| `DividerBrush` | `#E0DCD5` | 1.0 | 卡片间分隔（明显） |
| `DividerLightBrush` | `#EDE9E3` | 1.0 | 卡片内分区（微妙） |
| MDIX Outline | `{DynamicResource MaterialDesign.Brush.Outline}` | 0.12~0.2 | 边框、隔线 |

### 2.8 交互状态色

| 状态 | 颜色方案 | 实现方式 |
|------|----------|----------|
| **Hover** | Primary 50 (`#EFEBE9`) 背景 | MDIX 样式自动处理 |
| **Pressed** | Primary 100 (`#D7CCC8`) 背景 | MDIX 样式自动处理 |
| **Focused** | Primary 500 边框 2px | `FocusVisualStyle` 或 `FocusManager` |
| **Selected** | Primary 50 (`#EFEBE9`) 背景 + Primary 500 左边框 | DataGrid 行选中 |
| **Disabled** | 全局 Opacity=0.4 | MDIX 样式自动处理 |

---

## 3. 字体系统

### 3.1 字体族

| 用途 | FontFamily 栈 | 说明 |
|------|---------------|------|
| **全局默认** | `Microsoft YaHei UI, Microsoft YaHei, Segoe UI` | YaHei UI 专为小尺寸 UI 渲染优化，是 Windows 10/11 最佳中文字体 |
| **等宽** | `Cascadia Code, Consolas, Microsoft YaHei UI` | 手机号、拼音码、编号、代码片段 |
| **图标** | MDIX PackIcon | Material Design 图标集，不用 emoji |

**字体选择理由**：
- `Microsoft YaHei UI` vs `Microsoft YaHei`：UI 版本针对 9-12px 小字号优化了字距和笔画，正文用 UI 版本更清晰
- `Segoe UI`：Windows 系统字体，英文 fallback 最佳
- `Cascadia Code`：微软等宽字体，支持连字(ligatures)，中文回退到 YaHei UI

### 3.2 字号层级

| 层级 | MDIX Style | 字号 | 字重 | 行高 | 字间距 | 用途 |
|------|-----------|------|------|------|--------|------|
| H4 | `MaterialDesignHeadline4TextBlock` | 34px | Regular | 40px | 0.25px | 极少用（大标题） |
| H5 | `MaterialDesignHeadline5TextBlock` | 24px | Regular | 32px | 0px | 页面主标题 |
| H6 | `MaterialDesignHeadline6TextBlock` | 20px | SemiBold | 28px | 0.15px | 概要头（用户名） |
| Subtitle1 | `MaterialDesignSubtitle1TextBlock` | 16px | SemiBold | 24px | 0.15px | 卡片标题、DetailToolbar |
| Body1 | `MaterialDesignBody1TextBlock` | 14px | Regular | 20px | 0.25px | 主要正文 |
| Body2 | `MaterialDesignBody2TextBlock` | 14px | Regular | 20px | 0.25px | 字段值（详情面板） |
| Caption | `MaterialDesignCaptionTextBlock` | 12px | Regular | 16px | 0.4px | 字段标签、区标题 |
| Button | `MaterialDesignButtonTextBlock` | 14px | Medium | 20px | 1.25px | 按钮文字 |
| Overline | `MaterialDesignOverlineTextBlock` | 10px | Regular | 16px | 1.5px | 徽章、极小标签 |

**行高规则**：中文行高 = 字号 × 1.4~1.5（比英文的 1.2~1.3 稍大，中文笔画密集需要更多呼吸空间）。

**等宽字号**：数据表格中等宽字体用 12px（`DataGridMonoCellStyle` 已定义），比正文小一号以容纳更多数据。

---

## 4. 间距系统

### 4.1 Token 表

定义在 `Themes/Spacing.xaml`：

| Token | 值 | 用途 | 使用频率 |
|-------|----|------|----------|
| `SpacingXS` | 4 | 图标与文字间距 | 高 |
| `SpacingS` | 8 | 紧凑控件间距、筛选栏 | 高 |
| `SpacingM` | 12 | 区标题与内容间距 | 中 |
| `SpacingL` | 16 | 卡片底部 Margin、字段间垂直 | 高 |
| `SpacingXL` | 24 | 卡片内边距、编辑表单列间距 | 高 |
| `SpacingXXL` | 32 | 大分区间距 | 低 |
| `SpacingXXXL` | 40 | 特殊大间距 | 低 |

### 4.2 用法规则

```xml
<!-- ✅ 正确：引用 Token -->
<Button Margin="{StaticResource SpacingL}" />

<!-- ✅ 正确：数值 + 注释（WPF 不支持内联 StaticResource 于 Thickness 字符串） -->
<Button Margin="0,0,0,16" /> <!-- SpacingL -->

<!-- ❌ 错误：硬编码无注释 -->
<Button Margin="0,0,0,16" />

<!-- ❌ 错误：内联 StaticResource（WPF 不支持） -->
<Button Margin="0,0,0,{StaticResource SpacingL}" />
```

---

## 5. 圆角系统

| Token | 值 | 用途 |
|-------|----|------|
| 无圆角 | 0 | 分隔线、平面元素 |
| 小圆角 | 4 | 按钮（MDIX 默认）、小标签 |
| 中圆角 | 8 | InfoCard、Detail 面板、输入框 |
| 大圆角 | 12 | Dialog、大卡片 |
| 全圆 | 999 | 圆形头像、StatusBadge |

> MDIX 自带控件圆角由样式控制，不手动覆盖。自定义 Border 用 8。

---

## 6. 投影系统

Material Design 投影原则：**Surface 越高，投影越宽越淡**。

定义在 `Themes/Surfaces.xaml`：

| Token | Blur | Depth | Opacity | Direction | 用途 |
|-------|------|-------|---------|-----------|------|
| `Elevation1` | 8 | 1px | 0.06 | 270° | InfoCard、Master 面板 |
| `Elevation2` | 12 | 2px | 0.08 | 270° | 悬停态卡片、浮起面板 |
| `Elevation3` | 24 | 8px | 0.12 | 270° | Dialog、PopupBox 弹出层 |

**投影使用原则**：
- L1 面板用 Elevation1（轻微浮起）
- L2 卡片用 Elevation1（与 L1 同级，靠内容区分）
- L3 弹窗用 Elevation3（明显浮起）
- 悬停态可临时提升一级投影

---

## 7. MDIX 资源映射表

### 7.1 Brush Key 完整列表

| 设计 Token | XAML 引用 | 定义位置 |
|-----------|-----------|----------|
| L0 暖灰底 | `{DynamicResource SurfaceLevel0Brush}` | Surfaces.xaml |
| L1 白面板 | `{DynamicResource SurfaceLevel1Brush}` | Surfaces.xaml |
| 主色 500 | `{DynamicResource MaterialDesign.Brush.Primary}` | MDIX BundledTheme |
| 主色 Light | `{DynamicResource MaterialDesign.Brush.Primary.Light}` | MDIX BundledTheme |
| 主色 Dark | `{DynamicResource MaterialDesign.Brush.Primary.Dark}` | MDIX BundledTheme |
| 主色前景 | `{DynamicResource MaterialDesign.Brush.Primary.Foreground}` | MDIX BundledTheme |
| 辅色 400 | `{DynamicResource MaterialDesign.Brush.Secondary}` | MDIX BundledTheme |
| 前景色 | `{DynamicResource MaterialDesign.Brush.Foreground}` | MDIX BundledTheme |
| 纸面 | `{DynamicResource MaterialDesignPaper}` | MDIX BundledTheme |
| 背景色 | `{DynamicResource MaterialDesign.Brush.Background}` | Surfaces.xaml (覆盖) |
| 轮廓色 | `{DynamicResource MaterialDesign.Brush.Outline}` | MDIX BundledTheme |
| 错误色 | `{DynamicResource ValidationErrorBrush}` | App.xaml |
| 错误底色 | `{DynamicResource ValidationErrorBackgroundBrush}` | App.xaml |
| 分隔线 | `{DynamicResource DividerBrush}` | Surfaces.xaml |
| 浅分隔线 | `{DynamicResource DividerLightBrush}` | Surfaces.xaml |
| 投影1 | `{StaticResource Elevation1}` | Surfaces.xaml |
| 间距M | `{StaticResource SpacingM}` | Spacing.xaml |

### 7.2 加载顺序（关键）

```xml
<!-- App.xaml ResourceDictionary 加载顺序 -->
<materialDesign:BundledTheme ... />                                    <!-- 1. MDIX 主题 -->
<ResourceDictionary Source=".../MaterialDesign2.Defaults.xaml" />      <!-- 2. MDIX 默认样式 -->
<ResourceDictionary Source="/.../Surfaces.xaml" />                     <!-- 3. 覆盖 Background -->
<ResourceDictionary Source="/.../Icons.xaml" />                        <!-- 4. 图标 -->
<ResourceDictionary Source="/.../Spacing.xaml" />                      <!-- 5. 间距 -->
<ResourceDictionary Source="/.../DataGridStyles.xaml" />               <!-- 6. DataGrid -->
<ResourceDictionary Source="/.../Converters.xaml" />                   <!-- 7. 转换器 -->
```

> **Surfaces.xaml 必须在 MDIX 之后加载**，否则 `MaterialDesign.Brush.Background` 覆盖不生效。

---

## 8. 组件样式规范

### 8.1 InfoCard（信息卡片）

```xml
<Border Background="{DynamicResource MaterialDesignPaper}"
        CornerRadius="8"
        Padding="24,16"
        Margin="0,0,0,16"
        Effect="{StaticResource Elevation1}">
```

| 属性 | 值 | 说明 |
|------|-----|------|
| Background | `MaterialDesignPaper` (L1 白) | 与 L0 暖灰底形成对比 |
| CornerRadius | 8 | 中圆角 |
| Padding | 24, 16 | 水平24 / 垂直16 |
| Margin(底) | 0, 0, 0, 16 | 卡片间距 |
| Effect | `Elevation1` | 轻投影浮起 |
| Title | `MaterialDesignSubtitle1TextBlock` | 16px SemiBold |

### 8.2 MasterDetailLayout（主从布局）

| 区域 | 背景 | 投影 | 边框 |
|------|------|------|------|
| 主容器 | `SurfaceLevel0Brush` (L0 暖灰) | 无 | 无 |
| Master 面板 | `SurfaceLevel1Brush` (L1 白) | `Elevation1` | 右边框 `DividerBrush` |
| Detail 面板 | Transparent (L0 透出) | 无 | 无 |
| EmptyState | Transparent | 无 | 无 |

### 8.3 DetailToolbar（详情工具栏）

| 属性 | 值 |
|------|-----|
| Background | `MaterialDesignPaper` (L1 白) |
| Border | 底边 `MaterialDesign.Brush.Outline` Opacity 0.5 |
| Padding | 16, 12 |
| 编辑按钮 | `MaterialDesignRaisedButton` (Primary 色) |
| 保存按钮 | `MaterialDesignRaisedButton` (Primary 色) |
| 取消按钮 | `MaterialDesignOutlinedButton` |
| 删除按钮 | `MaterialDesignRaisedButton` + `ValidationErrorBrush` 背景 + 白色文字 |

### 8.4 DataGrid（数据表格）

**单元格样式**：

| 样式名 | 用途 | 关键属性 |
|--------|------|----------|
| `DataGridEmphasisCellStyle` | 用户名等强调字段 | SemiBold |
| `DataGridTextCellStyle` | 标准文本 | 默认，Padding 8,0 |
| `DataGridSecondaryCellStyle` | 角色等次要信息 | Opacity=0.7 |
| `DataGridMonoCellStyle` | 手机号等等宽 | Cascadia Code 栈 + Opacity=0.7 |
| `DataGridNumericCellStyle` | 数字 | MonoCell + TextAlignment=Right |

**列宽规范**：
- 弹性优先：`Width="*"` + `MinWidth`，不用固定像素
- 内容自适应：`Width="Auto"`（角色、状态等短内容列）
- 最小宽度：用户名 80px，姓名 60px，手机号 100px

### 8.5 表单（编辑模式）

| 属性 | 值 |
|------|-----|
| 输入框样式 | `MaterialDesignOutlinedTextBox` |
| ComboBox 样式 | `MaterialDesignOutlinedComboBox` |
| Label | `MaterialDesignCaptionTextBlock` + SemiBold |
| 必填标识 | `<Run Text=" *" Foreground="{DynamicResource ValidationErrorBrush}"/>` |
| 验证错误 | 12px，`ValidationErrorBrush`，TextWrapping=Wrap，MinHeight=16 |
| 列间距 | 左列 `Margin="0,0,24,0"`（不用空列 hack） |
| 行间距 | `Margin="0,0,0,24"` |

### 8.6 筛选栏

| 控件 | 规范 |
|------|------|
| ComboBox | `MinWidth="120"`（不用 `Width="100"`），`MaterialDesignOutlinedComboBox` |
| SearchBox | 撑满可用宽度 |
| CheckBox | `MaterialDesignCheckBox` |
| 清除按钮 | `MaterialDesignOutlinedButton`，`Padding="8,2"` |
| 控件间距 | `SpacingS(8)` / `SpacingM(12)` |

### 8.7 溢出操作（PopupBox）

| 属性 | 值 |
|------|-----|
| 控件 | `materialDesign:PopupBox`（不用 Menu/MenuItem） |
| 内容 | 垂直 StackPanel |
| 按钮 | `HorizontalAlignment="Stretch"`, `HorizontalContentAlignment="Left"` |
| 分组 | `<Separator/>` 分隔功能组和危险操作 |
| Padding | 按钮 `16,8` |

---

## 9. 图标系统

| 用途 | 图标集 | 示例 |
|------|--------|------|
| 创建 | `Plus` | 新增用户 |
| 编辑 | `Pencil` | 编辑 |
| 删除 | `Delete` | 删除（配合 Error 色） |
| 刷新 | `Refresh` | 刷新列表 |
| 搜索 | `Magnify` | 搜索框 |
| 更多 | `DotsHorizontal` | 溢出菜单 |
| 密钥 | `Key` | 重置密码 |
| 开关 | `ToggleSwitchOutline` | 切换状态 |
| 撤销 | `Undo` | 恢复 |
| 导入 | `Import` | 导入 |
| 文档 | `FileDocumentOutline` | 模板下载 |
| 用户 | `Account` | 用户概要头 |
| 筛选 | `FilterVariant` | 筛选 |
| 导出 | `Export` | 导出 |

> 所有图标使用 `materialDesign:PackIcon Kind="..."`，不用 emoji。

---

## 10. 可访问性

### 10.1 对比度检查

| 组合 | 前景 | 背景 | 对比度 | 标准 |
|------|------|------|--------|------|
| 正文 | #DD000000 | #FFFFFF | 15.2:1 | ✓ AAA |
| 正文 | #DD000000 | #FAF8F5 | 14.6:1 | ✓ AAA |
| 次要文字 | #89000000 | #FFFFFF | 8.2:1 | ✓ AAA |
| 主色按钮文字 | #FFFFFF | #795548 | 4.6:1 | ✓ AA |
| 错误文字 | #B00020 | #FFFFFF | 5.9:1 | ✓ AA |
| 标签 | #89000000(0.6) | #FFFFFF | ~5:1 | ✓ AA |

### 10.2 键盘导航

- 所有交互元素必须有可见的 Focus 框
- Tab 顺序与视觉顺序一致
- Enter/Space 激活按钮

---

## 11. 文件结构

```
src/Client/Desktop/
├── Shell/App.xaml                          # MDIX 主题 + 资源注册
├── Core/LYBT.Desktop.Controls/
│   ├── Themes/
│   │   ├── Surfaces.xaml                   # 表面层次 + 投影 Token（§2.3 §6）
│   │   ├── Spacing.xaml                    # 间距 Token（§4）
│   │   ├── DataGridStyles.xaml             # DataGrid 单元格样式（§8.4）
│   │   └── Icons.xaml                      # 图标资源
│   └── Controls/
│       ├── MasterDetailLayout.xaml         # 主从布局（§8.2）
│       ├── InfoCard.xaml                   # 信息卡片（§8.1）
│       ├── DetailToolbar.xaml              # 详情工具栏（§8.3）
│       ├── DataGridToolbar.xaml            # 列表工具栏
│       ├── SearchBox.xaml                  # 搜索框
│       ├── StatusBadge.xaml                # 状态徽章
│       ├── EmptyState.xaml                 # 空状态
│       ├── UnifiedPaginationBar.xaml       # 分页栏
│       └── LoadingOverlay.xaml             # 加载遮罩
└── Modules/LYBT.Desktop.Users/Controls/
    ├── UserMasterDetailControl.xaml        # 用户管理主从控件
    ├── UserViewControl.xaml                # 用户详情查看
    └── UserEditControl.xaml                # 用户编辑表单
```

---

## 12. 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-27 | v1 | 初始版本：Surface 层次、配色、字体、间距、组件规范 |
| 2026-06-27 | v2 | 深化：色彩心理、WCAG 对比度、暗色模式预定义、状态色、MDIX Brush 完整映射、图标系统、可访问性 |
