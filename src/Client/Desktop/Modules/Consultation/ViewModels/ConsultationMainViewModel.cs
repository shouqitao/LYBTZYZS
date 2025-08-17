using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using AutoMapper;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - UltraThink架构标准版本
    /// 完全使用IConsultationModuleService实现模块自包含，符合四层架构规范
    /// 负责协调看诊流程，与医疗案例模块深度集成
    /// </summary>
    public class ConsultationMainViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IConsultationModuleService _consultationModuleService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ConsultationMainViewModel> _logger;
        private readonly IMapper _mapper;

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
            _consultationModuleService.GetCurrentPrescriptionItems();

        public decimal TotalPrice
        {
            get
            {
                var totalResult = _consultationModuleService.CalculatePrescriptionTotalAsync(CurrentConsultation?.Id ?? Guid.Empty).GetAwaiter().GetResult();
                return totalResult.IsSuccess ? totalResult.Data : 0;
            }
        }

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
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
            IConsultationModuleService consultationModuleService,
            IMedicalCaseService medicalCaseService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILogger<ConsultationMainViewModel> logger,
            IMapper mapper)
        {
            _consultationModuleService = consultationModuleService ?? throw new ArgumentNullException(nameof(consultationModuleService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());
            NewConsultationCommand = new DelegateCommand(async () => await StartNewConsultationAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            AddHerbCommand = new DelegateCommand<HerbInfo>(AddHerbToPrescription);
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
            try
            {
                // UltraThink四层架构：使用模块化服务加载数据
                var patientsResult = await _consultationModuleService.GetAllPatientsAsync();
                var herbsResult = await _consultationModuleService.GetAllHerbsAsync();
                var formulasResult = await _consultationModuleService.GetAllFormulasAsync();

                if (patientsResult.IsSuccess)
                {
                    Patients = new ObservableCollection<PatientInfo>(patientsResult.Data);
                }

                if (herbsResult.IsSuccess)
                {
                    AvailableHerbs = new ObservableCollection<HerbInfo>(herbsResult.Data);
                }

                if (formulasResult.IsSuccess)
                {
                    AvailableFormulas = new ObservableCollection<FormulaInfo>(formulasResult.Data);
                }

                // 发布状态消息
                _eventAggregator.GetEvent<StatusMessageEvent>()
                    .Publish(new StatusMessageEventArgs("数据刷新完成", StatusMessageType.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新数据失败");
                _eventAggregator.GetEvent<StatusMessageEvent>()
                    .Publish(new StatusMessageEventArgs($"数据刷新失败: {ex.Message}", StatusMessageType.Error));
            }
        }

        #endregion

        #region 看诊流程

        private void OnPatientSelected()
        {
            if (SelectedPatient != null)
            {
                _eventAggregator.GetEvent<PatientSelectedEvent>()
                    .Publish(new PatientSelectedEventArgs(SelectedPatient));
            }
        }

        private async Task StartNewConsultationAsync()
        {
            if (SelectedPatient == null) 
            {
                await _dialogService.ShowWarningAsync("请先选择患者", "提示");
                return;
            }

            try
            {
                // UltraThink四层架构：使用模块化服务验证和创建看诊
                var validationResult = await _consultationModuleService.ValidatePatientForConsultationAsync(SelectedPatient.Id);
                if (!validationResult.IsSuccess)
                {
                    await _dialogService.ShowWarningAsync(validationResult.ErrorMessage ?? "患者验证失败", "验证失败");
                    return;
                }

                var result = await _consultationModuleService.StartConsultationAsync(SelectedPatient.Id, Guid.Empty);
                if (result.IsSuccess)
                {
                    CurrentConsultation = result.Data;
                    await _consultationModuleService.ClearCurrentPrescriptionAsync();
                    
                    _eventAggregator.GetEvent<ConsultationStartedEvent>()
                        .Publish(new ConsultationStartedEventArgs(CurrentConsultation));
                        
                    await _dialogService.ShowSuccessAsync("看诊已开始", "提示");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "开始看诊失败", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊失败");
                await _dialogService.ShowErrorAsync($"开始看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            try
            {
                // UltraThink四层架构：使用模块化服务验证和保存
                var validationResult = await _consultationModuleService.ValidateCurrentPrescriptionAsync();
                if (!validationResult.IsSuccess)
                {
                    await _dialogService.ShowWarningAsync(validationResult.ErrorMessage ?? "处方验证失败", "验证失败");
                    return;
                }

                var saveResult = await _consultationModuleService.SaveConsultationWithPrescriptionAsync(
                    CurrentConsultation.Id,
                    CurrentConsultation.Diagnosis ?? "",
                    "汤剂", 7, "每日一剂，水煎服");

                if (saveResult.IsSuccess)
                {
                    // 如果从MedicalCase导航过来，更新MedicalCase状态
                    if (IsNavigatedFromMedicalCase && MedicalCaseId.HasValue)
                    {
                        await UpdateMedicalCaseStatusAsync();
                    }

                    _eventAggregator.GetEvent<ConsultationCompletedEvent>()
                        .Publish(new ConsultationCompletedEventArgs(CurrentConsultation));
                    
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
                else
                {
                    await _dialogService.ShowErrorAsync(saveResult.ErrorMessage ?? "保存看诊失败", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存看诊失败");
                await _dialogService.ShowErrorAsync($"保存看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task UpdateMedicalCaseStatusAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
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

        private async void AddHerbToPrescription(HerbInfo? herb)
        {
            if (herb != null && CurrentConsultation != null)
            {
                try
                {
                    var result = await _consultationModuleService.AddHerbToPrescriptionAsync(CurrentConsultation.Id, herb.Id, 10m);
                    if (result.IsSuccess)
                    {
                        RaisePropertyChanged(nameof(TotalPrice));
                        RaisePropertyChanged(nameof(PrescriptionItems));
                    }
                    else
                    {
                        await _dialogService.ShowWarningAsync(result.ErrorMessage ?? "添加药材失败", "提示");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "添加药材到处方失败");
                    await _dialogService.ShowErrorAsync($"添加药材失败: {ex.Message}", "错误");
                }
            }
        }

        private async void RemoveHerbFromPrescription(PrescriptionItemInfo? item)
        {
            if (item != null && CurrentConsultation != null)
            {
                try
                {
                    var result = await _consultationModuleService.RemoveHerbFromPrescriptionAsync(CurrentConsultation.Id, item.HerbId);
                    if (result.IsSuccess)
                    {
                        RaisePropertyChanged(nameof(TotalPrice));
                        RaisePropertyChanged(nameof(PrescriptionItems));
                    }
                    else
                    {
                        await _dialogService.ShowWarningAsync(result.ErrorMessage ?? "移除药材失败", "提示");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从处方移除药材失败");
                    await _dialogService.ShowErrorAsync($"移除药材失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task ApplyFormulaAsync(FormulaInfo? formula)
        {
            if (formula == null || CurrentConsultation == null) return;

            try
            {
                var result = await _consultationModuleService.ApplyFormulaTemplateAsync(CurrentConsultation.Id, formula);
                if (result.IsSuccess)
                {
                    RaisePropertyChanged(nameof(TotalPrice));
                    RaisePropertyChanged(nameof(PrescriptionItems));
                    await _dialogService.ShowSuccessAsync($"验方 {formula.Name} 已应用到处方", "提示");
                }
                else
                {
                    await _dialogService.ShowWarningAsync(result.ErrorMessage ?? "应用验方失败", "提示");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用验方失败");
                await _dialogService.ShowErrorAsync($"应用验方失败: {ex.Message}", "错误");
            }
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<DataRefreshRequestEvent>()
                .Subscribe(async args => 
                {
                    if (args.RefreshType == DataRefreshType.All)
                        await RefreshDataAsync();
                });

            // 订阅模块化服务的事件
            _consultationModuleService.PrescriptionItemsChanged += OnPrescriptionItemsChanged;
            _consultationModuleService.ConsultationStatusChanged += OnConsultationStatusChanged;
        }

        private void OnPrescriptionItemsChanged(object? sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(PrescriptionItems));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        private void OnConsultationStatusChanged(object? sender, ConsultationStatusChangedEventArgs e)
        {
            if (e.ConsultationId == CurrentConsultation?.Id)
            {
                CurrentConsultation.Status = e.NewStatus;
                RaisePropertyChanged(nameof(CurrentConsultation));
            }
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

            // 取消事件订阅
            _consultationModuleService.PrescriptionItemsChanged -= OnPrescriptionItemsChanged;
            _consultationModuleService.ConsultationStatusChanged -= OnConsultationStatusChanged;
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
                        UserId = Guid.Empty,
                        Status = dto.Status,
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
                // UltraThink四层架构：使用模块化服务获取看诊记录
                var result = await _consultationModuleService.GetByMedicalCaseIdAsync(MedicalCaseId.Value);
                
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
                // UltraThink四层架构：使用模块化服务开始看诊
                var result = await _consultationModuleService.StartConsultationForMedicalCaseAsync(
                    MedicalCaseId.Value,
                    CurrentMedicalCase.PatientId,
                    Guid.Empty); // TODO: 需要从用户会话中获取医生ID
                
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentConsultation = result.Data;
                    _eventAggregator.GetEvent<ConsultationStartedEvent>()
                        .Publish(new ConsultationStartedEventArgs(CurrentConsultation));
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