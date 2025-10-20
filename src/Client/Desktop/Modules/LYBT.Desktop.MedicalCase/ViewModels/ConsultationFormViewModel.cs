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

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 诊断表单ViewModel - Task #1498 Step 2实现
    /// 复用MedicalCaseEntryViewModel逻辑，集成到MedicalCaseFlowView流程
    /// Epic #1494: 医案流程UI重构
    /// </summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;

        #endregion

        #region 数据属性

        // 患者信息（从MedicalCaseFlowViewModel传递）
        private PatientDto? _currentPatient;
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        // 医疗案例ID（从MedicalCaseFlowViewModel传递）
        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        // 基本诊断信息
        private string _chiefComplaint = string.Empty;
        private string _presentIllness = string.Empty;
        private string _tcmDiagnosis = string.Empty;
        private string _treatmentPrinciple = string.Empty;

        /// <summary>
        /// 主诉（必填）
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
        /// 中医诊断（必填）
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

        // 四诊合参
        private string _inspection = string.Empty;
        private string _auscultationOlfaction = string.Empty;
        private string _inquiry = string.Empty;
        private string _palpation = string.Empty;

        /// <summary>
        /// 望诊（神色、形体、舌象等）
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        /// <summary>
        /// 闻诊（语声、呼吸、咳嗽、口气等）
        /// </summary>
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        /// <summary>
        /// 问诊（主诉、现病史、既往史等）
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        /// <summary>
        /// 切诊（脉象、腹诊等）
        /// </summary>
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        // 备注
        private string _remarks = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        // IValidatable实现
        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 清空表单命令
        /// </summary>
        public DelegateCommand ClearFormCommand { get; }

        /// <summary>
        /// 从历史导入命令
        /// </summary>
        public DelegateCommand ImportFromHistoryCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationFormViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化命令
            ClearFormCommand = new DelegateCommand(ExecuteClearForm);
            ImportFromHistoryCommand = new DelegateCommand(ExecuteImportFromHistory, CanExecuteImportFromHistory)
                .ObservesProperty(() => CurrentPatient);

            Logger.LogInformation("ConsultationFormViewModel已初始化");
        }

        #endregion

        #region IValidatable实现

        /// <summary>
        /// 验证必填字段（主诉、中医诊断）
        /// </summary>
        public bool Validate()
        {
            if (CurrentPatient == null)
            {
                ValidationMessage = "请先选择患者";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                ValidationMessage = "主诉不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(TCMDiagnosis))
            {
                ValidationMessage = "中医诊断不能为空";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存诊断信息 - Task #1463: 使用聚合根模式
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存诊断信息...");

                // 1. 验证必填字段
                if (!Validate())
                {
                    Logger.LogWarning("诊断信息验证失败：{Message}", ValidationMessage);
                    return false;
                }

                // 2. 构造Consultation数据
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

                // 3. 如果MedicalCase已存在，调用聚合根方法
                if (MedicalCaseId != Guid.Empty)
                {
                    // 方式1：更新现有MedicalCase关联Consultation
                    // TODO: Task #1463实现后，使用真实API
                    // await _medicalCaseRepository.AddConsultationAsync(MedicalCaseId, consultationDto);
                    Logger.LogInformation("诊断信息已保存（模拟），MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                }
                else
                {
                    // 方式2：一次性创建MedicalCase和Consultation
                    var medicalCaseDto = new MedicalCaseCreateDto
                    {
                        PatientId = CurrentPatient!.Id,
                        DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                        ChiefComplaint = ChiefComplaint,
                        Remark = $"创建于: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    };

                    var result = await _medicalCaseRepository.CreateWithDetailsAsync(
                        medicalCaseDto,
                        consultationDto,
                        null // 暂无处方
                    );

                    MedicalCaseId = result.Id;
                    Logger.LogInformation("诊断信息已保存，MedicalCaseId: {MedicalCaseId}", result.Id);
                }

                await ShowSuccessMessageAsync("诊断信息已保存");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断信息时发生异常");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
                return false;
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ExecuteClearForm()
        {
            try
            {
                ChiefComplaint = string.Empty;
                PresentIllness = string.Empty;
                TCMDiagnosis = string.Empty;
                TreatmentPrinciple = string.Empty;
                Inspection = string.Empty;
                AuscultationOlfaction = string.Empty;
                Inquiry = string.Empty;
                Palpation = string.Empty;
                Remarks = string.Empty;

                Logger.LogInformation("已清空诊断表单");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清空表单时发生异常");
            }
        }

        /// <summary>
        /// 从历史导入
        /// </summary>
        private void ExecuteImportFromHistory()
        {
            try
            {
                // TODO: Task #1502 - 打开患者历史诊断选择对话框
                Logger.LogInformation("打开历史诊断选择对话框（功能开发中）");
                ShowInfoMessage("从历史导入功能开发中...");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "从历史导入时发生异常");
            }
        }

        private bool CanExecuteImportFromHistory()
        {
            return CurrentPatient != null;
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // 获取患者信息（从MedicalCaseFlowViewModel传递）
                if (navigationContext.Parameters.ContainsKey("Patient"))
                {
                    CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("Patient");
                    Logger.LogInformation("接收到患者信息：{PatientName} (ID: {PatientId})",
                        CurrentPatient.Name, CurrentPatient.Id);
                }

                // 获取MedicalCaseId（从MedicalCaseFlowViewModel传递）
                if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
                {
                    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                    Logger.LogInformation("接收到MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到诊断表单时发生异常");
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public override void OnNavigatedFrom(NavigationContext navigationContext) { }

        #endregion
    }
}
