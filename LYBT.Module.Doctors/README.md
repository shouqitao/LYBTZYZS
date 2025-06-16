# LYBT.Module.Doctors 模块

## 主要功能

- 医生信息的增删改查
- 结构化领域模型与DTO
- 业务服务与数据访问分层

## 结构说明

- Enums：枚举类型，带中文描述
- Interfaces：服务/仓储接口
- Models：领域实体及DTO
- Services：业务逻辑实现
- Repositories：数据访问实现（可替换为数据库实现）
- Extensions：实体DTO映射扩展
- DoctorsModule.cs：模块注册入口

## 接口说明

- `IDoctorService`：医生业务逻辑接口
- `IDoctorRepository`：数据库操作接口，使用 `AppDbContext` 实现

## 特别说明

- 性别字段已统一调用 LYBT.Common.Enums.Gender 枚举。
- DoctorStatus 仅包含在职、离职。
- DoctorWorkStatus 独立表示在职医生的工作状态（诊所坐诊、外出就诊、休假）。
- 所有枚举用英文命名，[Description] 注解中文。
- 代码全部带详细中文注释。

---
