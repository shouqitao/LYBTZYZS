using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户详情视图模型 - Phase 4B 骨架实现（已统一架构）
    /// </summary>
    public class UserDetailViewModel : UnifiedViewModelBase
    {
        private UserDto? _user;

        /// <summary>
        /// 当前用户
        /// </summary>
        public UserDto? User
        {
            get => _user;
            set => SetProperty(ref _user, value);
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

        public UserDetailViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            GoBackCommand = new DelegateCommand(ExecuteGoBack);
            EditUserCommand = new DelegateCommand(ExecuteEditUser, CanExecuteEditUser);
            ResetPasswordCommand = new DelegateCommand(ExecuteResetPassword, CanExecuteResetPassword);
        }

        private void ExecuteGoBack()
        {
            Logger.LogInformation("UserDetailView - 返回命令执行（骨架实现）");

            // TODO: Phase 4C - 实现返回导航
            // NavigateBack(RegionNames.MainRegion);
        }

        private void ExecuteEditUser()
        {
            Logger.LogInformation("UserDetailView - 编辑用户命令执行（骨架实现）");
            Logger.LogDebug("用户ID: {UserId}", User?.Id);

            // TODO: Phase 4C - 实现编辑用户逻辑
        }

        private bool CanExecuteEditUser()
        {
            return User != null && !IsBusy;
        }

        private void ExecuteResetPassword()
        {
            Logger.LogInformation("UserDetailView - 重置密码命令执行（骨架实现）");
            Logger.LogDebug("用户ID: {UserId}", User?.Id);

            // TODO: Phase 4C - 实现重置密码逻辑
        }

        private bool CanExecuteResetPassword()
        {
            return User != null && !IsBusy;
        }
    }
}
