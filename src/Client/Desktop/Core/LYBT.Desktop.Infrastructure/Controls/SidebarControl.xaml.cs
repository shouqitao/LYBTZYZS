using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 侧边栏控件
    /// OpenSpec: refactor-role-navigation
    ///
    /// 功能：
    /// - 展开/收缩切换
    /// - 用户信息显示
    /// - 返回主页按钮（角色感知导航）
    /// - 修改个人信息/修改密码
    /// - 状态信息（网络+时间）
    /// - 退出登录
    /// </summary>
    public partial class SidebarControl : UserControl
    {
        public SidebarControl() => InitializeComponent();

        #region IsExpanded - 展开/收缩状态

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(SidebarControl),
                new PropertyMetadata(false));

        #endregion

        #region CurrentUser - 当前用户信息

        /// <summary>
        /// OpenSpec: dto-architecture-specification - 统一使用UserDetailDto
        /// </summary>
        public UserDetailDto? CurrentUser
        {
            get => (UserDetailDto?)GetValue(CurrentUserProperty);
            set => SetValue(CurrentUserProperty, value);
        }

        public static readonly DependencyProperty CurrentUserProperty =
            DependencyProperty.Register(nameof(CurrentUser), typeof(UserDetailDto), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion

        #region ApiStatus - API状态

        public ApiHealthStatus ApiStatus
        {
            get => (ApiHealthStatus)GetValue(ApiStatusProperty);
            set => SetValue(ApiStatusProperty, value);
        }

        public static readonly DependencyProperty ApiStatusProperty =
            DependencyProperty.Register(nameof(ApiStatus), typeof(ApiHealthStatus), typeof(SidebarControl),
                new PropertyMetadata(ApiHealthStatus.Checking));

        #endregion

        #region CurrentTime - 当前时间

        public DateTime CurrentTime
        {
            get => (DateTime)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register(nameof(CurrentTime), typeof(DateTime), typeof(SidebarControl),
                new PropertyMetadata(DateTime.Now));

        #endregion

        #region ToggleCommand - 展开/收缩命令

        public ICommand? ToggleCommand
        {
            get => (ICommand?)GetValue(ToggleCommandProperty);
            set => SetValue(ToggleCommandProperty, value);
        }

        public static readonly DependencyProperty ToggleCommandProperty =
            DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion

        #region NavigateToHomeCommand - 返回主页命令

        public ICommand? NavigateToHomeCommand
        {
            get => (ICommand?)GetValue(NavigateToHomeCommandProperty);
            set => SetValue(NavigateToHomeCommandProperty, value);
        }

        public static readonly DependencyProperty NavigateToHomeCommandProperty =
            DependencyProperty.Register(nameof(NavigateToHomeCommand), typeof(ICommand), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion

        #region EditProfileCommand - 修改个人信息命令

        public ICommand? EditProfileCommand
        {
            get => (ICommand?)GetValue(EditProfileCommandProperty);
            set => SetValue(EditProfileCommandProperty, value);
        }

        public static readonly DependencyProperty EditProfileCommandProperty =
            DependencyProperty.Register(nameof(EditProfileCommand), typeof(ICommand), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion

        #region ChangePasswordCommand - 修改密码命令

        public ICommand? ChangePasswordCommand
        {
            get => (ICommand?)GetValue(ChangePasswordCommandProperty);
            set => SetValue(ChangePasswordCommandProperty, value);
        }

        public static readonly DependencyProperty ChangePasswordCommandProperty =
            DependencyProperty.Register(nameof(ChangePasswordCommand), typeof(ICommand), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion

        #region LogoutCommand - 退出登录命令

        public ICommand? LogoutCommand
        {
            get => (ICommand?)GetValue(LogoutCommandProperty);
            set => SetValue(LogoutCommandProperty, value);
        }

        public static readonly DependencyProperty LogoutCommandProperty =
            DependencyProperty.Register(nameof(LogoutCommand), typeof(ICommand), typeof(SidebarControl),
                new PropertyMetadata(null));

        #endregion
    }
}
