# Shell 主题体系重构设计文档

> 版本: v1.0 | 日期: 2026-06-26 | 状态: 待审批

---

## [S1] 问题陈述

### 1.1 现状

Shell 主题体系存在以下技术债务：

| 问题 | 严重度 | 影响范围 |
|------|--------|---------|
| 幽灵画刷（132+ 处引用未定义） | **高** | 全部角色视图 |
| 双 token 命名体系 | 中 | DesignSystem.xaml |
| MDIX 使用风格不一致 | 中 | 全部角色视图 |
| Sysadmin 暗色主题全局影响 | **高** | 可能破坏其他角色 |
| Typography.xaml 全局覆盖 | 低 | 全局 TextBlock |
| 注释残留 HandyControl 引用 | 低 | UnifiedComponents.xaml |

### 1.2 幽灵画刷详情

以下画刷在整个代码库中被大量引用，但**在任何 XAML 文件中都没有定义**：

| 幽灵画刷 | 引用文件数 | 预计修改点 |
|----------|-----------|-----------|
| `SecondaryRegionBrush` | ~15 文件 | ~50 处 |
| `ThirdlyTextBrush` | ~8 文件 | ~15 处 |
| `SecondaryBorderBrush` | ~10 文件 | ~20 处 |
| `RegionBrush` | ~10 文件 | ~15 处 |
| `DisabledBackgroundBrush` | ~3 文件 | ~5 处 |
| `PrimaryRegionBrush` | ~1 文件 | ~2 处 |
| `DataGridAlternatingRowBrush` | ~1 文件 | ~1 处 |
| **合计** | **~30 文件** | **~108 处** |

这些画刷在运行时 `DynamicResource` 绑定会静默失败（返回 null），导致相关 UI 元素缺少背景色/前景色。

### 1.3 双 Token 命名详情

DesignSystem.xaml 中存在重复的 token 定义：

| 新命名 | 旧命名 | 值相同 | 引用文件数 |
|--------|--------|--------|-----------|
| `SpacingXS` / `SpacingSM` / `SpacingMD` | `SpacingXSmall` / `SpacingSmall` / `SpacingMedium` | 是 | ~5 文件 |
| `SpacingLG` / `SpacingXL` / `SpacingXXL` | `SpacingLarge` / `SpacingXLarge` / `SpacingXXLarge` | 是 | ~3 文件 |
| `RadiusSM` / `RadiusMD` / `RadiusLG` | `CornerRadiusSmall` / `CornerRadiusMedium` / `CornerRadiusLarge` | 是 | ~6 文件 |
| **合计** | | | **~10 文件, ~35 处** |

---

## [S2] 设计目标

### 2.1 核心原则

1. **MDIX 优先** - 所有主题资源使用 MDIX 原生键
2. **单一来源** - Token 命名统一，消除重复
3. **Dark 主题自动适配** - 所有视图使用 `DynamicResource`，Dark 切换自动生效
4. **清理技术债务** - 移除幽灵画刷引用和 HandyControl 残留

### 2.2 验收标准

| 目标 | 验收标准 |
|------|---------|
| 幽灵画刷清理 | 所有幽灵画刷引用替换为 MDIX 原生资源 |
| Token 命名统一 | 只保留新命名（SpacingXS/SM/MD/LG/XL），旧命名引用清零 |
| MDIX 使用统一 | 所有视图直接使用 MDIX 原生键 |
| Dark 主题切换 | Sysadmin 切换 Dark 后，其他角色视图自动适配 |
| 注释清理 | HandyControl 引用全部移除 |

---

## [S3] 详细设计

### 3.1 幽灵画刷清理

**方案：** 在所有引用幽灵画刷的地方，替换为对应的 MDIX 原生资源。

| 幽灵画刷 | 替换为 | MDIX 资源键 |
|----------|--------|------------|
| `RegionBrush` | 窗口/页面背景 | `MaterialDesignPaper` |
| `SecondaryRegionBrush` | 卡片/面板背景 | `MaterialDesignBody` |
| `SecondaryBorderBrush` | 边框 | `MaterialDesignOutline` |
| `ThirdlyTextBrush` | 次要文字 | `MaterialDesignBodyLight` |
| `DisabledBackgroundBrush` | 禁用背景 | `MaterialDesignDivider` |
| `DataGridAlternatingRowBrush` | 交替行 | `MaterialDesignBody` + 透明度 |
| `PrimaryRegionBrush` | 主色背景 | `MaterialDesignBrus Primary` |

**实施步骤：**
1. 使用 Grep 搜索所有幽灵画刷引用
2. 逐个替换为 MDIX 原生资源
3. 验证编译通过

### 3.2 Token 命名统一

**方案：** 保留新命名（`SpacingXS/SM/MD/LG/XL`、`RadiusSM/MD/LG`），清理旧命名引用。

**保留的 Token：**
- `SpacingXS` (4) / `SpacingSM` (8) / `SpacingMD` (12) / `SpacingLG` (16) / `SpacingXL` (24) / `SpacingXXL` (32) / `SpacingXXXL` (48)
- `RadiusSM` (4) / `RadiusMD` (8) / `RadiusLG` (12)

**清理的 Token：**
- `SpacingXSmall` / `SpacingSmall` / `SpacingMedium` / `SpacingLarge` / `SpacingXLarge` / `SpacingXXLarge`
- `CornerRadiusSmall` / `CornerRadiusMedium` / `CornerRadiusLarge`

**实施步骤：**
1. 在 DesignSystem.xaml 中删除旧命名定义
2. 搜索所有引用旧命名的地方，替换为新命名
3. 验证编译通过

### 3.3 MDIX 使用统一

**方案：** 所有视图直接使用 MDIX 原生资源键，不再通过自定义 token 间接引用。

**MDIX 资源键映射：**

| 用途 | MDIX 资源键 |
|------|------------|
| 窗口/页面背景 | `MaterialDesignPaper` |
| 卡片/面板背景 | `MaterialDesignBody` |
| 主要文字 | `MaterialDesignBody` |
| 次要文字 | `MaterialDesignBodyLight` |
| 边框 | `MaterialDesignOutline` |
| 主色 | `MaterialDesignBrus Primary` |
| 强调色 | `MaterialDesignFlatButton.ClickThrough` |
| 成功色 | `MaterialDesignBrush.Success` |
| 危险色 | `MaterialDesignBrush.Error` |
| 警告色 | `MaterialDesignBrush.Warning` |

**实施步骤：**
1. 搜索所有角色视图中的自定义 token 引用
2. 替换为 MDIX 原生资源键
3. 验证编译通过

### 3.4 Dark 主题自动适配

**方案：** 所有视图使用 `DynamicResource` 引用颜色/画刷，确保 Dark 切换时自动更新。

**关键原则：**
- 颜色/画刷必须使用 `DynamicResource`（非 `StaticResource`）
- 硬编码颜色（如 `Background="White"`）必须改为 `DynamicResource`
- Sysadmin Dark 切换通过 `ThemeService.ApplyTheme(true)` 全局生效

**实施步骤：**
1. 搜索所有角色视图中的硬编码颜色
2. 替换为 `DynamicResource` 引用
3. 验证 Dark/Light 切换正常

### 3.5 注释残留清理

**方案：** 清理 `UnifiedComponents.xaml` 和其他文件中的 HandyControl 引用注释。

**实施步骤：**
1. 搜索所有 HandyControl 相关注释
2. 更新为 MDIX 描述或删除

---

## [S4] 影响范围

### 4.1 需要修改的文件

**Controls 层（~15 文件）：**
- `DesignSystem.xaml` - 清理旧 token 定义
- `UnifiedComponents.xaml` - 清理 HandyControl 注释
- `Typography.xaml` - 清理旧 token 引用
- `Controls.xaml` - 替换幽灵画刷
- `HomePageStyles.xaml` - 替换幽灵画刷
- `ButtonStyles.xaml` - 替换幽灵画刷 + 旧 token
- `InputStyles.xaml` - 替换幽灵画刷 + 旧 token
- `PanelStyles.xaml` - 替换幽灵画刷 + 旧 token
- `DataGridStyles.xaml` - 替换幽灵画刷
- `MedicalCaseStyles.xaml` - 替换幽灵画刷

**Controls 控件（~10 文件）：**
- `BaseDetailContainer.xaml` - 替换幽灵画刷
- `BreadcrumbBar.xaml` - 替换幽灵画刷
- `BreadcrumbControl.xaml` - 替换幽灵画刷
- `CardReaderStatusControl.xaml` - 替换幽灵画刷
- `DataGridToolbar.xaml` - 替换幽灵画刷
- `DetailToolbar.xaml` - 替换幽灵画刷
- `EmptyState.xaml` - 替换幽灵画刷
- `HerbListControl.xaml` - 替换幽灵画刷
- `InfoCard.xaml` - 替换幽灵画刷
- `MasterDetailLayout.xaml` - 替换幽灵画刷
- `PatientInfoCardControl.xaml` - 替换幽灵画刷
- `SearchBox.xaml` - 替换幽灵画刷
- `UnifiedManagementTable.xaml` - 替换幽灵画刷
- `UnifiedManagementToolBar.xaml` - 替换旧 token
- `UnifiedPaginationBar.xaml` - 替换旧 token

**Shell 层（~5 文件）：**
- `MainWindow.xaml` - 替换幽灵画刷
- `Controls.xaml` - 替换幽灵画刷
- `Typography.xaml` - 替换幽灵画刷
- `AccountSettingsControl.xaml` - 替换幽灵画刷
- `UnfinishedCaseDialog.xaml` - 替换幽灵画刷

**角色视图（~10 文件）：**
- `AdminHomeView.xaml` - 替换幽灵画刷
- `SystemSettingsView.xaml` - 替换旧 token
- `ClinicalHomeView.xaml` - 替换幽灵画刷
- `ClinicalWorkspaceView.xaml` - 替换幽灵画刷
- `MedicalCaseWorkspaceView.xaml` - 替换幽灵画刷
- `PatientSelectionView.xaml` - 替换幽灵画刷
- `ReceptionistHomeView.xaml` - 替换幽灵画刷
- `ReportsHomeView.xaml` - 替换幽灵画刷

**Module 视图（~5 文件）：**
- `MedicalCaseEditControl.xaml` - 替换幽灵画刷
- `MedicalCaseViewControl.xaml` - 替换幽灵画刷
- `WorkflowStepIndicator.xaml` - 替换幽灵画刷
- `PatientEditControl.xaml` - 替换幽灵画刷

**Sysadmin（~3 文件）：**
- `SysadminDarkTheme.xaml` - 已迁移到 MDIX
- `SysadminHomeView.xaml` - 已迁移到 MDIX
- `LogLevelControlView.xaml` - 已迁移到 MDIX

### 4.2 不需要修改的文件

- `App.xaml` - 已正确配置 MDIX BundledTheme
- `ThemeService.cs` - 已正确使用 PaletteHelper
- `SysadminDarkTheme.xaml` - 已迁移到 MDIX 资源

---

## [S5] 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 幽灵画刷替换后样式变化 | UI 外观改变 | 逐个替换并验证 |
| Dark 主题切换影响其他角色 | UI 崩溃 | 使用 DynamicResource 确保自动适配 |
| 旧 token 清理遗漏 | 编译错误 | 全局搜索替换 |

---

## [S6] 变更日志

| 日期 | 版本 | 变更 | 作者 |
|------|------|------|------|
| 2026-06-26 | v1.0 | 初始设计 | MiMoCode |
