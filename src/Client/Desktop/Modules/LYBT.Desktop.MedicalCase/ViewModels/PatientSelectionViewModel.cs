using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 患者选择ViewModel（Issue #1567 - 独立化患者选择）
    /// 作为"看诊"功能的中枢界面
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IPatientRepository _patientRepository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IRegionManager _regionManager;

        #endregion

        #region 属性

        private ObservableCollection<PatientDto> _patients = new();
        /// <summary>
        /// 患者列表
        /// </summary>
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        /// <summary>
        /// 选中的患者
        /// </summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    StartConsultationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _searchText = string.Empty;
        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        #endregion

        #region 命令

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand StartConsultationCommand { get; }

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            IPatientRepository patientRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            SearchCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            StartConsultationCommand = new DelegateCommand(async () => await ExecuteStartConsultationAsync(), CanExecuteStartConsultation)
                .ObservesProperty(() => SelectedPatient)
                .ObservesProperty(() => IsBusy);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 返回主页
        /// </summary>
        private void ExecuteBackToHome()
        {
            try
            {
                var homeViewName = SessionManager?.CurrentUser?.Role switch
                {
                    UserRole.Admin => "AdminHomeView",
                    UserRole.Doctor => "ClinicalHomeView",
                    _ => "ClinicalHomeView"
                };

                Logger.LogInformation("返回主页，导航到：{HomeView}", homeViewName);
                _regionManager.RequestNavigate("ContentRegion", homeViewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页时发生异常");
            }
        }

        /// <summary>
        /// 开始诊断（核心逻辑）
        /// Issue #1567 - 在这里创建MedicalCase，而不是在FlowViewModel中
        /// </summary>
        private async Task ExecuteStartConsultationAsync()
        {
            if (SelectedPatient == null)
            {
                await ShowErrorMessageAsync("请先选择患者");
                return;
            }

            try
            {
                SetIsBusy(true, "正在创建医案...");

                Logger.LogInformation("开始诊断，患者：{PatientName}（ID: {PatientId}）",
                    SelectedPatient.Name, SelectedPatient.Id);

                // 1. 检查是否有未完成的医案
                var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(SelectedPatient.Id);
                if (unfinishedCase != null)
                {
                    // TODO Phase 3: 显示确认对话框"该患者有未完成的医案，是否继续？"
                    Logger.LogInformation("检测到未完成的医案，ID: {MedicalCaseId}", unfinishedCase.Id);
                    // 暂时跳过，继续创建新医案
                }

                // 2. 创建MedicalCase
                var medicalCaseId = await CreateMedicalCaseAsync(SelectedPatient.Id);
                if (medicalCaseId == Guid.Empty)
                {
                    await ShowErrorMessageAsync("创建医案失败，请重试");
                    return;
                }

                Logger.LogInformation("医案创建成功，ID: {MedicalCaseId}", medicalCaseId);

                // 3. 导航到看病流程（MedicalCaseFlowView）
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCaseId },
                    { "CurrentPatient", SelectedPatient }
                };

                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

                Logger.LogInformation("已导航到看病流程，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始诊断时发生异常");
                await ShowErrorMessageAsync($"开始诊断失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteStartConsultation()
        {
            return SelectedPatient != null && !IsBusy;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载患者列表
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "加载患者列表...");

                Logger.LogInformation("加载患者列表，搜索关键字：{SearchText}", SearchText);

                // 调用API获取患者列表（带搜索）
                var patients = await _patientRepository.SearchAsync(SearchText);

                Patients = new ObservableCollection<PatientDto>(patients);

                Logger.LogInformation("患者列表加载完成，共 {Count} 条", Patients.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查患者是否有未完成的医案
        /// </summary>
        private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
        {
            try
            {
                // TODO Phase 3: 实现检查逻辑
                // 查询该患者的所有医案，Status=InProgress的即为未完成
                await Task.CompletedTask;
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查未完成医案失败，PatientId: {PatientId}", patientId);
                return null;
            }
        }

        /// <summary>
        /// 创建MedicalCase
        /// Issue #1567 - 从MedicalCaseFlowViewModel迁移到此处
        /// </summary>
        private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
        {
            try
            {
                Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

                if (SessionManager == null || SessionManager.CurrentUser == null)
                {
                    Logger.LogError("SessionManager或CurrentUser为null，无法创建MedicalCase");
                    return Guid.Empty;
                }

                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = patientId,
                    DoctorId = SessionManager.CurrentUser.Id,
                    Status = MedicalCaseStatus.Active,
                    Remark = null
                };

                Logger.LogInformation("调用API创建MedicalCase，DoctorId: {DoctorId}", createDto.DoctorId);

                var createdDto = await _medicalCaseRepository.CreateAsync(createDto);

                Logger.LogInformation("MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);
                return createdDto.Id;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建MedicalCase失败，PatientId: {PatientId}", patientId);
                return Guid.Empty;
            }
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            Logger.LogInformation("进入患者选择界面");

            // 自动加载患者列表
            _ = LoadPatientsAsync();
        }

        #endregion
    }
}
