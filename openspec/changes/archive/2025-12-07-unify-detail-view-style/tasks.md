# Tasks: unify-detail-view-style

## Phase 1: 样式基础设施

### Task 1.1: 扩展UnifiedComponents.xaml
- [x] 添加DetailView通用样式
  - DetailViewToolbarStyle（顶部工具栏）
  - DetailViewContentStyle（内容区域）
  - DetailViewFooterStyle（底部操作栏）
  - FormFieldStyle（表单字段布局）
  - LoadingOverlayStyle（加载遮罩）

### Task 1.2: 创建BaseDetailView模板（可选）
- [x] 评估是否需要类似BaseMasterDataListView的基类
- [x] 如需要，创建BaseDetailView.xaml（已实现为BaseDetailContainer）

## Phase 2: 详情页重构

### Task 2.1: PatientDetailView重构
- [x] 统一布局为3行结构（使用BaseDetailContainer）
- [x] 右上角添加"编辑"按钮（SwitchToEditCommand）
- [x] 移除本地重复样式，引用UnifiedComponents
- [x] 统一加载指示器

### Task 2.2: HerbDetailView重构
- [x] 统一布局为3行结构（使用BaseDetailContainer）
- [x] 右上角添加"编辑"按钮（SwitchToEditCommand）
- [x] 移除本地重复样式，引用UnifiedComponents
- [x] 统一加载指示器

### Task 2.3: UserDetailView重构
- [x] 重构为3行布局（使用BaseDetailContainer）
- [x] 右上角添加"编辑"按钮（SwitchToEditCommand）
- [x] 替换内联样式为共享样式引用
- [x] 添加加载指示器

### Task 2.4: FormulaDetailView重构
- [x] 调整为标准3行布局（使用BaseDetailContainer）
- [x] 右上角添加"编辑"按钮（EditCommand）
- [x] 统一样式引用
- [x] 统一加载指示器样式

### Task 2.5: MedicalCaseDetailView重构
- [x] 将底部"编辑"按钮移至右上角工具栏（ActionButtons）
- [x] 保持卡片式内容布局（医案场景特殊）
- [x] 统一样式引用

## Phase 3: 列表页修改

### Task 3.1: PatientManagementView修改
- [x] 移除操作列中的"编辑"按钮
- [x] 调整操作列宽度

### Task 3.2: HerbManagementView修改
- [x] 移除操作列中的"编辑"按钮
- [x] 调整操作列宽度

### Task 3.3: UserManagementView修改
- [x] 移除操作列中的"编辑"按钮
- [x] 调整操作列宽度

### Task 3.4: FormulaManagementView修改
- [x] 移除操作列中的"编辑"按钮
- [x] 调整操作列宽度

### Task 3.5: MedicalCaseManagementView修改
- [x] 移除操作列中的"编辑"按钮
- [x] 调整操作列宽度

## Phase 4: 验证

### Task 4.1: 编译验证
- [x] 全解决方案编译通过
- [x] 无XAML绑定警告

### Task 4.2: 功能测试
- [x] 各详情页正常打开
- [x] 右上角编辑按钮功能正常
- [x] 列表页操作按钮正常
- [x] 加载指示器正常显示

### Task 4.3: UI一致性检查
- [x] 所有详情页布局一致（使用BaseDetailContainer）
- [x] 所有详情页样式一致（引用UnifiedComponents）
- [x] 操作流程符合预期

## 依赖关系

```
Phase 1 (样式基础)
    ↓
Phase 2 (详情页重构) ─┬─ Task 2.1-2.5 可并行
    ↓                 │
Phase 3 (列表页修改) ─┴─ Task 3.1-3.5 可并行
    ↓
Phase 4 (验证)
```

## 预估工作量

| Phase | 任务数 | 复杂度 | 状态 |
|-------|--------|--------|------|
| Phase 1 | 2 | 中 | 已完成 |
| Phase 2 | 5 | 中-高 | 已完成 |
| Phase 3 | 5 | 低 | 已完成 |
| Phase 4 | 3 | 低 | 已完成 |
| **总计** | **15** | - | **全部完成** |

## 完成说明

所有任务已通过`refactor-detail-view-container` OpenSpec完成：
- 创建了`BaseDetailContainer`容器组件替代了原计划的BaseDetailView
- 所有5个DetailView已迁移使用BaseDetailContainer
- 所有5个ManagementView已移除编辑按钮
- UnifiedComponents.xaml包含所有必需的共享样式
- 编译验证通过：0错误0警告
