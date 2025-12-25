using System.Windows.Controls;

namespace LYBT.Desktop.Patients.Views
{
    /// <summary>
    /// 患者Master-Detail视图V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 迁移完成后将移除V2后缀，替换原PatientMasterDetailView
    /// </summary>
    public partial class PatientMasterDetailView : UserControl
    {
        public PatientMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
