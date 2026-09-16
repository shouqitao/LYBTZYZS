# LYBT.Entities - Server Domain Entities

**Purpose**: Core domain entities organized by business aggregate (DDD-style folders).

## Structure

```
LYBT.Entities/
├── Auth/                # AuthSession（弱实体）, SecurityAuditLog
├── Common/              # BaseEntity, IAuditableEntity, ISoftDeletable, SystemLog
├── Consultations/       # Consultation（继承 BaseEntity）
├── Formulas/            # Formula（继承 BaseEntity）, FormulaHerbItem（弱实体）
├── Herbs/               # Herb（继承 BaseEntity）
├── MedicalCases/        # MedicalCase（聚合根, 继承 BaseEntity）, MedicalCaseTime, MedicalCasePrintLog, MedicalCaseAuditLog
├── Patients/            # Patient（继承 BaseEntity）
├── Prescriptions/       # Prescription（继承 BaseEntity）, PrescriptionItem（弱实体）
├── Registrations/       # Registration（继承 BaseEntity）
└── Users/               # ApplicationUser（Identity 实体，手抄审计字段）
```

> 注：枚举定义在 `LYBT.Shared.Models/Enums/`，不在本项目内。文件名保留 `*Model.cs` 历史命名，类名已简化（如 `PatientModel.cs` → class `Patient`）。

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Base entity | `Common/BaseEntity.cs` | Id, CreatedAt, UpdatedAt, IsDeleted |
| Audit interface | `Common/IAuditableEntity.cs` | CreatedAt/UpdatedAt/CreatedBy/UpdatedBy |
| Aggregate root | `MedicalCases/MedicalCaseModel.cs` | class `MedicalCase`，含 Consultation + Prescription 内部实体 |
| Identity user | `Users/ApplicationUser.cs` | 继承 IdentityUser<Guid>，手抄审计/软删除字段 |
| Domain methods | `MedicalCases/MedicalCaseModel.cs` | Complete(), SaveAsDraft(), SoftDelete() |

## CONVENTIONS

- **Aggregate root** — MedicalCase 是唯一 DDD 聚合根；Consultation + Prescription 是内部实体
- **Base entity** — 业务实体继承 BaseEntity（Id, 审计字段, 软删除）
- **Anemic model** — 实体以数据容器为主；领域逻辑集中在 MedicalCase
- **No cross-entity references** — 实体间用 Guid 引用，导航属性在 DbContext 配置

### 子实体基类策略（有意设计，非不一致）

| 类别 | 实体 | 基类 | 审计字段 | 理由 |
|------|------|------|----------|------|
| **弱实体** | FormulaHerbItem, PrescriptionItem, AuthSession | 无（不继承 BaseEntity） | 无 | 从属实体，生命周期由父聚合管理，无需独立审计 |
| **审计日志** | MedicalCasePrintLog, MedicalCaseAuditLog | BaseEntity | 有 | 独立审计记录，需 CreatedAt 追溯 |
| **Identity 特例** | ApplicationUser | IdentityUser<Guid> | 手抄 | 无法继承 BaseEntity，手抄 IAuditableEntity + ISoftDeletable 字段，与 BaseEntity 同步维护 |

## ANTI-PATTERNS

- **Navigation property abuse** — Prefer Guid references; let DbContext handle joins
- **Domain logic in entities** — Only MedicalCase has domain methods; keep others anemic
- **Missing soft-delete** — 资源类实体（User/Herb/Formula/Patient）必须有 IsDeleted
- **给弱实体加审计字段** — FormulaHerbItem/PrescriptionItem/AuthSession 是有意不继承 BaseEntity 的弱实体，不要"顺手"补审计字段
