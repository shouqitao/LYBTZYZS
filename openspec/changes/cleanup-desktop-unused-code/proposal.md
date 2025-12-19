# Change: Desktop层未使用代码清理

## Why

Desktop层经过多次迭代开发，积累了一些未使用的代码，包括：
- 未注册的Dialog和ViewModel
- 未使用的Service类
- 未实现的接口定义
- 不规范的组件使用方式

这些代码增加了维护成本，降低了代码可读性，需要在v1.0.0发布前清理。

## What Changes

### Phase 1: 清理未使用的UI组件
- **删除** Shell/Dialogs/Views/ErrorDetailsDialog.xaml 及对应ViewModel
- **删除** Shell/Dialogs/Views/InformationDialog.xaml 及对应ViewModel

### Phase 2: 清理未使用的Service代码
- **删除** MedicalCase/Services/MedicalCaseStatusPresenter.cs（未注册未使用）
- **删除** MedicalCase/Services/MedicalCaseEventCoordinator.cs（TODO未实现）
- **评估** Users/Services/UserDataManager.cs 和 UserValidator.cs 的使用情况

### Phase 3: 修复不规范使用
- **修复** UnfinishedCaseDialog 改用RegisterDialog方式注册

### Phase 4: 清理孤立接口
- **评估** MedicalCase/Interfaces/IDataProvider.cs 是否需要保留

## Impact

- Affected specs: desktop-structure-cleanup
- Affected code:
  - Shell/Dialogs/ (2 dialogs)
  - MedicalCase/Services/ (2 services)
  - Users/Services/ (2 services - 待评估)
  - Patients/Dialogs/ (1 dialog - 修复使用方式)
  - MedicalCase/Interfaces/ (1 interface - 待评估)

## Risk Assessment

- **低风险**: ErrorDetailsDialog、InformationDialog - 确认未注册未使用
- **低风险**: MedicalCaseStatusPresenter - 确认未注册未使用
- **中风险**: MedicalCaseEventCoordinator - 需确认是否有后续计划
- **需评估**: Users服务和IDataProvider接口需进一步确认
