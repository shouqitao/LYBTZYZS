# Tasks: Desktop层未使用代码清理

## Phase 1: 清理未使用的UI组件

### 1.1 Shell Dialogs清理
- [x] 1.1.1 确认ErrorDetailsDialog无引用后删除
  - Shell/Dialogs/Views/ErrorDetailsDialog.xaml
  - Shell/Dialogs/Views/ErrorDetailsDialog.xaml.cs
  - Shell/Dialogs/ViewModels/ErrorDetailsDialogViewModel.cs
- [x] 1.1.2 确认InformationDialog无引用后删除
  - Shell/Dialogs/Views/InformationDialog.xaml
  - Shell/Dialogs/Views/InformationDialog.xaml.cs
  - Shell/Dialogs/ViewModels/InformationDialogViewModel.cs
- [x] 1.1.3 更新Shell/README.md移除已删除Dialog的文档

## Phase 2: 清理未使用的Service代码

### 2.1 MedicalCase模块清理
- [x] 2.1.1 确认MedicalCaseStatusPresenter无引用后删除
  - MedicalCase/Services/MedicalCaseStatusPresenter.cs
- [x] 2.1.2 确认MedicalCaseEventCoordinator无引用后删除
  - MedicalCase/Services/MedicalCaseEventCoordinator.cs

### 2.2 Users模块清理
- [x] 2.2.1 确认UserDataManager未使用后删除
  - Users/Interfaces/IUserDataManager.cs
  - Users/Services/UserDataManager.cs
- [x] 2.2.2 确认UserValidator未使用后删除
  - Users/Services/UserValidator.cs
- [x] 2.2.3 删除对应测试文件
  - tests/.../UserDataManagerTests.cs
  - tests/.../UserValidatorTests.cs

## Phase 3: 修复不规范使用

### 3.1 Dialog注册规范化
- [x] 3.1.1 评估UnfinishedCaseDialog
  - 结论：使用WPF Window模式，功能正常，保持现状
  - 原因：Pre-Release阶段避免不必要的重构风险

## Phase 4: 清理孤立接口

### 4.1 接口评估
- [x] 4.1.1 评估IDataProvider接口
  - 结论：接口被PrescriptionPanelViewModel和ConsultationPanelViewModel实现
  - 结论：接口被MedicalCaseWorkspaceCoordinator广泛使用
  - 保持现状，不删除

## Phase 5: 验证

### 5.1 编译验证
- [x] 5.1.1 执行完整编译确认无错误
- [x] 5.1.2 确认0错误0警告(除transient file lock warning)

## 清理总结

**已删除文件 (11个)**:
- Shell/Dialogs/Views/ErrorDetailsDialog.xaml
- Shell/Dialogs/Views/ErrorDetailsDialog.xaml.cs
- Shell/Dialogs/ViewModels/ErrorDetailsDialogViewModel.cs
- Shell/Dialogs/Views/InformationDialog.xaml
- Shell/Dialogs/Views/InformationDialog.xaml.cs
- Shell/Dialogs/ViewModels/InformationDialogViewModel.cs
- MedicalCase/Services/MedicalCaseStatusPresenter.cs
- MedicalCase/Services/MedicalCaseEventCoordinator.cs
- Users/Interfaces/IUserDataManager.cs
- Users/Services/UserDataManager.cs
- Users/Services/UserValidator.cs

**已删除测试文件 (2个)**:
- UserDataManagerTests.cs
- UserValidatorTests.cs

**已更新文档 (1个)**:
- Shell/README.md (移除已删除Dialog的引用)

**跳过项**:
- UnfinishedCaseDialog: 功能正常，保持WPF Window模式
- IDataProvider: 被广泛使用，非孤立接口
