# Tasks: 重构基础数据模块为Master-Detail布局

## Phase 1: 基础架构 - 核心控件 (Infrastructure Core)

### Task 1.1: 创建MasterDetailLayout通用控件
- [ ] 创建 `MasterDetailLayout.xaml` 在 Infrastructure.Controls
- [ ] 实现左右分割布局 (GridSplitter可调节)
- [ ] 支持 MasterContent 和 DetailContent 区域
- [ ] 添加 HasSelection 依赖属性
- [ ] 支持空状态提示 (EmptyContent)

### Task 1.2: 创建IMasterDetailViewModel接口
- [ ] 定义 `IMasterDetailViewModel<T>` 接口
- [ ] 包含 Items, SelectedItem, IsDetailVisible 属性
- [ ] 包含 LoadCommand, RefreshCommand, SelectCommand
- [ ] 包含 ViewDetailCommand, EditCommand, SaveCommand, CancelCommand

### Task 1.3: 创建MasterDetailViewModelBase基类
- [ ] 实现 IMasterDetailViewModel 接口
- [ ] 继承自 ViewModelBase
- [ ] 封装列表加载、选择、详情展示逻辑
- [ ] 集成搜索和分页支持

---

## Phase 1.5: 基础架构 - 可复用控件 (Reusable Controls)

### Task 1.4: 创建SearchBox控件
- [ ] 创建 `SearchBox.xaml` 在 Infrastructure.Controls
- [ ] 实现搜索框 + 清除按钮 + 搜索按钮
- [ ] 支持 SearchText, Placeholder, SearchCommand 依赖属性
- [ ] 实现防抖搜索 (SearchDelay)

### Task 1.5: 创建DetailToolbar控件
- [ ] 创建 `DetailToolbar.xaml` 在 Infrastructure.Controls
- [ ] 实现编辑/保存/取消/删除按钮组
- [ ] 支持 IsEditMode 状态切换
- [ ] 支持各按钮的Command绑定

### Task 1.6: 创建EmptyState控件
- [ ] 创建 `EmptyState.xaml` 在 Infrastructure.Controls
- [ ] 支持 Icon, Title, Subtitle 属性
- [ ] 支持可选的 ActionText, ActionCommand

### Task 1.7: 创建LoadingOverlay控件
- [ ] 创建 `LoadingOverlay.xaml` 在 Infrastructure.Controls
- [ ] 支持 IsLoading, LoadingText 属性
- [ ] 实现半透明遮罩效果

### Task 1.8: 创建DataGridToolbar控件
- [ ] 创建 `DataGridToolbar.xaml` 在 Infrastructure.Controls
- [ ] 实现新增/刷新/导出按钮组
- [ ] 支持各按钮的Command和Visibility绑定

---

## Phase 2: 患者模块重构 (Patients)

### Task 2.1: 创建PatientMasterDetailView
- [ ] 使用 MasterDetailLayout 容器
- [ ] 左侧: 患者列表 (DataGrid)
- [ ] 右侧: PatientViewControl / PatientEditControl
- [ ] 实现选中同步和模式切换

### Task 2.2: 重构PatientViewModel
- [ ] 继承 MasterDetailViewModelBase
- [ ] 合并原 PatientManagementViewModel 和 PatientDetailViewModel 逻辑
- [ ] 实现 CRUD 操作在同一ViewModel

### Task 2.3: 更新导航注册
- [ ] 更新 PatientsModule 注册
- [ ] 移除独立 PatientDetailView 导航
- [ ] 更新菜单和快捷导航

---

## Phase 3: 用户模块重构 (Users)

### Task 3.1: 创建UserMasterDetailView
- [ ] 使用 MasterDetailLayout 容器
- [ ] 左侧: 用户列表
- [ ] 右侧: UserViewControl / UserEditControl

### Task 3.2: 重构UserViewModel
- [ ] 继承 MasterDetailViewModelBase
- [ ] 合并列表和详情逻辑

### Task 3.3: 更新导航注册
- [ ] 更新 UsersModule 注册
- [ ] 移除独立详情页导航

---

## Phase 4: 药材模块重构 (Herbs)

### Task 4.1: 创建HerbMasterDetailView
- [ ] 使用 MasterDetailLayout 容器
- [ ] 左侧: 药材列表
- [ ] 右侧: HerbViewControl / HerbEditControl

### Task 4.2: 重构HerbViewModel
- [ ] 继承 MasterDetailViewModelBase
- [ ] 合并列表和详情逻辑

### Task 4.3: 更新导航注册
- [ ] 更新 HerbsModule 注册

---

## Phase 5: 验方模块重构 (Formula)

### Task 5.1: 创建FormulaMasterDetailView
- [ ] 使用 MasterDetailLayout 容器
- [ ] 左侧: 验方列表
- [ ] 右侧: FormulaViewControl / FormulaEditControl

### Task 5.2: 重构FormulaViewModel
- [ ] 继承 MasterDetailViewModelBase
- [ ] 合并列表和详情逻辑

### Task 5.3: 更新导航注册
- [ ] 更新 FormulaModule 注册

---

## Phase 6: 清理与优化 (Cleanup)

### Task 6.1: 删除废弃的View文件
- [ ] 删除 `PatientManagementView.xaml` 及 `.xaml.cs`
- [ ] 删除 `PatientDetailView.xaml` 及 `.xaml.cs`
- [ ] 删除 `UserManagementView.xaml` 及 `.xaml.cs`
- [ ] 删除 `UserDetailView.xaml` 及 `.xaml.cs`
- [ ] 删除 `HerbManagementView.xaml` 及 `.xaml.cs`
- [ ] 删除 `HerbDetailView.xaml` 及 `.xaml.cs`
- [ ] 删除 `FormulaManagementView.xaml` 及 `.xaml.cs`
- [ ] 删除 `FormulaDetailView.xaml` 及 `.xaml.cs`

### Task 6.2: 删除废弃的ViewModel文件
- [ ] 删除 `PatientManagementViewModel.cs`
- [ ] 删除 `PatientDetailViewModel.cs`
- [ ] 删除 `UserManagementViewModel.cs`
- [ ] 删除 `UserDetailViewModel.cs`
- [ ] 删除 `HerbManagementViewModel.cs`
- [ ] 删除 `HerbDetailViewModel.cs`
- [ ] 删除 `FormulaManagementViewModel.cs`
- [ ] 删除 `FormulaDetailViewModel.cs`

### Task 6.3: 清理导航和模块注册
- [ ] 移除各Module中废弃的View注册
- [ ] 更新菜单导航配置
- [ ] 清理未使用的Region定义
- [ ] 移除废弃的NavigationParameters

### Task 6.4: 清理废弃的基类和容器
- [ ] 评估 `BaseMasterDataListView` 是否仍需要 (可能仅MedicalCase使用)
- [ ] 评估 `BaseDetailContainer` 是否仍需要
- [ ] 删除或标记为Obsolete不再使用的基类

### Task 6.5: 样式清理与统一
- [ ] 删除废弃的View专用样式
- [ ] 提取 MasterDetail 相关样式到 UnifiedComponents.xaml
- [ ] 统一列表选中样式
- [ ] 统一详情面板样式

### Task 6.6: 测试验证
- [ ] 验证各模块 CRUD 功能正常
- [ ] 验证导航和选择同步
- [ ] 验证响应式布局 (窗口缩放)
- [ ] 验证无残留的废弃引用 (编译无警告)

---

## Checklist Summary

| Phase | Tasks | Priority | 说明 |
|-------|-------|----------|------|
| Phase 1: 基础架构-核心 | 3 | P0 | MasterDetailLayout + ViewModel基类 |
| Phase 1.5: 基础架构-控件 | 5 | P0 | SearchBox, DetailToolbar, EmptyState等 |
| Phase 2: 患者模块 | 3 | P1 | PatientMasterDetailView |
| Phase 3: 用户模块 | 3 | P1 | UserMasterDetailView |
| Phase 4: 药材模块 | 3 | P1 | HerbMasterDetailView |
| Phase 5: 验方模块 | 3 | P1 | FormulaMasterDetailView |
| Phase 6: 清理优化 | 6 | P2 | 删除废弃文件、样式统一、测试 |

**总计: 26个任务**

---

## 重要原则

1. **及时清理**: 每完成一个模块重构，立即删除对应的废弃文件
2. **编译验证**: 每次删除后确保编译通过，无未解析引用
3. **逐步迁移**: 一次只重构一个模块，确保稳定后再进行下一个
4. **保留回滚能力**: 通过Git提交记录确保可回滚到任意阶段
