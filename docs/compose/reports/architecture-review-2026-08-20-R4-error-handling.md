# 第4轮审查：错误处理链完整性验证

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent (架构师视角, intended vs implemented)  
> **审查范围**: `06-error-handling` 异常体系/处理器链/ErrorCode/CorrelationId/RateLimit + `AppException`继承链 + `IExceptionHandler` + `ControllerBaseExtensions` + `Desktop CrudServiceBase` + `UnifiedMiddlewareConfiguration`

## 一、执行摘要

| 维度 | 结论 |
| :--- | :--- |
| **链路完整性** | `Controller(零catch) → Service(抛AppException/返Result) → IExceptionHandler链(Business→System) → ApiResponse/ProblemDetails` 主链闭合，`CorrelationId(W3C traceparent→X-Correlation-ID→LogContext→Serilog)` 全链路贯通，`RateLimit 429` 与 `Validation 400/422` 双通道均可达 |
| **门禁** | `B02_Use_GlobalExceptionHandler_Only` 架构守卫通过（禁`GlobalExceptionMiddleware`），但 `ConfigurationController:176 catch(Exception)` 与 `Registration/Catalog` 多处 `InvalidOperationException` 直抛破坏“类型化ErrorCode”约定 |
| **严重度** | 🔴 CRITICAL 2 · 🟠 HIGH 3 · 🟡 MEDIUM 3 · ✅ PASS 6 |
| **最大风险** | `Registration.Cancel/StartVisit` 的 `InvalidOperationException` 未携带 `TypedErrorCode`，经 `SystemExceptionHandler` 误判为 **500** 而非 **422(ERR-80301)**；`BusinessException` 无码分支默认500导致前端无法按`ERR-3xxxx`分支提示 |

---

## 二、CRITICAL（错误码/状态码丢失）

### C1 — 领域守卫大量抛 `InvalidOperationException` 裸异常：`SystemExceptionHandler` 将业务校验误判为 500
- **意图** (`06-error-handling §异常类型体系`)：业务违规必须抛 `BusinessException(ErrorCode)`→`BusinessExceptionHandler`→`TypedErrorCode.ToHttpStatusCode()`→`400/409/422`
- **证据**：
  ```csharp
  // RegistrationModel.cs:114-122
  public void Cancel(){ if(Status!=Waiting) throw new InvalidOperationException("只有等待中的挂号可以取消"); }
  public void StartVisit(){ if(Status!=Waiting) throw new InvalidOperationException("只有等待中的挂号可以接诊"); }
  // MedicalCaseCommandService.ValidateEditReason:255 throw new InvalidOperationException("医案已打印...")
  ```
  `SystemExceptionHandler.cs:173-176` 对 `InvalidOperationException` 统一返回 `500 "操作无法执行"`，`ErrorCode` 丢失
- **特例**：`CancelRegistrationCommandHandler:36 catch(InvalidOperationException)` 在 Handler 层捕获并转`Result.Failure(ErrorCode.RegistrationInvalidStatusTransition)`，**该路径正确**；但 `RegistrationCrossModuleService.HandleMedicalCaseCancelledAsync:44 entity.Cancel()` 未被Handler捕获，直达`SystemExceptionHandler`→500
- **修复**：所有`InvalidOperationException`域守卫改为`throw new BusinessException(ErrorCode.RegistrationInvalidStatusTransition, message)`；`Registration.CancelFromInProgress`（第3轮C1修复）同样携带`ErrorCode`

### C2 — `BusinessException` 无 `TypedErrorCode` 分支默认500
- **证据**：`BusinessException.cs:20-29 BusinessException(string message)` 仅设`UserMessage`，`TypedErrorCode=null`；`AppException.GetHttpStatusCode()=> TypedErrorCode?.ToHttpStatusCode() ?? 500`
- **证据2**：`BatchOperationHandlerBase.cs:96 catch(Exception ex)` 将`ex.Message`直接作为`FailedItems.Reason`，未保留`TypedErrorCode`
- **修复**：禁`BusinessException(string)`无码构造（加`ArchTest: BusinessException must have TypedErrorCode`）；批量路径透传`TypedErrorCode`

---

## 三、HIGH（链路降级/可观测性受损）

### H1 — `ConfigurationController:176 catch(Exception)` 与 `SecurityAuditService:39 catch(Exception)` 静默吞异常
- **证据**：`ConfigurationController.cs:176 catch(Exception ex){ _logger.LogWarning(...); }` 为审计写入的`fire-and-forget`，吞`DbUpdateException`导致配置变更成功却审计丢失
- **修复**：保留`catch`但提升为`LogError` + `Meter.Counter("audit_write_failed")`

### H2 — 双验证通道状态码不一致：`FluentValidation.ValidationException(400)` vs `LYBT.ValidationException(422)`
- **证据**：`ValidationBehavior:13` MediatR管线抛`FluentValidation.ValidationException`→`SystemExceptionHandler:48→400`；而`MedicalCaseCommandService`手写校验抛`ValidationException:422`
- **修复**：统一为`422 + TypedErrorCode=ValidationFailed`，`ValidationBehavior`捕获后转抛`LYBT.ValidationException`

### H3 — 桌面端 `CrudServiceBase.ExecuteAsync` 全局 `catch(Exception)` 丢失 `CorrelationId/ErrorCode`
- **证据**：
  ```csharp
  // CrudServiceBase.cs:126-135
  catch(Exception ex){ return CommandResult<T>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operation,ex)); }
  ```
  `GetSafeOperationFailureMessage`按`HttpStatusCode`映射为固定文案，丢弃`ApiResponse.Errors.correlationId/code`
- **修复**：捕获`Refit.ApiException`时提取`Content`中的`Errors.correlationId/code`并透传至`CommandResult`

---

## 四、MEDIUM

### M1 — `RateLimit 429` 未使用 `ErrorCode.RateLimitExceeded(ERR-00012)`
- **证据**：`UnifiedMiddlewareConfiguration.cs:53 UseStatusCodePages`对`429`返回`ApiResponse.CreateFail("请求处理失败 (HTTP 429)")`，未注入`code=ERR-00012`
- **修复**：`429→ErrorCode.RateLimitExceeded.ToFormattedString()`

### M2 — `DbUpdateConcurrencyException` 字符串匹配重试
- **证据**：`MedicalCaseServiceHelper.cs:132 catch(InvalidOperationException ex) when(ex.Message.Contains("数据已被其他用户修改"))` 依赖中文消息
- **修复**：`Repository`抛`ConflictException(ErrorCode.ConcurrencyConflict)`，`ServiceHelper`改`catch(ConflictException)`

### M3 — `NullReferenceException/ArgumentException` 在生产环境隐藏细节但丢失 `ErrorCode`
- **证据**：`SystemExceptionHandler:180-195`对`ArgumentException→400`，`NullReferenceException→500`，生产环境返回固定文案但无`code`字段
- **修复**：生产环境仍返回`code=ERR-00000(Unknown)` + `category=General`

---

## 五、PASS

| 链路 | 证据 | 结论 |
| :--- | :--- | :--- |
| **IExceptionHandler链顺序** | `UnifiedMiddlewareConfiguration:25-52 UseExceptionHandler(Run→GetServices<IExceptionHandler> foreach TryHandleAsync)`，`Business→System` + Fallback 500 | ✅ |
| **CorrelationId全链路** | `UseLybtCorrelationId`尽早注册；`Business/SystemHandler.GetCorrelationId: X-Correlation-Id→TraceIdentifier`；`Serilog Enricher + AsyncLocal` | ✅ |
| **ErrorCode→HttpStatus映射** | `ErrorCodeExtensions.ToHttpStatusCode`覆盖`400/401/403/404/409/422/429/500/503`全分区 | ✅ |
| **Controller零业务catch** | `grep Controllers catch`仅`ConfigurationController`审计1处；`MedicalCase/Patients/Registration`均`HandleResult` | ✅ |
| **模块级特殊约束** | `MedicalCase BR-001 409`，`Herbs BR-DEL-001 400`，`Users sysadmin 403`均经`BusinessException(ErrorCode)` | ✅ |
| **Desktop仓储层透传** | `ApiClientRepositoryBase.HandleException: ExceptionDispatchInfo.Capture.Throw()`保留堆栈，`CrudServiceBase`仅服务层吞 | ✅ |

---

## 六、修复清单

| 优先级 | 缺陷 | 文件 | 改动 |
| :--- | :--- | :--- | :--- |
| **P0** | C1 裸`InvalidOperationException` | `RegistrationModel.cs` + `MedicalCaseCommandService.ValidateEditReason` | 全部改为`BusinessException(TypedErrorCode)` |
| **P0** | C2 无码`BusinessException` default 500 | `BusinessException.cs` + `BatchOperationHandlerBase.cs` | 禁无码构造，批量透传`TypedErrorCode` |
| **P1** | H2 验证双通道400/422 | `ValidationBehavior.cs` + `SystemExceptionHandler.cs` | 统一422 |
| **P1** | H3 桌面丢失CorrelationId | `CrudServiceBase.cs` | 提取`Errors.correlationId/code` |
| **P2** | M1 429无ERR-00012 | `UnifiedMiddlewareConfiguration.cs:45` | `429→ErrorCode.RateLimitExceeded` |
| **P2** | M2 重试字符串匹配 | `MedicalCaseServiceHelper.cs:132` | `catch(ConflictException)` |

> **验证**：`dotnet test` 新增 `ExceptionMappingTests` + 手动`curl -H "X-Correlation-Id: test-123" /api/v1/patients/invalid → {"code":"ERR-..." "correlationId":"test-123"}`；`Desktop`冒烟`CrudServiceBase`失败提示含追踪ID。
