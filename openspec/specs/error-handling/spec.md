# error-handling Specification

## Purpose
TBD - created by archiving change refactor-logging-system. Update Purpose after archive.
## Requirements
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

### Requirement: ERR-008 统一异常处理项目

异常处理代码 SHALL 集中在独立的 `LYBT.Shared.ExceptionHandling` 项目中。

**项目位置**: `src/Shared/LYBT.Shared.ExceptionHandling/`

**项目结构**:
- `Exceptions/` - 异常类定义 (Base, Business, Security, External, Factory)
- `ErrorCodes/` - 错误码枚举和消息映射
- `Handlers/` - 异常处理器 (Server, Desktop)
- `ProblemDetails/` - RFC 7807支持
- `Mappers/` - 错误消息映射器
- `Extensions/` - DI扩展方法

**迁移来源**:
- `LYBT.Shared.Models/Exceptions/` (8个文件)
- `LYBT.Shared.Models/Errors/` (2个文件)
- `LYBT.Infrastructure/Errors/` (2个文件)
- `LYBT.WebAPI/ExceptionHandlers/` (2个文件)
- `LYBT.Desktop.Foundation/Exceptions/` (4个文件)
- `LYBT.Desktop.Models/Exceptions/` (1个文件)

#### Scenario: 新项目编译验证
- **WHEN** 执行 `dotnet build LYBT.Shared.ExceptionHandling.csproj`
- **THEN** 编译 SHALL 成功且无警告
- **AND** 所有异常类、错误码、处理器 SHALL 可从新命名空间访问

#### Scenario: 旧代码清理验证
- **WHEN** 迁移完成后
- **THEN** 原位置19个文件 SHALL 全部删除
- **AND** 全解决方案 SHALL 编译通过

---

### Requirement: ERR-009 Controller层零catch块

Controller层 SHALL NOT 包含冗余的try-catch块，异常由IExceptionHandler统一处理。

**允许的catch块**:
- 无 (所有Controller异常由IExceptionHandler处理)

**禁止的反模式**:
```csharp
// 禁止
try {
    return Ok(await _service.GetAsync(id));
} catch (Exception ex) {
    _logger.LogError(ex, "操作失败");
    return StatusCode(500, "服务器内部错误");
}
```

**推荐模式**:
```csharp
// 推荐
return Ok(await _service.GetAsync(id));
```

#### Scenario: Controller移除catch块
- **WHEN** 重构Controller代码
- **THEN** 约94个catch块 SHALL 被移除
- **AND** 异常 SHALL 由BusinessExceptionHandler或SystemExceptionHandler处理
- **AND** API响应格式 SHALL 保持RFC 7807 ProblemDetails

#### Scenario: 异常传播到IExceptionHandler
- **WHEN** Service层抛出NotFoundException
- **THEN** Controller层 SHALL NOT 捕获该异常
- **AND** BusinessExceptionHandler SHALL 处理并返回404 ProblemDetails

---

### Requirement: ERR-010 Service层异常透传

Service层 SHALL 移除catch-return-failure反模式，让异常自然传播到IExceptionHandler。

**已完成模块** (eliminate-service-catch-return):
- Auth模块: 81个测试通过
- Users模块: 31个测试通过
- Patients模块: 54个测试通过
- Herbs模块: 33个测试通过
- MedicalCases模块: 41个测试通过

**保留的catch块**:
- Fire-and-forget模式 (审计日志、非关键操作)
- 重试逻辑
- 批处理item-level错误隔离

#### Scenario: Service层异常透传
- **WHEN** Repository操作失败抛出异常
- **THEN** Service层 SHALL NOT 捕获并返回Result.Failure
- **AND** 异常 SHALL 传播到Controller层
- **AND** 最终由IExceptionHandler统一处理

#### Scenario: 保留合法的fire-and-forget
- **WHEN** 审计日志记录失败
- **THEN** catch块 SHALL 记录警告日志
- **AND** 主操作 SHALL 继续执行
- **AND** 异常 SHALL NOT 影响业务结果

---

### Requirement: ERR-011 ProblemDetails工厂

新增 `ProblemDetailsFactory` 类 SHALL 提供统一的ProblemDetails创建方法。

**工厂方法**:
```csharp
public static ProblemDetails Create(
    AppException exception,
    string instance,
    string correlationId,
    string traceId)
```

**自动填充**:
- status: 根据异常类型确定HTTP状态码
- title: 根据异常类型确定标题
- type: 对应RFC文档URI
- extensions: correlationId, traceId, timestamp, errorCode, errorCategory

#### Scenario: 从ValidationException创建ProblemDetails
- **WHEN** 调用 `ProblemDetailsFactory.Create(validationException, ...)`
- **THEN** status SHALL 为400
- **AND** title SHALL 为"验证失败"
- **AND** extensions SHALL 包含errors字典

#### Scenario: 从NotFoundException创建ProblemDetails
- **WHEN** 调用 `ProblemDetailsFactory.Create(notFoundException, ...)`
- **THEN** status SHALL 为404
- **AND** title SHALL 为"资源未找到"
- **AND** extensions SHALL 包含resourceType和resourceId

---

### Requirement: ERR-012 ErrorMessages多语言映射

新增 `ErrorMessages` 静态类 SHALL 提供ErrorCode到中英文消息的映射。

**映射结构**:
```csharp
Dictionary<ErrorCode, (string Zh, string En)>
```

**API方法**:
- `Get(ErrorCode code, bool english = false)` - 获取消息
- `GetFormatted(ErrorCode code, bool english, params object[] args)` - 格式化消息

#### Scenario: 获取中文错误消息
- **WHEN** 调用 `ErrorMessages.Get(ErrorCode.UserNotFound)`
- **THEN** 返回值 SHALL 为"用户不存在"

#### Scenario: 获取英文错误消息
- **WHEN** 调用 `ErrorMessages.Get(ErrorCode.UserNotFound, english: true)`
- **THEN** 返回值 SHALL 为"User not found"

#### Scenario: 格式化错误消息
- **WHEN** 调用 `ErrorMessages.GetFormatted(ErrorCode.NotFound, false, "患者")`
- **THEN** 返回值 SHALL 为"{0}未找到"格式化后的结果

