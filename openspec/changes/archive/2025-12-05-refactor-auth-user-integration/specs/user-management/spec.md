## ADDED Requirements

### Requirement: USER-AUTH-001 密码验证职责归属
IUserService **SHALL** 提供密码验证方法，供AuthService调用，实现认证与用户管理的职责分离。

#### Scenario: Auth模块调用User模块验证密码
- **GIVEN** 用户提交登录请求
- **WHEN** AuthService需要验证用户凭据
- **THEN** AuthService调用IUserService.ValidatePasswordAsync
- **AND** UserService执行密码比对逻辑
- **AND** 返回验证结果(成功/失败)

#### Scenario: 密码验证失败返回详细错误
- **GIVEN** 用户提交错误密码
- **WHEN** UserService验证密码
- **THEN** 返回验证失败结果
- **AND** 不暴露具体失败原因(安全要求)
- **AND** AuthService负责返回统一错误码

---

### Requirement: USER-AUTH-002 密码修改业务流程
UserService **SHALL** 负责密码修改的业务逻辑，AuthService仅负责修改后的会话处理。

#### Scenario: 普通用户修改密码
- **GIVEN** 已登录用户请求修改密码
- **WHEN** 用户提供旧密码和新密码
- **THEN** UserService验证旧密码正确
- **AND** UserService应用密码策略检查新密码
- **AND** UserService更新密码哈希
- **AND** AuthService使该用户所有Token Family失效
- **AND** 用户需要重新登录

#### Scenario: 系统管理员重置用户密码
- **GIVEN** 系统管理员请求重置某用户密码
- **WHEN** 管理员提供新密码
- **THEN** UserService直接更新密码哈希(无需旧密码)
- **AND** AuthService使该用户所有Token Family失效
- **AND** 系统记录管理员操作审计日志

#### Scenario: 密码策略检查
- **GIVEN** 用户提交新密码
- **WHEN** UserService检查密码策略
- **THEN** 验证密码最小长度(8位)
- **AND** 验证包含字母和数字组合
- **AND** 不满足策略时返回明确提示

---

### Requirement: USER-AUTH-003 用户状态与认证联动
系统 **SHALL** 在用户状态变更时自动处理相关认证会话。

#### Scenario: 用户被禁用时强制登出
- **GIVEN** 管理员禁用某用户账号
- **WHEN** 用户状态变更为Disabled
- **THEN** 系统使该用户所有Token Family失效
- **AND** 用户当前会话立即失效
- **AND** 用户再次登录时返回UserDisabled错误

#### Scenario: 用户被删除时清理会话
- **GIVEN** 管理员删除某用户账号
- **WHEN** 用户被软删除
- **THEN** 系统使该用户所有Token Family失效
- **AND** 清理该用户所有RefreshToken记录
- **AND** 记录审计日志

---

### Requirement: USER-001 用户查询接口
IUserService **SHALL** 提供基础用户查询方法，支持Auth模块的凭据验证需求，包含认证相关查询方法。

#### Scenario: 按用户名查询(认证用)
- **GIVEN** AuthService需要验证用户凭据
- **WHEN** 调用IUserService.GetByUsernameAsync
- **THEN** 返回用户信息(包含密码哈希)
- **AND** 结果仅供内部认证使用

#### Scenario: 按ID查询(常规用)
- **GIVEN** 需要获取用户详情
- **WHEN** 调用IUserService.GetByIdAsync
- **THEN** 返回UserDto(不含敏感信息)

