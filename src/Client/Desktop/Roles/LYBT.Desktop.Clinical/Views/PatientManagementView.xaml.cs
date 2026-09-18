using System.Windows.Controls;
using Prism.Regions;

namespace LYBT.Desktop.Clinical.Views
{
    /// <summary>
    /// 患者管理视图
    ///
    /// 薄包装View，复用业务模块的PatientMasterDetailControl
    /// View在角色台，Control在业务模块
    ///
    /// 导航参数：Action=AddNew|Create → 自动新建；SearchKeyword → 预填搜索
    ///
    /// 权限设计：
    /// - 诊所共享患者：只读参考
    /// - 医生自创患者：可完整管理
    /// </summary>
    public partial class PatientManagementView : UserControl, INavigationAware
    {
        public PatientManagementView()
        {
            InitializeComponent();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 薄包装无自有 VM：转发到嵌入 Control，由 PatientMasterDetailViewModel 消费参数
            PatientMasterDetailControl.ApplyNavigationParameters(navigationContext);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
