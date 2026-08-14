# Desktop 测试审计报告

> **日期**: 2026-08-14  
> **范围**: `tests/LYBT.Tests.Desktop/` — 92 个测试文件，820 个测试方法  
> **目的**: 分类测试价值，识别可清理/可跳过的测试，指导后续重构

---

## 1. 总览

| 指标 | 数值 |
|------|------|
| 测试文件总数 | 92 |
| `[Fact]` 方法 | 793 |
| `[Theory]` 方法 | 27 |
| **测试总数** | **820** |
| 已标记 `Skip` | 18 |
| Unit 测试文件 | 58 |
| Integration 测试文件 | 34 |
| `_Infrastructure` 辅助文件 | 12 |

## 2. 按业务模块分类

### 2.1 模块统计表

| 模块 | 文件数 | Fact | Theory | 合计 | Skip | 测试性质 |
|------|--------|------|--------|------|------|----------|
| **Auth** | 1 | 21 | 0 | 21 | 0 | Unit: LoginViewModel 行为 |
| **Clinical** | 7 | 72 | 9 | 81 | 0 | Unit: 状态机/卡读器/患者选择 |
| **ErrorHandling** | 3 | 10 | 0 | 10 | 2 | Unit: 异常处理/通知映射 |
| **Formula** | 1 | 9 | 0 | 9 | 0 | Unit: MasterDetail VM |
| **Foundation** | 15 | 126 | 2 | 128 | 0 | Unit+Integration: 认证/连接/启动 |
| **Herbs** | 1 | 6 | 0 | 6 | 0 | Unit: MasterDetail VM |
| **Infrastructure** | 3 | 32 | 0 | 32 | 0 | Unit: Breadcrumb/Toast/WPF 控件 |
| **LocalWebAPI** | 9 | 52 | 0 | 52 | 0 | Integration: 控制器测试(LocalDB) |
| **Logging** | 2 | 28 | 0 | 28 | 0 | Unit: 日志级别/敏感数据脱敏 |
| **MedicalCase** | 12 | 149 | 2 | 151 | 0 | Unit: 医案/处方/工作流 |
| **Models** | 1 | 1 | 1 | 2 | 0 | Unit: 导航项 |
| **Modules (E2E)** | 12 | 105 | 0 | 105 | 11 | Integration: 模块 CRUD+E2E |
| **Patients** | 1 | 30 | 0 | 30 | 3 | Unit: MasterDetail VM |
| **RefitContract** | 1 | 12 | 0 | 12 | 0 | Unit: Refit 接口契约 |
| **Registration** | 4 | 15 | 12 | 27 | 0 | Unit: MasterDetail/队列/状态 |
| **Roles** | 3 | 23 | 0 | 23 | 0 | Integration: 权限边界(E2E) |
| **Shell** | 9 | 42 | 1 | 43 | 0 | Unit: 启动管线/配置/导出 |
| **Sysadmin** | 1 | 6 | 0 | 6 | 0 | Unit: 配置中心 VM |
| **Users** | 1 | 15 | 0 | 15 | 0 | Unit: MasterDetail VM |
| **ViewModels** | 1 | 21 | 0 | 21 | 0 | Unit: 导航基类 |
| **Workflows** | 3 | 10 | 0 | 10 | 0 | Integration: 完整业务流程(E2E) |
| **FrameworkVerification** | 1 | 8 | 0 | 8 | 0 | Integration: 测试框架验证 |

### 2.2 按测试层级分类

| 层级 | 描述 | 文件数 | 测试数 | 依赖 |
|------|------|--------|--------|------|
| **Unit (Pure)** | 纯 ViewModel/Service 逻辑，无 DB/WPF 依赖 | ~28 | ~350 | NSubstitute mocks |
| **Unit (WPF)** | 需 WPF 环境但无 DB 的 VM 测试 | ~13 | ~250 | WpfTestHelper + NSubstitute |
| **Integration (LocalWebAPI)** | 嵌入式 Kestrel + LocalDB 控制器测试 | 9 | 52 | LocalDB + WebApplicationFactory |
| **Integration (E2E)** | 需运行中 localhost:5000 服务 | ~24 | ~160 | 活跃 WebAPI 服务 |
| **Infrastructure** | 测试框架验证 | 1 | 8 | LocalDB |

## 3. 已跳过测试分析

### 3.1 当前 Skip 列表

| 文件 | 测试名 | Skip 原因 | 建议 |
|------|--------|-----------|------|
| `FormulaTests.cs` | Clone 相关 (2) | Clone endpoint not yet implemented | **保留 Skip** — 功能待实现 |
| `FormulaTests.cs` | Export/Import (2) | Export/Import endpoints not yet implemented | **保留 Skip** — 功能待实现 |
| `MedicalCaseTests.cs` | 多活动医案 | Cannot create multiple active cases — business rule | **删除** — 违反业务规则的测试 |
| `PatientTests.cs` | Export/Import | Export/Import endpoints not yet implemented | **保留 Skip** — 功能待实现 |
| `PatientMasterDetailViewModelTests.cs` | Restore (3) | RestoreAsync removed | **删除** — 方法已删除 |
| `NotificationTypeMappingTests.cs` | 映射测试 (2) | ExceptionSeverityMapper class does not exist | **删除** — 死代码引用 |

### 3.2 Skip 总结

- **保留 Skip**: 5 个（功能待实现，有明确计划）
- **建议删除**: 8 个（引用已删除的 API/类，无恢复可能）
- **建议保留**: 5 个（Export/Import/Clone 待实现）

## 4. 价值评估与清理建议

### 4.1 高价值测试（保留）

**业务逻辑核心 — 无需任何变更**

| 模块 | 文件 | 测试数 | 理由 |
|------|------|--------|------|
| Auth | `LoginViewModelTests.cs` | 21 | 登录核心流程，VM 行为验证 |
| Clinical | `EditModeStateMachineTests.cs` | 37 | 编辑模式状态机全覆盖，纯逻辑 |
| Clinical | `CardReaderPureTests.cs` | 11 | 读卡器数据解析，纯计算逻辑 |
| Foundation | `AuthenticationStateMachineTests.cs` | 33 | 认证状态机全覆盖 |
| Foundation | `CredentialVaultTests.cs` | 15 | 凭据存储安全逻辑 |
| Foundation | `LogoutServiceTests.cs` | 17 | 登出流程 |
| MedicalCase | `ConsultationItemTests.cs` | 21 | 诊断项目逻辑 |
| MedicalCase | `PrescriptionItemTests.cs` | 24 | 处方项目逻辑 |
| MedicalCase | `WorkflowStepIndicatorTests.cs` | 13 | 工作流步骤逻辑 |
| Registration | `RegistrationStatusTransitionTests.cs` | 8 | 状态转换规则（Theory 覆盖） |
| Registration | `RegistrationQueueLogicTests.cs` | 7 | 队列逻辑 |
| Shell | `StartupPipelineTests.cs` | 13 | 启动管线状态管理 |
| ViewModels | `NavigableViewModelBaseTests.cs` | 21 | 基类导航行为 |
| Logging | `SensitiveDataMaskerTests.cs` | 15 | 敏感数据脱敏 |
| Logging | `LoggingLevelManagerTests.cs` | 13 | 日志级别管理 |
| ErrorHandling | `ErrorTraceCodeTests.cs` | 4 | 错误追踪码逻辑 |
| RefitContract | `RefitClientContractTests.cs` | 12 | Refit 接口契约验证 |

**合计**: ~288 个高价值测试

### 4.2 中等价值测试（保留，但标记 WPF 依赖）

**ViewModel 行为测试 — 需要 WPF 环境但验证业务逻辑**

| 模块 | 文件 | 测试数 | 理由 |
|------|------|--------|------|
| MedicalCase | `MedicalCaseMasterDetailViewModelTests.cs` | 24 | 医案管理 VM 行为 |
| MedicalCase | `MedicalCaseWorkspaceViewModelTests.cs` | 11 | 工作区 VM 状态管理 |
| MedicalCase | `ConsultationEditorViewModelTests.cs` | 8 | 诊断编辑 VM |
| MedicalCase | `PrescriptionEditorViewModelTests.cs` | 12 | 处方编辑 VM |
| Patients | `PatientMasterDetailViewModelTests.cs` | 27 (扣 3 Skip) | 患者管理 VM |
| Registration | `RegistrationMasterDetailViewModelTests.cs` | 6 | 挂号管理 VM |
| Formula | `FormulaMasterDetailViewModelTests.cs` | 9 | 验方管理 VM |
| Herbs | `HerbMasterDetailViewModelTests.cs` | 6 | 药材管理 VM |
| Users | `UserMasterDetailViewModelTests.cs` | 15 | 用户管理 VM |
| Sysadmin | `ConfigurationCenterViewModelTests.cs` | 6 | 配置中心 VM |
| Clinical | `PatientSelectionViewModelTests.cs` | 11 | 患者选择 VM |
| Clinical | `PendingQueueViewModelTests.cs` | 5 | 待诊队列 VM |

**合计**: ~140 个中等价值测试

### 4.3 高价值 Integration 测试（保留）

**E2E 业务流程验证 — 需运行中服务**

| 模块 | 文件 | 测试数 | 理由 |
|------|------|--------|------|
| LocalWebAPI | 各控制器测试 (9 文件) | 52 | 嵌入式 Kestrel 控制器测试 |
| Roles | `PermissionBoundaryTests.cs` | 8 | 权限边界验证 |
| Roles | `RolePermissionBoundaryTests.cs` | 12 | 角色权限矩阵 |
| Workflows | `PatientVisitWorkflowTests.cs` | 3 | 完整就诊流程 |
| Workflows | `HerbFormulaWorkflowTests.cs` | 3 | 药材→验方流程 |
| Workflows | `DataIntegrityTests.cs` | 4 | 数据完整性验证 |
| Modules | 各模块正向测试 (6 文件) | 76 (扣 Skip) | 模块 CRUD 验证 |

**合计**: ~158 个高价值 Integration 测试

### 4.4 低价值/可清理测试

#### A. 纯 UI 绑定/控件测试（建议删除或标记跳过）

| 文件 | 测试数 | 理由 | 建议 |
|------|--------|------|------|
| `BreadcrumbBarTests.cs` | 15 | WPF 控件属性测试，无业务逻辑 | **删除** — 控件行为由框架保证 |
| `ToastServiceTests.cs` | 13 | Toast 通知调用测试，仅验证"不抛异常" | **删除** — 测试断言太弱（仅 `exception.Should().BeNull()`） |
| `WorkflowStepIndicatorTests.cs` | 13 | 工作流步骤指示器 UI 状态 | **保留** — 验证业务状态转换逻辑 |

**注意**: `ToastServiceTests` 的测试质量极低——所有测试仅验证 `Record.Exception(() => ...).Should().BeNull()`，不验证任何实际行为。这类测试给人虚假的安全感。

#### B. 通知/错误处理映射测试（建议删除）

| 文件 | 测试数 | 理由 | 建议 |
|------|--------|------|------|
| `NotificationTypeMappingTests.cs` | 4 (2 Skip) | 引用不存在的 `ExceptionSeverityMapper` 类 | **删除** — 死代码引用 |
| `DesktopExceptionHandlerTests.cs` | 2 | 异常处理器测试 | **保留** — 验证错误处理逻辑 |

#### C. 框架验证测试（建议标记为可选）

| 文件 | 测试数 | 理由 | 建议 |
|------|--------|------|------|
| `FrameworkVerificationTests.cs` | 8 | 验证 LocalDB 连接/WPF 初始化等测试基础设施 | **标记为 `[Trait("Category","Infrastructure")]`** — 开发时运行，CI 可选 |

### 4.5 E2E 测试中可清理的 Skip 测试

| 文件 | 测试数 | 理由 | 建议 |
|------|--------|------|------|
| `MedicalCaseTests.cs` | 1 (Skip) | 违反业务规则的测试（多活动医案） | **删除整个测试方法** |
| `PatientMasterDetailViewModelTests.cs` | 3 (Skip) | 引用已删除的 `RestoreAsync` | **删除整个测试方法** |
| `NotificationTypeMappingTests.cs` | 2 (Skip) | 引用不存在的类 | **删除整个测试文件** |

## 5. 文件组织评估

### 5.1 当前结构

```
tests/LYBT.Tests.Desktop/
├── _Infrastructure/           # 测试基础设施 (12 文件)
│   ├── Assertions/            # JWT 断言
│   ├── Builders/              # 测试数据构建器
│   ├── TestDoubles/           # 测试替身 (TestUiThreadDispatcher)
│   ├── LocalDbContext.cs      # 测试用 DbContext
│   ├── TestDataFactory.cs     # 测试数据工厂
│   ├── UserJourneyFixture.cs  # DB fixture
│   ├── UserJourneyTestBase.cs # VM 测试基类
│   └── WpfTestHelper.cs       # WPF 初始化
├── Unit/                      # 单元测试 (58 文件)
│   ├── Auth/                  # 认证
│   ├── Clinical/              # 临床/卡读器
│   ├── ErrorHandling/         # 错误处理
│   ├── Formula/               # 验方
│   ├── Foundation/            # 基础设施/认证状态机
│   ├── Herbs/                 # 药材
│   ├── Infrastructure/        # WPF 控件
│   ├── Logging/               # 日志
│   ├── MedicalCase/           # 医案/处方
│   ├── Models/                # 导航模型
│   ├── Patients/              # 患者
│   ├── Registration/          # 挂号
│   ├── Shell/                 # 启动/配置
│   ├── Sysadmin/              # 运维配置
│   ├── Users/                 # 用户管理
│   ├── ViewModels/            # VM 基类
│   └── RefitClientContractTests.cs  # 接口契约
├── Integration/               # 集成测试 (34 文件)
│   ├── Foundation/            # 认证集成
│   ├── Infrastructure/        # E2E 基类
│   ├── LocalWebAPI/           # 控制器测试
│   ├── Modules/               # 模块 CRUD
│   ├── Roles/                 # 权限边界
│   └── Workflows/             # 完整业务流程
└── FrameworkVerificationTests.cs  # 框架验证
```

### 5.2 组织评估

**优点**:
- Unit/Integration 分层清晰
- 按业务模块组织子目录
- `_Infrastructure` 集中管理测试辅助代码
- 测试基类继承关系合理（UserJourneyTestBase → WpfTestHelper）

**问题**:
- `FrameworkVerificationTests.cs` 放在 Integration 根目录，不在子模块中
- 部分 Unit 测试（如 `BreadcrumbBarTests`, `ToastServiceTests`）测试 WPF 控件行为，放在 Unit 下但实际是控件测试
- `MedicalCase` 模块测试文件过多（12 文件/151 测试），可考虑拆分为子目录

## 6. 清理优先级

### P0 — 立即删除（无恢复价值）

| 操作 | 文件 | 测试数 | 理由 |
|------|------|--------|------|
| 删除测试方法 | `PatientMasterDetailViewModelTests.cs` 中 3 个 Restore 测试 | 3 | 引用已删除的 API |
| 删除整个文件 | `NotificationTypeMappingTests.cs` | 4 | 引用不存在的类（2 已 Skip + 2 可能编译失败） |
| 删除测试方法 | `MedicalCaseTests.cs` 中多活动医案测试 | 1 | 违反业务规则 |

**预计减少**: 8 个测试

### P1 — 建议删除（低价值/虚假安全）

| 操作 | 文件 | 测试数 | 理由 |
|------|------|--------|------|
| 删除整个文件 | `BreadcrumbBarTests.cs` | 15 | WPF 控件属性测试，无业务逻辑 |
| 删除整个文件 | `ToastServiceTests.cs` | 13 | 测试断言太弱（仅验证不抛异常） |

**预计减少**: 28 个测试

### P2 — 建议标记跳过（STA/WPF 依赖问题）

以下 Unit 测试继承 `UserJourneyTestBase`，依赖 `WpfTestHelper.InitializeWpf()`，在 headless/CI 环境可能因 STA 线程问题失败：

| 文件 | 测试数 | STA 风险 |
|------|--------|----------|
| `MedicalCaseMasterDetailViewModelTests.cs` | 24 | 中 — 用 Dispatcher |
| `PatientMasterDetailViewModelTests.cs` | 27 | 中 — 用 Dispatcher |
| `RegistrationMasterDetailViewModelTests.cs` | 6 | 中 — 用 Dispatcher |
| `BreadcrumbBarTests.cs` | 15 | 高 — 直接操作 WPF 控件 |
| `ToastServiceTests.cs` | 13 | 高 — 直接操作 WPF 服务 |
| `WorkflowStepIndicatorTests.cs` | 13 | 中 — WPF 环境依赖 |

**建议**: 这些测试在本地 STA 线程环境（如 VS Test Explorer）可正常运行，CI 中可按需过滤。不建议全部跳过——它们验证的是真实 VM 行为。

### P3 — 保留但优化

| 文件 | 测试数 | 优化建议 |
|------|--------|----------|
| `FrameworkVerificationTests.cs` | 8 | 标记 `[Trait("Category","Infrastructure")]`，CI 可选运行 |
| `NavigableViewModelBaseTests.cs` | 21 | 确认无 WPF 依赖后保留（当前继承 UserJourneyTestBase 但实际不需 WPF） |

## 7. 清理影响预估

| 操作 | 测试数变化 | 剩余测试 |
|------|-----------|----------|
| 当前总数 | 820 | — |
| P0 删除 | -8 | 812 |
| P1 删除 | -28 | 784 |
| **净减少** | **-36** | **784** |

**清理后**:
- 高价值 Unit 测试: ~288（保留）
- 中等价值 Unit 测试（WPF 依赖）: ~140（保留）
- 高价值 Integration 测试: ~158（保留）
- 低价值/可选测试: ~198（保留但可按需运行）

## 8. 推荐操作清单

### 立即执行（本次审计后）

1. **删除** `NotificationTypeMappingTests.cs` 整个文件
2. **删除** `PatientMasterDetailViewModelTests.cs` 中 3 个 Restore 测试方法
3. **删除** `MedicalCaseTests.cs` 中多活动医案测试方法
4. **删除** `BreadcrumbBarTests.cs` 整个文件
5. **删除** `ToastServiceTests.cs` 整个文件

### 后续重构时（用户明确需要重构前端测试）

1. 将 WPF 依赖的 Unit 测试迁移至不依赖 `WpfTestHelper` 的模式
2. E2E 测试考虑用 `WebApplicationFactory<T>` 替代运行中服务
3. `FrameworkVerificationTests` 可合并到测试基础设施中
4. MedicalCase 模块测试可按子功能拆分目录

### CI 配置建议

```bash
# 运行所有测试（本地开发）
dotnet test tests/LYBT.Tests.Desktop/

# 仅运行 Unit 测试（CI 快速反馈）
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Unit"

# 跳过 STA 问题测试（CI 可选）
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Unit&Category!=STA"

# 仅运行 E2E（需运行中服务）
dotnet test tests/LYBT.Tests.Desktop/ --filter "Category=E2E"
```

---

## 附录 A: 完整文件清单

### Unit 测试（58 文件，~637 测试）

| 文件 | Fact | Theory | Skip | WPF 依赖 | 价值 |
|------|------|--------|------|-----------|------|
| Auth/LoginViewModelTests.cs | 21 | 0 | 0 | ✅ | 🔴 高 |
| Clinical/CardReaderDataFillTests.cs | 8 | 1 | 0 | ❌ | 🔴 高 |
| Clinical/CardReaderOptionsConfigurationTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Clinical/CardReaderPureTests.cs | 9 | 2 | 0 | ❌ | 🔴 高 |
| Clinical/EditModeStateMachineTests.cs | 31 | 6 | 0 | ❌ | 🔴 高 |
| Clinical/PatientSelectionViewModelTests.cs | 11 | 0 | 0 | ✅ | 🟡 中 |
| Clinical/PatientSelectionWorkspaceContextTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Clinical/PendingQueueViewModelTests.cs | 5 | 0 | 0 | ✅ | 🟡 中 |
| ErrorHandling/DesktopExceptionHandlerTests.cs | 2 | 0 | 0 | ❌ | 🔴 高 |
| ErrorHandling/ErrorTraceCodeTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| ErrorHandling/NotificationTypeMappingTests.cs | 4 | 0 | 2 | ❌ | ⚫ 删除 |
| Formula/FormulaMasterDetailViewModelTests.cs | 9 | 0 | 0 | ✅ | 🟡 中 |
| Foundation/AuthEventPublishingTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/AuthenticationStateMachineTests.cs | 33 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/ConnectionSettingsServiceTests.cs | 9 | 2 | 0 | ❌ | 🔴 高 |
| Foundation/CredentialVaultTests.cs | 15 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/DesktopUpdateServiceTests.cs | 3 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/HttpClientApiClientEnvelopeTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/LocalTokenValidatorTests.cs | 6 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/LogoutServiceTests.cs | 17 | 0 | 0 | ❌ | 🔴 高 |
| Foundation/SwitchingApiClientTests.cs | 7 | 0 | 0 | ❌ | 🔴 高 |
| Herbs/HerbMasterDetailViewModelTests.cs | 6 | 0 | 0 | ✅ | 🟡 中 |
| Infrastructure/BreadcrumbBarTests.cs | 15 | 0 | 0 | ✅ | ⚫ 删除 |
| Infrastructure/CardReaderDiagnosticsServiceTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Infrastructure/ToastServiceTests.cs | 13 | 0 | 0 | ✅ | ⚫ 删除 |
| Logging/LoggingLevelManagerTests.cs | 13 | 0 | 0 | ❌ | 🔴 高 |
| Logging/SensitiveDataMaskerTests.cs | 15 | 0 | 0 | ❌ | 🔴 高 |
| MedicalCase/ConsultationEditorPureTests.cs | 6 | 0 | 0 | ❌ | 🔴 高 |
| MedicalCase/ConsultationEditorViewModelTests.cs | 8 | 0 | 0 | ✅ | 🟡 中 |
| MedicalCase/ConsultationItemTests.cs | 21 | 0 | 0 | ✅ | 🔴 高 |
| MedicalCase/MedicalCaseMasterDetailViewModelTests.cs | 24 | 0 | 0 | ✅ | 🟡 中 |
| MedicalCase/MedicalCaseWorkspaceViewModelTests.cs | 11 | 0 | 0 | ❌ | 🟡 中 |
| MedicalCase/PrescriptionEditorPureTests.cs | 10 | 0 | 0 | ❌ | 🔴 高 |
| MedicalCase/PrescriptionEditorViewModelTests.cs | 12 | 0 | 0 | ✅ | 🟡 中 |
| MedicalCase/PrescriptionImportExtensionsTests.cs | 5 | 0 | 0 | ❌ | 🔴 高 |
| MedicalCase/PrescriptionItemTests.cs | 24 | 0 | 0 | ✅ | 🔴 高 |
| MedicalCase/PrescriptionPrintHandlerTests.cs | 5 | 0 | 0 | ❌ | 🔴 高 |
| MedicalCase/WorkflowStepIndicatorTests.cs | 13 | 0 | 0 | ✅ | 🟡 中 |
| MedicalCase/WorkspaceStateTests.cs | 10 | 2 | 0 | ❌ | 🔴 高 |
| Models/NavigationItemTests.cs | 1 | 1 | 0 | ❌ | 🔴 高 |
| Patients/PatientMasterDetailViewModelTests.cs | 30 | 0 | 3 | ✅ | 🟡 中 |
| RefitClientContractTests.cs | 12 | 0 | 0 | ❌ | 🔴 高 |
| Registration/RegistrationMasterDetailViewModelTests.cs | 6 | 0 | 0 | ✅ | 🟡 中 |
| Registration/RegistrationQueueLogicTests.cs | 4 | 3 | 0 | ❌ | 🔴 高 |
| Registration/RegistrationSourceTests.cs | 2 | 4 | 0 | ❌ | 🔴 高 |
| Registration/RegistrationStatusTransitionTests.cs | 3 | 5 | 0 | ❌ | 🔴 高 |
| Shell/DpapiPhotoStorageServiceTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Shell/LocalConfigurationControllerPermissionTests.cs | 2 | 0 | 0 | ❌ | 🔴 高 |
| Shell/LocalDeployControllerPermissionTests.cs | 1 | 0 | 0 | ❌ | 🔴 高 |
| Shell/LocalImportExportJsonTests.cs | 4 | 0 | 0 | ❌ | 🔴 高 |
| Shell/LocalMedicalCaseStatusEndpointTests.cs | 2 | 0 | 0 | ❌ | 🔴 高 |
| Shell/LocalOwnershipCheckLayeringTests.cs | 1 | 1 | 0 | ❌ | 🔴 高 |
| Shell/LocalReportsControllerLayeringTests.cs | 1 | 0 | 0 | ❌ | 🔴 高 |
| Shell/StartupPipelineTests.cs | 13 | 0 | 0 | ❌ | 🔴 高 |
| Shell/StartupStepsTests.cs | 14 | 0 | 0 | ❌ | 🔴 高 |
| Sysadmin/ConfigurationCenterViewModelTests.cs | 6 | 0 | 0 | ❌ | 🟡 中 |
| Users/UserMasterDetailViewModelTests.cs | 15 | 0 | 0 | ✅ | 🟡 中 |
| ViewModels/NavigableViewModelBaseTests.cs | 21 | 0 | 0 | ✅ | 🔴 高 |

### Integration 测试（34 文件，~183 测试）

| 文件 | Fact | Theory | Skip | 依赖 | 价值 |
|------|------|--------|------|------|------|
| Foundation/AuthNegativeTests.cs | 5 | 0 | 0 | E2E | 🔴 高 |
| Foundation/AuthTests.cs | 7 | 0 | 0 | E2E | 🔴 高 |
| Foundation/AuthenticationIntegrationTests.cs | 4 | 0 | 0 | E2E | 🔴 高 |
| Foundation/DiagnosticsTests.cs | 4 | 0 | 0 | E2E | 🔴 高 |
| Foundation/HealthCheckTests.cs | 3 | 0 | 0 | E2E | 🔴 高 |
| Foundation/TokenRefreshHandlerIntegrationTests.cs | 5 | 0 | 0 | E2E | 🔴 高 |
| FrameworkVerificationTests.cs | 8 | 0 | 0 | LocalDB | 🟡 可选 |
| LocalWebAPI/AuthControllerTests.cs | 8 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/FormulasControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/HealthControllerTests.cs | 3 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/HerbsControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/LocalWebApiDbContextTests.cs | 5 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/MedicalCasesControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/PatientsControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/RegistrationsControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| LocalWebAPI/UsersControllerTests.cs | 6 | 0 | 0 | LocalDB | 🔴 高 |
| Modules/FormulaNegativeTests.cs | 4 | 0 | 0 | E2E | 🔴 高 |
| Modules/FormulaTests.cs | 13 | 0 | 5 | E2E | 🔴 高 |
| Modules/HerbNegativeTests.cs | 5 | 0 | 0 | E2E | 🔴 高 |
| Modules/HerbTests.cs | 12 | 0 | 2 | E2E | 🔴 高 |
| Modules/MedicalCaseNegativeTests.cs | 5 | 0 | 0 | E2E | 🔴 高 |
| Modules/MedicalCaseTests.cs | 18 | 0 | 1 | E2E | 🔴 高 |
| Modules/PatientNegativeTests.cs | 5 | 0 | 0 | E2E | 🔴 高 |
| Modules/PatientTests.cs | 10 | 0 | 3 | E2E | 🔴 高 |
| Modules/RegistrationNegativeTests.cs | 4 | 0 | 0 | E2E | 🔴 高 |
| Modules/RegistrationTests.cs | 11 | 0 | 0 | E2E | 🔴 高 |
| Modules/UserNegativeTests.cs | 6 | 0 | 0 | E2E | 🔴 高 |
| Modules/UserTests.cs | 12 | 0 | 2 | E2E | 🔴 高 |
| Roles/PermissionBoundaryTests.cs | 8 | 0 | 0 | E2E | 🔴 高 |
| Roles/RolePermissionBoundaryTests.cs | 12 | 0 | 0 | E2E | 🔴 高 |
| Roles/WorkflowIntegrationTests.cs | 3 | 0 | 0 | E2E | 🔴 高 |
| Workflows/DataIntegrityTests.cs | 4 | 0 | 0 | E2E | 🔴 高 |
| Workflows/HerbFormulaWorkflowTests.cs | 3 | 0 | 0 | E2E | 🔴 高 |
| Workflows/PatientVisitWorkflowTests.cs | 3 | 0 | 0 | E2E | 🔴 高 |

---

**审计结论**: Desktop 测试项目整体质量良好，业务逻辑覆盖充分。主要问题集中在：(1) 少量引用已删除 API 的死测试；(2) 少量纯 UI 控件测试价值低；(3) WPF 依赖导致 CI 环境兼容性问题。建议执行 P0+P1 清理（减少 36 个测试），剩余测试在前端重构时按需优化。
