using System.Windows.Controls;
using LYBT.Desktop.Patients.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者Master-Detail控件
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// 从PatientMasterDetailView重构而来
    ///
    /// 复用PatientMasterDetailViewModel作为DataContext
    /// </summary>
    public partial class PatientMasterDetailControl : UserControl
    {
        public PatientMasterDetailControl()
        {
            InitializeComponent();

            // 从DI容器解析ViewModel并设置DataContext
            // 这样可以复用现有的PatientMasterDetailViewModel
            var container = ContainerLocator.Container;
            DataContext = container.Resolve<PatientMasterDetailViewModel>();
        }
    }
}
