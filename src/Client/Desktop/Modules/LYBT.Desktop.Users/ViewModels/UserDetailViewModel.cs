using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户详情视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class UserDetailViewModel : BindableBase
    {
        private readonly ILogger<UserDetailViewModel> _logger;
        private UserDto? _user;
        private bool _isLoading;

        /// <summary>
        /// 当前用户
        /// </summary>
        public UserDto? User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; }

        /// <summary>
        /// 编辑用户命令
        /// </summary>
        public DelegateCommand EditUserCommand { get; }

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public DelegateCommand ResetPasswordCommand { get; }

        public UserDetailViewModel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<UserDetailViewModel>()
                ?? throw new ArgumentNullException(nameof(loggerFactory));

            GoBackCommand = new DelegateCommand(ExecuteGoBack);
            EditUserCommand = new DelegateCommand(ExecuteEditUser, CanExecuteEditUser);
            ResetPasswordCommand = new DelegateCommand(ExecuteResetPassword, CanExecuteResetPassword);
        }

        private void ExecuteGoBack()
        {
            _logger.LogInformation("UserDetailView - 返回命令执行（骨架实现）");

            // TODO: Phase 4C - 实现返回导航
        }

        private void ExecuteEditUser()
        {
            _logger.LogInformation("UserDetailView - 编辑用户命令执行（骨架实现）");
            _logger.LogDebug("用户ID: {UserId}", User?.Id);

            // TODO: Phase 4C - 实现编辑用户逻辑
        }

        private bool CanExecuteEditUser()
        {
            return User != null && !IsLoading;
        }

        private void ExecuteResetPassword()
        {
            _logger.LogInformation("UserDetailView - 重置密码命令执行（骨架实现）");
            _logger.LogDebug("用户ID: {UserId}", User?.Id);

            // TODO: Phase 4C - 实现重置密码逻辑
        }

        private bool CanExecuteResetPassword()
        {
            return User != null && !IsLoading;
        }
    }
}
