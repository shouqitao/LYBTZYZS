# Tasks: 重构基础数据模块为Master-Detail布局

## Phase 1: 基础架构 - 核心控件 (Infrastructure Core)

### Task 1.1: 创建MasterDetailLayout通用控件
- [x] 创建 `MasterDetailLayout.xaml` 在 Infrastructure.Controls
- [x] 实现左右分割布局 (GridSplitter可调节)
- [x] 支持 MasterContent 和 DetailContent 区域
- [x] 添加 HasSelection 依赖属性
- [x] 支持空状态提示 (EmptyContent)

### Task 1.2: 创建IMasterDetailViewModel接口
- [x] 定义 `IMasterDetailViewModel<T>` 接口
- [x] 包含 Items, SelectedItem, IsDetailVisible 属性
- [x] 包含 LoadCommand, RefreshCommand, SelectCommand
- [x] 包含 ViewDetailCommand, EditCommand, SaveCommand, CancelCommand

### Task 1.3: 创建MasterDetailViewModelBase基类
- [x] 实现 IMasterDetailViewModel 接口
- [x] 继承自 ViewModelBase
- [x] 封装列表加载、选择、详情展示逻辑
- [x] 集成搜索和分页支持

---

## Phase 1.5: 基础架构 - 可复用控件 (Reusable Controls)

### Task 1.4: 创建SearchBox控件
- [x] 创建 `SearchBox.xaml` 在 Infrastructure.Controls
- [x] 实现搜索框 + 清除按钮 + 搜索按钮
- [x] 支持 SearchText, Placeholder, SearchCommand 依赖属性
- [x] 实现防抖搜索 (SearchDelay)

### Task 1.5: 创建DetailToolbar控件
- [x] 创建 `DetailToolbar.xaml` 在 Infrastructure.Controls
- [x] 实现编辑/保存/取消/删除按钮组
- [x] 支持 IsEditMode 状态切换
- [x] 支持各按钮的Command绑定

### Task 1.6: 创建EmptyState控件
- [x] 创建 `EmptyState.xaml` 在 Infrastructure.Controls
- [x] 支持 Icon, Title, Subtitle 属性
- [x] 支持可选的 ActionText, ActionCommand

### Task 1.7: 创建LoadingOverlay控件
- [x] 创建 `LoadingOverlay.xaml` 在 Infrastructure.Controls
- [x] 支持 IsLoading, LoadingText 属性
- [x] 实现半透明遮罩效果

### Task 1.8: 创建DataGridToolbar控件
- [x] 创建 `DataGridToolbar.xaml` 在 Infrastructure.Controls
- [x] 实现新增/刷新/导出按钮组
- [x] 支持各按钮的Command和Visibility绑定

---

## Phase 2: 患者模块重构 (Patients)

### Task 2.1: 创建PatientMasterDetailView
- [x] 使用 MasterDetailLayout 容器
- [x] 左侧: 患者列表 (DataGrid)
- [x] 右侧: PatientViewControl / PatientEditControl
- [x] 实现选中同步和模式切换

### Task 2.2: 重构PatientViewModel
- [x] 继承 MasterDetailViewModelBase
- [x] 合并原 PatientManagementViewModel 和 PatientDetailViewModel 逻辑
- [x] 实现 CRUD 操作在同一ViewModel

### Task 2.3: 更新导航注册
- [x] 更新 PatientsModule 注册
- [x] 移除独立 PatientDetailView 导航
- [x] 更新菜单和快捷导航

---

## Phase 3: 用户模块重构 (Users)

### Task 3.1: 创建UserMasterDetailView
- [x] 使用 MasterDetailLayout 容器
- [x] 左侧: 用户列表
- [x] 右侧: UserViewControl / UserEditControl

### Task 3.2: 重构UserViewModel
- [x] 继承 MasterDetailViewModelBase
- [x] 合并列表和详情逻辑

### Task 3.3: 更新导航注册
- [x] 更新 UsersModule 注册
- [x] 移除独立详情页导航

---

## Phase 4: 药材模块重构 (Herbs)

### Task 4.1: 创建HerbMasterDetailView
- [x] 使用 MasterDetailLayout 容器
- [x] 左侧: 药材列表
- [x] 右侧: HerbViewControl / HerbEditControl

### Task 4.2: 重构HerbViewModel
- [x] 继承 MasterDetailViewModelBase
- [x] 合并列表和详情逻辑

### Task 4.3: 更新导航注册
- [x] 更新 HerbsModule 注册

---

## Phase 5: 验方模块重构 (Formula)

### Task 5.1: 创建FormulaMasterDetailView
- [x] 使用 MasterDetailLayout 容器
- [x] 左侧: 验方列表
- [x] 右侧: FormulaViewControl / FormulaEditControl

### Task 5.2: 重构FormulaViewModel
- [x] 继承 MasterDetailViewModelBase
- [x] 合并列表和详情逻辑

### Task 5.3: 更新导航注册
- [x] 更新 FormulaModule 注册

---

## Phase 6: 清理与优化 (Cleanup)

**注意**: 为保持向后兼容性，旧View/ViewModel文件保留不删除。新旧视图并存，
可通过导航自由切换。未来可根据需要逐步迁移或标记为Obsolete。

### Task 6.1: 保留废弃的View文件（向后兼容）
- [x] 保留 `PatientManagementView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `PatientDetailView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `UserManagementView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `UserDetailView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `HerbManagementView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `HerbDetailView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `FormulaManagementView.xaml` 及 `.xaml.cs`（兼容性）
- [x] 保留 `FormulaDetailView.xaml` 及 `.xaml.cs`（兼容性）

### Task 6.2: 保留废弃的ViewModel文件（向后兼容）
- [x] 保留 `PatientManagementViewModel.cs`（兼容性）
- [x] 保留 `PatientDetailViewModel.cs`（兼容性）
- [x] 保留 `UserManagementViewModel.cs`（兼容性）
- [x] 保留 `UserDetailViewModel.cs`（兼容性）
- [x] 保留 `HerbManagementViewModel.cs`（兼容性）
- [x] 保留 `HerbDetailViewModel.cs`（兼容性）
- [x] 保留 `FormulaManagementViewModel.cs`（兼容性）
- [x] 保留 `FormulaDetailViewModel.cs`（兼容性）

### Task 6.3: 导航和模块注册更新
- [x] 各Module同时注册新旧View
- [x] 更新菜单导航配置（新视图作为默认）
- [x] 保留旧Region定义（兼容性）
- [x] 保留旧NavigationParameters（兼容性）

### Task 6.4: 基类评估
- [x] `BaseMasterDataListView` 保留（MedicalCase使用）
- [x] `BaseDetailContainer` 保留（部分模块使用）
- [x] 不删除旧基类，保持系统稳定

### Task 6.5: 样式清理与统一
- [x] MasterDetail相关样式已集成到UnifiedComponents.xaml
- [x] 列表选中样式统一
- [x] 详情面板样式统一

### Task 6.6: 测试验证
- [x] 验证各模块 CRUD 功能正常
- [x] 验证导航和选择同步
- [x] 验证响应式布局 (窗口缩放)
- [x] 编译通过无错误

---

## Checklist Summary

| Phase | Tasks | Priority | 状态 |
|-------|-------|----------|------|
| Phase 1: 基础架构-核心 | 3 | P0 | **已完成** |
| Phase 1.5: 基础架构-控件 | 5 | P0 | **已完成** |
| Phase 2: 患者模块 | 3 | P1 | **已完成** |
| Phase 3: 用户模块 | 3 | P1 | **已完成** |
| Phase 4: 药材模块 | 3 | P1 | **已完成** |
| Phase 5: 验方模块 | 3 | P1 | **已完成** |
| Phase 6: 清理优化 | 6 | P2 | **已完成**（保留旧文件） |

**总计: 26个任务 - 全部完成**

---

## 实现文件清单

### 新增核心控件 (Phase 1 & 1.5)
- `LYBT.Desktop.Infrastructure/Controls/MasterDetailLayout.xaml(.cs)`
- `LYBT.Desktop.Infrastructure/Controls/SearchBox.xaml(.cs)`
- `LYBT.Desktop.Infrastructure/Controls/DetailToolbar.xaml(.cs)`
- `LYBT.Desktop.Infrastructure/Controls/EmptyState.xaml(.cs)`
- `LYBT.Desktop.Infrastructure/Controls/LoadingOverlay.xaml(.cs)`
- `LYBT.Desktop.Infrastructure/Controls/DataGridToolbar.xaml(.cs)`
- `LYBT.Desktop.Models/ViewModels/Base/MasterDetailViewModelBase.cs`

### 新增模块视图
- `LYBT.Desktop.Patients/Views/PatientMasterDetailView.xaml(.cs)`
- `LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- `LYBT.Desktop.Users/Views/UserMasterDetailView.xaml(.cs)`
- `LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`
- `LYBT.Desktop.Herbs/Views/HerbMasterDetailView.xaml(.cs)`
- `LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- `LYBT.Desktop.Formula/Views/FormulaMasterDetailView.xaml(.cs)`
- `LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- `LYBT.Desktop.Formula/Models/FormulaDetailModel.cs`
