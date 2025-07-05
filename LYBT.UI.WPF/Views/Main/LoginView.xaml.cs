using LYBT.UI.WPF.ViewModels.Main;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 登录视图的交互逻辑
    /// </summary>
    public partial class LoginView : UserControl {
        public LoginView() {
            InitializeComponent();
            InitializeView();
        }

        /// <summary>
        /// 初始化视图
        /// </summary>
        private void InitializeView() {
            // 当视图加载完成后进行初始化
            this.Loaded += OnViewLoaded;
        }

        /// <summary>
        /// 视图加载完成事件处理
        /// </summary>
        private void OnViewLoaded(object sender, RoutedEventArgs e) {
            if (DataContext is LoginViewModel viewModel) {
                // 如果有记住的密码，自动填充到密码框
                if (!string.IsNullOrEmpty(viewModel.Password)) {
                    passwordBox.Password = viewModel.Password;
                }

                // 设置初始焦点
                SetInitialFocus();
            }
        }

        /// <summary>
        /// 设置初始焦点
        /// </summary>
        private void SetInitialFocus() {
            // 如果用户名为空，聚焦到用户名输入框
            if (string.IsNullOrWhiteSpace(userNameBox.Text)) {
                userNameBox.Focus();
                userNameBox.SelectAll();
            }
            // 如果用户名不为空但密码为空，聚焦到密码输入框
            else if (string.IsNullOrWhiteSpace(passwordBox.Password)) {
                passwordBox.Focus();
            }
            // 都不为空时，聚焦到登录按钮
            else {
                BtnLogin.Focus();
            }
        }

        /// <summary>
        /// 密码框密码改变事件处理
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox) {
                // 同步密码到视图模型
                if (passwordBox.Password != viewModel.Password) {
                    viewModel.Password = passwordBox.Password;
                }
            }
        }

        /// <summary>
        /// 支持回车键登录
        /// </summary>
        private void UserControl_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && DataContext is LoginViewModel loginViewModel) {
                // 如果登录命令可以执行，则执行登录
                if (loginViewModel.LoginCommand.CanExecute()) {
                    loginViewModel.LoginCommand.Execute();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape) {
                // 按 ESC 键清空表单
                if (DataContext is LoginViewModel clearViewModel) {
                    if (clearViewModel.ClearCommand.CanExecute()) {
                        clearViewModel.ClearCommand.Execute();
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void BtnExit_Click(object sender, RoutedEventArgs e) {
            // 确认退出
            var result = MessageBox.Show(
                "确定要退出系统吗？",
                "退出确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes) {
                Application.Current.Shutdown();
            }
        }
    }
}
