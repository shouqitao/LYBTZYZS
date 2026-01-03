using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 医案工作区视图 - 医生工作台专用
    /// OpenSpec: refactor-clinical-workflow
    /// 
    /// 布局: 左侧50%诊断(Consultation) + 右侧50%处方(Prescription)
    /// 使用 MedicalCase 模块的控件: ConsultationPanel, PrescriptionEditorPanel
    /// </summary>
    public partial class MedicalCaseWorkspaceView : UserControl
    {
        public MedicalCaseWorkspaceView()
        {
            InitializeComponent();
        }
    }
}
