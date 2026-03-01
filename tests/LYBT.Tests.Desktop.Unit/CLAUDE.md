# LYBT.Tests.Desktop.Unit

WPF 桌面端核心单元测试项目。覆盖认证、Shell、Infrastructure 服务、LocalData 离线数据源、ViewModel、模块级 ViewModel。

## 项目基本信息

- **目标框架**: net8.0-windows (UseWPF=true)
- **测试框架**: xunit + FluentAssertions + NSubstitute + Xunit.StaFact
- **总测试方法数**: 约 633 个 ([Fact] + [Theory] + [StaFact])

## 目录结构

```
tests/LYBT.Tests.Desktop.Unit/
├── Admin/ViewModels/AdminHomeViewModelTests.cs                    # (10)
├── Auth/ViewModels/LoginViewModelTests.cs                         # (10)
├── Clinical/ViewModels/ClinicalHomeViewModelTests.cs              # (10)
├── Formula/
│   ├── Integration/FormulaEditRegressionTests.cs                  # (9)
│   └── ViewModels/FormulaHerbItemViewModelTests.cs                # (15)
├── Foundation/Security/
│   ├── AuthenticationServiceTests.cs                              # (14)
│   ├── AuthenticationStateMachineTests.cs                         # (33)
│   ├── CredentialVaultTests.cs                                    # (22)
│   ├── LocalTokenValidatorTests.cs                                # (8)
│   ├── LogoutServiceTests.cs                                      # (20)
│   └── TokenManagerTests.cs                                       # (14)
├── Herbs/ViewModels/Base/HerbItemViewModelBaseTests.cs            # (18)
├── Infrastructure/
│   ├── Controls/UnifiedManagementTableTests.cs                    # (4, [StaFact])
│   ├── Events/PatientEventsTests.cs                               # (6)
│   ├── Models/Options/ (DisplayOptionsTests, PaginationOptionsTests)  # (6)
│   ├── Models/State/ (LoadingState, PaginationState, SearchState)     # (15)
│   ├── Services/ (6 个服务测试)                                      # (95)
│   ├── Views/BaseMasterDataListViewTests.cs                       # (4, [StaFact])
│   └── WpfTestInitializer.cs                                     # (非测试类)
├── LocalData/
│   ├── DataSources/ (Formula, Herb, Patient)                      # (53)
│   ├── Services/LocalAuthServiceTests.cs                          # (17)
│   └── TestFixtures/LocalDbContextFixture.cs                      # (非测试类)
├── MedicalCase/
│   ├── Components/MedicalCaseEditModeStateMachineTests.cs         # (37)
│   ├── Integration/PrescriptionEditFlowTests.cs                   # (7)
│   └── ViewModels/ (价格计算, 剂量验证)                              # (30)
├── Patients/ (Models, Repositories, Services, ViewModels)          # (42)
├── Shell/ (LoginCoordinator, SessionLifecycle, StartupPipeline)    # (56)
└── Users/ (Repositories, Services, ViewModels)                     # (44)
```

## 测试模式

### WPF 线程隔离
- `[StaFact]` (Xunit.StaFact): STA 线程运行，用于控件测试
- `WpfTestInitializer.Initialize()`: 静态构造函数初始化 Application 资源字典

### LocalData 数据库夹具
`LocalDbContextFixture` (IClassFixture) 为每个测试创建独立 SQLite InMemory 连接

### 命名约定
`{Method}_{Scenario}_{ExpectedBehavior}` (如 `GetById_PatientExists_ReturnsPatient`)

### Mock 策略
NSubstitute Mock 全部外部依赖: API 接口 (IAuthApi/IPatientApi)、DataSource、Repository、Prism 接口 (IEventAggregator/IRegionManager/IDialogService)

## 占位符测试 (无实际覆盖)

以下文件仅含 `Placeholder_Test_ShouldPass()`，被测 ViewModel 无真实测试:
- PatientListViewModelTests
- ShellViewModelTests
- UserListViewModelTests

## 覆盖空白

- Desktop.Sync 模块无对应测试
- Herbs 模块 ViewModel (非 Base 层) 无专项测试
- MedicalCaseFormViewModel 因依赖复杂度暂时简化

## 已删除文件

- Foundation/Security/TokenEventsTests.cs (对应 TokenEvents.cs 已删除)

---
最后更新: 2026-03-01
