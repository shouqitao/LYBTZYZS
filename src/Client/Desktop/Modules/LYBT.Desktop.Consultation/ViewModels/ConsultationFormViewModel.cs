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

        // Issue #1563: 保存时使用聚合根Repository
        private readonly IMedicalCaseRepository _medicalCaseRepository;

        // Issue #1570: 加载时使用ConsultationRepository
        private readonly IConsultationRepository _consultationRepository;

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

        // Issue #1562 Phase 1: 已删除工作流事件发布逻辑（PublishConsultationCompletedEvent）

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

                // Issue #1563: 构建ConsultationUpdateDto（使用聚合根方法）
                // ConsultationUpdateDto只包含诊断信息字段，不需要PatientId/UserId等关联字段
                // 这些信息已经在MedicalCase聚合根中，Server端通过MedicalCaseId获取
                var updateDto = new ConsultationUpdateDto
                {
                    Id = MedicalCaseId, // Consultation使用与MedicalCase相同的ID（共享主键）
                    ChiefComplaint = ChiefComplaint.Trim(),
                    PresentIllness = string.IsNullOrWhiteSpace(PresentIllness) ? null : PresentIllness.Trim(),
                    Inspection = string.IsNullOrWhiteSpace(Inspection) ? null : Inspection.Trim(),
                    AuscultationOlfaction = string.IsNullOrWhiteSpace(AuscultationOlfaction) ? null : AuscultationOlfaction.Trim(),
                    Inquiry = string.IsNullOrWhiteSpace(Inquiry) ? null : Inquiry.Trim(),
                    Palpation = string.IsNullOrWhiteSpace(Palpation) ? null : Palpation.Trim(),
                    TCMDiagnosis = TCMDiagnosis.Trim(),
                    TreatmentPrinciple = string.IsNullOrWhiteSpace(TreatmentPrinciple) ? null : TreatmentPrinciple.Trim(),
                    Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim()
                };

                // Issue #1563: 使用聚合根Repository方法更新Consultation
                var updatedDto = await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, updateDto);

                Logger.LogInformation("诊断数据保存成功，ConsultationId: {ConsultationId}", updatedDto.Id);

                // Issue #1562 Phase 1: 已删除工作流事件发布调用

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

        public DelegateCommand ClearFormCommand { get; }

        // Issue #1562 Phase 1: 已删除ImportFromHistoryCommand（未实现的扩展功能）

        #endregion

        #region 构造函数

        public ConsultationFormViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IConsultationRepository consultationRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1563: 只注入聚合根Repository
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));

            // 初始化命令
            ClearFormCommand = new DelegateCommand(ExecuteClearForm);

            Logger.LogInformation("ConsultationFormViewModel已初始化");
        }

        #endregion

        #region 命令实现

        // Issue #1562 Phase 1: 已删除ExecuteImportFromHistory（未实现的扩展功能）

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

        #region INavigationAware

        /// <summary>
        /// 导航到当前视图时调用
        /// Issue #1557 Phase 3: 接收MedicalCaseFlowViewModel传来的MedicalCaseId和CurrentPatient
        /// Issue #1570: 继续看诊时加载现有Consultation数据
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
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
                }

                // 接收CurrentPatient参数
                var currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");
                if (currentPatient != null)
                {
                    Logger.LogInformation("接收到CurrentPatient: {PatientName}", currentPatient.Name);
                    CurrentPatient = currentPatient;
                }

                // Issue #1570: 加载现有Consultation数据（如果存在）
                if (medicalCaseId != Guid.Empty)
                {
                    _ = LoadExistingConsultationAsync(medicalCaseId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到ConsultationFormView时发生异常");
            }
        }

        /// <summary>
        /// 加载现有Consultation数据
        /// Issue #1570: 继续看诊时从后端加载旧数据
        /// </summary>
        private async Task LoadExistingConsultationAsync(Guid medicalCaseId)
        {
            try
            {
                Logger.LogInformation("尝试加载现有Consultation，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                SetIsBusy(true, "正在加载诊断数据...");

                // Issue #1570: 使用ConsultationRepository.GetByMedicalCaseIdAsync获取Consultation
                // Consultation与MedicalCase共享主键，所以可以通过MedicalCaseId查询
                var consultations = await _consultationRepository.GetByMedicalCaseIdAsync(medicalCaseId);
                var consultation = consultations.FirstOrDefault();

                if (consultation != null)
                {
                    // 填充表单数据
                    ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
                    PresentIllness = consultation.PresentIllness ?? string.Empty;
                    Inspection = consultation.Inspection ?? string.Empty;
                    AuscultationOlfaction = consultation.AuscultationOlfaction ?? string.Empty;
                    Inquiry = consultation.Inquiry ?? string.Empty;
                    Palpation = consultation.Palpation ?? string.Empty;
                    TCMDiagnosis = consultation.TCMDiagnosis ?? string.Empty;
                    TreatmentPrinciple = consultation.TreatmentPrinciple ?? string.Empty;
                    Remark = consultation.Remark ?? string.Empty;

                    Logger.LogInformation("Consultation数据加载成功，主诉: {ChiefComplaint}", ChiefComplaint);
                }
                else
                {
                    Logger.LogInformation("MedicalCase无关联Consultation，保持表单空白");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载Consultation数据失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                // 不显示错误提示，允许用户继续填写新数据
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
