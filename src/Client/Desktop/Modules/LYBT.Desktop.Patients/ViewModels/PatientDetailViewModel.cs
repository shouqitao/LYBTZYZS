using System.Windows.Input;
using AutoMapper;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Services.Print;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{

    /// <summary>
    /// 患者详情视图模型 - Phase 2模块化架构
    /// Issue #1114 - 直接使用Repository，去除Service层
    /// </summary>
    public class PatientDetailViewModel : UnifiedViewModelBase
    {

        #region 私有字段

        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly IPrescriptionPrintService _printService;

        private Guid _patientId;
        private PatientDto? _patient;
        private bool _isLoading;
        private bool _isReadOnly = true;

        #endregion 私有字段

        #region 属性

        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        public PatientDto? Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // 患者基本信息属性
        public string PatientName => Patient?.Name ?? string.Empty;

        public string Gender => Patient?.Gender switch
        {
            Shared.Models.Enums.Gender.Male => "男",
            Shared.Models.Enums.Gender.Female => "女",
            _ => "未知"
        };

        public int Age => Patient?.Age ?? 0;
        public string PhoneNumber => Patient?.PhoneNumber ?? string.Empty;
        public string IdNumber => Patient?.IdNumber ?? string.Empty;
        public string Address => Patient?.Address ?? string.Empty;
        public string EmergencyContact => Patient?.EmergencyContactName ?? string.Empty;
        public string EmergencyPhone => Patient?.EmergencyContactPhone ?? string.Empty;
        public DateTime? CreatedAt => Patient?.CreatedAt;
        public DateTime? UpdatedAt => Patient?.UpdatedAt;
        public string StatusText => GetStatusText();

        #endregion 属性

        #region 命令

        public ICommand LoadDataCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ViewMedicalHistoryCommand { get; }

        #endregion 命令

        #region 构造函数

        public PatientDetailViewModel(
            IPatientRepository patientRepository,
            IMapper mapper,
            IPrescriptionPrintService printService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            BackCommand = new DelegateCommand(NavigateBack);
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            PrintCommand = new DelegateCommand(async () => await PrintPatientAsync());
            ViewMedicalHistoryCommand = new DelegateCommand(async () => await ViewMedicalHistoryAsync());
        }

        #endregion 构造函数

        #region INavigationAware 实现

        /// <inheritdoc/>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                PatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");

                if (navigationContext.Parameters.ContainsKey("ViewMode"))
                {
                    var viewMode = navigationContext.Parameters.GetValue<string>("ViewMode");
                    IsReadOnly = viewMode != "Edit";
                }

                Task.Run(async () => await LoadDataAsync());
            }
        }

        /// <inheritdoc/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                var targetPatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                return PatientId == targetPatientId;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (!IsReadOnly && HasUnsavedChanges())
            {
                // 可以在这里添加保存确认逻辑
            }
        }

        #endregion INavigationAware 实现

        #region 数据操作

        private async Task LoadDataAsync()
        {
            if (PatientId == Guid.Empty)
            {
                return;
            }

            try
            {
                IsLoading = true;

                // Phase 2: 直接使用Repository，无ServiceResult包装
                Patient = await _patientRepository.GetByIdAsync(PatientId);

                if (Patient != null)
                {
                    RefreshProperties();
                }
                else
                {
                    await ShowErrorMessageAsync("未找到该患者信息");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"加载患者详情失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (Patient == null)
            {
                return;
            }

            try
            {
                IsLoading = true;

                // Phase 2: 映射到UpdateDto后更新
                var updateDto = _mapper.Map<PatientUpdateDto>(Patient);
                var updatedPatient = await _patientRepository.UpdateAsync(updateDto);

                if (updatedPatient != null)
                {
                    Patient = updatedPatient;
                    IsReadOnly = true;
                    RefreshProperties();
                    RaiseCanExecuteChanged();

                    await ShowSuccessMessageAsync("患者信息保存成功");
                }
                else
                {
                    await ShowErrorMessageAsync("保存失败：服务器未返回数据");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion 数据操作

        #region 命令处理

        private void NavigateBack()
        {
            RegionManager.RequestNavigate("ContentRegion", "PatientManagementView");
        }

        private void EnableEdit()
        {
            IsReadOnly = false;
            RaiseCanExecuteChanged();
        }

        private void CancelEdit()
        {
            IsReadOnly = true;

            // 重新加载数据以取消更改
            Task.Run(async () => await LoadDataAsync());
        }

        /// <summary>
        /// P0-03新增：患者病历打印功能
        /// Epic 03-P0-03: 实用化患者病历打印功能，专为小诊所设计
        /// 使用专业的IPrescriptionPrintService打印患者档案和病历信息
        /// </summary>
        private async Task PrintPatientAsync()
        {
            if (Patient == null)
            {
                await ShowWarningMessageAsync("患者信息不完整，无法打印");
                return;
            }

            try
            {
                // P0-03核心：使用专业打印服务生成患者病历预览
                // TODO: 需要修改打印服务或创建患者专用打印方法
                // var previewResult = await _printService.PreviewPatientAsync(Patient);
                // 暂时注释掉打印功能
                await ShowWarningMessageAsync("打印功能正在开发中");
                return;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"打印病历失败: {ex.Message}");
            }
        }

        private async Task ViewMedicalHistoryAsync()
        {
            if (Patient == null)
            {
                return;
            }

            try
            {
                // 导航到医疗历史视图 - 使用Task.Run包装同步操作以修复CS1998警告
                var navigationParameters = new NavigationParameters
                {
                    { "PatientId", Patient.Id }
                };
                // 使用同步导航
                RegionManager.RequestNavigate("ContentRegion", "MedicalCaseListView", navigationParameters);
                await Task.CompletedTask; // 保持异步签名
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"操作失败: {ex.Message}");
            }
        }

        #endregion 命令处理

        #region 命令状态

        private bool CanEdit() => Patient != null && IsReadOnly && !IsLoading;

        private bool CanSave() => Patient != null && !IsReadOnly && !IsLoading;

        private bool CanCancelEdit() => Patient != null && !IsReadOnly && !IsLoading;

        private void RaiseCanExecuteChanged()
        {
            ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        #endregion 命令状态

        #region 辅助方法

        private void RefreshProperties()
        {
            RaisePropertyChanged(nameof(PatientName));
            RaisePropertyChanged(nameof(Gender));
            RaisePropertyChanged(nameof(Age));
            RaisePropertyChanged(nameof(PhoneNumber));
            RaisePropertyChanged(nameof(IdNumber));
            RaisePropertyChanged(nameof(Address));
            RaisePropertyChanged(nameof(EmergencyContact));
            RaisePropertyChanged(nameof(EmergencyPhone));
            RaisePropertyChanged(nameof(CreatedAt));
            RaisePropertyChanged(nameof(UpdatedAt));
            RaisePropertyChanged(nameof(StatusText));
        }

        private string GetStatusText()
        {
            if (Patient?.IsEnabled == true)
            {
                return "正常";
            }

            return "已禁用";
        }

        private bool HasUnsavedChanges()
        {
            // 简单实现：如果处于编辑模式就认为有未保存的更改
            return !IsReadOnly;
        }

        #endregion 辅助方法
    }
}
