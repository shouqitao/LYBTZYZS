using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 药材参考视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的HerbMasterDetailControl
    /// View在角色台，Control在业务模块
    /// 用于医生诊疗时查看药材信息
    /// </summary>
    public partial class HerbReferenceView : UserControl
    {
        public HerbReferenceView()
        {
            InitializeComponent();
        }
    }
}
