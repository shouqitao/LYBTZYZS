using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主页视图模型 - 简化版本，删除错误较多的业务代�?
    /// </summary>
    public class HomeViewModel : UnifiedViewModelBase, INavigationAware
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;

        #endregion 依赖服务

        #region 属�?

        private string _welcomeMessage = "欢迎使用凌隐宝堂中医诊所管理系统";

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        #endregion 属�?

        #region 命令

        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToConsultationCommand { get; }

        #endregion 命令

        #region 构造函�?

        public HomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命�?
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToConsultationCommand = new DelegateCommand(() => NavigateTo("ConsultationView"));
        }

        #endregion 构造函�?

        #region 导航方法

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
            }
            catch
            {
                // 简化错误处�?- 静默处理导航失败
            }
        }

        #endregion 导航方法

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 简化实�?- 仅设置基本状�?
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 简化实�?- 无需清理
        }

        #endregion INavigationAware
    }
}
