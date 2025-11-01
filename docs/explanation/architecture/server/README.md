# Server端架构指南

**🏗️ 三层架构、8个模块、服务标准** - 凌美对齐实际代码架构实现

## 🎯 Server端架构概述

凌隐宝堂中医诊所管理系统Server端采用经典的三层架构设计，确保代码的高内聚、低耦合和可扩展性。本架构指南详细阐述Server端的架构设计原理、技术选型和实现规范，与项目实际代码架构完全对齐。

## 🏗️ 三层架构设计


### 架构层次结构

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                (Controllers & DTOs)                     │
│              ┌─────────────┬─────────────────┐               │
│              │  Controllers   │   DTOs/Models    │               │
│              └─────────────┴─────────────────┘               │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                     │
│                  (Services & Interfaces)                 │
│              ┌─────────────────────────────────────┐           │
│              │    Business Services                    │           │
│              │    Domain Interfaces                    │           │
│              │    Application Services              │           │
│              └─────────────────────────────────────┘           │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                  │
│                (Repositories & Data Access)                 │
│              ┌─────────────────────────────────────┐           │
│              │    Entity Framework Core               │           │
│              │    Repository Implementations          │           │
│              │    Database Connections               │           │
│              └─────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────┘
```
`
### 实际项目结构映射

⚠️ **关键架构说明**：
- **Controllers位置**：所有API控制器统一在`LYBT.WebAPI/Controllers/`，不在各Module中
- **Module组成**：各Module仅包含Services + Repositories + Interfaces，专注业务逻辑层
- **接口定义**：服务接口（IXxxService）分散在各模块的`Interfaces/`文件夹，无集中式接口项目（Issue #1729）
- **分层职责**：Controllers（表示层）→ Services（应用层）→ Repositories（数据访问层）

```
src/Server/
├── Services/LYBT.WebAPI/           # ⭐ Presentation Layer（表示层）
│   ├── Controllers/                # ⭐ 所有API控制器统一位置（13个）
│   │   ├── 业务控制器（8个）：
│   │   ├── AuthController.cs           # 认证授权API
│   │   ├── UsersController.cs          # 用户管理API
│   │   ├── PatientsController.cs       # 患者管理API
│   │   ├── MedicalCaseController.cs    # 医案管理API
│   │   ├── ConsultationController.cs   # 诊疗记录API
│   │   ├── PrescriptionsController.cs  # 处方管理API
│   │   ├── HerbsController.cs          # 药材管理API
│   │   ├── FormulasController.cs       # 验方管理API
│   │   └── 系统控制器（4个）：
│   │       ├── HealthController.cs         # 健康检查（Issue #1733 MVP简化）
│   │       ├── CacheHealthController.cs    # 缓存管理（Issue #1733 MVP简化）
│   │       ├── RootHealthController.cs     # 根路径健康检查
│   │       └── BaseApiController.cs        # 基础控制器（抽象基类）
│   ├── DTOs/                       # 数据传输对象
│   ├── Middleware/                 # 中间件
│   ├── Filters/                    # 过滤器
│   └── Configuration/              # 配置类
│
├── Core/                           # Application Layer (Shared)
│   ├── LYBT.Entities/              # 实体定义
│   └── LYBT.Infrastructure/        # 基础设施（含BaseApiController）
│
├── Modules/                        # ⭐ Application Layer (Business) - 仅包含业务逻辑
│   ├── LYBT.Module.Auth/           # 认证模块
│   │   ├── Services/               # ✅ AuthService业务服务
│   │   └── Interfaces/             # ✅ IAuthService接口
│   ├── LYBT.Module.Users/          # 用户管理模块
│   │   ├── Services/               # ✅ UserService业务服务
│   │   ├── Repositories/           # ✅ UserRepository数据访问
│   │   ├── Validators/             # ✅ FluentValidation验证器
│   │   ├── Mapping/                # ✅ AutoMapper映射配置
│   │   └── Interfaces/             # ✅ 模块接口定义
│   ├── LYBT.Module.Patients/       # 患者管理模块
│   │   ├── Services/               # ✅ PatientService
│   │   ├── Repositories/           # ✅ PatientRepository
│   │   ├── Validators/             # ✅ PatientValidator
│   │   ├── Mapping/                # ✅ PatientMappingProfile
│   │   └── Interfaces/             # ✅ IPatientService, IPatientRepository
│   ├── LYBT.Module.MedicalCase/    # 医案管理模块
│   ├── LYBT.Module.Consultation/   # 诊疗记录模块
│   ├── LYBT.Module.Prescriptions/  # 处方管理模块
│   ├── LYBT.Module.Herbs/          # 药材管理模块
│   └── LYBT.Module.Formula/        # 验方管理模块
│   ❌ 注意：Module中不包含Controllers！
│
└── Infrastructure/LYBT.Infrastructure/  # Infrastructure Layer
    ├── Data/                       # 数据访问
    │   ├── AppDbContext.cs
    │   ├── Configurations/
    │   └── Migrations/
    ├── Repositories/               # 仓储基类实现
    │   └── BaseRepository.cs
    ├── Utilities/                  # 工具类（Issue #1757）
    │   ├── ValidationHelper.cs     # 验证工具类
    │   └── PasswordHelper.cs       # 密码工具类
    ├── Configuration/              # 基础设施配置
    ├── Services/                   # 基础设施服务
    └── Extensions/                 # 扩展方法
```

**架构层次职责划分**：
1. **WebAPI项目（Presentation）**：
   - ✅ 包含所有Controllers（13个）
   - ✅ 处理HTTP请求/响应
   - ✅ 依赖注入Services

2. **Module项目（Application）**：
   - ✅ 包含Services（业务逻辑）
   - ✅ 包含Repositories（数据访问）
   - ✅ 包含Validators、Mapping、Interfaces
   - ❌ 不包含Controllers

3. **Infrastructure项目（Infrastructure）**：
   - ✅ DbContext和数据库配置
   - ✅ 基础仓储实现
   - ✅ 通用基础设施服务
   - ✅ 工具类（Utilities）：无状态纯函数工具类

## 📋 业务模块架构

### 8个核心业务模块

#### 1. 认证模块 (Auth Module)
**职责**：用户身份认证、授权管理、JWT令牌处理
- **服务层**：AuthService、TokenService、AuthorizationService
- **数据层**：UserRepository、AdminSecretRepository
- **核心实体**：User、AdminSecret、RefreshToken
- **关键特性**：双轨认证、令牌刷新、权限控制

#### 2. 用户管理模块 (Users Module)
**职责**：用户信息管理、角色权限分配、密码安全
- **服务层**：UserService、RoleService、PermissionService
- **数据层**：UserRepository、RoleRepository
- **核心实体**：User、Role、Permission、UserRole
- **关键特性**：RBAC权限模型、密码加密、用户状态管理

#### 3. 患者管理模块 (Patients Module)
**职责**：患者信息管理、Excel导入导出、查询统计
- **服务层**：PatientService、PatientImportService、PatientSearchService
- **数据层**：PatientRepository、PatientHistoryRepository
- **核心实体**：Patient、PatientContact、PatientHistory
- **关键特性**：批量导入、重复检查、数据统计

#### 4. 医案管理模块 (MedicalCase Module) - ⭐ Epic #1612重构版

> **📚 权威参考**：详细实体关系定义参见 [clinical-workflow-entity-relationships.md](../shared/clinical-workflow-entity-relationships.md)（⭐⭐⭐权威文档）
>
> **📋 API文档**：完整API参考参见 [medicalcase-api.md](../../api/medicalcase-api.md)
>
> **🧪 测试报告**：E2E测试分析参见 [e2e-test-coverage-analysis.md](../../reports/e2e-test-coverage-analysis.md)

**职责**：医案记录管理、状态流转、业务流程（**聚合根模式**：MedicalCase统一管理Consultation和Prescription生命周期）

**架构重构（Epic #1612）**：
- ✅ 三层对齐架构实现
- ✅ Write/Read/Helper Layer分离
- ✅ 聚合根边界强制（AR-001）
- ✅ 一诊一方约束（AR-003）
- ✅ 三步流程验证（BF-002）
- ✅ 单患者单Active病案（BR-001）

##### 服务层（Application Layer）

**IMedicalCaseService接口** (`src/Server/Modules/LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`)

**Write Layer - 写操作（8个方法）**：
```csharp
// 1. 创建新病案
Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate);

// 2. 更新辨证信息（Step 1）
Task<MedicalCaseEntity?> UpdateConsultationAsync(
    Guid medicalCaseId, UpdateConsultationRequest request);

// 3. 标记处方需求（Step 2）
Task<MedicalCaseEntity?> SetPrescriptionFlagAsync(
    Guid medicalCaseId, bool needsPrescription);

// 4. 创建处方（Step 3a）
Task<PrescriptionEntity?> CreatePrescriptionAsync(
    Guid medicalCaseId, CreatePrescriptionRequest request);

// 5. 更新处方
Task<PrescriptionEntity?> UpdatePrescriptionAsync(
    Guid medicalCaseId, Guid prescriptionId, UpdatePrescriptionRequest request);

// 6. 删除处方
Task<bool> DeletePrescriptionAsync(Guid medicalCaseId, Guid prescriptionId);

// 7. 更新状态
Task<MedicalCaseEntity?> UpdateStatusAsync(
    Guid medicalCaseId, MedicalCaseStatus status);

// 8. 完成病案（Step 3b）
Task<MedicalCaseEntity?> CompleteAsync(Guid medicalCaseId);
```

**Read Layer - 读操作（4个方法）**：
```csharp
// 9. 获取病案详情（预加载Consultation/Prescription）
Task<MedicalCaseEntity?> GetByIdAsync(Guid medicalCaseId);

// 10. 查询病案列表（分页+过滤）
Task<PagedResult<MedicalCaseEntity>> GetListAsync(
    MedicalCaseStatus? status, Guid? patientId, int page, int pageSize);

// 11. 查询辨证记录列表
Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId);

// 12. 查询处方列表
Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId);
```

**Helper Layer - 辅助功能（2个方法）**：
```csharp
// 13. 验证病案是否可编辑
Task<CanEditResponse> CanEditAsync(Guid medicalCaseId);

// 14. 验证处方是否可删除
Task<CanDeleteResponse> CanDeletePrescriptionAsync(
    Guid medicalCaseId, Guid prescriptionId);
```

**业务规则验证**：
- **AR-001**：所有Consultation/Prescription操作必须通过MedicalCase聚合根
- **AR-003**：一诊一方约束（已有处方时禁止再次创建）
- **BF-002**：三步流程验证（辨证 → 标记 → 开处方/完成）
- **BR-001**：单患者单Active病案约束

##### 数据访问层（Infrastructure Layer）

**IMedicalCaseRepository接口** (`src/Server/Modules/LYBT.Module.MedicalCase/Repositories/IMedicalCaseRepository.cs`)

**核心查询方法（Epic #1612优化）**：
```csharp
// 详情查询（预加载关联）
Task<MedicalCaseEntity?> GetByIdWithDetailsAsync(Guid id);

// 分页查询（预加载+过滤）
Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
    int page, int pageSize, string? keyword);

// 按患者查询（支持Active状态过滤）
Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId);
```

**性能优化**：
- ✅ Include预加载：减少N+1查询
- ✅ AsSplitQuery：优化多关联查询
- ✅ AsNoTracking：只读查询性能提升

##### 测试覆盖（Epic #1612）

**单元测试** (`tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`)：
- 测试数量：32个测试
- 行覆盖率：82.6%
- 分支覆盖率：57.14%
- 测试框架：xUnit + Moq + FluentAssertions

**集成测试** (`tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`)：
- 测试数量：18个测试
- 通过率：100%
- 覆盖范围：14个API端点（Write 8 + Read 4 + Helper 2）
- E2E场景：4个完整业务流程验证

##### 核心实体

- **MedicalCase（聚合根）**：医案主实体，管理生命周期
- **Consultation**：辨证信息（四诊、诊断、治则）
- **Prescription**：处方信息（药品、剂数、价格）
- **MedicalCaseHistory**：审计跟踪
- **CaseStatus**：状态枚举（Draft/Active/Completed/Cancelled）

##### 关键特性

- ✅ 状态机模式：严格状态流转控制
- ✅ 三步工作流：辨证 → 标记 → 开处方/完成
- ✅ 审计跟踪：所有变更记录
- ✅ 聚合根边界：强制通过MedicalCase操作
- ✅ 业务规则验证：四条核心规则（AR-001, AR-003, BF-002, BR-001）

**本模块重点**：从WebAPI和Service层视角实现聚合根模式，确保Consultation/Prescription只能通过MedicalCase进行创建/更新/删除操作。Epic #1612完成了完整的三层对齐架构重构，达到生产级质量标准。

#### 5. 诊疗记录模块 (Consultation Module)
**职责**：四诊信息记录、辨证论治、诊断结果
- **服务层**：ConsultationService、DiagnosisService、TreatmentService
- **数据层**：ConsultationRepository、DiagnosisRepository
- **核心实体**：Consultation、Diagnosis、Examination、Treatment
- **关键特性**：四诊合参、中医诊断、治法方案

#### 6. 处方管理模块 (Prescriptions Module)
**职责**：处方创建管理、药材配伍、价格计算、处方编号生成
- **服务层**：PrescriptionService、PrescriptionCalculationService、PrescriptionValidationService、**PrescriptionNumberService (Issue #1551)**
- **数据层**：PrescriptionRepository、PrescriptionItemRepository
- **核心实体**：Prescription、PrescriptionItem、PrescriptionStatus
- **关键特性**：四种录入方式、配伍检查、自动计价、**处方自动编号（RX-YYYYMMDD-NNNN）**

#### 7. 药材管理模块 (Herbs Module)
**职责**：药材信息管理、拼音检索、价格管理
- **服务层**：HerbService、HerbSearchService
- **数据层**：HerbRepository
- **核心实体**：Herb、HerbCategory
- **关键特性**：2000+药材、拼音码检索、价格管理

#### 8. 验方管理模块 (Formula Module)
**职责**：验方模板管理、智能推荐、统计分析
- **服务层**：FormulaService、FormulaRecommendationService、FormulaAnalysisService
- **数据层**：FormulaRepository、FormulaItemRepository
- **核心实体**：Formula、FormulaItem、FormulaCategory
- **关键特性**：模板管理、智能推荐、使用统计

## 🔧 服务层设计模式

### Service层实现原则

> **⚠️ 架构说明**：当前MVP阶段，Service层采用**直接实现接口**模式，不使用抽象基类。
>
> **设计原则**：
> - ✅ 每个Service直接实现对应的IService接口（如`PatientService : IPatientService`）
> - ✅ 通过构造函数注入Repository、Mapper、Logger等依赖
> - ✅ 避免使用抽象基类（如BaseService&lt;T&gt;）造成的过度设计
> - ✅ 符合YAGNI原则（You Aren't Gonna Need It），够用即好
> - ✅ **已移除EventBus依赖**（Epic #1725 Phase 1）- 7个Service类删除IEventBus注入，简化构造函数
>
> **演进触发条件**（参见ADR-005长期演进策略）：
> - 业务规则数 &gt;20条（当前~14条）→ 演进到富领域模型
> - Service方法长度 &gt;200行（当前&lt;100行）→ 拆分领域服务
> - 聚合根关系 &gt;3层（当前2层）→ 引入领域事件
>
> **实际实现示例**见下文"实际Service实现示例"章节。

> **📝 Epic #1725改进** - Service层简化（2025-10-30）：
> - ✅ **Phase 1**: 移除所有Service类的IEventBus冗余依赖（~14行代码）
> - ✅ **Phase 3**: PrescriptionService提取LoadRelatedDataAsync方法，消除重复逻辑（~30行）
> - ✅ 标注MVP性能限制（全量加载、N+1查询），为未来优化提供指引
> - 📖 详见[ADR-007](../decisions/ADR-007-repository-service-simplification.md)


### 实际Service实现示例

以下展示实际项目中PatientService的典型实现，完整体现MVP阶段的设计原则：

**接口定义** (LYBT.Module.Patients/Interfaces/IPatientService.cs)：
```csharp
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(int id);
    Task<ServiceResult<PagedResult<PatientListDto>>> GetPagedAsync(PatientQueryDto query);
    Task<ServiceResult<int>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult> UpdateAsync(int id, PatientUpdateDto dto);
    Task<ServiceResult> DeleteAsync(int id);
    Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync();
}
```

**Service实现** (LYBT.Module.Patients/Services/PatientService.cs)：
```csharp
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(int id)
    {
        try
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null)
                return ServiceResult<PatientDto>.Failure("患者不存在");

            var dto = _mapper.Map<PatientDto>(patient);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败：{PatientId}", id);
            return ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
    }

    public async Task<ServiceResult<PagedResult<PatientListDto>>> GetPagedAsync(PatientQueryDto query)
    {
        try
        {
            var pagedResult = await _repository.GetPagedAsync(
                query.PageIndex,
                query.PageSize,
                query.Keyword);

            var dtos = _mapper.Map<List<PatientListDto>>(pagedResult.Items);
            var result = new PagedResult<PatientListDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                PageIndex = pagedResult.PageIndex,
                PageSize = pagedResult.PageSize
            };

            return ServiceResult<PagedResult<PatientListDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者分页列表失败");
            return ServiceResult<PagedResult<PatientListDto>>.Failure("获取患者列表失败");
        }
    }

    public async Task<ServiceResult<int>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            // 业务规则验证（MVP阶段的简化验证）
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<int>.Failure("患者姓名不能为空");

            if (dto.Phone != null && await _repository.ExistsByPhoneAsync(dto.Phone))
                return ServiceResult<int>.Failure("该手机号已存在");

            var patient = _mapper.Map<Patient>(dto);
            patient.CreatedAt = DateTime.Now;

            var id = await _repository.AddAsync(patient);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("创建患者成功：{PatientId}", id);
            return ServiceResult<int>.Success(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return ServiceResult<int>.Failure("创建患者失败");
        }
    }
}
```

**设计特点**：
- ✅ 直接实现IPatientService接口，无抽象基类
- ✅ 构造函数注入依赖（Repository、Mapper、Logger）
- ✅ 业务逻辑集中在Service层（手机号唯一性验证）
- ✅ 统一的ServiceResult返回类型
- ✅ 完整的异常处理和日志记录
- ✅ 方法长度<100行，符合MVP标准

### 仓储模式实现

**Repository可见性约束**（Epic #1600 - Phase 3）：

为了强制执行聚合根模式（AR-001），所有Repository实现类从\`public\`改为\`internal\`，禁止外部直接访问Repository层。

**关键约束**：
- ✅ **Repository实现类**：必须为\`internal\`（7个模块全部执行）
- ✅ **Repository接口**：保持\`public\`（供依赖注入和测试使用）
- ✅ **单元测试访问**：通过\`InternalsVisibleTo\`属性允许测试项目访问内部类

**已应用的7个模块**：
1. \`ConsultationRepository\` - internal (LYBT.Module.Consultation)
2. \`PrescriptionRepository\` - internal (LYBT.Module.Prescriptions)
3. \`MedicalCaseRepository\` - internal (LYBT.Module.MedicalCase)
4. \`PatientRepository\` - internal (LYBT.Module.Patients)
5. \`UserRepository\` - internal (LYBT.Module.Users)
6. \`HerbRepository\` - internal (LYBT.Module.Herbs)
7. \`FormulaRepository\` - internal (LYBT.Module.Formula)

**InternalsVisibleTo配置**（项目文件示例）：
\`\`\`xml
<!-- 允许测试项目访问internal类 (Issue #1600 Phase 3) -->
<ItemGroup>
  <InternalsVisibleTo Include="LYBT.Module.{ModuleName}.Tests" />
</ItemGroup>
\`\`\`

**架构优势**：
- ✅ **强制聚合根模式**：外部模块无法直接访问Repository，必须通过Service层
- ✅ **依赖方向正确**：Presentation → Application → Infrastructure（单向依赖）
- ✅ **封装性增强**：Repository实现细节对外部模块隐藏
- ✅ **测试支持**：通过InternalsVisibleTo保证单元测试可访问

**BaseRepository抽象类模板**：

> **📝 Epic #1725改进** - 新增GetPagedResultAsync辅助方法（2025-10-30）：
> - ✅ 提取分页逻辑到protected辅助方法，避免每个Repository重复实现
> - ✅ 简化5个Repository实现（Consultation/Prescription/Formula/MedicalCase/Herb）
> - ✅ 减少~100行重复代码，提升可维护性
> - 📖 详见[ADR-007](../decisions/ADR-007-repository-service-simplification.md)

```csharp
/// <summary>
/// 标准仓储基类
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    // ✅ Epic #1725新增：分页辅助方法（2025-10-30）
    /// <summary>
    /// 分页辅助方法 - 统一处理分页逻辑
    /// </summary>
    protected async Task<PagedResult<T>> GetPagedResultAsync(
        IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize,
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = _dbSet.AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            Delete(entity);
        }
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

### 实际Repository实现示例

以下展示实际项目中PatientRepository的典型实现，体现Epic #1600的internal可见性约束：

**Repository接口** (LYBT.Module.Patients/Interfaces/IPatientRepository.cs)：
```csharp
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id);
    Task<PagedResult<Patient>> GetPagedAsync(int pageIndex, int pageSize, string? keyword);
    Task<bool> ExistsByPhoneAsync(string phone);
    Task<int> AddAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task DeleteAsync(int id);
    Task<int> SaveChangesAsync();
}
```

**Repository实现** (LYBT.Module.Patients/Repositories/PatientRepository.cs)：
```csharp
// ⚠️ 注意：实现类为internal，强制执行聚合根模式（Epic #1600 Phase 3）
internal class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.MedicalCases)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(int pageIndex, int pageSize, string? keyword)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                (p.Phone != null && p.Phone.Contains(keyword)) ||
                (p.IdCard != null && p.IdCard.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Patient>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsByPhoneAsync(string phone)
    {
        return await _context.Patients
            .AnyAsync(p => p.Phone == phone);
    }

    public async Task<int> AddAsync(Patient patient)
    {
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient.Id;
    }

    public async Task UpdateAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var patient = await GetByIdAsync(id);
        if (patient != null)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

**设计特点**：
- ⚠️ **internal可见性**：Repository实现类为internal，外部模块无法直接访问
- ✅ **接口public**：IPatientRepository接口保持public，供依赖注入使用
- ✅ **InternalsVisibleTo**：通过项目文件配置允许测试项目访问
- ✅ **EF Core最佳实践**：使用Include加载关联实体、AsNoTracking优化查询
- ✅ **强制聚合根模式**：外部模块必须通过PatientService访问，不能直接访问Repository

**项目文件配置** (LYBT.Module.Patients.csproj)：
```xml
<ItemGroup>
  <!-- 允许测试项目访问internal类 (Epic #1600 Phase 3) -->
  <InternalsVisibleTo Include="LYBT.Module.Patients.Tests" />
</ItemGroup>
```

## 🌐 API控制器设计

### Controller层实现原则

> **⚠️ 架构说明**：当前MVP阶段，Controller层采用**两层设计**模式，不使用单层泛型基类。
>
> **两层设计架构**：
> ```
> BaseControllerCore (Layer 1 - 核心基础设施)
>   ↓ 继承
> BaseApiController (Layer 2 - API响应包装)
>   ↓ 继承
> 具体Controller (Layer 3 - 业务逻辑)
> ```
>
> **设计优势**：
> - ✅ **职责分离**：核心功能（日志、操作者信息）与API包装（响应格式）分离
> - ✅ **灵活性高**：具体Controller可选择继承BaseControllerCore（需要核心功能）或BaseApiController（需要标准API响应）
> - ✅ **方法级泛型**：使用`HandleServiceResult&lt;T&gt;()`等方法级泛型，比类级泛型更灵活
> - ✅ **避免过度抽象**：不强制所有Controller实现固定的CRUD接口
>
> **核心组件**：
> - **BaseControllerCore** (src/Server/Core/LYBT.Infrastructure/Web/BaseControllerCore.cs) - 147行
>   - 提供：GetOperator()、LogOperation()、HandleExceptionCore()等核心方法
> - **BaseApiController** (src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs) - 475行
>   - 提供：Success&lt;T&gt;()、HandleServiceResult&lt;T&gt;()、HandlePagedServiceResult&lt;T&gt;()等API包装方法
>
> **实际实现示例**见下文"实际Controller实现示例"章节。

### 实际Controller实现示例

以下展示实际项目中PatientsController的典型实现，完整体现两层设计模式：

**Controller实现** (LYBT.WebAPI/Controllers/PatientsController.cs)：
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : BaseApiController
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _patientService.GetByIdAsync(id);
        return HandleServiceResult(result);
    }

    /// <summary>
    /// 获取患者分页列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] PatientQueryDto query)
    {
        var result = await _patientService.GetPagedAsync(query);
        return HandlePagedServiceResult(result);
    }

    /// <summary>
    /// 创建患者
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Data },
                Success(result.Data, "创建患者成功"));
        }
        return HandleServiceResult(result);
    }

    /// <summary>
    /// 更新患者
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] PatientUpdateDto dto)
    {
        var result = await _patientService.UpdateAsync(id, dto);
        return HandleServiceResult(result);
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patientService.DeleteAsync(id);
        return HandleServiceResult(result);
    }

    /// <summary>
    /// 获取患者统计信息
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<PatientStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _patientService.GetStatisticsAsync();
        return HandleServiceResult(result);
    }
}
```

**设计特点**：
- ✅ 继承BaseApiController，获得统一API响应格式
- ✅ 使用HandleServiceResult<T>()方法处理Service层返回结果
- ✅ 标准RESTful API设计（GET/POST/PUT/DELETE）
- ✅ API版本控制（v1）
- ✅ 完整的XML注释和Swagger文档支持
- ✅ 统一的异常处理（由BaseApiController提供）
- ✅ 方法级泛型，灵活性高

**BaseApiController提供的核心方法**：
- `Success<T>(T data, string message)` - 成功响应
- `HandleServiceResult<T>(ServiceResult<T> result)` - 处理Service结果
- `HandlePagedServiceResult<T>(ServiceResult<PagedResult<T>> result)` - 处理分页结果
- `BadRequest(string message)` - 错误请求响应
- `NotFound(string message)` - 未找到资源响应

## 🔗 数据访问层设计

### 实体框架配置
```csharp
/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets for entities
    public DbSet<User> Users { get; set; }
    public DbSet<AdminSecret> AdminSecrets { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<MedicalCase> MedicalCases { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    public DbSet<Herb> Herbs { get; set; }
    public DbSet<Formula> Formulas { get; set; }
    public DbSet<FormulaItem> FormulaItems { get; set; }
    public DbSet<HerbInventory> HerbInventory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity relationships
        ConfigureEntities(modelBuilder);
        
        // Configure indexes
        ConfigureIndexes(modelBuilder);
        
        // Configure query filters
        ConfigureQueryFilters(modelBuilder);
    }

    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(p => p.RowVersion).IsRowVersion();
            
            // Indexes
            entity.HasIndex(p => p.PhoneNumber);
            entity.HasIndex(p => p.IdentificationNumber).IsUnique();
            entity.HasIndex(p => new { p.Name, p.DateOfBirth });
            entity.HasIndex(p => p.Status, p.CreatedDate);
        });

        // MedicalCase configuration
        modelBuilder.Entity<MedicalCase>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.VisitDate).IsRequired();
            entity.Property(m => m.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(m => m.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(m => m.Patient)
                  .WithMany(p => p.MedicalCases)
                  .HasForeignKey(m => m.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Doctor)
                  .WithMany(d => d.MedicalCases)
                  .HasForeignKey(m => m.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(m => m.PatientId, m.VisitDate);
            entity.HasIndex(m => m.DoctorId, m.VisitDate);
            entity.HasIndex(m => m.Status, m.CreatedDate);
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PrescriptionDate).IsRequired();
            entity.Property(p => p.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(p => p.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(p => p.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(p => p.MedicalCase)
                  .WithMany(m => m.Prescriptions)
                  .HasForeignKey(p => p.MedicalCaseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Doctor)
                  .WithMany(d => d.Prescriptions)
                  .HasForeignKey(p => p.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(p => p.MedicalCaseId, p.PrescriptionDate);
            entity.HasIndex(p => p.DoctorId, p.PrescriptionDate);
            entity.HasIndex(p => p.Status, p.CreatedDate);
        });

        // Other entities...
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes
        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasIndex(pi => pi.PrescriptionId);
            entity.HasIndex(pi => pi.HerbId);
        });

        modelBuilder.Entity<HerbInventory>(entity =>
        {
            entity.HasIndex(hi => hi.HerbId);
            entity.HasIndex(hi => hi.TransactionDate);
        });
    }

    private void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        // Soft delete filters
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasQueryFilter(p => p.Status != "Deleted");
        });

        modelBuilder.Entity<MedicalCase>(entity =>
        {
            entity.HasQueryFilter(m => m.Status != "Deleted");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasQueryFilter(p => p.Status != "Deleted");
        });

        modelBuilder.Entity<Herb>(entity =>
        {
            entity.HasQueryFilter(h => h.IsActive);
        });
    }
}
```

## 🔧 依赖注入配置

> **⚠️ 核心架构原则**：本节记录Issue #1732服务注册架构重构的演进历史和设计理念

### 服务注册架构演进历史

#### Issue #1732 (2025-10-31): 服务注册架构重构

**演进动机（MVP原则）**:
- ❌ **旧架构**：`UnifiedServiceRegistration.cs` - 单一类包含所有服务注册逻辑
  - **过度抽象**：将所有服务注册集中在一个类中，违反单一职责原则
  - **可维护性差**：超过500行代码，混合了数据库、认证、API、业务模块等不同职责
  - **违背MVP原则**："够用即好"，无需过早抽象

- ✅ **新架构**：按职责拆分为4个`ServiceCollectionExtensions`文件
  - **清晰职责**：每个文件负责一个明确的领域
  - **易于维护**：每个文件100-200行，职责单一
  - **符合MVP原则**：简单直接，无过度抽象

**架构拆分策略**:

```
📁 src/Server/Services/LYBT.WebAPI/Extensions/
├── ServiceCollectionExtensions.cs              (主协调器)
├── DatabaseServiceCollectionExtensions.cs      (基础设施)
├── AuthenticationServiceCollectionExtensions.cs (认证安全)
└── ApiServiceCollectionExtensions.cs           (API文档)
```

---

### 4个ServiceCollectionExtensions文件职责

#### 1. ServiceCollectionExtensions.cs（主协调器）

**职责**：主入口协调、业务模块注册、控制器配置

**核心方法**:
```csharp
/// <summary>
/// 服务注册主入口
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分并重组
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册应用服务（统一入口）
    /// 协调10个步骤的服务注册
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 1）基础设施（数据库、缓存、健康检查）
        services.RegisterInfrastructureServices(configuration);

        // 2）认证与安全（JWT、授权策略）
        services.RegisterAuthenticationServices(configuration);

        // 3）业务模块（8个业务模块）
        services.RegisterBusinessModules(configuration);

        // 4）API 文档（Swagger、API版本）
        services.RegisterApiServices();

        // 5）控制器与 JSON（FluentValidation、JSON序列化）
        services.RegisterControllerServices(configuration);

        // 6）速率限制（Login端点防暴力攻击）
        services.ConfigureRateLimiting(configuration, environment);

        // 7）性能优化
        services.ConfigurePerformanceOptimizations(configuration);

        // 8）API 版本管理（已整合到RegisterApiServices）

        // 9）安全服务（数据保护、密钥管理）
        services.AddSecurityServices(configuration, environment);

        // 10）环境感知配置校验（生产强校验）
        services.AddEnvironmentAwareValidation(environment);

        return services;
    }

    /// <summary>
    /// 注册业务模块
    /// 使用各模块的静态扩展方法进行注册
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(...)
    {
        // 8个业务模块按依赖顺序注册
        services.AddAuthModule(configuration);           // 1. 认证模块
        services.AddUsersModule(configuration);          // 2. 用户模块
        services.AddPatientsModule(configuration);       // 3. 患者模块
        services.AddHerbsModule(configuration);          // 4. 中药模块
        services.AddConsultationModule(configuration);   // 5. 问诊模块
        services.AddPrescriptionsModule();               // 6. 处方模块
        services.AddFormulaModule();                     // 7. 配方模块
        services.AddMedicalCaseModule();                 // 8. 病例模块

        return services;
    }

    /// <summary>
    /// 注册控制器与 JSON 配置
    /// Epic #1731 Phase 3: 集成FluentValidation到ASP.NET Core Pipeline
    /// </summary>
    private static IServiceCollection RegisterControllerServices(...)
    {
        // FluentValidation全局自动验证
        services.AddFluentValidationAutoValidation(config =>
        {
            config.DisableDataAnnotationsValidation = false; // 保留DataAnnotations
        });
        services.AddFluentValidationClientsideAdapters();

        // JSON序列化配置、自动400响应等
        services.AddControllers().AddJsonOptions(...);
        services.Configure<ApiBehaviorOptions>(...);

        return services;
    }
}
```

---

#### 2. DatabaseServiceCollectionExtensions.cs（基础设施）

**职责**：数据库配置、健康检查、性能优化

**核心方法**:
```csharp
/// <summary>
/// 数据库与基础设施服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// 注册基础设施服务
    /// 包含：DbContext、连接池、健康检查、性能优化
    /// </summary>
    public static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. DbContext配置（连接池、查询跟踪、日志）
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                sqlOptions.CommandTimeout(30);
            });
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // 2. 健康检查
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddSqlServer(connectionString);

        // 3. 性能优化（内存缓存、响应压缩、响应缓存）
        services.AddMemoryCache();
        services.AddResponseCompression();
        services.AddResponseCaching();

        return services;
    }
}
```

---

#### 3. AuthenticationServiceCollectionExtensions.cs（认证安全）

**职责**：JWT认证配置、授权策略

**核心方法**:
```csharp
/// <summary>
/// 认证与授权服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// 注册认证与授权服务
    /// 包含：JWT认证、授权策略、多密钥轮换支持
    /// </summary>
    public static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. JWT 认证（支持环境变量覆盖）
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                       configuration["Lybt:Authentication:Jwt:SecretKey"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.FromSeconds(300),
                    TryAllIssuerSigningKeys = true // 支持密钥轮换
                };
            });

        // 2. 授权策略
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireRole("Doctor", "Admin"));
        });

        return services;
    }
}
```

---

#### 4. ApiServiceCollectionExtensions.cs（API文档）

**职责**：Swagger配置、API版本管理、速率限制

**核心方法**:
```csharp
/// <summary>
/// API服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// 注册API服务
    /// 包含：Swagger、API版本管理、ProblemDetails、AutoMapper
    /// </summary>
    public static IServiceCollection RegisterApiServices(
        this IServiceCollection services)
    {
        // 1. Swagger配置
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所管理系统 API",
                Version = "v1.0.0",
                Description = "基于ASP.NET Core 8.0的中医诊所管理系统Web API"
            });

            // JWT Bearer配置
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
        });

        // 2. API版本管理
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        // 3. ProblemDetails配置
        services.AddProblemDetails();

        // 4. AutoMapper配置
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        return services;
    }

    /// <summary>
    /// 配置速率限制
    /// 防止Login端点暴力攻击
    /// </summary>
    public static IServiceCollection ConfigureRateLimiting(...)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("LoginRateLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }
}
```

---

### Program.cs 使用示例

**入口调用**（src/Server/Services/LYBT.WebAPI/Program.cs:53）:
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ✅ 单一入口：调用RegisterAllApplicationServices
        builder.Services.RegisterAllApplicationServices(
            builder.Configuration,
            builder.Environment);

        var app = builder.Build();

        // 配置中间件
        app.ConfigureAllMiddleware();

        await app.RunAsync();
    }
}
```

**加载顺序说明**:
1. **基础设施优先**：数据库连接必须最先建立（RegisterInfrastructureServices）
2. **认证配置**：依赖基础设施的配置加载（RegisterAuthenticationServices）
3. **业务模块**：依赖数据库和认证（RegisterBusinessModules）
4. **API文档**：依赖业务模块的Controller定义（RegisterApiServices）
5. **控制器配置**：最后配置FluentValidation和JSON序列化（RegisterControllerServices）

---

### MVP设计理念总结

#### ✅ 符合MVP的设计（Issue #1732 新架构）

**原则**："够用即好" + "按职责拆分" + "避免过度抽象"

1. **职责清晰**：4个文件各司其职，边界明确
2. **易于维护**：每个文件100-200行，修改影响范围小
3. **简单直接**：静态扩展方法，无复杂继承和抽象层级
4. **易于测试**：职责单一，依赖关系清晰

#### ❌ 违背MVP的设计（旧UnifiedServiceRegistration）

**问题**："过度抽象" + "职责混乱" + "维护困难"

1. **职责过重**：单一类包含所有服务注册（数据库+认证+API+业务）
2. **难以维护**：超过500行代码，修改影响范围大
3. **违背单一职责**：违反SOLID原则的Single Responsibility Principle
4. **过早抽象**：MVP阶段无需将所有服务集中在一个类中

## 🎯 架构质量保证

### 代码质量标准
- ✅ **SOLID原则**：单一职责、开闭原则、里氏替换、接口隔离、依赖倒置
- ✅ **DDD领域驱动**：领域模型、领域服务、聚合根、值对象
- ✅ **Clean Code**：可读性、可维护性、命名规范、注释完整
- ✅ **Error Handling**：异常处理、错误恢复、日志记录

### 架构验证
- ✅ **分层验证**：确保代码严格遵循分层架构
- ✅ **依赖验证**：检查依赖方向和循环依赖
- ✅ **接口验证**：接口定义和实现一致性
- ✅ **性能验证**：查询性能、内存使用、并发处理

### 文档同步
- ✅ **实时同步**：代码变更后立即更新文档
- ✅ **准确一致**：文档内容与实际代码完全匹配
- ✅ **版本管理**：文档版本与代码版本对应
- ✅ **用户反馈**：收集使用反馈并持续改进

## 🔗 相关资源

### 📚 深度参考
- [深度参考文档](../../deep/README.md) - 完整技术细节
- [API设计最佳实践](../../deep/api-design-best-practices.md) - API架构规范
- [性能优化指南](../../deep/performance-optimization.md) - 性能架构优化

### 📋 架构决策记录 (ADR)
- [ADR-001: FluentValidation作为统一验证框架](../decisions/ADR-001-fluentvalidation-as-validation-framework.md)
- [ADR-002: AutoMapper作为映射框架](../decisions/ADR-002-automapper-as-mapping-framework.md)
- [ADR-003: Prescriptions/Consultation Repository层简化](../decisions/ADR-003-repository-simplification.md)
- [ADR-005: 聚合根长期架构演进策略](../decisions/ADR-005-aggregate-root-long-term-architecture.md)
- [ADR-006: MedicalCase/Consultation/Prescription架构重构](../decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md)
- [ADR-007: Repository和Service层简化重构](../decisions/ADR-007-repository-service-simplification.md) ⭐ Epic #1725

### 🛠️ 开发指南
- [开发指南总览](../../development/README.md) - 开发规范和流程
- [Server端开发](../../development/server/README.md) - Server开发规范
- [测试策略指南](../../deep/testing-strategies.md) - 架构测试策略

### 📊 监控和维护
- [文档使用指标](../../support/documentation-metrics.md) - 文档质量监控
- [文档维护指南](../../support/documentation-maintenance.md) - 文档维护流程

---

**Server端架构指南** - 为凌隐宝堂中医诊所提供稳定、可扩展、高性能的服务器端架构设计 🏗️

*本架构指南基于实际代码架构编写，确保架构设计与实现完全一致。如有架构问题或建议，请通过相应渠道反馈。*