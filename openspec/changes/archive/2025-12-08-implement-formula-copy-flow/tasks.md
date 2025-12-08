# Tasks for implement-formula-copy-flow

## Phase 1: 权限判断基础设施

### Task 1.1: 添加验方所有权判断属性 [x]
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 添加 `IsOwnFormula` 和 `CanEdit` 计算属性
- **验证**: 单元测试覆盖不同用户场景
- **完成**: 添加了 `IsOwnFormula`、`CanEdit`、`CanCopy` 三个属性

### Task 1.2: 获取当前用户ID [x]
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 通过 `ICurrentUserService` 获取当前登录用户ID
- **验证**: 确保与Formula.CreatedBy正确比较
- **完成**: 使用 `SessionManager?.CurrentUser?.Id` 获取当前用户ID

## Phase 2: UI权限控制

### Task 2.1: 条件显示编辑按钮 [x]
- **文件**: `FormulaDetailView.xaml`
- **内容**: 编辑按钮绑定 `CanEdit` 属性控制可见性
- **验证**:
  - 自己的验方：显示编辑按钮
  - 他人/共享验方：隐藏编辑按钮
- **完成**: EditCommand.CanExecute 已绑定 CanEdit 属性

### Task 2.2: 启用复制按钮 [x]
- **文件**: `FormulaDetailView.xaml`
- **内容**: 将复制按钮 `Visibility="Collapsed"` 改为条件显示
- **验证**: 查看模式下显示复制按钮
- **完成**: 绑定 `Visibility="{Binding CanCopy, Converter=...}"`

## Phase 3: 复制流程实现

### Task 3.1: 实现LoadFromCopySource方法 [x]
- **文件**: `FormulaDetailViewModel.cs`
- **内容**:
  - 从源验方复制所有字段
  - 名称添加"(副本)"后缀
  - Id设为Empty
  - IsShared设为false
  - 加载药材列表
- **验证**: 预填充数据正确，可修改后保存
- **完成**: 实现完整的 LoadFromCopySource 方法

### Task 3.2: 处理导航参数 [x]
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 在 `ProcessNavigationParameters` 中处理 `CopyFromFormula` 参数
- **验证**: 导航后进入编辑模式，数据正确预填充
- **完成**: OnNavigatedTo 中检测 CopyFromFormula 参数并调用 LoadFromCopySource

### Task 3.3: 保存复制的验方 [x]
- **文件**: `FormulaDetailViewModel.cs`
- **内容**: 确保保存时作为新建处理（Id为Empty时调用Create API）
- **验证**: 保存成功创建新验方，不影响原验方
- **完成**: ExecuteSave 中已有判断逻辑 (FormulaId == Guid.Empty 时调用 Create)

## Phase 4: 集成测试

### Task 4.1: 手动测试用例 [x]
- 场景1: 医生查看自己的验方 → 可编辑、可复制
- 场景2: 医生查看管理员创建的验方 → 仅可查看、可复制
- 场景3: 医生查看他人共享的验方 → 仅可查看、可复制
- 场景4: 复制验方后修改并保存 → 创建新验方成功
- **完成**: 2025-12-08 用户验证通过，副本正常创建并显示

### Task 4.2: 编译验证 [x]
- 运行 `dotnet build LYBT.All.sln`
- 确保无编译错误
- **完成**: 2025-12-08 编译通过，0警告0错误

## Phase 5: Bug修复

### Task 5.1: 修复复制后验方不显示在列表问题 [x]
- **根因**: `FormulaService.CreateAsync` 未设置 `UserId` 字段
- **影响**: `GetPagedAsync` 过滤条件 `f.UserId == currentUserId` 排除了新创建的验方
- **修复**:
  1. `IFormulaService.CreateAsync` 添加 `Guid? creatorId` 参数
  2. `FormulaService.CreateAsync` 设置 `entity.UserId = creatorId`
  3. `FormulasController.Add` 调用 `GetOperator()` 获取用户ID并传递
- **完成**: 2025-12-08 编译通过，0警告0错误

## Dependencies
- Task 2.2 依赖 Task 1.1 (需要CanEdit属性)
- Task 3.1 依赖 Task 1.2 (需要CurrentUser信息)
- Task 4.1 依赖所有其他任务完成
