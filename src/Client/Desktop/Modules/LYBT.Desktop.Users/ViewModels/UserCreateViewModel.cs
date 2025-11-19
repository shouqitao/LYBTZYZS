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

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 创建用户视图模型 - Issue #1927 (Sprint 1)
    /// 功能：用户创建表单，采用Navigation模式
    /// </summary>
    public class UserCreateViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region 用户输入属性

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;

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
                    SubmitCommand?.RaiseCanExecuteChanged();
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
                    SubmitCommand?.RaiseCanExecuteChanged();
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

        #endregion

        #region 选项集合

        /// <summary>
        /// 角色选项
        /// </summary>
        public UserRole[] RoleOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（创建）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public UserCreateViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            PageTitle = "创建用户";

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region Navigation模式方法

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            // 创建模式无需处理参数
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 初始化表单默认值
            UserName = string.Empty;
            RealName = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;

            Logger.LogDebug("UserCreateViewModel 初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（创建用户）
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在创建用户...";

                var createDto = new UserInputDto
                {
                    UserName = UserName.Trim(),
                    RealName = RealName.Trim(),
                    PhoneNumber = PhoneNumber?.Trim(),
                    Email = Email?.Trim(),
                    Role = SelectedRole,
                    Status = CommonStatus.Enabled
                };

                Logger.LogInformation("开始创建用户: UserName={UserName}, RealName={RealName}",
                    createDto.UserName, createDto.RealName);

                var result = await _commandHandler.CreateAsync(createDto);

                if (result.success && result.user != null)
                {
                    Logger.LogInformation("用户创建成功: UserId={UserId}, UserName={UserName}",
                        result.user.Id, result.user.UserName);

                    // 导航返回并传递刷新参数
                    NavigateBack("ContentRegion", new NavigationParameters
                    {
                        { "RefreshRequired", true },
                        { "Operation", "UserCreated" },
                        { "User", result.user }
                    });
                }
                else
                {
                    Logger.LogError("创建用户失败：{ErrorMessage}", result.errorMessage);
                    await ShowErrorMessageAsync(result.errorMessage ?? "创建用户失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建用户异常: UserName={UserName}", UserName);
                await ShowErrorMessageAsync($"创建用户失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            Logger.LogDebug("用户取消创建操作");
            NavigateBack("ContentRegion");
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
