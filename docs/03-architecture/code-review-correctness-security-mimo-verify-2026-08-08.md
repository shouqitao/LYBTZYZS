# Mimo Code 独立交叉验证报告：技术总监代码审查发现复核（2026-08-08）

> 验证人：Mimo Code（独立分析）｜只读验证，未修改任何代码
> 被验证报告：`code-review-correctness-security-2026-08-08.md`（技术总监独立分析）
> 验证对象：P1（`UnauthorizedAccessException` 映射 500 而非 403）、P2（`SaveWithDetailAsync` 返回 404 语义不准）
> 基线：`HEAD = fbca6d641`（2026-08-08 08:52）

---

## 〇、结论速览

| 发现 | 技术总监结论 | 独立验证结论 | 定性 |
|------|------------|------------|------|
| P1 | 确认缺陷：越权返回 500 而非 403 | **403 映射已存在且自 2026-07-21 起生效**，实际行为是 403 | **误报**（漏看 `SystemExceptionHandler.cs:97-102`） |
| P2 | 待确认：保存失败/无权限时返回 404 语义不准 | null ⟺ 医案不存在，404 语义准确；权限/并发/DB 失败全部抛异常向上传播，不可能走 404 | **不成立**（null 与失败路径被正确分离） |

---

## 一、任务 1：UnauthorizedAccessException 实际 HTTP 映射（P1 复核）

### 1.1 异常处理管道注册顺序（证据）

- **DI 注册**：`src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs:73-74`
  ```csharp
  services.AddExceptionHandler<BusinessExceptionHandler>();   // 先注册 → 先处理
  services.AddExceptionHandler<SystemExceptionHandler>();      // 后注册 → 兜底
  ```
- **中间件**：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:25-62`
  `UseExceptionHandler` 为自定义 Run 委托（第 38-47 行），按 DI 注册顺序遍历 `GetServices<IExceptionHandler>()`，**首个返回 `true` 者胜出**，无匹配时兜底写 500。
- `UseExceptionHandler` 是管道第一个中间件（第 25 行），在 `UseAuthentication/UseAuthorization` 之前，因此 Controller/Action 内抛出的未捕获异常必达处理器链。
- 全仓仅此两个 `IExceptionHandler` 实现（`grep "IExceptionHandler|AddExceptionHandler|UseExceptionHandler"` 无其他命中）。

### 1.2 SystemExceptionHandler switch 全分支（证据）

`src/Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs:86-173` 实际映射：

| 行号 | 异常类型 | 状态码 |
|------|---------|--------|
| 91-95 | FluentValidation.ValidationException | 400 |
| **97-102** | **UnauthorizedAccessException** | **403 「权限不足」** |
| 105-109 | OperationCanceledException | 499 |
| 111-116 | TimeoutException | 504 |
| 119-123 | DbUpdateConcurrencyException | 409 |
| 125-129 | DbUpdateException | 500 |
| 132-136 | HttpRequestException | 502 |
| 139-143 | ArgumentException | 400 |
| 146-150 | NullReferenceException | 500 |
| 153-157 | InvalidOperationException | 500 |
| 160-164 | KeyNotFoundException | 404 |
| 167-171 | 默认分支 | 500 |

**技术总监称"唯独缺 UnauthorizedAccessException"不成立**：第 97-102 行明确存在该映射（位于 `ValidationException` 与 `OperationCanceledException` 之间，紧邻注释 `// 权限拒绝 (已认证但无权限): HTTP 403 Forbidden`）。

### 1.3 git 历史佐证（排除"近期新增"可能）

```bash
git log -S "UnauthorizedAccessException" -- src/Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs
→ 22dcac755 refactor(exception-handling): split Server/Desktop handlers back to their tiers (2026-07-21 08:07)
```

该映射由 **2026-07-21** 的提交 `22dcac755` 引入，早于技术总监报告 **18 天**。报告基线 `fbca6d641`（2026-08-08）中该映射在 HEAD 上存在。**非误读版本、非近期变更，确系漏看。**

### 1.4 BusinessExceptionHandler 覆盖范围（证据）

`src/Server/Core/LYBT.Infrastructure/ExceptionHandling/BusinessExceptionHandler.cs:30-33`：
```csharp
if (exception is not AppException appException)
{
    return false; // 交给下一个处理器
}
```
`UnauthorizedAccessException`（System 类型）**不是** `AppException` 子类 → BusinessExceptionHandler 返回 false → 落入 SystemExceptionHandler → **403**。两个处理器互不覆盖，管道行为确定。

### 1.5 全仓受影响权限点（共 3 处，均已覆盖 403）

```bash
grep -rn "throw new UnauthorizedAccessException" src/Server
```
| 位置 | 语义 | 实际映射 |
|------|------|---------|
| `MedicalCaseServiceHelper.cs:161`（EnsureCanEdit） | 越权编辑医案 | 403 ✓ |
| `MedicalCaseServiceHelper.cs:193`（EnsureCanDelete） | 越权删除医案 | 403 ✓ |
| `OperatorAccessor.cs:42`（GetOperator） | 「未登录或用户信息无效」 | 403（语义应 401，见 §4.4） |

### 1.6 结论

**P1 误报。** 实际行为：越权编辑/删除医案 → 抛 `UnauthorizedAccessException` → BusinessExceptionHandler 不处理 → SystemExceptionHandler 返回 **403「权限不足」**，不是 500。且 `MedicalCasesController.cs:122` 的 Update 接口 `[ProducesResponseType(..., 403)]` 表明 403 是文档化的设计契约。

技术总监报告中仍成立的**次要**观察（非缺陷主体）：
1. `OperatorAccessor.cs:42` 的"未登录/身份无效"走 403 而非 401（语义瑕疵，见 §4.4）；
2. `SystemExceptionHandler` 对 `UnauthorizedAccessException` 仍按 `LogError` 系统异常级记录（第 35-43 行），安全事件与系统崩溃日志混级——审计降级观察成立，但那是日志分级问题，不是状态码问题；
3. 修复建议 S-04 的代码部分**已完成**（映射已存在），但"补 1 个单测断言越权返回 403"**仍空缺**（`tests/` 全仓无 SystemExceptionHandler/GetExceptionInfo 的测试，见 §5）。

---

## 二、任务 2：SaveAsync 失败语义复核（P2）

### 2.1 调用链（证据）

```
SaveWithDetailAsync (MedicalCaseCommandService.cs:347-359)
  └─ SaveAsync (256-271)
       ├─ !request.Id.HasValue → CreateFromInputDtoAsync (262-265)   // 创建分支
       └─ 否则 → ExecuteWithConcurrencyRetryAsync(ExecuteSaveAttemptAsync) (268-270)
               └─ ExecuteSaveAttemptAsync (277-316)
```

### 2.2 null 的唯一来源

`ExecuteSaveAttemptAsync:286-288`：
```csharp
var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId, cancellationToken);
if (medicalCase == null)
    return null;   // ← 全链路唯一 null 返回点
```
创建分支 `CreateFromInputDtoAsync`（77-155 行）**从不返回 null**：所有失败路径均为抛异常（`ArgumentException`/`KeyNotFoundException`/`BusinessException`/`DbUpdateException`）。

### 2.3 权限/并发/DB 失败是否被吞？（证据：否）

`ExecuteWithConcurrencyRetryAsync`（`MedicalCaseServiceHelper.cs:113-141`）仅捕获两类异常用于**重试**：
- `DbUpdateConcurrencyException`（attempt < maxRetries，第 125 行）
- `InvalidOperationException` 且消息含「数据已被其他用户修改」（attempt < maxRetries，第 131 行）

`UnauthorizedAccessException` **不在任何 catch 集合内** → 从 `ValidateEditPermission`（第 291 行 → `EnsureCanEdit`）直接向上传播，穿透 `SaveAsync` → `SaveWithDetailAsync` → 控制器 → IExceptionHandler → **403**。重试耗尽时抛新的 `InvalidOperationException` 同样向上传播（→ 500）。

### 2.4 控制器落地（证据）

`MedicalCasesController.cs:138-141`：
```csharp
if (!result.IsSuccess)
{
    return NotFound(result.Error ?? "医案不存在");   // Result.Failure(ErrorCode.NotFound) → HTTP 404
}
```
即：**404 仅出现在 `SaveAsync` 返回 null 时，而 null ⟺ 医案实体不存在**（2.2）。权限拒绝不可能走到该分支。

### 2.5 结论

**P2 不成立。** 技术总监假设的"保存失败/无权限 → 客户端收到 404"场景**在代码上不可能发生**：
- 无权限 → 抛 `UnauthorizedAccessException` → 403（不吞、不返回 null）；
- 并发/DB 失败 → 抛异常 → 409/500；
- null → 仅 entity 不存在 → 404 语义**准确**。

技术总监"本轮未展开 SaveAsync 包装层"的自我限定成立——包装层（`ExecuteWithConcurrencyRetryAsync`）经复核不含吞异常逻辑。

---

## 三、任务 3：其他未映射/语义存疑的异常类型

全仓 `throw new` 去重统计（排除构造器 `ArgumentNullException` 49 处）：

| 异常类型 | 次数 | SystemExceptionHandler 映射 | 现状 | 评价 |
|---------|------|---------------------------|------|------|
| InvalidOperationException | 25 | 500 ✓ | 含并发重试耗尽、配置错误 | 部分语义存疑（见下） |
| BusinessException（含子类） | 19 | 经 BusinessExceptionHandler→`GetHttpStatusCode()`（400/401/404/409/500 按 ErrorCode） | ✓ | 正常 |
| ArgumentException | 7 | 400 ✓ | ✓ | 正常 |
| NotSupportedException | 6 | **无** → 默认 500 | `BaseCrudController.cs:34,41,48,55,62,69` 抽象桩（"请 override GetList"等） | 语义应 501；仅派生控制器漏 override 时触发 |
| UnauthorizedAccessException | 3 | 403 ✓ | ✓ | 见 §4.4 |
| KeyNotFoundException | 2 | 404 ✓ | ✓ | 正常 |
| ProductionConfigurationException | 1 | **无** → 默认 500 | 仅启动期配置校验 | 非请求路径，无碍 |
| FluentValidation.ValidationException | 1 | 400 ✓ | ✓ | 正常 |

### 3.1 值得注意的两处语义瑕疵（低危）

1. **并发冲突重试耗尽 → 500 而非 409（映射不对称）**：`BaseRepository.cs:123-126` 把 `DbUpdateConcurrencyException` 重抛为 `InvalidOperationException("数据已被其他用户修改")`，重试耗尽后经 SystemExceptionHandler 的 InvalidOperationException 分支 → **500**；而直接暴露 `DbUpdateConcurrencyException` 的路径 → **409**。同一业务场景（持续并发冲突）因仓储实现差异得到不同状态码。
2. **Detached 竞态 → 500 而非 404**：`MedicalCaseRepository.Update.cs:195` 在 Detached 场景查无实体时抛 `InvalidOperationException($"医案 {entity.Id} 不存在")` → 500；语义应 404（其他路径均以 null/KeyNotFoundException 表达 404）。

---

## 四、独立结论与建议

### 4.1 对技术总监 P1 的裁定：**误报，需修正报告**

- 事实：`UnauthorizedAccessException → 403` 映射自 2026-07-21（`22dcac755`）起存在于 `SystemExceptionHandler.cs:97-102`，管道（Business→System 顺序 + 首个 true 胜出）行为确定，越权编辑/删除返回 403 无误。
- 技术总监对 switch 的分支枚举漏了第 97-102 行，导致"缺映射→500"推断失效。
- 建议：技术总监报告 §三 P1 修正为"已覆盖（自 07-21 起），无需代码修复"；S-04 仅保留测试项。

### 4.2 对技术总监 P2 的裁定：**不成立（语义准确）**

- null 与失败路径在代码上严格分离：null ⟺ 不存在 → 404 合理；权限/并发/DB 失败抛异常 → 403/409/500。
- 建议：P2 从报告中移除或标记"已复核无问题"。

### 4.3 仍然有效的行动项（按优先级）

| 优先级 | 行动 | 理由 |
|--------|------|------|
| P2（低） | 补 1 个单测：断言 `SystemExceptionHandler.GetExceptionInfo` 将 `UnauthorizedAccessException` 映射为 403 | 映射已存在但全仓**零测试覆盖**（tests 无 SystemExceptionHandler/GetExceptionInfo 引用），是唯一真正空缺 |
| P3（低） | 统一并发冲突状态码：重试耗尽后的 `InvalidOperationException("数据已被其他用户修改")` 映射为 409 或显式抛 `DbUpdateConcurrencyException` | 消除同一场景 409/500 的不对称（§3.1） |
| P3（低） | `OperatorAccessor.cs:42` 未登录/身份无效改抛 401 语义（如 `UnauthorizedException`(401) 或专门映射） | 「未登录」当前返回 403；受 `[Authorize]` 保护的路由实际难触发，纯语义瑕疵 |
| P4（可选） | `BaseCrudController` 的 `NotSupportedException` 补 501 映射或声明为抽象 | 6 处抽象桩目前落入默认 500 |

### 4.4 复核声明

- 本报告全部结论基于 HEAD `fbca6d641` 的静态证据（文件+行号+git 历史），未运行集成测试验证 HTTP 行为；
- 如需动态确认，建议在补单测时以真实 `UnauthorizedAccessException` 走一次处理器链断言 403（与 §4.3 行动项合并）。

---

## 五、证据索引

| 证据 | 位置 |
|------|------|
| `UnauthorizedAccessException → 403` 映射 | `src/Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs:97-102` |
| 映射引入提交 | `git log -S "UnauthorizedAccessException"` → `22dcac755`（2026-07-21） |
| 处理器注册顺序 | `ApiServiceCollectionExtensions.cs:73-74` |
| 管道遍历逻辑 | `UnifiedMiddlewareConfiguration.cs:38-47` |
| BusinessExceptionHandler 仅处理 AppException | `BusinessExceptionHandler.cs:30-33` |
| 越权抛出点（3 处） | `MedicalCaseServiceHelper.cs:161/193`、`OperatorAccessor.cs:42` |
| null 唯一来源 | `MedicalCaseCommandService.cs:286-288`（ExecuteSaveAttemptAsync） |
| 重试包装不吞 UnauthorizedAccessException | `MedicalCaseServiceHelper.cs:113-141` |
| 404 落地 | `MedicalCasesController.cs:138-141`、`MedicalCaseCommandService.cs:353-355` |
| 403 设计契约 | `MedicalCasesController.cs:122`（`[ProducesResponseType(403)]`） |
| 并发重试耗尽 → InvalidOperationException | `BaseRepository.cs:123-126`、`MedicalCaseServiceHelper.cs:140` |
| Detached 竞态 → InvalidOperationException | `MedicalCaseRepository.Update.cs:195` |
| 单测覆盖缺口 | `tests/` 无 `SystemExceptionHandler`/`GetExceptionInfo` 引用 |
