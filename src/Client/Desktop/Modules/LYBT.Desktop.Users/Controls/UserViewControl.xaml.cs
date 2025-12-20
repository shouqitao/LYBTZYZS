using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户预览控件 - OpenSpec: extract-detail-controls Task 4.1
    /// 独立的用户预览控件，可在UserDetailView和其他需要展示用户信息的地方复用
    /// OpenSpec: refactor-master-detail-layout - 详情区域UI优化
    /// </summary>
    public partial class UserViewControl : UserControl
    {
        public UserViewControl()
        {
            InitializeComponent();
        }

        #region 基本信息属性

        /// <summary>用户名</summary>
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(UserViewControl), new PropertyMetadata(string.Empty));

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        /// <summary>真实姓名</summary>
        public static readonly DependencyProperty RealNameProperty =
            DependencyProperty.Register(nameof(RealName), typeof(string), typeof(UserViewControl), new PropertyMetadata(string.Empty));

        public string RealName
        {
            get => (string)GetValue(RealNameProperty);
            set => SetValue(RealNameProperty, value);
        }

        /// <summary>拼音码</summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(nameof(PinYinCode), typeof(string), typeof(UserViewControl), new PropertyMetadata(string.Empty));

        public string PinYinCode
        {
            get => (string)GetValue(PinYinCodeProperty);
            set => SetValue(PinYinCodeProperty, value);
        }

        /// <summary>用户角色</summary>
        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register(nameof(Role), typeof(UserRole), typeof(UserViewControl), new PropertyMetadata(UserRole.Doctor));

        public UserRole Role
        {
            get => (UserRole)GetValue(RoleProperty);
            set => SetValue(RoleProperty, value);
        }

        #endregion

        #region 联系信息属性

        /// <summary>手机号码</summary>
        public static readonly DependencyProperty PhoneNumberProperty =
            DependencyProperty.Register(nameof(PhoneNumber), typeof(string), typeof(UserViewControl), new PropertyMetadata(string.Empty));

        public string PhoneNumber
        {
            get => (string)GetValue(PhoneNumberProperty);
            set => SetValue(PhoneNumberProperty, value);
        }

        /// <summary>邮箱地址</summary>
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(nameof(Email), typeof(string), typeof(UserViewControl), new PropertyMetadata(string.Empty));

        public string Email
        {
            get => (string)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }

        #endregion

        #region 系统信息属性

        /// <summary>账户状态</summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(CommonStatus), typeof(UserViewControl), new PropertyMetadata(CommonStatus.Enabled));

        public CommonStatus Status
        {
            get => (CommonStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>最后登录时间</summary>
        public static readonly DependencyProperty LastLoginTimeProperty =
            DependencyProperty.Register(nameof(LastLoginTime), typeof(DateTime?), typeof(UserViewControl), new PropertyMetadata(null));

        public DateTime? LastLoginTime
        {
            get => (DateTime?)GetValue(LastLoginTimeProperty);
            set => SetValue(LastLoginTimeProperty, value);
        }

        /// <summary>创建时间</summary>
        public static readonly DependencyProperty CreatedAtProperty =
            DependencyProperty.Register(nameof(CreatedAt), typeof(DateTime?), typeof(UserViewControl), new PropertyMetadata(null));

        public DateTime? CreatedAt
        {
            get => (DateTime?)GetValue(CreatedAtProperty);
            set => SetValue(CreatedAtProperty, value);
        }

        /// <summary>更新时间</summary>
        public static readonly DependencyProperty UpdatedAtProperty =
            DependencyProperty.Register(nameof(UpdatedAt), typeof(DateTime?), typeof(UserViewControl), new PropertyMetadata(null));

        public DateTime? UpdatedAt
        {
            get => (DateTime?)GetValue(UpdatedAtProperty);
            set => SetValue(UpdatedAtProperty, value);
        }

        /// <summary>是否显示状态字段</summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(nameof(ShowStatus), typeof(bool), typeof(UserViewControl), new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
