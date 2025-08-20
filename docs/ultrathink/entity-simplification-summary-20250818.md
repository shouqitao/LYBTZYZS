# UltraThink v2.0 实体简化总结文档

## 📋 重构概述

**重构时间**: 2025-08-18  
**重构版本**: UltraThink v2.0  
**重构目标**: 简化实体结构，删除冗余字段，优化20人以下诊所的实际使用需求

## 🎯 简化原则

1. **删除时间字段**: CreateTime、UpdateTime等改为日志记录
2. **删除计算属性**: Age、TotalPrice等改为DTO中计算
3. **删除审计字段**: CreatedBy、UpdatedBy等改为日志记录
4. **删除冗余业务字段**: 根据实际业务需求删除不必要字段
5. **保留核心业务字段**: 确保核心功能不受影响

## 🏗️ 实体简化结果

### 1. User 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Users/UserModel.cs`

**删除字段**:
- `CreateTime` - 创建时间（改用日志）
- `LastLoginTime` - 最后登录时间（改用日志）
- `UpdateTime` - 更新时间（改用日志）
- `FailedLoginCount` - 失败登录次数（改用日志）
- `LockoutEnd` - 锁定结束时间（改用日志）
- `Specialty` - 专长（医生专用字段）
- `RegistrationFee` - 挂号费（医生专用字段）
- `LicenseNumber` - 执业证书号（医生专用字段）
- `Introduction` - 简介（医生专用字段）

**保留字段** (8个):
- `Id`, `Username`, `RealName`, `PinYinCode`
- `PhoneNumber`, `Email`, `Role`, `Status`
- `PasswordHash`, `Remark`

### 2. AuthSession 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Auth/AuthSessionModel.cs`

**架构变更**: 简化Auth模块为单一AuthSession实体，删除LoginAttempt和SecurityLog

**保留字段** (11个):
- `Id`, `UserId`, `TokenHash`, `LoginTime`, `LogoutTime`
- `ExpiryTime`, `IpAddress`, `UserAgent`, `IsRevoked`, `Status`

### 3. Patient 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Patients/PatientModel.cs`

**删除字段**:
- `Age` - 年龄（计算值，改为DTO中计算）
- `CreateTime` - 创建时间（改用日志）
- `UpdateTime` - 更新时间（改用日志）
- `LastVisitTime` - 最后就诊时间（改用日志）
- `VisitCount` - 就诊次数（改用日志统计）
- `DisableReason` - 禁用原因（改用日志）
- `CreatedBy` - 创建者ID（改用日志）
- `UpdatedBy` - 更新者ID（改用日志）

**保留字段** (11个):
- `Id`, `Name`, `PinYinCode`, `Gender`, `BirthDate`
- `IdType`, `IdNumber`, `PhoneNumber`, `Address`, `AllergyHistory`, `Status`

### 4. Herb 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Herbs/HerbModel.cs`

**删除字段**:
- `CostPrice` - 成本价（20人诊所只需零售价）

**保留字段** (12个):
- `Id`, `Name`, `PinYinCode`, `Origin`, `Spec`, `Unit`
- `Price`, `Effect`, `Usage`, `Remark`, `Status`

### 5. Formula 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Formula/FormulaModel.cs`

**删除字段**:
- `CreateTime` - 创建时间（改用日志）
- `UpdateTime` - 更新时间（改用日志）
- `CreatedById` - 创建者ID（改用日志）

**保留字段** (8个):
- `Id`, `Name`, `Effect`, `Usage`, `Property`, `Remark`
- `Status`, `IsShared`, `Herbs`

### 6. Prescription 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`

**删除字段**:
- `CreateTime` - 创建时间（改用日志）
- `UpdateTime` - 更新时间（改用日志）
- `SingleDosePrice` - 单帖价格（计算值）
- `TotalPrice` - 处方总价（计算值）
- `TotalWeight` - 处方总重量（计算值）
- `DuplicateWarning` - 重复药材提醒（UI层处理）
- `MissingDrugWarning` - 缺药提醒（当前不涉及库存）

**新增字段**:
- `MedicalCaseId` - 医疗案例ID（关联字段）
- `Discount` - 折扣（医生可打折）

**字段名变更**:
- `Diagnosis` → `Indication` - 主治（更贴切的中医用词）

**保留字段** (12个):
- `Id`, `MedicalCaseId`, `PatientId`, `UserId`, `Indication`
- `DosageCount`, `Discount`, `Advice`, `FormulaSource`
- `Status`, `Remark`, `Items`

### 7. Consultation 实体
**文件位置**: `src/Server/Core/LYBT.Entities/Consultation/ConsultationModel.cs`

**删除字段**:
- `CreateTime` - 创建时间（改用日志）
- `UpdateTime` - 更新时间（改用日志）
- `PastHistory` - 既往史（不必要）
- `AllergyHistory` - 过敏史（Patient中已有）
- `PhysicalExamination` - 体格检查（西医项目）
- `TongueInspection` - 舌诊（合并到切诊）
- `PulseCondition` - 脉诊（合并到切诊）
- `Temperature` - 体温（西医项目）
- `SystolicPressure` - 收缩压（西医项目）
- `DiastolicPressure` - 舒张压（西医项目）
- `HeartRate` - 心率（西医项目）
- `RespiratoryRate` - 呼吸频率（西医项目）
- `WesternDiagnosis` - 西医诊断（西医项目）
- `Diagnosis` - 综合诊断（保留TCMDiagnosis）
- `DiagnosisCatalogId` - 诊断分类ID（过于复杂）
- `ConsultationTime` - 看诊时间（改用日志）
- `Duration` - 看诊时长（改用日志）

**字段调整**:
- `Palpation` - 切诊（包含脉诊、舌诊等）

**保留字段** (12个):
- `Id`, `MedicalCaseId`, `PatientId`, `UserId`
- `ChiefComplaint`, `PresentIllness`
- `Inspection`, `AuscultationOlfaction`, `Inquiry`, `Palpation`
- `TCMDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`
- `Status`, `Remark`

### 8. MedicalCase 实体
**文件位置**: `src/Server/Core/LYBT.Entities/MedicalCase/MedicalCaseModel.cs`

**删除字段**:
- `CreateTime` - 创建时间（保留ConsultationDate）
- `UpdateTime` - 更新时间（改用日志）
- `CompleteTime` - 完成时间（改用日志）
- `IsActive` - 是否有效（用Status管理）

**新增字段**:
- `ConsultationDate` - 看诊时间（医案核心时间信息）

**保留字段** (10个):
- `Id`, `PatientId`, `PatientName`, `DoctorId`, `DoctorName`
- `ConsultationId`, `PrescriptionId`, `ConsultationDate`
- `Status`, `Remark`

## 📊 简化统计

| 实体 | 简化前字段数 | 简化后字段数 | 删除字段数 | 简化率 |
|------|-------------|-------------|-----------|--------|
| User | 20+ | 8 | 12+ | 60%+ |
| AuthSession | 1个实体 | 11 | 2个实体删除 | 大幅简化 |
| Patient | 15+ | 11 | 8+ | 47%+ |
| Herb | 13 | 12 | 1 | 8% |
| Formula | 11 | 8 | 3 | 27% |
| Prescription | 14 | 12 | 7-5=2净增 | 功能增强 |
| Consultation | 30+ | 12 | 18+ | 60%+ |
| MedicalCase | 12 | 10 | 3-1=2净减 | 17% |

**总体简化效果**:
- 平均简化率: 约40%
- 删除冗余字段: 50+个
- 新增必要字段: 3个
- 架构更清晰，更适合实际业务需求

## 🎯 简化收益

### 1. 性能提升
- 减少数据库字段读写
- 减少内存占用
- 提升查询性能

### 2. 维护性提升
- 代码更清晰
- 职责分离更明确
- 业务逻辑更集中

### 3. 适用性提升
- 更符合20人以下诊所需求
- 删除不必要的复杂功能
- 专注核心中医诊疗业务

### 4. 一致性提升
- 时间管理统一通过日志
- 计算属性统一在DTO层
- 审计信息统一通过日志

## 🔄 后续工作

1. **DTO层对齐**: 确保DTO与简化的实体结构一致
2. **Repository层更新**: 调整数据访问层匹配新结构
3. **Service层调整**: 迁移业务逻辑到合适位置
4. **前端层重构**: 更新前端所有层次匹配新架构
5. **数据库迁移**: 生成并执行EF Core迁移

## 📝 注意事项

1. **数据迁移**: 需要小心处理现有数据的迁移
2. **依赖关系**: 注意更新所有引用这些字段的代码
3. **测试验证**: 确保简化后功能正常
4. **文档同步**: 更新相关技术文档

---

**文档版本**: v1.0  
**创建时间**: 2025-08-18  
**最后更新**: 2025-08-18  
**维护人员**: UltraThink 架构组