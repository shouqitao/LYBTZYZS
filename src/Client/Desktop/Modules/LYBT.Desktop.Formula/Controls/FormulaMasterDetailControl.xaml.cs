using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Desktop.Formula.ViewModels;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方Master-Detail控件
    /// OpenSpec: refactor-frontend-srp-patterns - 继承MasterDetailControlBase基类
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class FormulaMasterDetailControl : MasterDetailControlBase
    {
        public FormulaMasterDetailControl()
        {
            InitializeComponent();
            InitializeViewModel<FormulaMasterDetailViewModel>();
        }
    }
}
