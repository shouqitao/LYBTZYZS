using System.Windows;
using System.Windows.Controls;

namespace LYBT.WPF.Client.Controls.Authentication
{

    /// <summary>
    /// LoginControl.xaml 的交互逻辑
    /// 登录控件
    /// </summary>
    public partial class LoginControl : UserControl
    {

        public LoginControl()
        {
            InitializeComponent();

            // 处理密码框的数据绑定
            PasswordBox.PasswordChanged += OnPasswordChanged;
        }

        /// <summary>
        /// 密码依赖属性
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(LoginControl),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 获取或设置密码
        /// </summary>
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// 处理密码变更
        /// </summary>
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isUpdating)
            {
                _isUpdating = true;
                Password = PasswordBox.Password;
                _isUpdating = false;
            }
        }

        private bool _isUpdating;

        /// <summary>
        /// 当密码属性变更时更新密码框
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == PasswordProperty && !_isUpdating)
            {
                _isUpdating = true;
                PasswordBox.Password = Password;
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 错误消息依赖属性
        /// </summary>
        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(LoginControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取或设置错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        /// <summary>
        /// 是否有错误依赖属性
        /// </summary>
        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(LoginControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 获取或设置是否有错误
        /// </summary>
        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        /// <summary>
        /// 清除错误消息
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}
