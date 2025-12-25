using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views
{
    /// <summary>
    /// 医案Master-Detail视图V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 迁移完成后将移除V2后缀，替换原MedicalCaseMasterDetailView
    /// </summary>
    public partial class MedicalCaseMasterDetailView : UserControl
    {
        public MedicalCaseMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
