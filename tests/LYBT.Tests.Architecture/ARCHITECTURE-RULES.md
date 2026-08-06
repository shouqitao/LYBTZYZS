# 架构规则映射表

> 自动生成于 2026-08-06 | 测试总数: 85

## 规则索引

| 规则编号 | 描述 | 测试方法 | 文件 | 文档引用 |
|---------|------|---------|------|---------|
| P01 | UI层不得依赖Infrastructure层 | `P01_UI_Should_Not_Depend_On_Infrastructure` | ArchTests.cs | 03-server.md |
| P01b | UI层不得依赖Entities层 | `P01b_UI_Should_Not_Depend_On_Entities` | ArchTests.cs | 03-server.md |
| P01c | Desktop不得依赖WebAPI层 | `P01c_Desktop_Should_Not_Depend_On_WebAPI` | ArchTests.cs | 03-server.md |
| P02 | 控制器必须在WebAPI项目 | `P02_Controller_Should_Be_In_WebAPI_Project` | ArchTests.cs | 03-server.md |
| P02 | Repository必须继承BaseRepository | `P02_AllRepositories_Must_Inherit_BaseRepository` | ServerArchTests.cs | 03-server.md |
| P03 | 禁止工作流引擎框架 | `P03_No_Workflow_Framework_References` | ArchTests.cs | 03-server.md |
| P03b | 禁止规则引擎框架 | `P03b_No_Rules_Engine_References` | ArchTests.cs | 03-server.md |
| P04 | UserName命名规范 | `P04_UserName_Convention` | ArchTests.cs | 03-users.md |
| P05 | Entities不得依赖Shared | `P05_Entities_Should_Not_Depend_On_Shared` | ArchTests.cs | 03-server.md |
| P05b | Shared不得依赖AspNetCore | `P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore` | ArchTests.cs | 03-server.md |
| P05c | Shared不得依赖Swashbuckle | `P05c_SharedUtilities_Should_Not_Depend_On_Swashbuckle` | ArchTests.cs | 03-server.md |
| P05d | Shared不得依赖Server模块 | `P05d_Shared_Should_Not_Depend_On_Server_Modules` | ArchTests.cs | 03-server.md |
| P05e | Shared不得依赖Desktop层 | `P05e_Shared_Should_Not_Depend_On_Desktop` | ArchTests.cs | 03-server.md |
| P06 | 分层依赖方向（无反向/循环） | `P06_NoReverseOrCircularDependencies` | ArchTests.cs | 03-server.md |
| P07 | Server模块间不得相互引用 | `P07_ServerModules_Should_Not_Reference_Other_ServerModules` | ArchTests.cs | 03-server.md |
| P08 | 跨模块引用必须通过接口 | `P08_CrossModule_References_Must_Use_Interfaces` | ServerArchTests.cs | 03-server.md |
| P09 | Controller必须有[Authorize] | `P09_AllControllers_Must_Have_ClassLevel_Authorize` | ServerArchTests.cs | 03-server.md |
| P10 | Service不得直接注入AppDbContext | `P10_Services_Should_Not_Directly_Inject_AppDbContext` | ServerArchTests.cs | 03-server.md |
| P20 | LocalWebAPI依赖白名单 | `P20_LocalWebAPI_Controllers_Should_Only_Inject_Allowed_Types` | LocalWebApiPatternTests.cs | ADR-0010 |
| P21 | LocalWebAPI引用审计 | `P21_LocalWebAPI_ServerModule_References_Match_ADR0010` | LocalWebApiPatternTests.cs | ADR-0010 |
| P22 | LocalWebAPI必须有[ApiController] | `P22_LocalWebAPI_Controllers_Must_Have_ApiController_Attribute` | LocalWebApiPatternTests.cs | ADR-0010 |
| AR001 | MedicalCase聚合根 | `AR001_MedicalCase_Should_Be_Aggregate_Root` | AggregateRootArchTests.cs | 03-server.md |
| AR003 | 软删除一致性 | `AR003_All_Entities_Should_Support_Soft_Delete` | AggregateRootArchTests.cs | 03-server.md |
| B01 | 缓存唯一正源 | `B01_Cache_Should_Use_ICacheService_Only` | ArchTests.cs | 03-server.md |
| B01b | 禁止重复缓存注册 | `B01b_Cache_No_Duplicate_Registration` | ArchTests.cs | 03-server.md |
| B02 | 统一异常处理 | `B02_Use_GlobalExceptionHandler_Only` | ArchTests.cs | 03-server.md |
| B02b | Controller继承BaseApiController | `B02b_Controllers_Should_Use_BaseApiController` | ArchTests.cs | 03-server.md |
| B03 | 配置直读使用ConfigurationHelper | `B03_Configuration_Use_ConfigurationHelper` | ArchTests.cs | 03-server.md |
| B04 | 前端命名空间规范 | `B04_Frontend_Should_Use_Desktop_Namespace` | ArchTests.cs | 03-server.md |
| B05 | 防止已删除组件回潮 | `B05_No_Reintroduce_Deleted_Components` | ArchTests.cs | 03-server.md |

## 测试文件职责

| 文件 | 职责 | 测试数 |
|------|------|--------|
| `ArchTests.cs` | 通用分层依赖 + 命名规范 + 防回潮 | 25 |
| `ServerArchTests.cs` | Server端专属规则 | 27 |
| `DesktopLayerArchTests.cs` | Desktop层专属规则 | 18 |
| `AggregateRootArchTests.cs` | DDD聚合根模式 | 2 |
| `LocalWebApiPatternTests.cs` | LocalWebAPI模式 | 3 |
| `AntiMockRuleTests.cs` | 测试质量守卫 | 3 |
| `CustomControlArchTests.cs` | 自定义控件规范 | 4 |

## 程序集清单

所有测试统一使用 `TestAssemblies` 类定义的程序集：

- `TestAssemblies.Server` — Server端程序集（11个）
- `TestAssemblies.Desktop` — Desktop端程序集（15个）
- `TestAssemblies.All` — 全部程序集（Server + Desktop + Shared）

## 命名规范

测试方法命名格式：`规则编号_描述`

示例：
- `P06_NoReverseOrCircularDependencies`
- `P10_Services_Should_Not_Directly_Inject_AppDbContext`
- `AR001_MedicalCase_Should_Be_Aggregate_Root`
