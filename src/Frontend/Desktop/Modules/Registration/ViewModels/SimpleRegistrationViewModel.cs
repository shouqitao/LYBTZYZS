using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.WPF.Client.Modules.Registration.ViewModels
{
    /// <summary>
    /// 简化挂号视图模型 - 用于前台快速挂号
    /// </summary>
    public class SimpleRegistrationViewModel : BindableBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly ICommonDialogService _dialogService;

        private bool _isLoading;
        private string _patientSearchKeyword = string.Empty;
        private string _newPatientName = string.Empty;
        private string _newPatientPhone = string.Empty;
        private Gender _newPatientGender = Gender.Male;
        private int _newPatientAge = 30;
        
        private PatientDetailDto? _selectedPatient;
        private DoctorDto? _selectedDoctor;
        private int _queueNumber;
        private string _statusMessage = string.Empty;

        public SimpleRegistrationViewModel(
            IRegistrationService registrationService,
            IPatientService patientService,
            IDoctorService doctorService,
            ICommonDialogService dialogService)
        {
            _registrationService = registrationService;
            _patientService = patientService;
            _doctorService = doctorService;
            _dialogService = dialogService;

            Doctors = new ObservableCollection<DoctorDto>();
            SearchedPatients = new ObservableCollection<PatientDetailDto>();

            InitializeCommands();
            LoadDoctorsAsync();
        }

        #region Properties

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>医生列表</summary>
        public ObservableCollection<DoctorDto> Doctors { get; }

        /// <summary>搜索到的患者列表</summary>
        public ObservableCollection<PatientDetailDto> SearchedPatients { get; }

        /// <summary>患者搜索关键词</summary>
        public string PatientSearchKeyword
        {
            get => _patientSearchKeyword;
            set => SetProperty(ref _patientSearchKeyword, value);
        }

        /// <summary>新患者姓名</summary>
        public string NewPatientName
        {
            get => _newPatientName;
            set => SetProperty(ref _newPatientName, value);
        }

        /// <summary>新患者手机号</summary>
        public string NewPatientPhone
        {
            get => _newPatientPhone;
            set => SetProperty(ref _newPatientPhone, value);
        }

        /// <summary>新患者性别</summary>
        public Gender NewPatientGender
        {
            get => _newPatientGender;
            set => SetProperty(ref _newPatientGender, value);
        }

        /// <summary>新患者年龄</summary>
        public int NewPatientAge
        {
            get => _newPatientAge;
            set => SetProperty(ref _newPatientAge, value);
        }

        /// <summary>选中的患者</summary>
        public PatientDetailDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                SetProperty(ref _selectedPatient, value);
                SubmitRegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>选中的医生</summary>
        public DoctorDto? SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                SetProperty(ref _selectedDoctor, value);
                SubmitRegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>排队号</summary>
        public int QueueNumber
        {
            get => _queueNumber;
            set => SetProperty(ref _queueNumber, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public DelegateCommand? SearchPatientCommand { get; private set; }
        public DelegateCommand? CreatePatientCommand { get; private set; }
        public DelegateCommand<PatientDetailDto>? SelectPatientCommand { get; private set; }
        public DelegateCommand<DoctorDto>? SelectDoctorCommand { get; private set; }
        public DelegateCommand? SubmitRegistrationCommand { get; private set; }
        public DelegateCommand? ClearCommand { get; private set; }

        private void InitializeCommands()
        {
            SearchPatientCommand = new DelegateCommand(async () => await SearchPatientAsync());
            CreatePatientCommand = new DelegateCommand(async () => await CreatePatientAsync());
            SelectPatientCommand = new DelegateCommand<PatientDetailDto>(patient => SelectedPatient = patient);
            SelectDoctorCommand = new DelegateCommand<DoctorDto>(doctor => SelectedDoctor = doctor);
            SubmitRegistrationCommand = new DelegateCommand(
                async () => await SubmitRegistrationAsync(),
                () => SelectedPatient != null && SelectedDoctor != null
            );
            ClearCommand = new DelegateCommand(ClearForm);
        }

        #endregion

        #region Methods

        /// <summary>加载医生列表</summary>
        private async void LoadDoctorsAsync()
        {
            try
            {
                IsLoading = true;
                var doctors = await _doctorService.GetAvailableDoctorsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Doctors.Clear();
                    foreach (var doctor in doctors)
                    {
                        Doctors.Add(doctor);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载医生列表失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>搜索患者</summary>
        private async Task SearchPatientAsync()
        {
            if (string.IsNullOrWhiteSpace(PatientSearchKeyword))
            {
                StatusMessage = "请输入搜索关键词";
                return;
            }

            try
            {
                IsLoading = true;
                var patients = await _patientService.SearchPatientsAsync(PatientSearchKeyword);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchedPatients.Clear();
                    foreach (var patient in patients)
                    {
                        SearchedPatients.Add(patient);
                    }
                    
                    if (patients.Count == 0)
                    {
                        StatusMessage = "未找到患者，请创建新患者档案";
                    }
                    else
                    {
                        StatusMessage = $"找到 {patients.Count} 个患者";
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"搜索失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>创建新患者</summary>
        private async Task CreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPatientName))
            {
                StatusMessage = "请输入患者姓名";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPatientPhone))
            {
                StatusMessage = "请输入手机号";
                return;
            }

            try
            {
                IsLoading = true;
                
                var newPatient = new PatientDetailDto
                {
                    Name = NewPatientName,
                    PhoneNumber = NewPatientPhone,
                    Gender = NewPatientGender,
                    Age = NewPatientAge,
                    IsActive = true
                };

                var createdPatient = await _patientService.CreatePatientAsync(newPatient);
                
                if (createdPatient != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SelectedPatient = createdPatient;
                        StatusMessage = "患者档案创建成功";
                        
                        // 清空新建患者表单
                        NewPatientName = string.Empty;
                        NewPatientPhone = string.Empty;
                        NewPatientAge = 30;
                        NewPatientGender = Gender.Male;
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建患者失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>提交挂号</summary>
        private async Task SubmitRegistrationAsync()
        {
            if (SelectedPatient == null || SelectedDoctor == null)
            {
                StatusMessage = "请选择患者和医生";
                return;
            }

            try
            {
                IsLoading = true;
                
                var registration = new RegistrationCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = SelectedDoctor.Id,
                    RegistrationType = RegistrationType.Normal,
                    RegistrationFee = SelectedDoctor.RegistrationFee,
                    AppointmentDate = DateTime.Today,
                    AppointmentTimeSlot = "全天",
                    IsPaid = false
                };

                var result = await _registrationService.CreateRegistrationAsync(registration);
                
                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        QueueNumber = result.QueueNumber ?? 0;
                        StatusMessage = $"挂号成功！排队号：{QueueNumber}";
                        
                        _dialogService.ShowMessage(
                            $"挂号成功！\n\n患者：{SelectedPatient.Name}\n医生：{SelectedDoctor.Name}\n排队号：{QueueNumber}\n挂号费：￥{SelectedDoctor.RegistrationFee:F2}",
                            "挂号成功"
                        );
                        
                        // 清空表单准备下一个挂号
                        ClearForm();
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"挂号失败：{ex.Message}";
                _dialogService.ShowError($"挂号失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>清空表单</summary>
        private void ClearForm()
        {
            SelectedPatient = null;
            SelectedDoctor = null;
            PatientSearchKeyword = string.Empty;
            NewPatientName = string.Empty;
            NewPatientPhone = string.Empty;
            NewPatientAge = 30;
            NewPatientGender = Gender.Male;
            SearchedPatients.Clear();
            QueueNumber = 0;
            StatusMessage = string.Empty;
        }

        #endregion
    }
}