# viewmodel-conventions Specification

## Purpose

定义WPF/Prism MVVM架构中ViewModel层的设计规范和最佳实践，确保代码一致性、可维护性和可测试性。
## Requirements
### Requirement: VM-001 ViewModel大小限制规范

单个ViewModel类 MUST 控制在合理的代码行数内，超出限制需拆分。

**规范**:
- 简单ViewModel SHALL NOT 超过200行
- 中型ViewModel SHALL NOT 超过400行
- 复杂ViewModel SHALL NOT 超过600行（需配合Components拆分）
- 超过600行 MUST 拆分为Coordinator + Components模式

#### Scenario: 简单ViewModel
- **GIVEN** 仅包含数据展示的ViewModel
- **WHEN** 无复杂业务逻辑
- **THEN** 代码行数 SHALL NOT 超过200行
- **AND** 仅包含属性绑定和简单命令

#### Scenario: 复杂ViewModel拆分
- **GIVEN** ViewModel超过600行
- **WHEN** 需要重构
- **THEN** 创建对应的Components目录
- **AND** 将职责拆分到独立Component类
- **AND** ViewModel作为Coordinator协调各Component

---

### Requirement: VM-002 Components分层模式规范

大型ViewModel MUST 使用Components分层模式进行职责分离。

**规范**:
- Component SHALL 放置在`ViewModels/Components/`目录下
- 命名 SHALL 为`{Module}{Responsibility}.cs`（如`MedicalCaseDataManager.cs`）
- Component SHALL 通过构造函数注入到ViewModel
- Component SHALL 不直接继承ViewModelBase

#### Scenario: 标准Component结构
- **GIVEN** 需要创建ViewModel Component
- **WHEN** 设计Component
- **THEN** 创建在`ViewModels/Components/`目录
- **AND** 实现单一职责
- **AND** 通过DI容器注册

#### Scenario: Component职责划分
- **GIVEN** 大型ViewModel需要拆分
- **WHEN** 划分Component职责
- **THEN** DataManager/DataLoader负责数据加载和缓存
- **AND** CommandHandler负责CRUD操作
- **AND** Validator负责业务规则验证
- **AND** EventCoordinator负责事件协调

#### Scenario: ViewModel协调器模式
- **GIVEN** ViewModel使用Components
- **WHEN** 组织代码结构
- **THEN** ViewModel仅负责协调各Component
- **AND** ViewModel处理UI绑定和导航
- **AND** 业务逻辑委托给对应Component

---

### Requirement: VM-003 命令初始化模式规范

ViewModel中的命令 MUST 使用统一的初始化模式。

**规范**:
- 命令 SHALL 在构造函数中初始化
- 异步命令 SHALL 使用`DelegateCommand`配合async void执行方法
- 命令 SHALL 实现CanExecute条件判断
- 长时间操作 SHALL 配合IsBusy状态控制

#### Scenario: 标准命令初始化
- **GIVEN** ViewModel需要定义命令
- **WHEN** 初始化命令
- **THEN** 使用`new DelegateCommand(Execute, CanExecute)`
- **AND** 命令属性为只读（仅getter）

#### Scenario: 异步命令模式
- **GIVEN** 命令需要执行异步操作
- **WHEN** 定义执行方法
- **THEN** 执行方法签名为`async void ExecuteXxx()`
- **AND** 方法内部使用try-catch包装
- **AND** 设置IsBusy状态

#### Scenario: 命令可用性控制
- **GIVEN** 命令有执行条件
- **WHEN** 条件变化
- **THEN** 使用`.ObservesProperty(() => Property)`自动刷新
- **AND** 或调用`RaiseCanExecuteChanged()`手动刷新

---

### Requirement: VM-004 错误处理模式规范

ViewModel层 MUST 统一处理和展示错误信息。

**规范**:
- 所有async void方法 SHALL 使用try-catch包装
- 用户可见错误 SHALL 通过ShowErrorMessageAsync展示
- 日志 SHALL 记录完整异常信息
- SHALL NOT 让异常逃逸到UI线程未处理

#### Scenario: 标准错误处理
- **GIVEN** 异步命令执行
- **WHEN** 发生异常
- **THEN** 捕获异常
- **AND** 调用`Logger.LogError(ex, "操作描述")`
- **AND** 调用`ShowErrorMessageAsync("用户友好消息")`

#### Scenario: 静默错误处理
- **GIVEN** 后台操作失败
- **WHEN** 不需要用户感知
- **THEN** 仅记录日志
- **AND** 不显示错误对话框

#### Scenario: 验证错误展示
- **GIVEN** 业务验证失败
- **WHEN** 展示错误
- **THEN** 使用ValidationMessage属性
- **AND** 绑定到UI验证提示

---

### Requirement: VM-005 异步模式一致性规范

ViewModel中的异步操作 MUST 遵循统一的异步模式。

**规范**:
- 数据加载 SHALL 使用`async Task LoadDataAsync()`
- 命令执行 SHALL 使用`async void ExecuteXxx()`
- SHALL 使用`ConfigureAwait(true)`保持UI线程上下文
- 长时间操作 SHALL 设置IsBusy状态

#### Scenario: 数据加载模式
- **GIVEN** ViewModel需要加载数据
- **WHEN** 执行加载
- **THEN** 方法签名为`public async Task LoadDataAsync()`
- **AND** 设置`IsBusy = true`
- **AND** finally块中设置`IsBusy = false`

#### Scenario: 并发控制
- **GIVEN** 多个异步操作可能同时执行
- **WHEN** 需要避免重入
- **THEN** 检查IsBusy状态
- **AND** 或使用SemaphoreSlim控制并发

---

### Requirement: VM-006 导航模式规范

ViewModel MUST 使用Prism标准导航模式。

**规范**:
- 导航 SHALL 使用`IRegionManager.RequestNavigate()`
- 导航参数 SHALL 使用`NavigationParameters`
- 接收导航 SHALL 实现`INavigationAware`
- 导航确认 SHALL 实现`IConfirmNavigationRequest`

#### Scenario: 标准导航
- **GIVEN** 需要导航到新视图
- **WHEN** 执行导航
- **THEN** 使用`RegionManager.RequestNavigate(regionName, viewName, parameters)`
- **AND** 传递必要的导航参数

#### Scenario: 接收导航参数
- **GIVEN** ViewModel实现INavigationAware
- **WHEN** 导航到该ViewModel
- **THEN** `OnNavigatedTo`接收NavigationContext
- **AND** 从context.Parameters提取参数

#### Scenario: 导航离开确认
- **GIVEN** ViewModel有未保存数据
- **WHEN** 用户尝试导航离开
- **THEN** 实现`IConfirmNavigationRequest`
- **AND** 在`ConfirmNavigationRequest`中检查并提示

---

### Requirement: VM-007 基类继承规范

所有ViewModel MUST 继承项目定义的基类。

**规范**:
- 标准ViewModel SHALL 继承`UnifiedViewModelBase`
- 基类提供IsBusy、Logger、EventAggregator等基础设施
- SHALL NOT 直接继承`BindableBase`
- 对话框ViewModel SHALL 继承`DialogViewModelBase`

#### Scenario: 标准ViewModel基类
- **GIVEN** 创建新的ViewModel
- **WHEN** 定义类继承
- **THEN** 继承`UnifiedViewModelBase`
- **AND** 构造函数注入必要依赖
- **AND** 调用base构造函数

#### Scenario: 对话框ViewModel
- **GIVEN** 创建Prism对话框ViewModel
- **WHEN** 定义类继承
- **THEN** 继承`DialogViewModelBase`
- **AND** 实现`IDialogAware`接口方法

#### Scenario: 基类功能使用
- **GIVEN** ViewModel继承UnifiedViewModelBase
- **WHEN** 需要通用功能
- **THEN** 使用`SetIsBusy(true, "消息")`设置忙状态
- **AND** 使用`ShowErrorMessageAsync()`显示错误
- **AND** 使用`Logger`记录日志

---

### Requirement: VM-001 ViewModel Size Guidelines

单个ViewModel类 MUST NOT 超过500行代码。

**规范**:
- ViewModel类 SHALL NOT 超过500行（包括注释）
- 超过500行 SHOULD 拆分为Components或子ViewModel
- 拆分后的主ViewModel SHOULD 作为协调器（Coordinator）

#### Scenario: Large ViewModel warning
- **GIVEN** ViewModel文件超过500行
- **WHEN** 进行Code Review
- **THEN** MUST 评估拆分必要性
- **AND** 记录技术债务或执行拆分

#### Scenario: ViewModel decomposition
- **GIVEN** ViewModel需要拆分
- **WHEN** 执行拆分
- **THEN** 提取CommandHandler处理CRUD命令
- **AND** 提取DataManager处理数据加载和缓存
- **AND** 提取Validator处理业务验证
- **AND** 主ViewModel保留协调和UI状态管理

### Requirement: VM-002 Components Pattern

复杂业务模块 MUST 采用Components分层模式。

**规范**:
- Components文件夹位于模块的ViewModels目录下
- 每个Component SHOULD 专注单一职责
- Component通过构造函数注入到ViewModel

#### Scenario: Standard Components structure
- **GIVEN** 业务模块需要Components分层
- **WHEN** 创建Components
- **THEN** 目录结构为 `ViewModels/Components/{Component}.cs`
- **AND** 命名为 `{Entity}{Responsibility}.cs`（如 `MedicalCaseDataManager.cs`）

#### Scenario: CommandHandler responsibility
- **GIVEN** CommandHandler组件
- **WHEN** 定义职责
- **THEN** 处理CRUD命令（Create, Read, Update, Delete）
- **AND** 调用Repository/Api执行操作
- **AND** NOT 包含数据加载逻辑

#### Scenario: DataManager responsibility
- **GIVEN** DataManager组件
- **WHEN** 定义职责
- **THEN** 处理数据加载和刷新
- **AND** 管理数据缓存（如适用）
- **AND** NOT 包含命令处理逻辑

#### Scenario: Validator responsibility
- **GIVEN** Validator组件
- **WHEN** 定义职责
- **THEN** 处理业务规则验证
- **AND** 返回验证结果
- **AND** NOT 直接修改数据

### Requirement: VM-003 Command Initialization Pattern

DelegateCommand初始化 MUST 使用统一模式。

**规范**:
- 异步命令 SHALL 使用 `DelegateCommand(async () => await Method())`
- CanExecute条件 SHALL 使用 `ObservesProperty()`
- 常用条件（IsLoading, IsBusy）SHOULD 通过基类方法简化

#### Scenario: Async command with loading guard
- **GIVEN** 需要创建异步命令
- **WHEN** 命令需要在加载时禁用
- **THEN** 使用以下模式:
```csharp
AddCommand = new DelegateCommand(
    async () => await OnExecuteAddAsync(),
    () => !IsLoading && !IsBusy)
    .ObservesProperty(() => IsLoading)
    .ObservesProperty(() => IsBusy);
```

#### Scenario: Parameterized command
- **GIVEN** 需要创建带参数命令
- **WHEN** 参数为实体对象
- **THEN** 使用以下模式:
```csharp
ViewDetailsCommand = new DelegateCommand<TEntity>(
    ExecuteViewDetails,
    entity => entity != null);
```

### Requirement: VM-004 Error Handling Pattern

ViewModel错误处理 MUST 使用统一模式。

**规范**:
- 业务操作 SHALL 使用 `ExecuteSafelyAsync<T>()` 包装
- 异常 SHALL 记录到Logger
- 用户友好消息 SHALL 通过 `UserNotificationService` 显示

#### Scenario: Standard error handling
- **GIVEN** ViewModel执行可能失败的操作
- **WHEN** 操作抛出异常
- **THEN** 调用 `Logger.LogError(ex, "操作描述")`
- **AND** 调用 `UserNotificationService.HandleExceptionAsync(ex, contextMessage)`
- **AND** NOT 直接设置 `ErrorMessage = ex.Message`

#### Scenario: ExecuteSafelyAsync usage
- **GIVEN** 需要执行异步操作
- **WHEN** 操作可能失败
- **THEN** 使用以下模式:
```csharp
await ExecuteSafelyAsync(async () =>
{
    // 业务逻辑
}, "操作描述");
```

### Requirement: VM-005 Async Pattern Consistency

异步方法 MUST 遵循统一的命名和执行模式。

**规范**:
- 异步方法名 SHALL 以 `Async` 后缀结尾
- SHALL NOT 使用 `.Wait()` 或 `.Result` 同步阻塞
- 命令处理方法 SHALL 命名为 `OnExecute{Action}Async`

#### Scenario: Async method naming
- **GIVEN** 创建异步方法
- **WHEN** 命名方法
- **THEN** 名称以 `Async` 结尾
- **AND** 返回 `Task` 或 `Task<T>`

#### Scenario: Command handler naming
- **GIVEN** 创建命令处理方法
- **WHEN** 命名方法
- **THEN** 命名为 `OnExecute{Action}Async`
- **EXAMPLE** `OnExecuteAddAsync`, `OnExecuteDeleteAsync`, `OnExecuteSaveAsync`

#### Scenario: No sync-over-async
- **GIVEN** 需要调用异步方法
- **WHEN** 在同步上下文中
- **THEN** SHALL NOT 使用 `.Wait()` 或 `.Result`
- **AND** SHALL 使用 `async/await` 或 `Dispatcher.InvokeAsync`

### Requirement: VM-006 Navigation Pattern

ViewModel导航 MUST 使用统一的导航方式。

**规范**:
- 模块间导航 SHALL 使用 `IRegionManager.RequestNavigate()`
- 导航参数 SHALL 使用 `NavigationParameters`
- 事件通知 SHALL 使用 `IEventAggregator`

#### Scenario: Region navigation
- **GIVEN** 需要导航到其他视图
- **WHEN** 执行导航
- **THEN** 使用 `NavigateTo(regionName, viewName, parameters)`
- **AND** 参数通过 `NavigationParameters` 传递

#### Scenario: Event-based communication
- **GIVEN** 需要通知其他ViewModel
- **WHEN** 状态发生变化
- **THEN** 使用 `EventAggregator.GetEvent<TEvent>().Publish(payload)`
- **AND** 订阅方在 `OnNavigatedTo` 中订阅
- **AND** 订阅方在 `OnNavigatedFrom` 中取消订阅

### Requirement: VM-007 Base Class Inheritance

ViewModel MUST 继承适当的基类。

**规范**:
- 简单ViewModel SHALL 继承 `ViewModelBase`
- 带导航的ViewModel SHALL 继承 `UnifiedViewModelBase`
- 列表ViewModel SHALL 继承 `UnifiedListViewModelBase<T>`

#### Scenario: List ViewModel inheritance
- **GIVEN** ViewModel管理实体列表
- **WHEN** 选择基类
- **THEN** 继承 `UnifiedListViewModelBase<TEntity>`
- **AND** 实现 `GetItemsAsync()` 抽象方法

#### Scenario: Detail ViewModel inheritance
- **GIVEN** ViewModel管理单个实体详情
- **WHEN** 选择基类
- **THEN** 继承 `UnifiedViewModelBase`
- **AND** 实现 `InitializeAsync(NavigationParameters)` 方法

## Implementation Notes

### Component目录结构
```
LYBT.Desktop.{Module}/
├── ViewModels/
│   ├── {Feature}ViewModel.cs          # 协调器
│   └── Components/
│       ├── {Feature}DataManager.cs    # 数据管理
│       ├── {Feature}CommandHandler.cs # 命令处理
│       ├── {Feature}Validator.cs      # 验证逻辑
│       └── {Feature}EventCoordinator.cs # 事件协调
```

### DI注册示例
```csharp
// 在Module的RegisterTypes中注册
containerRegistry.Register<ViewModels.Components.{Feature}DataManager>();
containerRegistry.Register<ViewModels.Components.{Feature}CommandHandler>();
// ViewModel通过构造函数注入Components
```
