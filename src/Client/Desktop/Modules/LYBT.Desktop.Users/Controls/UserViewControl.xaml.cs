using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户预览控件 - OpenSpec: extract-detail-controls Task 4.1
    /// 独立的用户预览控件，可在UserDetailView和其他需要展示用户信息的地方复用
    /// </summary>
    public partial class UserViewControl : UserControl
    {
        public UserViewControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>
        /// 用户名
        /// </summary>
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(
                nameof(UserName),
                typeof(string),
                typeof(UserViewControl),
                new PropertyMetadata(string.Empty));

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public static readonly DependencyProperty RealNameProperty =
            DependencyProperty.Register(
                nameof(RealName),
                typeof(string),
                typeof(UserViewControl),
                new PropertyMetadata(string.Empty));

        public string RealName
        {
            get => (string)GetValue(RealNameProperty);
            set => SetValue(RealNameProperty, value);
        }

        /// <summary>
        /// 手机号码
        /// </summary>
        public static readonly DependencyProperty PhoneNumberProperty =
            DependencyProperty.Register(
                nameof(PhoneNumber),
                typeof(string),
                typeof(UserViewControl),
                new PropertyMetadata(string.Empty));

        public string PhoneNumber
        {
            get => (string)GetValue(PhoneNumberProperty);
            set => SetValue(PhoneNumberProperty, value);
        }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(
                nameof(Email),
                typeof(string),
                typeof(UserViewControl),
                new PropertyMetadata(string.Empty));

        public string Email
        {
            get => (string)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }

        /// <summary>
        /// 用户角色
        /// </summary>
        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register(
                nameof(Role),
                typeof(string),
                typeof(UserViewControl),
                new PropertyMetadata(string.Empty));

        public string Role
        {
            get => (string)GetValue(RoleProperty);
            set => SetValue(RoleProperty, value);
        }

        /// <summary>
        /// 账户状态
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(object),
                typeof(UserViewControl),
                new PropertyMetadata(null));

        public object? Status
        {
            get => GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(UserViewControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
