# MedicalCase聚合根架构重构方案（激进版）

## 📋 元数据

- **文档版本**: v1.0
- **创建日期**: 2025-10-24
- **状态**: 待审批
- **关联Issue**: Epic #1589架构违规修复
- **架构参考**: `docs/architecture/shared/medicalcase-architecture-correction-plan-v2.md`
- **问题报告**: `docs/reports/architecture-compliance-analysis-2025-10-24.md`

## 🎯 重构目标

**核心原则**：不考虑向后兼容性，直接实施正确的v2.0三层架构，彻底清除所有架构违规。

**重构范围**：
- ✅ 修复所有9个架构违规项
- ✅ 清理所有违规API端点（直接删除，不标记Obsolete）
- ✅ 重构Service层职责边界
- ✅ 清理Repository接口
- ✅ 规范DTO设计

**不做的事**：
- ❌ 不保留旧API端点
- ❌ 不考虑Client端兼容性
- ❌ 不使用Obsolete标记（直接删除）

## 📊 违规清单与修复策略

### 9个架构违规分类

| 违规ID | 位置 | 类型 | 严重性 | 修复策略 |
|--------|------|------|--------|---------|
| V1 | ConsultationController.CompleteStep1 | Write绕过聚合根 | 🔴 Critical | 直接删除 |
| V2 | ConsultationService双Repository | 职责不清 | 🔴 Critical | 移除IMedicalCaseRepository |
| V3 | PrescriptionsController.PhysicalDelete | Write绕过聚合根 | 🟠 High | 直接删除 |
| V4 | PrescriptionsController.SoftDelete | Write绕过聚合根 | 🟠 High | 直接删除 |
| V5 | PrescriptionsController.ImportFormula | Write绕过聚合根 | 🔴 Critical | 直接删除 |
| V6 | IConsultationRepository | 职责不清 | 🟡 Medium | 重构接口 |
| V7 | IPrescriptionRepository | 职责不清 | 🟡 Medium | 重构接口 |
| V8 | ConsultationDto.MedicalCase | 冗余导航 | 🟡 Medium | 移除属性 |
| V9 | PrescriptionDto.MedicalCase | 冗余导航 | 🟡 Medium | 移除属性 |

## 🔧 Phase 1: 清理违规API端点（直接删除）

### 1.1 ConsultationController清理

**删除端点**：
```csharp
// ❌ 直接删除（不保留）
// POST /api/v1/consultations/{medicalCaseId}/complete-step1
public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(...)
```

**理由**：
- Epic #1589 Phase 1已实施但架构违规
- 功能将迁移到MedicalCaseController
- Client端需要同步修改API调用

### 1.2 PrescriptionsController清理

**删除端点**：
```csharp
// ❌ 直接删除以下3个端点
// DELETE /api/v1/prescriptions/{id}
public async Task<ActionResult<ApiResponse>> PhysicalDelete(Guid id)

// DELETE /api/v1/prescriptions/{id}/soft
public async Task<ActionResult<ApiResponse>> SoftDelete(Guid id)

// POST /api/v1/prescriptions/{prescriptionId}/import-formula/{formulaId}
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(...)
```

**理由**：
- 所有写操作必须通过MedicalCase聚合根
- Delete功能通过MedicalCaseController实现
- ImportFormula功能迁移到MedicalCaseController

### 1.3 已标记Obsolete的端点处理

**保持Obsolete状态**（这些已经正确标记）：
```csharp
// ✅ 已正确标记为Obsolete，保持现状
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案（含处方）。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add(...)

[Obsolete("请使用 PUT /api/medicalcases/{id}/prescription 更新处方信息。", true)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(...)
```

**理由**：
- 这些端点已经在Issue #1477中正确处理
- 保持Obsolete状态，引导开发者使用正确API

## 🔧 Phase 2: 重构Service层职责边界

### 2.1 ConsultationService重构

**当前问题**：
```csharp
// ❌ 职责不清：同时依赖两个Repository
public class ConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMedicalCaseRepository _medicalCaseRepository; // 移除
}
```

**重构方案**：
```csharp
// ✅ 明确职责：只负责Read Layer查询
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultationService> _logger;
    
    // 构造函数：移除IMedicalCaseRepository
    public ConsultationService(
        IConsultationRepository repository,
        IMapper mapper,
        ILogger<ConsultationService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }
    
    // ✅ 保留Read-only方法
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(...)
    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(...)
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(...)
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(...)
    
    // ❌ 删除所有Write方法（迁移到MedicalCaseService）
    // CreateAsync() - 删除
    // UpdateAsync() - 删除
    // DeleteAsync() - 删除
    // CompleteStep1Async() - 删除
}
```

### 2.2 PrescriptionService重构

**类似ConsultationService重构**：
- 移除IMedicalCaseRepository依赖
- 保留Read-only方法
- 删除所有Write方法

### 2.3 MedicalCaseService扩展

**新增Write方法**（迁移自ConsultationService/PrescriptionService）：

```csharp
// ✅ 所有Write操作通过MedicalCase聚合根
public class MedicalCaseService
{
    // Epic #1589 Phase 1: 完成辩证步骤
    public async Task<ServiceResult<ConsultationStepDto>> CompleteStep1Async(
        Guid medicalCaseId, 
        CompleteStep1Request request)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        medicalCase.Consultation.Step1CompletedAt = DateTime.UtcNow;
        medicalCase.Consultation.PrescriptionEnabled = request.PrescriptionEnabled;
        await _repository.UpdateAsync(medicalCase);
        return ServiceResult<ConsultationStepDto>.Success(dto);
    }
    
    // Epic #1589 Phase 2: 重置诊疗步骤
    public async Task<ServiceResult> ResetConsultationStepsAsync(Guid medicalCaseId)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        medicalCase.Consultation.Step1CompletedAt = null;
        medicalCase.Consultation.Step2CompletedAt = null;
        medicalCase.Consultation.Step3CompletedAt = null;
        await _repository.UpdateAsync(medicalCase);
        return ServiceResult.Success();
    }
    
    // Epic #1589 Phase 4: 清空处方（替代Delete）
    public async Task<ServiceResult> ClearPrescriptionAsync(Guid medicalCaseId)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase.Prescription != null)
        {
            // 清空处方内容，但保留实体框架
            medicalCase.Prescription.ClearContent();
            await _repository.UpdateAsync(medicalCase);
        }
        return ServiceResult.Success();
    }
    
    // Epic #1589 Phase 4: 从配方导入处方
    public async Task<ServiceResult<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
        Guid medicalCaseId, 
        Guid formulaId)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        // 通过聚合根更新处方
        await medicalCase.ImportFormulaIntoPrescription(formulaId);
        await _repository.UpdateAsync(medicalCase);
        return ServiceResult<PrescriptionDto>.Success(dto);
    }
}
```

## 🔧 Phase 3: 重构Repository接口

### 3.1 IConsultationRepository简化

**当前问题**：接口职责不清，包含Write方法

**重构方案**：
```csharp
// ✅ 明确为Read-only Repository
public interface IConsultationRepository
{
    // Read-only查询方法
    Task<Consultation?> GetByIdAsync(Guid id);
    Task<Consultation?> GetByIdWithDetailsAsync(Guid id);
    Task<Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(int page, int pageSize, string? keyword = null);
    Task<IEnumerable<Consultation>> FindAsync(Expression<Func<Consultation, bool>> predicate);
    
    // ❌ 删除Write方法（不再需要）
    // Task<Consultation> AddAsync(Consultation entity) - 删除
    // Task<Consultation> UpdateAsync(Consultation entity) - 删除
    // Task<bool> DeleteAsync(Guid id) - 删除
}
```

**理由**：
- Consultation的Write操作通过MedicalCaseRepository实现
- ConsultationRepository只负责独立查询场景
- 符合Write/Read Layer分离原则

### 3.2 IPrescriptionRepository简化

**类似IConsultationRepository重构**

### 3.3 IMedicalCaseRepository无需改动

**当前设计已正确**：
- 包含完整的CRUD方法（聚合根Repository）
- GetByIdWithDetailsAsync预加载Consultation/Prescription
- 符合v2.0架构

## 🔧 Phase 4: 清理DTO冗余属性

### 4.1 ConsultationDto清理

**当前问题**：
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    
    // ❌ 冗余导航属性
    public virtual MedicalCaseDto? MedicalCase { get; set; }
    
    // ✅ 保留计算属性（业务需要）
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
}
```

**重构方案**：
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    
    // ✅ 保留必要的计算属性
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
    
    // 诊疗字段...
    public string? ChiefComplaint { get; set; }
    public string? TCMDiagnosis { get; set; }
    // ...
    
    // ❌ 移除MedicalCase导航属性
}
```

**AutoMapper配置调整**：
```csharp
CreateMap<Consultation, ConsultationDto>()
    .ForMember(dest => dest.PatientName, 
        opt => opt.MapFrom(src => src.MedicalCase.PatientName))
    .ForMember(dest => dest.DoctorName, 
        opt => opt.MapFrom(src => src.MedicalCase.DoctorName));
```

### 4.2 PrescriptionDto清理

**类似ConsultationDto重构**

## 🔧 Phase 5: MedicalCaseController扩展

### 5.1 新增聚合根Write端点

**所有Epic #1589功能通过MedicalCaseController实现**：

```csharp
/// <summary>
/// 完成辩证步骤（Step 1）
/// Epic #1589 Phase 1 - 架构合规版本
/// </summary>
[HttpPost("{id}/complete-step1")]
[ProducesResponseType(typeof(ApiResponse<ConsultationStepDto>), 200)]
public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(
    Guid id,
    [FromBody] CompleteStep1Request request)
{
    var result = await _medicalCaseService.CompleteStep1Async(id, request);
    return HandleServiceResult(result);
}

/// <summary>
/// 重置诊疗步骤
/// Epic #1589 Phase 2 - 架构合规版本
/// </summary>
[HttpPut("{id}/reset-consultation-steps")]
[ProducesResponseType(typeof(ApiResponse), 200)]
public async Task<ActionResult<ApiResponse>> ResetConsultationSteps(Guid id)
{
    var result = await _medicalCaseService.ResetConsultationStepsAsync(id);
    return HandleServiceResult(result);
}

/// <summary>
/// 清空处方内容
/// Epic #1589 Phase 4 - 架构合规版本（替代Delete）
/// </summary>
[HttpDelete("{id}/prescription/clear")]
[ProducesResponseType(typeof(ApiResponse), 200)]
public async Task<ActionResult<ApiResponse>> ClearPrescription(Guid id)
{
    var result = await _medicalCaseService.ClearPrescriptionAsync(id);
    return HandleServiceResult(result);
}

/// <summary>
/// 从配方导入处方
/// Epic #1589 Phase 4 - 架构合规版本
/// </summary>
[HttpPost("{id}/prescription/import-formula/{formulaId}")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(
    Guid id,
    Guid formulaId)
{
    var result = await _medicalCaseService.ImportFormulaIntoPrescriptionAsync(id, formulaId);
    return HandleServiceResult(result);
}
```

## 📋 实施顺序与依赖关系

### Step 1: Server端重构（8-10小时）

**顺序**（从底层到上层）：
1. Repository接口清理（1小时）
   - 修改IConsultationRepository/IPrescriptionRepository
   - 移除Write方法声明
2. DTO清理（1小时）
   - 移除ConsultationDto/PrescriptionDto冗余属性
   - 更新AutoMapper配置
3. Service层重构（3-4小时）
   - ConsultationService移除IMedicalCaseRepository
   - PrescriptionService移除IMedicalCaseRepository
   - MedicalCaseService新增Write方法
4. Controller层清理（3-4小时）
   - ConsultationController删除CompleteStep1
   - PrescriptionsController删除3个违规端点
   - MedicalCaseController新增5个端点

### Step 2: Client端同步修改（4-6小时）

**顺序**：
1. ApiClient层修改（2-3小时）
   - MedicalCaseApiClient新增方法
   - ConsultationApiClient移除废弃方法调用
   - PrescriptionApiClient移除废弃方法调用
2. ViewModel层修改（2-3小时）
   - ConsultationFormViewModel修改CompleteStep1调用路径
   - PrescriptionFormViewModel修改导入配方调用路径

### Step 3: 编译与验证（2-3小时）

1. 编译验证（0 errors, 0 warnings）
2. 运行时验证：
   - 启动WebAPI，验证Swagger文档
   - 启动Desktop Client，验证诊疗工作流
   - 测试CompleteStep1功能
3. 数据库验证：
   - 确认Step1CompletedAt字段正确保存

### Step 4: 文档更新（1-2小时）

1. 更新Epic #1589设计文档
2. 更新architecture-compliance-analysis-2025-10-24.md（标记为已修复）
3. 创建"架构重构复盘"文档

## 📊 总工作量估算

| 阶段 | 工作量 | 说明 |
|-----|--------|------|
| Server端重构 | 8-10小时 | Repository + Service + Controller |
| Client端同步 | 4-6小时 | ApiClient + ViewModel |
| 编译验证 | 2-3小时 | 编译 + 运行时 + 数据库 |
| 文档更新 | 1-2小时 | 设计文档 + 报告 |
| **总计** | **15-21小时** | **2-3天** |

## ✅ 验收标准

### 编译标准
- ✅ 0 errors, 0 warnings
- ✅ 所有废弃API端点已删除
- ✅ 所有Repository接口符合Read-only定义

### 架构合规标准
- ✅ 运行lybtzyzs-arch-compliance检查：0违规
- ✅ 所有Write操作通过MedicalCase聚合根
- ✅ ConsultationService/PrescriptionService只有Read方法

### 功能标准
- ✅ CompleteStep1功能正常工作（新端点）
- ✅ Step1CompletedAt字段正确保存到数据库
- ✅ Client端诊疗工作流无异常

### 文档标准
- ✅ Epic #1589设计文档更新完成
- ✅ 架构合规性报告标记为"已修复"
- ✅ CLAUDE.md添加设计流程改进章节

## 🔄 Epic #1589重新设计指导

### 架构合规要求

**所有新功能必须遵循**：
1. Write操作：必须通过MedicalCaseController/Service
2. Read操作：可以通过ConsultationController/Service（独立查询）
3. Helper操作：工具函数，不修改聚合根状态

### Phase 1-5重新设计清单

| Phase | 当前设计 | 合规设计 | 状态 |
|-------|---------|---------|------|
| Phase 1 | POST /consultations/{id}/complete-step1 | POST /medicalcases/{id}/complete-step1 | ✅ 已在重构方案中 |
| Phase 2 | PUT /consultations/{id}/reset-steps | PUT /medicalcases/{id}/reset-consultation-steps | ✅ 已在重构方案中 |
| Phase 3 | GET /consultations/other-cases | 保持不变（已合规） | ✅ 无需改动 |
| Phase 4 | DELETE /prescriptions/{id} | DELETE /medicalcases/{id}/prescription/clear | ✅ 已在重构方案中 |
| Phase 4 | POST /prescriptions/{id}/import-formula | POST /medicalcases/{id}/prescription/import-formula | ✅ 已在重构方案中 |
| Phase 5 | 待明确 | 确保通过MedicalCaseService.SaveAsDraft() | ⚠️ 需重新设计 |

## 📝 设计流程改进

### CLAUDE.md更新建议

**新增章节：设计阶段架构合规性检查（强制）**

```markdown
## 设计阶段架构合规性检查（强制环节）

### 适用场景
- 所有新功能设计（Epic/Feature Issue）
- 所有架构调整（重构/模块拆分）
- 所有API端点设计（新增/修改）

### 检查清单
1. **架构文档引用**：
   - ✅ 设计文档必须引用相关架构文档
   - ✅ 对于MedicalCase相关功能，必须引用`medicalcase-architecture-correction-plan-v2.md`

2. **聚合根原则验证**：
   - ✅ Write操作：通过MedicalCase聚合根
   - ✅ Read操作：可独立查询
   - ✅ Helper操作：不修改状态

3. **架构合规Skill检查**：
   - ✅ 设计完成后，运行`lybtzyzs-arch-compliance` Skill
   - ✅ 解决所有检测到的违规项
   - ✅ 在设计文档中记录检查结果

4. **设计评审要素**：
   - ✅ API端点路径符合Write/Read/Helper分层
   - ✅ Service层职责清晰
   - ✅ Repository使用符合聚合根原则

### 失败案例参考
- Epic #1589：设计时未参考v2.0架构，导致全面返工
- 教训：设计阶段架构验证的成本 << 实施后重构的成本
```

## 🎯 成功标准

**本次重构成功的标志**：
1. ✅ 9个架构违规全部修复
2. ✅ Epic #1589功能通过合规API实现
3. ✅ 设计流程改进防止未来违规
4. ✅ 无向后兼容性包袱，架构清晰
5. ✅ 文档完整，可作为最佳实践案例

---

**下一步**：用户审批本方案后，创建GitHub Issue清单并开始实施。
