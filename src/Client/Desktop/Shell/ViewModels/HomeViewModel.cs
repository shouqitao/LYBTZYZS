using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主页视图模型 - Phase 2 功能扩充版本
    /// </summary>
    public class HomeViewModel : UnifiedViewModelBase
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

        // 原有导航命令
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToConsultationCommand { get; }

        // Phase 2 新增命令
        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand StartConsultationCommand { get; }
        public DelegateCommand RefreshTodayPatientsCommand { get; }

        // DataGrid 行命令（今日患者列表）
        public DelegateCommand<object> StartConsultationForPatientCommand { get; }
        public DelegateCommand<object> ViewPatientDetailsCommand { get; }

        // 导航命令组
        public DelegateCommand NavigateToPatientReceptionCommand { get; }
        public DelegateCommand NavigateToMedicalCaseCommand { get; }
        public DelegateCommand NavigateToPrescriptionQueryCommand { get; }
        public DelegateCommand NavigateToHerbsCommand { get; }
        public DelegateCommand NavigateToFormulasCommand { get; }
        public DelegateCommand EnterSystemManagementCommand { get; }
        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToFormulaManagementCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        public DelegateCommand NavigateToDataBackupCommand { get; }

        #endregion 命令

        #region 构造函数

        public HomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化原有命令
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToConsultationCommand = new DelegateCommand(() => NavigateTo("ConsultationView"));

            // 初始化 Phase 2 新增命令
            LogoutCommand = new DelegateCommand(ExecuteLogout);
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation);
            RefreshTodayPatientsCommand = new DelegateCommand(ExecuteRefreshTodayPatients);

            // DataGrid 行命令
            StartConsultationForPatientCommand = new DelegateCommand<object>(ExecuteStartConsultationForPatient);
            ViewPatientDetailsCommand = new DelegateCommand<object>(ExecuteViewPatientDetails);

            // 导航命令组
            NavigateToPatientReceptionCommand = new DelegateCommand(() => NavigateTo("PatientReceptionView"));
            NavigateToMedicalCaseCommand = new DelegateCommand(() => NavigateTo("MedicalCaseManagementView"));
            NavigateToPrescriptionQueryCommand = new DelegateCommand(() => NavigateTo("PrescriptionManagementView"));
            NavigateToHerbsCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToFormulasCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));
            EnterSystemManagementCommand = new DelegateCommand(() => NavigateTo("AdminWorkstationView"));
            NavigateToUserManagementCommand = new DelegateCommand(() => NavigateTo("UserManagementView"));
            NavigateToHerbManagementCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToFormulaManagementCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));
            NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);
            NavigateToDataBackupCommand = new DelegateCommand(ExecuteNavigateToDataBackup);
        }

        #endregion 构造函数

        #region 命令实现

        /// <summary>
        /// 退出登录
        /// </summary>
        private void ExecuteLogout()
        {
            try
            {
                Logger.LogInformation("用户退出登录");
                // TODO: 清理会话状态
                NavigateTo("LoginView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "退出登录时发生异常");
            }
        }

        /// <summary>
        /// 开始诊疗
        /// </summary>
        private void ExecuteStartConsultation()
        {
            try
            {
                Logger.LogInformation("开始诊疗");
                NavigateTo("ClinicalWorkstationView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始诊疗时发生异常");
            }
        }

        /// <summary>
        /// 刷新今日患者列表
        /// </summary>
        private void ExecuteRefreshTodayPatients()
        {
            try
            {
                Logger.LogInformation("刷新今日患者列表");
                // TODO: 实现刷新今日患者列表逻辑
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新今日患者列表时发生异常");
            }
        }

        /// <summary>
        /// 为指定患者开始诊疗
        /// </summary>
        private void ExecuteStartConsultationForPatient(object patient)
        {
            if (patient == null) return;

            try
            {
                Logger.LogInformation("为患者开始诊疗");
                // TODO: 带患者信息导航到诊疗工作台
                NavigateTo("ClinicalWorkstationView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "为患者开始诊疗时发生异常");
            }
        }

        /// <summary>
        /// 查看患者详情
        /// </summary>
        private void ExecuteViewPatientDetails(object patient)
        {
            if (patient == null) return;

            try
            {
                Logger.LogInformation("查看患者详情");
                // TODO: 带患者 ID 导航到患者详情页
                NavigateTo("PatientDetailView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看患者详情时发生异常");
            }
        }

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        private void ExecuteNavigateToSystemSettings()
        {
            try
            {
                Logger.LogInformation("导航到系统设置");
                // TODO: 实现系统设置功能
                Logger.LogWarning("系统设置功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到系统设置时发生异常");
            }
        }

        /// <summary>
        /// 导航到数据备份
        /// </summary>
        private void ExecuteNavigateToDataBackup()
        {
            try
            {
                Logger.LogInformation("导航到数据备份");
                // TODO: 实现数据备份功能
                Logger.LogWarning("数据备份功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到数据备份时发生异常");
            }
        }

        #endregion 命令实现

        #region 导航方法

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到 {ViewName} 失败", viewName);
            }
        }

        #endregion 导航方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 简化实现 - 仅设置基本状态
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
