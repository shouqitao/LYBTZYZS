using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户编辑控件 - 对象DP模式
    /// OpenSpec: frontend-architecture-unification
    ///
    /// 通过 User 对象DP接收编辑数据，替代原有的扁平字段DP
    /// </summary>
    public partial class UserEditControl : UserControl
    {
        public UserEditControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>用户编辑上下文</summary>
        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register(
                nameof(User),
                typeof(UserEditContext),
                typeof(UserEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public UserEditContext User
        {
            get => (UserEditContext)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        /// <summary>用户名是否只读（编辑模式下不可修改）</summary>
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

        /// <summary>角色选项列表</summary>
        public static readonly DependencyProperty RoleOptionsProperty =
            DependencyProperty.Register(
                nameof(RoleOptions),
                typeof(ObservableCollection<UserRole>),
                typeof(UserEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<UserRole>? RoleOptions
        {
            get => (ObservableCollection<UserRole>?)GetValue(RoleOptionsProperty);
            set => SetValue(RoleOptionsProperty, value);
        }

        /// <summary>状态选项列表</summary>
        public static readonly DependencyProperty StatusOptionsProperty =
            DependencyProperty.Register(
                nameof(StatusOptions),
                typeof(ObservableCollection<CommonStatus>),
                typeof(UserEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<CommonStatus>? StatusOptions
        {
            get => (ObservableCollection<CommonStatus>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>是否显示状态字段</summary>
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

        /// <summary>
        /// 验证错误源 - 用于显示验证错误消息
        /// OpenSpec: ui-validation-framework
        /// </summary>
        public static readonly DependencyProperty ErrorsSourceProperty =
            DependencyProperty.Register(
                nameof(ErrorsSource),
                typeof(ValidationErrorsAccessor),
                typeof(UserEditControl),
                new PropertyMetadata(null));

        public ValidationErrorsAccessor? ErrorsSource
        {
            get => (ValidationErrorsAccessor?)GetValue(ErrorsSourceProperty);
            set => SetValue(ErrorsSourceProperty, value);
        }

        #endregion
    }
}
