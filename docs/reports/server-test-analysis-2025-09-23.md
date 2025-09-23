# 服务端测试与覆盖率现状分析（2025-09-23）

本报告根据最新仓库状态对服务端（`LYBT.Server.sln`）的测试体系进行扫描，重点关注测试目录结构、执行结果、覆盖率与环境配置，评估其与“服务端测试需达到 100% 覆盖并全部通过”这一目标的差距。

## 1. 测试工程与工具链概览

- **测试目录结构**：`tests/` 下划分为 Architecture、IntegrationTests、UnitTests、SecurityTests 等子目录，覆盖架构检查、WebAPI 合约、各业务模块及核心层单测。例如：
  - 基础设施单测：`tests/UnitTests/Core/LYBT.Infrastructure.Tests`（缓存、配置、仓储等）（`tests/UnitTests/Core/LYBT.Infrastructure.Tests/LYBT.Infrastructure.Tests.csproj`）。
  - 模块单测：`tests/UnitTests/Modules/<Module>.UnitTests`，每个模块包含 Service / Repository / Mapping 等测试文件（如 `tests/UnitTests/Modules/Auth.UnitTests/Services/AuthServiceTests.cs`）。
  - 集成测试：`tests/IntegrationTests/WebAPI.IntegrationTests/SimpleApiContractTests.cs` 针对认证、健康检查、用户接口做 API 合约校验。
- **覆盖率配置**：`tests/Directory.Build.targets` 为所有测试项目自动引入 Coverlet & ReportGenerator，并统一输出到 `BIN/TestResults`，目标阈值在 `tests/COVERAGE.md` 中设定（行覆盖 ≥90%，关键模块 ≥95%）。
- **覆盖率执行脚本**：`tests/RunCoverage.ps1`（Windows）及 `tests/run-coverage.sh`（Linux/macOS）封装 `dotnet test --collect:"XPlat Code Coverage"` 与报告生成流程。

## 2. 实际执行结果

在 PowerShell 环境下运行 `dotnet test LYBT.Server.sln -c Release`（`pwsh -Command`）结果显示：

- **模块测试失败**：`LYBT.Module.Consultation.Tests` 中的 `ConsultationMappingProfileTests` 断言 AutoMapper 配置缺少多项字段映射（`tests/UnitTests/Modules/Consultation.UnitTests/Mapping/ConsultationMappingProfileTests.cs:25-74`）。实体 `Consultation` 的 `CreatedBy/Status/RowVersion/StartTime/EndTime/...` 等属性未在 Profile 中配置，导致 `AssertConfigurationIsValid()` 抛出 `AutoMapperConfigurationException`。
- **集成测试失败**：`LYBT.WebAPI.Tests` 中 `SimpleApiContractTests` 期望所有 API 返回统一 `{ success, message, data }` 结构，但健康检查与登录接口未满足，造成多处断言失败（例如 `tests/IntegrationTests/WebAPI.IntegrationTests/SimpleApiContractTests.cs:60-137`）。
- 测试执行在上述失败后退出，后续测试项目未进入执行阶段，致使整体测试结果为失败。

## 3. 覆盖率现状

- 最新覆盖率报告 `tests/TestCoverageReport_Final.md` 显示：行覆盖率仅 **0.5% (247/45,209)**，分支覆盖率 **0.2%**，远低于 100% 的目标（`tests/TestCoverageReport_Final.md:9-40`）。
- 报告指出：虽然已经大量生成测试文件（基础设施、模块、实体等），但处于“创建测试骨架、尚未匹配真实代码”的阶段，存在字段命名不一致、DTO 属性缺失、仓储/服务签名变动等问题，导致大多数测试无法编译或断言失败（`tests/TestCoverageReport_Final.md:66-140`）。
- 由于 `dotnet test` 当前无法通过，覆盖率脚本无法输出有效数据，形成“测试多、覆盖率低、不可执行”的状态。

## 4. 主要缺口与风险

1. **测试与业务实现脱节**
   - 部分单测基于过时/假设字段，例如 `ConsultationMappingProfileTests` 仍断言 DTO 中存在 `DoctorName`、`StartTime` 等字段为空值，而实体模型已有更新（参考 `src/Server/Modules/LYBT.Module.Consultation`）。
   - 其它模块的测试也可能存在类似错位（历史报告曾提及 `PatientDto.IdCard`、`PatientUpdateDto.MedicalHistory` 等字段缺失）。
2. **API 合约测试过于理想化**
   - Integration 测试假设所有响应遵循统一包裹结构，但健康检查等基础端点仍返回原生 ASP.NET Core 模式，需要调整测试或统一接口实现。
3. **覆盖率与执行策略未落地**
   - 虽然 `tests/COVERAGE.md` 制定了严格指标，实际并未建立“先保证测试可运行，再谈覆盖率”的基线；大量测试文件处于模板状态，影响真实覆盖统计。
4. **CI/本地门禁缺失**
   - 当前 `dotnet test` 失败仍可合入，说明缺乏强制门禁。新代码易继续建立在失效测试之上。

## 5. 优先整改建议

1. **恢复测试可执行性（阻断因素）**
   - 以当前实体/DTO/Service 实现为准修复失败断言，逐个项目执行 `dotnet test --filter FullyQualifiedName~<Failure>`，确保能独立通过。
2. **评估并调整 API 合约测试范围**
   - 明确 API 统一响应是否为近期目标；若不是，应调整测试预期；若是目标，则需同步改造 WebAPI 输出。
3. **覆盖率推进策略**
   - 建立“可执行测试集 + 最低覆盖率 baseline”（例如 15-20%），使用 `tests/RunCoverage.ps1` 输出真实覆盖率；优先强化关键模块（Auth、Users、MedicalCase、Prescriptions）的 Service/Repository/Controller 测试。
4. **引入门禁与自动化**
   - 在 CI 中添加 `dotnet test LYBT.Server.sln` 阶段；修复通过后，再启用 `EnforceCoverageThresholds`（`tests/Directory.Build.targets` 已预置）。
5. **同步更新测试文档**
   - 待修复完成后重新生成覆盖率与执行报告，避免旧文档（如 `tests/TestCoverageReport_Final.md`、`tests/TestExecutionFinalReport_20250121.md`）导致误判。

## 6. 建议整改路径（示例）

| 阶段 | 目标 | 建议动作 |
|------|------|----------|
| Phase 0 | 测试可运行 | 修复 `Consultation` AutoMapper 测试；调整 WebAPI 合约测试预期；确保 `dotnet test LYBT.Server.sln` 返回 0。 |
| Phase 1 | 基准覆盖率 | 完善核心服务（Auth、Users、Patients）单测，结合 Coverlet 输出 15-20% 覆盖率基线报告。 |
| Phase 2 | 模块扩展 | 逐一补齐其他业务模块、控制器、仓储层测试，目标 60%+。 |
| Phase 3 | 覆盖率门禁 | 在 CI 中启用 `EnforceCoverageThresholds`（初始可设 30%-40%），稳定后逐步提高。 |

---

**结论**：虽然仓库已搭建完善的测试工程与覆盖率脚本，但当前测试严重脱节、无法运行，覆盖率接近于无。要达成“服务端测试 100% 覆盖、全部通过”的要求，必须先修正现有失败用例，重新建立可信的测试基线，随后按模块持续补充高质量用例，并通过 CI 门禁和覆盖率策略固化成果。

