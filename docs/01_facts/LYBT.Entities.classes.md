# LYBT.Entities 项目技术文档

> **生成时间**: 2025-09-10  
> **文档版本**: v1.0  
> **项目**: 凌隐宝堂中医诊所系统 (LYBTZYZS)  

## UserModel (src/Server/Core/LYBT.Entities/Users/UserModel.cs:1-180)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Users
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model
- **表名**: `Users`

### 2) 特性与注解
- `[Table("Users")]` - 指定数据库表名
- `[DisplayName("用户")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 用户唯一标识 |
| Username | string | 否 | [Required, StringLength(50)] | 用户名(统一命名) |
| RealName | string | 否 | [Required, StringLength(50), DisplayName("真实姓名")] | 真实姓名 |
| PinYinCode | string | 是 | [StringLength(50), DisplayName("拼音码")] | 拼音码(快速搜索) |
| PhoneNumber | string | 是 | [StringLength(20), DisplayName("电话号码")] | 电话号码 |
| Email | string | 是 | [StringLength(100), DisplayName("邮箱地址")] | 邮箱地址 |
| Role | UserRole | 否 | [DisplayName("用户角色")] | 用户角色(默认Doctor) |
| Status | CommonStatus | 否 | [DisplayName("用户状态")] | 用户状态(默认Enabled) |
| PasswordHash | string | 否 | [Required, StringLength(256)] | 密码哈希 |
| FailedLoginCount | int | 否 | [DisplayName("失败登录次数")] | 失败登录次数 |
| LockoutEnd | DateTime | 是 | [DisplayName("锁定结束时间")] | 锁定结束时间 |
| Specialty | string | 是 | [StringLength(200), DisplayName("专长")] | 专长 |
| RegistrationFee | decimal | 是 | [Column(TypeName = "decimal(18,2)"), DisplayName("挂号费")] | 挂号费 |
| LicenseNumber | string | 是 | [StringLength(50), DisplayName("执业证书号")] | 执业证书号 |
| Introduction | string | 是 | [StringLength(1000), DisplayName("简介")] | 简介 |
| CreatedTime | DateTime | 否 | [DisplayName("创建时间")] | 创建时间(默认Now) |
| UpdateTime | DateTime | 是 | [DisplayName("最后更新时间")] | 最后更新时间 |
| LastLoginTime | DateTime | 是 | [DisplayName("最后登录时间")] | 最后登录时间 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 4) 业务特点
- **医生角色融合**: 统一管理医生和管理员，医生专属字段可选
- **安全机制**: 包含失败登录计数和锁定机制
- **拼音搜索**: 支持中文姓名的快速检索
- **扩展性**: 医生专属字段（专长、挂号费、执业证书）设计为可选

### 5) 默认值配置
- `Role = UserRole.Doctor` - 默认为医生角色
- `Status = CommonStatus.Enabled` - 默认为启用状态
- `CreatedTime = DateTime.Now` - 创建时间自动设置
- `FailedLoginCount = 0` - 失败登录次数初始为0

---

## PatientModel (src/Server/Core/LYBT.Entities/Patients/PatientModel.cs:1-220)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Patients
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model
- **表名**: `Patients`

### 2) 特性与注解
- `[Table("Patients")]` - 指定数据库表名
- `[DisplayName("患者")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 患者唯一标识 |
| Name | string | 否 | [Required, StringLength(100), DisplayName("患者姓名")] | 患者姓名 |
| PinYinCode | string | 是 | [StringLength(20), DisplayName("拼音码")] | 拼音码 |
| Gender | Gender | 否 | [DisplayName("性别")] | 性别(默认Unknown) |
| BirthDate | DateTime | 是 | [DisplayName("出生日期")] | 出生日期 |
| Age | int | 是 | [NotMapped, DisplayName("年龄")] | 计算属性-年龄 |
| MaritalStatus | int | 否 | [DisplayName("婚姻状态")] | 婚姻状态 |
| IdType | int | 否 | [DisplayName("证件类型")] | 证件类型 |
| IdNumber | string | 是 | [StringLength(50), SensitiveData(SensitiveDataType.IdentityInfo), DisplayName("证件号码")] | 证件号码 |
| PhoneNumber | string | 是 | [StringLength(20), SensitiveData(SensitiveDataType.ContactInfo), DisplayName("手机号码")] | 手机号码 |
| Address | string | 是 | [StringLength(256), SensitiveData(SensitiveDataType.PersonalInfo), DisplayName("地址")] | 地址 |
| AllergyHistory | string | 是 | [StringLength(500), SensitiveData(SensitiveDataType.MedicalInfo), DisplayName("过敏史")] | 过敏史 |
| BloodType | int | 否 | [DisplayName("血型")] | 血型 |
| EmergencyContactName | string | 是 | [StringLength(50), DisplayName("紧急联系人姓名")] | 紧急联系人姓名 |
| EmergencyContactPhone | string | 是 | [StringLength(20), DisplayName("紧急联系人电话")] | 紧急联系人电话 |
| EmergencyContactRelation | string | 是 | [StringLength(30), DisplayName("紧急联系人关系")] | 紧急联系人关系 |
| Status | CommonStatus | 否 | [DisplayName("患者状态")] | 患者状态(默认Enabled) |
| DisableReason | string | 是 | [StringLength(128), DisplayName("禁用原因")] | 禁用原因 |
| LastVisitTime | DateTime | 是 | [DisplayName("最后就诊时间")] | 最后就诊时间 |
| VisitCount | int | 否 | [DisplayName("就诊次数")] | 就诊次数 |
| CreatedAt | DateTime | 否 | [DisplayName("创建时间")] | 创建时间 |
| UpdateTime | DateTime | 是 | [DisplayName("更新时间")] | 更新时间 |
| CreatedBy | Guid | 是 | [DisplayName("创建者ID")] | 创建者ID |
| UpdatedBy | Guid | 是 | [DisplayName("更新者ID")] | 更新者ID |

### 4) 计算属性方法

#### Age (年龄计算属性)
- **源码位置**: `src/Server/Core/LYBT.Entities/Patients/PatientModel.cs:85-95`
- **特性**: `[NotMapped]` - 不映射到数据库
- **计算逻辑**: 基于出生日期和当前日期计算年龄
- **返回类型**: `int?`
- **计算公式**:
  ```csharp
  get 
  {
      if (!BirthDate.HasValue) return null;
      var today = DateTime.Today;
      var age = today.Year - BirthDate.Value.Year;
      if (BirthDate.Value.Date > today.AddYears(-age)) age--;
      return age;
  }
  ```

### 5) 敏感数据保护
使用`SensitiveDataAttribute`标记敏感字段：
- **IdentityInfo**: 证件号码 (部分脱敏)
- **ContactInfo**: 手机号码 (部分脱敏) 
- **PersonalInfo**: 地址 (默认脱敏)
- **MedicalInfo**: 过敏史 (哈希脱敏)

### 6) 业务特点
- **年龄计算**: 基于出生日期的自动年龄计算属性
- **就诊统计**: 自动维护就诊次数和最后就诊时间
- **数据安全**: Epic 05-P0-03 数据安全保障要求
- **完整档案**: 包含身份、联系、医疗、紧急联系人等完整信息

---

## MedicalCaseModel (src/Server/Core/LYBT.Entities/MedicalCase/MedicalCaseModel.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.MedicalCase
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 聚合根
- **表名**: `MedicalCases`

### 2) 特性与注解
- `[Table("MedicalCases")]` - 指定数据库表名
- `[DisplayName("医疗案例")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 医疗案例ID |
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| PatientName | string | 否 | [Required, StringLength(50), DisplayName("患者姓名")] | 患者姓名(显示用) |
| DoctorId | Guid | 否 | [Required, DisplayName("医生ID")] | 医生ID |
| DoctorName | string | 否 | [Required, StringLength(50), DisplayName("医生姓名")] | 医生姓名(显示用) |
| PrescriptionId | Guid | 是 | [DisplayName("处方ID")] | 处方ID(可为空) |
| ConsultationDate | DateTime | 否 | [DisplayName("看诊时间")] | 看诊时间(默认Now) |
| Status | MedicalCaseStatus | 否 | [DisplayName("状态")] | 状态(默认Registered) |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 4) 导航属性

| 属性名 | 类型 | 关系类型 | 说明 |
|--------|------|----------|------|
| Consultation | virtual Consultation | 一对一 | 看诊记录 |
| Prescription | virtual Prescription | 一对零或一 | 处方信息 |

### 5) 关系模型
- **聚合根**: 作为诊疗流程的聚合根，管理完整病历
- **1:1关系**: 与Consultation是一对一关系  
- **可选处方**: 处方是可选的，支持仅诊断不开方的场景
- **冗余字段**: PatientName和DoctorName用于显示，避免关联查询

### 6) 业务状态流转
```
Registered → InProgress → Completed
     ↓             ↓
  Cancelled    Cancelled
```

### 7) 默认值配置
- `ConsultationDate = DateTime.Now` - 看诊时间默认当前时间
- `Status = MedicalCaseStatus.Registered` - 状态默认为挂号完成

---

## ConsultationModel (src/Server/Core/LYBT.Entities/Consultation/ConsultationModel.cs:1-160)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Consultation
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 中医诊断专用
- **表名**: `Consultations`

### 2) 特性与注解
- `[Table("Consultations")]` - 指定数据库表名
- `[DisplayName("看诊")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 看诊ID |
| MedicalCaseId | Guid | 否 | [Required, DisplayName("医疗案例ID")] | 医疗案例ID |
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| UserId | Guid | 否 | [Required, DisplayName("关联用户ID")] | 关联用户ID(医生) |
| ChiefComplaint | string | 是 | [StringLength(500), DisplayName("主诉")] | 主诉 |
| PresentIllness | string | 是 | [StringLength(1000), DisplayName("现病史")] | 现病史 |
| Inspection | string | 是 | [StringLength(500), DisplayName("望诊")] | 望诊 |
| AuscultationOlfaction | string | 是 | [StringLength(500), DisplayName("闻诊")] | 闻诊 |
| Inquiry | string | 是 | [StringLength(500), DisplayName("问诊")] | 问诊 |
| Palpation | string | 是 | [StringLength(500), DisplayName("切诊")] | 切诊(脉诊、舌诊等) |
| TCMDiagnosis | string | 否 | [Required, StringLength(500), DisplayName("中医辨证")] | 中医辨证 |
| TreatmentPrinciple | string | 是 | [StringLength(500), DisplayName("治疗原则")] | 治疗原则 |
| MedicalAdvice | string | 是 | [StringLength(1000), DisplayName("医嘱")] | 医嘱 |
| Status | CommonStatus | 否 | [DisplayName("状态")] | 状态(默认Enabled) |
| Remark | string | 是 | [StringLength(500), DisplayName("备注信息")] | 备注信息 |

### 4) 导航属性

| 属性名 | 类型 | 关系类型 | 说明 |
|--------|------|----------|------|
| Patient | virtual Patient | 多对一 | 患者信息 |
| User | virtual User | 多对一 | 医生信息 |
| MedicalCase | virtual MedicalCase | 一对一 | 医疗案例 |

### 5) 中医四诊体系

#### 四诊记录字段
1. **望诊 (Inspection)**: 观察患者神色、形态、舌象等
2. **闻诊 (AuscultationOlfaction)**: 听声音、嗅气味
3. **问诊 (Inquiry)**: 询问症状、病史、生活习惯等
4. **切诊 (Palpation)**: 脉诊、按诊等

#### 诊断结果字段
- **中医辨证 (TCMDiagnosis)**: 必填，中医诊断核心
- **治疗原则 (TreatmentPrinciple)**: 指导治疗方向
- **医嘱 (MedicalAdvice)**: 具体治疗建议

### 6) 中医特色
- **四诊合参**: 完整的中医四诊记录体系
- **辨证论治**: 中医诊断和治疗原则
- **纯数据记录**: 专注诊断数据存储，不涉及流程控制

---

## PrescriptionModel (src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs:1-140)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Prescriptions
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 处方管理
- **表名**: `Prescriptions`

### 2) 特性与注解
- `[Table("Prescriptions")]` - 指定数据库表名
- `[DisplayName("处方")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 处方唯一标识 |
| MedicalCaseId | Guid | 否 | [Required, DisplayName("医疗案例ID")] | 医疗案例ID |
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| UserId | Guid | 否 | [Required, DisplayName("关联用户ID")] | 关联用户ID(医生) |
| Indication | string | 是 | [StringLength(500), DisplayName("主治")] | 主治(适应症) |
| DosageCount | int | 否 | [DisplayName("处方帖数")] | 处方帖数(默认7) |
| Discount | decimal | 否 | [Column(TypeName = "decimal(3,2)"), DisplayName("折扣")] | 折扣(默认1.0) |
| Advice | string | 是 | [StringLength(500), DisplayName("医嘱")] | 医嘱 |
| FormulaSource | string | 是 | [StringLength(200), DisplayName("验方来源")] | 验方来源(自动填写) |
| Status | PrescriptionStatus | 否 | [DisplayName("处方状态")] | 处方状态(默认Draft) |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 4) 导航属性

| 属性名 | 类型 | 关系类型 | 说明 |
|--------|------|----------|------|
| Items | List<PrescriptionItemModel> | 一对多 | 处方项目(药材明细) |

### 5) 业务特点
- **验方集成**: 自动记录验方来源，支持多验方组合
- **价格计算**: 支持折扣机制，实际计算在DTO层处理
- **药材明细**: 通过Items集合管理处方中的具体药材
- **状态管理**: 从草稿到完成的完整状态流转

### 6) 处方状态流转
```
Draft → Confirmed → Dispensed → Completed
```

### 7) 默认值配置
- `DosageCount = 7` - 默认7帖
- `Discount = 1.0` - 默认无折扣
- `Status = PrescriptionStatus.Draft` - 默认草稿状态

---

## PrescriptionItemModel (src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItemModel.cs:1-100)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Prescriptions
- **基类**: (none)
- **实现接口**: IHerbItem
- **修饰符**: public
- **归属层角色**: Entity Model - 处方药材项

### 2) 特性与注解
- `[DisplayName("处方药材项")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 处方项唯一标识 |
| PrescriptionId | Guid | 否 | [Required, DisplayName("关联处方ID")] | 关联处方ID |
| HerbId | Guid | 否 | [Required, DisplayName("药材ID")] | 药材ID |
| HerbName | string | 否 | [Required, StringLength(100), DisplayName("药材名称")] | 药材名称 |
| Quantity | decimal | 否 | [Column(TypeName = "decimal(10,3)"), DisplayName("实际用量")] | 实际用量 |
| Unit | string | 否 | [StringLength(16), DisplayName("单位")] | 单位(默认"g") |
| UnitPrice | decimal | 否 | [Column(TypeName = "decimal(18,2)"), DisplayName("药材单价")] | 药材单价 |
| Amount | decimal | 否 | [NotMapped, DisplayName("小计金额")] | 小计金额(计算属性) |
| Usage | string | 是 | [StringLength(200), DisplayName("用法说明")] | 用法说明 |
| Remark | string | 是 | [StringLength(200), DisplayName("备注信息")] | 备注信息 |

### 4) 计算属性方法

#### Amount (小计金额计算属性)
- **源码位置**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItemModel.cs:65-70`
- **特性**: `[NotMapped]` - 不映射到数据库
- **计算逻辑**: `Amount = UnitPrice × Quantity`
- **返回类型**: `decimal`
- **计算公式**:
  ```csharp
  get => UnitPrice * Quantity;
  ```

### 5) 接口实现 (IHerbItem)
实现IHerbItem接口，支持多态处理：
- **HerbId**: 药材标识
- **HerbName**: 药材名称
- **Quantity**: 用量
- **Unit**: 单位
- **Usage**: 用法说明
- **Remark**: 备注信息

### 6) 业务特点
- **金额计算**: 自动计算小计金额
- **接口统一**: 通过IHerbItem接口与验方药材项统一处理
- **数据冗余**: HerbName冗余存储，避免关联查询影响性能

### 7) 默认值配置
- `Unit = "g"` - 默认单位为克

---

## HerbModel (src/Server/Core/LYBT.Entities/Herbs/HerbModel.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Herbs
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 中药材管理
- **表名**: `Herbs`

### 2) 特性与注解
- `[Table("Herbs")]` - 指定数据库表名
- `[DisplayName("中药材")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 药材唯一标识 |
| Name | string | 否 | [Required, StringLength(100), DisplayName("药材名称")] | 药材名称 |
| PinYinCode | string | 是 | [StringLength(50), DisplayName("拼音码")] | 拼音码 |
| Origin | string | 是 | [StringLength(100), DisplayName("产地")] | 产地 |
| Spec | string | 是 | [StringLength(100), DisplayName("规格")] | 规格 |
| Unit | string | 否 | [Required, StringLength(10), DisplayName("单位")] | 单位(默认"克") |
| Price | decimal | 否 | [Column(TypeName = "decimal(18,2)"), DisplayName("单价")] | 单价 |
| CostPrice | decimal | 是 | [Column(TypeName = "decimal(18,2)"), DisplayName("成本价")] | 成本价 |
| Effect | string | 是 | [StringLength(500), DisplayName("功效说明")] | 功效说明 |
| Usage | string | 是 | [StringLength(500), DisplayName("用法用量")] | 用法用量 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |
| Status | CommonStatus | 否 | [DisplayName("药材状态")] | 药材状态(默认Enabled) |

### 4) 业务定位
- **处方专用**: 仅用于处方开具，不含库存管理
- **价格计算**: 支持成本价和售价双轨制
- **中医属性**: 包含功效和用法用量说明
- **快速检索**: 通过拼音码支持快速搜索

### 5) 默认值配置
- `Unit = "克"` - 默认单位为克
- `Status = CommonStatus.Enabled` - 默认为启用状态

### 6) 中医药特色
- **功效说明**: 详细记录药材的中医功效
- **用法用量**: 标准的中医用法用量指导
- **产地规格**: 支持不同产地和规格的药材管理

---

## FormulaModel (src/Server/Core/LYBT.Entities/Formula/FormulaModel.cs:1-100)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Formula
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 验方管理
- **表名**: `Formulas`

### 2) 特性与注解
- `[Table("Formulas")]` - 指定数据库表名
- `[DisplayName("验方")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 验方唯一标识 |
| Name | string | 否 | [Required, StringLength(100), DisplayName("验方名称")] | 验方名称 |
| Effect | string | 是 | [StringLength(500), DisplayName("功效")] | 功效 |
| Usage | string | 是 | [StringLength(500), DisplayName("用法")] | 用法 |
| Property | string | 是 | [StringLength(200), DisplayName("性味归经")] | 性味归经 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |
| Status | CommonStatus | 否 | [DisplayName("验方状态")] | 验方状态(默认Enabled) |
| IsShared | bool | 否 | [DisplayName("是否共享")] | 是否共享(默认false) |

### 4) 导航属性

| 属性名 | 类型 | 关系类型 | 说明 |
|--------|------|----------|------|
| Herbs | List<FormulaHerbItem> | 一对多 | 药材组成列表 |

### 5) 业务特点
- **模板性质**: 作为处方模板，不含价格计算
- **共享机制**: 支持个人验方和共享验方
- **传统验方**: 支持经典验方和医生个人验方
- **中医理论**: 包含性味归经等中医理论属性

### 6) 验方分类
- **个人验方**: `IsShared = false` - 医生个人经验方
- **共享验方**: `IsShared = true` - 科室或医院共享方

### 7) 默认值配置
- `Status = CommonStatus.Enabled` - 默认为启用状态
- `IsShared = false` - 默认为个人验方

---

## FormulaHerbItem (src/Server/Core/LYBT.Entities/Formula/FormulaHerbItem.cs:1-80)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Formula
- **基类**: (none)
- **实现接口**: IHerbItem
- **修饰符**: public
- **归属层角色**: Entity Model - 验方药材项

### 2) 特性与注解
- `[DisplayName("验方药材项")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| HerbId | Guid | 否 | [DisplayName("药材ID")] | 药材ID(关联药材库) |
| HerbName | string | 否 | [Required, StringLength(100), DisplayName("药材名称")] | 药材名称 |
| Quantity | decimal | 否 | [DisplayName("剂量倍数")] | 剂量倍数(默认1) |
| Unit | string | 否 | [StringLength(16), DisplayName("单位")] | 单位(默认"g") |
| Usage | string | 是 | [StringLength(200), DisplayName("用法说明")] | 用法说明 |
| Remark | string | 是 | [StringLength(200), DisplayName("备注信息")] | 备注信息 |

### 4) 接口实现 (IHerbItem)
实现IHerbItem接口，与处方药材项统一处理：
- **HerbId**: 药材标识
- **HerbName**: 药材名称
- **Quantity**: 剂量倍数
- **Unit**: 单位
- **Usage**: 用法说明
- **Remark**: 备注信息

### 5) 设计理念
- **倍数概念**: 使用剂量倍数而非具体用量，提供配方灵活性
- **模板属性**: 实际用量 = 药材规格 × 剂量倍数
- **接口统一**: 通过IHerbItem接口与处方药材项统一处理

### 6) 默认值配置
- `Quantity = 1` - 默认剂量倍数为1
- `Unit = "g"` - 默认单位为克

---

## AdminSecretModel (src/Server/Core/LYBT.Entities/Users/AdminSecretModel.cs:1-50)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Users
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 安全认证
- **表名**: `AdminSecrets`

### 2) 特性与注解
- `[Table("AdminSecrets")]` - 指定数据库表名
- `[DisplayName("管理员密码")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 主键 |
| Username | string | 否 | [Required, StringLength(50), DisplayName("管理员用户名")] | 管理员用户名 |
| PasswordHash | string | 否 | [Required, StringLength(256), DisplayName("密码哈希")] | 密码哈希 |

### 4) 安全设计
- **独立存储**: 管理员密码单独存储，防止Users表被篡改
- **双重保护**: 结合Users表形成双重密码验证机制
- **最小化数据**: 只存储必要的认证信息

---

## AuthSessionModel (src/Server/Core/LYBT.Entities/Auth/AuthSessionModel.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Entities.Auth
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Entity Model - 会话管理
- **表名**: `AuthSessions`

### 2) 特性与注解
- `[Table("AuthSessions")]` - 指定数据库表名
- `[DisplayName("认证会话")]` - 中文显示名称

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 会话ID |
| UserId | Guid | 否 | [Required, DisplayName("用户ID")] | 用户ID |
| TokenHash | string | 否 | [Required, StringLength(256), DisplayName("会话令牌哈希")] | 会话令牌哈希 |
| LoginTime | DateTime | 否 | [DisplayName("登录时间")] | 登录时间(默认Now) |
| LogoutTime | DateTime | 是 | [DisplayName("登出时间")] | 登出时间 |
| ExpiryTime | DateTime | 否 | [DisplayName("过期时间")] | 过期时间 |
| IpAddress | string | 否 | [Required, StringLength(45), DisplayName("IP地址")] | IP地址 |
| UserAgent | string | 是 | [StringLength(500), DisplayName("用户代理")] | 用户代理 |
| IsRevoked | bool | 否 | [DisplayName("是否已撤销")] | 是否已撤销(默认false) |
| Status | CommonStatus | 否 | [DisplayName("状态")] | 状态(默认Enabled) |

### 4) 会话管理特点
- **安全追踪**: 完整的登录会话生命周期管理
- **令牌管理**: 支持令牌撤销和过期控制
- **审计日志**: IP地址和用户代理跟踪
- **会话安全**: 支持强制下线和会话撤销

### 5) 默认值配置
- `LoginTime = DateTime.Now` - 登录时间默认当前时间
- `IsRevoked = false` - 默认未撤销
- `Status = CommonStatus.Enabled` - 默认启用状态

---

## 枚举类型详细分析

### UserRole (用户角色枚举)
**文件路径**: `LYBT.Shared.Models.Enums/UserRole.cs`

```csharp
public enum UserRole
{
    [Description("管理员")]
    Admin = 0,
    
    [Description("医生")]
    Doctor = 1
}
```

### Gender (性别枚举)
**文件路径**: `LYBT.Shared.Models.Enums/Gender.cs`

```csharp
public enum Gender
{
    [Description("未知")]
    Unknown = 0,
    
    [Description("男性")]
    Male = 1,
    
    [Description("女性")]
    Female = 2
}
```

### CommonStatus (通用状态枚举)
**文件路径**: `LYBT.Shared.Models.Enums/CommonStatus.cs`

```csharp
public enum CommonStatus
{
    [Description("禁用")]
    Disabled = 0,
    
    [Description("启用")]
    Enabled = 1
}
```

### MedicalCaseStatus (医案状态枚举)
**文件路径**: `LYBT.Shared.Models.Enums/MedicalCaseStatus.cs`

```csharp
public enum MedicalCaseStatus
{
    [Description("挂号完成")]
    Registered = 0,
    
    [Description("诊疗中")]
    InProgress = 1,
    
    [Description("诊疗完成")]
    Completed = 2,
    
    [Description("已取消")]
    Cancelled = 3
}
```

### PrescriptionStatus (处方状态枚举)
**文件路径**: `LYBT.Shared.Models.Enums/PrescriptionStatus.cs`

```csharp
public enum PrescriptionStatus
{
    [Description("草稿")]
    Draft = 0,
    
    [Description("已确认")]
    Confirmed = 1,
    
    [Description("已配药")]
    Dispensed = 2,
    
    [Description("已完成")]
    Completed = 3
}
```

### SensitiveDataType (敏感数据类型枚举)
**文件路径**: `LYBT.Entities.Attributes/SensitiveDataType.cs`

```csharp
public enum SensitiveDataType
{
    PersonalInfo,   // 个人信息
    MedicalInfo,    // 医疗信息
    ContactInfo,    // 联系信息
    IdentityInfo,   // 身份信息
    FinancialInfo   // 财务信息
}
```

---

## 实体关系分析

### 核心关系图
```
Patient (1) ←→ (n) MedicalCase (1) ←→ (1) Consultation
                      ↓
                 (0..1) Prescription (1) ←→ (n) PrescriptionItem
                                                      ↓
                                               (1) Herb ←→ (n) FormulaHerbItem
                                                              ↓
                                                         (1) Formula

User (Doctor) (1) ←→ (n) MedicalCase
User (Doctor) (1) ←→ (n) Consultation  
User (Doctor) (1) ←→ (n) Prescription

User (Admin) (1) ←→ (1) AdminSecretModel
User (1) ←→ (n) AuthSession
```

### 关系特点
- **聚合根模式**: MedicalCase作为诊疗流程聚合根
- **1:1核心关系**: MedicalCase与Consultation一对一关系
- **可选处方**: 支持仅诊断不开方的灵活业务场景
- **接口统一**: IHerbItem统一处方项和验方项的处理

---

## 全局统计

### 实体统计
- **核心实体数量**: 12个主要业务实体
- **枚举类型数量**: 6个业务枚举
- **关系数量**: 15个主要关系映射
- **敏感数据字段**: 4个字段使用敏感数据保护

### 架构特点
- **UltraThink v2.0简化**: 实体设计简洁高效
- **中医诊疗特色**: 完整的中医四诊和验方体系
- **数据安全保障**: Epic 05-P0-03要求的敏感数据保护
- **关系设计合理**: 聚合根模式和1:1核心关系
- **扩展性考虑**: Guid主键和状态机设计

### 业务覆盖
- ✅ 用户认证：User + AdminSecret + AuthSession
- ✅ 患者管理：Patient (含敏感数据保护)
- ✅ 诊疗流程：MedicalCase + Consultation (1:1关系)
- ✅ 处方管理：Prescription + PrescriptionItem
- ✅ 药材管理：Herb (处方专用，无库存)
- ✅ 验方管理：Formula + FormulaHerbItem
- ✅ 接口统一：IHerbItem多态处理

### 设计原则体现
- **单一职责**：每个实体专注特定业务概念
- **开闭原则**：通过枚举和接口支持扩展
- **里氏替换**：IHerbItem接口的多态实现
- **合成复用**：通过组合关系而非继承实现复用
- **最少知识**：实体间通过ID关联，减少耦合