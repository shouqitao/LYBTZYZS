using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Consultation.Services;
// using Prism.Dialogs; // Removed for Prism 8.1.97 compatibility
using CoreWorkflowStep = LYBT.Desktop.Core.Models.Consultation.WorkflowStep;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 诊疗工作流视图模型 - UltraThink重构兼容性包装器
    /// 
    /// 重构前：947行超大ViewModel，违反单一职责原则
    /// 重构后：使用协调器模式，将职责分离到5个专门组件：
    /// - ConsultationStepManager: 步骤管理 (211行)
    /// - ConsultationDataManager: 数据管理 (266行)  
    /// - ConsultationHistoryManager: 历史记录管理 (244行)
    /// - ConsultationNavigationHandler: 导航处理 (212行)
    /// - ConsultationWorkflowCoordinator: 工作流协调 (340行)
    /// 
    /// 此类作为向后兼容性包装器，委托给ConsultationWorkflowCoordinator
    /// </summary>
    public class ConsultationWorkflowViewModel : BindableBase, INavigationAware
    {
        #region 核心协调器

        private readonly ConsultationWorkflowCoordinator _coordinator;

        #endregion

        #region 公开的管理器（向后兼容）

        /// <summary>
        /// 步骤管理器
        /// </summary>
        public ConsultationStepManager StepManager => _coordinator.StepManager;

        /// <summary>
        /// 数据管理器  
        /// </summary>
        public ConsultationDataManager DataManager => _coordinator.DataManager;

        /// <summary>
        /// 历史记录管理器
        /// </summary>
        public ConsultationHistoryManager HistoryManager => _coordinator.HistoryManager;

        /// <summary>
        /// 导航处理器
        /// </summary>
        public ConsultationNavigationHandler NavigationHandler => _coordinator.NavigationHandler;

        #endregion

        #region 委托属性（向后兼容）

        /// <summary>
        /// 当前工作流步骤
        /// </summary>
        public CoreWorkflowStep CurrentStep 
        {
            get => (CoreWorkflowStep)StepManager.CurrentStep;
            set => StepManager.CurrentStep = (LYBT.Desktop.Consultation.Services.WorkflowStep)value;
        }

        /// <summary>
        /// 当前步骤内容
        /// </summary>
        public object? CurrentStepContent 
        {
            get => NavigationHandler.CurrentStepContent;
            set => NavigationHandler.CurrentStepContent = value;
        }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId 
        {
            get => DataManager.MedicalCaseId;
            set => DataManager.MedicalCaseId = value;
        }

        /// <summary>
        /// 医疗案例
        /// </summary>
        public MedicalCaseInfo? MedicalCase 
        {
            get => DataManager.MedicalCase;
            set => DataManager.MedicalCase = value;
        }

        /// <summary>
        /// 患者信息
        /// </summary>
        public PatientInfo? Patient 
        {
            get => DataManager.Patient;
            set => DataManager.Patient = value;
        }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName 
        {
            get => DataManager.PatientName;
            set => DataManager.PatientName = value;
        }

        /// <summary>
        /// 患者性别年龄
        /// </summary>
        public string PatientGenderAge 
        {
            get => DataManager.PatientGenderAge;
            set => DataManager.PatientGenderAge = value;
        }

        /// <summary>
        /// 患者电话
        /// </summary>
        public string PatientPhone 
        {
            get => DataManager.PatientPhone;
            set => DataManager.PatientPhone = value;
        }

        /// <summary>
        /// 是否已选择患者
        /// </summary>
        public bool HasSelectedPatient 
        {
            get => DataManager.HasSelectedPatient;
            set => DataManager.HasSelectedPatient = value;
        }

        /// <summary>
        /// 患者步骤是否激活
        /// </summary>
        public bool IsPatientStepActive 
        {
            get => StepManager.IsPatientStepActive;
            set => StepManager.IsPatientStepActive = value;
        }

        /// <summary>
        /// 四诊步骤是否激活
        /// </summary>
        public bool IsFourDiagnosisStepActive 
        {
            get => StepManager.IsFourDiagnosisStepActive;
            set => StepManager.IsFourDiagnosisStepActive = value;
        }

        /// <summary>
        /// 辨证步骤是否激活
        /// </summary>
        public bool IsDifferentiationStepActive 
        {
            get => StepManager.IsDifferentiationStepActive;
            set => StepManager.IsDifferentiationStepActive = value;
        }

        /// <summary>
        /// 处方步骤是否激活
        /// </summary>
        public bool IsPrescriptionStepActive 
        {
            get => StepManager.IsPrescriptionStepActive;
            set => StepManager.IsPrescriptionStepActive = value;
        }

        /// <summary>
        /// 患者历史记录
        /// </summary>
        public ObservableCollection<HistoryRecord> PatientHistory => HistoryManager.PatientHistory;

        /// <summary>
        /// 是否显示历史面板
        /// </summary>
        public bool IsHistoryPanelVisible 
        {
            get => HistoryManager.IsHistoryPanelVisible;
            set => HistoryManager.IsHistoryPanelVisible = value;
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading 
        {
            get => DataManager.IsDataLoading || HistoryManager.IsHistoryLoading || NavigationHandler.IsNavigating;
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage 
        {
            get => _coordinator.StatusMessage;
            set => _coordinator.StatusMessage = value;
        }

        #endregion

        #region 委托命令（向后兼容）

        /// <summary>
        /// 上一步命令
        /// </summary>
        public ICommand PreviousStepCommand => _coordinator.PreviousStepCommand;

        /// <summary>
        /// 下一步命令
        /// </summary>
        public ICommand NextStepCommand => _coordinator.NextStepCommand;

        /// <summary>
        /// 保存草稿命令
        /// </summary>
        public ICommand SaveDraftCommand => _coordinator.SaveDraftCommand;

        /// <summary>
        /// 完成工作流命令
        /// </summary>
        public ICommand CompleteWorkflowCommand => _coordinator.CompleteWorkflowCommand;

        /// <summary>
        /// 退出工作流命令
        /// </summary>
        public ICommand ExitWorkflowCommand => _coordinator.ExitWorkflowCommand;

        /// <summary>
        /// 切换历史面板命令
        /// </summary>
        public ICommand ToggleHistoryCommand => _coordinator.ToggleHistoryCommand;

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand => _coordinator.RefreshCommand;

        #endregion

        #region 构造函数

        public ConsultationWorkflowViewModel(ConsultationWorkflowCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            // 订阅协调器的属性变化事件，以便通知UI更新
            _coordinator.PropertyChanged += (s, e) =>
            {
                RaisePropertyChanged(e.PropertyName);
            };

            // 订阅各个管理器的属性变化事件
            StepManager.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
            DataManager.PropertyChanged += (s, e) => 
            {
                RaisePropertyChanged(e.PropertyName);
                // 当加载状态变化时，更新IsLoading属性
                if (e.PropertyName == nameof(DataManager.IsDataLoading))
                {
                    RaisePropertyChanged(nameof(IsLoading));
                }
            };
            HistoryManager.PropertyChanged += (s, e) => 
            {
                RaisePropertyChanged(e.PropertyName);
                // 当加载状态变化时，更新IsLoading属性
                if (e.PropertyName == nameof(HistoryManager.IsHistoryLoading))
                {
                    RaisePropertyChanged(nameof(IsLoading));
                }
            };
            NavigationHandler.PropertyChanged += (s, e) => 
            {
                RaisePropertyChanged(e.PropertyName);
                // 当导航状态变化时，更新IsLoading属性
                if (e.PropertyName == nameof(NavigationHandler.IsNavigating))
                {
                    RaisePropertyChanged(nameof(IsLoading));
                }
            };
        }

        #endregion

        #region INavigationAware（委托给协调器）

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _coordinator.OnNavigatedTo(navigationContext);
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _coordinator.OnNavigatedFrom(navigationContext);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return _coordinator.IsNavigationTarget(navigationContext);
        }

        #endregion

        #region 向后兼容方法

        /// <summary>
        /// 加载患者历史记录（向后兼容）
        /// </summary>
        public async Task LoadPatientHistoryAsync(Guid patientId)
        {
            await HistoryManager.LoadPatientHistoryAsync(patientId);
        }

        /// <summary>
        /// 切换历史面板（向后兼容）
        /// </summary>
        public void ToggleHistoryPanel()
        {
            HistoryManager.ToggleHistoryPanel();
        }

        /// <summary>
        /// 导入四诊历史数据（向后兼容）
        /// </summary>
        public async Task ImportFourDiagnosisAsync(HistoryRecord history)
        {
            var data = await HistoryManager.GetFourDiagnosisDataAsync(history);
            if (data != null)
            {
                await DataManager.SaveConsultationDataAsync(data);
            }
        }

        /// <summary>
        /// 导入处方历史数据（向后兼容）
        /// </summary>
        public async Task ImportPrescriptionAsync(HistoryRecord history)
        {
            var data = await HistoryManager.GetPrescriptionDataAsync(history);
            if (data != null)
            {
                await DataManager.SavePrescriptionDataAsync(data);
            }
        }

        #endregion
    }
}