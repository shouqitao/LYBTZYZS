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

            // Issue #1826: 响应式适配 - 监听窗口大小变化（主要适配1080P）
            SizeChanged += OnSizeChanged;

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
        /// Issue #1826: 响应式布局调整
        /// 主要分辨率1080P（1920x1080），当窗口宽度小于800px时调整布局
        /// </summary>
        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            // Issue #1826 修复: 只在UserControl已加载且宽度有效时才执行响应式逻辑
            if (!IsLoaded || e.NewSize.Width <= 0)
            {
                return;
            }

            // 当前设计已针对1080P优化（左侧品牌区 + 右侧登录框480px）
            // 当窗口宽度小于800px时，隐藏左侧品牌区，登录框居中显示
            // 断点设为800px以确保1080P全屏下始终显示左右分栏布局
            if (e.NewSize.Width < 800)
            {
                // 隐藏左侧品牌区
                if (LeftBrandPanel != null)
                {
                    LeftBrandPanel.Visibility = System.Windows.Visibility.Collapsed;
                }
                // 登录框调整为居中
                if (RightLoginBox != null)
                {
                    RightLoginBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                }
            }
            else
            {
                // 恢复左右分栏布局
                if (LeftBrandPanel != null)
                {
                    LeftBrandPanel.Visibility = System.Windows.Visibility.Visible;
                }
                if (RightLoginBox != null)
                {
                    RightLoginBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
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
