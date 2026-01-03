using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 医案管理视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的MedicalCaseMasterDetailControl
    /// View在角色台，Control在业务模块
    /// </summary>
    public partial class MedicalCaseManagementView : UserControl
    {
        public MedicalCaseManagementView()
        {
            InitializeComponent();
        }
    }
}
