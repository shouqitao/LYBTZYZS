using System.Windows.Controls;
using LYBT.Desktop.MedicalCase.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// 医案Master-Detail控件
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// 从MedicalCaseMasterDetailView重构而来
    ///
    /// 复用MedicalCaseMasterDetailViewModel作为DataContext
    /// </summary>
    public partial class MedicalCaseMasterDetailControl : UserControl
    {
        public MedicalCaseMasterDetailControl()
        {
            InitializeComponent();

            // 从DI容器解析ViewModel并设置DataContext
            // 这样可以复用现有的MedicalCaseMasterDetailViewModel
            var container = ContainerLocator.Container;
            DataContext = container.Resolve<MedicalCaseMasterDetailViewModel>();
        }
    }
}
