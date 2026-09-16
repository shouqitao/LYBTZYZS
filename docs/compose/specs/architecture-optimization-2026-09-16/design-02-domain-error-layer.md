# 设计 02：领域错误层统一

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：审计报告 L2-06 / L1-06

## 1. 问题描述（实测）

**服务端**：`LYBT.Shared.ExceptionHandling` 已有 `AppException` 基类 + `BusinessException`/`ValidationException`/`NotFoundException`/`ConflictException`/`UnauthorizedException`/`ApiException` 六子类，`ErrorCode.ToHttpStatusCode()` 已存在。**但没有「异常 → 状态码」的单点自动映射**：`BusinessExceptionHandler.TryHandleAsync` 用「类型全名字符串匹配 + `dynamic`」逐条分支处理 `DbUpdateConcurrencyException` / `CryptographicException` / `FluentValidation.ValidationException`，再单独处理 `AppException`。新增异常类型必须改处理器。

**客户端**：三条异常路径原样上抛，无统一领域异常：
| 来源 | 抛出的异常 | 携带信息 |
|---|---|---|
| 本地非 2xx | `HttpRequestException(响应体字符串, null, StatusCode)` | 状态码有、服务端业务 message 未解析 |
| 远程非 2xx（Refit） | `Refit.ApiException` | `ClientErrorMessageMapper` 靠 `GetType().FullName` 字符串匹配 + 反射取 `Content` 的 message/detail/title |
| 超时/取消 | `TaskCanceledException(Inner=TimeoutException)` | Mapper 中 `TaskCanceledException` 分支排在 `TimeoutException` 之前 → **API 超时显示为「操作被取消」** |

**后果**：同一业务失败在本地/远程给用户不同文案；本地模式丢失 `SharedHost` 异常处理器写入的 `ApiResponse.message`；超时提示语义错误。

## 2. 设计方案

### 2.1 服务端：状态码单点映射

- 新增 `AppException.ToHttpStatusCode()`（`Shared.ExceptionHandling`），实现「显式 `ErrorCode` 优先 → `Category` 兜底」：
  `Validation→400`、`Authentication→401`、`Authorization→403`、`Resource→404`、`Conflict→409`、`RateLimit→429`、`Business/General→422/500`。
  （`ErrorCategory` 枚举已定义于 `Shared.Models/Primitives/ErrorCodes/ErrorCategory.cs`。）
- `BusinessExceptionHandler` 收敛为：**① `AppException` → `exception.ToHttpStatusCode()` 一步** + **②「无 AppException 包装的类型名 → 状态码」回退表**（`Dictionary<string,int>`，消除逐条 `if`；保留 `DbUpdateConcurrencyException→409`、`CryptographicException→422`、`FluentValidation.ValidationException→400` 三条既有语义）。

### 2.2 客户端：统一领域异常

- 新增 `ApiClientException`（`Foundation/ExceptionHandling/`）：属性 `string? ErrorCode`、`string? ServerMessage`；`StatusCode` 由基类提供。
  **实施修正（2026-09-16）**：设计初稿为 `: Exception`，实测后改为 **`: HttpRequestException`** —— 既有调用方大量以 `catch (HttpRequestException)` + `ex.StatusCode` 处理传输失败（`LogoutService` / `ApiHealthCheckService` / `ConnectionModeService` / `FormulaImportDialogViewModel` / E2E 断言助手），继承基类可在引入统一形状的同时不破坏现有功能（`HttpRequestException` 的 `(message, inner, statusCode)` 构造器为 public）。
- 本地：`HttpApiClientBase.EnsureSuccessOrThrowAsync` 先把响应体解析为 `ApiResponse`/ProblemDetails（`ApiErrorEnvelope.TryExtract`），再抛 `ApiClientException`（保留原始响应体作为 `Message` 供诊断）。
- 远程：`UnifiedApiClientExtensions` 的 `RefitSettings` 增加 `ExceptionFactory`，把非 2xx 转为同一 `ApiClientException`（共用 `ApiErrorEnvelope.TryExtract`，不重复解析逻辑）。
- `ClientErrorMessageMapper`：新增 `ApiClientException` 主分支（优先 `ServerMessage`，否则状态码映射）；删除 `Refit.ApiException` 反射分支与 `ExtractMessageFromApiResponse`（已无消费者）；`TaskCanceledException` 加 `when (InnerException is TimeoutException)` 前置为超时文案。

## 3. 影响范围

`src/Shared/LYBT.Shared.ExceptionHandling/**`、`src/Client/Desktop/Core/LYBT.Desktop.Foundation/ExceptionHandling/**`、`Foundation/Http/HttpApiClientBase.cs`、`Shell/Extensions/UnifiedApiClientExtensions.cs`。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| 用户可见文案回归 | 中 | `ClientErrorMessageMapper` 现有文案表**保持不变**，只改「如何取到状态码/errorCode」；状态码→文案的既有映射逐条保留 |
| `ApiException` 反射分支被其它代码依赖 | 低 | 全仓 grep 仅 Mapper 一处消费 |
| 客户端异常类型是公共 API | 中 | 新增类型，不改既有签名 |

## 5. 实施步骤

1. 服务端 `AppException.ToHttpStatusCode()` + 处理器收敛。
2. 客户端 `ApiClientException` + 本地/远程抛出点。
3. `ClientErrorMessageMapper` 分支改造 + 超时顺序修复。
4. build + 既有错误映射测试（`ExceptionMappingE2ETests`、`DesktopExceptionHandlerTests`、`ErrorTraceCodeTests`）。
