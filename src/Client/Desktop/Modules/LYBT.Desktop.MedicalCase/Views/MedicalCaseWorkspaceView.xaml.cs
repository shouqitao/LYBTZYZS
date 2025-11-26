using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views
{
    /// <summary>
    /// Epic #2210 Phase 4: 4:6统一看诊界面
    /// 设计文档: docs/explanation/architecture/patient-medicalcase-integration/patient-selection-workspace-integration.md
    /// 布局: 左侧40%诊断(Consultation) + 右侧60%处方(Prescription)
    /// 替代: MedicalCaseFlowView (已标记为Deprecated)
    /// </summary>
    public partial class MedicalCaseWorkspaceView : UserControl
    {
        public MedicalCaseWorkspaceView()
        {
            InitializeComponent();
        }
    }
}
