using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案Service - 聚合根门面模式实现
/// 合并了 Coordinator 的数据加载、聚合保存、生命周期编排职责
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseService> _logger;
    private readonly MedicalCaseCloneMapper _cloneMapper = new();
    private MedicalCaseDetailDto? _originalDetail;
    private MedicalCaseDetailDto? _currentDetail;

    // 缓存字段 (合并自 Coordinator)
    private MedicalCaseDetailDto? _cachedMedicalCase;
    private ConsultationDetailDto? _cachedConsultation;
    private PrescriptionDetailDto? _cachedPrescription;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        ILogger<MedicalCaseService> logger,
        ISessionManager? sessionManager = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
    }

    #region 属性

    public virtual Guid MedicalCaseId => _currentDetail?.Id ?? Guid.Empty;
    public virtual MedicalCaseDetailDto? Current => _currentDetail;
    public virtual ConsultationDetailDto? CurrentConsultation => _currentDetail?.Consultation;
    public virtual PrescriptionDetailDto? CurrentPrescription => _currentDetail?.Prescription;
    public virtual bool HasChanges => _currentDetail != null && _originalDetail != null &&
        (IsMedicalCaseChanged() || IsConsultationChanged() || IsPrescriptionChanged());

    // 缓存属性 (合并自 Coordinator)
    public MedicalCaseDetailDto? CachedMedicalCase => _cachedMedicalCase;
    public ConsultationDetailDto? CachedConsultation => _cachedConsultation;
    public PrescriptionDetailDto? CachedPrescription => _cachedPrescription;

    #endregion

    #region IDataManager实现

    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
    public async Task InitializeAsync(Guid entityId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Initialize started - MedicalCaseId={MedicalCaseId}", entityId);
            _currentDetail = await _repository.GetByIdAsync(entityId);
            if (_currentDetail == null) throw new InvalidOperationException($"未找到ID为{entityId}的医案");
            _originalDetail = _cloneMapper.Clone(_currentDetail);
            _logger.LogDebug("[SVC] MedicalCase.Initialize detail - PatientId={PatientId} UserId={UserId} PatientName={PatientName}",
                _currentDetail.PatientId, _currentDetail.UserId, _currentDetail.PatientName);
            _logger.LogInformation("[SVC] MedicalCase.Initialize completed - PatientName={PatientName}", _currentDetail.PatientName);
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Initialize failed - MedicalCaseId={MedicalCaseId}", entityId); throw; }
    }

    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
    public virtual async Task<bool> SaveAsync(CancellationToken ct = default)
    {
        if (_currentDetail == null) { _logger.LogWarning("[SVC] MedicalCase.Save → NoData"); return false; }
        if (!HasChanges) { _logger.LogDebug("[SVC] MedicalCase.Save → NoChanges - MedicalCaseId={MedicalCaseId}", _currentDetail.Id); return true; }

        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Save started - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            // OpenSpec: simplify-medicalcase-api - 通过聚合保存一次性更新MedicalCase+Consultation+Prescription
            var inputDto = _currentDetail.ToInputDto();
            _logger.LogDebug("[SVC] MedicalCase.Save inputDto - PatientId={PatientId} UserId={UserId}",
                inputDto.PatientId, inputDto.UserId);
            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated != null)
            {
                UpdateMedicalCaseFields(_currentDetail, updated);
                if (updated.Consultation != null) _currentDetail.Consultation = updated.Consultation;
                if (updated.Prescription != null) _currentDetail.Prescription = updated.Prescription;
            }
            _originalDetail = _cloneMapper.Clone(_currentDetail);
            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            return true;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Save failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id); return false; }
    }

    public virtual async Task<bool> DeleteAsync(CancellationToken ct = default)
    {
        if (_currentDetail == null) { _logger.LogWarning("[SVC] MedicalCase.Delete → NoData"); return false; }
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Delete started - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            var result = await _repository.DeleteAsync(_currentDetail.Id);
            if (result)
            {
                _logger.LogInformation("[SVC] MedicalCase.Delete completed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
                _currentDetail = null; _originalDetail = null;
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.Delete → Failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            }
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Delete failed - MedicalCaseId={MedicalCaseId}", _currentDetail?.Id ?? Guid.Empty); return false; }
    }

    public virtual async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_currentDetail != null)
        {
            _logger.LogDebug("[SVC] MedicalCase.Reload started - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            await InitializeAsync(_currentDetail.Id);
        }
    }

    #endregion

    #region 简单CRUD方法

    public virtual async Task<MedicalCaseDetailDto?> GetByIdSimpleAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.GetByIdSimple started - MedicalCaseId={MedicalCaseId}", id);
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
                _logger.LogWarning("[SVC] MedicalCase.GetByIdSimple → NotFound - MedicalCaseId={MedicalCaseId}", id);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.GetByIdSimple failed - MedicalCaseId={MedicalCaseId}", id); return null; }
    }

    // OpenSpec: simplify-desktop-data-layer - UpdateSimpleAsync、CreateAsync已删除
    // ViewModel应直接使用Repository进行CRUD操作

    // OpenSpec: consolidate-medicalcase-detail-queries - GetByIdWithDetailsAsync已删除，使用GetByIdAsync
    public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.GetPaged started - Page={Page} PageSize={PageSize}", page, pageSize);
            var result = await _repository.GetPagedAsync(page, pageSize, searchText);
            _logger.LogDebug("[SVC] MedicalCase.GetPaged completed - TotalCount={TotalCount}", result?.TotalCount ?? 0);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.GetPaged failed - Page={Page}", page); return null; }
    }


    /// <summary>
    /// 统一查询医案
    /// OpenSpec: optimize-medicalcase-api
    /// </summary>
    public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.Query started - QueryType={QueryType}", query.QueryType);
            var result = await _repository.QueryAsync(query);
            _logger.LogDebug("[SVC] MedicalCase.Query completed - TotalCount={TotalCount}", result?.TotalCount ?? 0);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Query failed - QueryType={QueryType}", query.QueryType); return null; }
    }

    // OpenSpec: simplify-desktop-data-layer - DeleteAsync(Guid)、SearchAsync已删除
    // ViewModel应直接使用Repository进行这些操作

    #endregion

    #region 业务命令方法（API-based）

    // OpenSpec: simplify-medicalcase-api - UpdateConsultationAsync已删除
    // 诊断更新通过聚合保存 SaveAsync 处理

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag started - MedicalCaseId={MedicalCaseId} NeedsPrescription={NeedsPrescription}",
                medicalCaseId, request.NeedsPrescription);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var data = await _repository.SetPrescriptionFlagAsync(medicalCaseId, request);
            
            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.SetPrescriptionFlag failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "设置处方标志失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SetPrescriptionFlag failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
    // - ClearPrescriptionAsync: Server端从未实现
    // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CloseCase started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var data = await _repository.CloseCaseAsync(medicalCaseId);
            
            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.CloseCase completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.CloseCase failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "关闭医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CloseCase failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    // OpenSpec: consolidate-medicalcase-detail-queries - 使用QueryAsync替代废弃的GetUnfinishedCaseByPatientIdAsync
    public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.GetUnfinishedByPatient started - PatientId={PatientId} DoctorId={DoctorId}", patientId, doctorId);
            
            // 使用统一查询端点
            var query = new MedicalCaseQueryDto
            {
                QueryType = LYBT.Shared.Models.Enums.MedicalCaseQueryType.Unfinished,
                PatientId = patientId,
                DoctorId = doctorId,
                IncludeAllDoctors = checkAllDoctors,
                PageSize = 1
            };
            var result = await _repository.QueryAsync(query);
            
            if (result?.Items?.Count > 0)
            {
                // 获取完整详情
                var detail = await _repository.GetByIdAsync(result.Items[0].Id);
                _logger.LogDebug("[SVC] MedicalCase.GetUnfinishedByPatient found - MedicalCaseId={MedicalCaseId}", detail?.Id);
                return detail;
            }
            
            _logger.LogDebug("[SVC] MedicalCase.GetUnfinishedByPatient → NotFound - PatientId={PatientId}", patientId);
            return null;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.GetUnfinishedByPatient failed - PatientId={PatientId}", patientId); throw; }
    }

    // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD方法已删除
    // - CreatePrescriptionViaApiAsync: 通过SaveAsync创建
    // - UpdatePrescriptionViaApiAsync: 通过SaveAsync更新
    // - DeletePrescriptionViaApiAsync: 通过SaveAsync设置NeedsPrescription=false触发

    public virtual async Task<ApiResponse> DeleteMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.DeleteViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var success = await _repository.DeleteAsync(medicalCaseId);
            
            if (success)
            {
                _logger.LogInformation("[SVC] MedicalCase.DeleteViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse { Success = true, Message = "医案已取消" };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.DeleteViaApi → Failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse { Success = false, Message = "删除失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.DeleteViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); return new ApiResponse { Success = false, Message = ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex) }; }
    }

    // ========== SoftDeleteMedicalCaseAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
    // Server端点DELETE /api/v1/medicalcases/{id}/soft 不存在，使用DeleteMedicalCaseAsync代替

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatusInputDto request)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus started - MedicalCaseId={MedicalCaseId} Status={Status}", medicalCaseId, request.Status);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var data = await _repository.UpdateStatusAsync(medicalCaseId, request);
            
            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.UpdateStatus completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "更新状态失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.UpdateStatus failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SuspendViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SuspendViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var data = await _repository.SuspendAsync(medicalCaseId, consultationData);

            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.SuspendViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.SuspendViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "挂起医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SuspendViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CancelViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // OpenSpec: simplify-desktop-data-layer - 改用Repository
            var request = string.IsNullOrEmpty(reason) ? null : new CancelMedicalCaseRequestDto { Reason = reason };
            var data = await _repository.CancelMedicalCaseAsync(medicalCaseId, request);
            
            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.CancelViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.CancelViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "取消医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CancelViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    #endregion

    // OpenSpec: cleanup-medicalcase-dead-code - 聚合根专用方法已删除（0调用，功能由SaveAsync替代）
    // - UpdateConsultation: 直接修改Current.Consultation即可
    // - CreatePrescriptionAsync: 通过SaveAsync创建
    // - UpdatePrescription: 直接修改Current.Prescription即可
    // - DeletePrescriptionAsync: 通过SaveAsync设置NeedsPrescription=false触发

    #region 私有方法 - 变更检测

    // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
    private bool IsMedicalCaseChanged() => _currentDetail != null && _originalDetail != null &&
        (_currentDetail.CaseNumber != _originalDetail.CaseNumber ||
         _currentDetail.PatientId != _originalDetail.PatientId || _currentDetail.UserId != _originalDetail.UserId ||
         _currentDetail.CaseStatus != _originalDetail.CaseStatus || _currentDetail.Remark != _originalDetail.Remark);

    // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
    private bool IsConsultationChanged()
    {
        if (_currentDetail?.Consultation == null || _originalDetail?.Consultation == null) return false;
        var c = _currentDetail.Consultation; var o = _originalDetail.Consultation;
        return c.PresentIllness != o.PresentIllness ||
               c.TongueDiagnosis != o.TongueDiagnosis || c.PulseDiagnosis != o.PulseDiagnosis ||
               c.TcmDiagnosis != o.TcmDiagnosis;
    }

    private bool IsPrescriptionChanged()
    {
        if (_currentDetail?.Prescription == null || _originalDetail?.Prescription == null) return false;
        var c = _currentDetail.Prescription; var o = _originalDetail.Prescription;
        return c.DosageCount != o.DosageCount || c.Usage != o.Usage ||
               c.Discount != o.Discount || c.Advice != o.Advice || c.Remark != o.Remark;
    }

    // OpenSpec: simplify-desktop-data-layer - Clone方法已迁移到MedicalCaseCloneMapper(Mapperly源生成)

    // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
    // OpenSpec: refactor-dto-simplification - MedicalCaseDto已删除，统一使用MedicalCaseDetailDto
    private void UpdateMedicalCaseFields(MedicalCaseDetailDto target, MedicalCaseDetailDto source)
    {
        target.CaseNumber = source.CaseNumber;
        target.PatientId = source.PatientId; target.PatientName = source.PatientName;
        target.PatientGender = source.PatientGender; target.PatientAge = source.PatientAge;
        target.UserId = source.UserId; target.DoctorName = source.DoctorName;
        target.ConsultationId = source.ConsultationId; target.PrescriptionId = source.PrescriptionId;
        target.CaseStatus = source.CaseStatus;
        target.Remark = source.Remark; target.UpdatedAt = source.UpdatedAt;
    }

    #endregion

    #region 生命周期管理（合并自MedicalCaseLifecycleHandler）

    /// <summary>
    /// 创建新医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="registrationId">关联挂号ID（可选，从前台挂号创建时传入）</param>
    public virtual async Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CreateNew started - PatientId={PatientId} RegistrationId={RegistrationId}",
                patientId, registrationId);

            // 验证SessionManager和CurrentUser
            if (_sessionManager == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.CreateNew → NullSessionManager");
                return (false, Guid.Empty, "会话管理器未初始化，无法创建医案");
            }
            if (_sessionManager.CurrentUser == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.CreateNew → NullCurrentUser");
                return (false, Guid.Empty, "用户信息丢失，无法创建医案");
            }

            _logger.LogDebug("[SVC] MedicalCase.CreateNew sessionValidated - UserName={UserName} UserId={UserId}",
                _sessionManager.CurrentUser.UserName, _sessionManager.CurrentUser.Id);

            var createDto = new MedicalCaseInputDto
            {
                Id = null,
                PatientId = patientId,
                UserId = _sessionManager.CurrentUser.Id,
                RegistrationId = registrationId,
                Remark = null
            };

            // OpenSpec: simplify-desktop-data-layer - 直接使用Repository
            var createdDto = await _repository.CreateAsync(createDto);
            if (createdDto == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.CreateNew → NullResult");
                return (false, Guid.Empty, "创建医案失败：服务返回空结果");
            }

            _logger.LogInformation("[SVC] MedicalCase.CreateNew completed - MedicalCaseId={MedicalCaseId}", createdDto.Id);
            return (true, createdDto.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.CreateNew failed - PatientId={PatientId}", patientId);
            return (false, Guid.Empty, ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex));
        }
    }

    /// <summary>
    /// 挂起医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> SuspendAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Suspend started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var response = await SuspendViaApiAsync(medicalCaseId);
            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.Suspend → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "挂起医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.Suspend completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.Suspend failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("挂起", ex));
        }
    }

    /// <summary>
    /// 取消医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Cancel started - MedicalCaseId={MedicalCaseId} HasReason={HasReason}",
                medicalCaseId, !string.IsNullOrEmpty(reason));

            var response = await CancelMedicalCaseViaApiAsync(medicalCaseId, reason);
            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "取消医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.Cancel completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.Cancel failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("取消", ex));
        }
    }

    /// <summary>
    /// 完成医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Complete started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var request = new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Completed,
                StatusChangeReason = null
            };
            var response = await UpdateStatusAsync(medicalCaseId, request);

            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.Complete → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "完成医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.Complete completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.Complete failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("完成", ex));
        }
    }

    /// <summary>
    /// 恢复挂起医案为Active状态
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> ResumeSuspendedAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.ResumeSuspended started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var request = new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Active,
                StatusChangeReason = null
            };
            var response = await UpdateStatusAsync(medicalCaseId, request);

            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.ResumeSuspended → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "恢复医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.ResumeSuspended completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.ResumeSuspended failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复", ex));
        }
    }

    #endregion

    #region 数据加载与缓存 (合并自 Coordinator)

    public async Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)> LoadDetailsAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.LoadDetails started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var detail = await GetByIdSimpleAsync(medicalCaseId);
            if (detail == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.LoadDetails → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (false, null, "未找到医案数据");
            }

            _cachedMedicalCase = detail;
            _cachedConsultation = detail.Consultation;
            _cachedPrescription = detail.Prescription;

            _logger.LogInformation("[SVC] MedicalCase.LoadDetails completed");
            return (true, detail, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.LoadDetails failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var errorMsg = ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex);
            return (false, null, errorMsg);
        }
    }

    public void ClearCache()
    {
        _logger.LogDebug("[SVC] MedicalCase.ClearCache");
        _cachedMedicalCase = null;
        _cachedConsultation = null;
        _cachedPrescription = null;
    }

    #endregion

    #region 聚合保存 (合并自 Coordinator)

    public async Task<(bool Success, MedicalCaseDetailDto? Data, string? Error)> AggregateSaveAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null,
        string? editReason = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.AggregateSave started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var aggregateDto = new MedicalCaseInputDto
            {
                Id = medicalCaseId,
                Remark = remark,
                EditReason = editReason,
                Consultation = consultation,
                Prescription = prescription
            };

            var result = await _repository.SaveAsync(medicalCaseId, aggregateDto);

            _cachedMedicalCase = result;
            _cachedConsultation = result?.Consultation;
            _cachedPrescription = result?.Prescription;

            _logger.LogInformation("[SVC] MedicalCase.AggregateSave completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.AggregateSave failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    public async Task<(bool Success, string? Error)> SaveAndCompleteAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        IValidatable? consultationValidator,
        IValidatable? prescriptionValidator,
        string? remark = null,
        bool isPrescriptionEnabled = true)
    {
        // 验证诊断数据
        if (consultationValidator != null && !consultationValidator.Validate())
            return (false, consultationValidator.ValidationMessage);

        // 验证处方数据（如果启用）
        if (isPrescriptionEnabled && prescriptionValidator != null && !prescriptionValidator.Validate())
            return (false, prescriptionValidator.ValidationMessage);

        // 聚合保存
        var (saveOk, _, saveError) = await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        if (!saveOk) return (false, saveError);

        // 完成医案
        return await CompleteMedicalCaseAsync(medicalCaseId);
    }

    public async Task<(bool Success, string? Error)> SaveAndSuspendAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null)
    {
        var (saveOk, _, saveError) = await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        if (!saveOk) return (false, saveError);

        return await SuspendAsync(medicalCaseId);
    }

    public async Task<(bool Success, string? Error)> SaveAndCancelAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null)
    {
        // 取消前保存供审计
        try
        {
            await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "[SVC] MedicalCase.SaveAndCancel → SaveFailed, proceeding with cancel");
        }

        return await CancelMedicalCaseAsync(medicalCaseId);
    }

    #endregion
}
