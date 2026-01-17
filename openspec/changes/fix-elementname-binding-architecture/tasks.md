# fix-elementname-binding-architecture Tasks

## Overview

- **变更类型**: Refactor (架构优化)
- **风险等级**: Low
- **预估工作量**: 30分钟

## Phase 1: 重构 PatientSelectionControl

### 1.1 移除 DependencyProperty (xaml.cs)

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml.cs`
- **变更**:
  - 删除以下 DependencyProperty 定义:
    - `Patients` / `PatientsProperty`
    - `SelectedPatient` / `SelectedPatientProperty`
    - `PatientDetail` / `PatientDetailProperty`
    - `HasSelection` / `HasSelectionProperty`
    - `SearchText` / `SearchTextProperty`
    - `CreateNewCommand` / `CreateNewCommandProperty`
    - `RefreshCommand` / `RefreshCommandProperty`
    - `SearchCommand` / `SearchCommandProperty`
    - `SelectCommand` / `SelectCommandProperty`
    - `IsLoading` / `IsLoadingProperty`
  - 保留 `PatientDoubleClicked` 事件
- **验证**: 文件只保留构造函数和事件

### 1.2 更新 DetailContent 区域绑定 (22个)

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml`
- **位置**: `<controls:MasterDetailLayout.DetailContent>` 内的 `PatientViewControl`
- **变更**:

| 行号 | 原绑定 | 新绑定 |
|------|--------|--------|
| 77 | `PatientName="{Binding PatientDetail.Name, ElementName=Root}"` | `PatientName="{Binding PatientDetail.Name}"` |
| 78 | `PinYinCode="{Binding PatientDetail.PinYinCode, ElementName=Root}"` | `PinYinCode="{Binding PatientDetail.PinYinCode}"` |
| 79 | `Gender="{Binding PatientDetail.Gender, ElementName=Root}"` | `Gender="{Binding PatientDetail.Gender}"` |
| 80 | `BirthDate="{Binding PatientDetail.BirthDate, ElementName=Root}"` | `BirthDate="{Binding PatientDetail.BirthDate}"` |
| 81 | `Age="{Binding PatientDetail.Age, ElementName=Root}"` | `Age="{Binding PatientDetail.Age}"` |
| 82 | `IdNumber="{Binding PatientDetail.IdNumber, ElementName=Root}"` | `IdNumber="{Binding PatientDetail.IdNumber}"` |
| 83 | `IdType="{Binding PatientDetail.IdType, ElementName=Root}"` | `IdType="{Binding PatientDetail.IdType}"` |
| 84 | `MaritalStatus="{Binding PatientDetail.MaritalStatus, ElementName=Root}"` | `MaritalStatus="{Binding PatientDetail.MaritalStatus}"` |
| 85 | `BloodType="{Binding PatientDetail.BloodType, ElementName=Root}"` | `BloodType="{Binding PatientDetail.BloodType}"` |
| 86 | `PhoneNumber="{Binding PatientDetail.PhoneNumber, ElementName=Root}"` | `PhoneNumber="{Binding PatientDetail.PhoneNumber}"` |
| 87 | `Address="{Binding PatientDetail.Address, ElementName=Root}"` | `Address="{Binding PatientDetail.Address}"` |
| 88 | `EmergencyContactName="{Binding PatientDetail.EmergencyContactName, ElementName=Root}"` | `EmergencyContactName="{Binding PatientDetail.EmergencyContactName}"` |
| 89 | `EmergencyContactPhone="{Binding PatientDetail.EmergencyContactPhone, ElementName=Root}"` | `EmergencyContactPhone="{Binding PatientDetail.EmergencyContactPhone}"` |
| 90 | `EmergencyContactRelation="{Binding PatientDetail.EmergencyContactRelation, ElementName=Root}"` | `EmergencyContactRelation="{Binding PatientDetail.EmergencyContactRelation}"` |
| 91 | `AllergyHistory="{Binding PatientDetail.AllergyHistory, ElementName=Root}"` | `AllergyHistory="{Binding PatientDetail.AllergyHistory}"` |
| 92 | `MedicalHistory="{Binding PatientDetail.MedicalHistory, ElementName=Root}"` | `MedicalHistory="{Binding PatientDetail.MedicalHistory}"` |
| 93 | `LastVisitTime="{Binding PatientDetail.LastVisitTime, ElementName=Root}"` | `LastVisitTime="{Binding PatientDetail.LastVisitTime}"` |
| 94 | `VisitCount="{Binding PatientDetail.VisitCount, ElementName=Root}"` | `VisitCount="{Binding PatientDetail.VisitCount}"` |
| 95 | `Status="{Binding PatientDetail.Status, ElementName=Root}"` | `Status="{Binding PatientDetail.Status}"` |
| 97 | `DisableReason="{Binding PatientDetail.DisableReason, ElementName=Root}"` | `DisableReason="{Binding PatientDetail.DisableReason}"` |
| 98 | `CreatedAt="{Binding PatientDetail.CreatedAt, ElementName=Root}"` | `CreatedAt="{Binding PatientDetail.CreatedAt}"` |
| 99 | `UpdatedAt="{Binding PatientDetail.UpdatedAt, ElementName=Root}"` | `UpdatedAt="{Binding PatientDetail.UpdatedAt}"` |

- **验证**: 无 `ElementName=Root` 遗留

### 1.3 更新 MasterContent 区域绑定 (8个)

- **文件**: `PatientSelectionControl.xaml`
- **位置**: `<controls:MasterDetailLayout.MasterContent>` 内
- **变更**:

| 组件 | 原绑定 | 新绑定 |
|------|--------|--------|
| DataGridToolbar | `CreateCommand="{Binding CreateNewCommand, ElementName=Root}"` | `CreateCommand="{Binding NewPatientCommand}"` |
| DataGridToolbar | `RefreshCommand="{Binding RefreshCommand, ElementName=Root}"` | `RefreshCommand="{Binding RefreshCommand}"` |
| SearchBox | `SearchText="{Binding SearchText, Mode=TwoWay, ElementName=Root}"` | `SearchText="{Binding SearchKeyword, Mode=TwoWay}"` |
| SearchBox | `SearchCommand="{Binding SearchCommand, ElementName=Root}"` | `SearchCommand="{Binding SearchCommand}"` |
| DataGrid | `ItemsSource="{Binding Patients, ElementName=Root}"` | `ItemsSource="{Binding Patients}"` |
| DataGrid | `SelectedItem="{Binding SelectedPatient, ElementName=Root, Mode=TwoWay}"` | `SelectedItem="{Binding SelectedPatient, Mode=TwoWay}"` |

- **验证**: 命令名称已映射到 ViewModel 属性

### 1.4 更新 HasSelection 和 EmptyContent 绑定 (2个)

- **文件**: `PatientSelectionControl.xaml`
- **变更**:

| 位置 | 原绑定 | 新绑定 |
|------|--------|--------|
| MasterDetailLayout | `HasSelection="{Binding HasSelection, ElementName=Root}"` | `HasSelection="{Binding HasSelection}"` |
| EmptyState | `ActionCommand="{Binding CreateNewCommand, ElementName=Root}"` | `ActionCommand="{Binding NewPatientCommand}"` |

### 1.5 简化 PatientSelectionView.xaml

- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml`
- **变更**: 移除 PatientSelectionControl 的属性赋值

**修改前 (Lines 40-52)**:
```xml
<patientControls:PatientSelectionControl
    Grid.Row="1"
    Margin="20"
    Patients="{Binding Patients}"
    SelectedPatient="{Binding SelectedPatient, Mode=TwoWay}"
    PatientDetail="{Binding PatientDetail, Mode=TwoWay}"
    SearchText="{Binding SearchKeyword, Mode=TwoWay}"
    CreateNewCommand="{Binding NewPatientCommand}"
    RefreshCommand="{Binding RefreshCommand}"
    SearchCommand="{Binding SearchCommand}"
    SelectCommand="{Binding StartMedicalCaseCommand}"
    IsLoading="{Binding IsBusy}"
    PatientDoubleClicked="PatientSelectionControl_PatientDoubleClicked"/>
```

**修改后**:
```xml
<patientControls:PatientSelectionControl
    Grid.Row="1"
    Margin="20"
    PatientDoubleClicked="PatientSelectionControl_PatientDoubleClicked"/>
```

- **验证**: 控件正确继承 DataContext

### 1.6 编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Phase 2: 运行时验证

### 2.1 功能测试
- **验证**: 启动应用，登录 Doctor 角色
- **验证**: 导航到患者选择界面
- **验证**: 患者列表正常显示
- **验证**: 选择患者后详情正确显示 (核心测试点)
- **验证**: 搜索功能正常
- **验证**: 新建/刷新按钮功能正常
- **验证**: 双击患者跳转正常

### 2.2 绑定错误检查
- **验证**: 检查 Visual Studio 输出窗口，确保无 System.Windows.Data Error: 40

## Phase 3: 架构规范文档

### 3.1 更新 CLAUDE.md
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CLAUDE.md`
- **变更**: 添加 "XAML 绑定最佳实践" 章节，记录三种绑定模式的使用场景

## Dependencies

```
Phase 1 (代码变更)
    ↓
Phase 2 (运行时验证)
    ↓
Phase 3 (文档更新)
```

## Validation Checklist

- [x] PatientSelectionControl.xaml.cs 的 10 个 DependencyProperty 已移除
- [x] PatientSelectionControl.xaml 的 31 个 ElementName=Root 绑定已替换
- [x] PatientSelectionView.xaml 已简化
- [x] Desktop 解决方案编译通过
- [ ] 运行时无 System.Windows.Data Error: 40 错误 (需用户验证)
- [ ] 患者选择功能正常工作（列表、详情、搜索、按钮）(需用户验证)

## Notes

**关键属性映射**:

| 原 DependencyProperty | ViewModel 属性 | 说明 |
|----------------------|----------------|------|
| CreateNewCommand | NewPatientCommand | 命名差异 |
| SearchText | SearchKeyword | 命名差异 |
| SelectCommand | StartMedicalCaseCommand | 命名差异 |
| IsLoading | IsBusy | 命名差异 |

**参考实现**: `PatientMasterDetailControl.xaml:244-268`

---

**生成时间**: 2026-01-11
**执行完成时间**: 2026-01-11
**状态**: 已执行 (Phase 1+3完成，Phase 2需用户运行时验证)
