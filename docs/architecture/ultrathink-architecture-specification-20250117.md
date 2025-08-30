# UltraThink架构规范文档 v1.0

> 文档版本：1.0  
> 更新日期：2025-01-17  
> 作者：UltraThink架构组  
> 状态：正式发布  

## 目录

1. [架构概述](#架构概述)
2. [三层架构设计](#三层架构设计)
3. [命名规范标准](#命名规范标准)
4. [模块化设计原则](#模块化设计原则)
5. [实施指南](#实施指南)
6. [架构验证清单](#架构验证清单)

---

## 架构概述

### 系统定位
凌隐宝堂中医诊所诊疗系统（LYBTZYZS）是基于.NET 8的企业级中医诊所管理系统，采用三层架构设计，实现前后端分离、模块化开发的现代化架构。

### 核心技术栈
- **后端**：.NET 8, ASP.NET Core Web API, Entity Framework Core 8
- **前端**：WPF (.NET 8), Prism.DryIoc 9.0, Refit
- **数据库**：SQL Server
- **认证**：JWT Bearer Token

### 架构目标
1. **模块自治**：每个业务模块独立完整，可独立开发测试
2. **职责清晰**：三层架构边界明确，各司其职
3. **统一规范**：命名、组织、交互遵循统一标准
4. **高效协作**：通过清晰的契约实现团队高效协作

---

## 三层架构设计

### 整体架构图

```
┌─────────────────────────────────────────────────────────┐
│                     LYBTZYZS Solution                    │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │    Server    │  │    Shared    │  │    Client    │  │
│  │   (后端)     │◄─┤   (契约层)   ├─►│   (前端)     │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ▲                  ▲                  ▲         │
│         │                  │                  │         │
│    业务逻辑处理        数据契约定义      用户界面展示   │
│    数据持久化          接口规范          交互处理      │
│    API暴露            工具方法          状态管理      │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Layer 1: Server层（后端服务）

#### 目录结构
```
src/Server/
├── Core/                          # 核心领域层
│   ├── LYBT.Models/              # 领域模型
│   │   ├── Entities/            # 实体定义
│   │   └── ValueObjects/        # 值对象
│   └── LYBT.Infrastructure/      # 基础设施
│       ├── Data/                # 数据上下文
│       └── Migrations/          # 数据库迁移
│
├── Modules/                       # 业务模块层
│   ├── LYBT.Module.Users/       # 用户模块
│   │   ├── Controllers/         # API控制器
│   │   ├── Services/           # 业务服务
│   │   │   ├── IUserService.cs
│   │   │   └── UserService.cs
│   │   ├── Repositories/        # 数据仓库
│   │   │   ├── IUserRepository.cs
│   │   │   └── UserRepository.cs
│   │   └── Mapping/            # 对象映射
│   │       └── UserMappingProfile.cs
│   │
│   ├── LYBT.Module.Patients/    # 患者模块
│   ├── LYBT.Module.Consultation/# 看诊模块
│   ├── LYBT.Module.Prescriptions/# 处方模块
│   ├── LYBT.Module.Herbs/       # 药材模块
│   ├── LYBT.Module.Formula/     # 验方模块
│   ├── LYBT.Module.MedicalCase/ # 病历模块
│   └── LYBT.Module.Auth/        # 认证模块
│
├── Services/                      # 跨模块服务
│   └── LYBT.Services.Common/    # 通用服务
│       ├── CacheService.cs
│       └── EmailService.cs
│
└── WebAPI/                        # API主机
    └── LYBT.WebAPI/              # 启动项目
        ├── Program.cs
        ├── Startup.cs
        └── appsettings.json
```

#### Server层职责定义

| 组件 | 职责 | 禁止事项 |
|------|------|----------|
| Controllers | 路由定义、请求验证、响应封装 | 包含业务逻辑 |
| Services | 业务逻辑处理、事务管理 | 直接访问数据库 |
| Repositories | 数据访问、CRUD操作 | 包含业务逻辑 |
| Models | 领域实体定义 | 包含数据访问逻辑 |
| Mapping | Model与DTO转换 | 包含业务逻辑 |

### Layer 2: Shared层（共享契约）

#### 目录结构
```
src/Shared/
├── LYBT.Shared.Models/           # 共享模型
│   ├── Contracts/               # 数据契约
│   │   ├── Users/              # 用户相关DTO
│   │   │   ├── UserDto.cs
│   │   │   ├── UserCreateDto.cs
│   │   │   ├── UserUpdateDto.cs
│   │   │   └── UserQueryDto.cs
│   │   ├── Patients/           # 患者相关DTO
│   │   ├── Consultation/       # 看诊相关DTO
│   │   ├── Prescriptions/      # 处方相关DTO
│   │   └── Common/             # 通用DTO
│   │       ├── ApiResponse.cs  # API响应包装
│   │       ├── PagedResult.cs  # 分页结果
│   │       └── ServiceResult.cs# 服务结果
│   │
│   ├── Core/                    # 核心模型
│   │   ├── BaseEntity.cs
│   │   └── BaseUser.cs
│   │
│   └── Enums/                   # 共享枚举
│       ├── UserRole.cs         # 用户角色
│       ├── CommonStatus.cs     # 通用状态
│       └── Gender.cs           # 性别
│
├── LYBT.Shared.Interfaces/       # 共享接口
│   └── Services/               # 业务服务接口契约
│       ├── IUserService.cs           # 用户业务服务接口
│       ├── IPatientService.cs        # 患者业务服务接口
│       ├── IConsultationService.cs   # 看诊业务服务接口
│       ├── IPrescriptionService.cs   # 处方业务服务接口
│       ├── IHerbService.cs           # 药材业务服务接口
│       ├── IFormulaService.cs        # 验方业务服务接口
│       ├── IMedicalCaseService.cs    # 病历业务服务接口
│       ├── IAuthenticationService.cs # 认证基础服务接口
│       └── ICacheService.cs          # 缓存基础服务接口
│
└── LYBT.Shared.Utilities/        # 共享工具
    ├── Extensions/             # 扩展方法
    │   ├── StringExtensions.cs
    │   └── DateTimeExtensions.cs
    └── Helpers/                # 辅助工具
        ├── ValidationHelper.cs
        └── SecurityHelper.cs
```

#### Shared层设计原则

| 原则 | 说明 | 示例 |
|------|------|------|
| 契约优先 | 定义清晰的数据契约 | UserDto定义所有字段 |
| 技术无关 | 不依赖特定技术框架 | 不引用EF Core |
| 可序列化 | 所有DTO可JSON序列化 | 使用基本数据类型 |
| 版本兼容 | 保持向后兼容 | 新增字段使用可空类型 |

### Layer 3: Client层（客户端应用）

#### 目录结构
```
src/Client/Desktop/
├── Core/                          # 基础设施层
│   ├── Interfaces/              # 基础接口
│   │   └── Services/           # 服务接口
│   │       ├── IAuthenticationService.cs
│   │       ├── ICacheService.cs
│   │       └── INavigationService.cs
│   │
│   ├── Services/                # 基础服务
│   │   ├── AuthenticationService.cs
│   │   ├── CacheService.cs
│   │   └── NavigationService.cs
│   │
│   ├── Models/                  # 客户端模型
│   │   ├── Users/
│   │   │   └── UserInfo.cs    # UI数据模型
│   │   ├── Patients/
│   │   │   └── PatientInfo.cs
│   │   └── Cache/
│   │       └── CachePolicy.cs
│   │
│   └── ViewModels/
│       └── Base/               # 基础ViewModels
│           ├── BaseViewModel.cs
│           └── DialogViewModel.cs
│
├── Modules/                       # 业务模块层
│   ├── Users/                   # 用户模块
│   │   ├── Api/               # API客户端
│   │   │   └── IUserApi.cs   # Refit接口
│   │   ├── Services/          # 模块服务
│   │   │   ├── UserModule.cs
│   │   │   └── Interfaces/
│   │   │       └── IUserModule.cs
│   │   ├── ViewModels/        # 视图模型
│   │   │   ├── UserManagementViewModel.cs
│   │   │   └── UserAddEditDialogViewModel.cs
│   │   ├── Views/             # 视图
│   │   │   ├── UserManagementView.xaml
│   │   │   └── UserAddEditDialog.xaml
│   │   └── UsersModule.cs     # Prism模块
│   │
│   ├── Patients/                # 患者模块
│   │   ├── Api/
│   │   │   └── IPatientApi.cs
│   │   ├── Services/
│   │   │   └── PatientModule.cs
│   │   ├── Coordinators/      # 数据协调
│   │   │   └── PatientCoordinator.cs
│   │   ├── ViewModels/
│   │   └── Views/
│   │
│   └── [其他业务模块...]
│
├── Services/                      # 基础设施服务（禁止业务代码）
│   ├── TokenManager.cs         # 令牌管理
│   ├── ApiHealthMonitor.cs     # API健康监控
│   └── ErrorHandlingService.cs # 错误处理
│
├── Infrastructure/                # 基础设施支持
│   └── Http/
│       ├── AuthHeaderHandler.cs
│       └── RetryPolicyHandler.cs
│
├── Shell/                         # 应用程序壳
│   ├── App.xaml.cs            # 应用入口
│   ├── MainWindow.xaml        # 主窗口
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs # DI配置
│
└── Workbenches/                   # 工作台
    └── ConsultationWorkbench/   # 统一工作台
        ├── ViewModels/
        └── Views/
```

#### Client层模块结构规范

每个业务模块必须遵循以下结构：

```
ModuleName/
├── Api/                          # API客户端层（必需）
│   └── IModuleNameApi.cs       # HTTP API接口定义
├── Services/                     # 服务层（必需）
│   ├── ModuleNameModule.cs     # 模块业务逻辑
│   └── Interfaces/
│       └── IModuleNameModule.cs
├── ViewModels/                   # 视图模型层（必需）
├── Views/                        # 视图层（必需）
├── Models/                       # 模块特定模型（可选）
├── Coordinators/                 # 协调器（可选）
└── ModuleNameModule.cs           # Prism模块注册（必需）
```

---

## 命名规范标准

### 通用命名原则

| 原则 | 说明 | 示例 |
|------|------|------|
| 简洁性 | 避免冗余词汇 | `IUserApi` 而非 `IUserApiService` |
| 一致性 | 同类使用同样模式 | 所有API接口都用 `IXxxApi` |
| 可读性 | 名称自解释 | `PatientInfo` 清晰表示患者信息 |
| 标准化 | 遵循.NET规范 | PascalCase用于类名和方法 |

### Server层命名规范

| 组件类型 | 命名规范 | 示例 |
|----------|----------|------|
| Entity模型 | `XxxModel` | `UserModel`, `PatientModel` |
| 控制器 | `XxxController` | `UserController` |
| Model服务接口 | `IXxxModelService` | `IUserModelService` (Server层业务服务) |
| Model服务实现 | `XxxModelService` | `UserModelService` (Server层业务服务) |
| 仓库接口 | `IXxxRepository` | `IUserRepository` |
| 仓库实现 | `XxxRepository` | `UserRepository` |

### Shared层命名规范

| 组件类型 | 命名规范 | 示例 |
|----------|----------|------|
| DTO模型 | `XxxDto` | `UserDto` |
| 创建DTO | `XxxCreateDto` | `UserCreateDto` |
| 更新DTO | `XxxUpdateDto` | `UserUpdateDto` |
| 查询DTO | `XxxQueryDto` | `UserQueryDto` |
| 分页查询 | `XxxPagedQueryDto` | `UserPagedQueryDto` |
| 响应包装 | `ApiResponse<T>` | `ApiResponse<UserDto>` |
| 服务结果 | `ServiceResult<T>` | `ServiceResult<UserDto>` |

### Client层命名规范

| 组件类型 | 命名规范 | 示例 |
|----------|----------|------|
| Info模型 | `XxxInfo` | `UserInfo`, `PatientInfo` |
| API接口 | `IXxxApi` | `IUserApi` (不是IUserApiService) |
| Info服务接口 | `IXxxInfoService` | `IUserInfoService` (Client层业务服务) |
| Info服务实现 | `XxxInfoService` | `UserInfoService` (Client层业务服务) |
| 模块服务 | `XxxModule` | `UserModule` (模块注册和协调) |
| 模块接口 | `IXxxModule` | `IUserModule` |
| 视图模型 | `XxxViewModel` | `UserManagementViewModel` |
| 对话框VM | `XxxDialogViewModel` | `UserAddEditDialogViewModel` |
| 视图 | `XxxView` | `UserManagementView` |
| 对话框 | `XxxDialog` | `UserAddEditDialog` |
| 协调器 | `XxxCoordinator` | `PatientCoordinator` |

### 文件命名规范

| 文件类型 | 命名规范 | 示例 |
|----------|----------|------|
| 接口文件 | `IXxx.cs` | `IUserApi.cs` |
| 实现文件 | `Xxx.cs` | `UserApi.cs` |
| 视图文件 | `XxxView.xaml` | `UserManagementView.xaml` |
| 代码后置 | `XxxView.xaml.cs` | `UserManagementView.xaml.cs` |
| 项目文件 | `LYBT.Module.Xxx.csproj` | `LYBT.Module.Users.csproj` |

### 变量命名规范

| 变量类型 | 命名规范 | 示例 |
|----------|----------|------|
| 私有字段 | `_camelCase` | `_userService`, `_logger` |
| 公共属性 | `PascalCase` | `CurrentUser`, `IsLoggedIn` |
| 方法参数 | `camelCase` | `userId`, `loginRequest` |
| 局部变量 | `camelCase` | `user`, `result` |
| 常量 | `PascalCase` | `DefaultTimeout`, `MaxRetries` |
| 集合 | `xxxList` 或 `xxxs` | `users`, `patientList` |

---

## 模块化设计原则

### 核心设计原则

#### 1. 模块自治原则
- **定义**：每个模块是独立的功能单元
- **要求**：
  - 模块内高内聚
  - 模块间低耦合
  - 可独立编译测试
  - 职责边界清晰

#### 2. 依赖倒置原则
- **定义**：依赖抽象而非具体实现
- **实践**：
  - 通过接口定义契约
  - 使用依赖注入解耦
  - 避免直接实例化

#### 3. 单一职责原则
- **定义**：每个类只有一个改变的理由
- **应用**：
  - Controller只做路由
  - Service处理业务逻辑
  - Repository处理数据访问
  - ViewModel处理UI逻辑

### 模块间通信规范

#### 允许的通信方式

1. **通过依赖注入的服务接口**
```csharp
public class UserModule : IUserModule
{
    private readonly IPatientModule _patientModule;
    
    public UserModule(IPatientModule patientModule)
    {
        _patientModule = patientModule;
    }
}
```

2. **通过事件聚合器**
```csharp
// 发布事件
_eventAggregator.GetEvent<UserUpdatedEvent>().Publish(user);

// 订阅事件
_eventAggregator.GetEvent<UserUpdatedEvent>().Subscribe(OnUserUpdated);
```

3. **通过共享的Coordinator**
```csharp
public class DataCoordinator : IDataCoordinator
{
    // 协调跨模块数据
}
```

#### 禁止的通信方式

1. ❌ 直接引用其他模块的内部类
2. ❌ 跨模块访问私有成员
3. ❌ 绕过接口直接调用
4. ❌ 使用静态类共享状态

### 模块职责边界

| 模块 | 核心职责 | 对外接口 | 依赖模块 |
|------|----------|----------|----------|
| Users | 用户管理 | IUserModule, IUserApi | Auth |
| Patients | 患者档案 | IPatientModule, IPatientApi | None |
| Consultation | 看诊流程 | IConsultationModule | Patients, Prescriptions |
| Prescriptions | 处方管理 | IPrescriptionModule | Herbs, Formula |
| Herbs | 药材信息 | IHerbModule | None |
| Formula | 验方模板 | IFormulaModule | Herbs |
| MedicalCase | 病历管理 | IMedicalCaseModule | Patients, Consultation |
| Auth | 认证授权 | IAuthModule | Users |

---

## 实施指南

### Phase 1: 准备阶段

#### 1.1 架构评估
- [ ] 分析现有代码结构
- [ ] 识别不符合规范的部分
- [ ] 评估重构工作量
- [ ] 制定重构计划

#### 1.2 团队培训
- [ ] 架构规范培训
- [ ] 命名规范培训
- [ ] 开发流程培训
- [ ] 工具使用培训

### Phase 2: 重构实施

#### 2.1 API接口重组（P0优先级）

**目标**：将业务API接口移至对应模块

**步骤**：
1. 在每个模块创建Api目录
2. 将Services/Interfaces中的业务API移至模块
3. 重命名接口（IXxxApiService → IXxxApi）
4. 更新所有引用
5. 删除原有文件

**示例**：
```bash
# 移动前
src/Client/Desktop/Services/Interfaces/IUserApiService.cs

# 移动后
src/Client/Desktop/Modules/Users/Api/IUserApi.cs
```

#### 2.2 模块服务规范化（P1优先级）

**目标**：统一模块服务命名

**步骤**：
1. 重命名模块服务接口（IXxxModuleService → IXxxModule）
2. 重命名模块服务实现（XxxModuleService → XxxModule）
3. 更新依赖注入配置
4. 更新所有引用

**示例**：
```csharp
// 重构前
public interface IUserModuleService { }
public class UserModuleService : IUserModuleService { }

// 重构后
public interface IUserModule { }
public class UserModule : IUserModule { }
```

#### 2.3 依赖注入更新（P1优先级）

**目标**：更新DI容器配置

**修改文件**：`Shell/Extensions/ServiceCollectionExtensions.cs`

```csharp
// 注册API客户端
containerRegistry.RegisterRefit<IUserApi>(apiUrl);

// 注册模块服务
containerRegistry.Register<IUserModule, UserModule>();
```

### Phase 3: 验证阶段

#### 3.1 编译验证
```bash
# 编译整个解决方案
dotnet build LYBT.All.sln

# 运行单元测试
dotnet test
```

#### 3.2 运行时验证
- [ ] 启动应用程序
- [ ] 测试主要功能
- [ ] 验证模块加载
- [ ] 检查依赖注入

### Phase 4: 优化阶段

#### 4.1 代码清理
- [ ] 删除冗余代码
- [ ] 优化引用
- [ ] 格式化代码
- [ ] 更新注释

#### 4.2 文档更新
- [ ] 更新架构文档
- [ ] 更新API文档
- [ ] 更新开发指南
- [ ] 更新部署文档

---

## 架构验证清单

### 整体架构验证

#### 三层分离验证
- [ ] Server层只包含后端逻辑
- [ ] Shared层只包含契约定义
- [ ] Client层只包含前端逻辑
- [ ] 无跨层直接依赖

#### 模块独立性验证
- [ ] 每个模块可独立编译
- [ ] 模块间通过接口通信
- [ ] 无循环依赖
- [ ] 职责边界清晰

### Server层验证

#### 结构验证
- [ ] Controllers只包含路由逻辑
- [ ] Services包含业务逻辑
- [ ] Repositories只做数据访问
- [ ] 正确使用AutoMapper

#### 命名验证
- [ ] Entity使用XxxModel命名
- [ ] 服务使用XxxService命名
- [ ] 仓库使用XxxRepository命名
- [ ] 控制器使用XxxController命名

### Shared层验证

#### 内容验证
- [ ] 只包含接口和DTO定义
- [ ] 无具体实现代码
- [ ] 无技术框架依赖
- [ ] 所有DTO可序列化

#### 命名验证
- [ ] DTO使用XxxDto命名
- [ ] 枚举定义清晰
- [ ] 接口命名规范
- [ ] 工具类命名合理

### Client层验证

#### 模块结构验证
- [ ] 每个模块有Api目录
- [ ] API接口在模块内定义
- [ ] 使用IXxxApi命名（不是IXxxApiService）
- [ ] 使用XxxModule命名（不是XxxModuleService）

#### 架构清洁度验证
- [ ] Services目录无业务代码
- [ ] Core层无业务逻辑
- [ ] 使用Info模型进行UI绑定
- [ ] 遵循MVVM模式

### 命名规范验证

#### 接口命名
- [ ] API接口：IXxxApi ✓
- [ ] 模块服务：IXxxModule ✓
- [ ] 基础服务：IXxxService ✓

#### 实现命名
- [ ] API实现：XxxApi ✓
- [ ] 模块实现：XxxModule ✓
- [ ] 服务实现：XxxService ✓

#### 文件命名
- [ ] 文件名与类名一致
- [ ] 使用PascalCase
- [ ] 扩展名正确

### 依赖关系验证

#### 正向依赖
- [ ] Client → Shared ✓
- [ ] Server → Shared ✓
- [ ] Module → Core ✓

#### 禁止依赖
- [ ] Client → Server ✗
- [ ] Server → Client ✗
- [ ] Core → Module ✗

---

## 附录

### A. 常见问题与解决方案

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 编译错误：找不到类型 | 命名空间变更 | 更新using语句 |
| DI解析失败 | 接口未注册 | 检查ServiceCollectionExtensions |
| API调用失败 | 接口定义不匹配 | 验证Refit接口与API一致 |
| 模块加载失败 | 模块注册错误 | 检查ModuleCatalog配置 |

### B. 重构检查清单模板

```markdown
## 模块重构检查清单 - [模块名]

### 准备阶段
- [ ] 备份现有代码
- [ ] 创建重构分支
- [ ] 分析依赖关系

### 执行阶段
- [ ] 创建Api目录
- [ ] 移动API接口
- [ ] 重命名接口和实现
- [ ] 更新引用
- [ ] 更新DI配置

### 验证阶段
- [ ] 编译通过
- [ ] 单元测试通过
- [ ] 功能测试通过
- [ ] 代码审查通过

### 完成阶段
- [ ] 删除冗余文件
- [ ] 提交代码
- [ ] 更新文档
- [ ] 合并到主分支
```

### C. Git提交规范

```bash
# 功能添加
feat: 添加用户模块API接口

# 重构
refactor: 重构用户模块符合UltraThink规范

# 修复
fix: 修复用户API接口命名问题

# 文档
docs: 更新UltraThink架构文档

# 样式
style: 格式化代码符合规范

# 测试
test: 添加用户模块单元测试

# 构建
chore: 更新依赖注入配置
```

### D. 相关文档链接

- [开发规范](../development/development-guidelines.md)
- [API设计指南](../api/api-design-guide.md)
- [部署文档](../deployment/deployment-guide.md)
- [测试指南](../testing/testing-guide.md)

---

## 版本历史

| 版本 | 日期 | 作者 | 说明 |
|------|------|------|------|
| 1.0 | 2025-01-17 | UltraThink架构组 | 初始版本，完整架构规范 |

---

**文档维护**：本文档由UltraThink架构组维护，如有问题请联系架构组。