# 开发标准 (Development Standards)

**版本**: 2.0
**日期**: 2025-10-11
**状态**: 单一事实来源 (SSOT)

---

## 0. 文档说明

### 0.1 本文档的定位

本文档是项目开发的**单一事实来源（Single Source of Truth, SSOT）**，是 AI 和开发者进行技术决策和代码审查的最高准则。

### 0.2 标准变更流程

1. **标准的权威性**：所有已记录的标准，在被正式修改前，均被视为**必须遵守**的现行规范。
2. **变更的发起**：任何对现有标准的修改（如升级、废弃），都**必须**由项目负责人（人工）通过创建一个新的、明确的 GitHub Issue 来发起。
3. **变更 Issue 的要求**：该 Issue 必须明确说明要变更的标准、变更前的内容、变更后的内容以及变更原因。
4. **AI 的职责**：AI **禁止**在日常审查或开发中，主动提出对本文件中已定义标准的修改建议。AI 的职责是**遵守和执行**。

---

## 1. 架构约束（Pass 7 治理基线）⭐ 最高优先级

### 1.1 Record-Only 功能模式

本项目实施 **Record-Only** 功能模式，仅允许以下操作类型：

```
✅ Create: 创建新记录 (患者、医案、处方等)
✅ Read: 数据查询 (GetById, GetPaged, Search)
✅ Update: 字段更新 (基础信息修改、状态切换)
✅ Delete: 记录删除 (软删除、状态管理)
✅ History: 历史查询 (就诊记录、处方历史)
✅ Search: 条件搜索 (姓名、时间、状态筛选)
✅ Calculate: 基础计算 (价格计算、数量统计)
✅ Validate: 基础数据验证
```

### 1.2 统一四层架构

```
Layer 1: UI层 (Desktop.ViewModels + LYBT.WebAPI.Controllers)
├── 职责: 用户交互、HTTP请求处理、数据展示、参数验证
├── 依赖: Application层接口
└── 禁止: 直接访问Domain层、Infrastructure层

Layer 2: Application层 (Desktop.Modules + Modules.Services)
├── 职责: 应用服务、业务编排、DTO转换、权限检查
├── 依赖: Domain层接口、Infrastructure层接口
└── 禁止: UI框架依赖、具体数据库实现

Layer 3: Domain层 (Entities + 领域服务)
├── 职责: 实体定义、领域逻辑、业务规则、实体关系
├── 依赖: 仅依赖.NET BCL
└── 禁止: 基础设施关注点、UI关注点

Layer 4: Infrastructure层 (LYBT.Infrastructure)
├── 职责: 数据访问、外部服务、技术实现
├── 依赖: Domain层接口、第三方库、数据库
└── 禁止: 业务逻辑、UI相关代码
```

### 1.3 八个核心业务模块

1. **Auth** - 身份认证记录 (登录、登出、会话管理)
2. **Users** - 用户信息记录 (用户CRUD、角色分配)
3. **Patients** - 患者档案记录 (患者信息管理、历史查询)
4. **MedicalCase** - 医疗案例记录 (案例创建、状态更新、查询)
5. **Consultation** - 看诊记录 (四诊数据记录、历史回顾)
6. **Prescriptions** - 处方记录 (处方开具、价格计算、打印)
7. **Herbs** - 中药材记录 (药材信息管理、库存记录)
8. **Formula** - 验方记录 (验方模板管理、历史查询)

### 1.4 功能禁区

❌ **禁止引入的功能**（违规将自动阻塞PR）：
- 智能推荐系统 (药材推荐、验方推荐)
- 配伍安全检查 (超出基础安全验证)
- 复杂业务规则引擎
- 工作流引擎 (自动化流程管理)
- 数据流水线 (复杂数据处理管道)
- 会话管理 (超出基础用户登录会话)
- 复杂状态机 (多状态自动转换)
- 事件驱动架构
- 预测性分析和机器学习

### 1.5 技术禁区

❌ **禁止的框架和库**：
- 工作流引擎: Workflow Foundation, Elsa
- 规则引擎: Rules Engine, Decision Tables
- 事件总线: MediatR, NServiceBus, MassTransit
- 状态机: Stateless, Automatonymous
- 流水线模式: Pipeline Patterns
- 会话引擎: Session State Providers
- AI/ML框架: ML.NET, TensorFlow.NET

❌ **禁止的命名模式**：
- 类名包含: `Pipeline`, `Workflow`, `Bus`, `Engine`, `Saga`
- 命名空间包含: `*.Workflows.*`, `*.Pipelines.*`, `*.Events.*`

### 1.6 API 约束

- **版本控制**: 仅允许 `/api/v1/*` 路由
- **禁止路由**: `/api/v2/*`, `/api/v3/*`, `/v2/*`, `/v3/*`
- **控制器位置**: 所有控制器必须在 `LYBT.WebAPI` 项目
- **响应格式**: 统一使用 `ApiResponse<T>` 格式
- **命名规范**: 用户字段统一使用 `Username`

### 1.7 事务管理约束

```csharp
// ✅ 首选方式: EF Core隐式事务
await _context.SaveChangesAsync();

// ✅ 必要时: 最小显式事务
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 最少必要操作
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## 2. 架构原则

### 2.1 适度设计原则 ⭐ 最高优先级

**标准**: 拒绝过度工程，所有技术选型必须符合项目实际规模

**决策日期**: 2025-09-27

**判断标准**:
- 并发用户 < 100 → 不引入分布式缓存
- 数据量 < 100万 → 不引入分库分表
- 团队 < 10人 → 不引入微服务
- 无外部API消费者 → 不引入API版本管理
- 单数据库实例 → 不引入分布式事务

**核心原则**:
- KISS (Keep It Simple, Stupid)
- YAGNI (You Aren't Gonna Need It)
- 够用就好，演进优于预设

**相关文档**: [项目体量分析报告](../architecture/project-scale-analysis-2025-09-27.md)

### 2.2 禁止引入的技术（过度工程黑名单）

| 技术 | 禁用理由 | 替代方案 |
|------|----------|----------|
| **Redis缓存** | 部署维护成本高 | MemoryCache |
| **消息队列** (RabbitMQ/Kafka) | 无异步处理需求 | 同步调用 |
| **微服务架构** | 系统规模不需要 | 单体应用 |
| **CQRS/MediatR** | 过度设计，增加复杂度 | 直接调用Service |
| **容器化部署** (Docker/K8s) | 增加运维复杂度 | 传统部署 |
| **GraphQL** | 学习成本高 | RESTful API |
| **SignalR** | 无实时通信需求 | HTTP轮询（如需要） |
| **gRPC** | 内部系统不需要 | HTTP/JSON |

**决策日期**: 2025-09-27

---

## 3. 技术选型标准

### 3.1 选型原则

| 原则 | 说明 | 权重 |
|------|------|------|
| **适度设计** | 避免过度工程，满足当前需求即可 | ★★★★★ |
| **成熟稳定** | 选择经过验证的技术，避免实验性技术 | ★★★★★ |
| **团队熟悉** | 优先选择团队已掌握的技术 | ★★★★ |
| **社区支持** | 有活跃社区和完善文档 | ★★★ |
| **许可证** | 商业友好的开源协议 | ★★★ |

### 3.2 核心技术栈

#### 3.2.1 后端技术栈

| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 运行时 | .NET | 8.0 LTS | 最新长期支持版，性能优秀 |
| 框架 | ASP.NET Core | 8.0 | 成熟的Web API框架 |
| ORM | Entity Framework Core | 8.0 | 简化数据访问，支持迁移 |
| 数据库 | SQL Server | 2019+ | 企业级稳定，团队熟悉 |
| 缓存 | MemoryCache | 内置 | 简单够用，无需Redis |
| 认证 | JWT | - | 无状态，适合分布式 |
| 日志 | Serilog | 3.1.1 | 结构化日志，配置灵活 |
| API文档 | Swagger | 6.5.0 | 自动生成，方便调试 |

#### 3.2.2 前端技术栈

| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 框架 | WPF | .NET 8 | 成熟稳定，适合复杂表单 |
| MVVM | Prism | 9.0 | 模块化架构，依赖注入 |
| IoC容器 | DryIoc | 5.4.3 | 轻量高效，Prism默认支持 |
| HTTP客户端 | Refit | 7.0.0 | 类型安全的REST客户端 |
| 控件库 | Material Design | 5.0.0 | 现代化UI，组件丰富 |
| 验证 | FluentValidation | 11.9.0 | 流畅的验证规则 |

#### 3.2.3 共享组件

| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 映射 | AutoMapper | 13.0.1 | 简化DTO转换 |
| 序列化 | System.Text.Json | 内置 | 高性能，.NET原生 |
| 拼音 | TinyPinyin | 1.0.2 | 轻量级拼音转换 |
| Excel | ClosedXML | 0.102.2 | 无需Office，功能完整 |

---

## 4. 编码规范

### 4.1 命名规范

#### 4.1.1 C# 命名规范

```csharp
// 类名：PascalCase
public class PatientService { }

// 接口：I前缀 + PascalCase
public interface IPatientService { }

// 公有成员：PascalCase
public string PatientName { get; set; }

// 私有字段：_camelCase
private readonly ILogger _logger;

// 参数和局部变量：camelCase
public void CreatePatient(string patientName)
{
    var localVariable = patientName;
}

// 常量：UPPER_CASE
public const int MAX_RETRY_COUNT = 3;

// 异步方法：Async后缀
public async Task<Patient> GetPatientAsync(Guid id) { }
```

#### 4.1.2 数据库命名规范

```sql
-- 表名：复数形式
CREATE TABLE Patients

-- 列名：PascalCase
PatientId, CreatedAt, IsDeleted

-- 索引：IX_表名_列名
CREATE INDEX IX_Patients_PhoneNumber

-- 外键：FK_子表_父表_列名
CONSTRAINT FK_MedicalCases_Patients_PatientId
```

#### 4.1.3 API 路由规范

```
GET    /api/v1/patients          # 资源复数
GET    /api/v1/patients/{id}     # 路径参数
POST   /api/v1/patients          # 创建资源
PUT    /api/v1/patients/{id}     # 更新资源
DELETE /api/v1/patients/{id}     # 删除资源

# 嵌套资源
GET    /api/v1/patients/{patientId}/medical-cases  # kebab-case
```

### 4.2 项目结构规范

#### 4.2.1 解决方案结构

```
LYBT.sln
├── src/
│   ├── Server/
│   │   ├── Core/
│   │   │   ├── LYBT.Entities/           # 领域实体
│   │   │   └── LYBT.Infrastructure/     # 基础设施
│   │   ├── Modules/
│   │   │   ├── LYBT.Module.Auth/
│   │   │   ├── LYBT.Module.Patients/
│   │   │   └── ...
│   │   └── Services/
│   │       └── LYBT.WebAPI/             # API入口
│   ├── Client/
│   │   └── Desktop/
│   │       ├── Core/
│   │       ├── Infrastructure/
│   │       ├── Modules/
│   │       └── Shell/
│   └── Shared/
│       ├── LYBT.Shared.Models/          # DTO定义
│       ├── LYBT.Shared.Interfaces/      # 接口定义
│       └── LYBT.Shared.Utilities/       # 工具类
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docs/
```

#### 4.2.2 模块内部结构

```
LYBT.Module.Patients/
├── Controllers/           # API控制器
│   └── PatientsController.cs
├── Services/              # 业务服务
│   ├── IPatientService.cs
│   ├── PatientQueryService.cs
│   └── PatientBusinessService.cs
├── Repositories/          # 数据访问
│   ├── IPatientRepository.cs
│   └── PatientRepository.cs
├── Validators/            # 验证器
│   └── PatientValidator.cs
├── Mapping/               # AutoMapper配置
│   └── PatientMappingProfile.cs
└── PatientsModule.cs      # 模块注册
```

### 4.3 类设计规范

```csharp
// 1. 一个文件一个类
// 2. 类不超过500行
// 3. 方法不超过50行
// 4. 参数不超过5个

public class PatientService : IPatientService
{
    // 依赖注入的字段放在最前
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientService> _logger;
    private readonly IMapper _mapper;

    // 构造函数
    public PatientService(
        IPatientRepository repository,
        ILogger<PatientService> logger,
        IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    // 公有方法
    public async Task<ServiceResult<PatientDto>> CreateAsync(
        PatientCreateDto dto)
    {
        // 实现
    }

    // 私有方法放在最后
    private void ValidatePatient(Patient patient)
    {
        // 验证逻辑
    }
}
```

### 4.4 异步编程规范

```csharp
// 1. 异步方法必须返回Task或Task<T>
// 2. 异步方法名必须以Async结尾
// 3. 不要使用async void（事件处理器除外）
// 4. 使用ConfigureAwait(false)（UI层除外）

public async Task<Patient> GetPatientAsync(Guid id)
{
    // 正确：使用await
    var patient = await _repository.GetByIdAsync(id)
        .ConfigureAwait(false);

    // 错误：不要使用.Result或.Wait()
    // var patient = _repository.GetByIdAsync(id).Result;

    return patient;
}
```

### 4.5 异常处理规范

```csharp
// 1. 使用特定异常类型
public class DomainException : Exception { }
public class ValidationException : Exception { }
public class NotFoundException : Exception { }

// 2. 不要吞掉异常
try
{
    await _repository.SaveAsync(entity);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "保存实体失败: {EntityId}", entity.Id);
    throw new DataException("保存失败", ex);
}

// 3. 使用全局异常处理器
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 4.6 魔法字符串禁止

- **严禁**在代码中使用"魔法字符串"（未经定义的字符串字面量）
- 对于属性名，使用 `nameof()`
- 对于固定的业务字符串，应定义在静态常量类中

---

## 5. 分层实现规约

### 5.1 Controller 层

**职责**：仅负责 ① 解析HTTP请求，② 调用服务方法，③ 映射HTTP响应。**严禁在Controller中编写任何业务逻辑**。

**数据契约**：所有方法的输入参数和返回类型都必须是定义在 `LYBT.Shared.Models` 中的DTO。**严禁将EF Core实体（Entities）暴露到Controller层**。

**示例**:
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetPatientById(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient == null) return NotFound();
        return Ok(patient);
    }
}
```

### 5.2 Service / Repository 层（写入侧）

**职责**：封装所有业务规则、验证和数据持久化操作。

**关键规范**：
- 必须通过构造函数注入依赖
- 一个公开方法对应一个完整业务用例
- 所有公开方法入口必须进行参数验证（推荐 FluentValidation）
- 涉及多个实体修改必须使用显式事务
- 更新操作必须处理 `DbUpdateConcurrencyException`

### 5.3 QueryService 层（读取侧）

**职责**：优化查询性能。

**关键规范**：
- **严禁**在读取侧调用 `SaveChangesAsync()`
- 所有查询默认必须使用 `.AsNoTracking()` 禁用变更跟踪
- 必须在数据库层完成数据到DTO的转换
- 优先使用 AutoMapper 的 `.ProjectTo<T>()` 或 LINQ 的 `.Select()`
- 缓存逻辑应封装在 QueryService 中，对调用方透明

**示例**:
```csharp
public class PatientQueryService : IPatientQueryService
{
    private readonly AppDbContext _context;
    private readonly IConfigurationProvider _mapperConfig;

    public async Task<PatientDto> GetByIdAsync(Guid id)
    {
        return await _context.Patients
            .AsNoTracking() // 禁用变更跟踪
            .Where(p => p.Id == id && !p.IsDeleted)
            .ProjectTo<PatientDto>(_mapperConfig) // 在DB层投影到DTO
            .FirstOrDefaultAsync();
    }
}
```

### 5.4 WPF MVVM 规范

#### 5.4.1 View 规范

- 视图必须是"哑"的
- **严禁**在Code-behind中编写任何业务逻辑（除纯粹的UI动效）
- 所有状态和操作都应通过数据绑定到ViewModel

#### 5.4.2 ViewModel 规范

- 包含所有UI状态（属性）和业务操作（命令）
- **严禁**直接引用任何UI控件（如`TextBox`, `Button`）
- ViewModel 应是可独立测试的
- 推荐使用 `CommunityToolkit.Mvvm` 包：
  - `[ObservableProperty]` 自动实现 `INotifyPropertyChanged`
  - `[RelayCommand]` 自动生成 `ICommand` 实现

**示例**:
```csharp
public partial class PatientDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _patientName;

    [ObservableProperty]
    private bool _isLoading;

    private readonly IPatientService _patientService;

    public PatientDetailViewModel(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [RelayCommand]
    private async Task LoadPatientAsync(Guid patientId)
    {
        IsLoading = true;
        var patient = await _patientService.GetByIdAsync(patientId);
        PatientName = patient.Name;
        IsLoading = false;
    }
}
```

#### 5.4.3 XAML 资源规范

- 所有跨视图共享的资源（样式、转换器、画刷等）**必须**定义在 `src/Client/Desktop/Shell/Resources/UnifiedDesignSystem.xaml` 中
- **严禁**在视图或模块级别定义重复的通用资源
- 所有重要控件使用 `x:Name` 命名，遵循驼峰式命名法（如 `PatientNameTextBox`）

---

## 6. API 设计标准

### 6.1 RESTful 规范

#### 6.1.1 HTTP 方法语义

| 方法 | 语义 | 幂等 | 安全 |
|------|------|------|------|
| GET | 查询资源 | ✅ | ✅ |
| POST | 创建资源 | ❌ | ❌ |
| PUT | 完整更新 | ✅ | ❌ |
| PATCH | 部分更新 | ✅ | ❌ |
| DELETE | 删除资源 | ✅ | ❌ |

#### 6.1.2 状态码规范

| 状态码 | 含义 | 使用场景 |
|--------|------|----------|
| 200 | 成功 | GET/PUT/PATCH成功 |
| 201 | 已创建 | POST成功创建资源 |
| 204 | 无内容 | DELETE成功 |
| 400 | 请求错误 | 参数验证失败 |
| 401 | 未认证 | 未登录或Token无效 |
| 403 | 禁止访问 | 无权限 |
| 404 | 未找到 | 资源不存在 |
| 409 | 冲突 | 业务规则冲突 |
| 500 | 服务器错误 | 未处理异常 |

### 6.2 请求响应格式

#### 6.2.1 响应格式

```json
// 成功响应
{
    "success": true,
    "code": 200,
    "message": "操作成功",
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "张三"
    },
    "timestamp": "2025-10-11T10:00:00Z"
}

// 错误响应
{
    "success": false,
    "code": 400,
    "message": "参数验证失败",
    "errors": {
        "phoneNumber": ["手机号格式不正确"],
        "idNumber": ["身份证号已存在"]
    },
    "timestamp": "2025-10-11T10:00:00Z"
}

// 分页响应
{
    "success": true,
    "code": 200,
    "data": {
        "items": [...],
        "totalCount": 100,
        "pageNumber": 1,
        "pageSize": 20,
        "totalPages": 5
    }
}
```

### 6.3 API 版本管理

```csharp
// URL路径版本（推荐）
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : ControllerBase { }

// 配置版本策略
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});
```

---

## 7. 安全规范

### 7.1 认证授权

#### 7.1.1 JWT 配置

- **Secret 密钥**：至少32字符
- **过期时间**：默认8小时（480分钟）
- **Refresh Token**：7天有效期

#### 7.1.2 权限控制

```csharp
// 角色授权
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase { }

// 策略授权
[Authorize(Policy = "CanModifyPatient")]
public async Task<IActionResult> UpdatePatient(Guid id) { }
```

### 7.2 数据安全

#### 7.2.1 密码安全

- 使用 BCrypt 哈希（工作因子≥12）
- 密码策略：最小长度6字符，要求数字+大写+小写

#### 7.2.2 数据脱敏

```csharp
// 手机号脱敏：138****1234
public static string MaskPhoneNumber(string phone)
{
    if (string.IsNullOrEmpty(phone) || phone.Length != 11)
        return phone;

    return $"{phone[..3]}****{phone[7..]}";
}

// 身份证脱敏：110***********1234
public static string MaskIdNumber(string idNumber)
{
    if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 18)
        return idNumber;

    return $"{idNumber[..3]}***********{idNumber[14..]}";
}
```

### 7.3 输入验证

使用 FluentValidation 进行参数验证：

```csharp
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .Length(2, 20).WithMessage("姓名长度必须在2-20个字符之间");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
    }
}
```

---

## 8. 性能优化标准

### 8.1 数据库优化

#### 8.1.1 索引策略

```sql
-- 主键索引（自动创建）
PRIMARY KEY (Id)

-- 唯一索引
CREATE UNIQUE INDEX UX_Patients_IdNumber ON Patients(IdNumber)

-- 查询索引
CREATE INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber)
CREATE INDEX IX_Patients_PinyinCode ON Patients(PinyinCode)

-- 复合索引
CREATE INDEX IX_MedicalCases_PatientId_CreatedAt
ON MedicalCases(PatientId, CreatedAt DESC)
```

#### 8.1.2 查询优化

```csharp
// 1. 避免N+1问题
var patients = await _context.Patients
    .Include(p => p.MedicalCases)
        .ThenInclude(m => m.Consultation)
    .ToListAsync();

// 2. 使用分页
var result = await query
    .OrderBy(p => p.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// 3. 只查询需要的字段
var summary = await _context.Patients
    .Select(p => new
    {
        p.Id,
        p.Name,
        CaseCount = p.MedicalCases.Count()
    })
    .ToListAsync();
```

### 8.2 缓存策略

#### 8.2.1 缓存时间配置

```csharp
public static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(5);
public static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(10);
public static readonly TimeSpan LongCache = TimeSpan.FromMinutes(30);
```

#### 8.2.2 缓存键生成

```csharp
public static string GetPatientKey(Guid id) => $"patient:{id}";
public static string GetHerbListKey() => "herbs:list";
public static string GetUserPermKey(Guid userId) => $"user:perm:{userId}";
```

---

## 9. 日志规范

### 9.1 日志级别

| 级别 | 使用场景 | 示例 |
|------|----------|------|
| **Fatal** | 系统崩溃 | 数据库不可用 |
| **Error** | 异常错误 | 未处理异常 |
| **Warning** | 警告信息 | 性能问题、重试 |
| **Information** | 业务事件 | 用户登录、创建订单 |
| **Debug** | 调试信息 | SQL语句、详细流程 |
| **Verbose** | 详细跟踪 | 方法进入/退出 |

### 9.2 结构化日志

使用 Serilog 记录结构化日志：

```csharp
_logger.LogInformation(
    "用户 {UserId} 创建了患者 {PatientId}，姓名：{PatientName}",
    userId, patientId, patientName);

_logger.LogError(ex,
    "保存患者 {PatientId} 失败",
    patientId);
```

### 9.3 审计日志

所有数据变更操作必须记录审计日志，包含：
- 表名、实体ID
- 操作类型（Create/Update/Delete）
- 旧值和新值
- 用户ID、用户名
- 时间戳、IP地址

---

## 10. 测试标准

### 10.1 测试层级

| 层级 | 占比 | 目标 | 工具 |
|------|------|------|------|
| 单元测试 | 70% | 业务逻辑 | xUnit, Moq |
| 集成测试 | 20% | API端点 | TestServer |
| E2E测试 | 10% | 用户流程 | Selenium |

### 10.2 单元测试规范

**命名规范**：`方法名_场景_期望结果`

```csharp
[Fact]
public async Task CreatePatient_WithValidData_ShouldReturnSuccess()
{
    // Arrange - 准备数据
    var dto = new PatientCreateDto { ... };

    _mockRepository
        .Setup(x => x.AddAsync(It.IsAny<Patient>()))
        .ReturnsAsync((Patient p) => p);

    // Act - 执行操作
    var result = await _service.CreateAsync(dto);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();

    _mockRepository.Verify(
        x => x.AddAsync(It.IsAny<Patient>()),
        Times.Once);
}
```

### 10.3 集成测试规范

使用 `WebApplicationFactory` 创建测试服务器，使用内存数据库替换真实数据库。

### 10.4 架构测试

必须通过所有架构约束测试：
- `LayerDependencyTests` - 层间依赖检查
- `ApiVersionTests` - API版本检查
- `ControllerLocationTests` - 控制器位置检查
- `NamingConventionTests` - 命名规范检查
- `ForbiddenFrameworkTests` - 禁止框架检查
- `RecordOnlyTests` - Record-Only功能检查

---

## 11. 部署标准

### 11.1 环境配置

#### 11.1.1 配置文件层级

```
appsettings.json              # 基础配置
appsettings.Development.json  # 开发环境
appsettings.Staging.json      # 测试环境
appsettings.Production.json   # 生产环境
```

#### 11.1.2 敏感信息管理

```csharp
// 开发环境：User Secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."

// 生产环境：环境变量
Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "...");
```

### 11.2 健康检查

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck("cache", () =>
    {
        var cache = serviceProvider.GetService<IMemoryCache>();
        return cache != null
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    });
```

---

## 12. 文档标准

### 12.1 代码注释

所有 `public` 的类、方法、属性都**必须**添加 `///` XML文档注释：

```csharp
/// <summary>
/// 创建患者档案
/// </summary>
/// <param name="dto">患者创建信息</param>
/// <returns>创建成功的患者信息</returns>
/// <exception cref="ValidationException">参数验证失败</exception>
/// <exception cref="DuplicateException">身份证号重复</exception>
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
{
    // 业务逻辑注释：解释为什么，而不是做什么
}
```

### 12.2 API 文档

使用 Swagger 自动生成 API 文档，必须添加响应类型和状态码说明：

```csharp
/// <summary>
/// 获取患者列表
/// </summary>
/// <param name="page">页码（从1开始）</param>
/// <param name="pageSize">每页数量（默认20）</param>
/// <response code="200">返回患者列表</response>
/// <response code="401">未授权</response>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<PatientListDto>), 200)]
[ProducesResponseType(401)]
public async Task<IActionResult> GetPatients(int page = 1, int pageSize = 20)
{
    // 实现
}
```

---

## 13. 版本管理标准

### 13.1 Git 分支策略

```
main/master     # 生产分支
  ├── develop   # 开发分支
  ├── feature/* # 功能分支
  ├── bugfix/*  # 缺陷修复
  └── hotfix/*  # 紧急修复
```

### 13.2 提交规范（Conventional Commits）

```
<type>(<scope>): <subject>

<body>

<footer>
```

**类型 (type)**:
- `feat`: 新功能 (仅限Record-Only范围内)
- `fix`: Bug修复
- `docs`: 文档更新
- `test`: 测试相关
- `refactor`: 重构 (不改变功能)
- `chore`: 构建/工具相关

**示例**:
```
feat(patients): add patient basic info CRUD operations

- Add CreatePatient, UpdatePatient, DeletePatient methods
- Add patient search and pagination
- Follow Record-Only baseline - no complex business logic

Closes #123
```

### 13.3 版本号规范

```
主版本.次版本.修订号
MAJOR.MINOR.PATCH

1.0.0 - 初始发布
1.1.0 - 新增功能
1.1.1 - 缺陷修复
2.0.0 - 不兼容更新
```

---

## 14. 质量门禁（CI/CD）

### 14.1 三级门禁体系（全部阻塞性）

**Level 1: 代码质量门禁**
```yaml
- dotnet format --verify-no-changes  # 格式检查
- dotnet build --configuration Release  # 编译检查
```

**Level 2: 测试质量门禁**
```yaml
- dotnet test --configuration Release  # 单元测试
- dotnet test tests/Architecture/  # 架构测试
```

**Level 3: 架构合规门禁**
```yaml
- LayerDependencyTests  # 层间依赖检查
- ApiVersionTests  # API版本检查
- ControllerLocationTests  # 控制器位置检查
- NamingConventionTests  # 命名规范检查
- ForbiddenFrameworkTests  # 禁止框架检查
- RecordOnlyTests  # Record-Only功能检查
```

### 14.2 门禁失败处理

如果任何门禁失败：

1. **PR自动阻塞** - 无法合并直到修复所有问题
2. **查看构建日志** - 识别具体失败原因
3. **本地修复** - 在本地环境修复所有问题
4. **重新提交** - 推送修复后的代码

---

## 15. 代码审查

### 15.1 AI 双审查机制 ⭐ GitHub Pro

**标准**: 所有 Pull Request 必须通过 **Claude Code + GitHub Copilot** 双重 AI 审查

**决策日期**: 2025-10-05

**流程**:

1. **Claude Code 自动初审** - 全面代码质量检查
   - 架构合规性（禁止框架、Record-Only 模式）
   - C# / .NET 8 最佳实践
   - WPF / Prism MVVM 规范
   - 代码质量、安全性、性能
   - 测试覆盖、文档同步

2. **GitHub Copilot 二审** - 补充性代码改进建议
   - 代码简洁性优化
   - 潜在问题检测
   - .NET 新特性使用建议

3. **人工审查（CODEOWNERS）** - 业务逻辑与设计决策
   - 业务逻辑正确性
   - 设计决策合理性
   - 代码可读性与可维护性

**相关文档**:
- [代码审查指南](code-review-guidelines.md)
- [分支保护配置](branch-protection-setup.md)
- `.github/CODEOWNERS`
- `.github/workflows/claude-code-review.yml`

**相关 Issue**: #935

### 15.2 强制审查规则（分支保护）

**标准**: `master` 分支必须配置以下保护规则

**决策日期**: 2025-10-05

**规则**:
- ✅ 需要至少 1 次人工审批（CODEOWNERS）
- ✅ 需要 Claude Code Review 通过
- ✅ 需要所有 CI 检查通过（编译、测试、覆盖率、架构合规）
- ✅ 需要线性提交历史
- ❌ 不允许绕过保护规则（包括管理员）

**配置指南**: [分支保护配置](branch-protection-setup.md)

---

## 附录A：检查清单

### 开发完成检查
- [ ] 功能仅限Record-Only范围 (CRUD + 历史查询)
- [ ] 未引入禁止的框架或命名模式
- [ ] 遵循统一四层架构约束
- [ ] API使用 /api/v1/* 路由格式
- [ ] 事务管理使用EF Core隐式事务优先

### 质量检查
- [ ] `dotnet format --verify-no-changes` 通过
- [ ] `dotnet build --configuration Release` 零错误零警告
- [ ] `dotnet test --configuration Release` 全部通过
- [ ] `dotnet test tests/Architecture/` 全部通过

### 文档检查
- [ ] 已更新相关文档
- [ ] 提交信息符合Conventional Commits规范
- [ ] PR描述清晰，包含变更说明

### 代码审查清单
- [ ] 命名是否符合规范？
- [ ] 是否有适当的注释？
- [ ] 是否处理了所有异常？
- [ ] 是否有单元测试？
- [ ] 是否有性能问题？
- [ ] 是否有安全问题？
- [ ] 是否符合SOLID原则？
- [ ] 是否有重复代码？

### 发布前检查清单
- [ ] 所有测试通过？
- [ ] 代码审查完成？
- [ ] 文档已更新？
- [ ] 配置文件正确？
- [ ] 数据库迁移脚本？
- [ ] 性能测试通过？
- [ ] 安全扫描通过？
- [ ] 版本号已更新？

---

## 附录B：工具清单

### 开发工具

| 工具 | 用途 | 版本 |
|------|------|------|
| Visual Studio | IDE | 2022 |
| VS Code | 轻量编辑器 | Latest |
| SQL Server Management Studio | 数据库管理 | 19.0 |
| Postman | API测试 | Latest |
| Git | 版本控制 | 2.40+ |

### NuGet 包清单

```xml
<!-- 后端核心包 -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />

<!-- 前端核心包 -->
<PackageReference Include="Prism.DryIoc" Version="9.0.271-pre" />
<PackageReference Include="MaterialDesignThemes" Version="5.0.0" />
<PackageReference Include="Refit" Version="7.0.0" />

<!-- 测试包 -->
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

---

**文档维护**：

| 版本 | 日期 | 修订内容 |
|------|------|----------|
| v1.0 | 2025-09-28 | 初始版本 |
| v2.0 | 2025-10-11 | SSOT整合：合并4个规范文档为唯一权威来源 |
