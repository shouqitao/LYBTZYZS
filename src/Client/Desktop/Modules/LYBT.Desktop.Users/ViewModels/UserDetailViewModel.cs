using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
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
    /// <summary>用户详情视图模型 - CRUD统一架构</summary>
    public class UserDetailViewModel : UnifiedViewModelBase
    {
        private readonly UserCommandHandler _commandHandler;

        private Guid _userId;
        private bool _isEditMode = true;
        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        public Guid UserId { get => _userId; set => SetProperty(ref _userId, value); }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { if (SetProperty(ref _isEditMode, value)) { RaisePropertyChanged(nameof(IsReadOnly)); RaisePropertyChanged(nameof(IsCreateMode)); RaisePropertyChanged(nameof(IsEditOrViewMode)); SubmitCommand?.RaiseCanExecuteChanged(); SwitchToEditModeCommand?.RaiseCanExecuteChanged(); } }
        }

        public bool IsReadOnly => !IsEditMode;
        public bool IsCreateMode => UserId == Guid.Empty;
        public bool IsEditOrViewMode => UserId != Guid.Empty;

        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        public string UserName { get => _userName; set { if (SetProperty(ref _userName, value)) { ValidateProperty(); SubmitCommand?.RaiseCanExecuteChanged(); } } }

        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        public string RealName { get => _realName; set { if (SetProperty(ref _realName, value)) { ValidateProperty(); SubmitCommand?.RaiseCanExecuteChanged(); } } }

        public string? PhoneNumber { get => _phoneNumber; set { if (SetProperty(ref _phoneNumber, value)) ValidateProperty(); } }
        public string? Email { get => _email; set { if (SetProperty(ref _email, value)) ValidateProperty(); } }
        public UserRole SelectedRole { get => _selectedRole; set => SetProperty(ref _selectedRole, value); }
        public CommonStatus Status { get => _status; set => SetProperty(ref _status, value); }
        public UserRole[] RoleOptions { get; }
        public CommonStatus[] StatusOptions { get; }

        public DelegateCommand SubmitCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand SwitchToEditModeCommand { get; }
        public DelegateCommand GoBackCommand { get; }

        public UserDetailViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), () => !IsLoading && !IsReadOnly && !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(RealName) && !HasErrors);
            CancelCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
            SwitchToEditModeCommand = new DelegateCommand(() => { IsEditMode = true; PageTitle = $"编辑用户 - {RealName}"; }, () => IsReadOnly && !IsLoading && UserId != Guid.Empty);
            GoBackCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
        }

        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            if (parameters.ContainsKey("UserId")) UserId = parameters.GetValue<Guid>("UserId");
            IsEditMode = !(parameters.ContainsKey("ReadOnly") && parameters.GetValue<bool>("ReadOnly"));
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            if (UserId != Guid.Empty) { await LoadUserAsync(); PageTitle = IsReadOnly ? $"查看用户 - {RealName}" : $"编辑用户 - {RealName}"; }
            else { UserName = RealName = string.Empty; PhoneNumber = Email = null; SelectedRole = UserRole.Doctor; Status = CommonStatus.Enabled; PageTitle = "创建用户"; }
        }

        private async Task LoadUserAsync()
        {
            if (UserId == Guid.Empty) return;
            try
            {
                IsLoading = true; StatusMessage = "正在加载用户信息...";
                var result = await _commandHandler.GetByIdAsync(UserId);
                if (result.success && result.user != null) { UserName = result.user.UserName; RealName = result.user.RealName; PhoneNumber = result.user.PhoneNumber; Email = result.user.Email; SelectedRole = result.user.Role; Status = result.user.Status; }
                else await ShowErrorMessageAsync(result.errorMessage ?? "未找到用户信息");
            }
            catch (Exception ex) { Logger.LogError(ex, "加载用户数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载用户数据", ex)); }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true; StatusMessage = UserId == Guid.Empty ? "正在创建用户..." : "正在保存修改...";
                var dto = new UserInputDto { Id = UserId, UserName = UserName.Trim(), RealName = RealName.Trim(), PhoneNumber = PhoneNumber?.Trim(), Email = Email?.Trim(), Role = SelectedRole, Status = UserId == Guid.Empty ? CommonStatus.Enabled : Status };
                var result = UserId == Guid.Empty ? await _commandHandler.CreateAsync(dto) : await _commandHandler.UpdateAsync(dto);
                if (result.success && result.user != null) NavigateBack("ContentRegion", new NavigationParameters { { "RefreshRequired", true }, { "Operation", UserId == Guid.Empty ? "UserCreated" : "UserUpdated" }, { "User", result.user } });
                else await ShowErrorMessageAsync(result.errorMessage ?? (UserId == Guid.Empty ? "创建用户失败" : "更新用户失败"));
            }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        protected override void RefreshCommands() { base.RefreshCommands(); SubmitCommand?.RaiseCanExecuteChanged(); SwitchToEditModeCommand?.RaiseCanExecuteChanged(); }
    }
}
