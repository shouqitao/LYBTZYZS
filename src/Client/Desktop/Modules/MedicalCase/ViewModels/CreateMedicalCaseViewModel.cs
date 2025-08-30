using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Models.Common;
using Prism.Commands;
using Prism.Events;
using AutoMapper;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 创建医疗案例对话框视图模型
    /// </summary>
    public class CreateMedicalCaseViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #region Properties


        private ObservableCollection<PatientDto> _patients = new();
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        public PatientDto? SelectedPatient
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
                    PatientAge = value.Age; // UltraThink v2.0: 使用计算属性Age
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

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        public CreateMedicalCaseViewModel(
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            IUserSessionManager userSessionManager,
            ICustomDialogService dialogService,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            IMapper mapper)
            : base(eventAggregator, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            DialogTitle = SystemConstants.CreateMedicalCaseDialogTitle;

            // Initialize commands
            SearchPatientCommand = new DelegateCommand(async () => await SearchPatientAsync());
            CreateNewPatientCommand = new DelegateCommand(async () => await CreateNewPatientAsync());

            InitializeDialog();
            
            // Load initial patients
            Task.Run(async () => await LoadPatientsAsync());
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        public CreateMedicalCaseViewModel(
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            IUserSessionManager userSessionManager,
            ICustomDialogService dialogService,
            IEventAggregator eventAggregator,
            IMapper mapper)
            : base(eventAggregator)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            DialogTitle = SystemConstants.CreateMedicalCaseDialogTitle;

            SearchPatientCommand = new DelegateCommand(async () => await SearchPatientAsync());
            CreateNewPatientCommand = new DelegateCommand(async () => await CreateNewPatientAsync());

            InitializeDialog();
            
            // Load initial patients
            Task.Run(async () => await LoadPatientsAsync());
        }

        #endregion

        #region DialogViewModel Implementation

        protected override async Task<bool> SaveAsync()
        {
            if (SelectedPatient == null)
            {
                ErrorMessage = "请选择患者";
                return false;
            }

            try
            {
                // UltraThink v2.0: 直接创建DTO，移除Info层
                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                    DiagnosisSummary = string.IsNullOrWhiteSpace(Remark) ? "初次就诊" : Remark.Trim(),
                    Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim()
                };

                var result = await _medicalCaseService.CreateAsync(createDto);
                if (result.IsSuccess)
                {
                    // 保存成功，关闭对话框
                    RaiseRequestClose(true);
                    return true;
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "创建医疗案例失败";
                    return false;
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("创建医疗案例", ex);
                return false;
            }
        }

        protected override bool CanSave()
        {
            return SelectedPatient != null && !IsLoading;
        }

        protected override void InitializeDialog()
        {
            base.InitializeDialog();
            
            // 监听属性变化以更新Command状态
            SaveCommand.ObservesProperty(() => SelectedPatient);
        }

        #endregion

        #region Private Methods

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "加载患者列表...";

                // Get active patients using SearchAsync
                var result = await _patientService.SearchAsync(""); // 获取所有活跃患者
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Patients.Clear();
                        // UltraThink v2.0: 直接使用DTO，SearchAsync已返回PatientDto列表
                        foreach (var patientDto in result.Data)
                        {
                            Patients.Add(patientDto);
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
                        // UltraThink v2.0: 直接使用DTO，从DetailDto转换为Dto
                        var patientDetail = result.Data;
                        // 创建基础PatientDto对象
                        var patientDto = new PatientDto
                        {
                            Id = patientDetail.Id,
                            Name = patientDetail.Name,
                            PhoneNumber = patientDetail.PhoneNumber,
                            Gender = patientDetail.Gender,
                            BirthDate = patientDetail.BirthDate, // UltraThink v2.0: 统一字段名后直接使用BirthDate
                            Status = patientDetail.Status
                            // UltraThink v2.0: 移除已删除的字段 CreateTime, UpdateTime, Remark
                        };
                        SelectedPatient = patientDto;
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
                    // UltraThink v2.0: SearchAsync已返回PatientDto列表，直接使用
                    foreach (var patientDto in result.Data)
                    {
                        Patients.Add(patientDto);
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


        // UltraThink v2.0: CalculateAge方法已移除，直接使用PatientDto.Age计算属性

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? "创建医疗案例";

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            return !IsSaving && !IsLoading;
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters?.ContainsKey("PatientId") == true && parameters["PatientId"] is Guid patientId)
            {
                Task.Run(async () => await LoadPatientByIdAsync(patientId));
            }
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源或执行其他关闭操作
        }

        /// <summary>
        /// 重写取消操作以使用ICustomDialogAware接口
        /// </summary>
        protected override void ExecuteCancel()
        {
            OnDialogClosing();
            RaiseRequestClose(false);
        }

        /// <summary>
        /// 触发关闭对话框请求
        /// </summary>
        protected void RaiseRequestClose(bool? dialogResult)
        {
            var result = dialogResult == true 
                ? CustomDialogResult.Success(new Dictionary<string, object>())
                : CustomDialogResult.Cancel();
                
            RequestClose?.Invoke(result);
        }

        #endregion
    }
}