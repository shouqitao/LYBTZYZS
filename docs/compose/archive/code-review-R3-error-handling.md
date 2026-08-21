# R3 错误处理与日志审查

**审查范围**：`src/Server/` 全部代码、`src/Client/Desktop/` 异常处理、`src/Shared/` 异常定义  
**审查日期**：2026-08-21  
**审查角度**：异常捕获、日志级别、CorrelationId、错误码

---

## 一、总体评价

| 维度 | 评级 | 说明 |
|------|------|------|
| 异常体系 | ✅ 优秀 | 统一 AppException 体系 + ExceptionFactory 工厂 + 结果模式（Result\<T\>）双轨并行 |
| 错误码定义 | ✅ 优秀 | MCCEE 分区规则清晰，100+ 条枚举，双语消息覆盖完整 |
| 全局异常处理 | ✅ 良好 | BusinessExceptionHandler + SystemExceptionHandler 链式处理，环境感知 |
| CorrelationId | ✅ 良好 | 中间件 + LogContext 注入 + 响应回传，端到端追踪 |
| 日志级别 | ✅ 良好 | 业务异常 Warning / 系统异常 Error 分级明确 |
| 敏感数据脱敏 | ✅ 良好 | SensitiveDataMasker + SensitiveDataAttribute + Serilog DestructuringPolicy |
| 客户端错误映射 | ✅ 良好 | ClientErrorMessageMapper 统一映射，用户友好 |

---

## 二、发现的问题

### P1 🔴 高优先级

#### P1-1: `ApiClientRepositoryBase.HandleException` 使用 `throw ex` 破坏调用栈

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/ApiClientRepositoryBase.cs:31`

```csharp
protected void HandleException(Exception ex, string operation)
{
    Logger.LogError(ex, "[REPO] {LogPrefix}.{Operation} failed", LogPrefix, operation);
    throw ex;  // ← 重置异常堆栈
}
```

**问题**：`throw ex` 会重置 StackTrace，丢失原始调用位置。上层日志只能看到 `HandleException` 的位置，无法追溯真正的业务调用点。

**建议**：改为 `throw;` 保留原始堆栈。`ExecuteAsync` 模板中的 `throw; // unreachable` 注释也佐证了调用方预期保留堆栈。

---

#### P1-2: `BaseApiController.LogOperation` 空 catch 吞异常

**文件**：`src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:48-51`

```csharp
catch
{
    // 记录日志失败时不应影响主业务流程
}
```

**问题**：虽然设计意图合理（日志不应阻断主流程），但空 catch 完全吞掉了所有异常类型（包括非日志相关的程序异常），且没有任何降级日志输出。若 `GetOperator()` 抛出非预期异常（如 ClaimsPrincipal 损坏），会被静默吞掉。

**建议**：至少捕获具体异常类型（`catch (Exception) when (/* 仅日志相关异常 */)`），或在 catch 中输出 `Console.Error` 作为降级：

```csharp
catch (Exception ex)
{
    // 日志系统不可用时降级到 Console
    Console.Error.WriteLine($"[WARN] LogOperation failed: {ex.Message}");
}
```

---

#### P1-3: `CredentialVault` 匿名 catch 块完全吞掉异常

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/CredentialVault.cs:183-186`

```csharp
catch
{
    return false;
}
```

**问题**：匿名 catch 吞掉所有异常且无任何日志或降级输出。对于密码凭据检查操作，无法排查 DPAPI 不可用、文件损坏等问题。

**建议**：至少添加 `_logger.LogDebug(ex, "...")` 或 `_logger.LogWarning(...)`。

---

### P2 🟡 中优先级

#### P2-1: ErrorCode 4xxxx（处方模块）区域为空

**文件**：`src/Shared/LYBT.Shared.Models/Primitives/ErrorCodes/ErrorCode.cs:461-463`

```csharp
#region 4xxxx - 处方模块 (Prescriptions)
#endregion
```

**问题**：分区预留但无任何错误码定义。所有处方相关错误散落在医案模块（304xx）中，如 `McPrescriptionFlagNotSet`、`McPrescriptionAlreadyExists` 等。未来处方模块独立演进时将缺乏专用错误码。

**建议**：按实际需要补充 4xxxx 预留码（即使当前映射到 304xx），或在文档中注明 4xxxx 合并到 304xx 的决策。

---

#### P2-2: ErrorCode 7xxxx 区域完全缺失

**文件**：`src/Shared/LYBT.Shared.Models/Primitives/ErrorCodes/ErrorCode.cs`

**问题**：MCCEE 分区规则文档中列出了 0-6、8，但跳过了 7（没有注释说明）。ClientErrorMessageMapper 中映射了 `ERR-07` = "同步相关错误"，但 ErrorCode 枚举中没有 7xxxx 的任何定义。

**建议**：补充 7xxxx 分区定义（如 Reports/Diagnostics 模块），或修正 ClientErrorMessageMapper 中的映射。

---

#### P2-3: `PatientsController.Update` 使用字符串匹配判断错误类型

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:228`

```csharp
if (result.Error?.Contains("不存在") == true)
    return NotFound(result.Error);
return HandleResult(result, useAuthMapping: true);
```

**问题**：通过 `Error?.Contains("不存在")` 判断是否返回 404，依赖中文错误消息的硬编码匹配。若消息国际化或措辞变更，判断逻辑失效。

**建议**：改用 `result.ModuleErrorCode == ErrorCode.PatientNotFound` 类型化判断。

---

#### P2-4: `MedicalCasesController` 未使用 `HandleResult` 统一映射

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`

**问题**：多个端点（`Create`、`Update`、`Delete`、`SetPrescriptionFlag`、`RecordPrint`、状态流转端点）直接 `return NotFound(result.Error ?? "...")`，绕过了 `HandleResult` 的 ErrorCode→HTTP 状态码映射。当 CommandService 返回具体的 MedicalCase 错误码（如 `30201 无权限` → 403，`30402 处方已存在` → 422）时，全部被降级为 404。

**影响**：客户端收到 404 而非语义正确的 403/422，无法区分「资源不存在」和「业务规则拒绝」。

**建议**：统一使用 `HandleResult(result, useAuthMapping: true)` 或显式检查 `result.ModuleErrorCode`。

---

#### P2-5: `ConflictException.Category` 返回 `Business` 而非 `Concurrency`

**文件**：`src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/ConflictException.cs:11`

```csharp
public override ErrorCategory Category => ErrorCategory.Business;
```

**问题**：`ConflictException` 用于并发/唯一性冲突场景，语义上应属于 `ErrorCategory.Concurrency`，而非 `ErrorCategory.Business`。这会影响日志分类和监控告警。

**建议**：改为 `ErrorCategory.Concurrency`。

---

#### P2-6: `BusinessExceptionHandler` 日志中包含 `UserId`（用户名）

**文件**：`src/Shared/LYBT.Shared.ExceptionHandling/Handlers/BusinessExceptionHandler.cs:47`

```csharp
httpContext.User?.Identity?.Name ?? "匿名用户"
```

**问题**：业务异常日志中直接记录用户名。在医疗系统中，用户名属于 PII（个人可识别信息），生产环境日志中应脱敏。

**建议**：使用 `SensitiveDataMasker` 脱敏或仅记录 `UserId`（GUID）而非用户名。

---

#### P2-7: `SystemExceptionHandler` Development 环境暴露完整 StackTrace

**文件**：`src/Shared/LYBT.Shared.ExceptionHandling/Handlers/SystemExceptionHandler.cs:55`

```csharp
stackTrace = exception.StackTrace,
```

**问题**：Development 环境返回完整堆栈。虽为标准做法，但需确认 `Development` 环境不会意外在生产中启用（如环境变量配置错误）。

**建议**：增加注释说明此行为，或改用 `_environment.IsProduction()` 的反向判断（Production 显式隐藏）。

---

### P3 🟢 低优先级 / 建议

#### P3-1: `ValidationException` 硬编码返回 422，不走 `ToHttpStatusCode` 映射

**文件**：`src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/ValidationException.cs:11`

```csharp
public override int GetHttpStatusCode() => 422;
```

**问题**：`ValidationException` 始终返回 422，即使构造时传入了 `TypedErrorCode`。`AppException.GetHttpStatusCode()` 会查映射表，但 `ValidationException` 覆写了此方法。设计意图是正确的（业务层验证 = 422），但与 FluentValidation 管道的 400 形成双标准——需确保开发者理解何时用哪个。

**建议**：在类注释中明确：`ValidationException` 用于服务层业务验证（422），FluentValidation 管道自动返回 400。

---

#### P3-2: `AppException.UserMessage` 与 `Message` 默认值不一致

**文件**：`src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Base/AppException.cs`

```csharp
public AppException() : base("应用程序异常") { }
public AppException(string message) : base(message) { }
```

**问题**：无参构造设置 `Message = "应用程序异常"`，但 `UserMessage` 为 null。当 `UserMessage` 为 null 时，`BusinessExceptionHandler` 会回退到 `Message`，可能向用户暴露内部消息。

**建议**：无参构造和单参构造中同步设置 `UserMessage`。

---

#### P3-3: `HttpApiClientBase.EnsureSuccessOrThrowAsync` 在失败时读取整个响应体

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs:108`

```csharp
var errorContent = await response.Content.ReadAsStringAsync();
```

**问题**：错误响应体可能很大（如 500 错误页面），全部读入字符串可能造成内存压力。

**建议**：添加大小限制（如前 4KB），或使用 `ReadAsStreamAsync` + 限制读取。

---

#### P3-4: `ClientErrorMessageMapper.GetRefitApiExceptionMessage` 使用反射

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs:182`

**问题**：通过 `GetType().GetProperty("StatusCode")` 反射获取 Refit.ApiException 属性。设计意图是避免直接引用 Refit 包，但反射存在性能开销和维护脆弱性。既然项目已经引用 Refit（RefitApiClient 存在），可直接 `catch Refit.ApiException`。

**建议**：评估是否可以直接引用 Refit 类型，或至少缓存 PropertyInfo。

---

#### P3-5: `DesktopExceptionHandler.DetermineLogLevel` 将 `HttpRequestException` 映射为 `Information`

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ExceptionHandling/DesktopExceptionHandler.cs:135`

```csharp
HttpRequestException => LogLevel.Information,
TimeoutException => LogLevel.Information,
```

**问题**：网络请求失败（如服务器宕机）记为 Information 级别可能被默认日志过滤掉。对于医疗系统，网络异常应至少为 Warning。

**建议**：考虑提升为 `LogLevel.Warning`。

---

#### P3-6: `MedicalCasesController.CancelMedicalCase` 缩进异常

**文件**：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs:356`

```csharp
LogOperation("取消医案", null, id);
```

**问题**：该行缩进不一致（顶格），虽不影响功能，但影响代码一致性。

---

## 三、正面发现（做得好的地方）

### ✅ 异常体系设计优秀

- **双轨制**：`Result<T>` 用于已知业务错误（不抛异常），`AppException` 用于需要全局处理的异常——清晰的职责分离
- **ExceptionFactory**：一行代码抛出语义化异常（`ExceptionFactory.NotFound(...)`），降低使用门槛
- **异常层次**：`AppException → BusinessException/NotFoundException/ConflictException/ValidationException/UnauthorizedException/ApiException`，每种 HTTP 语义一个子类

### ✅ 全局异常处理链设计完善

- `BusinessExceptionHandler` → `SystemExceptionHandler` 链式处理，先业务后兜底
- 环境感知：Development 暴露详细信息，Production 隐藏
- EF Core 异常按类型名匹配（避免 Shared 层引入 EF Core 依赖）

### ✅ CorrelationId 端到端追踪

- 中间件支持 W3C `traceparent` + `X-Correlation-ID` 双 Header
- `LogContext.PushProperty` 注入所有日志
- 响应头回传 CorrelationId，客户端可关联
- 异常处理器中提取 CorrelationId 并写入 ApiResponse

### ✅ 敏感数据脱敏体系完整

- `[SensitiveData]` 属性标记敏感字段（PatientModel 的 IdNumber/PhoneNumber）
- `SensitiveDataMasker.SerializeWithSanitization` 用于日志脱敏
- `SensitiveDataJsonConverterFactory` 用于 JSON 响应脱敏
- Serilog `SensitiveDataDestructuringPolicy` 管道级脱敏

### ✅ 错误码体系规范

- MCCEE 编码规则清晰（模块-子类别-序号）
- 双语消息字典完整（`ErrorMessages`）
- `ToHttpStatusCode()` 映射覆盖全面（400/401/403/404/409/422/429/500/503）
- `ToFormattedString()` 统一格式（`ERR-30001`）

### ✅ 客户端错误映射

- `ClientErrorMessageMapper` 提供用户友好消息
- Refit.ApiException 内容解析 + 状态码回退
- 统一 `DefaultErrorMessage` 兜底

---

## 四、错误码覆盖矩阵

| 模块 | 错误码范围 | 已定义数量 | HTTP 状态码覆盖 | 覆盖评估 |
|------|-----------|-----------|----------------|---------|
| 通用 | 0xxxx | 12 | 400/401/403/404/429/500/503 | ✅ 完整 |
| 用户/Auth | 1xxxx | 20 | 400/401/403/422 | ✅ 完整 |
| 患者 | 2xxxx | 16 | 400/409/422 | ✅ 完整 |
| 医案 | 3xxxx | 43 | 400/403/404/409/422/500 | ✅ 完整 |
| 处方 | 4xxxx | 0 | — | ⚠️ 空（合并到 304xx） |
| 草药 | 5xxxx | 14 | 400/403/404/422 | ✅ 完整 |
| 配方 | 6xxxx | 17 | 400/403/404/422 | ✅ 完整 |
| 挂号 | 8xxxx | 3 | 404/422 | ⚠️ 较少 |
| 同步 | 7xxxx | 0 | — | ❌ 缺失 |

---

## 五、422 vs 400 使用分析

| 场景 | 当前使用 | 评估 |
|------|---------|------|
| FluentValidation 管道 | 400 | ✅ 正确（ASP.NET Core 默认） |
| 服务层业务验证（ValidationException） | 422 | ✅ 正确（语义：请求格式正确但业务规则不允许） |
| BusinessFail 扩展方法 | 422 | ✅ 正确（业务规则违反） |
| 参数校验（ValidateGuid/ValidatePagination） | 400（ValidationFail） | ✅ 正确（输入格式错误） |
| 电话唯一冲突 | 409（PATIENT-PHONE-409-FIX） | ✅ 正确（资源冲突） |
| 并发版本冲突 | 409 | ✅ 正确 |

---

## 六、优先级排序与建议行动

| 优先级 | 编号 | 问题 | 建议修复方式 |
|--------|------|------|------------|
| 🔴 P1 | P1-1 | `throw ex` 破坏堆栈 | 改为 `throw;` |
| 🔴 P1 | P1-2 | LogOperation 空 catch | 缩小异常类型 + Console 降级 |
| 🔴 P1 | P1-3 | CredentialVault 匿名 catch | 添加日志输出 |
| 🟡 P2 | P2-1 | 4xxxx 错误码缺失 | 补充或文档注明合并 |
| 🟡 P2 | P2-2 | 7xxxx 错误码缺失 | 补充分区定义 |
| 🟡 P2 | P2-3 | PatientsController 字符串匹配 | 改为 ErrorCode 类型化判断 |
| 🟡 P2 | P2-4 | MedicalCasesController 未用 HandleResult | 统一映射 |
| 🟡 P2 | P2-5 | ConflictException.Category 错误 | 改为 Concurrency |
| 🟡 P2 | P2-6 | 日志中暴露用户名 | 脱敏或仅记 UserId |
| 🟢 P3 | P3-1 | ValidationException 422 硬编码 | 添加文档说明 |
| 🟢 P3 | P3-2 | UserMessage null 回退 | 构造函数同步设置 |
| 🟢 P3 | P3-3 | 错误响应体无大小限制 | 添加截断 |
| 🟢 P3 | P3-4 | Refit 反射获取属性 | 评估直接引用 |
| 🟢 P3 | P3-5 | HttpRequestException 日志级别低 | 提升为 Warning |
| 🟢 P3 | P3-6 | MedicalCasesController 缩进异常 | 格式化修正 |

---

## 七、结论

LYBTZYZS 项目的错误处理与日志体系**整体设计优秀**，在 .NET 8 项目中属于高水平实现：

1. **异常体系**：双轨制（Result + Exception）设计成熟，ExceptionFactory 降低使用门槛
2. **错误码**：MCCEE 分区规则清晰，100+ 覆盖完整，双语支持
3. **全局处理**：链式处理器 + 环境感知 + RFC 7807 ProblemDetails 支持
4. **CorrelationId**：端到端追踪完整，W3C Trace Context 兼容
5. **脱敏**：多层脱敏体系（属性标记 + JSON 转换器 + Serilog 管道）

主要改进方向集中在 P1 级别的 3 个问题（堆栈破坏 + 2 处空 catch），以及 P2 级别的 MedicalCasesController 错误码映射统一化。这些问题修复后，错误处理体系将达到生产级标准。
