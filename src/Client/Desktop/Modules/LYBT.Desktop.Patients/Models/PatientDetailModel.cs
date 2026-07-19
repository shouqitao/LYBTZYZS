using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.Validation;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Patients.Models
{
    /// <summary>
    /// 患者详情模型 - Master-Detail模式使用
    ///
    /// 用于在Detail区域展示和编辑患者信息
    /// </summary>
    public class PatientDetailModel : ValidatableModelBase
    {
        private Guid _id;
        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private Gender _gender = Gender.Unknown;
        private DateTime? _birthDate;
        private string? _idNumber;
        private string? _phoneNumber;
        private CommonStatus _status = CommonStatus.Enabled;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;

        /// <summary>患者ID</summary>
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
                    // 自动生成拼音码
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

        /// <summary>手机号</summary>
        [Phone(ErrorMessage = "手机号格式不正确")]
        [StringLength(ValidationConstants.PhoneMaxLength, ErrorMessage = "手机号长度不能超过20个字符")]
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => SetPropertyAndValidate(ref _phoneNumber, value);
        }

        /// <summary>状态</summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>创建时间</summary>
        public DateTime? CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        /// <summary>创建空模型</summary>
        public static PatientDetailModel CreateNew()
        {
            return new PatientDetailModel
            {
                Id = Guid.Empty,
                Name = string.Empty,
                Gender = Gender.Unknown,
                Status = CommonStatus.Enabled
            };
        }

        /// <summary>克隆模型</summary>
        public PatientDetailModel Clone()
        {
            var clone = new PatientDetailModel
            {
                Id = Id,
                Gender = Gender,
                BirthDate = BirthDate,
                IdNumber = IdNumber,
                PhoneNumber = PhoneNumber,
                Status = Status,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
            // 直接赋值名称和拼音码，避免设置Name时触发自动生成
            clone._name = Name;
            clone._pinYinCode = PinYinCode;
            return clone;
        }
    }
}
