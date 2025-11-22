# 病历管理问题解决指南

> **目标导向**: 解决病历管理过程中的实际问题和常见错误
> **适合人群**: 医生、护士、系统管理员、开发者
> **使用方式**: 问题驱动、按需查找、实用高效

## 🔥 高频问题解决

### 三步流程相关问题

#### 问题1: Step 1辨证信息无法保存

**现象**: 用户填写完辨证信息后点击保存，系统提示"保存失败"或数据丢失

**排查步骤**:
1. 检查必填字段是否完整填写
2. 验证病历状态是否为Active
3. 确认用户权限和病历归属
4. 检查网络连接和数据库状态

**解决方案**:
```csharp
// 1. 完整的数据验证
public class ConsultationInputValidator : AbstractValidator<ConsultationInputDto>
{
    public ConsultationInputValidator()
    {
        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .Length(1, 200).WithMessage("主诉长度应在1-200字符之间");

        RuleFor(x => x.TcmDiagnosis)
            .NotEmpty().WithMessage("中医诊断不能为空")
            .Length(1, 500).WithMessage("中医诊断长度应在1-500字符之间");

        RuleFor(x => x.PresentIllness)
            .MaximumLength(1000).WithMessage("现病史长度不能超过1000字符");

        RuleFor(x => x.TreatmentPlan)
            .MaximumLength(1000).WithMessage("治疗方案长度不能超过1000字符");
    }
}

// 2. 事务性保存
public async Task<Result<Guid>> SaveConsultationStep1Async(
    Guid medicalCaseId,
    ConsultationInputDto dto,
    Guid currentUserId,
    bool isAdmin = false)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    try
    {
        // 权限验证
        var permissionResult = await ValidateEditPermissionAsync(medicalCaseId, currentUserId, isAdmin);
        if (!permissionResult.IsAuthorized)
            return Result.Failure<Guid>(permissionResult.ErrorMessage);

        // 数据验证
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return Result.Failure<Guid>(string.Join("; ", validationResult.Errors));

        // 保存操作
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            return Result.Failure<Guid>("病历不存在");

        // 更新辨证信息
        _mapper.Map(dto, medicalCase.Consultation);
        medicalCase.Consultation.UpdatedAt = DateTime.Now;

        // 标记Step 1完成
        if (medicalCase.Consultation.Step1CompletedAt == null)
        {
            medicalCase.Consultation.Step1CompletedAt = DateTime.Now;
        }

        await _repository.UpdateAsync(medicalCase);
        await transaction.CommitAsync();

        return Result.Success(medicalCase.Id);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "保存辨证信息失败: {MedicalCaseId}", medicalCaseId);
        return Result.Failure<Guid>("保存失败，请重试");
    }
}
```

**预防措施**:
- 客户端实时验证必填字段
- 自动保存草稿功能
- 网络状态监测
- 操作日志记录

#### 问题2: Step 2处方需求标记无法进入

**现象**: Step 1完成后，Step 2处方需求界面无法点击或显示异常

**可能原因**:
- Step 1完成时间戳未正确设置
- 前端界面状态同步异常
- 用户权限验证失败

**解决方案**:
```csharp
// 前端ViewModel状态管理
public class MedicalCaseEditViewModel : BindableBase
{
    [ObservableProperty]
    private MedicalCaseDto medicalCase;

    [ObservableProperty]
    private bool canEditStep2;

    [ObservableProperty]
    private bool canEditStep3;

    // 当MedicalCase变化时更新界面状态
    partial void OnMedicalCaseChanged(MedicalCaseDto value)
    {
        UpdateStepAccessibility();
    }

    private void UpdateStepAccessibility()
    {
        if (MedicalCase == null || MedicalCase.Consultation == null)
        {
            CanEditStep2 = false;
            CanEditStep3 = false;
            return;
        }

        // Step 1完成才能进入Step 2
        CanEditStep2 = MedicalCase.Consultation.Step1CompletedAt.HasValue;

        // Step 2完成才能进入Step 3
        var step2Completed = MedicalCase.NeedsPrescription.HasValue &&
                            MedicalCase.Consultation.Step2CompletedAt.HasValue;

        // 如果需要开处方，Step 3可用
        CanEditStep3 = step2Completed && MedicalCase.NeedsPrescription == true;

        // 如果不需要开处方，直接显示完成按钮
        if (step2Completed && MedicalCase.NeedsPrescription == false)
        {
            ShowCompleteButton = true;
        }
    }

    [RelayCommand]
    private async Task SetPrescriptionNeedAsync(bool needsPrescription)
    {
        try
        {
            if (MedicalCase?.Id == null)
            {
                _dialogService.ShowMessage("请先保存病历基础信息");
                return;
            }

            var result = await _medicalCaseService.SetPrescriptionFlagAsync(
                MedicalCase.Id,
                needsPrescription,
                CurrentUserId,
                IsAdmin);

            if (result != null)
            {
                // 更新本地数据
                _mapper.Map(result, MedicalCase);

                // 刷新界面状态
                UpdateStepAccessibility();

                _dialogService.ShowMessage("处方需求设置成功");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"设置处方需求失败: {ex.Message}");
            _logger.LogError(ex, "设置处方需求失败: {MedicalCaseId}", MedicalCase?.Id);
        }
    }
}
```

**调试技巧**:
```csharp
// 添加详细的调试日志
public async Task<MedicalCaseEntity?> SetPrescriptionFlagAsync(...)
{
    _logger.LogInformation("开始设置处方标志 - MedicalCaseId: {MedicalCaseId}, " +
                       "NeedsPrescription: {NeedsPrescription}, UserId: {UserId}",
                       medicalCaseId, needsPrescription, currentUserId);

    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    _logger.LogInformation("获取病历结果 - Exists: {Exists}, Status: {Status}, " +
                       "Step1Completed: {Step1Completed}",
                       medicalCase != null,
                       medicalCase?.Status,
                       medicalCase?.Consultation?.Step1CompletedAt);

    // ... 业务逻辑

    _logger.LogInformation("处方标志设置完成 - NewStatus: {Status}, " +
                       "Step2CompletedAt: {Step2CompletedAt}",
                       medicalCase.Status,
                       medicalCase.Consultation?.Step2CompletedAt);
}
```

### 权限和访问控制问题

#### 问题3: 病历编辑权限异常

**现象**: 医生无法编辑自己创建的病历，或非管理员可以编辑他人病历

**权限规则验证**:
```csharp
public class MedicalCasePermissionService
{
    public async Task<PermissionResult> ValidateEditPermissionAsync(
        Guid medicalCaseId,
        Guid currentUserId,
        bool isAdmin)
    {
        try
        {
            var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
            {
                return new PermissionResult
                {
                    IsAuthorized = false,
                    ErrorMessage = "病历不存在",
                    ErrorCode = "MEDICAL_CASE_NOT_FOUND"
                };
            }

            // 规则1: 管理员可以编辑所有病历
            if (isAdmin)
            {
                _logger.LogInformation("管理员权限验证通过 - UserId: {UserId}, MedicalCaseId: {MedicalCaseId}",
                    currentUserId, medicalCaseId);

                return new PermissionResult
                {
                    IsAuthorized = true,
                    PermissionLevel = "Admin"
                };
            }

            // 规则2: 创建者当天可编辑
            var isCreator = medicalCase.DoctorId == currentUserId;
            var isSameDay = medicalCase.CreatedAt.Date == DateTime.Today;

            if (!isCreator)
            {
                return new PermissionResult
                {
                    IsAuthorized = false,
                    ErrorMessage = "只能编辑自己创建的病历",
                    ErrorCode = "NOT_CREATOR"
                };
            }

            if (!isSameDay)
            {
                return new PermissionResult
                {
                    IsAuthorized = false,
                    ErrorMessage = "只能编辑当天创建的病历",
                    ErrorCode = "NOT_SAME_DAY",
                    AdditionalInfo = new
                    {
                        CreatedDate = medicalCase.CreatedAt.Date,
                        CurrentDate = DateTime.Today
                    }
                };
            }

            // 规则3: 病历状态必须为Active
            if (medicalCase.Status != MedicalCaseStatus.Active)
            {
                return new PermissionResult
                {
                    IsAuthorized = false,
                    ErrorMessage = $"病历状态为{medicalCase.Status}，不允许编辑",
                    ErrorCode = "INVALID_STATUS",
                    AdditionalInfo = new
                    {
                        CurrentStatus = medicalCase.Status,
                        EditableStatuses = new[] { MedicalCaseStatus.Active }
                    }
                };
            }

            return new PermissionResult
            {
                IsAuthorized = true,
                PermissionLevel = "Creator"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "权限验证异常 - MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                medicalCaseId, currentUserId);

            return new PermissionResult
            {
                IsAuthorized = false,
                ErrorMessage = "权限验证失败，请联系系统管理员",
                ErrorCode = "PERMISSION_CHECK_FAILED"
            };
        }
    }
}
```

**权限结果DTO**:
```csharp
public class PermissionResult
{
    public bool IsAuthorized { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public string PermissionLevel { get; set; }
    public object AdditionalInfo { get; set; }
}
```

**前端权限控制**:
```csharp
public class MedicalCaseEditViewModel : BindableBase
{
    private readonly MedicalCasePermissionService _permissionService;

    [ObservableProperty]
    private bool canEditCurrentMedicalCase;

    [ObservableProperty]
    private string permissionMessage;

    // 在加载病历后检查权限
    private async Task CheckEditPermissionAsync()
    {
        try
        {
            var permissionResult = await _permissionService.ValidateEditPermissionAsync(
                MedicalCase.Id,
                CurrentUserId,
                IsAdmin);

            CanEditCurrentMedicalCase = permissionResult.IsAuthorized;
            PermissionMessage = permissionResult.ErrorMessage;

            if (!permissionResult.IsAuthorized)
            {
                _dialogService.ShowWarning($"无法编辑病历: {permissionResult.ErrorMessage}");

                // 根据错误类型提供具体建议
                switch (permissionResult.ErrorCode)
                {
                    case "NOT_SAME_DAY":
                        var createdDate = ((dynamic)permissionResult.AdditionalInfo).CreatedDate;
                        _dialogService.ShowInfo($"病历创建于 {createdDate:yyyy-MM-dd}，仅当天可编辑。如需修改请联系管理员。");
                        break;

                    case "INVALID_STATUS":
                        _dialogService.ShowInfo("病历已完成或取消，如需修改请联系管理员重新激活。");
                        break;

                    default:
                        _dialogService.ShowInfo(permissionResult.ErrorMessage);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            CanEditCurrentMedicalCase = false;
            PermissionMessage = "权限检查失败";
            _logger.LogError(ex, "检查编辑权限失败: {MedicalCaseId}", MedicalCase?.Id);
        }
    }
}
```

### 数据一致性和完整性问题

#### 问题4: 病历和关联数据不一致

**现象**: MedicalCase记录存在，但Consultation或Prescription数据丢失或不同步

**数据完整性检查**:
```csharp
public class MedicalCaseIntegrityService
{
    public async Task<IntegrityReport> CheckMedicalCaseIntegrityAsync(Guid medicalCaseId)
    {
        var report = new IntegrityReport { MedicalCaseId = medicalCaseId };

        try
        {
            // 检查MedicalCase是否存在
            var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
            {
                report.AddError("MEDICAL_CASE_NOT_FOUND", "病历记录不存在");
                return report;
            }

            report.MedicalCaseExists = true;
            report.Status = medicalCase.Status;
            report.CreatedAt = medicalCase.CreatedAt;

            // 检查Consultation关联
            var consultation = await _consultationRepository.GetByIdAsync(medicalCaseId);
            if (consultation == null)
            {
                report.AddError("CONSULTATION_MISSING", "辨证记录丢失");
            }
            else
            {
                report.ConsultationExists = true;
                report.ConsultationUpdatedAt = consultation.UpdatedAt;

                // 检查时间戳一致性
                if (consultation.UpdatedAt < medicalCase.UpdatedAt)
                {
                    report.AddWarning("CONSULTATION_OUTDATED", "辨证记录更新时间早于病历");
                }
            }

            // 检查Prescription关联（如果标记需要）
            if (medicalCase.NeedsPrescription == true)
            {
                var prescription = await _prescriptionRepository.GetByMedicalCaseIdAsync(medicalCaseId);
                if (prescription == null || prescription.IsDeleted)
                {
                    report.AddError("PRESCRIPTION_MISSING", "标记需要开处方但处方记录丢失");
                }
                else
                {
                    report.PrescriptionExists = true;
                    report.PrescriptionStatus = prescription.Status;

                    // 检查处方明细
                    var details = await _prescriptionDetailRepository.GetByPrescriptionIdAsync(prescription.Id);
                    if (!details.Any())
                    {
                        report.AddWarning("PRESCRIPTION_EMPTY", "处方存在但无药品明细");
                    }
                }
            }

            // 检查三步流程完整性
            if (consultation != null)
            {
                if (consultation.Step1CompletedAt == null)
                {
                    report.AddError("STEP1_INCOMPLETE", "Step 1未完成");
                }

                if (medicalCase.NeedsPrescription.HasValue)
                {
                    if (consultation.Step2CompletedAt == null)
                    {
                        report.AddError("STEP2_INCOMPLETE", "Step 2未完成");
                    }

                    if (medicalCase.NeedsPrescription == true)
                    {
                        var prescription = await _prescriptionRepository.GetByMedicalCaseIdAsync(medicalCaseId);
                        if (prescription == null)
                        {
                            report.AddError("STEP3_INCOMPLETE", "标记需要开处方但Step 3未完成");
                        }
                    }
                }
            }

            return report;
        }
        catch (Exception ex)
        {
            report.AddError("CHECK_FAILED", $"完整性检查失败: {ex.Message}");
            _logger.LogError(ex, "检查病历完整性失败: {MedicalCaseId}", medicalCaseId);
            return report;
        }
    }
}
```

**数据修复服务**:
```csharp
public class MedicalCaseRepairService
{
    public async Task<RepairResult> RepairMedicalCaseAsync(Guid medicalCaseId)
    {
        var result = new RepairResult { MedicalCaseId = medicalCaseId };

        try
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            // 1. 检查并修复Consultation
            await RepairConsultationAsync(medicalCaseId, result);

            // 2. 检查并修复时间戳
            await RepairTimestampsAsync(medicalCaseId, result);

            // 3. 检查并修复状态
            await RepairStatusAsync(medicalCaseId, result);

            await transaction.CommitAsync();
            result.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.IsSuccessful = false;
            result.AddError("REPAIR_FAILED", ex.Message);
            _logger.LogError(ex, "修复病历失败: {MedicalCaseId}", medicalCaseId);
        }

        return result;
    }

    private async Task RepairConsultationAsync(Guid medicalCaseId, RepairResult result)
    {
        var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
        var consultation = await _consultationRepository.GetByIdAsync(medicalCaseId);

        if (consultation == null && medicalCase != null)
        {
            // 创建丢失的Consultation
            var newConsultation = new ConsultationEntity
            {
                Id = medicalCaseId, // 共享主键
                Status = CommonStatus.Enabled,
                ChiefComplaint = "",
                CreatedAt = medicalCase.CreatedAt,
                UpdatedAt = DateTime.Now,
                CreatedBy = medicalCase.CreatedBy,
                UpdatedBy = medicalCase.UpdatedBy
            };

            await _consultationRepository.AddAsync(newConsultation);
            result.AddRepair("CONSULTATION_CREATED", "创建了丢失的辨证记录");
        }
    }

    private async Task RepairTimestampsAsync(Guid medicalCaseId, RepairResult result)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase?.Consultation == null) return;

        // 确保Consultation更新时间不早于MedicalCase
        if (medicalCase.Consultation.UpdatedAt < medicalCase.UpdatedAt)
        {
            medicalCase.Consultation.UpdatedAt = medicalCase.UpdatedAt;
            await _repository.UpdateAsync(medicalCase);
            result.AddRepair("TIMESTAMP_FIXED", "修复了时间戳不一致");
        }
    }
}
```

### 处方管理问题

#### 问题5: 处方打印后无法修改

**现象**: 处方打印后，系统不允许修改或删除，但用户需要调整

**处方状态管理**:
```csharp
public class PrescriptionStatusService
{
    public async Task<bool> CanModifyPrescriptionAsync(Guid prescriptionId, Guid currentUserId, bool isAdmin)
    {
        var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
        if (prescription == null) return false;

        // 1. 已打印的处方原则上不允许修改
        if (prescription.IsPrinted && !isAdmin)
        {
            return false;
        }

        // 2. 已归档的处方不允许修改
        if (prescription.Status == PrescriptionStatus.Archived)
        {
            return false;
        }

        // 3. 检查创建者权限（类似病历权限规则）
        var isCreator = prescription.UserId == currentUserId;
        var isSameDay = prescription.CreatedAt.Date == DateTime.Today;

        return isAdmin || (isCreator && isSameDay);
    }

    public async Task<PrescriptionModificationResult> RequestModificationAsync(
        Guid prescriptionId,
        string reason,
        Guid requestUserId)
    {
        var result = new PrescriptionModificationResult();

        try
        {
            var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "处方不存在";
                return result;
            }

            // 创建修改申请记录
            var modificationRequest = new PrescriptionModificationRequest
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                RequestedBy = requestUserId,
                Reason = reason,
                Status = ModificationRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            await _modificationRequestRepository.AddAsync(modificationRequest);

            // 通知管理员审核
            await _notificationService.NotifyAdminsAsync(
                $"处方修改申请: 处方ID {prescriptionId}",
                $"用户请求修改已打印处方，原因: {reason}",
                NotificationType.PrescriptionModification);

            result.IsSuccessful = true;
            result.RequestId = modificationRequest.Id;
            result.Message = "修改申请已提交，等待管理员审核";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交处方修改申请失败: {PrescriptionId}", prescriptionId);
            result.IsSuccessful = false;
            result.ErrorMessage = "提交申请失败，请重试";
            return result;
        }
    }
}
```

**管理员修改审批**:
```csharp
public class PrescriptionAdminController
{
    public async Task<ApprovalResult> ApproveModificationAsync(
        Guid requestId,
        bool approved,
        Guid adminUserId,
        string comment = null)
    {
        var request = await _modificationRequestRepository.GetByIdAsync(requestId);
        if (request == null)
        {
            return new ApprovalResult
            {
                IsSuccessful = false,
                ErrorMessage = "修改申请不存在"
            };
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 更新申请状态
            request.Status = approved ? ModificationRequestStatus.Approved : ModificationRequestStatus.Rejected;
            request.ReviewedBy = adminUserId;
            request.ReviewedAt = DateTime.Now;
            request.AdminComment = comment;

            await _modificationRequestRepository.UpdateAsync(request);

            if (approved)
            {
                // 允许临时修改
                var prescription = await _prescriptionRepository.GetByIdAsync(request.PrescriptionId);
                if (prescription != null)
                {
                    // 重置打印状态，允许修改
                    prescription.IsPrinted = false;
                    prescription.ModifiedAt = DateTime.Now;
                    prescription.ModifiedBy = adminUserId;

                    // 记录修改日志
                    var modificationLog = new PrescriptionModificationLog
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = request.PrescriptionId,
                        RequestedBy = request.RequestedBy,
                        ApprovedBy = adminUserId,
                        Reason = request.Reason,
                        AdminComment = comment,
                        ModifiedAt = DateTime.Now
                    };

                    await _modificationLogRepository.AddAsync(modificationLog);
                    await _prescriptionRepository.UpdateAsync(prescription);
                }
            }

            await transaction.CommitAsync();

            return new ApprovalResult
            {
                IsSuccessful = true,
                Message = approved ? "修改申请已批准，可以编辑处方" : "修改申请已拒绝"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "审批处方修改申请失败: {RequestId}", requestId);
            return new ApprovalResult
            {
                IsSuccessful = false,
                ErrorMessage = "审批失败，请重试"
            };
        }
    }
}
```

### 性能和并发问题

#### 问题6: 大量病历查询响应缓慢

**性能优化方案**:
```csharp
public class MedicalCaseQueryOptimizer
{
    // 1. 使用查询优化器
    public async Task<PagedResult<MedicalCaseDto>> GetOptimizedMedicalCasesAsync(
        MedicalCaseSearchCriteria criteria)
    {
        var query = _dbContext.MedicalCases.AsNoTracking();

        // 应用过滤器（优化顺序很重要）
        query = ApplyFilters(query, criteria);

        // 使用投影减少数据传输
        var projectedQuery = query.Select(m => new MedicalCaseDto
        {
            Id = m.Id,
            PatientId = m.PatientId,
            PatientName = m.PatientName,
            DoctorId = m.DoctorId,
            DoctorName = m.DoctorName,
            ConsultationDate = m.ConsultationDate,
            Status = m.Status,
            CreatedAt = m.CreatedAt,
            // 只包含必要的关联数据
            Consultation = m.Consultation == null ? null : new ConsultationDto
            {
                ChiefComplaint = m.Consultation.ChiefComplaint,
                TcmDiagnosis = m.Consultation.TcmDiagnosis,
                Step1CompletedAt = m.Consultation.Step1CompletedAt,
                Step2CompletedAt = m.Consultation.Step2CompletedAt
            }
        });

        // 分页
        var totalCount = await projectedQuery.CountAsync();
        var items = await projectedQuery
            .OrderByDescending(m => m.ConsultationDate)
            .Skip((criteria.PageIndex - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();

        return new PagedResult<MedicalCaseDto>(items, totalCount, criteria.PageIndex, criteria.PageSize);
    }

    private IQueryable<MedicalCase> ApplyFilters(IQueryable<MedicalCase> query, MedicalCaseSearchCriteria criteria)
    {
        // 1. 时间范围过滤（使用索引友好的查询）
        if (criteria.StartDate.HasValue)
        {
            query = query.Where(m => m.ConsultationDate >= criteria.StartDate.Value);
        }

        if (criteria.EndDate.HasValue)
        {
            query = query.Where(m => m.ConsultationDate <= criteria.EndDate.Value);
        }

        // 2. 状态过滤
        if (criteria.Status.HasValue)
        {
            query = query.Where(m => m.Status == criteria.Status.Value);
        }

        // 3. 患者过滤
        if (criteria.PatientId.HasValue)
        {
            query = query.Where(m => m.PatientId == criteria.PatientId.Value);
        }

        // 4. 医生过滤
        if (criteria.DoctorId.HasValue)
        {
            query = query.Where(m => m.DoctorId == criteria.DoctorId.Value);
        }

        // 5. 关键词搜索（放在最后）
        if (!string.IsNullOrEmpty(criteria.Keyword))
        {
            query = query.Where(m =>
                m.PatientName.Contains(criteria.Keyword) ||
                (m.Consultation != null && m.Consultation.TcmDiagnosis.Contains(criteria.Keyword)));
        }

        return query;
    }
}
```

**缓存策略**:
```csharp
public class MedicalCaseCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;

    public async Task<PagedResult<MedicalCaseDto>> GetCachedMedicalCasesAsync(
        MedicalCaseSearchCriteria criteria)
    {
        var cacheKey = GenerateCacheKey(criteria);

        // 1. 尝试从本地缓存获取
        if (_cache.TryGetValue(cacheKey, out PagedResult<MedicalCaseDto> cachedResult))
        {
            return cachedResult;
        }

        // 2. 尝试从分布式缓存获取
        var distributedData = await _distributedCache.GetStringAsync(cacheKey);
        if (distributedData != null)
        {
            var result = JsonSerializer.Deserialize<PagedResult<MedicalCaseDto>>(distributedData);

            // 回填本地缓存（短时间）
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

        // 3. 从数据库获取
        var dbResult = await _queryOptimizer.GetOptimizedMedicalCasesAsync(criteria);

        // 4. 缓存结果（根据查询复杂度决定缓存时间）
        var cacheDuration = GetCacheDuration(criteria);
        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dbResult),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheDuration
            });

        // 本地缓存（更短时间）
        _cache.Set(cacheKey, dbResult, TimeSpan.FromMinutes(5));

        return dbResult;
    }

    private TimeSpan GetCacheDuration(MedicalCaseSearchCriteria criteria)
    {
        // 历史数据查询缓存更长时间
        if (criteria.EndDate.HasValue && criteria.EndDate.Value < DateTime.Today.AddDays(-7))
        {
            return TimeSpan.FromHours(2);
        }

        // 当日数据缓存较短时间
        return TimeSpan.FromMinutes(15);
    }
}
```

## 🔧 故障排查工具

### 医疗业务诊断工具

```csharp
public class MedicalCaseDiagnosticTool
{
    public async Task<DiagnosticReport> DiagnoseMedicalCaseAsync(Guid medicalCaseId)
    {
        var report = new DiagnosticReport { MedicalCaseId = medicalCaseId };

        // 1. 基础完整性检查
        await CheckBasicIntegrityAsync(medicalCaseId, report);

        // 2. 业务规则验证
        await ValidateBusinessRulesAsync(medicalCaseId, report);

        // 3. 数据一致性检查
        await CheckDataConsistencyAsync(medicalCaseId, report);

        // 4. 性能分析
        await AnalyzePerformanceAsync(medicalCaseId, report);

        // 5. 生成修复建议
        GenerateRepairSuggestions(report);

        return report;
    }

    private async Task CheckBasicIntegrityAsync(Guid medicalCaseId, DiagnosticReport report)
    {
        // 检查MedicalCase存在性
        var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
        if (medicalCase == null)
        {
            report.AddCriticalIssue("MEDICAL_CASE_NOT_FOUND", "病历记录不存在");
            return;
        }

        report.MedicalCase = medicalCase;

        // 检查关联数据
        var consultation = await _consultationRepository.GetByIdAsync(medicalCaseId);
        if (consultation == null)
        {
            report.AddCriticalIssue("CONSULTATION_MISSING", "辨证记录缺失");
        }
        else
        {
            report.Consultation = consultation;
        }

        // 检查处方（如果需要）
        if (medicalCase.NeedsPrescription == true)
        {
            var prescription = await _prescriptionRepository.GetByMedicalCaseIdAsync(medicalCaseId);
            if (prescription == null)
            {
                report.AddCriticalIssue("PRESCRIPTION_MISSING", "标记需要开处方但处方缺失");
            }
            else
            {
                report.Prescription = prescription;
            }
        }
    }

    private void GenerateRepairSuggestions(DiagnosticReport report)
    {
        foreach (var issue in report.Issues.Where(i => i.Severity >= IssueSeverity.Warning))
        {
            switch (issue.Code)
            {
                case "CONSULTATION_MISSING":
                    issue.Suggestion = "运行数据修复工具创建缺失的辨证记录";
                    issue.CanAutoRepair = true;
                    break;

                case "STEP1_INCOMPLETE":
                    issue.Suggestion = "检查辨证信息是否完整填写，然后重新保存";
                    issue.CanAutoRepair = false;
                    break;

                case "TIMESTAMP_INCONSISTENT":
                    issue.Suggestion = "运行时间戳修复工具同步更新时间";
                    issue.CanAutoRepair = true;
                    break;

                case "PRESCRIPTION_MISSING":
                    issue.Suggestion = medicalCase.NeedsPrescription == true
                        ? "创建缺失的处方记录"
                        : "清除处方需求标记";
                    issue.CanAutoRepair = true;
                    break;
            }
        }
    }
}
```

## 📋 最佳实践检查清单

### 日常操作检查
- [ ] 每日检查病历数据完整性
- [ ] 定期备份重要病历数据
- [ ] 监控系统性能指标
- [ ] 验证权限控制有效性

### 数据质量管理
- [ ] 定期运行数据一致性检查
- [ ] 及时处理数据异常报告
- [ ] 建立数据质量监控机制
- [ ] 制定数据修复标准流程

### 系统维护
- [ ] 定期清理过期缓存
- [ ] 监控数据库性能
- [ ] 更新索引优化查询
- [ ] 检查日志记录完整性

### 安全合规
- [ ] 定期审计访问日志
- [ ] 验证数据脱敏规则
- [ ] 检查权限分配合理性
- [ ] 更新安全防护措施

---

**文档类型**: How-to Guide
**适用场景**: 病历管理问题解决
**更新时间**: 2025-11-22
**相关资源**: [病历管理教程](../../tutorials/modules/medicalcase/medical-case-management-tutorial.md) | [病历管理API](../../reference/api/medical-case.md)