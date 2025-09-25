# 服务端缓存健康管线验证任务

- **创建日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 背景
Phase 3 已完成缓存治理与监控链路的主体实现，但当前仓库缺乏针对核心组件的自动化测试与运行校验：
- `CacheDiagnosticsService` 与 `CacheHealthBackgroundService` 仅在手动测试中验证，阈值判断、历史快照、日志告警缺乏可重复验证的单元/集成测试。
- `CacheHealthController` 新增了多个敏感接口（清理、模式删除、诊断），需要确保授权、响应结构与异常路径符合约定。
- `QueryLayerDiagnostics.ps1` 在 API 不可达时仍回退随机数据，未对真实 API 的调用失败给出明确的运维提示，也未向文档说明脚本与接口的联动限制。

为确保 Phase 3 的成果可持续迭代，需要补齐测试、防御性处理与文档联动，形成可在 CI 中稳定运行的验证闭环。

## 目标
- 为缓存诊断与后台采样链路补充完整的单元/集成测试，覆盖阈值告警、快照滚动、日志 EventId 等关键逻辑。
- 加固系统级 API 的安全与错误处理，避免未授权调用、缺参操作导致缓存被误清除。
- 让诊断脚本在连接真实 API 失败时提供明确的错误来源和建议，并在文档中说明使用前置条件与常见故障排查步骤。

## 工作拆解
1. **缓存诊断服务测试补齐**
   - 在 `tests/UnitTests/Core/LYBT.Infrastructure.Tests` 下新增 `CacheDiagnosticsServiceTests`：
     - 构造伪造的 `ICacheService` 返回不同命中率、容量、逐出数据，验证健康等级、阈值状态、历史快照上限。
     - 覆盖命中率 < 阈值、容量 > 阈值、逐出率 > 阈值等分支，确保 EventId、日志等级符合配置。
     - 验证 `RunDiagnosticsAsync` 与 `GetLatestSnapshot` 的并发安全性（可通过并行调用 + 断言快照数量）。
   - 对 `CacheHealthBackgroundService` 使用 `FakeLogger` 与 `TestClock`（如需自定义接口），编写单元测试验证定时采样周期、异常重试与阈值告警日志内容。

2. **系统 API 集成测试**
   - 在 `tests/UnitTests/Services/LYBT.WebAPI.Tests` 新增 `CacheHealthControllerTests`（可使用 WebApplicationFactory 或最小化 TestServer）：
     - 验证未授权访问返回 401/403，Admin 角色可访问健康、统计、历史等 GET 接口。
     - 针对清理与模式删除接口，确保缺少参数、权限不足时返回正确状态码与错误信息，不实际触发缓存操作。
     - 模拟 `ICacheDiagnosticsService` 抛出异常时，接口应返回结构化错误响应并记录日志。

3. **脚本与文档强化**
   - 更新 `scripts/QueryLayerDiagnostics.ps1`：
     - 当 `-UseRealApi` 启用但请求失败时，取消随机补数，改为输出明确错误（含 HTTP 状态/异常消息），并在报告中标记“未获取真实数据”。
     - 可选：提供 `-OfflineFallback` 开关，显式由用户决定是否使用模拟数据。
   - 在 `docs/reports/server-query-layer-phase3-cache-governance-report.md` 或 README 的缓存章节新增诊断脚本使用说明：前置条件、常见错误与排查建议；强调需要 Admin token。

## 验收标准
- 新增/更新测试全部通过：`dotnet test tests -c Release --no-build`。
- `CacheDiagnosticsService` 与 `CacheHealthBackgroundService` 的阈值告警、历史快照逻辑在测试中覆盖关键分支，日志可通过断言 EventId 或消息片段验证。
- `CacheHealthController` 的主要接口在集成测试中验证授权、成功、失败三类路径；敏感操作（清理、模式删除）需要模拟缓存服务被调用的次数。
- 脚本在真实 API 失败时输出明确错误并终止为“需要排查 API 可用性”；文档同步描述新增参数及故障排查步骤。
- 相关文档更新指明脚本依赖 Admin token 和 `CacheHealthController` 接口。

> 任务完成后，请在 `docs/tasks/completed/2025-09-24-server-cache-health-validation-task-summary.md` 输出总结。
