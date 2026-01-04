using System.ComponentModel;
using System.Windows.Controls;
using LYBT.Desktop.Users.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.Users.Controls
{
    /// <summary>
    /// 用户Master-Detail控件
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 可复用业务控件，供Admin角色台使用
    /// 从UserMasterDetailView重构而来
    /// 为保持架构一致性，采用Control模式
    ///
    /// 复用UserMasterDetailViewModel作为DataContext
    /// </summary>
    public partial class UserMasterDetailControl : UserControl
    {
        public UserMasterDetailControl()
        {
            InitializeComponent();

            // 设计时跳过ViewModel解析，避免空引用异常
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // 从DI容器解析ViewModel并设置DataContext
            // 这样可以复用现有的UserMasterDetailViewModel
            var container = ContainerLocator.Container;
            DataContext = container.Resolve<UserMasterDetailViewModel>();
        }
    }
}
