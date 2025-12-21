using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>患者详情视图模型 - CRUD统一架构</summary>
    public class PatientDetailViewModel : UnifiedViewModelBase
    {
        private readonly IPatientRepository _patientRepository;

        private Guid _patientId;
        private bool _isEditMode = true;
        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private Gender _selectedGender = Gender.Unknown;
        private DateTime? _birthDate;
        private string? _idNumber;
        private string? _phoneNumber;
        private string? _address;
        private CommonStatus _status = CommonStatus.Enabled;

        public Guid PatientId { get => _patientId; set => SetProperty(ref _patientId, value); }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set { if (SetProperty(ref _isEditMode, value)) { RaisePropertyChanged(nameof(IsReadOnly)); SubmitCommand?.RaiseCanExecuteChanged(); SwitchToEditModeCommand?.RaiseCanExecuteChanged(); } }
        }

        public bool IsReadOnly => !IsEditMode;
        public bool IsCreateMode => PatientId == Guid.Empty;
        public bool IsEditOrViewMode => PatientId != Guid.Empty;

        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        public string Name
        {
            get => _name;
            set { if (SetProperty(ref _name, value)) { PinYinCode = PinYinHelper.GetPinYinCode(value); ValidateProperty(); SubmitCommand?.RaiseCanExecuteChanged(); } }
        }

        public string PinYinCode { get => _pinYinCode; private set => SetProperty(ref _pinYinCode, value); }
        public Gender SelectedGender { get => _selectedGender; set => SetProperty(ref _selectedGender, value); }

        public DateTime? BirthDate { get => _birthDate; set { if (SetProperty(ref _birthDate, value)) RaisePropertyChanged(nameof(Age)); } }

        public int? Age => BirthDate.HasValue ? DateTime.Today.Year - BirthDate.Value.Year - (BirthDate.Value.Date > DateTime.Today.AddYears(-(DateTime.Today.Year - BirthDate.Value.Year)) ? 1 : 0) : null;

        [Required(ErrorMessage = "身份证号不能为空")]
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        public string? IdNumber { get => _idNumber; set { if (SetProperty(ref _idNumber, value)) { ValidateProperty(); SubmitCommand?.RaiseCanExecuteChanged(); } } }

        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        public string? PhoneNumber { get => _phoneNumber; set { if (SetProperty(ref _phoneNumber, value)) ValidateProperty(); } }

        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        public string? Address { get => _address; set { if (SetProperty(ref _address, value)) ValidateProperty(); } }

        public CommonStatus Status { get => _status; set => SetProperty(ref _status, value); }
        public IEnumerable<Gender> GenderOptions { get; }
        public IEnumerable<CommonStatus> StatusOptions { get; }

        public DelegateCommand SubmitCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand SwitchToEditModeCommand { get; }
        public DelegateCommand GoBackCommand { get; }

        public PatientDetailViewModel(
            IPatientRepository patientRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            GenderOptions = Enum.GetValues<Gender>();
            StatusOptions = Enum.GetValues<CommonStatus>();
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), () => IsEditMode && !IsLoading && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(IdNumber) && !HasErrors);
            CancelCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
            SwitchToEditModeCommand = new DelegateCommand(() => { IsEditMode = true; PageTitle = $"编辑患者 - {Name}"; }, () => PatientId != Guid.Empty && !IsEditMode);
            GoBackCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
        }

        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            if (parameters.ContainsKey("PatientId")) PatientId = parameters.GetValue<Guid>("PatientId");
            IsEditMode = !(parameters.ContainsKey("ReadOnly") && parameters.GetValue<bool>("ReadOnly"));
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            if (PatientId != Guid.Empty) await LoadPatientAsync();
            else { Name = PinYinCode = string.Empty; SelectedGender = Gender.Unknown; BirthDate = null; IdNumber = PhoneNumber = Address = null; Status = CommonStatus.Enabled; PageTitle = "创建患者"; }
        }

        private async Task LoadPatientAsync()
        {
            try
            {
                IsLoading = true; StatusMessage = "正在加载患者信息...";
                var patient = await _patientRepository.GetByIdAsync(PatientId);
                if (patient != null)
                {
                    Name = patient.Name; PinYinCode = patient.PinYinCode ?? PinYinHelper.GetPinYinCode(patient.Name);
                    SelectedGender = patient.Gender; BirthDate = patient.BirthDate; IdNumber = patient.IdNumber;
                    PhoneNumber = patient.PhoneNumber; Address = patient.Address; Status = patient.Status;
                    PageTitle = IsReadOnly ? $"查看患者 - {Name}" : $"编辑患者 - {Name}";
                }
                else { await ShowErrorMessageAsync("未找到患者信息"); }
            }
            catch (Exception ex) { Logger.LogError(ex, "加载患者数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载患者数据", ex)); }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true; StatusMessage = PatientId == Guid.Empty ? "正在创建患者..." : "正在保存修改...";
                // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端默认为Enabled
                var dto = new PatientInputDto { Id = PatientId, Name = Name.Trim(), Gender = SelectedGender, BirthDate = BirthDate, IdNumber = IdNumber?.Trim(), PhoneNumber = PhoneNumber?.Trim(), Address = Address?.Trim() };
                var result = PatientId == Guid.Empty ? await _patientRepository.CreateAsync(dto) : await _patientRepository.UpdateAsync(dto);
                if (result != null) NavigateBack("ContentRegion", new NavigationParameters { { "RefreshList", true } });
                else await ShowErrorMessageAsync(PatientId == Guid.Empty ? "创建患者失败" : "更新患者失败");
            }
            catch (Exception ex) { Logger.LogError(ex, "保存患者失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存患者", ex)); }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }
    }
}
