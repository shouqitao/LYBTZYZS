# migrate-to-handycontrol Tasks

## Phase 1: HandyControl集成与TCM主题配置

- [x] 1.1 安装HandyControl NuGet包到Infrastructure项目
- [x] 1.2 重写TCM.Theme.xaml（仅HandyControl标准键，无兼容别名）
- [x] 1.3 配置App.xaml引入HandyControl Skin
  - 添加 `pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml`
  - 添加 `pack://application:,,,/HandyControl;component/Themes/Theme.xaml`
  - 添加 TCM.Theme.xaml（在HC之后覆盖配色）
- [x] 1.4 编译验证基础配置

## Phase 2: 资源键全局替换

### 2.1 品牌色替换
- [x] `BrandPrimaryBrush` → `PrimaryBrush`
- [x] `BrandPrimaryHoverBrush` → `DarkPrimaryBrush`
- [x] `BrandPrimaryPressedBrush` → `DarkPrimaryBrush`
- [x] `BrandPrimaryLightBrush` → `LightPrimaryBrush`
- [x] `BrandAccentBrush` → `AccentBrush`
- [x] `BrandAccentHoverBrush` → `DarkAccentBrush`
- [x] `BrandAccentLightBrush` → 移除或映射

### 2.2 语义色替换
- [x] `SemanticSuccessBrush` → `SuccessBrush`
- [x] `SemanticSuccessHoverBrush` → `DarkSuccessBrush`
- [x] `SemanticSuccessLightBrush` → `LightSuccessBrush`
- [x] `SemanticWarningBrush` → `WarningBrush`
- [x] `SemanticWarningHoverBrush` → `DarkWarningBrush`
- [x] `SemanticWarningLightBrush` → `LightWarningBrush`
- [x] `SemanticErrorBrush` → `DangerBrush`
- [x] `SemanticErrorHoverBrush` → `DarkDangerBrush`
- [x] `SemanticErrorLightBrush` → `LightDangerBrush`
- [x] `SemanticInfoBrush` → `InfoBrush`
- [x] `SemanticInfoHoverBrush` → `DarkInfoBrush`
- [x] `SemanticInfoLightBrush` → `LightInfoBrush`

### 2.3 文本色替换
- [x] `TextPrimaryBrush` → `PrimaryTextBrush`
- [x] `TextSecondaryBrush` → `SecondaryTextBrush`
- [x] `TextTertiaryBrush` → `ThirdlyTextBrush`
- [x] `TextDisabledBrush` → 使用HC Opacity或自定义
- [x] `TextOnBrandBrush` → `TextIconBrush`

### 2.4 表面色替换
- [x] `SurfaceBackgroundBrush` → `RegionBrush`
- [x] `SurfaceCardBrush` → `SecondaryRegionBrush`
- [x] `SurfaceCardHoverBrush` → `ThirdlyRegionBrush`
- [x] `SurfaceOverlayBrush` → `DarkOpacityBrush`

### 2.5 边框色替换
- [x] `BorderDefaultBrush` → `BorderBrush`
- [x] `BorderStrongBrush` → `SecondaryBorderBrush`
- [x] `BorderFocusBrush` → `PrimaryBrush`
- [x] `BorderDividerBrush` → `SecondaryBorderBrush`

### 2.6 状态色替换
- [x] `StateHoverBrush` → `SecondaryRegionBrush`
- [x] `StatePressedBrush` → `ThirdlyRegionBrush`
- [x] `StateSelectedBrush` → `LightPrimaryBrush`
- [x] `StateSelectedHoverBrush` → `LightPrimaryBrush`
- [x] `StateDisabledBrush` → 使用HC内置

### 2.7 DataGrid色替换
- [x] `DataGridAlternateRowBrush` → `SecondaryRegionBrush`
- [x] `DataGridHoverBrush` → `RegionBrush`
- [x] `DataGridSelectedBrush` → `LightPrimaryBrush`

### 2.8 UI元素色替换
- [x] `UISecondaryBrush` → `InfoBrush`
- [x] `UISecondaryHoverBrush` → `DarkInfoBrush`
- [x] `UINeutralBrush` → `SecondaryTextBrush`
- [x] `UINeutralHoverBrush` → `PrimaryTextBrush`
- [x] `UINeutralLightBrush` → `ThirdlyTextBrush`
- [x] `NeutralBrush` → `SecondaryTextBrush`
- [x] `NeutralLightBrush` → `ThirdlyTextBrush`

### 2.9 状态指示色替换
- [x] `StatusCompleteBrush` → `SuccessBrush`
- [x] `StatusPendingBrush` → `SecondaryTextBrush`
- [x] `StatusInProgressBrush` → `WarningBrush`

### 2.10 侧边栏色重命名
- [x] `SidebarBackgroundBrush` → `SidebarBrush`（TCM.Theme.xaml中定义）

### 2.11 编译验证
- [x] 执行全量编译验证

## Phase 3: 核心控件迁移

### 3.1 Infrastructure/Controls
- [x] MasterDetailLayout.xaml
- [x] InfoCard.xaml
- [x] StatusBadge.xaml
- [x] SearchBox.xaml
- [x] DataGridToolbar.xaml
- [x] DetailToolbar.xaml
- [x] EmptyState.xaml
- [x] PatientInfoCardControl.xaml
- [x] PatientSearchControl.xaml
- [x] PendingQueueControl.xaml
- [x] SidebarControl.xaml（保留Sidebar自定义色）
- [x] LoadingOverlay.xaml
- [x] GlobalStatusBar.xaml
- [x] UnifiedManagementToolBar.xaml
- [x] UnifiedManagementTable.xaml
- [x] UnifiedPaginationBar.xaml
- [x] CardReaderStatusControl.xaml

### 3.2 Infrastructure/Views
- [x] BaseDetailContainer.xaml
- [x] BaseMasterDataListView.xaml
- [x] UnfinishedCaseDialog.xaml

### 3.3 Shell控件
- [x] Shell/Controls/AccountSettingsControl.xaml (40处引用)
- [x] Shell/Views/MainWindow.xaml
- [x] Shell/Styles/Controls.xaml
- [x] Shell/Styles/CommonStyles.xaml
- [x] Resources/Dictionaries/IconResources.xaml

### 3.4 编译验证
- [x] 编译验证核心控件迁移

## Phase 4: 业务模块控件适配

### 4.1 Patients模块
- [x] PatientMasterDetailControl.xaml
- [x] PatientEditControl.xaml
- [x] PatientViewControl.xaml

### 4.2 MedicalCase模块
- [x] MedicalCaseMasterDetailControl.xaml
- [x] MedicalCaseEditControl.xaml
- [x] MedicalCaseViewControl.xaml
- [x] Dialogs/HistoryCopyDialog.xaml
- [x] Dialogs/FormulaImportDialog.xaml

### 4.3 Herbs模块
- [x] HerbMasterDetailControl.xaml
- [x] HerbEditControl.xaml
- [x] HerbViewControl.xaml

### 4.4 Formula模块
- [x] FormulaMasterDetailControl.xaml
- [x] FormulaEditControl.xaml
- [x] FormulaViewControl.xaml

### 4.5 Users模块
- [x] UserMasterDetailControl.xaml
- [x] UserEditControl.xaml
- [x] UserViewControl.xaml

### 4.6 Clinical角色视图
- [x] ClinicalHomeView.xaml
- [x] PatientSelectionView.xaml
- [x] MedicalCaseWorkspaceView.xaml

### 4.7 Admin角色视图
- [x] AdminHomeView.xaml
- [x] SystemSettingsView.xaml

### 4.8 编译验证
- [x] 编译验证业务模块和角色视图

## Phase 5: 清理遗留文件

- [x] 5.1 删除 `DesignTokens/Colors.Light.xaml`
- [x] 5.2 删除 `DesignTokens/Typography.xaml`
- [x] 5.3 更新 `Theme.Light.xaml`（仅保留Spacing.xaml合并）
- [x] 5.4 简化 `UnifiedComponents.xaml`（移除Colors.Light.xaml合并）
- [x] 5.5 保留 `Shell/Styles/Typography.xaml`（仍被App.xaml引用）
- [x] 5.6 保留 `Shell/Styles/Controls.xaml`（仍被App.xaml引用）
- [x] 5.7 删除 `Shell/Styles/Theme.Light.xaml`（已废弃）
- [x] 5.8 最终编译验证

## Validation Checklist

- [x] Desktop解决方案编译通过（0 errors）
- [x] Master-Detail布局点击操作正常（无崩溃）
- [x] 各模块CRUD功能正常
- [x] 侧边栏导航正常
- [x] 登录/登出功能正常
- [x] TCM主题配色符合预期
- [x] 无DependencyProperty.UnsetValue错误

---

**创建时间**: 2026-01-22
**完成时间**: 2026-01-22
**状态**: Completed ✅
