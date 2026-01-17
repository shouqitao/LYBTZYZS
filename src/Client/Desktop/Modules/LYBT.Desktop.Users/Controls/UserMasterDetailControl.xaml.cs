using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Desktop.Users.ViewModels;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户Master-Detail控件
    /// OpenSpec: refactor-frontend-srp-patterns - 继承MasterDetailControlBase基类
    ///
    /// 可复用业务控件，供Admin角色台使用
    /// </summary>
    public partial class UserMasterDetailControl : MasterDetailControlBase
    {
        public UserMasterDetailControl()
        {
            InitializeComponent();
            InitializeViewModel<UserMasterDetailViewModel>();
        }
    }
}
