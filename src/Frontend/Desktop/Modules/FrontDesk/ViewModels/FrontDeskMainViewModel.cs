using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Records;

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

        public FrontDeskMainViewModel(IPatientService patientService, IRecordService recordService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _patientService = patientService;
            _recordService = recordService;
            InitializeCommands();
            _ = LoadTodayRegistrations();
        }

        #region Properties

        private ObservableCollection<PatientDetailDto> _todayRegistrations = new();
        public ObservableCollection<PatientDetailDto> TodayRegistrations
        {
            get => _todayRegistrations;
            set => SetProperty(ref _todayRegistrations, value);
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
            set => SetProperty(ref _selectedPatient, value);
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string TodayRegistrationCount => $"今日挂号: {TodayRegistrations.Count}人";
        public string WaitingQueueCount => $"等待就诊: {WaitingQueue.Count}人";

        #endregion

        #region Commands

        public DelegateCommand SearchPatientCommand { get; private set; }
        public DelegateCommand RegisterNewPatientCommand { get; private set; }
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
            AddToQueueCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await AddToQueue(patient));
            RemoveFromQueueCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await RemoveFromQueue(patient));
            ViewPatientInfoCommand = new DelegateCommand<PatientDetailDto>(ViewPatientInfo);
            ViewPatientHistoryCommand = new DelegateCommand<PatientDetailDto>(async (patient) => await ViewPatientHistory(patient));
            RefreshCommand = new DelegateCommand(async () => await RefreshData());
            ClearNewPatientFormCommand = new DelegateCommand(ClearNewPatientForm);
        }

        #endregion

        #region Command Implementations

        private async Task LoadTodayRegistrations()
        {
            try
            {
                IsLoading = true;
                var result = await _patientService.GetActivePatientsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    TodayRegistrations.Clear();
                    foreach (var patient in result.Data.Take(10)) // 显示最近的10个患者
                    {
                        TodayRegistrations.Add(patient);
                    }
                }
                RaisePropertyChanged(nameof(TodayRegistrationCount));
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载今日挂号信息失败：{ex.Message}", "错误").GetAwaiter().GetResult();
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
                await _commonDialogService.ShowWarningAsync("请输入搜索关键词", "提示");
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _patientService.QuickSearchAsync(SearchKeyword);
                if (result.IsSuccess && result.Data != null)
                {
                    TodayRegistrations.Clear();
                    foreach (var patient in result.Data)
                    {
                        TodayRegistrations.Add(patient);
                    }
                    _commonDialogService.ShowInformationAsync($"找到 {result.Data.Count} 个患者", "搜索结果").GetAwaiter().GetResult();
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
                    
                    TodayRegistrations.Insert(0, newPatient);
                    ClearNewPatientForm();
                    RaisePropertyChanged(nameof(TodayRegistrationCount));
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
            await LoadTodayRegistrations();
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