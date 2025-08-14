## AGENTS.md — 数据模型模块（LYBT.Models）

### 1. Agent 概述

数据模型模块负责定义全系统领域核心的实体类、数据传输对象（DTO）、分页和查询模型，是业务逻辑、数据访问、接口传输的基础结构层。

### 2. 核心能力

- 定义所有业务核心实体（Model），如 UserModel、PatientModel、DoctorModel、RegistrationModel、PrescriptionModel、BillingModel、LogModel、SyncTaskModel、TreatmentRoomModel 等
- 定义与 Model 对应的数据传输对象（Dto），如 UserDto、PatientDto、PrescriptionDto、RecordDto、... 等
- 提供分页参数/结果结构、常用查询对象
- 约定实体字段类型、必填/非必填、数据注释与基础验证

### 3. 输入输出规范

#### 输入

- 通常不直接被业务调用；为各业务模块方法/接口提供输入输出类型声明

#### 输出


## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
- 作为数据库实体模型类型被业务层/仓储层/前端调用

### 4. 协作与依赖模块

- **业务模块**：如患者、医生、诊疗、费用等，全部依赖核心 Model 
- **基础设施模块**：使用实体定义生成数据库表结构
- **通用模块**：部分字段用通用枚举

### 5. 示例场景

#### 新增实体（如病人）

```csharp
public class PatientModel {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    ...
}
```

### 6. 主要类型示例

- 实体：UserModel、PatientModel、DoctorModel、RegistrationModel、QueueingModel、DiagnosisTreatmentModel、PrescriptionModel、HerbModel、FormulaTemplateModel、BillingModel、PharmacyModel、RecordModel、LogModel、SyncTaskModel、TreatmentRoomModel 等

