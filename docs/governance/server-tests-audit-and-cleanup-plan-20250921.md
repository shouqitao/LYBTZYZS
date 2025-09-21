# 服务器测试项目盘点与清理方案（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 日期：2025-09-21
- 目标：全面检查 Server 相关测试项目与 solution 结构，识别冗余与重复测试，提出删除/保留/整合建议（仅报告与方案，不做改动）
- 适用范围：`LYBT.Server.sln`、`tests/` 目录下与 Server 相关的测试项目

——

## 一、Server 解决方案结构（概览）

- Core：`LYBT.Infrastructure`、`LYBT.Entities`
- Services：`LYBT.WebAPI`
- Modules：Auth/Users/Patients/Herbs/Formula/Consultation/MedicalCase/Prescriptions
- Tests（已纳入 LYBT.Server.sln）：
  - 架构测试：`tests/Architecture/LYBT.ArchTests.csproj`
  - WebAPI 集成测试：`tests/IntegrationTests/WebAPI.IntegrationTests/LYBT.WebAPI.Tests.csproj`
  - 模块单元测试：Auth/Users/Patients/Herbs/Formula/Consultation/MedicalCase/Prescriptions
  - Shared.Models 单元测试（带模块映射引用）：`tests/UnitTests/Shared.Models.UnitTests/LYBT.Shared.Models.Tests.csproj`

结论：Server 解决方案的测试覆盖目标合理（架构+集成+模块+共享模型），基本匹配当前分层结构。

——

## 二、测试项目清单与建议

以下为 `tests/` 目录下主要测试项目清单、是否被 `LYBT.Server.sln` 引用，以及清理建议：

- 架构测试
  - 路径：`tests/Architecture/LYBT.ArchTests.csproj`
  - 引用：是
  - 建议：保留（约束分层/禁用框架/路由规范的门禁项）

- WebAPI 集成测试
  - 路径：`tests/IntegrationTests/WebAPI.IntegrationTests/LYBT.WebAPI.Tests.csproj`
  - 引用：是
  - 建议：保留（优先于 WebAPI UnitTests）

- WebAPI 单元测试（可能与集成测试重叠）
  - 路径：`tests/UnitTests/WebAPI.UnitTests/LYBT.WebAPI.UnitTests.csproj`
  - 引用：否
  - 建议：删除（由集成测试覆盖，更贴近端到端行为；避免重复维护）

- 模块单元测试（Auth/Users/Patients/Herbs/Formula/Consultation/MedicalCase/Prescriptions）
  - 路径：`tests/UnitTests/Modules/*/*.csproj`
  - 引用：是
  - 建议：保留（领域逻辑核心覆盖）

- Shared.Models 单元测试（带模块映射引用）
  - 路径：`tests/UnitTests/Shared.Models.UnitTests/LYBT.Shared.Models.Tests.csproj`
  - 引用：是
  - 建议：保留（跨模块映射/DTO 行为的集中验证）

- Shared.Models 单元测试（重复版本，仅引用 Shared.Models）
  - 路径：`tests/UnitTests/Shared/LYBT.Shared.Models.Tests/LYBT.Shared.Models.Tests.csproj`
  - 引用：否
  - 建议：删除（与上项项目名相同、功能重叠，易混淆）

- Shared.Utilities 单元测试
  - 路径：`tests/UnitTests/Shared/LYBT.Shared.Utilities.Tests/LYBT.Shared.Utilities.Tests.csproj`
  - 引用：否（可能纳入 `LYBT.All.sln`）
  - 建议：保留（跨层公用工具的基础测试）；若要收敛到 Server 解决方案，可仅在 `LYBT.All.sln` 中维护

- Entities 单元测试
  - 路径：`tests/UnitTests/Entities/LYBT.Entities.Tests/LYBT.Entities.Tests.csproj`
  - 引用：否
  - 建议：保留（实体规则与映射的基础测试）；是否纳入 Server 解决方案可按 CI 门禁策略决定

- Infrastructure 单元测试
  - 路径：`tests/UnitTests/Core/LYBT.Infrastructure.Tests/LYBT.Infrastructure.Tests.csproj`
  - 引用：否
  - 建议：保留（数据/配置/缓存等基础设施测试）；是否纳入 Server 解决方案可按 CI 门禁策略决定

——

## 三、删除候选（不执行，仅建议）

- 删除 `tests/UnitTests/WebAPI.UnitTests/LYBT.WebAPI.UnitTests.csproj`
  - 理由：与 WebAPI 集成测试目标重叠；集成测试更符合端到端验证；减少重复维护

- 删除 `tests/UnitTests/Shared/LYBT.Shared.Models.Tests/LYBT.Shared.Models.Tests.csproj`
  - 理由：与 `tests/UnitTests/Shared.Models.UnitTests/LYBT.Shared.Models.Tests.csproj` 名称相同、职责覆盖重叠；保留后者（包含模块引用，覆盖更全面）

若执行删除，需同步：
- 若被纳入 `LYBT.Server.sln`（当前两者均未纳入），仅做物理删除即可；
- 如在其他解决方案（如 `LYBT.All.sln`）中被引用，需先从对应 `.sln` 中移除再删除项目目录。

——

## 四、结构/拓扑改进建议（不执行，仅建议）

- 统一配置校验入口（P1）
  - 仅保留基础设施的 `EnvironmentAwareValidation` 入口；WebAPI 内部的生产校验过滤器合并/迁移，防止规则分叉

- 健康检查收敛或限权（P2）
  - 将 `SqlQueryRaw` 改为 `Database.CanConnectAsync()` + 迁移状态；`details` 接口仅管理员/非生产开放

- CORS 显式化（P2）
  - 删除 `RegisterCorsServices` 调用与占位，或仅在 `Security.Cors` 存在时启用

- 遗留扩展清理（P2）
  - 为 `Extensions/Application/MiddlewareConfigurationExtensions.cs` 与 `Extensions/ApiVersioningConfiguration.cs` 添加 `[Obsolete(error: true)]` 或移除，避免误用导致生产暴露 Swagger

——

## 五、建议执行步骤（拟定，暂不执行）

1) 评审并确认删除候选
- 由后端与测试负责人确认是否删除：
  - `tests/UnitTests/WebAPI.UnitTests/LYBT.WebAPI.UnitTests.csproj`
  - `tests/UnitTests/Shared/LYBT.Shared.Models.Tests/LYBT.Shared.Models.Tests.csproj`

2) 执行删除（示例命令）
- 从任何解决方案移除（如有）：
  - `dotnet sln LYBT.Server.sln remove tests/UnitTests/WebAPI.Unitests/LYBT.WebAPI.UnitTests.csproj`
  - `dotnet sln LYBT.Server.sln remove tests/UnitTests/Shared/LYBT.Shared.Models.Tests/LYBT.Shared.Models.Tests.csproj`
- 物理删除：
  - `git rm -r tests/UnitTests/WebAPI.UnitTests`
  - `git rm -r tests/UnitTests/Shared/LYBT.Shared.Models.Tests`

3) 调整 CI 测试脚本（如有）
- 确认 CI 仍执行：架构测试、WebAPI 集成测试、模块单元测试、Shared.Models 单元测试

4) 可选：将 Entities/Infrastructure/Shared.Utilities 测试保留在 `LYBT.All.sln` 中统一维护，Server 解决方案仅保留与 WebAPI 直接相关的测试

——

## 六、回归与验证

- 构建：`dotnet build LYBT.Server.sln -c Release`（0 错误）
- 执行测试（Server 相关）：`dotnet test tests -c Release --no-build`
- 校验：
  - 架构门禁通过
  - WebAPI 集成测试通过、关键端点覆盖在位
  - 模块单元测试通过
  - Shared.Models 单元测试通过（含映射与 DTO 约束）

> 注：本报告仅提供盘点与清理方案，未对仓库进行任何改动。如需我按此方案执行删除与 solution 清理，请确认后我再分步实施并提交。
