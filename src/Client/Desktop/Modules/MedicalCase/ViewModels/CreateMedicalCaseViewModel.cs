using System.Collections.ObjectModel;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;

// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Shared.Models.Contracts.Patients;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.MedicalCase.ViewModels
{

    /// <summary>
    /// 创建医疗案例对话框视图模型 - UltraThink双层架构UI层
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：医疗案例创建对话框交互逻辑、患者选择、数据验证、创建提交
    /// 基于DialogViewModel统一对话框模式，实现ICustomDialogAware接口
    /// 集成MedicalCaseModule双层服务，提供完整的医案创建用户体验
    /// 支持患者搜索选择、新患者创建、医案信息录入等功能
    /// 适配中医诊所医案建档流程，确保数据录入准确性和操作便利性
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
                    PatientPhone = value.PhoneNumber ?? string.Empty;
                    PatientGender = value.Gender.ToString();
                    PatientAge = value.Age; // UltraThink v2.0: 使用计算属性Age
                }

                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private string _patientSearchKeyword = string.Empty;

        public string PatientSearchKeyword
        {
            get => _patientSearchKeyword;
            set => SetProperty(ref _patientSearchKeyword, value);
        }

        private string _patientName = string.Empty;

        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientPhone = string.Empty;

        public string PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        private string _patientGender = string.Empty;

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

        private string _remark = string.Empty;

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion Properties

        #region Commands

        // UltraThink修复: 使用new关键字隐藏基类成员，并初始化为null!
        public new DelegateCommand SaveCommand { get; } = null!;

        public new DelegateCommand CancelCommand { get; } = null!;
        public DelegateCommand SearchPatientCommand { get; } = null!;
        public DelegateCommand CreateNewPatientCommand { get; } = null!;

        #endregion Commands

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateMedicalCaseViewModel"/> class.
        /// 构造函数 - UltraThink双层架构依赖注入
        /// 初始化医案创建模块、患者服务、会话管理器、对话框服务等依赖
        /// </summary>
        /// <param name="medicalCaseService">医疗案例服务</param>
        /// <param name="patientService">患者服务</param>
        /// <param name="userSessionManager">用户会话管理器</param>
        /// <param name="dialogService">自定义对话框服务</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="mapper">对象映射器</param>
        /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
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
        /// Initializes a new instance of the <see cref="CreateMedicalCaseViewModel"/> class.
        /// 兼容性构造函数 - 支持无错误处理服务的旧版本调用
        /// </summary>
        /// <param name="medicalCaseService">医疗案例服务</param>
        /// <param name="patientService">患者服务</param>
        /// <param name="userSessionManager">用户会话管理器</param>
        /// <param name="dialogService">自定义对话框服务</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="mapper">对象映射器</param>
        /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
        public CreateMedicalCaseViewModel(
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            IUserSessionManager userSessionManager,
            ICustomDialogService dialogService,
            IEventAggregator eventAggregator,
            IMapper mapper)
            : base(eventAggregator, null!)
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

        #endregion Constructor

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

        #endregion DialogViewModel Implementation

        #region Private Methods

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "加载患者列表...";

                // Get active patients using SearchAsync
                var result = await _patientService.SearchAsync(string.Empty); // 获取所有活跃患者
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
                StatusMessage = string.Empty;
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
                StatusMessage = string.Empty;
            }
        }

        private async Task CreateNewPatientAsync()
        {
            try
            {
                // 打开新建患者对话框
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };

                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);

                if (result.Result == true)
                {
                    // 患者创建成功，刷新患者列表

                    // 刷新患者列表
                    await LoadPatientsAsync();

                    // 如果有返回的患者数据，自动选择该患者
                    if (result.Data is Dictionary<string, object> data && data.ContainsKey("Patient") && data["Patient"] is PatientDto newPatient)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedPatient = newPatient;
                        });

                        // 已自动选择新创建的患者
                    }

                    await _dialogService.ShowSuccessAsync("患者创建成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("创建患者", ex);
                await _dialogService.ShowErrorAsync($"创建患者失败: {ex.Message}", "错误");
            }
        }

        // UltraThink v2.0: CalculateAge方法已移除，直接使用PatientDto.Age计算属性
        #endregion Private Methods

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? "创建医疗案例";

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = obj => { };

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

        #endregion ICustomDialogAware Implementation
    }
}
