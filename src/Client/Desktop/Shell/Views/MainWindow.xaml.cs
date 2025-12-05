using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Shell.ViewModels;

namespace LYBT.Desktop.Shell.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// remove-titlebar-add-close-button: 添加Alt+F4拦截逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // UltraThink修复 Issue #856: 在窗口完全加载后才检查登录状态
            // 原因：构造函数中启动Task.Run可能在Region注册前执行导航
            // 解决：订阅Loaded事件，确保所有XAML元素和Region已就绪
            Loaded += OnWindowLoaded;
        }

        /// <summary>
        /// 窗口加载完成事件处理 - UltraThink修复 Issue #856
        /// 确保所有Region已注册后才触发登录状态检查
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.OnWindowLoadedAsync();
            }
        }

        /// <summary>
        /// 拦截Alt+F4快捷键
        /// remove-titlebar-add-close-button: 仅在登录界面允许Alt+F4关闭程序，需显示确认框
        /// </summary>
        protected override async void OnPreviewKeyDown(KeyEventArgs e)
        {
            // 检测Alt+F4组合键
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true; // 先阻止默认行为

                // 仅在登录界面允许关闭（需要确认）
                if (IsOnLoginScreen())
                {
                    if (DataContext is MainWindowViewModel viewModel)
                    {
                        await viewModel.RequestCloseApplicationAsync();
                    }
                }
                // 非登录界面：Alt+F4被完全阻止
            }
            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// 判断当前是否在登录界面
        /// remove-titlebar-add-close-button: 通过ViewModel的IsNotLoggedIn属性判断
        /// </summary>
        private bool IsOnLoginScreen()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                return viewModel.IsNotLoggedIn;
            }
            // 默认允许关闭（安全考虑：无法判断时允许关闭）
            return true;
        }
    }
}
