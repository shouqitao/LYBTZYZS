# Change: Token滑动过期与自动登出

## Why
当前Token刷新机制基于API调用触发,无法区分用户实际活跃状态。用户长时间不操作但Token持续刷新,存在安全隐患;反之用户持续操作但因网络延迟导致Token过期,体验不佳。需要实现基于用户实际活动的滑动过期机制。

## What Changes
- **新增**: 统一定时任务调度服务(ApplicationTickService),单一Timer管理所有周期性任务
- **新增**: 用户活动追踪服务(UserActivityTracker),监听键盘/鼠标/UI交互事件
- **新增**: 不活跃检测机制,超过配置时间无用户活动则触发自动登出
- **修改**: Token刷新策略,从"API调用时刷新"改为"用户活跃时刷新"
- **修改**: MainWindowViewModel/UserExperienceService等,迁移到统一Timer
- **新增**: 登出前警告对话框,在Token即将过期时提醒用户
- **新增**: authentication capability spec(新建)

## Impact
- Affected specs: authentication(新建)
- Affected code:
  - `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/TokenRefreshHandler.cs`
  - `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationService.cs`
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/` (新增UserActivityTracker)
  - `src/Server/Core/LYBT.Infrastructure/Configuration/Options/LybtOptions.cs`

## 行业最佳实践参考
1. **短期Access Token + 长期Refresh Token**: 当前已实现(15分钟/7天)
2. **滑动过期**: 用户活跃时持续延长会话,不活跃则过期
3. **主动刷新**: 在Token过期前提前刷新(当前5分钟阈值)
4. **Token轮换**: 每次刷新时发放新的Refresh Token(增强安全性)
5. **优雅降级**: Token过期前显示警告,给用户保存工作的机会

## 技术方案概述
1. **UserActivityTracker**: 单例服务,使用低开销的事件监听追踪用户活动
2. **InactivityTimer**: 基于DispatcherTimer,每分钟检查用户活动状态
3. **与现有TokenRefreshHandler协作**: 只有在用户活跃时才触发Token刷新
4. **配置化**: 不活跃超时时间、警告提前时间等均可配置
