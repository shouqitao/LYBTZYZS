# refactor-startup-connection-resilience

## Why

当前连接处理架构存在5个结构性问题，导致启动体验差、代码分散、维护困难：

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| App.xaml.cs + LoginViewModel | 双入口 | 启动Dialog和登录页各自处理连接，逻辑分散 | 登录页作为唯一连接交互入口 |
| ConnectionMode枚举 | YAGNI违反 | Local模式UI存在但永久禁用(v2.0预留) | 完全移除，需要时再添加 |
| ApiConnectionFailedDialog + LoginView | 状态同步 | 两处独立维护ApiStatus，易不一致 | ApplicationStateService单一真相源 |
| ApiHealthCheckStartupStep | 阻塞启动 | IsRequired=true，失败则弹Dialog阻塞 | 非阻塞探测，直接进登录页 |
| 启动Dialog → 登录页 | UX断裂 | 用户先看到恢复Dialog，再看到登录页 | 统一在登录页内联展示连接状态 |

### 影响分析

- **涉及层级**: Desktop端 (WPF客户端)
- **涉及模块**: Shell (启动管道/对话框), Auth (登录), Foundation (状态服务), Contracts (接口)
- **不涉及**: Server端、Shared层、数据库

### 研究依据

基于以下最佳实践研究：

1. **Microsoft启动3阶段模型**: Splash → 可交互UI → 渐进加载数据。不应因数据未就绪阻塞用户看到UI
2. **Polly弹性模式(.NET 8)**: `AddStandardResilienceHandler()` 提供开箱即用的Retry+CircuitBreaker+Timeout
3. **UX错误处理最佳实践**: 风险分级(Toast vs Modal)、分层信息(友好→技术详情)、行动导向、避免过度弹窗
4. **业界Desktop应用**: 优秀应用(VS Code/Slack/Teams)均采用非阻塞启动+内联状态指示器模式

## What Changes

### 设计哲学

> **"Non-blocking Startup + Progressive Resilience"**
> 启动永不阻塞，弹性逐层递进。登录页是唯一的连接交互入口。

### Phase 1: 引入Polly弹性管道 + 清理YAGNI代码

**1.1 新增Polly弹性管道**
- 为HttpClient注册标准弹性处理器
- Retry: 3次，指数退避+抖动
- Circuit Breaker: 连续5次失败熔断30秒
- Timeout: 单次请求10秒
- 所有API调用自动享受弹性保护，对上层透明

**1.2 删除ConnectionMode系统**
- 删除 `ConnectionMode.cs` 枚举
- 删除 `IConnectionSettingsService.cs` + `ConnectionSettingsService.cs`
- 清理 LoginViewModel 中所有 ConnectionMode 相关属性

**1.3 删除连接恢复对话框系统**
- 删除 `ApiConnectionFailedDialog.xaml` + `.xaml.cs`
- 删除 `ApiConnectionFailedDialogViewModel.cs`
- 删除 `IApiConnectionRecoveryService.cs` + `ApiConnectionRecoveryService.cs`
- 删除 `RecoveryAction.cs` 枚举

### Phase 2: 重构启动管道 + 统一连接交互

**2.1 启动管道改为非阻塞**
- `ApiHealthCheckStartupStep.IsRequired` 改为 `false`
- 移除 `App.xaml.cs` 中的while循环重试逻辑
- 移除 `HandleApiConnectionFailureAsync()` 方法
- 启动流程: Splash → DI → 异步探测API(非阻塞) → 直接进登录页

**2.2 增强ApplicationStateService**
- 新增 `StatusChanged` 事件
- 新增 `LastError` 属性保存最后错误详情
- 确保是连接状态的单一真相源

**2.3 重构LoginViewModel连接状态**
- 移除所有ConnectionMode相关属性和逻辑
- 订阅 ApplicationStateService.StatusChanged 事件
- 内联显示连接状态Banner(Healthy/Checking/Unhealthy)
- 登录失败时区分"连接问题"和"凭证问题"

**2.4 重构LoginView.xaml**
- 移除连接模式RadioButton区域
- 新增顶部连接状态Banner：
  - Healthy: 绿色小字"服务器已连接"
  - Checking: 蓝色加载动画
  - Unhealthy: 橙色醒目Banner+重试按钮+可展开详情

### Phase 3: 更新OpenSpec规范

**3.1 更新dialog-patterns/spec.md**
- DLG-006: 重写为"启动时API连接失败直接进入登录页，登录页内联显示连接状态"
- DLG-007: 分层信息展示理念保留，应用于LoginView内联区域

## Architecture

### 重构后架构分层

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: HttpClient弹性管道 (Polly)                        │
│  • Retry + Exponential Backoff + Jitter                    │
│  • Circuit Breaker (自动熔断/恢复)                          │
│  • 对上层透明，所有API调用自动享受弹性保护                    │
└─────────────────────────────────────────────────────────────┘
                              |
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: ApplicationStateService (状态中枢)                │
│  • IsApiHealthy: 单一真相源                                 │
│  • StatusChanged: 事件通知                                  │
│  • LastError: 最后错误详情                                  │
│  • 后台定时探测 (HealthCheckCoordinator 10s)                │
└─────────────────────────────────────────────────────────────┘
                              |
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: LoginViewModel (统一交互入口)                     │
│  • 连接状态Banner (Healthy/Unhealthy/Checking)             │
│  • 手动重试按钮                                             │
│  • 内联错误展示，不弹对话框                                  │
└─────────────────────────────────────────────────────────────┘
```

### 重构后启动流程

```
App.OnStartup()
  |-- 显示SplashScreen
  |-- 初始化DI容器 (含Polly弹性管道注册)
  |-- 执行启动管道
  |     |-- ErrorHandling (Order=10)
  |     |-- ModuleCoordinator (Order=20)
  |     |-- CoreServices (Order=30)
  |     |-- ApiHealthCheck (Order=40, IsRequired=false) <-- 非阻塞
  |     '-- Warmup (Order=50)
  |-- 启动HealthCheckCoordinator (后台10s定时探测)
  |-- 导航到LoginView
  '-- 隐藏SplashScreen
```

### 变更影响范围

```
src/Client/Desktop/
  Shell/
    App.xaml.cs                          [修改] 移除while循环和HandleApiConnectionFailure
    Services/Startup/Steps/
      ApiHealthCheckStartupStep.cs       [修改] IsRequired改为false
    Services/HealthCheck/
      HealthCheckCoordinator.cs          [保留] 无变更
    Services/Recovery/
      ApiConnectionRecoveryService.cs    [删除]
    Dialogs/Views/
      ApiConnectionFailedDialog.xaml     [删除]
      ApiConnectionFailedDialog.xaml.cs  [删除]
    Dialogs/ViewModels/
      ApiConnectionFailedDialogViewModel.cs [删除]
    Extensions/
      ServiceCollectionExtensions.cs     [修改] 注册Polly，移除旧服务
      HttpClientResilienceExtensions.cs  [新增] Polly弹性管道配置
  Core/
    LYBT.Desktop.Contracts/Services/
      IApiConnectionRecoveryService.cs   [删除]
      RecoveryAction.cs                  [删除]
      IConnectionSettingsService.cs      [删除]
    LYBT.Desktop.Foundation/Application/
      ApplicationStateService.cs         [修改] 新增StatusChanged事件+LastError
      IApplicationStateService.cs        [修改] 新增事件+属性
  Modules/
    LYBT.Desktop.Auth/
      Models/ConnectionMode.cs           [删除]
      Services/ConnectionSettingsService.cs [删除]
      ViewModels/LoginViewModel.cs       [修改] 移除ConnectionMode，增强连接状态
      Views/LoginView.xaml               [修改] 移除RadioButton，增强Banner
```

## Impact

- **文件变更**: 删除8个文件，修改7个文件，新增1个文件
- **风险等级**: Medium (影响启动流程和登录页，但不影响核心业务)
- **NuGet依赖**: 新增 `Microsoft.Extensions.Http.Resilience` (Polly .NET 8集成)
- **测试要求**: 启动流程测试、登录页连接状态测试、Polly管道配置验证

## Risks

| 风险 | 缓解措施 |
|------|----------|
| Polly NuGet包引入新依赖 | 使用Microsoft官方包，.NET 8原生支持 |
| 启动流程变更可能引入回归 | 分Phase执行，每Phase编译验证 |
| 登录页Banner UX需要验证 | 设计阶段输出Banner状态规格，实现后人工验收 |
| 移除Local模式后配置文件残留 | 不主动删除用户本地配置文件，代码层面忽略即可 |

## Spec Impact

| 规范文件 | 影响 |
|----------|------|
| `dialog-patterns/spec.md` DLG-006 | 重写: 启动连接失败直接进登录页内联展示 |
| `dialog-patterns/spec.md` DLG-007 | 更新: 分层信息应用于LoginView内联区域 |

## References

- 用户需求: 彻底重构登录认证连接架构，不考虑兼容设计
- 研究来源: Microsoft官方启动文档、Polly弹性库文档、UX错误处理最佳实践、WPF/Prism社区模式
