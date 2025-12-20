using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>药材详情视图模型 - CRUD统一架构</summary>
    public class HerbDetailViewModel : UnifiedViewModelBase
    {
        private readonly IHerbRepository _herbRepository;

        private Guid _herbId;
        private bool _isEditMode = true;
        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private string? _origin;
        private string? _spec;
        private string _unit = "克";
        private decimal _price;
        private decimal? _costPrice;
        private string? _effect;
        private string? _usage;
        private string? _remark;
        private CommonStatus _status = CommonStatus.Enabled;

        public Guid HerbId { get => _herbId; set => SetProperty(ref _herbId, value); }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set { if (SetProperty(ref _isEditMode, value)) { RaisePropertyChanged(nameof(IsReadOnly)); RaisePropertyChanged(nameof(IsNameEditable)); SubmitCommand?.RaiseCanExecuteChanged(); SwitchToEditModeCommand?.RaiseCanExecuteChanged(); } }
        }

        public bool IsReadOnly => !IsEditMode;
        public bool IsCreateMode => HerbId == Guid.Empty;
        public bool IsEditOrViewMode => HerbId != Guid.Empty;
        public bool IsNameEditable => IsCreateMode;

        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(50, ErrorMessage = "药材名称长度不能超过50个字符")]
        public string Name
        {
            get => _name;
            set { if (SetProperty(ref _name, value)) { PinYinCode = PinYinHelper.GetPinYinCode(value); ValidateProperty(); SubmitCommand?.RaiseCanExecuteChanged(); } }
        }

        public string PinYinCode { get => _pinYinCode; private set => SetProperty(ref _pinYinCode, value); }

        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        public string? Origin { get => _origin; set { if (SetProperty(ref _origin, value)) ValidateProperty(); } }

        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        public string? Spec { get => _spec; set { if (SetProperty(ref _spec, value)) ValidateProperty(); } }

        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit { get => _unit; set { if (SetProperty(ref _unit, value)) ValidateProperty(); } }

        [Range(0, 999999.99, ErrorMessage = "零售价必须在0-999999.99之间")]
        public decimal Price { get => _price; set { if (SetProperty(ref _price, value)) ValidateProperty(); } }

        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        public decimal? CostPrice { get => _costPrice; set { if (SetProperty(ref _costPrice, value)) ValidateProperty(); } }

        [StringLength(500, ErrorMessage = "功效长度不能超过500个字符")]
        public string? Effect { get => _effect; set { if (SetProperty(ref _effect, value)) ValidateProperty(); } }

        [StringLength(200, ErrorMessage = "用法用量长度不能超过200个字符")]
        public string? Usage { get => _usage; set { if (SetProperty(ref _usage, value)) ValidateProperty(); } }

        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get => _remark; set { if (SetProperty(ref _remark, value)) ValidateProperty(); } }

        public CommonStatus Status { get => _status; set => SetProperty(ref _status, value); }
        public IEnumerable<CommonStatus> StatusOptions { get; }

        public DelegateCommand SubmitCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand GoBackCommand { get; }
        public DelegateCommand SwitchToEditModeCommand { get; }

        public HerbDetailViewModel(
            IHerbRepository herbRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            StatusOptions = Enum.GetValues<CommonStatus>();

            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
            GoBackCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
            SwitchToEditModeCommand = new DelegateCommand(() => { IsEditMode = true; PageTitle = $"编辑药材 - {Name}"; }, () => IsEditOrViewMode && IsReadOnly && !IsLoading);
        }

        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            if (parameters.ContainsKey("HerbId")) HerbId = parameters.GetValue<Guid>("HerbId");
            if (parameters.ContainsKey("ReadOnly")) IsEditMode = !parameters.GetValue<bool>("ReadOnly");
            else if (HerbId != Guid.Empty) IsEditMode = true;
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            if (HerbId != Guid.Empty) await LoadHerbAsync();
            else InitializeEmptyForm();
        }

        private async Task LoadHerbAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载药材信息...";
                var herb = await _herbRepository.GetByIdAsync(HerbId);
                if (herb != null)
                {
                    Name = herb.Name; PinYinCode = herb.PinYinCode ?? PinYinHelper.GetPinYinCode(herb.Name);
                    Origin = herb.Origin; Spec = herb.Spec; Unit = herb.Unit; Price = herb.Price;
                    CostPrice = herb.CostPrice; Effect = herb.Effect; Usage = herb.Usage; Remark = herb.Remark; Status = herb.Status;
                    PageTitle = IsEditMode ? $"编辑药材 - {Name}" : $"药材详情 - {Name}";
                }
                else { await ShowErrorMessageAsync("未找到药材信息"); }
            }
            catch (Exception ex) { Logger.LogError(ex, "加载药材数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载药材数据", ex)); }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        private void InitializeEmptyForm()
        {
            Name = string.Empty; PinYinCode = string.Empty; Origin = null; Spec = null;
            Unit = "克"; Price = 0; CostPrice = null; Effect = null; Usage = null; Remark = null;
            Status = CommonStatus.Enabled; PageTitle = "创建药材";
        }

        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = IsCreateMode ? "正在创建药材..." : "正在保存药材...";
                if (IsCreateMode) await CreateHerbAsync();
                else await UpdateHerbAsync();
            }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        private async Task CreateHerbAsync()
        {
            try
            {
                // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端默认为Enabled
                var createDto = new HerbInputDto
                {
                    Name = Name.Trim(), PinYinCode = PinYinCode?.Trim(), Origin = Origin?.Trim(), Spec = Spec?.Trim(),
                    Unit = Unit.Trim(), Price = Price, CostPrice = CostPrice, Effect = Effect?.Trim(),
                    Usage = Usage?.Trim(), Remark = Remark?.Trim()
                };
                var result = await _herbRepository.CreateAsync(createDto);
                if (result != null) NavigateBack("ContentRegion", new NavigationParameters { { "RefreshList", true } });
                else await ShowErrorMessageAsync("创建药材失败");
            }
            catch (Exception ex) { Logger.LogError(ex, "创建药材异常"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建药材", ex)); }
        }

        private async Task UpdateHerbAsync()
        {
            try
            {
                // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端保持原值
                var updateDto = new HerbInputDto
                {
                    Id = HerbId, Name = Name.Trim(), PinYinCode = PinYinCode?.Trim(), Origin = Origin?.Trim(),
                    Spec = Spec?.Trim(), Unit = Unit.Trim(), Price = Price, CostPrice = CostPrice,
                    Effect = Effect?.Trim(), Usage = Usage?.Trim(), Remark = Remark?.Trim()
                };
                var result = await _herbRepository.UpdateAsync(updateDto);
                if (result != null) NavigateBack("ContentRegion", new NavigationParameters { { "RefreshList", true } });
                else await ShowErrorMessageAsync("更新药材失败");
            }
            catch (Exception ex) { Logger.LogError(ex, "更新药材异常"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新药材", ex)); }
        }

        private bool CanSubmit() => !IsReadOnly && !IsLoading && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Unit) && !HasErrors;
    }
}
