using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Models;

/// <summary>
/// 问诊列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用ConsultationDto，实现Desktop层与Shared层的解耦
/// 保持属性名与ConsultationDto一致，确保XAML绑定兼容
/// </summary>
public partial class ConsultationItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private int medicalCaseId;

    [ObservableProperty]
    private int patientId;

    [ObservableProperty]
    private string patientName = string.Empty;

    [ObservableProperty]
    private string patientGender = string.Empty;

    [ObservableProperty]
    private int? patientAge;

    [ObservableProperty]
    private string chiefComplaint = string.Empty;

    [ObservableProperty]
    private string? presentIllness;

    [ObservableProperty]
    private string? pastHistory;

    [ObservableProperty]
    private string? personalHistory;

    [ObservableProperty]
    private string? familyHistory;

    [ObservableProperty]
    private string? allergyHistory;

    // 中医四诊
    [ObservableProperty]
    private string? inspection; // 望诊

    [ObservableProperty]
    private string? auscultation; // 闻诊

    [ObservableProperty]
    private string? inquiry; // 问诊

    [ObservableProperty]
    private string? palpation; // 切诊

    [ObservableProperty]
    private string? tcmDiagnosis; // 中医诊断

    [ObservableProperty]
    private string? syndrome; // 证型

    [ObservableProperty]
    private string? treatmentPrinciple; // 治则

    [ObservableProperty]
    private ConsultationStatus status;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? completedAt;

    [ObservableProperty]
    private int? prescriptionId;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isExpanded;

    /// <summary>
    /// 从ConsultationDto创建ConsultationItem
    /// </summary>
    public static ConsultationItem FromDto(ConsultationDto dto)
    {
        return new ConsultationItem
        {
            Id = dto.Id,
            MedicalCaseId = dto.MedicalCaseId,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = dto.PatientGender ?? string.Empty,
            PatientAge = dto.PatientAge,
            ChiefComplaint = dto.ChiefComplaint,
            PresentIllness = dto.PresentIllness,
            PastHistory = dto.PastHistory,
            PersonalHistory = dto.PersonalHistory,
            FamilyHistory = dto.FamilyHistory,
            AllergyHistory = dto.AllergyHistory,
            Inspection = dto.Inspection,
            Auscultation = dto.Auscultation,
            Inquiry = dto.Inquiry,
            Palpation = dto.Palpation,
            TcmDiagnosis = dto.TcmDiagnosis,
            Syndrome = dto.Syndrome,
            TreatmentPrinciple = dto.TreatmentPrinciple,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt,
            CompletedAt = dto.CompletedAt,
            PrescriptionId = dto.PrescriptionId
        };
    }

    /// <summary>
    /// 转换为ConsultationDto（用于API调用）
    /// </summary>
    public ConsultationDto ToDto()
    {
        return new ConsultationDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId,
            PatientId = PatientId,
            PatientName = PatientName,
            PatientGender = PatientGender,
            PatientAge = PatientAge,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            PastHistory = PastHistory,
            PersonalHistory = PersonalHistory,
            FamilyHistory = FamilyHistory,
            AllergyHistory = AllergyHistory,
            Inspection = Inspection,
            Auscultation = Auscultation,
            Inquiry = Inquiry,
            Palpation = Palpation,
            TcmDiagnosis = TcmDiagnosis,
            Syndrome = Syndrome,
            TreatmentPrinciple = TreatmentPrinciple,
            Status = Status,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            PrescriptionId = PrescriptionId
        };
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        ConsultationStatus.InProgress => "进行中",
        ConsultationStatus.Completed => "已完成",
        ConsultationStatus.Cancelled => "已取消",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        ConsultationStatus.InProgress => "#2196F3",
        ConsultationStatus.Completed => "#4CAF50",
        ConsultationStatus.Cancelled => "#F44336",
        _ => "#757575"
    };

    /// <summary>
    /// 是否进行中
    /// </summary>
    public bool IsInProgress => Status == ConsultationStatus.InProgress;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted => Status == ConsultationStatus.Completed;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool CanEdit => IsInProgress;

    /// <summary>
    /// 是否可开处方
    /// </summary>
    public bool CanCreatePrescription => IsInProgress && !PrescriptionId.HasValue;

    /// <summary>
    /// 四诊是否完整
    /// </summary>
    public bool IsFourDiagnosisComplete =>
        !string.IsNullOrWhiteSpace(Inspection) &&
        !string.IsNullOrWhiteSpace(Auscultation) &&
        !string.IsNullOrWhiteSpace(Inquiry) &&
        !string.IsNullOrWhiteSpace(Palpation);

    /// <summary>
    /// 诊断是否完整
    /// </summary>
    public bool IsDiagnosisComplete =>
        !string.IsNullOrWhiteSpace(TcmDiagnosis) &&
        !string.IsNullOrWhiteSpace(Syndrome) &&
        !string.IsNullOrWhiteSpace(TreatmentPrinciple);

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{PatientName} - {ChiefComplaint} ({StatusText})";

    /// <summary>
    /// 问诊时长（分钟）
    /// </summary>
    public int? DurationMinutes
    {
        get
        {
            if (CompletedAt.HasValue)
            {
                return (int)(CompletedAt.Value - CreatedAt).TotalMinutes;
            }
            else if (IsInProgress)
            {
                return (int)(DateTime.Now - CreatedAt).TotalMinutes;
            }
            return null;
        }
    }
}