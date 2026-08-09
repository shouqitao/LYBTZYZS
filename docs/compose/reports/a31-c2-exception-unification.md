# A-31-C2 异常统一设计报告（Shared.ExceptionHandling 完整职责）

> 任务：A-31-C2｜派发：Mimo Code｜版本：v1.0
> 完成日期：2026-08-09｜基线 commit：`61abb4c31`（A-31-C1 完成后）｜任务书：`docs/compose/specs/task-a31-c2-exception-unification-2026-08-08.md`
> 依据：S1 异常专项（`docs/compose/reports/method-audit-shared-2026-08-08.md` §3）

---

## 1. 动作清单（任务书 4 项 + 前置 2 项）

### 前置 1：蓝图 §0.5 技术栈表标注 ExceptionHandling 职责扩展 ✅

`docs/03-architecture/14-structure-design-blueprint.md`：
- §0.5 新增「技术引入治理记录（A-31-C2）」：ExceptionHandling 升级为异常完整职责项目（层次 + 处理器 + 注册扩展 + 错误码映射 SSOT），获准补充 ASP.NET Core 依赖（`FrameworkReference Microsoft.AspNetCore.App`，承载 IExceptionHandler 处理器与 `AddLybtExceptionHandling`；Desktop 进程已内嵌 LocalWebAPI，无新增运行时依赖类）；标注 Desktop 侧评估结论（职责差异保留，见 §4）；P05b 豁免清单同步说明。
- §1 SHARED 层项目表 ExceptionHandling 行职责更新（移除不准确的 "ProblemDetails" 表述，改为处理器 + 注册扩展 + 映射 SSOT + AspNetCore 依赖标注）。

### 前置 2：架构测试 P05b 豁免清单同步 ✅

`tests/LYBT.Tests.Architecture/ArchTests.cs` P05b：豁免清单注释更新（Logging + ExceptionHandling 并列例外），规则追加 `.DoNotResideInNamespaceStartingWith("LYBT.Shared.ExceptionHandling")`。

### 动作 1：处理器收敛（E1-E3）✅

- **E1**：`SystemExceptionHandler`（192 行）自 `LYBT.Infrastructure/ExceptionHandling/` 迁入 `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/SystemExceptionHandler.cs`（命名空间 `LYBT.Shared.ExceptionHandling.Handlers`，保留 `IExceptionHandler` 实现）。**依赖处理**：EF Core 异常分支（DbUpdateConcurrencyException/DbUpdateException）改为**类型名匹配**（`exception.GetType().FullName`），避免 Shared 层引入 EF Core 依赖——遵循 `ClientErrorMessageMapper` 对 Refit.ApiException 的既有类型名匹配先例。
- **E2**：`BusinessExceptionHandler`（82 行）同迁入 `Handlers/BusinessExceptionHandler.cs`。
- **E3**：`ApiServiceCollectionExtensions.cs:73-74` 两行直登改为 `services.AddLybtExceptionHandling()`；新扩展由 ExceptionHandling 提供（`Handlers/ExceptionHandlingServiceCollectionExtensions.cs`，Business 先 System 后，注册顺序语义不变）。
- 迁移后 `LYBT.Infrastructure` 对 `LYBT.Shared.ExceptionHandling` 的项目引用成为死引用，已移除（唯一消费者即两个 handler，符号级确认）。
- **传递依赖修复**：`LYBT.Module.MedicalCase` 与 `LYBT.Module.Registration` 此前经 Infrastructure 传递获得 ExceptionHandling 引用，Infrastructure 引用移除后编译失败——已为两模块补**直接** ProjectReference（明确依赖，符合模块自治原则）。

### 动作 2：映射 SSOT 化 ✅

- `ErrorCodeExtensions.ToHttpStatusCode`（Shared.Models）为**唯一**异常→HTTP 映射源（保留其 98 个显式 case + default 500）。
- 删除子类 `GetHttpStatusCode` 硬编码分支：`BusinessException.cs`（`=> 400`）、`NotFoundException.cs`（`=> 404`）。状态码统一经 `AppException.GetHttpStatusCode()` → `TypedErrorCode.ToHttpStatusCode()` 推导（`AppException.cs:35-36` 既有机制，子类不再 override）。
- **422/429 语义恢复**（S1 发现被吞）：生产 16 处 `BusinessException(ErrorCode.X, ...)` 中 14 处错误码本就映射 422（McInvalidStatusTransition/McPrescriptionRequired/McCompletedCannotSuspend 等），此前被子类硬编码 400 吞掉——现全部恢复 422；`RateLimitExceeded → 429` 本就存在（未动）。
- **补映射**：`ErrorCode.MedicalCaseMissingDiagnosis`（生产 2 处使用：`MedicalCaseCommandService.cs:119`、`MedicalCaseStateService.cs:146`）原先**不在** ToHttpStatusCode 映射中（移除 override 后会退化为 500），补入 `=> 422`（业务规则违反语义，与同组 MedicalCase 422 码一致）。
- 业务代码抛法零改动（只统一映射层）。
- 测试同步：`BusinessExceptionTests.BusinessException_WithTypedErrorCode_SetsProperties` 断言改为 `errorCode.ToHttpStatusCode()`（SSOT 推导，MedicalCaseNotFound → 404）；`ErrorCodeTests` 补 `MedicalCaseMissingDiagnosis → 422` 回归用例。

### 动作 3：死类删除（4 类 37 方法）✅

| 类 | 方法数 | 处置 |
|----|--------|------|
| `ConflictException.cs` | 8 | 删除（生产 0 引用；测试 `BusinessExceptionTests.cs` ConflictException region 同步删除） |
| `ApiException.cs` | 11 | 删除（生产 0 引用；`ClientErrorMessageMapper.cs:148-149` 处理的是 **Refit.ApiException** 按类型名匹配，不受影响） |
| `UnauthorizedException.cs` | 11 | 删除（生产 0 引用；测试 `UnauthorizedExceptionTests.cs` 整文件 + `BusinessExceptionTests.cs` region 同步删除） |
| `ValidationException.cs` | 7 | 删除（生产 0 引用；`ValidationBehavior.cs:35` 为 **FluentValidation.ValidationException**，符号级确认共享版 0 次直接构造） |

全仓 grep 确认零残留（含 tests；Desktop 测试 `catch (ApiException)` 均为 `using Refit` 的 Refit.ApiException）。

### 动作 4：Desktop 侧评估（不强迁）✅

- **结论：保留** `ClientErrorMessageMapper`（Desktop.Foundation）与 `DesktopExceptionHandler`（Desktop.Infrastructure）原地，**不实施统一迁移**。
- 理由（蓝图 §0.5 已注明差异）：**Desktop=UI 文案层**（异常/HTTP 状态 → 用户可读文案；其 ErrorCode→消息路径已委托 `ErrorMessages.GetUserMessage` 共享消息源，117 条目 SSOT）；**Server=HTTP 状态层**（异常→状态码，现收敛于 `ErrorCodeExtensions.ToHttpStatusCode`）。两方向相反、职责不同；Desktop 剩余本地表（HTTP 状态→通用文案 10 条 + ERR 前缀→文案 7 条）为 UI 兜底文案，非异常→HTTP 映射，迁入共享层不消除分叉反而使共享层承担 UI 职责。
- `DesktopExceptionHandler` 的 AppDomain/TaskScheduler 挂接是 WPF 运行时机制，本就属 Desktop 层（S1 亦如此定性）。

## 2. 验证（真实输出）

| 验证项 | 命令 | 结果 |
|--------|------|------|
| 全量构建门禁 | `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告 |
| 架构测试 | `dotnet test tests/LYBT.Tests.Architecture/` | ✅ 88/88 通过（P05b 豁免清单更新后无新增违规） |
| 异常相关单测 | `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~BusinessExceptionTests\|FullyQualifiedName~AppExceptionTests\|FullyQualifiedName~ErrorCodeTests\|FullyQualifiedName~SystemExceptionHandlerTests"` | ✅ 96/96 通过 |

## 3. 残留检查

- `LYBT.Infrastructure.ExceptionHandling` 命名空间仅剩 `ProblemTypeUris.cs`（被 WebAPI `ProblemDetailsConfiguration.cs:67` 使用，保留原地）。
- `ApiServiceCollectionExtensions` 中 `AddProblemDetailsConfiguration()`（ProblemDetails 管线）与 handler 链并行问题（S1 §3.2-Q4 遗留缺口：`UnifiedMiddlewareConfiguration.cs:37-46` 手写 `GetServices<IExceptionHandler>` 迭代不经过 ProblemDetails 管线）——**超出本任务范围**，仍为遗留风险（双输出源），建议后续批次处理。
- `SystemExceptionHandler`/`BusinessExceptionHandler` 各持一份私有 `GetCorrelationId`（S1 建议上移共享辅助 `CorrelationIdHelper`）——本任务按「先收敛再完善」方针未实施（避免范围蔓延），记为后续优化项。
- `NotFoundException` 5 个静态工厂（S1 判 D 级死方法）不在本任务删除范围（任务书仅授权 4 整类死类），保留。
- `ControllerBaseExtensions.BusinessFail` 硬编码 422 与 auth 映射 switch（S1 §3.2-Q4 提及）——本任务书动作清单未包含（任务书为权威），未改动；业务控制器返回路径不受影响。
- Desktop 双端 400→422 状态码变化对既有测试影响核查：Desktop negative 测试均用 `BeOneOf(..., UnprocessableEntity)` 断言，422 已在允许集合内，无破坏。

## 4. 变更文件清单

```
docs/03-architecture/14-structure-design-blueprint.md            # 前置 1
tests/LYBT.Tests.Architecture/ArchTests.cs                       # 前置 2（P05b 豁免）
src/Shared/LYBT.Shared.ExceptionHandling/Handlers/SystemExceptionHandler.cs        # 新增（E1）
src/Shared/LYBT.Shared.ExceptionHandling/Handlers/BusinessExceptionHandler.cs      # 新增（E2）
src/Shared/LYBT.Shared.ExceptionHandling/Handlers/ExceptionHandlingServiceCollectionExtensions.cs  # 新增（E3）
src/Shared/LYBT.Shared.ExceptionHandling/LYBT.Shared.ExceptionHandling.csproj      # FrameworkReference AspNetCore.App
src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs       # E3 改调用
src/Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs    # 删除（迁出）
src/Server/Core/LYBT.Infrastructure/ExceptionHandling/BusinessExceptionHandler.cs  # 删除（迁出）
src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj                     # 移除 ExceptionHandling 死引用
src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj          # 补直接引用
src/Server/Modules/LYBT.Module.Registration/LYBT.Module.Registration.csproj        # 补直接引用
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/BusinessException.cs   # 删 GetHttpStatusCode override
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/NotFoundException.cs  # 删 GetHttpStatusCode override
src/Shared/LYBT.Shared.Models/Primitives/ErrorCodes/ErrorCodeExtensions.cs         # 补 MedicalCaseMissingDiagnosis→422
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/ConflictException.cs  # 删除（死类）
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/ValidationException.cs # 删除（死类）
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/External/ApiException.cs        # 删除（死类）
src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Security/UnauthorizedException.cs # 删除（死类）
tests/LYBT.Tests.Server/Unit/ExceptionHandling/SystemExceptionHandlerTests.cs      # using 更新
tests/LYBT.Tests.Server/Unit/Shared/Shared/ExceptionHandling/BusinessExceptionTests.cs # 断言更新 + 3 region 删除
tests/LYBT.Tests.Server/Unit/Shared/Shared/ExceptionHandling/UnauthorizedExceptionTests.cs # 删除（死类测试）
tests/LYBT.Tests.Server/Unit/Shared/Shared/ExceptionHandling/ErrorCodeTests.cs      # 补 422 回归用例
```

## 5. 遗留风险（转后续）

1. ProblemDetails 管线与 handler 链双输出源（S1 遗留缺口，未在 A-31-C2 范围）。
2. `GetCorrelationId` 双份私有实现，可上移共享（S1 建议 `CorrelationIdHelper`）。
3. `NotFoundException` 5 静态工厂 + `ControllerBaseExtensions` 422 特判等 S1 D/C 级项，待后续清理批次。
