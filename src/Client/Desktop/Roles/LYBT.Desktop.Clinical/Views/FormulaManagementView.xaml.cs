using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 经验方管理视图
    /// OpenSpec: rename-reference-to-management
    ///
    /// 薄包装View，复用业务模块的FormulaMasterDetailControl
    /// View在角色台，Control在业务模块
    ///
    /// 权限设计：
    /// - 诊所共享经验方：只读参考
    /// - 医生自创经验方：可完整管理
    /// </summary>
    public partial class FormulaManagementView : UserControl
    {
        public FormulaManagementView()
        {
            InitializeComponent();
        }
    }
}
