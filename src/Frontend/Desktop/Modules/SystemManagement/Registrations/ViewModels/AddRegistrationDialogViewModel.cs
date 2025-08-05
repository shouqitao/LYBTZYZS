using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 新增挂号对话框视图模型
    /// </summary>
    public class AddRegistrationDialogViewModel : BindableBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly Window _window;

        #region Properties

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
                if (SetProperty(ref _selectedPatient, value))
                {
                    PatientDetailsVisible = value != null;
                }
            }
        }

        private string _searchPatientText = string.Empty;
        public string SearchPatientText
        {
            get => _searchPatientText;
            set => SetProperty(ref _searchPatientText, value);
        }

        private bool _patientDetailsVisible;
        public bool PatientDetailsVisible
        {
            get => _patientDetailsVisible;
            set => SetProperty(ref _patientDetailsVisible, value);
        }

        private ObservableCollection<string> _departmentList = new();
        public ObservableCollection<string> DepartmentList
        {
            get => _departmentList;
            set => SetProperty(ref _departmentList, value);
        }

        private string _selectedDepartment = string.Empty;
        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                {
                    _ = LoadDoctorsAsync();
                }
            }
        }

        private ObservableCollection<DoctorInfo> _doctors = new();
        public ObservableCollection<DoctorInfo> Doctors
        {
            get => _doctors;
            set => SetProperty(ref _doctors, value);
        }

        private DoctorInfo? _selectedDoctor;
        public DoctorInfo? SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                if (SetProperty(ref _selectedDoctor, value))
                {
                    UpdateRegistrationFee();
                }
            }
        }

        private ObservableCollection<string> _registrationTypeList = new();
        public ObservableCollection<string> RegistrationTypeList
        {
            get => _registrationTypeList;
            set => SetProperty(ref _registrationTypeList, value);
        }

        private string _selectedRegistrationType = "普通号";
        public string SelectedRegistrationType
        {
            get => _selectedRegistrationType;
            set
            {
                if (SetProperty(ref _selectedRegistrationType, value))
                {
                    UpdateRegistrationFee();
                }
            }
        }

        private DateTime _appointmentDate = DateTime.Today;
        public DateTime AppointmentDate
        {
            get => _appointmentDate;
            set => SetProperty(ref _appointmentDate, value);
        }

        private ObservableCollection<string> _timeSlotList = new();
        public ObservableCollection<string> TimeSlotList
        {
            get => _timeSlotList;
            set => SetProperty(ref _timeSlotList, value);
        }

        private string _selectedTimeSlot = "上午";
        public string SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set => SetProperty(ref _selectedTimeSlot, value);
        }

        private decimal _registrationFee = 10;
        public decimal RegistrationFee
        {
            get => _registrationFee;
            set => SetProperty(ref _registrationFee, value);
        }

        private bool _immediatePayment = true;
        public bool ImmediatePayment
        {
            get => _immediatePayment;
            set => SetProperty(ref _immediatePayment, value);
        }

        private string? _remark;
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddPatientCommand { get; }

        #endregion

        public AddRegistrationDialogViewModel(
            IRegistrationService registrationService,
            IPatientService patientService,
            IDoctorService doctorService)
        {
            _registrationService = registrationService;
            _patientService = patientService;
            _doctorService = doctorService;

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
                .ObservesProperty(() => SelectedPatient)
                .ObservesProperty(() => SelectedDepartment)
                .ObservesProperty(() => SelectedDoctor)
                .ObservesProperty(() => AppointmentDate)
                .ObservesProperty(() => SelectedTimeSlot);

            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddPatientCommand = new DelegateCommand(ExecuteAddPatient);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];

            // 初始化数据
            InitializeLists();
            _ = LoadPatientsAsync();
        }

        private void InitializeLists()
        {
            // 初始化科室列表
            DepartmentList.Clear();
            DepartmentList.Add("内科");
            DepartmentList.Add("外科");
            DepartmentList.Add("妇科");
            DepartmentList.Add("儿科");
            DepartmentList.Add("中医科");
            DepartmentList.Add("皮肤科");
            DepartmentList.Add("骨科");
            DepartmentList.Add("眼科");
            DepartmentList.Add("耳鼻喉科");

            // 初始化挂号类型列表
            RegistrationTypeList.Clear();
            RegistrationTypeList.Add("普通号");
            RegistrationTypeList.Add("专家号");
            RegistrationTypeList.Add("急诊号");
            RegistrationTypeList.Add("预约号");

            // 初始化时段列表
            TimeSlotList.Clear();
            TimeSlotList.Add("上午");
            TimeSlotList.Add("下午");
            TimeSlotList.Add("晚上");
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                var patients = await _patientService.GetListAsync();
                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载患者列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDoctorsAsync()
        {
            if (string.IsNullOrEmpty(SelectedDepartment))
            {
                Doctors.Clear();
                return;
            }

            try
            {
                var result = await _doctorService.GetByDepartmentAsync(SelectedDepartment);
                Doctors.Clear();
                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var doctor in result.Data)
                    {
                        Doctors.Add(doctor);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载医生列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRegistrationFee()
        {
            // 根据挂号类型计算挂号费
            RegistrationFee = SelectedRegistrationType switch
            {
                "普通号" => 10,
                "专家号" => 50,
                "急诊号" => 20,
                "预约号" => 15,
                _ => 10
            };

            // 如果选择了专家，增加额外费用
            if (SelectedDoctor != null && (SelectedDoctor.Title == DoctorTitle.ChiefPhysician || SelectedDoctor.Title == DoctorTitle.AssociateChiefPhysician))
            {
                RegistrationFee += 30;
            }
        }

        private bool CanExecuteSave()
        {
            return SelectedPatient != null &&
                   !string.IsNullOrWhiteSpace(SelectedDepartment) &&
                   SelectedDoctor != null &&
                   AppointmentDate != default &&
                   !string.IsNullOrWhiteSpace(SelectedTimeSlot);
        }

        private async void ExecuteSave()
        {
            if (SelectedPatient == null || SelectedDoctor == null) return;

            try
            {
                var registrationType = ConvertToRegistrationType(SelectedRegistrationType);
                
                var dto = new RegistrationCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = SelectedDoctor.Id,
                    Department = SelectedDepartment,
                    RegistrationType = registrationType,
                    RegistrationFee = RegistrationFee,
                    AppointmentDate = AppointmentDate,
                    AppointmentTimeSlot = SelectedTimeSlot,
                    IsPaid = ImmediatePayment,
                    Remark = Remark
                };

                var response = await _registrationService.CreateAsync(dto);
                if (response.IsSuccess)
                {
                    MessageBox.Show("挂号创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _window.DialogResult = true;
                    _window.Close();
                }
                else
                {
                    MessageBox.Show($"创建挂号失败: {response.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建挂号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RegistrationType ConvertToRegistrationType(string typeName)
        {
            return typeName switch
            {
                "普通号" => RegistrationType.Regular,
                "专家号" => RegistrationType.Expert,
                "急诊号" => RegistrationType.Emergency,
                "预约号" => RegistrationType.Appointment,
                _ => RegistrationType.Regular
            };
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }

        private void ExecuteAddPatient()
        {
            // TODO: 实现添加患者功能
            MessageBox.Show("添加患者功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}