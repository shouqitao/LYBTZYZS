# ViewModel开发指南

本指南基于OpenSpec `viewmodel-conventions` 规范，提供ViewModel开发的快速参考。

> **相关文档**:
> - [Component模式详解](../reference/component-patterns.md) - Components分层架构
> - [开发标准](./development-standards.md) - 用户上下文、枚举使用规范

---

## 规范速查表

| 规范 | 要求 | 说明 |
|-----|------|------|
| VM-001 | 行数限制 | 小型<200, 中型<400, 大型<600 |
| VM-002 | Components分层 | DataManager/CommandHandler/Validator |
| VM-003 | 命令初始化 | 使用CommandFactory或InitializeCommands() |
| VM-004 | 错误处理 | 使用ExecuteSafelyAsync() |
| VM-005 | 异步模式 | ConfigureAwait(false) + 取消令牌 |
| VM-006 | 导航模式 | INavigationAware + 参数验证 |
| VM-007 | 基类继承 | 继承ViewModelBase |

---

## CommandFactory使用指南

### 1. 基本配置

```csharp
public class MyViewModel : ViewModelBase
{
    private readonly CommandFactory _commandFactory;

    public MyViewModel(ILoggerFactory loggerFactory)
    {
        // 创建CommandFactory实例
        _commandFactory = loggerFactory.CreateCommandFactory(
            getIsBusy: () => IsBusy,
            setIsBusy: value => IsBusy = value,
            errorHandler: (ex, context) => HandleError(ex, context));

        InitializeCommands();
    }
}
```

### 2. 创建异步命令（带加载状态保护）

```csharp
// 推荐: 使用CommandFactory创建带保护的异步命令
SaveCommand = _commandFactory.CreateAsyncWithLoadingGuard(
    execute: ExecuteSaveAsync,
    canExecute: () => HasChanges && IsValid,
    operationName: "保存数据");

// 功能说明:
// - 执行时自动设置IsBusy=true
// - 完成后自动设置IsBusy=false
// - IsBusy期间自动禁用CanExecute
// - 异常自动调用errorHandler
// - OperationCanceledException不触发错误
```

### 3. 创建带参数的命令

```csharp
// 异步带参数命令
DeleteCommand = _commandFactory.CreateWithParameter<PatientDto>(
    execute: async patient => await DeletePatientAsync(patient),
    canExecute: patient => patient != null && CanDelete,
    operationName: "删除患者");

// 同步带参数命令
SelectCommand = _commandFactory.CreateSyncWithParameter<ItemDto>(
    execute: item => SelectedItem = item,
    canExecute: item => item != null);
```

### 4. 创建简单同步命令

```csharp
ClearCommand = _commandFactory.CreateSync(
    execute: () => ClearForm(),
    canExecute: () => HasData);
```

### 5. 完整示例

```csharp
public class PatientListViewModel : ViewModelBase, INavigationAware
{
    private readonly IPatientService _patientService;
    private readonly CommandFactory _commandFactory;

    public DelegateCommand RefreshCommand { get; private set; }
    public DelegateCommand<PatientDto> SelectCommand { get; private set; }
    public DelegateCommand<PatientDto> DeleteCommand { get; private set; }

    public PatientListViewModel(
        IPatientService patientService,
        ILoggerFactory loggerFactory)
    {
        _patientService = patientService;

        _commandFactory = loggerFactory.CreateCommandFactory(
            () => IsBusy,
            value => IsBusy = value,
            (ex, context) => ErrorMessage = $"{context}失败: {ex.Message}");

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = _commandFactory.CreateAsyncWithLoadingGuard(
            LoadPatientsAsync,
            operationName: "刷新患者列表");

        SelectCommand = _commandFactory.CreateSyncWithParameter<PatientDto>(
            p => SelectedPatient = p,
            p => p != null);

        DeleteCommand = _commandFactory.CreateWithParameter<PatientDto>(
            DeletePatientAsync,
            p => p != null && !p.HasActiveCase,
            operationName: "删除患者");
    }

    private async Task LoadPatientsAsync()
    {
        var result = await _patientService.GetAllAsync();
        Patients = new ObservableCollection<PatientDto>(result);
    }

    private async Task DeletePatientAsync(PatientDto patient)
    {
        await _patientService.DeleteAsync(patient.Id);
        Patients.Remove(patient);
    }
}
```

---

## 错误处理模式

### ExecuteSafelyAsync（ViewModelBase内置）

```csharp
// 在ViewModel中使用
await ExecuteSafelyAsync(async () =>
{
    var data = await _service.LoadDataAsync();
    Items = new ObservableCollection<ItemDto>(data);
}, "加载数据");

// ExecuteSafelyAsync功能:
// - 捕获并记录异常
// - 设置IsBusy状态
// - 支持操作名称用于日志
```

### CommandFactory vs ExecuteSafelyAsync

| 场景 | 推荐方式 |
|-----|---------|
| UI命令绑定 | CommandFactory |
| 导航加载 | ExecuteSafelyAsync |
| 事件处理 | ExecuteSafelyAsync |
| 定时任务 | ExecuteSafelyAsync |

---

## 导航模式

### INavigationAware实现

```csharp
public class DetailViewModel : ViewModelBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext context)
    {
        // 获取导航参数
        var id = context.Parameters.GetValue<int>("Id");
        var mode = context.Parameters.GetValue<EditMode>("Mode");

        // 验证参数
        if (id <= 0)
        {
            _logger.LogWarning("无效的导航参数: Id={Id}", id);
            return;
        }

        // 异步加载数据
        _ = LoadDataAsync(id);
    }

    public bool IsNavigationTarget(NavigationContext context)
    {
        // 判断是否复用当前实例
        var id = context.Parameters.GetValue<int>("Id");
        return CurrentId == id;
    }

    public void OnNavigatedFrom(NavigationContext context)
    {
        // 清理资源、取消订阅
        _eventAggregator.GetEvent<SomeEvent>().Unsubscribe(_token);
    }
}
```

---

## 异步模式

### ConfigureAwait使用

```csharp
// Service层: 使用ConfigureAwait(false)
public async Task<List<PatientDto>> GetAllAsync()
{
    var entities = await _repository.GetAllAsync()
        .ConfigureAwait(false);  // 不需要回到UI线程
    return _mapper.Map<List<PatientDto>>(entities);
}

// ViewModel层: 不使用ConfigureAwait(false)
private async Task LoadDataAsync()
{
    var data = await _service.GetAllAsync();  // 需要回到UI线程更新
    Patients = new ObservableCollection<PatientDto>(data);
}
```

### 取消令牌

```csharp
public class SearchViewModel : ViewModelBase
{
    private CancellationTokenSource? _searchCts;

    private async Task SearchAsync(string keyword)
    {
        // 取消之前的搜索
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _searchCts.Token);  // 防抖
            var results = await _service.SearchAsync(keyword, _searchCts.Token);
            SearchResults = new ObservableCollection<ResultDto>(results);
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
    }
}
```

---

## 快速检查清单

开发新ViewModel时，确保:

- [ ] 继承 `ViewModelBase`
- [ ] 使用 `CommandFactory` 创建命令
- [ ] 命令初始化放在 `InitializeCommands()` 方法
- [ ] 异步操作使用 `ExecuteSafelyAsync` 或 CommandFactory
- [ ] 实现 `INavigationAware` 处理导航
- [ ] 大型ViewModel拆分为Components（参考component-patterns.md）
- [ ] 错误消息用户友好，日志详细

---

## 对话框使用指南

> **相关规范**: [dialog-patterns spec](../../openspec/specs/dialog-patterns/spec.md)

### 服务选择

| 场景 | 服务 | 示例 |
|-----|------|------|
| 确认操作 | ICommonDialogService | 删除确认、保存确认 |
| 操作反馈 | IUserNotificationService | 成功/失败提示 |
| 自定义对话框 | IDialogService (Prism) | 患者详情、处方编辑 |

### 确认对话框

```csharp
public class PatientViewModel : UnifiedViewModelBase
{
    private readonly ICommonDialogService _dialogService;

    private async Task DeletePatientAsync(PatientDto patient)
    {
        // 简单确认
        var confirmed = await _dialogService.ShowConfirmAsync(
            "确定要删除此患者吗？",
            "删除确认");

        if (!confirmed) return;

        await _patientService.DeleteAsync(patient.Id);
        await ShowSuccessAsync("患者已删除");
    }

    private async Task<bool> SaveChangesBeforeLeaveAsync()
    {
        // 三选项确认（保存/不保存/取消）
        var result = await _dialogService.ShowTripleChoiceAsync(
            "您有未保存的更改，是否保存？",
            "离开确认");

        return result switch
        {
            TripleChoiceResult.Yes => await SaveAndReturnTrueAsync(),
            TripleChoiceResult.No => true,  // 不保存但继续
            TripleChoiceResult.Cancel => false  // 取消导航
        };
    }
}
```

### 通知显示

```csharp
// 使用ViewModelBase提供的便捷方法
await ShowSuccessAsync("保存成功");
await ShowErrorAsync("保存失败: " + errorMessage);
await ShowWarningAsync("数据已过期，请刷新");
await ShowInfoAsync("系统将在5分钟后维护");

// 或直接使用IUserNotificationService
await _notification.ShowSuccessAsync("操作完成");
await _notification.HandleExceptionAsync(ex, "保存患者时");
```

### 禁止的做法

```csharp
// 错误: 直接使用MessageBox
MessageBox.Show("保存成功");  // 禁止

// 正确: 使用服务
await _dialogService.ShowInfoAsync("保存成功");
```

---

## 样式使用指南

> **相关规范**: Phase 2 样式统一 (cleanup-ui-layer)

### 全局样式资源

样式文件位于 `Shell/Styles/`:

| 文件 | 内容 |
|-----|------|
| Colors.xaml | 颜色系统 (PrimaryBrush, SurfaceBrush等) |
| Typography.xaml | 文字样式 (TitleStyle, BodyStyle等) |
| Controls.xaml | 控件样式 (ButtonStyle, TextBoxStyle等) |

### 常用颜色

```xml
<!-- 主题色 -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#2E7D32"/>
<SolidColorBrush x:Key="PrimaryLightBrush" Color="#60AD5E"/>

<!-- 背景色 -->
<SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>
<SolidColorBrush x:Key="BackgroundBrush" Color="#F5F5F5"/>

<!-- 文字色 -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#212121"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#757575"/>

<!-- 状态色 -->
<SolidColorBrush x:Key="SuccessBrush" Color="#4CAF50"/>
<SolidColorBrush x:Key="WarningBrush" Color="#FF9800"/>
<SolidColorBrush x:Key="ErrorBrush" Color="#F44336"/>
```

### 在XAML中使用

```xml
<UserControl>
    <!-- 使用全局Brush而非硬编码颜色 -->
    <Border Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}">

        <TextBlock Text="标题"
                   Foreground="{StaticResource TextPrimaryBrush}"
                   Style="{StaticResource TitleStyle}"/>

        <Button Content="保存"
                Style="{StaticResource PrimaryButtonStyle}"/>
    </Border>
</UserControl>
```

### 禁止的做法

```xml
<!-- 错误: 硬编码颜色 -->
<Border Background="#FFFFFF"/>  <!-- 禁止 -->
<TextBlock Foreground="#212121"/>  <!-- 禁止 -->

<!-- 正确: 使用全局资源 -->
<Border Background="{StaticResource SurfaceBrush}"/>
<TextBlock Foreground="{StaticResource TextPrimaryBrush}"/>
```

---

## 导航服务使用指南

> **相关规范**: [viewmodel-conventions spec](../../openspec/specs/viewmodel-conventions/spec.md)

### 区域导航

```csharp
// 标准区域导航
_regionManager.RequestNavigate(
    RegionNames.ContentRegion,
    nameof(PatientDetailView),
    new NavigationParameters { { "PatientId", patientId } });
```

### 角色导航

```csharp
// 根据用户角色导航到主页
_roleNavigationService.NavigateToRoleHome(user.Role);
```

### 确认导航

```csharp
public class EditViewModel : ViewModelBase, IConfirmNavigationRequest
{
    public void ConfirmNavigationRequest(
        NavigationContext context,
        Action<bool> continuationCallback)
    {
        if (!HasUnsavedChanges)
        {
            continuationCallback(true);
            return;
        }

        // 异步确认
        _ = ConfirmNavigationAsync(continuationCallback);
    }

    private async Task ConfirmNavigationAsync(Action<bool> callback)
    {
        var result = await _dialogService.ShowTripleChoiceAsync(
            "有未保存的更改，是否保存？");

        var shouldContinue = result switch
        {
            TripleChoiceResult.Yes => await SaveAsync(),
            TripleChoiceResult.No => true,
            _ => false
        };

        callback(shouldContinue);
    }
}
```

---

## 快速检查清单

开发新ViewModel时，确保:

- [ ] 继承 `ViewModelBase`
- [ ] 使用 `CommandFactory` 创建命令
- [ ] 命令初始化放在 `InitializeCommands()` 方法
- [ ] 异步操作使用 `ExecuteSafelyAsync` 或 CommandFactory
- [ ] 实现 `INavigationAware` 处理导航
- [ ] 大型ViewModel拆分为Components（参考component-patterns.md）
- [ ] 错误消息用户友好，日志详细
- [ ] **使用对话框服务而非MessageBox**
- [ ] **使用全局样式而非硬编码颜色**

---

*OpenSpec: refactor-viewmodel-layer Phase 4.2, cleanup-ui-layer Phase 5*
*最后更新: 2025-12*
