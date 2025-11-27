using System;
using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views
{
    /// <summary>
    /// MedicalCaseFlowView.xaml 的交互逻辑
    /// Epic #1494 - Task #1496: 医案流程主视图
    ///
    /// [已废弃] Epic #2210 Phase 4: 此视图已被MedicalCaseWorkspaceView替代
    /// MedicalCaseWorkspaceView采用4:6统一布局（诊断40% + 处方60%）
    /// 请使用MedicalCaseWorkspaceView进行新的开发
    /// </summary>
    [Obsolete("此视图已被MedicalCaseWorkspaceView替代，采用4:6统一布局。请使用MedicalCaseWorkspaceView。")]
    public partial class MedicalCaseFlowView : UserControl
    {
        public MedicalCaseFlowView()
        {
            InitializeComponent();
        }
    }
}
