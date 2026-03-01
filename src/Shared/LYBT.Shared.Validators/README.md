# LYBT.Shared.Validators

> 共享验证器库 | FluentValidation规则 | 业务规则验证

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的DTO验证器和业务规则验证

## 目录结构

```
LYBT.Shared.Validators/
├── BusinessRules/            # 业务规则验证(1文件)
│   └── MedicalCaseBusinessRules.cs
├── Auth/                     # 认证验证器(3文件)
│   ├── LoginRequestValidator.cs
│   ├── ChangePasswordRequestValidator.cs
│   └── SuperAdminLoginRequestValidator.cs
├── Users/                    # 用户验证器
│   └── UserInputDtoValidator.cs
├── Patients/                 # 患者验证器
│   └── PatientInputDtoValidator.cs
├── MedicalCase/              # 医案验证器
│   └── MedicalCaseInputDtoValidator.cs
├── Consultation/             # 诊断验证器
│   └── ConsultationInputDtoValidator.cs
├── Prescriptions/            # 处方验证器
│   └── PrescriptionInputDtoValidator.cs
├── Herbs/                    # 药材验证器
│   └── HerbInputDtoValidator.cs
└── Formula/                  # 验方验证器
    └── FormulaInputDtoValidator.cs
```

## 核心组件

| 组件 | 说明 |
|------|------|
| MedicalCaseBusinessRules | 医案核心业务规则 (纯函数，Server/Client共用) |
| *InputDtoValidator | 各模块DTO输入验证器 (FluentValidation) |

## 验证器覆盖

| 模块 | 验证器数 | 说明 |
|------|----------|------|
| Auth | 3 | 登录/修改密码/超管登录 |
| Users | 1 | 用户输入验证 |
| Patients | 1 | 患者输入验证 |
| MedicalCase | 1 | 医案输入验证 |
| Consultation | 1 | 诊断输入验证 |
| Prescriptions | 1 | 处方输入验证 |
| Herbs | 1 | 药材输入验证 |
| Formula | 1 | 验方输入验证 |

## 业务规则验证

| 验证器 | 说明 |
|--------|------|
| MedicalCaseBusinessRules | 医案状态流转/Active唯一性/Suspended唯一性 (纯函数，无外部依赖) |

## 设计依据

- 验证规则集中于 Shared 层而非各模块内，Server 端请求验证和 Desktop 端输入验证共用同一套规则，避免规则不一致
- 采用 FluentValidation 框架，声明式规则定义比手写 if-else 更易维护和测试
- MedicalCaseBusinessRules 为纯函数设计，无外部依赖，是 Shared 层共享业务规则的标杆实现
- 实际被 7 个 Server 模块和 2 个 Desktop 模块引用，验证了集中化设计的复用价值

## 依赖关系

### 依赖
- LYBT.Shared.Models (DTO定义)
- LYBT.Shared.Primitives (ValidationConstants等基础常量)
- FluentValidation (验证框架)

### 被依赖
- LYBT.Module.*(所有Server模块使用验证器)
- LYBT.WebAPI (请求验证)
- LYBT.Desktop.*(Client端输入验证)

### NuGet包
- FluentValidation (11.x)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 死代码清理: 移除 BusinessRules 框架 (保留 MedicalCaseBusinessRules), 修正目录结构 |
| 2025-12-04 | 按README规范创建文档 |
| 2025-10-29 | 初始版本 |
