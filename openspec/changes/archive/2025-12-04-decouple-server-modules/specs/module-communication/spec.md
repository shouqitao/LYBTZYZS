# module-communication Specification Delta

本delta定义Server端模块间通信规范，确保Server端与Desktop端设计思想统一。

## Background

### Server-Client设计对比

| 维度 | Desktop端 (良好设计) | Server端 (重构目标) |
|------|----------------------|---------------------|
| **模块依赖声明** | Prism `[ModuleDependency]` | 仅Infrastructure/Entities/Shared |
| **跨模块数据获取** | 通过HTTP API调用 | 通过ICrossModuleQueryService |
| **聚合根遵循** | MedicalCaseRepository (Issue #1606) | 完全遵循 |
| **模块边界** | 清晰隔离 | 清晰隔离 |

### 行业最佳实践参考

本规范参考以下行业最佳实践:
- **ABP Framework**: Application Contracts模块共享DTO和接口
- **Microsoft eShopOnContainers**: 简化CQRS模式，读写分离
- **DDD Bounded Context**: 聚合根边界内操作

---

## ADDED Requirements

### Requirement: MOD-001 合法依赖类型

Server模块间依赖 SHALL 仅使用以下合法类型。

**规范**:
- **聚合内依赖**: 聚合根内部实体可直接引用
- **Infrastructure依赖**: 所有模块 MAY 依赖Infrastructure层
- **Shared依赖**: 所有模块 MAY 依赖Shared层
- **Service接口依赖**: 模块 MAY 通过接口依赖其他模块的Service
- **CrossModule查询**: 只读数据访问 SHALL 使用ICrossModuleQueryService

#### Scenario: 聚合内依赖
- **GIVEN** MedicalCase聚合根
- **WHEN** 需要访问Consultation或Prescription
- **THEN** MAY 直接通过导航属性访问
- **AND** 不需要跨模块查询

#### Scenario: Service接口依赖
- **GIVEN** Auth模块需要用户信息
- **WHEN** 获取用户数据
- **THEN** SHALL 注入IUserService接口
- **AND** SHALL NOT 直接注入IUserRepository

#### Scenario: CrossModule查询依赖-Prescriptions
- **GIVEN** Prescriptions模块需要患者信息
- **WHEN** 进行处方搜索
- **THEN** SHALL 使用ICrossModuleQueryService
- **AND** SHALL NOT 直接注入IPatientRepository

#### Scenario: CrossModule查询依赖-Formula
- **GIVEN** Formula模块需要验证或匹配药材
- **WHEN** 进行药材验证或匹配
- **THEN** SHALL 使用ICrossModuleQueryService
- **AND** SHALL NOT 直接注入IHerbRepository

#### Scenario: Infrastructure依赖
- **GIVEN** 任意业务模块
- **WHEN** 需要使用基础设施服务
- **THEN** MAY 依赖LYBT.Infrastructure
- **AND** MAY 使用ICrossModuleQueryService
- **AND** MAY 使用其他基础设施服务

---

### Requirement: MOD-002 禁止的依赖类型

Server模块间 SHALL NOT 使用以下依赖类型。

**规范**:
- 跨模块直接注入Repository接口 SHALL NOT 允许
- 跨模块直接引用Entity类 SHALL NOT 允许(使用DTO)
- 循环依赖 SHALL NOT 存在
- 模块间直接调用内部方法 SHALL NOT 允许

#### Scenario: 禁止跨模块Repository注入-Prescriptions
- **GIVEN** PrescriptionService
- **WHEN** 需要患者信息
- **THEN** SHALL NOT 注入IPatientRepository
- **AND** SHALL 使用ICrossModuleQueryService或IPatientService

#### Scenario: 禁止跨模块Repository注入-Formula
- **GIVEN** FormulaService
- **WHEN** 需要药材信息
- **THEN** SHALL NOT 注入IHerbRepository
- **AND** SHALL 使用ICrossModuleQueryService

#### Scenario: 禁止循环依赖
- **GIVEN** 模块A依赖模块B
- **WHEN** 检查模块B的依赖
- **THEN** 模块B SHALL NOT 依赖模块A
- **AND** 编译应无循环引用警告

#### Scenario: 禁止跨模块Entity引用
- **GIVEN** Prescriptions模块代码
- **WHEN** 需要传递患者数据
- **THEN** SHALL 使用PatientBasicDto
- **AND** SHALL NOT 直接使用Patient实体类

#### Scenario: 禁止跨模块内部方法调用
- **GIVEN** ModuleA的ServiceA
- **WHEN** 需要调用ModuleB的功能
- **THEN** SHALL 通过公开接口调用
- **AND** SHALL NOT 直接调用内部private/internal方法

---

### Requirement: MOD-003 CrossModuleQueryService使用规范

跨模块只读查询 SHALL 使用ICrossModuleQueryService。

**规范**:
- ICrossModuleQueryService SHALL 位于Infrastructure层
- 查询方法 SHALL 使用AsNoTracking()
- 批量查询 SHALL 优先于循环单个查询
- 返回类型 SHALL 使用BasicDto而非完整Entity

**接口方法清单**:

| 方法 | 返回类型 | 用途 |
|------|----------|------|
| GetPatientBasicInfoAsync(Guid) | PatientBasicDto? | 单个患者查询 |
| GetPatientsBasicInfoAsync(IEnumerable<Guid>) | Dictionary<Guid, PatientBasicDto> | 批量患者查询 |
| GetMedicalCaseBasicInfoAsync(Guid) | MedicalCaseBasicDto? | 单个医案查询(含诊断) |
| GetMedicalCasesBasicInfoAsync(IEnumerable<Guid>) | Dictionary<Guid, MedicalCaseBasicDto> | 批量医案查询(含诊断) |
| GetHerbBasicInfoAsync(Guid) | HerbBasicDto? | 单个药材查询 |
| GetHerbByNameOrPinyinAsync(string) | HerbBasicDto? | 按名称/拼音查询药材 |

#### Scenario: 单个实体查询
- **GIVEN** 需要获取单个患者信息
- **WHEN** 调用GetPatientBasicInfoAsync
- **THEN** 返回PatientBasicDto或null
- **AND** 查询使用AsNoTracking()

#### Scenario: 批量实体查询
- **GIVEN** 需要获取多个患者信息
- **WHEN** 有10个patientId需要查询
- **THEN** SHALL 调用GetPatientsBasicInfoAsync一次
- **AND** SHALL NOT 循环调用GetPatientBasicInfoAsync 10次

#### Scenario: 返回DTO类型
- **GIVEN** CrossModuleQueryService方法
- **WHEN** 返回数据
- **THEN** SHALL 返回BasicDto类型
- **AND** SHALL NOT 返回完整Entity类型

#### Scenario: 药材查询
- **GIVEN** Formula模块需要验证药材
- **WHEN** 调用GetHerbBasicInfoAsync或GetHerbByNameOrPinyinAsync
- **THEN** 返回HerbBasicDto或null
- **AND** 查询使用AsNoTracking()

#### Scenario: 医案查询包含诊断
- **GIVEN** 需要获取医案及其诊断信息
- **WHEN** 调用GetMedicalCaseBasicInfoAsync
- **THEN** 返回MedicalCaseBasicDto
- **AND** MedicalCaseBasicDto.TCMDiagnosis包含关联的诊断

---

### Requirement: MOD-004 模块边界验证

项目 SHALL 验证模块边界不被违反。

**规范**:
- 每个模块的csproj SHALL 仅引用允许的依赖
- 架构测试 SHALL 验证跨模块Repository注入
- PR审查 SHALL 检查新增的跨模块依赖

#### Scenario: Prescriptions模块依赖验证
- **GIVEN** LYBT.Module.Prescriptions.csproj
- **WHEN** 检查ProjectReference
- **THEN** SHALL 包含LYBT.Infrastructure
- **AND** SHALL 包含LYBT.Entities
- **AND** SHALL 包含LYBT.Shared.*
- **AND** SHALL NOT 包含LYBT.Module.Patients
- **AND** SHALL NOT 包含LYBT.Module.Consultation
- **AND** SHALL NOT 包含LYBT.Module.MedicalCase
- **AND** SHALL NOT 包含LYBT.Module.Herbs
- **AND** SHALL NOT 包含LYBT.Module.Formula

#### Scenario: Formula模块依赖验证
- **GIVEN** LYBT.Module.Formula.csproj
- **WHEN** 检查ProjectReference
- **THEN** SHALL 包含LYBT.Infrastructure
- **AND** SHALL 包含LYBT.Entities
- **AND** SHALL 包含LYBT.Shared.*
- **AND** SHALL NOT 包含LYBT.Module.Herbs

#### Scenario: 架构测试验证
- **GIVEN** 架构测试项目
- **WHEN** 运行模块依赖测试
- **THEN** SHALL 检测到违规的跨模块Repository注入
- **AND** 测试失败时提供清晰的错误信息

#### Scenario: 通用模块依赖模式
- **GIVEN** 任意LYBT.Module.{Domain}项目
- **WHEN** 检查ProjectReference
- **THEN** SHALL 包含LYBT.Infrastructure
- **AND** SHALL 包含LYBT.Entities
- **AND** MAY 包含LYBT.Shared.*
- **AND** SHALL NOT 包含其他LYBT.Module.*项目(除非在MOD-005允许清单中)

---

### Requirement: MOD-005 允许的跨模块依赖清单

以下跨模块依赖 SHALL 被视为合法。

**规范**:
- Auth → Users: 认证需要用户信息
- MedicalCase → Patients: 医案关联患者
- MedicalCase → Users: 医案关联医生
- Consultation → MedicalCase: 诊断属于医案聚合
- 其他跨模块依赖 SHALL 通过CrossModuleQueryService

**合法依赖的实现方式**:
- 合法依赖 SHALL 通过Service接口实现(如IUserService、IPatientService)
- 合法依赖 SHALL NOT 直接注入Repository
- 这些依赖已符合依赖倒置原则(DIP)

**为什么合法依赖不需要进一步解耦**:

| 解耦程度 | 实现方式 | 适用场景 | 本项目应用 |
|----------|----------|----------|------------|
| **高** | CrossModuleQueryService | 纯只读查询，无业务逻辑 | Prescriptions、Formula |
| **中** | Service接口依赖 | 需要调用业务方法 | Auth→Users、MedicalCase→Patients |
| **低** | 聚合内直接引用 | DDD聚合根内部关系 | Consultation→MedicalCase |

**不进一步解耦的理由**:
1. Service接口依赖已符合DIP原则，依赖于抽象而非具体实现
2. Auth/MedicalCase需要调用**业务方法**（如ValidateCredentialsAsync），不仅仅是数据查询
3. 过度解耦会导致业务逻辑重复，增加维护成本
4. 这些关系是业务核心，变化可能性低

**值对象副本模式** (数据存储层面):
- PrescriptionItem包含HerbId+HerbName副本，不引用Herb实体
- FormulaItem包含HerbId+HerbName副本，不引用Herb实体
- 此模式用于数据存储，在Client端完成转换

**CrossModuleQueryService查询** (运行时查询需求):
- FormulaService需要验证药材时，使用ICrossModuleQueryService.GetHerbBasicInfoAsync
- FormulaService需要匹配药材时，使用ICrossModuleQueryService.GetHerbByNameOrPinyinAsync
- PrescriptionService需要患者/医案信息时，使用ICrossModuleQueryService批量查询
- 这些是运行时查询需求，与数据存储的值对象副本模式不冲突

**依赖矩阵** (重构后):

```
依赖方 →              Auth  Users  Patients  MedicalCase  Consultation  Prescriptions  Herbs  Formula
被依赖方 ↓
Auth                   -      -       -           -            -              -           -       -
Users                  Y      -       -           -            -              -           -       -
Patients               -      -       -           Y            -              -           -       -
MedicalCase            -      -       -           -            Y              -           -       -
Consultation           -      -       -           -            -              -           -       -
Prescriptions          -      -       -           -            -              -           -       -
Herbs                  -      -       -           -            -              -           -       -
Formula                -      -       -           -            -              -           -       -
CrossModule(Infra)     -      -       -           -            -              Y           -       Y
```

**变化总结**:
- Prescriptions: 5个模块依赖 → 0个 (全部通过CrossModuleQueryService)
- Formula: 1个模块依赖 → 0个 (通过CrossModuleQueryService)

#### Scenario: Auth依赖Users
- **GIVEN** Auth模块
- **WHEN** 检查项目引用
- **THEN** MAY 引用LYBT.Module.Users
- **AND** SHALL 通过IUserService获取用户信息

#### Scenario: MedicalCase依赖Patients
- **GIVEN** MedicalCase模块
- **WHEN** 检查项目引用
- **THEN** MAY 引用LYBT.Module.Patients
- **AND** SHALL 通过IPatientService获取患者信息

#### Scenario: 值对象副本模式
- **GIVEN** PrescriptionItem或FormulaItem需要药材信息
- **WHEN** 存储药材引用
- **THEN** SHALL 使用HerbId + HerbName值对象副本
- **AND** SHALL NOT 直接引用Herb实体
- **AND** 转换逻辑 SHALL 在Client端完成

#### Scenario: 非允许清单的依赖
- **GIVEN** 任意模块需要新增跨模块依赖
- **WHEN** 依赖不在允许清单中
- **THEN** SHALL 使用CrossModuleQueryService
- **OR** SHALL 申请添加到允许清单(需架构评审)

#### Scenario: Prescriptions模块完全解耦
- **GIVEN** Prescriptions模块重构完成
- **WHEN** 需要患者、医案、诊断信息
- **THEN** SHALL 使用ICrossModuleQueryService.GetPatientsBasicInfoAsync
- **AND** SHALL 使用ICrossModuleQueryService.GetMedicalCasesBasicInfoAsync
- **AND** MedicalCaseBasicDto已包含TCMDiagnosis，不需要额外查询Consultation

#### Scenario: Formula模块完全解耦
- **GIVEN** Formula模块重构完成
- **WHEN** 需要验证或匹配药材
- **THEN** SHALL 使用ICrossModuleQueryService.GetHerbBasicInfoAsync
- **AND** SHALL 使用ICrossModuleQueryService.GetHerbByNameOrPinyinAsync

---

## BasicDto定义规范

### PatientBasicDto

```csharp
using LYBT.Shared.Models.Enums;

public class PatientBasicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }  // 使用枚举类型，与Patient实体一致
    public string? Phone { get; set; }
}
```

### MedicalCaseBasicDto

```csharp
public class MedicalCaseBasicDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TCMDiagnosis { get; set; }  // 来自关联的Consultation
}
```

### HerbBasicDto

```csharp
public class HerbBasicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Pinyin { get; set; }
    public string? Category { get; set; }
}
```

---

## Cross-Reference

- **project-architecture**: 项目架构总览规范
- **service-conventions**: Service层规范
- **repository-patterns**: Repository层规范
- **ARCH-003**: 依赖方向规范 (project-architecture)

---

## Implementation Notes

### Desktop端良好设计示例

```csharp
// PrescriptionsModule.cs - Desktop端
[ModuleDependency("ConsultationModule")] // 功能依赖：UI组件加载顺序
[ModuleDependency("HerbsModule")]        // 功能依赖：UI组件加载顺序
[ModuleDependency("FormulaModule")]      // 功能依赖：UI组件加载顺序
public class PrescriptionsModule : IModule { }
```

**特点**:
- 这是**功能依赖**，仅影响模块加载顺序
- 数据通过API获取，不直接访问其他模块内部
- Issue #1606已删除IPrescriptionRepository，所有Write操作通过MedicalCaseRepository聚合根

### Server端重构后设计

```csharp
// PrescriptionService.cs - Server端 (重构后)
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _repository;
    private readonly ICrossModuleQueryService _crossModuleQuery; // 替代3个跨模块Repository

    // 不再直接注入:
    // - IMedicalCaseRepository
    // - IPatientRepository
    // - IConsultationRepository
}
```

**特点**:
- 通过ICrossModuleQueryService获取跨模块数据
- 保持模块边界清晰
- 与Desktop端设计思想一致
