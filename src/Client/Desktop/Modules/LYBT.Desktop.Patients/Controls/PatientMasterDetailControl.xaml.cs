using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Desktop.Patients.ViewModels;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者Master-Detail控件
    /// OpenSpec: refactor-frontend-srp-patterns - 继承MasterDetailControlBase基类
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class PatientMasterDetailControl : MasterDetailControlBase
    {
        public PatientMasterDetailControl()
        {
            InitializeComponent();
            InitializeViewModel<PatientMasterDetailViewModel>();
        }
    }
}
