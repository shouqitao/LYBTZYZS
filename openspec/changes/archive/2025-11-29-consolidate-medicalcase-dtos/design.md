# Design: consolidate-medicalcase-dtos

## 设计决策

### 1. DTO命名规范

根据`project.md`和现有Shared层代码，统一采用以下命名规范：

| 类型 | 后缀 | 示例 |
|------|------|------|
| 数据传输对象 | `*Dto` | `MedicalCaseDto`, `ConsultationDto` |
| 创建请求 | `*CreateDto` 或 `*Request` | `PrescriptionCreateDto` |
| 更新请求 | `*UpdateDto` 或 `*Request` | `UpdateMedicalCaseRequest` |
| 输入对象 | `*InputDto` | `ConsultationInputDto` |
| 详情响应 | `*DetailDto` | `MedicalCaseDetailDto` |

### 2. DTO合并/迁移决策

#### 2.1 SetPrescriptionFlagRequest

**决策**: 删除Server层版本，使用Shared层版本

**理由**: 
- 两个版本完全相同
- Shared层版本位于正确位置

#### 2.2 MedicalCaseDetailResponse vs MedicalCaseDetailDto

**待分析字段对比**:

```csharp
// MedicalCaseDetailResponse (Server层)
public class MedicalCaseDetailResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public ConsultationDto? Consultation { get; set; }
    public MedicalCasePrescriptionDto? Prescription { get; set; }  // 使用简化版
    public MedicalCaseStatus Status { get; set; }
    public bool? NeedsPrescription { get; set; }
    // ...
}

// MedicalCaseDetailDto (Shared层)
public class MedicalCaseDetailDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public ConsultationDto? Consultation { get; set; }
    public PrescriptionDto? Prescription { get; set; }  // 使用完整版
    public MedicalCaseStatus Status { get; set; }
    public bool? NeedsPrescription { get; set; }
    // ...
}
```

**决策选项**:
- **选项A**: 合并到`MedicalCaseDetailDto`，删除Response版本
- **选项B**: 保留两者，将Response迁移到Shared层并重命名

**推荐**: 选项A - 功能重叠度高，应统一使用`MedicalCaseDetailDto`

#### 2.3 MedicalCasePrescriptionDto vs PrescriptionDto

**分析**:
- `MedicalCasePrescriptionDto`是`PrescriptionDto`的简化版本
- 可能是为了减少API响应体积

**决策选项**:
- **选项A**: 删除简化版，统一使用`PrescriptionDto`
- **选项B**: 迁移到Shared层，命名为`PrescriptionSummaryDto`

**推荐**: 需进一步分析使用场景后决定

#### 2.4 UpdateMedicalCaseRequest

**分析**:
- 包含复杂业务逻辑（UpdateMode枚举、多种子请求类型）
- 是MedicalCase更新的主要入口

**决策**: 整体迁移到Shared层

**迁移内容**:
```csharp
// 迁移到 MedicalCaseDtos.cs

public enum MedicalCaseUpdateMode
{
    UpdateAll,
    UpdateConsultation,
    UpdatePrescription,
    CompleteCase
}

public class UpdateMedicalCaseRequest
{
    public MedicalCaseStatus? Status { get; set; }
    public bool? NeedsPrescription { get; set; }
    public ConsultationInputDto? Consultation { get; set; }
    public PrescriptionCreateDto? CreatePrescription { get; set; }
    public PrescriptionUpdateRequest? UpdatePrescription { get; set; }
    public DeletePrescriptionRequest? DeletePrescription { get; set; }
    public CompleteCaseRequest? CompleteCase { get; set; }
    public MedicalCaseUpdateMode Mode { get; set; } = MedicalCaseUpdateMode.UpdateAll;
}

public class PrescriptionUpdateRequest
{
    public Guid PrescriptionId { get; set; }
    public PrescriptionCreateDto Data { get; set; } = null!;
}

public class DeletePrescriptionRequest
{
    public Guid PrescriptionId { get; set; }
}

public class CompleteCaseRequest
{
    public bool PrintPrescription { get; set; }
}
```

### 3. 迁移顺序

1. **先删除重复** - SetPrescriptionFlagRequest (风险最低)
2. **再迁移独立DTO** - UpdateMedicalCaseRequest (无合并冲突)
3. **最后处理重叠DTO** - 需要详细分析后决定

### 4. 命名空间

迁移后的DTO位于:
```
LYBT.Shared.Models.Contracts.MedicalCase
```

与现有MedicalCase相关DTO保持一致。
