## ADDED Requirements

### Requirement: AUTH-000 统一定时任务调度
系统 **SHALL** 提供统一的定时任务调度服务(IApplicationTickService),使用单一Timer管理所有周期性任务。

#### Scenario: 单一Timer统一调度
- **Given** 应用程序启动
- **When** ApplicationTickService启动
- **Then** 系统使用单一DispatcherTimer,每秒触发一次Tick事件
- **And** 所有需要定时执行的组件订阅Tick事件
- **And** 各组件根据自身需求决定执行频率(如每10次Tick执行一次健康检查)

#### Scenario: 订阅者按需执行
- **Given** 健康检查服务订阅了Tick事件
- **And** 健康检查需要每10秒执行一次
- **When** Tick事件触发
- **Then** 订阅者检查TickCount是否为10的倍数
- **And** 仅在满足条件时执行健康检查逻辑

#### Scenario: 生命周期管理
- **Given** ApplicationTickService已启动
- **When** 应用程序关闭
- **Then** ApplicationTickService停止Timer
- **And** 释放所有资源

---

### Requirement: AUTH-001 用户活动追踪
系统 **SHALL** 追踪用户的UI交互活动(键盘输入、鼠标操作、触摸事件),用于判断用户是否活跃。

#### Scenario: 用户键盘输入
- **Given** 用户已登录系统
- **When** 用户在任意输入框中输入文字
- **Then** 系统记录当前时间为最后活动时间
- **And** 用户被视为活跃状态

#### Scenario: 用户鼠标点击
- **Given** 用户已登录系统
- **When** 用户点击任意UI元素
- **Then** 系统记录当前时间为最后活动时间
- **And** 用户被视为活跃状态

#### Scenario: 无用户活动
- **Given** 用户已登录系统
- **When** 用户没有任何键盘或鼠标操作
- **Then** 最后活动时间保持不变
- **And** 随着时间推移用户逐渐被视为不活跃

---

### Requirement: AUTH-002 不活跃自动登出
系统 **SHALL** 在用户不活跃时间超过配置的超时时间后自动登出,确保安全性。

#### Scenario: 超时自动登出
- **Given** 用户已登录系统
- **And** 配置的不活跃超时时间为15分钟
- **When** 用户连续15分钟无任何操作
- **Then** 系统自动执行登出操作
- **And** 用户被导航到登录页面
- **And** 清除本地存储的认证信息

#### Scenario: 活跃用户不被登出
- **Given** 用户已登录系统
- **And** 配置的不活跃超时时间为15分钟
- **When** 用户在14分钟时进行了操作
- **Then** 不活跃计时器重置
- **And** 用户不会被登出

---

### Requirement: AUTH-003 登出前警告
系统 **SHALL** 在即将因不活跃而登出前显示警告对话框,给用户保存工作的机会。

#### Scenario: 显示即将过期警告
- **Given** 用户已登录系统
- **And** 配置的不活跃超时时间为15分钟
- **And** 配置的警告提前时间为2分钟
- **When** 用户已连续不活跃13分钟
- **Then** 系统显示会话即将过期的警告对话框
- **And** 对话框显示剩余时间约2分钟
- **And** 对话框提供"保持登录"和"立即登出"选项

#### Scenario: 用户选择保持登录
- **Given** 会话即将过期警告对话框已显示
- **When** 用户点击"保持登录"按钮
- **Then** 系统重置不活跃计时器
- **And** 系统刷新Access Token
- **And** 警告对话框关闭
- **And** 用户继续正常使用系统

#### Scenario: 用户选择立即登出
- **Given** 会话即将过期警告对话框已显示
- **When** 用户点击"立即登出"按钮
- **Then** 系统立即执行登出操作
- **And** 用户被导航到登录页面

#### Scenario: 用户忽略警告
- **Given** 会话即将过期警告对话框已显示
- **When** 用户在剩余时间内未进行任何操作
- **Then** 倒计时结束后系统自动登出
- **And** 用户被导航到登录页面

---

### Requirement: AUTH-004 Token滑动过期
系统 **SHALL** 仅在用户活跃时刷新Access Token,实现真正的滑动过期语义。

#### Scenario: 活跃用户Token刷新
- **Given** 用户已登录且处于活跃状态
- **And** Access Token将在5分钟内过期
- **When** 系统执行API调用
- **Then** 系统自动刷新Access Token
- **And** 新Token的有效期从当前时间开始计算

#### Scenario: 不活跃用户Token不刷新
- **Given** 用户已登录但已不活跃超过10分钟
- **And** Access Token将在5分钟内过期
- **When** 后台有定时任务尝试调用API
- **Then** 系统不自动刷新Token
- **And** 等待不活跃超时后自动登出

#### Scenario: Token过期后需重新登录
- **Given** 用户的Access Token已过期
- **And** 用户长时间未操作导致未触发刷新
- **When** 用户尝试执行需要认证的操作
- **Then** 系统提示用户会话已过期
- **And** 用户被导航到登录页面

---

### Requirement: AUTH-005 会话配置
系统 **SHALL** 提供可配置的会话管理参数,允许运维人员根据安全需求调整。

#### Scenario: 配置不活跃超时时间
- **Given** 系统管理员修改配置文件
- **When** 设置`Lybt:Session:InactivityTimeoutMinutes`为30
- **Then** 系统使用30分钟作为不活跃超时时间
- **And** 用户连续30分钟无操作后被自动登出

#### Scenario: 配置警告提前时间
- **Given** 系统管理员修改配置文件
- **When** 设置`Lybt:Session:WarningBeforeTimeoutMinutes`为5
- **Then** 系统在超时前5分钟显示警告
- **And** 用户有5分钟时间选择保持登录

#### Scenario: 使用默认配置
- **Given** 配置文件中未指定会话参数
- **When** 系统启动
- **Then** 使用默认不活跃超时时间15分钟
- **And** 使用默认警告提前时间2分钟
