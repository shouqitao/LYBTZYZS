using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Models;

/// <summary>
/// 处方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PrescriptionDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PrescriptionDto一致，确保XAML绑定兼容
/// </summary>
public partial class PrescriptionItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string prescriptionNumber = string.Empty;

    [ObservableProperty]
    private int patientId;

    [ObservableProperty]
    private string patientName = string.Empty;

    [ObservableProperty]
    private string? patientGender;

    [ObservableProperty]
    private int? patientAge;

    [ObservableProperty]
    private int? medicalCaseId;

    [ObservableProperty]
    private int? consultationId;

    [ObservableProperty]
    private string? diagnosis;

    [ObservableProperty]
    private string? syndrome; // 证型

    [ObservableProperty]
    private string? treatmentPrinciple; // 治则

    [ObservableProperty]
    private int doses = 1; // 剂数

    [ObservableProperty]
    private string? usage; // 用法

    [ObservableProperty]
    private string? frequency; // 频次

    [ObservableProperty]
    private string? note; // 备注

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private PrescriptionStatus status;

    [ObservableProperty]
    private string? doctorName;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? dispensedAt; // 配药时间

    [ObservableProperty]
    private string? dispensedBy; // 配药人

    [ObservableProperty]
    private ObservableCollection<PrescriptionHerbItem> herbs = new();

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isPrinted;

    /// <summary>
    /// 从PrescriptionDto创建PrescriptionItem
    /// </summary>
    public static PrescriptionItem FromDto(PrescriptionDto dto)
    {
        var item = new PrescriptionItem
        {
            Id = dto.Id,
            PrescriptionNumber = dto.PrescriptionNumber,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = dto.PatientGender,
            PatientAge = dto.PatientAge,
            MedicalCaseId = dto.MedicalCaseId,
            ConsultationId = dto.ConsultationId,
            Diagnosis = dto.Diagnosis,
            Syndrome = dto.Syndrome,
            TreatmentPrinciple = dto.TreatmentPrinciple,
            Doses = dto.Doses,
            Usage = dto.Usage,
            Frequency = dto.Frequency,
            Note = dto.Note,
            TotalAmount = dto.TotalAmount,
            Status = dto.Status,
            DoctorName = dto.DoctorName,
            CreatedAt = dto.CreatedAt,
            DispensedAt = dto.DispensedAt,
            DispensedBy = dto.DispensedBy
        };

        // 转换药材列表
        if (dto.Herbs != null)
        {
            foreach (var herbDto in dto.Herbs)
            {
                item.Herbs.Add(PrescriptionHerbItem.FromDto(herbDto));
            }
        }

        return item;
    }

    /// <summary>
    /// 转换为PrescriptionDto（用于API调用）
    /// </summary>
    public PrescriptionDto ToDto()
    {
        return new PrescriptionDto
        {
            Id = Id,
            PrescriptionNumber = PrescriptionNumber,
            PatientId = PatientId,
            PatientName = PatientName,
            PatientGender = PatientGender,
            PatientAge = PatientAge,
            MedicalCaseId = MedicalCaseId,
            ConsultationId = ConsultationId,
            Diagnosis = Diagnosis,
            Syndrome = Syndrome,
            TreatmentPrinciple = TreatmentPrinciple,
            Doses = Doses,
            Usage = Usage,
            Frequency = Frequency,
            Note = Note,
            TotalAmount = TotalAmount,
            Status = Status,
            DoctorName = DoctorName,
            CreatedAt = CreatedAt,
            DispensedAt = DispensedAt,
            DispensedBy = DispensedBy,
            Herbs = Herbs.Select(h => h.ToDto()).ToList()
        };
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        PrescriptionStatus.Draft => "草稿",
        PrescriptionStatus.Issued => "已开具",
        PrescriptionStatus.Dispensed => "已配药",
        PrescriptionStatus.Completed => "已完成",
        PrescriptionStatus.Cancelled => "已取消",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        PrescriptionStatus.Draft => "#9E9E9E",
        PrescriptionStatus.Issued => "#2196F3",
        PrescriptionStatus.Dispensed => "#FF9800",
        PrescriptionStatus.Completed => "#4CAF50",
        PrescriptionStatus.Cancelled => "#F44336",
        _ => "#757575"
    };

    /// <summary>
    /// 药材数量
    /// </summary>
    public int HerbCount => Herbs?.Count ?? 0;

    /// <summary>
    /// 单剂金额
    /// </summary>
    public decimal SingleDoseAmount => Doses > 0 ? TotalAmount / Doses : 0;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool CanEdit => Status == PrescriptionStatus.Draft;

    /// <summary>
    /// 是否可配药
    /// </summary>
    public bool CanDispense => Status == PrescriptionStatus.Issued;

    /// <summary>
    /// 是否可打印
    /// </summary>
    public bool CanPrint => Status != PrescriptionStatus.Draft && Status != PrescriptionStatus.Cancelled;

    /// <summary>
    /// 是否可取消
    /// </summary>
    public bool CanCancel => Status == PrescriptionStatus.Draft || Status == PrescriptionStatus.Issued;

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{PrescriptionNumber} - {PatientName} ({StatusText})";

    /// <summary>
    /// 处方组成简述
    /// </summary>
    public string CompositionSummary
    {
        get
        {
            if (Herbs == null || Herbs.Count == 0)
                return "暂无药材";

            var mainHerbs = Herbs.Take(3).Select(h => h.HerbName);
            var summary = string.Join("、", mainHerbs);

            if (Herbs.Count > 3)
                summary += $" 等{HerbCount}味";

            return summary;
        }
    }

    /// <summary>
    /// 用法用量文本
    /// </summary>
    public string UsageText
    {
        get
        {
            var text = $"{Doses}剂";
            if (!string.IsNullOrWhiteSpace(Usage))
                text += $"，{Usage}";
            if (!string.IsNullOrWhiteSpace(Frequency))
                text += $"，{Frequency}";
            return text;
        }
    }

    /// <summary>
    /// 金额显示文本
    /// </summary>
    public string AmountText => $"¥{TotalAmount:F2}";

    /// <summary>
    /// 计算总金额
    /// </summary>
    public void CalculateTotalAmount()
    {
        TotalAmount = Herbs.Sum(h => h.Subtotal) * Doses;
    }
}

/// <summary>
/// 处方中的药材项
/// </summary>
public partial class PrescriptionHerbItem : ObservableObject
{
    [ObservableProperty]
    private int herbId;

    [ObservableProperty]
    private string herbName = string.Empty;

    [ObservableProperty]
    private decimal dosage;

    [ObservableProperty]
    private string unit = string.Empty;

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private string? usage; // 特殊用法

    [ObservableProperty]
    private int sequence;

    [ObservableProperty]
    private decimal subtotal;

    [ObservableProperty]
    private bool isSelected;

    /// <summary>
    /// 从PrescriptionHerbDto创建
    /// </summary>
    public static PrescriptionHerbItem FromDto(PrescriptionHerbDto dto)
    {
        return new PrescriptionHerbItem
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Dosage,
            Unit = dto.Unit,
            UnitPrice = dto.UnitPrice,
            Usage = dto.Usage,
            Sequence = dto.Sequence,
            Subtotal = dto.Subtotal
        };
    }

    /// <summary>
    /// 转换为DTO
    /// </summary>
    public PrescriptionHerbDto ToDto()
    {
        return new PrescriptionHerbDto
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Dosage = Dosage,
            Unit = Unit,
            UnitPrice = UnitPrice,
            Usage = Usage,
            Sequence = Sequence,
            Subtotal = Subtotal
        };
    }

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText
    {
        get
        {
            var text = $"{HerbName} {Dosage}{Unit}";
            if (!string.IsNullOrWhiteSpace(Usage))
                text += $"（{Usage}）";
            return text;
        }
    }

    /// <summary>
    /// 价格文本
    /// </summary>
    public string PriceText => $"¥{UnitPrice:F2}/{Unit}";

    /// <summary>
    /// 小计文本
    /// </summary>
    public string SubtotalText => $"¥{Subtotal:F2}";

    /// <summary>
    /// 计算小计
    /// </summary>
    public void CalculateSubtotal()
    {
        Subtotal = Dosage * UnitPrice;
    }
}