# Tasks for implement-formula-copy-flow

## Phase 1: 权限判断基础设施

### Task 1.1: 添加验方所有权判断属性
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 添加 `IsOwnFormula` 和 `CanEdit` 计算属性
- **验证**: 单元测试覆盖不同用户场景

### Task 1.2: 获取当前用户ID
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 通过 `ICurrentUserService` 获取当前登录用户ID
- **验证**: 确保与Formula.CreatedBy正确比较

## Phase 2: UI权限控制

### Task 2.1: 条件显示编辑按钮
- **文件**: `FormulaDetailView.xaml`
- **内容**: 编辑按钮绑定 `CanEdit` 属性控制可见性
- **验证**:
  - 自己的验方：显示编辑按钮
  - 他人/共享验方：隐藏编辑按钮

### Task 2.2: 启用复制按钮
- **文件**: `FormulaDetailView.xaml`
- **内容**: 将复制按钮 `Visibility="Collapsed"` 改为条件显示
- **验证**: 查看模式下显示复制按钮

## Phase 3: 复制流程实现

### Task 3.1: 实现LoadFromCopySource方法
- **文件**: `FormulaDetailViewModel.cs`
- **内容**:
  - 从源验方复制所有字段
  - 名称添加"(副本)"后缀
  - Id设为Empty
  - IsShared设为false
  - 加载药材列表
- **验证**: 预填充数据正确，可修改后保存

### Task 3.2: 处理导航参数
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 在 `ProcessNavigationParameters` 中处理 `CopyFromFormula` 参数
- **验证**: 导航后进入编辑模式，数据正确预填充

### Task 3.3: 保存复制的验方
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 确保保存时作为新建处理（Id为Empty时调用Create API）
- **验证**: 保存成功创建新验方，不影响原验方

## Phase 4: 集成测试

### Task 4.1: 手动测试用例
- 场景1: 医生查看自己的验方 → 可编辑、可复制
- 场景2: 医生查看管理员创建的验方 → 仅可查看、可复制
- 场景3: 医生查看他人共享的验方 → 仅可查看、可复制
- 场景4: 复制验方后修改并保存 → 创建新验方成功

### Task 4.2: 编译验证
- 运行 `dotnet build LYBT.All.sln`
- 确保无编译错误

## Dependencies
- Task 2.2 依赖 Task 1.1 (需要CanEdit属性)
- Task 3.1 依赖 Task 1.2 (需要CurrentUser信息)
- Task 4.1 依赖所有其他任务完成
