using System.Windows.Controls;
using LYBT.Desktop.Shell.ViewModels;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Controls
{
    /// <summary>
    /// 账户设置控件 - 合并个人资料和修改密码功能
    /// OpenSpec: migrate-views-to-role-modules - 从Users模块迁移到Shell
    /// </summary>
    public partial class AccountSettingsControl : UserControl
    {
        /// <summary>
        /// 构造函数 - 内部DI解析ViewModel
        /// </summary>
        public AccountSettingsControl()
        {
            InitializeComponent();

            // Control模式：内部解析ViewModel
            DataContext = ContainerLocator.Container.Resolve<AccountSettingsViewModel>();
        }
    }
}
