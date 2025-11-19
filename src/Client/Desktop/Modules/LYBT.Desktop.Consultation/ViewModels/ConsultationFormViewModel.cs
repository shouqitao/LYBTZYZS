using LYBT.Desktop.Consultation.Components; // Issue #1784: 添加Component命名空间
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces; // 保留用于IValidatable和ISaveable接口
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 诊断表单ViewModel - Task #1498 Step 2
    /// Epic #1494: 医案流程UI重构
    ///
    /// 功能：
    /// - 填写诊断信息（主诉、现病史、四诊、中医诊断、治疗原则）
    /// - 必填字段验证（主诉、中医诊断）
    /// - 保存时创建Consultation实体
    /// - 集成到MedicalCaseFlowView的Step 2
    /// </summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        #region 服务依赖

        // Issue #1784: 使用Component替代Repository依赖
        private readonly ConsultationDataManager _dataManager;
        private readonly ConsultationCommandHandler _commandHandler;
        // Epic #1773: 已移除IMedicalCaseRepository依赖，使用DataManager替代

        #endregion

        #region 外部传入数据

        private PatientDto? _currentPatient;
        /// <summary>
        /// 当前选择的患者信息（从Step 1传递）
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        private Guid _medicalCaseId = Guid.Empty;
        /// <summary>
        /// 当前医案ID（从Step 1传递）
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        #endregion

        #region 基本诊断信息字段

        private string _chiefComplaint = string.Empty;
        /// <summary>
        /// 主诉（必填）
        /// </summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (SetProperty(ref _chiefComplaint, value))
                {
                    RaisePropertyChanged(nameof(HasChiefComplaint));
                }
            }
        }

        private string _presentIllness = string.Empty;
        /// <summary>
        /// 现病史
        /// </summary>
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        private string _tcmDiagnosis = string.Empty;
        /// <summary>
        /// 中医诊断（必填）
        /// </summary>
        public string TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set
            {
                if (SetProperty(ref _tcmDiagnosis, value))
                {
                    RaisePropertyChanged(nameof(HasTCMDiagnosis));
                }
            }
        }

        private string _treatmentPrinciple = string.Empty;
        /// <summary>
        /// 治疗原则
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        #endregion

        #region 四诊合参字段

        private string _inspection = string.Empty;
        /// <summary>
        /// 望诊（神色、形体、舌象）
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string _auscultationOlfaction = string.Empty;
        /// <summary>
        /// 闻诊（语声、呼吸、口气）
        /// </summary>
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        private string _inquiry = string.Empty;
        /// <summary>
        /// 问诊（主诉、病史等）
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string _palpation = string.Empty;
        /// <summary>
        /// 切诊（脉象、腹诊）
        /// </summary>
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        #endregion

        #region 其他字段

        private string _remark = string.Empty;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region REQ-001: 三步工作流优化-Step1属性

        private bool _prescriptionEnabled = true; // 默认开处方
        /// <summary>
        /// 是否开处方（RadioButton选中状态）
        /// </summary>
        public bool PrescriptionEnabled
        {
            get => _prescriptionEnabled;
            set
            {
                if (SetProperty(ref _prescriptionEnabled, value))
                {
                    RaisePropertyChanged(nameof(PrescriptionDisabled));
                }
            }
        }

        /// <summary>
        /// 不开处方（反向绑定）
        /// </summary>
        public bool PrescriptionDisabled
        {
            get => !_prescriptionEnabled;
            set
            {
                if (value)
                {
                    PrescriptionEnabled = false;
                }
            }
        }

        private DateTime? _step1CompletedAt;
        /// <summary>
        /// Step1完成时间（服务端返回）
        /// </summary>
        public DateTime? Step1CompletedAt
        {
            get => _step1CompletedAt;
            set
            {
                if (SetProperty(ref _step1CompletedAt, value))
                {
                    RaisePropertyChanged(nameof(Step1CompletedAtText));
                    RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
                }
            }
        }

        /// <summary>
        /// Step1完成时间文本（格式化显示）
        /// </summary>
        public string Step1CompletedAtText =>
            Step1CompletedAt.HasValue
                ? $" Step1已完成（{Step1CompletedAt.Value:yyyy-MM-dd HH:mm}）"
                : string.Empty;

        /// <summary>
        /// Step1完成时间可见性
        /// </summary>
        public System.Windows.Visibility Step1CompletedAtVisibility =>
            Step1CompletedAt.HasValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否已填写主诉（用于UI必填提示）
        /// </summary>
        public bool HasChiefComplaint => !string.IsNullOrWhiteSpace(ChiefComplaint);

        /// <summary>
        /// 是否已填写中医诊断（用于UI必填提示）
        /// </summary>
        public bool HasTCMDiagnosis => !string.IsNullOrWhiteSpace(TCMDiagnosis);

        #endregion

        #region IValidatable实现

        private string _validationMessage = string.Empty;
        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 验证当前步骤数据（主诉、中医诊断必填）
        /// </summary>
        public bool Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                errors.Add("主诉不能为空");
            }

            if (string.IsNullOrWhiteSpace(TCMDiagnosis))
            {
                errors.Add("中医诊断不能为空");
            }

            if (errors.Any())
            {
                ValidationMessage = string.Join("；", errors);
                Logger.LogWarning("诊断表单验证失败：{ValidationMessage}", ValidationMessage);
                return false;
            }

            ValidationMessage = string.Empty;
            Logger.LogInformation("诊断表单验证通过");
            return true;
        }

        #endregion

        #region 辅助方法

        // Issue #1562 Phase 1: 已删除工作流事件发布逻辑（PublishConsultationCompletedEvent）

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存诊断数据（创建Consultation实体）
        /// Issue #1784: 使用DataManager替代直接Repository访问
        /// </summary>
        /// <summary>
        /// 保存诊断数据
        /// Issue #1784: 从ViewModel同步数据到DataManager并保存
        /// Issue #1794: 优化方法长度（52→30行）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                Logger.LogInformation("开始保存诊断数据，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                var (isValid, errorMessage) = ValidatePrerequisites();
                if (!isValid)
                {
                    ValidationMessage = errorMessage;
                    return false;
                }

                // Issue #1784: 从ViewModel同步数据到DataManager
                SyncToDataManager();

                // Issue #1784: 使用DataManager保存
                var success = await _dataManager.SaveAsync();

                return HandleSaveResult(success);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断数据失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                ValidationMessage = $"保存失败：{ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 验证保存前置条件
        /// Issue #1794: 从SaveAsync提取，封装前置验证逻辑
        /// </summary>
        private (bool isValid, string errorMessage) ValidatePrerequisites()
        {
            if (MedicalCaseId == Guid.Empty)
            {
                Logger.LogError("MedicalCaseId为空，无法创建Consultation");
                return (false, "医案ID为空，无法保存诊断数据");
            }

            if (CurrentPatient == null)
            {
                Logger.LogError("CurrentPatient为空，无法创建Consultation");
                return (false, "患者信息丢失，无法保存诊断数据");
            }

            if (SessionManager?.CurrentUser == null)
            {
                Logger.LogError("SessionManager.CurrentUser为空，无法创建Consultation");
                return (false, "用户信息丢失，无法保存诊断数据");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 处理保存结果
        /// Issue #1794: 从SaveAsync提取，封装结果处理逻辑
        /// </summary>
        private bool HandleSaveResult(bool success)
        {
            if (success)
            {
                Logger.LogInformation("诊断数据保存成功，ConsultationId: {ConsultationId}", MedicalCaseId);
                return true;
            }
            else
            {
                Logger.LogError("DataManager保存失败");
                ValidationMessage = "保存失败，请检查数据有效性";
                return false;
            }
        }

        #endregion

        #region 命令

        public DelegateCommand ClearFormCommand { get; }

        // Issue #1562 Phase 1: 已删除ImportFromHistoryCommand（未实现的扩展功能）

        // Issue #1590: REQ-001 - 三步工作流优化-Step1命令
        public DelegateCommand CompleteStep1Command { get; }
        public DelegateCommand ShowOtherCasesQueryCommand { get; }

        // Issue #1594: 暂存功能完善
        public DelegateCommand SaveDraftCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationFormViewModel(
            ConsultationDataManager dataManager, // Issue #1784: 注入DataManager
            ConsultationCommandHandler commandHandler, // Issue #1784: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1784: 注入Component
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            // Epic #1773: 已移除IMedicalCaseRepository依赖

            // 初始化命令
            ClearFormCommand = new DelegateCommand(ExecuteClearForm);

            // Issue #1590: REQ-001 - 初始化新命令
            CompleteStep1Command = new DelegateCommand(async () => await ExecuteCompleteStep1());
            ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery);

            // Issue #1594: 暂存功能完善
            SaveDraftCommand = new DelegateCommand(async () => await ExecuteSaveDraft());

            Logger.LogInformation("ConsultationFormViewModel已初始化");
        }

        #endregion

        #region 命令实现

        // Issue #1562 Phase 1: 已删除ExecuteImportFromHistory（未实现的扩展功能）

        /// <summary>
        /// 清空表单
        /// Issue #1784: 使用CommandHandler替代手动清空字段
        /// </summary>
        private void ExecuteClearForm()
        {
            try
            {
                Logger.LogInformation("执行清空表单");

                // Issue #1784: 使用CommandHandler清空表单
                _commandHandler.ClearForm();

                // 从DataManager同步清空后的数据到ViewModel
                SyncFromDataManager();

                ValidationMessage = string.Empty;

                Logger.LogInformation("表单已清空");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清空表单失败");
            }
        }

        #endregion

        #region REQ-001: 命令实现

        /// <summary>
        /// 完成Step1（辩证）
        /// </summary>
        private async Task ExecuteCompleteStep1()
        {
            try
            {
                SetIsBusy(true, "正在完成Step1...");

                // 1. 验证表单
                if (!Validate())
                {
                    await ShowErrorMessageAsync(ValidationMessage);
                    return;
                }

                // 2. 调用API完成Step1（通过MedicalCase聚合根）
                var request = CreateCompleteStep1Request();
                var stepDto = await _dataManager.CompleteStep1Async(MedicalCaseId, request);

                // 3. 更新本地状态
                Step1CompletedAt = stepDto.Step1CompletedAt;

                // 4. 导航到下一步（Step2或Step3）
                await NavigateToNextStepAsync();

                Logger.LogInformation("Step1完成成功，导航到下一步");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成Step1失败");
                await ShowErrorMessageAsync($"完成Step1失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 创建CompleteStep1Request
        /// Issue #1794: 提取请求对象创建逻辑
        /// </summary>
        private CompleteStep1Request CreateCompleteStep1Request()
        {
            return new CompleteStep1Request
            {
                PrescriptionEnabled = PrescriptionEnabled
            };
        }

        /// <summary>
        /// 导航到下一步
        /// Issue #1794: 提取导航逻辑
        /// </summary>
        private async Task NavigateToNextStepAsync()
        {
            if (PrescriptionEnabled)
            {
                // 跳转到Step2（处方录入）- PrescriptionEditorView
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", MedicalCaseId },
                    { "CurrentPatient", CurrentPatient }
                };
                RegionManager?.RequestNavigate("ContentRegion", "PrescriptionEditorView", parameters);
            }
            else
            {
                // 跳转到Step3（汇总页）- 暂未实现，显示提示信息
                await ShowSuccessMessageAsync("Step1已完成！\n您选择了不开处方，后续将直接进入汇总页（暂未实现）。");
            }
        }

        /// <summary>
        /// 显示其他病案查询浮动菜单
        /// </summary>
        private void ExecuteShowOtherCasesQuery()
        {
            try
            {
                Logger.LogInformation("打开其他病案查询浮动菜单");

                // REQ-003: 导航到其他病案查询页（Phase 3实现）
                // 暂时显示提示信息
                ShowInfoMessage("其他病案查询功能将在Phase 3实现");

                // 未来实现代码：
                // var parameters = new NavigationParameters
                // {
                //     { "PatientId", CurrentPatient?.Id }
                // };
                // RegionManager?.RequestNavigate("ContentRegion", "OtherCasesQueryView", parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开其他病案查询失败");
            }
        }

        /// <summary>
        /// 保存草稿（Issue #1594）
        /// 功能：调用SaveAsync()保存数据，但不完成Step1
        /// </summary>
        private async Task ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存草稿...");
                Logger.LogInformation("开始保存诊断草稿，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // 调用现有的SaveAsync()方法保存数据
                // 注意：不调用CompleteStep1API，不更新Step1CompletedAt
                var saved = await SaveAsync();

                if (saved)
                {
                    await ShowSuccessMessageAsync("诊断草稿已保存！\n提示：请在填写完成后点击【完成Step1】按钮。");
                    Logger.LogInformation("诊断草稿保存成功");
                }
                else
                {
                    Logger.LogWarning("诊断草稿保存失败：{ValidationMessage}", ValidationMessage);
                    await ShowErrorMessageAsync($"保存草稿失败：{ValidationMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断草稿异常");
                await ShowErrorMessageAsync($"保存草稿失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region INavigationAware

        /// <summary>
        /// 导航到当前视图时调用
        /// Issue #1557 Phase 3: 接收MedicalCaseFlowViewModel传来的MedicalCaseId和CurrentPatient
        /// Issue #1784: 初始化DataManager
        /// </summary>
        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // 接收MedicalCaseId参数
                var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                if (medicalCaseId != Guid.Empty)
                {
                    Logger.LogInformation("接收到MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    MedicalCaseId = medicalCaseId;

                    // Issue #1784: 初始化DataManager
                    _dataManager.MedicalCaseId = medicalCaseId;
                    await _dataManager.InitializeAsync(medicalCaseId);

                    // 从DataManager同步数据到ViewModel属性
                    if (_dataManager.Current != null)
                    {
                        SyncFromDataManager();
                    }
                }

                // 接收CurrentPatient参数
                var currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");
                if (currentPatient != null)
                {
                    Logger.LogInformation("接收到CurrentPatient: {PatientName}", currentPatient.Name);
                    CurrentPatient = currentPatient;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到ConsultationFormView时发生异常");
            }
        }

        /// <summary>
        /// Issue #1784: 从DataManager同步数据到ViewModel
        /// </summary>
        private void SyncFromDataManager()
        {
            if (_dataManager.Current == null) return;

            var consultation = _dataManager.Current;
            ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
            PresentIllness = consultation.PresentIllness ?? string.Empty;
            Inspection = consultation.Inspection ?? string.Empty;
            AuscultationOlfaction = consultation.AuscultationOlfaction ?? string.Empty;
            Inquiry = consultation.Inquiry ?? string.Empty;
            Palpation = consultation.Palpation ?? string.Empty;
            TCMDiagnosis = consultation.TCMDiagnosis ?? string.Empty;
            TreatmentPrinciple = consultation.TreatmentPrinciple ?? string.Empty;
            Remark = consultation.Remark ?? string.Empty;
        }

        /// <summary>
        /// Issue #1784: 从ViewModel同步数据到DataManager
        /// </summary>
        private void SyncToDataManager()
        {
            _dataManager.UpdateField(nameof(ConsultationDto.ChiefComplaint), ChiefComplaint);
            _dataManager.UpdateField(nameof(ConsultationDto.PresentIllness), PresentIllness);
            _dataManager.UpdateField(nameof(ConsultationDto.Inspection), Inspection);
            _dataManager.UpdateField(nameof(ConsultationDto.AuscultationOlfaction), AuscultationOlfaction);
            _dataManager.UpdateField(nameof(ConsultationDto.Inquiry), Inquiry);
            _dataManager.UpdateField(nameof(ConsultationDto.Palpation), Palpation);
            _dataManager.UpdateField(nameof(ConsultationDto.TCMDiagnosis), TCMDiagnosis);
            _dataManager.UpdateField(nameof(ConsultationDto.TreatmentPrinciple), TreatmentPrinciple);
            _dataManager.UpdateField(nameof(ConsultationDto.Remark), Remark);
        }

        #endregion
    }
}
