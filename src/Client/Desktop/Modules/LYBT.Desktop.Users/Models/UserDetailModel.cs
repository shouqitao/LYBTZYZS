using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Users.Models
{
    /// <summary>
    /// 用户详情模型 - Master-Detail模式使用
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 用于在Detail区域展示和编辑用户信息
    /// </summary>
    public class UserDetailModel : ValidatableModelBase
    {
        private Guid _id;
        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string _pinYinCode = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _role = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;
        private DateTime? _lastLoginTime;
        private DateTime _createdAt;
        private DateTime? _updatedAt;
        private string? _remark;

        /// <summary>用户ID</summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>是否为新建</summary>
        public bool IsNew => Id == Guid.Empty;

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = 3,
            ErrorMessage = "用户名长度必须在3-50个字符之间")]
        public string UserName
        {
            get => _userName;
            set => SetPropertyAndValidate(ref _userName, value);
        }

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(ValidationConstants.NameMaxLength,
            ErrorMessage = "真实姓名长度不能超过100个字符")]
        public string RealName
        {
            get => _realName;
            set
            {
                if (SetPropertyAndValidate(ref _realName, value))
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

        /// <summary>手机号</summary>
        [Phone(ErrorMessage = "手机号格式不正确")]
        [StringLength(ValidationConstants.PhoneMaxLength,
            ErrorMessage = "手机号长度不能超过20个字符")]
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => SetPropertyAndValidate(ref _phoneNumber, value);
        }

        /// <summary>邮箱</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email
        {
            get => _email;
            set => SetPropertyAndValidate(ref _email, value);
        }

        /// <summary>角色</summary>
        public UserRole Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        /// <summary>状态</summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>最后登录时间</summary>
        public DateTime? LastLoginTime
        {
            get => _lastLoginTime;
            set => SetProperty(ref _lastLoginTime, value);
        }

        /// <summary>创建时间</summary>
        public DateTime CreatedAt
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

        /// <summary>备注</summary>
        [StringLength(ValidationConstants.RemarkMaxLength,
            ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetPropertyAndValidate(ref _remark, value);
        }

        /// <summary>创建空模型</summary>
        public static UserDetailModel CreateNew()
        {
            return new UserDetailModel
            {
                Id = Guid.Empty,
                UserName = string.Empty,
                RealName = string.Empty,
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
        }

        /// <summary>克隆模型</summary>
        public UserDetailModel Clone()
        {
            var clone = new UserDetailModel
            {
                Id = Id,
                UserName = UserName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Role = Role,
                Status = Status,
                LastLoginTime = LastLoginTime,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Remark = Remark
            };
            // 直接赋值拼音码，避免设置RealName时触发自动生成
            clone._realName = RealName;
            clone._pinYinCode = PinYinCode;
            return clone;
        }
    }
}
