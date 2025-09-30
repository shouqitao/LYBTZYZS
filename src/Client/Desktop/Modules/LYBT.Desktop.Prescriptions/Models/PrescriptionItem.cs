using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Prescriptions.Models;

/// <summary>
/// 处方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PrescriptionDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PrescriptionDto一致，确保XAML绑定兼容
/// </summary>
public class PrescriptionItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _prescriptionNumber = string.Empty;
    public string PrescriptionNumber
    {
        get => _prescriptionNumber;
        set => SetProperty(ref _prescriptionNumber, value);
    }

    private Guid _patientId;
    public Guid PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    private string _patientName = string.Empty;
    public string PatientName
    {
        get => _patientName;
        set => SetProperty(ref _patientName, value);
    }

    private string? _patientGender;
    public string? PatientGender
    {
        get => _patientGender;
        set => SetProperty(ref _patientGender, value);
    }

    private int? _patientAge;
    public int? PatientAge
    {
        get => _patientAge;
        set => SetProperty(ref _patientAge, value);
    }

    private Guid? _medicalCaseId;
    public Guid? MedicalCaseId
    {
        get => _medicalCaseId;
        set => SetProperty(ref _medicalCaseId, value);
    }

    private Guid? _consultationId;
    public Guid? ConsultationId
    {
        get => _consultationId;
        set => SetProperty(ref _consultationId, value);
    }

    private string? _diagnosis;
    public string? Diagnosis
    {
        get => _diagnosis;
        set => SetProperty(ref _diagnosis, value);
    }

    private string? _syndrome;
    public string? Syndrome
    {
        get => _syndrome;
        set => SetProperty(ref _syndrome, value);
    } // 证型

    private string? _treatmentPrinciple;
    public string? TreatmentPrinciple
    {
        get => _treatmentPrinciple;
        set => SetProperty(ref _treatmentPrinciple, value);
    } // 治则

    private int _doses = 1;
    public int Doses
    {
        get => _doses;
        set => SetProperty(ref _doses, value);
    } // 剂数

    private string? _usage;
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    } // 用法

    private string? _frequency;
    public string? Frequency
    {
        get => _frequency;
        set => SetProperty(ref _frequency, value);
    } // 频次

    private string? _note;
    public string? Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    } // 备注

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }

    private PrescriptionStatus _status;
    public PrescriptionStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private string? _doctorName;
    public string? DoctorName
    {
        get => _doctorName;
        set => SetProperty(ref _doctorName, value);
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _dispensedAt;
    public DateTime? DispensedAt
    {
        get => _dispensedAt;
        set => SetProperty(ref _dispensedAt, value);
    } // 配药时间

    private string? _dispensedBy;
    public string? DispensedBy
    {
        get => _dispensedBy;
        set => SetProperty(ref _dispensedBy, value);
    } // 配药人

    private ObservableCollection<PrescriptionHerbItem> _herbs = new();
    public ObservableCollection<PrescriptionHerbItem> Herbs
    {
        get => _herbs;
        set => SetProperty(ref _herbs, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isPrinted;
    public bool IsPrinted
    {
        get => _isPrinted;
        set => SetProperty(ref _isPrinted, value);
    }

    /// <summary>
    /// 从PrescriptionDetailDto创建PrescriptionItem
    /// </summary>
    public static PrescriptionItem FromDto(PrescriptionDetailDto dto)
    {
        var item = new PrescriptionItem
        {
            Id = dto.Id,
            PrescriptionNumber = dto.PrescriptionNo ?? dto.Id.ToString().Substring(0, 8).ToUpper(),
            PatientId = dto.PatientId,
            PatientName = string.Empty, // 需要从其他地方获取
            PatientGender = null, // 需要从其他地方获取
            PatientAge = null, // 需要从其他地方获取
            MedicalCaseId = dto.MedicalCaseId,
            ConsultationId = null, // DTO中没有此属性
            Diagnosis = dto.Indication,
            Syndrome = null, // DTO中没有此属性
            TreatmentPrinciple = null, // DTO中没有此属性
            Doses = dto.DosageCount,
            Usage = dto.Usage,
            Frequency = null, // DTO中没有此属性
            Note = dto.Remark,
            TotalAmount = dto.TotalPrice,
            Status = PrescriptionStatus.Draft, // 默认状态
            DoctorName = string.Empty, // 需要从其他地方获取
            CreatedAt = dto.CreateTime,
            DispensedAt = null, // DTO中没有此属性
            DispensedBy = null // DTO中没有此属性
        };

        // 转换药材列表
        if (dto.Items != null)
        {
            foreach (var itemDto in dto.Items)
            {
                item.Herbs.Add(PrescriptionHerbItem.FromDto(itemDto));
            }
        }

        return item;
    }

    /// <summary>
    /// 转换为PrescriptionDetailDto（用于API调用）
    /// </summary>
    public PrescriptionDetailDto ToDto()
    {
        return new PrescriptionDetailDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId ?? Guid.Empty,
            PatientId = PatientId,
            UserId = Guid.Empty, // 需要从其他地方获取
            Indication = Diagnosis,
            DosageCount = Doses,
            Discount = 1.0m,
            Advice = Note,
            FormulaSource = null,
            PrescriptionNo = PrescriptionNumber,
            Usage = Usage,
            MedicalAdvice = Note,
            Remark = Note,
            Items = Herbs.Select(h => h.ToDto()).ToList(),
            CreateTime = CreatedAt,
            UpdateTime = DateTime.Now,
            Status = CommonStatus.Enabled
        };
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        PrescriptionStatus.Draft => "草稿",
        PrescriptionStatus.Completed => "已完成",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        PrescriptionStatus.Draft => "#9E9E9E",
        PrescriptionStatus.Completed => "#4CAF50",
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
    public bool CanDispense => Status == PrescriptionStatus.Draft;

    /// <summary>
    /// 是否可打印
    /// </summary>
    public bool CanPrint => Status == PrescriptionStatus.Completed;

    /// <summary>
    /// 是否可取消
    /// </summary>
    public bool CanCancel => Status == PrescriptionStatus.Draft;

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
public class PrescriptionHerbItem : BindableBase
{
    private Guid _herbId;
    public Guid HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    private string _herbName = string.Empty;
    public string HerbName
    {
        get => _herbName;
        set => SetProperty(ref _herbName, value);
    }

    private decimal _dosage;
    public decimal Dosage
    {
        get => _dosage;
        set => SetProperty(ref _dosage, value);
    }

    private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    private string? _usage;
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    } // 特殊用法

    private int _sequence;
    public int Sequence
    {
        get => _sequence;
        set => SetProperty(ref _sequence, value);
    }

    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        set => SetProperty(ref _subtotal, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// 从PrescriptionItemDto创建
    /// </summary>
    public static PrescriptionHerbItem FromDto(PrescriptionItemDto dto)
    {
        return new PrescriptionHerbItem
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Quantity,
            Unit = dto.Unit,
            UnitPrice = dto.UnitPrice,
            Usage = dto.Usage,
            Sequence = 1, // 默认值，因为PrescriptionItemDto没有Sequence
            Subtotal = dto.Subtotal
        };
    }

    /// <summary>
    /// 转换为DTO
    /// </summary>
    public PrescriptionItemDto ToDto()
    {
        return new PrescriptionItemDto
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Quantity = Dosage,
            Unit = Unit,
            UnitPrice = UnitPrice,
            Usage = Usage,
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
