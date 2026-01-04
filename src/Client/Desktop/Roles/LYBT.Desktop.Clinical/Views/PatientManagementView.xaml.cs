using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 患者管理视图
    /// OpenSpec: rename-reference-to-management
    ///
    /// 薄包装View，复用业务模块的PatientMasterDetailControl
    /// View在角色台，Control在业务模块
    ///
    /// 权限设计：
    /// - 诊所共享患者：只读参考
    /// - 医生自创患者：可完整管理
    /// </summary>
    public partial class PatientManagementView : UserControl
    {
        public PatientManagementView()
        {
            InitializeComponent();
        }
    }
}
