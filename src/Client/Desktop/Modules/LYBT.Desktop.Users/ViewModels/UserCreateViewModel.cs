using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户创建视图模型 - Phase 1重构简化版本
    /// 基于新的PageViewModel实现用户创建功能
    /// </summary>
    public class UserCreateViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IUserService _userService;

        #endregion

        #region 用户输入属性

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        public string RealName
        {
            get => _realName;
            set
            {
                if (SetProperty(ref _realName, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ValidateProperty();
                    // 当密码改变时，重新验证确认密码
                    ValidateProperty(nameof(ConfirmPassword));
                }
            }
        }

        /// <summary>
        /// 确认密码
        /// </summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string? Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 选中的角色
        /// </summary>
        public UserRole SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        /// <summary>
        /// 用户状态
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 角色选项
        /// </summary>
        public UserRole[] RoleOptions { get; }

        /// <summary>
        /// 状态选项
        /// </summary>
        public CommonStatus[] StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 创建用户命令
        /// </summary>
        public DelegateCommand CreateUserCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 重置表单命令
        /// </summary>
        public DelegateCommand ResetFormCommand { get; }

        #endregion

        #region 构造函数

        public UserCreateViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            CreateUserCommand = new DelegateCommand(async () => await CreateUserAsync(), CanCreateUser);
            CancelCommand = new DelegateCommand(Cancel);
            ResetFormCommand = new DelegateCommand(ResetForm);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => CreateUserCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 创建用户
        /// </summary>
        private async Task CreateUserAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在创建用户...";

                // 创建用户数据传输对象
                var createDto = new UserCreateDto
                {
                    Username = Username.Trim(),
                    RealName = RealName.Trim(),
                    Password = Password,
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Role = SelectedRole,
                    Status = Status
                };

                // 调用服务创建用户
                var result = await _userService.CreateAsync(createDto);
                if (result.IsSuccess)
                {
                    StatusMessage = "用户创建成功";
                    System.Windows.MessageBox.Show("用户创建成功", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    // 创建成功后导航回用户管理页面
                    NavigateToUserManagement();
                }
                else
                {
                    ErrorMessage = $"创建用户失败: {result.ErrorMessage}";
                    System.Windows.MessageBox.Show(ErrorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建用户时发生异常");
                HandleError(ex, "创建用户");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 检查是否可以创建用户
        /// </summary>
        private bool CanCreateUser()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   Password == ConfirmPassword &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            NavigateToUserManagement();
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        private void ResetForm()
        {
            Username = string.Empty;
            RealName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;
            Status = CommonStatus.Enabled;

            // 清除验证错误
            ClearValidationErrors();
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证确认密码
        /// </summary>
        protected virtual void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            // 基础验证通过DataAnnotations自动处理

            // 特殊验证：确认密码
            if (propertyName == nameof(ConfirmPassword))
            {
                ClearValidationErrors(nameof(ConfirmPassword));
                if (Password != ConfirmPassword)
                {
                    AddValidationError(nameof(ConfirmPassword), "两次输入的密码不一致");
                }
            }

            // 特殊验证：手机号码格式
            if (propertyName == nameof(PhoneNumber) && !string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ClearValidationErrors(nameof(PhoneNumber));
                if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber, @"^1[3-9]\d{9}$"))
                {
                    AddValidationError(nameof(PhoneNumber), "请输入有效的手机号码");
                }
            }

            // 特殊验证：邮箱格式
            if (propertyName == nameof(Email) && !string.IsNullOrWhiteSpace(Email))
            {
                ClearValidationErrors(nameof(Email));
                if (!System.Text.RegularExpressions.Regex.IsMatch(Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    AddValidationError(nameof(Email), "请输入有效的邮箱地址");
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 导航到用户管理页面
        /// </summary>
        private void NavigateToUserManagement()
        {
            NavigateTo("MainRegion", "UserManagementView");
        }

        #endregion
    }
}
