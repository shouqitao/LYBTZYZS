using LYBT.Desktop.Controls.Controls;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// 医案Master-Detail控件
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class MedicalCaseMasterDetailControl : MasterDetailControlBase
    {
        public MedicalCaseMasterDetailControl()
        {
            InitializeComponent();
            InitializeAsyncSupport();
        }
    }
}
