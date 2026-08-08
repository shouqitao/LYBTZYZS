# WebApi 下一轮代码审查：业务正确性 / 安全 / 性能（2026-08-08，技术总监独立分析）

> 审查人：技术总监（独立分析）｜触发：Q-02(别名清理)+Q-03(命名空间治本) 完成后，用户要求"再一轮代码审查"，聚焦业务正确性/安全/性能（命名问题已收敛，不在范围）
> 关联：`webapi-deep-analysis-2026-08-07.md`（上一轮架构深度分析）、`13-project-master-plan.md`
> 工作流：本报告为第①份（技术总监独立报告），随后派发 Mimo Code 独立分析交叉验证，合并后出修复方案

---

## 一、审查范围与方法

- 范围：src/Server 全部代码（Controllers / Services / Repositories / Infrastructure / Module Handlers）
- 方法：静态审查 + 反模式扫描 + 关键路径（权限/并发/打印/批量）人工追踪
- 不审查：命名风格（Q-02/Q-03 已收敛）、文档一致性

---

## 二、已排查并确认无问题的项（排除法，避免误报）

| 检查项 | 结论 | 证据 |
|------|------|------|
| 同步阻塞（.Result/.Wait()） | **无** | grep 命中全是泛型 `Result<T>.Failure/Success`，非阻塞调用 |
| async void | **无** | 全仓零命中 |
| SQL 注入 | **无** | 唯一原始 SQL `LogCleanupService.cs:85` 使用 `SqlParameter` 参数化（DELETE TOP + 分批，良好） |
| 事务一致性 | **基本就位** | QuickVisit/StartVisit 显式事务；单实体写靠 SaveChanges 隐式事务足够 |
| 并发令牌 | **就位** | `BaseEntityConfiguration` + `UserConfiguration` 配 `IsConcurrencyToken`/`IsRowVersion`；`SystemExceptionHandler` 已处理 `DbUpdateConcurrencyException`→409 |
| 批次删除泛型基类（Q-01） | **设计严谨** | `BatchOperationHandlerBase<TEntity>` 模板方法，`ApplyOperationAsync`/`UpdateAsync` 抽象分离，`CatchExceptions`/`TrackIds` 钩子保行为等价；回归风险低 |
| 打印业务规则 | **符合** | `AddPrintLogAsync` 打印成功才 `IsPrinted=true`/`PrintCount++`（无次数限制）；`ResetPrintMarker` 打印后仍可修改（IsPrinted=false, PrintVersion++）；与 2026-08-03 定案一致 |
| 鉴权绕过 | **无** | `AllowAnonymous` 仅 AuthController(登录/刷新/回调)+HealthController(健康检查)，合理 |

---

## 三、发现的问题（按严重度）

### 🔴 P1 — 权限拒绝异常映射为 500 而非 403（安全/正确性缺陷）

**位置**：`src/Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs:167`（默认分支）→ 与 `MedicalCaseServiceHelper.EnsureCanEdit/EnsureCanDelete`（第 161 行抛 `UnauthorizedAccessException`）

**机制**：
- `MedicalCaseServiceHelper.EnsureCanEdit`（第 146-162 行）：非 Admin 且非本人创建/非进行中 → `throw new UnauthorizedAccessException("无权限编辑此医案：...")`
- `SystemExceptionHandler.GetExceptionInfo` 的 switch 映射了 `OperationCanceledException`/`TimeoutException`/`DbUpdateConcurrencyException`/`DbUpdateException`/`HttpRequestException`/`ArgumentException`/`NullReferenceException`/`InvalidOperationException`/`KeyNotFoundException`，**唯独缺 `UnauthorizedAccessException`**
- 落入默认分支（第 167 行）→ **返回 500 + "服务器内部错误"**

**后果**：
1. **越权访问返回 500 而非 403** —— 语义错误：客户端无法区分"服务器故障"与"无权限"，前端权限 UX 失效。
2. **审计降级**：越权是安全事件，但 `SystemExceptionHandler` 用 `LogError`（系统异常级）记录，与真实崩溃混在一起，安全审计无法区分；且返回 500 可能被监控误判为系统可用性故障。
3. 同理影响所有抛 `UnauthorizedAccessException` 的权限点（EnsureCanDelete 等）。

**修复建议（S-04）**：在 `GetExceptionInfo` switch 增加 `UnauthorizedAccessException => (403, "无权限", 生产环境隐藏细节)`，使越权正确返回 403；同时建议 `BusinessExceptionHandler` 是否已覆盖业务级无权限（若有则此处为兜底，仍应补 403 映射）。

**验证影响**：纯异常处理映射新增，0 行为变更，build 0 警告；建议补 1 个单测断言越权返回 403。

---

### 🟡 P2 — SaveWithDetailAsync 失败语义不准确（低危，待确认）

**位置**：`MedicalCaseCommandService.cs:353-355`

`SaveWithDetailAsync` 调 `SaveAsync`（内部 `ExecuteSaveAttemptAsync`）—— 保存失败时 `ExecuteSaveAttemptAsync` 返回 `null`（第 288 行 entity 为 null 时），但 `SaveAsync` 自身也可能因 `EnsureCanEdit` 抛 `UnauthorizedAccessException` 或并发异常向上传播。当返回 `null` 时，`SaveWithDetailAsync` 统一返回 `ErrorCode.NotFound`("医案不存在")，**若实际是"保存失败/无权限"，客户端收到 404 而非正确语义**。

**风险**：低（绝大多数 null 来自 entity 不存在），但语义不精确；建议区分"不存在"与"保存失败"返回码。需结合 `SaveAsync` 完整实现确认（本轮未展开 `SaveAsync` 包装层）。**列为待 Mimo 复核项**。

---

## 四、性能观察（无紧急项）

- 上一轮报告 O-04（只读查询缺 `AsNoTracking`）仍是有效优化项，但属 P1 性能、非正确性，建议并入后续性能批次，不在本轮修复。
- `LogCleanupService` 分批删除 + 延迟（第 97 行 `Task.Delay(100)`）设计良好，无锁表风险。

---

## 五、结论与下一步

本轮在"业务正确性/安全/性能"维度审查，**命名/并发/SQL注入/事务等基础质量均健康**，唯一确认的真实缺陷是 **P1：权限拒绝异常未映射 403（返回 500）**。P2 为待确认的低危语义项。

**建议**：
1. S-04（P1 修复）：`SystemExceptionHandler` 补 `UnauthorizedAccessException → 403` 映射 + 单测。
2. 派发 Mimo Code 独立分析交叉验证本报告（重点：① 确认 `UnauthorizedAccessException` 是否还有其他处理器拦截导致实际已 403；② 复核 P2 的 `SaveAsync` 失败语义；③ 全仓搜 `throw new UnauthorizedAccessException` 摸清所有受影响权限点）。
3. 交叉后定修复方案，单 commit + push。

> 注：此为独立分析报告，修复待交叉验证后执行；不修改任何代码。
