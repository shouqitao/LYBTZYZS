# LYBT.Models 功能说明文档

## 模块概述

LYBT.Models 是传统中医诊所管理系统的核心数据模型项目，定义了系统中所有业务实体的数据结构和数据传输对象(DTOs)。本项目采用领域驱动设计(DDD)理念，按业务领域组织模型，支持整个系统的数据一致性和业务规则约束。

## 项目结构

```
LYBT.Models/
├── Auth/                    # 认证相关DTOs
├── Billing/                 # 计费管理模型
├── Common/                  # 通用接口和基础类
├── Configuration/           # 系统配置模型
├── DiagnosisTreatment/      # 诊疗模型
├── Doctors/                 # 医生信息模型
├── FormulaTemplates/        # 经验方模板模型
├── Herbs/                   # 药材管理模型
├── Patients/               # 患者档案模型
├── Pharmacy/               # 药房管理模型
├── Prescriptions/          # 处方管理模型
├── Queueing/               # 排队管理模型
├── Records/                # 病历管理模型
├── Registration/           # 挂号管理模型
├── Sync/                   # 数据同步模型
├── TreatmentRoom/          # 治疗室管理模型
└── Users/                  # 用户管理模型
```

## 核心数据模型

### 1. 用户管理 (Users)

#### UserModel - 用户实体

**文件位置**: `Users/UserModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 用户唯一标识（主键） | 必填 |
| UserName | string | 用户名（唯一） | 长度2-32字符，必填 |
| RealName | string | 真实姓名 | 最长20字符，必填 |
| PinyinCode | string | 姓名拼音码 | 最长32字符，用于快速检索 |
| Role | UserRole | 用户角色 | 必填，枚举类型 |
| IsActive | bool | 启用状态 | 默认true |
| CreatedTime | DateTime | 创建时间 | 系统自动设置 |
| LastLoginTime | DateTime? | 最近登录时间 | 可为空 |
| PasswordHash | string | 密码哈希值 | 必填，敏感信息 |
| FailedLoginCount | int | 连续登录失败次数 | 用于账户锁定策略 |
| LockoutEnd | DateTime? | 账号锁定截止时间 | null表示未锁定 |
| Email | string? | 邮箱地址 | 可选，需符合邮箱格式 |
| PhoneNumber | string? | 手机号码 | 可选，需符合手机号格式 |

#### AdminSecretModel - 系统管理员密码

**文件位置**: `Users/AdminSecretModel.cs`

系统管理员特殊密码管理，用于sysadmin账户的密码存储。

### 2. 患者管理 (Patients)

#### PatientModel - 患者档案实体

**文件位置**: `Patients/PatientModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 患者档案唯一标识（主键） | 必填 |
| Name | string | 患者姓名 | 最长64字符，必填 |
| PinyinCode | string | 姓名拼音简码 | 最长32字符，用于快速搜索 |
| WuBiCode | string | 五笔编码 | 最长32字符，用于快速检索 |
| Gender | Gender | 性别 | 枚举：男/女/未知 |
| Age | int? | 年龄 | 可选，0-150范围 |
| PhoneNumber | string? | 联系电话 | 可选，符合手机号格式 |
| Address | string? | 详细地址 | 可选，最长200字符 |
| IdCard | string? | 身份证号 | 可选，符合身份证格式 |
| MedicalHistory | string? | 既往病史 | 可选，最长1000字符 |
| Allergies | string? | 过敏史 | 可选，最长500字符 |
| IsActive | bool | 是否启用 | 支持软删除策略 |
| CreatedTime | DateTime | 创建时间 | 系统自动设置 |
| UpdatedTime | DateTime | 更新时间 | 系统自动设置 |

### 3. 医生管理 (Doctors)

#### DoctorModel - 医生信息实体

**文件位置**: `Doctors/DoctorModel.cs`

关联用户系统，扩展医生专业信息。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 医生唯一标识（主键） | 必填 |
| UserId | Guid? | 关联用户ID | 可选，关联Users表 |
| Gender | Gender | 性别 | 枚举类型 |
| Age | int? | 年龄 | 可选，18-100范围 |
| Specialty | string? | 专业特长 | 可选，最长100字符 |
| LicenseNumber | string? | 执业证号 | 可选，最长32字符 |
| Department | string? | 科室 | 可选，最长50字符 |
| Title | string? | 职称 | 可选，最长50字符 |
| Introduction | string? | 个人简介 | 可选，最长1000字符 |
| Experience | int? | 从业年限 | 可选，0-50范围 |
| IsActive | bool | 是否启用 | 支持软删除策略 |
| CreatedTime | DateTime | 创建时间 | 系统自动设置 |

### 4. 药材管理 (Herbs)

#### HerbModel - 药材信息实体

**文件位置**: `Herbs/HerbModel.cs`

中药材基础信息管理，支持快速检索和库存管理。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 药材唯一标识（主键） | 必填 |
| Name | string | 药材名称 | 最长64字符，必填 |
| Specification | decimal? | 基础规格数值 | 可选，用于计算实际用量 |
| Unit | string | 单位 | 最长16字符，如：克、钱、两 |
| PinyinCode | string? | 拼音简码 | 可选，最长32字符 |
| WuBiCode | string? | 五笔编码 | 可选，最长32字符 |
| Category | HerbCategory | 药材类别 | 枚举类型 |
| Origin | string? | 产地 | 可选，最长100字符 |
| Price | decimal? | 单价 | 可选，decimal(18,2) |
| StockQuantity | decimal? | 库存数量 | 可选，decimal(18,3) |
| SafetyStock | decimal? | 安全库存 | 可选，预警线 |
| BatchNo | string? | 批次号 | 可选，最长32字符 |
| ExpireDate | DateTime? | 过期时间 | 可选，用于库存管理 |
| Supplier | string? | 供应商 | 可选，最长100字符 |
| Properties | string? | 药性 | 可选，中医药性描述 |
| Efficacy | string? | 功效 | 可选，药材功效说明 |
| IsActive | bool | 是否启用 | 支持软删除策略 |

### 5. 处方管理 (Prescriptions)

#### PrescriptionModel - 处方实体

**文件位置**: `Prescriptions/PrescriptionModel.cs`

中医处方管理，支持多验方组合和重复药材检测。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 处方唯一标识（主键） | 必填 |
| PatientId | Guid | 关联患者ID | 必填 |
| DoctorId | Guid | 开方医生ID | 必填 |
| PrescriptionNo | string? | 处方编号 | 可选，最长32字符 |
| TotalPrice | decimal | 处方总价 | decimal(18,2) |
| Status | PrescriptionStatus | 处方状态 | 枚举：草稿/已开/已调配/已完成 |
| Dosage | int | 剂数 | 默认1，用于计算总量 |
| Usage | string? | 服用方法 | 可选，最长500字符 |
| Remarks | string? | 备注信息 | 可选，最长1000字符 |
| CreatedTime | DateTime | 开方时间 | 系统自动设置 |
| CompletedTime | DateTime? | 完成时间 | 可选 |

#### PrescriptionItemModel - 处方药材明细

**文件位置**: `Prescriptions/PrescriptionItemModel.cs`

处方中的具体药材配置。

### 6. 经验方模板 (FormulaTemplates)

#### FormulaTemplateModel - 经验方模板实体

**文件位置**: `FormulaTemplates/FormulaTemplateModel.cs`

传统中医经验方管理，支持验方共享和模板化开方。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 模板唯一标识（主键） | 必填 |
| Name | string | 经验方名称 | 最长200字符，必填 |
| Effect | string? | 方剂功效 | 可选，最长500字符 |
| Usage | string? | 服用方法 | 可选，最长500字符 |
| Remark | string? | 备注信息 | 可选，最长1000字符 |
| Herbs | List&lt;FormulaTemplateHerbItem&gt; | 药材组成 | 方剂中包含的药材列表 |
| Property | string? | 性味归经 | 可选，中医理论属性 |
| IsActive | bool | 是否启用 | 支持软删除策略 |
| IsShared | bool | 是否共享 | 支持给其他医生使用 |
| CreatedById | Guid? | 创建者ID | 可选 |
| CreatedAt | DateTime | 创建时间 | 系统自动设置 |
| UpdatedAt | DateTime | 更新时间 | 系统自动设置 |

#### FormulaTemplateHerbItem - 验方药材项

**文件位置**: `FormulaTemplates/FormulaTemplateHerbItem.cs`

实现 `IHerbItem` 接口，验方模板中的药材组成。

### 7. 计费管理 (Billing)

#### BillingModel - 账单实体

**文件位置**: `Billing/BillingModel.cs`

诊所费用结算管理，支持多种支付方式和退款处理。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 账单唯一标识（主键） | 必填 |
| BillingId | string | 账单业务编码 | 最长64字符，如流水号 |
| PatientId | Guid | 病人ID | 必填 |
| PrescriptionId | Guid? | 对应处方ID | 可选 |
| Items | List&lt;BillingItem&gt; | 账单明细项目 | 必填，账单明细列表 |
| TotalAmount | decimal | 账单总金额 | decimal(18,2)，必填 |
| PaidAmount | decimal | 已缴金额 | decimal(18,2) |
| Status | BillingStatus | 当前状态 | 枚举：待缴费/已缴费/部分退款/已退款 |
| PaymentMethod | string | 缴费方式 | 最长32字符，如现金、微信等 |
| DoctorId | Guid | 开单医生ID | 必填 |
| CreatedTime | DateTime | 创建时间 | 系统自动设置 |
| PaidTime | DateTime? | 支付时间 | 可选 |
| CompletedTime | DateTime? | 完成时间 | 可选 |
| RefundTime | DateTime? | 退款时间 | 可选 |
| RefundReason | string? | 退款理由 | 可选，最长128字符 |
| BillingTime | DateTime | 账单时间 | 与创建时间可区分 |

#### BillingItem - 账单明细实体

**文件位置**: `Billing/BillingModel.cs`

账单的具体收费项目明细。

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| ItemId | Guid | 明细主键ID | 系统自动生成 |
| BillingId | Guid | 所属账单ID | 必填，外键关联 |
| Name | string | 项目名称 | 最长64字符，必填 |
| UnitPrice | decimal | 单价 | decimal(18,2) |
| Quantity | decimal | 数量 | decimal(18,2) |
| SubTotal | decimal | 小计 | 计算属性，单价×数量 |

### 8. 挂号管理 (Registration)

#### RegistrationModel - 挂号实体

**文件位置**: `Registration/RegistrationModel.cs`

患者挂号信息管理，支持预约和现场挂号。

### 9. 排队管理 (Queueing)

#### QueueingModel - 排队实体

**文件位置**: `Queueing/QueueingModel.cs`

诊所排队叫号系统，管理患者就诊顺序。

### 10. 诊疗管理 (DiagnosisTreatment)

#### DiagnosisTreatmentModel - 诊疗实体

**文件位置**: `DiagnosisTreatment/DiagnosisTreatmentModel.cs`

诊断和治疗记录，包含中医四诊信息。包含嵌套的方剂信息(FormulaModel)和治疗项目(TreatmentItemModel)。

### 11. 病历管理 (Records)

#### RecordModel - 病历实体

**文件位置**: `Records/RecordModel.cs`

完整的病历档案管理，支持病历共享和历史追踪。

### 12. 药房管理 (Pharmacy)

#### PharmacyModel - 药房实体

**文件位置**: `Pharmacy/PharmacyModel.cs`

药房信息和药材库存管理。

### 13. 治疗室管理 (TreatmentRoom)

#### TreatmentRoomModel - 治疗室实体

**文件位置**: `TreatmentRoom/TreatmentRoomModel.cs`

治疗室和设施管理。

### 14. 数据同步 (Sync)

#### SyncTaskModel & SyncLogModel - 同步任务和日志

**文件位置**: `Sync/SyncTaskModel.cs`, `Sync/SyncLogModel.cs`

数据同步任务管理和同步日志记录。

## 通用接口和基础类

### IHerbItem - 药材项基础接口

**文件位置**: `Common/IHerbItem.cs`

定义药材在不同场景下的通用属性：
- HerbId: 药材ID（关联药材库）
- HerbName: 药材名称
- Quantity: 剂量（实际用量）
- Unit: 单位

实现类包括：
- FormulaTemplateHerbItem（经验方药材项）
- 其他药材使用场景

## DTO 数据传输对象

每个业务领域都包含完整的DTO体系：

### 常见DTO类型
- **Dto**: 基础展示对象，用于列表显示
- **DetailDto**: 详细信息对象，用于详情查看
- **CreateDto**: 创建对象，用于新增操作
- **EditDto**: 编辑对象，用于更新操作
- **QueryDto**: 查询对象，用于分页和筛选
- **PagedQueryDto**: 分页查询对象

### 示例：用户管理DTOs
- **UserDto**: 用户基本信息展示
- **UserDetailDto**: 用户详细信息
- **UserCreateDto**: 创建用户请求
- **ChangePasswordDto**: 修改密码请求
- **ChangeProfileDto**: 修改个人资料请求
- **ResetPasswordDto**: 重置密码请求
- **UserQueryDto**: 用户查询条件
- **BatchIdsDto**: 批量操作ID集合

## 系统配置模型 (Configuration)

### TreatmentCatalogModel - 治疗目录

**文件位置**: `Configuration/TreatmentCatalogModel.cs`

系统治疗项目目录配置，支持层级结构和价格管理。

### TreatmentRoomInfoModel - 治疗室信息配置

**文件位置**: `Configuration/TreatmentRoomInfoModel.cs`

治疗室基础信息配置。

## 认证相关DTOs (Auth)

### 登录认证DTOs
- **LoginRequestDto**: 登录请求
- **LoginResponseDto**: 登录响应
- **LogoutRequestDto**: 登出请求
- **ChangeSysAdminPasswordDto**: 系统管理员密码修改

## 设计特点

### 1. 领域驱动设计
- 按业务领域组织模型结构
- 每个领域包含完整的实体和DTOs
- 支持领域内的业务规则约束

### 2. 数据验证
- 使用 DataAnnotations 进行字段验证
- 支持长度限制、必填验证、格式验证
- 自定义验证规则支持

### 3. 软删除策略
- 关键实体支持 IsActive 字段
- 避免物理删除，保持数据完整性
- 支持数据恢复和审计

### 4. 快速检索支持
- 拼音码和五笔码字段
- 支持中文输入法快速检索
- 提升用户操作体验

### 5. 审计追踪
- 创建时间、更新时间字段
- 操作人员记录
- 支持完整的操作历史追踪

### 6. 扩展性设计
- 接口抽象和继承
- 枚举类型支持业务扩展
- 预留扩展字段和配置

## 数据库关系

模型之间的主要关系：
- Users ← Doctors (一对一，可选关联)
- Patients → Registrations (一对多)
- Patients → Prescriptions (一对多)
- Doctors → Prescriptions (一对多)
- Prescriptions → PrescriptionItems (一对多)
- Herbs ← PrescriptionItems (多对一)
- FormulaTemplates → FormulaTemplateHerbItems (一对多)
- Billings → BillingItems (一对多)
- Patients → Records (一对多)

## 版本兼容性

- 目标框架：.NET 8.0
- 支持可空引用类型
- 使用现代C#语法特性
- 兼容Entity Framework Core

本模块为整个LYBT传统中医诊所管理系统提供了完整的数据基础，确保了数据一致性、业务规则约束和系统扩展性。