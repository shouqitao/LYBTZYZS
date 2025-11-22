# MedicalCase模块重构优化方案

**方案目标**: 保留业务必要复杂性的同时，消除过度设计问题
**重构原则**: 谨慎重构，确保业务逻辑完整性

## 🎯 重构目标

### 保留的合理复杂性
- ✅ **聚合根模式**: MedicalCase作为聚合根管理Consultation和Prescription
- ✅ **三步流程**: 辨证→标记→处方/完成的核心业务流程
- ✅ **业务规则**: BR-001、AR-003等核心业务约束
- ✅ **Write/Read分离**: 基本的职责划分

### 优化的过度设计
- ❌ **API端点冗余**: 16个端点 → 10个端点
- ❌ **重复权限验证**: 提取为统一验证方法
- ❌ **过度严格流程**: 提供灵活模式选项
- ❌ **专用DTO泛滥**: 简化DTO结构

## 📋 重构方案

### Phase 1: Controller层优化

#### API端点合并策略

| 现有端点 | 合并后端点 | 功能整合 | 简化效果 |
|---------|-----------|---------|----------|
| `CompleteMedicalCase` | `CompleteMedicalCase` | 保留强制三步验证 | ✅ |
| `CloseMedicalCase` | → `CompleteMedicalCase?mode=flexible` | 支持灵活完成模式 | ✅ -1端点 |
| `UpdateConsultation` | `UpdateMedicalCase` | 统一病案更新接口 | ✅ -1端点 |
| `SetPrescriptionFlag` | → `UpdateMedicalCase` 的字段更新 | 合并到统一更新 | ✅ -1端点 |
| `CreatePrescription` | `UpdateMedicalCase` 的处方操作 | 集成到统一更新 | ✅ -1端点 |
| `UpdatePrescription` | `UpdateMedicalCase` 的处方操作 | 集成到统一更新 | ✅ -1端点 |
| `CanEdit` | → 权限中间件 + 统一响应 | 移除专用验证端点 | ✅ -1端点 |
| `CanDeletePrescription` | → 权限中间件 + 统一响应 | 移除专用验证端点 | ✅ -1端点 |

**优化后**: 16个端点 → **10个端点** (减少37.5%)

#### 统一的MedicalCase更新接口设计

```csharp
[HttpPut("{id}")]
public async Task<ActionResult<object>> UpdateMedicalCase(
    Guid id,
    [FromBody] UpdateMedicalCaseRequest request)
{
    // 统一处理所有更新操作
    var result = await _medicalCaseService.UpdateMedicalCaseAsync(
        id, request, currentUserId, isAdmin);

    return Success(result);
}

public class UpdateMedicalCaseRequest
{
    // 辨证信息
    public ConsultationInputDto? Consultation { get; set; }

    // 流程控制
    public bool? NeedsPrescription { get; set; }
    public bool? ShouldComplete { get; set; }
    public bool FlexibleMode { get; set; } = false; // 灵活模式标志

    // 处方操作
    public PrescriptionOperationDto? PrescriptionOperation { get; set; }
}
```

### Phase 2: Service层优化

#### 统一权限验证基类

```csharp
public abstract class BaseService<T> where T : class
{
    protected async Task<(bool IsAuthorized, string ErrorMessage)> ValidateEditPermissionAsync(
        Guid entityId, Guid currentUserId, bool isAdmin = false)
    {
        // 统一的权限验证逻辑
        var entity = await GetEntityByIdAsync(entityId);
        if (entity == null) return (false, "资源不存在");

        if (isAdmin) return (true, string.Empty);

        // 当天可改规则
        if (entity.CreatedAt.Date == DateTime.Today &&
            entity.DoctorId == currentUserId)
            return (true, string.Empty);

        return (false, "无权限编辑此资源");
    }

    protected abstract Task<T?> GetEntityByIdAsync(Guid id);
}
```

#### 简化的MedicalCaseService

```csharp
public class MedicalCaseService : BaseService<MedicalCaseEntity>
{
    // 统一更新方法替代多个分散的更新方法
    public async Task<MedicalCaseEntity?> UpdateMedicalCaseAsync(
        Guid id,
        UpdateMedicalCaseRequest request,
        Guid currentUserId,
        bool isAdmin = false)
    {
        // 1. 权限验证（使用基类方法）
        var (isAuthorized, errorMsg) = await ValidateEditPermissionAsync(id, currentUserId, isAdmin);
        if (!isAuthorized) throw new UnauthorizedAccessException(errorMsg);

        // 2. 获取聚合根
        var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
        if (medicalCase == null) return null;

        // 3. 业务规则验证（统一入口）
        ValidateBusinessRules(medicalCase, request, currentUserId, isAdmin);

        // 4. 执行更新操作
        await ExecuteUpdateOperations(medicalCase, request, currentUserId);

        // 5. 保存并返回
        return await _repository.UpdateAsync(medicalCase);
    }

    private void ValidateBusinessRules(
        MedicalCaseEntity medicalCase,
        UpdateMedicalCaseRequest request,
        Guid currentUserId,
        bool isAdmin)
    {
        // BR-001: 单患者单Active病案
        if (request.CreateNewCase)
        {
            var existingCases = _repository.GetByPatientIdAsync(request.PatientId!.Value);
            if (!MedicalCaseRules.CanCreateNewCase(existingCases.Result))
                throw new InvalidOperationException("该患者已有进行中的医案");
        }

        // AR-003: 一诊一方约束
        if (request.PrescriptionOperation?.Type == PrescriptionOperationType.Create)
        {
            if (medicalCase.Prescription != null)
                throw new InvalidOperationException("该病案已有处方，违反一诊一方原则");
        }

        // BF-002: 三步流程验证（仅在非灵活模式下）
        if (!request.FlexibleMode && request.ShouldComplete == true)
        {
            ValidateThreeStepProcess(medicalCase);
        }
    }

    private async Task ExecuteUpdateOperations(
        MedicalCaseEntity medicalCase,
        UpdateMedicalCaseRequest request,
        Guid currentUserId)
    {
        // 辨证信息更新
        if (request.Consultation != null)
        {
            _mapper.Map(request.Consultation, medicalCase.Consultation);
            medicalCase.Consultation!.UpdatedAt = DateTime.Now;

            // 标记Step1完成
            if (medicalCase.Consultation.Step1CompletedAt == null)
                medicalCase.Consultation.Step1CompletedAt = DateTime.Now;
        }

        // 处方标志更新
        if (request.NeedsPrescription.HasValue)
        {
            medicalCase.NeedsPrescription = request.NeedsPrescription.Value;

            // 标记Step2完成
            if (medicalCase.Consultation?.Step2CompletedAt == null)
                medicalCase.Consultation!.Step2CompletedAt = DateTime.Now;
        }

        // 处方操作
        if (request.PrescriptionOperation != null)
        {
            await ExecutePrescriptionOperation(medicalCase, request.PrescriptionOperation);
        }

        // 完成病案
        if (request.ShouldComplete == true)
        {
            medicalCase.Status = MedicalCaseStatus.Completed;
            medicalCase.CompletedAt = DateTime.Now;
        }

        medicalCase.UpdatedAt = DateTime.Now;
    }
}
```

### Phase 3: DTO简化

#### 统一响应结构

```csharp
// 替换多个专用DTO为通用结构
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public ValidationErrors? Errors { get; set; }
    public object? Metadata { get; set; } // 额外信息
}

// 处方操作DTO
public class PrescriptionOperationDto
{
    public PrescriptionOperationType Type { get; set; }
    public Guid? PrescriptionId { get; set; } // 用于Update/Delete
    public PrescriptionCreateDto? CreateData { get; set; } // 用于Create
    public PrescriptionEditDto? EditData { get; set; } // 用于Update
}

public enum PrescriptionOperationType
{
    Create,
    Update,
    Delete
}
```

## 📊 优化效果预估

### 代码复杂度改善

| 指标 | 优化前 | 优化后 | 改善 |
|-----|--------|--------|------|
| **API端点数** | 16个 | 10个 | **-37.5%** |
| **Controller方法** | 16个 | 10个 | **-37.5%** |
| **权限验证重复** | 12处重复 | 1处统一 | **-92%** |
| **专用DTO数量** | 8个 | 3个 | **-62.5%** |
| **Service方法** | 14个 | 8个 | **-43%** |

### 维护性提升

- ✅ **统一权限验证**: 一处修改，全局生效
- ✅ **灵活模式支持**: 可选择严格或灵活的流程控制
- ✅ **API简化**: 减少客户端调用复杂度
- ✅ **代码复用**: 提高代码复用率

### 业务完整性保证

- ✅ **保留所有业务规则**: BR-001、AR-003、BF-002等核心规则
- ✅ **聚合根模式**: 继续维护数据一致性
- ✅ **事务边界**: 保持事务完整性
- ✅ **向后兼容**: 提供过渡期兼容性

## 🚀 实施计划

### Phase 1: Controller重构 (3天)
1. 创建统一的UpdateMedicalCase接口
2. 实现权限验证中间件
3. 保持旧接口兼容性（标记为废弃）

### Phase 2: Service重构 (4天)
1. 创建BaseService基类
2. 重构MedicalCaseService统一更新方法
3. 完善单元测试覆盖

### Phase 3: DTO优化 (2天)
1. 简化DTO结构
2. 更新API文档
3. 客户端适配

### Phase 4: 测试验证 (3天)
1. 完整的回归测试
2. 性能基准测试
3. 文档更新

**总工时**: 12天

## ⚠️ 风险控制

### 主要风险
1. **业务逻辑回归**: 确保所有业务规则正确迁移
2. **客户端兼容性**: 需要同时更新客户端代码
3. **性能影响**: 统一接口可能带来性能开销

### 缓解措施
1. **分阶段发布**: 保留旧接口过渡期
2. **完整测试**: 覆盖所有业务场景
3. **性能监控**: 实时监控性能指标
4. **回滚计划**: 准备快速回滚方案

## ✅ 成功标准

- [ ] 所有现有功能正常工作
- [ ] API端点减少到10个
- [ ] 权限验证统一化
- [ ] 支持灵活模式完成流程
- [ ] 单元测试覆盖率≥90%
- [ ] 性能无明显下降
- [ ] 文档更新完整

---

**结论**: MedicalCase模块的复杂性主要由业务需求驱动，建议进行有限度的重构优化，保留核心业务逻辑的完整性，同时消除明显的过度设计问题。