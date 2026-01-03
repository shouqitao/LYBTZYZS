using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 经验方参考视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的FormulaMasterDetailControl
    /// View在角色台，Control在业务模块
    /// 用于医生诊疗时查看和选用经验方
    /// </summary>
    public partial class FormulaReferenceView : UserControl
    {
        public FormulaReferenceView()
        {
            InitializeComponent();
        }
    }
}
