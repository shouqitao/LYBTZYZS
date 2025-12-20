# Spec Delta: error-handling

**Change ID**: refactor-exception-handling-system
**Base Spec Version**: Current (ERR-001 ~ ERR-007)

---

## ADDED Requirements

### Requirement: ERR-008 Service层异常抛出标准

Service层 SHALL 统一使用异常抛出机制处理错误，禁止使用catch-and-return模式。

**规范要求**:
- 业务验证失败 SHALL 抛出BusinessException
- 资源未找到 SHALL 抛出NotFoundException
- 数据冲突 SHALL 抛出ConflictException
- 禁止catch后返回Result.Failure

#### Scenario: 业务验证失败时抛出BusinessException

- **WHEN** Service方法执行业务验证失败
- **THEN** SHALL 抛出BusinessException而非返回Result.Failure
- **AND** 异常 SHALL 包含ErrorCode和详细消息

#### Scenario: 资源未找到时抛出NotFoundException

- **WHEN** Service方法查询资源但资源不存在
- **THEN** SHALL 抛出NotFoundException
- **AND** 异常消息 SHALL 包含资源标识信息

---

### Requirement: ERR-009 ViewModel安全执行模式

所有ViewModel异步操作 SHALL 通过SafeExecuteAsync方法执行，确保统一的异常处理。

**ViewModelBase扩展**:
- SafeExecuteAsync<T>: 有返回值的异步操作
- SafeExecuteAsync: 无返回值的异步操作
- IsBusy状态自动管理

#### Scenario: ViewModel加载数据使用SafeExecuteAsync

- **WHEN** ViewModel需要加载远程数据
- **THEN** SHALL 使用SafeExecuteAsync包装API调用
- **AND** SHALL NOT 直接使用try-catch块

#### Scenario: SafeExecuteAsync自动管理IsBusy状态

- **WHEN** 使用SafeExecuteAsync执行异步操作
- **THEN** IsBusy SHALL 在操作开始时设为true
- **AND** IsBusy SHALL 在操作完成后自动设为false

---

### Requirement: ERR-010 HTTP状态码特殊处理

特定HTTP状态码 SHALL 有专门的处理逻辑，提供针对性的用户体验。

**状态码处理规则**:
- 401 Unauthorized: 清除会话，导航到登录页
- 403 Forbidden: 显示权限不足提示
- 409 Conflict: 提示数据冲突，建议刷新
- 504 Gateway Timeout: 提示服务暂时不可用

#### Scenario: 401响应触发重新登录

- **WHEN** API返回401 Unauthorized
- **THEN** 客户端 SHALL 清除本地会话
- **AND** SHALL 导航到登录页面
- **AND** SHALL 显示"登录已过期，请重新登录"

#### Scenario: 409响应提示数据冲突

- **WHEN** API返回409 Conflict
- **THEN** 客户端 SHALL 显示确认对话框
- **AND** SHALL 询问用户是否刷新数据

#### Scenario: 504响应提示服务不可用

- **WHEN** API返回504 Gateway Timeout
- **THEN** 客户端 SHALL 显示"服务暂时不可用，请稍后重试"
- **AND** SHALL NOT 抛出未处理异常

---

### Requirement: ERR-011 HTTP韧性策略

HttpClient SHALL 配置Polly韧性策略，提高网络通信的健壮性。

**策略要求**:
- 重试策略: 最多3次，指数退避(2^n秒)
- 熔断策略: 连续5次失败后熔断30秒
- 超时策略: 单次请求30秒超时

#### Scenario: 瞬态故障自动重试

- **WHEN** HTTP请求遇到网络抖动或超时
- **THEN** 客户端 SHALL 自动重试最多3次
- **AND** 重试间隔 SHALL 使用指数退避策略

#### Scenario: 连续失败触发熔断

- **WHEN** 连续5次HTTP请求失败
- **THEN** 熔断器 SHALL 打开
- **AND** 后续请求 SHALL 在30秒内直接拒绝

#### Scenario: 请求超时有明确限制

- **WHEN** 单次HTTP请求超过30秒
- **THEN** SHALL 抛出TimeoutRejectedException
- **AND** 客户端 SHALL 可处理超时异常

---

### Requirement: ERR-012 异常消息安全化

用户界面显示的异常消息 SHALL 经过安全过滤，防止敏感信息泄露。

**过滤规则**:
- 业务异常: 使用ErrorCode对应的本地化消息
- 系统异常: 显示通用消息"操作失败，请稍后重试"
- 敏感信息: 数据库连接串、SQL语句、堆栈跟踪等 SHALL 被过滤

#### Scenario: 业务异常显示本地化消息

- **WHEN** 发生BusinessException
- **THEN** 客户端 SHALL 使用ClientErrorMessageMapper转换ErrorCode
- **AND** SHALL 显示对应的中文消息

#### Scenario: 系统异常显示通用消息

- **WHEN** 发生非业务异常
- **THEN** 客户端 SHALL 显示"操作失败，请稍后重试"
- **AND** SHALL NOT 直接显示ex.Message

#### Scenario: 敏感信息不会泄露

- **WHEN** 异常消息包含数据库连接字符串、SQL语句或堆栈跟踪
- **THEN** 过滤器 SHALL 将敏感信息替换为[已过滤]
- **AND** 详细信息 SHALL 仅记录到日志

---

## MODIFIED Requirements

### Requirement: ERR-004 AppException层级扩展

AppException体系 SHALL 扩展支持更多异常场景。

**新增异常类型**:
- TransientException: 瞬态故障(HTTP 503)
- RateLimitException: 请求限流(HTTP 429)

#### Scenario: 瞬态故障使用TransientException

- **WHEN** 发生网络抖动或暂时性服务不可用
- **THEN** SHALL 抛出TransientException
- **AND** HTTP响应状态码 SHALL 为503

#### Scenario: 请求限流使用RateLimitException

- **WHEN** 请求频率超过限制
- **THEN** SHALL 抛出RateLimitException
- **AND** HTTP响应状态码 SHALL 为429

---

## Validation Checklist

- [ ] ERR-008: grep检查无`catch.*return.*Failure`模式
- [ ] ERR-009: 所有ViewModel使用SafeExecuteAsync
- [ ] ERR-010: 401/409/504有专门处理逻辑
- [ ] ERR-011: HttpClient配置Polly策略
- [ ] ERR-012: 无直接显示ex.Message的代码
