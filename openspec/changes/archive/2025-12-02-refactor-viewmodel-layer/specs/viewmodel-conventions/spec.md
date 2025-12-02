# ViewModel Conventions Specification

## ADDED Requirements

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

## Cross-Reference

- **service-conventions**: Service层设计规范，ViewModel调用Service时遵循
- **client-api-conventions**: Client API层设计规范，ViewModel通过Refit接口调用API
