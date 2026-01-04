using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 药材管理视图
    /// OpenSpec: rename-reference-to-management
    ///
    /// 薄包装View，复用业务模块的HerbMasterDetailControl
    /// View在角色台，Control在业务模块
    ///
    /// 权限设计：
    /// - 诊所共享药材：只读参考
    /// - 医生自创药材：可完整管理
    /// </summary>
    public partial class HerbManagementView : UserControl
    {
        public HerbManagementView()
        {
            InitializeComponent();
        }
    }
}
