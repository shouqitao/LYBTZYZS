using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base.Refactored;
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
    /// 用户创建视图模型 - Phase 1架构重构版本
    /// 基于新的PageViewModel实现用户创建功能
    /// </summary>
    public class UserCreateViewModel : PageViewModel
    {
        #region 依赖服务
        
        private readonly IUserService _userService;
        
        #endregion

        #region 用户创建属性
        
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
                    RefreshCanExecuteChanged();
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
                    RefreshCanExecuteChanged();
                }
            }
        }
        
        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ValidateProperty();
                    // 重新验证确认密码
                    ValidateProperty(nameof(ConfirmPassword));
                    RefreshCanExecuteChanged();
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
                    RefreshCanExecuteChanged();
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
        /// 选中的用户角色
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
        
        /// <summary>
        /// 角色选项
        /// </summary>
        public IEnumerable<UserRole> RoleOptions { get; }
        
        /// <summary>
        /// 状态选项
        /// </summary>
        public IEnumerable<CommonStatus> StatusOptions { get; }
        
        #endregion

        #region 验证属性
        
        /// <summary>
        /// 表单是否有效
        /// </summary>
        public bool IsFormValid =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(RealName) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            Password == ConfirmPassword &&
            !HasErrors;
        
        /// <summary>
        /// 密码是否匹配
        /// </summary>
        public bool PasswordsMatch => Password == ConfirmPassword;
        
        /// <summary>
        /// 密码强度描述
        /// </summary>
        public string PasswordStrength
        {
            get
            {
                if (string.IsNullOrEmpty(Password))
                    return "请输入密码";
                
                if (Password.Length < 6)
                    return "密码长度至少6位";
                
                var score = 0;
                if (Password.Any(char.IsUpper)) score++;
                if (Password.Any(char.IsLower)) score++;
                if (Password.Any(char.IsDigit)) score++;
                if (Password.Any(c => !char.IsLetterOrDigit(c))) score++;
                
                return score switch
                {
                    1 => "弱",
                    2 => "一般",
                    3 => "强",
                    4 => "很强",
                    _ => "弱"
                };
            }
        }
        
        #endregion

        #region 命令
        
        /// <summary>
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; private set; }
        
        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; private set; }
        
        /// <summary>
        /// 重置命令
        /// </summary>
        public DelegateCommand ResetCommand { get; private set; }
        
        #endregion

        #region 构造函数
        
        public UserCreateViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            
            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();
            
            // 初始化页面属性
            PageTitle = "创建用户";
            
            // 初始化命令
            InitializeCommands();
            
            Logger.LogDebug("用户创建ViewModel已初始化");
        }
        
        #endregion

        #region 命令初始化
        
        private void InitializeCommands()
        {
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            ResetCommand = new DelegateCommand(ExecuteReset);
        }
        
        #endregion

        #region 导航处理
        
        protected override void ProcessNavigationParameters(Prism.Regions.NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            
            // 处理可能的预设值
            if (parameters.TryGetValue("role", out object roleObj) && roleObj is UserRole role)
            {
                SelectedRole = role;
            }
        }
        
        #endregion

        #region 命令实现
        
        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            if (!IsFormValid)
            {
                Logger.LogWarning("表单验证失败，无法保存用户");
                return;
            }
            
            await ExecuteSafelyAsync(async () =>
            {
                Logger.LogDebug("开始创建用户: {Username}", Username);
                
                var createDto = new UserCreateDto
                {
                    Username = Username.Trim(),
                    RealName = RealName.Trim(),
                    Password = Password,
                    ConfirmPassword = ConfirmPassword,
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Role = SelectedRole,
                    Status = Status
                };
                
                var result = await _userService.CreateAsync(createDto);
                
                if (result.IsSuccess)
                {
                    Logger.LogInformation("成功创建用户: {Username} - {RealName}", Username, RealName);
                    
                    // 发布用户创建成功事件
                    EventAggregator.GetEvent<PubSubEvent<string>>().Publish($"用户 {RealName} 创建成功");
                    
                    // 导航回用户管理页面
                    NavigateTo("ContentRegion", "UserManagementView");
                }
                else
                {
                    Logger.LogWarning("创建用户失败: {ErrorMessage}", result.ErrorMessage);
                    throw new InvalidOperationException($"创建用户失败: {result.ErrorMessage}");
                }
                
            }, "创建用户");
        }
        
        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanExecuteSave()
        {
            return IsFormValid && !IsLoading;
        }
        
        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            Logger.LogDebug("取消创建用户");
            
            // 检查是否有未保存的更改
            if (HasUnsavedChanges())
            {
                // 这里可以显示确认对话框
                Logger.LogDebug("存在未保存的更改，直接导航回列表");
            }
            
            // 导航回用户管理页面
            NavigateTo("ContentRegion", "UserManagementView");
        }
        
        /// <summary>
        /// 执行重置
        /// </summary>
        private void ExecuteReset()
        {
            Logger.LogDebug("重置用户创建表单");
            
            Username = string.Empty;
            RealName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;
            Status = CommonStatus.Enabled;
            
            ClearError();
            ClearValidationErrors();
        }
        
        #endregion

        #region 验证
        
        /// <summary>
        /// 验证指定属性
        /// </summary>
        private void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            
            ClearValidationErrors(propertyName);
            
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) { MemberName = propertyName };
            
            switch (propertyName)
            {
                case nameof(Username):
                    if (string.IsNullOrWhiteSpace(Username))
                    {
                        AddValidationError(propertyName, "用户名不能为空");
                    }
                    else if (Username.Length < 3 || Username.Length > 32)
                    {
                        AddValidationError(propertyName, "用户名长度必须在3-32个字符之间");
                    }
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(Username, @"^[a-zA-Z0-9_]+$"))
                    {
                        AddValidationError(propertyName, "用户名只能包含字母、数字和下划线");
                    }
                    break;
                    
                case nameof(RealName):
                    if (string.IsNullOrWhiteSpace(RealName))
                    {
                        AddValidationError(propertyName, "真实姓名不能为空");
                    }
                    else if (RealName.Length > 50)
                    {
                        AddValidationError(propertyName, "真实姓名长度不能超过50个字符");
                    }
                    break;
                    
                case nameof(Password):
                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        AddValidationError(propertyName, "密码不能为空");
                    }
                    else if (Password.Length < 6)
                    {
                        AddValidationError(propertyName, "密码长度不能少于6个字符");
                    }
                    else if (Password.Length > 128)
                    {
                        AddValidationError(propertyName, "密码长度不能超过128个字符");
                    }
                    break;
                    
                case nameof(ConfirmPassword):
                    if (string.IsNullOrWhiteSpace(ConfirmPassword))
                    {
                        AddValidationError(propertyName, "确认密码不能为空");
                    }
                    else if (Password != ConfirmPassword)
                    {
                        AddValidationError(propertyName, "两次输入的密码不一致");
                    }
                    break;
                    
                case nameof(PhoneNumber):
                    if (!string.IsNullOrWhiteSpace(PhoneNumber))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber, @"^1[3-9]\d{9}$"))
                        {
                            AddValidationError(propertyName, "手机号码格式不正确");
                        }
                    }
                    break;
                    
                case nameof(Email):
                    if (!string.IsNullOrWhiteSpace(Email))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            AddValidationError(propertyName, "邮箱格式不正确");
                        }
                    }
                    break;
            }
            
            // 更新相关属性
            RaisePropertyChanged(nameof(IsFormValid));
            RaisePropertyChanged(nameof(PasswordsMatch));
            if (propertyName == nameof(Password))
            {
                RaisePropertyChanged(nameof(PasswordStrength));
            }
        }
        
        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        protected override bool HasUnsavedChanges()
        {
            return !string.IsNullOrWhiteSpace(Username) ||
                   !string.IsNullOrWhiteSpace(RealName) ||
                   !string.IsNullOrWhiteSpace(Password) ||
                   !string.IsNullOrWhiteSpace(PhoneNumber) ||
                   !string.IsNullOrWhiteSpace(Email);
        }
        
        #endregion

        #region 命令刷新
        
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
        }
        
        #endregion
    }
}