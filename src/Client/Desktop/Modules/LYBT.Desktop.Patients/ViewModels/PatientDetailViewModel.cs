using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者详情视图模型 - 组件化架构
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// 使用PatientDataManager、PatientCommandHandler、PatientValidator三个组件
    /// </summary>
    public class PatientDetailViewModel : UnifiedViewModelBase
    {
        #region 私有字段

        private readonly PatientDataManager _dataManager;
        private readonly PatientCommandHandler _commandHandler;
        private readonly PatientValidator _validator;

        #endregion 私有字段

        #region 属性

        /// <summary>患者ID</summary>
        public Guid PatientId => _dataManager.PatientId;

        /// <summary>当前患者数据</summary>
        public PatientDto? Patient => _dataManager.CurrentPatient;

        /// <summary>是否正在加载</summary>
        public new bool IsLoading => _dataManager.IsLoading;

        /// <summary>是否只读模式</summary>
        public bool IsReadOnly
        {
            get => _dataManager.IsReadOnly;
            set
            {
                if (_dataManager.IsReadOnly != value)
                {
                    _dataManager.IsReadOnly = value;
                    RaisePropertyChanged();
                    RefreshCommands();
                }
            }
        }

        /// <summary>是否有未保存的变更</summary>
        public bool HasChanges => _dataManager.HasChanges;

        // 患者基本信息属性
        public string PatientName => Patient?.Name ?? string.Empty;

        public string Gender => Patient?.Gender switch
        {
            Shared.Models.Enums.Gender.Male => "男",
            Shared.Models.Enums.Gender.Female => "女",
            _ => "未知"
        };

        public int? Age => Patient?.Age;
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

        public ICommand LoadDataCommand => _commandHandler.BackCommand; // 使用返回命令
        public ICommand BackCommand => _commandHandler.BackCommand;
        public ICommand EditCommand => _commandHandler.EditCommand;
        public ICommand SaveCommand => _commandHandler.SaveCommand;
        public ICommand CancelEditCommand => _commandHandler.CancelEditCommand;
        public ICommand PrintCommand { get; }
        public ICommand ViewMedicalHistoryCommand => _commandHandler.ViewMedicalHistoryCommand;

        #endregion 命令

        #region 构造函数

        public PatientDetailViewModel(
            PatientDataManager dataManager,
            PatientCommandHandler commandHandler,
            PatientValidator validator,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            // 设置组件依赖
            _commandHandler.SetDependencies(_dataManager);

            // 订阅组件事件
            _commandHandler.OnEditEnabled += HandleEditEnabled;
            _commandHandler.OnEditCancelled += HandleEditCancelled;
            _commandHandler.OnPatientSaved += HandlePatientSaved;
            _commandHandler.OnPatientDeleted += HandlePatientDeleted;

            // 打印命令（待实现）
            PrintCommand = new Prism.Commands.DelegateCommand(async () => await PrintPatientAsync());
        }

        #endregion 构造函数

        #region 导航生命周期

        /// <summary>
        /// 处理导航参数（同步）- Issue #1240
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 立即设置导航参数，避免UI延迟
            if (parameters.ContainsKey("PatientId"))
            {
                var patientId = parameters.GetValue<Guid>("PatientId");

                if (parameters.ContainsKey("ViewMode"))
                {
                    var viewMode = parameters.GetValue<string>("ViewMode");
                    IsReadOnly = viewMode != "Edit";
                }

                // 在InitializeAsync中加载数据
            }
        }

        /// <summary>
        /// 异步初始化数据 - Issue #1240
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 在UI线程上异步加载数据
            if (parameters.ContainsKey("PatientId"))
            {
                var patientId = parameters.GetValue<Guid>("PatientId");
                await LoadDataAsync(patientId);
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
            base.OnNavigatedFrom(navigationContext);

            if (HasChanges)
            {
                // 可以在这里添加保存确认逻辑
            }
        }

        #endregion 导航生命周期

        #region 数据操作

        private async Task LoadDataAsync(Guid patientId)
        {
            try
            {
                await _dataManager.InitializeAsync(patientId);
                RefreshProperties();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"加载患者详情失败: {ex.Message}");
            }
        }

        #endregion 数据操作

        #region 事件处理

        private void HandleEditEnabled()
        {
            IsReadOnly = false;
        }

        private async void HandleEditCancelled()
        {
            IsReadOnly = true;
            await _dataManager.ReloadAsync();
            RefreshProperties();
        }

        private async void HandlePatientSaved()
        {
            try
            {
                // 验证患者数据
                if (Patient != null)
                {
                    var inputDto = _validator.ConvertToInputDto(Patient);
                    var validationResult = await _validator.ValidatePatientInputAsync(inputDto);

                    if (!_validator.IsValid(validationResult, out string errorMessage))
                    {
                        await ShowErrorMessageAsync($"数据验证失败: {errorMessage}");
                        return;
                    }
                }

                // 保存患者数据
                var success = await _dataManager.SaveAsync();

                if (success)
                {
                    IsReadOnly = true;
                    RefreshProperties();
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
        }

        private async void HandlePatientDeleted()
        {
            try
            {
                var success = await _dataManager.DeleteAsync();

                if (success)
                {
                    await ShowSuccessMessageAsync("患者删除成功");
                    RegionManager.RequestNavigate("ContentRegion", "PatientManagementView");
                }
                else
                {
                    await ShowErrorMessageAsync("删除失败");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"删除失败: {ex.Message}");
            }
        }

        #endregion 事件处理

        #region 辅助方法

        /// <summary>
        /// 患者病历打印功能（待实现 Issue #1202）
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
                await ShowWarningMessageAsync("打印功能正在开发中");
                return;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"打印病历失败: {ex.Message}");
            }
        }

        private void RefreshProperties()
        {
            RaisePropertyChanged(nameof(PatientId));
            RaisePropertyChanged(nameof(Patient));
            RaisePropertyChanged(nameof(IsLoading));
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
            RaisePropertyChanged(nameof(HasChanges));
        }

        private new void RefreshCommands()
        {
            // 命令由CommandHandler管理，这里刷新CanExecute状态
            RefreshProperties();
        }

        private string GetStatusText()
        {
            if (Patient?.IsEnabled == true)
            {
                return "正常";
            }

            return "已禁用";
        }

        #endregion 辅助方法
    }
}
