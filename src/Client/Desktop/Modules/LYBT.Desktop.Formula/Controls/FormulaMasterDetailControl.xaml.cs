using System.Windows.Controls;
using LYBT.Desktop.Formula.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方Master-Detail控件
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// 从FormulaMasterDetailView重构而来
    ///
    /// 复用FormulaMasterDetailViewModel作为DataContext
    /// </summary>
    public partial class FormulaMasterDetailControl : UserControl
    {
        public FormulaMasterDetailControl()
        {
            InitializeComponent();

            // 从DI容器解析ViewModel并设置DataContext
            // 这样可以复用现有的FormulaMasterDetailViewModel
            var container = ContainerLocator.Container;
            DataContext = container.Resolve<FormulaMasterDetailViewModel>();
        }
    }
}
