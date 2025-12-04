# Spec: server-layer-architecture

## Purpose

定义Server层(13个项目)的详细架构，包括Core层职责、Module层职责、Services层职责，以及CQRS和传统三层模式的选择标准。

## Requirements

### Requirement: SRV-001 Core层职责

Core层(LYBT.Entities, LYBT.Infrastructure) SHALL 提供基础设施支持。

**LYBT.Entities职责**:
- 定义所有领域实体(继承BaseEntity)
- 定义领域枚举和值对象
- 无外部依赖，仅引用.NET BCL
- 实体采用贫血模型(无业务逻辑)

**LYBT.Infrastructure职责**:
- 提供AppDbContext和EF Core配置
- 提供BaseRepository<T>基类(14个标准方法)
- 提供ICrossModuleQueryService跨模块查询
- 管理数据库迁移

#### Scenario: 新增领域实体
- **WHEN** 需要定义新的业务实体
- **THEN** SHALL 创建在LYBT.Entities对应目录
- **AND** SHALL 继承BaseEntity或其派生类
- **AND** SHALL NOT 包含业务逻辑方法

#### Scenario: 新增Repository实现
- **WHEN** 模块需要数据访问
- **THEN** SHALL 继承BaseRepository<T>
- **AND** MAY 覆盖ApplyKeywordFilter/ApplyDefaultOrdering
- **AND** MAY 添加领域特定查询方法

#### Scenario: 使用跨模块查询
- **WHEN** 模块需要其他模块数据
- **THEN** SHALL 注入ICrossModuleQueryService
- **AND** SHALL 调用GetXxxBasicInfoAsync方法
- **AND** SHALL NOT 直接注入其他模块Repository

---

### Requirement: SRV-002 Module层职责

Module层(8个LYBT.Module.*)SHALL 实现业务逻辑。

**标准目录结构**:
```
LYBT.Module.{Domain}/
├── {Domain}Module.cs          # 模块注册入口
├── Repositories/              # Repository实现
│   └── {Entity}Repository.cs
├── Services/                  # Service实现
│   ├── I{Entity}Service.cs    # 接口
│   └── {Entity}Service.cs     # 实现
├── Validators/                # 输入验证器(可选)
└── Dtos/                      # 模块私有DTO(可选)
```

**模块清单**:

| 模块 | 架构模式 | 跨模块通信 |
|------|----------|------------|
| Auth | 传统三层 | IUserService |
| Users | 传统三层 | - |
| Patients | 传统三层 | - |
| MedicalCase | CQRS | IPatientService |
| Consultation | 传统三层 | - |
| Prescriptions | 传统三层 | ICrossModuleQueryService |
| Herbs | 传统三层 | - |
| Formula | 传统三层 | ICrossModuleQueryService |

#### Scenario: 创建新业务模块
- **WHEN** 需要新增业务领域
- **THEN** SHALL 创建LYBT.Module.{Domain}项目
- **AND** SHALL 包含{Domain}Module.cs注册入口
- **AND** SHALL 引用Infrastructure和Entities

#### Scenario: 模块注册Service和Repository
- **WHEN** 模块初始化
- **THEN** {Domain}Module.RegisterTypes() SHALL 注册所有Service
- **AND** SHALL 注册所有Repository
- **AND** SHALL 使用Scoped生命周期

---

### Requirement: SRV-003 Services层职责

Services层(LYBT.WebAPI) SHALL 作为API入口点。

**职责**:
- HTTP请求处理和路由
- 认证授权中间件
- 全局异常处理
- 请求/响应日志
- 模块注册编排

**Controller规范**:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class {Entity}Controller : ControllerBase
{
    private readonly I{Entity}Service _service;

    // 注入Service，不注入Repository
    public {Entity}Controller(I{Entity}Service service) { }
}
```

#### Scenario: Controller依赖注入
- **WHEN** Controller需要业务功能
- **THEN** SHALL 注入Service接口
- **AND** SHALL NOT 注入Repository
- **AND** SHALL NOT 注入DbContext

#### Scenario: API统一响应格式
- **WHEN** Service返回Result类型
- **THEN** 成功 SHALL 返回Ok(result.Data)
- **AND** 失败 SHALL 返回BadRequest(result.Error)

#### Scenario: 全局异常处理
- **WHEN** 未捕获异常发生
- **THEN** ExceptionMiddleware SHALL 捕获
- **AND** SHALL 返回统一错误格式
- **AND** SHALL 记录错误日志

---

### Requirement: SRV-004 CQRS模式规范

复杂业务模块(MedicalCase) SHALL 采用CQRS模式。

**CQRS适用标准**:
- 读写操作复杂度差异大
- 需要细粒度权限控制
- 需要完整审计日志
- 状态流转逻辑复杂

**Service拆分**:

| Service | 职责 | 方法示例 |
|---------|------|----------|
| IMedicalCaseCommandService | 写操作 | Create, Update, Delete |
| IMedicalCaseQueryService | 读操作 | GetById, GetPaged, Search |
| IMedicalCaseStateService | 状态变更 | Submit, Archive, Revert |
| IMedicalCasePermissionService | 权限检查 | CanEdit, CanDelete |
| IMedicalCaseAuditService | 审计日志 | LogCreate, LogUpdate |

#### Scenario: 选择CQRS模式
- **WHEN** 模块满足CQRS适用标准
- **THEN** SHALL 拆分为多个职责单一的Service
- **AND** Controller SHALL 按操作类型调用不同Service

#### Scenario: 状态变更操作
- **WHEN** 需要变更实体状态(如Submit医案)
- **THEN** SHALL 调用StateService
- **AND** StateService SHALL 验证状态转换合法性
- **AND** SHALL 调用AuditService记录变更

---

### Requirement: SRV-005 传统三层模式规范

简单业务模块 SHALL 采用传统三层模式。

**传统三层适用标准**:
- 标准CRUD操作为主
- 业务逻辑相对简单
- 无复杂状态流转
- 无细粒度权限需求

**调用链路**:
```
Controller → Service → Repository → DbContext
```

**Service规范**:
- 继承BaseService<T>(可选)
- 返回Result<T>类型
- 包含业务验证逻辑
- 调用Repository进行数据操作

#### Scenario: 选择传统三层模式
- **WHEN** 模块为简单CRUD业务
- **THEN** SHALL 使用单一{Entity}Service
- **AND** Service SHALL 返回Result类型

#### Scenario: Service方法命名
- **WHEN** 定义Service方法
- **THEN** 查询方法 SHALL 以Get/List/Search开头
- **AND** 写入方法 SHALL 以Create/Update/Delete开头
- **AND** 所有异步方法 SHALL 以Async结尾

#### Scenario: 业务验证
- **WHEN** 执行写操作前
- **THEN** Service SHALL 验证业务规则
- **AND** 验证失败 SHALL 返回Result.Failure
- **AND** SHALL NOT 抛出异常

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| project-architecture | 项目架构总览 |
| module-communication | 模块间通信规范 |
| repository-patterns | Repository模式详细规范 |
| service-conventions | Service约定详细规范 |
| ddd-aggregate-roots | 聚合根边界定义 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义Server层架构规范 |
