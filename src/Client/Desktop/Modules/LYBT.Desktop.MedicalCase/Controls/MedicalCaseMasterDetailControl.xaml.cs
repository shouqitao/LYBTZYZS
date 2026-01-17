using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Desktop.MedicalCase.ViewModels;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// 医案Master-Detail控件
    /// OpenSpec: refactor-frontend-srp-patterns - 继承MasterDetailControlBase基类
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class MedicalCaseMasterDetailControl : MasterDetailControlBase
    {
        public MedicalCaseMasterDetailControl()
        {
            InitializeComponent();
            InitializeViewModel<MedicalCaseMasterDetailViewModel>();
        }
    }
}
