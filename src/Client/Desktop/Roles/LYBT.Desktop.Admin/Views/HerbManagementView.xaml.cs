using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 药材管理视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的HerbMasterDetailControl
    /// View在角色台，Control在业务模块
    /// </summary>
    public partial class HerbManagementView : UserControl
    {
        public HerbManagementView()
        {
            InitializeComponent();
        }
    }
}
