using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Components;

/// <summary>
/// 病案数据管理器 - 聚合根模式实现
/// 聚合根: 管理MedicalCase、Consultation、Prescription三个实体
/// </summary>
public class MedicalCaseDataManager : IDataManager<MedicalCaseDto>
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

    public virtual MedicalCaseDto? Current => _currentDetail;
    public virtual ConsultationDto? CurrentConsultation => _currentDetail?.Consultation;
    public virtual PrescriptionDto? CurrentPrescription => _currentDetail?.Prescription;

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
            if (IsMedicalCaseChanged())
            {
                var updated = await _repository.UpdateAsync(_currentDetail.ToInputDto());
                if (updated != null) UpdateMedicalCaseFields(_currentDetail, updated);
            }
            if (IsConsultationChanged() && _currentDetail.Consultation != null)
            {
                var updated = await _repository.UpdateConsultationAsync(_currentDetail.Id, _currentDetail.Consultation.ToInputDto());
                if (updated != null) _currentDetail.Consultation = updated;
            }
            if (IsPrescriptionChanged() && _currentDetail.Prescription != null)
            {
                var updated = await _repository.UpdatePrescriptionAsync(_currentDetail.Id, _currentDetail.Prescription.ToUpdateDto());
                if (updated != null) _currentDetail.Prescription = updated;
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

    public virtual async Task<MedicalCaseDto?> GetByIdSimpleAsync(Guid id)
    {
        try { return await _repository.GetByIdAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "获取病案失败: {Id}", id); return null; }
    }

    public virtual async Task<MedicalCaseDto?> UpdateSimpleAsync(MedicalCaseInputDto dto)
    {
        try { return await _repository.UpdateAsync(dto); }
        catch (Exception ex) { _logger.LogError(ex, "更新病案失败: {Id}", dto.Id); return null; }
    }

    public virtual async Task<MedicalCaseDto?> CreateAsync(MedicalCaseInputDto dto)
    {
        try { var created = await _repository.CreateAsync(dto); _logger.LogInformation("医案创建成功: {Id}", created.Id); return created; }
        catch (Exception ex) { _logger.LogError(ex, "创建医案失败: PatientId={PatientId}", dto.PatientId); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id)
    {
        try { return await _repository.GetByIdWithDetailsAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "获取医案详情失败: {Id}", id); return null; }
    }

    public virtual async Task<PagedResult<MedicalCaseDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null)
    {
        try { return await _repository.GetPagedAsync(page, pageSize, searchText); }
        catch (Exception ex) { _logger.LogError(ex, "分页获取医案失败: Page={Page}", page); return null; }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try { return await _repository.DeleteAsync(id); }
        catch (Exception ex) { _logger.LogError(ex, "删除医案失败: {Id}", id); return false; }
    }

    public virtual async Task<List<MedicalCaseDto>?> QueryAsync(string? patientName = null, DateTime? startDate = null, DateTime? endDate = null, string? diagnosisKeyword = null)
    {
        try { return await _repository.QueryAsync(patientName, startDate, endDate, diagnosisKeyword); }
        catch (Exception ex) { _logger.LogError(ex, "查询医案失败"); return null; }
    }

    #endregion

    #region 业务命令方法（API-based）

    public virtual async Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto request)
    {
        try { return await _api.UpdateConsultationAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "更新诊疗失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
    {
        try { return await _api.SetPrescriptionFlagAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "设置处方标志失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse> ClearPrescriptionAsync(Guid medicalCaseId)
    {
        try { return await _api.ClearPrescriptionAsync(medicalCaseId); }
        catch (Exception ex) { _logger.LogError(ex, "清空处方失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId)
    {
        try { return await _api.ImportFormulaIntoPrescriptionAsync(medicalCaseId, formulaId); }
        catch (Exception ex) { _logger.LogError(ex, "配方导入失败: {Id}, FormulaId={FormulaId}", medicalCaseId, formulaId); throw; }
    }

    public virtual async Task<ApiResponse> CloseCaseAsync(Guid medicalCaseId)
    {
        try { return await _api.CloseCaseAsync(medicalCaseId); }
        catch (Exception ex) { _logger.LogError(ex, "关闭病案失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false)
    {
        try { return await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors); }
        catch (Exception ex) { _logger.LogError(ex, "获取未完成病案失败: PatientId={PatientId}", patientId); throw; }
    }

    public virtual async Task<ApiResponse<PrescriptionDto>> CreatePrescriptionViaApiAsync(Guid medicalCaseId, PrescriptionCreateDto request)
    {
        try { return await _api.CreatePrescriptionAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "创建处方失败(API): {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionViaApiAsync(Guid medicalCaseId, PrescriptionUpdateDto request)
    {
        try { return await _api.UpdatePrescriptionAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "更新处方失败(API): {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse> DeletePrescriptionViaApiAsync(Guid medicalCaseId)
    {
        try { return await _api.DeletePrescriptionAsync(medicalCaseId); }
        catch (Exception ex) { _logger.LogError(ex, "删除处方失败(API): {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse> DeleteMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            var response = await _api.DeleteMedicalCaseAsync(medicalCaseId);
            return response.IsSuccessStatusCode
                ? new ApiResponse { Success = true, Message = "医案已取消" }
                : new ApiResponse { Success = false, Message = $"删除失败: {response.ReasonPhrase}" };
        }
        catch (Exception ex) { _logger.LogError(ex, "删除医案失败: {Id}", medicalCaseId); return new ApiResponse { Success = false, Message = $"删除失败: {ex.Message}" }; }
    }

    public virtual async Task<ApiResponse<ApiResponse>> SoftDeleteMedicalCaseAsync(Guid medicalCaseId)
    {
        try { return await _api.SoftDeleteMedicalCaseAsync(medicalCaseId); }
        catch (Exception ex) { _logger.LogError(ex, "软删除医案失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDto>> UpdateStatusAsync(Guid medicalCaseId, UpdateMedicalCaseStatusDto request)
    {
        try { return await _api.UpdateStatusAsync(medicalCaseId, request); }
        catch (Exception ex) { _logger.LogError(ex, "更新状态失败: {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDto>> SaveDraftViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)
    {
        try { return await _api.SaveDraftAsync(medicalCaseId, consultationData); }
        catch (Exception ex) { _logger.LogError(ex, "暂存医案失败(API): {Id}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)
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

    public void UpdateConsultation(ConsultationDto consultation)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
    }

    public virtual async Task<PrescriptionDto?> CreatePrescriptionAsync(PrescriptionCreateDto createDto)
    {
        if (_currentDetail == null) return null;
        try
        {
            var prescription = await _repository.CreatePrescriptionAsync(_currentDetail.Id, createDto);
            if (prescription != null) _currentDetail.Prescription = prescription;
            return prescription;
        }
        catch (Exception ex) { _logger.LogError(ex, "创建处方失败"); return null; }
    }

    public void UpdatePrescription(PrescriptionDto prescription)
    {
        if (_currentDetail == null) throw new InvalidOperationException("当前病案数据为空");
        _currentDetail.Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
    }

    public virtual async Task<bool> DeletePrescriptionAsync()
    {
        if (_currentDetail?.Prescription == null) return false;
        try { await _repository.DeletePrescriptionAsync(_currentDetail.Id); _currentDetail.Prescription = null; return true; }
        catch (Exception ex) { _logger.LogError(ex, "删除处方失败"); return false; }
    }

    #endregion

    #region 私有方法 - 变更检测

    private bool IsMedicalCaseChanged() => _currentDetail != null && _originalDetail != null &&
        (_currentDetail.CaseNumber != _originalDetail.CaseNumber || _currentDetail.ChiefComplaint != _originalDetail.ChiefComplaint ||
         _currentDetail.PatientId != _originalDetail.PatientId || _currentDetail.DoctorId != _originalDetail.DoctorId ||
         _currentDetail.CaseStatus != _originalDetail.CaseStatus || _currentDetail.Remark != _originalDetail.Remark);

    private bool IsConsultationChanged()
    {
        if (_currentDetail?.Consultation == null || _originalDetail?.Consultation == null) return false;
        var c = _currentDetail.Consultation; var o = _originalDetail.Consultation;
        return c.ChiefComplaint != o.ChiefComplaint || c.PresentIllness != o.PresentIllness || c.Inspection != o.Inspection ||
               c.AuscultationOlfaction != o.AuscultationOlfaction || c.Inquiry != o.Inquiry || c.Palpation != o.Palpation ||
               c.TCMDiagnosis != o.TCMDiagnosis || c.TreatmentPrinciple != o.TreatmentPrinciple || c.MedicalAdvice != o.MedicalAdvice || c.Remark != o.Remark;
    }

    private bool IsPrescriptionChanged()
    {
        if (_currentDetail?.Prescription == null || _originalDetail?.Prescription == null) return false;
        var c = _currentDetail.Prescription; var o = _originalDetail.Prescription;
        return c.Indication != o.Indication || c.DosageCount != o.DosageCount || c.Usage != o.Usage ||
               c.Discount != o.Discount || c.Advice != o.Advice || c.Remark != o.Remark;
    }

    #endregion

    #region 私有方法 - 深拷贝

    private MedicalCaseDetailDto CloneMedicalCaseDetail(MedicalCaseDetailDto s) => new()
    {
        Id = s.Id, CaseNumber = s.CaseNumber, ChiefComplaint = s.ChiefComplaint, PatientId = s.PatientId,
        PatientName = s.PatientName, PatientGender = s.PatientGender, PatientAge = s.PatientAge,
        DoctorId = s.DoctorId, DoctorName = s.DoctorName, ConsultationId = s.ConsultationId,
        PrescriptionId = s.PrescriptionId, ConsultationDate = s.ConsultationDate, CaseStatus = s.CaseStatus,
        Remark = s.Remark, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt,
        Consultation = s.Consultation != null ? CloneConsultation(s.Consultation) : null,
        Prescription = s.Prescription != null ? ClonePrescription(s.Prescription) : null
    };

    private ConsultationDto CloneConsultation(ConsultationDto s) => new()
    {
        Id = s.Id, MedicalCaseId = s.MedicalCaseId, PatientId = s.PatientId, UserId = s.UserId,
        PatientName = s.PatientName, DoctorName = s.DoctorName, ChiefComplaint = s.ChiefComplaint,
        PresentIllness = s.PresentIllness, Inspection = s.Inspection, AuscultationOlfaction = s.AuscultationOlfaction,
        Inquiry = s.Inquiry, Palpation = s.Palpation, TCMDiagnosis = s.TCMDiagnosis,
        TreatmentPrinciple = s.TreatmentPrinciple, MedicalAdvice = s.MedicalAdvice, Remark = s.Remark,
        CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt
    };

    private PrescriptionDto ClonePrescription(PrescriptionDto s) => new()
    {
        Id = s.Id, PrescriptionNumber = s.PrescriptionNumber, MedicalCaseId = s.MedicalCaseId,
        PatientId = s.PatientId, UserId = s.UserId, Indication = s.Indication, DosageCount = s.DosageCount,
        Usage = s.Usage, Discount = s.Discount, Advice = s.Advice, FormulaSource = s.FormulaSource,
        ReferencedFormulas = s.ReferencedFormulas, Remark = s.Remark, SingleDosePrice = s.SingleDosePrice,
        TotalPrice = s.TotalPrice, TotalWeight = s.TotalWeight, Status = s.Status,
        CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt, Items = s.Items
    };

    private void UpdateMedicalCaseFields(MedicalCaseDetailDto target, MedicalCaseDto source)
    {
        target.CaseNumber = source.CaseNumber; target.ChiefComplaint = source.ChiefComplaint;
        target.PatientId = source.PatientId; target.PatientName = source.PatientName;
        target.PatientGender = source.PatientGender; target.PatientAge = source.PatientAge;
        target.DoctorId = source.DoctorId; target.DoctorName = source.DoctorName;
        target.ConsultationId = source.ConsultationId; target.PrescriptionId = source.PrescriptionId;
        target.ConsultationDate = source.ConsultationDate; target.CaseStatus = source.CaseStatus;
        target.Remark = source.Remark; target.UpdatedAt = source.UpdatedAt;
    }

    #endregion
}
