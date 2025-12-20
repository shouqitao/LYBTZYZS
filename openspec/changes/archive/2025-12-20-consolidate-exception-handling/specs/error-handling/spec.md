# error-handling Specification Deltas

## ADDED Requirements

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
