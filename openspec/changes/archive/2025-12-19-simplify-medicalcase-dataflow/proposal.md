# OpenSpec Proposal: simplify-medicalcase-dataflow

## Metadata
- **Status**: proposed
- **Created**: 2025-12-19
- **Parent Epic**: #1961 (DTO Architecture Standardization)
- **Depends On**: unify-medicalcase-input-dto (已完成MedicalCaseInputDto简化)

## Problem Statement

当前医案模块仍存在DTO分裂和数据流复杂的问题:

### 1. DTO双轨制
- `MedicalCaseInputDto`: 用于创建医案(仅基本字段)
- `MedicalCaseAggregateInputDto`: 用于聚合保存(包含Consultation+Prescription)

两种DTO职责不清晰，增加了API理解成本和维护复杂度。

### 2. 数据流不统一
- 创建: MedicalCaseInputDto → CreateAsync
- 更新: MedicalCaseAggregateInputDto → SaveAggregateAsync
- 查询: 返回MedicalCaseDetailDto

创建和更新使用不同DTO，破坏了CRUD操作的一致性。

### 3. 聚合根语义不明确
医案作为聚合根(Aggregate Root)，应该统一管理Consultation和Prescription。当前设计将"创建空壳"和"填充内容"分为两个独立操作，违背DDD聚合根原则。

## Proposed Solution

### 核心思想: 统一DTO，简化数据流

**医案 = 诊断(Consultation) + 处方(Prescription)**

统一使用一个`MedicalCaseInputDto`处理所有写入场景(Create/Update)。

### 新的MedicalCaseInputDto设计

```csharp
/// <summary>
/// 医案输入DTO - 统一创建和更新
/// OpenSpec: simplify-medicalcase-dataflow
/// </summary>
public class MedicalCaseInputDto
{
    /// <summary>
    /// 医案ID - 创建时为null，更新时必填
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 患者ID - 必填
    /// </summary>
    public required Guid PatientId { get; set; }

    /// <summary>
    /// 医生ID - 可选，默认当前用户
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// 就诊日期 - 可选，默认当前时间
    /// </summary>
    public DateTime? VisitDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 诊断信息 - 可选，创建时可不填，更新时填写
    /// </summary>
    public ConsultationInputDto? Consultation { get; set; }

    /// <summary>
    /// 处方信息 - 可选，仅需要处方时填写
    /// </summary>
    public PrescriptionInputDto? Prescription { get; set; }
}
```

### DTO命名规范统一

| 用途 | DTO名称 | 说明 |
|------|---------|------|
| 写入(Create/Update) | `XxxInputDto` | 统一的输入DTO |
| 列表查询 | `XxxListDto` | 轻量级列表DTO |
| 详情查询 | `XxxDetailDto` | 完整详情DTO |

### API契约统一

```csharp
// 创建医案 - 使用MedicalCaseInputDto
POST /api/v1/medicalcases
Body: MedicalCaseInputDto { PatientId, DoctorId?, VisitDate?, Consultation?, Prescription? }

// 更新医案 - 使用相同的MedicalCaseInputDto
PUT /api/v1/medicalcases/{id}
Body: MedicalCaseInputDto { Id, PatientId, ..., Consultation?, Prescription? }

// 查询医案详情
GET /api/v1/medicalcases/{id}
Response: MedicalCaseDetailDto
```

### 删除的DTO

- `MedicalCaseAggregateInputDto` - 合并到MedicalCaseInputDto
- `PrescriptionAggregateInputDto` - 简化为PrescriptionInputDto

## Implementation Phases

### Phase 1: DTO重构
1. 扩展MedicalCaseInputDto，添加Consultation和Prescription字段
2. 删除MedicalCaseAggregateInputDto
3. 简化PrescriptionInputDto

### Phase 2: Server端重构
1. 统一MedicalCaseService的Create/Update逻辑
2. 删除SaveAggregateAsync，合并到SaveAsync
3. 更新Controller端点

### Phase 3: Client端适配
1. 更新MedicalCaseRepository调用
2. 简化MedicalCaseDataManager逻辑
3. 更新ViewModel数据流

### Phase 4: 测试与验证
1. 更新单元测试
2. 更新集成测试
3. 功能验证

## Impact Analysis

### Files to Modify
1. `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs`
2. `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionInputDto.cs`
3. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
4. `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
5. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/` - 多个文件

### Files to Delete
1. `MedicalCaseAggregateInputDto.cs`
2. `PrescriptionAggregateInputDto.cs`

### Breaking Changes
- `MedicalCaseAggregateInputDto` 移除
- `SaveAggregateAsync` API 移除
- Client需要迁移到新的统一API

## Success Criteria

1. [ ] MedicalCaseInputDto包含Consultation和Prescription字段
2. [ ] 删除MedicalCaseAggregateInputDto
3. [ ] 删除PrescriptionAggregateInputDto
4. [ ] Server端统一Create/Update逻辑
5. [ ] Client端使用统一DTO
6. [ ] 编译通过，0错误0警告
7. [ ] 所有测试通过
8. [ ] 功能正常(创建、编辑、保存医案)

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| 破坏现有功能 | 分Phase执行，每步验证 |
| API兼容性 | 保持端点URL不变，仅改变DTO结构 |
| 测试覆盖不足 | 优先修复测试文件 |

## Entity Field Optimization (2025-12-19)

本次重构同时完善三实体(MedicalCase/Consultation/Prescription)的字段定义。

### 决策记录

| 问题 | 决策 | 理由 |
|------|------|------|
| ConsultationDate字段 | **删除** | 用BaseEntity.CreatedAt代替，创建时间=就诊时间 |
| DoctorId命名 | 重命名为`UserId` | Doctor就是User，语义统一 |
| CaseNumber | **新增** | 业务编号(如MC20251219001)，与PrescriptionNumber对应 |
| CompletedAt | **新增** | 完成时间戳，用于锁定逻辑判断 |
| Prescription.Indication | 删除 | 打印时从Consultation.TCMDiagnosis获取，经验方有独立Indication |
| FormulaSource vs ReferencedFormulas | 删除FormulaSource | 功能重复，保留命名更规范的ReferencedFormulas |
| Prescription.Usage | 新增 | 处方用法("每日一剂，水煎服")与医嘱(Advice)职责分离 |
| Patient/User导航属性 | 不添加 | DDD原则：跨聚合仅用ID引用，不用导航属性 |

### 最终实体字段结构

#### MedicalCase (聚合根)
```csharp
public class MedicalCase : BaseEntity
{
    // ========== 跨聚合引用 (仅ID，符合DDD原则) ==========
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }  // 冗余-读优化
    public Guid UserId { get; set; }         // 重命名自DoctorId
    public string DoctorName { get; set; }   // 冗余-读优化

    // ========== 业务字段 ==========
    public string? CaseNumber { get; set; }  // 新增：业务编号
    public MedicalCaseStatus CaseStatus { get; set; }
    public bool? NeedsPrescription { get; set; }
    public DateTime? CompletedAt { get; set; }  // 新增：完成时间
    public string? Remark { get; set; }
    // ConsultationDate 已删除，用CreatedAt代替

    // ========== 同聚合导航属性 ==========
    public virtual Consultation? Consultation { get; set; }  // 1:1
    public virtual Prescription? Prescription { get; set; }  // 1:0..1

    // ========== 计算属性 ==========
    public bool IsLocked => CompletedAt.HasValue || CreatedAt.Date < DateTime.Today;
}
```

#### Consultation (诊断 - 共享主键)
```csharp
public class Consultation : BaseEntity
{
    // 诊断四要素 (已确定)
    public string? PresentIllness { get; set; }   // 现病史
    public string? TongueDiagnosis { get; set; }  // 舌诊
    public string? PulseDiagnosis { get; set; }   // 脉诊
    public string? TCMDiagnosis { get; set; }     // 中医辨证

    // 导航属性
    public virtual MedicalCase MedicalCase { get; set; }
}
```

#### Prescription (处方 - 外键关联)
```csharp
public class Prescription : BaseEntity
{
    public Guid MedicalCaseId { get; set; }
    public string? PrescriptionNumber { get; set; }
    public int DosageCount { get; set; } = 7;
    public decimal Discount { get; set; } = 1.0m;

    // 用法医嘱
    public string? Usage { get; set; }    // 新增：处方用法
    public string? Advice { get; set; }   // 医嘱

    // 验方引用
    public string? ReferencedFormulas { get; set; }  // 保留
    // FormulaSource 已删除

    // 其他
    public string? Remark { get; set; }

    // 打印管理
    public int PrintVersion { get; set; }
    public DateTime? LastPrintedAt { get; set; }
    public int PrintCount { get; set; }
    public bool IsPrinted { get; set; }

    // 导航属性
    public virtual MedicalCase? MedicalCase { get; set; }
    public virtual ICollection<PrescriptionItem> Items { get; set; }
}
```

### 数据库迁移

需要创建迁移处理以下变更：
1. 删除 `MedicalCases.ConsultationDate` 列 (用CreatedAt代替)
2. 新增 `MedicalCases.CaseNumber` 列 (StringLength 50)
3. 新增 `MedicalCases.CompletedAt` 列 (DateTime?)
4. 重命名 `MedicalCases.DoctorId` → `MedicalCases.UserId`
5. 删除 `Prescriptions.Indication` 列
6. 删除 `Prescriptions.FormulaSource` 列
7. 新增 `Prescriptions.Usage` 列 (StringLength 500)

### 三层字段一致性分析

参考Consultation诊断字段重构经验，对MedicalCase和Prescription进行三层字段一致性检查。

**发现的问题**：

| 层 | MedicalCase问题 | Prescription问题 |
|----|-----------------|------------------|
| Entity→DTO | CaseNumber/CompletedAt在DTO有但Entity无 | Usage在DTO有但Entity无 |
| DTO命名 | InputDto用VisitDate，其他用ConsultationDate | Diagnosis字段Entity无 |
| DTO→Client | Client的MedicalCaseItem有CompletedAt但DTO无 | FormulaSource冗余 |

**修复方案**：

1. **Entity新增字段** → 数据库迁移（Task 0.1-0.3）
2. **DTO字段同步** → 统一命名，删除冗余（Task 0.4）
3. **Client字段同步** → 与DTO对齐（Task 0.7）

## Permission Logic Unification (2025-12-19)

### 问题：权限逻辑散落多处

当前CanEdit/权限判断散落在4处，规则不一致：

| 位置 | 判断标准 | 调用者 |
|------|----------|--------|
| `MedicalCaseModel.CanEdit()` | 时间: `CreatedAt.Date == DateTime.Today` | 无(死代码) |
| `MedicalCaseModel.IsLocked` | 时间: `CreatedAt.Date < DateTime.Today` | Rules |
| `MedicalCaseRules.CanEdit()` | 状态: `Draft \|\| Active` | CommandService |
| `MedicalCasePermissionService.CanEdit()` | 状态: `Draft \|\| Active` | AuthorizationHandler |

**核心问题**：
1. Entity.CanEdit基于**时间**，Rules/Service基于**状态**，两种逻辑矛盾
2. MedicalCaseRules和PermissionService的CanEdit逻辑重复
3. Entity.CanEdit()方法从未被调用，成为死代码

### 统一设计方案

**原则**：实体提供状态属性，服务判断权限

```
┌─────────────────────────────────────────────────────────────┐
│                      MedicalCase Entity                      │
│  只有状态计算属性:                                            │
│  - IsLocked: CompletedAt.HasValue || CreatedAt.Date < Today │
│  - IsActive: CaseStatus == Draft || Active                  │
│  - IsCompleted: CaseStatus == Completed                     │
│  ❌ 删除 CanEdit() 方法                                      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            MedicalCasePermissionService (唯一入口)            │
│  CanEdit(userId, role, medicalCase)                         │
│  CanDelete(userId, role, medicalCase)                       │
│  CanCreate(userId, role)                                    │
│  GetPermissions(...)                                        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              MedicalCaseRules (仅保留非权限规则)               │
│  ✅ CanCreateNewCase() - 业务约束                            │
│  ✅ HasActiveCase() - 业务查询                               │
│  ✅ CanComplete() - 状态转换条件                             │
│  ❌ 删除 CanEdit() - 移到PermissionService                  │
│  ❌ 删除 CanDelete() - 移到PermissionService                │
└─────────────────────────────────────────────────────────────┘
```

### 统一后的权限逻辑

```csharp
// MedicalCasePermissionService.CanEdit - 唯一权限判断入口
public bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase)
{
    // 管理员权限
    if (IsAdmin(role)) return true;

    // 锁定检查(综合CompletedAt和时间)
    if (medicalCase.IsLocked) return false;

    // 医生只能编辑自己创建的、未完成的医案
    if (role == UserRole.Doctor && medicalCase.UserId == userId)
    {
        return medicalCase.IsActive;
    }

    return false;
}
```

### 锁定逻辑统一

```csharp
// 锁定条件(任一满足即锁定):
// 1. 已有完成时间 → CompletedAt.HasValue (主动锁定)
// 2. 非当天创建 → CreatedAt.Date < DateTime.Today (被动锁定)

public bool IsLocked => CompletedAt.HasValue || CreatedAt.Date < DateTime.Today;
```

### 变更清单

| 文件 | 变更 |
|------|------|
| `MedicalCaseModel.cs` | 删除CanEdit()方法，更新IsLocked定义，新增IsActive/IsCompleted |
| `MedicalCaseRules.cs` | 删除CanEdit/CanDelete/ValidateCaseUpdate |
| `MedicalCasePermissionService.cs` | 更新CanEdit使用Entity.IsLocked |
| `MedicalCaseCommandService.cs` | 改用PermissionService.CanEdit |
| `MedicalCaseStateService.cs` | 改用PermissionService.CanEdit |

## Alternatives Considered

1. **保持双DTO模式**: 不推荐，增加维护成本
2. **完全重构**: 风险过大
3. **渐进式统一(推荐)**: 分Phase执行，可控风险
