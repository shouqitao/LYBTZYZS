using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 经验方管理视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的FormulaMasterDetailControl
    /// View在角色台，Control在业务模块
    /// </summary>
    public partial class FormulaManagementView : UserControl
    {
        public FormulaManagementView()
        {
            InitializeComponent();
        }
    }
}
