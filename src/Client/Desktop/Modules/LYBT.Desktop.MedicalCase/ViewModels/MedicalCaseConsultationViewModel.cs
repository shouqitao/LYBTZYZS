using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 病案看诊ViewModel（实现动态流程）
    /// Task 3.2 (#1659): 实现MedicalCaseConsultationViewModel
    /// </summary>
    public class MedicalCaseConsultationViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        // Issue #1783: 使用DataManager替代直接Api访问
        private readonly MedicalCaseDataManager _dataManager;

        #endregion

        #region 辨证信息（完整四诊字段）

        private string? _chiefComplaint;
        private string? _presentIllness;
        private string? _inspection;            // 望诊
        private string? _auscultationOlfaction; // 闻诊
        private string? _inquiry;               // 问诊
        private string? _palpation;             // 切诊
        private string? _tcmDiagnosis;          // 中医诊断
        private string? _treatmentPrinciple;
        private string? _medicalAdvice;         // 医嘱
        private string? _remark;

        #endregion

        #region 开处方决策

        private bool _needsPrescription;
        private bool _showPrescriptionPanel;
        private CancellationTokenSource? _setPrescriptionFlagCts;

        #endregion

        #region 状态管理

        private Guid _medicalCaseId;
        private Guid _patientId;
        private Guid _doctorId;
        private bool _isSaving;
        private bool _isSavingPrescriptionFlag;
        private bool _canEdit;

        #endregion

        #region 构造函数

        public MedicalCaseConsultationViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1783: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            // 初始化命令
            SaveConsultationCommand = new DelegateCommand(
                async () => await SaveConsultationAsync(),
                () => CanEdit && !IsSaving)
                .ObservesProperty(() => CanEdit)
                .ObservesProperty(() => IsSaving);

            SaveDraftCommand = new DelegateCommand(
                async () => await SaveDraftAsync(),
                () => !IsSaving)
                .ObservesProperty(() => IsSaving);

            CompleteCommand = new DelegateCommand(
                async () => await CompleteAsync(),
                () => CanEdit && !IsSaving)
                .ObservesProperty(() => CanEdit)
                .ObservesProperty(() => IsSaving);

            Logger.LogInformation("MedicalCaseConsultationViewModel已初始化（Task 3.2 #1659）");
        }

        #endregion

        #region 属性（完整四诊字段）

        /// <summary>主诉</summary>
        public string? ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        /// <summary>现病史</summary>
        public string? PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        /// <summary>望诊结果</summary>
        public string? Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        /// <summary>闻诊结果</summary>
        public string? AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        /// <summary>问诊结果</summary>
        public string? Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        /// <summary>切诊结果</summary>
        public string? Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        /// <summary>中医诊断</summary>
        public string? TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set => SetProperty(ref _tcmDiagnosis, value);
        }

        /// <summary>治疗原则</summary>
        public string? TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        /// <summary>医嘱</summary>
        public string? MedicalAdvice
        {
            get => _medicalAdvice;
            set => SetProperty(ref _medicalAdvice, value);
        }

        /// <summary>备注</summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 是否需要开处方（RadioBox绑定）
        /// Task 3.4 (#1661): RadioBox变化时自动保存
        /// </summary>
        public bool NeedsPrescription
        {
            get => _needsPrescription;
            set
            {
                if (SetProperty(ref _needsPrescription, value))
                {
                    // RadioBox变化时自动保存标志并切换UI
                    ShowPrescriptionPanel = value;
                    _ = SetPrescriptionFlagAsync(value);
                }
            }
        }

        /// <summary>
        /// 是否显示处方输入面板
        /// </summary>
        public bool ShowPrescriptionPanel
        {
            get => _showPrescriptionPanel;
            set => SetProperty(ref _showPrescriptionPanel, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        /// <summary>
        /// 是否正在保存开处方标志
        /// </summary>
        public bool IsSavingPrescriptionFlag
        {
            get => _isSavingPrescriptionFlag;
            set => SetProperty(ref _isSavingPrescriptionFlag, value);
        }

        public bool CanEdit
        {
            get => _canEdit;
            set => SetProperty(ref _canEdit, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveConsultationCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand CompleteCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 加载病案数据（支持继续看诊）
        /// Task 3.6 (#1663): 继续看诊功能
        /// </summary>
        public async Task LoadAsync(Guid medicalCaseId)
        {
            try
            {
                _medicalCaseId = medicalCaseId;
                Logger.LogInformation("开始加载病案数据: {MedicalCaseId}", medicalCaseId);

                // 1. 获取病案详情
                // Issue #1783: 使用DataManager包装Repository方法（GetByIdWithDetailsAsync）
                var medicalCase = await _dataManager.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    await ShowErrorMessageAsync("加载病案失败：未找到病案数据");
                    return;
                }

                // Task 3.5 (#1662): 保存PatientId和DoctorId用于暂存功能
                _patientId = medicalCase.PatientId;
                _doctorId = medicalCase.DoctorId;

                // 2. 恢复辨证信息（完整四诊字段）
                if (medicalCase.Consultation != null)
                {
                    ChiefComplaint = medicalCase.Consultation.ChiefComplaint;
                    PresentIllness = medicalCase.Consultation.PresentIllness;
                    Inspection = medicalCase.Consultation.Inspection;
                    AuscultationOlfaction = medicalCase.Consultation.AuscultationOlfaction;
                    Inquiry = medicalCase.Consultation.Inquiry;
                    Palpation = medicalCase.Consultation.Palpation;
                    TCMDiagnosis = medicalCase.Consultation.TCMDiagnosis;
                    TreatmentPrinciple = medicalCase.Consultation.TreatmentPrinciple;
                    MedicalAdvice = medicalCase.Consultation.MedicalAdvice;
                    Remark = medicalCase.Consultation.Remark;
                }

                // 3. 恢复开处方标志
                // Task 3.6 (#1663): 根据是否有处方来判断NeedsPrescription
                // 说明：MedicalCaseDto没有NeedsPrescription字段，通过Prescription是否为null判断
                bool hasPrescription = medicalCase.Prescription != null;
                NeedsPrescription = hasPrescription;
                ShowPrescriptionPanel = hasPrescription;

                // 4. 检查是否可编辑（根据病案状态）
                // Phase 2: 将DTO业务逻辑移至ViewModel层
                CanEdit = medicalCase.CaseStatus == MedicalCaseStatus.Active || medicalCase.CaseStatus == MedicalCaseStatus.Draft;

                // 5. 如有处方，触发加载处方事件（后续集成任务）
                // TODO #1705: 实现PrescriptionLoadedEvent通知PrescriptionEditor加载处方数据（Epic #1676 Phase 3）
                // 说明：当前处方面板显示/隐藏已通过ShowPrescriptionPanel属性实现
                // if (medicalCase.Prescription != null)
                // {
                //     EventAggregator.GetEvent<PrescriptionLoadedEvent>()
                //         .Publish(medicalCase.Prescription);
                // }

                Logger.LogInformation("病案数据加载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载病案失败");
                await ShowErrorMessageAsync($"加载病案失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存辨证信息
        /// </summary>
        private async Task SaveConsultationAsync()
        {
            if (!ValidateConsultation())
            {
                return;
            }

            IsSaving = true;
            Logger.LogInformation("开始保存辨证信息");

            try
            {
                var request = new ConsultationInputDto
                {
                    Id = _medicalCaseId,
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness,
                    Inspection = Inspection,
                    AuscultationOlfaction = AuscultationOlfaction,
                    Inquiry = Inquiry,
                    Palpation = Palpation,
                    TCMDiagnosis = TCMDiagnosis,
                    TreatmentPrinciple = TreatmentPrinciple,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark
                };

                // Issue #1783: 使用DataManager业务命令方法
                var response = await _dataManager.UpdateConsultationAsync(_medicalCaseId, request);

                if (response.Success)
                {
                    await ShowSuccessMessageAsync("辨证信息已保存");
                    Logger.LogInformation("辨证信息保存成功");

                    // 发布事件通知其他组件
                    // TODO: Task 3.4 (#1661) - 实现ConsultationSavedEvent
                    // EventAggregator.GetEvent<ConsultationSavedEvent>()
                    //     .Publish(response.Data);
                }
                else
                {
                    await ShowErrorMessageAsync($"保存失败: {response.Message}");
                    Logger.LogWarning("辨证信息保存失败: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存辨证信息失败");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 标记是否开处方（RadioBox变化时自动调用）
        /// Task 3.4 (#1661): 实现防抖处理、错误回滚、加载指示器
        /// </summary>
        private async Task SetPrescriptionFlagAsync(bool needsPrescription)
        {
            // 防抖处理：取消之前的请求
            _setPrescriptionFlagCts?.Cancel();
            _setPrescriptionFlagCts?.Dispose();
            _setPrescriptionFlagCts = new CancellationTokenSource();

            var cts = _setPrescriptionFlagCts;

            try
            {
                // 防抖延迟（500ms）
                await Task.Delay(500, cts.Token);

                // 如果延迟期间被取消，则直接返回
                if (cts.Token.IsCancellationRequested)
                {
                    Logger.LogDebug("开处方标志更新被取消（防抖）");
                    return;
                }

                // 显示加载指示器
                IsSavingPrescriptionFlag = true;

                var request = new SetPrescriptionFlagRequest
                {
                    NeedsPrescription = needsPrescription
                };

                // Issue #1783: 使用DataManager业务命令方法
                var response = await _dataManager.SetPrescriptionFlagAsync(_medicalCaseId, request);

                if (response.Success)
                {
                    Logger.LogInformation("开处方标志已更新: {NeedsPrescription}", needsPrescription);
                }
                else
                {
                    // API返回失败，回滚UI状态
                    _needsPrescription = !needsPrescription;
                    RaisePropertyChanged(nameof(NeedsPrescription));
                    ShowPrescriptionPanel = !needsPrescription;

                    Logger.LogWarning("更新开处方标志失败: {Message}", response.Message);
                    await ShowErrorMessageAsync($"操作失败: {response.Message}");
                }
            }
            catch (OperationCanceledException)
            {
                // 防抖取消，正常情况，不记录错误
                Logger.LogDebug("开处方标志更新被取消（用户快速切换）");
            }
            catch (Exception ex)
            {
                // 异常情况，回滚UI状态
                _needsPrescription = !needsPrescription;
                RaisePropertyChanged(nameof(NeedsPrescription));
                ShowPrescriptionPanel = !needsPrescription;

                Logger.LogError(ex, "更新开处方标志失败");
                await ShowErrorMessageAsync($"操作失败: {ex.Message}");
            }
            finally
            {
                // 如果当前CTS未被替换，则隐藏加载指示器
                if (_setPrescriptionFlagCts == cts)
                {
                    IsSavingPrescriptionFlag = false;
                }
            }
        }

        /// <summary>
        /// 暂存病案
        /// Task 3.5 (#1662): 暂存病案功能
        /// </summary>
        private async Task SaveDraftAsync()
        {
            IsSaving = true;
            Logger.LogInformation("开始暂存病案");

            try
            {
                // Task 3.5 (#1662): 先保存辨证信息
                await SaveConsultationAsync();

                // 然后暂存病案状态
                var request = new MedicalCaseUpdateDto
                {
                    Id = _medicalCaseId,
                    PatientId = _patientId,
                    DoctorId = _doctorId,
                    Remark = Remark,
                    Status = "Draft" // 暂存状态
                };

                // Issue #1783: 使用DataManager业务命令方法
                var response = await _dataManager.SaveAsDraftAsync(_medicalCaseId, request);

                if (response.Success)
                {
                    await ShowSuccessMessageAsync("病案已暂存，可稍后继续看诊");
                    Logger.LogInformation("病案暂存成功");

                    // 导航回病案列表
                    // TODO: Task 3.5 (#1662) - 实现NavigateRequestEvent
                    // EventAggregator.GetEvent<NavigateRequestEvent>()
                    //     .Publish("MedicalCaseList");
                }
                else
                {
                    await ShowErrorMessageAsync($"暂存失败: {response.Message}");
                    Logger.LogWarning("病案暂存失败: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "暂存病案失败");
                await ShowErrorMessageAsync($"暂存失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 完成病案
        /// </summary>
        private async Task CompleteAsync()
        {
            if (!ValidateComplete())
            {
                return;
            }

            IsSaving = true;
            Logger.LogInformation("开始完成病案");

            try
            {
                // TODO #1706: 实现完成病案的API调用（依赖Phase 4 - CloseCaseAsync，Epic #1676）
                // 当前API接口中可能没有CompleteMedicalCaseAsync方法
                // 可能需要通过UpdateStatusAsync或其他方法实现

                await ShowWarningMessageAsync("完成病案功能待实现");
                Logger.LogWarning("完成病案功能尚未实现");

                // 临时实现示例：
                // var response = await _medicalCaseApi.CompleteMedicalCaseAsync(_medicalCaseId);
                // if (response.IsSuccess)
                // {
                //     await ShowSuccessMessageAsync("病案已完成");
                //     Logger.LogInformation("病案完成成功");
                //
                //     // 导航回病案列表
                //     EventAggregator.GetEvent<NavigateRequestEvent>()
                //         .Publish("MedicalCaseList");
                // }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成病案失败");
                await ShowErrorMessageAsync($"完成失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 验证辨证信息
        /// </summary>
        private bool ValidateConsultation()
        {
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                _ = ShowWarningMessageAsync("主诉不能为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证完成条件
        /// </summary>
        private bool ValidateComplete()
        {
            if (string.IsNullOrWhiteSpace(ChiefComplaint) ||
                string.IsNullOrWhiteSpace(TCMDiagnosis))
            {
                _ = ShowWarningMessageAsync("至少需要填写主诉和诊断");
                return false;
            }

            if (NeedsPrescription)
            {
                // TODO #1707: 检查是否已创建处方（依赖Phase 4 - HasPrescriptionAsync，Epic #1676）
                // 可通过PrescriptionViewModel状态判断
                Logger.LogWarning("需要检查处方是否已创建");
            }

            return true;
        }

        #endregion
    }
}
