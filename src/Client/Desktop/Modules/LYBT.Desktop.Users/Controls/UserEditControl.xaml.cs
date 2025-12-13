using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户编辑控件 - OpenSpec: extract-detail-controls Task 4.2
    /// 独立的用户编辑控件，可在UserDetailView中复用
    /// </summary>
    public partial class UserEditControl : UserControl
    {
        public UserEditControl()
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
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        /// <summary>
        /// 用户名是否只读（编辑模式下不可修改）
        /// </summary>
        public static readonly DependencyProperty IsUserNameReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsUserNameReadOnly),
                typeof(bool),
                typeof(UserEditControl),
                new PropertyMetadata(false));

        public bool IsUserNameReadOnly
        {
            get => (bool)GetValue(IsUserNameReadOnlyProperty);
            set => SetValue(IsUserNameReadOnlyProperty, value);
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public static readonly DependencyProperty RealNameProperty =
            DependencyProperty.Register(
                nameof(RealName),
                typeof(string),
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Role
        {
            get => (string)GetValue(RoleProperty);
            set => SetValue(RoleProperty, value);
        }

        /// <summary>
        /// 角色选项列表
        /// </summary>
        public static readonly DependencyProperty RoleOptionsProperty =
            DependencyProperty.Register(
                nameof(RoleOptions),
                typeof(ObservableCollection<string>),
                typeof(UserEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<string>? RoleOptions
        {
            get => (ObservableCollection<string>?)GetValue(RoleOptionsProperty);
            set => SetValue(RoleOptionsProperty, value);
        }

        /// <summary>
        /// 账户状态
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// 状态选项列表
        /// </summary>
        public static readonly DependencyProperty StatusOptionsProperty =
            DependencyProperty.Register(
                nameof(StatusOptions),
                typeof(ObservableCollection<string>),
                typeof(UserEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<string>? StatusOptions
        {
            get => (ObservableCollection<string>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(UserEditControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
