using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Patients.Models.Items;

/// <summary>
/// 患者编辑上下文 - 统一编辑真源 (编辑表单数据模型)
/// OpenSpec: frontend-architecture-unification
///
/// 替代 PatientDetailModel 的编辑角色，作为 EditControl 对象 DP 的绑定目标
/// 所有编辑字段集中于此，支持验证 (ValidatableModelBase)
/// </summary>
public class PatientEditContext : ValidatableModelBase
{
    private Guid _id;
    private string _name = string.Empty;
    private string _pinYinCode = string.Empty;
    private Gender _gender = Gender.Unknown;
    private DateTime? _birthDate;
    private string? _idNumber;
    private int _idType;
    private int _maritalStatus;
    private int _bloodType;
    private string? _phoneNumber;
    private string? _address;
    private string? _emergencyContactName;
    private string? _emergencyContactPhone;
    private string? _emergencyContactRelation;
    private string? _allergyHistory;
    private string? _medicalHistory;
    private CommonStatus _status = CommonStatus.Enabled;

    /// <summary>患者ID (Guid.Empty 表示新建)</summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>是否为新建</summary>
    public bool IsNew => Id == Guid.Empty;

    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "患者姓名长度不能超过100个字符")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetPropertyAndValidate(ref _name, value))
            {
                PinYinCode = PinYinHelper.GetPinYinCode(value);
            }
        }
    }

    /// <summary>拼音码</summary>
    public string PinYinCode
    {
        get => _pinYinCode;
        set => SetProperty(ref _pinYinCode, value);
    }

    /// <summary>性别</summary>
    public Gender Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    /// <summary>出生日期</summary>
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

    /// <summary>年龄（根据出生日期计算）</summary>
    public int? Age
    {
        get
        {
            if (!BirthDate.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>证件类型</summary>
    public int IdType
    {
        get => _idType;
        set => SetProperty(ref _idType, value);
    }

    /// <summary>身份证号</summary>
    [Required(ErrorMessage = "身份证号不能为空")]
    [RegularExpression(@"^[1-9]\d{5}(19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$",
        ErrorMessage = "身份证号格式不正确")]
    [StringLength(ValidationConstants.IdCardMaxLength, ErrorMessage = "身份证号长度不能超过18个字符")]
    public string? IdNumber
    {
        get => _idNumber;
        set => SetPropertyAndValidate(ref _idNumber, value);
    }

    /// <summary>婚姻状况</summary>
    public int MaritalStatus
    {
        get => _maritalStatus;
        set => SetProperty(ref _maritalStatus, value);
    }

    /// <summary>血型</summary>
    public int BloodType
    {
        get => _bloodType;
        set => SetProperty(ref _bloodType, value);
    }

    /// <summary>手机号</summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(ValidationConstants.PhoneMaxLength, ErrorMessage = "手机号长度不能超过20个字符")]
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetPropertyAndValidate(ref _phoneNumber, value);
    }

    /// <summary>地址</summary>
    [Required(ErrorMessage = "地址不能为空")]
    [StringLength(ValidationConstants.AddressMaxLength, ErrorMessage = "地址长度不能超过200个字符")]
    public string? Address
    {
        get => _address;
        set => SetPropertyAndValidate(ref _address, value);
    }

    /// <summary>紧急联系人姓名</summary>
    public string? EmergencyContactName
    {
        get => _emergencyContactName;
        set => SetProperty(ref _emergencyContactName, value);
    }

    /// <summary>紧急联系人电话</summary>
    public string? EmergencyContactPhone
    {
        get => _emergencyContactPhone;
        set => SetProperty(ref _emergencyContactPhone, value);
    }

    /// <summary>紧急联系人关系</summary>
    public string? EmergencyContactRelation
    {
        get => _emergencyContactRelation;
        set => SetProperty(ref _emergencyContactRelation, value);
    }

    /// <summary>过敏史</summary>
    [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "过敏史长度不能超过1000个字符")]
    public string? AllergyHistory
    {
        get => _allergyHistory;
        set => SetPropertyAndValidate(ref _allergyHistory, value);
    }

    /// <summary>病史</summary>
    [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "病史长度不能超过1000个字符")]
    public string? MedicalHistory
    {
        get => _medicalHistory;
        set => SetPropertyAndValidate(ref _medicalHistory, value);
    }

    /// <summary>状态</summary>
    public CommonStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    #region Factory

    /// <summary>创建空模型</summary>
    public static PatientEditContext CreateNew()
    {
        return new PatientEditContext
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Gender = Gender.Unknown,
            Status = CommonStatus.Enabled
        };
    }

    #endregion
}
