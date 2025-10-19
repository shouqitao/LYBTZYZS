using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医案流程主视图ViewModel（Epic #1494 - Task #1496）
    /// 管理4步流程：患者选择 → 填写诊断 → 填写处方 → 完成医案
    /// </summary>
    public class MedicalCaseFlowViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IRegionManager _regionManager;

        #endregion

        #region 属性

        private FlowStep _currentStep = FlowStep.SelectPatient;
        /// <summary>
        /// 当前流程步骤
        /// </summary>
        public FlowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoNext));
                    RaisePropertyChanged(nameof(PatientInfoBarVisible));
                    RaisePropertyChanged(nameof(IsStep1));
                    RaisePropertyChanged(nameof(IsStep2));
                    RaisePropertyChanged(nameof(IsStep3));
                    RaisePropertyChanged(nameof(IsStep4));
                    RaisePropertyChanged(nameof(NextButtonText));
                    PreviousStepCommand.RaiseCanExecuteChanged();
                    NextStepCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private ViewModelBase? _currentStepViewModel;
        /// <summary>
        /// 当前步骤的ViewModel（用于ContentControl绑定）
        /// </summary>
        public ViewModelBase? CurrentStepViewModel
        {
            get => _currentStepViewModel;
            set => SetProperty(ref _currentStepViewModel, value);
        }

        private string _selectedPatientName = string.Empty;
        /// <summary>
        /// 已选患者姓名（Step 2-4显示在患者信息条）
        /// </summary>
        public string SelectedPatientName
        {
            get => _selectedPatientName;
            set => SetProperty(ref _selectedPatientName, value);
        }

        private string _selectedPatientInfo = string.Empty;
        /// <summary>
        /// 已选患者信息（性别/年龄/电话）
        /// </summary>
        public string SelectedPatientInfo
        {
            get => _selectedPatientInfo;
            set => SetProperty(ref _selectedPatientInfo, value);
        }

        private Guid _medicalCaseId = Guid.Empty;
        /// <summary>
        /// 当前医案ID（患者选择后自动创建）
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        /// <summary>
        /// 是否可以返回上一步
        /// </summary>
        public bool CanGoBack => CurrentStep > FlowStep.SelectPatient;

        /// <summary>
        /// 是否可以前进下一步
        /// </summary>
        public bool CanGoNext => CurrentStep < FlowStep.CompleteMedicalCase;

        /// <summary>
        /// 患者信息条是否可见（Step 2-4显示）
        /// </summary>
        public bool PatientInfoBarVisible => CurrentStep >= FlowStep.FillConsultation;

        /// <summary>
        /// 下一步按钮文字（Step 4显示"完成看诊"）
        /// </summary>
        public string NextButtonText => CurrentStep == FlowStep.CompleteMedicalCase ? "完成看诊" : "下一步";

        // 进度条高亮标记
        public bool IsStep1 => CurrentStep == FlowStep.SelectPatient;
        public bool IsStep2 => CurrentStep == FlowStep.FillConsultation;
        public bool IsStep3 => CurrentStep == FlowStep.FillPrescription;
        public bool IsStep4 => CurrentStep == FlowStep.CompleteMedicalCase;

        #endregion

        #region 命令

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand PreviousStepCommand { get; }
        public DelegateCommand NextStepCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseFlowViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            PreviousStepCommand = new DelegateCommand(ExecutePreviousStep, CanExecutePreviousStep);
            NextStepCommand = new DelegateCommand(ExecuteNextStep, CanExecuteNextStep);
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            Logger.LogInformation("MedicalCaseFlowViewModel已初始化，当前步骤：{CurrentStep}", CurrentStep);
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
                Logger.LogInformation("返回主页");
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页时发生异常");
            }
        }

        /// <summary>
        /// 上一步
        /// </summary>
        private void ExecutePreviousStep()
        {
            if (CurrentStep <= FlowStep.SelectPatient)
            {
                Logger.LogWarning("已是第一步，无法返回");
                return;
            }

            try
            {
                var previousStep = (FlowStep)((int)CurrentStep - 1);
                Logger.LogInformation("从 {CurrentStep} 返回到 {PreviousStep}", CurrentStep, previousStep);
                NavigateToStep(previousStep);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行上一步时发生异常");
            }
        }

        private bool CanExecutePreviousStep()
        {
            return CanGoBack;
        }

        /// <summary>
        /// 下一步
        /// </summary>
        private void ExecuteNextStep()
        {
            if (CurrentStep >= FlowStep.CompleteMedicalCase)
            {
                // Step 4: 完成看诊，返回主页
                Logger.LogInformation("完成看诊，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                ExecuteBackToHome();
                return;
            }

            try
            {
                var nextStep = (FlowStep)((int)CurrentStep + 1);
                Logger.LogInformation("从 {CurrentStep} 前进到 {NextStep}", CurrentStep, nextStep);
                NavigateToStep(nextStep);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行下一步时发生异常");
            }
        }

        private bool CanExecuteNextStep()
        {
            // TODO: 添加每个Step的验证逻辑
            // Step 1: 必须选择患者
            // Step 2: Consultation必填字段验证
            // Step 3: Prescription至少1味药材
            return true; // 暂时允许所有步骤前进
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private void ExecuteSaveDraft()
        {
            try
            {
                Logger.LogInformation("保存草稿，当前步骤：{CurrentStep}, MedicalCaseId: {MedicalCaseId}", CurrentStep, MedicalCaseId);
                // TODO: 实现草稿保存逻辑（Task #1502）
                // 1. 收集当前Step的数据
                // 2. 保存到本地存储或后端
                // 3. 显示成功提示
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿时发生异常");
            }
        }

        /// <summary>
        /// 取消流程
        /// </summary>
        private void ExecuteCancel()
        {
            try
            {
                Logger.LogInformation("取消医案流程，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                // TODO: 确认对话框（是否放弃当前编辑？）
                ExecuteBackToHome();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消流程时发生异常");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 导航到指定步骤
        /// </summary>
        private void NavigateToStep(FlowStep step)
        {
            CurrentStep = step;

            // TODO: Task #1497-#1500 - 创建各个Step的View后，实现真实导航
            // 当前使用占位ViewModel
            switch (step)
            {
                case FlowStep.SelectPatient:
                    Logger.LogInformation("导航到患者选择步骤");
                    // _regionManager.RequestNavigate("MedicalCaseStepRegion", "PatientSelectionView");
                    CurrentStepViewModel = null; // 占位，待Task #1497实现
                    break;

                case FlowStep.FillConsultation:
                    Logger.LogInformation("导航到诊断录入步骤");
                    // _regionManager.RequestNavigate("MedicalCaseStepRegion", "ConsultationFormView");
                    CurrentStepViewModel = null; // 占位，待Task #1498实现
                    break;

                case FlowStep.FillPrescription:
                    Logger.LogInformation("导航到处方编辑步骤");
                    // _regionManager.RequestNavigate("MedicalCaseStepRegion", "PrescriptionEditorView");
                    CurrentStepViewModel = null; // 占位，待Task #1499实现
                    break;

                case FlowStep.CompleteMedicalCase:
                    Logger.LogInformation("导航到完成医案步骤");
                    // _regionManager.RequestNavigate("MedicalCaseStepRegion", "CompletionView");
                    CurrentStepViewModel = null; // 占位，待Task #1500实现
                    break;

                default:
                    Logger.LogWarning("未知步骤：{Step}", step);
                    break;
            }
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 从HomeView传来的参数
            var startStep = navigationContext.Parameters.GetValue<int>("StartStep");
            if (startStep > 0 && startStep <= 4)
            {
                Logger.LogInformation("接收到StartStep参数：{StartStep}", startStep);
                NavigateToStep((FlowStep)startStep);
            }

            var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                Logger.LogInformation("接收到SearchKeyword参数：{SearchKeyword}", searchKeyword);
                // TODO: 在Step 1中预填搜索关键字
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 允许重复导航（新的医案流程）
            return false;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开MedicalCaseFlowView，当前步骤：{CurrentStep}", CurrentStep);
        }

        #endregion
    }
}
