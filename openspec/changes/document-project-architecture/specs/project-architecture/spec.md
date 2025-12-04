# project-architecture Specification Delta

本delta定义项目架构总览规范。

## ADDED Requirements

### Requirement: ARCH-001 三层架构定义

项目 SHALL 采用三层架构: Server层、Shared层、Client层。

**规范**:
- Server层 SHALL 包含所有后端服务代码
- Shared层 SHALL 包含Server和Client共享的代码
- Client层 SHALL 包含所有客户端代码
- 层之间的依赖 SHALL 遵循: Server → Shared ← Client

#### Scenario: Server层结构
- **GIVEN** Server层目录 `src/Server/`
- **WHEN** 检查子目录
- **THEN** SHALL 包含 Core/, Modules/, Services/ 三个子目录
- **AND** Core/ SHALL 包含 LYBT.Entities 和 LYBT.Infrastructure
- **AND** Modules/ SHALL 包含 8个业务模块
- **AND** Services/ SHALL 包含 LYBT.WebAPI

#### Scenario: Shared层结构
- **GIVEN** Shared层目录 `src/Shared/`
- **WHEN** 检查子目录
- **THEN** SHALL 包含 LYBT.Shared.Models, LYBT.Shared.Utilities, LYBT.Shared.Validators, LYBT.Shared.Components

#### Scenario: Client层结构
- **GIVEN** Client层目录 `src/Client/Desktop/`
- **WHEN** 检查子目录
- **THEN** SHALL 包含 Core/, Modules/, Roles/, Shell/ 四个子目录

---

### Requirement: ARCH-002 项目命名规范

所有项目 SHALL 遵循统一的命名规范。

**规范**:
- 项目名 SHALL 以 `LYBT.` 为前缀
- Server项目 SHALL 使用 `LYBT.{Layer}.{Domain}` 格式
- Server模块 SHALL 使用 `LYBT.Module.{Domain}` 格式
- Desktop项目 SHALL 使用 `LYBT.Desktop.{Category}` 格式
- Shared项目 SHALL 使用 `LYBT.Shared.{Purpose}` 格式

#### Scenario: Server Core项目命名
- **GIVEN** 新建Server Core层项目
- **WHEN** 命名项目
- **THEN** 实体项目 SHALL 命名为 `LYBT.Entities`
- **AND** 基础设施项目 SHALL 命名为 `LYBT.Infrastructure`

#### Scenario: Server Module项目命名
- **GIVEN** 新建业务模块项目
- **WHEN** 命名项目
- **THEN** SHALL 使用 `LYBT.Module.{Domain}` 格式
- **AND** Domain SHALL 使用PascalCase单数形式(如 MedicalCase, Patient)

#### Scenario: Desktop项目命名
- **GIVEN** 新建Desktop项目
- **WHEN** 命名项目
- **THEN** Core层项目 SHALL 使用 `LYBT.Desktop.{Purpose}` 格式
- **AND** 业务模块 SHALL 使用 `LYBT.Desktop.{Domain}` 格式

---

### Requirement: ARCH-003 依赖方向规范

项目之间的依赖 SHALL 遵循明确的方向规范，禁止循环依赖。

**规范**:
- 依赖方向 SHALL 从高层指向低层
- Entities 层 SHALL NOT 依赖任何其他项目
- Infrastructure 层 SHALL 仅依赖 Entities 和 Shared.Models
- Modules 层 SHALL 依赖 Infrastructure, Entities, Shared.Models
- WebAPI 层 SHALL 依赖 Modules, Infrastructure, Shared.Models
- 循环依赖 SHALL NOT 存在

#### Scenario: Entities无依赖
- **GIVEN** LYBT.Entities 项目
- **WHEN** 检查项目引用
- **THEN** SHALL NOT 引用任何 LYBT.* 项目
- **AND** 仅引用 .NET BCL 和 System.*

#### Scenario: Infrastructure依赖
- **GIVEN** LYBT.Infrastructure 项目
- **WHEN** 检查项目引用
- **THEN** SHALL 引用 LYBT.Entities
- **AND** MAY 引用 LYBT.Shared.Models
- **AND** SHALL NOT 引用 LYBT.Module.* 或 LYBT.WebAPI

#### Scenario: Module依赖
- **GIVEN** LYBT.Module.{Domain} 项目
- **WHEN** 检查项目引用
- **THEN** SHALL 引用 LYBT.Infrastructure
- **AND** SHALL 引用 LYBT.Entities
- **AND** SHALL 引用 LYBT.Shared.Models
- **AND** SHALL NOT 引用其他 LYBT.Module.* 项目
- **AND** SHALL NOT 引用 LYBT.WebAPI

#### Scenario: WebAPI依赖
- **GIVEN** LYBT.WebAPI 项目
- **WHEN** 检查项目引用
- **THEN** SHALL 引用所有 LYBT.Module.* 项目
- **AND** SHALL 引用 LYBT.Infrastructure
- **AND** SHALL 引用 LYBT.Shared.Models

---

### Requirement: ARCH-004 模块注册规范

每个业务模块 SHALL 包含标准的模块注册入口。

**规范**:
- Server Module SHALL 包含 `{Domain}Module.cs` 注册类
- Server Module注册类 SHALL 提供 `AddServices(IServiceCollection)` 扩展方法
- Desktop Module SHALL 实现 `IModule` 接口
- Desktop Module SHALL 在 `RegisterTypes` 中注册所有依赖

#### Scenario: Server Module注册
- **GIVEN** LYBT.Module.Patients 项目
- **WHEN** 检查模块注册
- **THEN** SHALL 存在 `PatientsModule.cs` 文件
- **AND** SHALL 提供 `AddPatientsModule(this IServiceCollection services)` 扩展方法
- **AND** 扩展方法 SHALL 注册 IPatientService, IPatientRepository

#### Scenario: Desktop Module注册
- **GIVEN** LYBT.Desktop.Patients 项目
- **WHEN** 检查模块注册
- **THEN** SHALL 存在 `PatientsModule.cs` 文件
- **AND** SHALL 实现 `IModule` 接口
- **AND** `RegisterTypes` SHALL 注册 Views, ViewModels, Services

---

### Requirement: ARCH-005 项目数量约束

解决方案 SHALL 控制项目数量在合理范围内。

**规范**:
- Server Core项目 SHALL 为 2个 (Entities, Infrastructure)
- Server Module项目 SHALL 为 8个 (Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula)
- Server Services项目 SHALL 为 1个 (WebAPI)
- Shared项目 SHALL 为 4个 (Models, Utilities, Validators, Components)
- Desktop Core项目 SHALL 为 5个 (Contracts, Foundation, Infrastructure, Models, Presentation)
- Desktop Module项目 SHALL 为 8个 (与Server模块对应)
- Desktop Role项目 SHALL 为 2个 (Admin, Clinical)
- Desktop Shell项目 SHALL 为 1个

#### Scenario: 项目总数
- **GIVEN** LYBT.All.sln 解决方案
- **WHEN** 统计项目数量
- **THEN** Server层项目 SHALL 为 11个
- **AND** Shared层项目 SHALL 为 4个
- **AND** Desktop层项目 SHALL 为 16个
- **AND** 总计 SHALL 为 31个核心项目 (不含Tests)

---

## Cross-Reference

- **server-layer-architecture**: Server层详细规范
- **shared-layer-architecture**: Shared层详细规范
- **client-layer-architecture**: Client层详细规范
- **repository-patterns**: Repository层规范
- **service-conventions**: Service层规范
- **viewmodel-conventions**: ViewModel层规范
