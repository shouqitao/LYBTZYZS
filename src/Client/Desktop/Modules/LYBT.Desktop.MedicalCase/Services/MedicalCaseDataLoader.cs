// MedicalCaseDataManager now in same namespace (Services)
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案数据加载器 - 负责医案数据加载、患者信息格式化和数据缓存
/// Issue #1806: 从MedicalCaseFlowViewModel提取数据加载逻辑(~150行)
/// </summary>
public class MedicalCaseDataLoader
{
    private readonly MedicalCaseService _dataManager;
    private readonly ILogger<MedicalCaseDataLoader> _logger;

    /// <summary>
    /// 缓存的医案详情
    /// </summary>
    public MedicalCaseDetailDto? CachedMedicalCase { get; private set; }

    /// <summary>
    /// 缓存的诊疗记录
    /// </summary>
    public ConsultationDetailDto? CachedConsultation { get; private set; }

    /// <summary>
    /// 缓存的处方信息
    /// </summary>
    public PrescriptionDetailDto? CachedPrescription { get; private set; }

    /// <summary>
    /// 数据加载完成事件
    /// </summary>
    public event EventHandler<DataLoadedEventArgs>? DataLoaded;

    public MedicalCaseDataLoader(
        MedicalCaseService dataManager,
        ILogger<MedicalCaseDataLoader> logger)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 加载医案详情及关联数据
    /// </summary>
    public async Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)> LoadMedicalCaseDetailsAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("开始加载医案详情，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // 加载完整医案数据
            var medicalCaseDetail = await _dataManager.GetByIdWithDetailsAsync(medicalCaseId);

            if (medicalCaseDetail == null)
            {
                _logger.LogWarning("未找到医案数据，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return (false, null, "未找到医案数据");
            }

            // 缓存数据
            CachedMedicalCase = medicalCaseDetail;
            CachedConsultation = medicalCaseDetail.Consultation;
            CachedPrescription = medicalCaseDetail.Prescription;

            // 记录加载结果
            if (CachedConsultation != null)
            {
                _logger.LogInformation("已加载诊疗记录，ConsultationId: {ConsultationId}", CachedConsultation.Id);
            }
            else
            {
                _logger.LogInformation("无诊疗记录数据");
            }

            if (CachedPrescription != null)
            {
                _logger.LogInformation("已加载处方信息，PrescriptionId: {PrescriptionId}", CachedPrescription.Id);
            }
            else
            {
                _logger.LogInformation("无处方数据");
            }

            _logger.LogInformation("医案数据加载完成");

            // 触发事件
            DataLoaded?.Invoke(this, new DataLoadedEventArgs
            {
                Success = true,
                MedicalCaseId = medicalCaseId,
                HasConsultation = CachedConsultation != null,
                HasPrescription = CachedPrescription != null
            });

            return (true, medicalCaseDetail, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载医案数据失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            var errorMsg = ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex);

            // 触发事件
            DataLoaded?.Invoke(this, new DataLoadedEventArgs
            {
                Success = false,
                MedicalCaseId = medicalCaseId,
                ErrorMessage = errorMsg
            });

            return (false, null, errorMsg);
        }
    }

    /// <summary>
    /// 格式化患者信息为显示文本
    /// </summary>
    public (string patientName, string patientInfo) FormatPatientInfo(PatientDetailDto patient)
    {
        if (patient == null)
        {
            _logger.LogWarning("患者数据为null，无法格式化");
            return (string.Empty, string.Empty);
        }

        var patientName = patient.Name;
        var patientInfo = $"{patient.Gender} | {patient.Age}岁 | {patient.PhoneNumber}";

        _logger.LogInformation("格式化患者信息：{PatientName} - {PatientInfo}", patientName, patientInfo);

        return (patientName, patientInfo);
    }

    /// <summary>
    /// 清除所有缓存数据
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("清除数据缓存");
        CachedMedicalCase = null;
        CachedConsultation = null;
        CachedPrescription = null;
    }

    /// <summary>
    /// 获取缓存的诊疗记录（如果存在）
    /// </summary>
    public ConsultationDetailDto? GetCachedConsultation()
    {
        return CachedConsultation;
    }

    /// <summary>
    /// 获取缓存的处方信息（如果存在）
    /// </summary>
    public PrescriptionDetailDto? GetCachedPrescription()
    {
        return CachedPrescription;
    }

    /// <summary>
    /// 检查是否有缓存的医案数据
    /// </summary>
    public bool HasCachedData()
    {
        return CachedMedicalCase != null;
    }

    /// <summary>
    /// 获取缓存的医案ID（如果存在）
    /// </summary>
    public Guid? GetCachedMedicalCaseId()
    {
        return CachedMedicalCase?.Id;
    }
}

/// <summary>
/// 数据加载完成事件参数
/// </summary>
public class DataLoadedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public Guid MedicalCaseId { get; set; }
    public bool HasConsultation { get; set; }
    public bool HasPrescription { get; set; }
    public string? ErrorMessage { get; set; }
}
