# service-conventions Specification Delta

## ADDED Requirements

### Requirement: SVC-001 统一返回值类型

所有Service方法 MUST 使用`Result<T>`作为返回值类型。

**规范**:
- Service方法 SHALL 返回`Result<T>`或`Result<IEnumerable<T>>`
- SHALL NOT 使用`ServiceResult<T>`（已废弃）
- SHALL NOT 直接抛出业务异常，应返回`Result<T>.Failure()`

#### Scenario: 成功返回数据
- **GIVEN** Service方法执行成功
- **WHEN** 返回结果
- **THEN** 使用`Result<T>.Success(data)`
- **AND** `IsSuccess`为`true`
- **AND** `Data`包含返回数据

#### Scenario: 业务失败返回错误
- **GIVEN** Service方法因业务规则失败
- **WHEN** 返回结果
- **THEN** 使用`Result<T>.Failure(errorMessage)`
- **AND** `IsSuccess`为`false`
- **AND** `ErrorMessage`包含错误信息

#### Scenario: 验证失败返回多个错误
- **GIVEN** FluentValidation验证失败
- **WHEN** 返回结果
- **THEN** 使用`Result<T>.Failure(errors)`
- **AND** `Errors`列表包含所有验证错误

---

### Requirement: SVC-002 Service基类继承

所有业务Service MUST 继承`BaseService<TEntity>`基类。

**规范**:
- Service类 SHALL 继承`BaseService<TEntity>`
- `TEntity` SHALL 是Service主要操作的实体类型
- 构造函数 SHALL 调用`base(logger, mapper)`

#### Scenario: 基类构造函数调用
- **GIVEN** 创建新的Service类
- **WHEN** 定义构造函数
- **THEN** 必须调用`base(logger, mapper)`
- **AND** 额外依赖通过构造函数注入

#### Scenario: 基类方法使用
- **GIVEN** Service需要执行业务操作
- **WHEN** 可能抛出异常
- **THEN** SHOULD 使用`ExecuteAsync<T>()`包装
- **AND** 自动处理异常和日志

---

### Requirement: SVC-003 错误处理模式

Service层 MUST 统一使用BaseService提供的错误处理模式。

**规范**:
- SHALL 使用`ExecuteAsync<T>()`处理可能抛出异常的操作
- SHALL NOT 在Service方法中直接catch并重新throw
- 日志 SHALL 使用`_logger.LogError(ex, "{Operation} 失败", operationName)`格式

#### Scenario: 异常自动处理
- **GIVEN** Service方法使用`ExecuteAsync<T>()`
- **WHEN** 内部操作抛出异常
- **THEN** 异常被捕获
- **AND** 记录Error级别日志
- **AND** 返回`Result<T>.Failure()`

#### Scenario: 日志格式统一
- **GIVEN** Service方法执行失败
- **WHEN** 记录日志
- **THEN** 日志消息包含操作名称
- **AND** 日志级别为Error
- **AND** 包含完整异常信息

---

### Requirement: SVC-004 FluentValidation集成

所有接受用户输入的Service方法 MUST 使用FluentValidation验证。

**规范**:
- Create/Update方法 SHALL 注入对应的`IValidator<TDto>`
- SHALL 在业务逻辑执行前调用验证
- 验证失败 SHALL 返回`Result<T>.Failure(errors)`

#### Scenario: 创建操作验证
- **GIVEN** Service的CreateAsync方法接收InputDto
- **WHEN** 执行创建操作前
- **THEN** 必须调用`ValidateAsync(dto, validator)`
- **AND** 验证失败时立即返回错误

#### Scenario: 更新操作验证
- **GIVEN** Service的UpdateAsync方法接收InputDto
- **WHEN** 执行更新操作前
- **THEN** 必须调用`ValidateAsync(dto, validator)`
- **AND** 验证失败时立即返回错误

---

### Requirement: SVC-005 构造函数参数顺序

Service构造函数参数 MUST 遵循统一的顺序规范。

**规范**:
- 参数顺序 SHALL 为: Repository → Mapper → Logger → Validator → 其他依赖
- 基类参数（Logger, Mapper）SHALL 首先传递给base()
- 所有参数 SHALL 进行null检查

#### Scenario: 标准Service构造函数
- **GIVEN** 创建新的Service类
- **WHEN** 定义构造函数参数
- **THEN** 顺序为: `IXxxRepository`, `IMapper`, `ILogger<T>`, `IValidator<T>`, 其他
- **AND** 调用`base(logger, mapper)`

#### Scenario: 参数null检查
- **GIVEN** Service构造函数接收参数
- **WHEN** 参数为null
- **THEN** 抛出`ArgumentNullException`
- **AND** 异常消息包含参数名

---

### Requirement: SVC-006 大型Service拆分

超过500行的Service MUST 拆分为多个职责明确的子服务。

**规范**:
- 单个Service类 SHALL NOT 超过500行
- 职责明确的子服务 SHALL 通过独立接口定义
- 原Service SHALL 删除，不保留Facade（无兼容性包袱）
- Controller SHALL 同步更新注入新的小Service

#### Scenario: 直接拆分删除原Service
- **GIVEN** 大型Service（如MedicalCaseService 1465行）需要拆分
- **WHEN** 拆分为子服务
- **THEN** 删除原IMedicalCaseService接口
- **AND** 删除原MedicalCaseService实现
- **AND** 创建职责单一的新接口和实现

#### Scenario: 子服务职责划分
- **GIVEN** 需要拆分的Service
- **WHEN** 划分子服务职责
- **THEN** Command（Create/Update/Delete）为一组
- **AND** Query（Get/List/Search）为一组
- **AND** State（状态变更操作）为一组

#### Scenario: Controller同步更新
- **GIVEN** Service已拆分为多个子服务
- **WHEN** 更新Controller
- **THEN** 注入所有相关子服务
- **AND** 方法调用对应的子服务

---

### Requirement: SVC-007 命名规范

Service命名 MUST 遵循统一的命名规范。

**规范**:
- Service类名 SHALL 为`{Entity}Service`
- 接口名 SHALL 为`I{Entity}Service`
- 子服务 SHALL 为`{Entity}{Responsibility}Service`（如`MedicalCaseQueryService`）
- Validator类名 SHALL 为`{Dto}Validator`

#### Scenario: 标准Service命名
- **GIVEN** 为Patient实体创建Service
- **WHEN** 命名类和接口
- **THEN** 接口为`IPatientService`
- **AND** 实现为`PatientService`

#### Scenario: 子服务命名
- **GIVEN** MedicalCaseService需要拆分查询职责
- **WHEN** 创建子服务
- **THEN** 接口为`IMedicalCaseQueryService`
- **AND** 实现为`MedicalCaseQueryService`
