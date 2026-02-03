# refactor-masterdetail-command-refresh Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Low
- **预估工作量**: 15分钟

## Phase 1: 重构命令刷新机制

### 1.1 添加NotifyCommandsCanExecuteChanged方法 [TODO]
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- **位置**: L451 (`#endregion` 之前，CanDelete方法之后)
- **变更**: 添加以下代码
```csharp
/// <summary>
/// 通知所有命令刷新CanExecute状态
/// </summary>
private void NotifyCommandsCanExecuteChanged()
{
    CreateNewCommand.NotifyCanExecuteChanged();
    EditCommand.NotifyCanExecuteChanged();
    SaveCommand.NotifyCanExecuteChanged();
    CancelCommand.NotifyCanExecuteChanged();
    DeleteCommand.NotifyCanExecuteChanged();
}
```
- **验证**: 方法签名正确，无编译错误

### 1.2 修改Loading.PropertyChanged事件处理 [TODO]
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- **位置**: L224-227
- **变更**: 替换为
```csharp
// Loading状态变更
_masterDetailServices.Loading.PropertyChanged += (s, e) =>
{
    OnPropertyChanged(e.PropertyName);
    if (e.PropertyName == nameof(ILoadingStateManager.IsBusy))
    {
        NotifyCommandsCanExecuteChanged();
    }
};
```
- **验证**: 编译通过

### 1.3 修改Selection.PropertyChanged事件处理 [TODO]
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- **位置**: L247-250
- **变更**: 替换为
```csharp
// Selection变更
_masterDetailServices.Selection.PropertyChanged += (s, e) =>
{
    OnPropertyChanged(e.PropertyName);
    if (e.PropertyName == nameof(ISelectionService<TListItem>.SelectedItem))
    {
        NotifyCommandsCanExecuteChanged();
    }
};
```
- **验证**: 编译通过

### 1.4 修改DetailEditor.PropertyChanged事件处理 [TODO]
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- **位置**: L258-261
- **变更**: 替换为
```csharp
// DetailEditor变更
_masterDetailServices.DetailEditor.PropertyChanged += (s, e) =>
{
    OnPropertyChanged(e.PropertyName);
    if (e.PropertyName == nameof(IDetailEditorService<TDetail>.IsEditMode))
    {
        NotifyCommandsCanExecuteChanged();
    }
};
```
- **验证**: 编译通过

### 1.5 编译验证 [TODO]
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Dependencies

```
1.1 NotifyCommandsCanExecuteChanged ──┐
                                      │
1.2 Loading事件处理 ──────────────────┼──> 1.5 编译验证
                                      │
1.3 Selection事件处理 ────────────────┤
                                      │
1.4 DetailEditor事件处理 ─────────────┘
```

**说明**: 1.1必须先完成（定义方法），1.2-1.4依赖1.1但可并行执行

## Validation Checklist

- [ ] Desktop解决方案编译通过
- [ ] 用户管理"新建"按钮可用
- [ ] 患者管理"新建"按钮可用
- [ ] 药材管理"新建"按钮可用
- [ ] 验方管理"新建"按钮可用
- [ ] 编辑/删除按钮状态正确响应选择变化

## Notes

- 修改仅涉及基类MasterDetailViewModelBase
- 所有子类自动继承修复，无需逐个修改
- 不考虑向后兼容，直接重构
- 需要添加using: `using LYBT.Desktop.Infrastructure.Services;` (如果缺少ILoadingStateManager等接口引用)

---

**生成时间**: 2026-01-24
**状态**: 完整版 (设计阶段已细化)
