using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Prism.Mvvm;

namespace LYBT.Desktop.Patients.Models
{
    /// <summary>
    /// 患者详情模型 - Master-Detail模式使用
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 用于在Detail区域展示和编辑患者信息
    /// </summary>
    public class PatientDetailModel : BindableBase
    {
        private Guid _id;
        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private Gender _gender = Gender.Unknown;
        private DateTime? _birthDate;
        private string? _idNumber;
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
        private int _visitCount;
        private DateTime? _lastVisitTime;
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
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
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
        public string? IdNumber
        {
            get => _idNumber;
            set => SetProperty(ref _idNumber, value);
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
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        /// <summary>地址</summary>
        public string? Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
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
        public string? AllergyHistory
        {
            get => _allergyHistory;
            set => SetProperty(ref _allergyHistory, value);
        }

        /// <summary>病史</summary>
        public string? MedicalHistory
        {
            get => _medicalHistory;
            set => SetProperty(ref _medicalHistory, value);
        }

        /// <summary>状态</summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>就诊次数</summary>
        public int VisitCount
        {
            get => _visitCount;
            set => SetProperty(ref _visitCount, value);
        }

        /// <summary>最后就诊时间</summary>
        public DateTime? LastVisitTime
        {
            get => _lastVisitTime;
            set => SetProperty(ref _lastVisitTime, value);
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
                MaritalStatus = MaritalStatus,
                BloodType = BloodType,
                PhoneNumber = PhoneNumber,
                Address = Address,
                EmergencyContactName = EmergencyContactName,
                EmergencyContactPhone = EmergencyContactPhone,
                EmergencyContactRelation = EmergencyContactRelation,
                AllergyHistory = AllergyHistory,
                MedicalHistory = MedicalHistory,
                Status = Status,
                VisitCount = VisitCount,
                LastVisitTime = LastVisitTime,
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
