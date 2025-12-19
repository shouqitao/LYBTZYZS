# OpenSpec Proposal: unify-medicalcase-input-dto

## Metadata
- **Status**: proposed
- **Created**: 2025-12-19
- **Parent Epic**: #1961 (DTO Architecture Standardization)
- **Related**: consolidate-medicalcase-queries Phase 7.B (DEFERRED)

## Problem Statement

当前MedicalCase模块存在DTO定义混乱和API契约不一致的问题:

### 1. Client端DTO冗余
- `MedicalCaseInputDto`: 包含PatientId, DoctorId, VisitDate + 诊断字段(ChiefComplaint, TCMDiagnosis等)
- `MedicalCaseAggregateInputDto`: 包含Id, Remark, EditReason + 嵌套的Consultation/Prescription

两者职责重叠，且命名不符合DTO架构规范(InputDto应为简单输入，AggregateInputDto才是聚合输入)。

### 2. Client-Server契约不一致
Client的`MedicalCaseInputDto`包含诊断字段，但Server端的`CreateMedicalCaseRequest`只使用:
- PatientId
- VisitDate

**结果**: Client发送的诊断字段被Server完全忽略，造成API契约的隐式不一致。

### 3. Server内部类暴露问题
`CreateMedicalCaseRequest`是Server内部类，但其字段定义决定了实际API行为，与Shared层DTO定义不匹配。

## Proposed Solution

### Phase 1: 简化MedicalCaseInputDto

将`MedicalCaseInputDto`简化为仅包含创建医案所需的核心字段:

```csharp
public class MedicalCaseInputDto
{
    public Guid? Id { get; set; }  // 可选，用于区分Create/Update
    public required Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }  // 可选，Server端自动填充当前用户
    public DateTime? VisitDate { get; set; }  // 可选，默认当前时间
}
```

### Phase 2: 保留MedicalCaseAggregateInputDto

保留`MedicalCaseAggregateInputDto`用于聚合保存场景:

```csharp
public class MedicalCaseAggregateInputDto
{
    public required Guid Id { get; set; }  // 必须，聚合保存只用于更新
    public string? Remark { get; set; }
    public string? EditReason { get; set; }
    public ConsultationInputDto? Consultation { get; set; }
    public PrescriptionAggregateInputDto? Prescription { get; set; }
}
```

### Phase 3: 删除Server内部类

删除`CreateMedicalCaseRequest`，直接使用Shared层的`MedicalCaseInputDto`。

### Phase 4: 清理冗余字段

从现有`MedicalCaseInputDto`中移除Server不使用的诊断字段。

## Impact Analysis

### Files to Modify
1. `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs` - 简化字段
2. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 使用Shared DTO
3. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/` - 适配新DTO

### Files to Delete
1. `CreateMedicalCaseRequest` (如果存在为独立文件)

### Breaking Changes
- `MedicalCaseInputDto`字段减少
- 使用诊断字段的现有代码需要迁移到`MedicalCaseAggregateInputDto`

## Success Criteria

1. [ ] MedicalCaseInputDto仅包含创建医案的核心字段
2. [ ] MedicalCaseAggregateInputDto用于聚合保存场景
3. [ ] Server端删除内部CreateMedicalCaseRequest类
4. [ ] Client-Server API契约完全一致
5. [ ] 编译通过，0错误0警告
6. [ ] 现有功能不受影响

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| 现有代码依赖诊断字段 | 全局搜索并迁移到AggregateInputDto |
| 破坏现有API调用 | 保持向后兼容的字段命名 |
| 测试覆盖不足 | 优先修复测试文件 |

## Alternatives Considered

1. **保持现状**: 不推荐，技术债务持续累积
2. **完全重构**: 风险过大，不符合RC阶段策略
3. **渐进式统一(推荐)**: 分Phase执行，每步可验证
