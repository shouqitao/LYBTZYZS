using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 管理员工作台主页视图模型
    /// 核心功能：6个功能卡片导航
    /// Issue #1553: 角色模块化重构 - Admin模块
    /// </summary>
    public class AdminHomeViewModel : UnifiedViewModelBase
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;

        #endregion 依赖服务

        #region 命令

        /// <summary>
        /// 导航到用户管理
        /// </summary>
        public DelegateCommand NavigateToUserManagementCommand { get; }

        /// <summary>
        /// 导航到药材管理
        /// </summary>
        public DelegateCommand NavigateToHerbManagementCommand { get; }

        /// <summary>
        /// 导航到患者管理
        /// </summary>
        public DelegateCommand NavigateToPatientManagementCommand { get; }

        /// <summary>
        /// 导航到验方管理
        /// </summary>
        public DelegateCommand NavigateToFormulaManagementCommand { get; }

        /// <summary>
        /// 导航到病历管理
        /// </summary>
        public DelegateCommand NavigateToMedicalCaseManagementCommand { get; }

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        public DelegateCommand NavigateToSystemSettingsCommand { get; }

        #endregion 命令

        #region 构造函数

        public AdminHomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化导航命令
            NavigateToUserManagementCommand = new DelegateCommand(() => NavigateTo("UserManagementView"));
            NavigateToHerbManagementCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToFormulaManagementCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));
            NavigateToMedicalCaseManagementCommand = new DelegateCommand(() => NavigateTo("MedicalCaseManagementView"));
            NavigateToSystemSettingsCommand = new DelegateCommand(() => NavigateTo("SystemSettingsView"));
        }

        #endregion 构造函数

        #region 辅助方法

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        private void NavigateTo(string viewName)
        {
            try
            {
                Logger.LogInformation("导航到 {ViewName}", viewName);
                _regionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
                {
                    if (navigationResult.Result == true)
                    {
                        Logger.LogInformation("导航成功：{ViewName}", viewName);
                    }
                    else
                    {
                        Logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, navigationResult.Error?.Message ?? "未知错误");
                        if (navigationResult.Error != null)
                        {
                            Logger.LogError(navigationResult.Error, "导航异常详情");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            }
        }

        #endregion 辅助方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 导航到主页时的逻辑（如果需要）
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 简化实现 - 无需清理
        }

        #endregion INavigationAware
    }
}
