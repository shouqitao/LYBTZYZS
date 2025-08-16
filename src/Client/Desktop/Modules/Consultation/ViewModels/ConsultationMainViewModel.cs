using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;

// UltraThink重构: 统一HerbInfo和HerbDto，FormulaInfo和FormulaDto，使用Dto作为统一模型
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Models.Formulas;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - 增强版（集成MedicalCase）
    /// 负责协调看诊流程，与医疗案例模块深度集成
    /// </summary>
    public class ConsultationMainViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IConsultationDataService _dataService;
        private readonly IConsultationService _consultationService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPrescriptionManager _prescriptionManager;
        private readonly IFormulaManager _formulaManager;
        private readonly IConsultationValidator _validator;
        private readonly IConsultationEventHandler _eventHandler;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ConsultationMainViewModel> _logger;

        #endregion

        #region 属性

        private string _title = "看诊工作台";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ObservableCollection<PatientInfo> _patients = new();
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    OnPatientSelected();
                }
            }
        }

        private ConsultationInfo? _currentConsultation;
        public ConsultationInfo? CurrentConsultation
        {
            get => _currentConsultation;
            set => SetProperty(ref _currentConsultation, value);
        }

        private MedicalCaseInfo? _currentMedicalCase;
        public MedicalCaseInfo? CurrentMedicalCase
        {
            get => _currentMedicalCase;
            set => SetProperty(ref _currentMedicalCase, value);
        }

        private Guid? _medicalCaseId;
        public Guid? MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private bool _isNavigatedFromMedicalCase;
        public bool IsNavigatedFromMedicalCase
        {
            get => _isNavigatedFromMedicalCase;
            set => SetProperty(ref _isNavigatedFromMedicalCase, value);
        }

        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems => 
            _prescriptionManager.PrescriptionItems;

        public decimal TotalPrice => _prescriptionManager.TotalPrice;

        private ObservableCollection<HerbDto> _availableHerbs = new();
        public ObservableCollection<HerbDto> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private ObservableCollection<FormulaInfo> _availableFormulas = new();
        public ObservableCollection<FormulaInfo> AvailableFormulas
        {
            get => _availableFormulas;
            set => SetProperty(ref _availableFormulas, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }
        public ICommand NewConsultationCommand { get; }
        public ICommand SaveConsultationCommand { get; }
        public ICommand AddHerbCommand { get; }
        public ICommand RemoveHerbCommand { get; }
        public ICommand ApplyFormulaCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationMainViewModel(
            IConsultationDataService dataService,
            IConsultationService consultationService,
            IMedicalCaseService medicalCaseService,
            IPrescriptionManager prescriptionManager,
            IFormulaManager formulaManager,
            IConsultationValidator validator,
            IConsultationEventHandler eventHandler,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILogger<ConsultationMainViewModel> logger)
        {
            _dataService = dataService;
            _consultationService = consultationService;
            _medicalCaseService = medicalCaseService;
            _prescriptionManager = prescriptionManager;
            _formulaManager = formulaManager;
            _validator = validator;
            _eventHandler = eventHandler;
            _dialogService = dialogService;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _logger = logger;

            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());
            NewConsultationCommand = new DelegateCommand(async () => await StartNewConsultationAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            AddHerbCommand = new DelegateCommand<HerbDto>(AddHerbToPrescription);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemInfo>(RemoveHerbFromPrescription);
            ApplyFormulaCommand = new DelegateCommand<FormulaInfo>(async f => await ApplyFormulaAsync(f));

            SubscribeToEvents();
            _ = InitializeAsync();
        }

        #endregion

        #region 初始化和数据加载

        private async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await RefreshDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            var patientsTask = _dataService.LoadPatientsAsync();
            var herbsTask = _dataService.LoadHerbsAsync();
            var formulasTask = _dataService.LoadFormulasAsync();

            await Task.WhenAll(patientsTask, herbsTask, formulasTask);

            Patients = new ObservableCollection<PatientInfo>(await patientsTask);
            AvailableHerbs = new ObservableCollection<HerbDto>(await herbsTask);
            AvailableFormulas = new ObservableCollection<FormulaInfo>(await formulasTask);

            _eventHandler.PublishStatusMessage("数据刷新完成", StatusMessageType.Success);
        }

        #endregion

        #region 看诊流程

        private void OnPatientSelected()
        {
            if (SelectedPatient != null)
            {
                _eventHandler.PublishPatientSelected(SelectedPatient);
            }
        }

        private async Task StartNewConsultationAsync()
        {
            if (SelectedPatient == null) return;

            var validation = _validator.ValidatePatientForConsultation(SelectedPatient);
            if (!validation.IsValid)
            {
                _eventHandler.PublishError("Consultation", validation.FirstError ?? "患者验证失败");
                return;
            }

            CurrentConsultation = await _dataService.CreateConsultationAsync(SelectedPatient.Id);
            if (CurrentConsultation != null)
            {
                _prescriptionManager.ClearPrescription();
                _eventHandler.PublishConsultationStarted(CurrentConsultation);
            }
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            var validation = await _prescriptionManager.ValidatePrescriptionAsync();
            if (!validation)
            {
                _eventHandler.PublishError("Prescription", "处方验证失败");
                return;
            }

            var saved = await _prescriptionManager.SavePrescriptionAsync(
                CurrentConsultation.Id,
                CurrentConsultation.Diagnosis ?? "",
                "汤剂", 7, "每日一剂，水煎服");

            if (saved)
            {
                // 如果从MedicalCase导航过来，更新MedicalCase状态
                if (IsNavigatedFromMedicalCase && MedicalCaseId.HasValue)
                {
                    await UpdateMedicalCaseStatusAsync();
                }

                _eventHandler.PublishConsultationCompleted(CurrentConsultation);
                
                // 提示用户
                await _dialogService.ShowSuccessAsync("看诊已完成并保存", "操作成功");
                
                // 如果从MedicalCase导航过来，返回到MedicalCase详情
                if (IsNavigatedFromMedicalCase)
                {
                    NavigateBackToMedicalCase();
                }
                else
                {
                    CurrentConsultation = null;
                    SelectedPatient = null;
                }
            }
        }

        private async Task UpdateMedicalCaseStatusAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
                // 更新医疗案例状态为已完成
                var result = await _medicalCaseService.UpdateStatusAsync(
                    MedicalCaseId.Value, 
                    LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);
                
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("更新医疗案例状态失败: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态时发生错误");
            }
        }

        private void NavigateBackToMedicalCase()
        {
            if (MedicalCaseId.HasValue)
            {
                _regionManager.RequestNavigate("MainContentRegion", $"MedicalCaseDetailView?MedicalCaseId={MedicalCaseId.Value}&EditMode=false");
            }
        }

        #endregion

        #region 处方管理

        private void AddHerbToPrescription(HerbDto? herb)
        {
            if (herb != null && _prescriptionManager.AddHerbToPrescription(herb))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }

        private void RemoveHerbFromPrescription(PrescriptionItemInfo? item)
        {
            if (item != null && _prescriptionManager.RemoveHerbFromPrescription(item.HerbId))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }

        private async Task ApplyFormulaAsync(FormulaInfo? formula)
        {
            if (formula == null) return;

            var items = _formulaManager.ApplyFormulaTemplate(formula);
            _prescriptionManager.ImportPrescriptionItems(items);
            RaisePropertyChanged(nameof(TotalPrice));

            await Task.CompletedTask;
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            _eventHandler.SubscribeToDataRefreshRequest(async args => 
            {
                if (args.RefreshType == DataRefreshType.All)
                    await RefreshDataAsync();
            });
        }

        #endregion

        #region INavigationAware Implementation

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 检查是否从MedicalCase模块导航过来
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                IsNavigatedFromMedicalCase = true;

                // 加载医疗案例信息
                Task.Run(async () => await LoadMedicalCaseAsync());
            }

            // 检查是否有患者ID
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                Task.Run(async () => await LoadPatientAsync(patientId));
            }

            // 检查看诊模式
            if (navigationContext.Parameters.ContainsKey("ConsultationMode"))
            {
                var mode = navigationContext.Parameters.GetValue<string>("ConsultationMode");
                if (mode == "Start")
                {
                    Task.Run(async () => await StartConsultationFromMedicalCaseAsync());
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 如果有特定的医疗案例ID，检查是否匹配
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                var targetMedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                return MedicalCaseId == targetMedicalCaseId;
            }
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 如果看诊未完成，提示用户
            if (CurrentConsultation != null && !CurrentConsultation.IsCompleted)
            {
                _dialogService.ShowInformationAsync("看诊尚未完成，数据已自动保存", "提示");
            }
        }

        #endregion

        #region MedicalCase Integration Methods

        private async Task LoadMedicalCaseAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
                IsLoading = true;
                var result = await _medicalCaseService.GetByIdAsync(MedicalCaseId.Value);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var dto = result.Data;
                    CurrentMedicalCase = new MedicalCaseInfo
                    {
                        Id = dto.Id,
                        PatientId = dto.PatientId,
                        PatientName = dto.PatientName,
                        UserId = Guid.Empty, // 使用默认值
                        Status = Enum.TryParse<MedicalCaseStatus>(dto.Status, out var status) ? status : MedicalCaseStatus.Registered,
                        CreateTime = dto.CreateTime
                    };
                    Title = $"看诊工作台 - {CurrentMedicalCase?.PatientName}";
                    
                    // 加载或创建关联的看诊记录
                    await LoadOrCreateConsultationAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"加载医疗案例失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医疗案例失败");
                await _dialogService.ShowErrorAsync($"加载医疗案例失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadOrCreateConsultationAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
                // 尝试获取现有的看诊记录
                var result = await _consultationService.GetByMedicalCaseIdAsync(MedicalCaseId.Value);
                
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentConsultation = result.Data;
                }
                else
                {
                    // 创建新的看诊记录
                    await StartConsultationFromMedicalCaseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载或创建看诊记录失败");
            }
        }

        private async Task StartConsultationFromMedicalCaseAsync()
        {
            if (!MedicalCaseId.HasValue || CurrentMedicalCase == null) return;

            try
            {
                var startDto = new ConsultationStartDto
                {
                    MedicalCaseId = MedicalCaseId.Value,
                    PatientId = CurrentMedicalCase.PatientId
                    // DoctorId已移除
                };

                var result = await _consultationService.StartConsultationAsync(startDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentConsultation = result.Data;
                    _eventHandler.PublishConsultationStarted(CurrentConsultation);
                    await _dialogService.ShowSuccessAsync("看诊已开始", "提示");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"开始看诊失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊失败");
                await _dialogService.ShowErrorAsync($"开始看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task LoadPatientAsync(Guid patientId)
        {
            // 从患者列表中查找并选择患者
            var patient = Patients.FirstOrDefault(p => p.Id == patientId);
            if (patient != null)
            {
                SelectedPatient = patient;
            }
            else
            {
                // 如果列表中没有，重新加载
                await RefreshDataAsync();
                SelectedPatient = Patients.FirstOrDefault(p => p.Id == patientId);
            }
        }

        #endregion
    }
}