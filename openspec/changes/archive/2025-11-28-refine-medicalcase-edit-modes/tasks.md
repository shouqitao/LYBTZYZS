# Tasks: refine-medicalcase-edit-modes

## Phase 1: 基础架构

### Task 1.1: 添加WorkspaceMode枚举
- [ ] 在LYBT.Desktop.MedicalCase中创建`WorkspaceMode`枚举
- [ ] 定义值: `Clinical`(临床看诊), `Management`(管理编辑)

### Task 1.2: 添加EditState枚举
- [ ] 创建`EditState`枚举
- [ ] 定义值: `Editing`(编辑中), `ReadOnly`(只读)

### Task 1.3: 扩展导航参数
- [ ] 创建`MedicalCaseNavigationParameters`类
- [ ] 包含: MedicalCaseId, PatientId, WorkspaceMode, InitialEditState

## Phase 2: ViewModel重构

### Task 2.1: MedicalCaseWorkspaceViewModel动态属性
- [ ] 添加`WorkspaceMode`属性
- [ ] 添加`EditState`属性
- [ ] 添加计算属性:
  - `HeaderTitle` (根据模式返回"看诊中"或"编辑医案")
  - `BackButtonText` (根据模式返回返回按钮文案)
  - `IsEditing` (EditState == Editing)
  - `IsReadOnly` (EditState == ReadOnly)
  - `ShowSaveButton` (IsEditing)
  - `ShowEditButton` (IsReadOnly && HasEditPermission)

### Task 2.2: 更新导航命令
- [ ] 重构`BackCommand`根据WorkspaceMode导航到不同目标
- [ ] Clinical → PatientSelectionView
- [ ] Management → MedicalCaseManagementView

### Task 2.3: 更新操作命令
- [ ] 重命名`SaveDraftCommand`为`SaveAndStayCommand`(暂存医案)
- [ ] 实现: 保存数据 + 切换到ReadOnly状态
- [ ] 重命名`EnterEditModeCommand`为`EnterEditCommand`(修改医案)
- [ ] 实现: 切换到Editing状态
- [ ] 移除冗余的"暂停看诊"按钮（功能合并）

## Phase 3: View更新

### Task 3.1: 更新Header区域
- [ ] 绑定HeaderTitle到标题TextBlock
- [ ] 绑定BackButtonText到返回按钮

### Task 3.2: 更新底部操作栏
- [ ] 将"保存"改为"暂存医案"
- [ ] 将"编辑"改为"修改医案"
- [ ] 移除"暂停看诊"按钮
- [ ] 更新Tooltip文案

### Task 3.3: 更新按钮可见性绑定
- [ ] 暂存医案: `Visibility="{Binding IsEditing}"`
- [ ] 修改医案: `Visibility="{Binding ShowEditButton}"`
- [ ] 完成看诊: `Visibility="{Binding IsEditing}"`

## Phase 4: 调用方更新

### Task 4.1: PatientSelectionViewModel
- [ ] 导航时传递`WorkspaceMode.Clinical`
- [ ] 传递`InitialEditState.Editing`

### Task 4.2: MedicalCaseManagementViewModel
- [ ] ViewDetailsCommand传递`WorkspaceMode.Management` + `InitialEditState.ReadOnly`
- [ ] EditCommand传递`WorkspaceMode.Management` + `InitialEditState.Editing`

## Phase 5: 规范更新

### Task 5.1: 更新spec
- [ ] 更新medicalcase-edit-modes/spec.md反映最终实现

## Dependencies
- Phase 2 依赖 Phase 1
- Phase 3 依赖 Phase 2
- Phase 4 依赖 Phase 2
- Phase 5 在所有Phase完成后进行

## Validation Checklist
- [ ] 从患者选择进入: 标题"看诊中"，返回按钮"返回患者选择"
- [ ] 从医案管理查看进入: 标题"编辑医案"，返回按钮"返回医案列表"，只读模式
- [ ] 从医案管理编辑进入: 标题"编辑医案"，返回按钮"返回医案列表"，编辑模式
- [ ] 点击"暂存医案": 保存数据，切换到只读模式，留在当前界面
- [ ] 点击"修改医案": 切换到编辑模式
- [ ] 点击"完成看诊": 保存并返回来源页面
- [ ] 编译通过，无警告
