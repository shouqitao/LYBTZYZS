# LYBT.Shared.Validators 代码知识

共享验证器库，包含 FluentValidation DTO 验证器和业务规则验证框架，供 Server 端请求验证和 Desktop 端输入验证共用。

## 代码文件结构

```
Auth/
├── LoginRequestValidator.cs              # 登录请求验证 (用户名 + 密码)
├── SuperAdminLoginRequestValidator.cs     # 超管登录验证 (仅密码)
└── ChangePasswordRequestValidator.cs      # 修改密码验证 (旧密码 + 新密码)
BusinessRules/
└── MedicalCaseBusinessRules.cs            # 医案核心规则 (纯函数, 无外部依赖)
Consultation/
└── ConsultationInputDtoValidator.cs       # 诊疗 DTO 验证 (仅 TcmDiagnosis 必填)
Prescriptions/
└── PrescriptionInputDtoValidator.cs       # 处方 + 处方项目 DTO 验证
Herbs/
└── HerbInputDtoValidator.cs               # 药材 DTO 验证 (BR-001 ~ BR-008)
MedicalCase/
└── MedicalCaseInputDtoValidator.cs        # 医案 DTO 验证 (嵌套 Prescription)
Users/
└── UserInputDtoValidator.cs               # 用户 DTO 验证 (创建/更新区分)
Patients/
└── PatientInputDtoValidator.cs            # 患者 DTO 验证 (8 个验证点)
Formula/
└── FormulaInputDtoValidator.cs            # 方剂 + 方剂药材项 DTO 验证
```

### Auth/LoginRequestValidator.cs
**LoginRequestValidator** : AbstractValidator\<LoginRequest\> | 登录请求验证，用户名不超过 32 字符，密码不少于 6 字符

### Auth/SuperAdminLoginRequestValidator.cs
**SuperAdminLoginRequestValidator** : AbstractValidator\<SuperAdminLoginRequest\> | 超管登录验证，仅验证密码非空

### Auth/ChangePasswordRequestValidator.cs
**ChangePasswordRequestValidator** : AbstractValidator\<ChangePasswordRequest\> | 修改密码验证，新密码 8-50 字符且不等于旧密码

### BusinessRules/MedicalCaseBusinessRules.cs
**MedicalCaseBusinessRules** : static class | 医案核心业务规则，纯函数无外部依赖，Server 和 Client 共用

| 方法 | 说明 |
|------|------|
| CanCreateNewCase(existingStatuses) | 患者同时只能有一个 Active 或 Suspended 医案 |
| IsValidStatusTransition(from, to) | 状态流转: Suspended <-> Active 双向 |
| HasActiveCase(statuses) | 是否有 Active 状态医案 |
| HasSuspendedCase(statuses) | 是否有 Suspended 状态医案 |

### Consultation/ConsultationInputDtoValidator.cs
**ConsultationInputDtoValidator** : AbstractValidator\<ConsultationInputDto\> | 诊疗验证，TcmDiagnosis 必填且不超过 500 字符。PatientId/UserId 不验证必填 (Consultation 在 MedicalCase 创建时自动生成)

### Prescriptions/PrescriptionInputDtoValidator.cs
**PrescriptionInputDtoValidator** : AbstractValidator\<PrescriptionInputDto\> | 处方 DTO 验证，MedicalCaseId 创建时必填，剂数 1-100，折扣 0-1，嵌套验证处方项目

**PrescriptionItemInputDtoValidator** : AbstractValidator\<PrescriptionItemInputDto\> | 处方项目验证，HerbId 必填，用量 0-1000，Usage/Remark 长度限制

### Herbs/HerbInputDtoValidator.cs
**HerbInputDtoValidator** : AbstractValidator\<HerbInputDto\> | 药材验证，使用 ValidationConstants 统一常量。名称必填 1-50，单位必填，单价大于 0

### MedicalCase/MedicalCaseInputDtoValidator.cs
**MedicalCaseInputDtoValidator** : AbstractValidator\<MedicalCaseInputDto\> | 医案验证，PatientId/UserId 必填，Remark 可选长度限制，嵌套验证 Prescription

### Users/UserInputDtoValidator.cs
**UserInputDtoValidator** : AbstractValidator\<UserInputDto\> | 用户验证，创建时 UserName/RealName/Role 必填，密码 8 字符起，邮箱/手机号格式校验

### Patients/PatientInputDtoValidator.cs
**PatientInputDtoValidator** : AbstractValidator\<PatientInputDto\> | 患者验证，Name/IdNumber/PhoneNumber/Address 必填，BirthDate 须小于等于今天，AllergyHistory/MedicalHistory 长度限制

### Formula/FormulaInputDtoValidator.cs
**FormulaInputDtoValidator** : AbstractValidator\<FormulaInputDto\> | 方剂验证，名称必填，药材列表不可空且非空

**FormulaHerbItemInputDtoValidator** : AbstractValidator\<FormulaHerbItemInputDto\> | 方剂药材项验证，HerbName/Unit 必填，HerbId 可空支持延迟绑定，Dosage 0-1000

## 死代码清理记录

| 类型/方法 | 状态 | 说明 |
|-----------|------|------|
| IBusinessRuleValidator.cs | [已清理] 2026-03-01 | 文件已删除，含非泛型/泛型/操作验证三个接口，无外部模块引用 |
| BaseBusinessRuleValidator.cs | [已清理] 2026-03-01 | 文件已删除，含基类及泛型变体，未接入 DI 体系 |
| ValidationContext.cs | [已清理] 2026-03-01 | 文件已删除，业务规则验证上下文，无外部使用 |
| PatientBusinessRuleValidator.cs | [已清理] 2026-03-01 | 文件已删除，功能已被 PatientInputDtoValidator (FluentValidation) 替代 |
| UserBusinessRuleValidator.cs | [已清理] 2026-03-01 | 文件已删除，功能已被 UserInputDtoValidator (FluentValidation) 替代 |
| PrescriptionBusinessRuleValidator.cs | [已清理] 2026-03-01 | 文件已删除，功能已被 PrescriptionInputDtoValidator (FluentValidation) 替代 |
| Shared.Models/Enums/ValidationEnums.cs (BusinessOperation) | [已清理] 2026-03-01 | 关联文件已从 Shared.Models 删除，BusinessOperation 枚举随 ValidationContext 一并移除 |

### 保留项

| 类型/方法 | 状态 | 说明 |
|-----------|------|------|
| SuperAdminLoginRequestValidator | [SUSPECT] | 通过 AddValidatorsFromAssemblyContaining 程序集扫描隐式注册，待确认使用情况 |
| ChangePasswordRequestValidator | [SUSPECT] | 同上，通过程序集扫描隐式注册，待确认使用情况 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| BusinessRules/ 目录 | 2026-03-01 清理后仅保留 MedicalCaseBusinessRules.cs | 接口/基类/三个实现 Validator 已全部删除，框架层代码与 FluentValidation 重叠 | 目录仅保留纯函数的 MedicalCaseBusinessRules |
| MedicalCaseBusinessRules | 设计良好 | 纯函数无依赖，被 Server (MedicalCaseRules) 和 Client (LocalMedicalCaseDataSource) 实际引用 | 保持现状，是 Shared 层共享业务规则的标杆实现 |
| README.md | 目录结构过时 | README 列出 PrescriptionCreateDtoValidator.cs/PrescriptionEditDtoValidator.cs，实际已合并为 PrescriptionInputDtoValidator.cs；Common/ValidationConstants.cs 已迁移到 LYBT.Shared.Primitives | 更新 README 目录结构 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| FluentValidation 验证器通过 AddValidatorsFromAssemblyContaining 程序集扫描注册 | 只要其中一个验证器被指定，同程序集所有验证器自动注册 | 新增验证器只需放在 LYBT.Shared.Validators 程序集内即可自动发现 |
| ValidationContext 已删除 | 原与 FluentValidation.ValidationContext 同名导致混淆，2026-03-01 已随 BusinessRules 框架清理删除 | 不再存在命名冲突 |
| BusinessOperation 枚举已删除 | 原在 ValidationContext.cs 定义，曾迁移到 ValidationEnums.cs，2026-03-01 随清理一并删除 | 如需类似功能需重新定义 |
| ConsultationInputDtoValidator 不验证 PatientId/UserId | Consultation 在 MedicalCase 创建时自动生成，这两个字段通过 MedicalCase 关联获取 | 不要误以为漏掉了必填验证 |
| FormulaHerbItemInputDtoValidator 中 HerbId 不验证 NotEmpty | 支持延迟绑定场景 (Issue #2014)，方剂可先保存后绑定药材 | 需要在 Service 层处理 HerbId 为空的情况 |
