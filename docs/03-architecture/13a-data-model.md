# LYBTZYZS 数据模型

> 由 [13-project-master-plan.md §二](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03），内容原样迁移，不改变定义。

## 2.1 核心实体（Shared/LYBT.Entities）

| 实体 | 表名 | 关键字段 | 关系 |
|------|------|---------|------|
| **ApplicationUser** | Users (Identity) | RealName, PinYinCode, Role(UserRole), IsSysAdmin, Status, MustChangeOnNextLogin, RegistrationFee | IdentityUser<Guid> |
| **Patient** | Patients | Name, PinYinCode, Gender, BirthDate, IdNumber(加密), PhoneNumber(加密), Status | — |
| **MedicalCase** | MedicalCases | PatientId, UserId(Doctor), CaseNumber, CaseStatus, NeedsPrescription, IsPrinted, PrintCount | 聚合根 |
| **Consultation** | Consultations | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis | 1:1 MedicalCase |
| **Prescription** | Prescriptions | MedicalCaseId, PrescriptionNumber, DosageCount, Discount, Usage, Advice, ReferencedFormulas | 1:0..1 MedicalCase |
| **PrescriptionItem** | PrescriptionItems | PrescriptionId, HerbId, HerbName, Quantity, UnitPrice, Unit, Role(君臣佐使) | 1:N Prescription |
| **Herb** | Herbs | Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price, CostPrice, Effect, Usage | — |
| **Formula** | Formulas | Name, Effect, Indication, Usage, Status, IsShared, ValidationStatus, Category, FormulaType | — |
| **FormulaHerbItem** | FormulaHerbItems | FormulaId, HerbId, HerbName, Quantity, Unit, Role | 1:N Formula |
| **Registration** | Registrations | PatientId, DoctorId, MedicalCaseId, Source(前台/医生), Status, QueueNumber, RegistrationFee | — |
| **AuthSession** | AuthSessions | UserId, RefreshToken, TokenFamily | — |
| **SecurityAuditLog** | SecurityAuditLogs | 事件类型、用户、IP、时间 | — |
| **MedicalCaseAuditLog** | MedicalCaseAuditLogs | 医案ID、操作、操作人 | — |
| **MedicalCasePrintLog** | MedicalCasePrintLogs | 医案ID、打印类型、打印机、操作人 | — |
| **SystemLog** | SystemLogs | 系统日志（已标记死代码，待删除） | — |

## 2.2 状态枚举

| 枚举 | 值 | 用途 |
|------|-----|------|
| **MedicalCaseStatus** | Active/Completed/Suspended/Cancelled | 医案生命周期 |
| **RegistrationStatus** | Waiting/InProgress/Completed/Cancelled | 挂号生命周期 |
| **RegistrationSource** | Receptionist/Doctor | 挂号来源 |
| **CommonStatus** | Enabled/Disabled | 通用启用/禁用 |
| **UserRole** | SuperAdmin(100)/Admin(10)/Doctor(1)/Receptionist(0) | 角色 |
| **FormulaValidationStatus** | Draft/Validated | 验方验证状态 |
| **FormulaType** | Classic/Experience | 经典方/经验方 |
| **Gender** | Male/Female/Unknown | 性别 |
