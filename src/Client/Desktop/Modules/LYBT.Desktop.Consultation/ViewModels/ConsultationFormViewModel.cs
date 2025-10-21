using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
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

        private readonly IConsultationRepository _consultationRepository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;

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

        /// <summary>
        /// 更新MedicalCase的ConsultationId
        /// Issue #1544: 将保存成功的ConsultationId关联到MedicalCase
        /// </summary>
        private async Task UpdateMedicalCaseConsultationIdAsync(Guid consultationId)
        {
            try
            {
                Logger.LogInformation("开始更新MedicalCase.ConsultationId，MedicalCaseId: {MedicalCaseId}, ConsultationId: {ConsultationId}",
                    MedicalCaseId, consultationId);

                // 获取当前医案
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(MedicalCaseId);
                if (medicalCase == null)
                {
                    Logger.LogWarning("未找到医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    return;
                }

                // 构建更新DTO
                var updateDto = new LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseUpdateDto
                {
                    Id = medicalCase.Id,
                    PatientId = medicalCase.PatientId,
                    DoctorId = medicalCase.DoctorId,
                    ConsultationId = consultationId,
                    Remark = medicalCase.Remark
                };

                // 调用更新方法
                await _medicalCaseRepository.UpdateAsync(updateDto);

                Logger.LogInformation("已更新MedicalCase.ConsultationId: {ConsultationId}", consultationId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新MedicalCase.ConsultationId失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                // 不抛出异常，允许Consultation保存成功（后续可通过数据修复）
            }
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存诊断数据（创建Consultation实体）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                Logger.LogInformation("开始保存诊断数据，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                if (MedicalCaseId == Guid.Empty)
                {
                    Logger.LogError("MedicalCaseId为空，无法创建Consultation");
                    ValidationMessage = "医案ID为空，无法保存诊断数据";
                    return false;
                }

                if (CurrentPatient == null)
                {
                    Logger.LogError("CurrentPatient为空，无法创建Consultation");
                    ValidationMessage = "患者信息丢失，无法保存诊断数据";
                    return false;
                }

                if (SessionManager?.CurrentUser == null)
                {
                    Logger.LogError("SessionManager.CurrentUser为空，无法创建Consultation");
                    ValidationMessage = "用户信息丢失，无法保存诊断数据";
                    return false;
                }

                // 构建ConsultationCreateDto
                var createDto = new ConsultationCreateDto
                {
                    MedicalCaseId = MedicalCaseId,
                    PatientId = CurrentPatient.Id,
                    UserId = SessionManager.CurrentUser.Id,
                    StartTime = DateTime.Now,
                    ChiefComplaint = ChiefComplaint.Trim(),
                    PresentIllness = string.IsNullOrWhiteSpace(PresentIllness) ? null : PresentIllness.Trim(),
                    Inspection = string.IsNullOrWhiteSpace(Inspection) ? null : Inspection.Trim(),
                    AuscultationOlfaction = string.IsNullOrWhiteSpace(AuscultationOlfaction) ? null : AuscultationOlfaction.Trim(),
                    Inquiry = string.IsNullOrWhiteSpace(Inquiry) ? null : Inquiry.Trim(),
                    Palpation = string.IsNullOrWhiteSpace(Palpation) ? null : Palpation.Trim(),
                    TCMDiagnosis = TCMDiagnosis.Trim(),
                    TreatmentPrinciple = string.IsNullOrWhiteSpace(TreatmentPrinciple) ? null : TreatmentPrinciple.Trim(),
                    Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim(),
                    PatientName = CurrentPatient.Name,
                    DoctorName = SessionManager.CurrentUser.UserName
                };

                // 调用Repository创建Consultation
                var createdDto = await _consultationRepository.CreateAsync(createDto);

                Logger.LogInformation("诊断数据保存成功，ConsultationId: {ConsultationId}", createdDto.Id);

                // Issue #1544: 更新MedicalCase.ConsultationId
                await UpdateMedicalCaseConsultationIdAsync(createdDto.Id);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断数据失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                ValidationMessage = $"保存失败：{ex.Message}";
                return false;
            }
        }

        #endregion

        #region 命令

        public DelegateCommand ImportFromHistoryCommand { get; }
        public DelegateCommand ClearFormCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationFormViewModel(
            IConsultationRepository consultationRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化命令
            ImportFromHistoryCommand = new DelegateCommand(ExecuteImportFromHistory);
            ClearFormCommand = new DelegateCommand(ExecuteClearForm);

            Logger.LogInformation("ConsultationFormViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 从历史导入（预留功能，后续实现）
        /// </summary>
        private void ExecuteImportFromHistory()
        {
            try
            {
                Logger.LogInformation("执行从历史导入（未实现）");
                // TODO: Task #1502 - 打开历史诊断选择对话框
                Logger.LogWarning("从历史导入功能未实现");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "从历史导入失败");
            }
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ExecuteClearForm()
        {
            try
            {
                Logger.LogInformation("执行清空表单");

                // 清空所有字段
                ChiefComplaint = string.Empty;
                PresentIllness = string.Empty;
                Inspection = string.Empty;
                AuscultationOlfaction = string.Empty;
                Inquiry = string.Empty;
                Palpation = string.Empty;
                TCMDiagnosis = string.Empty;
                TreatmentPrinciple = string.Empty;
                Remark = string.Empty;

                ValidationMessage = string.Empty;

                Logger.LogInformation("表单已清空");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清空表单失败");
            }
        }

        #endregion
    }
}
