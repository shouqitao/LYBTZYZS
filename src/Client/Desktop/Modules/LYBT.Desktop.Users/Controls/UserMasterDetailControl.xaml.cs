using LYBT.Desktop.Controls.Controls;
using LYBT.Shared.Models.Enums;

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

        /// <summary>
        /// 设置默认角色过滤（供 Sysadmin 角色台调用）
        /// </summary>
        public void SetDefaultRoleFilter(UserRole role)
        {
            if (DataContext is ViewModels.UserMasterDetailViewModel vm)
            {
                vm.SelectedRoleFilter = role;
            }
        }
    }
}
