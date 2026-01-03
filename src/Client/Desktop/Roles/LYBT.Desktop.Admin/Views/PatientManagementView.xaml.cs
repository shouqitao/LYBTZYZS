using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 患者管理视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的PatientMasterDetailControl
    /// View在角色台，Control在业务模块
    /// </summary>
    public partial class PatientManagementView : UserControl
    {
        public PatientManagementView()
        {
            InitializeComponent();
        }
    }
}
