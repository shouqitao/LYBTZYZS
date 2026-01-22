# migrate-to-handycontrol Tasks

## Phase 1: HandyControl集成与TCM主题配置

- [x] 1.1 安装HandyControl NuGet包到Infrastructure项目
- [x] 1.2 重写TCM.Theme.xaml（仅HandyControl标准键，无兼容别名）
- [ ] 1.3 配置App.xaml引入HandyControl Skin
  - 添加 `pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml`
  - 添加 `pack://application:,,,/HandyControl;component/Themes/Theme.xaml`
  - 添加 TCM.Theme.xaml（在HC之后覆盖配色）
- [ ] 1.4 编译验证基础配置

## Phase 2: 资源键全局替换

### 2.1 品牌色替换
- [ ] `BrandPrimaryBrush` → `PrimaryBrush`
- [ ] `BrandPrimaryHoverBrush` → `DarkPrimaryBrush`
- [ ] `BrandPrimaryPressedBrush` → `DarkPrimaryBrush`
- [ ] `BrandPrimaryLightBrush` → `LightPrimaryBrush`
- [ ] `BrandAccentBrush` → `AccentBrush`
- [ ] `BrandAccentHoverBrush` → `DarkAccentBrush`
- [ ] `BrandAccentLightBrush` → 移除或映射

### 2.2 语义色替换
- [ ] `SemanticSuccessBrush` → `SuccessBrush`
- [ ] `SemanticSuccessHoverBrush` → `DarkSuccessBrush`
- [ ] `SemanticSuccessLightBrush` → `LightSuccessBrush`
- [ ] `SemanticWarningBrush` → `WarningBrush`
- [ ] `SemanticWarningHoverBrush` → `DarkWarningBrush`
- [ ] `SemanticWarningLightBrush` → `LightWarningBrush`
- [ ] `SemanticErrorBrush` → `DangerBrush`
- [ ] `SemanticErrorHoverBrush` → `DarkDangerBrush`
- [ ] `SemanticErrorLightBrush` → `LightDangerBrush`
- [ ] `SemanticInfoBrush` → `InfoBrush`
- [ ] `SemanticInfoHoverBrush` → `DarkInfoBrush`
- [ ] `SemanticInfoLightBrush` → `LightInfoBrush`

### 2.3 文本色替换
- [ ] `TextPrimaryBrush` → `PrimaryTextBrush`
- [ ] `TextSecondaryBrush` → `SecondaryTextBrush`
- [ ] `TextTertiaryBrush` → `ThirdlyTextBrush`
- [ ] `TextDisabledBrush` → 使用HC Opacity或自定义
- [ ] `TextOnBrandBrush` → `TextIconBrush`

### 2.4 表面色替换
- [ ] `SurfaceBackgroundBrush` → `RegionBrush`
- [ ] `SurfaceCardBrush` → `SecondaryRegionBrush`
- [ ] `SurfaceCardHoverBrush` → `ThirdlyRegionBrush`
- [ ] `SurfaceOverlayBrush` → `DarkOpacityBrush`

### 2.5 边框色替换
- [ ] `BorderDefaultBrush` → `BorderBrush`
- [ ] `BorderStrongBrush` → `SecondaryBorderBrush`
- [ ] `BorderFocusBrush` → `PrimaryBrush`
- [ ] `BorderDividerBrush` → `SecondaryBorderBrush`

### 2.6 状态色替换
- [ ] `StateHoverBrush` → `SecondaryRegionBrush`
- [ ] `StatePressedBrush` → `ThirdlyRegionBrush`
- [ ] `StateSelectedBrush` → `LightPrimaryBrush`
- [ ] `StateSelectedHoverBrush` → `LightPrimaryBrush`
- [ ] `StateDisabledBrush` → 使用HC内置

### 2.7 DataGrid色替换
- [ ] `DataGridAlternateRowBrush` → `SecondaryRegionBrush`
- [ ] `DataGridHoverBrush` → `RegionBrush`
- [ ] `DataGridSelectedBrush` → `LightPrimaryBrush`

### 2.8 UI元素色替换
- [ ] `UISecondaryBrush` → `InfoBrush`
- [ ] `UISecondaryHoverBrush` → `DarkInfoBrush`
- [ ] `UINeutralBrush` → `SecondaryTextBrush`
- [ ] `UINeutralHoverBrush` → `PrimaryTextBrush`
- [ ] `UINeutralLightBrush` → `ThirdlyTextBrush`
- [ ] `NeutralBrush` → `SecondaryTextBrush`
- [ ] `NeutralLightBrush` → `ThirdlyTextBrush`

### 2.9 状态指示色替换
- [ ] `StatusCompleteBrush` → `SuccessBrush`
- [ ] `StatusPendingBrush` → `SecondaryTextBrush`
- [ ] `StatusInProgressBrush` → `WarningBrush`

### 2.10 侧边栏色重命名
- [ ] `SidebarBackgroundBrush` → `SidebarBrush`（TCM.Theme.xaml中定义）

### 2.11 编译验证
- [ ] 执行全量编译验证

## Phase 3: 核心控件迁移

### 3.1 Infrastructure/Controls
- [ ] MasterDetailLayout.xaml
- [ ] InfoCard.xaml
- [ ] StatusBadge.xaml
- [ ] SearchBox.xaml
- [ ] DataGridToolbar.xaml
- [ ] DetailToolbar.xaml
- [ ] EmptyState.xaml
- [ ] PatientInfoCardControl.xaml
- [ ] PatientSearchControl.xaml
- [ ] PendingQueueControl.xaml
- [ ] SidebarControl.xaml（保留Sidebar自定义色）
- [ ] LoadingOverlay.xaml
- [ ] GlobalStatusBar.xaml
- [ ] UnifiedManagementToolBar.xaml
- [ ] UnifiedManagementTable.xaml
- [ ] UnifiedPaginationBar.xaml
- [ ] CardReaderStatusControl.xaml

### 3.2 Infrastructure/Views
- [ ] BaseDetailContainer.xaml
- [ ] BaseMasterDataListView.xaml
- [ ] UnfinishedCaseDialog.xaml

### 3.3 Shell控件
- [ ] Shell/Controls/AccountSettingsControl.xaml (40处引用)
- [ ] Shell/Views/MainWindow.xaml
- [ ] Shell/Styles/Controls.xaml
- [ ] Shell/Styles/CommonStyles.xaml
- [ ] Resources/Dictionaries/IconResources.xaml

### 3.4 编译验证
- [ ] 编译验证核心控件迁移

## Phase 4: 业务模块控件适配

### 4.1 Patients模块
- [ ] PatientMasterDetailControl.xaml
- [ ] PatientEditControl.xaml
- [ ] PatientViewControl.xaml

### 4.2 MedicalCase模块
- [ ] MedicalCaseMasterDetailControl.xaml
- [ ] MedicalCaseEditControl.xaml
- [ ] MedicalCaseViewControl.xaml
- [ ] Dialogs/HistoryCopyDialog.xaml
- [ ] Dialogs/FormulaImportDialog.xaml

### 4.3 Herbs模块
- [ ] HerbMasterDetailControl.xaml
- [ ] HerbEditControl.xaml
- [ ] HerbViewControl.xaml

### 4.4 Formula模块
- [ ] FormulaMasterDetailControl.xaml
- [ ] FormulaEditControl.xaml
- [ ] FormulaViewControl.xaml

### 4.5 Users模块
- [ ] UserMasterDetailControl.xaml
- [ ] UserEditControl.xaml
- [ ] UserViewControl.xaml

### 4.6 Clinical角色视图
- [ ] ClinicalHomeView.xaml
- [ ] PatientSelectionView.xaml
- [ ] MedicalCaseWorkspaceView.xaml

### 4.7 Admin角色视图
- [ ] AdminHomeView.xaml
- [ ] SystemSettingsView.xaml

### 4.8 编译验证
- [ ] 编译验证业务模块和角色视图

## Phase 5: 清理遗留文件

- [ ] 5.1 删除 `DesignTokens/Colors.Light.xaml`
- [ ] 5.2 删除 `DesignTokens/Typography.xaml`
- [ ] 5.3 更新 `Theme.Light.xaml`（仅保留Spacing.xaml合并）
- [ ] 5.4 简化 `UnifiedComponents.xaml`（移除Colors.Light.xaml合并）
- [ ] 5.5 删除 `Shell/Styles/Typography.xaml`
- [ ] 5.6 删除 `Shell/Styles/Controls.xaml`
- [ ] 5.7 删除 `Shell/Styles/CommonStyles.xaml`（如存在）
- [ ] 5.8 最终编译验证

## Validation Checklist

- [ ] Desktop解决方案编译通过（0 errors）
- [ ] Master-Detail布局点击操作正常（无崩溃）
- [ ] 各模块CRUD功能正常
- [ ] 侧边栏导航正常
- [ ] 登录/登出功能正常
- [ ] TCM主题配色符合预期
- [ ] 无DependencyProperty.UnsetValue错误

## 资源键映射速查表

### 品牌色
| 旧键 | 新键 |
|------|------|
| `BrandPrimaryBrush` | `PrimaryBrush` |
| `BrandPrimaryHoverBrush` | `DarkPrimaryBrush` |
| `BrandPrimaryPressedBrush` | `DarkPrimaryBrush` |
| `BrandPrimaryLightBrush` | `LightPrimaryBrush` |
| `BrandAccentBrush` | `AccentBrush` |
| `BrandAccentHoverBrush` | `DarkAccentBrush` |
| `BrandAccentLightBrush` | 移除 |

### 语义色
| 旧键 | 新键 |
|------|------|
| `SemanticSuccessBrush` | `SuccessBrush` |
| `SemanticSuccessHoverBrush` | `DarkSuccessBrush` |
| `SemanticSuccessLightBrush` | `LightSuccessBrush` |
| `SemanticWarningBrush` | `WarningBrush` |
| `SemanticWarningHoverBrush` | `DarkWarningBrush` |
| `SemanticWarningLightBrush` | `LightWarningBrush` |
| `SemanticErrorBrush` | `DangerBrush` |
| `SemanticErrorHoverBrush` | `DarkDangerBrush` |
| `SemanticErrorLightBrush` | `LightDangerBrush` |
| `SemanticInfoBrush` | `InfoBrush` |
| `SemanticInfoHoverBrush` | `DarkInfoBrush` |
| `SemanticInfoLightBrush` | `LightInfoBrush` |

### 文本色
| 旧键 | 新键 |
|------|------|
| `TextPrimaryBrush` | `PrimaryTextBrush` |
| `TextSecondaryBrush` | `SecondaryTextBrush` |
| `TextTertiaryBrush` | `ThirdlyTextBrush` |
| `TextDisabledBrush` | `ThirdlyTextBrush` |
| `TextOnBrandBrush` | `TextIconBrush` |

### 表面色
| 旧键 | 新键 |
|------|------|
| `SurfaceBackgroundBrush` | `RegionBrush` |
| `SurfaceCardBrush` | `SecondaryRegionBrush` |
| `SurfaceCardHoverBrush` | `ThirdlyRegionBrush` |
| `SurfaceOverlayBrush` | `DarkOpacityBrush` |

### 边框色
| 旧键 | 新键 |
|------|------|
| `BorderDefaultBrush` | `BorderBrush` |
| `BorderStrongBrush` | `SecondaryBorderBrush` |
| `BorderFocusBrush` | `PrimaryBrush` |
| `BorderDividerBrush` | `SecondaryBorderBrush` |

### 状态色
| 旧键 | 新键 |
|------|------|
| `StateHoverBrush` | `SecondaryRegionBrush` |
| `StatePressedBrush` | `ThirdlyRegionBrush` |
| `StateSelectedBrush` | `LightPrimaryBrush` |
| `StateSelectedHoverBrush` | `LightPrimaryBrush` |
| `StateDisabledBrush` | `SecondaryRegionBrush` |

### DataGrid色
| 旧键 | 新键 |
|------|------|
| `DataGridAlternateRowBrush` | `SecondaryRegionBrush` |
| `DataGridHoverBrush` | `RegionBrush` |
| `DataGridSelectedBrush` | `LightPrimaryBrush` |

### UI元素色
| 旧键 | 新键 |
|------|------|
| `UISecondaryBrush` | `InfoBrush` |
| `UISecondaryHoverBrush` | `DarkInfoBrush` |
| `UINeutralBrush` | `SecondaryTextBrush` |
| `UINeutralHoverBrush` | `PrimaryTextBrush` |
| `UINeutralLightBrush` | `ThirdlyTextBrush` |
| `NeutralBrush` | `SecondaryTextBrush` |
| `NeutralLightBrush` | `ThirdlyTextBrush` |

### 状态指示色
| 旧键 | 新键 |
|------|------|
| `StatusCompleteBrush` | `SuccessBrush` |
| `StatusPendingBrush` | `SecondaryTextBrush` |
| `StatusInProgressBrush` | `WarningBrush` |

### 侧边栏色 (保留项目特有定义)
| 旧键 | 新键 |
|------|------|
| `SidebarBackgroundBrush` | `SidebarBrush` |
| `SidebarHoverBrush` | `SidebarHoverBrush` (保留) |
| `SidebarTextBrush` | `SidebarTextBrush` (保留) |
| `SidebarTextSecondaryBrush` | `SidebarTextSecondaryBrush` (保留) |
| `SidebarAvatarBrush` | `SidebarAvatarBrush` (保留) |
| `SidebarDividerBrush` | `SidebarDividerBrush` (保留) |

---

**创建时间**: 2026-01-22
**状态**: Draft
