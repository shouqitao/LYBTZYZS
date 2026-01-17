using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 工作区状态聚合对象
/// OpenSpec: slim-workspace-viewmodel - State对象模式
/// OpenSpec: simplify-workspace-architecture - 合并StatusDisplay功能
/// 聚合UI状态属性，减少ViewModel属性数量
/// </summary>
public partial class WorkspaceState : ObservableObject
{
    #region 忙碌状态

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    /// <summary>
    /// 设置忙碌状态
    /// </summary>
    public void SetBusy(bool busy, string? message = null)
    {
        IsBusy = busy;
        BusyMessage = busy ? message : null;
    }

    #endregion

    #region 患者信息

    [ObservableProperty]
    private string _patientName = string.Empty;

    [ObservableProperty]
    private string _patientInfo = string.Empty;

    [ObservableProperty]
    private string _patientGenderDisplay = "未知";

    [ObservableProperty]
    private int _patientAge;

    [ObservableProperty]
    private string? _patientPhone;

    [ObservableProperty]
    private int _patientVisitCount;

    [ObservableProperty]
    private DateTime? _registrationTime;

    /// <summary>
    /// 患者显示模型 - 用于PatientInfoCardControl绑定
    /// </summary>
    public PatientDisplayModel? PatientDisplayModel => string.IsNullOrEmpty(PatientName)
        ? null
        : new PatientDisplayModel
        {
            Name = PatientName,
            Gender = PatientGenderDisplay,
            Age = PatientAge,
            PhoneNumber = PatientPhone,
            VisitCount = PatientVisitCount,
            RegistrationTime = RegistrationTime
        };

    /// <summary>
    /// 从PatientDetailDto更新患者信息
    /// </summary>
    public void UpdateFromPatient(PatientDetailDto? patient)
    {
        if (patient == null)
        {
            PatientName = string.Empty;
            PatientInfo = string.Empty;
            PatientGenderDisplay = "未知";
            PatientAge = 0;
            PatientPhone = null;
            PatientVisitCount = 0;
            RegistrationTime = null;
            OnPropertyChanged(nameof(PatientDisplayModel));
            return;
        }

        PatientName = patient.Name ?? string.Empty;
        PatientGenderDisplay = patient.Gender switch
        {
            Gender.Male => "男",
            Gender.Female => "女",
            _ => "未知"
        };
        PatientAge = patient.Age ?? 0;
        PatientPhone = patient.PhoneNumber;
        PatientVisitCount = patient.VisitCount;
        RegistrationTime = patient.CreatedAt;

        // 更新PatientInfo（组合显示）
        PatientInfo = $"{PatientName} ({PatientGenderDisplay}, {PatientAge}岁)";

        OnPropertyChanged(nameof(PatientDisplayModel));
    }

    #endregion

    #region 待诊队列状态

    [ObservableProperty]
    private bool _isRefreshingPendingQueue;

    #endregion

    #region 完成状态

    [ObservableProperty]
    private bool _canPrintPrescription;

    [ObservableProperty]
    private bool _canComplete;

    /// <summary>
    /// 更新完成状态
    /// </summary>
    public void UpdateCanComplete(bool consultationValid, bool prescriptionValid, bool needsPrescription, bool isEditing)
    {
        // 诊断必须完成
        if (!consultationValid)
        {
            CanComplete = false;
            return;
        }

        // 如果需要处方，处方必须有效
        if (needsPrescription && !prescriptionValid)
        {
            CanComplete = false;
            return;
        }

        // 必须在编辑模式
        CanComplete = isEditing;
    }

    /// <summary>
    /// 更新打印状态
    /// </summary>
    public void UpdateCanPrint(bool hasValidPrescription)
    {
        CanPrintPrescription = hasValidPrescription;
    }

    #endregion

    #region 导航状态

    [ObservableProperty]
    private bool _isFromManagement;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    #endregion

    #region 处方控制

    [ObservableProperty]
    private bool _isPrescriptionEnabled;

    [ObservableProperty]
    private bool _needsPrescription = true;

    /// <summary>
    /// 不开处方（反向绑定）
    /// </summary>
    public bool NoPrescription => !NeedsPrescription;

    partial void OnNeedsPrescriptionChanged(bool value)
    {
        OnPropertyChanged(nameof(NoPrescription));
    }

    #endregion

    #region 重置

    /// <summary>
    /// 重置所有状态
    /// </summary>
    public void Reset()
    {
        IsBusy = false;
        BusyMessage = null;
        PatientName = string.Empty;
        PatientInfo = string.Empty;
        PatientGenderDisplay = "未知";
        PatientAge = 0;
        PatientPhone = null;
        PatientVisitCount = 0;
        RegistrationTime = null;
        IsRefreshingPendingQueue = false;
        CanPrintPrescription = false;
        CanComplete = false;
        IsFromManagement = false;
        HasUnsavedChanges = false;
        IsPrescriptionEnabled = false;
        NeedsPrescription = true;

        OnPropertyChanged(nameof(PatientDisplayModel));
    }

    #endregion
}
