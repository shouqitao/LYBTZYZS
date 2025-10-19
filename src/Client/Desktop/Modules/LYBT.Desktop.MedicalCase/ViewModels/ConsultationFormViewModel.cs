using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// Step 2 - 诊断表单ViewModel（Epic #1494 - Task #1498）
    /// 基于现有MedicalCaseEntryViewModel（#1463），专为4步流程设计
    /// </summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase, ISaveable, IValidatable
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly ICommonDialogService _dialogService;
        private Guid _medicalCaseId = Guid.Empty;

        #endregion

        #region 基本诊断信息属性

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
                    RaisePropertyChanged(nameof(ValidationMessage));
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
                    RaisePropertyChanged(nameof(ValidationMessage));
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

        #region 四诊合参属性

        private string _inspection = string.Empty;
        /// <summary>
        /// 望诊（神色、形体、舌象等）
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string _auscultationOlfaction = string.Empty;
        /// <summary>
        /// 闻诊（语声、呼吸、咳嗽、口气等）
        /// </summary>
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        private string _inquiry = string.Empty;
        /// <summary>
        /// 问诊（主诉、现病史、既往史等）
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string _palpation = string.Empty;
        /// <summary>
        /// 切诊（脉象、腹诊等）
        /// </summary>
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        #endregion

        #region 备注

        private string _remarks = string.Empty;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        #endregion

        #region IValidatable实现

        /// <summary>
        /// 验证是否满足进入下一步的条件
        /// </summary>
        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(ChiefComplaint) &&
                   !string.IsNullOrWhiteSpace(TCMDiagnosis);
        }

        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ChiefComplaint))
                    return "主诉不能为空";
                if (string.IsNullOrWhiteSpace(TCMDiagnosis))
                    return "中医诊断不能为空";
                return string.Empty;
            }
        }

        #endregion

        #region 命令

        public DelegateCommand ClearCommand { get; }
        public DelegateCommand ImportHistoryCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationFormViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            ICommonDialogService dialogService,
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
            ClearCommand = new DelegateCommand(ExecuteClear);
            ImportHistoryCommand = new DelegateCommand(async () => await ExecuteImportHistoryAsync());

            Logger.LogInformation("ConsultationFormViewModel已初始化");
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存诊断数据（创建Consultation并关联到MedicalCase）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存诊断信息...");

                Logger.LogInformation("开始保存诊断信息，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

                // 验证必填字段
                if (!Validate())
                {
                    Logger.LogWarning("诊断信息验证失败：{Message}", ValidationMessage);
                    await ShowErrorMessageAsync(ValidationMessage);
                    return false;
                }

                // 构造Consultation创建DTO
                var consultationDto = new ConsultationCreateDto
                {
                    PatientId = Guid.Empty, // TODO: 从MedicalCase获取PatientId
                    UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    PatientName = "未知患者", // TODO: 从MedicalCase获取PatientName
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

                // 调用Repository创建Consultation
                // TODO: Task #1498实现后，使用真实API创建Consultation并关联到MedicalCase
                // var created = await _consultationRepository.CreateAsync(consultationDto);
                // await _medicalCaseRepository.UpdateConsultationIdAsync(_medicalCaseId, created.Id);

                // 临时模拟：保存成功
                await Task.Delay(500); // 模拟网络延迟
                Logger.LogInformation("诊断信息保存成功（模拟），MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断信息失败，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
                return false;
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置MedicalCaseId（由MedicalCaseFlowViewModel调用）
        /// </summary>
        public void SetMedicalCaseId(Guid medicalCaseId)
        {
            _medicalCaseId = medicalCaseId;
            Logger.LogInformation("设置MedicalCaseId: {MedicalCaseId}", _medicalCaseId);
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ExecuteClear()
        {
            try
            {
                Logger.LogInformation("清空诊断表单");

                ChiefComplaint = string.Empty;
                PresentIllness = string.Empty;
                TCMDiagnosis = string.Empty;
                TreatmentPrinciple = string.Empty;
                Inspection = string.Empty;
                AuscultationOlfaction = string.Empty;
                Inquiry = string.Empty;
                Palpation = string.Empty;
                Remarks = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清空表单时发生异常");
            }
        }

        /// <summary>
        /// 从历史医案导入诊断信息
        /// </summary>
        private async Task ExecuteImportHistoryAsync()
        {
            try
            {
                Logger.LogInformation("打开历史医案导入对话框");

                // TODO: Task #1498实现后，打开历史医案选择对话框
                // var result = await _dialogService.ShowDialogAsync("HistoryConsultationDialog", ...);
                // if (result.Success)
                // {
                //     var history = result.Data as ConsultationDto;
                //     ChiefComplaint = history.ChiefComplaint;
                //     // ... 其他字段
                // }

                await _dialogService.ShowWarningAsync("从历史导入功能待实现（需要创建历史医案选择对话框）");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入历史医案失败");
                await ShowErrorMessageAsync($"导入失败：{ex.Message}");
            }
        }

        #endregion
    }
}
