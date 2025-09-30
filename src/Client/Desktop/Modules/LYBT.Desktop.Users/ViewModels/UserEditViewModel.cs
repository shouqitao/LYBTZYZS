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
    /// 用户编辑视图模型 - Phase 1架构重构版本
    /// 基于新的PageViewModel实现用户编辑功能
    /// </summary>
    public class UserEditViewModel : UnifiedViewModelBase
    {
        #region 依赖服务

        private readonly IUserService _userService;

        #endregion

        #region 用户信息

        private Guid _userId;
        private UserDto? _originalUser;
        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;
        private bool _isUserLoaded;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId
        {
            get => _userId;
            private set => SetProperty(ref _userId, value);
        }

        /// <summary>
        /// 用户名（只读）
        /// </summary>
        public string Username
        {
            get => _username;
            private set => SetProperty(ref _username, value);
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
        /// 用户是否已加载
        /// </summary>
        public bool IsUserLoaded
        {
            get => _isUserLoaded;
            private set => SetProperty(ref _isUserLoaded, value);
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
            IsUserLoaded &&
            !string.IsNullOrWhiteSpace(RealName) &&
            !HasErrors;

        /// <summary>
        /// 是否有更改
        /// </summary>
        public bool HasChanges
        {
            get
            {
                if (_originalUser == null || !IsUserLoaded) return false;

                return _originalUser.RealName != RealName ||
                       _originalUser.PhoneNumber != PhoneNumber ||
                       _originalUser.Email != Email ||
                       _originalUser.Role != SelectedRole ||
                       _originalUser.Status != Status;
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

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public DelegateCommand ResetPasswordCommand { get; private set; }

        #endregion

        #region 构造函数

        public UserEditViewModel(
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
            PageTitle = "编辑用户";

            // 初始化命令
            InitializeCommands();

            Logger.LogDebug("用户编辑ViewModel已初始化");
        }

        #endregion

        #region 命令初始化

        protected override void InitializeCommands()
        {
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            ResetCommand = new DelegateCommand(ExecuteReset, () => IsUserLoaded);
            ResetPasswordCommand = new DelegateCommand(async () => await ExecuteResetPasswordAsync(), CanExecuteResetPassword);
        }

        #endregion

        #region 导航处理

        protected override void ProcessNavigationParameters(Prism.Regions.NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 获取用户ID
            if (parameters.TryGetValue("userId", out object userIdObj) && userIdObj is Guid userId)
            {
                UserId = userId;
                Logger.LogDebug("获取到用户ID: {UserId}", UserId);
            }
            else
            {
                Logger.LogWarning("未提供有效的用户ID参数");
                // 可以考虑导航回列表页面或显示错误
            }
        }

        protected async Task OnInitializeDataAsync()
        {
            if (UserId != Guid.Empty)
            {
                await LoadUserAsync();
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载用户数据
        /// </summary>
        private async Task LoadUserAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                Logger.LogDebug("开始加载用户数据: {UserId}", UserId);

                var result = await _userService.GetByIdAsync(UserId);

                if (result.IsSuccess && result.Data != null)
                {
                    _originalUser = result.Data;
                    LoadUserData(_originalUser);
                    IsUserLoaded = true;

                    PageTitle = $"编辑用户 - {_originalUser.RealName}";

                    Logger.LogDebug("成功加载用户数据: {Username} - {RealName}", _originalUser.UserName, _originalUser.RealName);
                }
                else
                {
                    Logger.LogWarning("加载用户数据失败: {ErrorMessage}", result.ErrorMessage);
                    throw new InvalidOperationException($"加载用户数据失败: {result.ErrorMessage}");
                }

            }, "加载用户数据");
        }

        /// <summary>
        /// 将用户数据加载到ViewModel属性
        /// </summary>
        private void LoadUserData(UserDto user)
        {
            Username = user.UserName;
            RealName = user.RealName;
            PhoneNumber = user.PhoneNumber;
            Email = user.Email;
            SelectedRole = user.Role;
            Status = user.Status;

            ClearValidationErrors();

            // 通知相关属性更新
            RaisePropertyChanged(nameof(HasChanges));
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            if (!IsFormValid || !HasChanges)
            {
                Logger.LogWarning("表单无效或无更改，无法保存用户");
                return;
            }

            await ExecuteSafelyAsync(async () =>
            {
                Logger.LogDebug("开始更新用户: {UserId} - {Username}", UserId, Username);

                var updateDto = new UserUpdateDto
                {
                    Id = UserId,
                    RealName = RealName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Role = SelectedRole,
                    Status = Status
                };

                var result = await _userService.UpdateAsync(UserId, updateDto);

                if (result.IsSuccess && result.Data != null)
                {
                    _originalUser = result.Data;
                    LoadUserData(_originalUser);

                    Logger.LogInformation("成功更新用户: {Username} - {RealName}", Username, RealName);

                    // 发布用户更新成功事件
                    EventAggregator.GetEvent<PubSubEvent<string>>().Publish($"用户 {RealName} 更新成功");

                    // 导航回用户管理页面
                    NavigateTo("ContentRegion", "UserManagementView");
                }
                else
                {
                    Logger.LogWarning("更新用户失败: {ErrorMessage}", result.ErrorMessage);
                    throw new InvalidOperationException($"更新用户失败: {result.ErrorMessage}");
                }

            }, "更新用户");
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanExecuteSave()
        {
            return IsFormValid && HasChanges && !IsLoading;
        }

        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            Logger.LogDebug("取消编辑用户");

            // 检查是否有未保存的更改
            if (HasChanges)
            {
                Logger.LogDebug("存在未保存的更改，直接导航回列表");
                // 这里可以显示确认对话框
            }

            // 导航回用户管理页面
            NavigateTo("ContentRegion", "UserManagementView");
        }

        /// <summary>
        /// 执行重置
        /// </summary>
        private void ExecuteReset()
        {
            if (_originalUser != null)
            {
                Logger.LogDebug("重置用户编辑表单到原始值");
                LoadUserData(_originalUser);
            }
        }

        /// <summary>
        /// 执行重置密码
        /// </summary>
        private async Task ExecuteResetPasswordAsync()
        {
            await ExecuteSafelyAsync(() =>
            {
                Logger.LogDebug("重置用户密码: {UserId} - {Username}", UserId, Username);

                // 这里应该调用密码重置服务，或者打开重置密码对话框
                // 暂时记录日志
                Logger.LogInformation("用户 {Username} 的密码重置请求已提交", Username);

                // 实际实现可能需要：
                // 1. 打开重置密码对话框
                // 2. 调用密码重置API
                // 3. 发送重置通知

                return Task.CompletedTask;
            }, "重置密码");
        }

        /// <summary>
        /// 是否可以重置密码
        /// </summary>
        private bool CanExecuteResetPassword()
        {
            return IsUserLoaded && Status == CommonStatus.Enabled && !IsLoading;
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证指定属性
        /// </summary>
        private void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            switch (propertyName)
            {
                case nameof(RealName):
                    if (string.IsNullOrWhiteSpace(RealName))
                    {
                        AddValidationError(propertyName, "$1");
                    }
                    else if (RealName.Length > 50)
                    {
                        AddValidationError(propertyName, "$1");
                    }
                    else
                    {
                        ClearValidationErrors(propertyName);
                    }
                    break;

                case nameof(PhoneNumber):
                    if (!string.IsNullOrWhiteSpace(PhoneNumber))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber, @"^1[3-9]\d{9}$"))
                        {
                            AddValidationError(propertyName, "$1");
                        }
                        else
                        {
                            ClearValidationErrors(propertyName);
                        }
                    }
                    else
                    {
                        ClearValidationErrors(propertyName);
                    }
                    break;

                case nameof(Email):
                    if (!string.IsNullOrWhiteSpace(Email))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            AddValidationError(propertyName, "$1");
                        }
                        else
                        {
                            ClearValidationErrors(propertyName);
                        }
                    }
                    else
                    {
                        ClearValidationErrors(propertyName);
                    }
                    break;
            }

            // 更新相关属性
            RaisePropertyChanged(nameof(IsFormValid));
            RaisePropertyChanged(nameof(HasChanges));
        }

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        protected virtual bool HasUnsavedChanges()
        {
            return HasChanges;
        }

        #endregion

        #region 命令刷新

        protected virtual void RefreshCanExecuteChanged()
        {
            SaveCommand?.RaiseCanExecuteChanged();
            ResetCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 刷新属性变化通知
        /// </summary>
        private void RefreshPropertyChanged()
        {
            RaisePropertyChanged(nameof(IsFormValid));
            RaisePropertyChanged(nameof(HasChanges));
        }

        #endregion

        #region 属性变化处理

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            // 当关键属性变化时，刷新相关状态
            if (args.PropertyName is nameof(RealName) or nameof(PhoneNumber) or nameof(Email)
                or nameof(SelectedRole) or nameof(Status))
            {
                RefreshPropertyChanged();
            }
        }

        #endregion
    }
}
