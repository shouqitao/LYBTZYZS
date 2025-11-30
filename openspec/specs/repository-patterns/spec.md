# repository-patterns Specification

## Purpose
TBD - created by archiving change refactor-repository-layer. Update Purpose after archive.
## Requirements
### Requirement: Repository接口位置

IRepository<T>和IReadRepository<T>接口SHALL位于Infrastructure层，而非Shared层。

**规范**:
- 接口命名空间SHALL为LYBT.Infrastructure.Interfaces
- Shared层SHALL NOT包含Repository接口
- 所有Server端代码SHALL引用Infrastructure层的接口

#### Scenario: 接口位置正确
- **GIVEN** LYBT.Infrastructure项目
- **WHEN** 检查Interfaces目录
- **THEN** 包含IRepository.cs和IReadRepository.cs
- **AND** 命名空间为LYBT.Infrastructure.Interfaces

#### Scenario: Shared层不包含Repository接口
- **GIVEN** LYBT.Shared.Models项目
- **WHEN** 检查Interfaces目录
- **THEN** 不包含IRepository.cs
- **AND** 不包含IReadRepository.cs

### Requirement: Repository构造函数统一

所有Repository实现类SHALL使用统一的必须参数构造函数。

**规范**:
- BaseRepository子类SHALL接受(AppDbContext context, ILogger<T> logger)参数
- BaseReadRepository子类SHALL接受(AppDbContext context, ILogger<T> logger)参数
- 构造函数SHALL对null参数抛出ArgumentNullException
- 不允许同一Repository类存在多个构造函数重载

#### Scenario: BaseRepository子类构造函数
- **GIVEN** 一个继承自BaseRepository<T>的仓库类
- **WHEN** 实例化时传入null的context
- **THEN** 抛出ArgumentNullException

#### Scenario: BaseReadRepository子类构造函数
- **GIVEN** 一个继承自BaseReadRepository<T>的仓库类
- **WHEN** 实例化时传入null的logger
- **THEN** 抛出ArgumentNullException

### Requirement: 模板方法分页查询

BaseRepository SHALL使用模板方法模式实现分页查询，子类通过覆盖虚方法提供过滤和排序逻辑。

**规范**:
- BaseRepository SHALL定义protected virtual ApplyKeywordFilter方法
- BaseRepository SHALL定义protected virtual ApplyDefaultOrdering方法
- GetPagedAsync SHALL调用模板方法而非硬编码逻辑
- 子类SHALL NOT重写整个GetPagedAsync方法

#### Scenario: 默认分页无过滤
- **GIVEN** 一个未覆盖ApplyKeywordFilter的Repository
- **WHEN** 调用GetPagedAsync(1, 10, "test")
- **THEN** 返回所有未删除记录（不应用关键字过滤）
- **AND** 按CreatedAt降序排列

#### Scenario: 自定义关键字过滤
- **GIVEN** PatientRepository覆盖了ApplyKeywordFilter
- **WHEN** 调用GetPagedAsync(1, 10, "张")
- **THEN** 返回Name或PinYinCode包含"张"的患者
- **AND** 按Name升序排列

#### Scenario: 子类不重写GetPagedAsync
- **GIVEN** 一个继承BaseRepository的子类
- **WHEN** 需要自定义分页查询逻辑
- **THEN** 通过覆盖ApplyKeywordFilter和ApplyDefaultOrdering实现
- **AND** 不重写GetPagedAsync方法本身

### Requirement: 实体命名统一

Repository实现SHALL直接使用实体类名，不使用using别名，除非存在命名空间冲突。

**规范**:
- 默认SHALL使用命名空间导入`using LYBT.Entities.Xxx;`
- 默认SHALL使用简短名称如`Patient`而非`PatientEntity`
- **例外**: 当实体类名与模块命名空间冲突时，SHALL使用实体别名
  - `Formula`实体与`LYBT.Module.Formula`命名空间冲突 → 使用`FormulaEntity`别名
  - `Consultation`实体与`LYBT.Module.Consultation`命名空间冲突 → 使用`ConsultationEntity`别名
  - `MedicalCase`实体与`LYBT.Module.MedicalCase`命名空间冲突 → 使用`MedicalCaseEntity`别名
- 需要别名时SHALL在remarks注释中说明原因

#### Scenario: 无命名空间冲突的实体引用
- **GIVEN** PatientRepository源文件
- **WHEN** 检查using声明
- **THEN** 包含`using LYBT.Entities.Patients;`
- **AND** 不包含`using PatientEntity = ...`
- **AND** 基类为`BaseRepository<Patient>`

#### Scenario: 存在命名空间冲突的实体引用
- **GIVEN** FormulaRepository源文件（位于LYBT.Module.Formula命名空间）
- **WHEN** 检查using声明
- **THEN** 包含`using FormulaEntity = LYBT.Entities.Formulas.Formula;`
- **AND** 基类为`BaseRepository<FormulaEntity>`
- **AND** remarks注释说明冲突原因

#### Scenario: ConsultationRepository命名空间冲突处理
- **GIVEN** ConsultationRepository源文件（位于LYBT.Module.Consultation命名空间）
- **WHEN** 检查using声明
- **THEN** 包含`using ConsultationEntity = LYBT.Entities.Consultations.Consultation;`
- **AND** 基类为`BaseReadRepository<ConsultationEntity>`

