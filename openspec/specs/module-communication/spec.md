# Spec: module-communication

## Purpose

定义Server端模块间通信的标准方式，确保模块保持独立性和可维护性。模块间禁止直接依赖其他模块的Repository/Service，统一通过`ICrossModuleQueryService`进行跨模块只读查询。
## Requirements
### Requirement: 模块间只允许通过指定方式进行依赖

模块 SHALL 只通过Core层(LYBT.Infrastructure, LYBT.Entities)、Shared层(LYBT.Shared.Models, LYBT.Shared.Validators)或ICrossModuleQueryService进行跨模块数据访问。

#### Scenario: 模块需要获取其他模块数据时
- **WHEN** Formula模块需要验证药材是否存在
- **THEN** 调用`_crossModuleQuery.GetHerbBasicInfoAsync(herbId)`返回`HerbBasicDto`或`null`

#### Scenario: 模块需要批量获取数据时
- **WHEN** Prescriptions模块需要加载多个医案的患者信息
- **THEN** 调用`_crossModuleQuery.GetPatientsBasicInfoAsync(patientIds)`返回`Dictionary<Guid, PatientBasicDto>`

### Requirement: 禁止直接依赖其他模块的Repository或Service

模块 SHALL NOT 直接注入或引用其他模块的IRepository、IService或Entity进行操作。

#### Scenario: 新增功能需要跨模块数据
- **WHEN** 开发者需要在新功能中获取患者信息并尝试注入`IPatientRepository`
- **THEN** 编译应失败（无项目引用），应使用`ICrossModuleQueryService`

#### Scenario: 代码审查发现跨模块引用
- **WHEN** PR中包含跨模块Repository注入
- **THEN** 应拒绝合并，要求使用`ICrossModuleQueryService`

### Requirement: CrossModuleQueryService提供标准化的跨模块查询方法

ICrossModuleQueryService SHALL 提供Patient、MedicalCase、Herb的单个和批量查询方法，返回只读BasicDto。

#### Scenario: 需要N+1查询优化
- **WHEN** 需要加载100个处方的患者信息
- **THEN** 应使用`GetPatientsBasicInfoAsync`批量查询而非循环调用单个查询

#### Scenario: 需要医案的诊断信息
- **WHEN** 调用`GetMedicalCaseBasicInfoAsync`
- **THEN** 返回的`MedicalCaseBasicDto.TCMDiagnosis`包含关联的诊断信息

### Requirement: 模块边界通过编译时验证和架构测试保证

模块 SHALL 通过csproj ProjectReference检查和ArchTests确保无跨模块直接依赖。

#### Scenario: 验证Prescriptions模块边界
- **WHEN** 检查LYBT.Module.Prescriptions.csproj的ProjectReference
- **THEN** 不包含LYBT.Module.*引用（除Core和Shared外）

#### Scenario: 验证Formula模块边界
- **WHEN** 检查LYBT.Module.Formula.csproj的ProjectReference
- **THEN** 不包含LYBT.Module.Herbs引用

### Requirement: 新增跨模块依赖需遵循标准流程

新增跨模块查询 SHALL 按流程扩展：评估必要性→添加ICrossModuleQueryService方法→创建BasicDto→实现→测试→更新文档。

#### Scenario: 需要新增跨模块查询
- **WHEN** 新功能需要查询其他模块数据且已有方法无法满足
- **THEN** 按标准流程在ICrossModuleQueryService中扩展

---

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

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| ddd-aggregate-roots | 聚合根边界定义 |
| three-layer-architecture | 三层架构依赖方向 |
| openspec/changes/decouple-server-modules | 本规范的实现变更 |

---

## Implementation Notes

### 已完成的解耦

| 模块 | 移除的依赖数 | 状态 |
|------|-------------|------|
| LYBT.Module.Prescriptions | 5个 (Patients, Consultation, MedicalCase, Herbs, Formula) | 已完成 |
| LYBT.Module.Formula | 1个 (Herbs) | 已完成 |

### 测试覆盖

- CrossModuleQueryService: 18个单元测试
- PrescriptionService: 使用ICrossModuleQueryService Mock
- FormulaService: 使用ICrossModuleQueryService Mock

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2024-12-04 | 1.0 | 初始版本，定义模块通信规范 |
