# refactor-masterdetail-command-refresh 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计。修复MasterDetailViewModelBase中RelayCommand的CanExecute不刷新问题。

## 架构决策

### ADR-1: 统一命令刷新方法

**状态**: 已采纳

**背景**: 多个命令依赖相同的状态属性（IsBusy、IsEditMode、HasSelection），需要在这些属性变化时统一刷新所有相关命令。

**决策**: 添加 `NotifyCommandsCanExecuteChanged()` 私有方法，在关键属性变化时调用此方法刷新所有命令。

**后果**:
- 正面: 代码集中，易于维护；新增命令时只需在一处添加
- 负面: 每次刷新所有命令可能略有性能开销（可忽略）

### ADR-2: 按属性名精确触发刷新

**状态**: 已采纳

**背景**: PropertyChanged事件会为多个属性触发，不需要每次都刷新命令。

**决策**: 在事件回调中检查 `e.PropertyName`，仅在 `IsBusy`、`IsEditMode`、`SelectedItem` 变化时触发命令刷新。

**后果**:
- 正面: 减少不必要的刷新，提升性能
- 负面: 需要维护属性名列表

## 实现策略

### 策略选择

选择在现有 `SubscribeToServiceEvents()` 方法中增强事件处理逻辑，而非引入新的机制（如 INotifyCanExecuteChanged 接口）。原因：

1. 改动最小，风险最低
2. 不需要修改服务层代码
3. 与现有架构一致

### 关键实现点

1. **NotifyCommandsCanExecuteChanged() 方法位置**
   - 放在 `#region 详情命令` 区域末尾，CanExecute方法之后
   - 作为私有方法，不暴露给子类

2. **触发时机**
   - `Loading.PropertyChanged` → 检查 `IsBusy`
   - `DetailEditor.PropertyChanged` → 检查 `IsEditMode`
   - `Selection.PropertyChanged` → 检查 `SelectedItem`

3. **刷新的命令**
   - CreateNewCommand
   - EditCommand
   - SaveCommand
   - CancelCommand
   - DeleteCommand

## 变更清单

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs` | 1. 添加NotifyCommandsCanExecuteChanged()方法 2. 修改SubscribeToServiceEvents()中的三处事件回调 |

### 具体代码变更

#### 1. 添加命令刷新方法 (L451后插入)

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

#### 2. 修改Loading.PropertyChanged回调 (L224-227)

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

#### 3. 修改Selection.PropertyChanged回调 (L247-250)

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

#### 4. 修改DetailEditor.PropertyChanged回调 (L258-261)

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

## 依赖关系

### 变更顺序

所有变更在同一文件内，无依赖顺序要求。建议执行顺序：
1. 先添加 NotifyCommandsCanExecuteChanged() 方法
2. 再修改三处事件回调（顺序不限）

## 测试策略

### 手动测试

| 测试场景 | 验证点 |
|----------|--------|
| 初始状态 | "新建"按钮可用，"编辑"/"删除"按钮禁用 |
| 选中记录 | "编辑"/"删除"按钮变为可用 |
| 点击新建 | 进入编辑模式，"新建"/"编辑"/"删除"禁用，"保存"/"取消"可用 |
| 点击取消 | 退出编辑模式，状态恢复 |
| 加载中 | 所有操作按钮禁用 |

### 覆盖模块

- Users 用户管理
- Patients 患者管理
- Herbs 药材管理
- Formula 验方管理

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 遗漏属性名检查 | 低 | 中 | 代码审查确认三处回调都已修改 |
| 命令名称错误 | 低 | 高 | 编译时即可发现 |

## 回滚计划

如果变更失败:
1. `git checkout -- src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
2. 重新编译验证

---

**设计者**: Claude Code
**日期**: 2026-01-24
**状态**: 待审批
