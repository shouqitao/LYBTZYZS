# 全量代码审核报告（LYBT.All）

## 执行概览
- 构建：`dotnet build LYBT.Server.sln -c Release --no-restore` 成功
- 构建（All）：`dotnet build LYBT.All.sln` 失败（WPF Core 对象池引用与命名冲突）
- 测试：部分单测通过，Users 模块单测大量失败；架构测试失败 1 项
- 时间：2025-09-21

## 关键发现
- 架构违规
  - `tests/Architecture/ArchTests.cs:62` 检出 UI 层依赖 Entities 层：`HealthController`
- 桌面端构建失败（影响 All.sln）
  - 缺少 `Microsoft.Extensions.ObjectPool` 引用且内部命名空间与泛型 `ObjectPool<T>` 同名导致解析冲突
  - 路径：`src/Client/Desktop/Core/ObjectPool/ObjectPoolService.cs`
- 业务时间使用分散且多处 `DateTime.Now`
  - 建议服务端统一 `DateTimeOffset.UtcNow`，并引入 `IClock` 抽象；UI 可保留本地时区显示
  - 典型位置：`src/Shared/LYBT.Shared.Models/*`、`src/Server/Modules/*`
- Users 模块单测大量失败
  - 失败数 67，涵盖创建/删除/批量启用禁用/口令场景（见 `LYBT.Module.Users.Tests`）
- 覆盖率与收敛
  - 当前聚合报告显示多数模块 0%（噪音居多），`Shared.Interfaces` 100%（接口层）
  - 覆盖率采集存在 `coverlet` 映射路径干扰与“无构建”运行的差异，需要校正收集方式
- 过时 API 提示
  - `UnifiedEventArchitecture` 系列标注 `[Obsolete]`，建议逐步迁移至 Prism 简化事件

## 风险评估
- 架构违规：中（违背边界，易引入耦合回归）
- All.sln 构建失败：高（阻塞全量流水线、桌面端不可构建）
- Users 单测失败：高（核心域稳定性与回归保护不足）
- 时间 API 不统一：中（多时区/序列化/报表一致性风险）
- 覆盖率收集失真：中（质量信号失真，影响决策）

## 诊断细节
- 架构测试
  - 命令：`dotnet test tests/Architecture/LYBT.ArchTests.csproj -c Release --no-build`
  - 失败：`LayerDependencyTests_UI_Should_Not_Depend_On_Entities`
- 单元测试
  - Users：`LYBT.Module.Users.Tests` 多用例失败（创建/删除/批量操作/密码场景）
  - Shared.Models：通过 6
- 代码扫描（示例）
  - `DateTime.Now` 多处：`src/Shared/LYBT.Shared.Models/*`、`src/Server/Modules/*`、`src/Client/Desktop/Core/*`
  - `async void` 出现在 UI 事件处理器（可接受，但需异常捕获）

## 结论
- Server 侧构建健康，但 All.sln 因桌面端失败不能全绿
- 架构边界与测试质量需重点收敛；时间 API 与对象池问题需优先修复

