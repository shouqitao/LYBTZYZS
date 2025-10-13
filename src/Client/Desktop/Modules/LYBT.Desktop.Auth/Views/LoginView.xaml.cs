using System.ComponentModel;
using System.Windows.Controls;

namespace LYBT.Desktop.Auth.Views
{
    /// <summary>
    /// LoginView.xaml 的交互逻辑 - 架构重构后简化版本
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            // Issue #1246 修复: 监听 DataContext 变化，设置双向绑定
            DataContextChanged += OnDataContextChanged;
            
            // Issue #1246 关键修复: Prism 可能在 InitializeComponent 时就设置了 DataContext
            // 此时 DataContextChanged 事件不会触发，需要手动处理当前的 DataContext
            if (DataContext is INotifyPropertyChanged currentViewModel)
            {
                currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // 立即同步已有的密码值（处理时序竞争）
                if (DataContext is Auth.ViewModels.LoginViewModel viewModel 
                    && !string.IsNullOrEmpty(viewModel.Password))
                {
                    PasswordBox.Password = viewModel.Password;
                }
            }
        }

        /// <summary>
        /// DataContext 变化时订阅 ViewModel 的 PropertyChanged 事件
        /// </summary>
        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // 取消旧的订阅
            if (e.OldValue is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // 订阅新的 ViewModel
            if (e.NewValue is INotifyPropertyChanged newViewModel)
            {
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // Issue #1246 修复: 立即同步已有的密码值（处理时序竞争）
                if (e.NewValue is Auth.ViewModels.LoginViewModel viewModel 
                    && !string.IsNullOrEmpty(viewModel.Password))
                {
                    PasswordBox.Password = viewModel.Password;
                }
            }
        }

        /// <summary>
        /// ViewModel 属性变化时同步到 PasswordBox
        /// Issue #1246: 实现 Password 属性的双向绑定
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Auth.ViewModels.LoginViewModel.Password))
            {
                if (DataContext is Auth.ViewModels.LoginViewModel viewModel)
                {
                    // 只有当 PasswordBox 的值与 ViewModel 不同时才更新（避免循环）
                    if (PasswordBox.Password != viewModel.Password)
                    {
                        PasswordBox.Password = viewModel.Password;
                    }
                }
            }
        }

        /// <summary>
        /// PasswordBox 值变化时同步到 ViewModel
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is Auth.ViewModels.LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                // 只有当 ViewModel 的值与 PasswordBox 不同时才更新（避免循环）
                if (viewModel.Password != passwordBox.Password)
                {
                    viewModel.Password = passwordBox.Password;
                }
            }
        }
    }
}
