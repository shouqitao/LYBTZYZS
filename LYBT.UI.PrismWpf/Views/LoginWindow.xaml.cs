using System.Windows;
using System.Windows.Controls;
using LYBT.UI.PrismWpf.ViewModels;

namespace LYBT.UI.PrismWpf.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            // 订阅密码框变化事件
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            
            // 设置焦点到用户名输入框
            Loaded += (s, e) => UserNameTextBox.Focus();
        }

        /// <summary>
        /// 密码框变化事件处理
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.SetPassword(PasswordBox.Password);
                viewModel.ClearError(); // 清除之前的错误消息
            }
        }

        /// <summary>
        /// 设置ViewModel并订阅事件
        /// </summary>
        public void SetViewModel(LoginWindowViewModel viewModel)
        {
            DataContext = viewModel;
            viewModel.LoginSuccessful += OnLoginSuccessful;
        }

        /// <summary>
        /// 登录成功处理
        /// </summary>
        private void OnLoginSuccessful()
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 窗口关闭时清理事件订阅
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.LoginSuccessful -= OnLoginSuccessful;
            }
            base.OnClosed(e);
        }
    }
}