# LYBT.Shared.Validators

> 共享验证器库 | FluentValidation规则 | 业务规则验证

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的DTO验证器和业务规则验证

## 目录结构

```
LYBT.Shared.Validators/
├── Common/                   # 通用验证
│   └── ValidationConstants.cs
├── BusinessRules/            # 业务规则验证(6文件)
│   ├── IBusinessRuleValidator.cs
│   ├── BaseBusinessRuleValidator.cs
│   ├── ValidationContext.cs
│   ├── UserBusinessRuleValidator.cs
│   ├── PatientBusinessRuleValidator.cs
│   └── PrescriptionBusinessRuleValidator.cs
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
├── Prescriptions/            # 处方验证器(2文件)
│   ├── PrescriptionCreateDtoValidator.cs
│   └── PrescriptionEditDtoValidator.cs
├── Herbs/                    # 药材验证器
│   └── HerbInputDtoValidator.cs
└── Formula/                  # 验方验证器
    └── FormulaInputDtoValidator.cs
```

## 核心组件

| 组件 | 说明 |
|------|------|
| IBusinessRuleValidator | 业务规则验证接口 |
| BaseBusinessRuleValidator | 业务规则验证基类 |
| ValidationContext | 验证上下文(传递依赖数据) |
| *InputDtoValidator | 各模块DTO输入验证器 |

## 验证器覆盖

| 模块 | 验证器数 | 说明 |
|------|----------|------|
| Auth | 3 | 登录/修改密码/超管登录 |
| Users | 1 | 用户输入验证 |
| Patients | 1 | 患者输入验证 |
| MedicalCase | 1 | 医案输入验证 |
| Consultation | 1 | 诊断输入验证 |
| Prescriptions | 2 | 创建/编辑处方验证 |
| Herbs | 1 | 药材输入验证 |
| Formula | 1 | 验方输入验证 |

## 业务规则验证

| 验证器 | 说明 |
|--------|------|
| UserBusinessRuleValidator | 用户名唯一性、角色有效性 |
| PatientBusinessRuleValidator | 身份证唯一性、年龄合理性 |
| PrescriptionBusinessRuleValidator | 药材重复检查、剂量合理性 |

## 依赖关系

### 依赖
- LYBT.Shared.Models (DTO定义)
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
| 2025-12-04 | 按README规范创建文档 |
| 2025-10-29 | 初始版本 |
