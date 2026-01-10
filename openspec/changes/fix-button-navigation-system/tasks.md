# fix-button-navigation-system Tasks

## Overview

- **变更类型**: Refactor + Bug Fix
- **风险等级**: Medium
- **预估工作量**: 2-3小时
- **核心原则**: 端到端语义一致（按钮 → Command → Service → Repository → API）

---

## Phase 1: Critical Bug Fix (命令绑定修复)

### 1.1 修复ClinicalHomeView命令绑定
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml`
- **当前**: 第151行 `StartConsultationCommand`
- **ViewModel**: `ClinicalHomeViewModel.StartMedicalCaseCommand`
- **变更**: `StartConsultationCommand` → `StartMedicalCaseCommand`
- **验证**: 编译通过，点击"开始看诊"按钮正常导航

### 1.2 修复MedicalCaseWorkspaceView暂存按钮绑定
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`
- **当前**: 第216行 `SaveAndStayCommand`
- **ViewModel**: `MedicalCaseWorkspaceViewModel.SaveDraftCommand`
- **变更**: `SaveAndStayCommand` → `SaveDraftCommand`
- **验证**: 编译通过，点击"暂存"按钮正常工作

### 1.3 修复MedicalCaseWorkspaceView完成按钮绑定
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`
- **当前**: 第232行 `CompleteConsultationCommand`
- **ViewModel**: `MedicalCaseWorkspaceViewModel.CompleteMedicalCaseCommand`
- **变更**: `CompleteConsultationCommand` → `CompleteMedicalCaseCommand`
- **验证**: 编译通过，点击"完成看诊"按钮正常工作

### 1.4 修复MedicalCaseWorkspaceView第二处暂存绑定
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`
- **当前**: 第240行 `SaveAndStayCommand`
- **变更**: `SaveAndStayCommand` → `SaveDraftCommand`
- **验证**: 编译通过

### 1.5 添加SystemSettingsViewModel导航命令
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/SystemSettingsViewModel.cs`
- **当前**: 缺少 `NavigateToHomeCommand`
- **变更**: 添加 `NavigateToHomeCommand` 实现导航到AdminHomeView
- **参考**: `AdminHomeViewModel` 的导航实现
- **验证**: 编译通过，点击"返回"按钮正常导航

### 1.6 Phase 1编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

---

## Phase 2: 全量命令审计 (已在设计阶段完成)

### 2.1 审计结果

| 模块 | 文件 | 状态 | 说明 |
|------|------|------|------|
| Clinical | ClinicalHomeView | Phase 1修复 | StartConsultation问题 |
| Clinical | PatientSelectionView | Phase 3处理 | 术语重命名 |
| Clinical | MedicalCaseWorkspaceView | Phase 1修复 | SaveAndStay/CompleteConsultation问题 |
| Admin | AdminHomeView | OK | 6个命令全部匹配 |
| Admin | SystemSettingsView | Phase 1修复 | NavigateToHome缺失 |
| Auth | LoginView | OK | 3个命令全部匹配 |

---

## Phase 3: 术语统一重构

**术语规范**:
- **MedicalCase** = 医案/看诊/病案（整体概念）
- **Consultation** = 诊断（仅指诊断部分）

### 3.1 重命名PatientSelectionViewModel方法
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- **变更**:
  - 第159行 `CanStartConsultation` → `CanStartMedicalCase`
  - 第160行 `StartConsultationAsync` → `StartMedicalCaseAsync`
  - 第195行 `CanStartConsultation` → `CanStartMedicalCase`
- **工具**: 使用 `serena rename_symbol` 或手动Edit
- **验证**: 编译通过

### 3.2 更新PatientSelectionView绑定
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml`
- **变更**:
  - 第54行 `StartConsultationCommand` → `StartMedicalCaseCommand`
  - 第91行 `StartConsultationCommand` → `StartMedicalCaseCommand`
- **验证**: 编译通过

### 3.3 更新PatientSelectionView.xaml.cs代码引用
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml.cs`
- **变更**: 第27-29行 `StartConsultationCommand` → `StartMedicalCaseCommand`
- **验证**: 编译通过

### 3.4 更新TodayPatientItem属性名
- **文件**: `src/Client/Desktop/Shell/Models/TodayPatientItem.cs`
- **变更**: 第89行 `CanStartConsultation` → `CanStartMedicalCase`
- **验证**: 编译通过，检查所有引用

### 3.5 更新MainWindowViewModel属性名
- **文件**: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- **变更**: 第168行 `QuickStartConsultationCommand` → `QuickStartMedicalCaseCommand`
- **验证**: 编译通过

### 3.6 更新MenuManager命令和方法
- **文件**: `src/Client/Desktop/Shell/Services/MenuManager.cs`
- **变更**:
  - 第43行 `QuickStartConsultationCommand` → `QuickStartMedicalCaseCommand`
  - 第82行 `QuickStartConsultation` → `QuickStartMedicalCase`
  - 第111行 `QuickStartConsultationAsync` → `QuickStartMedicalCaseAsync`
- **验证**: 编译通过

### 3.7 更新MainWindow KeyBinding
- **文件**: `src/Client/Desktop/Shell/Views/MainWindow.xaml`
- **变更**: 第19行 `QuickStartConsultationCommand` → `QuickStartMedicalCaseCommand`
- **验证**: 编译通过

### 3.8 Phase 3编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

---

## Phase 4: 导航功能验证

### 4.1 Clinical角色导航验证
- [ ] ClinicalHomeView -> 开始看诊 -> PatientSelectionView
- [ ] ClinicalHomeView -> 患者管理 -> PatientManagementView
- [ ] ClinicalHomeView -> 医案查询 -> MedicalCaseManagementView
- [ ] ClinicalHomeView -> 药材库 -> HerbManagementView
- [ ] ClinicalHomeView -> 验方库 -> FormulaManagementView
- [ ] PatientSelectionView -> 返回主页 -> ClinicalHomeView
- [ ] PatientSelectionView -> 选择患者开始看诊 -> MedicalCaseWorkspaceView
- [ ] MedicalCaseWorkspaceView -> 返回 -> PatientSelectionView
- [ ] MedicalCaseWorkspaceView -> 完成看诊 -> 医案关闭
- [ ] MedicalCaseWorkspaceView -> 暂存 -> 保存草稿
- [ ] MedicalCaseWorkspaceView -> 打印处方笺 -> 打印功能

### 4.2 Admin角色导航验证
- [ ] AdminHomeView -> 用户管理 -> UserManagementView
- [ ] AdminHomeView -> 药材管理 -> HerbManagementView
- [ ] AdminHomeView -> 患者管理 -> PatientManagementView
- [ ] AdminHomeView -> 验方管理 -> FormulaManagementView
- [ ] AdminHomeView -> 医案管理 -> MedicalCaseManagementView
- [ ] AdminHomeView -> 系统设置 -> SystemSettingsView
- [ ] SystemSettingsView -> 返回主页 -> AdminHomeView

### 4.3 Auth模块导航验证
- [ ] LoginView -> 登录 -> 对应角色主页
- [ ] LoginView -> 关闭应用
- [ ] LoginView -> API重试

---

## Phase 5: 历史代码清理

### 5.1 更新Clinical模块README
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/README.md`
- **变更**: 更新命令文档，反映统一后的命令名称
  - 添加正确的命令清单
  - 说明MedicalCase(医案) vs Consultation(诊断)术语规范

### 5.2 更新Patients模块README
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/README.md`
- **变更**: 检查并更新过期的命令引用

### 5.3 Phase 5编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

---

## Dependencies

```
Phase 1 (Critical Fix) ──────> Phase 2 (已完成) ──────> Phase 3 (术语统一)
                                                              │
                                                              v
                                                   Phase 4 (Validation)
                                                              │
                                                              v
                                                     Phase 5 (Cleanup)
```

**依赖说明**:
- Phase 1修复已知关键问题，可独立执行
- Phase 2在设计阶段已完成审计
- Phase 3依赖Phase 1+2的问题清单
- Phase 4在所有代码变更后进行
- Phase 5为最后清理工作

---

## Validation Checklist

### 编译验证
- [ ] Desktop解决方案编译通过（0错误）

### 功能验证
- [ ] Clinical角色所有导航按钮正常工作
- [ ] Admin角色所有导航按钮正常工作
- [ ] Auth模块所有按钮正常工作

### 代码质量验证
- [ ] 无命令绑定不匹配
- [ ] 统一使用MedicalCase术语
- [ ] README文档与实际代码一致

### 调用链验证
- [ ] 按钮 → Command → ViewModel 一致
- [ ] ViewModel → Service → Repository → API 调用链完整

---

## Notes

- CommunityToolkit.Mvvm的`[RelayCommand]`特性会自动生成命令属性
- 方法名规则: `MethodName()` → `MethodNameCommand`
- 异步方法规则: `MethodNameAsync()` → `MethodNameCommand` (去掉Async后缀)
- 验证时注意区分手动定义的Command属性(DelegateCommand)和自动生成的Command
- **术语规范**: MedicalCase=医案/看诊/病案（整体），Consultation=诊断（仅诊断部分）

---

**生成时间**: 2026-01-09
**状态**: 完整版 (已完成设计阶段细化)
