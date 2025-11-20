using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components; // Issue #1785: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户详情视图模型 - Issue #2168 CRUD统一架构
    /// 支持三种模式：Create（创建）、Edit（编辑）、View（查看）
    /// </summary>
    public class UserDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region 私有字段

        private Guid _userId;
        private bool _isEditMode = true;

        // 表单字段
        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        #endregion

        #region 模式控制属性

        /// <summary>
        /// 用户ID（空=Create模式，非空=Edit/View模式）
        /// </summary>
        public Guid UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        /// <summary>
        /// 是否为编辑模式（false=View只读模式）
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    RaisePropertyChanged(nameof(IsReadOnly));
                    RaisePropertyChanged(nameof(IsCreateMode));
                    RaisePropertyChanged(nameof(IsEditOrViewMode));
                    SubmitCommand?.RaiseCanExecuteChanged();
                    SwitchToEditModeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否为只读模式
        /// </summary>
        public bool IsReadOnly => !IsEditMode;

        /// <summary>
        /// 是否为创建模式
        /// </summary>
        public bool IsCreateMode => UserId == Guid.Empty;

        /// <summary>
        /// 是否为编辑或查看模式（非创建）
        /// </summary>
        public bool IsEditOrViewMode => UserId != Guid.Empty;

        #endregion

        #region 表单属性

        /// <summary>
        /// 用户名（Create可编辑，Edit/View只读）
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "用户名不能为空")]
        [System.ComponentModel.DataAnnotations.StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "真实姓名不能为空")]
        [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
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
        /// 提交命令（创建或更新）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 切换到编辑模式命令（View→Edit）
        /// </summary>
        public DelegateCommand SwitchToEditModeCommand { get; }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; }

        #endregion

        #region 构造函数

        public UserDetailViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            LYBT.Desktop.Infrastructure.Interfaces.ISessionManager? sessionManager = null,
            LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            SwitchToEditModeCommand = new DelegateCommand(ExecuteSwitchToEditMode, CanSwitchToEditMode);
            GoBackCommand = new DelegateCommand(ExecuteGoBack);
        }

        #endregion

        #region Navigation模式方法

        /// <summary>
        /// 处理导航参数（同步）
        /// Issue #2168: 根据参数区分Create/Edit/View模式
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 提取UserId参数（空=Create，非空=Edit/View）
            if (parameters.ContainsKey("UserId"))
            {
                UserId = parameters.GetValue<Guid>("UserId");
            }

            // 提取ReadOnly参数（true=View模式）
            if (parameters.ContainsKey("ReadOnly") && parameters.GetValue<bool>("ReadOnly"))
            {
                IsEditMode = false;  // View模式
            }
            else
            {
                IsEditMode = true;   // Create/Edit模式
            }
        }

        /// <summary>
        /// 异步初始化数据
        /// Issue #2168: 根据UserId区分Create/Edit/View模式
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            if (UserId != Guid.Empty)
            {
                // Edit/View模式：加载现有数据
                await LoadUserAsync();
                PageTitle = IsReadOnly ? $"查看用户 - {RealName}" : $"编辑用户 - {RealName}";
            }
            else
            {
                // Create模式：初始化空表单
                InitializeEmptyForm();
                PageTitle = "创建用户";
            }

            Logger.LogInformation("UserDetailViewModel 初始化完成，模式={Mode}, UserId={UserId}",
                IsCreateMode ? "Create" : (IsReadOnly ? "View" : "Edit"), UserId);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载用户数据（Edit/View模式）
        /// </summary>
        private async Task LoadUserAsync()
        {
            if (UserId == Guid.Empty)
            {
                Logger.LogWarning("LoadUserAsync: UserId 为空");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载用户信息...";

                Logger.LogInformation("开始加载用户数据: UserId={UserId}", UserId);

                var result = await _commandHandler.GetByIdAsync(UserId);

                if (result.success && result.user != null)
                {
                    // 填充表单
                    UserName = result.user.UserName;
                    RealName = result.user.RealName;
                    PhoneNumber = result.user.PhoneNumber;
                    Email = result.user.Email;
                    SelectedRole = result.user.Role;
                    Status = result.user.Status;

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

        /// <summary>
        /// 初始化空表单（Create模式）
        /// </summary>
        private void InitializeEmptyForm()
        {
            UserName = string.Empty;
            RealName = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;
            Status = CommonStatus.Enabled;

            Logger.LogDebug("空表单初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（创建或更新）
        /// Issue #2168: 根据UserId区分Create/Update逻辑
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;

                if (UserId == Guid.Empty)
                {
                    // Create逻辑
                    await CreateUserAsync();
                }
                else
                {
                    // Update逻辑
                    await UpdateUserAsync();
                }
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        private async Task CreateUserAsync()
        {
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

        /// <summary>
        /// 更新用户
        /// </summary>
        private async Task UpdateUserAsync()
        {
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

                // 导航返回并传递刷新参数
                NavigateBack("ContentRegion", new NavigationParameters
                {
                    { "RefreshRequired", true },
                    { "Operation", "UserUpdated" },
                    { "User", result.user }
                });
            }
            else
            {
                Logger.LogError("更新用户失败：{ErrorMessage}", result.errorMessage);
                await ShowErrorMessageAsync(result.errorMessage ?? "更新用户失败");
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void ExecuteCancel()
        {
            Logger.LogDebug("用户取消操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            return !IsLoading &&
                   !IsReadOnly &&  // View模式不能提交
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   !HasErrors;
        }

        /// <summary>
        /// 切换到编辑模式（View→Edit）
        /// </summary>
        private void ExecuteSwitchToEditMode()
        {
            Logger.LogInformation("切换到编辑模式: UserId={UserId}", UserId);
            IsEditMode = true;
            PageTitle = $"编辑用户 - {RealName}";
        }

        /// <summary>
        /// 是否可以切换到编辑模式
        /// </summary>
        private bool CanSwitchToEditMode()
        {
            return IsReadOnly && !IsLoading && UserId != Guid.Empty;
        }

        /// <summary>
        /// 返回用户列表
        /// </summary>
        private void ExecuteGoBack()
        {
            Logger.LogInformation("返回用户列表");
            NavigateBack("ContentRegion");
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        protected override void RefreshCommands()
        {
            base.RefreshCommands();
            SubmitCommand?.RaiseCanExecuteChanged();
            SwitchToEditModeCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
