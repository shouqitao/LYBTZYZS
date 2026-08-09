# 架构测试规则映射表

> 自动生成于 2026-08-06 | 测试总数: 83

## 命名规则

- `P` 前缀: 分层/依赖/命名规范规则 (Platform)
- `B` 前缀: Batch 治理规则 (Batch)
- `A` 前缀: A-09 架构守卫规则 (Architecture)
- `AR` 前缀: 聚合根规则 (Aggregate Root)
- `DP` 前缀: Desktop 层规则 (Desktop Pattern)
- `DM` 前缀: Desktop 模块规则 (Desktop Module)
- `CC` 前缀: 自定义控件规则 (Custom Control)
- `AM` 前缀: 反 Mock 规则 (Anti-Mock)
- `MC` 前缀: MedicalCase 业务规则 (Medical Case)
- `P20-P22`: LocalWebAPI 规则

## 规则映射

| 规则编号 | 测试方法 | 文件 | 简述 |
|---------|---------|------|------|
| **分层依赖 (ArchTests.cs)** ||||
| P01 | P01_UI_Should_Not_Depend_On_Infrastructure | ArchTests.cs | UI层禁依赖Infrastructure |
| P01b | P01b_UI_Should_Not_Depend_On_Entities | ArchTests.cs | UI层禁依赖Entities |
| P01c | P01c_Desktop_Should_Not_Depend_On_WebAPI | ArchTests.cs | Desktop禁依赖WebAPI |
| P02 | P02_Controller_Should_Be_In_WebAPI_Project | ArchTests.cs | Controller必须在WebAPI项目 |
| P03 | P03_No_Workflow_Framework_References | ArchTests.cs | 禁用工作流引擎框架 |
| P03b | P03b_No_Rules_Engine_References | ArchTests.cs | 禁用规则引擎框架 |
| P04 | P04_UserName_Convention | ArchTests.cs | UserName字段命名规范 |
| P05 | P05_Entities_Should_Not_Depend_On_Shared | ArchTests.cs | Entities禁依赖Shared |
| P05b | P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore | ArchTests.cs | Shared禁依赖AspNetCore（豁免：LYBT.Shared.Logging，A-31-C1） |
| P05c | P05c_SharedUtilities_Should_Not_Depend_On_Swashbuckle | ArchTests.cs | Shared工具禁依赖Swashbuckle |
| P05d | P05d_Shared_Should_Not_Depend_On_Server_Modules | ArchTests.cs | Shared禁依赖Server模块 |
| P05e | P05e_Shared_Should_Not_Depend_On_Desktop | ArchTests.cs | Shared禁依赖Desktop |
| P06 | P06_NoReverseOrCircularDependencies | ArchTests.cs | 分层依赖方向(Theory,8组) |
| P07 | P07_ServerModules_Should_Not_Reference_Other_ServerModules | ArchTests.cs | Server模块间隔离 |
| **Batch治理 (ArchTests.cs)** ||||
| B01 | B01_Cache_Should_Use_ICacheService_Only | ArchTests.cs | 缓存统一使用ICacheService |
| B01b | B01b_Cache_No_Duplicate_Registration | ArchTests.cs | 禁止重复缓存注册 |
| B02 | B02_Use_GlobalExceptionHandler_Only | ArchTests.cs | 统一异常处理 |
| B02b | B02b_Controllers_Should_Use_BaseApiController | ArchTests.cs | Controller必须继承BaseApiController |
| B03 | B03_Configuration_Use_ConfigurationHelper | ArchTests.cs | 配置直读统一 |
| B04 | B04_Frontend_Should_Use_Desktop_Namespace | ArchTests.cs | 前端命名空间规范 |
| B05 | B05_No_Reintroduce_Deleted_Components | ArchTests.cs | 防止已删除组件回归 |
| **Server架构 (ServerArchTests.cs)** ||||
| P02b | P02b_AllRepositories_Must_Inherit_BaseRepository | ServerArchTests.cs | Repository必须继承BaseRepository |
| P08 | P08_CrossModule_References_Must_Use_Interfaces | ServerArchTests.cs | 跨模块引用必须通过接口 |
| P09 | P09_Controller_Must_Have_ClassLevel_Authorize | ServerArchTests.cs | Controller必须有类级别Authorize(含Batch策略检查) |
| P09b | P09b_Controllers_Should_Use_V1_Routes | ServerArchTests.cs | Controller路由必须使用v1 |
| P09c | P09c_Controller_Must_Be_In_Controllers_Namespace | ServerArchTests.cs | Controller必须在Controllers命名空间 |
| P10 | P10_Services_Should_Not_Directly_Inject_AppDbContext | ServerArchTests.cs | Service禁直接注入AppDbContext |
| P10b | P10b_Service_Must_Have_Service_Suffix | ServerArchTests.cs | Service类必须以Service结尾 |
| P11 | P11_No_Redis_Usage | ServerArchTests.cs | Server端禁止Redis |
| P11b | P11b_Only_Use_EntityFramework | ServerArchTests.cs | 只允许EF作为ORM |
| P12 | P12_Entities_Should_Not_Depend_On_Business_Layers | ServerArchTests.cs | Entities禁依赖业务层 |
| P12b | P12b_Infrastructure_Should_Not_Depend_On_WebAPI | ServerArchTests.cs | Infrastructure禁依赖WebAPI |
| P13 | P13_Dto_Must_Have_Dto_Suffix | ServerArchTests.cs | DTO必须以Dto结尾 |
| P14 | P14_Service_IO_Methods_Must_Be_Async | ServerArchTests.cs | Service I/O方法必须异步 |
| P15 | P15_Configuration_Must_Be_In_Correct_Location | ServerArchTests.cs | Configuration类位置规范 |
| P16 | P16_Modules_No_Circular_Dependencies | ServerArchTests.cs | 模块间不得循环依赖 |
| P17 | P17_Infrastructure_Hardening_Rules | ServerArchTests.cs | 基础设施强化规则 |
| P19 | P19_Cqrs_Services_Must_Not_Expose_Write_Methods | ServerArchTests.cs | CQRS模块Service接口禁暴露写方法（蓝图§2.2，A-28） |
| P19b | P19b_Cqrs_Write_Endpoints_Must_Not_Call_Service_Write_Methods | ServerArchTests.cs | CQRS Controller禁直调Service写方法（IL扫描，A-28） |
| MC01 | MC01_MedicalCase_StateTransition_Rules | ServerArchTests.cs | MedicalCase状态流转规则 |
| MC02 | MC02_MedicalCase_Validators_Must_Exist | ServerArchTests.cs | MedicalCase验证器必须存在 |
| A01 | A01_Controllers_Must_Inherit_BaseApiController | ServerArchTests.cs | Controller必须继承BaseApiController |
| A02 | A02_Desktop_Repositories_Must_Inherit_ApiClientRepositoryBase | ServerArchTests.cs | Desktop Repository必须继承ApiClientRepositoryBase |
| A03 | A03_Modules_Must_Have_DI_Registration | ServerArchTests.cs | 模块必须有DI注册方法 |
| A04 | A04_Options_Must_Define_SectionName | ServerArchTests.cs | Options类必须定义SectionName |
| A05 | A05_Controller_Methods_Must_Return_IActionResult | ServerArchTests.cs | Controller方法必须返回IActionResult |
| A06 | A06_Validators_Must_Inherit_AbstractValidator | ServerArchTests.cs | 验证器必须继承AbstractValidator |
| A07 | A07_Mapperly_Mappers_Must_Have_Mapper_Attribute | ServerArchTests.cs | Mapperly Mapper必须有Mapper注解 |
| **聚合根 (AggregateRootArchTests.cs)** ||||
| AR001 | AR001_MedicalCase_Should_Be_Aggregate_Root | AggregateRootArchTests.cs | MedicalCase聚合根验证 |
| AR003 | AR003_All_Entities_Should_Support_Soft_Delete | AggregateRootArchTests.cs | 软删除一致性验证 |
| **LocalWebAPI (LocalWebApiPatternTests.cs)** ||||
| P20 | P20_LocalWebAPI_Controllers_Only_Inject_Allowed_Types | LocalWebApiPatternTests.cs | LocalWebAPI依赖白名单 |
| P21 | P21_LocalWebAPI_References_Match_ADR0010 | LocalWebApiPatternTests.cs | Server模块引用需匹配ADR-0010 |
| P22 | P22_LocalWebAPI_Controllers_Must_Have_ApiController | LocalWebApiPatternTests.cs | LocalWebAPI必须有ApiController |
| **Desktop层 (DesktopLayerArchTests.cs)** ||||
| DP01 | DP01_Desktop_Should_Not_Depend_On_Server | DesktopLayerArchTests.cs | Desktop禁依赖Server |
| DP02 | DP02_Desktop_Should_Not_Contain_DTO_Classes | DesktopLayerArchTests.cs | Desktop禁包含DTO类 |
| DP03 | DP03_UI_Models_Must_Have_Correct_Suffix | DesktopLayerArchTests.cs | UI模型命名后缀规范 |
| DP04 | DP04_ViewModels_Must_Inherit_Base_Classes | DesktopLayerArchTests.cs | ViewModel必须继承基类 |
| DP05 | DP05_Events_No_Duplicate_Definitions | DesktopLayerArchTests.cs | 事件定义不得重复 |
| DP06 | DP06_Desktop_Should_Not_Use_Entity_Classes | DesktopLayerArchTests.cs | Desktop禁直接使用Entity |
| DP07 | DP07_Services_Must_Follow_Naming_Convention | DesktopLayerArchTests.cs | Service命名规范 |
| DP08 | DP08_ViewModels_No_Direct_Api_Interfaces | DesktopLayerArchTests.cs | ViewModel禁直接使用Api接口 |
| DP09 | DP09_Must_Use_Unified_Navigation_Service | DesktopLayerArchTests.cs | 必须使用统一导航服务 |
| DM01 | DM01_AllRepositories_Must_Have_Remote_Implementation | DesktopLayerArchTests.cs | Repository必须有Remote实现 |
| DM01b | DM01b_Modules_No_Forbidden_Directories | DesktopLayerArchTests.cs | 模块禁包含禁止目录 |
| DM02 | DM02_ViewModels_Use_Standard_Base_Classes | DesktopLayerArchTests.cs | ViewModel使用标准基类 |
| DM03 | DM03_CrudViewModels_Must_Inherit_MasterDetailViewModelBase | DesktopLayerArchTests.cs | CRUD VM必须继承MasterDetailViewModelBase |
| DM04 | DM04_ViewModels_No_New_DelegateCommand | DesktopLayerArchTests.cs | 禁止新增DelegateCommand |
| DM05 | DM05_Repository_Interfaces_Must_Be_In_Contracts | DesktopLayerArchTests.cs | Repository接口必须在Contracts |
| DM06 | DM06_Business_Modules_No_Cross_References | DesktopLayerArchTests.cs | 业务模块间不得相互引用 |
| DM07 | DM07_LocalData_Must_Not_Depend_On_SQLite | DesktopLayerArchTests.cs | LocalData禁依赖SQLite |
| DM08 | DM08_Production_Should_Not_Contain_LocalDbContext | DesktopLayerArchTests.cs | 生产层不得含 LocalDbContext（A-21 C1 已移入测试项目） |
| **自定义控件 (CustomControlArchTests.cs)** ||||
| CC01 | CC01_Custom_Controls_Must_Exist | CustomControlArchTests.cs | 自定义控件必须存在 |
| CC02 | CC02_MasterDetailLayout_Must_Have_Content_Properties | CustomControlArchTests.cs | MasterDetailLayout内容属性 |
| CC03 | CC03_DataGridToolbar_Must_Have_Content_Properties | CustomControlArchTests.cs | DataGridToolbar内容属性 |
| CC04 | CC04_Controls_Must_Inherit_From_Control | CustomControlArchTests.cs | Controls必须继承Control |
| **反Mock (AntiMockRuleTests.cs)** ||||
| AM01 | AM01_ServerTests_No_NSubstitute_Reference | AntiMockRuleTests.cs | Server测试禁引用NSubstitute |
| AM02 | AM02_ServerTests_No_NSubstitute_Dependencies | AntiMockRuleTests.cs | Server测试禁NSubstitute依赖 |
| AM03 | AM03_IntegrationTests_No_EFCore_InMemory | AntiMockRuleTests.cs | 集成测试禁用EF InMemory |
