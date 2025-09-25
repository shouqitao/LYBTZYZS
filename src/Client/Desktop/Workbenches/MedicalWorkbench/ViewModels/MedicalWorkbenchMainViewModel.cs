using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Workbench.Medical.Navigation;
using LYBT.Desktop.Workbench.Core;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Workbench.Medical.ViewModels
{

    /// <summary>
    /// 诊疗工作台主视图模型
    /// 为医生提供专业的诊疗相关功能导航
    /// </summary>
    public class MedicalWorkbenchMainViewModel : ModernViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IWorkbenchRouter _workbenchRouter;
        private readonly IMedicalWorkbenchNavigator _navigator;
        private readonly IPatientService? _patientService;

        #region Properties

        private int _newPatientsCount = 0;

        public int NewPatientsCount
        {
            get => _newPatientsCount;
            set => SetProperty(ref _newPatientsCount, value);
        }

        private int _todayConsultationsCount = 0;

        public int TodayConsultationsCount
        {
            get => _todayConsultationsCount;
            set => SetProperty(ref _todayConsultationsCount, value);
        }

        public Visibility PatientsNotificationVisibility =>
            NewPatientsCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ConsultationsNotificationVisibility =>
            TodayConsultationsCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        #endregion Properties

        #region Commands

        public ICommand QuickAddPatientCommand { get; }
        public ICommand StartConsultationCommand { get; }
        public ICommand NavigateToPatientsCommand { get; }
        public ICommand NavigateToConsultationsCommand { get; }
        public ICommand NavigateToMedicalCasesCommand { get; }
        public ICommand NavigateToPrescriptionsCommand { get; }
        public ICommand NavigateToFormulasCommand { get; }
        public ICommand NavigateToPersonalSettingsCommand { get; }

        #endregion Commands

        public MedicalWorkbenchMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IWorkbenchRouter workbenchRouter,
            IMedicalWorkbenchNavigator navigator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService errorHandlingService,
            IPatientService? patientService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _workbenchRouter = workbenchRouter ?? throw new ArgumentNullException(nameof(workbenchRouter));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _patientService = patientService; // 可为null，取决于是否注册了服务

            // 初始化命令
            QuickAddPatientCommand = new DelegateCommand(ExecuteQuickAddPatient);
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation);
            NavigateToPatientsCommand = new DelegateCommand(ExecuteNavigateToPatients);
            NavigateToConsultationsCommand = new DelegateCommand(ExecuteNavigateToConsultations);
            NavigateToMedicalCasesCommand = new DelegateCommand(ExecuteNavigateToMedicalCases);
            NavigateToPrescriptionsCommand = new DelegateCommand(ExecuteNavigateToPrescriptions);
            NavigateToFormulasCommand = new DelegateCommand(ExecuteNavigateToFormulas);
            NavigateToPersonalSettingsCommand = new DelegateCommand(ExecuteNavigateToPersonalSettings);

            // 初始化数据
            LoadNotificationCounts();
        }

        #region Command Implementations

        private void ExecuteQuickAddPatient()
        {
            try
            {
                // 快速添加患者 - 打开患者新增对话框
                _navigator.NavigateToPatients();

                // TODO: 可以考虑直接打开新增对话框
                // var parameters = new NavigationParameters();
                // parameters.Add("Action", "Add");
                // _navigator.NavigateToView("PatientAddEditDialog", parameters);
            }
            catch (Exception ex)
            {
                // TODO: 添加日志记录
                System.Diagnostics.Debug.WriteLine($"快速添加患者失败: {ex.Message}");
            }
        }

        private void ExecuteStartConsultation()
        {
            try
            {
                // 开始诊疗 - 先导航到患者管理，让医生选择患者
                _navigator.NavigateToPatients();

                // TODO: 可以考虑显示今日预约患者列表
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始诊疗失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToPatients()
        {
            try
            {
                _navigator.NavigateToPatients();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到患者管理失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToConsultations()
        {
            try
            {
                _navigator.NavigateToConsultations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到诊疗管理失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToMedicalCases()
        {
            try
            {
                _navigator.NavigateToMedicalCases();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到医疗案例失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToPrescriptions()
        {
            try
            {
                _navigator.NavigateToPrescriptions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到处方管理失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToFormulas()
        {
            try
            {
                _navigator.NavigateToFormulas();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到验方模板失败: {ex.Message}");
            }
        }

        private void ExecuteNavigateToPersonalSettings()
        {
            try
            {
                _navigator.NavigateToPersonalSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到个人设置失败: {ex.Message}");
            }
        }

        #endregion Command Implementations

        #region Private Methods

        private void LoadNotificationCounts()
        {
            try
            {
                // TODO: 实际项目中应该从服务加载真实数据
                // 这里使用模拟数据作为示例
                NewPatientsCount = 3; // 新患者数量
                TodayConsultationsCount = 8; // 今日诊疗数量

                // 更新可见性属性
                RaisePropertyChanged(nameof(PatientsNotificationVisibility));
                RaisePropertyChanged(nameof(ConsultationsNotificationVisibility));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载通知数据失败: {ex.Message}");
            }
        }

        #endregion Private Methods
    }
}
