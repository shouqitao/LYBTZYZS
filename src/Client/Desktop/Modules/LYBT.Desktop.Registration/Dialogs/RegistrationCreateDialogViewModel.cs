using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Registration.Dialogs;

/// <summary>
/// 创建挂号弹窗 ViewModel -- US-REG-001
/// </summary>
public partial class RegistrationCreateDialogViewModel : DialogViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly IUserService _userService;
    private readonly IRegistrationService _registrationService;

    [ObservableProperty]
    private string _patientSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PatientListDto> _patientSearchResults = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private PatientListDto? _selectedPatient;

    [ObservableProperty]
    private ObservableCollection<UserListDto> _doctorList = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private UserListDto? _selectedDoctor;

    [ObservableProperty]
    private string? _remark;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showPatientResults;

    [ObservableProperty]
    private bool _isSearchingPatients;

    public RegistrationCreateDialogViewModel(
        IViewModelServices services,
        IPatientService patientService,
        IUserService userService,
        IRegistrationService registrationService)
        : base(services)
    {
        _patientService = patientService;
        _userService = userService;
        _registrationService = registrationService;
        Title = "新建挂号";
    }

    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        LoadDoctorsAsync().SafeFireAndForget(
            ex => Logger.LogError(ex, "[REG-DIALOG] 加载医生列表失败"));
    }

    protected override bool CanConfirm() =>
        SelectedPatient is not null
        && SelectedDoctor is not null
        && !IsBusy
        && !IsLoading;

    protected override void Confirm()
    {
        if (SelectedPatient is null || SelectedDoctor is null) return;

        ConfirmAsync().SafeFireAndForget(
            ex => Logger.LogError(ex, "[REG-DIALOG] 创建挂号失败"));
    }

    private async Task ConfirmAsync()
    {
        try
        {
            SetBusy(true, "正在创建挂号...");

            var input = new RegistrationInputDto
            {
                PatientId = SelectedPatient.Id,
                PatientName = SelectedPatient.Name,
                DoctorId = SelectedDoctor.Id,
                DoctorName = SelectedDoctor.RealName,
                Source = RegistrationSource.Receptionist,
                Remark = Remark
            };

            var result = await _registrationService.CreateAsync(input);
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("[REG-DIALOG] 挂号创建成功: RegistrationId={Id}", result.Data.Id);
                var parameters = new DialogParameters
                {
                    { "CreatedRegistration", result.Data }
                };
                CloseDialog(parameters, ButtonResult.OK);
            }
            else
            {
                StatusMessage = result.Error ?? "创建挂号失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-DIALOG] 创建挂号失败");
            StatusMessage = $"创建挂号失败: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(PatientSearchText) || PatientSearchText.Length < 1)
        {
            PatientSearchResults = [];
            ShowPatientResults = false;
            return;
        }

        try
        {
            IsSearchingPatients = true;
            StatusMessage = "正在搜索患者...";

            var result = await _patientService.SearchPatientsAsync(PatientSearchText);
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
        Logger.LogDebug("[REG-DIALOG] 选择患者: {PatientId} - {PatientName}", patient.Id, patient.Name);
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

    partial void OnSelectedDoctorChanged(UserListDto? value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }

    private async Task LoadDoctorsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载医生列表...";

            var result = await _userService.GetDoctorsAsync();
            if (result.Success && result.Data != null)
            {
                var doctors = result.Data.Where(d => d.IsEnabled).ToList();
                DoctorList = new ObservableCollection<UserListDto>(doctors);
                StatusMessage = doctors.Count > 0
                    ? $"共 {doctors.Count} 位医生可选"
                    : "暂无可用医生";
                Logger.LogDebug("[REG-DIALOG] 加载医生列表完成: {Count} 位", doctors.Count);
            }
            else
            {
                StatusMessage = result.Error ?? "加载医生列表失败";
                Logger.LogWarning("[REG-DIALOG] 加载医生列表失败: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-DIALOG] 加载医生列表异常");
            StatusMessage = "加载医生列表失败，请检查网络连接";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
