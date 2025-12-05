## ADDED Requirements

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

## MODIFIED Requirements

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
