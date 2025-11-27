# 实现任务清单: Token滑动过期与自动登出

## 1. 统一定时任务调度服务 (新增)

- [x] 1.1 创建`IApplicationTickService`接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Interfaces/IApplicationTickService.cs`
  - 包含: Tick事件、TickCount属性、Start/Stop方法
  - 包含: ApplicationTickEventArgs类

- [x] 1.2 实现`ApplicationTickService`服务
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ApplicationTickService.cs`
  - 使用单一DispatcherTimer,间隔1秒
  - 维护TickCount计数器
  - 实现IDisposable

- [x] 1.3 注册ApplicationTickService
  - 文件: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
  - 注册为Singleton

## 2. 用户活动追踪服务

- [x] 2.1 创建`IUserActivityTracker`接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Interfaces/IUserActivityTracker.cs`
  - 包含: LastActivityTime, IsUserActive, TimeUntilInactive属性
  - 包含: SessionExpiring, SessionExpired事件
  - 包含: StartTracking, StopTracking, ResetActivity方法
  - 包含: SessionExpiringEventArgs类

- [x] 2.2 实现`UserActivityTracker`服务
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/UserActivityTracker.cs`
  - 使用InputManager.Current.PreProcessInput监听输入
  - 订阅IApplicationTickService.Tick事件(每60Tick检查一次)
  - 实现SessionExpiring和SessionExpired事件触发逻辑

- [x] 2.3 注册UserActivityTracker
  - 文件: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
  - 注册为Singleton，同时映射IUserActivityTracker和IUserActivityState接口

## 3. 配置选项

- [x] 3.1 更新LybtOptions配置
  - 文件: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/LybtOptions.cs`
  - 添加InactivityTimeoutMinutes(默认15)
  - 添加WarningBeforeTimeoutMinutes(默认2)
  - 添加ActivityCheckIntervalSeconds(默认60)

## 4. Token刷新逻辑修改

- [x] 4.1 修改TokenRefreshHandler
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/TokenRefreshHandler.cs`
  - 注入IUserActivityState（通过Contracts层接口避免循环依赖）
  - 在刷新Token前检查IsUserActive
  - 仅在用户活跃时执行Token刷新（滑动过期机制）
  - 刷新成功后调用ResetActivity

- [x] 4.2 创建IUserActivityState接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUserActivityState.cs`
  - 供Foundation层查询用户活跃状态

## 5. Shell层集成

- [x] 5.1 重构MainWindowViewModel定时器
  - 移除独立的_clockTimer和_healthCheckTimer
  - 注入IApplicationTickService
  - 订阅Tick事件,在回调中处理时钟更新(每次)和健康检查(每10次)

- [x] 5.2 集成UserActivityTracker
  - 注入IUserActivityTracker
  - 订阅SessionExpiring和SessionExpired事件
  - SessionExpired时调用PerformLogoutAsync

- [x] 5.3 添加会话即将过期警告对话框
  - 使用MessageBox显示警告
  - 显示剩余时间
  - 用户确认后调用ResetActivity

## 6. 重构现有Timer使用 (可选/后续)

- [x] 6.1 重构UserExperienceService
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Presentation/UserExperience/UserExperienceService.cs`
  - 移除独立的_feedbackTimer
  - 注入IApplicationTickService
  - 订阅Tick事件,内部维护计数器(每3Tick执行清除)
  - 更新Shell层DI注册以支持新构造函数签名

- [x] 6.2 重构GlobalStatusBar.SystemTimeProvider (可选) - **保留独立Timer**
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/GlobalStatusBar.xaml.cs`
  - 评估结果: 保留独立Timer
  - 原因: 单例模式用于XAML绑定，与DI注入不兼容，重构成本高于收益

## 7. 测试

- [x] 7.1 ApplicationTickService单元测试
  - 文件: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/ApplicationTickServiceTests.cs`
  - 测试Tick事件触发
  - 测试TickCount递增
  - 测试Start/Stop行为
  - 测试Dispose行为
  - 共13个测试用例,全部通过

- [x] 7.2 UserActivityTracker单元测试
  - 文件: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/UserActivityTrackerTests.cs`
  - 测试活动追踪逻辑
  - 测试StartTracking/StopTracking行为
  - 测试ResetActivity重置行为
  - 测试IsUserActive/TimeUntilInactive属性
  - 测试IUserActivityState接口实现
  - 共19个测试用例,全部通过

- [x] 7.3 集成测试
  - 文件: `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.Foundation.IntegrationTests/Http/TokenRefreshHandlerIntegrationTests.cs`
  - 测试TokenRefreshHandler与UserActivityTracker协作
  - 测试用户活跃/不活跃时的Token刷新行为
  - 测试滑动过期核心逻辑
  - 共5个测试用例,全部通过

## 8. 文档

- [x] 8.1 更新开发文档
  - 文件: `openspec/changes/refactor-token-sliding-expiration/dev-guide.md`
  - 记录IApplicationTickService使用方式
  - 记录IUserActivityTracker使用方式
  - 记录IUserActivityState接口设计
  - 说明Token滑动过期机制工作原理
  - 提供从DispatcherTimer迁移指南
  - 说明配置选项
