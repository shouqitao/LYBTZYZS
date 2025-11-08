using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Events;
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
    /// 编辑用户视图模型 - Issue #1927 (Sprint 1)
    /// 功能：用户编辑表单，采用Navigation模式
    /// </summary>
    public class UserEditViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region 私有字段

        private Guid _userId;

        #endregion

        #region 用户输入属性

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        /// <summary>
        /// 用户名（只读）
        /// </summary>
        public string UserName
        {
            get => _username;
            set => SetProperty(ref _username, value);
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
        /// 提交命令（保存）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public UserEditViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            PageTitle = "编辑用户";

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

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

            if (parameters.ContainsKey("UserId"))
            {
                UserId = parameters.GetValue<Guid>("UserId");
            }
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

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
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载用户信息...";

                Logger.LogInformation("开始加载用户数据: UserId={UserId}", UserId);

                var result = await _commandHandler.GetByIdAsync(UserId);

                if (result.success && result.user != null)
                {
                    UserName = result.user.UserName;
                    RealName = result.user.RealName;
                    PhoneNumber = result.user.PhoneNumber;
                    Email = result.user.Email;
                    SelectedRole = result.user.Role;
                    Status = result.user.Status;

                    PageTitle = $"编辑用户 - {RealName}";

                    Logger.LogInformation("用户数据加载成功: UserName={UserName}", UserName);
                }
                else
                {
                    Logger.LogWarning("未找到用户: UserId={UserId}", UserId);
                    await ShowErrorMessageAsync(result.errorMessage ?? "未找到用户信息");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户数据失败: UserId={UserId}", UserId);
                await ShowErrorMessageAsync($"加载用户数据失败：{ex.Message}");
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
        /// 提交表单（保存用户）
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存修改...";

                var updateDto = new UserInputDto
                {
                    Id = UserId,
                    UserName = UserName.Trim(),
                    RealName = RealName.Trim(),
                    PhoneNumber = PhoneNumber?.Trim(),
                    Email = Email?.Trim(),
                    Role = SelectedRole,
                    Status = Status
                };

                Logger.LogInformation("开始更新用户: UserId={UserId}, UserName={UserName}",
                    UserId, updateDto.UserName);

                var result = await _commandHandler.UpdateAsync(updateDto);

                if (result.success && result.user != null)
                {
                    Logger.LogInformation("用户更新成功: UserId={UserId}, UserName={UserName}",
                        result.user.Id, result.user.UserName);

                    // 发布事件通知列表刷新
                    EventAggregator.GetEvent<UserUpdatedEvent>().Publish(result.user);

                    // 导航返回
                    NavigateBack("ContentRegion");
                }
                else
                {
                    Logger.LogError("更新用户失败：{ErrorMessage}", result.errorMessage);
                    await ShowErrorMessageAsync(result.errorMessage ?? "更新用户失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新用户异常: UserId={UserId}", UserId);
                await ShowErrorMessageAsync($"更新用户失败：{ex.Message}");
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
            Logger.LogDebug("用户取消编辑操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            return !IsLoading &&
                   UserId != Guid.Empty &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   !HasErrors;
        }

        #endregion
    }
}
