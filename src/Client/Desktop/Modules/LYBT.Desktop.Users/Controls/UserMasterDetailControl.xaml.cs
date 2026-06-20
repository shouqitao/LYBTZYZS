using LYBT.Desktop.Controls.Controls;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户Master-Detail控件
    ///
    /// 可复用业务控件，供Admin角色台使用
    /// </summary>
    public partial class UserMasterDetailControl : MasterDetailControlBase
    {
        public UserMasterDetailControl()
        {
            InitializeComponent();
            InitializeAsyncSupport();
        }
    }
}
