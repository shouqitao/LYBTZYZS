# Tasks: unify-detail-view-style

## Phase 1: 样式基础设施

### Task 1.1: 扩展UnifiedComponents.xaml
- [ ] 添加DetailView通用样式
  - DetailViewToolbarStyle（顶部工具栏）
  - DetailViewContentStyle（内容区域）
  - DetailViewFooterStyle（底部操作栏）
  - FormFieldStyle（表单字段布局）
  - LoadingOverlayStyle（加载遮罩）

### Task 1.2: 创建BaseDetailView模板（可选）
- [ ] 评估是否需要类似BaseMasterDataListView的基类
- [ ] 如需要，创建BaseDetailView.xaml

## Phase 2: 详情页重构

### Task 2.1: PatientDetailView重构
- [ ] 统一布局为3行结构
- [ ] 右上角添加"编辑"按钮
- [ ] 移除本地重复样式，引用UnifiedComponents
- [ ] 统一加载指示器

### Task 2.2: HerbDetailView重构
- [ ] 统一布局为3行结构
- [ ] 右上角添加"编辑"按钮
- [ ] 移除本地重复样式，引用UnifiedComponents
- [ ] 统一加载指示器

### Task 2.3: UserDetailView重构
- [ ] 重构为3行布局（当前为2行ScrollViewer）
- [ ] 右上角添加"编辑"按钮
- [ ] 替换内联样式为共享样式引用
- [ ] 添加加载指示器

### Task 2.4: FormulaDetailView重构
- [ ] 调整为标准3行布局
- [ ] 右上角添加"编辑"按钮
- [ ] 统一样式引用
- [ ] 统一加载指示器样式

### Task 2.5: MedicalCaseDetailView重构
- [ ] 将底部"编辑"按钮移至右上角工具栏
- [ ] 保持卡片式内容布局（医案场景特殊）
- [ ] 统一样式引用

## Phase 3: 列表页修改

### Task 3.1: PatientManagementView修改
- [ ] 移除操作列中的"编辑"按钮
- [ ] 调整操作列宽度

### Task 3.2: HerbManagementView修改
- [ ] 移除操作列中的"编辑"按钮
- [ ] 调整操作列宽度

### Task 3.3: UserManagementView修改
- [ ] 移除操作列中的"编辑"按钮
- [ ] 调整操作列宽度

### Task 3.4: FormulaManagementView修改
- [ ] 移除操作列中的"编辑"按钮
- [ ] 调整操作列宽度

### Task 3.5: MedicalCaseManagementView修改
- [ ] 移除操作列中的"编辑"按钮
- [ ] 调整操作列宽度

## Phase 4: 验证

### Task 4.1: 编译验证
- [ ] 全解决方案编译通过
- [ ] 无XAML绑定警告

### Task 4.2: 功能测试
- [ ] 各详情页正常打开
- [ ] 右上角编辑按钮功能正常
- [ ] 列表页操作按钮正常
- [ ] 加载指示器正常显示

### Task 4.3: UI一致性检查
- [ ] 所有详情页布局一致
- [ ] 所有详情页样式一致
- [ ] 操作流程符合预期

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

| Phase | 任务数 | 复杂度 |
|-------|--------|--------|
| Phase 1 | 2 | 中 |
| Phase 2 | 5 | 中-高 |
| Phase 3 | 5 | 低 |
| Phase 4 | 3 | 低 |
| **总计** | **15** | - |
