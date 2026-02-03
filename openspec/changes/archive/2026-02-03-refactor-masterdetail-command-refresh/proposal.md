# refactor-masterdetail-command-refresh

## Why

### 发现的问题

所有管理模块（用户、患者、药材、验方）的"新建"、"编辑"、"删除"按钮点击无响应。

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| `MasterDetailViewModelBase.cs` | 命令状态不刷新 | RelayCommand的CanExecute依赖代理属性变化时不更新 | 属性变化时自动刷新命令状态 |
| `SubscribeToServiceEvents()` | 事件处理不完整 | 仅转发PropertyChanged，未刷新命令 | 转发PropertyChanged并刷新相关命令 |

### 根因分析

```
问题链路:
1. CanCreateNew() => !IsEditMode && !IsBusy
2. IsEditMode/IsBusy 是代理属性，指向 MasterDetailServices 内部服务
3. 服务状态变化时触发 PropertyChanged
4. SubscribeToServiceEvents() 转发 PropertyChanged 到 ViewModel
5. ❌ 缺失: NotifyCanExecuteChanged() 未被调用
6. 结果: 按钮 IsEnabled 状态永远不更新
```

### 影响分析

- **影响模块**: Users, Patients, Herbs, Formula, MedicalCase（所有继承MasterDetailViewModelBase的模块）
- **影响命令**: CreateNewCommand, EditCommand, SaveCommand, CancelCommand, DeleteCommand
- **兼容性**: Breaking Change - 不考虑向后兼容

## What Changes

### Phase 1: 重构命令刷新机制

1. **添加命令刷新方法**
   - 在 `MasterDetailViewModelBase` 中添加 `NotifyCommandsCanExecuteChanged()` 方法
   - 统一刷新所有依赖状态属性的命令

2. **修改事件订阅逻辑**
   - `Loading.PropertyChanged` 回调中，IsBusy变化时刷新命令
   - `DetailEditor.PropertyChanged` 回调中，IsEditMode变化时刷新命令
   - `Selection.PropertyChanged` 回调中，选择变化时刷新命令

3. **命令依赖关系**

| 命令 | CanExecute | 依赖属性 |
|------|------------|----------|
| CreateNewCommand | `!IsEditMode && !IsBusy` | IsEditMode, IsBusy |
| EditCommand | `HasSelection && !IsEditMode && !IsBusy` | HasSelection, IsEditMode, IsBusy |
| SaveCommand | `IsEditMode && CurrentDetail != null && !IsBusy` | IsEditMode, CurrentDetail, IsBusy |
| CancelCommand | `IsEditMode` | IsEditMode |
| DeleteCommand | `HasSelection && !IsEditMode && !IsBusy` | HasSelection, IsEditMode, IsBusy |

### Phase 2: 编译验证与功能测试

1. 编译全解决方案
2. 验证各模块"新建"按钮可用
3. 验证编辑/删除按钮状态正确

## Architecture

### 变更影响范围

```
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/
└── ViewModels/
    └── MasterDetailViewModelBase.cs  ← 主要修改文件

影响的子类 (无需修改，自动继承修复):
├── Users/ViewModels/UsersMasterDetailViewModel.cs
├── Patients/ViewModels/PatientMasterDetailViewModel.cs
├── Herbs/ViewModels/HerbMasterDetailViewModel.cs
├── Formula/ViewModels/FormulaMasterDetailViewModel.cs
└── MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs
```

### 修改前后对比

**Before:**
```csharp
_masterDetailServices.Loading.PropertyChanged += (s, e) =>
{
    OnPropertyChanged(e.PropertyName);
    // 缺少命令刷新
};
```

**After:**
```csharp
_masterDetailServices.Loading.PropertyChanged += (s, e) =>
{
    OnPropertyChanged(e.PropertyName);
    if (e.PropertyName == nameof(ILoadingStateManager.IsBusy))
    {
        NotifyCommandsCanExecuteChanged();
    }
};

private void NotifyCommandsCanExecuteChanged()
{
    CreateNewCommand.NotifyCanExecuteChanged();
    EditCommand.NotifyCanExecuteChanged();
    SaveCommand.NotifyCanExecuteChanged();
    CancelCommand.NotifyCanExecuteChanged();
    DeleteCommand.NotifyCanExecuteChanged();
}
```

## Impact

- **文件变更**: 1个文件 (`MasterDetailViewModelBase.cs`)
- **风险等级**: Low（修改集中在基类，逻辑简单明确）
- **测试要求**: 手动验证各模块的新建/编辑/删除按钮功能

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 命令刷新过于频繁 | 仅在特定属性变化时刷新，非所有PropertyChanged |
| 遗漏某些命令 | 统一使用NotifyCommandsCanExecuteChanged()，不遗漏 |

## References

- 用户需求: 修复管理模块"新建"按钮不起作用
- 相关文件: `MasterDetailViewModelBase.cs:221-268` (SubscribeToServiceEvents)
- 相关文件: `MasterDetailViewModelBase.cs:446-450` (CanExecute方法)

---

**创建时间**: 2026-01-24
**变更类型**: Refactor
**兼容性**: Breaking Change
