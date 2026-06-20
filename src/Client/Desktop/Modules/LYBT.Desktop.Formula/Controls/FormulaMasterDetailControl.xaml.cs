using LYBT.Desktop.Controls.Controls;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方Master-Detail控件
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class FormulaMasterDetailControl : MasterDetailControlBase
    {
        public FormulaMasterDetailControl()
        {
            InitializeComponent();
            InitializeAsyncSupport();
        }
    }
}
