# Tasks: 重构MasterDetail编辑模式

## Phase 1: 修复P0 Bug

- [ ] 1.1 在MasterDetailViewModelBase添加`_isCreatingNew`私有字段
- [ ] 1.2 修改`OnExecuteAddAsync`：先设置`_isCreatingNew = true`，执行完成后重置
- [ ] 1.3 修改`RefreshCanExecuteChanged`：当`_isCreatingNew`时跳过`CurrentDetail = null`逻辑
- [ ] 1.4 验证Users模块新建保存功能

## Phase 2: 统一编辑模式 - Users模块

- [ ] 2.1 移除UserMasterDetailViewModel中的Edit属性（EditUserName, EditRealName等7个）
- [ ] 2.2 移除ClearEditProperties()方法
- [ ] 2.3 移除PopulateEditProperties()或类似的属性填充方法（如存在）
- [ ] 2.4 修改SaveDetailAsync直接使用CurrentDetail属性
- [ ] 2.5 清理CreateNewDetail()中多余的Edit属性初始化代码
- [ ] 2.6 更新UserMasterDetailView.xaml绑定从EditXxx改为CurrentDetail.Xxx
- [ ] 2.7 验证Users模块CRUD功能

## Phase 3: 统一编辑模式 - Patients模块

- [ ] 3.1 移除PatientMasterDetailViewModel中的Edit属性（8个）
- [ ] 3.2 移除ClearEditProperties()方法
- [ ] 3.3 移除PopulateEditProperties()或类似的属性填充方法（如存在）
- [ ] 3.4 修改SaveDetailAsync直接使用CurrentDetail属性
- [ ] 3.5 清理CreateNewDetail()中多余的Edit属性初始化代码
- [ ] 3.6 更新PatientMasterDetailView.xaml绑定
- [ ] 3.7 验证Patients模块CRUD功能

## Phase 4: 统一编辑模式 - Herbs模块

- [ ] 4.1 移除HerbMasterDetailViewModel中的Edit属性（11个）
- [ ] 4.2 移除ClearEditProperties()方法
- [ ] 4.3 移除PopulateEditProperties()或类似的属性填充方法（如存在）
- [ ] 4.4 修改SaveDetailAsync直接使用CurrentDetail属性
- [ ] 4.5 清理CreateNewDetail()中多余的Edit属性初始化代码
- [ ] 4.6 更新HerbMasterDetailView.xaml绑定
- [ ] 4.7 验证Herbs模块CRUD功能

## Phase 5: 清理迭代遗留代码

- [ ] 5.1 检查并移除各模块中废弃的辅助方法（OnEditModeChanged等）
- [ ] 5.2 检查并移除未使用的私有字段和属性
- [ ] 5.3 统一CloneDetail()实现模式（使用MemberwiseClone或统一构造）
- [ ] 5.4 清理SaveDetailAsync中冗余的DTO构建逻辑
- [ ] 5.5 移除注释掉的旧代码块
- [ ] 5.6 统一命名规范（确保所有模块一致）

## Phase 6: MedicalCase模块检查

- [ ] 6.1 确认MedicalCase的CreateNewDetail()逻辑正确（入口在看诊工作台）
- [ ] 6.2 检查MedicalCase是否存在遗留的Edit属性模式
- [ ] 6.3 如有不一致，统一为CurrentDetail绑定模式

## Phase 7: Formula模块验证

- [ ] 7.1 确认Formula模块已使用统一的CurrentDetail绑定模式
- [ ] 7.2 检查是否存在遗留的混合模式代码
- [ ] 7.3 如有不一致，清理遗留代码

## Phase 8: 最终验证

- [ ] 8.1 运行全量编译验证（无警告）
- [ ] 8.2 执行功能回归测试（5个模块CRUD）
- [ ] 8.3 检查绑定错误日志
- [ ] 8.4 更新相关文档
