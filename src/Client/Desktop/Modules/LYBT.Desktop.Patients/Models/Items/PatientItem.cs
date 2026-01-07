using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Patients.Models.Items;

/// <summary>
/// 患者列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PatientDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PatientDto一致，确保XAML绑定兼容
/// OpenSpec: resolve-mapperly-source-generator-conflict - 使用BindableBase确保Mapperly兼容
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
        set
        {
            if (SetProperty(ref _name, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>
    /// 性别
    /// </summary>
    private Gender _gender;
    public Gender Gender
    {
        get => _gender;
        set
        {
            if (SetProperty(ref _gender, value))
            {
                RaisePropertyChanged(nameof(GenderDisplay));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>
    /// 性别显示文本（用于UI绑定）
    /// </summary>
    public string GenderDisplay => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    /// <summary>
    /// 出生日期（Issue #2240: 存储BirthDate，Age从此计算）
    /// </summary>
    private DateTime? _birthDate;
    public DateTime? BirthDate
    {
        get => _birthDate;
        set
        {
            if (SetProperty(ref _birthDate, value))
            {
                RaisePropertyChanged(nameof(Age));
                RaisePropertyChanged(nameof(DisplayText));
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

    /// <summary>
    /// 身份证号
    /// </summary>
    private string? _idNumber;
    public string? IdNumber
    {
        get => _idNumber;
        set => SetProperty(ref _idNumber, value);
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
        set
        {
            if (SetProperty(ref _createdAt, value))
            {
                RaisePropertyChanged(nameof(IsNewPatient));
            }
        }
    }

    /// <summary>
    /// 最后就诊时间
    /// </summary>
    private DateTime? _lastVisitTime;
    public DateTime? LastVisitTime
    {
        get => _lastVisitTime;
        set => SetProperty(ref _lastVisitTime, value);
    }

    private int _visitCount;
    public int VisitCount
    {
        get => _visitCount;
        set
        {
            if (SetProperty(ref _visitCount, value))
            {
                RaisePropertyChanged(nameof(IsNewPatient));
            }
        }
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

    #region 计算属性

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{Name} ({GenderDisplay}/{Age}岁)";

    /// <summary>
    /// 是否为新患者（30天内首次就诊）
    /// </summary>
    public bool IsNewPatient => CreatedAt > DateTime.Now.AddDays(-30) && VisitCount <= 1;

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从PatientDto更新当前项
    /// </summary>
    public void UpdateFromDto(PatientDetailDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Gender = dto.Gender;
        BirthDate = dto.BirthDate;
        PhoneNumber = dto.PhoneNumber ?? string.Empty;
        Address = dto.Address;
        IdNumber = dto.IdNumber;
        MedicalHistory = dto.MedicalHistory;
        AllergyHistory = dto.AllergyHistory;
        CreatedAt = dto.CreatedAt;
        LastVisitTime = dto.LastVisitTime;
        VisitCount = dto.VisitCount;
    }

    #endregion
}
