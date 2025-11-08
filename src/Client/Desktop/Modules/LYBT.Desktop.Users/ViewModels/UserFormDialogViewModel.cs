using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户表单对话框 ViewModel - Issue #1798
    /// 功能：合并用户创建和编辑功能，支持Create/Edit两种模式
    /// </summary>
    public class UserFormDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region IDialogAware实现

        public string Title => _dialogTitle;

        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 私有字段

        private string _dialogTitle = "用户表单";
        private string _mode = "create"; // "create" or "edit"
        private Guid? _userId;
        private UserDto? _originalUser;

        #endregion

        #region 用户输入属性

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        public string UserName
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

        #region UI属性

        private string _submitButtonText = "创建";
        private string _loadingMessage = "正在处理...";

        /// <summary>
        /// 提交按钮文本
        /// </summary>
        public string SubmitButtonText
        {
            get => _submitButtonText;
            set => SetProperty(ref _submitButtonText, value);
        }

        /// <summary>
        /// 加载消息
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// 是否为创建模式
        /// </summary>
        public bool IsCreateMode => _mode == "create";

        /// <summary>
        /// 是否为编辑模式
        /// </summary>
        public bool IsEditMode => _mode == "edit";

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（创建或保存）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public UserFormDialogViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(Cancel);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) =>
            {
                SubmitCommand.RaiseCanExecuteChanged();
                if (e.PropertyName == nameof(UserName) ||
                    e.PropertyName == nameof(RealName) ||
                    e.PropertyName == nameof(PhoneNumber) ||
                    e.PropertyName == nameof(Email) ||
                    e.PropertyName == nameof(SelectedRole) ||
                    e.PropertyName == nameof(Status))
                {
                    RaisePropertyChanged(nameof(IsCreateMode));
                    RaisePropertyChanged(nameof(IsEditMode));
                }
            };
        }

        #endregion

        #region IDialogAware实现方法

        public bool CanCloseDialog()
        {
            return !IsLoading;
        }

        public void OnDialogClosed()
        {
            Logger.LogDebug("用户表单对话框已关闭");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 获取模式参数
            _mode = parameters.GetValue<string>("mode") ?? "create";
            Logger.LogDebug("打开用户表单对话框，模式：{Mode}", _mode);

            if (_mode == "create")
            {
                InitializeCreateMode();
            }
            else if (_mode == "edit")
            {
                _userId = parameters.GetValue<Guid?>("userId");
                if (_userId.HasValue && _userId.Value != Guid.Empty)
                {
                    InitializeEditMode(_userId.Value);
                }
                else
                {
                    Logger.LogError("编辑模式缺少userId参数");
                    ShowErrorMessage("无法加载用户信息：缺少用户ID");
                }
            }

            // 更新UI
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(IsCreateMode));
            RaisePropertyChanged(nameof(IsEditMode));
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化创建模式
        /// </summary>
        private void InitializeCreateMode()
        {
            _dialogTitle = "创建用户";
            SubmitButtonText = "创建";
            LoadingMessage = "正在创建用户...";

            // 清空表单
            UserName = string.Empty;
            RealName = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;
            Status = CommonStatus.Enabled;

            Logger.LogDebug("初始化创建模式完成");
        }

        /// <summary>
        /// 初始化编辑模式
        /// </summary>
        private async void InitializeEditMode(Guid userId)
        {
            _dialogTitle = "编辑用户";
            SubmitButtonText = "保存";
            LoadingMessage = "正在保存修改...";

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载用户信息...";

                // 加载用户数据
                var result = await _commandHandler.GetByIdAsync(userId);
                if (result.success && result.user != null)
                {
                    _originalUser = result.user;

                    // 填充表单
                    UserName = result.user.UserName;
                    RealName = result.user.RealName;
                    PhoneNumber = result.user.PhoneNumber;
                    Email = result.user.Email;
                    SelectedRole = result.user.Role;
                    Status = result.user.Status;

                    Logger.LogDebug("成功加载用户信息：{UserName}", result.user.UserName);
                }
                else
                {
                    Logger.LogError("用户不存在：{UserId}", userId);
                    ShowErrorMessage(result.errorMessage ?? "用户不存在");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户信息失败");
                ShowErrorMessage($"加载用户信息失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（创建或保存）
        /// </summary>
        /// <summary>
        /// 提交表单 (Issue #1911修复: IsLoading状态由Create/Update方法管理)
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;

                if (_mode == "create")
                {
                    await CreateUserAsync();
                }
                else if (_mode == "edit")
                {
                    await UpdateUserAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "提交表单失败");
                ShowErrorMessage($"操作失败：{ex.Message}");
                // 异常情况下清除IsLoading
                IsLoading = false;
            }
            // 注意：成功情况下IsLoading由Create/UpdateUserAsync在关闭对话框前清除
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <summary>
        /// 创建用户 (Issue #1911修复: IsLoading状态管理)
        /// </summary>
        private async Task CreateUserAsync()
        {
            StatusMessage = "正在创建用户...";

            var createDto = new UserInputDto
            {
                UserName = UserName,
                RealName = RealName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Role = SelectedRole,
                Status = Status
            };

            var result = await _commandHandler.CreateAsync(createDto);

            if (result.success && result.user != null)
            {
                Logger.LogInformation("成功创建用户：{UserName}", result.user.UserName);
                
                // 修复：在关闭对话框前清除IsLoading状态
                IsLoading = false;
                
                ShowInfoMessage($"成功创建用户：{result.user.RealName}");

                // 关闭对话框，返回成功结果
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                {
                    { "user", result.user }
                }));
            }
            else
            {
                Logger.LogError("创建用户失败：{ErrorMessage}", result.errorMessage);
                ShowErrorMessage(result.errorMessage ?? "创建用户失败");
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <summary>
        /// 更新用户 (Issue #1911修复: IsLoading状态管理)
        /// </summary>
        private async Task UpdateUserAsync()
        {
            if (!_userId.HasValue || _originalUser == null)
            {
                ShowErrorMessage("无法更新用户：缺少用户信息");
                return;
            }

            StatusMessage = "正在保存修改...";

            var updateDto = new UserInputDto
            {
                Id = _userId.Value,
                UserName = UserName,
                RealName = RealName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                Role = SelectedRole,
                Status = Status
            };

            var result = await _commandHandler.UpdateAsync(updateDto);

            if (result.success && result.user != null)
            {
                Logger.LogInformation("成功更新用户：{UserName}", result.user.UserName);
                
                // 修复：在关闭对话框前清除IsLoading状态
                IsLoading = false;
                
                ShowInfoMessage($"成功更新用户：{result.user.RealName}");

                // 关闭对话框，返回成功结果
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                {
                    { "user", result.user }
                }));
            }
            else
            {
                Logger.LogError("更新用户失败：{ErrorMessage}", result.errorMessage);
                ShowErrorMessage(result.errorMessage ?? "更新用户失败");
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            Logger.LogDebug("用户取消操作");
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   !HasErrors;
        }

        #endregion
    }
}
