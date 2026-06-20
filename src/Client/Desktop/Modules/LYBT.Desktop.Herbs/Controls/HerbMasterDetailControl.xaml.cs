using LYBT.Desktop.Controls.Controls;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材Master-Detail控件
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class HerbMasterDetailControl : MasterDetailControlBase
    {
        public HerbMasterDetailControl()
        {
            InitializeComponent();
            InitializeAsyncSupport();
        }
    }
}
