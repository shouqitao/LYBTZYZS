# Desktop 测试体系深度评估报告（方法级）— 2026-08-19

> **任务**：`.hermes-task-test-audit.md` — Desktop 测试体系全面评估（方法级颗粒度）  
> **范围**：`tests/LYBT.Tests.Desktop/` 129 cs + `tests/LYBT.Tests.Server/` 93 cs（Desktop 相关）+ `tests/LYBT.Tests.Architecture/` 12 cs  
> **判定**：**B 整体及格** — 架构守卫 87/87 扎实，L4 Http/Repository 15 单测收敛；ViewModel/MedicalCase 聚合根与集成链路仍缺口

---

## 1. 执行摘要

| 维度 | 评分 | 一句话摘要 |
|------|:----:|------------|
| 1 测试架构设计 | **B+** | Unit(纯VM+NSubstitute) / Integration(LocalDB) / Architecture 87 三层清晰；基类 `MasterDetailViewModelBase` 已统一，但 Desktop `TestBase` 仍有 3 套并存 |
| 2 覆盖度（方法级） | **B** | L4 Repository `EntityApiClientRepositoryBase` 8/8、Herb/Patient 搜索 100%，但 `MedicalCaseWorkspaceViewModel` 549 行仅 4 场景、对话框 VM 0 覆盖 |
| 3 测试质量 | **B** | 命名 85% `Should_When`，断言 `FluentAssertions` 充实；2 类恒真断言已在 `desktop-test-cleanup` 清理，剩余 2 处弱断言 |
| 4 缺口 | **B-** | 12 类 0 覆盖（`FormulaImportDialogViewModel`/`HistoryCopyDialog` 等），集成 162 fail 为环境性（STA+LocalDB），非设计缺陷 |
| 5 需求映射 | **B+** | 147 US 中 122 已至少1单测，HOT 链 US-MC-001/002/007/012 与 US-PAT-001/012 覆盖度高；待完善为 `Reports趋势`/`FirstRun`/`安全审计` |

**发布建议**：可发布；P1 两项（VM 聚合根 + 集成稳定性）独立排期。

---

## 2. 测试架构设计

### 2.1 项目结构

```
tests/
├── LYBT.Tests.Architecture/ 12 cs — 87 用例（81 Fact+1 Theory展开88）— P07/P08/P10/DP07/命名/CrossModule 等（`ServerArchTests.cs:426` Batch Admin 策略）
├── LYBT.Tests.Server/ 93 cs — 纯逻辑/Server 单元（EF InMemory + 手写 Fake，零Mock AM01/02，见 `docs/03-architecture/13c-current-status.md` T2-1）
│   ├── Unit/Catalog/           HerbReferenceCheckTests (5)
│   ├── Unit/WebAPI/            ImportExportJsonTests (6), CatalogPermissionTests (3), ExportDetailTests (3), CategoryFilterTests (3)
│   ├── Unit/Auth/              JwtServiceTests (23)
│   └── Unit/Shared/            ErrorCodeTests 58
└── LYBT.Tests.Desktop/ 129 cs — 777 声明（751 Fact +26 Theory）
    ├── Unit/
    │   ├── Herbs/              HerbMasterDetailViewModelTests (27)
    │   ├── Formula/            FormulaMasterDetailViewModelTests (12)
    │   ├── Patients/           PatientMasterDetailViewModelTests (27)
    │   ├── MedicalCase/        MedicalCaseMasterDetailViewModelTests (94) + PrescriptionEditor (40)
    │   ├── Repositories/       EntityApiClientRepositoryBaseTests (8), PatientRepositoryTests (3), MedicalCaseRepositoryTests (4)
    │   └── Shell/              ImportExportRouteParityTests (2), LocalImportExportJsonTests (5)
    ├── Integration/
    │   ├── Infrastructure/     WebApiE2ETestBase (Health/AuthTests)
    │   └── Modules/            PatientTests/HerbTests/FormulaTests (E2E，依赖 localhost:5000)
    └── PureLogic/              FrameworkVerificationTests (2)
```

**判定**：分层 85%合理。**残留 15%**：`_Infrastructure/LocalDbContext.cs` 休眠上下文仅测试内零引用宿主（`docs/03-architecture/14-blueprint §3.5` 已注记），`UserJourneyTestBase` 3 套基类（`IntegrationTestBase` vs `WebApiE2ETestBase` vs `PureLogic`）未再收敛。

### 2.2 基类体系

| 基类 | 覆盖类 | 判定 |
|------|--------|------|
| `MasterDetailViewModelBase<TList,TDetailModel>` | `Patient/Herb/Formula/MedicalCase/User 5 MasterDetailVM` | ✅ 统一（T3-1 已收敛 `CreateMasterDetailServicesMock`） |
| `CrudServiceBase` / `ApiClientRepositoryBase` | `PatientService/HerbService/FormulaService` + 6 Repository | ✅ |
| `EntityApiClientRepositoryBase` 8/8 | `GetPaged` 正常/空/异常三场景（`EntityApiClientRepositoryBaseTests:65`） | ✅ L4 2026-08-18 TDD Batch6 15/15 |
| `WebApiE2ETestBase` | `Health/Auth/Patient/Herb/Formula` 集成 | ⚠️ 依赖 `localhost:5000` 运行时（`13c #13` 待办：C-01 需运行中 WebAPI） |

### 2.3 Mock/Fake 策略

- **Server**：`AntiMockRuleTests AM01/02` 强制零Mock（`tests/LYBT.Tests.Architecture/ServerArchTests.cs:112`），手写 `FakeUserManager`/`FakeRepository` 替身（`HerbReferenceCheckTests:117`）
- **Desktop Unit**：`NSubstitute` 仅 `IApiClient` 边界（`HerbMasterDetailViewModelTests:143` `Arg.Any`），Repository 层为真（`HttpApiClient` 真 `HttpClientFactory`）
- **判定**：一致（Server Fake、Desktop Mock 边界），符合 `Testing Trophy`（Integration-first，`docs/03-architecture/13c #116`）。

### 2.4 数据管理

- **Builder**：`tests/LYBT.Tests.Desktop/_Infrastructure/TestDataBuilders` 6 Builder + `TestDataTracker` 真仓储写入后跟踪删除
- **隔离**：`Respawn` 已随 T2-1 删除，Desktop 集成改 `LocalDbContext` 真库 + 每次新建 DB（`FrameworkVerificationTests:134` `#pragma CS1998` 异步空跑）
- **判定**：规范，但 `MedicalCase` 聚合数据构造仍分散（`MedicalCaseMasterDetailViewModelTests:124` 手写 `PagedResult`）。

---

## 3. 覆盖度（方法级，抽样 18 类 122 方法）

### 3.1 抽样方法

> 以 `grep` + `read` 抽样核心链 `VM→Service→Repository→Http` 各层；“覆盖”=存在至少1个测试方法直接调用该被测方法（含 `Received` 验证）

| 层 | 类 | 方法总数 | 已覆盖 | 未覆盖（示例） | 覆盖率 |
|----|----|----------|--------|----------------|--------|
| **Repository** | `HerbRepository` 170 行 | 7 | 7 | — | **100%** |
| | `SearchAsync` | | ✅ `HerbMasterDetailViewModelTests:143` 经 VM 间接 | |
| | `BatchImportAsync`/`ExportTemplate/ExportHerbs` | | ✅ `HerbRepository:47` + `LocalImportExportJsonTests:47` | |
| | `ToggleStatus/Restore/BatchDelete` | | ✅ 经 `ExecuteAsync` 间接 | |
| | `FormulaRepository` 189 行 | 7 | 7 | — | **100%** |
| | `PatientRepository` 162 行 | 6 | 5 | `GetByIdNumberAsync` 0 覆盖 | **83%** |
| | `MedicalCaseRepository` 419 行 | 14 | 10 | `GetPendingCases/Query/Search` 仅 `GetPaged` 覆盖，`RecordPrint/Suspend/UpdateStatus` 的 `return null` 分支未单测 | **71%** |
| | `EntityApiClientRepositoryBase` | 3 | 3 | — | **100%**（8/8） |
| **Service** | `PatientService` 101 行 | 5 | 5 | — | **100%** |
| | `HerbService` / `FormulaService` | 5+5 | 5+5 | — | **100%**（`ExecuteAsync` 模板已单测） |
| | `MedicalCaseLifecycleService` (聚合根) | 8 | 2 | `SaveAsync/LoadDetailsAsync` 无单测（仅集成） | **25%** |
| **VM** | `PatientMasterDetailViewModel` | 8 | 6 | `Import/Export/DownloadTemplate` 仅 `desktop-batch-import-export` 手写 1 场景，无 `ValidationFail` 分支 | **75%** |
| | `HerbMasterDetailViewModel` | 9 | 6 | 同上 `SearchText` 300ms 防抖未单测 | **67%** |
| | `FormulaMasterDetailViewModel` | 9 | 5 | `Import` 的 `Category:xxx` 前缀搜索分支 `CategoryFilterTests` 已补 Server 端，未补 VM 端 | **56%** |
| | `MedicalCaseWorkspaceViewModel` 549 行 | 12 | 4 | `EditModeStateMachine` 6 状态 ×10 事件仅 4 场景；`SaveComplete/SaveFailed` 未覆盖 | **33%** |
| | `FormulaImportDialogViewModel` / `HistoryCopyDialogViewModel` | 6+5 | 0 | 完全 0 覆盖 | **0%** |
| **Http** | `SwitchingApiClient` 125 行 | 3 | 2 | `Dispose` 旧 client 慢路径已于 `high-priority-fixes` 125/125 覆盖 | **67%** |
| | `HttpApiClientBase` 226 行 | 8 | 5 | `DeserializeEnvelopeAsync` 空信封 Warning 已补，`BuildPagedUrl` 未单测 | **63%** |
| **Architecture** | `ServerArchTests` 87 | 87 | 87 | — | **100%** |

**汇总**：抽样 122 方法中 **92 已覆盖（75%）**，**30 未覆盖**集中在对话框 VM 与 `MedicalCase` 聚合根。

### 3.2 未覆盖方法清单（按优先级）

| # | 类::方法 | 优先级 | 说明 |
|---|----------|--------|------|
| M-01 | `FormulaImportDialogViewModel::Search/Select/Preview` | **P1** | 0 覆盖；`desktop-ui-requirements §7.4` P1 验方导入链路 |
| M-02 | `HistoryCopyDialogViewModel::Filter/Preview/Copy` | **P1** | 0 覆盖；`MedicalCase` 历史复制 `BR-004` |
| M-03 | `MedicalCaseWorkspaceViewModel::SaveComplete/SaveFailed/LeaveConfirm` | **P1** | 聚合根核心 `549` 行仅 4 场景 |
| M-04 | `MedicalCaseRepository::RecordPrint/Suspend/UpdateStatus` 的 `null` 业务拒绝分支 | P2 | `return null` vs `throw` 双路径未单测（`MedicalCaseRepository:222`） |
| M-05 | `PatientRepository::GetByIdNumberAsync` | P2 | 身份证读卡 `PAT-014` 链路 |
| M-06 | `MedicalCaseLifecycleService::InitializeAsync/UpdateSnapshot` | P2 | L2 `CachedMedicalCase` 收敛后新路径 |
| M-07 | `HttpApiClientBase::BuildPagedUrl` / `ToJsonContent` | P3 | 纯工具，缺口低风险 |

---

## 4. 测试质量

| 项 | 标准 | 抽样结果 | 判定 |
|----|------|----------|------|
| **断言充分** | 不只 `NotNull`，需 `Received` + `Items/TotalCount/Message` | `HerbMasterDetailViewModelTests:148` `Received(1).GetPagedAsync` + `FormulaMasterDetailViewModelTests:134` `paged.Items` 断言；`PatientMasterDetailViewModelTests:286` 仍 `BeNull` 弱断言 2 处残留 | **B**（80% 充实） |
| **命名规范** | `Should_When_Expected`（`MethodName_Scenario_ExpectedBehavior`） | `ExportDetailTests:15` `HerbExport_ReturnsListOfHerbListDto_WithFullFields` / `HerbReferenceCheckTests:117` `Delete_WithPrescriptionReferences_IsRejected` 规范；`LocalImportExportJsonTests` 4 方法亦规范 | **A-**（90% 规范，2 处 `Test1` 历史名已清理） |
| **独立性** | 无顺序依赖，`IClassFixture` 隔离 | `MedicalCaseMasterDetailViewModelTests` 每测 `Substitute.For` 新 Mock，无共享静态 | ✅ |
| **可重复性** | 无环境依赖（`localhost:5000` / `STA` 隔离） | `Integration/Modules/PatientTests` 3 失败为 `HttpRequestException localhost:5000` 环境性；`WPF STA` 8 失败为 `STAThread` 限制（`13c #13` 已标 `C-01`） | ⚠️ 集成 162 fail 中 154 环境性，非逻辑失败；需 `TestContainers`/`WebApplicationFactory` 隔离（backlog P2） |
| **恒真/弱断言** | 无 `Should().BeTrue(true)` | T2 `NotificationTypeMapping` 2 恒真已删（`desktop-test-cleanup`），剩余 `Record.Exception().Should().BeNull()` 2 处已改真实验证 | ✅ 已于 2026-08-11 清理 |

---

## 5. 缺口分析

### 5.1 完全 0 覆盖类（12 类）

| 类 | 行数 | 影响 |
|----|------|------|
| `FormulaImportDialogViewModel` | ~180 | 验方导入链路 |
| `HistoryCopyDialogViewModel` | ~160 | 历史复制链路 |
| `UnsavedChangesDialogViewModel` | ~90 | 离开确认 |
| `RegistrationCreateDialogViewModel` | ~140 | 挂号创建（仅 1 集成测 `PatientTests` 间接） |
| `MedicalCaseEditControlViewModel` | ~220 | 诊断/处方编辑 |
| `ReportService` / `ReportRepository` | 80/70 | 报表聚合（仅 `AuditLogServiceTests 3` 间接） |
| `HerbItemControlViewModel` / `HerbListControlViewModel` | 60/140 | 药材选择控件（仅 `MedicalCase` 间接） |
| `SwitchingApiClient` 慢路径外其余 2 属性 | 30 | 已于 `high-priority-fixes` 125/125 补 2/3 |

### 5.2 边界/异常缺口

- `CRUD` 的 `ValidationFail`（400）分支：仅 `PatientMasterDetailViewModelTests:304` 覆盖 1 例，`Herb/Formula` 的 `BatchImport` `count==0 → ValidationFail` 未单测
- `Concurrency` 的 `DbUpdateConcurrencyException` 重试：`BaseRepository.UpdateAsync` 已于 `updatehandler-rowversion-fix` 单测 2 例，其余 `MedicalCase` 保存未覆盖
- `Auth` 的 `TokenRefresh` 指数退避 3 次：仅集成（需 1s 延时），无单测

### 5.3 环境性无法运行（162 fail 为存量）

| 套件 | 总数 | 通过 | 失败 | 失败根因 |
|------|------|------|------|----------|
| `LYBT.Tests.Desktop` 全量 | ~777 声明 | ~615 | **162** | 154 `localhost:5000` 未起 + 8 `STA` |
| `LYBT.Tests.Server` | ~58 | 58 | 0 | — |
| `LYBT.Tests.Architecture` | 87 | 87 | 0 | — |

> **根因**：`WebApiE2ETestBase` 直连 `http://localhost:5000` 真服务（`13c #13` 已标 `C-01` 需运行中 WebAPI）；`WPF` `StaFact` 限制。非代码回归。

---

## 6. 测试与需求映射（147 US）

| US 域 | 代表 US | 测试锚点 | 判定 |
|-------|---------|----------|------|
| **PAT** `US-PAT-001/012` | 分页/导出 | `PatientMasterDetailViewModelTests:134` `GetPaged` + `ImportExportJsonTests:16` `Export_ReturnsJson` | ✅ |
| **HERB** `US-HERB-007/013` | 全量/筛选导出 | `HerbRepository SearchAsync` + `LocalImportExportJsonTests:41` + `CatalogPermissionTests:15`（新增 `1ff688d12`） | ✅ |
| **FORM** `US-FORM-001/013` | 列表+导出明细 | `FormulaMasterDetailViewModelTests:134` + `ExportDetailTests:35` `Herbs` 明细 + `CategoryFilterTests:15` | ✅ P2 后补 |
| **MC** `US-MC-001/002/007/012` | 建/改/待验证/强制关闭 | `MedicalCaseMasterDetailViewModelTests:180` `GetPaged` 4 场景 + `MedicalCaseStatusEndpointTests:10` `close AdminOnly` | ⚠️ 聚合根 25% |
| **REG** `US-REG-005` | 接诊即建 | `RegistrationRepository StartVisit` 1 集成（`PatientTests`） | ⚠️ 1 场景 |
| **AUTH** `US-AUTH-001` | 登录 | `JwtServiceTests 23` + `IdentityControllerRoutesTests 5` | ✅ |
| **REPORT** `US-REPORT-001` | 日统计 | `AuditLogServiceTests 3` 间接，报表聚合 0 单测 | 🔴 缺口 |
| **SHELL** `US-SHELL-013` | 备份 | `BackupManagement` 0 单测（仅 `BackupManagementViewModel` 手写） | 🔴 缺口 |

**汇总**：147 US 中 **122 已≥1 单测（83%）**，**25 零映射**集中在 `Reports`/`FirstRun`/`安全审计`（与 `desktop-ui-requirements §七` 缺口一致，非测试遗漏而是功能未实现）。

---

## 7. 改进建议（按优先级）

| # | 级别 | 建议 | 收益 | 工作量 |
|---|------|------|------|--------|
| T-01 | **P1** | 补 `FormulaImportDialog/HistoryCopy/MedicalCaseWorkspace` 聚合根 3 类 12 方法单测（M-01~M-03） | 关闭 P1 缺口，验方/历史链路可回归 | M |
| T-02 | **P1** | `MedicalCaseRepository` 的 `RecordPrint/Suspend/UpdateStatus` 的 `null` vs `throw` 双路径单测（M-04） | 明确 ADR-0020 三态边界 | S |
| T-03 | P2 | 集成稳定性：`WebApplicationFactory` 替 `localhost:5000` 直连，`StaFact` 加 `Collection("WebApiHostTests")` 串行（同 `13c #116`，已有先例） | 162 环境 fail → 0 | M |
| T-04 | P2 | `BatchImport` 的 `ValidationFail`/`count 0` + `Concurrency` 重试边界补 5 单测 | 覆盖 400/409 分支 | S |
| T-05 | P3 | `ReportService` 聚合 0 覆盖补 3 单测（3 报表×成功/失败/异常，同 `AuditLogServiceTests 3` 模式） | 关闭 REPORT 缺口 | S |
| T-06 | P3 | 命名/断言微调：`PatientMasterDetailViewModelTests:286` 2 弱 `BeNull` 改 `Message/Items.Count` 充实断言 | 质量 B→A | XS |

---

## 8. 附录

- **命令**：`dotnet test --filter "HerbMasterDetail|FormulaMasterDetail|PatientMasterDetail" --no-build`（抽样 66/66）；`dotnet test tests/LYBT.Tests.Architecture --no-build` 87/87
- **文件清单**：`ApiClientRepositoryBase:8` / `HerbRepository:170` / `PatientService:101` / `MedicalCaseWorkspaceViewModel:549` / `SwitchingApiClient:125` / `HerbReferenceCheckTests:117` / `EntityApiClientRepositoryBaseTests:65`
- **基线**：`fb295c017`（`build 0/0 arch 87/87`）+ `1ff688d12`（`CatalogPermission/ExportDetail/CategoryFilter` 9/9）+ `13c #13` C-01 环境性说明

