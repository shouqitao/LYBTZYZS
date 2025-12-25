using System.Windows.Controls;

namespace LYBT.Desktop.Formula.Views
{
    /// <summary>
    /// 验方Master-Detail视图V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 迁移完成后将移除V2后缀，替换原FormulaMasterDetailView
    /// </summary>
    public partial class FormulaMasterDetailView : UserControl
    {
        public FormulaMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
