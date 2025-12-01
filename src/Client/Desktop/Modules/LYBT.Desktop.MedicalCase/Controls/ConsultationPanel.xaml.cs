using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// 诊断面板UserControl
    /// OpenSpec: controlify-workspace - Phase 2
    /// 用于MedicalCaseWorkspaceView的左侧1:1区域
    /// 通过Prism Region + RegionContext与父视图通信
    /// </summary>
    public partial class ConsultationPanel : UserControl
    {
        public ConsultationPanel()
        {
            InitializeComponent();
        }
    }
}
