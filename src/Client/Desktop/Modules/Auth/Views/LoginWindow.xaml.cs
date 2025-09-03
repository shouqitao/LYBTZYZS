using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Auth.ViewModels;
using Prism.Events;

namespace LYBT.Desktop.Auth.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑 (已弃用，现在使用单窗口模式)
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // 注意：此窗口已弃用，现在使用单窗口模式的 LoginView
            // 这里保留是为了向后兼容
        }

        private void PasswordChanged(PasswordBox passwordBox)
        {
            // 已弃用
        }
    }
}