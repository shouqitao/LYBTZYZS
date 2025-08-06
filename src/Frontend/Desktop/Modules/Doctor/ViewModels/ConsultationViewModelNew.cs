using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
// using Prism.Services.Dialogs; // 暂时注释掉，使用简单的对话框服务
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.Doctor.ViewModels
{
    /// <summary>
    /// 看诊界面视图模型（新版）
    /// </summary>
    public class ConsultationViewModelNew : BindableBase, INotifyPropertyChanged
    {
        private readonly IConsultationApiService _consultationApiService;
        private readonly IPatientService _patientService;
        private readonly IHerbService _herbService;
        private readonly ICommonDialogService _commonDialogService;
        private readonly IPrescriptionPrintService _prescriptionPrintService;

        public ConsultationViewModelNew(
            IConsultationApiService consultationApiService,
            IPatientService patientService,
            IHerbService herbService,
            ICommonDialogService commonDialogService,
            IPrescriptionPrintService prescriptionPrintService)
        {
            _consultationApiService = consultationApiService;
            _patientService = patientService;
            _herbService = herbService;
            _commonDialogService = commonDialogService;
            _prescriptionPrintService = prescriptionPrintService;

            InitializeCommands();
            _ = InitializeDataAsync();
        }

        #region Properties - 基本信息

        private Guid _currentConsultationId;
        public Guid CurrentConsultationId
        {
            get => _currentConsultationId;
            set => SetProperty(ref _currentConsultationId, value);
        }

        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private PatientInfo _currentPatient = new();
        public PatientInfo CurrentPatient
        {
            get => _currentPatient;
            set
            {
                SetProperty(ref _currentPatient, value);
                RaisePropertyChanged(nameof(PatientDisplayInfo));
            }
        }

        private string _doctorName = "当前医生";
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value);
        }

        private DateTime _consultationTime = DateTime.Now;
        public DateTime ConsultationTime
        {
            get => _consultationTime;
            set => SetProperty(ref _consultationTime, value);
        }

        public string PatientDisplayInfo
        {
            get
            {
                if (CurrentPatient == null) return "未选择患者";
                var age = GetPatientAge(CurrentPatient.BirthDate);
                var gender = CurrentPatient.Gender == Gender.Male ? "男" : CurrentPatient.Gender == Gender.Female ? "女" : "未知";
                return $"{CurrentPatient.Name} | {gender} | {age}岁 | {CurrentPatient.PhoneNumber}";
            }
        }

        #endregion

        #region Properties - 病史采集

        private string _chiefComplaint = string.Empty;
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        private string _presentIllness = string.Empty;
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        private string _pastHistory = string.Empty;
        public string PastHistory
        {
            get => _pastHistory;
            set => SetProperty(ref _pastHistory, value);
        }

        private string _allergyHistory = string.Empty;
        public string AllergyHistory
        {
            get => _allergyHistory;
            set => SetProperty(ref _allergyHistory, value);
        }

        private string _physicalExamination = string.Empty;
        public string PhysicalExamination
        {
            get => _physicalExamination;
            set => SetProperty(ref _physicalExamination, value);
        }

        #endregion

        #region Properties - 中医四诊

        private string _inspection = string.Empty;
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string _auscultationOlfaction = string.Empty;
        public string AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        private string _inquiry = string.Empty;
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string _palpation = string.Empty;
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        private string _tongueInspection = string.Empty;
        public string TongueInspection
        {
            get => _tongueInspection;
            set => SetProperty(ref _tongueInspection, value);
        }

        private string _pulseCondition = string.Empty;
        public string PulseCondition
        {
            get => _pulseCondition;
            set => SetProperty(ref _pulseCondition, value);
        }

        #endregion

        #region Properties - 生命体征

        private decimal? _temperature;
        public decimal? Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        private int? _systolicPressure;
        public int? SystolicPressure
        {
            get => _systolicPressure;
            set
            {
                SetProperty(ref _systolicPressure, value);
                RaisePropertyChanged(nameof(BloodPressureDisplay));
            }
        }

        private int? _diastolicPressure;
        public int? DiastolicPressure
        {
            get => _diastolicPressure;
            set
            {
                SetProperty(ref _diastolicPressure, value);
                RaisePropertyChanged(nameof(BloodPressureDisplay));
            }
        }

        private int? _heartRate;
        public int? HeartRate
        {
            get => _heartRate;
            set => SetProperty(ref _heartRate, value);
        }

        private int? _respiratoryRate;
        public int? RespiratoryRate
        {
            get => _respiratoryRate;
            set => SetProperty(ref _respiratoryRate, value);
        }

        public string BloodPressureDisplay
        {
            get
            {
                if (SystolicPressure.HasValue && DiastolicPressure.HasValue)
                {
                    return $"{SystolicPressure}/{DiastolicPressure} mmHg";
                }
                return "未测量";
            }
        }

        #endregion

        #region Properties - 诊断

        private string _tcmDiagnosis = string.Empty;
        public string TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set => SetProperty(ref _tcmDiagnosis, value);
        }

        private string _westernDiagnosis = string.Empty;
        public string WesternDiagnosis
        {
            get => _westernDiagnosis;
            set => SetProperty(ref _westernDiagnosis, value);
        }

        private string _diagnosis = string.Empty;
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        private string _treatmentPrinciple = string.Empty;
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        private string _medicalAdvice = string.Empty;
        public string MedicalAdvice
        {
            get => _medicalAdvice;
            set => SetProperty(ref _medicalAdvice, value);
        }

        #endregion

        #region Properties - UI状态

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "准备就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        #endregion

        #region Properties - 队列管理

        private ObservableCollection<ConsultationQueueItem> _waitingQueue = new();
        public ObservableCollection<ConsultationQueueItem> WaitingQueue
        {
            get => _waitingQueue;
            set => SetProperty(ref _waitingQueue, value);
        }

        private ConsultationQueueItem _selectedQueueItem;
        public ConsultationQueueItem SelectedQueueItem
        {
            get => _selectedQueueItem;
            set => SetProperty(ref _selectedQueueItem, value);
        }

        #endregion

        #region Commands

        public DelegateCommand LoadPatientCommand { get; private set; }
        public DelegateCommand StartConsultationCommand { get; private set; }
        public DelegateCommand SaveProgressCommand { get; private set; }
        public DelegateCommand CompleteConsultationCommand { get; private set; }
        public DelegateCommand OpenPrescriptionCommand { get; private set; }
        public DelegateCommand NextTabCommand { get; private set; }
        public DelegateCommand PreviousTabCommand { get; private set; }
        public DelegateCommand RefreshQueueCommand { get; private set; }
        public DelegateCommand<ConsultationQueueItem> CallPatientCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            LoadPatientCommand = new DelegateCommand(async () => await LoadPatientAsync());
            StartConsultationCommand = new DelegateCommand(async () => await StartConsultationAsync(), CanStartConsultation)
                .ObservesProperty(() => SelectedQueueItem);
            SaveProgressCommand = new DelegateCommand(async () => await SaveProgressAsync(), CanSaveProgress)
                .ObservesProperty(() => CurrentConsultationId);
            CompleteConsultationCommand = new DelegateCommand(async () => await CompleteConsultationAsync(), CanCompleteConsultation)
                .ObservesProperty(() => Diagnosis);
            OpenPrescriptionCommand = new DelegateCommand(OpenPrescriptionDialog);
            NextTabCommand = new DelegateCommand(NextTab);
            PreviousTabCommand = new DelegateCommand(PreviousTab);
            RefreshQueueCommand = new DelegateCommand(async () => await RefreshQueueAsync());
            CallPatientCommand = new DelegateCommand<ConsultationQueueItem>(async (item) => await CallPatientAsync(item));
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载数据...";

                // 加载今日排队列表
                await RefreshQueueAsync();

                // 获取当前医生信息
                // TODO: 从登录信息获取
                DoctorName = "张医生";

                StatusMessage = "准备就绪";
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"初始化失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Command Implementations

        private async Task LoadPatientAsync()
        {
            try
            {
                // TODO: 打开患者选择对话框
                await _commonDialogService.ShowInformationAsync("患者选择功能待实现", "提示");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载患者信息失败：{ex.Message}", "错误");
            }
        }

        private async Task StartConsultationAsync()
        {
            try
            {
                if (SelectedQueueItem == null)
                {
                    await _commonDialogService.ShowWarningAsync("请先选择要看诊的患者", "提示");
                    return;
                }

                IsLoading = true;
                StatusMessage = "正在开始看诊...";

                var startDto = new ConsultationStartDto
                {
                    MedicalCaseId = SelectedQueueItem.MedicalCaseId,
                    PatientId = SelectedQueueItem.PatientId,
                    DoctorId = Guid.NewGuid(), // TODO: 从登录信息获取
                    RegistrationId = SelectedQueueItem.RegistrationId
                };

                var response = await _consultationApiService.StartConsultationAsync(startDto);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var result = response.Content;
                    CurrentConsultationId = result.Id;
                    MedicalCaseId = result.MedicalCaseId;
                    
                    // 加载患者信息
                    var patientResult = await _patientService.GetByIdAsync(result.PatientId);
                    if (patientResult.IsSuccess && patientResult.Data != null)
                    {
                        // 转换为 PatientInfo
                        CurrentPatient = new PatientInfo
                        {
                            Id = patientResult.Data.Id,
                            Name = patientResult.Data.Name ?? "",
                            Gender = patientResult.Data.Gender,
                            Age = patientResult.Data.Age,
                            BirthDate = patientResult.Data.BirthDate,
                            PhoneNumber = patientResult.Data.PhoneNumber ?? "",
                            Address = patientResult.Data.Address ?? "",
                            IdType = "",  // patientResult.Data.IDType 字段不存在
                            IdNumber = patientResult.Data.IDNumber ?? "",
                            Occupation = "" ?? "",
                            MaritalStatus = "未婚", // patientResult.Data.MaritalStatus 字段已移除
                            Ethnicity = "汉族", // patientResult.Data.Ethnicity 字段已移除
                            Education = "不详", // patientResult.Data.Education 字段已移除
                            AllergyHistory = patientResult.Data.AllergyHistory ?? "",
                            PinYinCode = patientResult.Data.PinYinCode ?? "",
                            EmergencyContact = "", // PatientDetailDto 没有这个字段，设为空
                            EmergencyPhone = "", // PatientDetailDto 没有这个字段，设为空
                            CreateTime = patientResult.Data.CreateTime,
                            UpdateTime = patientResult.Data.UpdateTime,
                            IsActive = patientResult.Data.IsActive
                        };
                    }

                    // 加载已有的看诊信息
                    LoadConsultationData(result);

                    StatusMessage = "看诊已开始";
                    SelectedTabIndex = 1; // 切换到病史采集页

                    // 从队列中移除
                    WaitingQueue.Remove(SelectedQueueItem);
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"开始看诊失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveProgressAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存...";

                var updateDto = new ConsultationUpdateDto
                {
                    ChiefComplaint = ChiefComplaint,
                    PresentIllness = PresentIllness,
                    PastHistory = PastHistory,
                    AllergyHistory = AllergyHistory,
                    PhysicalExamination = PhysicalExamination,
                    Inspection = Inspection,
                    AuscultationOlfaction = AuscultationOlfaction,
                    Inquiry = Inquiry,
                    Palpation = Palpation,
                    TongueInspection = TongueInspection,
                    PulseCondition = PulseCondition,
                    Temperature = Temperature,
                    SystolicPressure = SystolicPressure,
                    DiastolicPressure = DiastolicPressure,
                    HeartRate = HeartRate,
                    RespiratoryRate = RespiratoryRate,
                    TCMDiagnosis = TCMDiagnosis,
                    WesternDiagnosis = WesternDiagnosis,
                    Diagnosis = Diagnosis,
                    TreatmentPrinciple = TreatmentPrinciple,
                    MedicalAdvice = MedicalAdvice
                };

                var result = await _consultationApiService.UpdateConsultationAsync(CurrentConsultationId, updateDto);
                if (result != null)
                {
                    StatusMessage = "保存成功";
                    await _commonDialogService.ShowInformationAsync("看诊信息已保存", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CompleteConsultationAsync()
        {
            try
            {
                var confirm = await _commonDialogService.ShowConfirmationAsync(
                    "确定要完成本次看诊吗？完成后将无法修改。",
                    "确认完成");

                if (!confirm) return;

                IsLoading = true;
                StatusMessage = "正在完成看诊...";

                var completeDto = new ConsultationCompleteDto
                {
                    Diagnosis = Diagnosis,
                    TCMDiagnosis = TCMDiagnosis,
                    WesternDiagnosis = WesternDiagnosis,
                    TreatmentPrinciple = TreatmentPrinciple,
                    MedicalAdvice = MedicalAdvice,
                    NeedFollowUp = false // TODO: 从界面获取
                };

                var response = await _consultationApiService.CompleteConsultationAsync(CurrentConsultationId, completeDto);
                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "看诊已完成";
                    await _commonDialogService.ShowInformationAsync("看诊已完成", "成功");
                    
                    // 重置界面
                    ResetConsultation();
                    
                    // 刷新队列
                    await RefreshQueueAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"完成看诊失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenPrescriptionDialog()
        {
            // TODO: 打开处方开具对话框
            _commonDialogService.ShowInformationAsync("处方开具功能待实现", "提示");
        }

        private async Task RefreshQueueAsync()
        {
            try
            {
                IsLoading = true;
                // TODO: 从服务器获取今日排队列表
                
                // 模拟数据
                WaitingQueue.Clear();
                WaitingQueue.Add(new ConsultationQueueItem
                {
                    QueueNumber = 1,
                    PatientName = "张三",
                    PatientId = Guid.NewGuid(),
                    MedicalCaseId = Guid.NewGuid(),
                    RegistrationId = Guid.NewGuid(),
                    RegistrationType = "普通号",
                    Status = "等待中"
                });
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"刷新队列失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CallPatientAsync(ConsultationQueueItem item)
        {
            if (item == null) return;
            
            try
            {
                // TODO: 发送叫号通知
                await _commonDialogService.ShowInformationAsync($"正在呼叫 {item.PatientName}", "叫号");
                item.Status = "已叫号";
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"叫号失败：{ex.Message}", "错误");
            }
        }

        #endregion

        #region Helper Methods

        private bool CanStartConsultation()
        {
            return SelectedQueueItem != null;
        }

        private bool CanSaveProgress()
        {
            return CurrentConsultationId != Guid.Empty;
        }

        private bool CanCompleteConsultation()
        {
            return !string.IsNullOrWhiteSpace(Diagnosis) && CurrentConsultationId != Guid.Empty;
        }

        private void NextTab()
        {
            if (SelectedTabIndex < 4) // 假设有5个标签页
            {
                SelectedTabIndex++;
            }
        }

        private void PreviousTab()
        {
            if (SelectedTabIndex > 0)
            {
                SelectedTabIndex--;
            }
        }

        private void LoadConsultationData(ConsultationDetailDto data)
        {
            ChiefComplaint = data.ChiefComplaint ?? string.Empty;
            PresentIllness = data.PresentIllness ?? string.Empty;
            PastHistory = data.PastHistory ?? string.Empty;
            AllergyHistory = data.AllergyHistory ?? string.Empty;
            PhysicalExamination = data.PhysicalExamination ?? string.Empty;
            
            Inspection = data.Inspection ?? string.Empty;
            AuscultationOlfaction = data.AuscultationOlfaction ?? string.Empty;
            Inquiry = data.Inquiry ?? string.Empty;
            Palpation = data.Palpation ?? string.Empty;
            TongueInspection = data.TongueInspection ?? string.Empty;
            PulseCondition = data.PulseCondition ?? string.Empty;
            
            Temperature = data.Temperature;
            SystolicPressure = data.SystolicPressure;
            DiastolicPressure = data.DiastolicPressure;
            HeartRate = data.HeartRate;
            RespiratoryRate = data.RespiratoryRate;
            
            TCMDiagnosis = data.TCMDiagnosis ?? string.Empty;
            WesternDiagnosis = data.WesternDiagnosis ?? string.Empty;
            Diagnosis = data.Diagnosis ?? string.Empty;
            TreatmentPrinciple = data.TreatmentPrinciple ?? string.Empty;
            MedicalAdvice = data.MedicalAdvice ?? string.Empty;
        }

        private void ResetConsultation()
        {
            CurrentConsultationId = Guid.Empty;
            MedicalCaseId = Guid.Empty;
            CurrentPatient = new PatientInfo();
            
            ChiefComplaint = string.Empty;
            PresentIllness = string.Empty;
            PastHistory = string.Empty;
            AllergyHistory = string.Empty;
            PhysicalExamination = string.Empty;
            
            Inspection = string.Empty;
            AuscultationOlfaction = string.Empty;
            Inquiry = string.Empty;
            Palpation = string.Empty;
            TongueInspection = string.Empty;
            PulseCondition = string.Empty;
            
            Temperature = null;
            SystolicPressure = null;
            DiastolicPressure = null;
            HeartRate = null;
            RespiratoryRate = null;
            
            TCMDiagnosis = string.Empty;
            WesternDiagnosis = string.Empty;
            Diagnosis = string.Empty;
            TreatmentPrinciple = string.Empty;
            MedicalAdvice = string.Empty;
            
            SelectedTabIndex = 0;
            SelectedQueueItem = null;
        }

        private int GetPatientAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            var age = DateTime.Now.Year - birthDate.Value.Year;
            if (DateTime.Now.DayOfYear < birthDate.Value.DayOfYear)
                age--;
            return age;
        }

        #endregion
    }

    /// <summary>
    /// 看诊队列项
    /// </summary>
    public class ConsultationQueueItem : BindableBase
    {
        public int QueueNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public Guid MedicalCaseId { get; set; }
        public Guid RegistrationId { get; set; }
        public string RegistrationType { get; set; } = string.Empty;
        
        private string _status = "等待中";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }
}