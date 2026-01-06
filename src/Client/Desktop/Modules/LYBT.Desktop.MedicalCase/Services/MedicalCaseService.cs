using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 病案Service - 聚合根模式实现
/// OpenSpec: standardize-service-layer - 统一使用Service命名
/// OpenSpec: simplify-medicalcase-api - 统一管理Consultation和Prescription
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMedicalCaseApi _api;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseService> _logger;
    private MedicalCaseDetailDto? _originalDetail;
    private MedicalCaseDetailDto? _currentDetail;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IMedicalCaseApi api,
        ILogger<MedicalCaseService> logger,
        ISessionManager? sessionManager = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
    }

    #region 属性

    /// <summary>
    /// 医案ID（聚合根ID）
    /// OpenSpec: simplify-medicalcase-api - 统一接口
    /// </summary>
    public virtual Guid MedicalCaseId => _currentDetail?.Id ?? Guid.Empty;

    public virtual MedicalCaseDetailDto? Current => _currentDetail;
    public virtual ConsultationDetailDto? CurrentConsultation => _currentDetail?.Consultation;
    public virtual PrescriptionDetailDto? CurrentPrescription => _currentDetail?.Prescription;

    public virtual bool HasChanges => _currentDetail != null && _originalDetail != null &&
        (IsMedicalCaseChanged() || IsConsultationChanged() || IsPrescriptionChanged());

    #endregion

    #region IDataManager实现

    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
    public async Task InitializeAsync(Guid entityId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Initialize started - MedicalCaseId={MedicalCaseId}", entityId);
            _currentDetail = await _repository.GetByIdAsync(entityId);
            if (_currentDetail == null) throw new InvalidOperationException($"未找到ID为{entityId}的病案");
            _originalDetail = CloneMedicalCaseDetail(_currentDetail);
            _logger.LogDebug("[SVC] MedicalCase.Initialize detail - PatientId={PatientId} UserId={UserId} PatientName={PatientName}",
                _currentDetail.PatientId, _currentDetail.UserId, _currentDetail.PatientName);
            _logger.LogInformation("[SVC] MedicalCase.Initialize completed - PatientName={PatientName}", _currentDetail.PatientName);
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Initialize failed - MedicalCaseId={MedicalCaseId}", entityId); throw; }
    }

    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
    public virtual async Task<bool> SaveAsync()
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
            _originalDetail = CloneMedicalCaseDetail(_currentDetail);
            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            return true;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Save failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id); return false; }
    }

    public virtual async Task<bool> DeleteAsync()
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

    public virtual async Task ReloadAsync()
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

    public virtual async Task<MedicalCaseDetailDto?> UpdateSimpleAsync(MedicalCaseInputDto dto)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateSimple started - MedicalCaseId={MedicalCaseId}", dto.Id);
            var result = await _repository.UpdateAsync(dto);
            _logger.LogInformation("[SVC] MedicalCase.UpdateSimple completed - MedicalCaseId={MedicalCaseId}", dto.Id);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.UpdateSimple failed - MedicalCaseId={MedicalCaseId}", dto.Id); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> CreateAsync(MedicalCaseInputDto dto)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.Create started - PatientId={PatientId}", dto.PatientId);
            var created = await _repository.CreateAsync(dto);
            _logger.LogInformation("[SVC] MedicalCase.Create completed - MedicalCaseId={MedicalCaseId}", created.Id);
            return created;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Create failed - PatientId={PatientId}", dto.PatientId); return null; }
    }

    // OpenSpec: consolidate-medicalcase-detail-queries - GetByIdWithDetailsAsync已删除，使用GetByIdAsync
    public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null)
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
    public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query)
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

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.DeleteById started - MedicalCaseId={MedicalCaseId}", id);
            var result = await _repository.DeleteAsync(id);
            if (result)
                _logger.LogInformation("[SVC] MedicalCase.DeleteById completed - MedicalCaseId={MedicalCaseId}", id);
            else
                _logger.LogWarning("[SVC] MedicalCase.DeleteById → NotFound - MedicalCaseId={MedicalCaseId}", id);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.DeleteById failed - MedicalCaseId={MedicalCaseId}", id); return false; }
    }

    public virtual async Task<PagedResult<MedicalCaseDetailDto>?> SearchAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.Search started - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword}", patientName, diagnosisKeyword);
            var result = await _repository.SearchAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
            _logger.LogDebug("[SVC] MedicalCase.Search completed - TotalCount={TotalCount}", result?.TotalCount ?? 0);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.Search failed"); return null; }
    }

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
            var result = await _api.SetPrescriptionFlagAsync(medicalCaseId, request);
            _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SetPrescriptionFlag failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
    // - ClearPrescriptionAsync: Server端从未实现
    // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CloseCase started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            // OpenSpec: optimize-medicalcase-api - 返回完整医案详情
            var result = await _api.CloseCaseAsync(medicalCaseId);
            _logger.LogInformation("[SVC] MedicalCase.CloseCase completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CloseCase failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    // OpenSpec: consolidate-medicalcase-detail-queries - 使用QueryAsync替代废弃的GetUnfinishedCaseByPatientIdAsync
    public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false)
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
            var response = await _api.DeleteMedicalCaseAsync(medicalCaseId);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SVC] MedicalCase.DeleteViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse { Success = true, Message = "医案已取消" };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.DeleteViaApi → Failed - MedicalCaseId={MedicalCaseId} Reason={Reason}", medicalCaseId, response.ReasonPhrase);
                return new ApiResponse { Success = false, Message = $"删除失败: {response.ReasonPhrase}" };
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
            var result = await _api.UpdateStatusAsync(medicalCaseId, request);
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.UpdateStatus failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SaveDraftViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SaveDraftViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var result = await _api.SaveDraftAsync(medicalCaseId, consultationData);
            _logger.LogInformation("[SVC] MedicalCase.SaveDraftViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SaveDraftViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CancelViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var request = string.IsNullOrEmpty(reason) ? null : new CancelMedicalCaseRequestDto { Reason = reason };
            var result = await _api.CancelMedicalCaseAsync(medicalCaseId, request);
            _logger.LogInformation("[SVC] MedicalCase.CancelViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CancelViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    #endregion

    #region 聚合根专用方法

    public void UpdateConsultation(ConsultationDetailDto consultation)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
        _logger.LogDebug("[SVC] MedicalCase.UpdateConsultation - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
    }

    /// <summary>
    /// 创建处方（通过聚合保存）
    /// OpenSpec: simplify-medicalcase-api - 通过SaveAsync创建处方
    /// </summary>
    public virtual async Task<PrescriptionDetailDto?> CreatePrescriptionAsync(PrescriptionInputDto createDto)
    {
        if (_currentDetail == null) { _logger.LogWarning("[SVC] MedicalCase.CreatePrescription → NoData"); return null; }
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CreatePrescription started - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            // 构建包含Prescription的InputDto
            var inputDto = _currentDetail.ToInputDto();
            inputDto.Prescription = createDto;
            inputDto.NeedsPrescription = true;

            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated?.Prescription != null)
            {
                _currentDetail.Prescription = updated.Prescription;
                _currentDetail.PrescriptionId = updated.PrescriptionId;
                _logger.LogInformation("[SVC] MedicalCase.CreatePrescription completed - PrescriptionId={PrescriptionId}", updated.Prescription.Id);
                return updated.Prescription;
            }
            _logger.LogWarning("[SVC] MedicalCase.CreatePrescription → NoPrescriptionReturned - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            return null;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CreatePrescription failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id); return null; }
    }

    public void UpdatePrescription(PrescriptionDetailDto prescription)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        _logger.LogDebug("[SVC] MedicalCase.UpdatePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}", _currentDetail.Id, prescription.Id);
    }

    /// <summary>
    /// 删除处方（通过聚合保存设置NeedsPrescription=false）
    /// OpenSpec: simplify-medicalcase-api - 通过SaveAsync删除处方
    /// </summary>
    public virtual async Task<bool> DeletePrescriptionAsync()
    {
        if (_currentDetail == null) { _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → NoData"); return false; }
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.DeletePrescription started - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            // 设置NeedsPrescription=false触发服务端软删除
            var inputDto = _currentDetail.ToInputDto();
            inputDto.NeedsPrescription = false;
            inputDto.Prescription = null;

            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated != null)
            {
                _currentDetail.Prescription = null;
                _currentDetail.PrescriptionId = null;
                _logger.LogInformation("[SVC] MedicalCase.DeletePrescription completed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
                return true;
            }
            _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → Failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id);
            return false;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.DeletePrescription failed - MedicalCaseId={MedicalCaseId}", _currentDetail.Id); return false; }
    }

    #endregion

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

    #endregion

    #region 私有方法 - 深拷贝

    // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
    private MedicalCaseDetailDto CloneMedicalCaseDetail(MedicalCaseDetailDto s) => new()
    {
        Id = s.Id, CaseNumber = s.CaseNumber, PatientId = s.PatientId,
        PatientName = s.PatientName, PatientGender = s.PatientGender, PatientAge = s.PatientAge,
        UserId = s.UserId, DoctorName = s.DoctorName, ConsultationId = s.ConsultationId,
        PrescriptionId = s.PrescriptionId, CaseStatus = s.CaseStatus,
        Remark = s.Remark, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt,
        Consultation = s.Consultation != null ? CloneConsultation(s.Consultation) : null,
        Prescription = s.Prescription != null ? ClonePrescription(s.Prescription) : null
    };

    // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
    private ConsultationDetailDto CloneConsultation(ConsultationDetailDto s) => new()
    {
        Id = s.Id, MedicalCaseId = s.MedicalCaseId, PatientId = s.PatientId, UserId = s.UserId,
        PatientName = s.PatientName, DoctorName = s.DoctorName,
        PresentIllness = s.PresentIllness, TongueDiagnosis = s.TongueDiagnosis,
        PulseDiagnosis = s.PulseDiagnosis, TcmDiagnosis = s.TcmDiagnosis,
        CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt
    };

    // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除，通过MedicalCaseId关联获取
    private PrescriptionDetailDto ClonePrescription(PrescriptionDetailDto s) => new()
    {
        Id = s.Id, PrescriptionNumber = s.PrescriptionNumber, MedicalCaseId = s.MedicalCaseId,
        DosageCount = s.DosageCount,
        Usage = s.Usage, Discount = s.Discount, Advice = s.Advice,
        ReferencedFormulas = s.ReferencedFormulas, Remark = s.Remark, SingleDosePrice = s.SingleDosePrice,
        TotalPrice = s.TotalPrice, TotalWeight = s.TotalWeight, Status = s.Status,
        CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt, Items = s.Items
    };

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
    public virtual async Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CreateNew started - PatientId={PatientId}", patientId);

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
                Remark = null
            };

            var createdDto = await CreateAsync(createDto);
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
    /// 暂存医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> SaveDraftAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SaveDraft started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var response = await SaveDraftViaApiAsync(medicalCaseId);
            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "暂存医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.SaveDraft completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.SaveDraft failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("暂存", ex));
        }
    }

    /// <summary>
    /// 取消医案
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null)
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
    public virtual async Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId)
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
    /// 恢复暂存医案为Active状态
    /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
    /// </summary>
    public virtual async Task<(bool success, string? errorMessage)> ResumeDraftAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.ResumeDraft started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var request = new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Active,
                StatusChangeReason = null
            };
            var response = await UpdateStatusAsync(medicalCaseId, request);

            if (!response.Success)
            {
                _logger.LogWarning("[SVC] MedicalCase.ResumeDraft → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "恢复医案失败");
            }

            _logger.LogInformation("[SVC] MedicalCase.ResumeDraft completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.ResumeDraft failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复", ex));
        }
    }

    #endregion
}
