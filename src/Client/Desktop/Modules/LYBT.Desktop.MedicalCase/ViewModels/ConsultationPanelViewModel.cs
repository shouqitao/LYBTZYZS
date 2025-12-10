using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// Epic #2210 Phase 4: 诊断面板ViewModel
    /// 用于MedicalCaseWorkspaceView的左侧40%区域
    /// 实现IValidatable和IDataProvider接口
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 移除ISaveable，使用IDataProvider
    /// </summary>
    public class ConsultationPanelViewModel : UnifiedViewModelBase, IValidatable, IDataProvider
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private Guid _medicalCaseId;

        #endregion

        #region 诊断属性

        private string _chiefComplaint = string.Empty;
        /// <summary>
        /// 主诉（必填）
        /// </summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
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
            set => SetProperty(ref _tcmDiagnosis, value);
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

        private string _inspection = string.Empty;
        /// <summary>
        /// 望诊
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string _auscultationOlfaction = string.Empty;
        /// <summary>
        /// 闻诊
        /// </summary>
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        private string _inquiry = string.Empty;
        /// <summary>
        /// 问诊
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string _palpation = string.Empty;
        /// <summary>
        /// 切诊
        /// </summary>
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        /// <summary>
        /// 医案备注（保存时传递到服务端更新MedicalCase.Remark）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// </summary>
        public string? MedicalCaseRemark { get; set; }

        private bool _needsPrescription = true;
        /// <summary>
        /// 是否需要开处方
        /// </summary>
        public bool NeedsPrescription
        {
            get => _needsPrescription;
            set
            {
                if (SetProperty(ref _needsPrescription, value))
                {
                    RaisePropertyChanged(nameof(NoPrescription));
                }
            }
        }

        /// <summary>
        /// 不开处方（反向绑定）
        /// </summary>
        public bool NoPrescription => !NeedsPrescription;

        #endregion

        #region IValidatable

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public bool Validate()
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                ValidationMessage = "请填写主诉";
                return false;
            }

            if (string.IsNullOrWhiteSpace(TCMDiagnosis))
            {
                ValidationMessage = "请填写中医诊断";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region 命令

        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand ConfirmConsultationCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationPanelViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
            ConfirmConsultationCommand = new DelegateCommand(ExecuteConfirmConsultation);

            Logger.LogInformation("ConsultationPanelViewModel已初始化");
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化面板（由父ViewModel调用）
        /// </summary>
        public void Initialize(Guid medicalCaseId, ConsultationDto? existingConsultation = null)
        {
            _medicalCaseId = medicalCaseId;

            if (existingConsultation != null)
            {
                LoadFromDto(existingConsultation);
            }

            Logger.LogInformation("ConsultationPanel初始化完成，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        private void LoadFromDto(ConsultationDto dto)
        {
            ChiefComplaint = dto.ChiefComplaint ?? string.Empty;
            PresentIllness = dto.PresentIllness ?? string.Empty;
            TCMDiagnosis = dto.TCMDiagnosis ?? string.Empty;
            TreatmentPrinciple = dto.TreatmentPrinciple ?? string.Empty;
            Inspection = dto.Inspection ?? string.Empty;
            AuscultationOlfaction = dto.AuscultationOlfaction ?? string.Empty;
            Inquiry = dto.Inquiry ?? string.Empty;
            Palpation = dto.Palpation ?? string.Empty;
            // OpenSpec: clarify-cancel-consultation-logic
            // 诊断不需要独立备注，MedicalCaseRemark由父ViewModel在保存前设置
        }

        #endregion

        #region 内部保存（供命令使用）

        /// <summary>
        /// 保存诊断数据到服务器
        /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 内部方法，供命令使用
        /// 注意：主要的保存流程已迁移到聚合保存模式，此方法仅供内部命令使用
        /// </summary>
        private async Task<bool> SaveAsync()
        {
            try
            {
                if (!Validate())
                {
                    await ShowErrorMessageAsync(ValidationMessage);
                    return false;
                }

                var request = new ConsultationInputDto
                {
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness,
                    TCMDiagnosis = TCMDiagnosis,
                    TreatmentPrinciple = TreatmentPrinciple,
                    Inspection = Inspection,
                    AuscultationOlfaction = AuscultationOlfaction,
                    Inquiry = Inquiry,
                    Palpation = Palpation,
                    MedicalCaseRemark = MedicalCaseRemark
                };

                var result = await _medicalCaseRepository.UpdateConsultationAsync(_medicalCaseId, request);

                if (result != null)
                {
                    Logger.LogInformation("诊断数据保存成功");
                    return true;
                }

                Logger.LogWarning("诊断数据保存失败");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊断数据异常");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDataProvider

        /// <summary>
        /// 获取诊断数据（四诊信息）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.2)
        /// </summary>
        /// <returns>诊断数据DTO</returns>
        public ConsultationInputDto? GetConsultationData()
        {
            return new ConsultationInputDto
            {
                ChiefComplaint = ChiefComplaint,
                PresentIllness = PresentIllness,
                TCMDiagnosis = TCMDiagnosis,
                TreatmentPrinciple = TreatmentPrinciple,
                Inspection = Inspection,
                AuscultationOlfaction = AuscultationOlfaction,
                Inquiry = Inquiry,
                Palpation = Palpation,
                MedicalCaseRemark = MedicalCaseRemark
            };
        }

        /// <summary>
        /// 获取处方数据
        /// ConsultationPanel不提供处方数据，返回null
        /// </summary>
        /// <returns>null（处方数据由PrescriptionPanel提供）</returns>
        public PrescriptionAggregateDto? GetPrescriptionData() => null;

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存草稿
        /// </summary>
        private async void ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                var success = await SaveAsync();

                if (success)
                {
                    await ShowSuccessMessageAsync("诊断草稿已保存");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿失败");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 确认诊断（触发处方面板启用）
        /// </summary>
        private async void ExecuteConfirmConsultation()
        {
            try
            {
                if (!Validate())
                {
                    await ShowErrorMessageAsync(ValidationMessage);
                    return;
                }

                SetIsBusy(true, "正在确认诊断...");

                // 保存诊断数据
                var saveSuccess = await SaveAsync();
                if (!saveSuccess)
                {
                    return;
                }

                // 发布诊断完成事件
                EventAggregator.GetEvent<ConsultationCompletedEvent>()
                    .Publish(new ConsultationCompletedPayload
                    {
                        MedicalCaseId = _medicalCaseId,
                        NeedsPrescription = NeedsPrescription
                    });

                await ShowSuccessMessageAsync("诊断已确认");
                Logger.LogInformation("诊断确认完成，NeedsPrescription: {NeedsPrescription}", NeedsPrescription);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "确认诊断失败");
                await ShowErrorMessageAsync($"确认失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
