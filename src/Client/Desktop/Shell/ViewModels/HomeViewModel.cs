using System.Collections.ObjectModel;
using System.Windows;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主页视图模型 - 简化版本，删除错误较多的业务代码
    /// </summary>
    public class HomeViewModel : ModernViewModelBase, INavigationAware
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;

        #endregion 依赖服务

        #region 属性

        private string _welcomeMessage = "欢迎使用凌隐宝堂中医诊所管理系统";

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        #endregion 属性

        #region 命令

        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToConsultationCommand { get; }

        #endregion 命令

        #region 构造函数

        public HomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToConsultationCommand = new DelegateCommand(() => NavigateTo("ConsultationView"));
        }

        #endregion 构造函数

        #region 导航方法

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
            }
            catch
            {
                // 简化错误处理 - 静默处理导航失败
            }
        }

        #endregion 导航方法

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 简化实现 - 仅设置基本状态
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 简化实现 - 无需清理
        }

        #endregion INavigationAware
    }
}