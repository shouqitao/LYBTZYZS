# 服务端查询层缓存治理（Phase 3）

- **创建日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 背景
Phase 2 已经统一了查询仓储的缓存键命名、空值穿透保护与调试日志，并补全了基准测试与脚本工具。但阶段总结暴露三项风险：
1. 内存缓存容量依旧由代码硬编码，缺少与配置中心对齐的尺寸治理与阈值告警，无法响应不同部署环境的容量差异。
2. CacheStatistics 目前只在 MemoryCacheAdapter 内部累计，未对外输出，也没有后台巡检手段，诊断脚本仍然依赖随机模拟数据，无法满足运维对实时命中率与逐出次数的需求。
3. Phase 2 建议的监控面板与逐出策略尚未具象化；若不补强，Phase 3 的 CQRS 推进将面临无数据支撑的容量风险。

## 目标
- 以配置驱动的方式治理服务器内存缓存的容量、逐出与扫描策略，支持按环境调节阈值并输出结构化日志。
- 建立缓存运行态监控链路：周期性抓取实际命中率/逐出/容量指标，提供系统接口供脚本与后续 Prometheus 接入复用。
- 强化 QueryLayerDiagnostics 脚本，使其读取真实数据并给出基于阈值的建议，形成开发-运维共享的可观测闭环。

## 工作拆解
1. **缓存容量治理实现**
   - 在 `src/Server/Core/LYBT.Infrastructure/Configuration/Options` 下新增 `CacheOptions`（含 `Memory`、`Monitoring` 子节点），绑定 `appsettings[*].json` 中的 `CacheOptions` 配置，提供数据注解校验与默认值。
   - 调整 `UnifiedServiceRegistration.RegisterInfrastructureServices`：读取新 Options 中的 `SizeLimit`、`CompactionPercentage`、`ExpirationScanFrequency` 等参数，替换当前硬编码；当配置缺失时保持向后兼容并输出带 EventId 的 Warning 日志。
   - 在 `MemoryCacheAdapter` 中补充缓存项大小与优先级策略：允许通过 `CacheOptions.Memory.ItemSize`/`PriorityStrategy` 设置 `MemoryCacheEntryOptions.Size` 与 `Priority`，提供默认 LRU 行为，并对逐出回调记录结构化日志（包含 `reason`、`key` 前缀、估算占用）。

2. **运行态监控与告警**
   - 新增 `ICacheDiagnosticsService`（Infrastructure 层），负责包装 `ICacheService.GetStatisticsAsync` 并计算命中率、容量占比、逐出速率等指标；允许注入阈值（如命中率<0.8、容量占比>0.85）。
   - 在 WebAPI 层注册 `CacheHealthBackgroundService`（`BackgroundService`），默认每 60 秒采样一次，超过阈值时写 Warning 日志并通过 `EventId` 明确分类；采样结果缓存最近一次快照以供控制器读取。
   - 基于 `BaseSystemController` 新增系统接口 `CacheHealthController`（路由 `/api/v1/system/cache/health`，仅限 Admin），返回最近快照（命中率、逐出次数、容量占比、阈值命中情况）以及采样时间；补充集成测试验证授权与返回结构。

3. **诊断脚本与文档联动**
   - 更新 `scripts/QueryLayerDiagnostics.ps1`：在 `-CacheStatus` 模式下调用上述 API（支持自定义 BaseUrl/token），解析 JSON 并替换现有随机模拟逻辑；当命中率、容量占比达到阈值时，以颜色区分并附带改善建议。
   - 在 `docs/reports/server-query-layer-phase2-hardening-report.md` 基础上新增缓存治理章节，描述新的配置项、告警规则与脚本使用示例；同步更新 README 查询架构章节的缓存配置小节。
   - 在 `tests/UnitTests/Core/LYBT.Infrastructure.Tests`、`tests/UnitTests/Services/LYBT.WebAPI.Tests` 补充针对 Options 绑定、Diagnostics Service 计算逻辑、背景服务阈值触发的单元/集成测试，覆盖告警分支。

## 验收标准
- 所有缓存参数均可通过 `appsettings*.json` 驱动，未配置时继承默认值并打印单次 Warning；`dotnet build LYBT.All.sln -c Release --no-restore`、`dotnet test tests -c Release --no-build` 均需通过。
- `/api/v1/system/cache/health` 返回的快照包含命中率、容量占比、逐出次数及阈值状态，未授权访问返回 401/403，相关测试到位。
- BackgroundService 在命中率或容量占比超过阈值时写入结构化 Warning 日志（包含指标值、阈值、采样窗口），并可在测试中通过 FakeLogger 验证。
- QueryLayerDiagnostics 脚本在连接本地 API 时展示真实数据；当 API 不可达时输出明确错误并回退为离线提示，不再使用随机模拟数值。
- 文档更新列出新配置与监控流程，Phase 3 完成后便于 Phase 4（CQRS）复用。

> 任务完成后，请在 `docs/tasks/completed/2025-09-24-server-phase3-cache-governance-task-summary.md` 生成同名总结，覆盖实现细节、测试与遗留风险。
