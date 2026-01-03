using System.Windows.Controls;
using LYBT.Desktop.Herbs.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材Master-Detail控件
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// 从HerbMasterDetailView重构而来
    ///
    /// 复用HerbMasterDetailViewModel作为DataContext
    /// </summary>
    public partial class HerbMasterDetailControl : UserControl
    {
        public HerbMasterDetailControl()
        {
            InitializeComponent();

            // 从DI容器解析ViewModel并设置DataContext
            // 这样可以复用现有的HerbMasterDetailViewModel
            var container = ContainerLocator.Container;
            DataContext = container.Resolve<HerbMasterDetailViewModel>();
        }
    }
}
