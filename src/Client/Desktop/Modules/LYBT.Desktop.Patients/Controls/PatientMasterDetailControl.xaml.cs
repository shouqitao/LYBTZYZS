using LYBT.Desktop.Controls.Controls;
using Prism.Regions;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者Master-Detail控件
    ///
    /// 可复用业务控件，供Admin和Clinical角色台使用
    /// </summary>
    public partial class PatientMasterDetailControl : MasterDetailControlBase
    {
        public PatientMasterDetailControl()
        {
            InitializeComponent();
            InitializeAsyncSupport();
        }

        /// <summary>
        /// 消费导航参数（Action/SearchKeyword）— 供薄包装 View 的 INavigationAware 转发。
        /// 对齐 UserMasterDetailControl.SetDefaultRoleFilter 先例。
        /// </summary>
        public void ApplyNavigationParameters(NavigationContext navigationContext)
        {
            if (DataContext is ViewModels.PatientMasterDetailViewModel vm)
            {
                _ = vm.ApplyNavigationParametersAsync(navigationContext);
            }
        }
    }
}
