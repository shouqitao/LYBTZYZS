# authentication Specification

## Purpose
TBD - created by archiving change refactor-token-sliding-expiration. Update Purpose after archive.
## Requirements
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
系统 **SHALL** 仅在用户活跃时刷新Access Token,实现真正的滑动过期语义。**新增绝对过期时间限制**。

#### Scenario: 活跃用户Token刷新
- **GIVEN** 用户已登录且处于活跃状态
- **AND** Access Token将在5分钟内过期
- **WHEN** 系统执行API调用
- **THEN** 系统自动刷新Access Token
- **AND** 新Token的有效期从当前时间开始计算

#### Scenario: 不活跃用户Token不刷新
- **GIVEN** 用户已登录但已不活跃超过10分钟
- **AND** Access Token将在5分钟内过期
- **WHEN** 后台有定时任务尝试调用API
- **THEN** 系统不自动刷新Token
- **AND** 等待不活跃超时后自动登出

#### Scenario: Token过期后需重新登录
- **GIVEN** 用户的Access Token已过期
- **AND** 用户长时间未操作导致未触发刷新
- **WHEN** 用户尝试执行需要认证的操作
- **THEN** 系统提示用户会话已过期
- **AND** 用户被导航到登录页面

#### Scenario: 绝对过期时间强制重新登录
- **GIVEN** 用户持续活跃使用系统
- **AND** 用户的会话已持续30天
- **WHEN** 用户尝试刷新Token
- **THEN** 系统拒绝刷新请求
- **AND** 系统返回ErrorCode=SessionExpired
- **AND** 用户必须重新登录

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

### Requirement: AUTH-006 统一错误码体系
系统 **SHALL** 使用结构化错误码(AuthErrorCode)返回认证相关错误，便于客户端统一处理和国际化。

#### Scenario: 凭据错误返回InvalidCredentials
- **GIVEN** 用户提交登录请求
- **WHEN** 用户名或密码错误
- **THEN** 系统返回HTTP 401状态码
- **AND** 响应包含ErrorCode=InvalidCredentials(101)
- **AND** 响应包含本地化错误消息

#### Scenario: Token过期返回TokenExpired
- **GIVEN** 用户使用已过期的AccessToken访问API
- **WHEN** 服务端验证Token
- **THEN** 系统返回HTTP 401状态码
- **AND** 响应包含ErrorCode=TokenExpired(201)

#### Scenario: 客户端处理错误码
- **GIVEN** 客户端收到认证错误响应
- **WHEN** 响应包含AuthErrorCode
- **THEN** 客户端根据错误码显示对应提示
- **AND** 必要时自动执行Token刷新或重新登录

---

### Requirement: AUTH-007 Refresh Token重放攻击检测
系统 **SHALL** 检测并阻止Refresh Token重放攻击，当检测到已使用的Token被再次使用时，使整个Token Family失效。

#### Scenario: 正常Token刷新
- **GIVEN** 用户持有有效的RefreshToken
- **WHEN** 用户请求刷新Token
- **THEN** 系统颁发新的AccessToken和RefreshToken
- **AND** 原RefreshToken标记为已使用
- **AND** 新RefreshToken保持相同的FamilyId

#### Scenario: 检测到Token重放
- **GIVEN** 攻击者获取了一个已使用的RefreshToken
- **WHEN** 攻击者尝试使用该Token刷新
- **THEN** 系统检测到Token已被使用
- **AND** 系统使整个Token Family失效
- **AND** 系统返回ErrorCode=TokenRevoked
- **AND** 系统记录安全审计日志

#### Scenario: Family失效后合法用户需重新登录
- **GIVEN** Token Family已因检测到重放而失效
- **WHEN** 合法用户尝试使用任何该Family的Token
- **THEN** 系统拒绝请求
- **AND** 用户需要重新登录获取新的Token Family

---

### Requirement: AUTH-008 过期Token登出支持
系统 **SHALL** 允许使用已过期的AccessToken进行登出操作，确保服务端会话被正确清理。

#### Scenario: 过期Token登出成功
- **GIVEN** 用户的AccessToken已过期
- **AND** 用户持有有效或无效的RefreshToken
- **WHEN** 用户请求登出
- **THEN** 系统接受登出请求
- **AND** 系统清除服务端会话记录
- **AND** 系统撤销相关RefreshToken
- **AND** 系统返回登出成功响应

#### Scenario: 无Token登出
- **GIVEN** 用户未提供任何Token
- **AND** 用户提供了用户名和RefreshToken
- **WHEN** 用户请求登出
- **THEN** 系统根据RefreshToken查找并清理会话
- **AND** 系统返回登出成功响应

---

### Requirement: AUTH-009 Logout后强制重新登录
系统 **SHALL** 确保用户Logout后必须重新输入密码执行Login操作，不支持任何形式的自动重连或会话恢复。

#### Scenario: Logout后服务端Token失效
- **GIVEN** 用户执行Logout操作
- **WHEN** Logout请求到达服务端
- **THEN** 服务端撤销该用户的RefreshToken
- **AND** 该RefreshToken无法再用于刷新AccessToken
- **AND** 系统记录登出审计日志

#### Scenario: Logout后客户端清除认证状态
- **GIVEN** 用户执行Logout操作
- **WHEN** 客户端处理Logout响应
- **THEN** 客户端清除内存中所有认证信息(Token、用户信息)
- **AND** 客户端导航到登录页面
- **AND** 用户必须输入密码执行Login操作

#### Scenario: 保存密码场景仍需执行Login
- **GIVEN** 客户端已保存用户密码(加密存储)
- **AND** 用户已Logout
- **WHEN** 用户点击登录按钮
- **THEN** 客户端使用保存的密码调用Login API
- **AND** 服务端生成新的Token Family
- **AND** 客户端获得全新的AccessToken和RefreshToken

#### Scenario: 禁止会话恢复
- **GIVEN** 用户已Logout
- **WHEN** 应用尝试使用旧Token访问API
- **THEN** 服务端返回TokenRevoked错误
- **AND** 客户端不得尝试自动恢复会话
- **AND** 用户必须重新执行完整Login流程

---

