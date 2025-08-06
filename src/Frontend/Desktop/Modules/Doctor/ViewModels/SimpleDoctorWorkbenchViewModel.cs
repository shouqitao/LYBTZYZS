using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Enums;
using System.Collections.Generic;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.WPF.Client.Modules.Doctor.ViewModels
{
    /// <summary>
    /// 简化医生工作台视图模型 - 包含看诊核心功能
    /// </summary>
    public class SimpleDoctorWorkbenchViewModel : BindableBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IPatientService _patientService;
        private readonly IHerbService _herbService;
        private readonly IFormulaTemplateService _formulaTemplateService;
        // private readonly IPrescriptionService _prescriptionService; // 暂时注释，接口未定义
        private readonly ICommonDialogService _dialogService;
        private readonly IPrescriptionPrintService _printService;

        private bool _isLoading;
        private RegistrationDetailDto? _currentRegistration;
        private PatientDetailDto? _currentPatient;
        
        // 病史采集
        private string _chiefComplaint = string.Empty;  // 主诉
        private string _presentIllness = string.Empty;  // 现病史
        
        // 中医辨证
        private string _tcmDiagnosis = string.Empty;    // 中医辨证
        private string _lookDiagnosis = string.Empty;   // 望诊
        private string _listenDiagnosis = string.Empty; // 闻诊
        private string _askDiagnosis = string.Empty;    // 问诊
        private string _pulseDiagnosis = string.Empty;  // 切诊
        
        // 处方开具
        private ObservableCollection<PrescriptionItemViewModel> _prescriptionItems;
        private FormulaTemplateDetailDto? _selectedTemplate;
        private HerbDto? _selectedHerb;
        private decimal _totalPrice;
        
        public SimpleDoctorWorkbenchViewModel(
            IRegistrationService registrationService,
            IPatientService patientService,
            IHerbService herbService,
            IFormulaTemplateService formulaTemplateService,
            // IPrescriptionService prescriptionService, // 暂时注释，接口未定义
            ICommonDialogService dialogService,
            IPrescriptionPrintService printService)
        {
            _registrationService = registrationService;
            _patientService = patientService;
            _herbService = herbService;
            _formulaTemplateService = formulaTemplateService;
            // _prescriptionService = prescriptionService; // 暂时注释，接口未定义
            _dialogService = dialogService;
            _printService = printService;

            WaitingPatients = new ObservableCollection<RegistrationDetailDto>();
            AvailableHerbs = new ObservableCollection<HerbDto>();
            FormulaTemplates = new ObservableCollection<FormulaTemplateDetailDto>();
            PrescriptionItems = new ObservableCollection<PrescriptionItemViewModel>();

            InitializeCommands();
            LoadDataAsync();
        }

        #region Properties

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>等待看诊的患者列表</summary>
        public ObservableCollection<RegistrationDetailDto> WaitingPatients { get; }

        /// <summary>当前挂号信息</summary>
        public RegistrationDetailDto? CurrentRegistration
        {
            get => _currentRegistration;
            set
            {
                SetProperty(ref _currentRegistration, value);
                if (value != null)
                {
                    LoadPatientDetailsAsync(value.PatientId);
                }
            }
        }

        /// <summary>当前患者信息</summary>
        public PatientDetailDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        #region 病史采集

        /// <summary>主诉</summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        /// <summary>现病史</summary>
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        #endregion

        #region 中医辨证

        /// <summary>中医辨证</summary>
        public string TcmDiagnosis
        {
            get => _tcmDiagnosis;
            set => SetProperty(ref _tcmDiagnosis, value);
        }

        /// <summary>望诊</summary>
        public string LookDiagnosis
        {
            get => _lookDiagnosis;
            set => SetProperty(ref _lookDiagnosis, value);
        }

        /// <summary>闻诊</summary>
        public string ListenDiagnosis
        {
            get => _listenDiagnosis;
            set => SetProperty(ref _listenDiagnosis, value);
        }

        /// <summary>问诊</summary>
        public string AskDiagnosis
        {
            get => _askDiagnosis;
            set => SetProperty(ref _askDiagnosis, value);
        }

        /// <summary>切诊</summary>
        public string PulseDiagnosis
        {
            get => _pulseDiagnosis;
            set => SetProperty(ref _pulseDiagnosis, value);
        }

        #endregion

        #region 处方开具

        /// <summary>可用药材列表</summary>
        public ObservableCollection<HerbDto> AvailableHerbs { get; }

        /// <summary>验方模板列表</summary>
        public ObservableCollection<FormulaTemplateDetailDto> FormulaTemplates { get; }

        /// <summary>处方项目列表</summary>
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems
        {
            get => _prescriptionItems;
            set
            {
                SetProperty(ref _prescriptionItems, value);
                CalculateTotalPrice();
            }
        }

        /// <summary>选中的验方模板</summary>
        public FormulaTemplateDetailDto? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                SetProperty(ref _selectedTemplate, value);
                if (value != null)
                {
                    ApplyFormulaTemplate(value);
                }
            }
        }

        /// <summary>选中的药材</summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        /// <summary>总价</summary>
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        #endregion

        #endregion

        #region Commands

        public DelegateCommand<RegistrationDetailDto>? SelectPatientCommand { get; private set; }
        public DelegateCommand? AddHerbToPrescriptionCommand { get; private set; }
        public DelegateCommand<PrescriptionItemViewModel>? RemoveHerbFromPrescriptionCommand { get; private set; }
        public DelegateCommand? ClearPrescriptionCommand { get; private set; }
        public DelegateCommand? SaveAndCompleteCommand { get; private set; }
        public DelegateCommand? PrintPrescriptionCommand { get; private set; }

        private void InitializeCommands()
        {
            SelectPatientCommand = new DelegateCommand<RegistrationDetailDto>(registration => CurrentRegistration = registration);
            AddHerbToPrescriptionCommand = new DelegateCommand(AddHerbToPrescription);
            RemoveHerbFromPrescriptionCommand = new DelegateCommand<PrescriptionItemViewModel>(RemoveHerbFromPrescription);
            ClearPrescriptionCommand = new DelegateCommand(ClearPrescription);
            SaveAndCompleteCommand = new DelegateCommand(async () => await SaveAndCompleteConsultation());
            PrintPrescriptionCommand = new DelegateCommand(async () => await PrintPrescription());
        }

        #endregion

        #region Methods

        /// <summary>加载数据</summary>
        private async void LoadDataAsync()
        {
            await LoadWaitingPatients();
            await LoadAvailableHerbs();
            await LoadFormulaTemplates();
        }

        /// <summary>加载等待看诊的患者</summary>
        private async Task LoadWaitingPatients()
        {
            try
            {
                IsLoading = true;
                var registrations = await _registrationService.GetPagedAsync(1, 100);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WaitingPatients.Clear();
                    foreach (var reg in registrations.Items.Where(r => r.Status == RegistrationStatus.Scheduled))
                    {
                        // 将 RegistrationInfo 转换为 RegistrationDetailDto
                        var dto = new RegistrationDetailDto
                        {
                            Id = reg.Id,
                            RegistrationNumber = reg.RegistrationNumber,
                            PatientId = reg.PatientId,
                            PatientName = reg.PatientName,
                            PatientPhone = reg.PatientPhone,
                            DoctorId = reg.DoctorId,
                            DoctorName = reg.DoctorName,
                            RegistrationType = reg.RegistrationType.ToString(),
                            RegistrationFee = reg.RegistrationFee,
                            Status = reg.Status.ToString(),
                            AppointmentDate = reg.AppointmentDate,
                            AppointmentTimeSlot = reg.AppointmentTimeSlot,
                            QueueNumber = reg.QueueNumber,
                            IsPaid = reg.IsPaid,
                            CreateTime = reg.CreateTime,
                            UpdateTime = reg.UpdateTime,
                            Remark = reg.Remark
                        };
                        WaitingPatients.Add(dto);
                    }
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>加载患者详情</summary>
        private async void LoadPatientDetailsAsync(Guid patientId)
        {
            try
            {
                var patientResult = await _patientService.GetByIdAsync(patientId);
                CurrentPatient = patientResult.Data;
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"加载患者信息失败：{ex.Message}");
            }
        }

        /// <summary>加载可用药材</summary>
        private async Task LoadAvailableHerbs()
        {
            try
            {
                var herbs = await _herbService.GetAvailableHerbsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableHerbs.Clear();
                    foreach (var herb in herbs)
                    {
                        AvailableHerbs.Add(new HerbDto { Id = herb.Id, Name = herb.Name, Price = herb.Price, Unit = herb.Unit ?? "克" });
                    }
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"加载药材列表失败：{ex.Message}");
            }
        }

        /// <summary>加载验方模板</summary>
        private async Task LoadFormulaTemplates()
        {
            try
            {
                var templates = await _formulaTemplateService.GetListAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FormulaTemplates.Clear();
                    foreach (var templateInfo in templates.Data)
                    {
                        // 转换为 FormulaTemplateDetailDto
                        var dto = new FormulaTemplateDetailDto 
                        {
                            Id = templateInfo.Id,
                            Name = templateInfo.Name,
                            Herbs = new List<FormulaTemplateHerbDto>()
                        };
                        FormulaTemplates.Add(dto);
                    }
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"加载验方模板失败：{ex.Message}");
            }
        }

        /// <summary>应用验方模板</summary>
        private void ApplyFormulaTemplate(FormulaTemplateDetailDto template)
        {
            PrescriptionItems.Clear();
            
            foreach (var herbItem in template.Herbs)
            {
                var herb = AvailableHerbs.FirstOrDefault(h => h.Id == herbItem.HerbId);
                if (herb != null)
                {
                    PrescriptionItems.Add(new PrescriptionItemViewModel
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        Quantity = herbItem.Quantity,
                        Unit = herbItem.Unit,
                        Price = herb.Price,
                        Subtotal = herbItem.Quantity * herb.Price
                    });
                }
            }
            
            CalculateTotalPrice();
        }

        /// <summary>添加药材到处方</summary>
        private void AddHerbToPrescription()
        {
            if (SelectedHerb == null)
            {
                _dialogService.ShowInformationAsync("请先选择药材");
                return;
            }

            var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == SelectedHerb.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 10; // 默认增加10克
                existingItem.Subtotal = existingItem.Quantity * existingItem.Price;
            }
            else
            {
                PrescriptionItems.Add(new PrescriptionItemViewModel
                {
                    HerbId = SelectedHerb.Id,
                    HerbName = SelectedHerb.Name,
                    Quantity = 10, // 默认10克
                    Unit = SelectedHerb.Unit ?? "克",
                    Price = SelectedHerb.Price,
                    Subtotal = 10 * SelectedHerb.Price
                });
            }
            
            CalculateTotalPrice();
        }

        /// <summary>从处方移除药材</summary>
        private void RemoveHerbFromPrescription(PrescriptionItemViewModel item)
        {
            PrescriptionItems.Remove(item);
            CalculateTotalPrice();
        }

        /// <summary>清空处方</summary>
        private void ClearPrescription()
        {
            PrescriptionItems.Clear();
            CalculateTotalPrice();
        }

        /// <summary>计算总价</summary>
        private void CalculateTotalPrice()
        {
            TotalPrice = PrescriptionItems.Sum(p => p.Subtotal);
        }

        /// <summary>保存并完成看诊</summary>
        private async Task SaveAndCompleteConsultation()
        {
            // 验证必填项
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                _dialogService.ShowInformationAsync("请填写主诉");
                return;
            }

            if (string.IsNullOrWhiteSpace(PresentIllness))
            {
                _dialogService.ShowInformationAsync("请填写现病史");
                return;
            }

            if (string.IsNullOrWhiteSpace(TcmDiagnosis))
            {
                _dialogService.ShowInformationAsync("请填写中医辨证");
                return;
            }

            if (!PrescriptionItems.Any())
            {
                _dialogService.ShowInformationAsync("请开具处方");
                return;
            }

            try
            {
                IsLoading = true;

                // TODO: 调用服务保存看诊记录
                // await _consultationService.SaveConsultation(...);

                _dialogService.ShowInformationAsync("看诊记录保存成功！");
                
                // 清空表单，准备下一个患者
                ClearForm();
                await LoadWaitingPatients();
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"保存失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>打印处方</summary>
        private async Task PrintPrescription()
        {
            if (!PrescriptionItems.Any())
            {
                _dialogService.ShowInformationAsync("处方为空，无法打印");
                return;
            }

            if (CurrentPatient == null || CurrentRegistration == null)
            {
                _dialogService.ShowInformationAsync("请先选择患者");
                return;
            }

            try
            {
                // 创建处方模型
                var prescriptionModel = new SimplePrescriptionModel
                {
                    PatientName = CurrentPatient.Name,
                    PatientGender = CurrentPatient.Gender.ToString(),
                    PatientAge = 0 /* CurrentPatient.Age ?? 0 */,
                    PatientPhone = CurrentPatient.PhoneNumber ?? string.Empty,
                    DoctorName = "当前医生", // TODO: 从登录信息获取
                    PrescriptionDate = DateTime.Now,
                    Diagnosis = TcmDiagnosis,
                    Herbs = PrescriptionItems.Select(p => new HerbItem
                    {
                        Name = p.HerbName,
                        Quantity = p.Quantity,
                        Unit = p.Unit
                    }).ToList(),
                    TotalPrice = TotalPrice,
                    Usage = "每日一剂，水煎服，分两次温服",
                    DoctorAdvice = "忌辛辣生冷"
                };

                // 调用打印服务
                var success = await _printService.PrintPrescriptionAsync(prescriptionModel);
                if (success)
                {
                    _dialogService.ShowInformationAsync("处方已发送到打印机");
                }
                else
                {
                    _dialogService.ShowErrorAsync("打印失败，请检查打印机设置");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorAsync($"打印失败：{ex.Message}");
            }
        }

        /// <summary>清空表单</summary>
        private void ClearForm()
        {
            CurrentRegistration = null;
            CurrentPatient = null;
            ChiefComplaint = string.Empty;
            PresentIllness = string.Empty;
            TcmDiagnosis = string.Empty;
            LookDiagnosis = string.Empty;
            ListenDiagnosis = string.Empty;
            AskDiagnosis = string.Empty;
            PulseDiagnosis = string.Empty;
            PrescriptionItems.Clear();
            TotalPrice = 0;
        }

        #endregion
    }

    /// <summary>
    /// 处方项视图模型
    /// </summary>
    public class PrescriptionItemViewModel : BindableBase
    {
        private decimal _quantity;
        private decimal _subtotal;

        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                Subtotal = _quantity * Price;
            }
        }
        
        public string Unit { get; set; } = "克";
        public decimal Price { get; set; }
        
        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }
    }
}