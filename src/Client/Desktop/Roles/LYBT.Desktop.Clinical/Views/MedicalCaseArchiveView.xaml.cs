using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 医案归档视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的MedicalCaseMasterDetailControl
    /// View在角色台，Control在业务模块
    /// 用于医生查看和管理历史医案
    /// </summary>
    public partial class MedicalCaseArchiveView : UserControl
    {
        public MedicalCaseArchiveView()
        {
            InitializeComponent();
        }
    }
}
