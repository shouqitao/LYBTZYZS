using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.ViewModels;
using Prism.Commands;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using Prism.Events;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 创建医疗案例对话框视图模型
    /// </summary>
    public class CreateMedicalCaseViewModel : BaseViewModel // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDialogService _dialogService;

        #region Properties

        private string _title = "新建医疗案例";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ObservableCollection<PatientInfo> _patients = new();
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                SetProperty(ref _selectedPatient, value);
                if (value != null)
                {
                    PatientName = value.Name;
                    PatientPhone = value.PhoneNumber ?? "";
                    PatientGender = value.Gender.ToString();
                    PatientAge = CalculateAge(value.BirthDate);
                }
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private string _patientSearchKeyword = "";
        public string PatientSearchKeyword
        {
            get => _patientSearchKeyword;
            set => SetProperty(ref _patientSearchKeyword, value);
        }

        private string _patientName = "";
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientPhone = "";
        public string PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        private string _patientGender = "";
        public string PatientGender
        {
            get => _patientGender;
            set => SetProperty(ref _patientGender, value);
        }

        private int? _patientAge;
        public int? PatientAge
        {
            get => _patientAge;
            set => SetProperty(ref _patientAge, value);
        }

        private string _remark = "";
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand SearchPatientCommand { get; }
        public DelegateCommand CreateNewPatientCommand { get; }

        #endregion

        public CreateMedicalCaseViewModel(
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _medicalCaseService = medicalCaseService;
            _patientService = patientService;
            _userSessionManager = userSessionManager;
            _dialogService = dialogService;

            // Initialize commands
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);
            SearchPatientCommand = new DelegateCommand(async () => await SearchPatientAsync());
            CreateNewPatientCommand = new DelegateCommand(async () => await CreateNewPatientAsync());

            // Load initial patients
            Task.Run(async () => await LoadPatientsAsync());
        }

        #region Dialog Implementation (Temporary - Waiting for Prism 9 IDialogAware fix)

        public event Action<IDialogResult> RequestClose = delegate { };

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            // Cleanup resources
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("PatientId"))
            {
                var patientId = parameters.GetValue<Guid>("PatientId");
                Task.Run(async () => await LoadPatientByIdAsync(patientId));
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "加载患者列表...";

                // Get active patients
                var result = await _patientService.GetActivePatientsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Patients.Clear();
                        foreach (var patientDetail in result.Data)
                        {
                            // Convert PatientDetailDto to PatientInfo
                            var patientInfo = new PatientInfo
                            {
                                Id = patientDetail.Id,
                                Name = patientDetail.Name,
                                Phone = patientDetail.PhoneNumber,
                                Gender = patientDetail.Gender,
                                BirthDate = patientDetail.DateOfBirth
                            };
                            Patients.Add(patientInfo);
                        }
                    });
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"加载患者列表失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载患者列表时发生错误: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = "";
            }
        }

        private async Task LoadPatientByIdAsync(Guid patientId)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Convert PatientDetailDto to PatientInfo
                        var patientInfo = new PatientInfo
                        {
                            Id = result.Data.Id,
                            Name = result.Data.Name,
                            Phone = result.Data.PhoneNumber,
                            Gender = result.Data.Gender,
                            BirthDate = result.Data.DateOfBirth
                        };
                        SelectedPatient = patientInfo;
                    });
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载患者信息失败: {ex.Message}", "错误");
            }
        }

        private async Task SearchPatientAsync()
        {
            if (string.IsNullOrWhiteSpace(PatientSearchKeyword))
            {
                await LoadPatientsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "搜索患者...";

                var result = await _patientService.SearchAsync(PatientSearchKeyword);
                if (result.IsSuccess && result.Data != null)
                {
                    Patients.Clear();
                    foreach (var patientDetail in result.Data)
                    {
                        // Convert PatientDetailDto to PatientInfo
                        var patientInfo = new PatientInfo
                        {
                            Id = patientDetail.Id,
                            Name = patientDetail.Name,
                            Phone = patientDetail.PhoneNumber,
                            Gender = patientDetail.Gender,
                            BirthDate = patientDetail.DateOfBirth
                        };
                        Patients.Add(patientInfo);
                    }
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"搜索患者失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"搜索患者时发生错误: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = "";
            }
        }

        private async Task CreateNewPatientAsync()
        {
            // TODO: Implement patient creation dialog integration
            await _dialogService.ShowInformationAsync("新增患者功能将在患者模块中实现", "提示");
        }

        private bool CanSave()
        {
            return SelectedPatient != null && !IsLoading;
        }

        private async Task SaveAsync()
        {
            if (SelectedPatient == null)
            {
                await _dialogService.ShowErrorAsync("请选择患者", "验证失败");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "创建医疗案例...";

                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                    Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim()
                };

                var result = await _medicalCaseService.CreateAsync(createDto);
                if (result.IsSuccess)
                {
                    await _dialogService.ShowSuccessAsync("医疗案例创建成功", "操作完成");
                    // RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                    // {
                    //     { "CreatedMedicalCase", result.Data }
                    // })); // Temporarily disabled - DialogResult constructor issue
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"创建失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"创建医疗案例时发生错误: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = "";
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private static int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            
            return age;
        }

        #endregion
    }
}