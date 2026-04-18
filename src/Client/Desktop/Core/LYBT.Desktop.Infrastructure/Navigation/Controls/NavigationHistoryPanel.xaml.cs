using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// NavigationHistoryPanel - Phase 2.1: Navigation Improvements
    /// 导航历史面板 - 显示和管理导航历史记录
    ///
    /// Usage:
    /// In XAML:
    /// <nav:NavigationHistoryPanel x:Name="HistoryPanel"/>
    ///
    /// In ViewModel:
    /// HistoryPanel.DataContext = new NavigationHistoryPanelViewModel(_navigationService);
    /// </summary>
    public partial class NavigationHistoryPanel : UserControl
    {
        public NavigationHistoryPanel()
        {
            InitializeComponent();
        }
    }
}
