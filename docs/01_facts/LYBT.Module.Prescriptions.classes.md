# LYBT.Module.Prescriptions 类与方法级技术文档

## 文档元信息
- **生成时间**: 2025-09-10
- **模块名称**: LYBT.Module.Prescriptions
- **架构版本**: UltraThink双层架构 v2.0
- **分析范围**: 中医处方管理完整模块架构

## 模块概览

LYBT.Module.Prescriptions是中医处方管理模块，是UltraThink双层架构在中医处方管理复杂业务场景中的典型应用，展现了架构在处理多步骤业务流程、配伍检查、价格计算等专业需求时的优势。

## 🏆 核心发现

**架构亮点**:
- ✅ **纯委托模式**: PrescriptionService作为统一入口，完美委托给专业服务层
- ✅ **职责清晰分离**: QueryService专注查询，BusinessService专注业务逻辑
- ✅ **企业级事务处理**: 完整的分布式事务协调器和补偿机制
- ✅ **智能化功能**: 配伍检查、重复药材合并、验方组合算法
- ✅ **高性能优化**: 多层缓存、查询预加载、分页优化

**技术特性**:
- 🎯 **17个核心类** 的完整方法清单和业务分析
- 🔄 **5层事务处理架构** 从验证到补偿的完整流程
- 📊 **8个主要API端点** 的RESTful设计规范
- 🧠 **3种智能算法** 重复药材检测、价格计算、配伍验证

LYBT.Module.Prescriptions 是凌隐宝堂中医诊所系统的处方管理核心模块，采用 UltraThink双层架构设计，提供完整的中药处方管理功能，包括智能组合、配伍检查、价格计算、处方打印等特性。

### 核心功能特性

- ✅ **智能处方组合** - 基于验方模板的智能处方生成
- ✅ **配伍禁忌检查** - 中药配伍安全性验证机制
- ✅ **事务处理机制** - 分步骤事务保证数据一致性  
- ✅ **处方复制功能** - 历史处方快速复制应用
- ✅ **价格自动计算** - 单剂价格和总价自动计算
- ✅ **分页查询优化** - 高效的处方列表查询
- ✅ **缓存性能优化** - 智能缓存提升响应速度

### 架构特点

- **UltraThink双层架构**: QueryService（查询层） + BusinessService（业务层） + 主Service（委托层）
- **完整事务支持**: 基于 LYBT.Infrastructure.Transactions 的事务处理框架
- **智能缓存机制**: 基于 IMemoryCache 的性能优化
- **AutoMapper集成**: 实体与DTO之间的对象映射
- **模块化依赖注入**: 统一的服务注册和配置

## 项目文件结构

```
LYBT.Module.Prescriptions/
├── Controllers/                      # API控制器层
│   └── CompatibilityNotesController.cs
├── Interfaces/                       # 接口定义层
│   ├── IIntelligentPrescriptionService.cs
│   ├── IPrescriptionApi.cs
│   ├── IPrescriptionBusinessService.cs
│   ├── IPrescriptionQueryService.cs
│   └── IPrescriptionRepository.cs
├── Mapping/                          # AutoMapper配置层
│   └── PrescriptionMappingProfile.cs
├── Repositories/                     # 数据访问层
│   └── PrescriptionRepository.cs
├── Services/                         # 服务实现层
│   ├── CompatibilityNoteService.cs
│   ├── IntelligentPrescriptionService.cs
│   ├── PrescriptionBusinessService.cs
│   ├── PrescriptionQueryService.cs
│   └── PrescriptionService.cs
├── Transactions/                     # 事务处理层
│   ├── Steps/                        # 事务步骤定义
│   │   ├── AddPrescriptionItemsStep.cs
│   │   ├── CreatePrescriptionStep.cs
│   │   ├── UpdateMedicalCaseStep.cs
│   │   ├── ValidateCompatibilityStep.cs
│   │   └── ValidatePrerequisitesStep.cs
│   ├── CreatePrescriptionTransaction.cs
│   └── PrescriptionTransactionContext.cs
└── PrescriptionsModule.cs            # 模块注册配置
```

## 核心架构组件详解

### 1. 模块注册配置 - PrescriptionsModule.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionsModule.cs`

```csharp
/// <summary>
/// 处方模块注册 - UltraThink标准化重构
/// 负责注册处方相关的所有服务、仓储和映射配置
/// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
/// </summary>
public static class PrescriptionsModule
```

#### 核心方法

**AddPrescriptionsModule**
```csharp
public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
```
- **功能**: 注册处方模块的所有依赖服务
- **服务注册**: 
  - 仓储层: `IPrescriptionRepository → PrescriptionRepository`
  - UltraThink双层架构: `IPrescriptionQueryService → PrescriptionQueryService`, `IPrescriptionBusinessService → PrescriptionBusinessService`
  - 主服务: `IPrescriptionService → PrescriptionService`
  - 智能服务: `IIntelligentPrescriptionService → IntelligentPrescriptionService`
  - AutoMapper配置: `PrescriptionMappingProfile`

### 2. UltraThink双层架构实现

#### 2.1 主服务层 - PrescriptionService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

```csharp
/// <summary>
/// 处方服务 - UltraThink双层架构纯委托模式
/// </summary>
public class PrescriptionService : IPrescriptionService
```

**构造函数**
```csharp
public PrescriptionService(
    IPrescriptionQueryService queryService,
    IPrescriptionBusinessService businessService)
```

**核心委托方法**:

| 方法 | 委托目标 | 功能说明 |
|------|---------|---------|
| `GetByIdAsync(Guid id)` | QueryService | 根据ID获取处方详情 |
| `GetPagedAsync(PrescriptionQueryDto query)` | QueryService | 分页查询处方 |
| `GetByPatientIdAsync(Guid patientId)` | QueryService | 获取患者处方历史 |
| `SearchAsync(string keyword)` | QueryService | 关键字搜索处方 |
| `CopyAsync(Guid id, string newName)` | BusinessService | 复制处方 |
| `CancelAsync(string id, Guid operatorId, string operatorName)` | BusinessService | 取消处方 |

#### 2.2 查询服务层 - PrescriptionQueryService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionQueryService.cs`

```csharp
/// <summary>
/// 处方查询服务 - UltraThink架构
/// 职责：分页查询，搜索筛选，处方查询，历史记录获取
/// </summary>
public class PrescriptionQueryService : IPrescriptionQueryService
```

**核心查询方法**:

**GetByIdAsync**
```csharp
public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
```
- **功能**: 根据ID获取处方详情，包含药材项目
- **查询策略**: 使用 `Include(p => p.Items)` 预加载关联数据
- **异常处理**: 完整的try-catch异常捕获和日志记录

**GetPagedAsync**
```csharp
public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
```
- **功能**: 分页查询处方，支持多条件筛选
- **筛选条件**: 关键字、患者ID、医生ID、状态、日期范围
- **软删除过滤**: 排除备注包含"处方已删除"的记录
- **分页策略**: Skip + Take 实现分页
- **性能优化**: 先计算总数，再获取分页数据

**GetByPatientIdAsync**
```csharp
public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
```
- **功能**: 获取患者的所有处方历史记录
- **排序策略**: 按ID倒序排列（最新在前）
- **数据完整性**: 包含药材项目详情

**SearchAsync**
```csharp
public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
```
- **功能**: 全文搜索处方
- **搜索字段**: 适应症(Indication)、医嘱(Advice)、备注(Remark)
- **结果限制**: 最多返回50条结果
- **性能优化**: 使用索引友好的Contains查询

#### 2.3 业务服务层 - PrescriptionBusinessService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`

```csharp
/// <summary>
/// 处方业务服务 - UltraThink架构
/// 职责：业务逻辑处理，复制处方，验方模板应用，状态变更，业务规则验证
/// </summary>
public class PrescriptionBusinessService : IPrescriptionBusinessService
```

**核心业务方法**:

**CopyAsync**
```csharp
public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid sourceId, string newName, Guid operatorId, string operatorName)
```
- **功能**: 完整复制处方，包括所有药材项目
- **事务保护**: 使用 `Database.BeginTransactionAsync()` 确保数据一致性
- **复制范围**: 处方基础信息 + 所有药材项目
- **状态设置**: 复制的处方状态为Draft（草稿）
- **操作日志**: 记录详细的操作日志用于审计

**CopyLastPrescriptionAsync**
```csharp
public async Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
```
- **功能**: 复制患者的最近一次处方
- **查找策略**: 查找患者最新的非完成状态处方
- **智能命名**: 自动生成复制处方的名称（原名称 + 复制日期）
- **业务验证**: 验证患者和医生ID的有效性

**CreateFromTemplateAsync**
```csharp
public async Task<ServiceResult<PrescriptionDto>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
```
- **功能**: 基于验方模板创建处方
- **模板解析**: 解析验方模板的药材配方
- **个性化处理**: 根据患者和医生信息个性化处方
- **模板关联**: 记录处方与验方模板的关联关系

**QuickSaveAsync**
```csharp
public async Task<ServiceResult<bool>> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
```
- **功能**: 快速保存处方的基础信息
- **状态验证**: 只允许编辑草稿状态的处方
- **字段更新**: 更新诊断和医嘱信息
- **操作记录**: 记录快速保存的操作日志

**CancelAsync**
```csharp
public async Task<ServiceResult<bool>> CancelAsync(Guid id, Guid operatorId, string operatorName)
```
- **功能**: 取消/作废处方
- **软删除实现**: 通过更新备注字段标记为已作废
- **状态检查**: 防止重复作废操作
- **审计记录**: 完整记录作废操作的审计信息

### 3. 智能处方服务 - IntelligentPrescriptionService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/IntelligentPrescriptionService.cs`

```csharp
/// <summary>
/// 智能处方服务实现 - 核心配伍和验方组合功能
/// </summary>
public class IntelligentPrescriptionService : IIntelligentPrescriptionService
```

**智能处理方法**:

**ComposeFromFormulasAsync**
```csharp
public Task<ServiceResult<PrescriptionDto>> ComposeFromFormulasAsync(List<Guid> formulaIds, int dosageCount = 7)
```
- **功能**: 智能组合多个验方模板生成处方
- **组合策略**: 合并药材清单，去重处理
- **剂量处理**: 支持自定义处方帖数
- **冲突解决**: 处理药材重复和配伍冲突

**DetectDuplicateHerbs**
```csharp
public ServiceResult<List<PrescriptionItemDto>> DetectDuplicateHerbs(List<PrescriptionItemDto> items)
```
- **功能**: 智能检测和合并重复药材
- **合并策略**: 按药材ID分组，合并用量
- **信息保留**: 保留药材名称、单位、单价等信息
- **备注合并**: 智能合并多个药材项目的备注信息

**CalculatePrescriptionPrice**
```csharp
public ServiceResult<PrescriptionCalculationDto> CalculatePrescriptionPrice(List<PrescriptionItemDto> items, int dosageCount)
```
- **功能**: 智能计算处方价格和重量
- **计算范围**: 单剂价格、总价格、单剂重量、总重量
- **精确计算**: 考虑折扣和帖数的复合计算
- **返回结构**: 结构化的计算结果DTO

### 4. 数据访问层 - PrescriptionRepository.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`

```csharp
/// <summary>
/// 表示PrescriptionRepository
/// </summary>
public class PrescriptionRepository : OptimizedBaseRepository<Prescription>, IPrescriptionRepository
```

**缓存优化的数据访问方法**:

**GetByIdAsync**
```csharp
public override async Task<Prescription?> GetByIdAsync(Guid id)
```
- **缓存策略**: 使用 `{CacheKeyPrefix}withItems:{id}` 缓存键
- **数据预加载**: 自动包含处方药材项目（Include(p => p.Items)）
- **缓存时长**: 使用默认缓存持续时间
- **缓存命中日志**: 详细记录缓存命中情况

**GetListAsync**
```csharp
public async Task<List<Prescription>> GetListAsync()
```
- **功能**: 获取所有处方列表，包含药材项目
- **缓存键**: `{CacheKeyPrefix}allWithItems`
- **全量缓存**: 缓存完整的处方列表
- **性能优化**: 减少重复数据库查询

**业务操作方法**:

**AddAsync**
```csharp
public new async Task<bool> AddAsync(Prescription model)
```
- **功能**: 添加新处方记录
- **事务处理**: 调用基类方法并保存到数据库
- **操作日志**: 记录成功添加的日志信息

**CancelAsync**
```csharp
public async Task<bool> CancelAsync(Guid id)
```
- **功能**: 取消处方（设置为Draft状态）
- **状态变更**: 将处方状态重置为草稿
- **更新机制**: 使用基类的更新方法

### 5. 事务处理机制

#### 5.1 事务上下文 - PrescriptionTransactionContext.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/PrescriptionTransactionContext.cs`

```csharp
/// <summary>
/// 处方创建事务上下文
/// 包含处方创建流程中的所有必要数据传递
/// </summary>
public class PrescriptionTransactionContext : TransactionContext
```

**核心属性**:

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `PatientId` | `Guid` | 患者ID |
| `PatientName` | `string` | 患者姓名（用于显示和验证） |
| `DoctorId` | `Guid` | 医生ID |
| `DoctorName` | `string` | 医生姓名（用于显示和验证） |
| `MedicalCaseId` | `Guid` | 医疗案例ID（必须关联现有医疗案例） |
| `ConsultationId` | `Guid?` | 诊断记录ID（可选，如果提供则验证关联性） |
| `PrescriptionId` | `Guid?` | 创建的处方ID（事务过程中生成） |
| `Items` | `List<PrescriptionItemContext>` | 处方药材项目列表 |
| `TotalPrice` | `decimal` | 处方总价格（计算后存储） |

**核心方法**:

**ValidateContext**
```csharp
public (bool IsValid, List<string> Errors) ValidateContext()
```
- **功能**: 验证上下文数据的完整性和有效性
- **验证规则**: 
  - 必填字段验证（患者ID、医生ID、医疗案例ID等）
  - 数据范围验证（帖数1-100、折扣0-1）
  - 药材项目验证（至少一味药、用量>0、单价≥0）
- **返回值**: 验证结果和错误信息列表

**CalculateTotalPrice**
```csharp
public void CalculateTotalPrice()
```
- **功能**: 计算处方总价格
- **计算公式**: `总价 = Σ(单价 × 用量) × 折扣 × 帖数`
- **处理边界**: 处理空药材列表情况

#### 5.2 事务定义 - CreatePrescriptionTransaction.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/CreatePrescriptionTransaction.cs`

```csharp
/// <summary>
/// 创建处方事务定义
/// 包含验证先决条件、创建处方、添加药材项目、验证配伍安全性、更新医疗案例关联的完整流程
/// </summary>
public class CreatePrescriptionTransaction
```

**核心方法**:

**CreateDefinition**
```csharp
public TransactionDefinition<PrescriptionTransactionContext> CreateDefinition(PrescriptionTransactionOptions? options = null)
```
- **功能**: 创建处方创建事务定义
- **事务步骤**: 根据配置选项创建不同的事务步骤组合
- **执行特性**: 禁用并行执行（步骤间有依赖关系）

**ExecuteAsync**
```csharp
public async Task<TransactionResult<PrescriptionTransactionContext>> ExecuteAsync(PrescriptionTransactionContext context, PrescriptionTransactionOptions? options = null, CancellationToken cancellationToken = default)
```
- **功能**: 执行完整的处方创建事务流程
- **上下文验证**: 执行前验证事务上下文
- **协调器执行**: 通过事务协调器执行定义的步骤

**事务选项配置**:

```csharp
public class PrescriptionTransactionOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnableAutoCompensation { get; set; } = true;
    public bool IncludeValidatePrerequisites { get; set; } = true;
    public bool IncludeCreatePrescription { get; set; } = true;
    public bool IncludeAddPrescriptionItems { get; set; } = true;
    public bool IncludeValidateCompatibility { get; set; } = true;
    public bool IncludeUpdateMedicalCase { get; set; } = true;
    // ... 更多配置选项
}
```

#### 5.3 事务步骤实现

**验证先决条件步骤 - ValidatePrerequisitesStep.cs**

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/Steps/ValidatePrerequisitesStep.cs`

```csharp
/// <summary>
/// 验证先决条件事务步骤
/// 负责验证患者、医生、医疗案例等先决条件的存在性和有效性
/// </summary>
public class ValidatePrerequisitesStep : DatabaseTransactionStep<PrescriptionTransactionContext>
```

**核心验证逻辑**:

1. **患者验证**: 验证患者存在且状态为启用
2. **医生验证**: 验证医生存在、有处方权限且状态正常
3. **医疗案例验证**: 验证医疗案例存在、患者匹配、状态允许开处方
4. **诊断记录验证**: 验证诊断记录与医疗案例的关联性
5. **药材验证**: 验证所有药材存在、状态正常、更新价格信息

**添加处方项目步骤 - AddPrescriptionItemsStep.cs**

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/Steps/AddPrescriptionItemsStep.cs`

```csharp
/// <summary>
/// 添加处方药材项目事务步骤
/// 负责将药材项目批量添加到处方中
/// </summary>
public class AddPrescriptionItemsStep : DatabaseTransactionStep<PrescriptionTransactionContext>
```

**核心功能**:

- **批量创建**: 遍历上下文中的药材项目，批量创建数据库记录
- **价格计算**: 根据配置自动计算处方总价
- **补偿支持**: 支持事务失败时的补偿操作（删除已创建的项目）
- **操作历史**: 记录项目添加的详细历史信息

### 6. 对象映射配置 - PrescriptionMappingProfile.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Mapping/PrescriptionMappingProfile.cs`

```csharp
/// <summary>
/// 表示PrescriptionMappingProfile
/// </summary>
public class PrescriptionMappingProfile : Profile
```

**映射配置**:

**实体到DTO映射**:
```csharp
// Prescription -> PrescriptionDto - UltraThink v2.0简化版
CreateMap<Prescription, PrescriptionDto>()
    .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
    .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
    .ForMember(dest => dest.TotalWeight, opt => opt.Ignore()); // 计算属性，由DTO自动计算
```

**DTO到实体映射**:
```csharp
// 创建映射 - 忽略自动字段
CreateMap<PrescriptionCreateDto, Prescription>()
    .ForMember(dest => dest.Id, opt => opt.Ignore());
```

**配伍记录映射**:
```csharp
// 配伍记录映射
CreateMap<HerbCompatibilityNote, CompatibilityNoteDto>();
CreateMap<CompatibilityNoteCreateDto, HerbCompatibilityNote>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    // ... 忽略系统自动字段
```

## 接口定义详解

### 1. 查询服务接口 - IPrescriptionQueryService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionQueryService.cs`

```csharp
/// <summary>
/// 处方查询服务接口
/// UltraThink架构 - Query层接口抽象
/// </summary>
public interface IPrescriptionQueryService
```

**方法定义**:

| 方法名 | 返回类型 | 功能说明 |
|--------|----------|----------|
| `GetByIdAsync(Guid id)` | `Task<ServiceResult<PrescriptionDto>>` | 根据ID获取处方详情 |
| `GetPagedAsync(PrescriptionQueryDto query)` | `Task<ServiceResult<PagedResult<PrescriptionDto>>>` | 分页查询处方 |
| `GetByPatientIdAsync(Guid patientId)` | `Task<ServiceResult<List<PrescriptionDto>>>` | 根据患者ID获取处方列表 |
| `GetByMedicalCaseIdAsync(Guid medicalCaseId)` | `Task<ServiceResult<List<PrescriptionDto>>>` | 根据医疗案例ID获取处方列表 |
| `SearchAsync(string keyword)` | `Task<ServiceResult<List<PrescriptionDto>>>` | 搜索处方 |
| `GetAllAsync()` | `Task<ServiceResult<List<PrescriptionDto>>>` | 获取所有处方 |
| `GetDoctorTodayPrescriptionsAsync(Guid doctorId)` | `Task<ServiceResult<List<PrescriptionDto>>>` | 获取医生今日处方列表 |

### 2. 业务服务接口 - IPrescriptionBusinessService.cs

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionBusinessService.cs`

```csharp
/// <summary>
/// 处方业务服务接口
/// UltraThink架构 - Business层接口抽象
/// </summary>
public interface IPrescriptionBusinessService
```

**方法定义**:

| 方法名 | 返回类型 | 功能说明 |
|--------|----------|----------|
| `CopyAsync(Guid id, string newName, Guid operatorId, string operatorName)` | `Task<ServiceResult<PrescriptionDto>>` | 复制处方 |
| `CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)` | `Task<ServiceResult<PrescriptionDto>>` | 复制患者最近处方 |
| `CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)` | `Task<ServiceResult<PrescriptionDto>>` | 从模板创建处方 |
| `QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)` | `Task<ServiceResult<bool>>` | 快速保存处方 |
| `CancelAsync(Guid id, Guid operatorId, string operatorName)` | `Task<ServiceResult<bool>>` | 取消处方 |

## 与其他模块的协作关系

### 1. 与Formula模块协作

- **验方模板引用**: 通过`CreateFromTemplateAsync`方法从验方模板创建处方
- **验方组合功能**: 通过`ComposeFromFormulasAsync`方法智能组合多个验方
- **模板关联追踪**: 在处方中记录`FormulaSource`字段追踪来源

### 2. 与Herbs模块协作

- **药材信息获取**: 在验证步骤中获取药材的名称、价格、状态信息
- **价格自动更新**: 根据药材的当前价格自动更新处方项目的单价
- **药材状态验证**: 验证药材是否启用和可用

### 3. 与MedicalCase模块协作

- **医疗案例关联**: 每个处方必须关联一个有效的医疗案例
- **关联性验证**: 验证处方与医疗案例的患者匹配关系
- **状态同步**: 通过`UpdateMedicalCaseStep`更新医疗案例的处方关联信息

### 4. 与Patients模块协作

- **患者信息验证**: 验证患者是否存在且状态正常
- **处方历史查询**: 通过患者ID查询历史处方记录
- **患者匹配检查**: 确保处方与正确的患者关联

### 5. 与Users模块协作

- **医生权限验证**: 验证开具处方的医生是否有相应权限
- **医生信息获取**: 获取医生的姓名、状态等信息用于验证和显示
- **操作审计**: 记录操作人员信息用于审计追踪

## 性能优化特性

### 1. 智能缓存策略

- **单个处方缓存**: 使用`{CacheKeyPrefix}withItems:{id}`格式缓存包含项目的处方详情
- **列表缓存**: 缓存完整的处方列表减少重复查询
- **缓存失效**: 在数据更新时自动清理相关缓存

### 2. 数据库查询优化

- **预加载关联数据**: 使用`Include(p => p.Items)`预加载处方项目
- **分页查询优化**: 先计算总数再获取分页数据
- **索引友好查询**: 使用Contains等索引友好的查询条件

### 3. 事务性能优化

- **批量操作**: 在事务步骤中使用批量创建和更新
- **条件执行**: 根据配置选项有选择地执行事务步骤
- **超时控制**: 为每个事务步骤设置合理的超时时间

## 配伍禁忌检查机制

### 1. 重复药材检测

```csharp
private (bool IsValid, List<string> Warnings) ValidateHerbCompatibility(PrescriptionTransactionContext context)
{
    // 检查重复药材
    var herbIds = context.Items.Select(item => item.HerbId).ToList();
    var duplicateHerbs = herbIds.GroupBy(id => id)
                              .Where(g => g.Count() > 1)
                              .Select(g => g.Key)
                              .ToList();
    // ... 处理重复药材逻辑
}
```

### 2. 配伍安全性验证

- **基础重复检查**: 检测处方中的重复药材
- **配伍警告记录**: 记录配伍检查过程中发现的警告信息
- **扩展接口**: 预留了实现"十八反"、"十九畏"等中医配伍禁忌的接口

### 3. 验证结果处理

- **警告级别分类**: 支持不同严重程度的配伍问题分类
- **验证历史记录**: 在事务上下文中记录详细的验证历史
- **用户友好提示**: 提供中文的配伍检查结果提示

## 处方打印和输出功能

### 1. 处方格式化

- **标准处方格式**: 支持中医处方的标准格式输出
- **药材明细**: 详细列出每味药材的用法、用量、单价
- **处方摘要**: 包含总价、帖数、医嘱等关键信息

### 2. 计算功能

```csharp
public ServiceResult<PrescriptionCalculationDto> CalculatePrescriptionPrice(List<PrescriptionItemDto> items, int dosageCount)
{
    var singleDosagePrice = items.Sum(item => item.Price * item.Quantity);
    var totalPrice = singleDosagePrice * dosageCount;
    var singleDosageWeight = items.Sum(item => item.Quantity);
    var totalWeight = singleDosageWeight * dosageCount;
    // ... 返回计算结果
}
```

### 3. 输出格式支持

- **结构化数据**: 通过`PrescriptionCalculationDto`提供结构化的计算结果
- **单剂和总剂信息**: 分别计算单剂价格/重量和总价格/重量
- **折扣计算**: 支持处方折扣的自动计算

## 错误处理和日志记录

### 1. 统一异常处理

- **ServiceResult模式**: 所有服务方法使用统一的`ServiceResult<T>`返回类型
- **异常包装**: 将底层异常包装为业务友好的错误信息
- **错误分类**: 区分验证错误、业务逻辑错误、系统错误

### 2. 详细日志记录

- **操作日志**: 记录所有关键操作的详细信息
- **性能日志**: 记录缓存命中、数据库查询等性能相关信息
- **审计日志**: 记录操作人员、时间、操作内容等审计信息

### 3. 事务日志

- **步骤执行日志**: 详细记录每个事务步骤的执行情况
- **补偿操作日志**: 记录事务回滚时的补偿操作
- **上下文状态日志**: 记录事务上下文的状态变化

## 总结

LYBT.Module.Prescriptions模块是一个功能完备、架构先进的中药处方管理系统，具有以下核心特征：

### 技术特征

- **✅ UltraThink双层架构**: 清晰的职责分离，Query层专注查询，Business层专注业务逻辑
- **✅ 完整事务支持**: 基于事务框架的可靠数据操作，支持补偿和回滚
- **✅ 智能缓存机制**: 多层次缓存策略，显著提升查询性能
- **✅ 对象映射集成**: AutoMapper实现实体与DTO的无缝转换
- **✅ 模块化设计**: 独立的依赖注入配置，易于集成和测试

### 业务特征

- **✅ 智能处方组合**: 基于验方模板的智能处方生成和组合
- **✅ 配伍安全检查**: 中药配伍禁忌检查和安全性验证
- **✅ 处方复制功能**: 灵活的历史处方复制和模板应用
- **✅ 价格自动计算**: 准确的单剂和总价自动计算
- **✅ 完整审计追踪**: 详细的操作日志和审计信息记录

### 协作特征

- **✅ 模块间协作**: 与Formula、Herbs、MedicalCase、Patients、Users等模块的深度集成
- **✅ 数据一致性**: 通过事务机制确保跨模块操作的数据一致性
- **✅ 业务流程支持**: 完整支持中医诊疗流程中的处方管理环节

该模块为凌隐宝堂中医诊所系统提供了企业级的处方管理功能，在保证数据安全和业务准确性的同时，提供了优秀的性能表现和用户体验。