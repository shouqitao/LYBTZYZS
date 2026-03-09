# Sprint 6 - 执行日志

## Session: 2026-03-09

### EXECUTE 阶段 -- Step 1.4 + 1.5

**执行动作**:
1. 确认 PatientRepository 试点 (1.4) 已在上一会话完成: IPatientRepository -> Contracts/Repositories/, PatientRepository (Remote), LocalPatientRepository (Local)
2. 并行启动 5 个子代理重构其余 Repository (1.5):
   - HerbRepository: IHerbApi (Remote) + LocalDbContext (Local), 6 文件 using 更新
   - FormulaRepository: IFormulaApi (Remote) + LocalDbContext (Local), 6 文件 using 更新
   - UserRepository: IUserApi (Remote) + LocalDbContext+BCrypt (Local), 6 文件 using 更新
   - MedicalCaseRepository: IMedicalCaseApi (Remote) + LocalDbContext+聚合根 (Local), 7 文件 using 更新
   - RegistrationRepository: IRegistrationApi (Remote) + LocalDbContext (Local), 2 文件 using 更新
3. 全量编译验证: 0 errors, 0 warnings
4. Desktop 测试: 493 passed
5. Architecture 测试: 78 passed, 1 skipped

### EXECUTE 阶段 -- Step 2.1 (SYNC-D03 核心架构)

**执行动作**:
1. **IConnectionModeProvider**: 添加 `SwitchModeAsync(ConnectionMode, CancellationToken)`, `IsSwitching`, `ModeSwitchResult` record
2. **DataSourceRegistrationExtensions**: 完全重写
   - 两套基础设施始终注册 (本地 LocalDbContext + 远程 HTTP Client 均可用)
   - 6 个 Repository 改为工厂注册 (resolve 时根据 `IConnectionModeProvider.CurrentMode` 选择)
   - `ConnectionModeProvider` 构造函数扩展: 注入 `IModeSwitchValidator`, `IActiveConsultationService`, `INavigationCoordinator`
3. **ConnectionModeProvider**: 实现 `SwitchModeAsync` 五步流程:
   - ActiveConsultation 检查 -> ModeSwitchValidator 验证 -> Region 清理 + 导航历史清除 -> 切换模式 -> 导航首页
4. **LoggingRegistrationExtensions**: `RegisterDataSourceLoggers` 移除 ConnectionMode 参数，始终注册两套 Logger
5. 全量编译: 0 errors; Desktop 472 passed; Architecture 78 passed

**修改文件** (4):
- Contracts/Services/IConnectionModeProvider.cs (+SwitchModeAsync, +IsSwitching, +ModeSwitchResult)
- Shell/Services/ConnectionModeProvider.cs (完全重写，5 步切换逻辑)
- Shell/Extensions/DataSourceRegistrationExtensions.cs (完全重写，工厂注册)
- Shell/Extensions/LoggingRegistrationExtensions.cs (始终注册两套 Logger)
- Shell/Extensions/ServiceCollectionExtensions.cs (移除 connectionMode 参数)

---

### EXECUTE 阶段 -- Step 1.9 + 1.10

**执行动作**:
1. **Step 1.9**: DataSource 文件已在上一会话中删除 (git status 确认)，补创建 `LocalRegistrationMapper.cs` (Mapperly)
2. **Step 1.10**: 架构测试更新:
   - `Desktop_Should_Not_Use_Entity_Classes`: 移除 `"DataSource"` 允许模式
   - `P01_AllDataSources_Must_Have_Both_Remote_And_Local` -> `P01_AllRepositories_Must_Have_Both_Remote_And_Local` (检查 Contracts Repository 接口在模块和 LocalData 中都有实现)
   - `All_Repositories_Should_Be_Registered_In_Modules` -> `All_Repository_Interfaces_Should_Be_In_Contracts` (验证模块不再包含 Repository 接口定义)
3. Integration 测试迁移: 4 个 FlowTests 从 `RemoteXxxDataSource` 迁移到 `XxxRepository`
   - 更新 using、类型名、方法签名 (PagedResult/ToggleStatus 返回类型/SaveAsync 参数)
   - 添加模块项目引用到 Integration 测试 csproj
4. 全量编译: 0 errors
5. Desktop 测试: 472 passed; Architecture 测试: 78 passed, 1 skipped

**修改文件** (7):
- tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs (3 处测试更新)
- tests/LYBT.Tests.Integration/Flows/PatientFlowTests.cs (DataSource -> Repository)
- tests/LYBT.Tests.Integration/Flows/HerbFlowTests.cs (DataSource -> Repository)
- tests/LYBT.Tests.Integration/Flows/FormulaFlowTests.cs (DataSource -> Repository)
- tests/LYBT.Tests.Integration/Flows/MedicalCaseFlowTests.cs (DataSource -> Repository)
- tests/LYBT.Tests.Integration/LYBT.Tests.Integration.csproj (添加 4 个模块引用)
- src/Client/Desktop/Core/LYBT.Desktop.LocalData/Repositories/LocalRegistrationRepository.cs (添加 using)

**新建文件** (1):
- src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/LocalRegistrationMapper.cs

---

### EXECUTE 阶段 -- Step 1.6 + 1.7 + 1.8

**执行动作**:
1. **Step 1.6**: 重写 `DataSourceRegistrationExtensions` - 新增 `RegisterRepositories()` 方法，按 ConnectionMode 选择远程或本地 Repository 实现; 注册 `IConnectionModeProvider` Singleton; 移除所有 DataSource 注册
2. **Step 1.7**: 重构 `MenuManager` - `ConnectionMode` 枚举注入改为 `IConnectionModeProvider` 接口注入; `LoginCoordinator` 和 `HealthCheckCoordinator` 同步更新
3. **Step 1.8**: 重写 `ModeSwitchValidator` - 移除 `IMedicalCaseDataSource` 依赖，改用直接 SQL 查询本地库未完成医案计数 (更简洁、无循环依赖)
4. 移除 4 个模块 (Users/Formula/MedicalCase/Registration) 中的直接 Repository 注册，统一由 Shell 工厂注册
5. 更新 `ModeSwitchValidatorTests` 适配新构造函数
6. 全量编译: 0 errors, 0 warnings
7. Desktop 测试: 488 passed; Architecture 测试: 78 passed, 1 skipped

**修改文件** (9):
- Shell/Extensions/DataSourceRegistrationExtensions.cs (完全重写: DataSource -> Repository 工厂)
- Shell/Extensions/ServiceCollectionExtensions.cs (RegisterDataSources -> RegisterRepositories)
- Shell/Services/MenuManager.cs (ConnectionMode -> IConnectionModeProvider)
- Shell/Services/Login/LoginCoordinator.cs (IConfiguration -> IConnectionModeProvider)
- Shell/Services/HealthCheck/HealthCheckCoordinator.cs (IConfiguration -> IConnectionModeProvider)
- LocalData/Services/ModeSwitchValidator.cs (IMedicalCaseDataSource -> 直接 SQL)
- Modules/Users/UsersModule.cs (移除 RegisterSingleton<IUserRepository>)
- Modules/Formula/FormulaModule.cs (移除 RegisterSingleton<IFormulaRepository>)
- Modules/MedicalCase/MedicalCaseModule.cs (移除 RegisterSingleton<IMedicalCaseRepository>)
- Modules/Registration/RegistrationModule.cs (移除 RegisterSingleton<IRegistrationRepository>)
- Tests/ModeSwitchValidatorTests.cs (适配新构造函数)

---

**新建文件** (10):
- Contracts/Repositories/: IHerbRepository.cs, IFormulaRepository.cs, IUserRepository.cs, IMedicalCaseRepository.cs, IRegistrationRepository.cs
- LocalData/Repositories/: LocalHerbRepository.cs, LocalFormulaRepository.cs, LocalUserRepository.cs, LocalMedicalCaseRepository.cs, LocalRegistrationRepository.cs

**重写文件** (5):
- Herbs/Repositories/HerbRepository.cs (Remote-only)
- Formula/Repositories/FormulaRepository.cs (Remote-only)
- Users/Repositories/UserRepository.cs (Remote-only)
- MedicalCase/Repositories/MedicalCaseRepository.cs (Remote-only)
- Registration/Repositories/RegistrationRepository.cs (Remote-only)

**删除文件** (5):
- Herbs/Interfaces/IHerbRepository.cs
- Formula/Interfaces/IFormulaRepository.cs
- Users/Interfaces/IUserRepository.cs
- MedicalCase/Interfaces/IMedicalCaseRepository.cs
- Registration/Interfaces/IRegistrationRepository.cs

---

### BRAINSTORM + PLAN 阶段 -- COMPLETE

**执行动作**:
1. Planner agent 分析 v2.0 全部 28 个延期功能，评估提前可行性
2. 用户确认 6 项功能纳入 Sprint 6 (含 SYNC-D02 + SYNC-D03 架构重构)
3. Planner agent 生成详细实施计划 (6 Phase, ~9-10 天)
4. Gemini 审核架构设计，确认方案 A+ 合理，补充 6 个关键风险点
5. 三文件覆盖重建

**关键决策**:
- 方案 A+ (Factory + Dual Repository) 确认
- Singleton 依赖审计纳入 Phase 1
- MenuManager 重构纳入 Phase 1
- ActiveConsultation 检查纳入 Phase 2

**计划文件**:
- `.claude/plan/sprint6-datasource-refactor.md` -- 完整实施计划 (含 Gemini SESSION_ID)

---

### EXECUTE 阶段 -- Step 2.2 + 2.3 + 2.4 + 2.5 + 2.6 (SYNC-D03 UI + 测试)

**执行动作**:
1. **Step 2.2**: MainWindowViewModel 添加 `SwitchModeCommand` (RelayCommand, CanExecute 检查 IsSwitchingMode + IsLoggedIn)
   - 注入 `IConnectionModeProvider`，添加 `IsSwitchingMode` 可观察属性 + `ConnectionModeText` 计算属性
   - 订阅 `ModeChanged` 事件，切换后刷新所有模式相关 UI (菜单可见性/IsLocalMode/ConnectionModeText)
   - 切换前显示确认对话框
2. **Step 2.3**: 切换前确认机制 -- ActiveConsultation + ModeSwitchValidator 已覆盖核心阻断 (活跃医案 + 未完成医案 + LocalDB 可用性)
3. **Step 2.4**: SidebarControl 添加模式切换按钮 (Row 5, swap_horiz 图标)
   - 新增 4 个 DependencyProperty: SwitchModeCommand, IsLocalMode, IsSwitchingMode, ConnectionModeText
   - MainWindow.xaml 添加半透明遮罩层 (Panel.ZIndex=100, IsSwitchingMode 绑定)
4. **Step 2.5**: CancellationToken 已在 SwitchModeAsync API 中支持
5. **Step 2.6**: 16 个 ConnectionModeProvider 单元测试 (初始化/成功切换/阻断条件/验证器路由/取消/异常处理)
6. 全量编译: 0 errors, 0 warnings
7. Desktop 测试: 488 passed; Architecture 测试: 78 passed, 1 skipped

**修改文件** (4):
- Shell/ViewModels/MainWindowViewModel.cs (+IConnectionModeProvider 注入, +SwitchModeCommand, +IsSwitchingMode, +ConnectionModeText, +OnConnectionModeChanged)
- Infrastructure/Controls/SidebarControl.xaml.cs (+4 DependencyProperty)
- Infrastructure/Controls/SidebarControl.xaml (+模式切换按钮 Row 5, Grid 行数 7->8)
- Shell/Views/MainWindow.xaml (+模式切换遮罩层, +SidebarControl 新属性绑定)

**新建文件** (1):
- tests/LYBT.Tests.Desktop/PureLogic/Shell/Services/ConnectionModeProviderTests.cs (16 tests)

---

### EXECUTE 阶段 -- Phase 3 (D2 诊所信息配置化)

**执行动作**:
1. **Step 3.1**: 扩展 `ClinicSettingsOptions` 添加 LicenseNumber/Email 字段; 删除重复的 `Infrastructure/Configuration/ClinicSettings.cs` POCO
2. **Step 3.2**: 创建独立 `clinic-settings.json` 配置文件; `ServiceCollectionExtensions` 添加 `AddJsonFile("clinic-settings.json", reloadOnChange: true)`; csproj 添加 CopyToOutput; 从 `appsettings.json` 移除 ClinicSettings 节
3. **Step 3.3**: 重写 `ClinicSettingsService` -- 保持 `IConfiguration` 直接读取 (每次访问都从配置读取，支持热更新); 新增 `SaveSettingsAsync` 方法写入 `clinic-settings.json`; 添加 `ILogger` 依赖
4. **Step 3.4**: 修复打印断链 -- `PrescriptionPrintHandler` 新增 `IClinicSettingsService` 注入; `BuildPrintModel` 方法从配置读取诊所名称/地址/电话/科室填充到打印模型
5. **Step 3.5**: `SystemSettingsViewModel` 添加 6 个诊所属性 + `IClinicSettingsService` 注入; `SystemSettingsView.xaml` 添加"诊所信息"配置区域; Save/Reset 操作同时处理诊所配置
6. Infrastructure csproj 添加 Shared.Configuration 项目引用
7. 更新 `PrescriptionPrintHandlerTests` 适配新构造函数
8. 全量编译: 0 errors; Desktop 504 passed; Architecture 78 passed

**修改文件** (9):
- Shared/Configuration/Options/Client/ClinicSettingsOptions.cs (+LicenseNumber, +Email)
- Infrastructure/Interfaces/IClinicSettingsService.cs (重写: 使用 ClinicSettingsOptions, +SaveSettingsAsync, +LicenseNumber, +Email)
- Infrastructure/Services/ClinicSettingsService.cs (重写: IConfiguration 热读取 + SaveSettingsAsync)
- Infrastructure/LYBT.Desktop.Infrastructure.csproj (+Shared.Configuration 引用)
- Shell/Extensions/ServiceCollectionExtensions.cs (+clinic-settings.json 加载)
- Shell/LYBT.Desktop.Shell.csproj (+clinic-settings.json CopyToOutput)
- Shell/appsettings.json (移除 ClinicSettings 节)
- MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs (+IClinicSettingsService 注入, +诊所信息填充)
- Admin/ViewModels/SystemSettingsViewModel.cs (重写: +诊所配置属性和保存逻辑)
- Admin/Views/SystemSettingsView.xaml (+诊所信息配置区域)
- tests/PrescriptionPrintHandlerTests.cs (+IClinicSettingsService mock)

**新建文件** (1):
- Shell/clinic-settings.json

**删除文件** (1):
- Infrastructure/Configuration/ClinicSettings.cs

---

### EXECUTE 阶段 -- Phase 4 (D1 PDF 处方导出)

**执行动作**:
1. **调研**: 发现 QuestPDF 2025.4.0 已添加、PrescriptionPdfExporter.cs 已实现、ExportAsync 已路由 PDF -- Step 4.1 + 4.2 在上一会话已完成
2. **Step 4.3**: UI 集成
   - `PrescriptionPrintHandler` 添加 `ExportPdfAsync` 方法 (SaveFileDialog + ExportFormat.Pdf 路由)
   - `MedicalCaseCommandsViewModel` 添加 `ExportPdfCommand` (DelegateCommand, CanExecute 复用 CanPrint)
   - `MedicalCaseWorkspaceView.xaml` 添加"导出PDF"按钮 (紧跟"打印处方单"按钮)
3. 全量编译: 0 errors; Desktop 488 passed; Architecture 78 passed

**修改文件** (3):
- MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs (+ExportPdfAsync 方法)
- MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs (+ExportPdfCommand, +ExecuteExportPdf)
- Clinical/Views/MedicalCaseWorkspaceView.xaml (+导出PDF按钮)

---

## 进度汇总

| Phase | 内容 | 状态 |
|-------|------|------|
| 1 | SYNC-D02 DataSource 废除 | **complete** |
| 2 | SYNC-D03 运行时切换 | **complete** |
| 3 | D2 诊所配置化 | **complete** |
| 4 | D1 PDF 导出 | **complete** |
| 5 | C2 照片加密 | **complete** |
| 6 | D3 草稿水印 | **complete** |

---

### EXECUTE 阶段 -- Phase 5 + Phase 6 (2026-03-09 Session 2)

**Phase 5 (C2 照片加密)**: 上一会话已完成接口+实现+集成，本次修复测试 (Moq -> NSubstitute)
- 修复 `DpapiPhotoStorageServiceTests.cs`: `Moq` -> `NSubstitute`，添加 `using System.IO`
- 11 tests passed

**Phase 6 (D3 草稿水印)**:
1. `PrescriptionPrintModel` 添加 `bool IsDraft` 属性
2. 4 个 XAML 打印模板添加草稿水印层:
   - `PrescriptionPrintTemplate.xaml` (A5 首页, FontSize=72)
   - `PrescriptionPrintA4Template.xaml` (A4 首页, FontSize=96)
   - `PrescriptionContinuationTemplate.xaml` (A5 续页, FontSize=72)
   - `PrescriptionContinuationA4Template.xaml` (A4 续页, FontSize=96)
   - 实现: 外层 Grid 包裹, TextBlock 水印 (Panel.ZIndex=100, -35度旋转, #20FF0000 半透明红)
   - 绑定 DataTrigger IsDraft=True 时显示
3. `PrescriptionPdfExporter.Export()` 添加 QuestPDF `page.Foreground()` 水印 (72pt, -35度旋转, #30FF0000)
4. `PrescriptionPrintHandler.BuildPrintModel()` 设置 `IsDraft = CaseStatus != Completed`
5. 全量编译: 0 errors; Desktop 515 passed; Architecture 78 passed, 1 skipped
