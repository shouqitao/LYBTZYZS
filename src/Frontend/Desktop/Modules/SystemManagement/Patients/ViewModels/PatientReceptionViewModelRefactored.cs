using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 重构后的患者接待ViewModel - UltraThink架构实现
    /// 职责单一：作为4个专门组件的协调器和UI绑定层
    /// 代码干净：简洁的组件组合和清晰的职责分离  
    /// 性能出色：优化的组件协作和资源管理
    /// 
    /// 从原来的632行超大文件，重构为简洁的协调器模式：
    /// - PatientDataManager: 数据管理
    /// - PatientSearchService: 搜索服务
    /// - ReceptionWorkflowCoordinator: 工作流协调
    /// - ReceptionValidationService: 验证服务
    /// </summary>
    public class PatientReceptionViewModelRefactored : BindableBase, INavigationAware, IDisposable
    {
        #region UltraThink专门化组件

        private readonly PatientDataManager _dataManager;
        private readonly PatientSearchService _searchService;
        private readonly ReceptionWorkflowCoordinator _workflowCoordinator;
        private readonly ReceptionValidationService _validationService;
        private readonly ILogger<PatientReceptionViewModelRefactored> _logger;

        #endregion

        #region UI绑定属性（委托给DataManager）

        /// <summary>
        /// 标题
        /// </summary>
        public string Title => "患者接待";

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _dataManager.SearchKeyword;
            set
            {
                if (_dataManager.SearchKeyword != value)
                {
                    _dataManager.SearchKeyword = value;
                    RaisePropertyChanged();
                    
                    // 实时搜索
                    if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                    {
                        _ = ExecuteSearchAsync();
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        _dataManager.ClearSearchResults();
                        RaisePropertyChanged(nameof(SearchResults));
                    }
                }
            }
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public ObservableCollection<PatientInfo> SearchResults => _dataManager.SearchResults;

        /// <summary>
        /// 选中的患者
        /// </summary>
        public PatientInfo? SelectedPatient
        {
            get => _dataManager.SelectedPatient;
            set
            {
                if (_dataManager.SelectedPatient != value)
                {
                    _dataManager.SetSelectedPatient(value);
                    RaisePropertyChanged();
                    
                    // 加载患者详情
                    if (value != null)
                    {
                        _ = LoadPatientDetailsAsync(value.Id);
                    }
                    
                    // 更新命令状态
                    RefreshCommandStates();
                }
            }
        }

        /// <summary>
        /// 患者详细信息
        /// </summary>
        public PatientDetailDto? PatientDetails => _dataManager.PatientDetails;

        /// <summary>
        /// 最近的医疗案例
        /// </summary>
        public ObservableCollection<MedicalCaseInfo> RecentCases => _dataManager.RecentCases;

        /// <summary>
        /// 加载状态
        /// </summary>
        public bool IsLoading => _dataManager.IsLoading;

        /// <summary>
        /// 是否新患者
        /// </summary>
        public bool IsNewPatient => _dataManager.IsNewPatient;

        #region 快速接待表单属性

        public string PatientName
        {
            get => _dataManager.PatientName;
            set
            {
                _dataManager.PatientName = value;
                RaisePropertyChanged();
                RefreshCommandStates();
            }
        }

        public string PatientGender
        {
            get => _dataManager.PatientGender;
            set
            {
                _dataManager.PatientGender = value;
                RaisePropertyChanged();
            }
        }

        public string PatientAge
        {
            get => _dataManager.PatientAge;
            set
            {
                _dataManager.PatientAge = value;
                RaisePropertyChanged();
            }
        }

        public string PatientPhone
        {
            get => _dataManager.PatientPhone;
            set
            {
                _dataManager.PatientPhone = value;
                RaisePropertyChanged();
                RefreshCommandStates();
            }
        }

        public string PatientIdCard
        {
            get => _dataManager.PatientIdCard;
            set
            {
                _dataManager.PatientIdCard = value;
                RaisePropertyChanged();
            }
        }

        public string ChiefComplaint
        {
            get => _dataManager.ChiefComplaint;
            set
            {
                _dataManager.ChiefComplaint = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #endregion

        #region 命令属性

        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand QuickReceptionCommand { get; private set; } = null!;
        public ICommand CreateMedicalCaseCommand { get; private set; } = null!;
        public ICommand StartConsultationCommand { get; private set; } = null!;
        public ICommand ViewPatientDetailsCommand { get; private set; } = null!;
        public ICommand RegisterNewPatientCommand { get; private set; } = null!;
        public ICommand ClearFormCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        public PatientReceptionViewModelRefactored(
            IPatientService patientService,
            IMedicalCaseService medicalCaseService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            ILogger<PatientReceptionViewModelRefactored> logger,
            ILogger<PatientDataManager> dataManagerLogger,
            ILogger<PatientSearchService> searchServiceLogger,
            ILogger<ReceptionWorkflowCoordinator> workflowCoordinatorLogger,
            ILogger<ReceptionValidationService> validationServiceLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _logger.LogDebug("开始初始化重构后的PatientReceptionViewModel");

                // 创建专门化组件
                _dataManager = new PatientDataManager(dataManagerLogger);
                _searchService = new PatientSearchService(patientService, medicalCaseService, searchServiceLogger);
                _workflowCoordinator = new ReceptionWorkflowCoordinator(
                    medicalCaseService, userSessionManager, dialogService, 
                    prismDialogService, regionManager, workflowCoordinatorLogger);
                _validationService = new ReceptionValidationService(validationServiceLogger);

                // 建立组件间的依赖关系
                EstablishComponentDependencies();

                // 初始化命令
                InitializeCommands();

                // 初始化数据
                _ = InitializeAsync();

                _logger.LogInformation("PatientReceptionViewModel重构完成，组件化架构已建立");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化PatientReceptionViewModel失败");
                throw;
            }
        }

        #endregion

        #region 组件依赖建立

        /// <summary>
        /// 建立组件间的依赖关系
        /// </summary>
        private void EstablishComponentDependencies()
        {
            try
            {
                // SearchService需要DataManager
                _searchService.SetDataManager(_dataManager);

                // WorkflowCoordinator需要所有其他组件
                _workflowCoordinator.SetDependencies(_dataManager, _searchService, _validationService);

                // ValidationService需要DataManager
                _validationService.SetDataManager(_dataManager);

                _logger.LogDebug("组件依赖关系建立完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立组件依赖关系失败");
                throw;
            }
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            QuickReceptionCommand = new DelegateCommand(async () => await ExecuteQuickReceptionAsync(), CanExecuteQuickReception);
            CreateMedicalCaseCommand = new DelegateCommand(async () => await ExecuteCreateMedicalCaseAsync(), () => SelectedPatient != null);
            StartConsultationCommand = new DelegateCommand<MedicalCaseInfo>(async mc => await ExecuteStartConsultationAsync(mc));
            ViewPatientDetailsCommand = new DelegateCommand<PatientInfo>(ExecuteViewPatientDetails);
            RegisterNewPatientCommand = new DelegateCommand(() => _workflowCoordinator.ShowNewPatientRegistrationDialog());
            ClearFormCommand = new DelegateCommand(ExecuteClearForm);
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                _dataManager.SetLoadingState(true);
                
                // 加载今日患者数据
                var result = await _searchService.LoadTodayPatientsAsync();
                if (result.IsSuccess)
                {
                    _dataManager.SetRecentCases(result.RecentCases);
                    RaisePropertyChanged(nameof(RecentCases));
                }

                _logger.LogInformation("患者接待模块初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化患者接待模块失败");
            }
            finally
            {
                _dataManager.SetLoadingState(false);
                RaisePropertyChanged(nameof(IsLoading));
            }
        }

        #endregion

        #region 命令执行方法

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                _dataManager.SetLoadingState(true);
                RaisePropertyChanged(nameof(IsLoading));

                var result = await _searchService.RealTimeSearchAsync(_dataManager.SearchKeyword);
                
                if (result.IsSuccess)
                {
                    _dataManager.SetSearchResults(result.Patients);
                    RaisePropertyChanged(nameof(SearchResults));
                    RaisePropertyChanged(nameof(SelectedPatient));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者失败");
            }
            finally
            {
                _dataManager.SetLoadingState(false);
                RaisePropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// 执行快速接待
        /// </summary>
        private async Task ExecuteQuickReceptionAsync()
        {
            try
            {
                var result = await _workflowCoordinator.ExecuteQuickReceptionAsync();
                
                if (result.IsSuccess)
                {
                    if (result.ShouldStartConsultation && result.MedicalCase != null)
                    {
                        _workflowCoordinator.NavigateToConsultation(result.MedicalCase);
                    }
                    else
                    {
                        await ExecuteRefreshAsync();
                        ExecuteClearForm();
                    }
                    
                    // 刷新UI
                    RefreshAllProperties();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速接待失败");
            }
        }

        /// <summary>
        /// 执行创建医疗案例
        /// </summary>
        private async Task ExecuteCreateMedicalCaseAsync()
        {
            try
            {
                var result = await _workflowCoordinator.CreateMedicalCaseForSelectedPatientAsync();
                if (result.IsSuccess)
                {
                    RaisePropertyChanged(nameof(RecentCases));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
            }
        }

        /// <summary>
        /// 执行开始看诊
        /// </summary>
        private async Task ExecuteStartConsultationAsync(MedicalCaseInfo? medicalCase)
        {
            if (medicalCase != null)
            {
                await _workflowCoordinator.StartConsultationAsync(medicalCase);
            }
        }

        /// <summary>
        /// 执行查看患者详情
        /// </summary>
        private void ExecuteViewPatientDetails(PatientInfo? patient)
        {
            if (patient != null)
            {
                _workflowCoordinator.ViewPatientDetails(patient.Id);
            }
        }

        /// <summary>
        /// 执行清空表单
        /// </summary>
        private void ExecuteClearForm()
        {
            _dataManager.ClearForm();
            RefreshAllProperties();
        }

        /// <summary>
        /// 执行刷新
        /// </summary>
        private async Task ExecuteRefreshAsync()
        {
            await _workflowCoordinator.RefreshReceptionDataAsync();
            RaisePropertyChanged(nameof(RecentCases));
        }

        /// <summary>
        /// 加载患者详情
        /// </summary>
        private async Task LoadPatientDetailsAsync(Guid patientId)
        {
            try
            {
                var result = await _searchService.LoadPatientDetailsAsync(patientId);
                if (result.IsSuccess)
                {
                    _dataManager.SetPatientDetails(result.PatientDetails);
                    _dataManager.SetRecentCases(result.MedicalCases);
                    
                    RaisePropertyChanged(nameof(PatientDetails));
                    RaisePropertyChanged(nameof(RecentCases));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者详情失败");
            }
        }

        #endregion

        #region 命令条件检查

        private bool CanExecuteQuickReception()
        {
            return _dataManager.IsQuickReceptionFormValid() && !_dataManager.IsLoading;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            ((DelegateCommand)QuickReceptionCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)CreateMedicalCaseCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 刷新所有属性
        /// </summary>
        private void RefreshAllProperties()
        {
            RaisePropertyChanged(nameof(SearchKeyword));
            RaisePropertyChanged(nameof(SearchResults));
            RaisePropertyChanged(nameof(SelectedPatient));
            RaisePropertyChanged(nameof(PatientDetails));
            RaisePropertyChanged(nameof(RecentCases));
            RaisePropertyChanged(nameof(IsLoading));
            RaisePropertyChanged(nameof(IsNewPatient));
            RaisePropertyChanged(nameof(PatientName));
            RaisePropertyChanged(nameof(PatientGender));
            RaisePropertyChanged(nameof(PatientAge));
            RaisePropertyChanged(nameof(PatientPhone));
            RaisePropertyChanged(nameof(PatientIdCard));
            RaisePropertyChanged(nameof(ChiefComplaint));
        }

        #endregion

        #region INavigationAware实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                // 如果有患者ID参数，直接加载该患者
                if (navigationContext.Parameters.ContainsKey("PatientId"))
                {
                    var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                    _ = LoadPatientDetailsAsync(patientId);
                }

                // 刷新数据
                _ = ExecuteRefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到患者接待页面失败");
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理表单
            ExecuteClearForm();
        }

        #endregion

        #region IDisposable实现

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _logger.LogDebug("PatientReceptionViewModel资源清理完成");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理PatientReceptionViewModel资源失败");
                    }
                }

                _disposed = true;
            }
        }

        #endregion
    }
}