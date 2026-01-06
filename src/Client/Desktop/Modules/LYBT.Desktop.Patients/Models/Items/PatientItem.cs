using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Models.Items;

/// <summary>
/// 患者列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PatientDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PatientDto一致，确保XAML绑定兼容
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
/// </summary>
public partial class PatientItem : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 性别 - OpenSpec: unify-frontend-backend-types Phase 1
    /// 统一使用Gender枚举，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenderDisplay))]
    private Gender _gender;

    /// <summary>
    /// 性别显示文本（用于UI绑定）- OpenSpec: unify-frontend-backend-types Phase 1
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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Age))]
    private DateTime? _birthDate;

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

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string? _address;

    /// <summary>
    /// 身份证号 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为IdNumber，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private string? _idNumber;

    [ObservableProperty]
    private string? _medicalHistory;

    [ObservableProperty]
    private string? _allergyHistory;

    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// 最后就诊时间 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为LastVisitTime，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private DateTime? _lastVisitTime;

    [ObservableProperty]
    private int _visitCount;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isHighlighted;

    /// <summary>
    /// 从PatientDto创建PatientItem
    /// Issue #2240: 直接传递BirthDate，Age自动计算
    /// OpenSpec: unify-frontend-backend-types Phase 1 - Gender直接使用枚举
    /// </summary>
    /// <remarks>已废弃：请使用PatientMappingService.ToItem()</remarks>
    [Obsolete("请使用PatientMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public static PatientItem FromDto(PatientDetailDto dto)
    {
        return new PatientItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Gender = dto.Gender, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            BirthDate = dto.BirthDate, // Issue #2240: 存储BirthDate，Age自动计算
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            Address = dto.Address,
            IdNumber = dto.IdNumber, // OpenSpec: unify-frontend-backend-types - 直接映射
            MedicalHistory = null, // PatientDto中没有此属性，将来扩展
            AllergyHistory = dto.AllergyHistory,
            CreatedAt = dto.CreatedAt,
            LastVisitTime = dto.LastVisitTime, // OpenSpec: unify-frontend-backend-types - 直接映射
            VisitCount = dto.VisitCount
        };
    }

    /// <summary>
    /// 转换为PatientDto（用于API调用）
    /// Issue #2240: 直接传递BirthDate，不再从Age反算
    /// OpenSpec: unify-frontend-backend-types Phase 1 - Gender直接使用枚举
    /// </summary>
    /// <remarks>已废弃：请使用PatientMappingService.ToDto()</remarks>
    [Obsolete("请使用PatientMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public PatientDetailDto ToDto()
    {
        return new PatientDetailDto
        {
            Id = Id,
            Name = Name,
            Gender = Gender, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            BirthDate = BirthDate, // Issue #2240: 直接传递BirthDate，Age在PatientDto中也是计算属性
            PhoneNumber = PhoneNumber,
            Address = Address,
            IdNumber = IdNumber, // OpenSpec: unify-frontend-backend-types - 直接映射
            AllergyHistory = AllergyHistory,
            Status = CommonStatus.Enabled, // 默认启用状态
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.Now,
            LastVisitTime = LastVisitTime, // OpenSpec: unify-frontend-backend-types - 直接映射
            VisitCount = VisitCount
        };
    }

    /// <summary>
    /// 从PatientDto更新当前项
    /// Issue #2240: 更新BirthDate，Age自动计算
    /// OpenSpec: unify-frontend-backend-types Phase 1 - Gender直接使用枚举
    /// </summary>
    public void UpdateFromDto(PatientDetailDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Gender = dto.Gender; // OpenSpec: unify-frontend-backend-types - 直接使用枚举
        BirthDate = dto.BirthDate; // Issue #2240: 更新BirthDate，Age自动计算
        PhoneNumber = dto.PhoneNumber ?? string.Empty;
        Address = dto.Address;
        IdNumber = dto.IdNumber; // OpenSpec: unify-frontend-backend-types - 直接映射
        MedicalHistory = null; // PatientDto中没有此属性，将来扩展
        AllergyHistory = dto.AllergyHistory;
        CreatedAt = dto.CreatedAt;
        LastVisitTime = dto.LastVisitTime; // OpenSpec: unify-frontend-backend-types - 直接映射
        VisitCount = dto.VisitCount;
    }

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// OpenSpec: unify-frontend-backend-types Phase 1 - 使用GenderDisplay替代Gender
    /// </summary>
    public string DisplayText => $"{Name} ({GenderDisplay}/{Age}岁)";

    /// <summary>
    /// 是否为新患者（30天内首次就诊）
    /// </summary>
    public bool IsNewPatient => CreatedAt > DateTime.Now.AddDays(-30) && VisitCount <= 1;
}
