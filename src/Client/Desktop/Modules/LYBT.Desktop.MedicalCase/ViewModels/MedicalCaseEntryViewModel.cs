using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 病案录入视图模型 - Issue #1463: 以MedicalCase为中心的激进重构
    /// 实现基于四诊数据的病案记录录入功能
    /// Epic #1456: 临床工作台看诊流程完整实现
    /// </summary>
    public class MedicalCaseEntryViewModel : UnifiedViewModelBase, INavigationAware
    {
        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IDialogService _dialogService;

        #endregion

        #region 数据属性

        // 患者信息
        private PatientDto? _currentPatient;
        private string _patientName = "未选择患者";

        /// <summary>
        /// 当前患者
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set
            {
                if (SetProperty(ref _currentPatient, value))
                {
                    PatientName = value?.Name ?? "未选择患者";
                }
            }
        }

        /// <summary>
        /// 患者姓名显示
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        // 医疗案例ID (后台自动创建)
        private Guid? _medicalCaseId;

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid? MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        // 四诊数据
        private string _inspection = string.Empty;              // 望诊
        private string _auscultationOlfaction = string.Empty;   // 闻诊
        private string _inquiry = string.Empty;                 // 问诊
        private string _palpation = string.Empty;               // 切诊

        /// <summary>
        /// 望诊 (神色、形体、舌象等)
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        /// <summary>
        /// 闻诊 (语声、呼吸、咳嗽、口气等)
        /// </summary>
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        /// <summary>
        /// 问诊 (主诉、现病史、既往史等)
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        /// <summary>
        /// 切诊 (脉象、腹诊等)
        /// </summary>
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        // 诊断信息
        private string _chiefComplaint = string.Empty;      // 主诉 (必填)
        private string _presentIllness = string.Empty;      // 现病史
        private string _tcmDiagnosis = string.Empty;        // 中医诊断 (必填)
        private string _treatmentPrinciple = string.Empty;  // 治疗原则
        private string _remarks = string.Empty;             // 备注

        /// <summary>
        /// 主诉 (必填)
        /// </summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        /// <summary>
        /// 现病史
        /// </summary>
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        /// <summary>
        /// 中医诊断 (必填)
        /// </summary>
        public string TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set => SetProperty(ref _tcmDiagnosis, value);
        }

        /// <summary>
        /// 治疗原则
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 保存病案命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 清空命令
        /// </summary>
        public DelegateCommand ClearCommand { get; }

        /// <summary>
        /// 开处方命令
        /// </summary>
        public DelegateCommand PrescribeCommand { get; }

        /// <summary>
        /// 导入历史命令
        /// </summary>
        public DelegateCommand ImportHistoryCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseEntryViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave)
                .ObservesProperty(() => ChiefComplaint)
                .ObservesProperty(() => TCMDiagnosis)
                .ObservesProperty(() => CurrentPatient);
            ClearCommand = new DelegateCommand(Clear);
            PrescribeCommand = new DelegateCommand(Prescribe, () => MedicalCaseId.HasValue)
                .ObservesProperty(() => MedicalCaseId);
            ImportHistoryCommand = new DelegateCommand(ImportHistory, () => CurrentPatient != null)
                .ObservesProperty(() => CurrentPatient);
        }

        #endregion

        #region INavigationAware 实现

        /// <summary>
        /// 导航进入时调用
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                // 获取患者信息 (从ClinicalWorkstation传递)
                if (navigationContext.Parameters.ContainsKey("Patient"))
                {
                    CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("Patient");
                    Logger.LogInformation("病案录入界面加载, 患者: {PatientName} (ID: {PatientId})",
                        CurrentPatient.Name, CurrentPatient.Id);
                }

                // 获取医疗案例ID (如果已存在)
                if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
                {
                    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                    Logger.LogInformation("已关联医疗案例ID: {MedicalCaseId}", MedicalCaseId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到病案录入界面时发生异常");
                ShowErrorMessage("初始化失败, 请稍后重试");
            }
        }

        /// <summary>
        /// 是否可以导航离开
        /// </summary>
        public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 导航离开时调用
        /// </summary>
        public override void OnNavigatedFrom(NavigationContext navigationContext) { }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存病案 - Issue #1463: 使用聚合根模式
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存病案记录...");

                // 1. 验证必填字段
                if (!ValidateInput())
                {
                    return;
                }

                // 2. 构造MedicalCase数据（聚合根）
                var medicalCaseDto = new MedicalCaseCreateDto
                {
                    PatientId = CurrentPatient!.Id,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    ChiefComplaint = ChiefComplaint,
                    Remark = $"创建于: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                };

                // 3. 构造Consultation数据（子实体）
                var consultationDto = new ConsultationCreateDto
                {
                    PatientId = CurrentPatient!.Id,
                    UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    PatientName = CurrentPatient.Name,
                    DoctorName = SessionManager?.CurrentUser?.RealName ?? "未知医生",
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness,
                    Inspection = Inspection,
                    AuscultationOlfaction = AuscultationOlfaction,
                    Inquiry = Inquiry,
                    Palpation = Palpation,
                    TCMDiagnosis = TCMDiagnosis,
                    TreatmentPrinciple = TreatmentPrinciple,
                    Remark = Remarks,
                    StartTime = DateTime.Now
                };

                // 4. 使用聚合根方法一次性创建（原子操作）
                var result = await _medicalCaseRepository.CreateWithDetailsAsync(
                    medicalCaseDto,
                    consultationDto,
                    null // 暂无处方
                );

                MedicalCaseId = result.Id;

                Logger.LogInformation("病案记录保存成功, MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
                    result.Id, CurrentPatient.Name);

                await ShowSuccessMessageAsync("病案记录已保存");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存病案记录时发生异常");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            if (CurrentPatient == null)
            {
                ShowErrorMessage("请先选择患者");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                ShowErrorMessage("主诉不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TCMDiagnosis))
            {
                ShowErrorMessage("中医诊断不能为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 清空所有输入
        /// </summary>
        private void Clear()
        {
            Inspection = string.Empty;
            AuscultationOlfaction = string.Empty;
            Inquiry = string.Empty;
            Palpation = string.Empty;
            ChiefComplaint = string.Empty;
            PresentIllness = string.Empty;
            TCMDiagnosis = string.Empty;
            TreatmentPrinciple = string.Empty;
            Remarks = string.Empty;

            Logger.LogInformation("已清空病案录入内容");
        }

        /// <summary>
        /// 开处方 (跳转到PrescriptionView)
        /// </summary>
        private void Prescribe()
        {
            if (!MedicalCaseId.HasValue)
            {
                ShowErrorMessage("请先保存病案记录");
                return;
            }

            try
            {
                // 跳转到处方界面, 传递MedicalCaseId
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", MedicalCaseId.Value },
                    { "PatientName", CurrentPatient?.Name ?? "未知患者" }
                };

                RegionManager.RequestNavigate("ClinicalContentRegion", "PrescriptionView", parameters);

                Logger.LogInformation("跳转到处方界面, MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "跳转到处方界面时发生异常");
                ShowErrorMessage($"跳转失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入历史诊断
        /// </summary>
        private void ImportHistory()
        {
            // TODO: 打开历史诊断选择对话框
            ShowInfoMessage("导入历史诊断功能开发中...");
            Logger.LogInformation("打开历史诊断选择对话框 (功能开发中)");
        }

        #endregion

        #region 命令状态检查

        private bool CanSave()
        {
            return CurrentPatient != null &&
                   !string.IsNullOrWhiteSpace(ChiefComplaint) &&
                   !string.IsNullOrWhiteSpace(TCMDiagnosis) &&
                   !IsBusy;
        }

        #endregion
    }
}
