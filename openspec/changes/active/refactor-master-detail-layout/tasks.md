# Tasks: 重构基础数据模块为Master-Detail布局

## Phase 1: 基础架构 (Infrastructure)

### Task 1.1: 创建MasterDetailLayout通用控件
- [ ] 创建 `MasterDetailLayout.xaml` 在 Infrastructure.Controls
- [ ] 实现左右分割布局 (GridSplitter可调节)
- [ ] 支持 MasterContent 和 DetailContent 区域
- [ ] 添加 SelectedItem 依赖属性
- [ ] 支持空状态提示 (EmptyDetailTemplate)

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

### Task 6.1: 移除废弃代码
- [ ] 删除独立的 *DetailView.xaml 文件
- [ ] 删除独立的 *DetailViewModel.cs 文件
- [ ] 清理未使用的导航配置

### Task 6.2: 样式统一
- [ ] 提取 MasterDetail 相关样式到 UnifiedComponents.xaml
- [ ] 统一列表选中样式
- [ ] 统一详情面板样式

### Task 6.3: 测试验证
- [ ] 验证各模块 CRUD 功能
- [ ] 验证导航和选择同步
- [ ] 验证响应式布局 (窗口缩放)

---

## Checklist Summary

| Phase | Tasks | Priority |
|-------|-------|----------|
| Phase 1: 基础架构 | 3 | P0 |
| Phase 2: 患者模块 | 3 | P1 |
| Phase 3: 用户模块 | 3 | P1 |
| Phase 4: 药材模块 | 3 | P1 |
| Phase 5: 验方模块 | 3 | P1 |
| Phase 6: 清理优化 | 3 | P2 |

**总计: 18个任务**
