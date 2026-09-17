# 数据模型（视图层速查）
> 版本: v1.2 | 日期: 2026-09-17

> **用户速览**：核心实体索引 + 状态枚举指针，**不是权威定义**。
>
> **权威定义**：实体字段/状态机/计算属性等详细定义见 [04-data-model.md](04-data-model.md)（SSOT，见 [02-ssot-architecture.md](../00-governance/02-ssot-architecture.md)）。
>
> 由 [13-project-master-plan.md §二](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03）。

## 2.1 核心实体索引（Shared/LYBT.Entities）

| 实体 | 表名 | 权威文档 |
|------|------|---------|
| **ApplicationUser** | Users (Identity) | [04-data-model.md](04-data-model.md) §实体定义 |
| **Patient** | Patients | [04-data-model.md](04-data-model.md) §实体定义 |
| **MedicalCase** | MedicalCases | [04-data-model.md](04-data-model.md) §实体定义（聚合根） |
| **Consultation** | Consultations | [04-data-model.md](04-data-model.md) §实体定义（MedicalCase 内部） |
| **Prescription** | Prescriptions | [04-data-model.md](04-data-model.md) §实体定义（MedicalCase 内部） |
| **PrescriptionItem** | PrescriptionItems | [04-data-model.md](04-data-model.md) §实体定义（Prescription 内部） |
| **Herb** | Herbs | [04-data-model.md](04-data-model.md) §实体定义 |
| **Formula** | Formulas | [04-data-model.md](04-data-model.md) §实体定义 |
| **FormulaHerbItem** | FormulaHerbItems | [04-data-model.md](04-data-model.md) §实体定义（Formula 内部） |
| **Registration** | Registrations | [04-data-model.md](04-data-model.md) §实体定义 |
| **AuthSession** | AuthSessions | [04-data-model.md](04-data-model.md) §辅助实体概览 |
| **SecurityAuditLog** | SecurityAuditLogs | [04-data-model.md](04-data-model.md) §辅助实体概览 |
| **MedicalCaseAuditLog** | MedicalCaseAuditLogs | [04-data-model.md](04-data-model.md) §辅助实体概览 |
| **MedicalCasePrintLog** | MedicalCasePrintLogs | [04-data-model.md](04-data-model.md) §辅助实体概览 |
| **SystemLog** | SystemLogs | 已标记死代码，待删除 |

> 字段定义、关系与索引一律以 [04-data-model.md](04-data-model.md) 为准；本文件不再维护字段级清单（避免与代码漂移）。

## 2.2 状态枚举指针

状态枚举的权威定义见 [04-data-model.md §枚举定义](04-data-model.md#枚举定义)。速查指针：

| 枚举 | 权威文档 |
|------|---------|
| **MedicalCaseStatus** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **RegistrationStatus** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **RegistrationSource** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **CommonStatus** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **UserRole** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **FormulaValidationStatus** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **FormulaType** | [04-data-model.md](04-data-model.md) §枚举定义 |
| **Gender** | [04-data-model.md](04-data-model.md) §枚举定义 |

> **补充**：取消医案 = 物理删除（无 `Cancelled` 状态），详见 [02-requirements/07-medical-cases.md](../02-requirements/07-medical-cases.md) BR-000。
