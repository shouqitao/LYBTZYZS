# 2025-09-24 缓存健康监控遗留问题修复计划

- **创建日期**：2025-09-24
- **编写人**：Thinker（ChatGPT）

## 背景
Phase 3 缓存治理新增了诊断服务、后台健康监控与脚本联动，但现有代码和测试中仍存在阻断 CI 的编译错误以及阈值逻辑未生效的问题。为确保缓存健康管线在持续集成环境下稳定运行，需要集中修复以下缺陷。

## 问题清单
1. **基础实体断言过时**（测试编译失败）
   - `tests/UnitTests/Core/LYBT.Infrastructure.Tests/Data/AppDbContextTests.cs:566-583` 仍根据旧用户模型构造数据（`Password`、`Name`、`CreatedBy="system"`、`CommonStatus.Deleted` 等），与现有实体 `PasswordHash` / `RealName` / `Guid? CreatedBy` 不匹配。
   - 直接导致 `dotnet test` 出现 `CS0029`、`CS0117` 编译错误。
2. **FluentAssertions API 使用了废弃方法**
   - `DefaultPasswordOptionsTests.cs:220/226` 与 `SysAdminOptionsTests.cs:260` 调用 `StringAssertions.HaveMinimumLength`，该方法在当前版本不存在，编译时报 `CS1061`。
3. **缓存统计未回填容量数据**
   - `MemoryCacheAdapter.GetStatisticsAsync` 仅返回命中数等基础指标，`CurrentItemCount`、`MaxCapacity`、`TotalMemoryUsage`、`EvictionRate` 始终为零。
   - `CacheDiagnosticsService.CheckThresholds` 与 `CacheHealthBackgroundService.LogThresholdAlerts` 因此无法触发容量/逐出告警（EventId 5002/5003）。
4. **配置日志触发多重 ServiceProvider**
   - `UnifiedServiceRegistration.cs:136` 在 `ConfigureServices` 阶段执行 `services.BuildServiceProvider()` 以获取 Logger，违反 ASP.NET Core 单容器约定，可能导致额外实例与配置缺失。

## 修复目标
- 让 `dotnet test tests/UnitTests/Core/LYBT.Infrastructure.Tests -c Release` 正常编译执行，覆盖缓存诊断新增用例。
- 使缓存阈值告警在真实数据下可触发，并输出结构化日志。
- 保持缓存配置日志的安全注入方式，避免二次构建容器。

## 修复方案
1. **同步测试数据模型**
   - 更新 AppDbContext 用例：改用 `PasswordHash`、`RealName`、`Guid? CreatedBy` 等字段；若需要软删除状态，使用 `IsDeleted` / `DeleteStatus`。
   - 检查并统一 `CommonStatus` 的使用，移除对已删除的枚举值引用。
2. **调整断言写法**
   - 替换 `HaveMinimumLength` 为 `Subject.Length.Should().BeGreaterOrEqualTo(…)` 或 `HaveLengthGreaterThanOrEqualTo`（结合最新 FluentAssertions API）。
   - 确认测试使用的 FluentAssertions 版本与写法一致。
3. **补齐缓存统计数据**
   - 在 `MemoryCacheAdapter` 中维护 `_statistics.CurrentItemCount`、`TotalMemoryUsage`（可基于 `_keys.Count` 的估算或配置值）、`MaxCapacity = cacheOptions.Memory.SizeLimit`，并在逐出回调时更新 `EvictionRate`（结合历史时间间隔）。
   - 确保 `CacheDiagnosticsService` 能获得真实的容量、逐出速率以判定阈值。
4. **安全注入 Logger**
   - 移除 `BuildServiceProvider` 调用，改用 `services.AddSingleton<ILogger<MemoryCacheOptions>>` 注入或在 `Configure` 阶段通过 `ILogger<UnifiedServiceRegistration>` 记录一次缺省配置警告。

## 工作分解
1. 更新 `AppDbContextTests` 用户/患者构造逻辑，使其使用新字段并移除旧枚举。
2. 全文搜 `HaveMinimumLength`、`Password =`、`CommonStatus.Deleted` 校验是否还有遗漏，统一替换。
3. 调整 `MemoryCacheAdapter` 统计字段：
   - 在 `Set`/`Remove`/`Clear` 时维护 `CurrentItemCount`。
   - 回填 `MaxCapacity`、`TotalMemoryUsage`、`EvictionRate`。
4. 修改缓存注册日志调用方式，确保不再构建临时 ServiceProvider。
5. 重新执行 `dotnet test tests/UnitTests/Core/LYBT.Infrastructure.Tests -c Release` 与 `dotnet test tests/UnitTests/Services/LYBT.WebAPI.Tests -c Release` 验证。
6. 更新 `docs/tasks/completed/2025-09-24-server-cache-health-validation-task-summary.md` 补充缺陷修复记录。

## 验收标准
- 所有相关测试项目编译并通过，CI 不再因缓存健康测试阻断。
- 手动模拟高容量/高逐出率时能看到 EventId 5002/5003 的 Warning 日志。
- `ConfigureServices` 中无 `BuildServiceProvider` 调用，应用启动日志正常。
- 文档总结更新，说明修复内容与测试结果。

## 风险提示
- `MemoryCacheAdapter` 统计的准确性需与监控需求对齐，若需更精确数据，后续可能引入内部计数或性能计数器。
- 测试修改涉及多个领域实体，需关注与其他模块的映射/DTO 测试是否同步。
- 日志注入改动需验证不会影响现有依赖于默认缓存配置的模块。
