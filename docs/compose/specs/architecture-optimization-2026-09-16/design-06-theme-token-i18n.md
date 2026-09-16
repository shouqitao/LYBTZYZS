# 设计 06：L6 主题 Token 化 / 样式提取 / i18n

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：审计报告 L6-02 / L6-08 / L6-09 / L6-13 / L6-14 / L6-18

## 1. 问题描述（实测）

| 子项 | 现状 | 证据 |
|---|---|---|
| 硬编码色值 | 20+ 页 `Background="#FBF7F3"`（`SurfaceLevel0Brush` 已提供等价 token）；`HerbItemControl` `Foreground="Black"`；`RegistrationListView` `Background="White"`/`BorderBrush="#E9DFD7"`；`FormulaViewControl`/`MedicalCaseViewControl` `#E0F7FA`/`#5B8FA8` | 逐文件行号见审计报告 L6-02 |
| 重复样式 | `ValidationErrorMessageVisibleStyle` 5 份；`HerbTagStyle/NameStyle/DosageStyle` 2 份；`CompactSectionBorderStyle` 2 份；`Functions.xaml` 5 样式在 `ReceptionistHomeView`/`SysadminHomeView` 本地重定义（**改用硬编码色**） | 同上 L6-08 |
| 打印模板 | 4 份模板各自内联 ~7-10 个同名样式（仅字号/宽度不同）+ 重复标签文本 | `PrescriptionPrint*Template.xaml` ×4 |
| i18n | `Shell/Resources/Strings/StringResources.resx` 已存在，但 **XAML 0 处引用**；中文全部硬编码（`DataGridToolbar` 6 处、`LoginView` 19+、4 打印模板各 15+、`SysadminHomeView` 15+） | L6-14 |
| 可访问性 | `AutomationProperties` 全仓 **1 处** | `LoginView.xaml:199` |
| 死资源 | `Spacing.xaml`（7 键 0 引用，仍被 `App.xaml` 合并）；`Icons.xaml` 5 个未用几何；`Surfaces.xaml` 5 个未用键；`Converters.xaml` 9/10 键无引用 | L6-18 |

## 2. 设计方案

### 2.1 主题 Token 化（先做，零语义风险）
- 字面量 → 既有 token：`#FBF7F3` → `{DynamicResource SurfaceLevel0Brush}`；`White` → `{DynamicResource MaterialDesign.Brush.Background}`；`#E9DFD7` → `{DynamicResource MaterialDesign.Brush.Outline}`；`HerbItemControl` 的 `Foreground="Black"` → `{DynamicResource MaterialDesign.Brush.Foreground}`。
- 逐文件替换 + 全仓 grep 复核「改动文件内无遗留字面量色值」。

### 2.2 样式提取
- 新增并合并进 `App.xaml`：
  - `Core/LYBT.Desktop.Controls/Themes/ValidationStyles.xaml`（`ValidationErrorMessageVisibleStyle` + 编辑控件共用变体）
  - `Core/LYBT.Desktop.Controls/Themes/TcmTags.xaml`（`HerbTagStyle` / `HerbTagNameStyle` / `HerbTagDosageStyle` / `CompactSectionBorderStyle`）
  - `Core/LYBT.Desktop.Controls/Themes/PrintStyles.xaml`（4 个打印模板的公共样式，参数化字号/宽度）
- 删除 5 处本地重定义；`ReceptionistHomeView`/`SysadminHomeView` 改用 `Functions.xaml` 全局样式。

### 2.3 i18n
- **必须先补键再替换**：`{x:Static}` 引用缺失键不会抛异常，只会返回空串（静默空白），故顺序不可颠倒。
- 键分区：`Common.` / `Login.` / `Grid.` / `Print.` / `Admin.`；先落地 3 批：`DataGridToolbar`、`LoginView`、4 个打印模板。
- VM 内中文提示暂不动（本轮只做 XAML 可见文本）。

### 2.4 可访问性
- 表单输入补 `AutomationProperties.Name`（复用其 TextBlock 标签文案）；图标按钮补 `AutomationProperties.Name`。

### 2.5 死资源清理
- 删 `Spacing.xaml` 及 `App.xaml` 合并项；删 `Icons.xaml` 未用几何；删 `Converters.xaml` 并把 `InputDialog.xaml:44` 改走 `Cvt` 静态门面。

## 3. 影响范围

82 个 XAML + `App.xaml` + `StringResources.resx`/`Designer.cs` + `Core/LYBT.Desktop.Controls/Themes/`。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| **无法做视觉验收**（本环境无 GUI） | **高** | 每步只做「等价替换」（token ↔ 同值字面量），并在报告中显式标注未做目视验证 |
| `x:Static` 缺键 → 静默空白 | **高** | 先补 key 再替换；替换后 grep 校验每个 `StringResources.X` 在 `.resx` 中存在 |
| 样式提取改变视觉层级（`BasedOn`/`TargetType` 丢失） | 中 | 提取时保持 `TargetType` 与 `BasedOn` 链；本地重定义删除前逐字段比对 |
| 打印模板字号参数化改变版式 | 中 | 参数化只暴露「已存在的差异维度」（字号/宽度），默认值与现状逐一对应 |

## 5. 实施步骤

1. 2.5 死资源清理（最低风险，先做）。
2. 2.1 主题 Token 化。
3. 2.2 样式提取。
4. 2.3 i18n（先 `.resx` 补键）。
5. 2.4 可访问性。
6. 每步 build；最终 build + Architecture + 定向 UI 测试（`ShellViewMappings` 守卫等）。
