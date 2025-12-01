using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 诊断控件ViewModel
/// OpenSpec: controlify-workspace - Phase 2
/// 基于ConsultationPanelViewModel，添加事件发布支持
/// </summary>
public class ConsultationControlViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    #region 字段

    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private Guid _medicalCaseId;
    private bool _isInitialized;

    #endregion

    #region 诊断属性

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
                NotifyDataChanged();
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
        set
        {
            if (SetProperty(ref _presentIllness, value))
            {
                NotifyDataChanged();
            }
        }
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
                NotifyDataChanged();
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
        set
        {
            if (SetProperty(ref _treatmentPrinciple, value))
            {
                NotifyDataChanged();
            }
        }
    }

    private string _inspection = string.Empty;
    /// <summary>
    /// 望诊
    /// </summary>
    public string Inspection
    {
        get => _inspection;
        set
        {
            if (SetProperty(ref _inspection, value))
            {
                NotifyDataChanged();
            }
        }
    }

    private string _auscultationOlfaction = string.Empty;
    /// <summary>
    /// 闻诊
    /// </summary>
    public string AuscultationOlfaction
    {
        get => _auscultationOlfaction;
        set
        {
            if (SetProperty(ref _auscultationOlfaction, value))
            {
                NotifyDataChanged();
            }
        }
    }

    private string _inquiry = string.Empty;
    /// <summary>
    /// 问诊
    /// </summary>
    public string Inquiry
    {
        get => _inquiry;
        set
        {
            if (SetProperty(ref _inquiry, value))
            {
                NotifyDataChanged();
            }
        }
    }

    private string _palpation = string.Empty;
    /// <summary>
    /// 切诊
    /// </summary>
    public string Palpation
    {
        get => _palpation;
        set
        {
            if (SetProperty(ref _palpation, value))
            {
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// 医案备注
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
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// 不开处方（反向绑定）
    /// </summary>
    public bool NoPrescription => !NeedsPrescription;

    private bool _isReadOnly;
    /// <summary>
    /// 是否只读模式
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    private bool _hasUnsavedChanges;
    /// <summary>
    /// 是否有未保存修改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

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

    public ConsultationControlViewModel(
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
    {
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

        SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft, CanExecuteEdit)
            .ObservesProperty(() => IsReadOnly);
        ConfirmConsultationCommand = new DelegateCommand(ExecuteConfirmConsultation, CanExecuteEdit)
            .ObservesProperty(() => IsReadOnly);

        // 订阅保存所有请求事件
        EventAggregator.GetEvent<SaveAllRequestedEvent>()
            .Subscribe(OnSaveAllRequested, ThreadOption.UIThread);

        Logger.LogInformation("ConsultationControlViewModel已初始化");
    }

    private bool CanExecuteEdit() => !IsReadOnly;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化控件（由父ViewModel调用）
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="existingConsultation">现有诊断数据（可选）</param>
    /// <param name="isReadOnly">是否只读</param>
    public void Initialize(Guid medicalCaseId, ConsultationDto? existingConsultation = null, bool isReadOnly = false)
    {
        _medicalCaseId = medicalCaseId;
        IsReadOnly = isReadOnly;

        if (existingConsultation != null)
        {
            LoadFromDto(existingConsultation);
        }

        _isInitialized = true;
        HasUnsavedChanges = false;

        Logger.LogInformation("ConsultationControl初始化完成，MedicalCaseId: {MedicalCaseId}, IsReadOnly: {IsReadOnly}",
            medicalCaseId, isReadOnly);
    }

    private void LoadFromDto(ConsultationDto dto)
    {
        _isInitialized = false; // 临时禁用脏数据追踪

        ChiefComplaint = dto.ChiefComplaint ?? string.Empty;
        PresentIllness = dto.PresentIllness ?? string.Empty;
        TCMDiagnosis = dto.TCMDiagnosis ?? string.Empty;
        TreatmentPrinciple = dto.TreatmentPrinciple ?? string.Empty;
        Inspection = dto.Inspection ?? string.Empty;
        AuscultationOlfaction = dto.AuscultationOlfaction ?? string.Empty;
        Inquiry = dto.Inquiry ?? string.Empty;
        Palpation = dto.Palpation ?? string.Empty;

        _isInitialized = true;
    }

    #endregion

    #region ISaveable

    public async Task<bool> SaveAsync()
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
                HasUnsavedChanges = false;

                // 发布保存完成事件
                EventAggregator.GetEvent<ConsultationSavedEvent>()
                    .Publish(new ConsultationSavedPayload
                    {
                        MedicalCaseId = _medicalCaseId,
                        IsAutoSave = false
                    });

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

    /// <summary>
    /// 静默保存（不显示验证错误对话框）
    /// </summary>
    public async Task<bool> SaveSilentlyAsync()
    {
        try
        {
            if (_medicalCaseId == Guid.Empty)
            {
                Logger.LogWarning("MedicalCaseId为空，跳过保存");
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
                HasUnsavedChanges = false;
                Logger.LogInformation("诊断数据静默保存成功");
                return true;
            }

            Logger.LogWarning("诊断数据静默保存失败");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "静默保存诊断数据异常");
            return false;
        }
    }

    #endregion

    #region 事件处理

    private async void OnSaveAllRequested(Guid medicalCaseId)
    {
        if (medicalCaseId != _medicalCaseId) return;

        await SaveSilentlyAsync();
    }

    private void NotifyDataChanged()
    {
        if (!_isInitialized) return;

        if (!HasUnsavedChanges)
        {
            HasUnsavedChanges = true;

            EventAggregator.GetEvent<ConsultationDataChangedEvent>()
                .Publish(_medicalCaseId);
        }
    }

    #endregion

    #region 命令实现

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

            var saveSuccess = await SaveAsync();
            if (!saveSuccess)
            {
                return;
            }

            // 发布诊断完成事件（使用Infrastructure层定义）
            EventAggregator.GetEvent<Infrastructure.Events.ConsultationCompletedEvent>()
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

    #region 清理

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        EventAggregator.GetEvent<SaveAllRequestedEvent>().Unsubscribe(OnSaveAllRequested);
    }

    #endregion
}
