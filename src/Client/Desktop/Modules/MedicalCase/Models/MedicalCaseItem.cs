using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Modules.MedicalCase.Models;

/// <summary>
/// 病历列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用MedicalCaseDto，实现Desktop层与Shared层的解耦
/// 保持属性名与MedicalCaseDto一致，确保XAML绑定兼容
/// </summary>
public partial class MedicalCaseItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private int patientId;

    [ObservableProperty]
    private string patientName = string.Empty;

    [ObservableProperty]
    private string patientGender = string.Empty;

    [ObservableProperty]
    private int? patientAge;

    [ObservableProperty]
    private string caseNumber = string.Empty;

    [ObservableProperty]
    private string chiefComplaint = string.Empty;

    [ObservableProperty]
    private string? presentIllness;

    [ObservableProperty]
    private string? diagnosis;

    [ObservableProperty]
    private string? treatmentPlan;

    [ObservableProperty]
    private MedicalCaseStatus status;

    [ObservableProperty]
    private int? consultationId;

    [ObservableProperty]
    private int? prescriptionId;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? completedAt;

    [ObservableProperty]
    private string? completionReason;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isHighlighted;

    [ObservableProperty]
    private bool isExpanded;

    /// <summary>
    /// 从MedicalCaseDto创建MedicalCaseItem
    /// </summary>
    public static MedicalCaseItem FromDto(MedicalCaseDto dto)
    {
        return new MedicalCaseItem
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = dto.PatientGender ?? string.Empty,
            PatientAge = dto.PatientAge,
            CaseNumber = dto.CaseNumber,
            ChiefComplaint = dto.ChiefComplaint,
            PresentIllness = dto.PresentIllness,
            Diagnosis = dto.Diagnosis,
            TreatmentPlan = dto.TreatmentPlan,
            Status = dto.Status,
            ConsultationId = dto.ConsultationId,
            PrescriptionId = dto.PrescriptionId,
            CreatedAt = dto.CreatedAt,
            CompletedAt = dto.CompletedAt,
            CompletionReason = dto.CompletionReason
        };
    }

    /// <summary>
    /// 转换为MedicalCaseDto（用于API调用）
    /// </summary>
    public MedicalCaseDto ToDto()
    {
        return new MedicalCaseDto
        {
            Id = Id,
            PatientId = PatientId,
            PatientName = PatientName,
            PatientGender = PatientGender,
            PatientAge = PatientAge,
            CaseNumber = CaseNumber,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            Diagnosis = Diagnosis,
            TreatmentPlan = TreatmentPlan,
            Status = Status,
            ConsultationId = ConsultationId,
            PrescriptionId = PrescriptionId,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            CompletionReason = CompletionReason
        };
    }

    /// <summary>
    /// 从MedicalCaseDto更新当前项
    /// </summary>
    public void UpdateFromDto(MedicalCaseDto dto)
    {
        Id = dto.Id;
        PatientId = dto.PatientId;
        PatientName = dto.PatientName ?? string.Empty;
        PatientGender = dto.PatientGender ?? string.Empty;
        PatientAge = dto.PatientAge;
        CaseNumber = dto.CaseNumber;
        ChiefComplaint = dto.ChiefComplaint;
        PresentIllness = dto.PresentIllness;
        Diagnosis = dto.Diagnosis;
        TreatmentPlan = dto.TreatmentPlan;
        Status = dto.Status;
        ConsultationId = dto.ConsultationId;
        PrescriptionId = dto.PrescriptionId;
        CreatedAt = dto.CreatedAt;
        CompletedAt = dto.CompletedAt;
        CompletionReason = dto.CompletionReason;
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        MedicalCaseStatus.Active => "进行中",
        MedicalCaseStatus.Closed => "已完成",
        MedicalCaseStatus.Cancelled => "已取消",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色（用于UI绑定）
    /// </summary>
    public string StatusColor => Status switch
    {
        MedicalCaseStatus.Active => "#4CAF50",
        MedicalCaseStatus.Closed => "#9E9E9E",
        MedicalCaseStatus.Cancelled => "#F44336",
        _ => "#757575"
    };

    /// <summary>
    /// 是否为活动状态
    /// </summary>
    public bool IsActive => Status == MedicalCaseStatus.Active;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted => Status == MedicalCaseStatus.Closed;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool CanEdit => IsActive;

    /// <summary>
    /// 是否可开始问诊
    /// </summary>
    public bool CanStartConsultation => IsActive && !ConsultationId.HasValue;

    /// <summary>
    /// 是否可开处方
    /// </summary>
    public bool CanCreatePrescription => IsActive && ConsultationId.HasValue && !PrescriptionId.HasValue;

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{CaseNumber} - {PatientName} ({StatusText})";

    /// <summary>
    /// 就诊时长（分钟）
    /// </summary>
    public int? DurationMinutes
    {
        get
        {
            if (CompletedAt.HasValue)
            {
                return (int)(CompletedAt.Value - CreatedAt).TotalMinutes;
            }
            else if (IsActive)
            {
                return (int)(DateTime.Now - CreatedAt).TotalMinutes;
            }
            return null;
        }
    }
}