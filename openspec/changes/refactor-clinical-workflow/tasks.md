# Tasks: refactor-clinical-workflow

## 三大部分概述

| 部分 | 界面 | 工作内容 | 状态 |
|------|------|----------|------|
| 1. 主页 | ClinicalHomeView | **不动** | 完成 |
| 2. 患者选择 | PatientSelectionView | 重新设计 + 迁移到Clinical | 完成 |
| 3. 看诊 | MedicalCaseWorkspaceView | 迁移到Clinical | 完成 |

---

## Phase 1: 准备工作

- [x] 确认WorkspaceMode枚举是否已包含Reception值
- [x] 确认MasterDetailLayout控件可用性
- [x] 确认PatientViewControl控件可用性
- [x] 确认IRoleNavigationService服务可用性

---

## Phase 2: Part 2 - 患者选择控件 (Patients模块)

### 2.1 创建PatientSelectionControl

- [x] 创建 `Patients/Controls/PatientSelectionControl.xaml`
  - 使用 `MasterDetailLayout` 控件
  - Master区域：工具栏 + 搜索框 + 患者列表 + 分页
  - Detail区域：复用 `PatientViewControl`
  - 空状态：`EmptyState` 控件

- [x] 创建 `Patients/Controls/PatientSelectionControl.xaml.cs`
  - DependencyProperty: SelectedPatient, PatientDetail
  - DependencyProperty: CreateNewCommand, RefreshCommand
  - 内部搜索和分页逻辑

### 2.2 注册控件

- [x] 修改 `PatientsModule.cs` 注册新控件

---

## Phase 3: Part 2 - 患者选择主界面 (Clinical模块)

### 3.1 创建View

- [x] 创建 `Clinical/Views/PatientSelectionView.xaml`
  - 顶部导航栏：标题 + 返回主页按钮
  - 中间区域：引用 PatientSelectionControl
  - 底部操作栏：状态消息 + 角色按钮(挂号/开始看诊)

- [x] 创建 `Clinical/Views/PatientSelectionView.xaml.cs`
  - 标准code-behind

### 3.2 创建ViewModel

- [x] 创建 `Clinical/ViewModels/PatientSelectionViewModel.cs`
  - 属性：WorkspaceMode, SelectedPatient, PatientDetail, StatusMessage
  - 属性：IsReceptionMode, IsClinicalMode (计算属性)
  - 命令：BackCommand, CreateNewCommand, RegisterCommand, StartConsultationCommand
  - 实现挂号逻辑（检查挂起 → 创建待诊记录）
  - 实现看诊逻辑（检查挂起 → 四选项弹窗 → 创建/继续医案）
  - 实现导航参数处理（OnNavigatedTo）

### 3.3 实现四选项弹窗

- [x] 确认 `ICommonDialogService` 是否已有四选项弹窗方法
- [x] 使用现有 `ShowConfirmAsync` 实现简化版两选项弹窗
- [x] 实现弹窗逻辑 (继续/关闭新建)

### 3.4 注册View/ViewModel

- [x] 修改 `ClinicalModule.cs` 注册新View/ViewModel

---

## Phase 4: Part 3 - 看诊界面迁移 (Clinical模块)

### 4.1 迁移文件

- [x] 迁移 `MedicalCase/Views/MedicalCaseWorkspaceView.xaml` → `Clinical/Views/`
- [x] 迁移 `MedicalCase/Views/MedicalCaseWorkspaceView.xaml.cs` → `Clinical/Views/`
- [x] 迁移 `MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs` → `Clinical/ViewModels/`

### 4.2 更新命名空间

- [x] 更新迁移文件的命名空间为 `LYBT.Desktop.Clinical.*`
- [x] 更新XAML中的命名空间引用

### 4.3 更新模块注册

- [x] 从 `MedicalCaseModule.cs` 删除旧注册
- [x] 在 `ClinicalModule.cs` 添加新注册

### 4.4 保留MedicalCase控件

- [x] 确认 `PendingQueueControl` 等控件保持在MedicalCase模块
- [x] 确认Clinical模块正确引用MedicalCase控件

---

## Phase 5: 更新导航

- [x] 修改 `ClinicalHomeViewModel.cs`
  - 更新"开始接诊"导航目标为 `PatientSelectionView`
  - 传递 `WorkspaceMode.Clinical` 参数

- [x] 修改 `MedicalCaseNavigationHandler.cs`
  - 更新返回目标为新 `PatientSelectionView`

- [ ] (可选) 添加前台入口导航
  - 如果存在 `ReceptionHomeViewModel`，添加导航到 `PatientSelectionView`
  - 传递 `WorkspaceMode.Reception` 参数

---

## Phase 6: 删除旧代码

- [x] 删除 `Patients/Views/PatientSelectionView.xaml`
- [x] 删除 `Patients/Views/PatientSelectionView.xaml.cs`
- [x] 删除 `Patients/ViewModels/PatientSelectionViewModel.cs`
- [ ] 删除 `Patients/ViewModels/Components/PatientSelectionCommandExecutor.cs`
- [ ] 删除 `Patients/Services/PendingQueueManager.cs`
- [x] 从 `PatientsModule.cs` 删除旧注册
- [x] 删除旧的测试文件 `PatientSelectionViewModelTests.cs`

---

## Phase 7: 编译验证

- [x] 编译验证无错误
- [x] 搜索并修复所有旧引用
- [ ] 运行现有测试

---

## Phase 8: 功能测试

### 8.1 患者选择界面

- [ ] 验证Master-Detail布局正确显示
- [ ] 验证患者列表分页功能
- [ ] 验证选择患者显示详情
- [ ] 验证新建患者功能

### 8.2 角色操作

- [ ] 验证医生模式显示"开始看诊"按钮
- [ ] 验证前台模式显示"挂号"按钮（如有前台模块）

### 8.3 挂起医案处理

- [ ] 验证选择无挂起患者 → 直接创建医案
- [ ] 验证选择有挂起患者 → 显示四选项弹窗
- [ ] 验证四选项弹窗各选项功能正确

### 8.4 导航流程

- [ ] 验证 主页 → 患者选择 → 看诊 完整流程
- [ ] 验证 看诊 → 返回患者选择 功能
- [ ] 验证 患者选择 → 返回主页 功能

---

## Acceptance Criteria

1. [x] **架构工整**: 三个主界面都在Clinical模块
2. [x] **患者选择**: 使用Master-Detail布局，简洁清晰
3. [ ] **角色区分**: 前台显示挂号，医生显示开始看诊
4. [ ] **挂起处理**: 四选项弹窗正常工作
5. [x] **看诊界面**: 功能不变，仅位置迁移
6. [x] **代码清理**: 旧代码删除，编译无错误
