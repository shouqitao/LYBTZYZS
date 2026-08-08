# 模块级结构审计报告（Mimo Code 独立分析）

> 独立分析者 #2｜对应任务书：`structure-audit-module-level-task-spec-2026-08-08.md`（阶段 2 扩展梳理）
> 技术总监将与本报告交叉验证；本报告只读，未修改任何代码，未 commit。

## 0. 方法说明

| 项 | 说明 |
|---|---|
| 时间点 | 2026-08-08；HEAD=`6285d3202`（任务书 docs 提交） |
| 范围 | Desktop 7 模块 + Server 8 模块 + LocalWebAPI + Shell/Core 层 + F 类遗留 8 项 + Tools 4 项 + 测试 3 项目 + B 类未做功能前置条件 |
| 工具 | serena 符号级（find_symbol/find_referencing_symbols）、codebase-memory 图谱（`LYBTZYZS`，总节点 28122）、rg 初筛、csproj 依赖图 |
| 执行 | 8 个审计子代理（general）并行 + 16 个专题探索子代理（explore）；关键发现主代理二次抽查验证（RoleDefinitionBase bug、PatientRepository:145 内嵌 Mapper、MedicalCaseStartCoordinator:75 假实现、FeatureToggle 开关数等全部复证通过） |
| 与 A-16 衔接 | A-18（P1-1~P1-7）/A-19/A-17 已修项标注「已修」，不重复审计；A-16 已报项（LocalDbContext 休眠、AsyncLocalCorrelationIdProvider 已删等）仅确认状态 |
| 局限 | 未运行测试/应用，功能级结论基于静态证据链；部分探索子代理无 serena 环境，以全仓 grep 引用计数等价替代（与 serena 交叉结果一致） |

**已修项基线（不重复审计）**：A-18 P1-1/P1-2 契约双套统一（Api internal 化 + Admin 3 VM 换注入）、P1-3 CorrelationId 收敛（AsyncLocal 已删）、P1-4 Server 4 模块手写 Mapper 改 Mapperly（Formula/Herbs/Patients/Users）、P1-5 DP07 模块边界守卫（Registration→MedicalCase 已解除）、P1-6 本地配置落盘（JsonFileConfigurationStore）、P1-7 文档重写（08-shared/03-server/05-dual-mode）；A-19 Auto 模式移除；A-17 本地 CRUD override 补全。

---

## A. Desktop 7 模块内部（重点）

> 共性基线：文件均 <600 行（全仓最大 512 行为 Designer 生成物）；V2 组合模式 + MasterDetailViewModelBase 骨架统一；CommunityToolkit [ObservableProperty]/[RelayCommand] 一致。**核心新发现：A-18 P1-4 只修了 Server 侧，Desktop 侧 Mapperly「接入统一」未落地——Users/Herbs/Formula 的 Mapper 文件零引用，DTO↔Model 映射全部手写且每模块重复 2-3 处。**

### LYBT.Desktop.Auth

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | VM→Contracts 服务（ILoginCoordinator `Shell/Services/Login/LoginCoordinator.cs:20`），无 Repository（认证跨 Shell 合理），无越层 | 无 |
| 职责单一 | 5/5 | 最大文件 266 行（ConnectionStatusViewModel），5 个 VM 各司其职 | 无 |
| Repository | N/A | 无数据访问层（`AuthenticationModule.cs:30` 注释确认服务由 Shell 统一注册） | — |
| Mapper | N/A | 无 DTO 映射需求（仅本地枚举 `Models/ConnectionTestStatus.cs`） | — |
| ViewModel 模式 | 4/5 | CommunityToolkit 一致（5/5 VM）；`LoginView.xaml.cs:18-35` PasswordBox 双向同步（不可绑定，合理） | `LoginViewModel.cs:66-130` 15 个手写代理属性转发子 VM（XAML 兼容包袱，P2） |
| 死代码/重复 | 4/5 | 无明显死代码 | `ConnectionStatusViewModel.cs:35`/`LoginViewModel.cs:108` IsApiUnhealthy/HasMessage 无通知且 XAML 零引用（P2） |
| 内部组织 | 5/5 | Views/ViewModels/Models 三目录清晰 | 无 |

**三分决策：保留**（7 模块中最健康）。修补点（P2 级，非阻塞）：删无通知计算属性、评估手写代理属性可否用 x:Bind 等价替代。

### LYBT.Desktop.Users

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 4/5 | VM→IUserService→IUserRepository→IApiClient 守序（`RemoteUserService.cs:41`），无越层 | Service 纯转发冗余（P2） |
| 职责单一 | 4/5 | 最大 406 行（UserMasterDetailViewModel） | 手写映射块重复（见 Mapper） |
| Repository | 4/5 | `UserRepository.cs:17` 继承 `EntityApiClientRepositoryBase`（Foundation:17）✓ | `UserRepository.cs:81-108,130-183` 手写 try/catch 与基类 ExecuteAsync 混用；SearchAsync 手写绕过基类（P2） |
| Mapper | 1/5 | `Mappers/UserMapper.cs`（Mapperly）存在但**全仓零引用**（serena find_referencing_symbols 空） | VM 全部手写映射：`UserMasterDetailViewModel.cs:194-226` 同数据双份手写（UserDetailDto+UserDetailModel）；`UserEditorViewModel.cs:35-82` 双向手写（P1） |
| ViewModel 模式 | 4/5 | [ObservableProperty]/[RelayCommand] 一致；xaml.cs 仅 DP 声明 | 无 |
| 死代码/重复 | 3/5 | UserMapper 死；`UserEditContext.cs` 与 `UserDetailModel.cs` 180 行同构双份 | 两个几乎相同的模型类（P2） |
| 内部组织 | 4/5 | Repositories/Services/ViewModels/Handlers/Models/Mappers 齐全 | 实际用 `Controls/` 而非 AGENTS.md 描述的 `Views/`（文档失实，P2）；Mappers 目录名存实亡 |

**三分决策：修补**。修补点：① Mapper 二选一——接通 UserMapper 替换手写映射，或删除（当前死代码）；② 收敛 UserEditContext/UserDetailModel 双模型；③ Service 贫血模板三份合并（Users/Patients/Herbs 同为 try/catch+CommandResult+转发，P2）。

### LYBT.Desktop.Patients

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 4/5 | VM→IPatientService→IPatientRepository→IApiClient 守序（`PatientService.cs:40`），读卡器集成走 Repository | 4 个组件注册零消费（见死代码） |
| 职责单一 | 4/5 | 最大 342 行（PatientMasterDetailViewModel） | `PatientCardReaderIntegration.cs` 292 行双职责（匹配+创建，P2） |
| Repository | 4/5 | `PatientRepository.cs:15-17` 继承基类 ✓ | 内嵌 Mapper；`BatchImport/Export` 手写 try/catch 与基类模式混用（P2） |
| Mapper | 1/5 | **无 Mappers 目录**；`PatientListToDetailMapper` 内嵌于 `PatientRepository.cs:145-152` 且零调用 | 手写映射散落 3 处：`PatientEditorViewModel.cs:36-80` 双向手写、`PatientValidator.cs:168-180` ConvertToInputDto、VM 回填（P1） |
| ViewModel 模式 | 3/5 | CommunityToolkit 一致 | `PatientSelectionControl.xaml.cs:43-72` 反射 `GetPropertyValue<T>` 从 DataContext 取命令执行（脆弱越层，P2）；`PatientMasterDetailViewModel.cs:240-263` ViewMedicalRecords/NewConsultation 空壳（// FUTURE） |
| 死代码/重复 | 2/5 | **四件零消费死组件**：`MedicalCaseStartCoordinator.cs` 197 行（`CheckUnfinishedCaseAsync:75` 直接 return null 假实现）+ `PatientSearchManager.cs` 299 行 + `PatientSearchCache.cs` 195 行 + `PatientValidator.cs` 182 行——仅 `PatientsModule.cs:47-53` 注册、serena 引用零消费者；`MatchPatientAsync:148`（PRD-15）零消费 | 死代码总量 ~680 行（P1） |
| 内部组织 | 4/5 | Interfaces/Services/ViewModels/Components/Controls 分层齐全 | Interfaces 中 3 个接口对应的实现全是死代码 |

**三分决策：修补**。修补点：① 先清死组件链（MedicalCaseStartCoordinator/PatientSearchManager/Cache/PatientValidator，最高性价比任务）；② 立 Mappers 目录，手写映射收敛；③ 反射执行命令改事件绑定。

### LYBT.Desktop.Herbs

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 4/5 | VM→IHerbService→IHerbRepository→IApiClient 守序（`RemoteHerbService.cs:40`） | Service 纯转发冗余（P2） |
| 职责单一 | 5/5 | 最大 278 行（HerbMasterDetailViewModel） | 无 |
| Repository | 4/5 | `HerbRepository.cs:13` 继承基类 ✓ | 批操作手写 try/catch 与基类混用（P2） |
| Mapper | 1/5 | `Mappers/HerbMapper.cs`（Mapperly）**零引用** | `HerbMasterDetailViewModel.cs:114-148` 同数据手写双份（HerbDetailModel+HerbDetailDto）；`HerbEditorViewModel.cs:31-87` 双向手写（P1） |
| ViewModel 模式 | 4/5 | CommunityToolkit 一致；xaml.cs 无业务逻辑 | 无 |
| 死代码/重复 | 3/5 | HerbMapper 死 | 无其他 |
| 内部组织 | 4/5 | 目录清晰 | Mappers 名存实亡 |

**三分决策：修补**。修补点：Mapper 接入（或删除）+ 手写映射收敛，与 Users 同批次。

### LYBT.Desktop.Formula

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | VM→IFormulaService→IFormulaRepository（EntityApiClientRepositoryBase）→IApiClient 链路完整（`FormulaModule.cs:33-43`） | 无越层 |
| 职责单一 | 5/5 | 最大 MasterDetailVM 370 行 | 无 |
| Repository | 5/5 | `FormulaRepository.cs:13` 继承泛型基类（**三模块中唯一规范使用 Entity 泛型基类的模块**） | 规范 |
| Mapper | 3/5 | 3 个 Mapperly；仅 FormulaDetailModelMapper 注册（`FormulaModule.cs:36`），FormulaMapper/FormulaHerbItemMapper **零引用** | 2/3 mapper 未注册=死代码（P2） |
| ViewModel 模式 | 5/5 | [RelayCommand]+MasterDetailViewModelBase；xaml.cs 仅 DP | 无 |
| 死代码/重复 | 2/5 | FormulaMapper.cs(235)+FormulaHerbItemMapper.cs(76)+Models/Items/FormulaItem.cs(356)+FormulaHerbItem.cs(82) 全库零引用 ≈750 行死簇 | 死代码+文档漂移（AGENTS.md 仍列 CommandHandlers/FormulaValidator 已不存在）（P2） |
| 内部组织 | 4/5 | Mappers/Models/Repositories/Services/ViewModels 清晰 | 无 Views/ 目录（视图即 Controls），AGENTS.md 结构段不实 |

**三分决策：保留（轻修补）**。修补点：删死代码簇（FormulaMapper/FormulaHerbItemMapper/FormulaItem/FormulaHerbItem），同步 AGENTS.md。

### LYBT.Desktop.MedicalCase（最大模块，49 源文件）

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 3/5 | 主链 VM→IMedicalCaseService→Query/Command/Lifecycle→Repo→IApiClient 守序；但 **AuditLogViewModel.cs:18,57 直接注入 IApiClientMedicalCases**、**ReportsHomeViewModel.cs:14 直接注入 IApiClient**（VM→ApiClient 越层） | 2 处越层（P1） |
| 职责单一 | 3/5 | 无 >600（最大 CommandsVM 513/HistoryCopy 464）；但 MedicalCaseService.cs(350) 肥大门面，LoadDetails/AggregateSave 与 Lifecycle/Command 服务职责重叠（P2） | 服务层职责重叠 |
| Repository | 3/5 | MedicalCaseRepository.cs:14 用 `ApiClientRepositoryBase` 手写，未用 Entity 泛型基类；**UpdateAsync(:81-99) 全库无调用方**（实际走 SaveAsync） | 基类不一致+死方法（P2） |
| Mapper | 2/5 | **A-18 P1-4 桌面侧未修复**：4 个 Mapperly 仅 MedicalCaseDetailModelMapper 注册（`MedicalCaseModule.cs:46`）；Consultation/Prescription/CloneMapper 在 7 处 `new` 实例化（PrescriptionItemViewModel.cs:26 static、ConsultationItem.cs:23、EditContext.cs:14、CommandsVM.cs:44、EditorVM.cs:19-20），PrescriptionItemViewModel.cs:25 自带 TODO 承认不一致 | DI 风格分裂（P1） |
| ViewModel 模式 | 3/5 | [ObservableProperty]/[RelayCommand] 一致；但子 VM 在 MedicalCaseWorkspaceViewModel.cs:275-298 手动 `new`（Formula 同场景走 DI） | 构造风格不一致（P2） |
| 死代码/重复 | 2/5 | **MedicalCaseChangeTracker.cs 零引用**；MedicalCaseService.cs:237-349「额外业务方法」6 个方法全库无调用方且与 LifecycleService 逻辑重复 | 死代码+重复实现（P1） |
| 内部组织 | 3/5 | Reports/ 为 MedicalCase 程序集内第二个 Prism 模块（ReportsModule.cs:11）；Models 命名空间分裂：MedicalCaseDetailModel.cs:10 用陈旧 `LYBT.Desktop.Modules.MedicalCase.Models`；PrescriptionItemViewModel 放 Models/Items（VM 落错层） | 子模块嵌套+命名空间分裂（P2） |

**三分决策：修补**（骨架好，无需重写）。修补点：① 3 处 VM→IApiClient 越层改走 Service/Repository；② Mapper 统一 DI 注入（消 7 处 new/static）；③ 删死代码（ChangeTracker/6 方法/UpdateAsync）；④ 子 VM 走 DI。

### LYBT.Desktop.Registration（最小模块，15 源文件）

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 3/5 | **RegistrationListViewModel.cs:35,228 直接注入 IApiClientPatients**（VM→ApiClient 越层取患者）；CreateDialog 走 IPatientService 正常 | 1 处越层（P1） |
| 职责单一 | 5/5 | 最大 354 行 | 无 |
| Repository | 3/5 | RegistrationRepository.cs:13 用 ApiClientRepositoryBase 手写，未用 Entity 泛型基类 | 与 Formula 不一致（P2） |
| Mapper | 2/5 | **无 Mappers 目录、无 Mapperly**：WaitingQueue 直接绑定 `ObservableCollection<RegistrationListDto>`（RegistrationListViewModel.cs:44-45），DTO 直绑 XAML | 三模块中唯一无映射层（P2，模块小可接受需文档声明） |
| ViewModel 模式 | 5/5 | [ObservableProperty]/[RelayCommand] 统一；xaml.cs 仅初始化 | 无 |
| 死代码/重复 | 5/5 | 未发现 | 无 |
| 内部组织 | 3/5 | **命名空间 `LYBT.Desktop.Registrations`（复数）与项目/程序集/目录 `LYBT.Desktop.Registration`（单数）不符**（全部 .cs 均复数）；AGENTS.md 仍写 IApiRouter/ILocalRegistrationApi，实际已走 IApiClient | 命名空间漂移+文档过时（P2） |

**三分决策：修补**。修补点：修越层 + 命名空间；Mapper 可不引（模块小，DTO 直绑在文档声明）。

### Desktop 共性结论

1. **Desktop Mapper 全面失联（A-18 未完成部分，P1）**：Users/Herbs/Formula 的 Mapperly Mapper 文件 + Patients 内嵌 Mapper 全部零引用；DTO↔Model 映射全部手写且每模块重复 2-3 处（Editor VM/Master VM/Validator）。A-18 P1-4 只修 Server 侧。
2. **VM→IApiClient 越层 3 处（P1）**：AuditLogViewModel:18、ReportsHomeViewModel:14、RegistrationListViewModel:35。
3. **Repository 基类三口径（P2）**：Formula 用 EntityApiClientRepositoryBase（规范），MedicalCase/Registration 用 ApiClientRepositoryBase 手写，Users/Patients/Herbs 继承但混用手写 try/catch——「保留 Entity 泛型基类还是推广」待决策。
4. **三模块 Service 贫血模板（P2）**：RemoteUserService/PatientService/RemoteHerbService 同为 try/catch+CommandResult+转发，模板重复三份。

---

## B. Server 8 模块内部

> 共性基线：分层骨架全部合规（Controller→Service/Handler→Repository→DbContext），无越层直连 DbContext（除 IDbContextAccessor 灰色地带，见下）。**核心新发现：A-14 混合注入「规则」在 8 模块中无一完全符合——写操作普遍留在 Service 绕过 MediatR 管道（零验证零审计），部分查询反而走 MediatR；8 个领域事件全部无订阅者空转。**

### LYBT.Module.Auth

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | Controller 仅注入 ISender（AuthController.cs:24-32）；Repo→DbContext 合规 | 无越层 |
| Handler 组织 | 3/5 | 5 命令+1 Query 均有 Handler | 仅 Login 有 Validator（LoginRequestValidator.cs:9），其余 4 命令缺失；验证器命名 LoginRequestValidator 应为 LoginCommandValidator，且与 Shared 层同名类并存双注册（AuthModule.cs:58-59）（P2） |
| Service vs MediatR | 2/5 | ValidateTokenQuery 走 MediatR（AuthController.cs:144） | 查询违反 A-14 应走 Service（P1）；AutoLoginCommandHandler 纯委托无验证无审计（AutoLoginCommandHandler.cs:22-35），被禁用/删除用户仍可 auto-login（仅验 JWT 签名）（P1）；RefreshToken 重放仅拒单会话未整族失效（RefreshTokenCommandHandler.cs:38-63，偏离 ADR-0008）（P1） |
| Mapper | 2/5 | AuthUserMapper.cs:11-12 为 `[Mapper] public partial class`（非 static partial） | A-18 P1-4 名单未覆盖 Auth 模块，形态偏离文档声明；漏网手写：JwtService.cs:270-282/348-360 手拼 LoginResponse+UserDetailDto 两处、LoginCommandHandler.cs:162-168（P2） |
| 模块内重复 | 1/5 | ComputeTokenHash 完全相同 4 份：LoginCommandHandler.cs:204/Logout:64/Refresh:120/ValidateToken:67 | JwtService.RefreshToken(225-298) vs ValidateAutoLoginToken(303-376) 近 50 行同构；登录失败 LogWarning+审计 4 连重复（P2） |
| 死代码 | 1/5 | GetActiveSessionsAsync/GetByIdAsync（IAuthSessionRepository.cs:33/13）零调用 | SessionCreated/RevokedEvent 仅发布无订阅；AutoLoginCommand.IpAddress/UserAgent 采集未用（P2） |

**三分决策：保留**（分层最健康）。修补点：ComputeTokenHash 提取公共、JwtService 两方法合并、4 命令补 Validator+改名、删死代码、AutoLogin 补会话/状态校验、Mapper 改 static partial。

### LYBT.Module.Users

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 4/5 | Controller 无越层（BaseUsersController.cs:27） | 双 DbContext：UserManager 绑 AppDbContext（Program.cs:173-184），仓储用 UsersDbContext（UserRepository.cs:16），UserCrossModuleService.cs:18-24 经 IDbContextAccessor 直连 AppDbContext——同一 Users 表双源（P2） |
| Handler 组织 | 5/5 | 9 命令 1:1 Handler，无孤儿；无 Query 目录（查询全走 Service 符合 A-14） | RestoreUserCommand.cs:9-11 参数命名 OperatorRole vs 其余 CurrentUserId/IsAdmin 不一致（P3）；CreateUserCommandHandler.cs:37-40 手工判空与 Validator 重复（P2） |
| Service vs MediatR | 3/5 | 查询走 Service ✓（BaseUsersController.cs:45,57）；命令走 MediatR ✓ | Update/ChangeProfile 写操作走 Service（BaseUsersController.cs:83,223→UserService.cs:55,76）绕过验证+审计（P1）；UserService 与 Handler 职责重叠（P2） |
| Mapper | 4/5 | UserMapper.cs:11-12 已 Mapperly（A-18 已修不重报） | 漏网：UserCrossModuleService.cs:38-56/68-87 手写 16 属性复制且两方法近全同；BaseUsersController.cs:205-209 手写 ResetPasswordResponseDto（P2） |
| 模块内重复 | 3/5 | **单/批行为分叉**：Toggle 禁用撤销会话+审计（ToggleUserStatusCommandHandler.cs:47-59），批禁无（BatchDisable:31-37）；单 Delete 发事件+审计（DeleteUserCommandHandler.cs:48-61），批删无；单删不防自删、批删防（BatchDeleteUsersCommandHandler.cs:49-50） | 被禁用户旧 Token 仍有效（对应 03-server.md:617 安全风险）（P1）；恢复/启用字段重置重复；IsSysAdmin 校验 3 个批 Handler 重复（P2） |
| 死代码 | 3/5 | AddAsync/ExistsByUserNameAsync（UserRepository.cs:87/80）零调用（创建走 UserManager） | UserCreated/DeletedEvent 派发无订阅者；GetPagedAsync role/status 参数恒 null（UserService.cs:23）死参数（P2） |

**三分决策：保留+修补**。修补点：单/批行为对齐（批禁用补撤销会话+事件+审计）、Update 迁 MediatR、删死方法/死参数/无订阅事件、UserCrossModuleService 换 Mapperly。

### LYBT.Module.Patients

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 2/5 | PatientRepository.cs:15,17 直接注入共享 AppDbContext（serena 确认），全库无 PatientDbContext | **违反 ADR-0017「每模块独立 DbContext」**（0017:47）（P0）；未继承 BaseRepository<T>（:13），重复实现 Add/Update+SaveChanges（:95-106）；PatientCrossModuleService.cs:16,21,82-84 经 IDbContextAccessor 直查 MedicalCases 表——跨模块数据访问破坏隔离（P1） |
| Handler 组织 | 4/5 | 5 命令 1:1 Handler 齐全 | 无 UpdatePatient/RestoreCommand（Update/Restore 仅走 Service，PatientsController.cs:99,177）；仅 Create 有 Validator 其余 4 命令无；命名单复数混用 DeletePatient vs BatchDeletePatients（P2） |
| Service vs MediatR | 2/5 | Update 走 _patientService.UpdateAsync（PatientsController.cs:107）；Restore 走 Service（:182） | 写操作绕 MediatR→零校验零审计（P1）；CheckReference/BatchCheckReference 查询走 MediatR（:243,:273）违 A-14（P1）；UpdateProfile 双处实现（PatientService.cs:59-66 vs BatchImportHandler:75-78）；引用计数逻辑 4 处（P2） |
| Mapper | 3/5 | PatientMapper 已 Mapperly（A-18 已修） | 漏网手写：PatientCrossModuleService.cs:30-37/54-61 new PatientBasicDto{}；两 QueryHandler 手写 PatientReferenceCheckDto（CheckPatientReferenceQueryHandler.cs:30-39/Batch:33-42）（P2） |
| 模块内重复 | 2/5 | "患者有{n}条医案记录"消息 4 处（DeletePatientHandler:40/BatchDelete:52/两 Query:37/:40） | GetPatientsBasicInfoAsync N+1 循环（PatientCrossModuleService.cs:49-68）；PatientService.cs:27-33 手工重建 PagedResult（P2） |
| 死代码 | 2/5 | GetPatientsBasicInfoAsync(:41)/PatientExistsAsync(:73)/CheckPatientReferenceAsync(:80) 零调用 | PatientCreated/DeletedEvent 仅 Dispatch 无订阅者；模块 AGENTS.md 声称的 ImportExportService/Repositories 目录不存在（文档失真）（P2） |

**三分决策：修补（优先 P0）**。修补点：PatientRepository 迁独立 PatientDbContext（或明确豁免并在 ADR 记录）、补 Update/Restore 命令+Validator、跨模块引用走合法 ICrossModuleService、删死代码。

### LYBT.Module.Herbs

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 3/5 | 链路清晰，两仓储职责明确（与 03-server.md:563-571 一致） | HerbCrossModuleService.cs:17,20-24 经 IDbContextAccessor 直连 AppDbContext 绕过仓储，规避 P10 架构测试（仅查构造参数类型未拦住）（P2）；HerbReferenceRepository.cs:15 继承 BaseRepository\<Herb\> 类型参数无意义 |
| Handler 组织 | 4/5 | 4 命令 1:1，CheckHerbReference 有 Query | 单文件双 Query（CheckHerbReferenceQuery.cs:7-9）与双 IRequestHandler 非 1:1；验证器命名错位 HerbBatchImportCommandValidator.cs:6 vs BatchImportHerbsCommand.cs:11；缺 Delete/BatchDelete/Check/BatchCheck 验证器（P2） |
| Service vs MediatR | 2/5 | Update/ToggleStatus/Restore/BatchEnable/BatchDisable 5 个**写**端点走 Service（HerbsController.cs:110,164,184,276,296） | 写绕 MediatR→无 ValidationBehavior 无验证器（全仓无 AddFluentValidationAutoValidation，HerbsModule.cs:50 注册的 HerbInputDtoValidator 从未执行，Update 零验证）（P1）；CheckReference 查询走 MediatR（:241）违 A-14 且无 Service 等价方法未文档化（P1） |
| Mapper | 4/5 | HerbMapper.cs:11-12 已 Mapperly（ToEntity 手写属有意设计） | 两 QueryHandler/批量导入 Handler 手工构造 DTO（可接受） |
| 模块内重复 | 2/5 | **引用计数双实现且结果不一致**：HerbCrossModuleService.cs:60-70 只数 PrescriptionItems 漏 FormulaHerbItem；CheckHerbReferenceQueryHandler.cs:31-34 两者都数 | 跨模块删除保护弱于模块内检查，正确性不一致（P1）；名称/拼音双实现（HerbRepository.cs:108-125 vs CrossModuleService:41-58）；BatchEnable/BatchDisable 近全复制（HerbService.cs:107-161）；三套验证边界不一致（CreateHerbValidator ≤100/≤20/≥0 vs HerbInputDtoValidator ≤50/≤10/>0 vs BatchImport 仅 Name/Price）；Delete/Toggle 前重复读库（P2） |
| 死代码 | 2/5 | GetByNameOrPinyinAsync/GetAllAsync/GetByCategoryAsync（HerbRepository.cs:108,128,137）零调用 | HerbCreated/DeletedEvent 仅 raise 无订阅者（InMemoryDomainEventDispatcher.cs:18 直发）（P2） |

**三分决策：保留+修补**。修补点：统一引用计数逻辑（含验方）、验证边界三套合一、写端点补 Validator、删死仓储方法、批启禁抽公共方法。

### LYBT.Module.Formula

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | Service 仅注入 IFormulaRepository（FormulaService.cs:15-20）；独立 FormulaDbContext（FormulaModule.cs:39-47）；无越层 | 无 |
| Handler 组织 | 5/5 | 5 命令+1 查询全部成对；Validator 齐全（CreateFormulaValidator、FormulaBatchImportCommandValidator） | 无 |
| Service vs MediatR | 3/5 | 命令双轨：MediatR（Create/Delete/BatchDelete/BatchImport/ValidateHerb，FormulasController.cs:83,137,206,224,271）vs Service（Update/ToggleStatus/Restore/BatchEnable/Disable，:112,164,184,297,317） | 5 个**写操作**走 Service，违反 A-14「命令走 MediatR 验证+审计」（03-server.md:286）（P1） |
| Mapper | 5/5 | FormulaDtoMapper 已 Mapperly 化（A-18 P1-4 已修）；Service/Handler 零手工属性复制 | 无 |
| 模块内重复 | 3/5 | BatchEnableAsync/BatchDisableAsync 仅状态常量不同（FormulaService.cs:101-127 vs 129-155，~55 行） | 可抽公共循环（P2） |
| 死代码 | 2/5 | GetAllWithHerbsAsync/GetByCategoryWithHerbsAsync 全仓 0 调用（IFormulaRepository.cs:51-56、FormulaRepository.cs:119-136） | README 仍声称 GetTemplatesAsync/IFormulaImportExportService 存在（已删/不存在）→ 文档漂移（P2） |

**三分决策：修补**。修补点：① 删 2 死仓储方法；② BatchEnable/Disable 合并；③ Update/Toggle/Restore/BatchEnable/Disable 迁 MediatR 或修订 A-14 文档（二选一，先定策略）。

### LYBT.Module.MedicalCase

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 4/5 | Service 只注入 repository/cross-module（QueryService.cs:25-33、CommandService.cs:34-52） | 仓储直操 ChangeTracker 做实体状态手术（MedicalCaseRepository.Update.cs:20-66）并残留 `[诊断]` 调试日志（Repository.cs:265-303、Update.cs:80-96,111-143,206-215）（P2） |
| Handler 组织 | 3/5 | 无 Application/Commands/Queries/Validators——「CQRS」实现为 Command/Query/State 三 Service | MedicalCaseInputDtoValidator 已注册（MedicalCaseModule.cs:43）但**无 MediatR 管道触发**，校验靠手工（CommandService.cs:120-124）（P2） |
| Service vs MediatR | 2/5 | **0 MediatR**：命令全走 Service，无 ValidationBehavior/审计管道 | 与 A-14「命令走 MediatR」相反，注释（QueryService.cs:16-19）自认 CQRS（P1） |
| Mapper | 3/5 | Mapperly 主映射+手动 enrich 已文档化（MedicalCaseMapper.cs:141-165） | **漏网**：QueryRecentAsync DTO→DTO 手工 17 字段（QueryService.cs:374-391）、GetAuditLogsAsync 手工映射+switch（:489-503）、患者辨证/处方历史手工 enrich（:428-441,456-470）（P2） |
| 模块内重复 | 2/5 | 4 处：AddPrintLogAsync vs RecordPrintAsync（CommandService.cs:381-423 vs 428-465）；EnsureCanEdit vs EnsureCanDelete（Helper.cs:146-162 vs 179-194）；QueryAsync vs QueryPagedAsync（Repository.cs:168-213 vs 219-253）；GetPendingCasesAsync vs GetAllPendingCasesAsync（PendingCases.cs:20-71 vs 77-118） | 每处 40-60 行（P2） |
| 死代码 | 2/5 | 服务层 8 方法无调用方：CommandService 的 CreateAsync/UpdateConsultationAsync/SetPrescriptionFlagAsync/CreatePrescriptionAsync/CopyHistoricalPrescriptionAsync/UpdatePrescriptionAsync/DeletePrescriptionAsync（CommandService.cs:57,161,204,213,222,230,241）、StateService.CloseCaseAsync（:169）；接口实体版 GetByIdAsync/GetBatchAsync（IMedicalCaseQueryService.cs:23,155）；仓储 QueryAsync/GetByPatientIdWithDetailsAsync（Repository.cs:168,101） | 接口膨胀（19 成员实 8 无人用）（P1） |

**三分决策：保留+修补**（领域逻辑完整、测试覆盖健在，不重写）。修补点：瘦接口（删实体泄漏成员）、4 处去重、清 `[诊断]` 日志、手写映射并入 Mapper。

### LYBT.Module.Registration

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | Handler→IRegistrationRepository→AppDbContext（CreateRegistrationCommandHandler.cs:19-34）；跨模块走 ICrossModuleService | 无 |
| Handler 组织 | 4/5 | 4 命令+3 查询成对齐全、命名一致 | StartVisitCommand/CancelRegistrationCommand **无 Validator**，靠领域方法抛 InvalidOperationException 兜（StartVisitCommandHandler.cs:41-46）（P2） |
| Service vs MediatR | 3/5 | 100% MediatR——**查询也走 MediatR**（BaseRegistrationsController.cs:41,55,90） | 与 A-14「查询走 Service」相反；模块内自身一致（P1） |
| Mapper | 3/5 | Mapperly 已建（RegistrationMapper.cs:10-42） | CreateRegistrationCommandHandler.cs:43-57 手工 `new Registration{...}` 绕过 ToEntity，导致 `RegistrationMapper.ToEntity`（:42）成死代码（P2） |
| 模块内重复 | 3/5 | 派生 RegistrationsController 与基类重复：StartVisit/Cancel override 体与基类逐行一致（RegistrationsController.cs:77-105 vs BaseRegistrationsController.cs:97-124）；基类 GetList 手解析 Query 4 参数与查询 7 参数重复（Base:35-38） | 派生类冗余 override（P2） |
| 死代码 | 4/5 | 仅 RegistrationMapper.ToEntity 0 调用 | 单一（P2） |

**三分决策：修补**。修补点：① Create 改用 _mapper.ToEntity（消死代码+去手写）；② 删派生类 StartVisit/Cancel 冗余 override；③ StartVisit/Cancel 补 Validator 或文档化领域校验策略；④ 修订 A-14 或 Registration 迁 Service 查询。

### LYBT.Module.Reports

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| 分层健康 | 5/5 | Controller→ReportService→ReportRepository→AppDbContext（ReportRepository.cs:14-19）；注入 AppDbContext 属 A-18 P1-7 已文档化已知设计 | 无 |
| Handler 组织 | 4/5 | 纯只读聚合模块无 Command/Query，符合 A-14 特例（03-server.md:278） | 无 |
| Service vs MediatR | 5/5 | 纯 Service，与文档「只读聚合走 Service」完全一致 | 无 |
| Mapper | 4/5 | 无实体映射器：仓储投影直构 DTO（ReportRepository.cs:198-212）、Service 组装（ReportService.cs:86-96），只读投影合理 | 无 |
| 模块内重复 | 3/5 | 药费聚合 SQL **3 处**（ReportRepository.cs:30-44/87-98/129-136）；挂号费 2 处（:22-27/77-84）；DoctorPerformance 4 查询内存合并（:111-155） | 聚合可复用为公共 IQueryable（P2） |
| 死代码 | 4/5 | 模块内 0 死代码（8 服务方法全被消费） | **A-16「5 端点无桌面消费」复核仍成立**：trend/income、trend/consultations、doctor-performance、herbs/ranking、patient-flow（ReportsController.cs:89-178）在 IApiClientReports 无对应方法（仅 3 个 daily，IApiClientReports.cs:8-12），LocalWebAPI 也只实现 3 个（LocalWebAPI/ReportsController.cs:23-66）→ 本地模式 5 端点 404（P2 跨层未闭合面） |

**三分决策：保留**（8 模块中最健康）。修补点（可选）：合并药费/挂号费聚合为公共查询。

### Server 共性结论（跨模块）

1. **A-14 混合注入「规则」无一模块完全符合（P1，架构级）**：Formula 5 写操作走 Service、Registration 查询走 MediatR、MedicalCase 命令全走 Service、Auth 查询 ValidateToken 走 MediatR、Users/Patients/Herbs Update 走 Service——规律是「MediatR 迁移不彻底」，A-14 文档只记录原则未记录此遗留。**需决策：统一迁移写操作回 MediatR（补验证器）或修订 A-14 文档为真实约定**。
2. **领域事件全部空转（P1）**：8 个 Created/Deleted 事件（Auth 2/Users 2/Patients 2/Herbs 2）均无任何 INotificationHandler 订阅——事件投递进虚空，需决策补订阅者或删事件。
3. **架构测试盲区（P2）**：P10 仅查构造注入参数类型，未拦 IDbContextAccessor 直连模式（Patients/Herbs/Users 三处跨模块直查 AppDbContext 均绕过）；A-07 Mapperly 守卫空集即放行（手写 Mapper 可逃逸）。
4. **P0 唯一项**：Patients 违反 ADR-0017 无独立 DbContext（PatientRepository.cs:15,17 直连 AppDbContext）。

---

## C. LocalWebAPI 内部

| 维度 | 评分 | 证据 | 问题 |
|---|---|---|---|
| Controller 语义一致性 | 3/5 | 多数控制器薄壳继承共享基类，语义同构 | ①Auth 错误映射/响应包装不一致（本地 BusinessFail 无 401 映射，validate 匿名可达，AuthController.cs:66-72 类级无 [Authorize]，远程有）②HealthController 类级 [AllowAnonymous]（:16）vs 远程 [Authorize] ③Reports 权限面扩大 DoctorOrAdminOrReceptionist（:13）vs DoctorOrAdmin ④DeployController 纯 stub（:19,25）⑤MedicalCases 失败分支 BusinessFail vs 远程 NotFound、本地 200 vs 远程 201 |
| Program DI 组织 | 3/5 | LocalWebApiProgram.cs:48-128 8 模块注册齐全 | ①中间件仅 4 个（:132-135），远程 18+（UnifiedMiddlewareConfiguration.cs:20-180），**无 UseExceptionHandler/CorrelationId/CORS/SecurityHeaders**——本地异常回落到默认 500 HTML 非 ApiResponse JSON ②**双入口重复**：Program.cs（独立 5290 端口）内联复刻 RunAsync 逻辑，`LocalWebApiProgram.RunAsync`（:149-155）全仓库零调用（死方法） |
| 业务逻辑复用 | 4/5 | 绝大多数控制器委托 Server Service（ADR-0010 真复用） | ReportsController 直连 `IReportRepository`（:16,20）绕过 Service 层（P1，唯一直连 Repository 的本地控制器）；本地 Auth CQRS 4 Handler 为合理例外 |
| 死代码/休眠 | 2/5 | LocalDbContext 休眠已确认（A-16 一致） | LocalWebApiSeedData 英文样例（:26,38,48 Ginseng/Sample Formula/Sample Patient）污染本地库；EnsureCreated（SeedData:17）在 MigrateAsync（Program:144）后冗余调用（A-16 A1 确认仍在）；README:53 文档与 MigrateAsync 顺序不符 |

**三分决策：修补**（结构复用度高，无需重写）。修补点：
- **P1**：① AuthController validate 匿名 + 无 401 映射（本地认证语义与远程不一致，双模式切换错误分支不可预测）；② ReportsController 改注入 IReportService；③ MigrateAsync/EnsureCreated 混用消除；④ HealthController 权限策略对齐远程。
- **P2**：RunAsync 死方法删除（双入口合并）；英文种子数据删除/中文化；本地补 UseExceptionHandler；Formulas.Clone 本地独有端点缺 ValidateGuid/Authorize；Reports 本地仅 3/8 端点；JWT 占位密钥无强校验。
- **保留**：Auth 本地 CQRS（本地 JWT 语义合理）、Diagnostics 本地独有端点（db-info/version/logs/recent）、HealthController 实查库（比远程静态更有用）。

---

## D. Shell/Core 层

### Shell（4/5）

| 维度 | 结论 | 证据 |
|---|---|---|
| 组合根职责单一性 | ✅ 优。App 仅做平台初始化+委托，无业务逻辑；启动管线全下沉 | App.xaml.cs:38-52,90-115,126-131 |
| 模块注册方式 | ✅ 显式目录，WhenAvailable/OnDemand 分级合理 | App.xaml.cs:134-163 |
| 导航组织 | ⚠️ Region 常量与 XAML 字面量漂移风险 | MainWindow.xaml:35,184 硬编码 "LoginRegion"/"ContentRegion" vs RegionNames.cs:5-6（P2） |
| Roles 编排 | ⚠️ **P1-1 角色加载 Bug（真实缺陷）** | RoleDefinitionBase.cs:17 写 "AuthModule"，实际模块名 "AuthenticationModule"（AuthenticationModule.cs:14，App.xaml.cs:139 注册 WhenAvailable）。LoginCoordinator.cs:313 每次登录触发 LoadModulesForRoleAsync → LoadModule("AuthModule") 抛 ModuleNotFoundException → 被 ApplicationBootstrapper.cs:63-67 吞掉。功能无害（Auth 已 WhenAvailable）但每次登录产生错误日志，且 RoleRegistry 返回清单错误 |

启动步骤 4 个（ErrorHandling 10→ModuleCoordinator 20→ApiHealthCheck 40→LocalWebApi 250），顺序执行机制健全（StartupPipeline.cs:95）。⚠️ ApiHealthCheck 实际 Task.Run 后立即返回成功，作为"步骤"空转（ApiHealthCheckStartupStep.cs:43-66）；LocalWebApi 标记非必需但本地模式是硬依赖（LocalWebApiStartupStep.cs:26）。Shell 直接引用 LocalWebAPI（csproj:78）并硬编码 LocalDB 连接串（EmbeddedLocalWebApiService.cs:18-19，P2）。

**三分决策：修补**。修补点：RoleDefinitionBase "AuthModule"→"AuthenticationModule"；MainWindow Region 字面量改用常量；ApiHealthCheck 步骤语义修正或移除。

### Core 五项目

| 项目 | 职责边界 | 依赖方向 | 死代码 | 内部组织 |
|---|---|---|---|---|
| **Contracts** | 4.5/5。纯接口+契约载荷，仅 MedicalCaseNavigationParameters 一处 Prism 框架耦合（已 Compile Remove 隔离） | ✅ 仅 Shared.Models（csproj:34） | 无 | ✅ 11 Api + 13 ApiClient + 32 Services 接口分区清晰 |
| **Foundation** | 4.5/5。无头运行时层，WPF 类型 0 引用（UseWPF=false） | ✅ 仅 Contracts+Shared（csproj:58） | 无 | ✅ 10 子目录，HTTP/Security 为主 |
| **Controls** | 4/5。纯表现层；2 个内嵌控件 VM（HerbItem/HerbList）属自洽交互逻辑 | ⚠️ **Foundation 引用为死引用**（csproj:11，全项目 `LYBT.Desktop.Foundation` 0 使用） | 无 | ✅（ConverterInstances.cs:17 陈旧注释） |
| **Printing** | 4/5。打印排版/格式化规则可接受 | ❌ **Infrastructure 引用为死引用**（csproj:37，全源码 0 使用，唯一 "Infrastructure" 是 QuestPDF.Infrastructure） | 无 | ✅ 4 模板×8 文件，仅处方打印 |
| **Infrastructure** | 3/5。22 个顶层目录（见下方拆分） | ✅ 单向合法；但 Entities/EF Core/BCrypt 依赖仅支撑 LocalData 死代码 | **6 项新发现** | ❌ 职责过载 |

**依赖方向总评：单向无环 ✓**（Contracts→Shared.Models；Foundation→Contracts；Controls→Contracts+Foundation；Infrastructure→Controls+Foundation+Contracts+Shared+Entities；Printing→Infrastructure（死）；Shell→全部）。

### Infrastructure 职责过载拆分评估（F1-2 延续，只评估不实施）

| 职责 | 当前目录 | 建议归属 | 优先级 |
|---|---|---|---|
| Http（ApiResponseHelper/LoggingHttpHandler） | Http/ | → **Foundation/Http**（本属无头运行时） | P1 下沉 |
| CardReader（6 子目录） | CardReader/ | → 独立项目或归 Modules（被 Clinical 4 处 VM 消费，**存活**非死代码） | P1 下沉 |
| LocalData（LocalDbContext） | LocalData/ | **删除**（A-16 已报死代码）；删后 Infrastructure 可移除 Entities+EF Core 引用 | P1 删除 |
| Security（SensitiveInfoFilter） | Security/ | **删除**（0 引用）或归 Foundation | P1 删除 |
| Behaviors | Behaviors/ | 保留；删 ResponsiveLayoutBehavior（0 引用，与 Controls/ResponsiveLayoutHelper 功能重复） | P2 删除+保留 |
| Services/ 杂项 | Services/ | 删 ApiRouter + PrescriptionSettingsService（注册孤儿，0 消费）；其余存活 | P1 删除 |
| Views/Windows | Views/、Windows/ | 保留；UnfinishedCaseDialog 系列删（注册孤儿，ShowUnfinishedCaseDialogAsync 0 调用点） | P2 删除 |
| ViewModels | ViewModels/ | 保留（AGENTS 定义归属） | 保留 |
| Navigation | Navigation/ | 保留实现；INavigationServices/RegionNames 未随 A-18 下沉 Contracts（A-18 导航契约下沉仅部分兑现：INavigationCoordinator ✓） | P2 |
| Roles | Roles/ | 实现保留；契约 IRoleRegistry/IRoleDefinition 已在 Contracts ✓ | 保留 |
| Commands/Events/Helpers/Performance/Constants/Logging/Configuration/Extensions/DependencyInjection/Interfaces/Models | 各自 | 保留（全部存活） | 保留 |

**核心结论**：14 类并非全部是"职责过载"——真正过载表现为 22 个目录 + 6 项新死代码 + 2 个死项目引用。**先删后拆**：清理后 Infrastructure 实际职责收敛为 ViewModels/Views/Windows/Navigation/Behaviors/Commands/Events/Constants/Performance（纯 WPF 层），Http 归 Foundation、CardReader 独立，即可与 AGENTS.md「WPF services」定义对齐。

### Infrastructure 新增死代码（6 项，非 A-16 重复）

| 死代码 | 证据 |
|---|---|
| Security/SensitiveInfoFilter.cs | 全仓（含 tests）0 引用 |
| Behaviors/ResponsiveLayoutBehavior.cs | 0 引用，功能重复 Controls 同源实现 |
| Services/ApiRouter.cs + Contracts/Services/IApiRouter.cs | 仅注册 ServiceCollectionExtensions.cs:212，生产 0 消费，仅测试 |
| Services/PrescriptionSettingsService.cs + IPrescriptionSettingsService | 仅注册 :152，0 消费（与 F-01 联动） |
| Views/UnfinishedCaseDialog* + ViewModel + CommonDialogService.cs:121-141 | 已注册 App.xaml.cs:102-103，0 调用点 |
| Configuration/ConfigurationExtensions.cs:14 | 空实现 AddInfrastructureConfiguration，0 调用点 |

**P1-2/P1-3 死项目引用**：Printing→Infrastructure（可降级为零 Desktop Core 依赖）；Controls→Foundation（仅需 Contracts）。

---

## E. F 类遗留 8 项评估

| ID | 现状证据（文件:行号） | 评估 | 三分决策 |
|---|---|---|---|
| **F-01** FeatureToggle | **14→2 个开关**：`FeatureToggleOptions.cs:14,19`（12 个死开关 2026-06-14 `72042e244` 已删）。`OverwriteConflicts` 生产 0 消费（仅测试断言 ConfigurationLoadingTests.cs:120）；`DuplicateHerbMergeStrategy` 唯一消费 `PrescriptionSettingsService.cs:34-35`（绕过 Options 直读 IConfiguration），但该 Service 仅注册无注入消费（ServiceCollectionExtensions.cs:152，本身是死代码），实际合并已由 `DuplicateDosageStrategy` 枚举接管（HerbListControlViewModel.cs:177,205）。三种读法并存不统一 | 机制被产品文档自我否定（11b-configuration.md:113「开关已废弃，UI 可见性由 VM 自管」）；2 开关均无有效生产消费 | **删除**（含 PrescriptionSettingsService+IPrescriptionSettingsService 死服务；联动 F-03/F-04） |
| **F-02** 桌面 Mapper 统一 | Desktop **无 PatientMapper/MedicalCaseMapper 类**（Patients README.md:27 目录树为文档残留）。已全面 Mapperly：10 个 `[Mapper]`，AutoMapper 已移除。**DI 不一致**：仅 2 个走容器（FormulaModule.cs:36、MedicalCaseModule.cs:46），6+ 处手动 `new`（PrescriptionItemViewModel.cs:26、ConsultationItem.cs:23、MedicalCaseChangeTracker.cs:14 等）；另 5 处手写 Model↔Dto + 6 个死 Mapper（HerbMapper/UserMapper/FormulaMapper 链/PatientListToDetailMapper） | A-18 P1-4 **未覆盖 Desktop**（只改 Server 4 模块），但 Desktop 技术路线已同构；遗留点是 DI 风格 + 手写映射 + 死 Mapper | **修补**（统一 DI 单例注册；删死 Mapper；手写映射收敛；Patients README 目录树修正） |
| **F-03** LocalData Mapper Target | `LocalDbContext.cs:19` 生产**零引用**（仅 ICurrentUserProvider.cs:4 注释提及；真实引用全在 5 个测试文件）。LocalData 目录仅 1 文件，**无 Mapper**。全仓 15 个显式 `[Mapper]` 100% Target；仅 2 个未声明：PatientRepository.cs:145（死代码）、MedicalCaseCloneMapper.cs:20（UseDeepCloning） | TASK-06「统一 Target」显式层已实质达成（15/15）；LocalData Mapper 前提为空集；LocalDbContext 休眠被测试强依赖 | **修补**（MedicalCaseCloneMapper:20 补 Target；死 mapper 随 F-05 删；LocalDbContext 归入休眠清理批次，需同步改 5 个测试夹具） |
| **F-04** SyncService CS8602 | **已删除**：全仓 0 匹配，commit `f9a61cf2b`（2026-06-16，-20843 行）删整个 Sync 模块（含 SyncService.cs 697 行）。残留仅休眠物：LocalDbContext、OfflineModeOptions.cs:6、ErrorCode 80000 段、3 处缓存注释 | 任务对象已不存在，CS8602 随删除消失 | **删除**（任务 ⬜→✅ 关闭，总账 13-project-master-plan.md:127 标注已删） |
| **F-05** PatientMapper 死代码 | Server `PatientMapper.cs:12`（Mapperly + AutoUserMappings=false + 手写 ToEntity）**非死代码**：9 处真实调用（TogglePatientStatusCommandHandler.cs:52、CreatePatientCommandHandler.cs:36,45、BatchImportPatientsCommandHandler.cs:99、PatientService.cs:26,42,50,69,83） | A-18 P1-4 正向产物；Desktop 无同名类，无冲突 | **保留** |
| **F-06** 打印模板扩展（低） | 4 模板类型×8 文件（Printing/Templates/：Prescription 标准/A4/续页×2）；三大服务仅处方；DI 仅 PrintingModule.cs:27 注册 IPrintService\<PrescriptionPrintModel\>；无诊断报告/患者摘要/医案打印 | 与原 TASK-11 目标一致，未实现；IPrintService\<TModel\> 泛型已预留扩展 | **后置**（保留现状；建议 Printing AGENTS.md 注明未实现） |
| **F-07** API 版本化（低） | Server 12 Controller 全部 `[Route("api/v{version:apiVersion}/...")]`；Asp.Versioning 已注册（ApiServiceCollectionExtensions.cs:48-66，UrlSegment+Default v1.0）。**缺口**：管道无 UseApiVersioning()（UnifiedMiddlewareConfiguration.cs:100-177，0 匹配）；ServiceCollectionExtensions.cs:63-64 注释死代码；客户端 Refit 硬编码 /api/v1/（236 处） | 非硬编码，v2 扩展基础已具备；缺中间件显式调用属协商/报告能力 | **修补**（清理注释死代码；客户端升级时改 Refit 模板；UseApiVersioning 留待 v2） |
| **F-08** 日志归档（低） | LogCleanupService.cs 存在（113 行）且已注册（DatabaseServiceCollectionExtensions.cs:136）。语义=**删除**旧日志（:84-87 DELETE TOP 且 Error/Fatal 永久保留），**非「按月压缩」**——总账 master-plan.md:131 描述与实现偏差；构造注入 IServiceProvider 不触发 P10 | 「90 天删除+Error/Fatal 保留」合理；文档-实现语义偏差 | **修补**（仅修文档措辞为「删除旧日志（已实现）」；按月归档留待合规需求） |

---

## F. Tools + 测试项目

### Tools 4 项（均不在 sln/34 项目中）

| 工具 | 职责(行数) | 依赖 | 与测试重叠 | 决策 |
|---|---|---|---|---|
| ApiTester | 登录+reset-password（111 行） | 零依赖 | 极高：E2E 覆盖 AuthTests.cs:37/UserTests.cs:242；端口 5001≠测试 5000 | **删除** |
| LoginTester | 批量登录验证（96 行） | 零依赖 | 极高：AuthTests+AuthNegativeTests（5 Fact）全覆盖 | **删除** |
| UserInfoVerifier | 登录+用户详情解析（211 行） | 零依赖（手工 DTO 冗余于 UserDetailDto） | 极高：AuthTests.cs:102/UserTests.cs:75 | **删除** |
| PasswordHashGenerator | Identity PBKDF2 哈希运维（181 行） | 3 包+2 项目 | 低：无测试等价物，bea06505b 专门修复过 | **保留**（建议 appsettings 默认密码移 user-secrets） |

⚠️ 3 个待删工具 README 仍记录旧明文密码（ApiTester README:80 等）。

### 测试项目（与代码同步性）

| 项目 | 测试类 | 同步性结论 |
|---|---|---|
| LYBT.Tests.Server | 47 类（Unit 全覆盖 8 模块） | **同步 ✓**：0 引用已删类型（AsyncLocalCorrelationIdProvider/MediatR Handler 0 命中） |
| LYBT.Tests.Desktop | 88 类（Unit+Integration+LocalWebAPI+Roles） | **同步 ✓**：无 StaFact/Auto/DetectBestModeAsync 残留；冗余包 Microsoft.Data.SqlClient |
| LYBT.Tests.Architecture | 7 类/77 方法/**84 用例**（0 Skip） | **同步 ✓**：与总账 84/84 一致；P01-P22+DP01-09+MC/A/B/AM/CC/AR 全绿 |

**测试发现（P2）**：
1. **守卫失效（真空）**：ArchTests.cs:12 用 TestAssemblies.Server 扫描，但 P01/P01b 查 `.*\.ViewModels`、P01c 查 `LYBT.Desktop.*`——Server 程序集内不存在，三条恒通过；Desktop→WebAPI/Server Module 边界**无有效守卫**（DP01 只查 Infrastructure）。建议 P01c 改用 Desktop 程序集集。
2. **A07 守卫过弱**：ServerArchTests.cs:806 仅校验「有生成方法须有 [Mapper]」，mapperTypes 为空集即放行，不强制「业务映射必须 Mapperly」。
3. **隐式依赖**：Architecture csproj 未直引 Registration/Controls/Printing，靠传递复制 DLL 使 Assembly.Load 成功（脆性）。
4. 低危：DP07 编号重名（DesktopLayerArchTests.cs:205 vs :662）；P20 白名单残留 "MediatR"；测试含死配置键 FeatureToggles:ConsultationCreate/PrescriptionCreate（ConfigurationLoadingTests.cs:500-504）。

---

## G. B 类未做功能前置条件

| ID | 功能 | 前置条件（新架构正确路径） | 阻碍/半成品 | 标准动作 |
|---|---|---|---|---|
| **B-06** | 数据备份/恢复 | 远程：新 LYBT.Module.Backup 模块（DP07 边界内）+ MediatR Command（SQL BACKUP DATABASE）；LocalDB：LocalWebAPI 侧备份 | **无任何 BackupService**（全仓 0 命中）；DeployController 仅 upload/restart；IApiClientDeploy.cs:9-10 无备份方法 | Shared.Models 加 BackupDto → Server Backup 模块 → 两端端点 → IApiClientDeploy 加方法 → Refit+HTTP 双实现 |
| **B-07** | 初始化向导完善 | 向导多步化（服务器连接→诊所信息→管理员初始化） | FirstRunSetupView（Auth 模块 Views/）为对话框非 Shell 组件，注册于 AuthenticationModule.cs:42，触发于 LoginViewModel.cs:203-222；当前仅远程地址+测试连接+本地回退 | 保留 Auth 模块对话框或上移 Shell；新增步骤复用 IConnectionSettingsService |
| **B-08** | Desktop 发布包 | Shell csproj 加 PublishProfile/RID/SelfContained | LYBT.Desktop.Shell.csproj 无任何发布配置；全仓仅 WebAPI 有 PublishProfiles | 新建 Desktop FolderProfile.pubxml + 依赖裁剪 |
| **B-09** | 自动更新 | Velopack 集成（依赖 B-08） | **Velopack 零引用**（全仓 0 命中），纯新建 | dotnet add package Velopack + 发布脚本 hook |
| **B-11/B-12** | 药材/验方模板+患者导入导出 | Server 两端补 import-template/export 端点；NPOI 生成 Excel | **最大阻碍**：客户端双路径已调用但服务端两端都缺端点——HTTP 适配器 PatientsHttpApiClient.cs:43,47、FormulasHttpApiClient.cs:53,60、HerbsHttpApiClient.cs:43,47；Refit 同款 IFormulaApi.cs:70,76、IHerbApi.cs:57,63、IPatientApi.cs:59,67；而 WebAPI PatientsController.cs:212 等**均无 template/export 端点**（batch-import 为 JSON FromBody）→ **远端+本地模式均 404**。NPOI 仅 WebAPI.csproj:67 引用，**零代码使用（死引用）** | 仅需 Server 端补 3×2 端点（复用 NPOI），DTO 入 Shared.Models；客户端 URL 已对齐无需改 |
| **B-13** | 验方校验 UI | 纯 UI 补齐 | 服务端全链路**已通**：WebAPI FormulasController.cs:258(validate),241(pending-validation)；LocalWebAPI:238；客户端 IApiClientFormulas.cs:118,126+FormulasHttpApiClient.cs:76。缺口：Desktop Formula 模块**无校验 UI**（rg 无 ValidateHerb） | 建 FormulaValidationView + VM 调 IApiClientFormulas |
| **B-14** | 挂号排班 | 新实体 DoctorSchedule/ScheduleSlot → DTO → MediatR → 端点 → 客户端段 | LYBT.Module.Registration MediatR 结构完整但仅挂号 CRUD/队列/就诊；LYBT.Entities/Registrations/ 仅 RegistrationModel.cs，**无排班/号源实体**；IApiClientRegistrations 无排班方法；Desktop 无排班 UI | 全链路新建 |
| **B-15** | 离线同步 v2.0 | 接口层同步（IApiClient 统一面） | LocalDbContext.cs 生产零引用（休眠）；本地模式实走 LocalWebAPI HTTP；LYBT.Desktop.Sync 模块仅存在于文档（Modules/AGENTS.md），**项目未建** | 机遇：无历史债务，同步可在 SwitchingApiClient 双轨处实现；先定 LocalDbContext 去留（删或激活）避免双 DbContext 并存 |
| **B-16** | Swagger | — | **已完成未登记**：UnifiedMiddlewareConfiguration.cs:193-194(SwaggerEndpoint)、ApiServiceCollectionExtensions.cs:81-117(SwaggerOptions+XML 注释)；总账 13-plan 中 B-16 仍 ⬜ | 仅验收+登记，无需开发 |

**共性问题总结**：
1. **契约先行、服务端未落地（B-11/12 最大风险）**：IApiClient 子接口 + Refit + HttpClientApiClient 三处均已声明 import-template/export 并写死 URL，但 WebAPI 与 LocalWebAPI 两端控制器都没有对应路由 → 用户点导出即 404。属最紧急"半成品"，补齐服务端即可，勿动客户端契约。
2. **桩代码/死引用**：NPOI（WebAPI.csproj:67）引用零使用；LocalWebAPI DeployController 为占位桩；LocalDbContext 休眠但保留完整配置，B-15 需明确去留。
3. **"已实现未登记"与"服务端全通只缺 UI"两类特例**：B-16 代码已完成（总账 ⬜ 待更新）；B-13 服务端+客户端接口+适配器全通，只剩桌面 UI。
4. **新功能标准动作（契约统一后）**：Shared.Models 定义 DTO → Server 模块 MediatR Command/Query + Mapperly → WebAPI + LocalWebAPI 对称端点 → IApiClientXxx 接口方法 → RefitApiClient 与 HttpClientApiClient 双实现 URL 对齐 → Desktop 模块 VM/View。B-06/14 全新建，B-11/12/13 半途续建（只做缺失段）。

---

## 统计汇总

| 维度 | 数量 |
|---|---|
| 评分卡 | Desktop 7 + Server 8 + LocalWebAPI 1 + Shell 1 + Core 5 = **22 张** |
| 三分决策 | 保留 4（Desktop.Auth、Server.Auth、Server.Reports、Formula 轻修补）/ 修补 16 / 重写 0 |
| P0 | **1**（Server.Patients 违反 ADR-0017 无独立 DbContext） |
| P1 | 约 15（Desktop Mapper 失联、VM→ApiClient 越层×3、Patients 死组件链、A-14 系统性偏离、领域事件空转、Auth AutoLogin/RefreshToken 安全、Users 批操作行为分叉、LocalWebAPI Auth/Reports/EnsureCreated、Shell 角色加载 bug、Printing/Controls 死引用、B-11/12 双端 404） |
| P2 | 约 40（死代码、文档漂移、命名空间漂移、验证器缺口、重复实现等） |
| F 类 | 删除 2（F-01 机制、F-04 关闭）/ 修补 4（F-02/F-03/F-07/F-08）/ 保留 1（F-05）/ 后置 1（F-06） |
| Tools | 删除 3 / 保留 1 |
| 测试 | 84/84 全绿同步 ✓；守卫真空 3 处 + A07 过弱 |
| B 类 | B-11/12 最紧急（客户端已调服务端 404）；B-16 已完成未登记；B-13 仅缺 UI；B-06/08/09/14 全新建；B-15 机遇大于阻碍 |

**审计声明**：全部结论基于静态代码证据（文件:行号 + 符号级引用验证），未修改任何代码、未 commit、未运行应用。P0（Patients DbContext）与 P1 级安全项（AutoLogin 无状态校验、批禁用不撤销会话）建议技术总监运行验证后再定级处置。报告文件由技术总监统一提交。
