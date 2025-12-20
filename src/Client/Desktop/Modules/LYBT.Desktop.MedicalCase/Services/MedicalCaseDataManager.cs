using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 病案数据管理器 - 聚合根模式实现
/// 聚合根: 管理MedicalCase、Consultation、Prescription三个实体
/// OpenSpec: simplify-medicalcase-api - 统一管理Consultation和Prescription
/// </summary>
public class MedicalCaseDataManager : IMedicalCaseDataManager
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMedicalCaseApi _api;
    private readonly ILogger<MedicalCaseDataManager> _logger;
    private MedicalCaseDetailDto? _originalDetail;
    private MedicalCaseDetailDto? _currentDetail;

    public MedicalCaseDataManager(IMedicalCaseRepository repository, IMedicalCaseApi api, ILogger<MedicalCaseDataManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    public async Task InitializeAsync(Guid entityId)
    {
        try
        {
            _logger.LogInformation("加载病案聚合根: {Id}", entityId);
            _currentDetail = await _repository.GetByIdWithDetailsAsync(entityId);
            if (_currentDetail == null) throw new InvalidOperationException($"未找到ID为{entityId}的病案");
            _originalDetail = CloneMedicalCaseDetail(_currentDetail);
            // 调试日志：追踪400错误原因 - 验证服务器返回的UserId
            _logger.LogInformation("[调试] 加载的病案 - PatientId: {PatientId}, UserId: {UserId}, PatientName: {PatientName}",
                _currentDetail.PatientId, _currentDetail.UserId, _currentDetail.PatientName);
            _logger.LogInformation("病案加载成功: {PatientName}", _currentDetail.PatientName);
        }
        catch (Exception ex) { _logger.LogError(ex, "加载病案失败: {Id}", entityId); throw; }
    }

    public virtual async Task<bool> SaveAsync()
    {
        if (_currentDetail == null) { _logger.LogWarning("无法保存：当前病案数据为空"); return false; }
        if (!HasChanges) { _logger.LogInformation("无变更，跳过保存"); return true; }

        try
        {
            _logger.LogInformation("保存病案: {Id}", _currentDetail.Id);
            // OpenSpec: simplify-medicalcase-api - 通过聚合保存一次性更新MedicalCase+Consultation+Prescription
            var inputDto = _currentDetail.ToInputDto();
            // 调试日志：追踪400错误原因
            _logger.LogInformation("[调试] InputDto - Id: {Id}, PatientId: {PatientId}, UserId: {UserId}",
                inputDto.Id, inputDto.PatientId, inputDto.UserId);
            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated != null)
            {
                UpdateMedicalCaseFields(_currentDetail, updated);
                if (updated.Consultation != null) _currentDetail.Consultation = updated.Consultation;
                if (updated.Prescription != null) _currentDetail.Prescription = updated.Prescription;
            }
            _originalDetail = CloneMedicalCaseDetail(_currentDetail);
            _logger.LogInformation("保存成功");
            return true;
        }
        catch (Exception ex) { _logger.LogError(ex, "保存失败: {Id}", _currentDetail.Id); return false; }
    }

    public virtual async Task<bool> DeleteAsync()
    {
        if (_currentDetail == null) return false;
        try
        {
            var result = await _repository.DeleteAsync(_currentDetail.Id);
            if (result) { _currentDetail = null; _originalDetail = null; }
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "删除失败: {Id}", _currentDetail?.Id ?? Guid.Empty); return false; }
    }

    public virtual async Task ReloadAsync()
    {
        if (_currentDetail != null) await InitializeAsync(_currentDetail.Id);
    }

    #endregion

    #region 简单CRUD方法

    public virtual async Task<MedicalCaseDetailDto?> GetByIdSimpleAsync(Guid id)
    {
        try { return await _repository.GetByIdAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "获取病案失败: {Id}", id); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> UpdateSimpleAsync(MedicalCaseInputDto dto)
    {
        try { return await _repository.UpdateAsync(dto); }
        catch (Exception ex) { _logger.LogError(ex, "更新病案失败: {Id}", dto.Id); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> CreateAsync(MedicalCaseInputDto dto)
    {
        try { var created = await _repository.CreateAsync(dto); _logger.LogInformation("医案创建成功: {Id}", created.Id); return created; }
        catch (Exception ex) { _logger.LogError(ex, "创建医案失败: PatientId={PatientId}", dto.PatientId); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id)
    {
        try { return await _repository.GetByIdWithDetailsAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "获取医案详情失败: {Id}", id); return null; }
    }

    public virtual async Task<PagedResult<MedicalCaseDetailDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null)
    {
        try { return await _repository.GetPagedAsync(page, pageSize, searchText); }
        catch (Exception ex) { _logger.LogError(ex, "分页获取医案失败: Page={Page}", page); return null; }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try { return await _repository.DeleteAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "删除医案失败: {Id}", id); return false; }
    }

    public virtual async Task<List<MedicalCaseDetailDto>?> QueryAsync(string? patientName = null, DateTime? startDate = null, DateTime? endDate = null, string? diagnosisKeyword = null)
    {
        try { return await _repository.QueryAsync(patientName, startDate, endDate, diagnosisKeyword); }
        catch (Exception ex) { _logger.LogError(ex, "查询医案失败"); return null; }
    }

    #endregion

    #region 业务命令方法（API-based）

    // OpenSpec: simplify-medicalcase-api - UpdateConsultationAsync已删除
    // 诊断更新通过聚合保存 SaveAsync 处理

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
    {
        try { return await _api.SetPrescriptionFlagAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "设置处方标志失败: {Id}", medicalCaseId); throw; }
    }

    // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
    // - ClearPrescriptionAsync: Server端从未实现
    // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

    public virtual async Task<ApiResponse> CloseCaseAsync(Guid medicalCaseId)
    {
        try { return await _api.CloseCaseAsync(medicalCaseId); }
        catch (Exception ex) { _logger.LogError(ex, "关闭病案失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false)
    {
        try { return await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors); }
        catch (Exception ex) { _logger.LogError(ex, "获取未完成病案失败: PatientId={PatientId}", patientId); throw; }
    }

    // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD方法已删除
    // - CreatePrescriptionViaApiAsync: 通过SaveAsync创建
    // - UpdatePrescriptionViaApiAsync: 通过SaveAsync更新
    // - DeletePrescriptionViaApiAsync: 通过SaveAsync设置NeedsPrescription=false触发

    public virtual async Task<ApiResponse> DeleteMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            var response = await _api.DeleteMedicalCaseAsync(medicalCaseId);
            return response.IsSuccessStatusCode
                ? new ApiResponse { Success = true, Message = "医案已取消" }
                : new ApiResponse { Success = false, Message = $"删除失败: {response.ReasonPhrase}" };
        }
        catch (Exception ex) { _logger.LogError(ex, "删除医案失败: {Id}", medicalCaseId); return new ApiResponse { Success = false, Message = Infrastructure.Localization.ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex) }; }
    }

    // ========== SoftDeleteMedicalCaseAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
    // Server端点DELETE /api/v1/medicalcases/{id}/soft 不存在，使用DeleteMedicalCaseAsync代替

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatusInputDto request)
    {
        try { return await _api.UpdateStatusAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "更新状态失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SaveDraftViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)
    {
        try { return await _api.SaveDraftAsync(medicalCaseId, consultationData); }
        catch (Exception ex) { _logger.LogError(ex, "暂存医案失败(API): {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)
    {
        try
        {
            var request = string.IsNullOrEmpty(reason) ? null : new CancelMedicalCaseRequestDto { Reason = reason };
            return await _api.CancelMedicalCaseAsync(medicalCaseId, request);
        }
        catch (Exception ex) { _logger.LogError(ex, "取消医案失败(API): {Id}", medicalCaseId); throw; }
    }

    #endregion

    #region 聚合根专用方法

    public void UpdateConsultation(ConsultationDetailDto consultation)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
    }

    /// <summary>
    /// 创建处方（通过聚合保存）
    /// OpenSpec: simplify-medicalcase-api - 通过SaveAsync创建处方
    /// </summary>
    public virtual async Task<PrescriptionDetailDto?> CreatePrescriptionAsync(PrescriptionInputDto createDto)
    {
        if (_currentDetail == null) return null;
        try
        {
            // 构建包含Prescription的InputDto
            var inputDto = _currentDetail.ToInputDto();
            inputDto.Prescription = createDto;
            inputDto.NeedsPrescription = true;

            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated?.Prescription != null)
            {
                _currentDetail.Prescription = updated.Prescription;
                _currentDetail.PrescriptionId = updated.PrescriptionId;
                return updated.Prescription;
            }
            return null;
        }
        catch (Exception ex) { _logger.LogError(ex, "创建处方失败"); return null; }
    }

    public void UpdatePrescription(PrescriptionDetailDto prescription)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
    }

    /// <summary>
    /// 删除处方（通过聚合保存设置NeedsPrescription=false）
    /// OpenSpec: simplify-medicalcase-api - 通过SaveAsync删除处方
    /// </summary>
    public virtual async Task<bool> DeletePrescriptionAsync()
    {
        if (_currentDetail == null) return false;
        try
        {
            // 设置NeedsPrescription=false触发服务端软删除
            var inputDto = _currentDetail.ToInputDto();
            inputDto.NeedsPrescription = false;
            inputDto.Prescription = null;

            var updated = await _repository.SaveAsync(_currentDetail.Id, inputDto);
            if (updated != null)
            {
                _currentDetail.Prescription = null;
                _currentDetail.PrescriptionId = null;
                return true;
            }
            return false;
        }
        catch (Exception ex) { _logger.LogError(ex, "删除处方失败"); return false; }
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
               c.TCMDiagnosis != o.TCMDiagnosis;
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
        PulseDiagnosis = s.PulseDiagnosis, TCMDiagnosis = s.TCMDiagnosis,
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
}
