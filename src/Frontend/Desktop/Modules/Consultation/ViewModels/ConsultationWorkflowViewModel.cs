using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Events;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Prescriptions;
namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 诊疗工作流视图模型
    /// 管理整个诊疗流程：患者选择 → 四诊采集 → 辨证分析 → 处方开具
    /// </summary>
    public class ConsultationWorkflowViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IConsultationService _consultationService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService _patientService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ConsultationWorkflowViewModel> _logger;

        #endregion

        #region 工作流状态

        private WorkflowStep _currentStep = WorkflowStep.PatientSelection;
        public WorkflowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepStatus();
                    UpdateNavigationButtons();
                    LoadStepContent();
                }
            }
        }

        private object? _currentStepContent;
        public object? CurrentStepContent
        {
            get => _currentStepContent;
            set => SetProperty(ref _currentStepContent, value);
        }

        #endregion

        #region 属性

        // 医疗案例信息
        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private MedicalCaseInfo? _medicalCase;
        public MedicalCaseInfo? MedicalCase
        {
            get => _medicalCase;
            set => SetProperty(ref _medicalCase, value);
        }

        // 患者信息
        private PatientInfo? _patient;
        public PatientInfo? Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        private string _patientName = "";
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientGenderAge = "";
        public string PatientGenderAge
        {
            get => _patientGenderAge;
            set => SetProperty(ref _patientGenderAge, value);
        }

        private string _patientPhone = "";
        public string PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        private bool _hasSelectedPatient;
        public bool HasSelectedPatient
        {
            get => _hasSelectedPatient;
            set => SetProperty(ref _hasSelectedPatient, value);
        }

        // 步骤状态
        private bool _isPatientStepActive;
        public bool IsPatientStepActive
        {
            get => _isPatientStepActive;
            set => SetProperty(ref _isPatientStepActive, value);
        }

        private bool _isFourDiagnosisStepActive;
        public bool IsFourDiagnosisStepActive
        {
            get => _isFourDiagnosisStepActive;
            set => SetProperty(ref _isFourDiagnosisStepActive, value);
        }

        private bool _isDifferentiationStepActive;
        public bool IsDifferentiationStepActive
        {
            get => _isDifferentiationStepActive;
            set => SetProperty(ref _isDifferentiationStepActive, value);
        }

        private bool _isPrescriptionStepActive;
        public bool IsPrescriptionStepActive
        {
            get => _isPrescriptionStepActive;
            set => SetProperty(ref _isPrescriptionStepActive, value);
        }

        // 导航控制
        private bool _canGoPrevious;
        public bool CanGoPrevious
        {
            get => _canGoPrevious;
            set => SetProperty(ref _canGoPrevious, value);
        }

        private bool _canGoNext;
        public bool CanGoNext
        {
            get => _canGoNext;
            set => SetProperty(ref _canGoNext, value);
        }

        private string _nextButtonText = "下一步";
        public string NextButtonText
        {
            get => _nextButtonText;
            set => SetProperty(ref _nextButtonText, value);
        }

        // 历史记录
        private ObservableCollection<HistoryRecord> _patientHistory = new();
        public ObservableCollection<HistoryRecord> PatientHistory
        {
            get => _patientHistory;
            set => SetProperty(ref _patientHistory, value);
        }

        private bool _isHistoryPanelVisible;
        public bool IsHistoryPanelVisible
        {
            get => _isHistoryPanelVisible;
            set => SetProperty(ref _isHistoryPanelVisible, value);
        }

        // 工作流数据
        private Core.Models.Consultation.ConsultationData _consultationData = new();
        public Core.Models.Consultation.ConsultationData CurrentConsultationData
        {
            get => _consultationData;
            set => SetProperty(ref _consultationData, value);
        }

        private bool _isDraft;
        public bool IsDraft
        {
            get => _isDraft;
            set => SetProperty(ref _isDraft, value);
        }

        #endregion

        #region 命令

        public ICommand PreviousStepCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand ExitWorkflowCommand { get; }
        public ICommand ToggleHistoryPanelCommand { get; }
        public ICommand CloseHistoryPanelCommand { get; }
        public ICommand ViewHistoryDetailCommand { get; }
        public ICommand ImportFourDiagnosisCommand { get; }
        public ICommand ImportPrescriptionCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationWorkflowViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IMedicalCaseService medicalCaseService,
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            IPatientService patientService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            ILogger<ConsultationWorkflowViewModel> logger)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _medicalCaseService = medicalCaseService;
            _consultationService = consultationService;
            _prescriptionService = prescriptionService;
            _patientService = patientService;
            _userSessionManager = userSessionManager;
            _dialogService = dialogService;
            _logger = logger;

            // 初始化命令
            PreviousStepCommand = new DelegateCommand(async () => await PreviousStepAsync(), () => CanGoPrevious);
            NextStepCommand = new DelegateCommand(async () => await NextStepAsync(), () => CanGoNext);
            SaveDraftCommand = new DelegateCommand(async () => await SaveDraftAsync());
            ExitWorkflowCommand = new DelegateCommand(async () => await ExitWorkflowAsync());
            ToggleHistoryPanelCommand = new DelegateCommand(ToggleHistoryPanel);
            CloseHistoryPanelCommand = new DelegateCommand(() => IsHistoryPanelVisible = false);
            ViewHistoryDetailCommand = new DelegateCommand<HistoryRecord>(async hr => await ViewHistoryDetailAsync(hr));
            ImportFourDiagnosisCommand = new DelegateCommand<HistoryRecord>(async hr => await ImportFourDiagnosisAsync(hr));
            ImportPrescriptionCommand = new DelegateCommand<HistoryRecord>(async hr => await ImportPrescriptionAsync(hr));

            // 订阅事件
            SubscribeEvents();
        }

        #endregion

        #region 初始化

        private void SubscribeEvents()
        {
            // 订阅步骤完成事件
            _eventAggregator.GetEvent<WorkflowStepCompletedEvent>().Subscribe(OnStepCompleted);
            
            // 订阅数据更新事件
            _eventAggregator.GetEvent<ConsultationDataUpdatedEvent>().Subscribe(OnDataUpdated);
        }

        private async Task InitializeWorkflowAsync()
        {
            try
            {
                // 加载医疗案例信息
                if (MedicalCaseId != Guid.Empty)
                {
                    await LoadMedicalCaseAsync();
                }

                // 加载患者信息
                if (Patient != null)
                {
                    await LoadPatientInfoAsync();
                }

                // 初始化步骤状态
                UpdateStepStatus();
                UpdateNavigationButtons();
                
                // 加载第一步内容
                LoadStepContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化诊疗工作流失败");
                await _dialogService.ShowErrorAsync("初始化失败: " + ex.Message, "错误");
            }
        }

        private async Task LoadMedicalCaseAsync()
        {
            try
            {
                var result = await _medicalCaseService.GetByIdAsync(MedicalCaseId);
                if (result.IsSuccess && result.Data != null)
                {
                    var dto = result.Data;
                    MedicalCase = new MedicalCaseInfo 
                    {
                        Id = dto.Id,
                        PatientId = dto.PatientId,
                        DoctorId = Guid.Empty, // 使用默认值
                        CreateTime = dto.CreateTime,
                        Status = dto.Status,
                        Diagnosis = "", // 使用默认值
                        ChiefComplaint = "" // 使用默认值
                    };
                    
                    // 加载已有的诊疗数据
                    await LoadExistingDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医疗案例失败");
            }
        }

        private async Task LoadPatientInfoAsync()
        {
            if (Patient == null) return;

            try
            {
                PatientName = Patient.Name;
                PatientGenderAge = $"{Patient.Gender} {Patient.Age}岁";
                PatientPhone = Patient.Phone ?? "";
                HasSelectedPatient = true;

                // 加载患者历史记录
                await LoadPatientHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者信息失败");
            }
        }

        private async Task LoadPatientHistoryAsync()
        {
            if (Patient == null) return;

            try
            {
                var result = await _medicalCaseService.GetByPatientIdAsync(Patient.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    var cases = result.Data as List<MedicalCaseInfo> ?? new List<MedicalCaseInfo>();
                    
                    // 排除当前案例，取最近5条
                    var history = cases
                        .Where(c => c.Id != MedicalCaseId)
                        .OrderByDescending(c => c.CreateTime)
                        .Take(5)
                        .Select(c => new HistoryRecord
                        {
                            Id = c.Id,
                            VisitDate = c.CreateTime,
                            TimeAgo = CalculateTimeAgo(c.CreateTime),
                            Diagnosis = c.Diagnosis ?? "未诊断",
                            ChiefComplaint = c.ChiefComplaint,
                            Status = c.Status
                        });

                    PatientHistory = new ObservableCollection<HistoryRecord>(history);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者历史记录失败");
            }
        }

        private async Task LoadExistingDataAsync()
        {
            if (MedicalCase == null) return;

            try
            {
                // 加载已保存的诊疗数据
                var consultationResult = await _consultationService.GetByMedicalCaseIdAsync(MedicalCaseId);
                if (consultationResult.IsSuccess && consultationResult.Data != null)
                {
                    var consultInfo = consultationResult.Data;
                    CurrentConsultationData = new Core.Models.Consultation.ConsultationData
                    {
                        MedicalCaseId = MedicalCaseId,
                        PatientId = MedicalCase.PatientId,
                        Diagnosis = consultInfo.Diagnosis,
                        Status = Core.Models.Consultation.ConsultationStatus.Draft
                    };
                    IsDraft = CurrentConsultationData.Status == Core.Models.Consultation.ConsultationStatus.Draft;
                    
                    // 根据已完成的步骤调整当前步骤
                    DetermineCurrentStep();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载已有诊疗数据失败");
            }
        }

        #endregion

        #region 步骤管理

        private void UpdateStepStatus()
        {
            IsPatientStepActive = CurrentStep == WorkflowStep.PatientSelection;
            IsFourDiagnosisStepActive = CurrentStep == WorkflowStep.FourDiagnosis;
            IsDifferentiationStepActive = CurrentStep == WorkflowStep.Differentiation;
            IsPrescriptionStepActive = CurrentStep == WorkflowStep.Prescription;
        }

        private void UpdateNavigationButtons()
        {
            CanGoPrevious = CurrentStep > WorkflowStep.PatientSelection;
            CanGoNext = ValidateCurrentStep();
            
            // 更新下一步按钮文本
            NextButtonText = CurrentStep == WorkflowStep.Prescription ? "完成" : "下一步";
        }

        private bool ValidateCurrentStep()
        {
            switch (CurrentStep)
            {
                case WorkflowStep.PatientSelection:
                    return Patient != null;
                    
                case WorkflowStep.FourDiagnosis:
                    return !string.IsNullOrWhiteSpace(CurrentConsultationData.FourDiagnosis?.Inspection) ||
                           !string.IsNullOrWhiteSpace(CurrentConsultationData.FourDiagnosis?.Auscultation) ||
                           !string.IsNullOrWhiteSpace(CurrentConsultationData.FourDiagnosis?.Inquiry) ||
                           !string.IsNullOrWhiteSpace(CurrentConsultationData.FourDiagnosis?.Palpation);
                           
                case WorkflowStep.Differentiation:
                    return !string.IsNullOrWhiteSpace(CurrentConsultationData.Diagnosis);
                    
                case WorkflowStep.Prescription:
                    return CurrentConsultationData.Prescription?.Items?.Count > 0;
                    
                default:
                    return false;
            }
        }

        private void DetermineCurrentStep()
        {
            // 根据已填写的数据确定应该显示哪个步骤
            if (CurrentConsultationData.Prescription?.Items?.Count > 0)
            {
                CurrentStep = WorkflowStep.Prescription;
            }
            else if (!string.IsNullOrWhiteSpace(CurrentConsultationData.Diagnosis))
            {
                CurrentStep = WorkflowStep.Differentiation;
            }
            else if (CurrentConsultationData.FourDiagnosis != null)
            {
                CurrentStep = WorkflowStep.FourDiagnosis;
            }
            else
            {
                CurrentStep = WorkflowStep.PatientSelection;
            }
        }

        private void LoadStepContent()
        {
            try
            {
                switch (CurrentStep)
                {
                    case WorkflowStep.PatientSelection:
                        LoadPatientSelectionView();
                        break;
                        
                    case WorkflowStep.FourDiagnosis:
                        LoadFourDiagnosisView();
                        break;
                        
                    case WorkflowStep.Differentiation:
                        LoadDifferentiationView();
                        break;
                        
                    case WorkflowStep.Prescription:
                        LoadPrescriptionView();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载步骤内容失败: {CurrentStep}");
            }
        }

        private void LoadPatientSelectionView()
        {
            // 创建患者选择视图
            CurrentStepContent = new Views.PatientSelectionView();
        }

        private void LoadFourDiagnosisView()
        {
            // 创建四诊采集视图（使用简化版纯文本输入界面）
            CurrentStepContent = new Views.SimpleTCMFourDiagnosisView();
        }

        private void LoadDifferentiationView()
        {
            // 创建辨证分析视图
            CurrentStepContent = new Views.DifferentiationView();
        }

        private void LoadPrescriptionView()
        {
            // 创建处方开具视图
            CurrentStepContent = new Views.PrescriptionView();
        }

        #endregion

        #region 导航操作

        private async Task PreviousStepAsync()
        {
            if (CurrentStep > WorkflowStep.PatientSelection)
            {
                // 保存当前步骤数据
                await SaveCurrentStepDataAsync();
                
                // 切换到上一步
                CurrentStep = (WorkflowStep)((int)CurrentStep - 1);
            }
        }

        private async Task NextStepAsync()
        {
            try
            {
                // 验证当前步骤
                if (!ValidateCurrentStep())
                {
                    await _dialogService.ShowWarningAsync("请完成当前步骤的必填信息", "提示");
                    return;
                }

                // 保存当前步骤数据
                await SaveCurrentStepDataAsync();

                if (CurrentStep == WorkflowStep.Prescription)
                {
                    // 完成整个流程
                    await CompleteWorkflowAsync();
                }
                else
                {
                    // 切换到下一步
                    CurrentStep = (WorkflowStep)((int)CurrentStep + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到下一步失败");
                await _dialogService.ShowErrorAsync("操作失败: " + ex.Message, "错误");
            }
        }

        private async Task SaveDraftAsync()
        {
            try
            {
                CurrentConsultationData.Status = Core.Models.Consultation.ConsultationStatus.Draft;
                await SaveCurrentStepDataAsync();
                
                await _dialogService.ShowSuccessAsync("草稿保存成功", "成功");
                IsDraft = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存草稿失败");
                await _dialogService.ShowErrorAsync("保存失败: " + ex.Message, "错误");
            }
        }

        private async Task SaveCurrentStepDataAsync()
        {
            try
            {
                // 发布事件让当前步骤保存数据
                _eventAggregator.GetEvent<SaveStepDataEvent>().Publish(CurrentStep);
                
                // 保存到服务器
                var result = await _consultationService.SaveAsync(CurrentConsultationData);
                if (!result.IsSuccess)
                {
                    throw new Exception(result.ErrorMessage ?? "保存失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存当前步骤数据失败");
                throw;
            }
        }

        private async Task CompleteWorkflowAsync()
        {
            try
            {
                // 更新状态为已完成
                CurrentConsultationData.Status = Core.Models.Consultation.ConsultationStatus.Completed;
                MedicalCase.Status = MedicalCaseStatus.Completed;
                
                // 保存最终数据
                await SaveCurrentStepDataAsync();
                
                // 更新医疗案例状态
                await _medicalCaseService.UpdateStatusAsync(MedicalCaseId, MedicalCaseStatus.Completed);
                
                await _dialogService.ShowSuccessAsync("诊疗完成！", "成功");
                
                // 导航回主界面
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成诊疗流程失败");
                await _dialogService.ShowErrorAsync("完成失败: " + ex.Message, "错误");
            }
        }

        private async Task ExitWorkflowAsync()
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                IsDraft ? "是否保存草稿并退出？" : "确定要退出诊疗流程吗？",
                "退出确认");
                
            if (confirm)
            {
                if (IsDraft)
                {
                    await SaveDraftAsync();
                }
                
                // 导航回主界面
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
        }

        #endregion

        #region 历史数据操作

        private void ToggleHistoryPanel()
        {
            IsHistoryPanelVisible = !IsHistoryPanelVisible;
        }

        private async Task ViewHistoryDetailAsync(HistoryRecord? history)
        {
            if (history == null) return;

            try
            {
                // 加载历史案例详情
                var result = await _medicalCaseService.GetByIdAsync(history.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    // 显示详情对话框
                    await _dialogService.ShowInformationAsync(
                        $"就诊日期：{history.VisitDate:yyyy-MM-dd}\n" +
                        $"主诉：{history.ChiefComplaint}\n" +
                        $"诊断：{history.Diagnosis}",
                        "历史就诊详情");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看历史详情失败");
                await _dialogService.ShowErrorAsync("加载失败: " + ex.Message, "错误");
            }
        }

        private async Task ImportFourDiagnosisAsync(HistoryRecord? history)
        {
            if (history == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmAsync(
                    "确定要导入该次就诊的四诊信息吗？",
                    "导入确认");
                    
                if (!confirm) return;

                // 加载历史四诊数据
                var result = await _consultationService.GetByMedicalCaseIdAsync(history.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    // 导入四诊信息
                    var consultInfo = result.Data;
                    if (consultInfo != null)
                    {
                        CurrentConsultationData.FourDiagnosis = new Core.Models.Consultation.FourDiagnosisData
                        {
                            Inspection = consultInfo.ChiefComplaint ?? "",
                            Auscultation = "",
                            Inquiry = consultInfo.ChiefComplaint ?? "",
                            Palpation = ""
                        };
                    }
                    CurrentConsultationData.FourDiagnosis.ImportSource = $"导入自{history.VisitDate:yyyy-MM-dd}就诊";
                    
                    // 刷新界面
                    if (CurrentStep == WorkflowStep.FourDiagnosis)
                    {
                        LoadFourDiagnosisView();
                    }
                    
                    await _dialogService.ShowSuccessAsync("四诊信息导入成功", "成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入四诊信息失败");
                await _dialogService.ShowErrorAsync("导入失败: " + ex.Message, "错误");
            }
        }

        private async Task ImportPrescriptionAsync(HistoryRecord? history)
        {
            if (history == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmAsync(
                    "确定要导入该次就诊的处方吗？",
                    "导入确认");
                    
                if (!confirm) return;

                // 加载历史处方数据
                var result = await _prescriptionService.GetByMedicalCaseIdAsync(history.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    // 导入处方（追加模式）
                    if (CurrentConsultationData.Prescription == null)
                    {
                        CurrentConsultationData.Prescription = new Core.Models.Consultation.PrescriptionData();
                    }
                    
                    // 添加导入来源标记
                    foreach (var item in result.Data.Items)
                    {
                        var prescriptionItem = new Core.Models.Consultation.PrescriptionItem
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            ImportSource = $"导入自{history.VisitDate:yyyy-MM-dd}就诊"
                        };
                        CurrentConsultationData.Prescription.Items.Add(prescriptionItem);
                    }
                    
                    // 刷新界面
                    if (CurrentStep == WorkflowStep.Prescription)
                    {
                        LoadPrescriptionView();
                    }
                    
                    await _dialogService.ShowSuccessAsync("处方导入成功", "成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入处方失败");
                await _dialogService.ShowErrorAsync("导入失败: " + ex.Message, "错误");
            }
        }

        #endregion

        #region 事件处理

        private void OnStepCompleted(WorkflowStepData stepData)
        {
            // 更新对应步骤的数据
            switch (stepData.Step)
            {
                case WorkflowStep.FourDiagnosis:
                    CurrentConsultationData.FourDiagnosis = stepData.Data as FourDiagnosisData;
                    break;
                    
                case WorkflowStep.Differentiation:
                    CurrentConsultationData.Diagnosis = stepData.Data as string;
                    break;
                    
                case WorkflowStep.Prescription:
                    CurrentConsultationData.Prescription = stepData.Data as Core.Models.Consultation.PrescriptionData;
                    break;
            }
            
            // 更新导航按钮状态
            UpdateNavigationButtons();
        }

        private void OnDataUpdated(ConsultationData data)
        {
            CurrentConsultationData = data;
            UpdateNavigationButtons();
        }

        #endregion

        #region 辅助方法

        private string CalculateTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            
            if (span.TotalDays < 1)
                return "今天";
            else if (span.TotalDays < 2)
                return "昨天";
            else if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}天前";
            else if (span.TotalDays < 30)
                return $"{(int)(span.TotalDays / 7)}周前";
            else if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)}个月前";
            else
                return $"{(int)(span.TotalDays / 365)}年前";
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 获取导航参数
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            }
            
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                _ = LoadPatientByIdAsync(patientId);
            }
            
            if (navigationContext.Parameters.ContainsKey("Patient"))
            {
                Patient = navigationContext.Parameters.GetValue<PatientInfo>("Patient");
            }
            
            // 初始化工作流
            _ = InitializeWorkflowAsync();
        }

        private async Task LoadPatientByIdAsync(Guid patientId)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    Patient = new PatientInfo
                    {
                        Id = result.Data.Id,
                        Name = result.Data.Name,
                        Gender = result.Data.Gender,
                        Age = CalculateAge(result.Data.BirthDate),
                        Phone = "" // 使用默认值
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者信息失败");
            }
        }

        private int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            
            return age;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 保存当前状态
            if (IsDraft)
            {
                _ = SaveDraftAsync();
            }
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 历史记录
        /// </summary>
        public class HistoryRecord
        {
            public Guid Id { get; set; }
            public DateTime VisitDate { get; set; }
            public string TimeAgo { get; set; } = "";
            public string Diagnosis { get; set; } = "";
            public string? ChiefComplaint { get; set; }
            public MedicalCaseStatus Status { get; set; }
        }

        #endregion
    }
}