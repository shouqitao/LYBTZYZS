using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// BreadcrumbControl - Phase 2.1: Navigation Improvements
    /// 面包屑导航控件 - 显示导航层级路径
    ///
    /// Usage:
    /// In XAML:
    /// <nav:BreadcrumbControl x:Name="BreadcrumbControl"/>
    ///
    /// In ViewModel:
    /// BreadcrumbControl.DataContext = new BreadcrumbControlViewModel(_navigationService);
    /// </summary>
    public partial class BreadcrumbControl : UserControl
    {
        public BreadcrumbControl()
        {
            InitializeComponent();
        }
    }
}
