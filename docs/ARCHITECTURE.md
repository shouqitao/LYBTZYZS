# 凌隐宝堂系统架构文档

**当前版本**: v2.4
**最后更新**: 2025-10-12
**状态**: Living Document

---

## 📋 文档说明

本文档是系统架构的**唯一权威来源**（Single Source of Truth - SSOT）。

**核心原则**：
- ✅ 所有架构变更必须在本文档中体现
- ✅ 本文档优先级高于任何其他架构文档
- ✅ 旧版架构文档已归档至 `docs/reports/archive/architecture/`
- ✅ 架构决策以ADR形式记录在 Part V

**适用范围**：
- Server端：ASP.NET Core WebAPI + EF Core
- Desktop端：WPF + Prism MVVM
- Shared层：DTO、接口、领域模型

---

## 📖 目录

### Part I: 整体架构
- [1.1 架构愿景](#11-架构愿景)
- [1.2 分层架构](#12-分层架构)
- [1.3 技术栈](#13-技术栈)
- [1.4 架构约束](#14-架构约束)

### Part II: Server端架构 (Current: v1.4)
- [2.1 三层架构](#21-三层架构)
- [2.2 目录结构](#22-目录结构)
- [2.3 Service接口设计](#23-service接口设计)
- [2.4 Repository层设计](#24-repository层设计)
- [2.5 DTO设计规范](#25-dto设计规范)
- [2.6 服务注册模式](#26-服务注册模式)

### Part III: Desktop端架构 (Current: v2.4)
- [3.1 模块化架构](#31-模块化架构)
- [3.2 目录结构](#32-目录结构)
- [3.3 ViewModel设计](#33-viewmodel设计)
- [3.4 Repository层设计](#34-repository层设计)
- [3.5 组件化架构](#35-组件化架构)
- [3.6 View层设计](#36-view层设计)

### Part IV: 共享层架构
- [4.1 Shared.Models架构](#41-sharedmodels架构)
- [4.2 Shared.Interfaces架构](#42-sharedinterfaces架构)
- [4.3 DTO设计原则](#43-dto设计原则)

### Part V: 架构决策记录 (ADR)
- [5.1 ADR-001: 禁止CQRS模式](#51-adr-001-禁止cqrs模式)
- [5.2 ADR-002: Desktop移除Service层](#52-adr-002-desktop移除service层)
- [5.3 ADR-003: Repository接口位置标准](#53-adr-003-repository接口位置标准)
- [5.4 ADR-004: Service接口统一设计](#54-adr-004-service接口统一设计)

### Part VI: 架构演进
- [6.1 版本历史](#61-版本历史)
- [6.2 迁移指南](#62-迁移指南)
- [6.3 质量检查清单](#63-质量检查清单)

---

## Part I: 整体架构

### 1.1 架构愿景

凌隐宝堂系统采用**经典分层架构 + 领域驱动设计（DDD Lite）**，面向小型中医诊所（并发 < 10人）的实际需求，避免过度工程。

**核心设计目标**：
- ✅ **简单清晰**：三层架构足以满足业务需求
- ✅ **易于维护**：统一的模式与约定，降低学习曲线
- ✅ **模块化**：业务模块垂直切分，职责独立
- ✅ **可测试性**：依赖注入 + 接口抽象，支持单元测试

### 1.2 分层架构

```
┌─────────────────────────────────────────────────────┐
│              Desktop Client (WPF)                   │
│         View → ViewModel → Repository               │
└────────────────────┬────────────────────────────────┘
                     │ HTTP/REST
┌────────────────────▼────────────────────────────────┐
│                Server (ASP.NET Core)                │
│      Controller → Service → Repository → DB        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│             Shared Kernel (Cross-Cutting)           │
│    DTO (Contracts) + Interfaces + Domain Models    │
└─────────────────────────────────────────────────────┘
```

**层次职责**：
- **Presentation Layer (Desktop)**: UI逻辑、用户交互、导航、状态管理
- **Application Layer (Server Controller)**: HTTP请求处理、路由、认证授权
- **Business Layer (Server Service)**: 业务逻辑、事务控制、数据转换
- **Data Access Layer (Repository)**: 数据访问、持久化、查询封装
- **Shared Kernel**: 跨层共享的契约与模型

### 1.3 技术栈

**Server端**：
- ASP.NET Core 8.0 - Web框架
- Entity Framework Core 8.0 - ORM
- AutoMapper - 对象映射
- FluentValidation - 验证框架
- Serilog - 日志框架

**Desktop端**：
- WPF (.NET 8.0) - UI框架
- Prism 9.0 - MVVM框架
- MaterialDesignThemes - UI组件库
- CommunityToolkit.Mvvm - MVVM辅助库

**Shared层**：
- .NET Standard 2.1 - 跨平台兼容性
- System.ComponentModel.DataAnnotations - 基础验证

### 1.4 架构约束

**技术黑名单**（严格禁止）：
- ❌ **Redis** - 小型系统无需分布式缓存
- ❌ **CQRS** - 读写分离过度设计
- ❌ **Docker** - 部署复杂度高，MVP阶段不需要
- ❌ **GraphQL** - REST API已足够
- ❌ **MediatR** - 事件总线过度设计
- ❌ **RabbitMQ/Kafka** - 无需消息队列

**架构强制规则**：
- ✅ 三层架构（Controller → Service → Repository）
- ✅ 依赖注入（构造函数注入，禁止ServiceLocator）
- ✅ 异步优先（I/O操作必须 async/await）
- ✅ DTO分离（CreateDto/UpdateDto/Dto场景分离）
- ✅ 单元测试（核心业务逻辑必须覆盖）

---

## Part II: Server端架构 (Current: v1.4)

### 2.1 三层架构

所有Server模块必须遵循以下三层架构：

```
Controller → Service → Repository → Database
```

- **Controller层**: 负责HTTP请求处理、路由、参数验证
- **Service层**: 负责业务逻辑实现、事务控制、数据转换
- **Repository层**: 负责数据访问、查询封装、持久化操作

#### 2.1.1 禁止CQRS模式

**严格禁止**在Server模块中使用CQRS（Command Query Responsibility Segregation）模式，包括：

- ❌ 禁止拆分 `IXxxQueryService` 和 `IXxxBusinessService`
- ❌ 禁止拆分 `XxxQueryService` 和 `XxxBusinessService`
- ✅ 必须使用单一 `IXxxService` 接口和 `XxxService` 实现

**违规示例**（禁止）：
```csharp
// ❌ 错误：双层Service接口
services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
services.AddScoped<IConsultationBusinessService, ConsultationBusinessService>();
```

**正确示例**：
```csharp
// ✅ 正确：单一Service接口
services.AddScoped<LYBT.Shared.Interfaces.Services.IConsultationService, ConsultationService>();
```

**禁止理由**：
- 小型诊所系统无需CQRS复杂性
- 三层架构已足够满足业务需求
- 避免过度工程导致的维护负担

### 2.2 目录结构

每个Server模块必须遵循以下目录结构：

```
LYBT.Module.Xxx/
├── Controllers/          # （可选）API控制器
├── Entities/            # （已废弃）实体定义已迁移至LYBT.Entities
├── Interfaces/          # 模块内部接口（仅Repository接口）
│   └── IXxxRepository.cs
├── Mapping/             # AutoMapper映射配置
│   └── XxxMappingProfile.cs
├── Options/             # 模块配置选项
│   └── XxxModuleOptions.cs
├── Repositories/        # 仓储实现
│   └── XxxRepository.cs
├── Services/            # 业务服务实现
│   └── XxxService.cs
├── Validators/          # DTO验证器
│   ├── XxxCreateDtoValidator.cs
│   └── XxxUpdateDtoValidator.cs
└── XxxModule.cs         # 模块服务注册
```

**目录职责说明**：

- **Interfaces/** 目录：仅存放Repository接口，Service接口已统一迁移至 `LYBT.Shared.Interfaces.Services`
- **Repositories/** 目录：必须存放所有Repository实现类，禁止放置在其他目录（如Services/）

### 2.3 Service接口设计

#### 2.3.1 Service接口统一位置

所有Service接口必须定义在 `LYBT.Shared.Interfaces.Services` 命名空间：

```csharp
// 文件位置: src/Shared/LYBT.Shared.Interfaces/Services/IConsultationService.cs
namespace LYBT.Shared.Interfaces.Services
{
    public interface IConsultationService
    {
        // 查询操作
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(...);
        Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);

        // 业务操作
        Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto);
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
```

**优势**：
- ✅ Desktop端和Server端共享相同接口契约
- ✅ 避免接口重复定义
- ✅ 简化依赖注入配置

#### 2.3.2 Service接口设计原则

1. **最小接口原则（ISP）**：每个Service接口方法数控制在 **6-12个之间**
   - 下限（6方法）：标准CRUD（3）+ 查询（2-3）
   - 上限（12方法）：标准CRUD + 查询 + 业务操作（≤5）

2. **单一职责原则（SRP）**：每个Service接口只负责**一个业务实体**的核心操作

3. **YAGNI原则**：MVP阶段**优先实现核心功能**，非必需功能延后

#### 2.3.3 标准Service接口结构

```csharp
namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// {Entity}业务服务接口
    /// </summary>
    public interface I{Entity}Service
    {
        #region 查询操作 (2-4 methods)

        /// <summary>
        /// 分页查询{Entity}列表
        /// </summary>
        Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null
        );

        /// <summary>
        /// 根据ID查询{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id);

        #endregion

        #region CRUD 操作 (3 methods)

        /// <summary>
        /// 创建{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> CreateAsync(
            {Entity}CreateDto dto,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// 更新{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> UpdateAsync(
            Guid id,
            {Entity}UpdateDto dto,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// 删除{Entity}（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        #endregion

        #region 业务操作 (0-5 methods)

        // Entity-specific business methods
        // 示例：
        // Task<ServiceResult> DisableAsync(Guid id);
        // Task<ServiceResult> EnableAsync(Guid id);

        #endregion
    }
}
```

#### 2.3.4 命名约定

**方法命名**：统一使用 **动词 + Async** 格式

| 操作类型 | 标准命名 | 禁止命名 |
|---------|---------|---------|
| 创建 | `CreateAsync` | ❌ CreateUserAsync, AddAsync |
| 更新 | `UpdateAsync` | ❌ UpdateUserAsync, ModifyAsync |
| 删除 | `DeleteAsync` | ❌ DeleteUserAsync, RemoveAsync |
| 查询单个 | `GetByIdAsync` | ❌ FindByIdAsync, GetAsync |
| 分页查询 | `GetPagedAsync` | ❌ GetAllAsync, QueryAsync |

**参数命名**：

| 参数类型 | 标准命名 | 类型 | 示例 |
|---------|---------|------|------|
| 主键 | `id` | `Guid` | `GetByIdAsync(Guid id)` |
| 创建DTO | `dto` | `{Entity}CreateDto` | `CreateAsync(UserCreateDto dto)` |
| 更新DTO | `dto` | `{Entity}UpdateDto` | `UpdateAsync(Guid id, UserUpdateDto dto)` |
| 分页参数 | `page`, `pageSize` | `int` | `GetPagedAsync(int page, int pageSize)` |
| 关键词 | `keyword` | `string?` | `GetPagedAsync(..., string? keyword = null)` |

**返回类型**：

| 返回场景 | 标准返回类型 | 禁止返回类型 |
|---------|------------|------------|
| 有数据返回 | `Task<ServiceResult<T>>` | ❌ `Task<T>`, `Task<bool>` |
| 无数据返回 | `Task<ServiceResult>` | ❌ `Task<ServiceResult<bool>>`, `Task` |
| 分页数据 | `Task<ServiceResult<PagedResult<T>>>` | ❌ `Task<ServiceResult<List<T>>>` |

### 2.4 Repository层设计

Repository接口继续保留在各模块的 `Interfaces/` 目录：

```csharp
// 文件位置: src/Server/Modules/LYBT.Module.Xxx/Interfaces/IXxxRepository.cs
namespace LYBT.Module.Xxx.Interfaces
{
    public interface IXxxRepository
    {
        Task<XxxEntity> GetByIdAsync(Guid id);
        Task<List<XxxEntity>> GetAllAsync();
        Task<XxxEntity> AddAsync(XxxEntity entity);
        Task UpdateAsync(XxxEntity entity);
        Task DeleteAsync(Guid id);
    }
}
```

### 2.5 DTO设计规范

**📚 权威参考**: 请参阅 [DTO 设计原则](architecture/dto-design-principles.md) 获取完整的DTO设计规范。

**Server端DTO使用要点**:

1. **DTO定义位置**:
   - ✅ 所有DTO必须定义在 `Shared.Models.Contracts.*`
   - ❌ 禁止在Server Module中重复定义DTO

2. **场景分离原则**:
   ```csharp
   // 创建场景 - CreateDto
   public class ConsultationCreateDto
   {
       public Guid MedicalCaseId { get; set; }  // 必需,非 nullable
       public string ChiefComplaint { get; set; } = string.Empty;
       // 不包含 Id, CreatedAt 等系统字段
   }

   // 更新场景 - UpdateDto
   public class ConsultationUpdateDto
   {
       public string? ChiefComplaint { get; set; }  // 可选,nullable
       public string? TCMDiagnosis { get; set; }
       // 不包含 Id, MedicalCaseId, CreatedAt 等
   }

   // 展示场景 - Dto
   public class ConsultationDto
   {
       public Guid Id { get; set; }
       public string PatientName { get; set; } = string.Empty;  // 扁平化
       public string DoctorName { get; set; } = string.Empty;   // 扁平化
       // 包含展示所需的所有字段
   }
   ```

3. **AutoMapper映射**:
   - Mapping Profile 必须放在 `Mapping/` 目录
   - Service 层使用 `_mapper.Map<T>()` 进行转换

4. **验证规范**:
   - 简单验证: Data Annotations
   - 复杂验证: FluentValidation (放在 `Validators/` 目录)

### 2.6 服务注册模式

每个模块的 `XxxModule.cs` 必须遵循以下注册顺序和模式：

```csharp
namespace LYBT.Module.Xxx
{
    public static class XxxModule
    {
        public static IServiceCollection AddXxxModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. 注册仓储
            services.AddScoped<IXxxRepository, XxxRepository>();

            // 2. 注册服务实现类（统一使用Shared接口）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>();

            // 3. 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>();

            // 4. AutoMapper - 无需显式注册（已在UnifiedServiceRegistration中集中注册）

            // 5. 注册模块特定配置（可选）
            services.AddOptions<XxxModuleOptions>()
                .Bind(configuration.GetSection("Modules:Xxx"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
```

**关键点**：
- ✅ 使用 `LYBT.Shared.Interfaces.Services.IXxxService` 注册
- ❌ 禁止注册模块内部Query/Business接口
- ✅ 使用 `AddValidatorsFromAssemblyContaining<T>()` 自动扫描
- ✅ AutoMapper已在 `UnifiedServiceRegistration.cs` 中集中注册，无需显式注册

---

## Part III: Desktop端架构 (Current: v2.4)

### 3.1 模块化架构

```
┌─────────────────────────────────────────┐
│           View (XAML)                   │
│     用户界面、数据绑定、样式             │
└───────────────┬─────────────────────────┘
                │ Binding
┌───────────────▼─────────────────────────┐
│         ViewModel                       │
│   UI逻辑、命令、属性、状态管理           │
│   异常处理（ViewModelBase）              │
└───────────────┬─────────────────────────┘
                │ 直接调用（无Service层）
┌───────────────▼─────────────────────────┐
│        Repository                       │
│   数据访问、HTTP调用、返回裸类型          │
└───────────────┬─────────────────────────┘
                │ HTTP
┌───────────────▼─────────────────────────┐
│         WebAPI (Server)                 │
│   业务逻辑、数据持久化                   │
└─────────────────────────────────────────┘
```

**架构变更说明（v2.1）**：
- ❌ **移除Service层**：Desktop端不应重复Server端业务逻辑
- ✅ **ViewModel直调Repository**：简化调用链，提升性能
- ✅ **Repository返回裸类型**：直接返回DTO或PagedResult，异常通过抛出处理
- ✅ **异常处理在UnifiedViewModelBase**：基类统一捕获Repository异常

**模块组织原则**：
- **模块 = 垂直切片**：每个模块包含 Models、ViewModels、Views、Repositories
- **职责独立**：每个模块拥有独立的数据访问层（Repositories）
- **水平分层**：技术基础设施（Foundation）、UI基础设施（Presentation）集中管理

### 3.2 目录结构

```
LYBT.Desktop.{ModuleName}/
├── Models/                      ✅ UI专用模型
│   ├── {Entity}Item.cs         (列表项模型)
│   ├── {Entity}ViewState.cs    (视图状态)
│   └── {Wizard}Step.cs         (向导步骤枚举)
│
├── ViewModels/                  ✅ 视图模型
│   ├── Components/              🆕 v2.4 组件目录
│   │   ├── {Entity}Calculator.cs       (计算组件)
│   │   ├── {Entity}Validator.cs        (验证组件)
│   │   ├── {Entity}CommandHandler.cs   (命令处理组件)
│   │   └── {Entity}DataManager.cs      (数据管理组件)
│   │
│   ├── {Entity}ManagementViewModel.cs  (列表管理)
│   ├── {Entity}DetailViewModel.cs      (详情查看)
│   ├── {Entity}CreateViewModel.cs      (创建)
│   ├── {Entity}EditViewModel.cs        (编辑)
│   └── {Action}DialogViewModel.cs      (对话框)
│
├── Views/                       ✅ XAML视图
│   ├── {Entity}ManagementView.xaml     (+ .xaml.cs)
│   ├── {Entity}DetailView.xaml         (+ .xaml.cs)
│   └── {Action}Dialog.xaml             (+ .xaml.cs)
│
├── Interfaces/                  🆕 v2.2 模块接口目录
│   └── I{Entity}Repository.cs  (Repository接口)
│
├── Repositories/                🆕 模块独立数据访问层
│   └── {Entity}Repository.cs   (Repository实现)
│
├── {ModuleName}Module.cs        ✅ Prism模块注册
└── README.md                    ✅ 模块说明文档
```

**v2.0 关键变更**：
- 🆕 **Repositories/** 目录：每个模块拥有独立的数据访问层
- ❌ **Services/** 目录：已废弃，不再使用Service层

**v2.2 架构调整**：
- 🆕 **Interfaces/** 目录：Repository接口独立目录，对齐Server端标准
- ✅ **Repositories/** 目录：仅包含实现类，不再混合接口

**Core层目录结构**：

```
Desktop/Core/
├── Desktop.Foundation/          🆕 技术基础设施（Infrastructure Services）
│   ├── Security/               # 认证服务（AuthenticationService）
│   ├── Caching/                # 缓存服务（CacheService）
│   ├── Configuration/          # 配置服务（ConfigurationService）
│   ├── Http/                   # HTTP客户端管理（ApiClientManager）
│   ├── Diagnostics/            # 诊断服务
│   ├── ErrorHandling/          # 异常处理
│   ├── Performance/            # 性能监控
│   ├── Session/                # 会话管理
│   ├── Settings/               # 用户设置
│   └── HealthCheck/            # 健康检查
│
├── Desktop.Presentation/        🆕 UI基础设施
│   ├── Navigation/
│   ├── Notifications/
│   ├── Theming/
│   ├── UserExperience/
│   └── Print/
│
├── Desktop.Infrastructure/      ✅ 保留（通用接口与基类）
└── Desktop.Models/              ✅ 保留（共享模型）
```

### 3.3 ViewModel设计

#### 3.3.1 基类选择规则

| 场景 | 基类 | 示例 |
|------|------|------|
| 列表管理 | `UnifiedListViewModelBase<TDto>` | PatientManagementViewModel |
| 详情/单项 | `UnifiedViewModelBase` | PatientDetailViewModel |
| 对话框 | `UnifiedViewModelBase` | ConfirmDialogViewModel |

#### 3.3.2 构造函数依赖注入（强制标准，v2.0）

```csharp
/// <summary>
/// {Entity}{ViewType}ViewModel - {简要描述}
/// </summary>
public XxxViewModel(
    // 1️⃣ Repository依赖（必需，非null）
    IXxxRepository xxxRepository,

    // 2️⃣ 基类必需依赖
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,

    // 3️⃣ 可选依赖（末尾，使用 = null）
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager,
           sessionManager, userNotificationService)
{
    _xxxRepository = xxxRepository ?? throw new ArgumentNullException(nameof(xxxRepository));
}
```

**依赖顺序规则（v2.0）**：
1. Repository依赖优先（如 IPatientRepository）
2. 基类必需依赖（EventAggregator, LoggerFactory, RegionManager）
3. 可选依赖最后（SessionManager, NotificationService）

**v2.1 关键变更**：
- ❌ 不再注入 `IXxxService`（已废弃Server Service依赖）
- ✅ 直接注入 `IXxxRepository`（模块内数据访问层）
- ❌ 不再注入 `IMapper`（Repository直接返回DTO，无需映射）
- ⚠️ **重要**：禁止使用 `LYBT.Shared.Interfaces.Services.*` 命名空间（会导致DI容器解析失败）

#### 3.3.3 命令命名标准

| 命令类型 | 命名规则 | 示例 |
|---------|---------|------|
| CRUD | `{Action}Command` | `AddCommand`, `EditCommand`, `DeleteCommand`, `SaveCommand` |
| 导航 | `{Direction/Target}Command` | `BackCommand`, `NextCommand`, `GotoPatientCommand` |
| 刷新 | `RefreshCommand` / `LoadDataCommand` | `RefreshCommand` |
| 搜索 | `SearchCommand` / `ClearSearchCommand` | `SearchCommand` |

#### 3.3.4 属性命名标准

| 属性类型 | 命名规则 | 示例 |
|---------|---------|------|
| 数据集合 | `Items` | `Items` (列表项) |
| 当前选中 | `SelectedItem` / `CurrentItem` | `SelectedPatient`, `CurrentUser` |
| 状态标志 | `Is{State}` | `IsLoading`, `IsBusy`, `IsReadOnly` |
| 计数 | `{Noun}Count` / `Total{Noun}` | `ItemCount`, `TotalPages` |

### 3.4 Repository层设计

#### 3.4.0 Repository vs Infrastructure Service决策标准

**核心问题**：Desktop端什么时候用Repository，什么时候用Infrastructure Service？

**Repository模式核心特征**：

| 特征 | 说明 | 典型方法 |
|-----|------|---------|
| **集合式接口** | 把数据源当作内存集合操作 | GetAll(), GetById(id), Add(entity), Update(entity), Delete(id) |
| **封装数据访问** | 隐藏底层数据源细节（SQL/HTTP/文件） | 调用者不知道是数据库还是API |
| **返回领域对象** | 返回业务实体（Entity/DTO） | User, Patient, Herb等 |

**判断标准决策表**：

| 场景 | 是否Repository | 原因 | 应该用什么 |
|-----|---------------|------|----------|
| 患者管理 | ✅ 是 | CRUD集合操作，管理Patient领域对象 | PatientRepository |
| 用户管理 | ✅ 是 | CRUD集合操作，管理User领域对象 | UserRepository |
| 认证操作 | ❌ 否 | Login/Logout不是集合操作，返回Token | AuthenticationService |
| 缓存管理 | ❌ 否 | Set/Get操作，不是领域对象 | CacheService |
| 配置读取 | ❌ 否 | 读取配置文件，不是数据CRUD | ConfigurationService |
| 日志记录 | ❌ 否 | 单向写入，不是集合查询 | LoggingService |

**案例详解：认证服务为什么不用Repository？**

```csharp
// ✅ 正确：AuthenticationService（Foundation层）
public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> ValidateTokenAsync();
    Task<string> RefreshTokenAsync();
    string? GetCurrentToken();
}
```

**这些操作的特点**：
- ❌ 不是集合操作（没有GetAll, GetById, Add, Update, Delete）
- ❌ 不管理领域对象（返回Token字符串、LoginResult、bool）
- ❌ 不涉及数据持久化（Token存储在内存/加密文件，不是数据库）
- ✅ 是会话管理和安全机制（横切关注点）

**Repository vs Infrastructure Service关键区别**：

| 维度 | Repository | Infrastructure Service |
|-----|-----------|----------------------|
| **职责** | 数据访问（CRUD） | 横切关注点（认证/缓存/配置） |
| **接口模式** | 集合式（GetAll/GetById/Add/Update/Delete） | 特定操作（Login/Logout/Set/Get） |
| **返回类型** | 领域对象（DTO） | 基础类型（string/bool/Token） |
| **位置** | 各业务模块`Repositories/` | Foundation层 `Security/`、`Caching/` |
| **依赖方向** | ViewModel → Repository | ViewModel → Foundation Service |

**架构决策关联**：
- 参见 [ADR-002: Desktop移除Service层](#52-adr-002-desktop移除service层)
- 注意：ADR-002移除的是Business Service层，保留Infrastructure Service

---

#### 3.4.1 Repository实现位置（v2.2修订）

- **接口位置**: `Desktop.{Module}/Interfaces/I{Entity}Repository.cs` （v2.2新增独立目录）
- **实现位置**: `Desktop.{Module}/Repositories/{Entity}Repository.cs`
- **命名**: `{Entity}Repository` (如 PatientRepository, UserRepository)
- **原则**: 每个模块拥有独立的Repository，接口与实现分离，对齐Server端标准

#### 3.4.2 构造函数依赖（强制顺序）

```csharp
public PatientRepository(
    IApiClientManager apiClientManager,     // 1️⃣ Foundation层的统一API客户端管理器
    ILogger<PatientRepository> logger)      // 2️⃣ 日志
{
    _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**v2.1 关键变更**：
- ❌ 不再注入 `IMapper`（Repository直接返回DTO）
- ❌ 不再注入 `IExceptionHandler`（异常直接抛出，由ViewModel基类捕获）
- ✅ 注入 `IApiClientManager`（Foundation层统一HTTP客户端，替代直接注入HttpClient）

#### 3.4.3 Repository返回类型标准（v2.1）

| 场景 | 返回类型 | 说明 |
|------|---------|------|
| 查询单条 | `Task<{Entity}Dto>` | 返回单个实体（裸类型） |
| 查询列表 | `Task<PagedResult<{Entity}Dto>>` | 分页结果（裸类型） |
| 创建 | `Task<{Entity}Dto>` | 返回创建的实体（裸类型） |
| 更新 | `Task<{Entity}Dto>` | 返回更新的实体（裸类型） |
| 删除 | `Task` | 无返回数据（删除成功或抛异常） |

**v2.1 关键变更**：
- ✅ **Repository返回裸类型**：不再封装 `ServiceResult<T>`，直接返回 DTO
- ✅ **错误处理**：异常向上抛出，由 UnifiedViewModelBase 统一捕获
- ❌ **不再使用AutoMapper**：Repository直接从ApiClient获取DTO

#### 3.4.4 Repository示例模板（v2.2修订）

```csharp
using LYBT.Desktop.Foundation.Api;
using LYBT.Desktop.{Module}.Interfaces;  // v2.2: 接口在独立Interfaces目录
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.{Module}.Repositories
{
    /// <summary>
    /// {Entity}Repository - 数据访问层（v2.1 模块化架构，返回裸类型）
    /// </summary>
    public class {Entity}Repository : I{Entity}Repository
    {
        private readonly IApiClientManager _apiClient;
        private readonly ILogger<{Entity}Repository> _logger;
        private const string ApiBase = "/api/{entity}";

        public {Entity}Repository(
            IApiClientManager apiClientManager,
            ILogger<{Entity}Repository> logger)
        {
            _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<{Entity}Dto>> GetPagedAsync(
            int pageIndex, int pageSize, string? keyword = null)
        {
            _logger.LogInformation("查询{Entity}列表: pageIndex={PageIndex}, pageSize={PageSize}, keyword={Keyword}",
                pageIndex, pageSize, keyword);

            // ✅ 服务端分页：参数通过URL查询字符串传递给Server API
            var query = new PagedQueryBaseDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Keyword = keyword
            };

            // ApiClient 统一处理HTTP请求，异常向上抛出
            return await _apiClient.GetPagedAsync<{Entity}Dto>(ApiBase, query);
        }

        public async Task<{Entity}Dto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("查询{Entity}详情: id={Id}", id);
            return await _apiClient.GetAsync<{Entity}Dto>($"{ApiBase}/{id}");
        }

        public async Task<{Entity}Dto> CreateAsync({Entity}CreateDto dto)
        {
            _logger.LogInformation("创建{Entity}: {@Dto}", dto);
            return await _apiClient.PostAsync<{Entity}Dto>(ApiBase, dto);
        }

        public async Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto)
        {
            _logger.LogInformation("更新{Entity}: {@Dto}", dto);
            return await _apiClient.PutAsync<{Entity}Dto>($"{ApiBase}/{dto.Id}", dto);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("删除{Entity}: id={Id}", id);
            await _apiClient.DeleteAsync($"{ApiBase}/{id}");
        }
    }
}
```

**v2.1 关键改进**：
- ✅ **服务端分页**：GetPagedAsync 通过 ApiClient 传递查询参数，由Server端分页
- ✅ **统一API客户端**：使用 Foundation 层的 `IApiClientManager`，统一HTTP调用
- ✅ **返回裸类型**：直接返回 DTO，异常向上抛出
- ✅ **简化错误处理**：不再使用 try-catch 和 ServiceResult，由 UnifiedViewModelBase 统一捕获异常

### 3.5 组件化架构

#### 3.5.1 组件化触发条件（复杂度阈值，v2.4）

当 ViewModel 满足以下**任一条件**时，应考虑进行组件化重构：

| 触发条件 | 阈值 | 评估方式 |
|---------|------|---------|
| **代码行数** | ≥ 800 行 | 使用 `wc -l` 统计（含注释和空行） |
| **独立职责数量** | ≥ 4 个 | 识别独立的功能模块（如验证、计算、命令处理、数据管理） |
| **MVP 功能点数** | ≥ 50 个 | 统计 Issue 清单中的功能点数量 |
| **架构对齐需求** | - | 类似模块需要统一架构模式 |

#### 3.5.2 组件化架构模式

```
ViewModel（协调器，200-300行）
├── Calculator 组件（计算逻辑）
├── Validator 组件（验证逻辑）
├── CommandHandler 组件（命令操作）
└── DataManager 组件（数据管理）
```

**组件职责划分**：

| 组件类型 | 职责 | 典型行数 | 示例 |
|---------|------|---------|------|
| **Calculator** | 业务计算、统计分析、比率计算 | 150-200 | `FormulaCalculator`, `PrescriptionCalculator` |
| **Validator** | 数据验证、业务规则检查、错误收集 | 120-250 | `FormulaValidator`, `PrescriptionValidator` |
| **CommandHandler** | 保存、复制、删除等命令操作 | 150-200 | `FormulaCommandHandler` |
| **DataManager** | 数据加载、刷新、集合管理 | 100-360 | `FormulaDataManager` |

#### 3.5.3 共享组件模式（推荐）

对于具有相似业务逻辑的模块（如 Prescription、Formula），优先使用共享组件基类：

**步骤 1：定义共享接口**
```csharp
// LYBT.Shared.Components/IHerbItem.cs
public interface IHerbItem
{
    Guid HerbId { get; }
    string HerbName { get; }
    decimal Dosage { get; }
    string Unit { get; }
    decimal Quantity { get; }
    decimal UnitPrice { get; }
}
```

**步骤 2：创建泛型基类**
```csharp
// LYBT.Shared.Components/HerbCalculatorBase.cs
public abstract class HerbCalculatorBase<TItem> where TItem : IHerbItem
{
    public decimal CalculateTotalDosage(IEnumerable<TItem> items)
    {
        return items?.Sum(i => i.Dosage) ?? 0m;
    }

    public decimal CalculateTotalPrice(IEnumerable<TItem> items)
    {
        return items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
    }
}
```

**步骤 3：模块特定实现**
```csharp
// LYBT.Desktop.Formula/ViewModels/Components/FormulaCalculator.cs
public class FormulaCalculator : HerbCalculatorBase<FormulaHerbItemViewModel>
{
    // 继承共享逻辑

    // 添加 Formula 特定计算
    public FormulaRatioAnalysis CalculateRatioDistribution(...)
    {
        // Formula 特有的配方比例分析
    }
}
```

#### 3.5.4 组件设计原则

1. **单一职责原则（SRP）**：每个组件只负责一类业务逻辑
2. **依赖注入原则**：组件通过构造函数接收依赖（Repository, Logger）
3. **返回值约定**：使用 Tuple 返回操作结果：`(bool success, T? result, string? errorMessage)`
4. **无状态设计（推荐）**：组件尽量设计为无状态（Stateless），状态由 ViewModel 管理
5. **线程安全考虑**：异步组件需要处理线程同步

### 3.6 View层设计

#### 3.6.1 XAML基础结构（强制模板）

```xml
<UserControl x:Class="LYBT.Desktop.{Module}.Views.{Entity}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 标题栏 -->
            <RowDefinition Height="*" />     <!-- 内容区 -->
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Style="{StaticResource TitleBarStyle}">
            <TextBlock Text="{Binding PageTitle}" />
        </Border>

        <!-- 内容区 -->
        <ScrollViewer Grid.Row="1">
            <Grid Margin="16">
                <!-- 具体内容 -->
            </Grid>
        </ScrollViewer>

        <!-- 加载遮罩 -->
        <Grid Grid.RowSpan="2"
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
            <ProgressBar IsIndeterminate="True" />
        </Grid>
    </Grid>
</UserControl>
```

#### 3.6.2 代码后置（Code-behind）标准

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.{Module}.Views
{
    /// <summary>
    /// {Entity}View.xaml 的交互逻辑
    /// </summary>
    public partial class {Entity}View : UserControl
    {
        public {Entity}View()
        {
            InitializeComponent();
            // 仅初始化，不包含任何业务逻辑
            // 所有逻辑必须在 ViewModel 中
        }
    }
}
```

**强制规则**：
- ✅ 代码后置仅包含 `InitializeComponent()`
- ❌ 禁止在代码后置中编写业务逻辑
- ❌ 禁止在代码后置中访问 ViewModel

---

## Part IV: 共享层架构

### 4.1 Shared.Models架构

```
LYBT.Shared.Models/
├── Contracts/           # DTO契约
│   ├── Auth/           # 认证相关DTO
│   ├── Common/         # 通用DTO（PagedResult、ServiceResult）
│   ├── Consultation/   # 诊疗DTO
│   ├── Formula/        # 配方DTO
│   ├── Herbs/          # 药材DTO
│   ├── MedicalCase/    # 病历DTO
│   ├── Patients/       # 患者DTO
│   ├── Prescriptions/  # 处方DTO
│   └── Users/          # 用户DTO
│
├── Domain/             # 领域模型（共享）
│   └── Enums/         # 枚举定义
│
└── Common/             # 通用基类
    ├── ServiceResult.cs        # 服务结果封装
    └── PagedResult.cs          # 分页结果封装
```

### 4.2 Shared.Interfaces架构

```
LYBT.Shared.Interfaces/
├── Services/           # 服务接口（Server端实现）
│   ├── IAuthService.cs
│   ├── IConsultationService.cs
│   ├── IFormulaService.cs
│   ├── IHerbService.cs
│   ├── IMedicalCaseService.cs
│   ├── IPatientService.cs
│   ├── IPrescriptionService.cs
│   └── IUserService.cs
│
└── Repositories/       # （可选）共享Repository接口
```

### 4.3 DTO设计原则

**场景分离原则**（强制）：

| 场景 | DTO类型 | 命名规则 | 包含字段 |
|------|--------|---------|---------|
| 创建 | CreateDto | `{Entity}CreateDto` | 业务必需字段（非 nullable） |
| 更新 | UpdateDto | `{Entity}UpdateDto` | 可更新字段（nullable） |
| 展示 | Dto | `{Entity}Dto` | 所有展示字段（扁平化） |
| 查询 | SearchDto | `{Entity}SearchDto` | 查询条件字段（MVP阶段尽量避免） |

**示例**：

```csharp
// 创建场景 - CreateDto
public class PatientCreateDto
{
    public string Name { get; set; } = string.Empty;      // 必需
    public string Gender { get; set; } = string.Empty;    // 必需
    public DateTime? BirthDate { get; set; }              // 可选
    // 不包含 Id, CreatedAt 等系统字段
}

// 更新场景 - UpdateDto
public class PatientUpdateDto
{
    public Guid Id { get; set; }                          // 必需（标识）
    public string? Name { get; set; }                     // 可选
    public string? Gender { get; set; }                   // 可选
    public DateTime? BirthDate { get; set; }              // 可选
    // 不包含 CreatedAt, CreatedBy 等
}

// 展示场景 - Dto
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }                          // 计算字段（扁平化）
    public string CreatedBy { get; set; } = string.Empty; // 包含完整信息
}
```

**验证规范**：
- **简单验证**：使用 Data Annotations (`[Required]`, `[MaxLength]`, `[Range]`)
- **复杂验证**：使用 FluentValidation（放在Server端 `Validators/` 目录）

---

## Part V: 架构决策记录 (ADR)

### 5.1 ADR-001: 禁止CQRS模式

**决策日期**: 2025-10-07
**状态**: ✅ 已采纳
**影响范围**: Server端所有模块

**背景**：
- 小型诊所系统（并发 < 10人）
- 简单的CRUD业务场景
- 维护团队规模小

**决策**：
- ❌ **禁止**使用CQRS模式（Command Query Responsibility Segregation）
- ❌ **禁止**拆分 `IXxxQueryService` 和 `IXxxBusinessService`
- ✅ **强制**使用单一 `IXxxService` 接口

**理由**：
- 三层架构已足够满足业务需求
- CQRS会增加代码维护成本和学习曲线
- 避免过度工程导致的开发效率下降

**影响**：
- 所有模块使用统一的Service接口设计
- 简化依赖注入配置
- 降低新开发者学习成本

### 5.2 ADR-002: Desktop移除Service层

**决策日期**: 2025-01-09
**状态**: ✅ 已完成（Issue #1194）
**完成日期**: 2025-10-12
**影响范围**: Desktop端所有模块

**背景**：
- Desktop端原有架构：ViewModel → Service → Repository → WebAPI
- Service层重复了Server端业务逻辑（违反DRY原则）
- 导致维护成本高（业务变更需要同步修改两处）

**决策**：
- ❌ **移除** Desktop端Business Service层
- ✅ **保留** Desktop端Infrastructure Service（Foundation层）
  - AuthenticationService（认证服务）
  - CacheService（缓存服务）
  - ConfigurationService（配置服务）
  - 其他横切关注点服务
- ✅ **ViewModel直接调用Repository**（数据访问层）
- ✅ **Repository返回裸类型**（非 ServiceResult）
- ✅ **异常处理在UnifiedViewModelBase**

**新架构**：
```
ViewModel → Repository → WebAPI (Server)
```

**理由**：
- Desktop端不应重复Server端业务逻辑
- 简化调用链，提升性能
- Repository直接返回DTO，无需额外映射

**影响**：
- 删除 `Desktop.Services` 项目
- Repository下沉到各业务模块
- ViewModel直接注入Repository接口

**实施完成（Issue #1194）**：
- ✅ Phase 1: Desktop.Services 项目完整移除（commit 7c41070b）
  - 删除项目文件夹
  - 修复所有命名空间引用
  - 补充缺失的接口方法（IAuthenticationService.ChangePasswordAsync）
- ✅ Phase 2: 重复服务定义清理（commit b29bcf06）
  - ISessionManager 架构优化：扩展功能（CurrentUserId、RefreshToken）、删除 2 个重复定义
  - UserExperienceService 分层修正：删除 Infrastructure 版本、保留 Presentation 版本
- ✅ 编译验证：0 个警告，0 个错误
- ✅ 核心服务可用性验证通过

### 5.3 ADR-003: Repository接口位置标准

**决策日期**: 2025-10-11
**状态**: ✅ 已采纳
**影响范围**: Desktop端所有模块

**背景**：
- v2.0: Repository接口与实现混合在 `Repositories/` 目录
- Server端标准：接口在 `Interfaces/` 目录，实现在 `Repositories/` 目录
- Desktop端与Server端标准不一致

**决策**：
- ✅ **Desktop端对齐Server端标准**
- ✅ **接口位置**: `Desktop.{Module}/Interfaces/I{Entity}Repository.cs`
- ✅ **实现位置**: `Desktop.{Module}/Repositories/{Entity}Repository.cs`

**理由**：
- 统一Server端和Desktop端的接口位置规范
- 接口与实现分离，职责清晰
- 便于接口复用和测试

**影响**：
- 7个业务模块（Patients, Users, MedicalCase, Consultation, Prescriptions, Herbs, Formula）
- 命名空间调整：`LYBT.Desktop.{Module}.Repositories` → `LYBT.Desktop.{Module}.Interfaces`

### 5.4 ADR-004: Service接口统一设计

**决策日期**: 2025-10-07
**状态**: ✅ 已采纳
**影响范围**: Server端所有模块

**背景**：
- Service接口方法数量不一致（6个~26个不等）
- 缺乏统一的命名和返回类型规范
- MVP阶段存在过度设计（批量操作、内部验证方法等）

**决策**：
- ✅ **最小接口原则（ISP）**：方法数控制在 6-12 个之间
- ✅ **单一职责原则（SRP）**：每个接口只负责一个业务实体
- ✅ **YAGNI原则**：MVP阶段优先实现核心功能
- ✅ **统一命名约定**：
  - 创建：`CreateAsync`
  - 更新：`UpdateAsync`
  - 删除：`DeleteAsync`（软删除）
  - 查询：`GetByIdAsync`, `GetPagedAsync`
- ✅ **统一返回类型**：
  - 有数据：`Task<ServiceResult<T>>`
  - 无数据：`Task<ServiceResult>`
  - 分页：`Task<ServiceResult<PagedResult<T>>>`

**理由**：
- 降低接口复杂度，提升可维护性
- 统一模式，减少开发者心智负担
- 避免过度设计，聚焦MVP核心功能

**影响**：
- IUserService 从 26 个方法重构为 11 个方法
- 所有模块采用统一的Service接口模板

---

## Part VI: 架构演进

### 6.1 版本历史

#### Server端架构演进

| 版本 | 日期 | 关键变更 | ADR |
|------|------|---------|-----|
| **v1.4** | 2025-10-11 | Rules.cs vs Validator使用场景标准化 | - |
| **v1.3** | 2025-01-09 | 添加DTO设计规范章节 | - |
| **v1.2** | 2025-10-07 | AutoMapper/Validator注册标准化、迁移指南 | - |
| **v1.1** | 2025-10-07 | Service接口统一设计标准（6-12方法、ISP/SRP/YAGNI） | ADR-004 |
| **v1.0** | 2025-10-07 | 初始版本：三层架构、禁止CQRS | ADR-001 |

#### Desktop端架构演进

| 版本 | 日期 | 关键变更 | ADR |
|------|------|---------|-----|
| **v2.4** | 2025-10-12 | ViewModel组件化架构标准（复杂度阈值、共享组件） | - |
| **v2.2** | 2025-10-11 | Repository接口位置对齐Server端标准 | ADR-003 |
| **v2.1** | 2025-01-11 | Repository返回裸类型、UpdateAsync方法签名调整 | - |
| **v2.0** | 2025-01-09 | 移除Service层、Repository下沉模块、服务端分页 | ADR-002 |
| **v1.1** | 2025-01-09 | 添加DTO使用规范 | - |
| **v1.0** | 2025-10-07 | 初始版本：MVVM三层架构 | - |

### 6.2 迁移指南

#### 6.2.1 Server端：从混乱到统一（分步迁移）

**Step 1: 评估现状（30分钟）**
1. 检查模块目录结构是否符合标准
2. 确认Service接口位置（应在`Shared.Interfaces.Services`）
3. 检查是否存在CQRS拆分（Query/Business Service）
4. 检查AutoMapper和Validator注册方式

**Step 2: 删除CQRS遗留（如有）（1小时）**
1. 搜索`ICommandService`和`IQueryService`引用
2. 如未使用，直接删除接口文件
3. 如已使用，合并到单一Service接口
4. 更新服务注册

**Step 3: 统一服务注册（2小时）**
1. **AutoMapper**：
   - 删除模块中的`services.AddAutoMapper(typeof(XxxProfile))`
   - 确认`Mapping/XxxMappingProfile.cs`存在
   - 依赖`UnifiedServiceRegistration`的集中注册

2. **Validator**：
   - 改用`services.AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>()`
   - 删除显式的`services.AddScoped<IValidator<...>, ...>()`

3. **Service接口**：
   - 使用`services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>()`

**Step 4: 验证与测试（1小时）**
1. 编译验证：`dotnet build LYBT.Server.sln -c Release`
2. 测试验证：`dotnet test LYBT.Server.sln -c Release`

#### 6.2.2 Desktop端：从Service层迁移到Repository层

**旧架构（v1.0）**：
```
ViewModel → Service → Repository → WebAPI
```

**新架构（v2.0）**：
```
ViewModel → Repository → WebAPI
```

**迁移步骤**：

**Step 1：创建模块Repository目录**
```bash
# 在模块内创建Repositories目录
mkdir src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories
mkdir src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces  # v2.2
```

**Step 2：迁移Repository接口和实现**
```csharp
// v2.2 位置: Desktop.Patients/Interfaces/IPatientRepository.cs

namespace LYBT.Desktop.Patients.Interfaces  // v2.2: 接口独立目录
{
    public interface IPatientRepository
    {
        Task<PagedResult<PatientDto>> GetPagedAsync(int page, int pageSize, string? keyword);
        Task<PatientDto> GetByIdAsync(Guid id);
        Task<PatientDto> CreateAsync(PatientCreateDto dto);
        Task<PatientDto> UpdateAsync(PatientUpdateDto dto);  // v2.1: dto包含Id
        Task DeleteAsync(Guid id);
    }
}
```

**Step 3：更新ViewModel依赖**
```csharp
// ❌ 旧代码（v1.0）
using LYBT.Shared.Interfaces.Services;  // ❌ 会导致DI解析失败

public PatientManagementViewModel(
    IPatientService patientService,  // 删除Service依赖
    ...)
{
    _patientService = patientService;
}

// ✅ 新代码（v2.2）
using LYBT.Desktop.Patients.Interfaces;  // ✅ v2.2: 接口在独立Interfaces目录

public PatientManagementViewModel(
    IPatientRepository patientRepository,  // 直接注入Repository接口
    ...)
{
    _patientRepository = patientRepository;
}
```

**Step 4：修复P0性能问题（客户端分页→服务端分页）**
```csharp
// ❌ 旧代码（客户端分页）
var allPatients = await _repository.GetAllAsync();  // ❌ 获取全部10,000条
var items = allPatients.Skip(...).Take(...);        // 客户端分页

// ✅ 新代码（服务端分页）
var query = new PagedQueryBaseDto
{
    PageIndex = pageIndex,
    PageSize = pageSize,
    Keyword = keyword
};
var result = await _apiClient.GetPagedAsync<PatientDto>("/api/patients", query);
```

**Step 5：删除废弃代码**
- 删除 `Desktop.Services/Business/{Entity}Service.cs`
- 删除 `Desktop.Services/Repositories/` 目录
- 删除 `Desktop.Services/Mapping/` 目录
- 最终删除整个 `Desktop.Services` 项目

### 6.3 质量检查清单

#### 6.3.1 Server端检查清单

**架构验收**：
- [ ] 遵循三层架构（Controller → Service → Repository）
- [ ] 未使用CQRS模式（无Query/Business Service拆分）
- [ ] Service接口定义在 `LYBT.Shared.Interfaces.Services`

**目录结构验收**：
- [ ] Repository实现类位于 `Repositories/` 目录
- [ ] Service实现类位于 `Services/` 目录
- [ ] Validator位于 `Validators/` 目录
- [ ] `Interfaces/` 目录仅包含Repository接口

**服务注册验收**：
- [ ] 使用 `LYBT.Shared.Interfaces.Services.IXxxService` 注册
- [ ] 未注册模块内部Service接口（如IXxxQueryService）
- [ ] Repository使用 `IXxxRepository` 注册
- [ ] FluentValidation自动注册生效

**编译验证**：
- [ ] `dotnet build LYBT.Server.sln -c Release` 0错误0警告

#### 6.3.2 Desktop端检查清单

**ViewModel检查**：
- [ ] 继承正确的基类（`UnifiedViewModelBase` 或 `UnifiedListViewModelBase<TDto>`）
- [ ] 构造函数依赖顺序符合标准
- [ ] 所有必需依赖使用 `?? throw new ArgumentNullException`
- [ ] 可选依赖使用 `= null` 默认值

**Repository检查（v2.2修订）**：
- [ ] ✅ **v2.2**: 接口定义在模块的 `Interfaces/I{Entity}Repository.cs`
- [ ] ✅ **v2.2**: 实现类在模块的 `Repositories/{Entity}Repository.cs`
- [ ] 构造函数依赖顺序符合标准（`IApiClientManager`, `ILogger`）
- [ ] ✅ **所有方法返回裸类型**（如 `Task<T>`, `Task<PagedResult<T>>`, `Task`）
- [ ] ✅ **GetPagedAsync使用服务端分页**（通过ApiClient传递PagedQueryBaseDto）
- [ ] 使用 `_logger` 记录关键操作（使用结构化日志）
- [ ] 调用 Foundation 层的 `IApiClientManager`
- [ ] ❌ 不使用AutoMapper
- [ ] ❌ 不使用 try-catch 封装（异常向上抛出，由ViewModel基类捕获）

**View检查**：
- [ ] 使用 `prism:ViewModelLocator.AutoWireViewModel="True"`
- [ ] 标题栏 + 内容区 + 加载遮罩 三段式结构
- [ ] 命令绑定使用 `{Binding XxxCommand}`
- [ ] 数据绑定指定 `Mode` 和 `UpdateSourceTrigger`
- [ ] 代码后置仅包含 `InitializeComponent()`

**目录结构检查（v2.2修订）**：
- [ ] ✅ 有 `Models/`、`ViewModels/`、`Views/`
- [ ] ✅ **v2.2**: 有 `Interfaces/`（包含Repository接口）
- [ ] ✅ 有 `Repositories/`（包含Repository实现）
- [ ] ✅ 有 `{Module}Module.cs` 和 `README.md`
- [ ] ❌ 无 `Mappings/` 目录（已废弃）
- [ ] ❌ 无 `Services/` 目录（已废弃）

---

## 📊 Changelog

### v2.4 (2025-10-12) - 架构文档SSOT整合

**重大变更**：
- ✅ **创建统一架构文档**：合并 `server-module-design-standard.md` 和 `unified-design-standard.md`
- ✅ **建立SSOT原则**：本文档成为架构的唯一权威来源
- ✅ **整合ADR记录**：提取关键架构决策到 Part V
- ✅ **完整Changelog**：追溯所有历史版本

**架构整合**：
- Part I: 整体架构（DDD分层、技术栈、架构约束）
- Part II: Server端架构（v1.4标准）
- Part III: Desktop端架构（v2.4标准）
- Part IV: 共享层架构（DTO、接口规范）
- Part V: ADR决策记录（4个关键决策）
- Part VI: 架构演进（版本历史、迁移指南、质量检查）

**文档归档**：
- 📦 归档旧文档至 `docs/reports/archive/architecture/`
- 📦 归档分析报告至 `docs/reports/archive/`

---

### v2.4 (2025-10-12) - Desktop ViewModel组件化架构

**Desktop端新增**：
- 🆕 **组件化触发条件**：代码行数≥800行、独立职责≥4个、功能点≥50个
- 🆕 **组件化架构模式**：Calculator/Validator/CommandHandler/DataManager
- 🆕 **共享组件基类**：泛型基类设计（IHerbItem、HerbCalculatorBase）
- 🆕 **组件设计原则**：SRP、DI、Tuple返回值、无状态设计
- 🆕 **组件目录结构**：`ViewModels/Components/` 目录
- 📊 **重构效果**：FormulaDetailViewModel 从 672 行 → 280 行（减少 58%）

**实际案例（Issue #1153）**：
- FormulaDetailViewModel: 672 行 → 280 行
- PatientImportWizardViewModel: 1079 行 → 组件化重构
- Prescription 模块：删除 195 行重复代码（共享基类）

---

### v2.2 (2025-10-11) - Desktop Repository接口位置对齐

**Desktop端架构调整**：
- ✅ **Interfaces/ 目录**：Repository接口独立目录（7个模块）
- ✅ **Repositories/ 目录**：仅保留实现类，不再混合接口
- ✅ **架构一致性**：Desktop与Server端接口位置统一
- ✅ **命名空间调整**：`LYBT.Desktop.{Module}.Repositories` → `LYBT.Desktop.{Module}.Interfaces`

**影响模块**：Patients, Users, MedicalCase, Consultation, Prescriptions, Herbs, Formula

**关联**：ADR-003 Repository接口位置标准

---

### v2.1 (2025-01-11) - Desktop Repository返回裸类型

**Desktop端实现修订**：
- ✅ **Repository 返回裸类型**（非 ServiceResult）
- ✅ **UpdateAsync 方法签名调整**（dto 包含 Id，无需额外参数）
- ✅ **IApiClientManager 替代 HttpClient**（Foundation 层统一API客户端）
- ✅ **异常处理模式**：Repository 抛出异常，UnifiedViewModelBase 捕获
- ⚠️ **强调禁止使用** `LYBT.Shared.Interfaces.Services.*`（DI 解析失败）

---

### v2.0 (2025-01-09) - Desktop Service层移除

**Desktop端重大架构变更**：
- ❌ **删除Desktop.Services项目**
- ✅ **Repository下沉到各模块**
- ✅ **新增Desktop.Foundation/Presentation**
- ✅ **修复P0性能问题**（服务端分页）
- ❌ **废弃AutoMapper**

**关联**：ADR-002 Desktop移除Service层

---

### v1.4 (2025-10-11) - Server Rules.cs vs Validator标准化

**Server端补充**：
- 🆕 **Rules.cs 使用场景**：复杂领域逻辑、跨模块共享、纯计算
- 🆕 **Validator 使用场景**：单字段验证、框架规则、DTO绑定
- 🆕 **命名规范**：`{Module}Rules.cs`
- 🆕 **职责边界表**：Rules vs Validator vs Service

---

### v1.2 (2025-10-07) - Server AutoMapper/Validator注册标准化

**Server端补充**：
- 📚 **AutoMapper注册说明**（集中 vs 显式）
- 📚 **Validator注册说明**（自动扫描 vs 显式）
- 📚 **常见注册错误与修复**
- 📚 **迁移指南**（分步迁移、检查清单）
- 📚 **常见问题FAQ**（10个常见问题解答）

---

### v1.1 (2025-10-07) - Server Service接口统一设计

**Server端新增**：
- 📐 **Service接口设计原则**（ISP/SRP/YAGNI）
- 📐 **标准Service接口结构**（6-12方法模板）
- 📐 **命名约定**（方法/参数/返回类型）
- 📐 **分页查询标准**
- 📐 **软删除标准**
- 📐 **CancellationToken标准**

**关联**：ADR-004 Service接口统一设计标准

---

### v1.0 (2025-10-07) - 初始版本

**Server端初始标准**：
- 🏗️ **三层架构**（Controller → Service → Repository）
- ❌ **禁止CQRS模式**
- 📁 **目录结构标准**
- 🔧 **服务注册模式**

**Desktop端初始标准**：
- 🏗️ **MVVM三层架构**（View → ViewModel → Repository）
- 📁 **目录结构标准**
- 🔧 **基类选择规则**

**关联**：ADR-001 禁止CQRS模式

---

## 相关文档

- [DTO 设计原则](architecture/dto-design-principles.md) - 本项目 DTO 设计规范
- [技术标准与规范](development/standards.md) - 架构禁令与技术决策
- [功能模块设计](architecture/functional-modules-design.md) - 模块化设计详解
- [Prism 官方文档](https://prismlibrary.com/)
- [MVVM 设计模式](https://learn.microsoft.com/zh-cn/dotnet/architecture/maui/mvvm)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

**文档维护规则**：
1. 所有架构变更必须更新本文档的对应章节
2. 添加新版本记录到 Changelog（v{major}.{minor}格式）
3. 重大决策添加到 Part V ADR 章节
4. 保持文档简洁，避免冗余内容
5. 每次更新修改 "最后更新" 日期
