# Spec: error-handling

## Purpose

定义LYBTZYZS项目的统一错误处理规范,包括RFC 7807 Problem Details格式、IExceptionHandler处理器链、ErrorCode体系等。

## ADDED Requirements

### Requirement: ERR-001 RFC 7807 Problem Details格式

所有API错误响应 SHALL 遵循RFC 7807 Problem Details标准格式。

**标准字段**:
- type: 错误类型URI
- title: 错误标题
- status: HTTP状态码
- detail: 错误详情
- instance: 请求路径

**扩展字段**:
- correlationId: 请求追踪ID
- timestamp: 错误发生时间
- errorCode: 业务错误码

#### Scenario: API返回业务错误
- **WHEN** 服务端抛出业务异常(如NotFoundException)
- **THEN** 响应Content-Type SHALL 为application/problem+json
- **AND** 响应体 SHALL 包含type、title、status、detail、instance字段
- **AND** extensions SHALL 包含correlationId、timestamp、errorCode

#### Scenario: API返回系统错误
- **WHEN** 服务端发生未处理异常
- **THEN** 响应状态码 SHALL 为500
- **AND** 响应体 SHALL 为ProblemDetails格式
- **AND** detail字段 SHALL NOT 包含敏感的堆栈信息

---

### Requirement: ERR-002 IExceptionHandler处理器链

异常处理 SHALL 使用ASP.NET Core 8的IExceptionHandler接口实现分层处理。

**处理器优先级**:
1. BusinessExceptionHandler: 处理AppException及子类
2. SystemExceptionHandler: 兜底处理所有未处理异常

**处理器职责**:
- 返回true表示已处理,终止处理器链
- 返回false表示未处理,继续下一个处理器
- 记录适当级别的结构化日志

#### Scenario: 业务异常被正确处理器捕获
- **WHEN** 服务抛出ValidationException
- **THEN** BusinessExceptionHandler SHALL 返回true
- **AND** 响应状态码 SHALL 为400
- **AND** 日志级别 SHALL 为Warning

#### Scenario: 系统异常被兜底处理器捕获
- **WHEN** 服务抛出NullReferenceException
- **THEN** BusinessExceptionHandler SHALL 返回false
- **AND** SystemExceptionHandler SHALL 处理异常
- **AND** 响应状态码 SHALL 为500
- **AND** 日志级别 SHALL 为Error含完整堆栈

---

### Requirement: ERR-003 ErrorCode分层枚举体系

错误码 SHALL 按模块划分,便于问题定位和客户端处理。

**错误码格式**: 5位数字(模块2位 + 具体错误3位)

**模块分配**:
- 00xxx: 通用错误
- 10xxx: Auth模块
- 20xxx: Users模块
- 30xxx: Patients模块
- 40xxx: MedicalCase模块
- 50xxx: Consultation模块
- 60xxx: Prescriptions模块
- 70xxx: Herbs/Formula模块

#### Scenario: 使用ErrorCode标识特定错误
- **WHEN** 医案模块发生并发冲突
- **THEN** ErrorCode SHALL 为MedicalCase_ConcurrencyConflict(40003)
- **AND** 客户端 SHALL 可根据ErrorCode显示特定提示

#### Scenario: 新增模块错误码
- **WHEN** 需要为新模块添加错误码
- **THEN** 错误码前缀 SHALL 符合模块分配规则
- **AND** 错误码 SHALL NOT 与现有错误码冲突

---

### Requirement: ERR-004 扩展AppException异常体系

AppException体系 SHALL 支持ErrorCode和更丰富的上下文信息。

**新增异常类型**:
- ConflictException: 并发冲突(HTTP 409)
- UnauthorizedException: 授权失败(HTTP 401)

**扩展属性**:
- ErrorCode: 业务错误码(默认Unknown)

#### Scenario: 创建带ErrorCode的异常
- **WHEN** 需要抛出业务异常
- **THEN** 构造函数 SHALL 支持ErrorCode参数
- **AND** 默认ErrorCode SHALL 为Unknown

#### Scenario: 使用ConflictException处理并发冲突
- **WHEN** EF Core检测到DbUpdateConcurrencyException
- **THEN** Service层 SHALL 转换为ConflictException
- **AND** HTTP响应状态码 SHALL 为409

---

### Requirement: ERR-005 客户端ProblemDetails解析

客户端 SHALL 能正确解析服务端返回的ProblemDetails响应。

**解析要求**:
- 支持所有标准ProblemDetails字段
- 支持extensions扩展字段(errorCode、correlationId)
- 对非ProblemDetails响应有降级处理

#### Scenario: 解析标准ProblemDetails响应
- **WHEN** HTTP响应Content-Type为application/problem+json
- **THEN** 客户端 SHALL 成功提取title、detail、status字段
- **AND** SHALL 成功提取extensions中的errorCode和correlationId

#### Scenario: 处理非ProblemDetails错误响应
- **WHEN** HTTP响应为非JSON格式或旧格式错误
- **THEN** 解析器 SHALL NOT 抛出异常
- **AND** SHALL 返回降级的错误信息(使用HTTP状态码和原始内容)

---

### Requirement: ERR-006 友好错误消息显示

客户端 SHALL 根据ErrorCode显示用户友好的中文错误消息。

**消息映射规则**:
- 业务错误: 根据ErrorCode查找对应消息
- 验证错误: 显示具体字段验证失败信息
- 系统错误: 显示通用友好消息

**消息存储**:
- 使用resx资源文件
- 预留多语言扩展能力

#### Scenario: 显示业务错误消息
- **WHEN** 收到ErrorCode为MedicalCase_NotFound的错误
- **THEN** 客户端 SHALL 显示"未找到指定的医案记录"

#### Scenario: 显示系统错误消息
- **WHEN** 收到ErrorCode为Unknown的错误
- **THEN** 客户端 SHALL 显示通用消息"系统繁忙,请稍后重试"
- **AND** SHALL NOT 暴露技术细节给用户

---

### Requirement: ERR-007 错误日志关联追踪

错误处理日志 SHALL 包含CorrelationId支持端到端追踪。

**日志要求**:
- IExceptionHandler日志包含CorrelationId属性
- ProblemDetails响应包含correlationId扩展字段
- 客户端错误日志包含从响应提取的correlationId

#### Scenario: 服务端错误日志包含CorrelationId
- **WHEN** IExceptionHandler记录异常日志
- **THEN** 日志 SHALL 包含CorrelationId属性
- **AND** ProblemDetails响应 SHALL 包含相同correlationId

#### Scenario: 客户端日志关联服务端错误
- **WHEN** 客户端收到错误响应并记录日志
- **THEN** 日志 SHALL 包含从响应中提取的correlationId
- **AND** 可通过correlationId关联服务端日志

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| logging-infrastructure | 日志基础设施,CorrelationId定义 |
| server-layer-architecture | Server层架构,异常处理中间件 |
| client-layer-architecture | Client层架构,错误处理服务 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本,定义错误处理规范 |
