using System.Windows.Controls;

namespace LYBT.WPF.Client.Shell.Views
{
    /// <summary>
    /// WorkingHomeView.xaml 的交互逻辑
    /// 使用SimpleHomeViewModel避免依赖注入问题的临时主页解决方案
    /// </summary>
    public partial class WorkingHomeView : UserControl
    {
        public WorkingHomeView()
        {
            InitializeComponent();
        }
    }
}