using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Registrations.Dialogs;

/// <summary>
/// 快速就诊弹窗 ViewModel（B2 US-REG-002: 医生直接开始就诊——
/// 急诊/特殊通道 + 本地模式无前台场景的常规看诊入口）
/// </summary>
public partial class QuickVisitDialogViewModel : DialogViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly IRegistrationService _registrationService;

    [ObservableProperty]
    private string _patientSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PatientListDto> _patientSearchResults = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private PatientListDto? _selectedPatient;

    [ObservableProperty]
    private string? _remark;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showPatientResults;

    [ObservableProperty]
    private bool _isSearchingPatients;

    public QuickVisitDialogViewModel(
        IViewModelServices services,
        IPatientService patientService,
        IRegistrationService registrationService)
        : base(services)
    {
        _patientService = patientService;
        _registrationService = registrationService;
        Title = "快速就诊";
    }

    protected override bool CanConfirm() =>
        SelectedPatient is not null && !IsBusy && !IsLoading;

    protected override void Confirm()
    {
        if (SelectedPatient is null) return;

        ConfirmAsync().SafeFireAndForget(
            ex => Logger.LogError(ex, "[REG-DIALOG] 快速就诊失败"));
    }

    private async Task ConfirmAsync()
    {
        try
        {
            SetBusy(true, "正在开始就诊...");

            var request = new QuickVisitInputDto
            {
                PatientId = SelectedPatient!.Id,
                PatientName = SelectedPatient.Name,
                Remark = Remark
            };

            var result = await _registrationService.QuickVisitAsync(request);
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("[REG-DIALOG] 快速就诊成功: RegistrationId={RegId}, MedicalCaseId={McId}",
                    result.Data.RegistrationId, result.Data.MedicalCaseId);
                var parameters = new DialogParameters
                {
                    { "QuickVisitResult", result.Data }
                };
                CloseDialog(parameters, ButtonResult.OK);
            }
            else
            {
                StatusMessage = result.Error ?? "快速就诊失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-DIALOG] 快速就诊失败");
            StatusMessage = "快速就诊失败，请稍后重试";
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(PatientSearchText))
        {
            PatientSearchResults = [];
            ShowPatientResults = false;
            return;
        }

        try
        {
            IsSearchingPatients = true;
            StatusMessage = "正在搜索患者...";

            var result = await _patientService.SearchAsync(PatientSearchText);
            if (result.Success && result.Data != null)
            {
                PatientSearchResults = new ObservableCollection<PatientListDto>(result.Data);
                ShowPatientResults = PatientSearchResults.Count > 0;
                StatusMessage = PatientSearchResults.Count > 0
                    ? $"找到 {PatientSearchResults.Count} 位患者"
                    : "未找到匹配的患者";
            }
            else
            {
                PatientSearchResults = [];
                ShowPatientResults = false;
                StatusMessage = result.Error ?? "搜索患者失败";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-DIALOG] 搜索患者失败");
            StatusMessage = "搜索失败，请检查网络连接";
            PatientSearchResults = [];
            ShowPatientResults = false;
        }
        finally
        {
            IsSearchingPatients = false;
        }
    }

    [RelayCommand]
    private void SelectPatient(PatientListDto patient)
    {
        SelectedPatient = patient;
        PatientSearchText = patient.Name;
        ShowPatientResults = false;
        PatientSearchResults = [];
        StatusMessage = $"已选择患者: {patient.Name}";
    }

    [RelayCommand]
    private void ClearPatientSelection()
    {
        SelectedPatient = null;
        PatientSearchText = string.Empty;
        PatientSearchResults = [];
        ShowPatientResults = false;
        StatusMessage = string.Empty;
    }

    partial void OnPatientSearchTextChanged(string value)
    {
        if (SelectedPatient is not null && value != SelectedPatient.Name)
        {
            SelectedPatient = null;
            ConfirmCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedPatientChanged(PatientListDto? value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }
}
