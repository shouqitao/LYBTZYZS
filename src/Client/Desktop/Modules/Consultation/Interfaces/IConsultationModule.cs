using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊模块接口 - UltraThink三层架构模块层
/// 职责：统一模块入口，事件管理，模块间协调
/// </summary>
public interface IConsultationModule : IConsultationService, IDisposable
{
    #region 事件定义

    /// <summary>
    /// 看诊状态变更事件
    /// </summary>
    event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;

    /// <summary>
    /// 看诊操作事件
    /// </summary>
    event EventHandler<ConsultationOperationEventArgs>? ConsultationOperation;

    /// <summary>
    /// 诊断更新事件
    /// </summary>
    event EventHandler<DiagnosisUpdatedEventArgs>? DiagnosisUpdated;

    /// <summary>
    /// 四诊记录事件
    /// </summary>
    event EventHandler<FourDiagnosisRecordedEventArgs>? FourDiagnosisRecorded;

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 获取看诊统计摘要
    /// </summary>
    Task<ServiceResult<ConsultationStatisticsSummaryDto>> GetStatisticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取四诊详细信息
    /// </summary>
    Task<ServiceResult<FourDiagnosisDetailDto>> GetFourDiagnosisDetailAsync(Guid consultationId);

    /// <summary>
    /// 保存完整四诊记录
    /// </summary>
    Task<ServiceResult<bool>> SaveCompleteFourDiagnosisAsync(Guid consultationId, CompleteFourDiagnosisDto fourDiagnosisData);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    Task<ServiceResult<DoctorWorkStatisticsDto>> GetDoctorWorkStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 批量更新看诊状态
    /// </summary>
    Task<ServiceResult<ConsultationBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> consultationIds, ConsultationStatus status);

    /// <summary>
    /// 获取患者看诊趋势
    /// </summary>
    Task<ServiceResult<List<PatientConsultationTrendDto>>> GetPatientConsultationTrendAsync(Guid patientId, int months = 6);

    /// <summary>
    /// 智能诊断建议
    /// </summary>
    Task<ServiceResult<List<DiagnosisSuggestionDto>>> GetDiagnosisSuggestionsAsync(FourDiagnosisDataDto fourDiagnosisData);

    /// <summary>
    /// 获取看诊模板
    /// </summary>
    Task<ServiceResult<List<ConsultationTemplateDto>>> GetConsultationTemplatesAsync(string? category = null);

    #endregion
}

/// <summary>
/// 看诊状态变更事件参数
/// </summary>
public class ConsultationStatusChangedEventArgs : EventArgs
{
    public Guid ConsultationId { get; set; }
    public ConsultationStatus OldStatus { get; set; }
    public ConsultationStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
}

/// <summary>
/// 看诊操作事件参数
/// </summary>
public class ConsultationOperationEventArgs : EventArgs
{
    public Guid ConsultationId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string OperationDetails { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 诊断更新事件参数
/// </summary>
public class DiagnosisUpdatedEventArgs : EventArgs
{
    public Guid ConsultationId { get; set; }
    public string? OldDiagnosis { get; set; }
    public string NewDiagnosis { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
}

/// <summary>
/// 四诊记录事件参数
/// </summary>
public class FourDiagnosisRecordedEventArgs : EventArgs
{
    public Guid ConsultationId { get; set; }
    public string DiagnosisType { get; set; } = string.Empty;  // Inspection, Auscultation, Inquiry, Palpation
    public string Content { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
}

/// <summary>
/// 看诊状态枚举
/// </summary>
public enum ConsultationStatus
{
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// 看诊统计摘要DTO
/// </summary>
public class ConsultationStatisticsSummaryDto
{
    public int TotalConsultations { get; set; }
    public int CompletedConsultations { get; set; }
    public int InProgressConsultations { get; set; }
    public int CancelledConsultations { get; set; }
    public decimal AverageConsultationDuration { get; set; }
    public List<string> TopDiagnoses { get; set; } = new();
    public Dictionary<string, int> DailyConsultationCounts { get; set; } = new();
}

/// <summary>
/// 四诊详细信息DTO
/// </summary>
public class FourDiagnosisDetailDto
{
    public Guid ConsultationId { get; set; }
    public string Inspection { get; set; } = string.Empty;        // 望诊
    public string Auscultation { get; set; } = string.Empty;      // 闻诊
    public string Inquiry { get; set; } = string.Empty;          // 问诊
    public string Palpation { get; set; } = string.Empty;        // 切诊
    public DateTime LastUpdatedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

/// <summary>
/// 完整四诊记录DTO
/// </summary>
public class CompleteFourDiagnosisDto
{
    public string Inspection { get; set; } = string.Empty;        // 望诊：面色、神态、体型等
    public string Auscultation { get; set; } = string.Empty;      // 闻诊：语音、呼吸、咳嗽、体味等
    public string Inquiry { get; set; } = string.Empty;          // 问诊：主诉、现病史、既往史等
    public string Palpation { get; set; } = string.Empty;        // 切诊：脉象、按诊等
    public string? ChiefComplaint { get; set; }                  // 主诉
    public string? Diagnosis { get; set; }                       // 诊断
    public string? TreatmentPlan { get; set; }                   // 治疗方案
    public string? Remarks { get; set; }                         // 备注
}

/// <summary>
/// 医生工作统计DTO
/// </summary>
public class DoctorWorkStatisticsDto
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int TotalConsultations { get; set; }
    public int CompletedConsultations { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageConsultationTime { get; set; }
    public List<string> SpecialtyDiagnoses { get; set; } = new();
    public int TotalPatients { get; set; }
    public List<ConsultationDailyStatDto> DailyStats { get; set; } = new();
}

/// <summary>
/// 看诊批量操作结果DTO
/// </summary>
public class ConsultationBatchOperationResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<Guid> SuccessfulIds { get; set; } = new();
    public List<Guid> FailedIds { get; set; } = new();
}

/// <summary>
/// 患者看诊趋势DTO
/// </summary>
public class PatientConsultationTrendDto
{
    public DateTime Month { get; set; }
    public int ConsultationCount { get; set; }
    public List<string> MainComplaints { get; set; } = new();
    public string? TrendDirection { get; set; }  // "Improving", "Stable", "Worsening"
}

/// <summary>
/// 诊断建议DTO
/// </summary>
public class DiagnosisSuggestionDto
{
    public string Diagnosis { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> RecommendedTreatments { get; set; } = new();
    public List<string> RelatedSymptoms { get; set; } = new();
}

/// <summary>
/// 四诊数据DTO
/// </summary>
public class FourDiagnosisDataDto
{
    public string Inspection { get; set; } = string.Empty;
    public string Auscultation { get; set; } = string.Empty;
    public string Inquiry { get; set; } = string.Empty;
    public string Palpation { get; set; } = string.Empty;
    public string? ChiefComplaint { get; set; }
}

/// <summary>
/// 看诊模板DTO
/// </summary>
public class ConsultationTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CompleteFourDiagnosisDto Template { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 看诊日统计DTO
/// </summary>
public class ConsultationDailyStatDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal AverageTime { get; set; }
    public int CompletedCount { get; set; }
}