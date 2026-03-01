# LYBT.Desktop.Contracts

> Desktop端接口契约层 | Refit API定义 | 服务抽象 | 数据源接口

## 项目定位

- **层级**: Client Core层
- **职责**: 定义Desktop端所有抽象接口，包括API客户端契约、服务接口、数据源抽象和事件类型。所有模块通过此层实现依赖倒置

## 目录结构

```
LYBT.Desktop.Contracts/
├── Api/                          # Refit API接口(7个模块)
│   ├── IAuthApi.cs               # 认证(登录/登出/Token/健康检查)
│   ├── IUserApi.cs               # 用户CRUD+密码+批量操作
│   ├── IPatientApi.cs            # 患者CRUD+导入导出+批量操作
│   ├── IMedicalCaseApi.cs        # 医案CRUD+生命周期+打印+批量
│   ├── IHerbApi.cs               # 药材CRUD+导入导出+批量操作
│   ├── IFormulaApi.cs            # 验方CRUD+克隆+导入导出+批量
│   └── ISyncApi.cs               # 数据同步(比对/上传/下载/删除)
├── CommandHandlers/              # 统一命令模式(3文件)
│   ├── CommandResult.cs          # 统一返回类型(含隐式bool转换)
│   ├── ICommandHandlerBase.cs    # 泛型CRUD接口
│   └── QueryParams.cs            # 统一查询参数
├── DataSources/                  # 数据源抽象层(6文件，支持双模式)
│   ├── IDataSourceBase.cs        # 通用CRUD基础接口
│   └── I{Module}DataSource.cs    # 各模块数据源(Formula/Herb/MedicalCase/Patient/User)
├── Events/CacheEvents.cs         # 缓存失效事件
├── Models/ImportValidationResult.cs # 导入验证结果
├── Roles/                        # 角色体系(2文件)
│   ├── IRoleDefinition.cs        # 角色定义接口(策略模式)
│   └── IRoleRegistry.cs          # 角色注册表接口
├── Security/                     # 安全认证(2文件)
│   ├── AuthState.cs              # 认证状态枚举+事件+参数
│   └── IAuthenticationStateMachine.cs # 状态机接口
└── Services/                     # 服务接口(24文件)
    ├── CrossModule/              # 跨模块搜索(2文件)
    │   ├── IFormulaSearchProvider.cs
    │   └── IHerbSearchProvider.cs
    ├── ILoginCoordinator.cs      # 登录流程协调
    ├── ISessionManager.cs        # 会话管理
    ├── INavigationCoordinator.cs # 统一导航
    ├── IMedicalCase*.cs          # 医案三分离(Query/Command/Lifecycle)
    ├── IPendingQueueManager.cs   # 待诊队列
    ├── IStartupPipeline.cs       # 启动管道
    ├── ISyncService.cs           # 数据同步协调
    └── ...                       # 其他服务接口
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IAuthApi | 7 | 登录/自动登录/登出/Token刷新/验证/健康检查 |
| IUserApi | 14 | 用户CRUD/密码管理/批量操作 |
| IPatientApi | 10 | 患者CRUD/导入导出/批量操作 |
| IMedicalCaseApi | 19 | 医案CRUD/状态管理/打印/审计/批量操作 |
| IHerbApi | 13 | 药材CRUD/导入导出/批量操作 |
| IFormulaApi | 14 | 验方CRUD/克隆/导入导出/批量操作 |
| ISyncApi | 6 | 同步元数据/比对/上传/下载/删除 |

## 设计特点

| 特点 | 说明 |
|------|------|
| Refit框架 | 通过特性标注自动生成HTTP客户端实现 |
| 类型安全 | 所有API方法使用强类型DTO，编译时检查 |
| 依赖倒置 | 业务模块依赖此层接口，不直接互相依赖 |
| 双模式支持 | DataSource抽象层支持Remote/Local两种实现 |
| SRP三分离 | MedicalCase拆分为Query/Command/Lifecycle三个接口 |

## 设计依据

- Contracts层独立于实现，允许模块间通过接口通信而不产生直接依赖
- 使用Refit接口定义API契约，消除手写HttpClient代码
- API接口与Shared.Models中的DTO配合，确保Client与Server端API类型编译时同步
- 跨模块服务契约(如IHerbSearchProvider)放在此层，避免业务模块循环依赖

## 依赖关系

### 依赖
- LYBT.Shared.Models (共享DTO)
- Refit (7.x)
- Refit.HttpClientFactory (7.x)

### 被依赖
- LYBT.Desktop.Foundation (Refit客户端注册)
- LYBT.Desktop.Models (Repository层调用)
- 所有Desktop业务模块

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 修正目录结构，移除已删除的ghost接口 |
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-12 | 从Shared.Interfaces迁移至Desktop.Contracts |
