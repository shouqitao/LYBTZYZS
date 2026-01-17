using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Desktop.Herbs.ViewModels;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材Master-Detail控件
    /// OpenSpec: refactor-frontend-srp-patterns - 继承MasterDetailControlBase基类
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class HerbMasterDetailControl : MasterDetailControlBase
    {
        public HerbMasterDetailControl()
        {
            InitializeComponent();
            InitializeViewModel<HerbMasterDetailViewModel>();
        }
    }
}
