using System.Windows.Controls;

namespace LYBT.Desktop.Herbs.Views
{
    /// <summary>
    /// 药材Master-Detail视图V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 迁移完成后将移除V2后缀，替换原HerbMasterDetailView
    /// </summary>
    public partial class HerbMasterDetailView : UserControl
    {
        public HerbMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
