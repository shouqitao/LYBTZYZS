using System.Windows;
using LYBT.Desktop.Shell.ViewModels;

namespace LYBT.Desktop.Shell.Views
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
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
    }
}
