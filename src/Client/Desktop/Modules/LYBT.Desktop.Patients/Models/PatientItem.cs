using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Modules.Patients.Models;

/// <summary>
/// 患者列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PatientDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PatientDto一致，确保XAML绑定兼容
/// </summary>
public class PatientItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _gender = string.Empty;
    public string Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    private DateTime? _birthDate;
    /// <summary>
    /// 出生日期（Issue #2240: 存储BirthDate，Age从此计算）
    /// </summary>
    public DateTime? BirthDate
    {
        get => _birthDate;
        set
        {
            if (SetProperty(ref _birthDate, value))
            {
                RaisePropertyChanged(nameof(Age));
            }
        }
    }

    /// <summary>
    /// 年龄（只读计算属性，从BirthDate计算）
    /// Issue #2240: Age不再存储，而是实时计算
    /// </summary>
    public int? Age
    {
        get
        {
            if (BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age))
                {
                    age--;
                }
                return age;
            }
            return null;
        }
    }

    private string _phoneNumber = string.Empty;
    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    private string? _address;
    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    private string? _idCard;
    public string? IdCard
    {
        get => _idCard;
        set => SetProperty(ref _idCard, value);
    }

    private string? _medicalHistory;
    public string? MedicalHistory
    {
        get => _medicalHistory;
        set => SetProperty(ref _medicalHistory, value);
    }

    private string? _allergyHistory;
    public string? AllergyHistory
    {
        get => _allergyHistory;
        set => SetProperty(ref _allergyHistory, value);
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _lastVisitDate;
    public DateTime? LastVisitDate
    {
        get => _lastVisitDate;
        set => SetProperty(ref _lastVisitDate, value);
    }

    private int _visitCount;
    public int VisitCount
    {
        get => _visitCount;
        set => SetProperty(ref _visitCount, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    /// <summary>
    /// 从PatientDto创建PatientItem
    /// Issue #2240: 直接传递BirthDate，Age自动计算
    /// </summary>
    public static PatientItem FromDto(PatientDetailDto dto)
    {
        return new PatientItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Gender = dto.Gender.ToString(), // 枚举转字符串
            BirthDate = dto.BirthDate, // Issue #2240: 存储BirthDate，Age自动计算
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            Address = dto.Address,
            IdCard = dto.IdNumber, // PatientDto中是IdNumber
            MedicalHistory = null, // PatientDto中没有此属性，将来扩展
            AllergyHistory = dto.AllergyHistory,
            CreatedAt = dto.CreatedAt, // PatientDto中是CreateTime
            LastVisitDate = dto.LastVisitTime, // PatientDto中是LastVisitTime
            VisitCount = dto.VisitCount
        };
    }

    /// <summary>
    /// 转换为PatientDto（用于API调用）
    /// Issue #2240: 直接传递BirthDate，不再从Age反算
    /// </summary>
    public PatientDetailDto ToDto()
    {
        return new PatientDetailDto
        {
            Id = Id,
            Name = Name,
            Gender = Enum.Parse<Gender>(Gender), // 字符串转枚举
            BirthDate = BirthDate, // Issue #2240: 直接传递BirthDate，Age在PatientDto中也是计算属性
            PhoneNumber = PhoneNumber,
            Address = Address,
            IdNumber = IdCard, // PatientItem的IdCard对应PatientDto的IdNumber
            AllergyHistory = AllergyHistory,
            Status = CommonStatus.Enabled, // 默认启用状态
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.Now,
            LastVisitTime = LastVisitDate,
            VisitCount = VisitCount
        };
    }

    /// <summary>
    /// 从PatientDto更新当前项
    /// Issue #2240: 更新BirthDate，Age自动计算
    /// </summary>
    public void UpdateFromDto(PatientDetailDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Gender = dto.Gender.ToString(); // 枚举转字符串
        BirthDate = dto.BirthDate; // Issue #2240: 更新BirthDate，Age自动计算
        PhoneNumber = dto.PhoneNumber ?? string.Empty;
        Address = dto.Address;
        IdCard = dto.IdNumber; // PatientDto中是IdNumber
        MedicalHistory = null; // PatientDto中没有此属性，将来扩展
        AllergyHistory = dto.AllergyHistory;
        CreatedAt = dto.CreatedAt; // PatientDto中是CreateTime
        LastVisitDate = dto.LastVisitTime; // PatientDto中是LastVisitTime
        VisitCount = dto.VisitCount;
    }

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{Name} ({Gender}/{Age}岁)";

    /// <summary>
    /// 是否为新患者（30天内首次就诊）
    /// </summary>
    public bool IsNewPatient => CreatedAt > DateTime.Now.AddDays(-30) && VisitCount <= 1;
}
