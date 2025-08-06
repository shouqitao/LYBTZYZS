using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 编辑挂号对话框视图模型
    /// </summary>
    public class EditRegistrationDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IRegistrationService _registrationService;
        private readonly IDoctorService _doctorService;
        private readonly Window _window;
        private Guid _registrationId;

        #region Properties

        private RegistrationInfo? _registration;
        public RegistrationInfo? Registration
        {
            get => _registration;
            set => SetProperty(ref _registration, value);
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

        private string _selectedRegistrationType = string.Empty;
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

        private DateTime? _appointmentDate;
        public DateTime? AppointmentDate
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

        private string _selectedTimeSlot = string.Empty;
        public string SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set => SetProperty(ref _selectedTimeSlot, value);
        }

        private decimal _registrationFee;
        public decimal RegistrationFee
        {
            get => _registrationFee;
            set => SetProperty(ref _registrationFee, value);
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

        #endregion

        public EditRegistrationDialogViewModel(IRegistrationService registrationService,
            IDoctorService doctorService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _registrationService = registrationService;
            _doctorService = doctorService;

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
                .ObservesProperty(() => SelectedDepartment)
                .ObservesProperty(() => SelectedDoctor)
                .ObservesProperty(() => AppointmentDate)
                .ObservesProperty(() => SelectedTimeSlot);

            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];

            // 初始化数据
            InitializeLists();
        }

        public async void Initialize(Guid registrationId)
        {
            _registrationId = registrationId;
            await LoadRegistrationData();
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

        private async Task LoadRegistrationData()
        {
            try
            {
                var registration = await _registrationService.GetByIdAsync(_registrationId);
                if (registration != null)
                {
                    Registration = registration;
                    
                    // 设置当前值
                    SelectedDepartment = Registration.Department ?? string.Empty;
                    AppointmentDate = Registration.AppointmentDate;
                    SelectedTimeSlot = Registration.AppointmentTimeSlot ?? "上午";
                    SelectedRegistrationType = Registration.RegistrationTypeName ?? "普通号";
                    RegistrationFee = Registration.RegistrationFee;
                    Remark = Registration.Remark;

                    // 加载医生列表并设置当前医生
                    await LoadDoctorsAsync();
                    if (Registration.DoctorId != Guid.Empty)
                    {
                        SelectedDoctor = Doctors.FirstOrDefault(d => d.Id == Registration.DoctorId);
                    }
                }
                else
                {
                    _commonDialogService.ShowErrorAsync("未找到挂号信息", "错误").GetAwaiter().GetResult();
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载挂号信息失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                _window.Close();
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
                _commonDialogService.ShowErrorAsync($"加载医生列表失败: {ex.Message}", "错误").GetAwaiter().GetResult();
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
            if (SelectedDoctor != null && (SelectedDoctor/* .Title = */= DoctorTitle.ChiefPhysician || SelectedDoctor/* .Title = */= DoctorTitle.AssociateChiefPhysician))
            {
                RegistrationFee += 30;
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(SelectedDepartment) &&
                   SelectedDoctor != null &&
                   AppointmentDate.HasValue &&
                   !string.IsNullOrWhiteSpace(SelectedTimeSlot);
        }

        private async void ExecuteSave()
        {
            if (SelectedDoctor == null || !AppointmentDate.HasValue) return;

            try
            {
                var registrationType = ConvertToRegistrationType(SelectedRegistrationType);
                
                var dto = new RegistrationEditDto
                {
                    Id = _registrationId,
                    DoctorId = SelectedDoctor?.Id ?? throw new InvalidOperationException("未选中医生"),
                    RegistrationType = registrationType,
                    Remark = Remark ?? string.Empty
                };

                var response = await _registrationService.UpdateAsync(dto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("挂号更新成功", "成功").GetAwaiter().GetResult();
                    _window.DialogResult = true;
                    _window.Close();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"更新挂号失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"更新挂号失败: {ex.Message}", "错误").GetAwaiter().GetResult();
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
    }
}