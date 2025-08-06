using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.WPF.Client.Core.Models.Registration;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Records;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.FrontDesk.ViewModels
{
    /// <summary>
    /// 前台接待主界面视图模型
    /// </summary>
    public class FrontDeskMainViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IPatientService _patientService;
        private readonly IRecordService _recordService;
        private readonly IRegistrationService _registrationService;
        private readonly IDoctorService _doctorService;

        public FrontDeskMainViewModel(IPatientService patientService, IRecordService recordService,
            IRegistrationService registrationService, IDoctorService doctorService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _patientService = patientService;
            _recordService = recordService;
            _registrationService = registrationService;
            _doctorService = doctorService;
            InitializeCommands();
            _ = LoadInitialData();
        }

        #region Properties

        // 患者搜索结果列表
        private ObservableCollection<PatientDetailDto> _searchResults = new();
        public ObservableCollection<PatientDetailDto> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        // 今日挂号列表
        private ObservableCollection<LYBT.WPF.Client.Core.Models.Registration.RegistrationInfo> _todayRegistrations = new();
        public ObservableCollection<LYBT.WPF.Client.Core.Models.Registration.RegistrationInfo> TodayRegistrations
        {
            get => _todayRegistrations;
            set => SetProperty(ref _todayRegistrations, value);
        }

        // 可用医生列表
        private ObservableCollection<DoctorInfo> _availableDoctors = new();
        public ObservableCollection<DoctorInfo> AvailableDoctors
        {
            get => _availableDoctors;
            set => SetProperty(ref _availableDoctors, value);
        }

        private ObservableCollection<PatientDetailDto> _waitingQueue = new();
        public ObservableCollection<PatientDetailDto> WaitingQueue
        {
            get => _waitingQueue;
            set => SetProperty(ref _waitingQueue, value);
        }

        private PatientDetailDto _selectedPatient;
        public PatientDetailDto SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    // 选中患者后自动填充挂号信息
                    if (value != null)
                    {
                        RegistrationPatientName = value.Name;
                        RegistrationPatientId = value.Id;
                    }
                }
            }
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        // 新患者注册信息
        private string _newPatientName = string.Empty;
        public string NewPatientName
        {
            get => _newPatientName;
            set => SetProperty(ref _newPatientName, value);
        }

        private string _newPatientPhone = string.Empty;
        public string NewPatientPhone
        {
            get => _newPatientPhone;
            set => SetProperty(ref _newPatientPhone, value);
        }

        private int _newPatientGender = 0;
        public int NewPatientGender
        {
            get => _newPatientGender;
            set => SetProperty(ref _newPatientGender, value);
        }

        private int _newPatientAge;
        public int NewPatientAge
        {
            get => _newPatientAge;
            set => SetProperty(ref _newPatientAge, value);
        }

        private string _newPatientAddress = string.Empty;
        public string NewPatientAddress
        {
            get => _newPatientAddress;
            set => SetProperty(ref _newPatientAddress, value);
        }

        private string _newPatientIDNumber = string.Empty;
        public string NewPatientIDNumber
        {
            get => _newPatientIDNumber;
            set => SetProperty(ref _newPatientIDNumber, value);
        }

        // 挂号相关属性
        private Guid _registrationPatientId;
        public Guid RegistrationPatientId
        {
            get => _registrationPatientId;
            set => SetProperty(ref _registrationPatientId, value);
        }

        private string _registrationPatientName = string.Empty;
        public string RegistrationPatientName
        {
            get => _registrationPatientName;
            set => SetProperty(ref _registrationPatientName, value);
        }

        private DoctorInfo _selectedDoctor;
        public DoctorInfo SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetProperty(ref _selectedDoctor, value);
        }

        private string _registrationRemark = string.Empty;
        public string RegistrationRemark
        {
            get => _registrationRemark;
            set => SetProperty(ref _registrationRemark, value);
        }

        private bool _showCreatePatientPanel;
        public bool ShowCreatePatientPanel
        {
            get => _showCreatePatientPanel;
            set => SetProperty(ref _showCreatePatientPanel, value);
        }

        private bool _showRegistrationPanel;
        public bool ShowRegistrationPanel
        {
            get => _showRegistrationPanel;
            set => SetProperty(ref _showRegistrationPanel, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string TodayRegistrationCount => $"今日挂号: {TodayRegistrations?.Count ?? 0}人";
        public string WaitingQueueCount => $"等待就诊: {WaitingQueue?.Count ?? 0}人";
        public string SearchResultsCount => $"搜索结果: {SearchResults?.Count ?? 0}人";

        #endregion

        #region Commands

        public DelegateCommand SearchPatientCommand { get; private set; }
        public DelegateCommand RegisterNewPatientCommand { get; private set; }
        public DelegateCommand<PatientDetailDto> SelectPatientForRegistrationCommand { get; private set; }
        public DelegateCommand CreateRegistrationCommand { get; private set; }
        public DelegateCommand ShowCreatePatientCommand { get; private set; }
        public DelegateCommand CancelCreatePatientCommand { get; private set; }
        public DelegateCommand<PatientDetailDto> AddToQueueCommand { get; private set; }
        public DelegateCommand<PatientDetailDto> RemoveFromQueueCommand { get; private set; }
        public DelegateCommand<PatientDetailDto> ViewPatientInfoCommand { get; private set; }
        public DelegateCommand<PatientDetailDto> ViewPatientHistoryCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
        public DelegateCommand ClearNewPatientFormCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchPatientCommand = new DelegateCommand(async () => await SearchPatient());
            RegisterNewPatientCommand = new DelegateCommand(async () => await RegisterNewPatient());
            SelectPatientForRegistrationCommand = new DelegateCommand<PatientDetailDto>(SelectPatientForRegistration);
            CreateRegistrationCommand = new DelegateCommand(async () => await CreateRegistration());
            ShowCreatePatientCommand = new DelegateCommand(() => ShowCreatePatientPanel = true);
            CancelCreatePatientCommand = new DelegateCommand(() => { ShowCreatePatientPanel = false; ClearNewPatientForm(); });
            AddToQueueCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await AddToQueue(patient));
            RemoveFromQueueCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await RemoveFromQueue(patient));
            ViewPatientInfoCommand = new DelegateCommand<PatientDetailDto>(ViewPatientInfo);
            ViewPatientHistoryCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await ViewPatientHistory(patient));
            RefreshCommand = new DelegateCommand(async () => await RefreshData());
            ClearNewPatientFormCommand = new DelegateCommand(ClearNewPatientForm);
        }

        #endregion

        #region Command Implementations

        private async Task LoadInitialData()
        {
            await LoadAvailableDoctors();
            await LoadTodayRegistrations();
        }

        private async Task LoadAvailableDoctors()
        {
            try
            {
                var result = await _doctorService.GetDoctorsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableDoctors.Clear();
                    foreach (var doctor in result.Data.Where(d => d.IsActive))
                    {
                        AvailableDoctors.Add(doctor);
                    }
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载医生列表失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async Task LoadTodayRegistrations()
        {
            try
            {
                IsLoading = true;
                // 获取今日的挂号记录
                var today = DateTime.Today;
                var result = await _registrationService.GetPagedAsync(1, 100, null, today, today.AddDays(1));
                if (result != null && result.Items != null)
                {
                    TodayRegistrations.Clear();
                    foreach (var registration in result.Items)
                    {
                        TodayRegistrations.Add(registration);
                    }
                }
                RaisePropertyChanged(nameof(TodayRegistrationCount));
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载今日挂号信息失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchPatient()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                await _commonDialogService.ShowWarningAsync("请输入搜索关键词（姓名、电话或身份证）", "提示");
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _patientService.QuickSearchAsync(SearchKeyword);
                if (result.IsSuccess && result.Data != null)
                {
                    SearchResults.Clear();
                    foreach (var patient in result.Data)
                    {
                        SearchResults.Add(patient);
                    }
                    
                    if (result.Data.Count == 0)
                    {
                        var createNew = await _commonDialogService.ShowConfirmationAsync(
                            "未找到匹配的患者，是否创建新患者？", "提示");
                        if (createNew)
                        {
                            ShowCreatePatientPanel = true;
                            NewPatientName = SearchKeyword; // 预填充搜索的名字
                        }
                    }
                    else
                    {
                        _commonDialogService.ShowInformationAsync($"找到 {result.Data.Count} 个患者，请选择或创建新患者", "搜索结果").GetAwaiter().GetResult();
                    }
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"搜索失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"搜索患者时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RegisterNewPatient()
        {
            if (string.IsNullOrWhiteSpace(NewPatientName) || string.IsNullOrWhiteSpace(NewPatientPhone))
            {
                await _commonDialogService.ShowWarningAsync("请填写患者姓名和手机号", "提示");
                return;
            }

            try
            {
                IsLoading = true;
                var newPatient = new PatientDetailDto
                {
                    Id = Guid.NewGuid(),
                    Name = NewPatientName,
                    PhoneNumber = NewPatientPhone,
                    Gender = (LYBT.Shared.Models.Enums.Gender)NewPatientGender,
                    Age = NewPatientAge,
                    Address = NewPatientAddress,
                    IDNumber = NewPatientIDNumber,
                    CreateTime = DateTime.Now,
                    IsActive = true
                };

                var result = await _patientService.AddAsync(newPatient);
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("患者注册成功！", "成功").GetAwaiter().GetResult();
                    
                    // 自动选中新创建的患者准备挂号
                    SelectedPatient = newPatient;
                    SearchResults.Insert(0, newPatient);
                    ShowCreatePatientPanel = false;
                    ShowRegistrationPanel = true;
                    ClearNewPatientForm();
                    RaisePropertyChanged(nameof(SearchResultsCount));
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"患者注册失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"注册患者时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddToQueue(PatientDetailDto patient)
        {
            if (patient == null) return;

            if (WaitingQueue.Any(p => p.Id == patient.Id))
            {
                await _commonDialogService.ShowWarningAsync("该患者已在等待队列中", "提示");
                return;
            }

            WaitingQueue.Add(patient);
            RaisePropertyChanged(nameof(WaitingQueueCount));
            await _commonDialogService.ShowInformationAsync($"患者 {patient.Name} 已加入等待队列", "成功");
            await Task.CompletedTask;
        }

        private async Task RemoveFromQueue(PatientDetailDto patient)
        {
            if (patient == null) return;

            var patientInQueue = WaitingQueue.FirstOrDefault(p => p.Id == patient.Id);
            if (patientInQueue != null)
            {
                WaitingQueue.Remove(patientInQueue);
                RaisePropertyChanged(nameof(WaitingQueueCount));
                await _commonDialogService.ShowInformationAsync($"患者 {patient.Name} 已从等待队列中移除", "成功");
            }
            await Task.CompletedTask;
        }

        private void ViewPatientInfo(PatientDetailDto patient)
        {
            if (patient == null) return;

            var info = $"患者信息：\n" +
                      $"姓名：{patient.Name}\n" +
                      $"性别：{GetGenderText((int)patient.Gender)}\n" +
                      $"年龄：{patient.Age}\n" +
                      $"电话：{patient.PhoneNumber}\n" +
                      $"地址：{patient.Address}\n" +
                      $"身份证：{patient.IDNumber}";

            _commonDialogService.ShowInformationAsync(info, "患者详细信息").GetAwaiter().GetResult();
        }

        private async Task ViewPatientHistory(PatientDetailDto patient)
        {
            if (patient == null) return;

            try
            {
                IsLoading = true;
                var result = await _patientService.GetHistoryRecordsAsync(patient.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    var history = $"患者 {patient.Name} 的就诊历史：\n\n";
                    if (result.Data.Count == 0)
                    {
                        history += "暂无就诊记录";
                    }
                    else
                    {
                        foreach (var record in result.Data.Take(5))
                        {
                            history += $"就诊时间：{record.RecordTime:yyyy-MM-dd HH:mm}\n";
                            history += $"诊断：{record.Diagnosis}\n\n";
                        }
                    }

                    _commonDialogService.ShowInformationAsync(history, "就诊历史").GetAwaiter().GetResult();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"获取就诊历史失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"获取就诊历史时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshData()
        {
            await LoadInitialData();
        }

        // 选择患者进行挂号
        private void SelectPatientForRegistration(PatientDetailDto patient)
        {
            if (patient == null) return;
            
            SelectedPatient = patient;
            ShowRegistrationPanel = true;
        }

        // 创建挂号
        private async Task CreateRegistration()
        {
            if (SelectedPatient == null)
            {
                await _commonDialogService.ShowWarningAsync("请先选择患者", "提示");
                return;
            }

            if (SelectedDoctor == null)
            {
                await _commonDialogService.ShowWarningAsync("请选择就诊医生", "提示");
                return;
            }

            try
            {
                IsLoading = true;
                
                var registration = new RegistrationCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = SelectedDoctor.Id,
                    Department = SelectedDoctor.Department ?? "中医科",
                    RegistrationType = RegistrationType.Regular,
                    RegistrationFee = 20.00m, // 默认挂号费
                    AppointmentDate = DateTime.Today,
                    AppointmentTimeSlot = DateTime.Now.ToString("HH:mm") + "-" + DateTime.Now.AddMinutes(30).ToString("HH:mm"),
                    IsPaid = false,
                    Remark = RegistrationRemark
                };

                var result = await _registrationService.CreateAsync(registration);
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync($"挂号成功！\n患者：{SelectedPatient.Name}\n医生：{SelectedDoctor.Name}", "成功").GetAwaiter().GetResult();
                    
                    // 清空选择并刷新列表
                    SelectedPatient = null;
                    SelectedDoctor = null;
                    RegistrationRemark = string.Empty;
                    ShowRegistrationPanel = false;
                    
                    await LoadTodayRegistrations();
                    RaisePropertyChanged(nameof(TodayRegistrationCount));
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"挂号失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"挂号时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearNewPatientForm()
        {
            NewPatientName = string.Empty;
            NewPatientPhone = string.Empty;
            NewPatientGender = 0;
            NewPatientAge = 0;
            NewPatientAddress = string.Empty;
            NewPatientIDNumber = string.Empty;
        }

        private string GetGenderText(int gender)
        {
            return gender switch
            {
                1 => "男",
                2 => "女",
                _ => "未知"
            };
        }

        #endregion
    }
}