using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 患者历史视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的PatientMasterDetailControl
    /// View在角色台，Control在业务模块
    /// 用于医生查看患者详细信息和历史记录
    /// </summary>
    public partial class PatientHistoryView : UserControl
    {
        public PatientHistoryView()
        {
            InitializeComponent();
        }
    }
}
