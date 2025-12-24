# desktop-architecture Spec Delta

## MODIFIED Requirements

### Requirement: ARCH-010 Desktop数据操作模式统一

Desktop层所有CRUD模块 **SHALL** 使用CommandHandler模式进行数据操作。

**CommandHandler规范**:
- 无状态设计（无Current/HasChanges属性）
- 统一返回类型：`(bool success, T? data, string? error)`
- 依赖Repository进行数据访问
- 统一日志前缀：[CMD]

**适用模块**:
- Users, Patients, Consultation, Formula, MedicalCase, Herbs

**例外模块**:
- Auth：使用ILoginCoordinator（无Repository，非CRUD）
- Prescriptions：工具服务（打印功能，非CRUD）

#### Scenario: Herbs模块使用CommandHandler
- **GIVEN** Herbs模块需要执行CRUD操作
- **WHEN** ViewModel调用数据操作
- **THEN** **SHALL** 通过IHerbCommandHandler执行
- **AND** **SHALL NOT** 直接依赖Repository
- **AND** **SHALL NOT** 使用DataManager模式

#### Scenario: CommandHandler返回类型
- **GIVEN** CommandHandler方法执行完成
- **WHEN** 返回结果
- **THEN** Create/Update/GetById **SHALL** 返回 `(bool, TDetailDto?, string?)`
- **AND** Delete **SHALL** 返回 `(bool, string?)`

#### Scenario: CommandHandler无状态设计
- **GIVEN** CommandHandler类定义
- **WHEN** 检查类成员
- **THEN** **SHALL NOT** 包含Current属性
- **AND** **SHALL NOT** 包含HasChanges属性
- **AND** **SHALL NOT** 包含_original*字段

#### Scenario: CommandHandler日志规范
- **GIVEN** CommandHandler执行操作
- **WHEN** 记录日志
- **THEN** 日志消息 **SHALL** 以[CMD]前缀开头
- **AND** 开始日志 **SHALL** 记录操作名称和关键参数
- **AND** 完成日志 **SHALL** 记录操作结果

---

### Requirement: ARCH-011 DataManager模式废弃

IDataManager接口及其实现 **SHALL** 被移除，由CommandHandler模式替代。

**废弃清单**:
- `IDataManager<T>` 基接口
- `IHerbDataManager` 接口
- `HerbDataManager` 实现类

#### Scenario: IDataManager接口删除
- **GIVEN** 代码库中存在IDataManager接口
- **WHEN** 重构完成
- **THEN** IDataManager接口 **SHALL** 被删除
- **AND** 所有引用 **SHALL** 被移除

#### Scenario: HerbDataManager替换
- **GIVEN** HerbMasterDetailViewModel依赖IHerbDataManager
- **WHEN** 重构完成
- **THEN** **SHALL** 改为依赖IHerbCommandHandler
- **AND** HerbDataManager类 **SHALL** 被删除

---

### Requirement: ARCH-012 聚合根逻辑设计

MedicalCase **SHALL** 作为聚合根，逻辑上包含Consultation和Prescription子节点。

**设计原则**:
- 物理项目结构保留（不删除模块项目）
- 逻辑上遵循聚合根设计
- 方便后续扩展

#### Scenario: 聚合根物理结构
- **GIVEN** MedicalCase聚合根设计
- **WHEN** 检查项目结构
- **THEN** LYBT.Desktop.MedicalCase项目 **SHALL** 保留
- **AND** LYBT.Desktop.Consultation项目 **SHALL** 保留
- **AND** LYBT.Desktop.Prescriptions项目 **SHALL** 保留

#### Scenario: 聚合根逻辑归属
- **GIVEN** 业务操作涉及医案
- **WHEN** 执行完整医案流程
- **THEN** 诊断(Consultation) **SHALL** 通过MedicalCase入口访问
- **AND** 处方(Prescription) **SHALL** 通过MedicalCase入口访问
