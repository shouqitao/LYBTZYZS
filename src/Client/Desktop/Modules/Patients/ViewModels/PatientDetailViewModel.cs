using System.Windows.Input;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels {

    /// <summary>
    /// 患者详情视图模型 - UltraThink v2.0架构
    /// 提供患者详细信息查看功能
    /// </summary>
    public class PatientDetailViewModel : ServiceViewModel, INavigationAware {

        #region 私有字段

        private readonly IPatientService _patientService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;

        private Guid _patientId;
        private PatientDto? _patient;
        private bool _isLoading;
        private bool _isReadOnly = true;

        #endregion 私有字段

        #region 属性

        public Guid PatientId {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        public PatientDto? Patient {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public new bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsReadOnly {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // 患者基本信息属性
        public string PatientName => Patient?.Name ?? "";

        public string Gender => Patient?.Gender switch {
            Shared.Models.Enums.Gender.Male => "男",
            Shared.Models.Enums.Gender.Female => "女",
            _ => "未知"
        };

        public int Age => Patient?.Age ?? 0;
        public string PhoneNumber => Patient?.PhoneNumber ?? "";
        public string IdNumber => Patient?.IdNumber ?? "";
        public string Address => Patient?.Address ?? "";
        public string EmergencyContact => Patient?.EmergencyContact ?? "";
        public string EmergencyPhone => Patient?.EmergencyPhone ?? "";
        public DateTime? CreateTime => Patient?.CreateTime;
        public DateTime? UpdateTime => Patient?.UpdateTime;
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
            IPatientService patientService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IMapper mapper,
            IErrorHandlingService errorHandlingService,
            IEventAggregator eventAggregator)
            : base(eventAggregator, errorHandlingService) {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

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

        public void OnNavigatedTo(NavigationContext navigationContext) {
            if (navigationContext.Parameters.ContainsKey("PatientId")) {
                PatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");

                if (navigationContext.Parameters.ContainsKey("ViewMode")) {
                    var viewMode = navigationContext.Parameters.GetValue<string>("ViewMode");
                    IsReadOnly = viewMode != "Edit";
                }

                Task.Run(async () => await LoadDataAsync());
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) {
            if (navigationContext.Parameters.ContainsKey("PatientId")) {
                var targetPatientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                return PatientId == targetPatientId;
            }
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext) {
            if (!IsReadOnly && HasUnsavedChanges()) {
                // 可以在这里添加保存确认逻辑
            }
        }

        #endregion INavigationAware 实现

        #region 数据操作

        private async Task LoadDataAsync() {
            if (PatientId == Guid.Empty) {
                return;
            }

            try {
                IsLoading = true;

                var result = await _patientService.GetByIdAsync(PatientId);

                if (result.IsSuccess && result.Data != null) {
                    Patient = result.Data;
                    RefreshProperties();
                } else {
                    await _dialogService.ShowErrorAsync($"加载患者详情失败: {result.ErrorMessage}", "错误");
                }
            } catch (Exception ex) {
                await _dialogService.ShowErrorAsync($"加载患者详情失败: {ex.Message}", "错误");
            } finally {
                IsLoading = false;
            }
        }

        private async Task SaveAsync() {
            if (Patient == null) {
                return;
            }

            try {
                IsLoading = true;

                var updateDto = _mapper.Map<PatientUpdateDto>(Patient);

                var result = await _patientService.UpdateAsync(Patient.Id, updateDto);

                if (result.IsSuccess && result.Data != null) {
                    Patient = result.Data;
                    IsReadOnly = true;
                    RefreshProperties();
                    RaiseCanExecuteChanged();

                    await _dialogService.ShowSuccessAsync("患者信息保存成功", "成功");
                } else {
                    await _dialogService.ShowErrorAsync($"保存失败: {result.ErrorMessage}", "错误");
                }
            } catch (Exception ex) {
                await _dialogService.ShowErrorAsync($"保存失败: {ex.Message}", "错误");
            } finally {
                IsLoading = false;
            }
        }

        #endregion 数据操作

        #region 命令处理

        private void NavigateBack() {
            _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion, "PatientManagementView");
        }

        private void EnableEdit() {
            IsReadOnly = false;
            RaiseCanExecuteChanged();
        }

        private void CancelEdit() {
            IsReadOnly = true;
            // 重新加载数据以取消更改
            Task.Run(async () => await LoadDataAsync());
        }

        private async Task PrintPatientAsync() {
            try {
                await _dialogService.ShowInformationAsync(
                    "打印功能将在后续版本中提供\n\n当前支持的操作：\n• 查看患者详细信息\n• 编辑患者档案\n• 查看就诊历史",
                    "功能说明");
            } catch (Exception ex) {
                await _dialogService.ShowErrorAsync($"打印失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewMedicalHistoryAsync() {
            if (Patient == null) {
                return;
            }

            try {
                // 导航到医疗历史视图 - 使用Task.Run包装同步操作以修复CS1998警告
                await Task.Run(() => {
                    _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion,
                        $"MedicalCaseListView?PatientId={Patient.Id}");
                });
            } catch (Exception ex) {
                await _dialogService.ShowErrorAsync($"操作失败: {ex.Message}", "错误");
            }
        }

        #endregion 命令处理

        #region 命令状态

        private bool CanEdit() => Patient != null && IsReadOnly && !IsLoading;

        private bool CanSave() => Patient != null && !IsReadOnly && !IsLoading;

        private bool CanCancelEdit() => Patient != null && !IsReadOnly && !IsLoading;

        private new void RaiseCanExecuteChanged() {
            ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        #endregion 命令状态

        #region 辅助方法

        private void RefreshProperties() {
            RaisePropertyChanged(nameof(PatientName));
            RaisePropertyChanged(nameof(Gender));
            RaisePropertyChanged(nameof(Age));
            RaisePropertyChanged(nameof(PhoneNumber));
            RaisePropertyChanged(nameof(IdNumber));
            RaisePropertyChanged(nameof(Address));
            RaisePropertyChanged(nameof(EmergencyContact));
            RaisePropertyChanged(nameof(EmergencyPhone));
            RaisePropertyChanged(nameof(CreateTime));
            RaisePropertyChanged(nameof(UpdateTime));
            RaisePropertyChanged(nameof(StatusText));
        }

        private string GetStatusText() {
            if (Patient?.IsActive == true) {
                return "正常";
            }

            return "已禁用";
        }

        private bool HasUnsavedChanges() {
            // 简单实现：如果处于编辑模式就认为有未保存的更改
            return !IsReadOnly;
        }

        #endregion 辅助方法
    }
}
