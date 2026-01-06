# viewmodel-conventions Spec Delta

## MODIFIED Requirements

### Requirement: VM-007 Base Class Inheritance

ViewModel MUST 继承适当的基类。**标准化为CommunityToolkit.Mvvm基础 + Prism导航组合模式**。

**精简后的基类体系** (5个核心基类):

| 基类 | 父类 | 用途 |
|------|------|------|
| `CoreViewModelBase` | ObservableObject | 最小核心基类，提供IsBusy、Logger、EventAggregator |
| `NavigableViewModelBase` | CoreViewModelBase | 导航支持，实现INavigationAware、IRegionMemberLifetime |
| `DialogViewModelBase` | CoreViewModelBase | 对话框基类，实现IDialogAware |
| `ValidatingViewModelBase` | NavigableViewModelBase | 验证支持，实现INotifyDataErrorInfo |
| `PageViewModelBase` | NavigableViewModelBase | 主内容页面，添加PageTitle、RefreshCommand |

**技术栈**:
- ViewModel层: CommunityToolkit.Mvvm (ObservableObject + 源生成器)
- Item类: Prism BindableBase (Mapperly兼容性要求)
- 导航/对话框: Prism INavigationAware/IDialogAware

#### Scenario: 新ViewModel继承选择
- **GIVEN** 创建新的ViewModel
- **WHEN** 选择基类
- **THEN** 主内容页面继承 `PageViewModelBase`
- **AND** 对话框继承 `DialogViewModelBase`
- **AND** 带验证的表单继承 `ValidatingViewModelBase`
- **AND** 简单ViewModel继承 `CoreViewModelBase`

#### Scenario: Item类保持BindableBase
- **GIVEN** 创建绑定到列表的Item类（如PatientItem, HerbItem）
- **WHEN** 选择基类
- **THEN** 继承 `BindableBase` (Prism)
- **AND** 使用显式属性定义（非[ObservableProperty]）
- **AND** 确保Mapperly源生成器兼容

#### Scenario: 废弃基类处理
- **GIVEN** 代码使用废弃基类（ViewModelBase, LightViewModelBase, UnifiedViewModelBase等）
- **WHEN** 进行重构
- **THEN** 根据功能需求选择新的核心基类
- **AND** 迁移属性到[ObservableProperty]
- **AND** 迁移命令到[RelayCommand]

---

### Requirement: VM-003 Command Initialization Pattern

命令初始化 MUST 使用CommunityToolkit.Mvvm源生成器模式（统一标准）。

**规范**:
- **标准模式**: 使用 `[RelayCommand]` 特性
- **废弃模式**: DelegateCommand仅在遗留代码中保留
- CanExecute条件 SHALL 使用 `[NotifyCanExecuteChangedFor]` 自动刷新
- 异步命令 SHALL 使用 `[RelayCommand]` + `async Task` 方法

#### Scenario: 标准命令模式
- **GIVEN** 需要创建命令
- **WHEN** 定义命令
- **THEN** 使用以下模式:
```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync()
{
    // 业务逻辑
}

private bool CanSave() => !IsBusy && !HasErrors;
```

#### Scenario: 命令状态自动刷新
- **GIVEN** 命令可用性依赖属性
- **WHEN** 属性变化
- **THEN** 使用`[NotifyCanExecuteChangedFor]`自动通知:
```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private bool _isBusy;
```

#### Scenario: 带参数命令
- **GIVEN** 需要创建带参数命令
- **WHEN** 定义命令
- **THEN** 使用以下模式:
```csharp
[RelayCommand]
private void SelectItem(HerbItem item)
{
    SelectedItem = item;
}
```

#### Scenario: 命令取消支持
- **GIVEN** 长时间异步操作需要取消
- **WHEN** 定义命令
- **THEN** 使用`IncludeCancelCommand = true`:
```csharp
[RelayCommand(IncludeCancelCommand = true)]
private async Task LoadDataAsync(CancellationToken token)
{
    await _service.LoadAsync(token);
}
// 自动生成 LoadDataCancelCommand
```

---

## ADDED Requirements

### Requirement: VM-020 事件订阅生命周期管理

ViewModel事件订阅 MUST 使用EventSubscriptionManager自动管理生命周期。

**规范**:
- 事件订阅 SHALL 通过`Events`属性进行
- EventSubscriptionManager SHALL 在Dispose时自动清理所有订阅
- SHALL NOT 手动管理SubscriptionToken

#### Scenario: 标准事件订阅
- **GIVEN** ViewModel需要订阅事件
- **WHEN** 在OnNavigatedTo中订阅
- **THEN** 使用Events管理器:
```csharp
public override void OnNavigatedTo(NavigationContext context)
{
    Events.Subscribe<PatientSelectedEvent, PatientSelectedPayload>(
        payload => OnPatientSelected(payload));
}
```

#### Scenario: 事件过滤订阅
- **GIVEN** 只需处理特定条件的事件
- **WHEN** 订阅事件
- **THEN** 使用带过滤器的Subscribe:
```csharp
Events.Subscribe<CaseUpdatedEvent, CaseUpdatedPayload>(
    OnCaseUpdated,
    payload => payload.CaseId == _currentCaseId);
```

#### Scenario: 自动资源清理
- **GIVEN** ViewModel被销毁
- **WHEN** Dispose被调用
- **THEN** Events管理器自动取消所有订阅
- **AND** 无需手动调用Unsubscribe

---

### Requirement: VM-021 导航参数提取规范

导航参数提取 MUST 使用类型安全的辅助方法。

**规范**:
- 使用`GetNavigationParameter<T>`提取参数
- 必需参数缺失时 SHALL 抛出ArgumentException
- 可选参数 SHALL 使用默认值重载

#### Scenario: 必需参数提取
- **GIVEN** 导航需要必需参数
- **WHEN** 在OnNavigatedTo中提取
- **THEN** 使用类型安全方法:
```csharp
public override void OnNavigatedTo(NavigationContext context)
{
    var patientId = GetNavigationParameter<Guid>(context, "PatientId");
    _ = LoadPatientAsync(patientId);
}
```

#### Scenario: 可选参数提取
- **GIVEN** 导航参数可选
- **WHEN** 提取参数
- **THEN** 使用带默认值的重载:
```csharp
var isReadOnly = GetNavigationParameter(context, "IsReadOnly", false);
```

---

### Requirement: VM-022 导航离开确认规范

有未保存数据的ViewModel MUST 实现导航确认机制。

**规范**:
- 实现`IConfirmNavigationRequest`接口
- 使用`HasUnsavedChanges`属性跟踪状态
- 使用统一的确认对话框提示用户

#### Scenario: 未保存数据离开确认
- **GIVEN** ViewModel有未保存的编辑
- **WHEN** 用户尝试导航离开
- **THEN** 显示三选对话框（保存/不保存/取消）
- **AND** 根据用户选择执行对应操作

#### Scenario: HasUnsavedChanges自动跟踪
- **GIVEN** 使用ValidatingViewModelBase
- **WHEN** 任何[ObservableProperty]属性变化
- **THEN** HasUnsavedChanges自动设为true
- **AND** 保存成功后重置为false

---

### Requirement: VM-023 异步命令异常处理规范

异步命令执行 MUST 使用统一的异常处理机制。

**规范**:
- 使用`ExecuteWithErrorHandlingAsync`包装异步操作
- 异常 SHALL 自动记录到Logger
- 用户友好消息 SHALL 通过统一对话框显示

#### Scenario: 标准异步操作包装
- **GIVEN** 执行可能失败的异步操作
- **WHEN** 定义命令方法
- **THEN** 使用错误处理包装:
```csharp
[RelayCommand]
private async Task SaveAsync()
{
    await ExecuteWithErrorHandlingAsync(async () =>
    {
        await _service.SaveAsync(CurrentItem);
        HasUnsavedChanges = false;
    }, "保存失败");
}
```

#### Scenario: 静默错误处理
- **GIVEN** 后台操作失败不需要用户感知
- **WHEN** 执行操作
- **THEN** 使用`showErrorToUser: false`:
```csharp
await ExecuteWithErrorHandlingAsync(
    async () => await _service.RefreshCacheAsync(),
    "刷新缓存失败",
    showErrorToUser: false);
```
