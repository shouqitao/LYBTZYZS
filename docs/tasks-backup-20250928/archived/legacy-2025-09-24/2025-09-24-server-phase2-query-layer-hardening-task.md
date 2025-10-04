# 任务：Server层查询组件巩固与性能验证（Phase 2）

- **发布日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 背景
Phase 1 已完成查询层重构，建立 ReadRepository 体系并实现 QueryService 解耦。总结报告指出缓存命中率、性能收益和测试覆盖率仍需验证，目前缺少统一的回归保障与运行态指标。为确保重构成果稳定落地，需要进入巩固迭代，对缓存策略、查询性能与单元测试进行专项完善。

## 目标
- 校验并固化缓存策略与软删除过滤逻辑，确保 QueryService 行为与旧版本一致。
- 构建针对 ReadRepository 的单元测试与性能基准测试，验证核心查询路径。
- 建立可复用的查询层诊断脚本与文档，支撑后续 CQRS 规划。

## 工作内容
1. **缓存策略核实与补强**
   - 梳理 7 个 ReadRepository 的缓存键命名、过期策略与软删除过滤是否一致。
   - 补充缓存命中率可视化日志（保留默认级别为 Debug），确保上线可追踪。
   - 针对高频查询补充缓存穿透防护（空结果缓存或参数校验）。
2. **测试体系构建**
   - 在 tests/UnitTests/Modules 子目录下新增或补全 ReadRepositoryTests，覆盖缓存、生存期、软删除过滤。
   - 引入轻量性能基准（BenchmarkDotNet 或自定义 Stopwatch 基线），对比缓存前后查询耗时并记录数据。
   - 调整现有 QueryService 测试，使其通过仓储 Mock 验证缓存命中与 DTO 投影结果。
3. **文档与脚本**
   - 在 docs/reports 下新增 server-query-layer-phase2-hardening-report.md，记录缓存策略、测试结果与性能数据。
   - 在 scripts 下新增 QueryLayerDiagnostics.ps1，支持指定模块执行缓存状态、EF 查询跟踪与性能采样。
   - 更新 README.md 的查询层章节，说明缓存策略与测试约定。

## 验收标准
- 缓存逻辑统一，7 个 ReadRepository 的缓存键格式与过期策略一致并通过代码审查。
- 新增的单元测试与性能基准脚本全部运行通过，并在报告中附录性能数据。
- 查询层诊断脚本与文档到位，README 已同步。
- dotnet test tests -c Release --no-build 与 dotnet build LYBT.All.sln -c Release 均通过。

> 完成后请在 docs/tasks/completed/2025-09-24-server-phase2-query-layer-hardening-task-summary.md 中总结执行情况。
