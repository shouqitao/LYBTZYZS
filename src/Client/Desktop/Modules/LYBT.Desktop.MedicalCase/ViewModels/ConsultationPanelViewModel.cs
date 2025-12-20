using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
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
        // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        // 移除: ChiefComplaint (主诉), FourDiagnosis (四诊), TreatmentPrinciple (治疗原则), MedicalCaseRemark (备注)
        // 保留: PresentIllness (现病史), TongueDiagnosis (舌诊), PulseDiagnosis (脉诊), TCMDiagnosis (中医诊断-必填)

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

        private string _tongueDiagnosis = string.Empty;
        /// <summary>
        /// 舌诊
        /// </summary>
        public string TongueDiagnosis
        {
            get => _tongueDiagnosis;
            set => SetProperty(ref _tongueDiagnosis, value);
        }

        private string _pulseDiagnosis = string.Empty;
        /// <summary>
        /// 脉诊
        /// </summary>
        public string PulseDiagnosis
        {
            get => _pulseDiagnosis;
            set => SetProperty(ref _pulseDiagnosis, value);
        }

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
            // OpenSpec: refactor-diagnosis-fields - 只有TCMDiagnosis是必填字段
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
        public void Initialize(Guid medicalCaseId, ConsultationDetailDto? existingConsultation = null)
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
        /// OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        /// </summary>
        private void LoadFromDto(ConsultationDetailDto dto)
        {
            PresentIllness = dto.PresentIllness ?? string.Empty;
            TCMDiagnosis = dto.TCMDiagnosis ?? string.Empty;
            TongueDiagnosis = dto.TongueDiagnosis ?? string.Empty;
            PulseDiagnosis = dto.PulseDiagnosis ?? string.Empty;
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

                // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
                // OpenSpec: simplify-medicalcase-api - 通过聚合保存处理诊断更新
                var consultationInput = new ConsultationInputDto
                {
                    PresentIllness = PresentIllness,
                    TCMDiagnosis = TCMDiagnosis,
                    TongueDiagnosis = TongueDiagnosis,
                    PulseDiagnosis = PulseDiagnosis
                };

                var medicalCaseInput = new MedicalCaseInputDto
                {
                    Id = _medicalCaseId,
                    Consultation = consultationInput
                };

                var result = await _medicalCaseRepository.SaveAsync(_medicalCaseId, medicalCaseInput);

                if (result?.Consultation != null)
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
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
                return false;
            }
        }

        #endregion

        #region IDataProvider

        /// <summary>
        /// 获取诊断数据
        /// OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        /// </summary>
        /// <returns>诊断数据DTO</returns>
        public ConsultationInputDto? GetConsultationData()
        {
            return new ConsultationInputDto
            {
                PresentIllness = PresentIllness,
                TCMDiagnosis = TCMDiagnosis,
                TongueDiagnosis = TongueDiagnosis,
                PulseDiagnosis = PulseDiagnosis
            };
        }

        /// <summary>
        /// 获取处方数据
        /// ConsultationPanel不提供处方数据，返回null
        /// </summary>
        /// <returns>null（处方数据由PrescriptionPanel提供）</returns>
        public PrescriptionInputDto? GetPrescriptionData() => null;

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
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
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
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("确认诊断", ex));
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
