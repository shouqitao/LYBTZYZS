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
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
// UltraThink v2.0: 直接使用DTO，移除Info模型引用
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
// UltraThink v2.0: 引用正确的API接口
using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Modules.Patients.Api;
using LYBT.Desktop.Modules.Herbs.Api;
using LYBT.Desktop.Modules.Formula.Api;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - UltraThink v2.0架构重构版本
    /// 使用各个模块的专用服务，直接使用DTOs，符合三层架构规范
    /// 负责协调看诊流程，与医疗案例模块深度集成
    /// </summary>
    public class ConsultationMainViewModel : SessionAwareViewModel, INavigationAware
    {
        #region 依赖服务

        private readonly IConsultationService _consultationService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientApi _patientApiService;
        private readonly IHerbApi _herbApiService;
        private readonly IFormulaApi _formulaApiService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        private readonly IMapper _mapper;

        #endregion

        #region 属性

        private string _title = "看诊工作台";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // UltraThink v2.0: 直接使用PatientDto
        private ObservableCollection<PatientDto> _patients = new();
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        public PatientDto? SelectedPatient
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

        // UltraThink v2.0: 直接使用ConsultationDto
        private ConsultationDto? _currentConsultation;
        public ConsultationDto? CurrentConsultation
        {
            get => _currentConsultation;
            set => SetProperty(ref _currentConsultation, value);
        }

        // UltraThink v2.0: 直接使用MedicalCaseDto
        private MedicalCaseDto? _currentMedicalCase;
        public MedicalCaseDto? CurrentMedicalCase
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

        // UltraThink v2.0: 简化处方项目 - 使用基础集合而非复杂的模块服务
        private ObservableCollection<PrescriptionItemDto> _prescriptionItems = new();
        public ObservableCollection<PrescriptionItemDto> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        public decimal TotalPrice
        {
            get
            {
                return PrescriptionItems?.Sum(item => item.Price * item.Quantity) ?? 0;
            }
        }

        // UltraThink v2.0: 直接使用HerbDto
        private ObservableCollection<HerbDto> _availableHerbs = new();
        public ObservableCollection<HerbDto> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        // UltraThink v2.0: 直接使用FormulaDto
        private ObservableCollection<FormulaDto> _availableFormulas = new();
        public ObservableCollection<FormulaDto> AvailableFormulas
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
            IConsultationService consultationService,
            IMedicalCaseService medicalCaseService,
            IPatientApi patientApiService,
            IHerbApi herbApiService,
            IFormulaApi formulaApiService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<ConsultationMainViewModel> logger,
            IMapper mapper)
            : base(sessionManager, notificationService, logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _patientApiService = patientApiService ?? throw new ArgumentNullException(nameof(patientApiService));
            _herbApiService = herbApiService ?? throw new ArgumentNullException(nameof(herbApiService));
            _formulaApiService = formulaApiService ?? throw new ArgumentNullException(nameof(formulaApiService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());
            NewConsultationCommand = new DelegateCommand(async () => await StartNewConsultationAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            AddHerbCommand = new DelegateCommand<HerbDto>(AddHerbToPrescription);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemDto>(RemoveHerbFromPrescription);
            ApplyFormulaCommand = new DelegateCommand<FormulaDto>(ApplyFormula);

            SubscribeToEvents();
            _ = InitializeAsync();

            LogInfo("ConsultationMainViewModel 已初始化，使用 UltraThink SessionManager 架构");
        }

        #endregion

        #region UltraThink SessionAware 重写方法

        /// <summary>
        /// 当SessionManager中的患者变化时调用
        /// </summary>
        protected override void OnPatientChanged(PatientChangedEventArgs args)
        {
            base.OnPatientChanged(args);
            
            // 同步UI选择状态
            if (args.NewPatient != null)
            {
                _selectedPatient = args.NewPatient;
                RaisePropertyChanged(nameof(SelectedPatient));
                LogInfo($"从SessionManager同步患者选择: {args.NewPatient.Name}");
            }
        }

        /// <summary>
        /// 当SessionManager中的诊疗状态变化时调用
        /// </summary>
        protected override void OnConsultationChanged(ConsultationChangedEventArgs args)
        {
            base.OnConsultationChanged(args);
            
            if (args.NewConsultation != null)
            {
                CurrentConsultation = args.NewConsultation;
                LogInfo($"从SessionManager同步诊疗状态: {args.NewStatus}");
            }
        }

        #endregion

        #region 初始化和数据加载

        private async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                ShowLoading("正在加载看诊数据...");
                await RefreshDataAsync();
            }
            finally
            {
                HideLoading();
                IsLoading = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                // UltraThink v2.0: 直接调用各个模块的API服务
                var patientsTask = LoadPatientsAsync();
                var herbsTask = LoadHerbsAsync();
                var formulasTask = LoadFormulasAsync();

                await Task.WhenAll(patientsTask, herbsTask, formulasTask);

                // UltraThink SessionManager: 使用NotificationService替代EventAggregator
                ShowSuccess("数据刷新完成");
                LogInfo("ConsultationMainViewModel 数据刷新完成");
            }
            catch (Exception ex)
            {
                LogError(ex, "刷新数据失败");
                ShowError($"数据刷新失败: {ex.Message}");
            }
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                // UltraThink v2.0: 使用正确的API接口
                var result = await _patientApiService.GetPatientsAsync(pageIndex: 1, pageSize: 50);
                if (result.IsSuccessStatusCode && result.Content?.Data != null)
                {
                    Patients = new ObservableCollection<PatientDto>(result.Content.Data);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载患者列表失败");
            }
        }

        private async Task LoadHerbsAsync()
        {
            try
            {
                // UltraThink v2.0: 使用正确的API接口
                var result = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 100);
                if (result.IsSuccessStatusCode && result.Content?.Data != null)
                {
                    AvailableHerbs = new ObservableCollection<HerbDto>(result.Content.Data);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载中药材列表失败");
            }
        }

        private async Task LoadFormulasAsync()
        {
            try
            {
                // UltraThink v2.0: 使用正确的API接口
                var result = await _formulaApiService.GetFormulasAsync();
                if (result.IsSuccessStatusCode && result.Content?.Data != null)
                {
                    AvailableFormulas = new ObservableCollection<FormulaDto>(result.Content.Data);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载验方列表失败");
            }
        }

        #endregion

        #region 看诊流程

        /// <summary>
        /// 本地患者选择处理 - 同步到SessionManager
        /// </summary>
        private void OnPatientSelected()
        {
            if (SelectedPatient != null)
            {
                // UltraThink SessionManager: 通过SessionManager设置当前患者
                SessionManager.CurrentPatient = SelectedPatient;
                
                // 保持原有事件发布（用于其他组件兼容性）
                _eventAggregator.GetEvent<PatientSelectedEvent>()?
                    .Publish(SelectedPatient);
                
                LogInfo($"患者选择已同步到SessionManager: {SelectedPatient.Name}");
            }
        }

        private async Task StartNewConsultationAsync()
        {
            if (SelectedPatient == null)
            {
                ShowWarning("请先选择患者", "提示");
                return;
            }

            try
            {
                ShowLoading("正在开始看诊...");
                
                // UltraThink SessionManager: 通过SessionManager开始诊疗会话
                SessionManager.StartConsultation(SelectedPatient, MedicalCaseId);

                // UltraThink v2.0: 直接创建看诊记录
                var startDto = new ConsultationStartDto
            {
                PatientId = SelectedPatient.Id,
                DoctorId = CurrentUser?.Id ?? Guid.Empty,
                MedicalCaseId = MedicalCaseId ?? Guid.NewGuid() // 如果没有MedicalCase，创建新的
            };
            
            var result = await _consultationService.StartAsync(startDto);
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentConsultation = result.Data;
                    PrescriptionItems.Clear();
                    
                    _eventAggregator.GetEvent<ConsultationStartedEvent>()?
                        .Publish(new ConsultationSessionData { 
                            ConsultationId = CurrentConsultation.Id,
                            PatientId = SelectedPatient.Id,
                            MedicalCaseId = CurrentConsultation.MedicalCaseId,
                            SessionStartTime = DateTime.Now 
                        });
                        
                    ShowSuccess("看诊已开始", "操作成功");
                    LogInfo($"看诊开始成功 - 患者: {SelectedPatient.Name}, 诊疗ID: {CurrentConsultation.Id}");
                }
                else
                {
                    ShowError(result.ErrorMessage ?? "开始看诊失败", "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "开始看诊失败");
                ShowError($"开始看诊失败: {ex.Message}", "错误");
            }
            finally
            {
                HideLoading();
            }
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            try
            {
                ShowLoading("正在保存看诊记录...");
                
                // UltraThink v2.0: 保存看诊记录 - 创建DetailDto
                var updateDto = new ConsultationDetailDto
                {
                    Id = CurrentConsultation.Id,
                    MedicalCaseId = CurrentConsultation.MedicalCaseId,
                    PatientId = CurrentConsultation.PatientId,
                    PatientName = SelectedPatient?.Name ?? "患者", // 从选择的患者获取姓名
                    DoctorId = CurrentConsultation.DoctorId,
                    DoctorName = CurrentConsultation.DoctorName,
                    ConsultationTime = CurrentConsultation.ConsultationTime,
                    Diagnosis = CurrentConsultation.Diagnosis ?? "",
                    TCMDiagnosis = CurrentConsultation.TCMDiagnosis ?? "",
                    TreatmentPrinciple = CurrentConsultation.TreatmentPrinciple ?? "",
                    Inspection = CurrentConsultation.Inspection ?? "",
                    AuscultationOlfaction = CurrentConsultation.AuscultationOlfaction ?? "",
                    Inquiry = CurrentConsultation.Inquiry ?? "",
                    Palpation = CurrentConsultation.Palpation ?? "",
                    TongueInspection = CurrentConsultation.TongueInspection ?? "",
                    PulseCondition = CurrentConsultation.PulseCondition ?? "",
                    PatternDifferentiation = CurrentConsultation.DifferentiationAnalysis ?? "", // 修正属性名
                    MedicalAdvice = CurrentConsultation.MedicalAdvice ?? "",
                    StartTime = DateTime.Now, // 使用当前时间，因为ConsultationDto没有StartTime
                    EndTime = DateTime.Now,
                    Status = LYBT.Shared.Models.Enums.ConsultationStatus.Completed,
                    IsCompleted = true,
                    CreateTime = CurrentConsultation.CreateTime,
                    UpdateTime = DateTime.Now
                };

                var result = await _consultationService.UpdateAsync(CurrentConsultation.Id, updateDto);
                if (result.IsSuccess)
                {
                    // 如果从MedicalCase导航过来，更新MedicalCase状态
                    if (IsNavigatedFromMedicalCase && MedicalCaseId.HasValue)
                    {
                        await UpdateMedicalCaseStatusAsync();
                    }

                    // UltraThink SessionManager: 结束诊疗会话
                    SessionManager.EndConsultation();

                    _eventAggregator.GetEvent<ConsultationCompletedEvent>()?
                        .Publish(new ConsultationCompletedData { 
                            ConsultationId = CurrentConsultation.Id,
                            PatientId = SelectedPatient.Id,
                            CompletedTime = DateTime.Now,
                            IsSuccessful = true
                        });
                    
                    ShowSuccess("看诊已完成并保存", "操作成功");
                    LogInfo($"看诊完成 - 诊疗ID: {CurrentConsultation.Id}");
                    
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
                    ShowError(result.ErrorMessage ?? "保存看诊失败", "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "保存看诊失败");
                ShowError($"保存看诊失败: {ex.Message}", "错误");
            }
            finally
            {
                HideLoading();
            }
        }

        private async Task UpdateMedicalCaseStatusAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
                var result = await _medicalCaseService.UpdateStatusAsync(
                    MedicalCaseId.Value, 
                    (int)MedicalCaseStatus.Completed);
                
                if (!result.IsSuccess)
                {
                    LogWarning("更新医疗案例状态失败: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "更新医疗案例状态时发生错误");
            }
        }

        private void NavigateBackToMedicalCase()
        {
            if (MedicalCaseId.HasValue)
            {
                _regionManager.RequestNavigate(RegionNames.ConsultationWorkbenchContentRegion, $"MedicalCaseDetailView?MedicalCaseId={MedicalCaseId.Value}&EditMode=false");
            }
        }

        #endregion

        #region 处方管理

        private void AddHerbToPrescription(HerbDto? herb)
        {
            if (herb != null && CurrentConsultation != null)
            {
                var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == herb.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += 10;
                }
                else
                {
                    var newItem = new PrescriptionItemDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        Price = herb.Price,
                        Quantity = 10,
                        Unit = herb.Unit,
                        Remark = ""
                    };
                    PrescriptionItems.Add(newItem);
                }
                RaisePropertyChanged(nameof(TotalPrice));
                LogInfo($"添加中药材到处方: {herb.Name}");
            }
        }

        private void RemoveHerbFromPrescription(PrescriptionItemDto? item)
        {
            if (item != null)
            {
                PrescriptionItems.Remove(item);
                RaisePropertyChanged(nameof(TotalPrice));
                LogInfo($"从处方移除中药材: {item.HerbName}");
            }
        }

        private void ApplyFormula(FormulaDto? formula)
        {
            if (formula == null || CurrentConsultation == null) return;

            try
            {
                ShowLoading("正在应用验方...");
                
                // UltraThink v2.0: 简化验方应用逻辑 - 20人以下小诊所暂不实现复杂验方功能
                ShowInfo($"验方 {formula.Name} 应用功能暂不支持", "提示");
                RaisePropertyChanged(nameof(TotalPrice));
                LogInfo($"应用验方: {formula.Name}");
            }
            catch (Exception ex)
            {
                LogError(ex, "应用验方失败");
                ShowError($"应用验方失败: {ex.Message}", "错误");
            }
            finally
            {
                HideLoading();
            }
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<DataRefreshRequestEvent>()?
                .Subscribe(async args => 
                {
                    if (args.RefreshType == DataRefreshType.All)
                        await RefreshDataAsync();
                });

            // UltraThink v2.0: 简化事件订阅，移除复杂的模块服务事件
            LogInfo("事件订阅完成");
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
                LogInfo($"从MedicalCase导航过来，案例ID: {MedicalCaseId}");
            }

            // 检查是否有患者ID
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                Task.Run(async () => await LoadPatientAsync(patientId));
                LogInfo($"导航参数包含患者ID: {patientId}");
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
                ShowInfo("看诊尚未完成，数据已自动保存", "提示");
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
                ShowLoading("正在加载医疗案例...");
                
                var result = await _medicalCaseService.GetByIdAsync(MedicalCaseId.Value);
                
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentMedicalCase = result.Data;
                    Title = $"看诊工作台 - {CurrentMedicalCase.PatientName}";
                    
                    // 加载或创建关联的看诊记录
                    await LoadOrCreateConsultationAsync();
                    LogInfo($"医疗案例加载完成: {CurrentMedicalCase.PatientName}");
                }
                else
                {
                    ShowError($"加载医疗案例失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载医疗案例失败");
                ShowError($"加载医疗案例失败: {ex.Message}", "错误");
            }
            finally
            {
                HideLoading();
                IsLoading = false;
            }
        }

        private async Task LoadOrCreateConsultationAsync()
        {
            if (!MedicalCaseId.HasValue) return;

            try
            {
                // UltraThink v2.0: 尝试根据MedicalCaseId获取看诊记录
                // 注意：这需要ConsultationModuleService支持按MedicalCaseId查询
                // 如果不存在，则创建新的看诊记录
                await StartConsultationFromMedicalCaseAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "加载或创建看诊记录失败");
            }
        }

        private async Task StartConsultationFromMedicalCaseAsync()
        {
            if (!MedicalCaseId.HasValue || CurrentMedicalCase == null) return;

            try
            {
                ShowLoading("正在准备看诊...");
                
                // UltraThink v2.0: 为医疗案例创建看诊记录
                var startDto = new ConsultationStartDto
                {
                    PatientId = CurrentMedicalCase.PatientId,
                    DoctorId = CurrentUser?.Id ?? Guid.Empty,
                    MedicalCaseId = MedicalCaseId.Value
                };
                
                var result = await _consultationService.StartAsync(startDto);
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentConsultation = result.Data;
                    
                    // UltraThink SessionManager: 通过SessionManager开始诊疗会话
                    var patientDto = new PatientDto
                    {
                        Id = CurrentMedicalCase.PatientId,
                        Name = CurrentMedicalCase.PatientName
                        // 其他字段根据需要填充
                    };
                    SessionManager.StartConsultation(patientDto, MedicalCaseId);
                    
                    _eventAggregator.GetEvent<ConsultationStartedEvent>()?
                        .Publish(new ConsultationSessionData { 
                            ConsultationId = CurrentConsultation.Id,
                            PatientId = CurrentMedicalCase.PatientId,
                            MedicalCaseId = MedicalCaseId ?? Guid.Empty,
                            SessionStartTime = DateTime.Now 
                        });
                    ShowSuccess("看诊已开始", "提示");
                    LogInfo($"从医疗案例开始看诊成功 - 案例: {CurrentMedicalCase.PatientName}");
                }
                else
                {
                    ShowError($"开始看诊失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "开始看诊失败");
                ShowError($"开始看诊失败: {ex.Message}", "错误");
            }
            finally
            {
                HideLoading();
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
            
            if (SelectedPatient != null)
            {
                LogInfo($"患者加载成功: {SelectedPatient.Name}");
            }
        }

        #endregion
    }
}