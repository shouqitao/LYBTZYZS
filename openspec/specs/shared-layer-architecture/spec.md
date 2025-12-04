# Spec: shared-layer-architecture

## Purpose

定义Shared层(4个项目)的详细架构，包括Models、Utilities、Validators、Components各项目的职责边界和DTO继承层次规范。

## Requirements

### Requirement: SHR-001 Models项目职责

LYBT.Shared.Models SHALL 定义所有API契约和共享DTO。

**目录结构**:
```
LYBT.Shared.Models/
├── Contracts/                 # API契约DTO
│   ├── Auth/                  # 认证相关
│   ├── Patient/               # 患者相关
│   ├── MedicalCase/           # 医案相关
│   ├── Consultation/          # 诊断相关
│   ├── Prescription/          # 处方相关
│   ├── Herb/                  # 药材相关
│   ├── Formula/               # 经验方相关
│   ├── User/                  # 用户相关
│   └── Common/                # 跨模块共享DTO(BasicDto系列)
├── Common/                    # 通用类型
│   ├── BaseDto.cs             # DTO基类
│   ├── PagedRequest.cs        # 分页请求
│   ├── PagedResponse.cs       # 分页响应
│   └── Result.cs              # 统一结果类型
├── Enums/                     # 共享枚举
│   ├── Gender.cs
│   ├── MedicalCaseStatus.cs
│   └── CommonStatus.cs
└── Constants/                 # 常量定义
    └── ErrorCodes.cs
```

#### Scenario: 创建API请求DTO
- **WHEN** 定义API输入参数
- **THEN** SHALL 创建{Action}{Entity}Request.cs
- **AND** SHALL 放置在Contracts/{Entity}/目录
- **AND** SHALL 使用record或class

#### Scenario: 创建API响应DTO
- **WHEN** 定义API返回数据
- **THEN** SHALL 创建{Entity}Dto.cs或{Entity}DetailDto.cs
- **AND** SHALL 继承适当的基类(BaseDto/TimestampDto等)

#### Scenario: 创建跨模块BasicDto
- **WHEN** ICrossModuleQueryService需要返回类型
- **THEN** SHALL 创建{Entity}BasicDto.cs
- **AND** SHALL 放置在Contracts/Common/目录
- **AND** SHALL 仅包含必要字段

---

### Requirement: SHR-002 Utilities项目职责

LYBT.Shared.Utilities SHALL 提供通用工具类。

**目录结构**:
```
LYBT.Shared.Utilities/
├── Configuration/             # 配置辅助
│   └── ConfigurationHelper.cs
├── Security/                  # 安全相关
│   ├── PasswordHasher.cs      # BCrypt封装
│   └── JwtHelper.cs           # JWT辅助
├── Text/                      # 文本处理
│   ├── PinYinConverter.cs     # 中文转拼音
│   └── StringExtensions.cs    # 字符串扩展
└── Helpers/                   # 通用辅助
    └── DateTimeHelper.cs
```

#### Scenario: 添加新工具类
- **WHEN** 需要Server和Client共享的工具方法
- **THEN** SHALL 创建静态类或扩展方法
- **AND** SHALL 放置在对应功能目录
- **AND** SHALL 无状态(纯函数)

#### Scenario: 工具类依赖限制
- **WHEN** Utilities项目引用其他项目
- **THEN** SHALL NOT 引用任何LYBT项目
- **AND** MAY 引用第三方NuGet包

---

### Requirement: SHR-003 Validators项目职责

LYBT.Shared.Validators SHALL 提供FluentValidation验证器。

**目录结构**:
```
LYBT.Shared.Validators/
├── Common/                    # 通用验证器
│   ├── BaseValidator.cs       # 验证器基类
│   └── BusinessRuleValidator.cs
├── Auth/
│   └── LoginRequestValidator.cs
├── Patient/
│   ├── CreatePatientRequestValidator.cs
│   └── UpdatePatientRequestValidator.cs
├── MedicalCase/
│   └── ...
└── ...
```

#### Scenario: 创建请求验证器
- **WHEN** 需要验证API请求DTO
- **THEN** SHALL 创建{Action}{Entity}RequestValidator.cs
- **AND** SHALL 继承AbstractValidator<T>
- **AND** SHALL 放置在对应领域目录

#### Scenario: 验证器注册
- **WHEN** 应用启动
- **THEN** 验证器 SHALL 通过FluentValidation DI扩展注册
- **AND** SHALL 自动扫描程序集

#### Scenario: 共享验证规则
- **WHEN** 多个验证器有相同规则
- **THEN** SHALL 提取到扩展方法
- **AND** SHALL 放置在Common目录

---

### Requirement: SHR-004 Components项目职责

LYBT.Shared.Components SHALL 提供可复用业务组件。

**目录结构**:
```
LYBT.Shared.Components/
├── Interfaces/                # 组件接口
│   └── IHerbItem.cs           # 药材条目接口
├── Calculators/               # 计算器
│   ├── HerbCalculatorBase.cs  # 药材计算基类
│   └── PrescriptionCalculator.cs
└── Validators/                # 业务验证
    └── HerbValidatorBase.cs   # 药材验证基类
```

**组件特点**:
- 包含业务逻辑(与Utilities不同)
- 可被Server和Client复用
- 通过接口实现多态

#### Scenario: 创建业务计算组件
- **WHEN** Server和Client需要相同计算逻辑
- **THEN** SHALL 创建Calculator类
- **AND** SHALL 放置在Calculators目录
- **AND** SHALL 使用接口隔离依赖

#### Scenario: Components依赖限制
- **WHEN** Components项目引用其他项目
- **THEN** MAY 引用LYBT.Shared.Models
- **AND** MAY 引用LYBT.Shared.Utilities
- **AND** SHALL NOT 引用Server或Client项目

---

### Requirement: SHR-005 DTO继承层次规范

DTO SHALL 遵循标准继承层次。

**继承层次**:
```
BaseDto (Id: Guid)
    └── TimestampDto (CreatedAt, UpdatedAt)
        └── StatusDto (IsDeleted)
            └── AuditDto (CreatedBy, UpdatedBy)
```

**基类定义**:
```csharp
public class BaseDto { public Guid Id { get; set; } }

public class TimestampDto : BaseDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class StatusDto : TimestampDto
{
    public bool IsDeleted { get; set; }
}

public class AuditDto : StatusDto
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

#### Scenario: 选择DTO基类
- **WHEN** 创建实体对应的DTO
- **THEN** 仅需Id SHALL 继承BaseDto
- **AND** 需要时间戳 SHALL 继承TimestampDto
- **AND** 需要删除状态 SHALL 继承StatusDto
- **AND** 需要审计信息 SHALL 继承AuditDto

#### Scenario: 列表DTO vs 详情DTO
- **WHEN** 定义实体响应DTO
- **THEN** 列表用 {Entity}Dto SHALL 包含关键字段
- **AND** 详情用 {Entity}DetailDto SHALL 包含完整字段
- **AND** 详情DTO MAY 继承列表DTO

#### Scenario: BasicDto命名
- **WHEN** 定义跨模块查询返回类型
- **THEN** SHALL 命名为 {Entity}BasicDto
- **AND** SHALL 仅包含最少必要字段
- **AND** SHALL 放置在Contracts/Common/

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| project-architecture | 项目架构总览 |
| module-communication | 跨模块查询使用BasicDto |
| server-layer-architecture | Server层使用Shared层 |
| client-layer-architecture | Client层使用Shared层 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义Shared层架构规范 |
